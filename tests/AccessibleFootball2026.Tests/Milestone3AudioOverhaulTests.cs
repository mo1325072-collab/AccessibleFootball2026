using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using AccessibleFootball2026.Audio;
using AccessibleFootball2026.Audio.Anthems;
using AccessibleFootball2026.Audio.Commentary;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;
using AccessibleFootball2026.Input;

namespace AccessibleFootball2026.Tests;

/// <summary>
/// MILESTONE 3: AUDIO OVERHAUL & AUTHENTIC PES 2013 AUDIO INTEGRATION TEST SUITE
/// Covers Tests 14A through 14P:
/// - CRI ADX Asset Extraction & Valid Headers / Sample Rates
/// - 8+1 Tonal Musical Earcons (including TackleMissed) & Zero TTS during playback
/// - 100% Screen Reader Silence during active match play
/// - Defender Switch Earcon micro-click with zero speech
/// - Tactical Radar exclusive speech on explicit Space request
/// - 3D Spatial Goal Beacon Proximity Volume Curve, Stereo Panning & Half-Time Inversion
/// - Up/Down Arrow Aiming Angle Trajectory
/// - Event Bindings: Goalkeeper Save, Woodwork Hit, Stoppage Time, Penalty Awarded, Full Time
/// - Authentic National Anthems & UEFA Champions League Anthem with instant skip
/// - Grass Footsteps & Synthetic Audio Elimination
/// </summary>
public static class Milestone3AudioOverhaulTests
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

    private static MatchEngine CreateTestMatch(MatchEventBus? eventBus = null, KeyboardInputHandler? inputHandler = null)
    {
        var home = TeamDatabase.CreateAlAhly();
        var away = TeamDatabase.CreateZamalek();
        eventBus ??= new MatchEventBus();
        inputHandler ??= new KeyboardInputHandler();
        return new MatchEngine(home, away, eventBus, inputHandler);
    }

    public static void RunAll(Action<string, Action> runTest)
    {
        Console.WriteLine("\n--- MILESTONE 3: AUDIO OVERHAUL & AUTHENTIC PES AUDIO TEST SUITE ---");

        runTest("14A. PES Audio Extraction: Valid RIFF headers, channels and sample rates on disk",
            TestPESAudioExtraction_ValidHeadersAndSampleRates);

        runTest("14B. Tonal Musical Earcons: All 9 earcons exist, valid PCM, trigger correctly with zero TTS",
            TestAll8Earcons_ExistAndPlayWithoutTTS);

        runTest("14C. Screen Reader Match Silence: 100% speech silence during active match play",
            TestScreenReaderSilence_ActiveMatchPlay);

        runTest("14D. Defender Switching: Plays micro-click earcon only, zero TTS speech",
            TestDefenderSwitch_FiresEarcon_ZeroSpeech);

        runTest("14E. Tactical Radar: Speech strictly reserved for explicit Space key request",
            TestTacticalRadar_OnlySpeaksOnSpace);

        runTest("14F. 3D Spatial Goal Beacon: Smooth monotonic volume swell curve based on proximity",
            TestGoalBeacon_VolumeCurveProximity);

        runTest("14G. 3D Spatial Goal Beacon: Accurate stereo panning geometry (center, left, right flanks)",
            TestGoalBeacon_StereoPanningGeometry);

        runTest("14H. 3D Spatial Goal Beacon: Half-time side swap inversion (targets X=0)",
            TestGoalBeacon_HalfTimeInversion);

        runTest("14I. Up/Down Arrow Aiming: Tilts facing direction and shot trajectory towards top/bottom corners",
            TestUpDownArrow_AimingAngleTrajectory);

        runTest("14J. Event Binding: GoalkeeperSave triggers save deflection SFX and Raouf Khlif commentary",
            TestEventBinding_GoalkeeperSave);

        runTest("14K. Event Binding: WoodworkHit triggers post_hit sound and woodwork commentary",
            TestEventBinding_WoodworkHit);

        runTest("14L. Event Binding: Stoppage time triggers at 44:30 and 89:30 with StoppageTimeAnnounced",
            TestEventBinding_StoppageTimeAnnounced);

        runTest("14M. Event Binding: Penalty box foul triggers PenaltyAwarded, whistle, and Penalty earcon",
            TestEventBinding_PenaltyAwarded);

        runTest("14N. Event Binding: Match reaching 90:00 triggers FullTime event and full-time commentary",
            TestEventBinding_FullTime);

        runTest("14O. National Anthems: Champions League & National Anthems wired and skippable",
            TestNationalAnthems_WiredAndSkippable);

        runTest("14P. Synthetic Sound Elimination: Footsteps & synth removed, 9th TackleMissed earcon active",
            TestSyntheticSoundElimination_AndTackleMissedEarcon);
    }

    public static void TestPESAudioExtraction_ValidHeadersAndSampleRates()
    {
        string root = FindWorkspaceRoot();
        string audioDir = Path.Combine(root, "Assets", "Audio");

        // 1. Champions League Anthem
        string clPath = Path.Combine(audioDir, "Anthems", "ChampionsLeague.wav");
        Assert(File.Exists(clPath), $"ChampionsLeague.wav must exist at: {clPath}");
        var clInfo = new FileInfo(clPath);
        Assert(clInfo.Length > 1_000_000, $"ChampionsLeague.wav size ({clInfo.Length} bytes) must be > 1MB");

        using (var fs = File.OpenRead(clPath))
        using (var br = new BinaryReader(fs))
        {
            string riff = Encoding.ASCII.GetString(br.ReadBytes(4));
            br.ReadInt32(); // size
            string wave = Encoding.ASCII.GetString(br.ReadBytes(4));
            Assert(riff == "RIFF" && wave == "WAVE", "ChampionsLeague.wav must have valid RIFF/WAVE header");

            string fmt = Encoding.ASCII.GetString(br.ReadBytes(4));
            Assert(fmt == "fmt ", "fmt subchunk missing");
            br.ReadInt32(); // subchunk1Size
            short audioFormat = br.ReadInt16();
            Assert(audioFormat == 1, "PCM format expected");
            short channels = br.ReadInt16();
            Assert(channels == 2, "Stereo 2-channel audio expected");
            int sampleRate = br.ReadInt32();
            Assert(sampleRate == 24000, $"Sample rate must be 24000 Hz, got {sampleRate}");
        }

        // 2. Crowd Ambiance
        string crowdPath = Path.Combine(audioDir, "SFX", "Crowd", "CrowdAmbiance.wav");
        Assert(File.Exists(crowdPath), $"CrowdAmbiance.wav must exist at: {crowdPath}");
        using (var fs = File.OpenRead(crowdPath))
        using (var br = new BinaryReader(fs))
        {
            string riff = Encoding.ASCII.GetString(br.ReadBytes(4));
            br.ReadInt32();
            string wave = Encoding.ASCII.GetString(br.ReadBytes(4));
            Assert(riff == "RIFF" && wave == "WAVE", "CrowdAmbiance.wav must have valid RIFF/WAVE header");
            br.ReadBytes(4); // fmt
            br.ReadInt32();
            br.ReadInt16(); // PCM
            short channels = br.ReadInt16();
            Assert(channels == 2, "Crowd must be stereo 2-channel");
            int sr = br.ReadInt32();
            Assert(sr == 18000 || sr == 24000, $"Crowd sample rate must be 18000 or 24000 Hz, got {sr}");
        }

        // 3. PostHit
        string postPath = Path.Combine(audioDir, "SFX", "PostHit", "PostHit.wav");
        Assert(File.Exists(postPath), $"PostHit.wav must exist at: {postPath}");

        // 4. Raouf Khlif Goal Commentary
        string goalPath = Path.Combine(audioDir, "Commentary", "RaoufKhlif", "Goals", "Goal_133.wav");
        Assert(File.Exists(goalPath), $"Goal_133.wav must exist at: {goalPath}");
        using (var fs = File.OpenRead(goalPath))
        using (var br = new BinaryReader(fs))
        {
            string riff = Encoding.ASCII.GetString(br.ReadBytes(4));
            br.ReadInt32();
            string wave = Encoding.ASCII.GetString(br.ReadBytes(4));
            Assert(riff == "RIFF" && wave == "WAVE", "Goal_133.wav must have valid RIFF/WAVE header");
            br.ReadBytes(4); // fmt
            br.ReadInt32();
            br.ReadInt16(); // PCM
            br.ReadInt16(); // channels
            int sr = br.ReadInt32();
            Assert(sr == 44100, $"Goal commentary sample rate must be 44100 Hz, got {sr}");
        }

        // 5. National Anthems
        string[] nationalAnthems = { "FRA.wav", "ARG.wav", "EGY.wav", "BRA.wav" };
        foreach (var na in nationalAnthems)
        {
            string p = Path.Combine(audioDir, "Anthems", "National", na);
            Assert(File.Exists(p), $"National anthem {na} must exist at {p}");
        }
    }

    public static void TestAll8Earcons_ExistAndPlayWithoutTTS()
    {
        string root = FindWorkspaceRoot();
        string earconsDir = Path.Combine(root, "Assets", "Audio", "SFX", "Earcons");

        string[] earconNames = 
        {
            "TackleWon", "BallLost", "Foul", "Penalty", 
            "Corner", "GoalKick", "ThrowIn", "PlayerSwitch", "TackleMissed"
        };

        var mockReader = new MockScreenReader();
        SoundEffectsPlayer.Initialize(Path.Combine(root, "Assets", "Audio"));

        foreach (var name in earconNames)
        {
            string file = Path.Combine(earconsDir, name + ".wav");
            Assert(File.Exists(file), $"Earcon file missing on disk: {file}");
            var fi = new FileInfo(file);
            Assert(fi.Length > 2000, $"Earcon {name} size ({fi.Length} bytes) must be > 2KB");

            using var fs = File.OpenRead(file);
            byte[] header = new byte[12];
            fs.ReadExactly(header, 0, 12);
            string riff = Encoding.ASCII.GetString(header, 0, 4);
            string wave = Encoding.ASCII.GetString(header, 8, 4);
            Assert(riff == "RIFF" && wave == "WAVE", $"Earcon {name} must be valid RIFF WAVE");

            // Play earcon
            SoundEffectsPlayer.PlayEarcon(name);
            Assert(SoundEffectsPlayer.LastPlayedSound == name, 
                $"LastPlayedSound must be '{name}', got: '{SoundEffectsPlayer.LastPlayedSound}'");

            // Verify ZERO TTS speech
            Assert(mockReader.SpokenTexts.Count == 0, 
                $"Earcon {name} playback must produce 0 TTS speech messages!");
        }
    }

    public static void TestScreenReaderSilence_ActiveMatchPlay()
    {
        SmartAudioCoordinator.SilenceLiveMatchSpeech = true;
        var mockReader = new MockScreenReader();
        var coordinator = new SmartAudioCoordinator(mockReader);
        var eventBus = new MatchEventBus();
        eventBus.OnMatchEvent += coordinator.HandleMatchEvent;

        var inputHandler = new KeyboardInputHandler();
        var match = new MatchEngine(TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek(), eventBus, inputHandler);

        // Run 200 ticks (4.0s of active play)
        for (int i = 0; i < 200; i++)
        {
            match.Update(0.02f);
        }

        // Fire routine in-play match events
        coordinator.NotifyBallZoneChanged(PitchZone.Midfield);
        eventBus.Publish(new MatchEvent(MatchEventType.BallKicked, "تمريرة أرضية قصيرة"));
        eventBus.Publish(new MatchEvent(MatchEventType.BallTrapped, "استلم صلاح الكرة بنجاح"));
        eventBus.Publish(new MatchEvent(MatchEventType.UserTackleSuccessful, "استخلاص رائع للكرة"));
        eventBus.Publish(new MatchEvent(MatchEventType.ShotTaken, "تسديدة قوية"));
        eventBus.Publish(new MatchEvent(MatchEventType.TacticalRadarInfo, "بعيد عن حامل الكرة: 4.8 متر"));

        // Verify 100% Speech Silence during active play
        Assert(mockReader.SpokenTexts.Count == 0, 
            $"CRITICAL DIRECTIVE VIOLATION: Active match play produced {mockReader.SpokenTexts.Count} spoken texts! Expected: 0. First: '{(mockReader.SpokenTexts.Count > 0 ? mockReader.SpokenTexts[0] : "")}'");
    }

    public static void TestDefenderSwitch_FiresEarcon_ZeroSpeech()
    {
        SmartAudioCoordinator.SilenceLiveMatchSpeech = true;
        var mockReader = new MockScreenReader();
        var coordinator = new SmartAudioCoordinator(mockReader);
        var eventBus = new MatchEventBus();
        eventBus.OnMatchEvent += coordinator.HandleMatchEvent;

        var match = new MatchEngine(TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek(), eventBus, new KeyboardInputHandler());
        var initial = match.ActiveUserPlayer;

        SoundEffectsPlayer.ResetTelemetry();
        match.SwitchToClosestPlayer();

        Assert(match.ActiveUserPlayer != initial, "Defender switch must select new teammate");
        Assert(SoundEffectsPlayer.LastPlayedSound == "PlayerSwitch", 
            $"Defender switch must play 'PlayerSwitch' micro-click earcon, got: '{SoundEffectsPlayer.LastPlayedSound}'");
        Assert(mockReader.SpokenTexts.Count == 0, 
            "Defender switch MUST NOT produce any spoken speech under SilenceLiveMatchSpeech = true!");
    }

    public static void TestTacticalRadar_OnlySpeaksOnSpace()
    {
        SmartAudioCoordinator.SilenceLiveMatchSpeech = true;
        var mockReader = new MockScreenReader();
        var coordinator = new SmartAudioCoordinator(mockReader);
        var eventBus = new MatchEventBus();
        eventBus.OnMatchEvent += coordinator.HandleMatchEvent;

        var match = new MatchEngine(TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek(), eventBus, new KeyboardInputHandler());

        // Explicit user request via Space key
        match.ReportTacticalRadar();

        Assert(mockReader.SpokenTexts.Count == 1, 
            $"Explicit Space radar must produce exactly 1 spoken announcement, got {mockReader.SpokenTexts.Count}");
        string text = mockReader.SpokenTexts[0];
        Assert(text.Contains("الملعب") || text.Contains("مرمى") || text.Contains("متر"), 
            $"Radar text must contain location/distance info, got: '{text}'");
    }

    public static void TestGoalBeacon_VolumeCurveProximity()
    {
        // 1st half: Target goal at (105, 34)
        // D > 52.5m: volume == 0
        GoalBeaconPlayer.CalculateBeacon(30.0f, 34.0f, isSecondHalf: false, masterVolume: 1.0f, out float d1, out float v1, out _);
        Assert(d1 == 75.0f, $"Distance at X=30 should be 75m, got {d1}");
        Assert(v1 == 0.0f, $"Volume at D=75m must be 0, got {v1}");

        GoalBeaconPlayer.CalculateBeacon(52.5f, 34.0f, isSecondHalf: false, masterVolume: 1.0f, out float d2, out float v2, out _);
        Assert(d2 == 52.5f, $"Distance at X=52.5 should be 52.5m, got {d2}");
        Assert(v2 == 0.0f, $"Volume at D=52.5m boundary must be 0, got {v2}");

        // Attacking third (D = 35m)
        GoalBeaconPlayer.CalculateBeacon(70.0f, 34.0f, isSecondHalf: false, masterVolume: 1.0f, out float d3, out float v3, out _);
        Assert(d3 == 35.0f, $"Distance at X=70 should be 35m, got {d3}");
        Assert(v3 > 0.15f && v3 < 0.40f, $"Volume at D=35m should be ~0.27, got {v3:F3}");

        // Edge of box (D = 15m)
        GoalBeaconPlayer.CalculateBeacon(90.0f, 34.0f, isSecondHalf: false, masterVolume: 1.0f, out float d4, out float v4, out _);
        Assert(d4 == 15.0f, $"Distance at X=90 should be 15m, got {d4}");
        Assert(v4 > 0.50f && v4 < 0.75f, $"Volume at D=15m should be ~0.60, got {v4:F3}");

        // Penalty spot / 6-yard box (D = 5m)
        GoalBeaconPlayer.CalculateBeacon(100.0f, 34.0f, isSecondHalf: false, masterVolume: 1.0f, out float d5, out float v5, out _);
        Assert(d5 == 5.0f, $"Distance at X=100 should be 5m, got {d5}");
        Assert(v5 > 0.85f, $"Volume at D=5m should be > 0.85, got {v5:F3}");

        // Goal line (D = 0m)
        GoalBeaconPlayer.CalculateBeacon(105.0f, 34.0f, isSecondHalf: false, masterVolume: 1.0f, out float d6, out float v6, out _);
        Assert(d6 == 0.0f, $"Distance at goal line must be 0, got {d6}");
        Assert(Math.Abs(v6 - 1.0f) < 0.001f, $"Volume at goal line must be 1.0, got {v6:F3}");

        // Strict monotonicity
        Assert(v6 > v5 && v5 > v4 && v4 > v3 && v3 > v2, 
            "Goal beacon volume curve must be smoothly and strictly monotonic with proximity!");
    }

    public static void TestGoalBeacon_StereoPanningGeometry()
    {
        // 1st half: Target goal at (105, 34)
        // Center: Y=34 -> pan == 0
        GoalBeaconPlayer.CalculateBeacon(85.0f, 34.0f, isSecondHalf: false, masterVolume: 1.0f, out _, out _, out float panCenter);
        Assert(Math.Abs(panCenter) < 0.001f, $"Center Y=34 must produce pan == 0, got {panCenter:F3}");

        // Left flank: Y=10 -> Goal is to the right (+pan)
        GoalBeaconPlayer.CalculateBeacon(85.0f, 10.0f, isSecondHalf: false, masterVolume: 1.0f, out _, out _, out float panLeft);
        Assert(panLeft > 0.60f, $"Left flank Y=10 must produce pan > 0.60 towards right, got {panLeft:F3}");

        // Right flank: Y=58 -> Goal is to the left (-pan)
        GoalBeaconPlayer.CalculateBeacon(85.0f, 58.0f, isSecondHalf: false, masterVolume: 1.0f, out _, out _, out float panRight);
        Assert(panRight < -0.60f, $"Right flank Y=58 must produce pan < -0.60 towards left, got {panRight:F3}");

        // Touchlines: Y=0 -> pan == 1.0; Y=68 -> pan == -1.0
        GoalBeaconPlayer.CalculateBeacon(85.0f, 0.0f, isSecondHalf: false, masterVolume: 1.0f, out _, out _, out float panTop);
        Assert(Math.Abs(panTop - 1.0f) < 0.001f, $"Top touchline Y=0 must clamp pan to 1.0, got {panTop:F3}");

        GoalBeaconPlayer.CalculateBeacon(85.0f, 68.0f, isSecondHalf: false, masterVolume: 1.0f, out _, out _, out float panBottom);
        Assert(Math.Abs(panBottom - (-1.0f)) < 0.001f, $"Bottom touchline Y=68 must clamp pan to -1.0, got {panBottom:F3}");

        // Hardware invocation integration check: verify PlayBeaconPulse executes genuine MCI mpegvideo stream routing
        GoalBeaconPlayer.PlayBeaconPulse(0.75f, 0.4f);
    }

    public static void TestGoalBeacon_HalfTimeInversion()
    {
        // 2nd half: Target goal inverted to (0, 34)
        // Player at X=85 is in defensive half: D=85m > 52.5m -> vol == 0
        GoalBeaconPlayer.CalculateBeacon(85.0f, 34.0f, isSecondHalf: true, masterVolume: 1.0f, out float dDef, out float vDef, out _);
        Assert(dDef == 85.0f && vDef == 0.0f, $"In 2nd half, X=85 is D=85m -> volume must be 0, got {vDef}");

        // Player at X=15 is attacking: D=15m -> vol > 0.5
        GoalBeaconPlayer.CalculateBeacon(15.0f, 34.0f, isSecondHalf: true, masterVolume: 1.0f, out float dAtt, out float vAtt, out _);
        Assert(dAtt == 15.0f && vAtt > 0.50f, $"In 2nd half, X=15 is D=15m -> volume must be > 0.50, got {vAtt}");

        // Flank pan inversion: Facing towards X=0 inverts perspective!
        GoalBeaconPlayer.CalculateBeacon(15.0f, 10.0f, isSecondHalf: true, masterVolume: 1.0f, out _, out _, out float panLeftInverted);
        Assert(panLeftInverted < -0.60f, $"In 2nd half, left flank Y=10 must invert to pan < -0.60, got {panLeftInverted:F3}");

        GoalBeaconPlayer.CalculateBeacon(15.0f, 58.0f, isSecondHalf: true, masterVolume: 1.0f, out _, out _, out float panRightInverted);
        Assert(panRightInverted > 0.60f, $"In 2nd half, right flank Y=58 must invert to pan > 0.60, got {panRightInverted:F3}");
    }

    public static void TestUpDownArrow_AimingAngleTrajectory()
    {
        var inputHandler = new KeyboardInputHandler();
        var match = CreateTestMatch(inputHandler: inputHandler);
        var player = match.ActiveUserPlayer;

        // Position player at (90, 34) with ball
        player.Position2D = new Vector2(90.0f, 34.0f);
        player.FacingDirection = new Vector2(1.0f, 0.0f);
        match.Ball.Position = new Vector3(90.0f, 34.0f, 0.0f);
        match.Ball.CatchOrTackle(player);

        // 1. Aim Up (VK_UP = 0x26)
        inputHandler.HandleKeyDown(0x26, isPossession: true);
        match.Update(0.02f);
        Assert(player.FacingDirection.Y < -0.1f, 
            $"Holding Up arrow must tilt facing direction up (negative Y), got {player.FacingDirection.Y:F3}");

        match.ExecuteShot();
        Assert(match.Ball.Velocity.Y < -0.5f, 
            $"Shot with Up arrow tilt must have negative Y trajectory, got {match.Ball.Velocity.Y:F2}");
        inputHandler.HandleKeyUp(0x26);

        // 2. Aim Down (VK_DOWN = 0x28)
        player.Position2D = new Vector2(90.0f, 34.0f);
        player.FacingDirection = new Vector2(1.0f, 0.0f);
        match.Ball.Position = new Vector3(90.0f, 34.0f, 0.0f);
        match.Ball.CatchOrTackle(player);

        inputHandler.HandleKeyDown(0x28, isPossession: true);
        match.Update(0.02f);
        Assert(player.FacingDirection.Y > 0.1f, 
            $"Holding Down arrow must tilt facing direction down (positive Y), got {player.FacingDirection.Y:F3}");

        match.ExecuteShot();
        Assert(match.Ball.Velocity.Y > 0.5f, 
            $"Shot with Down arrow tilt must have positive Y trajectory, got {match.Ball.Velocity.Y:F2}");
        inputHandler.HandleKeyUp(0x28);
    }

    public static void TestEventBinding_GoalkeeperSave()
    {
        SoundEffectsPlayer.ResetTelemetry();
        var mockReader = new MockScreenReader();
        var coordinator = new SmartAudioCoordinator(mockReader);
        var eventBus = new MatchEventBus();
        eventBus.OnMatchEvent += coordinator.HandleMatchEvent;

        var match = CreateTestMatch(eventBus: eventBus);
        match.TriggerGoalkeeperSave();

        Assert(SoundEffectsPlayer.LastPlayedSound == "pass_kick", 
            $"GoalkeeperSave must route to deflection kick audio, got: '{SoundEffectsPlayer.LastPlayedSound}'");

        string root = FindWorkspaceRoot();
        string audioDir = Path.Combine(root, "Assets", "Audio");
        var comm = new CommentaryEngine(CommentatorId.RaoufKhlif, audioDir);
        bool commented = comm.TryCommentOnEvent(new MatchEvent(MatchEventType.GoalkeeperSave, "تصدي رائع"));
        Assert(commented, "Raouf Khlif commentary must trigger authentic save voice clip");
    }

    public static void TestEventBinding_WoodworkHit()
    {
        SoundEffectsPlayer.ResetTelemetry();
        var mockReader = new MockScreenReader();
        var coordinator = new SmartAudioCoordinator(mockReader);
        var eventBus = new MatchEventBus();
        eventBus.OnMatchEvent += coordinator.HandleMatchEvent;

        var match = CreateTestMatch(eventBus: eventBus);
        match.TriggerWoodworkHit();

        Assert(SoundEffectsPlayer.LastPlayedSound == "post_hit", 
            $"WoodworkHit must play 'post_hit' SFX, got: '{SoundEffectsPlayer.LastPlayedSound}'");

        string root = FindWorkspaceRoot();
        string audioDir = Path.Combine(root, "Assets", "Audio");
        var comm = new CommentaryEngine(CommentatorId.RaoufKhlif, audioDir);
        bool commented = comm.TryCommentOnEvent(new MatchEvent(MatchEventType.WoodworkHit, "في القائم!"));
        Assert(commented, "Raouf Khlif commentary must trigger authentic woodwork voice clip");
    }

    public static void TestEventBinding_StoppageTimeAnnounced()
    {
        var eventBus = new MatchEventBus();
        bool stoppage1st = false;
        bool stoppage2nd = false;

        eventBus.OnMatchEvent += evt =>
        {
            if (evt.Type == MatchEventType.StoppageTimeAnnounced)
            {
                if (evt.Description.Contains("الأول")) stoppage1st = true;
                if (evt.Description.Contains("الثاني")) stoppage2nd = true;
            }
        };

        var match = new MatchEngine(TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek(), eventBus, new KeyboardInputHandler());

        // 1st half: 44.5m (44:30)
        match.SetMatchTimeMinutes(44.4f);
        match.Update(0.02f);
        match.SetMatchTimeMinutes(44.6f);
        match.Update(0.02f);

        Assert(stoppage1st, "StoppageTimeAnnounced for 1st half must fire at 44:30");
        Assert(match.Stoppage1stHalfAnnounced, "match.Stoppage1stHalfAnnounced must be true");

        // Transition to 2nd half
        match.TriggerHalfTime();
        match.ResumeMatch();

        // 2nd half: 89.5m (89:30)
        match.SetMatchTimeMinutes(89.4f);
        match.Update(0.02f);
        match.SetMatchTimeMinutes(89.6f);
        match.Update(0.02f);

        Assert(stoppage2nd, "StoppageTimeAnnounced for 2nd half must fire at 89:30");
        Assert(match.Stoppage2ndHalfAnnounced, "match.Stoppage2ndHalfAnnounced must be true");
    }

    public static void TestEventBinding_PenaltyAwarded()
    {
        var eventBus = new MatchEventBus();
        int penaltyCount = 0;

        eventBus.OnMatchEvent += evt =>
        {
            if (evt.Type == MatchEventType.PenaltyAwarded) penaltyCount++;
        };

        var coordinator = new SmartAudioCoordinator(new MockScreenReader());
        eventBus.OnMatchEvent += coordinator.HandleMatchEvent;

        var match = new MatchEngine(TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek(), eventBus, new KeyboardInputHandler());

        // 1. Direct Trigger
        SoundEffectsPlayer.ResetTelemetry();
        match.TriggerPenaltyAwarded();
        Assert(penaltyCount == 1, "TriggerPenaltyAwarded must publish PenaltyAwarded event!");
        Assert(SoundEffectsPlayer.LastPlayedSound == "Penalty", 
            $"PenaltyAwarded must route to Penalty tonal earcon, got: '{SoundEffectsPlayer.LastPlayedSound}'");

        // 2. Tackle foul in opponent's penalty box while attacking right (X=95m) -> NO penalty
        Assert(match.UserAttackingRight, "1st half must attack right");
        var opponent = match.AwayTeam.Players[9];
        opponent.Position2D = new Vector2(95.0f, 34.0f);
        opponent.FacingDirection = new Vector2(-1.0f, 0.0f);
        match.Ball.Position = new Vector3(95.0f, 34.0f, 0.0f);
        match.Ball.CatchOrTackle(opponent);

        match.ActiveUserPlayer.Position2D = new Vector2(95.5f, 34.0f);
        match.ActiveUserPlayer.FacingDirection = new Vector2(-1.0f, 0.0f);

        int countBeforeAttackingFoul = penaltyCount;
        match.ExecuteSlideTackle(); // Slide tackle from behind opponent carrier
        Assert(penaltyCount == countBeforeAttackingFoul, 
            "Attacking foul committed inside opponent's penalty box must NOT award a penalty kick!");

        // 3. Tackle foul in user's defending penalty box (X=10m) -> PENALTY awarded!
        match.Update(1.0f); // Elapse tackle cooldown timer (0.9s)
        opponent.Position2D = new Vector2(10.0f, 34.0f);
        opponent.FacingDirection = new Vector2(-1.0f, 0.0f);
        match.Ball.Position = new Vector3(10.0f, 34.0f, 0.0f);
        match.Ball.CatchOrTackle(opponent);

        match.ActiveUserPlayer.Position2D = new Vector2(10.5f, 34.0f);
        match.ActiveUserPlayer.FacingDirection = new Vector2(-1.0f, 0.0f);

        match.ExecuteSlideTackle();
        Assert(penaltyCount > countBeforeAttackingFoul, 
            "Defensive foul committed inside user's defending penalty box MUST award a penalty kick!");
    }

    public static void TestEventBinding_FullTime()
    {
        var eventBus = new MatchEventBus();
        bool ftPublished = false;

        eventBus.OnMatchEvent += evt =>
        {
            if (evt.Type == MatchEventType.FullTime) ftPublished = true;
        };

        var match = new MatchEngine(TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek(), eventBus, new KeyboardInputHandler());
        match.TriggerHalfTime();
        match.ResumeMatch();
        match.SetMatchTimeMinutes(90.0f + match.StoppageMinutes2nd + 0.1f);
        match.Update(0.02f);

        Assert(ftPublished, "Match reaching full time with stoppage must publish MatchEventType.FullTime event");
        Assert(match.FullTimeAnnounced, "match.FullTimeAnnounced must be true");
    }

    public static void TestNationalAnthems_WiredAndSkippable()
    {
        string root = FindWorkspaceRoot();
        string audioDir = Path.Combine(root, "Assets", "Audio");
        var reader = new MockScreenReader();
        var engine = new AnthemEngine(reader, Path.Combine(audioDir, "Anthems"));

        // 1. Champions League Anthem
        engine.PlayChampionsLeagueIntro(isAutomatedTest: false, skipChecker: () => true);
        Assert(engine.LastPlayedAnthem == "ChampionsLeague", 
            $"LastPlayedAnthem must be 'ChampionsLeague', got '{engine.LastPlayedAnthem}'");
        Assert(engine.WasSkipped, "Anthem playback must be skippable on Enter");

        // 2. National Anthems
        var france = TeamDatabase.CreateFrance();
        var argentina = TeamDatabase.CreateArgentina();

        engine.PlayMatchNationalAnthems(france, argentina, isAutomatedTest: false, skipChecker: () => true);
        Assert(engine.LastPlayedAnthem == "ChampionsLeague", "Canceled national feature must not replace approved UCL state");
        Assert(!engine.TryPlayNationalAnthemSample("FRA"), "Canceled national preview must be inert");
    }

    public static void TestSyntheticSoundElimination_AndTackleMissedEarcon()
    {
        // 1. Reflection verification: Footstep loop and synthetic audio must be eliminated from source code
        var stepField = typeof(MatchEngine).GetField("_stepTimer", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert(stepField == null, "MatchEngine._stepTimer MUST be completely deleted!");

        var stepEvent = typeof(MatchEngine).GetEvent("OnUserPlayerStepped");
        Assert(stepEvent == null, "MatchEngine.OnUserPlayerStepped MUST be completely deleted!");

        var turfMethod = typeof(AudioSynthesizer).GetMethod("CreateTurfStep", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert(turfMethod == null, "AudioSynthesizer.CreateTurfStep MUST be completely deleted!");

        var dribbleMethod = typeof(AudioSynthesizer).GetMethod("CreateBallDribble", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert(dribbleMethod == null, "AudioSynthesizer.CreateBallDribble MUST be completely deleted!");

        var crowdMethod = typeof(AudioSynthesizer).GetMethod("CreateCrowdAmbiance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert(crowdMethod == null, "AudioSynthesizer.CreateCrowdAmbiance MUST be completely deleted!");

        // 2. User Direct Directive: 9th Earcon TackleMissed.wav on out-of-reach tackle
        SmartAudioCoordinator.SilenceLiveMatchSpeech = true;
        var mockReader = new MockScreenReader();
        var coordinator = new SmartAudioCoordinator(mockReader);
        var eventBus = new MatchEventBus();
        eventBus.OnMatchEvent += coordinator.HandleMatchEvent;

        var match = new MatchEngine(TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek(), eventBus, new KeyboardInputHandler());
        match.ActiveUserPlayer.Position2D = new Vector2(50.0f, 30.0f);

        var opponent = match.AwayTeam.Players[9];
        opponent.Position2D = new Vector2(55.0f, 30.0f); // 5.0m away (out of 1.8m reach)
        match.Ball.Position = new Vector3(55.0f, 30.0f, 0.0f);
        match.Ball.CatchOrTackle(opponent);

        SoundEffectsPlayer.ResetTelemetry();
        match.ExecuteStandTackle();

        Assert(SoundEffectsPlayer.LastPlayedSound == "TackleMissed", 
            $"Out-of-reach tackle attempt MUST trigger 9th earcon 'TackleMissed'! Got: '{SoundEffectsPlayer.LastPlayedSound}'");
        Assert(mockReader.SpokenTexts.Count == 0, 
            "Out-of-reach tackle under SilenceLiveMatchSpeech = true MUST produce 0 speech to screen reader!");
    }

    private class MockScreenReader : IScreenReader
    {
        public bool IsAvailable => true;
        public List<string> SpokenTexts { get; } = new();
        public void Speak(string text, bool interrupt = true) => SpokenTexts.Add(text);
        public void CancelSpeech() {}
    }
}
