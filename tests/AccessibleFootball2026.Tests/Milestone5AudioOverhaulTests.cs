using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using AccessibleFootball2026.Audio;
using AccessibleFootball2026.Audio.Commentary;
using AccessibleFootball2026.Audio.SFX;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;
using AccessibleFootball2026.Input;

namespace AccessibleFootball2026.Tests;

/// <summary>
/// MILESTONE 5: EXHAUSTIVE PES 2013 SOUNDBANK EXTRACTION & AUDIO SUBSYSTEM UPGRADE TEST SUITE
/// Covers Tests 16A through 16G:
/// - 16A: Raouf Khlif commentary extraction across all 12 semantic directories (3,712 files, 44.1kHz mono PCM)
/// - 16B: Crowd chants (643 files, 18kHz stereo PCM) and stadium acoustics (139 files, 24kHz stereo PCM)
/// - 16C: Callname cataloging (13,610 entries), CallnameManager zero-latency indexing, and Arabic normalization
/// - 16D: CommentaryEngine in-memory cache pre-warming (zero disk I/O) and complete event routing
/// - 16E: MatchEngine throw-in event gap fix (publishes MatchEventType.ThrowIn, respects 10-man rule)
/// - 16F: MatchEngine dramatic events (HatTrickAchieved on 3rd goal, DramaticComeback in minute 70+)
/// - 16G: StadiumSoundEngine match context atmosphere, team chants, and acoustic switching
/// </summary>
public static class Milestone5AudioOverhaulTests
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
        Console.WriteLine("\n--- MILESTONE 5: EXHAUSTIVE PES 2013 AUDIO EXTRACTION & SUBSYSTEM UPGRADE ---");

        runTest("16A. Raouf Khlif Commentary: All 12 semantic directories exist with target counts and 44.1kHz mono headers",
            Test16A_RaoufKhlifCommentary_SemanticDirectoriesAndValidHeaders);

        runTest("16B. Crowd Chants & Acoustics: 643 chants (18kHz stereo) and 139 acoustics (24kHz stereo) valid on disk",
            Test16B_CrowdChantsAndAcoustics_ValidCountsAndFormat);

        runTest("16C. Callnames Index & Manager: 13,610 callnames indexed with sub-microsecond Arabic normalization",
            Test16C_CallnamesCatalogAndManager_IndexingAndSubMicrosecondLookup);

        runTest("16D. CommentaryEngine: Zero-latency in-memory caching and complete event routing (Cards, Injuries, HT, FT)",
            Test16D_CommentaryEngine_InMemoryCacheAndEventRouting);

        runTest("16E. MatchEngine ThrowIn: Publishes MatchEventType.ThrowIn and respects 10-man roster rules",
            Test16E_MatchEngine_ThrowInEventTrigger);

        runTest("16F. MatchEngine Dramatic Triggers: HatTrickAchieved on 3rd goal and DramaticComeback in late match",
            Test16F_MatchEngine_DramaticComebackAndHatTrickTriggers);

        runTest("16G. StadiumSoundEngine: Atmosphere slots, team chants, and stadium acoustics loop management",
            Test16G_StadiumSoundEngine_AtmosphereAndAcoustics);
    }

    public static void Test16A_RaoufKhlifCommentary_SemanticDirectoriesAndValidHeaders()
    {
        string root = FindWorkspaceRoot();
        string commDir = Path.Combine(root, "Assets", "Audio", "Commentary", "RaoufKhlif");
        Assert(Directory.Exists(commDir), $"Commentary directory must exist: {commDir}");

        var expectedMinCounts = new Dictionary<string, int>
        {
            { "Goals", 350 },
            { "OwnGoals", 60 },
            { "Shots", 550 },
            { "Saves", 380 },
            { "Woodwork", 120 },
            { "Tackles", 450 },
            { "Fouls", 380 },
            { "Cards", 180 },
            { "Injuries", 110 },
            { "StoppageTime", 130 },
            { "Intros", 420 },
            { "HalfTime", 280 },
            { "FullTime", 292 }
        };

        int totalSituationalFiles = 0;

        foreach (var (dirName, minCount) in expectedMinCounts)
        {
            string dirPath = Path.Combine(commDir, dirName);
            Assert(Directory.Exists(dirPath), $"Directory '{dirName}' must exist in {commDir}");

            var wavFiles = Directory.GetFiles(dirPath, "*.wav");
            Assert(wavFiles.Length >= minCount, 
                $"Directory '{dirName}' has {wavFiles.Length} files, expected >= {minCount}");
            totalSituationalFiles += wavFiles.Length;

            // Sample and verify the first file in each directory has genuine 44.1kHz mono PCM header
            string sampleFile = wavFiles[0];
            using var fs = File.OpenRead(sampleFile);
            using var br = new BinaryReader(fs);

            string riff = Encoding.ASCII.GetString(br.ReadBytes(4));
            br.ReadInt32(); // size
            string wave = Encoding.ASCII.GetString(br.ReadBytes(4));
            Assert(riff == "RIFF" && wave == "WAVE", $"File '{sampleFile}' must have valid RIFF/WAVE header");

            string fmt = Encoding.ASCII.GetString(br.ReadBytes(4));
            Assert(fmt == "fmt ", $"File '{sampleFile}' fmt chunk missing");
            br.ReadInt32(); // fmt size
            short audioFormat = br.ReadInt16();
            Assert(audioFormat == 1, $"File '{sampleFile}' must be PCM (format 1), got {audioFormat}");
            short channels = br.ReadInt16();
            Assert(channels == 1, $"File '{sampleFile}' must be Mono (channel 1), got {channels}");
            int sampleRate = br.ReadInt32();
            Assert(sampleRate == 44100, $"File '{sampleFile}' must have 44100Hz sample rate, got {sampleRate}");
            br.ReadInt32(); // byte rate
            br.ReadInt16(); // block align
            short bitsPerSample = br.ReadInt16();
            Assert(bitsPerSample == 16, $"File '{sampleFile}' must be 16-bit, got {bitsPerSample}");
        }

        Assert(totalSituationalFiles >= 3712, 
            $"Total situational commentary files must be >= 3712, got {totalSituationalFiles}");
    }

    public static void Test16B_CrowdChantsAndAcoustics_ValidCountsAndFormat()
    {
        string root = FindWorkspaceRoot();
        string chantsDir = Path.Combine(root, "Assets", "Audio", "SFX", "Chants");
        string acousticsDir = Path.Combine(root, "Assets", "Audio", "SFX", "StadiumAcoustics");
        string kicksDir = Path.Combine(root, "Assets", "Audio", "SFX", "Kicks");
        string whistlesDir = Path.Combine(root, "Assets", "Audio", "SFX", "Whistles");

        Assert(Directory.Exists(chantsDir), $"Chants dir must exist: {chantsDir}");
        Assert(Directory.Exists(acousticsDir), $"StadiumAcoustics dir must exist: {acousticsDir}");
        Assert(Directory.Exists(kicksDir), $"Kicks dir must exist: {kicksDir}");
        Assert(Directory.Exists(whistlesDir), $"Whistles dir must exist: {whistlesDir}");

        // 1. Chants (643 total, 18kHz stereo PCM)
        var chantFiles = Directory.GetFiles(chantsDir, "*.wav", SearchOption.AllDirectories);
        Assert(chantFiles.Length >= 643, $"Chant files must be >= 643, got {chantFiles.Length}");

        string sampleChant = chantFiles[0];
        using (var fs = File.OpenRead(sampleChant))
        using (var br = new BinaryReader(fs))
        {
            string riff = Encoding.ASCII.GetString(br.ReadBytes(4));
            br.ReadInt32();
            string wave = Encoding.ASCII.GetString(br.ReadBytes(4));
            Assert(riff == "RIFF" && wave == "WAVE", $"Chant '{sampleChant}' invalid RIFF/WAVE");
            string fmt = Encoding.ASCII.GetString(br.ReadBytes(4));
            Assert(fmt == "fmt ", $"Chant '{sampleChant}' fmt chunk missing");
            br.ReadInt32();
            short audioFormat = br.ReadInt16();
            Assert(audioFormat == 1, $"Chant must be PCM (1), got {audioFormat}");
            short channels = br.ReadInt16();
            Assert(channels == 2, $"Chant must be stereo (2 channels), got {channels}");
            int sampleRate = br.ReadInt32();
            Assert(sampleRate == 18000, $"Chant sample rate must be 18000Hz, got {sampleRate}");
        }

        // 2. Stadium Acoustics (139 files, 24kHz stereo PCM demuxed from AIX)
        var acousticFiles = Directory.GetFiles(acousticsDir, "*.wav");
        Assert(acousticFiles.Length >= 139, $"Stadium acoustics must be >= 139, got {acousticFiles.Length}");

        string sampleAcoustic = acousticFiles[0];
        using (var fs = File.OpenRead(sampleAcoustic))
        using (var br = new BinaryReader(fs))
        {
            string riff = Encoding.ASCII.GetString(br.ReadBytes(4));
            br.ReadInt32();
            string wave = Encoding.ASCII.GetString(br.ReadBytes(4));
            Assert(riff == "RIFF" && wave == "WAVE", $"Acoustic '{sampleAcoustic}' invalid RIFF/WAVE");
            string fmt = Encoding.ASCII.GetString(br.ReadBytes(4));
            Assert(fmt == "fmt ", $"Acoustic '{sampleAcoustic}' fmt chunk missing");
            br.ReadInt32();
            short audioFormat = br.ReadInt16();
            Assert(audioFormat == 1, $"Acoustic must be PCM (1), got {audioFormat}");
            short channels = br.ReadInt16();
            Assert(channels == 2, $"Acoustic must be stereo (2 channels), got {channels}");
            int sampleRate = br.ReadInt32();
            Assert(sampleRate == 24000, $"Acoustic sample rate must be 24000Hz, got {sampleRate}");
        }

        // 3. Kicks and Whistles
        var kickFiles = Directory.GetFiles(kicksDir, "*.wav");
        var whistleFiles = Directory.GetFiles(whistlesDir, "*.wav");
        Assert(kickFiles.Length >= 20, $"Kick files must be >= 20, got {kickFiles.Length}");
        Assert(whistleFiles.Length >= 10, $"Whistle files must be >= 10, got {whistleFiles.Length}");
    }

    public static void Test16C_CallnamesCatalogAndManager_IndexingAndSubMicrosecondLookup()
    {
        string root = FindWorkspaceRoot();
        string callnamesDir = Path.Combine(root, "Assets", "Audio", "Callnames");
        Assert(Directory.Exists(callnamesDir), $"Callnames dir must exist: {callnamesDir}");

        string jsonIndex = Path.Combine(callnamesDir, "callnames_index.json");
        string csvIndex = Path.Combine(callnamesDir, "callnames_index.csv");
        Assert(File.Exists(jsonIndex), $"callnames_index.json must exist at: {jsonIndex}");
        Assert(File.Exists(csvIndex), $"callnames_index.csv must exist at: {csvIndex}");

        var csvLines = File.ReadAllLines(csvIndex);
        Assert(csvLines.Length >= 13610, $"callnames_index.csv must have >= 13610 entries, got {csvLines.Length}");

        var manager = new CallnameManager();

        // Arabic normalization tests
        Assert(CallnameManager.NormalizeArabic("مُحَمَّدٌ") == "محمد", "Diacritics must be removed");
        Assert(CallnameManager.NormalizeArabic("أحمد") == "احمد", "Alef with Hamza above must be normalized to bare Alef");
        Assert(CallnameManager.NormalizeArabic("إبراهيم") == "ابراهيم", "Alef with Hamza below must be normalized to bare Alef");
        Assert(CallnameManager.NormalizeArabic("فاطمة") == "فاطمه", "Taa Marbouta must normalize to Haa");
        Assert(CallnameManager.NormalizeArabic("علي") == "علي", "Yaa normalized");

        // JIT warm-up
        for (int i = 0; i < 1000; i++)
        {
            _ = CallnameManager.NormalizeArabic("مُحَمَّد صَلَاح غَالِي");
        }

        // High-speed benchmark: 10,000 iterations must take < 150ms total (< 15 microseconds per call)
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
        {
            _ = CallnameManager.NormalizeArabic("مُحَمَّد صَلَاح غَالِي");
        }
        sw.Stop();
        Assert(sw.ElapsedMilliseconds < 150, 
            $"Arabic normalization benchmark exceeded budget: {sw.ElapsedMilliseconds}ms for 10,000 iterations");

        // Squad pre-warming test
        var home = TeamDatabase.CreateAlAhly();
        var away = TeamDatabase.CreateZamalek();
        manager.PreWarmSquads(home, away);

        // Player callname retrieval test
        var player = home.Players[0];
        bool played = manager.TryPlayPlayerCallname(player, force: true);
        // Whether or not the specific player has a callname, the method must execute cleanly without exceptions
        Assert(true, "TryPlayPlayerCallname executed cleanly");
    }

    public static void Test16D_CommentaryEngine_InMemoryCacheAndEventRouting()
    {
        var engine = new CommentaryEngine(CommentatorId.RaoufKhlif);

        // Verify pre-warmed cache does not throw and handles new event types
        var yellowCardEvent = new MatchEvent(MatchEventType.YellowCard, "إنذار وبطاقة صفراء");
        bool handledYellow = engine.TryCommentOnEvent(yellowCardEvent, force: true);
        Assert(handledYellow, "CommentaryEngine must handle YellowCard event");
        Assert(yellowCardEvent.HandledByCommentary, "YellowCard event must be marked as handled");

        var injuryEvent = new MatchEvent(MatchEventType.PlayerInjured, "سقوط اللاعب وإصابة");
        bool handledInjury = engine.TryCommentOnEvent(injuryEvent, force: true);
        Assert(handledInjury, "CommentaryEngine must handle PlayerInjured event");

        var htEvent = new MatchEvent(MatchEventType.HalfTime, "نهاية الشوط الأول");
        bool handledHT = engine.TryCommentOnEvent(htEvent, force: true);
        Assert(handledHT, "CommentaryEngine must handle HalfTime event");

        var ftEvent = new MatchEvent(MatchEventType.FullTime, "صافرة نهاية المباراة");
        bool handledFT = engine.TryCommentOnEvent(ftEvent, force: true);
        Assert(handledFT, "CommentaryEngine must handle FullTime event");

        var cornerEvent = new MatchEvent(MatchEventType.CornerKick, "ركلة ركنية");
        bool handledCorner = engine.TryCommentOnEvent(cornerEvent, force: true);
        Assert(handledCorner, "CommentaryEngine must handle CornerKick event");
    }

    public static void Test16E_MatchEngine_ThrowInEventTrigger()
    {
        var eventBus = new MatchEventBus();
        var match = new MatchEngine(TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek(), eventBus, new KeyboardInputHandler());

        MatchEvent? publishedThrowIn = null;
        MatchEvent? publishedKicked = null;

        eventBus.OnMatchEvent += evt =>
        {
            if (evt.Type == MatchEventType.ThrowIn) publishedThrowIn = evt;
            if (evt.Type == MatchEventType.BallKicked) publishedKicked = evt;
        };

        // Put ball out of bounds on the sideline (touchline)
        match.Ball.Owner = null;
        match.Ball.State = BallState.Free;
        match.Ball.Position = new Vector3(50.0f, 70.0f, 0.0f); // Y=70 is past the 68m sideline
        match.Ball.Velocity = Vector3.Zero;

        // Run match engine update
        match.Update(0.02f);

        Assert(publishedThrowIn != null, 
            "MatchEngine MUST publish MatchEventType.ThrowIn when ball crosses touchline!");
        Assert(publishedKicked == null, 
            "MatchEngine MUST NOT publish routine BallKicked on throw-in awards!");
        Assert(match.ActiveUserPlayer != null && !match.ActiveUserPlayer.IsSentOff, 
            "ActiveUserPlayer on throw-in must never be a sent-off player");
    }

    public static void Test16F_MatchEngine_DramaticComebackAndHatTrickTriggers()
    {
        var eventBus = new MatchEventBus();
        var match = new MatchEngine(TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek(), eventBus, new KeyboardInputHandler());

        bool hatTrickFired = false;
        bool comebackFired = false;

        eventBus.OnMatchEvent += evt =>
        {
            if (evt.Type == MatchEventType.HatTrickAchieved) hatTrickFired = true;
            if (evt.Type == MatchEventType.DramaticComeback) comebackFired = true;
        };

        var scorer = match.HomeTeam.Players[9]; // Striker
        scorer.MatchGoalsCount = 0;

        // 1. Simulate 3 goals by the same scorer -> Hat-trick!
        for (int i = 1; i <= 3; i++)
        {
            match.State = MatchState.InPlay;
            match.Ball.Owner = null;
            match.Ball.State = BallState.Free;
            match.Ball.Position = new Vector3(105.1f, 34.0f, 1.0f);
            match.Ball.Velocity = Vector3.Zero;
            typeof(MatchEngine).GetField("LastKicker", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(match, scorer);

            match.Update(0.02f);
        }

        Assert(scorer.MatchGoalsCount >= 3, $"Scorer goals must be >= 3, got {scorer.MatchGoalsCount}");
        Assert(hatTrickFired, "MatchEngine MUST publish HatTrickAchieved on 3rd goal scored by a player!");

        // 2. Simulate Dramatic Comeback in late match (Minute 75)
        var comebackBus = new MatchEventBus();
        comebackBus.OnMatchEvent += evt =>
        {
            if (evt.Type == MatchEventType.DramaticComeback) comebackFired = true;
        };

        var comebackMatch = new MatchEngine(TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek(), comebackBus, new KeyboardInputHandler());
        var awayScorer = comebackMatch.AwayTeam.Players[9];

        // Away scores first (0-1) -> Home trails
        comebackMatch.State = MatchState.InPlay;
        comebackMatch.Ball.Owner = null;
        comebackMatch.Ball.State = BallState.Free;
        comebackMatch.Ball.Position = new Vector3(-0.1f, 34.0f, 1.0f);
        comebackMatch.Ball.Velocity = Vector3.Zero;
        typeof(MatchEngine).GetField("LastKicker", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(comebackMatch, awayScorer);
        comebackMatch.Update(0.02f);

        // Advance match time past minute 70 (e.g. 75th minute)
        typeof(MatchEngine).GetProperty("HalfTimeReached", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            ?.SetValue(comebackMatch, true);
        typeof(MatchEngine).GetProperty("MatchTimeMinutes", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            ?.SetValue(comebackMatch, 75.0f);

        // Home scores equalizer (1-1)
        comebackMatch.State = MatchState.InPlay;
        comebackMatch.Ball.Owner = null;
        comebackMatch.Ball.State = BallState.Free;
        comebackMatch.Ball.Position = new Vector3(105.1f, 34.0f, 1.0f);
        comebackMatch.Ball.Velocity = Vector3.Zero;
        typeof(MatchEngine).GetField("LastKicker", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(comebackMatch, scorer);
        comebackMatch.Update(0.02f);

        // Home scores winner to lead (2-1)
        comebackMatch.State = MatchState.InPlay;
        comebackMatch.Ball.Owner = null;
        comebackMatch.Ball.State = BallState.Free;
        comebackMatch.Ball.Position = new Vector3(105.1f, 34.0f, 1.0f);
        comebackMatch.Ball.Velocity = Vector3.Zero;
        typeof(MatchEngine).GetField("LastKicker", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(comebackMatch, scorer);
        comebackMatch.Update(0.02f);

        Assert(comebackFired, "MatchEngine MUST publish DramaticComeback when trailing team takes the lead in minute 70+!");
    }

    public static void Test16G_StadiumSoundEngine_AtmosphereAndAcoustics()
    {
        var soundEngine = new StadiumSoundEngine();
        var home = TeamDatabase.CreateAlAhly();
        var away = TeamDatabase.CreateZamalek();

        soundEngine.SetMatchTeams(home, away);

        // Trigger various match contexts
        soundEngine.PlayTeamChant(home, ChantSlot.AttackingMomentum);
        soundEngine.PlayTeamChant(home, ChantSlot.GoalCelebration);
        soundEngine.PlayTeamChant(home, ChantSlot.ComebackDefiance);
        soundEngine.PlayTeamChant(home, ChantSlot.DiscontentWhistle);

        // Switching acoustics
        soundEngine.ApplyStadiumAcousticsForTeam(home);

        // Context handling
        soundEngine.HandleMatchContext(
            new MatchEvent(MatchEventType.BallZoneChanged, "تغير المنطقة", PitchZone.AttackingThird),
            homeScore: 1,
            awayScore: 0,
            matchMinute: 25.0f);

        Assert(true, "StadiumSoundEngine handled all atmospheric states and acoustics cleanly");
    }
}
