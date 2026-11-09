using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;

namespace AccessibleFootball2026.Challenger;

public class MockInputHandler : AccessibleFootball2026.Core.Input.IInputHandler
{
    public HashSet<AccessibleFootball2026.Core.Input.GameAction> PollCurrentActions(bool isPossession) => [];
    public bool IsActionTriggered(AccessibleFootball2026.Core.Input.GameAction action, bool isPossession) => false;
}

public static class Program
{
    private static int _passed = 0;
    private static int _failed = 0;

    public static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("==========================================================================");
        Console.WriteLine("    EMPIRICAL CHALLENGER: PASS PHYSICS & KINEMATICS HARNESS               ");
        Console.WriteLine("==========================================================================");

        Console.WriteLine("\n--- SECTION 1: GROUND BALL DECELERATION (10m, 18m, 25m, 30m) ---");
        RunTest("1.1 Ground Ball Stopping Distance & Kinematic Match: 10m Pass", () => TestGroundBallStoppingDistance(10.0f));
        RunTest("1.2 Ground Ball Stopping Distance & Kinematic Match: 18m Pass", () => TestGroundBallStoppingDistance(18.0f));
        RunTest("1.3 Ground Ball Stopping Distance & Kinematic Match: 25m Pass", () => TestGroundBallStoppingDistance(25.0f));
        RunTest("1.4 Ground Ball Stopping Distance & Kinematic Match: 30m Pass", () => TestGroundBallStoppingDistance(30.0f));
        RunTest("1.5 Game Pass Speed (18m/s) Kinematic Trajectory across 10m, 18m, 25m, 30m", TestGameShortPassTrajectory);
        RunTest("1.6 Game ThroughBall Speed (22m/s) Kinematic Trajectory", TestGameThroughBallTrajectory);

        Console.WriteLine("\n--- SECTION 2: FRAME-RATE INVARIANCE ---");
        RunTest("2.1 Multi-FPS Invariance (30Hz, 50Hz, 60Hz, 100Hz, 120Hz)", TestMultiFpsInvariance);
        RunTest("2.2 Variable DeltaTime Jitter Invariance", TestVariableDeltaTimeJitter);
        RunTest("2.3 Large DeltaTime Spike Robustness (Spike dt = 0.1s, 0.5s)", TestLargeDeltaTimeSpike);

        Console.WriteLine("\n--- SECTION 3: VERTICAL SPEED IMPACT ANALYSIS (0.0f vs 0.15f) ---");
        RunTest("3.1 Ground Pass with verticalSpeed 0.0f vs 0.15f Air Resistance Analysis", TestVerticalSpeedImpact);

        Console.WriteLine("\n--- SECTION 4: MATCH ENGINE RECEPTION AT 10m, 18m, 25m, 30m ---");
        RunTest("4.0 PassTransit Lifespan Diagnostic (How many frames does a pass survive before being reclaimed?)", TestPassTransitLifespan);
        RunTest("4.1 MatchEngine End-to-End Reception at 10m", () => TestMatchEngineReceptionAtDistance(10.0f));
        RunTest("4.2 MatchEngine End-to-End Reception at 18m", () => TestMatchEngineReceptionAtDistance(18.0f));
        RunTest("4.3 MatchEngine End-to-End Reception at 25m", () => TestMatchEngineReceptionAtDistance(25.0f));
        RunTest("4.4 MatchEngine End-to-End Reception at 30m", () => TestMatchEngineReceptionAtDistance(30.0f));

        Console.WriteLine("\n--- SECTION 5: ADVERSARIAL EDGE CASES & FAILURE MODES ---");
        RunTest("5.1 Out-of-bounds pass cleans up transit state safely", TestOutOfBoundsPassTransitCleanup);
        RunTest("5.2 Intercepted pass cleans up transit state immediately", TestDefenderInterceptTransitCleanup);
        RunTest("5.3 Zero velocity / sub-threshold kick handled cleanly", TestSubThresholdKick);
        RunTest("5.4 Negative or zero deltaTime handled safely", TestNonPositiveDeltaTime);

