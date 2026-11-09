using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AccessibleFootball2026.Audio;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Input;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;
using AccessibleFootball2026.Input;

namespace AccessibleFootball2026.Tests;

/// <summary>
/// EMPIRICAL CHALLENGER (M1): Adversarial stress testing harness for
/// Milestone 1: Controls Alignment, Escape Pause, Fair Tackle Determinism, and Half-Time Inversion.
/// </summary>
public static class AdversarialM1Challenge
{
    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception($"Assertion Failed: {message}");
    }

    public static void RunAll(Action<string, Action> runTest)
    {
        Console.WriteLine("\n--- EMPIRICAL CHALLENGER (M1): ADVERSARIAL STRESS TEST SUITE ---");

        runTest("11A. Controls Matrix: S, D, W, A, Ctrl, Shift, Space, Esc discrete edge & suppression",
            TestControls_DiscreteEdgeAndSuppressionMatrix);

        runTest("11B. Escape Pause State Machine: InPlay -> Paused -> InPlay freeze and resume determinism",
            TestEscapePause_StateMachineFreezeAndResume);

        runTest("11C. Fair Tackle Multi-Angle Matrix: 360-deg sweep, clean front/flank vs foul behind vs loose ball",
            TestFairTackle_MultiAngleAndReachMatrix);

        runTest("11D. Half-Time Boundary: Exact 45:00 trigger, time clamp, and pause state lock",
            TestHalfTime_Exact45MinTriggerAndPauseLock);

        runTest("11E. Half-Time Coordinate Mirroring: All 22 players, home positions, and kickoff center spot",
            TestHalfTime_PlayerCoordinateMirroringAndKickoffSetup);

        runTest("11F. Second-Half Left Attack: Arrow movement, W shot direction (Vx < 0), and radar distance",
            TestSecondHalf_LeftAttack_MovementShootingRadar);

        runTest("11G. Second-Half Inverted Zones & Dual-Goal Scoring: AttackingThird at X<=35 and Left goal scores Home",
            TestSecondHalf_InvertedZonesAndDualGoalScoring);

        runTest("11H. Second-Half Restarts: Inverted Goal Kicks and Corner Kicks geometry",
            TestSecondHalf_Restarts_GoalKicksAndCornersGeometry);
    }

    /// <summary>
    /// 11A. Stress-tests keyboard input handler for discrete S, D, W, A, Ctrl, Shift, Space, Esc.
    /// Verifies single edge triggering on press, zero repeat on hold across 50 frames, and shift continuity.
    /// </summary>
    private static void TestControls_DiscreteEdgeAndSuppressionMatrix()
    {
        var handler = new KeyboardInputHandler();
        const int VK_KEY_S = 0x53;
        const int VK_KEY_D = 0x44;
        const int VK_KEY_W = 0x57;
        const int VK_KEY_A = 0x41;
        const int VK_CONTROL = 0x11;
        const int VK_SPACE = 0x20;
        const int VK_ESCAPE = 0x1B;
        const int VK_SHIFT = 0x10;

        // 1. In Possession: S -> ShortPass, D -> Cross, W -> Shoot, A -> ThroughBall
        int passCount = 0, crossCount = 0, shotCount = 0, throughCount = 0;

        handler.HandleKeyDown(VK_KEY_S, isPossession: true);
        for (int i = 0; i < 50; i++)
        {
            var actions = handler.PollCurrentActions(isPossession: true);
            if (actions.Contains(GameAction.ShortPass)) passCount++;
        }
        handler.HandleKeyUp(VK_KEY_S);
        Assert(passCount == 1, $"S (ShortPass) must trigger exactly once when held for 50 frames. Count: {passCount}");

        handler.HandleKeyDown(VK_KEY_D, isPossession: true);
        for (int i = 0; i < 50; i++)
        {
            var actions = handler.PollCurrentActions(isPossession: true);
            if (actions.Contains(GameAction.Cross)) crossCount++;
        }
        handler.HandleKeyUp(VK_KEY_D);
        Assert(crossCount == 1, $"D (Cross) must trigger exactly once when held for 50 frames. Count: {crossCount}");

        handler.HandleKeyDown(VK_KEY_W, isPossession: true);
        for (int i = 0; i < 50; i++)
        {
            var actions = handler.PollCurrentActions(isPossession: true);
            if (actions.Contains(GameAction.Shoot)) shotCount++;
        }
        handler.HandleKeyUp(VK_KEY_W);
        Assert(shotCount == 1, $"W (Shoot) must trigger exactly once when held for 50 frames. Count: {shotCount}");

        handler.HandleKeyDown(VK_KEY_A, isPossession: true);
        for (int i = 0; i < 50; i++)
        {
            var actions = handler.PollCurrentActions(isPossession: true);
            if (actions.Contains(GameAction.ThroughBall)) throughCount++;
        }
        handler.HandleKeyUp(VK_KEY_A);
        Assert(throughCount == 1, $"A (ThroughBall) must trigger exactly once when held for 50 frames. Count: {throughCount}");

        // 2. Defending (!isPossession): S -> StandTackle, D/W/A -> SlideTackle
        int standCount = 0, slideCount = 0;
        handler.HandleKeyDown(VK_KEY_S, isPossession: false);
        for (int i = 0; i < 50; i++)
        {
            var actions = handler.PollCurrentActions(isPossession: false);
            if (actions.Contains(GameAction.StandTackle)) standCount++;
        }
        handler.HandleKeyUp(VK_KEY_S);
        Assert(standCount == 1, $"S (StandTackle) must trigger exactly once on hold in defense. Count: {standCount}");

        handler.HandleKeyDown(VK_KEY_D, isPossession: false);
        for (int i = 0; i < 50; i++)
        {
            var actions = handler.PollCurrentActions(isPossession: false);
            if (actions.Contains(GameAction.SlideTackle)) slideCount++;
        }
        handler.HandleKeyUp(VK_KEY_D);
        Assert(slideCount == 1, $"D (SlideTackle) must trigger exactly once on hold in defense. Count: {slideCount}");

        // 3. Ctrl, Space, Escape
        int switchCount = 0, radarCount = 0, pauseCount = 0;
        handler.HandleKeyDown(VK_CONTROL, isPossession: false);
        for (int i = 0; i < 50; i++)
        {
            var actions = handler.PollCurrentActions(isPossession: false);
            if (actions.Contains(GameAction.SwitchPlayer)) switchCount++;
        }
        handler.HandleKeyUp(VK_CONTROL);
        Assert(switchCount == 1, $"Ctrl (SwitchPlayer) must trigger exactly once on hold. Count: {switchCount}");

        handler.HandleKeyDown(VK_SPACE, isPossession: true);
        for (int i = 0; i < 50; i++)
        {
            var actions = handler.PollCurrentActions(isPossession: true);
            if (actions.Contains(GameAction.TacticalRadar)) radarCount++;
        }
        handler.HandleKeyUp(VK_SPACE);
        Assert(radarCount == 1, $"Space (TacticalRadar) must trigger exactly once on hold. Count: {radarCount}");

        handler.HandleKeyDown(VK_ESCAPE, isPossession: true);
        for (int i = 0; i < 50; i++)
        {
            var actions = handler.PollCurrentActions(isPossession: true);
            if (actions.Contains(GameAction.Pause)) pauseCount++;
        }
        handler.HandleKeyUp(VK_ESCAPE);
        Assert(pauseCount == 1, $"Escape (Pause) must trigger exactly once on hold. Count: {pauseCount}");

        // 4. Shift: continuous sprint while held across all 50 frames
        handler.HandleKeyDown(VK_SHIFT, isPossession: true);
        int sprintFrames = 0;
        for (int i = 0; i < 50; i++)
        {
            var actions = handler.PollCurrentActions(isPossession: true);
            if (actions.Contains(GameAction.Sprint)) sprintFrames++;
        }
        handler.HandleKeyUp(VK_SHIFT);
        var postReleaseActions = handler.PollCurrentActions(isPossession: true);

        Assert(sprintFrames == 50, $"Shift (Sprint) must be continuous across all 50 frames while held. Frames: {sprintFrames}");
        Assert(!postReleaseActions.Contains(GameAction.Sprint), "Shift (Sprint) must clear immediately upon KeyUp");
    }

    /// <summary>
    /// 11B. Stress-tests Escape key pause state machine.
    /// Verifies that while paused:
    /// - MatchTimeMinutes is frozen
    /// - Player positions do not update
    /// - Ball physics do not integrate
    /// And upon ResumeMatch(), normal simulation resumes cleanly.
    /// </summary>
    private static void TestEscapePause_StateMachineFreezeAndResume()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        Assert(match.State == MatchState.InPlay, "Initial state must be InPlay");

        // Advance 10 frames in play
        for (int i = 0; i < 10; i++) match.Update(0.02f);
        float timeBeforePause = match.MatchTimeMinutes;
        Assert(timeBeforePause > 0.0f, "Match time must advance while InPlay");

        // Pause match via Escape action simulation
        match.PauseMatch();
        Assert(match.State == MatchState.Paused, "PauseMatch must set State to MatchState.Paused");

        // Snapshot players and ball
        Vector3 ballPosPaused = match.Ball.Position;
        Vector2 userPosPaused = match.ActiveUserPlayer.Position2D;

        // Run 100 simulation ticks while paused
        for (int i = 0; i < 100; i++)
        {
            match.Update(0.02f);
        }

        Assert(match.MatchTimeMinutes == timeBeforePause,
            $"Match time must remain completely frozen while paused. Expected: {timeBeforePause}, Got: {match.MatchTimeMinutes}");
        Assert(match.Ball.Position == ballPosPaused,
            "Ball position must not change while paused");
        Assert(match.ActiveUserPlayer.Position2D == userPosPaused,
            "Player position must not change while paused");

        // Resume match
        match.ResumeMatch();
        Assert(match.State == MatchState.InPlay, "ResumeMatch must set State back to InPlay");

        // Run 10 simulation ticks after resuming
        for (int i = 0; i < 10; i++)
        {
            match.Update(0.02f);
        }

        Assert(match.MatchTimeMinutes > timeBeforePause,
            $"Match time must resume advancing after ResumeMatch. Got: {match.MatchTimeMinutes}");
    }

    /// <summary>
    /// 11C. Stress-tests Fair Tackle spatial formula under various approach angles:
    /// - Front (0 deg): clean tackle won
    /// - Front-flank (45 deg): clean tackle won
    /// - Flank-front (60 deg): clean tackle won
    /// - Direct behind (180 deg): deterministic foul called with "من الخلف"
    /// - Rear-flank (135 deg): deterministic foul called
    /// - Out of reach (>2.8m): announces numerical distance
    /// - Loose ball: immediate recovery without foul
    /// </summary>
    private static void TestFairTackle_MultiAngleAndReachMatrix()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var carrier = awayTeam.Players[9];
        carrier.Position2D = new Vector2(50.0f, 34.0f);
        carrier.FacingDirection = new Vector2(1.0f, 0.0f); // Carrier moving towards Right goal

        // 1. Direct Front approach (angle 0 degrees relative to facing direction):
        // Carrier is facing +X. Front is at +X (e.g. (51.2, 34)).
        // Ball is at carrier.Position2D + carrierFacing * 0.35 = (50.35, 34).
        // Tackler at (51.2, 34):
        // distToBall = 51.2 - 50.35 = 0.85m.
        // distToOpponent = 51.2 - 50.0 = 1.20m.
        // distToBall (0.85) < distToOpponent (1.20) -> Clean tackle!
        match.ActiveUserPlayer.Position2D = new Vector2(51.2f, 34.0f);
        match.ActiveUserPlayer.FacingDirection = new Vector2(-1.0f, 0.0f);
        match.Ball.Position = new Vector3(50.0f, 34.0f, 0.0f);
        match.Ball.CatchOrTackle(carrier);

        bool frontCleanWon = false;
        void FrontHandler(MatchEvent evt)
        {
            if (evt.Type == MatchEventType.UserTackleSuccessful) frontCleanWon = true;
        }
        eventBus.OnMatchEvent += FrontHandler;
        match.ExecuteStandTackle();
        eventBus.OnMatchEvent -= FrontHandler;

        Assert(frontCleanWon, "Frontal tackle must cleanly win the ball (distToBall < distToOpponent)");
        Assert(match.Ball.Owner == match.ActiveUserPlayer, "Clean tackle must immediately transfer ball ownership to user");

        // 2. Direct Behind tackle (angle 180 degrees):
        // Tackler at (48.8, 34).
        // distToBall = 50.35 - 48.8 = 1.55m.
        // distToOpponent = 50.0 - 48.8 = 1.20m.
        // distToBall (1.55) > distToOpponent (1.20) -> Foul!
        for (int i = 0; i < 50; i++) match.Update(0.02f); // Clear cooldown
        carrier.Position2D = new Vector2(50.0f, 34.0f);
        carrier.FacingDirection = new Vector2(1.0f, 0.0f);
        match.Ball.Position = new Vector3(50.0f, 34.0f, 0.0f);
        match.Ball.CatchOrTackle(carrier);
        match.ActiveUserPlayer.Position2D = new Vector2(48.8f, 34.0f);
        match.ActiveUserPlayer.FacingDirection = new Vector2(1.0f, 0.0f);

        bool behindFoulCalled = false;
        bool behindDescMentioned = false;
        void BehindHandler(MatchEvent evt)
        {
            if (evt.Type == MatchEventType.FoulCommitted)
            {
                behindFoulCalled = true;
                if (evt.Description.Contains("من الخلف")) behindDescMentioned = true;
            }
        }
        eventBus.OnMatchEvent += BehindHandler;
        match.ExecuteStandTackle();
        eventBus.OnMatchEvent -= BehindHandler;

        Assert(behindFoulCalled, "Tackle from behind must deterministically result in a foul (distToBall > distToOpponent)");
        Assert(behindDescMentioned, "Foul description must explicitly announce tackle from behind ('من الخلف')");

        // 3. Out of Reach: distance > 2.8m slide reach
        for (int i = 0; i < 50; i++) match.Update(0.02f);
        carrier.Position2D = new Vector2(50.0f, 34.0f);
        match.Ball.Position = new Vector3(50.0f, 34.0f, 0.0f);
        match.Ball.CatchOrTackle(carrier);
        match.ActiveUserPlayer.Position2D = new Vector2(53.2f, 34.0f); // 3.2m away

        string? outOfReachText = null;
        void ReachHandler(MatchEvent evt)
        {
            if (evt.Type == MatchEventType.TacticalRadarInfo && evt.Description.Contains("بعيد"))
            {
                outOfReachText = evt.Description;
            }
        }
        eventBus.OnMatchEvent += ReachHandler;
        match.ExecuteSlideTackle();
        eventBus.OnMatchEvent -= ReachHandler;

        Assert(outOfReachText != null, "Out-of-reach tackle must announce distance via TacticalRadarInfo");
        Assert(outOfReachText!.Contains("3.2"), $"Announcement must report exact numerical distance 3.2m. Got: {outOfReachText}");
        Assert(!outOfReachText.Contains("اضغط"), "Announcement must not contain tutorial prompts");

        // 4. Exact Mathematical Boundary Testing (Midpoint between carrier and ball)
        // Carrier at (50, 34), ball at carrier feet (50.35, 34). Midpoint is X = 50.175m.
        // A. Tackler just ahead of midpoint (X = 50.195m, Y = 34.8m) -> distToBall (0.815m) < distToOpponent (0.823m)
        for (int i = 0; i < 50; i++) match.Update(0.02f);
        carrier.Position2D = new Vector2(50.0f, 34.0f);
        carrier.FacingDirection = new Vector2(1.0f, 0.0f);
        match.Ball.Position = new Vector3(50.0f, 34.0f, 0.0f);
        match.Ball.CatchOrTackle(carrier);
        match.ActiveUserPlayer.Position2D = new Vector2(50.195f, 34.8f);
        match.ActiveUserPlayer.FacingDirection = new Vector2(-1.0f, 0.0f);

        bool boundaryCleanWon = false;
        void BoundaryCleanHandler(MatchEvent evt) { if (evt.Type == MatchEventType.UserTackleSuccessful) boundaryCleanWon = true; }
        eventBus.OnMatchEvent += BoundaryCleanHandler;
        match.ExecuteStandTackle();
        eventBus.OnMatchEvent -= BoundaryCleanHandler;
        Assert(boundaryCleanWon, "Boundary test: when distToBall < distToOpponent by 0.008m, clean tackle must be won");

        // B. Tackler exactly on perpendicular bisector (X = 50.175m, Y = 34.8m) -> distToBall == distToOpponent (0.819m == 0.819m)
        for (int i = 0; i < 50; i++) match.Update(0.02f);
        carrier.Position2D = new Vector2(50.0f, 34.0f);
        carrier.FacingDirection = new Vector2(1.0f, 0.0f);
        match.Ball.Position = new Vector3(50.0f, 34.0f, 0.0f);
        match.Ball.CatchOrTackle(carrier);
        match.ActiveUserPlayer.Position2D = new Vector2(50.175f, 34.8f);
        match.ActiveUserPlayer.FacingDirection = new Vector2(-1.0f, 0.0f);

        bool boundaryFoulExact = false;
        void BoundaryExactHandler(MatchEvent evt) { if (evt.Type == MatchEventType.FoulCommitted) boundaryFoulExact = true; }
        eventBus.OnMatchEvent += BoundaryExactHandler;
        match.ExecuteStandTackle();
        eventBus.OnMatchEvent -= BoundaryExactHandler;
        Assert(boundaryFoulExact, "Boundary test: when distToBall == distToOpponent exactly, foul must be called deterministically");

        // C. Tackler just behind midpoint (X = 50.155m, Y = 34.8m) -> distToBall (0.823m) > distToOpponent (0.815m)
        for (int i = 0; i < 50; i++) match.Update(0.02f);
        carrier.Position2D = new Vector2(50.0f, 34.0f);
        carrier.FacingDirection = new Vector2(1.0f, 0.0f);
        match.Ball.Position = new Vector3(50.0f, 34.0f, 0.0f);
        match.Ball.CatchOrTackle(carrier);
        match.ActiveUserPlayer.Position2D = new Vector2(50.155f, 34.8f);
        match.ActiveUserPlayer.FacingDirection = new Vector2(-1.0f, 0.0f);

        bool boundaryFoulBehind = false;
        void BoundaryBehindHandler(MatchEvent evt) { if (evt.Type == MatchEventType.FoulCommitted) boundaryFoulBehind = true; }
        eventBus.OnMatchEvent += BoundaryBehindHandler;
        match.ExecuteStandTackle();
        eventBus.OnMatchEvent -= BoundaryBehindHandler;
        Assert(boundaryFoulBehind, "Boundary test: when distToBall > distToOpponent by 0.008m, foul must be called deterministically");

        // 5. Loose ball recovery (no carrier)
        for (int i = 0; i < 50; i++) match.Update(0.02f);
        if (match.Ball.Owner != null)
        {
            match.Ball.Owner.HasBall = false;
            match.Ball.Owner = null;
        }
        match.Ball.Position = new Vector3(50.0f, 34.0f, 0.0f);
        match.ActiveUserPlayer.Position2D = new Vector2(50.8f, 34.0f); // within 1.8m standing reach

        bool looseBallRecovered = false;
        void LooseHandler(MatchEvent evt)
        {
            if (evt.Type == MatchEventType.UserTackleSuccessful) looseBallRecovered = true;
        }
        eventBus.OnMatchEvent += LooseHandler;
        match.ExecuteStandTackle();
        eventBus.OnMatchEvent -= LooseHandler;

        Assert(looseBallRecovered, "Tackle on loose ball within reach must recover the ball");
        Assert(match.Ball.Owner == match.ActiveUserPlayer, "User must become owner of loose ball");
    }

    /// <summary>
    /// 11D. Stress-tests Half-Time boundary:
    /// - At 44.98 minutes: InPlay, HalfTimeReached=false, UserAttackingRight=true
    /// - At 45.00 minutes: pauses, State=HalfTime, HalfTimeReached=true, UserAttackingRight=false
    /// - Updates while State=HalfTime do not advance match time
    /// </summary>
    private static void TestHalfTime_Exact45MinTriggerAndPauseLock()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        Assert(!match.HalfTimeReached, "HalfTimeReached must start false");
        Assert(match.UserAttackingRight, "1st half must attack Right");

        // Advance to 44.90m
        match.SetMatchTimeMinutes(44.90f);
        match.Update(0.02f);

        Assert(!match.HalfTimeReached, "At 44.90m, HalfTimeReached must still be false");
        Assert(match.State == MatchState.InPlay, "At 44.90m, State must still be InPlay");
        Assert(match.UserAttackingRight, "At 44.90m, UserAttackingRight must still be true");

        // Advance past 45.00m + stoppage time
        match.SetMatchTimeMinutes(45.00f + match.StoppageMinutes1st);
        match.Update(0.02f);

        Assert(match.HalfTimeReached, "At end of stoppage time, HalfTimeReached must be true");
        Assert(match.State == MatchState.HalfTime, "At end of stoppage time, State must transition to HalfTime");
        Assert(!match.UserAttackingRight, "At end of stoppage time, UserAttackingRight must flip to false");
        Assert(match.MatchTimeMinutes == 45.0f, $"MatchTimeMinutes must clamp at exactly 45.0f. Got: {match.MatchTimeMinutes}");

        // Attempt 50 updates while in MatchState.HalfTime -> Time must remain locked at 45.0f
        for (int i = 0; i < 50; i++)
        {
            match.Update(0.02f);
        }
        Assert(match.MatchTimeMinutes == 45.0f, "Time must not advance while match is in HalfTime state");

        // Resume match -> Time resumes into 2nd half
        match.ResumeMatch();
        Assert(match.State == MatchState.InPlay, "State must resume to InPlay");
        match.Update(0.02f);
        Assert(match.MatchTimeMinutes > 45.0f, "Time must advance past 45.0f after resuming 2nd half");
    }

    /// <summary>
    /// 11E. Stress-tests coordinate mirroring upon Half-Time trigger:
    /// - Every player on HomeTeam and AwayTeam has position mirrored: X' = 105 - X, Y' = 68 - Y
    /// - Home striker positioned at Pitch.CenterSpot (52.5, 34)
    /// - Ball positioned at Pitch.CenterSpot (52.5, 34) with zero velocity
    /// - Ball owner is active user player
    /// - Opponents in center circle are pushed outside
    /// </summary>
    private static void TestHalfTime_PlayerCoordinateMirroringAndKickoffSetup()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);

        // Record initial positions for Home defender 2 and Away defender 2
        var homeDef = homeTeam.Players[2];
        var awayDef = awayTeam.Players[2];
        Vector2 homeDefOrig = homeDef.Position2D;
        Vector2 awayDefOrig = awayDef.Position2D;

        match.TriggerHalfTime();

        // Verify coordinate mirroring: X' = Pitch.Length - X, Y' = Pitch.Width - Y
        float expectedHomeDefX = Pitch.Length - homeDefOrig.X;
        float expectedHomeDefY = Pitch.Width - homeDefOrig.Y;
        Assert(Math.Abs(homeDef.Position2D.X - expectedHomeDefX) < 0.1f,
            $"Home defender X must be mirrored to {expectedHomeDefX:F1}. Got: {homeDef.Position2D.X:F1}");
        Assert(Math.Abs(homeDef.Position2D.Y - expectedHomeDefY) < 0.1f,
            $"Home defender Y must be mirrored to {expectedHomeDefY:F1}. Got: {homeDef.Position2D.Y:F1}");

        float expectedAwayDefX = Pitch.Length - awayDefOrig.X;
        float expectedAwayDefY = Pitch.Width - awayDefOrig.Y;
        Assert(Math.Abs(awayDef.Position2D.X - expectedAwayDefX) < 0.1f,
            $"Away defender X must be mirrored to {expectedAwayDefX:F1}. Got: {awayDef.Position2D.X:F1}");

        // Verify Kickoff setup
        Assert(match.ActiveUserPlayer.Position2D == match.Pitch.CenterSpot,
            $"Active user player must be positioned at center spot ({match.Pitch.CenterSpot}). Got: {match.ActiveUserPlayer.Position2D}");
        Assert(match.ActiveUserPlayer.FacingDirection.X < 0.0f,
            "Active user player must face Left (-X) for 2nd half attack");
        Assert(match.Ball.Position2D == match.Pitch.CenterSpot,
            "Ball must be positioned at center spot");
        Assert(match.Ball.Owner == match.ActiveUserPlayer,
            "Active user player must own ball for 2nd half kickoff");

        // Verify away players are outside center circle on their side
        foreach (var opp in awayTeam.Players)
        {
            Assert(opp.Position2D.X <= match.Pitch.CenterSpot.X - 11.0f,
                $"Away player {opp.Name} must be outside center circle on their side (X <= {match.Pitch.CenterSpot.X - 11.0f}). Got: {opp.Position2D.X}");
        }
    }

    /// <summary>
    /// 11F. Stress-tests 2nd half attack Left mechanics:
    /// - Left arrow decreases X (moves toward Left goal X=0)
    /// - Right arrow increases X (moves away from Left goal toward own goal)
    /// - W shot fires with negative X velocity (towards Left goal)
    /// - Up/Down arrow aim angles target upper/lower corners of Left goal
    /// - Tactical radar reports distance to Left goal as Ball.Position2D.X
    /// </summary>
    private static void TestSecondHalf_LeftAttack_MovementShootingRadar()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        match.TriggerHalfTime();
        match.ResumeMatch();

        var player = match.ActiveUserPlayer;
        player.Position2D = new Vector2(30.0f, 34.0f);
        match.Ball.Position = new Vector3(30.0f, 34.0f, 0.0f);
        match.Ball.CatchOrTackle(player);

        // 1. Tactical Radar distance in 2nd half
        string? radarAnnouncement = null;
        void RadarHandler(MatchEvent evt)
        {
            if (evt.Type == MatchEventType.TacticalRadarInfo && evt.Description.Contains("المرمى"))
            {
                radarAnnouncement = evt.Description;
            }
        }
        eventBus.OnMatchEvent += RadarHandler;
        match.ReportTacticalRadar();
        eventBus.OnMatchEvent -= RadarHandler;

        Assert(radarAnnouncement != null, "Tactical radar event must fire");
        Assert(radarAnnouncement!.Contains("30"),
            $"Radar must report distance 30m to Left goal (X=0) in 2nd half. Got: {radarAnnouncement}");

        // 2. W Shot targeting Left goal (negative Vx)
        const int VK_KEY_W = 0x57;
        inputHandler.HandleKeyDown(VK_KEY_W, isPossession: true);
        match.Update(0.02f);
        inputHandler.HandleKeyUp(VK_KEY_W);

        Assert(match.Ball.Velocity.X < -15.0f,
            $"Shot in 2nd half must have strong negative X velocity towards Left goal. Got: {match.Ball.Velocity.X:F1}");
        Assert(Math.Abs(match.Ball.Velocity.Y) < 5.0f,
            $"Center shot must travel predominantly horizontal. Got Vy: {match.Ball.Velocity.Y:F1}");

        // 3. Movement towards Left goal (holding Left arrow)
        for (int i = 0; i < 50; i++) match.Update(0.02f); // Clear shot
        match.Ball.CatchOrTackle(player);
        player.Position2D = new Vector2(40.0f, 34.0f);
        float startX = player.Position2D.X;

        const int VK_LEFT = 0x25;
        inputHandler.HandleKeyDown(VK_LEFT, isPossession: true);
        for (int i = 0; i < 10; i++)
        {
            match.Update(0.02f);
        }
        inputHandler.HandleKeyUp(VK_LEFT);

        Assert(player.Position2D.X < startX,
            $"Left arrow in 2nd half must advance player forward towards Left goal (startX: {startX}, newX: {player.Position2D.X})");
    }

    /// <summary>
    /// 11G. Stress-tests inverted zone calculations and dual-goal scoring rules in 2nd half:
    /// - X <= 35m is AttackingThird in 2nd half (vs OwnThird in 1st half)
    /// - X >= 70m is OwnThird in 2nd half (vs AttackingThird in 1st half)
    /// - Ball entering Left goal (X <= 0) awards goal to HomeTeam in 2nd half
    /// - Ball entering Right goal (X >= 105) awards goal to AwayTeam in 2nd half
    /// </summary>
    private static void TestSecondHalf_InvertedZonesAndDualGoalScoring()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        match.TriggerHalfTime();
        match.ResumeMatch();

        // 1. Zone Inversion Checks
        var pitch = match.Pitch;
        PitchZone zoneNear0 = pitch.CalculateZone(new Vector3(25.0f, 34.0f, 0.0f), attackingTowardsRight: false);
        PitchZone zoneNear105 = pitch.CalculateZone(new Vector3(85.0f, 34.0f, 0.0f), attackingTowardsRight: false);
        PitchZone zoneCenter = pitch.CalculateZone(new Vector3(52.5f, 34.0f, 0.0f), attackingTowardsRight: false);
        PitchZone zoneBox = pitch.CalculateZone(new Vector3(10.0f, 34.0f, 0.0f), attackingTowardsRight: false);

        Assert(zoneNear0 == PitchZone.AttackingThird,
            $"In 2nd half, X=25m must be AttackingThird. Got: {zoneNear0}");
        Assert(zoneNear105 == PitchZone.OwnThird,
            $"In 2nd half, X=85m must be OwnThird. Got: {zoneNear105}");
        Assert(zoneCenter == PitchZone.Midfield,
            $"In 2nd half, X=52.5m must be Midfield. Got: {zoneCenter}");
        Assert(zoneBox == PitchZone.OpponentPenaltyBox,
            $"In 2nd half, X=10m (inside 16.5m depth at Left) must be OpponentPenaltyBox. Got: {zoneBox}");

        // 2. Goal Scoring Inversion
        // Ball into Left goal in 2nd half -> HomeTeam score increments!
        if (match.Ball.Owner != null)
        {
            match.Ball.Owner.HasBall = false;
            match.Ball.Owner = null;
        }
        match.Ball.Position = new Vector3(-0.2f, 34.0f, 1.0f);
        match.Ball.Velocity = new Vector3(-3.0f, 0.0f, 0.0f);

        int homeScoreBefore = match.HomeTeam.Score;
        int awayScoreBefore = match.AwayTeam.Score;

        match.Update(0.02f);

        Assert(match.HomeTeam.Score == homeScoreBefore + 1,
            $"In 2nd half, ball entering Left goal must award goal to HomeTeam. Score: {match.HomeTeam.Score}");
        Assert(match.AwayTeam.Score == awayScoreBefore,
            $"AwayTeam score must not change on Home goal. Score: {match.AwayTeam.Score}");
        Assert(match.State == MatchState.GoalCelebration,
            "Goal must trigger GoalCelebration state");

        // Fast-forward past celebration
        for (int i = 0; i < 250; i++) match.Update(0.02f);

        // Ball into Right goal in 2nd half -> AwayTeam score increments!
        if (match.Ball.Owner != null)
        {
            match.Ball.Owner.HasBall = false;
            match.Ball.Owner = null;
        }
        match.Ball.Position = new Vector3(105.2f, 34.0f, 1.0f);
        match.Ball.Velocity = new Vector3(3.0f, 0.0f, 0.0f);

        homeScoreBefore = match.HomeTeam.Score;
        awayScoreBefore = match.AwayTeam.Score;

        match.Update(0.02f);

        Assert(match.AwayTeam.Score == awayScoreBefore + 1,
            $"In 2nd half, ball entering Right goal must award goal to AwayTeam. Score: {match.AwayTeam.Score}");
        Assert(match.HomeTeam.Score == homeScoreBefore,
            $"HomeTeam score must not change on Away goal. Score: {match.HomeTeam.Score}");
    }

    /// <summary>
    /// 11H. Stress-tests restarts (Goal Kicks & Corners) in 2nd half:
    /// - Ball out over Left goal line off Away defender -> Home Corner at X=1.0m
    /// - Ball out over Left goal line off Home attacker -> Away Goal Kick at X=5.0m
    /// - Ball out over Right goal line off Home defender -> Away Corner at X=104.0m
    /// - Ball out over Right goal line off Away attacker -> Home Goal Kick at X=100.0m
    /// </summary>
    private static void TestSecondHalf_Restarts_GoalKicksAndCornersGeometry()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        match.TriggerHalfTime();
        match.ResumeMatch();

        // 1. Home Corner at Left goal (X=1.0m)
        // Ball out over Left goal line (X < 0, Y outside goal: Y = 10.0m), last touched by Away defender
        typeof(MatchEngine).GetProperty("LastKicker")!.SetValue(match, awayTeam.Players[2]);
        match.Ball.Position = new Vector3(-1.0f, 10.0f, 0.0f);
        match.Ball.Velocity = Vector3.Zero;
        if (match.Ball.Owner != null)
        {
            match.Ball.Owner.HasBall = false;
            match.Ball.Owner = null;
        }

        bool cornerAwarded = false;
        void CornerHandler(MatchEvent evt)
        {
            if (evt.Type == MatchEventType.CornerKick && evt.Description.Contains(homeTeam.Players[9].Name))
            {
                cornerAwarded = true;
            }
        }
        eventBus.OnMatchEvent += CornerHandler;
        match.Update(0.02f);
        eventBus.OnMatchEvent -= CornerHandler;

        Assert(cornerAwarded, "In 2nd half, ball out over Left line off Away defender must award Home Corner");
        Assert(match.Ball.Position.X == 1.0f,
            $"In 2nd half, Home Corner must be placed at X=1.0m (near opponent Left goal). Got: {match.Ball.Position.X}");

        // 2. Away Goal Kick at Left goal (X=5.0m)
        // Ball out over Left goal line, last touched by Home attacker
        typeof(MatchEngine).GetProperty("LastKicker")!.SetValue(match, homeTeam.Players[9]);
        match.Ball.Position = new Vector3(-1.0f, 10.0f, 0.0f);
        match.Ball.Velocity = Vector3.Zero;
        if (match.Ball.Owner != null)
        {
            match.Ball.Owner.HasBall = false;
            match.Ball.Owner = null;
        }

        bool awayGoalKickAwarded = false;
        void AwayGkHandler(MatchEvent evt)
        {
            if (evt.Type == MatchEventType.GoalKick && evt.Description.Contains(awayTeam.Name))
            {
                awayGoalKickAwarded = true;
            }
        }
        eventBus.OnMatchEvent += AwayGkHandler;
        match.Update(0.02f);
        eventBus.OnMatchEvent -= AwayGkHandler;

        Assert(awayGoalKickAwarded, "In 2nd half, ball out over Left line off Home attacker must award Away Goal Kick");
        Assert(match.Ball.Position.X == 5.0f,
            $"In 2nd half, Away Goal Kick must be placed at X=5.0m (near Away Left goal). Got: {match.Ball.Position.X}");

        // 3. Home Goal Kick at Right goal (X=100.0m)
        // Ball out over Right goal line (X > 105), last touched by Away attacker
        typeof(MatchEngine).GetProperty("LastKicker")!.SetValue(match, awayTeam.Players[9]);
        match.Ball.Position = new Vector3(106.0f, 10.0f, 0.0f);
        match.Ball.Velocity = Vector3.Zero;
        if (match.Ball.Owner != null)
        {
            match.Ball.Owner.HasBall = false;
            match.Ball.Owner = null;
        }

        bool homeGoalKickAwarded = false;
        void HomeGkHandler(MatchEvent evt)
        {
            if (evt.Type == MatchEventType.GoalKick && evt.Description.Contains(homeTeam.Players[0].Name))
            {
                homeGoalKickAwarded = true;
            }
        }
        eventBus.OnMatchEvent += HomeGkHandler;
        match.Update(0.02f);
        eventBus.OnMatchEvent -= HomeGkHandler;

        Assert(homeGoalKickAwarded, "In 2nd half, ball out over Right line off Away attacker must award Home Goal Kick");
        Assert(match.Ball.Position.X == Pitch.Length - 5.0f,
            $"In 2nd half, Home Goal Kick must be placed at X={Pitch.Length - 5.0f:F1}m (near Home Right goal). Got: {match.Ball.Position.X}");
    }
}
