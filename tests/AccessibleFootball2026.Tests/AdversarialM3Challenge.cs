using System;
using System.Collections.Generic;
using System.IO;
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
/// EMPIRICAL CHALLENGER (M3): Adversarial stress testing harness for
/// Milestone 3: Defense Switching & Distinct Tackling Mechanics.
/// </summary>
public static class AdversarialM3Challenge
{
    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception($"Assertion Failed: {message}");
    }

    public static void RunAll(Action<string, Action> runTest)
    {
        Console.WriteLine("\n--- EMPIRICAL CHALLENGER (M3): ADVERSARIAL STRESS TEST SUITE ---");

        runTest("9A. Player Switching: Hold 'Ctrl' across 100 frames -> Exactly 1 switch, zero 50Hz cycling",
            TestSwitching_HoldCtrl100Frames_SingleSwitchNoCycling);

        runTest("9B. Player Switching: ActiveUserPlayer strictly excluded from closest search even at 0.2m",
            TestSwitching_ActiveUserPlayerStrictlyExcluded);

        runTest("9C. Player Switching: Goalkeeper strictly excluded even when ball is at 0.1m",
            TestSwitching_GoalkeeperStrictlyExcluded);

        runTest("9D. Standing Tackle Reach: Misses at 1.9m (> 1.8m) with distance speech; hits at 1.7m (<= 1.8m)",
            TestStandingTackle_ReachThresholds_1_7MHit_1_9MMiss);

        runTest("9E. Slide Tackle Reach: Hits at 2.7m (<= 2.8m); misses at 2.9m (> 2.8m) with distance speech",
            TestSlideTackle_ReachThresholds_2_7MHit_2_9MMiss);

        runTest("9F. Reach Differential at 2.0m: Standing tackle misses (out of reach) while Slide tackle hits",
            TestReachDifferential_2_0M_StandingMisses_SlideHits);

        runTest("9G. Slide Tackle Impulse: +4.0m/s forward impulse across 0.3s yields ~1.20m displacement",
            TestSlideTackle_ForwardImpulse_DisplacementAndClamping);

        runTest("9H. Slide Recovery & Cooldown: Input lockout during slide, recovery period blocks new tackles",
            TestSlideTackle_RecoveryLockoutAndCooldownExpiry);

        runTest("9I. Zero-Clutter Audio Policy: TacticalRadar and tackle announcements contain zero tutorial prompts",
            TestZeroClutter_AnnouncementsContainNoTutorialPrompts);

        runTest("9J. Audio Telemetry End-to-End: Shot, Pass, Cross, Stand/Slide Tackles produce non-silent SFX",
            TestAudioTelemetry_EndToEndSFXRouting);

        runTest("9K. Zero-Clutter Goalkeeper Catch: In-match GK catch announces cleanly with zero 'اضغط' tutorial prompts",
            TestZeroClutter_GoalkeeperCatch_ZeroTutorialPrompts);
    }

    private static void TestSwitching_HoldCtrl100Frames_SingleSwitchNoCycling()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        int switchEventCount = 0;
        var switchedPlayers = new List<Player>();

        eventBus.OnMatchEvent += evt =>
        {
            if (evt.Type == MatchEventType.TacticalRadarInfo && evt.Description.Contains("-"))
            {
                switchEventCount++;
            }
        };

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var initialPlayer = match.ActiveUserPlayer;

        // Away team striker has ball firmly controlled -> user team is defending
        var opponent = awayTeam.Players[9];
        match.Ball.CatchOrTackle(opponent);
        match.Ball.Position = new Vector3(opponent.Position2D.X, opponent.Position2D.Y, 0.0f);

        // Simulate pressing and holding Ctrl (VK_CONTROL = 0x11)
        inputHandler.HandleKeyDown(0x11, false);

        // Frame 0: First update
        match.Update(0.02f);
        var firstSwitchedPlayer = match.ActiveUserPlayer;
        switchedPlayers.Add(firstSwitchedPlayer);

        Assert(firstSwitchedPlayer != initialPlayer,
            $"Frame 0 must switch away from initial player {initialPlayer.Name}");
        Assert(switchEventCount == 1,
            $"Frame 0 must emit exactly 1 switch event. Got: {switchEventCount}");

        // Frames 1 to 99: Hold Ctrl continuously with OS repeat simulation
        for (int frame = 1; frame < 100; frame++)
        {
            // Keep opponent in possession and at safe distance so AI does not shoot
            opponent.Position2D = new Vector2(65.0f, 34.0f);
            match.Ball.CatchOrTackle(opponent);

            // Simulate OS auto-repeat sending KeyDown every frame
            inputHandler.HandleKeyDown(0x11, false);
            match.Update(0.02f);

            Assert(match.ActiveUserPlayer == firstSwitchedPlayer,
                $"Frame {frame}: ActiveUserPlayer must remain {firstSwitchedPlayer.Name}. Changed to: {match.ActiveUserPlayer.Name} (50Hz cycling detected!)");
        }

        Assert(switchEventCount == 1,
            $"Holding Ctrl across 100 frames must produce EXACTLY 1 switch event. Got: {switchEventCount}");

        // Now release Ctrl and press again after debounce period (250ms)
        inputHandler.HandleKeyUp(0x11);
        System.Threading.Thread.Sleep(250);

        inputHandler.HandleKeyDown(0x11, false);
        match.Update(0.02f);

        Assert(switchEventCount == 2,
            $"Releasing and pressing Ctrl again after debounce must produce second switch. Got: {switchEventCount}");
        Assert(match.ActiveUserPlayer != firstSwitchedPlayer,
            "Second press must switch to another teammate");
    }

    private static void TestSwitching_ActiveUserPlayerStrictlyExcluded()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var initialActive = match.ActiveUserPlayer;

        // Position current active player at (50, 30)
        initialActive.Position2D = new Vector2(50.0f, 30.0f);

        // Place ball at (50.2, 30.0) -> Distance from ActiveUserPlayer is 0.2m!
        match.Ball.Position = new Vector3(50.2f, 30.0f, 0.0f);

        // Position closest teammate at (52.0, 30.0) -> Distance is 1.8m
        var teammate1 = homeTeam.Players.First(p => p != initialActive && p.Role != PlayerRole.Goalkeeper);
        teammate1.Position2D = new Vector2(52.0f, 30.0f);

        // Position other outfield teammates further away (> 10m)
        foreach (var p in homeTeam.Players.Where(p => p != initialActive && p != teammate1 && p.Role != PlayerRole.Goalkeeper))
        {
            p.Position2D = new Vector2(80.0f, 60.0f);
        }

        // Switch player
        match.SwitchToClosestPlayer();

        // Must select teammate1 (1.8m), NOT initialActive (0.2m)
        Assert(match.ActiveUserPlayer == teammate1,
            $"ActiveUserPlayer was at 0.2m from ball, teammate at 1.8m. Switch MUST exclude active player and select teammate! Selected: {match.ActiveUserPlayer.Name}");

        // Wait out debounce
        System.Threading.Thread.Sleep(250);

        // Now teammate1 is ActiveUserPlayer. Switch again.
        match.SwitchToClosestPlayer();

        // Must now exclude teammate1 and select initialActive (who is 0.2m away)
        Assert(match.ActiveUserPlayer == initialActive,
            $"Second switch must exclude teammate1 and select initialActive. Selected: {match.ActiveUserPlayer.Name}");
    }

    private static void TestSwitching_GoalkeeperStrictlyExcluded()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var gk = homeTeam.Players.First(p => p.Role == PlayerRole.Goalkeeper);

        // Place Goalkeeper at goal line (5.0, 34.0)
        gk.Position2D = new Vector2(5.0f, 34.0f);

        // Place ball 0.1m from Goalkeeper!
        match.Ball.Position = new Vector3(5.1f, 34.0f, 0.0f);

        // Place active user player at (50.0, 30.0)
        match.ActiveUserPlayer.Position2D = new Vector2(50.0f, 30.0f);

        // Place nearest outfield defender at (15.0, 34.0) -> 9.9m away
        var nearestDefender = homeTeam.Players.First(p => p != match.ActiveUserPlayer && p.Role != PlayerRole.Goalkeeper);
        nearestDefender.Position2D = new Vector2(15.0f, 34.0f);

        // Place all other outfield players far away
        foreach (var p in homeTeam.Players.Where(p => p != match.ActiveUserPlayer && p != nearestDefender && p.Role != PlayerRole.Goalkeeper))
        {
            p.Position2D = new Vector2(90.0f, 50.0f);
        }

        match.SwitchToClosestPlayer();

        Assert(match.ActiveUserPlayer != gk,
            "Switching MUST NEVER select Goalkeeper even when Goalkeeper is 0.1m from ball!");
        Assert(match.ActiveUserPlayer == nearestDefender,
            $"Switching must select closest outfield defender. Got: {match.ActiveUserPlayer.Name}");
    }

    private static void TestStandingTackle_ReachThresholds_1_7MHit_1_9MMiss()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        string? outOfReachMsg = null;
        eventBus.OnMatchEvent += evt =>
        {
            if (evt.Type == MatchEventType.TacticalRadarInfo && evt.Description.Contains("بعيد"))
            {
                outOfReachMsg = evt.Description;
            }
        };

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var tackler = match.ActiveUserPlayer;
        tackler.Position2D = new Vector2(50.0f, 30.0f);

        var carrier = awayTeam.Players[9];

        // ----------------------------------------------------
        // Part 1: Distance = 1.9m (> 1.8m reach) -> MUST MISS
        // ----------------------------------------------------
        outOfReachMsg = null;
        carrier.Position2D = new Vector2(51.9f, 30.0f);
        match.Ball.Position = new Vector3(51.9f, 30.0f, 0.0f);
        match.Ball.CatchOrTackle(carrier);

        match.ExecuteStandTackle();

        Assert(outOfReachMsg != null,
            "Standing tackle at 1.9m (> 1.8m) MUST trigger out-of-reach announcement!");
        Assert(outOfReachMsg!.Contains("1.9"),
            $"Out-of-reach announcement at 1.9m must report '1.9'. Got: '{outOfReachMsg}'");
        Assert(match.Ball.Owner == carrier,
            "Standing tackle at 1.9m must not strip ball from carrier");

        // ----------------------------------------------------
        // Part 2: Distance = 1.7m (<= 1.8m reach) -> MUST HIT (in range)
        // ----------------------------------------------------
        // Wait out cooldown (0.4s = 20 ticks)
        for (int i = 0; i < 25; i++) match.Update(0.02f);

        outOfReachMsg = null;
        carrier.Position2D = new Vector2(51.7f, 30.0f);
        match.Ball.Position = new Vector3(51.7f, 30.0f, 0.0f);
        match.Ball.CatchOrTackle(carrier);

        match.ExecuteStandTackle();

        Assert(outOfReachMsg == null,
            $"Standing tackle at 1.7m (<= 1.8m) is IN RANGE and must NOT emit out-of-reach message. Got: '{outOfReachMsg}'");
        Assert(match.TackleCooldownRemaining > 0.35f,
            "Executing tackle in range must trigger 0.4s cooldown");
    }

    private static void TestSlideTackle_ReachThresholds_2_7MHit_2_9MMiss()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        string? outOfReachMsg = null;
        eventBus.OnMatchEvent += evt =>
        {
            if (evt.Type == MatchEventType.TacticalRadarInfo && evt.Description.Contains("بعيد"))
            {
                outOfReachMsg = evt.Description;
            }
        };

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var tackler = match.ActiveUserPlayer;
        tackler.Position2D = new Vector2(50.0f, 30.0f);
        tackler.FacingDirection = new Vector2(1.0f, 0.0f);

        var carrier = awayTeam.Players[9];

        // ----------------------------------------------------
        // Part 1: Distance = 2.9m (> 2.8m reach) -> MUST MISS
        // ----------------------------------------------------
        outOfReachMsg = null;
        carrier.Position2D = new Vector2(52.9f, 30.0f);
        match.Ball.Position = new Vector3(52.9f, 30.0f, 0.0f);
        match.Ball.CatchOrTackle(carrier);

        match.ExecuteSlideTackle();

        Assert(outOfReachMsg != null,
            "Slide tackle at 2.9m (> 2.8m) MUST trigger out-of-reach announcement!");
        Assert(outOfReachMsg!.Contains("2.9"),
            $"Out-of-reach announcement at 2.9m must report '2.9'. Got: '{outOfReachMsg}'");

        // ----------------------------------------------------
        // Part 2: Distance = 2.7m (<= 2.8m reach) -> MUST HIT (in range)
        // ----------------------------------------------------
        // Wait out cooldown (0.9s = 45 ticks)
        for (int i = 0; i < 50; i++) match.Update(0.02f);

        outOfReachMsg = null;
        carrier.Position2D = new Vector2(52.7f, 30.0f);
        match.Ball.Position = new Vector3(52.7f, 30.0f, 0.0f);
        match.Ball.CatchOrTackle(carrier);

        match.ExecuteSlideTackle();

        Assert(outOfReachMsg == null,
            $"Slide tackle at 2.7m (<= 2.8m) is IN RANGE and must NOT emit out-of-reach message. Got: '{outOfReachMsg}'");
        Assert(match.IsSliding, "Slide tackle in range must activate sliding physics");
        Assert(match.TackleCooldownRemaining > 0.85f, "Slide cooldown must be ~0.9s");
    }

    private static void TestReachDifferential_2_0M_StandingMisses_SlideHits()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        string? outOfReachMsg = null;
        eventBus.OnMatchEvent += evt =>
        {
            if (evt.Type == MatchEventType.TacticalRadarInfo && evt.Description.Contains("بعيد"))
            {
                outOfReachMsg = evt.Description;
            }
        };

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var tackler = match.ActiveUserPlayer;
        tackler.Position2D = new Vector2(50.0f, 30.0f);
        tackler.FacingDirection = new Vector2(1.0f, 0.0f);

        var carrier = awayTeam.Players[9];
        // Target at exactly 2.0m: Out of standing reach (1.8m), but within slide reach (2.8m)
        carrier.Position2D = new Vector2(52.0f, 30.0f);
        match.Ball.Position = new Vector3(52.0f, 30.0f, 0.0f);
        match.Ball.CatchOrTackle(carrier);

        // 1. Standing tackle at 2.0m -> MUST MISS
        outOfReachMsg = null;
        match.ExecuteStandTackle();
        Assert(outOfReachMsg != null,
            "At 2.0m, Standing Tackle MUST miss and report out-of-reach!");

        // Wait out cooldown
        for (int i = 0; i < 25; i++) match.Update(0.02f);

        // 2. Slide tackle at 2.0m -> MUST BE IN RANGE
        outOfReachMsg = null;
        match.ExecuteSlideTackle();
        Assert(outOfReachMsg == null,
            "At 2.0m, Slide Tackle MUST be in reach (no out-of-reach announcement)!");
        Assert(match.IsSliding, "Slide tackle at 2.0m must initiate slide");
    }

    private static void TestSlideTackle_ForwardImpulse_DisplacementAndClamping()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var tackler = match.ActiveUserPlayer;
        tackler.Position2D = new Vector2(50.0f, 30.0f);
        tackler.FacingDirection = new Vector2(1.0f, 0.0f); // Facing +X

        match.ExecuteSlideTackle();

        Assert(match.IsSliding, "Slide must start with IsSliding = true");
        Assert(match.SlideDurationRemaining > 0.28f, "Slide duration initial timer ~0.3s");

        float initialX = tackler.Position2D.X;

        // Step simulation for 15 frames (0.30s @ 0.02s per frame)
        for (int i = 0; i < 15; i++)
        {
            match.Update(0.02f);
        }

        float totalDisplacement = tackler.Position2D.X - initialX;
        // In 0.30s at +4.0m/s impulse, expected theoretical displacement is exactly 1.20m
        Assert(Math.Abs(totalDisplacement - 1.20f) < 0.05f,
            $"Slide impulse (+4.0m/s for 0.30s) must displace player ~1.20m. Actual: {totalDisplacement:F3}m");

        // Advance 1 extra frame (0.02s) to ensure discrete float subtraction timer fully clears to 0
        match.Update(0.02f);

        // Slide duration must now be exhausted
        Assert(!match.IsSliding, "After slide duration, IsSliding must be false");
        Assert(match.SlideDurationRemaining == 0.0f, "SlideDurationRemaining must be 0.0s");

        // Pitch Clamping test: slide towards right goal line
        tackler.Position2D = new Vector2(Pitch.Length - 0.5f, 30.0f);
        tackler.FacingDirection = new Vector2(1.0f, 0.0f);

        // Wait out cooldown
        for (int i = 0; i < 35; i++) match.Update(0.02f);

        match.ExecuteSlideTackle();
        for (int i = 0; i < 15; i++) match.Update(0.02f);

        Assert(tackler.Position2D.X <= Pitch.Length,
            $"Player position must be clamped to pitch length ({Pitch.Length}m). Got: {tackler.Position2D.X}");
    }

    private static void TestSlideTackle_RecoveryLockoutAndCooldownExpiry()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var tackler = match.ActiveUserPlayer;
        tackler.Position2D = new Vector2(50.0f, 30.0f);
        tackler.FacingDirection = new Vector2(1.0f, 0.0f);

        match.ExecuteSlideTackle();

        // ----------------------------------------------------
        // Phase 1: During Slide (0.0s to 0.3s)
        // Arrow movement input MUST be locked out
        // ----------------------------------------------------
        // Simulate holding MoveUp (VK_UP = 0x26)
        inputHandler.HandleKeyDown(0x26, false);

        float initialY = tackler.Position2D.Y;
        // Step 5 frames (0.10s) while sliding
        for (int i = 0; i < 5; i++)
        {
            match.Update(0.02f);
        }

        // Y coordinate must NOT have changed because user steering is locked out during slide!
        Assert(Math.Abs(tackler.Position2D.Y - initialY) < 0.001f,
            $"User movement keys must be locked out during slide. Y moved: {tackler.Position2D.Y - initialY}");

        // ----------------------------------------------------
        // Phase 2: Slide Ends, but Cooldown Recovery Active (0.3s to 0.9s)
        // Player can move again, but CANNOT tackle
        // ----------------------------------------------------
        // Step remaining 11 frames (total 16 frames = 0.32s) to fully clear slide timer
        for (int i = 0; i < 11; i++)
        {
            match.Update(0.02f);
        }

        Assert(!match.IsSliding, "Slide must have finished at ~0.30s");
        Assert(match.TackleCooldownRemaining > 0.50f && match.TackleCooldownRemaining <= 0.65f,
            $"Tackle cooldown remaining must be ~0.58s. Got: {match.TackleCooldownRemaining:F2}s");

        // Try executing tackle during recovery cooldown
        float cooldownBefore = match.TackleCooldownRemaining;
        match.ExecuteSlideTackle();
        Assert(!match.IsSliding, "Cannot slide while tackle is on recovery cooldown");
        Assert(match.TackleCooldownRemaining <= cooldownBefore, "Cooldown timer must not reset on blocked tackle");

        // Movement now works: step 5 frames with MoveUp held
        float yBeforeMove = tackler.Position2D.Y;
        for (int i = 0; i < 5; i++)
        {
            match.Update(0.02f);
        }
        Assert(tackler.Position2D.Y < yBeforeMove,
            "Movement keys must be unlocked after slide duration finishes (recovery movement enabled)");

        // ----------------------------------------------------
        // Phase 3: Cooldown Expiry (t > 0.9s)
        // Tackles can now be executed again
        // ----------------------------------------------------
        // Release MoveUp
        inputHandler.HandleKeyUp(0x26);

        // Advance until cooldown expires
        while (match.TackleCooldownRemaining > 0.0f)
        {
            match.Update(0.02f);
        }

        Assert(match.TackleCooldownRemaining == 0.0f, "Cooldown must reach 0.0s");

        // Execute standing tackle
        match.ExecuteStandTackle();
        Assert(match.TackleCooldownRemaining > 0.35f,
            "After cooldown expiration, a new tackle must be accepted and reset cooldown");
    }

    private static void TestZeroClutter_AnnouncementsContainNoTutorialPrompts()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        var recordedAnnouncements = new List<string>();
        eventBus.OnMatchEvent += evt =>
        {
            if (!string.IsNullOrEmpty(evt.Description))
            {
                recordedAnnouncements.Add(evt.Description);
            }
        };

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);

        // 1. Switch player
        match.SwitchToClosestPlayer();

        // 2. Out-of-reach standing tackle
        match.ActiveUserPlayer.Position2D = new Vector2(10.0f, 10.0f);
        match.Ball.Position = new Vector3(30.0f, 30.0f, 0.0f);
        match.ExecuteStandTackle();

        // 3. Out-of-reach slide tackle
        match.ExecuteSlideTackle();

        // 4. Simulate 100 frames of dynamic match play
        for (int i = 0; i < 100; i++)
        {
            match.Update(0.02f);
        }

        // Check all recorded announcements
        Assert(recordedAnnouncements.Count > 0, "Expected match announcements during simulated scenario");
        foreach (var msg in recordedAnnouncements)
        {
            Assert(!msg.Contains("اضغط"),
                $"CRITICAL DIRECTIVE VIOLATION: Announcement contains tutorial prompt 'اضغط': '{msg}'");
            Assert(!msg.Contains("Ctrl") && !msg.Contains("للتسديد") && !msg.Contains("للتبديل"),
                $"CRITICAL DIRECTIVE VIOLATION: Announcement contains tutorial key instructions: '{msg}'");
        }
    }

    private static void TestZeroClutter_GoalkeeperCatch_ZeroTutorialPrompts()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        var recordedAnnouncements = new List<string>();
        eventBus.OnMatchEvent += evt =>
        {
            if (!string.IsNullOrEmpty(evt.Description))
            {
                recordedAnnouncements.Add(evt.Description);
            }
        };

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);

        // Identify home goalkeeper
        var gk = homeTeam.Players.First(p => p.Role == PlayerRole.Goalkeeper);

        // Place ball 1.2m in front of goalkeeper in open play (within 2.5m catch threshold)
        match.Ball.State = BallState.Free;
        match.Ball.Owner = null;
        match.Ball.Position = new Vector3(gk.Position2D.X + 1.2f, gk.Position2D.Y, 0.0f);
        match.Ball.Velocity = Vector3.Zero;

        // Run simulation update tick
        match.Update(0.02f);

        // Verify goalkeeper caught ball
        Assert(match.Ball.Owner == gk, "Goalkeeper must catch the ball within 2.5m range");

        // Verify goalkeeper catch announcement occurred and is completely free of tutorial instructions
        var gkCatchEvents = recordedAnnouncements.Where(a => a.Contains("حارس مرماك")).ToList();
        Assert(gkCatchEvents.Count > 0, "Must publish goalkeeper catch announcement");

        foreach (var msg in recordedAnnouncements)
        {
            Assert(!msg.Contains("اضغط"),
                $"CRITICAL DIRECTIVE VIOLATION: In-match goalkeeper announcement contains tutorial prompt 'اضغط': '{msg}'");
            Assert(!msg.Contains("للتمرير") && !msg.Contains("للرفع"),
                $"CRITICAL DIRECTIVE VIOLATION: In-match goalkeeper announcement contains tutorial button prompts: '{msg}'");
        }
    }

    private static void TestAudioTelemetry_EndToEndSFXRouting()
    {
        var screenReader = new MockScreenReader();
        var coordinator = new SmartAudioCoordinator(screenReader);

        // 1. Shot kick
        SoundEffectsPlayer.ResetTelemetry();
        coordinator.HandleMatchEvent(new MatchEvent(MatchEventType.ShotTaken, "تسديدة قوية", spatialPan: 0.5f));
        Assert(SoundEffectsPlayer.LastPlayedSound == "shot_kick",
            $"ShotTaken must play 'shot_kick'. Got: '{SoundEffectsPlayer.LastPlayedSound}'");
        Assert(Math.Abs(SoundEffectsPlayer.LastPlayedPan - 0.5f) < 0.01f, "Pan must match");

        // 2. Pass kick
        SoundEffectsPlayer.ResetTelemetry();
        coordinator.HandleMatchEvent(new MatchEvent(MatchEventType.BallKicked, "تمريرة قصيرة", spatialPan: -0.2f));
        Assert(SoundEffectsPlayer.LastPlayedSound == "pass_kick",
            $"Pass must play 'pass_kick'. Got: '{SoundEffectsPlayer.LastPlayedSound}'");

        // 3. Cross kick
        SoundEffectsPlayer.ResetTelemetry();
        coordinator.HandleMatchEvent(new MatchEvent(MatchEventType.BallKicked, "عرضية في العمق", spatialPan: 0.1f));
        Assert(SoundEffectsPlayer.LastPlayedSound == "cross_kick",
            $"Cross must play 'cross_kick'. Got: '{SoundEffectsPlayer.LastPlayedSound}'");

        // 4. Slide tackle attempted
        SoundEffectsPlayer.ResetTelemetry();
        coordinator.HandleMatchEvent(new MatchEvent(MatchEventType.TackleAttempted, "انزلاق", spatialPan: 0.0f));
        Assert(SoundEffectsPlayer.LastPlayedSound == "slide_tackle",
            $"TackleAttempted slide must play 'slide_tackle'. Got: '{SoundEffectsPlayer.LastPlayedSound}'");

        // 5. Standing tackle success
        SoundEffectsPlayer.ResetTelemetry();
        coordinator.HandleMatchEvent(new MatchEvent(MatchEventType.UserTackleSuccessful, "استخلاص: هادئ", spatialPan: -0.3f));
        Assert(SoundEffectsPlayer.LastPlayedSound == "stand_tackle",
            $"TackleSuccessful quiet must play 'stand_tackle'. Got: '{SoundEffectsPlayer.LastPlayedSound}'");

        // 6. Slide tackle success
        SoundEffectsPlayer.ResetTelemetry();
        coordinator.HandleMatchEvent(new MatchEvent(MatchEventType.UserTackleSuccessful, "استخلاص: بانزلاق أرضي", spatialPan: 0.4f));
        Assert(SoundEffectsPlayer.LastPlayedSound == "slide_tackle",
            $"TackleSuccessful slide must play 'slide_tackle'. Got: '{SoundEffectsPlayer.LastPlayedSound}'");

        // 7. Whistle
        SoundEffectsPlayer.ResetTelemetry();
        coordinator.HandleMatchEvent(new MatchEvent(MatchEventType.FoulCommitted, "خطأ ضد اللاعب", spatialPan: 0.0f));
        Assert(SoundEffectsPlayer.LastPlayedSound == "whistle",
            $"FoulCommitted must blow whistle. Got: '{SoundEffectsPlayer.LastPlayedSound}'");
    }

    private class MockScreenReader : IScreenReader
    {
        public bool IsAvailable => true;
        public List<string> SpokenTexts { get; } = new();
        public void Speak(string text, bool interrupt = true) => SpokenTexts.Add(text);
        public void CancelSpeech() {}
    }
}