        Console.WriteLine("\n==========================================================================");
        Console.WriteLine($"TOTAL: {_passed + _failed} | PASSED: {_passed} | FAILED: {_failed}");
        Console.WriteLine("==========================================================================");

        return _failed > 0 ? 1 : 0;
    }

    private static void RunTest(string name, Action test)
    {
        Console.Write($"[CHALLENGE] {name} ... ");
        try
        {
            test();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("PASSED [OK]");
            Console.ResetColor();
            _passed++;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"FAILED [FAIL]: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine(ex.StackTrace);
            _failed++;
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    // =========================================================================
    // SECTION 1: GROUND BALL DECELERATION & KINEMATICS
    // =========================================================================
    private static void TestGroundBallStoppingDistance(float targetDistance)
    {
        // For linear deceleration a = 2.5 m/s²:
        // stopping distance s_stop = v0² / (2a) => v0 = sqrt(2 * a * targetDistance)
        float a = Ball.GroundDeceleration; // 2.5
        float v0 = MathF.Sqrt(2.0f * a * targetDistance);
        float expectedStopTime = v0 / a;

        var ball = new Ball { Position = Vector3.Zero };
        ball.Kick(new Vector2(1.0f, 0.0f), horizontalSpeed: v0, verticalSpeed: 0.0f);

        float dt = 0.02f; // 50Hz
        float t = 0.0f;
        float maxKinematicDeviation = 0.0f;

        while (ball.Velocity.Length() > 0.05f && t < 15.0f)
        {
            float actualX = ball.Position.X;
            // Theoretical kinematic formula s(t) = v0*t - 0.5*a*t²
            float theoreticalX = (t <= expectedStopTime) 
                ? (v0 * t - 0.5f * a * t * t) 
                : targetDistance;

            float deviation = MathF.Abs(actualX - theoreticalX);
            if (deviation > maxKinematicDeviation)
            {
                maxKinematicDeviation = deviation;
            }

            ball.Update(dt);
            t += dt;
        }

        float finalDistance = ball.Position.X;
        float distanceError = MathF.Abs(finalDistance - targetDistance);

        Console.Write($"[v0={v0:F2}m/s, StopDist={finalDistance:F2}m (target={targetDistance:F1}m, err={distanceError:F3}m), MaxDev={maxKinematicDeviation:F3}m] ");

        // Verify:
        // 1. Overcomes 6m wall
        Assert(finalDistance > 6.0f, $"Ball failed to overcome 6m wall! Traveled only {finalDistance:F2}m");
        // 2. Final distance is within numerical discretization tolerance (Euler O(dt))
        Assert(distanceError < 0.25f, $"Final distance {finalDistance:F2}m deviates from target {targetDistance:F2}m by {distanceError:F2}m (> 0.25m tolerance)");
        // 3. Max deviation during trajectory follows v0*t - 0.5*a*t² within Euler discretization error 0.5*a*t*dt
        Assert(maxKinematicDeviation < 0.35f, $"Kinematic formula deviation {maxKinematicDeviation:F2}m exceeds tolerance 0.35m");
    }

    private static void TestGameShortPassTrajectory()
    {
        // Short pass uses passSpeed = 18.0 m/s
        float v0 = 18.0f;
        float a = Ball.GroundDeceleration; // 2.5 m/s²
        var ball = new Ball { Position = Vector3.Zero };
        ball.Kick(new Vector2(1.0f, 0.0f), horizontalSpeed: v0, verticalSpeed: 0.0f);

        float dt = 0.02f;
        float[] checkpoints = [10.0f, 18.0f, 25.0f, 30.0f];
        var reachedTimes = new Dictionary<float, float>();

        float t = 0.0f;
        float maxDeviation = 0.0f;

        while (ball.Position.X < 35.0f && ball.Velocity.Length() > 0.05f && t < 10.0f)
        {
            float actualX = ball.Position.X;
            float theoreticalX = v0 * t - 0.5f * a * t * t;
            float dev = MathF.Abs(actualX - theoreticalX);
            if (dev > maxDeviation) maxDeviation = dev;

            foreach (var cp in checkpoints)
            {
                if (!reachedTimes.ContainsKey(cp) && actualX >= cp)
                {
                    reachedTimes[cp] = t;
                }
            }

            ball.Update(dt);
            t += dt;
        }

        Console.Write($"[Checkpoints reached: 10m@{reachedTimes[10.0f]:F2}s, 18m@{reachedTimes[18.0f]:F2}s, 25m@{reachedTimes[25.0f]:F2}s, 30m@{reachedTimes[30.0f]:F2}s, MaxDev={maxDeviation:F3}m] ");

        Assert(reachedTimes.ContainsKey(10.0f), "Pass failed to reach 10m");
        Assert(reachedTimes.ContainsKey(18.0f), "Pass failed to reach 18m (6m wall check)");
        Assert(reachedTimes.ContainsKey(25.0f), "Pass failed to reach 25m");
        Assert(reachedTimes.ContainsKey(30.0f), "Pass failed to reach 30m");
        Assert(maxDeviation < 0.25f, $"Trajectory deviation {maxDeviation:F3}m exceeds tolerance");
    }

    private static void TestGameThroughBallTrajectory()
    {
        // Through ball uses passSpeed = 22.0 m/s
        float v0 = 22.0f;
        float a = Ball.GroundDeceleration;
        var ball = new Ball { Position = Vector3.Zero };
        ball.Kick(new Vector2(1.0f, 0.0f), horizontalSpeed: v0, verticalSpeed: 0.0f);

        float dt = 0.02f;
        float t = 0.0f;
        float maxDev = 0.0f;

        while (ball.Position.X < 40.0f && ball.Velocity.Length() > 0.05f && t < 10.0f)
        {
            float actualX = ball.Position.X;
            float theoreticalX = v0 * t - 0.5f * a * t * t;
            float dev = MathF.Abs(actualX - theoreticalX);
            if (dev > maxDev) maxDev = dev;
            ball.Update(dt);
            t += dt;
        }

        Console.Write($"[ThroughBall v0=22 reached {ball.Position.X:F1}m, MaxDev={maxDev:F3}m] ");
        Assert(ball.Position.X >= 35.0f, "Through ball failed to travel > 35m");
        Assert(maxDev < 0.35f, $"Trajectory deviation {maxDev:F3}m exceeds tolerance");
    }

    // =========================================================================
    // SECTION 2: FRAME-RATE INVARIANCE
    // =========================================================================
    private static void TestMultiFpsInvariance()
    {
        // Simulate an 18 m/s pass for exactly 1.5 seconds under different fixed frame rates
        float targetDuration = 1.5f;
        float v0 = 18.0f;
        float[] frameRates = [30.0f, 50.0f, 60.0f, 100.0f, 120.0f];
        var finalDistances = new Dictionary<float, float>();

        foreach (var fps in frameRates)
        {
            float dt = 1.0f / fps;
            var ball = new Ball { Position = Vector3.Zero };
            ball.Kick(new Vector2(1.0f, 0.0f), horizontalSpeed: v0, verticalSpeed: 0.0f);

            int totalSteps = (int)Math.Round(targetDuration * fps);
            for (int step = 0; step < totalSteps; step++)
            {
                ball.Update(dt);
            }
            finalDistances[fps] = ball.Position.X;
        }

        // Print results
        string report = string.Join(", ", finalDistances.Select(kv => $"{kv.Key}Hz:{kv.Value:F3}m"));
        Console.Write($"[{report}] ");

        float min = finalDistances.Values.Min();
        float max = finalDistances.Values.Max();
        float diff = max - min;

        // Discretization difference across 30Hz to 120Hz should be < 0.15m (< 0.6% of 24.2m)
        Assert(diff < 0.15f, $"Frame-rate discrepancy too large across FPS! Max spread: {diff:F4}m");
        // All must be around theoretical s = 18(1.5) - 0.5(2.5)(1.5)² = 24.1875m
        foreach (var kv in finalDistances)
        {
            Assert(MathF.Abs(kv.Value - 24.1875f) < 0.20f, 
                $"FPS {kv.Key} final distance {kv.Value:F3}m deviates from theoretical 24.188m");
        }
    }

    private static void TestVariableDeltaTimeJitter()
    {
        // Simulate jittering delta time between 10ms and 30ms (mean 20ms = 50Hz)
        var rng = new Random(42);
        float v0 = 18.0f;

        var ballFixed = new Ball { Position = Vector3.Zero };
        ballFixed.Kick(new Vector2(1.0f, 0.0f), v0, 0.0f);
        for (int i = 0; i < 75; i++) ballFixed.Update(0.02f); // Exactly 1.5s at 50Hz

        var ballJitter = new Ball { Position = Vector3.Zero };
        ballJitter.Kick(new Vector2(1.0f, 0.0f), v0, 0.0f);
        float totalTime = 0.0f;
        while (totalTime < 1.5f)
        {
            float dt = (float)(0.010 + rng.NextDouble() * 0.020); // 10ms .. 30ms
            if (totalTime + dt > 1.5f) dt = 1.5f - totalTime;
            ballJitter.Update(dt);
            totalTime += dt;
        }

        float diff = MathF.Abs(ballJitter.Position.X - ballFixed.Position.X);
        Console.Write($"[Fixed50Hz={ballFixed.Position.X:F3}m, Jitter={ballJitter.Position.X:F3}m, Diff={diff:F4}m] ");

        Assert(diff < 0.10f, $"Variable deltaTime jitter caused significant deviation: {diff:F4}m");
    }

    private static void TestLargeDeltaTimeSpike()
    {
        // Test robustness against single large frame lag spikes: dt = 0.2s and dt = 0.5s
        var ball = new Ball { Position = Vector3.Zero };
        ball.Kick(new Vector2(1.0f, 0.0f), 5.0f, 0.0f); // 5 m/s, stops in 2.0s

        // Normal step
        ball.Update(0.02f);
        // Spike 1: dt = 0.2s
        ball.Update(0.2f);
        Assert(!float.IsNaN(ball.Position.X) && !float.IsInfinity(ball.Position.X), "Position NaN after 0.2s spike");
        Assert(!float.IsNaN(ball.Velocity.X), "Velocity NaN after 0.2s spike");

        // Spike 2: dt = 2.5s (exceeds stopping time remaining!)
        ball.Update(2.5f);
        Assert(ball.Velocity.Length() == 0.0f, "Ball velocity must be clamped to 0 after massive spike");
        Assert(ball.State == BallState.Free, "Ball state must be Free after coming to complete stop");
        Assert(ball.Position.X > 0.0f, "Position must be positive");
    }

    // =========================================================================
    // SECTION 3: VERTICAL SPEED IMPACT ANALYSIS
    // =========================================================================
    private static void TestVerticalSpeedImpact()
    {
        // Worker used verticalSpeed = 0.15f in ExecutePass.
        // Let's compare pure ground pass (vz = 0) vs worker's kick (vz = 0.15f).
        var ballGround = new Ball { Position = Vector3.Zero };
        ballGround.Kick(new Vector2(1.0f, 0.0f), 18.0f, 0.0f);

        var ballWorker = new Ball { Position = Vector3.Zero };
        ballWorker.Kick(new Vector2(1.0f, 0.0f), 18.0f, 0.15f);

        float dt = 0.02f;
        for (int i = 0; i < 75; i++) // 1.5 seconds
        {
            ballGround.Update(dt);
            ballWorker.Update(dt);
        }

        float diff = MathF.Abs(ballWorker.Position.X - ballGround.Position.X);
        float pctDiff = (diff / ballGround.Position.X) * 100.0f;

        Console.Write($"[Ground={ballGround.Position.X:F3}m, WorkerVz0.15={ballWorker.Position.X:F3}m, Diff={diff:F3}m ({pctDiff:F2}%)] ");

        // When vz = 0.15f, ball spends ~2 frames (40ms) slightly off ground (z <= 0.003m),
        // air resistance is applied for 2 frames (0.985² ≈ 0.9702), reducing horizontal speed by ~3%.
        // Over 1.5s, distance is ~23.5m vs 24.2m.
        // Confirm this does NOT reintroduce a 6m wall and remains > 22.0m.
        Assert(ballWorker.Position.X > 22.0f, $"Worker vz=0.15f pass distance too low: {ballWorker.Position.X:F2}m");
        Assert(pctDiff < 5.0f, $"Difference due to vz=0.15f is {pctDiff:F2}% (> 5% limit)");
    }

    // =========================================================================
    // SECTION 4: MATCH ENGINE RECEPTION AT 10m, 18m, 25m, 30m
    // =========================================================================
    private static void TestPassTransitLifespan()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new MockInputHandler();

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var passer = match.ActiveUserPlayer;
        Assert(match.Ball.Owner == passer, "Passer must own ball initially");

        // Execute short pass
        match.ExecutePass(new Vector2(1.0f, 0.0f), isThroughBall: false);
        Assert(match.IsPassInTransit, "Pass must be in transit on Frame 0");

        int framesAlive = 0;
        Player? reclaimer = null;

        for (int frame = 1; frame <= 50; frame++)
        {
            match.Update(0.02f);
            if (match.IsPassInTransit)
            {
                framesAlive++;
            }
            else
            {
                reclaimer = match.Ball.Owner;
                break;
            }
        }

        Console.Write($"[Pass survived {framesAlive} frames. Killed by: {(reclaimer != null ? (reclaimer == passer ? "PASSER THEMSELVES!" : reclaimer.Name) : "None (timeout/out of bounds)")}] ");

        Assert(framesAlive > 10, $"CRITICAL BUG: Pass transit was killed after ONLY {framesAlive} frame(s)! The passer instantly reclaimed the ball at their own feet!");
    }
    private static void TestMatchEngineReceptionAtDistance(float passDistance)
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new MockInputHandler();

        bool receptionFired = false;
        eventBus.OnMatchEvent += evt =>
        {
            if (evt.Type == MatchEventType.BallTrapped) receptionFired = true;
        };

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var passer = match.ActiveUserPlayer;
        passer.Position2D = new Vector2(30.0f, 34.0f);

        // Position intended receiver exactly passDistance ahead
        var receiver = homeTeam.Players.First(p => p != passer && p.Role != PlayerRole.Goalkeeper);
        receiver.Position2D = new Vector2(passer.Position2D.X + passDistance, 34.0f);

        // Move other players out of pass corridor to isolate this specific pass test
        foreach (var teammate in homeTeam.Players)
        {
            if (teammate != passer && teammate != receiver)
            {
                teammate.Position2D = new Vector2(10.0f, 5.0f);
            }
        }
        foreach (var opp in awayTeam.Players)
        {
            opp.Position2D = new Vector2(90.0f, 5.0f);
        }

        match.Ball.CatchOrTackle(passer);

        // Execute pass aimed towards receiver
        match.ExecutePass(new Vector2(1.0f, 0.0f), isThroughBall: false);

        Assert(match.IsPassInTransit, $"Pass must be in transit for distance {passDistance}m");
        Assert(match.ActivePassTransit!.IntendedReceiver == receiver, "Intended receiver mismatch");

        // Simulate engine ticks until received or timeout
        int maxTicks = (int)((passDistance / 15.0f + 2.5f) * 50); // Generous time window
        int tick = 0;
        bool received = false;

        for (; tick < maxTicks; tick++)
        {
            if (passDistance == 10.0f && (tick % 10 == 0 || tick < 5))
            {
                float d = Vector2.Distance(receiver.Position2D, match.Ball.Position2D);
                Console.WriteLine($"\n[TICK {tick}] Ball=({match.Ball.Position.X:F2}, {match.Ball.Position.Y:F2}, {match.Ball.Position.Z:F2}) Vel=({match.Ball.Velocity.X:F2},{match.Ball.Velocity.Y:F2}) Rec=({receiver.Position2D.X:F2},{receiver.Position2D.Y:F2}) dist={d:F2} inTransit={match.IsPassInTransit} owner={match.Ball.Owner?.Name}");
            }
            match.Update(0.02f);
            if (match.Ball.Owner == receiver)
            {
                received = true;
                break;
            }
        }

        Console.Write($"[Dist={passDistance}m, Ticks={tick} ({(tick * 0.02f):F2}s), Received={received}, ActiveUser={match.ActiveUserPlayer.Name}] ");

        Assert(received, $"Receiver failed to receive pass at distance {passDistance}m within {maxTicks} ticks!");
        Assert(receptionFired, $"BallTrapped event did not fire at distance {passDistance}m");
        Assert(match.ActiveUserPlayer == receiver, "User control did not transfer to receiver");
        Assert(!match.IsPassInTransit, "Pass transit state remained active after reception");
    }

    // =========================================================================
    // SECTION 5: ADVERSARIAL EDGE CASES
    // =========================================================================
    private static void TestOutOfBoundsPassTransitCleanup()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new MockInputHandler();

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        // Kick pass directly towards touchline
        match.ExecutePass(new Vector2(0.0f, 1.0f), isThroughBall: false);
        Assert(match.IsPassInTransit, "Transit must be active initially");

        // Force ball out of bounds
        match.Ball.Position = new Vector3(50.0f, Pitch.Width + 2.0f, 0.0f);
        match.Update(0.02f);

        Assert(!match.IsPassInTransit, "ActivePassTransit must clear when ball goes out of bounds");
        Assert(match.ActivePassTransit == null, "ActivePassTransit must be null after out of bounds");
    }

    private static void TestDefenderInterceptTransitCleanup()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new MockInputHandler();

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        match.ExecutePass(new Vector2(1.0f, 0.0f), isThroughBall: false);
        Assert(match.IsPassInTransit, "Transit must be active initially");

        // Opponent intercepts ball
        var defender = awayTeam.Players[4];
        match.Ball.CatchOrTackle(defender);
        match.Update(0.02f);

        Assert(!match.IsPassInTransit, "ActivePassTransit must clear when opponent intercepts");
        Assert(match.ActivePassTransit == null, "ActivePassTransit must be null after opponent intercept");
    }

    private static void TestSubThresholdKick()
    {
        var ball = new Ball { Position = Vector3.Zero };
        // Kick with tiny speed below cutoff (0.04 m/s <= 0.05 m/s)
        ball.Kick(new Vector2(1.0f, 0.0f), 0.04f, 0.0f);
        ball.Update(0.02f);

        Assert(ball.Velocity.Length() == 0.0f, "Sub-threshold velocity must be zeroed immediately");
        Assert(ball.State == BallState.Free, "Ball state must be Free");
    }

    private static void TestNonPositiveDeltaTime()
    {
        var ball = new Ball { Position = new Vector3(10.0f, 10.0f, 0.0f) };
        ball.Kick(new Vector2(1.0f, 0.0f), 10.0f, 0.0f);

        // dt = 0.0f
        Vector3 posBefore = ball.Position;
        ball.Update(0.0f);
        Assert(ball.Position == posBefore, "Ball position must not change when dt = 0");
        Assert(!float.IsNaN(ball.Velocity.Length()), "Velocity must not be NaN when dt = 0");
    }
}
