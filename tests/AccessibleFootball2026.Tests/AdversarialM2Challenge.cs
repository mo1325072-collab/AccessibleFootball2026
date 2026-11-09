using System;
using System.Linq;
using System.Numerics;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Input;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;

namespace AccessibleFootball2026.Tests;

public static class AdversarialM2Challenge
{
    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception($"Assertion Failed: {message}");
    }

    public static void RunAll(Action<string, Action> runTest)
    {
        Console.WriteLine("\n--- EMPIRICAL CHALLENGER (M2): ADVERSARIAL STRESS TEST SUITE ---");

        runTest("7A. Teammate Receiver: Active movement towards ball with ZERO arrow input", 
            TestReceiver_ActiveMovement_ZeroUserInput);

        runTest("7B. Teammate Receiver: Sprint speed calculation exceeds standard walking speed", 
            TestReceiver_SprintSpeed_ExceedsWalking);

        runTest("7C. Teammate Receiver: Directional arrow keys held during transit do NOT divert receiver", 
            TestReceiver_ArrowKeysHeld_DoNotDivertReceiver);

        runTest("7D. Immediate Ball Control: Trap triggers at distance <= 2.2m with ownership & player transfer", 
            TestImmediateBallControl_Threshold2_2M);

        runTest("7E. Immediate Ball Control: No premature trap when distance is 2.25m (> 2.2m)", 
            TestImmediateBallControl_NoPrematureTrapAt2_25M);

        runTest("7F. Immediate Ball Control: High ball (Z > 1.5m) does not trap until ball descends", 
            TestImmediateBallControl_HighBall_WaitsForDescent);

        runTest("7G. Opponent AI Balance: Defender at 20m does NOT swarm or intercept during transit", 
            TestOpponentAI_NoSwarmFrom20M_DuringTransit);

        runTest("7H. Opponent AI Balance: Legendary difficulty pressRange capped at 8m during pass transit", 
            TestOpponentAI_LegendaryPressRange_CappedAt8M);

        runTest("7I. Opponent AI Balance: 1.3m tackle radius vs 2.2m teammate trap priority", 
            TestOpponentAI_TackleRadius1_3M_Versus_TeammateTrap2_2M);

        runTest("7J. Full Pass Lifecycle: Complete end-to-end pass and trap with zero user input", 
            TestFullPassLifecycle_EndToEnd_NoUserInput);
    }

    private static void TestReceiver_ActiveMovement_ZeroUserInput()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var passer = match.ActiveUserPlayer;

        // Execute short pass aimed forward (+X)
        match.ExecutePass(new Vector2(1.0f, 0.0f), isThroughBall: false);

        Assert(match.IsPassInTransit, "Pass must be in transit");
        Assert(match.ActivePassTransit != null, "ActivePassTransit must not be null");

        var receiver = match.ActivePassTransit!.IntendedReceiver;
        Vector2 initialPos = receiver.Position2D;

        // Step 10 ticks (0.2s) with zero keyboard input
        for (int i = 0; i < 10; i++)
        {
            match.Update(0.02f);
            if (!match.IsPassInTransit) break;
        }

        float distMoved = Vector2.Distance(receiver.Position2D, initialPos);
        Assert(distMoved > 0.15f, $"Receiver must move actively towards ball (moved {distMoved:F2}m)");
        Assert(receiver.FacingDirection.Length() > 0.1f, "Receiver must set a facing direction");

        // Verify receiver faces the ball
        Vector2 toBall = Vector2.Normalize(match.Ball.Position2D - receiver.Position2D);
        float dot = Vector2.Dot(receiver.FacingDirection, toBall);
        Assert(dot > 0.7f, $"Receiver must face incoming ball trajectory (dot={dot:F2})");
    }

    private static void TestReceiver_SprintSpeed_ExceedsWalking()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        match.ExecutePass(new Vector2(1.0f, 0.0f));

        var receiver = match.ActivePassTransit!.IntendedReceiver;
        Vector2 startPos = receiver.Position2D;

        // Trace frames
        for (int i = 0; i < 10; i++)
        {
            match.Update(0.02f);
            Console.WriteLine($"\n[TRACE 7B Frame {i+1}] InTransit={match.IsPassInTransit}, BallOwner={match.Ball.Owner?.Name ?? "null"}, ActiveUser={match.ActiveUserPlayer.Name}, BallPos={match.Ball.Position}, BallVel={match.Ball.Velocity}, RecPos={receiver.Position2D}");
            if (!match.IsPassInTransit) break;
        }

        float distMoved = Vector2.Distance(receiver.Position2D, startPos);
        float estimatedSpeed = distMoved / 0.2f;

        Assert(estimatedSpeed > 6.0f, $"Receiver speed ({estimatedSpeed:F2} m/s) must be sprint speed > 6.0 m/s");
    }

    private static void TestReceiver_ArrowKeysHeld_DoNotDivertReceiver()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        match.ExecutePass(new Vector2(1.0f, 0.0f));

        var receiver = match.ActivePassTransit!.IntendedReceiver;
        Vector2 startPos = receiver.Position2D;

        const int VK_DOWN = 0x28;
        inputHandler.HandleKeyDown(VK_DOWN, isPossession: false);

        for (int i = 0; i < 10; i++)
        {
            inputHandler.HandleKeyDown(VK_DOWN, isPossession: false);
            match.Update(0.02f);
            if (!match.IsPassInTransit) break;
        }

        inputHandler.HandleKeyUp(VK_DOWN);

        Vector2 ballPos = match.Ball.Position2D;
        float distToBall = Vector2.Distance(receiver.Position2D, ballPos);
        float originalDistToBall = Vector2.Distance(startPos, ballPos);
        Assert(distToBall < originalDistToBall, 
            "Receiver must close distance to ball regardless of arrow keys held by user for passer");
    }

    private static void TestImmediateBallControl_Threshold2_2M()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        bool trappedEventFired = false;
        eventBus.OnMatchEvent += evt =>
        {
            if (evt.Type == MatchEventType.BallTrapped) trappedEventFired = true;
        };

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var passer = match.ActiveUserPlayer;
        var receiver = homeTeam.Players.First(p => p != passer && p.Role != PlayerRole.Goalkeeper);

        match.ExecutePass(Vector2.Normalize(receiver.Position2D - passer.Position2D));
        Assert(match.IsPassInTransit, "Pass must be in transit");

        // Place ball at exactly 2.19m (<= 2.2m threshold) and Z = 0.0m
        match.Ball.Position = new Vector3(receiver.Position2D.X + 2.19f, receiver.Position2D.Y, 0.0f);
        match.Ball.Velocity = new Vector3(-1.0f, 0.0f, 0.0f);

        match.Update(0.02f);

        Assert(match.Ball.Owner == receiver, "Ball.Owner must be the receiver");
        Assert(receiver.HasBall, "receiver.HasBall must be true");
        Assert(match.ActiveUserPlayer == receiver, "ActiveUserPlayer must be receiver immediately");
        Assert(match.Ball.State == BallState.Controlled, "Ball.State must be Controlled");
        Assert(match.Ball.Velocity == Vector3.Zero, "Ball.Velocity must be Zero when trapped");
        Assert(!match.IsPassInTransit, "IsPassInTransit must be false");
        Assert(match.ActivePassTransit == null, "ActivePassTransit must be cleared");
        Assert(trappedEventFired, "BallTrapped event must be fired");
    }

    private static void TestImmediateBallControl_NoPrematureTrapAt2_25M()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var passer = match.ActiveUserPlayer;
        var receiver = homeTeam.Players.First(p => p != passer && p.Role != PlayerRole.Goalkeeper);

        match.ExecutePass(Vector2.Normalize(receiver.Position2D - passer.Position2D));

        // Place ball at 2.25m (> 2.2m)
        match.Ball.Position = new Vector3(receiver.Position2D.X + 2.25f, receiver.Position2D.Y, 0.0f);
        match.Ball.Velocity = new Vector3(-1.0f, 0.0f, 0.0f);

        match.Update(0.02f);

        Assert(match.Ball.Owner == null, "Ball must NOT be owned yet at 2.25m distance");
        Assert(match.ActiveUserPlayer == passer, "ActiveUserPlayer must still be passer");
        Assert(match.IsPassInTransit, "Pass must still be in transit at 2.25m");
    }

    private static void TestImmediateBallControl_HighBall_WaitsForDescent()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var passer = match.ActiveUserPlayer;
        var receiver = homeTeam.Players.First(p => p != passer && p.Role != PlayerRole.Goalkeeper);

        match.ExecutePass(Vector2.Normalize(receiver.Position2D - passer.Position2D));

        // Place ball within 1.0m horizontally, but at Z = 2.5m (over head height)
        match.Ball.Position = new Vector3(receiver.Position2D.X + 1.0f, receiver.Position2D.Y, 2.5f);
        match.Ball.Velocity = new Vector3(0.0f, 0.0f, -4.0f); // Falling

        Console.WriteLine($"\n[TRACE 7F Before Update] Ball.Owner={match.Ball.Owner?.Name ?? "null"}, Ball.Pos={match.Ball.Position}");
        match.Update(0.02f);
        Console.WriteLine($"[TRACE 7F After Update] Ball.Owner={match.Ball.Owner?.Name ?? "null"}, Ball.Pos={match.Ball.Position}, InTransit={match.IsPassInTransit}");

        // While Z > 1.5m, should NOT trap
        Assert(match.Ball.Owner == null, "Ball must NOT trap while flying high above player (Z > 1.5m)");
        Assert(match.IsPassInTransit, "Pass must still be in transit while high in the air");

        // Now lower ball height to 1.2m (<= 1.5m)
        match.Ball.Position = new Vector3(receiver.Position2D.X + 1.0f, receiver.Position2D.Y, 1.2f);
        match.Update(0.02f);

        Assert(match.Ball.Owner == receiver, "Ball must trap once Z descends to <= 1.5m");
        Assert(match.ActiveUserPlayer == receiver, "ActiveUserPlayer must transfer to receiver upon trap");
    }

    private static void TestOpponentAI_NoSwarmFrom20M_DuringTransit()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        MatchSettings.Current.Difficulty = DifficultyLevel.Legendary;

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var passer = match.ActiveUserPlayer;
        var receiver = homeTeam.Players.First(p => p != passer && p.Role != PlayerRole.Goalkeeper);

        match.ExecutePass(Vector2.Normalize(receiver.Position2D - passer.Position2D));

        var defender = awayTeam.Players.First(p => p.Role != PlayerRole.Goalkeeper);
        defender.Position2D = match.Ball.Position2D + new Vector2(20.0f, 0.0f);
        defender.HomePosition = defender.Position2D;
        Vector2 defInitialPos = defender.Position2D;

        for (int i = 0; i < 10; i++)
        {
            match.Update(0.02f);
            if (!match.IsPassInTransit) break;
        }

        MatchSettings.Current.Difficulty = DifficultyLevel.Normal;

        float distMovedTowardsBall = defInitialPos.X - defender.Position2D.X;
        Assert(distMovedTowardsBall <= 0.05f, 
            $"Opponent defender at 20m must NOT swarm towards the ball during pass transit (moved {distMovedTowardsBall:F2}m)");
    }

    private static void TestOpponentAI_LegendaryPressRange_CappedAt8M()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        MatchSettings.Current.Difficulty = DifficultyLevel.Legendary;

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var passer = match.ActiveUserPlayer;
        var receiver = homeTeam.Players.First(p => p != passer && p.Role != PlayerRole.Goalkeeper);

        match.ExecutePass(Vector2.Normalize(receiver.Position2D - passer.Position2D));

        var defA = awayTeam.Players.First(p => p.Role != PlayerRole.Goalkeeper);
        defA.Position2D = match.Ball.Position2D + new Vector2(11.0f, 0.0f);
        defA.HomePosition = defA.Position2D;
        Vector2 defAInitial = defA.Position2D;

        match.Update(0.02f);

        Assert(Math.Abs(defA.Position2D.X - defAInitial.X) < 0.02f, 
            "Defender at 11m must NOT press because press range is capped at 8m during transit");

        defA.Position2D = match.Ball.Position2D + new Vector2(6.0f, 0.0f);
        Vector2 defAInside = defA.Position2D;

        match.Update(0.02f);

        Assert(defA.Position2D.X < defAInside.X, 
            "Defender at 6m (inside 8m) SHOULD press towards ball");

        MatchSettings.Current.Difficulty = DifficultyLevel.Normal;
    }

    private static void TestOpponentAI_TackleRadius1_3M_Versus_TeammateTrap2_2M()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var passer = match.ActiveUserPlayer;
        var teammate = homeTeam.Players.First(p => p != passer && p.Role != PlayerRole.Goalkeeper);
        var opponent = awayTeam.Players.First(p => p.Role != PlayerRole.Goalkeeper);

        match.ExecutePass(Vector2.Normalize(teammate.Position2D - passer.Position2D));

        match.Ball.Position = new Vector3(50.0f, 30.0f, 0.0f);
        match.Ball.Velocity = Vector3.Zero;

        teammate.Position2D = new Vector2(52.0f, 30.0f);
        opponent.Position2D = new Vector2(48.5f, 30.0f);

        match.Update(0.02f);

        Assert(match.Ball.Owner == teammate, "Teammate must trap ball with 2.2m priority");
        Assert(match.ActiveUserPlayer == teammate, "ActiveUserPlayer must be teammate");
        Assert(match.Ball.Owner != opponent, "Opponent at 1.5m must not steal ball (tackle radius is 1.3m)");
    }

    private static void TestFullPassLifecycle_EndToEnd_NoUserInput()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        bool trapped = false;
        eventBus.OnMatchEvent += evt =>
        {
            if (evt.Type == MatchEventType.BallTrapped) trapped = true;
        };

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var passer = match.ActiveUserPlayer;

        // Passer is at (52.5, 34). Aim towards Player 8 (LW at 72, 14)
        match.ExecutePass(new Vector2(1.0f, 0.0f), isThroughBall: false);
        Assert(match.IsPassInTransit, "Pass must start in transit");
        Assert(match.ActivePassTransit!.IntendedReceiver != null, "Receiver must be assigned");

        var actualReceiver = match.ActivePassTransit!.IntendedReceiver;
        Console.WriteLine($"\n[TRACE 7J Init] Passer={passer.Name} at {passer.Position2D}, Receiver={actualReceiver!.Name} at {actualReceiver.Position2D}, TargetPos={match.ActivePassTransit!.TargetPosition}, BallVel={match.Ball.Velocity}");

        bool caught = false;
        for (int frame = 0; frame < 75; frame++)
        {
            match.Update(0.02f);
            if (frame % 10 == 0 || !match.IsPassInTransit || match.Ball.Owner != null)
            {
                Console.WriteLine($"[TRACE 7J Frame {frame}] InTransit={match.IsPassInTransit}, BallOwner={match.Ball.Owner?.Name ?? "null"}, BallPos={match.Ball.Position}, RecPos={actualReceiver.Position2D}, Dist={Vector2.Distance(actualReceiver.Position2D, match.Ball.Position2D):F2}");
            }
            if (match.Ball.Owner == actualReceiver)
            {
                caught = true;
                break;
            }
            if (!match.IsPassInTransit)
            {
                break;
            }
        }

        Assert(caught, "Receiver must successfully catch the ball in flight without any user input");
        Assert(match.Ball.Owner == actualReceiver, "Ball owner must be receiver");
        Assert(match.ActiveUserPlayer == actualReceiver, "Active user player must be receiver");
        Assert(!match.IsPassInTransit, "Pass must no longer be in transit after catch");
        Assert(trapped, "BallTrapped event must have been published");
    }
}
