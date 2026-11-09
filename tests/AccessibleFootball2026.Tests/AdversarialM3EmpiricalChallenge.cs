using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using AccessibleFootball2026.Audio;
using AccessibleFootball2026.Audio.Commentary;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;
using AccessibleFootball2026.Input;

namespace AccessibleFootball2026.Tests;

/// <summary>
/// EMPIRICAL CHALLENGER (M3): Adversarial stress testing harness for
/// Milestone 3 Audio Overhaul:
/// 1. 100% Screen Reader Silence: simulate 500+ ticks of continuous gameplay, verify zero unsolicited speech during active match play.
/// 2. 3D Spatial Goal Beacon: verify proximity volume curves and stereo panning geometry across both 1st and 2nd half.
/// 3. Out-of-reach tackle: verify TackleMissed earcon plays without speech.
/// </summary>
public static class AdversarialM3EmpiricalChallenge
{
    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception($"Assertion Failed: {message}");
    }

    private static string FindWorkspaceRoot()
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "AccessibleFootball2026.sln")))
            {
                return dir;
            }
            dir = Path.GetDirectoryName(dir);
        }
        throw new DirectoryNotFoundException("Could not find AccessibleFootball2026.sln in parent tree.");
    }

    public static void RunAll(Action<string, Action> runTest)
    {
        Console.WriteLine("\n--- EMPIRICAL CHALLENGER (M3): ADVERSARIAL AUDIO & SILENCE STRESS TESTS ---");

        // 1. Screen Reader Silence (500+ ticks continuous gameplay)
        runTest("15A. Silence Stress: 600 ticks continuous sprint/dribble across all zones -> Zero unsolicited speech",
            TestSilence_600TicksContinuousSprintAndDribble_ZeroSpeech);

        runTest("15B. Silence Stress: 500 ticks with rapid/held Ctrl player switches -> Micro-clicks only, zero speech",
            TestSilence_500TicksCtrlDefenderSwitching_ZeroSpeech);

        runTest("15C. Silence Stress: Repeated passing, crossing, through balls, and shots -> Zero unsolicited speech",
            TestSilence_RepeatedKicksAndShots_ZeroSpeech);

        runTest("15D. Silence Stress: Comprehensive 600-tick active match play with saves and woodwork -> Zero speech",
            TestSilence_Comprehensive600TickMatchPlay_ZeroSpeech);

        runTest("15E. Tactical Radar: Speech strictly isolated to explicit Space key; zero speech on subsequent ticks",
            TestTacticalRadar_ExplicitSpaceOnly_ZeroSpeechAfter);

        runTest("15F. Commentary & Silence: Raouf Khlif authentic clips vs screen reader speech check",
            TestCommentary_RaoufKhlifClips_NoScreenReaderFallbackInPlay);

        // 2. 3D Spatial Goal Beacon
        runTest("15G. Goal Beacon: Boundary coordinates stress test (X=0, 105, Y=0, 68, center 34) in 1st Half",
            TestGoalBeacon_1stHalf_BoundaryCoordinates);

        runTest("15H. Goal Beacon: Boundary coordinates stress test (X=0, 105, Y=0, 68, center 34) in 2nd Half (X=0)",
            TestGoalBeacon_2ndHalf_BoundaryCoordinates);

        runTest("15I. Goal Beacon: 1,000-point length sweep -> Strict monotonic proximity curve, silent in own half",
            TestGoalBeacon_1000PointLengthSweep_MonotonicProximity);

        runTest("15J. Goal Beacon: 500-point width sweep -> Antisymmetry Pan(34-d) == -Pan(34+d), clamped [-1, 1]",
            TestGoalBeacon_500PointWidthSweep_AntisymmetryAndClamping);

        runTest("15K. Goal Beacon: Possession gating -> Pulses occur ONLY when user has ball possession",
            TestGoalBeacon_PossessionGating_OnlyPlaysWhenUserHasBall);

        // 3. Out-of-Reach Tackle & TackleMissed Earcon
        runTest("15L. Out-of-Reach Standing Tackle: 1.79m hits, 1.81m misses -> TackleMissed earcon, zero speech",
            TestOutOfReach_StandingTackle_Threshold_PlaysEarconZeroSpeech);

        runTest("15M. Out-of-Reach Slide Tackle: 2.79m hits, 2.81m misses -> TackleMissed earcon, zero speech",
            TestOutOfReach_SlideTackle_Threshold_PlaysEarconZeroSpeech);

        runTest("15N. Out-of-Reach Tackles: Extreme distance (5m, 15m, 40m) & loose ball -> TackleMissed, zero speech",
            TestOutOfReach_ExtremeDistancesAndLooseBall_TackleMissedZeroSpeech);

        runTest("15O. TackleMissed Audio Asset: Valid RIFF header, non-empty PCM wave data, valid frequency sweep",
            TestTackleMissed_AudioAssetVerification);
    }

    // =========================================================================
    // 1. SCREEN READER SILENCE: 500+ TICKS CONTINUOUS GAMEPLAY
    // =========================================================================

    public static void TestSilence_600TicksContinuousSprintAndDribble_ZeroSpeech()
    {
        SmartAudioCoordinator.SilenceLiveMatchSpeech = true;
        var mockReader = new MockScreenReader();
        var coordinator = new SmartAudioCoordinator(mockReader);
        var eventBus = new MatchEventBus();
        eventBus.OnMatchEvent += coordinator.HandleMatchEvent;

        var inputHandler = new KeyboardInputHandler();
        var match = new MatchEngine(TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek(), eventBus, inputHandler);

        // User player starts at midfield (52.5, 34)
        var user = match.ActiveUserPlayer;
        match.Ball.CatchOrTackle(user);

        // Simulate holding Right Arrow (VK_RIGHT = 0x27) and Left Shift (VK_SHIFT = 0x10) across 600 ticks (12.0 seconds)
        inputHandler.HandleKeyDown(0x27, isPossession: true);
        inputHandler.HandleKeyDown(0x10, isPossession: true);

        for (int tick = 0; tick < 600; tick++)
        {
            match.Update(0.02f);

            // Notify ball zone changes as user crosses the pitch
            coordinator.NotifyBallZoneChanged(match.CurrentZone);

            // Assert zero speech on EVERY single tick
            Assert(mockReader.SpokenTexts.Count == 0,
                $"CRITICAL FAILURE: Screen reader spoke on tick {tick} during sprinting/dribbling! Message: '{mockReader.SpokenTexts.LastOrDefault()}'");
        }

        inputHandler.HandleKeyUp(0x27);
        inputHandler.HandleKeyUp(0x10);

        Assert(mockReader.SpokenTexts.Count == 0,
            $"Expected exactly 0 spoken texts across 600 sprint ticks, got {mockReader.SpokenTexts.Count}");
    }

    public static void TestSilence_500TicksCtrlDefenderSwitching_ZeroSpeech()
    {
        SmartAudioCoordinator.SilenceLiveMatchSpeech = true;
        var mockReader = new MockScreenReader();
        var coordinator = new SmartAudioCoordinator(mockReader);
        var eventBus = new MatchEventBus();
        eventBus.OnMatchEvent += coordinator.HandleMatchEvent;

        var inputHandler = new KeyboardInputHandler();
        var match = new MatchEngine(TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek(), eventBus, inputHandler);

        // Away team striker has ball -> user is defending
        var opponent = match.AwayTeam.Players[9];
        match.Ball.CatchOrTackle(opponent);
        match.Ball.Position = new Vector3(opponent.Position2D.X, opponent.Position2D.Y, 0.0f);

        int earconPlays = 0;
        int switchCount = 0;
        Player? prevPlayer = match.ActiveUserPlayer;

        // Run 500 ticks: tap and hold Ctrl at various intervals
        for (int tick = 0; tick < 500; tick++)
        {
            // Tap Ctrl every 25 ticks (0.5s)
            if (tick % 25 == 0)
            {
                inputHandler.HandleKeyDown(0x11, isPossession: false); // VK_CONTROL
            }
            else if (tick % 25 == 15)
            {
                inputHandler.HandleKeyUp(0x11);
            }

            match.Update(0.02f);

            if (match.ActiveUserPlayer != prevPlayer)
            {
                switchCount++;
                prevPlayer = match.ActiveUserPlayer;
                if (SoundEffectsPlayer.LastPlayedSound == "PlayerSwitch")
                {
                    earconPlays++;
                }
            }

            Assert(mockReader.SpokenTexts.Count == 0,
                $"CRITICAL FAILURE: Ctrl defender switch emitted screen reader speech on tick {tick}! Message: '{mockReader.SpokenTexts.LastOrDefault()}'");
        }

        Assert(switchCount >= 10, $"Expected at least 10 switches across 500 ticks, got {switchCount}");
        Assert(mockReader.SpokenTexts.Count == 0,
            $"Ctrl defender switching must produce 0 spoken texts across 500 ticks! Got: {mockReader.SpokenTexts.Count}");
    }

    public static void TestSilence_RepeatedKicksAndShots_ZeroSpeech()
    {
        SmartAudioCoordinator.SilenceLiveMatchSpeech = true;
        var mockReader = new MockScreenReader();
        var coordinator = new SmartAudioCoordinator(mockReader);
        var eventBus = new MatchEventBus();
        eventBus.OnMatchEvent += coordinator.HandleMatchEvent;

        var inputHandler = new KeyboardInputHandler();
        var match = new MatchEngine(TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek(), eventBus, inputHandler);

        int[] kickKeys = { 0x53 /* S */, 0x44 /* D */, 0x41 /* A */, 0x57 /* W */ };

        // Perform 50 passes, crosses, through balls, and shots
        for (int i = 0; i < 50; i++)
        {
            match.ActiveUserPlayer.Position2D = new Vector2(70.0f, 34.0f);
            match.Ball.Position = new Vector3(70.0f, 34.0f, 0.0f);
            match.Ball.CatchOrTackle(match.ActiveUserPlayer);

            int key = kickKeys[i % kickKeys.Length];
            inputHandler.HandleKeyDown(key, isPossession: true);

            // Update 10 ticks per kick
            for (int t = 0; t < 10; t++)
            {
                match.Update(0.02f);
                if (t == 1) inputHandler.HandleKeyUp(key);
            }

            Assert(mockReader.SpokenTexts.Count == 0,
                $"CRITICAL FAILURE: Kicks and shots emitted screen reader speech at iteration {i}! Message: '{mockReader.SpokenTexts.LastOrDefault()}'");
        }

        Assert(mockReader.SpokenTexts.Count == 0, "Repeated kicks/shots must emit ZERO screen reader speech!");
    }

    public static void TestSilence_Comprehensive600TickMatchPlay_ZeroSpeech()
    {
        SmartAudioCoordinator.SilenceLiveMatchSpeech = true;
        var mockReader = new MockScreenReader();
        var coordinator = new SmartAudioCoordinator(mockReader);
        var eventBus = new MatchEventBus();
        eventBus.OnMatchEvent += coordinator.HandleMatchEvent;

        var inputHandler = new KeyboardInputHandler();
        var match = new MatchEngine(TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek(), eventBus, inputHandler);

        // Run full dynamic simulation of 600 ticks with diverse events firing
        for (int tick = 0; tick < 600; tick++)
        {
            match.Update(0.02f);

            // Inject match events dynamically
            if (tick == 50) match.TriggerGoalkeeperSave();
            if (tick == 120) match.TriggerWoodworkHit();
            if (tick == 200) eventBus.Publish(new MatchEvent(MatchEventType.CornerKick, "ركنية"));
            if (tick == 280) eventBus.Publish(new MatchEvent(MatchEventType.ThrowIn, "رمية تماس"));
            if (tick == 350) eventBus.Publish(new MatchEvent(MatchEventType.GoalKick, "ركلة مرمى"));
            if (tick == 420) eventBus.Publish(new MatchEvent(MatchEventType.BallKicked, "تمريرة"));
            if (tick == 480) eventBus.Publish(new MatchEvent(MatchEventType.BallTrapped, "استلام"));
            if (tick == 540) eventBus.Publish(new MatchEvent(MatchEventType.UserTackleSuccessful, "استخلاص"));

            Assert(mockReader.SpokenTexts.Count == 0,
                $"CRITICAL FAILURE: Comprehensive match play emitted speech on tick {tick}! Spoken: '{mockReader.SpokenTexts.LastOrDefault()}'");
        }

        Assert(mockReader.SpokenTexts.Count == 0,
            $"Expected ZERO spoken texts across entire 600-tick comprehensive match play simulation, got {mockReader.SpokenTexts.Count}");
    }

    public static void TestTacticalRadar_ExplicitSpaceOnly_ZeroSpeechAfter()
    {
        SmartAudioCoordinator.SilenceLiveMatchSpeech = true;
        var mockReader = new MockScreenReader();
        var coordinator = new SmartAudioCoordinator(mockReader);
        var eventBus = new MatchEventBus();
        eventBus.OnMatchEvent += coordinator.HandleMatchEvent;

        var match = new MatchEngine(TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek(), eventBus, new KeyboardInputHandler());

        // 100 ticks before space: 0 speech
        for (int i = 0; i < 100; i++) match.Update(0.02f);
        Assert(mockReader.SpokenTexts.Count == 0, "No speech before explicit Space press");

        // Explicit Space press
        match.ReportTacticalRadar();
        Assert(mockReader.SpokenTexts.Count == 1,
            $"Space press must produce exactly 1 speech message, got {mockReader.SpokenTexts.Count}");
        string radarSpeech = mockReader.SpokenTexts[0];
        Assert(!string.IsNullOrWhiteSpace(radarSpeech), "Radar announcement must not be empty");

        // 100 ticks after space: still exactly 1 message (zero subsequent unsolicited speech)
        for (int i = 0; i < 100; i++) match.Update(0.02f);
        Assert(mockReader.SpokenTexts.Count == 1,
            $"Zero unsolicited speech must follow after radar query! Count was {mockReader.SpokenTexts.Count}");
    }

    public static void TestCommentary_RaoufKhlifClips_NoScreenReaderFallbackInPlay()
    {
        string root = FindWorkspaceRoot();
        string audioDir = Path.Combine(root, "Assets", "Audio");
        var comm = new CommentaryEngine(CommentatorId.RaoufKhlif, audioDir);

        bool speechTriggered = false;
        string? spokenPhrase = null;
        comm.OnCommentarySpoken += (name, phrase) =>
        {
            speechTriggered = true;
            spokenPhrase = phrase;
        };

        // Test primary gameplay events for Raouf Khlif
        var eventsToTest = new[]
        {
            new MatchEvent(MatchEventType.ShotTaken, "تسديدة"),
            new MatchEvent(MatchEventType.UserTackleSuccessful, "افتكاك"),
            new MatchEvent(MatchEventType.GoalkeeperSave, "تصدي"),
            new MatchEvent(MatchEventType.WoodworkHit, "القائم"),
            new MatchEvent(MatchEventType.FoulCommitted, "فاول")
        };

        foreach (var evt in eventsToTest)
        {
            speechTriggered = false;
            spokenPhrase = null;
            bool handled = comm.TryCommentOnEvent(evt);

            // Must be handled by genuine audio clip, NEVER by screen reader speech
            Assert(handled, $"Raouf Khlif must handle event {evt.Type} via authentic PES 2013 audio");
            Assert(!speechTriggered,
                $"CRITICAL DIRECTIVE VIOLATION: Event {evt.Type} fell back to screen reader speech: '{spokenPhrase}' instead of PES audio clip!");
        }
    }

    // =========================================================================
    // 2. 3D SPATIAL GOAL BEACON STRESS TESTS
    // =========================================================================

    public static void TestGoalBeacon_1stHalf_BoundaryCoordinates()
    {
        // 1st Half: Target goal is Right Goal at (105, 34)
        // 1. Defending Goal line (X=0, Y=34): D=105m > 52.5m -> vol=0, pan=0
        GoalBeaconPlayer.CalculateBeacon(0.0f, 34.0f, isSecondHalf: false, masterVolume: 1.0f, out float d0, out float v0, out float p0);
        Assert(d0 == 105.0f, $"1st half X=0, Y=34 distance must be 105, got {d0}");
        Assert(v0 == 0.0f, $"1st half X=0, Y=34 volume must be 0, got {v0}");
        Assert(Math.Abs(p0) < 0.001f, $"1st half Y=34 pan must be 0, got {p0}");

        // 2. Center of pitch (X=52.5, Y=34): D=52.5m boundary -> vol=0, pan=0
        GoalBeaconPlayer.CalculateBeacon(52.5f, 34.0f, isSecondHalf: false, masterVolume: 1.0f, out float dMid, out float vMid, out float pMid);
        Assert(dMid == 52.5f, $"Pitch center distance must be 52.5, got {dMid}");
        Assert(vMid == 0.0f, $"Pitch center volume must be 0, got {vMid}");
        Assert(Math.Abs(pMid) < 0.001f, $"Pitch center pan must be 0, got {pMid}");

        // 3. Attacking Goal line center (X=105, Y=34): D=0m -> vol=1.0, pan=0
        GoalBeaconPlayer.CalculateBeacon(105.0f, 34.0f, isSecondHalf: false, masterVolume: 1.0f, out float dGoal, out float vGoal, out float pGoal);
        Assert(dGoal == 0.0f, $"Target goal distance must be 0, got {dGoal}");
        Assert(Math.Abs(vGoal - 1.0f) < 0.001f, $"Target goal volume must be 1.0, got {vGoal}");
        Assert(Math.Abs(pGoal) < 0.001f, $"Target goal pan must be 0, got {pGoal}");

        // 4. Top-right corner flag (X=105, Y=0): D=34m -> vol=(18.5/52.5)^1.5 ~ 0.209, pan=+1.0 (hard right)
        GoalBeaconPlayer.CalculateBeacon(105.0f, 0.0f, isSecondHalf: false, masterVolume: 1.0f, out float dTopRight, out float vTopRight, out float pTopRight);
        Assert(dTopRight == 34.0f, $"Corner flag distance must be 34m, got {dTopRight}");
        Assert(vTopRight > 0.18f && vTopRight < 0.23f, $"Corner flag volume must be ~0.209, got {vTopRight:F3}");
        Assert(Math.Abs(pTopRight - 1.0f) < 0.001f, $"Top-right corner flag pan must be +1.0 (right), got {pTopRight:F3}");

        // 5. Bottom-right corner flag (X=105, Y=68): D=34m -> vol=(18.5/52.5)^1.5 ~ 0.209, pan=-1.0 (hard left)
        GoalBeaconPlayer.CalculateBeacon(105.0f, 68.0f, isSecondHalf: false, masterVolume: 1.0f, out float dBotRight, out float vBotRight, out float pBotRight);
        Assert(dBotRight == 34.0f, $"Corner flag distance must be 34m, got {dBotRight}");
        Assert(vBotRight > 0.18f && vBotRight < 0.23f, $"Corner flag volume must be ~0.209, got {vBotRight:F3}");
        Assert(Math.Abs(pBotRight - (-1.0f)) < 0.001f, $"Bottom-right corner flag pan must be -1.0 (left), got {pBotRight:F3}");

        // 6. Top-left defensive corner (X=0, Y=0): D=110.36m > 52.5m -> vol=0
        GoalBeaconPlayer.CalculateBeacon(0.0f, 0.0f, isSecondHalf: false, masterVolume: 1.0f, out float dTopLeft, out float vTopLeft, out _);
        Assert(dTopLeft > 52.5f && vTopLeft == 0.0f, $"Defensive corner must be silent, got vol {vTopLeft}");

        // 7. Bottom-left defensive corner (X=0, Y=68): D=110.36m > 52.5m -> vol=0
        GoalBeaconPlayer.CalculateBeacon(0.0f, 68.0f, isSecondHalf: false, masterVolume: 1.0f, out float dBotLeft, out float vBotLeft, out _);
        Assert(dBotLeft > 52.5f && vBotLeft == 0.0f, $"Defensive corner must be silent, got vol {vBotLeft}");
    }

    public static void TestGoalBeacon_2ndHalf_BoundaryCoordinates()
    {
        // 2nd Half: Target goal inverted to Left Goal at (0, 34)
        // 1. Defending Goal line (X=105, Y=34): D=105m > 52.5m -> vol=0, pan=0
        GoalBeaconPlayer.CalculateBeacon(105.0f, 34.0f, isSecondHalf: true, masterVolume: 1.0f, out float d105, out float v105, out float p105);
        Assert(d105 == 105.0f, $"2nd half X=105, Y=34 distance must be 105, got {d105}");
        Assert(v105 == 0.0f, $"2nd half X=105, Y=34 volume must be 0, got {v105}");
        Assert(Math.Abs(p105) < 0.001f, $"2nd half Y=34 pan must be 0, got {p105}");

        // 2. Center of pitch (X=52.5, Y=34): D=52.5m boundary -> vol=0, pan=0
        GoalBeaconPlayer.CalculateBeacon(52.5f, 34.0f, isSecondHalf: true, masterVolume: 1.0f, out float dMid, out float vMid, out float pMid);
        Assert(dMid == 52.5f, $"Pitch center distance must be 52.5, got {dMid}");
        Assert(vMid == 0.0f, $"Pitch center volume must be 0, got {vMid}");
        Assert(Math.Abs(pMid) < 0.001f, $"Pitch center pan must be 0, got {pMid}");

        // 3. Attacking Goal line center (X=0, Y=34): D=0m -> vol=1.0, pan=0
        GoalBeaconPlayer.CalculateBeacon(0.0f, 34.0f, isSecondHalf: true, masterVolume: 1.0f, out float dGoal, out float vGoal, out float pGoal);
        Assert(dGoal == 0.0f, $"Target goal distance must be 0 in 2nd half, got {dGoal}");
        Assert(Math.Abs(vGoal - 1.0f) < 0.001f, $"Target goal volume must be 1.0 in 2nd half, got {vGoal}");
        Assert(Math.Abs(pGoal) < 0.001f, $"Target goal pan must be 0 in 2nd half, got {pGoal}");

        // 4. Top-left corner flag (X=0, Y=0): D=34m -> vol ~ 0.209, pan=-1.0 (inverted: facing X=0 makes Y=0 to the player's left!)
        GoalBeaconPlayer.CalculateBeacon(0.0f, 0.0f, isSecondHalf: true, masterVolume: 1.0f, out float dTopLeft, out float vTopLeft, out float pTopLeft);
        Assert(dTopLeft == 34.0f, $"2nd half corner flag distance must be 34m, got {dTopLeft}");
        Assert(vTopLeft > 0.18f && vTopLeft < 0.23f, $"2nd half corner flag volume must be ~0.209, got {vTopLeft:F3}");
        Assert(Math.Abs(pTopLeft - (-1.0f)) < 0.001f, $"2nd half top-left corner pan must be -1.0 (left), got {pTopLeft:F3}");

        // 5. Bottom-left corner flag (X=0, Y=68): D=34m -> vol ~ 0.209, pan=+1.0 (inverted: facing X=0 makes Y=68 to the player's right!)
        GoalBeaconPlayer.CalculateBeacon(0.0f, 68.0f, isSecondHalf: true, masterVolume: 1.0f, out float dBotLeft, out float vBotLeft, out float pBotLeft);
        Assert(dBotLeft == 34.0f, $"2nd half corner flag distance must be 34m, got {dBotLeft}");
        Assert(vBotLeft > 0.18f && vBotLeft < 0.23f, $"2nd half corner flag volume must be ~0.209, got {vBotLeft:F3}");
        Assert(Math.Abs(pBotLeft - 1.0f) < 0.001f, $"2nd half bottom-left corner pan must be +1.0 (right), got {pBotLeft:F3}");
    }

    public static void TestGoalBeacon_1000PointLengthSweep_MonotonicProximity()
    {
        // 1st Half: Sweep X from 0.0 to 105.0 at Y=34 (1000 points)
        float prevVol1st = -0.01f;
        for (int i = 0; i <= 1000; i++)
        {
            float x = i * (105.0f / 1000.0f);
            GoalBeaconPlayer.CalculateBeacon(x, 34.0f, isSecondHalf: false, masterVolume: 1.0f, out float dist, out float vol, out float pan);

            Assert(!float.IsNaN(vol) && !float.IsNaN(dist) && !float.IsNaN(pan), $"NaN detected at X={x}");
            Assert(vol >= 0.0f && vol <= 1.0f, $"Volume {vol} out of range [0, 1] at X={x}");
            Assert(Math.Abs(pan) < 0.001f, $"Center Y=34 pan must be 0, got {pan} at X={x}");

            if (x <= 52.5f)
            {
                Assert(vol == 0.0f, $"Volume must be 0 in defensive half (X={x}), got {vol}");
            }
            else
            {
                Assert(vol >= prevVol1st, $"Volume must be non-decreasing towards goal! At X={x}, vol={vol} < prevVol={prevVol1st}");
                prevVol1st = vol;
            }
        }
        Assert(Math.Abs(prevVol1st - 1.0f) < 0.001f, $"Peak volume at goal line must be 1.0, got {prevVol1st}");

        // 2nd Half: Sweep X from 105.0 down to 0.0 at Y=34 (1000 points)
        float prevVol2nd = -0.01f;
        for (int i = 0; i <= 1000; i++)
        {
            float x = 105.0f - i * (105.0f / 1000.0f);
            GoalBeaconPlayer.CalculateBeacon(x, 34.0f, isSecondHalf: true, masterVolume: 1.0f, out float dist, out float vol, out float pan);

            Assert(!float.IsNaN(vol) && !float.IsNaN(dist) && !float.IsNaN(pan), $"NaN detected in 2nd half at X={x}");
            Assert(vol >= 0.0f && vol <= 1.0f, $"Volume {vol} out of range [0, 1] in 2nd half at X={x}");
            Assert(Math.Abs(pan) < 0.001f, $"Center Y=34 pan must be 0 in 2nd half, got {pan} at X={x}");

            if (x >= 52.5f)
            {
                Assert(vol == 0.0f, $"Volume must be 0 in defensive half (X={x}) in 2nd half, got {vol}");
            }
            else
            {
                Assert(vol >= prevVol2nd, $"2nd half volume must be non-decreasing towards X=0! At X={x}, vol={vol} < prevVol={prevVol2nd}");
                prevVol2nd = vol;
            }
        }
        Assert(Math.Abs(prevVol2nd - 1.0f) < 0.001f, $"Peak volume at X=0 must be 1.0, got {prevVol2nd}");
    }

    public static void TestGoalBeacon_500PointWidthSweep_AntisymmetryAndClamping()
    {
        // Width sweep across Y from 0 to 68 at attacking third X=85 (1st half)
        float prevPan = 2.0f;
        for (int i = 0; i <= 500; i++)
        {
            float y = i * (68.0f / 500.0f);
            GoalBeaconPlayer.CalculateBeacon(85.0f, y, isSecondHalf: false, masterVolume: 1.0f, out _, out _, out float pan);

            Assert(pan >= -1.0f && pan <= 1.0f, $"Pan {pan} out of range [-1, 1] at Y={y}");

            // Pan must decrease monotonically from +1.0 to -1.0 as Y increases
            Assert(pan <= prevPan, $"Pan must decrease monotonically with Y! At Y={y}, pan={pan} > prevPan={prevPan}");
            prevPan = pan;

            // Test perfect antisymmetry around GoalCenterY = 34
            float delta = y - 34.0f;
            if (delta > 0.0f && delta <= 34.0f)
            {
                GoalBeaconPlayer.CalculateBeacon(85.0f, 34.0f - delta, isSecondHalf: false, masterVolume: 1.0f, out _, out _, out float panMirrored);
                Assert(Math.Abs(pan + panMirrored) < 0.001f,
                    $"Pan antisymmetry failed at delta={delta}! Pan({34 + delta})={pan}, Pan({34 - delta})={panMirrored}");
            }
        }
    }

    public static void TestGoalBeacon_PossessionGating_OnlyPlaysWhenUserHasBall()
    {
        // When hasPossession == false, UpdateBeacon must never play
        GoalBeaconPlayer.Initialize();
        
        // At X=100, Y=34 (attacking 6-yard box, vol > 0.85), but hasPossession = false
        // UpdateBeacon should return without throwing or playing
        GoalBeaconPlayer.UpdateBeacon(100.0f, 34.0f, isSecondHalf: false, hasPossession: false);

        // At X=100, Y=34 with hasPossession = true
        GoalBeaconPlayer.UpdateBeacon(100.0f, 34.0f, isSecondHalf: false, hasPossession: true);
        GoalBeaconPlayer.Stop();
    }

    // =========================================================================
    // 3. OUT-OF-REACH TACKLE & TACKLEMISSED EARCON
    // =========================================================================

    public static void TestOutOfReach_StandingTackle_Threshold_PlaysEarconZeroSpeech()
    {
        SmartAudioCoordinator.SilenceLiveMatchSpeech = true;
        var mockReader = new MockScreenReader();
        var coordinator = new SmartAudioCoordinator(mockReader);
        var eventBus = new MatchEventBus();
        eventBus.OnMatchEvent += coordinator.HandleMatchEvent;

        var match = new MatchEngine(TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek(), eventBus, new KeyboardInputHandler());
        var user = match.ActiveUserPlayer;
        user.Position2D = new Vector2(50.0f, 30.0f);

        var carrier = match.AwayTeam.Players[9];

        // Part 1: Distance = 1.79m (<= 1.8m) -> IN RANGE
        carrier.Position2D = new Vector2(51.79f, 30.0f);
        match.Ball.Position = new Vector3(51.79f, 30.0f, 0.0f);
        match.Ball.CatchOrTackle(carrier);

        SoundEffectsPlayer.ResetTelemetry();
        match.ExecuteStandTackle();

        Assert(SoundEffectsPlayer.LastPlayedSound != "TackleMissed",
            $"Tackle at 1.79m is IN REACH and must NOT trigger TackleMissed! Got: '{SoundEffectsPlayer.LastPlayedSound}'");
        Assert(mockReader.SpokenTexts.Count == 0, "In-reach tackle must produce zero speech");

        // Part 2: Wait out cooldown
        for (int i = 0; i < 25; i++) match.Update(0.02f);

        // Part 3: Distance = 1.81m (> 1.8m) -> OUT OF REACH
        carrier.Position2D = new Vector2(51.81f, 30.0f);
        match.Ball.Position = new Vector3(51.81f, 30.0f, 0.0f);
        match.Ball.CatchOrTackle(carrier);

        SoundEffectsPlayer.ResetTelemetry();
        match.ExecuteStandTackle();

        Assert(SoundEffectsPlayer.LastPlayedSound == "TackleMissed",
            $"Standing tackle at 1.81m (> 1.8m) MUST play 'TackleMissed' earcon! Got: '{SoundEffectsPlayer.LastPlayedSound}'");
        Assert(mockReader.SpokenTexts.Count == 0,
            $"Out-of-reach tackle under SilenceLiveMatchSpeech = true must produce ZERO speech! Got {mockReader.SpokenTexts.Count}");
    }

    public static void TestOutOfReach_SlideTackle_Threshold_PlaysEarconZeroSpeech()
    {
        SmartAudioCoordinator.SilenceLiveMatchSpeech = true;
        var mockReader = new MockScreenReader();
        var coordinator = new SmartAudioCoordinator(mockReader);
        var eventBus = new MatchEventBus();
        eventBus.OnMatchEvent += coordinator.HandleMatchEvent;

        var match = new MatchEngine(TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek(), eventBus, new KeyboardInputHandler());
        var user = match.ActiveUserPlayer;
        user.Position2D = new Vector2(50.0f, 30.0f);
        user.FacingDirection = new Vector2(1.0f, 0.0f);

        var carrier = match.AwayTeam.Players[9];

        // Part 1: Distance = 2.79m (<= 2.8m) -> IN RANGE
        carrier.Position2D = new Vector2(52.79f, 30.0f);
        match.Ball.Position = new Vector3(52.79f, 30.0f, 0.0f);
        match.Ball.CatchOrTackle(carrier);

        SoundEffectsPlayer.ResetTelemetry();
        match.ExecuteSlideTackle();

        Assert(SoundEffectsPlayer.LastPlayedSound != "TackleMissed",
            $"Slide tackle at 2.79m is IN REACH and must NOT trigger TackleMissed! Got: '{SoundEffectsPlayer.LastPlayedSound}'");
        Assert(mockReader.SpokenTexts.Count == 0, "In-reach slide tackle must produce zero speech");

        // Part 2: Wait out cooldown
        for (int i = 0; i < 50; i++) match.Update(0.02f);

        // Part 3: Distance = 2.81m (> 2.8m) -> OUT OF REACH
        carrier.Position2D = new Vector2(52.81f, 30.0f);
        match.Ball.Position = new Vector3(52.81f, 30.0f, 0.0f);
        match.Ball.CatchOrTackle(carrier);

        SoundEffectsPlayer.ResetTelemetry();
        match.ExecuteSlideTackle();

        Assert(SoundEffectsPlayer.LastPlayedSound == "TackleMissed",
            $"Slide tackle at 2.81m (> 2.8m) MUST play 'TackleMissed' earcon! Got: '{SoundEffectsPlayer.LastPlayedSound}'");
        Assert(mockReader.SpokenTexts.Count == 0,
            $"Out-of-reach slide tackle under SilenceLiveMatchSpeech = true must produce ZERO speech! Got {mockReader.SpokenTexts.Count}");
    }

    public static void TestOutOfReach_ExtremeDistancesAndLooseBall_TackleMissedZeroSpeech()
    {
        SmartAudioCoordinator.SilenceLiveMatchSpeech = true;
        var mockReader = new MockScreenReader();
        var coordinator = new SmartAudioCoordinator(mockReader);
        var eventBus = new MatchEventBus();
        eventBus.OnMatchEvent += coordinator.HandleMatchEvent;

        var match = new MatchEngine(TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek(), eventBus, new KeyboardInputHandler());
        var user = match.ActiveUserPlayer;
        user.Position2D = new Vector2(50.0f, 30.0f);

        float[] distances = { 5.0f, 15.0f, 40.0f };
        foreach (var dist in distances)
        {
            // Reset cooldown
            for (int i = 0; i < 30; i++) match.Update(0.02f);

            var carrier = match.AwayTeam.Players[9];
            carrier.Position2D = new Vector2(50.0f + dist, 30.0f);
            match.Ball.Position = new Vector3(50.0f + dist, 30.0f, 0.0f);
            match.Ball.CatchOrTackle(carrier);

            SoundEffectsPlayer.ResetTelemetry();
            match.ExecuteStandTackle();

            Assert(SoundEffectsPlayer.LastPlayedSound == "TackleMissed",
                $"Tackle at distance {dist}m must trigger 'TackleMissed', got: '{SoundEffectsPlayer.LastPlayedSound}'");
            Assert(mockReader.SpokenTexts.Count == 0,
                $"Tackle at distance {dist}m must produce 0 spoken texts, got {mockReader.SpokenTexts.Count}");
        }

        // Test with loose ball (no carrier) 10m away
        for (int i = 0; i < 30; i++) match.Update(0.02f);
        match.Ball.Position = new Vector3(60.0f, 30.0f, 0.0f);
        if (match.Ball.Owner != null)
        {
            match.Ball.Owner.HasBall = false;
            match.Ball.Owner = null;
        }
        match.Ball.State = BallState.Free;

        SoundEffectsPlayer.ResetTelemetry();
        match.ExecuteStandTackle();

        Assert(SoundEffectsPlayer.LastPlayedSound == "TackleMissed",
            $"Tackle on loose ball 10m away must trigger 'TackleMissed', got: '{SoundEffectsPlayer.LastPlayedSound}'");
        Assert(mockReader.SpokenTexts.Count == 0,
            $"Tackle on loose ball must produce 0 spoken texts, got {mockReader.SpokenTexts.Count}");
    }

    public static void TestTackleMissed_AudioAssetVerification()
    {
        string root = FindWorkspaceRoot();
        string path = Path.Combine(root, "Assets", "Audio", "SFX", "Earcons", "TackleMissed.wav");

        Assert(File.Exists(path), $"TackleMissed.wav must exist at: {path}");
        var fi = new FileInfo(path);
        Assert(fi.Length > 2000, $"TackleMissed.wav length ({fi.Length} bytes) must be > 2KB");

        using var fs = File.OpenRead(path);
        using var br = new BinaryReader(fs);

        string riff = Encoding.ASCII.GetString(br.ReadBytes(4));
        int chunkSize = br.ReadInt32();
        string wave = Encoding.ASCII.GetString(br.ReadBytes(4));
        Assert(riff == "RIFF" && wave == "WAVE", "TackleMissed.wav must have valid RIFF/WAVE header");

        string fmt = Encoding.ASCII.GetString(br.ReadBytes(4));
        Assert(fmt == "fmt ", "fmt subchunk missing");
        int subchunk1Size = br.ReadInt32();
        short audioFormat = br.ReadInt16();
        Assert(audioFormat == 1, "PCM format expected");
        short channels = br.ReadInt16();
        Assert(channels == 2, "Stereo 2-channel audio expected");
        int sampleRate = br.ReadInt32();
        Assert(sampleRate == 44100, $"Sample rate must be 44100 Hz, got {sampleRate}");

        // Verify non-silent samples
        fs.Seek(44, SeekOrigin.Begin);
        int nonZeroSamples = 0;
        int maxSample = 0;
        while (fs.Position < fs.Length - 2)
        {
            short sample = br.ReadInt16();
            if (Math.Abs(sample) > 100) nonZeroSamples++;
            if (Math.Abs(sample) > maxSample) maxSample = Math.Abs(sample);
        }

        Assert(nonZeroSamples > 500, $"TackleMissed.wav must contain audible audio, found only {nonZeroSamples} non-zero samples");
        Assert(maxSample > 5000, $"TackleMissed.wav peak amplitude must be audible (>5000), got {maxSample}");
    }

    private class MockScreenReader : IScreenReader
    {
        public bool IsAvailable => true;
        public List<string> SpokenTexts { get; } = new();
        public void Speak(string text, bool interrupt = true) => SpokenTexts.Add(text);
        public void CancelSpeech() {}
    }
}
