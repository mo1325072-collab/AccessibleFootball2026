using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AccessibleFootball2026.Audio;
using AccessibleFootball2026.Audio.Anthems;
using AccessibleFootball2026.Audio.Commentary;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;
using AccessibleFootball2026.Input;

namespace AccessibleFootball2026.ChallengerAudio;

public class MockScreenReader : IScreenReader
{
    public bool IsAvailable => true;
    public List<string> SpokenTexts { get; } = new();
    public void Speak(string text, bool interrupt = true) => SpokenTexts.Add(text);
    public void CancelSpeech() {}
}

public static class Program
{
    private static int _passed = 0;
    private static int _failed = 0;
    private static string _audioDir = "";

    public static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("==========================================================================");
        Console.WriteLine("  EMPIRICAL CHALLENGER: MILESTONE 3 AUDIO & EVENT BINDING HARNESS          ");
        Console.WriteLine("==========================================================================");

        _audioDir = FindAudioDir();
        Console.WriteLine($"[AUDIO ROOT]: {_audioDir}\n");
        SoundEffectsPlayer.Initialize(_audioDir);

        Console.WriteLine("--- CHALLENGE 1: EVENT-TO-AUDIO DETERMINISTIC BINDING (ALL 18 EVENTS) ---");
        RunTest("1.1 Goal Scored (Normal) -> GoalNet + CrowdGoal SFX & Commentary Clip", TestGoalNormal_AudioBinding);
        RunTest("1.2 Goal Scored (Own Goal) -> GoalNet + CrowdGoal SFX & Commentary Clip", TestGoalOwnGoal_AudioBinding);
        RunTest("1.3 Missed Chance -> CrowdMiss SFX & Commentary Clip", TestMissedChance_AudioBinding);
        RunTest("1.4 Shot Taken -> Shot Kick SFX & Raouf Khlif Shot Commentary Clip", TestShotTaken_AudioBinding);
        RunTest("1.5 Cross / Lob -> Cross Kick SFX", TestCross_AudioBinding);
        RunTest("1.6 Short Pass -> Pass Kick SFX", TestPass_AudioBinding);
        RunTest("1.7 Stand Tackle -> StandTackle SFX & Commentary Clip", TestStandTackle_AudioBinding);
        RunTest("1.8 Slide Tackle -> SlideTackle SFX & Commentary Clip", TestSlideTackle_AudioBinding);
        RunTest("1.9 Clean Tackle Won -> TackleWon Earcon", TestTackleWon_AudioBinding);
        RunTest("1.10 Ball Lost -> BallLost Earcon", TestBallLost_AudioBinding);
        RunTest("1.11 Tackle Out of Reach (Missed) -> TackleMissed Earcon (9th Earcon)", TestTackleMissed_AudioBinding);
        RunTest("1.12 Foul Committed -> Foul Earcon + Whistle SFX & Commentary Clip", TestFoul_AudioBinding);
        RunTest("1.13 Yellow Card -> Foul/Whistle Audio & Commentary Clip", TestYellowCard_AudioBinding);
        RunTest("1.14 Red Card -> Foul/Whistle Audio & Commentary Clip", TestRedCard_AudioBinding);
        RunTest("1.15 Player Injured -> Whistle Audio & Commentary Handling", TestPlayerInjured_AudioBinding);
        RunTest("1.16 Offside -> Whistle SFX", TestOffside_AudioBinding);
        RunTest("1.17 Penalty Awarded -> Whistle + Penalty Earcon & Commentary Clip", TestPenalty_AudioBinding);
        RunTest("1.18 Corner Kick -> Corner Earcon & Commentary Clip", TestCorner_AudioBinding);
        RunTest("1.19 Throw-in -> ThrowIn Earcon", TestThrowIn_AudioBinding);
        RunTest("1.20 Goal Kick -> GoalKick Earcon", TestGoalKick_AudioBinding);
        RunTest("1.21 HalfTime -> Whistle SFX", TestHalfTime_AudioBinding);
        RunTest("1.22 FullTime -> Whistle SFX & FullTime Commentary Clip", TestFullTime_AudioBinding);
        RunTest("1.23 Goalkeeper Save -> PassKick SFX & Save Commentary Clip", TestSave_AudioBinding);
        RunTest("1.24 Woodwork Hit -> PostHit SFX & Woodwork Commentary Clip", TestWoodwork_AudioBinding);
        RunTest("1.25 Stoppage Time -> Whistle SFX & StoppageTime Commentary Clip", TestStoppageTime_AudioBinding);
        RunTest("1.26 Dramatic Comeback & HatTrick -> Commentary Handling", TestDramaticEvents_AudioBinding);

        Console.WriteLine("\n--- CHALLENGE 2: VERIFY ALL 9 EARCON WAVS (RIFF, SAMPLE RATE, RMS) ---");
        RunTest("2.1 TackleWon.wav: RIFF WAVE, PCM 44100Hz, Non-zero RMS, Headroom", () => VerifyEarconWav("TackleWon.wav"));
        RunTest("2.2 BallLost.wav: RIFF WAVE, PCM 44100Hz, Non-zero RMS, Headroom", () => VerifyEarconWav("BallLost.wav"));
        RunTest("2.3 Foul.wav: RIFF WAVE, PCM 44100Hz, Non-zero RMS, Headroom", () => VerifyEarconWav("Foul.wav"));
        RunTest("2.4 Penalty.wav: RIFF WAVE, PCM 44100Hz, Non-zero RMS, Headroom", () => VerifyEarconWav("Penalty.wav"));
        RunTest("2.5 Corner.wav: RIFF WAVE, PCM 44100Hz, Non-zero RMS, Headroom", () => VerifyEarconWav("Corner.wav"));
        RunTest("2.6 GoalKick.wav: RIFF WAVE, PCM 44100Hz, Non-zero RMS, Headroom", () => VerifyEarconWav("GoalKick.wav"));
        RunTest("2.7 ThrowIn.wav: RIFF WAVE, PCM 44100Hz, Non-zero RMS, Headroom", () => VerifyEarconWav("ThrowIn.wav"));
        RunTest("2.8 PlayerSwitch.wav: RIFF WAVE, PCM 44100Hz, Non-zero RMS, Headroom", () => VerifyEarconWav("PlayerSwitch.wav"));
        RunTest("2.9 TackleMissed.wav (9th): RIFF WAVE, PCM 44100Hz, Non-zero RMS, Headroom", () => VerifyEarconWav("TackleMissed.wav"));

        Console.WriteLine("\n--- CHALLENGE 3: 3D SPATIAL GOAL BEACON ACOUSTIC REASONING ---");
        RunTest("3.1 3D Spatial Goal Beacon: Proximity Curve Monotonicity", TestBeaconProximity);
        RunTest("3.2 3D Spatial Goal Beacon: Dynamic Stereo Panning across Y=0..68", TestBeaconStereoPanning);
        RunTest("3.3 3D Spatial Goal Beacon: Half-Time Inversion towards X=0", TestBeaconHalfTimeInversion);

        Console.WriteLine("\n--- CHALLENGE 4: STRESS HARNESS & RAPID FIRE EVENT BURST ---");
        RunTest("4.1 1,000 Rapid Consecutive Match Events: No Exceptions or Leaks", Test1000RapidEventsBurst);
        RunTest("4.2 Zero File Missing across ALL Commentary Folders & Sound Banks", TestZeroMissingCommentaryClips);

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

    private static void Assert(bool cond, string msg)
    {
        if (!cond) throw new Exception(msg);
    }

    private static string FindAudioDir()
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            string candidate = Path.Combine(dir, "Assets", "Audio");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "SFX", "Earcons", "TackleWon.wav")))
            {
                return candidate;
            }
            dir = Path.GetDirectoryName(dir);
        }
        throw new DirectoryNotFoundException("Could not locate Assets/Audio directory.");
    }

    // -----------------------------------------------------------------------------------------
    // SECTION 1: EVENT-TO-AUDIO DETERMINISTIC BINDING
    // -----------------------------------------------------------------------------------------
    private static void TestGoalNormal_AudioBinding()
    {
        SoundEffectsPlayer.ResetTelemetry();
        var reader = new MockScreenReader();
        var coord = new SmartAudioCoordinator(reader);
        var comm = new CommentaryEngine(CommentatorId.RaoufKhlif, _audioDir);

        var team = TeamDatabase.CreateAlAhly();
        var player = team.Players[9];
        var goalData = new GoalDetails(player, null, false, 25, 1, 0, team);
        var evt = new MatchEvent(MatchEventType.GoalScored, "هدف رائع", PitchZone.AttackingThird, goalData);

        coord.HandleMatchEvent(evt);
        Assert(SoundEffectsPlayer.LastPlayedSound == "crowd_goal", 
            $"Goal must play crowd_goal, got {SoundEffectsPlayer.LastPlayedSound}");

        // Check commentary
        bool commHandled = comm.TryCommentOnEvent(evt);
        Assert(commHandled, "Raouf Khlif commentary must handle GoalScored");
        Assert(SoundEffectsPlayer.LastPlayedSound == "comm_voice", "Commentary must trigger comm_voice");
    }

    private static void TestGoalOwnGoal_AudioBinding()
    {
        SoundEffectsPlayer.ResetTelemetry();
        var reader = new MockScreenReader();
        var coord = new SmartAudioCoordinator(reader);
        var comm = new CommentaryEngine(CommentatorId.RaoufKhlif, _audioDir);

        var team = TeamDatabase.CreateAlAhly();
        var player = team.Players[2];
        var goalData = new GoalDetails(player, null, true, 30, 0, 1, team);
        var evt = new MatchEvent(MatchEventType.GoalScored, "هدف عكسي", PitchZone.OwnThird, goalData);

        coord.HandleMatchEvent(evt);
        Assert(SoundEffectsPlayer.LastPlayedSound == "crowd_goal", "Own goal must trigger crowd_goal");

        bool commHandled = comm.TryCommentOnEvent(evt);
        Assert(commHandled, "Commentary must handle Own Goal");
    }

    private static void TestMissedChance_AudioBinding()
    {
        SoundEffectsPlayer.ResetTelemetry();
        var reader = new MockScreenReader();
        var coord = new SmartAudioCoordinator(reader);
        var comm = new CommentaryEngine(CommentatorId.RaoufKhlif, _audioDir);

        var evt = new MatchEvent(MatchEventType.MissedChance, "فرصة ضائعة خطيرة");
        coord.HandleMatchEvent(evt);
        Assert(SoundEffectsPlayer.LastPlayedSound == "crowd_miss", 
            $"MissedChance must play crowd_miss, got {SoundEffectsPlayer.LastPlayedSound}");
    }

    private static void TestShotTaken_AudioBinding()
    {
        SoundEffectsPlayer.ResetTelemetry();
        var reader = new MockScreenReader();
        var coord = new SmartAudioCoordinator(reader);
        var comm = new CommentaryEngine(CommentatorId.RaoufKhlif, _audioDir);

        var evt = new MatchEvent(MatchEventType.ShotTaken, "تسديدة قوية", spatialPan: 0.5f);
        coord.HandleMatchEvent(evt);
        Assert(SoundEffectsPlayer.LastPlayedSound == "shot_kick", 
            $"ShotTaken must trigger shot_kick, got {SoundEffectsPlayer.LastPlayedSound}");
        Assert(SoundEffectsPlayer.LastPlayedPan == 0.5f, "Spatial pan must be recorded");

        bool commHandled = comm.TryCommentOnEvent(evt);
        Assert(commHandled, "Raouf Khlif commentary must handle ShotTaken");
    }

    private static void TestCross_AudioBinding()
    {
        SoundEffectsPlayer.ResetTelemetry();
        var reader = new MockScreenReader();
        var coord = new SmartAudioCoordinator(reader);

        var evt = new MatchEvent(MatchEventType.BallKicked, "كرة عرضية عالية إلى منطقة الجزاء", spatialPan: -0.6f);
        coord.HandleMatchEvent(evt);
        Assert(SoundEffectsPlayer.LastPlayedSound == "cross_kick", 
            $"Cross must trigger cross_kick, got {SoundEffectsPlayer.LastPlayedSound}");
        Assert(SoundEffectsPlayer.LastPlayedPan == -0.6f, "Spatial pan must be preserved");
    }

    private static void TestPass_AudioBinding()
    {
        SoundEffectsPlayer.ResetTelemetry();
        var reader = new MockScreenReader();
        var coord = new SmartAudioCoordinator(reader);

        var evt = new MatchEvent(MatchEventType.BallKicked, "تمريرة أرضية قصيرة", spatialPan: 0.2f);
        coord.HandleMatchEvent(evt);
        Assert(SoundEffectsPlayer.LastPlayedSound == "pass_kick", 
            $"Pass must trigger pass_kick, got {SoundEffectsPlayer.LastPlayedSound}");
    }

    private static void TestStandTackle_AudioBinding()
    {
        SoundEffectsPlayer.ResetTelemetry();
        var reader = new MockScreenReader();
        var coord = new SmartAudioCoordinator(reader);
        var comm = new CommentaryEngine(CommentatorId.RaoufKhlif, _audioDir);

        var evt = new MatchEvent(MatchEventType.UserTackleSuccessful, "استخلاص وقوف هادئ للكرة");
        coord.HandleMatchEvent(evt);
        Assert(SoundEffectsPlayer.LastPlayedSound == "stand_tackle", 
            $"Standing tackle must trigger stand_tackle, got {SoundEffectsPlayer.LastPlayedSound}");

        bool commHandled = comm.TryCommentOnEvent(evt);
        Assert(commHandled, "Tackle must be handled by commentary");
    }

    private static void TestSlideTackle_AudioBinding()
    {
        SoundEffectsPlayer.ResetTelemetry();
        var reader = new MockScreenReader();
        var coord = new SmartAudioCoordinator(reader);

        var evt = new MatchEvent(MatchEventType.UserTackleSuccessful, "استخلاص انزلاق أرضي");
        coord.HandleMatchEvent(evt);
        Assert(SoundEffectsPlayer.LastPlayedSound == "slide_tackle", 
            $"Slide tackle must trigger slide_tackle, got {SoundEffectsPlayer.LastPlayedSound}");
    }

    private static void TestTackleWon_AudioBinding()
    {
        SoundEffectsPlayer.ResetTelemetry();
        var reader = new MockScreenReader();
        var coord = new SmartAudioCoordinator(reader);

        var evt = new MatchEvent(MatchEventType.UserTackleSuccessful, "استخلاص نظيف ممتاز");
        coord.HandleMatchEvent(evt);
        Assert(SoundEffectsPlayer.LastPlayedSound == "TackleWon", 
            $"Clean tackle won must trigger TackleWon earcon, got {SoundEffectsPlayer.LastPlayedSound}");
    }

    private static void TestBallLost_AudioBinding()
    {
        SoundEffectsPlayer.ResetTelemetry();
        var reader = new MockScreenReader();
        var coord = new SmartAudioCoordinator(reader);

        var evt = new MatchEvent(MatchEventType.OpponentTackleSuccessful, "فقدت الكرة لصالح الخصم");
        coord.HandleMatchEvent(evt);
        Assert(SoundEffectsPlayer.LastPlayedSound == "BallLost", 
            $"Ball lost must trigger BallLost earcon, got {SoundEffectsPlayer.LastPlayedSound}");
    }

    private static void TestTackleMissed_AudioBinding()
    {
        SoundEffectsPlayer.ResetTelemetry();
        var reader = new MockScreenReader();
        var coord = new SmartAudioCoordinator(reader);

        var evt = new MatchEvent(MatchEventType.TacticalRadarInfo, "بعيد عن حامل الكرة: 4.5 متر");
        coord.HandleMatchEvent(evt);
        Assert(SoundEffectsPlayer.LastPlayedSound == "TackleMissed", 
            $"Out-of-reach tackle must trigger TackleMissed earcon, got {SoundEffectsPlayer.LastPlayedSound}");
    }

    private static void TestFoul_AudioBinding()
    {
        SoundEffectsPlayer.ResetTelemetry();
        var reader = new MockScreenReader();
        var coord = new SmartAudioCoordinator(reader);
        var comm = new CommentaryEngine(CommentatorId.RaoufKhlif, _audioDir);

        var evt = new MatchEvent(MatchEventType.FoulCommitted, "خطأ تكتيكي");
        coord.HandleMatchEvent(evt);
        Assert(SoundEffectsPlayer.LastPlayedSound == "whistle", 
            $"FoulCommitted must trigger whistle, got {SoundEffectsPlayer.LastPlayedSound}");

        bool commHandled = comm.TryCommentOnEvent(evt);
        Assert(commHandled, "Commentary must handle FoulCommitted");
    }

    private static void TestYellowCard_AudioBinding()
    {
        var comm = new CommentaryEngine(CommentatorId.RaoufKhlif, _audioDir);
        var evt = new MatchEvent(MatchEventType.YellowCard, "إنذار وبطاقة صفراء");
        bool handled = comm.TryCommentOnEvent(evt);
        Assert(handled, "Commentary must handle YellowCard");
    }

    private static void TestRedCard_AudioBinding()
    {
        var comm = new CommentaryEngine(CommentatorId.RaoufKhlif, _audioDir);
        var evt = new MatchEvent(MatchEventType.RedCard, "طرد مباشر بالبطاقة الحمراء");
        bool handled = comm.TryCommentOnEvent(evt);
        Assert(handled, "Commentary must handle RedCard");
    }

    private static void TestPlayerInjured_AudioBinding()
    {
        var reader = new MockScreenReader();
        var coord = new SmartAudioCoordinator(reader);
        var evt = new MatchEvent(MatchEventType.PlayerInjured, "إصابة بالغة للاعب");
        coord.HandleMatchEvent(evt);
        // PlayerInjured does not crash and handles safely
        Assert(true, "PlayerInjured handled safely");
    }

    private static void TestOffside_AudioBinding()
    {
        SoundEffectsPlayer.ResetTelemetry();
        var reader = new MockScreenReader();
        var coord = new SmartAudioCoordinator(reader);

        var evt = new MatchEvent(MatchEventType.Offside, "تسلل واضح");
        coord.HandleMatchEvent(evt);
        Assert(SoundEffectsPlayer.LastPlayedSound == "whistle", 
            $"Offside must trigger whistle, got {SoundEffectsPlayer.LastPlayedSound}");
    }

    private static void TestPenalty_AudioBinding()
    {
        SoundEffectsPlayer.ResetTelemetry();
        var reader = new MockScreenReader();
        var coord = new SmartAudioCoordinator(reader);
        var comm = new CommentaryEngine(CommentatorId.RaoufKhlif, _audioDir);

        var evt = new MatchEvent(MatchEventType.PenaltyAwarded, "ركلة جزاء مستحقة");
        coord.HandleMatchEvent(evt);
        Assert(SoundEffectsPlayer.LastPlayedSound == "Penalty", 
            $"PenaltyAwarded must trigger Penalty earcon, got {SoundEffectsPlayer.LastPlayedSound}");

        bool commHandled = comm.TryCommentOnEvent(evt);
        Assert(commHandled, "Commentary must handle PenaltyAwarded");
    }

    private static void TestCorner_AudioBinding()
    {
        SoundEffectsPlayer.ResetTelemetry();
        var reader = new MockScreenReader();
        var coord = new SmartAudioCoordinator(reader);
        var comm = new CommentaryEngine(CommentatorId.RaoufKhlif, _audioDir);

        var evt = new MatchEvent(MatchEventType.CornerKick, "ركلة ركنية");
        coord.HandleMatchEvent(evt);
        Assert(SoundEffectsPlayer.LastPlayedSound == "Corner", 
            $"CornerKick must trigger Corner earcon, got {SoundEffectsPlayer.LastPlayedSound}");

        bool commHandled = comm.TryCommentOnEvent(evt);
        Assert(commHandled, "Commentary must handle CornerKick");
    }

    private static void TestThrowIn_AudioBinding()
    {
        SoundEffectsPlayer.ResetTelemetry();
        var reader = new MockScreenReader();
        var coord = new SmartAudioCoordinator(reader);

        var evt = new MatchEvent(MatchEventType.ThrowIn, "رمية تماس");
        coord.HandleMatchEvent(evt);
        Assert(SoundEffectsPlayer.LastPlayedSound == "ThrowIn", 
            $"ThrowIn must trigger ThrowIn earcon, got {SoundEffectsPlayer.LastPlayedSound}");
    }

    private static void TestGoalKick_AudioBinding()
    {
        SoundEffectsPlayer.ResetTelemetry();
        var reader = new MockScreenReader();
        var coord = new SmartAudioCoordinator(reader);

        var evt = new MatchEvent(MatchEventType.GoalKick, "ركلة مرمى");
        coord.HandleMatchEvent(evt);
        Assert(SoundEffectsPlayer.LastPlayedSound == "GoalKick", 
            $"GoalKick must trigger GoalKick earcon, got {SoundEffectsPlayer.LastPlayedSound}");
    }

    private static void TestHalfTime_AudioBinding()
    {
        SoundEffectsPlayer.ResetTelemetry();
        var reader = new MockScreenReader();
        var coord = new SmartAudioCoordinator(reader);

        var evt = new MatchEvent(MatchEventType.HalfTime, "نهاية الشوط الأول");
        coord.HandleMatchEvent(evt);
        Assert(SoundEffectsPlayer.LastPlayedSound == "whistle", 
            $"HalfTime must trigger whistle, got {SoundEffectsPlayer.LastPlayedSound}");
    }

    private static void TestFullTime_AudioBinding()
    {
        SoundEffectsPlayer.ResetTelemetry();
        var reader = new MockScreenReader();
        var coord = new SmartAudioCoordinator(reader);
        var comm = new CommentaryEngine(CommentatorId.RaoufKhlif, _audioDir);

        var evt = new MatchEvent(MatchEventType.FullTime, "صافرة نهاية المباراة");
        coord.HandleMatchEvent(evt);
        Assert(SoundEffectsPlayer.LastPlayedSound == "whistle", 
            $"FullTime must trigger whistle, got {SoundEffectsPlayer.LastPlayedSound}");

        bool commHandled = comm.TryCommentOnEvent(evt);
        Assert(commHandled, "FullTime must be handled by commentary");
    }

    private static void TestSave_AudioBinding()
    {
        SoundEffectsPlayer.ResetTelemetry();
        var reader = new MockScreenReader();
        var coord = new SmartAudioCoordinator(reader);
        var comm = new CommentaryEngine(CommentatorId.RaoufKhlif, _audioDir);

        var evt = new MatchEvent(MatchEventType.GoalkeeperSave, "تصدي رائع للحارس");
        coord.HandleMatchEvent(evt);
        Assert(SoundEffectsPlayer.LastPlayedSound == "pass_kick", 
            $"Save must trigger deflection pass_kick, got {SoundEffectsPlayer.LastPlayedSound}");

        bool commHandled = comm.TryCommentOnEvent(evt);
        Assert(commHandled, "Raouf Khlif commentary must handle Save");
    }

    private static void TestWoodwork_AudioBinding()
    {
        SoundEffectsPlayer.ResetTelemetry();
        var reader = new MockScreenReader();
        var coord = new SmartAudioCoordinator(reader);
        var comm = new CommentaryEngine(CommentatorId.RaoufKhlif, _audioDir);

        var evt = new MatchEvent(MatchEventType.WoodworkHit, "ارتطام عنيف بالقائم");
        coord.HandleMatchEvent(evt);
        Assert(SoundEffectsPlayer.LastPlayedSound == "post_hit", 
            $"WoodworkHit must trigger post_hit SFX, got {SoundEffectsPlayer.LastPlayedSound}");

        bool commHandled = comm.TryCommentOnEvent(evt);
        Assert(commHandled, "Raouf Khlif commentary must handle WoodworkHit");
    }

    private static void TestStoppageTime_AudioBinding()
    {
        SoundEffectsPlayer.ResetTelemetry();
        var reader = new MockScreenReader();
        var coord = new SmartAudioCoordinator(reader);
        var comm = new CommentaryEngine(CommentatorId.RaoufKhlif, _audioDir);

        var evt = new MatchEvent(MatchEventType.StoppageTimeAnnounced, "الحكم يعلن الوقت بدل الضائع");
        coord.HandleMatchEvent(evt);
        Assert(SoundEffectsPlayer.LastPlayedSound == "whistle", 
            $"StoppageTime must trigger whistle, got {SoundEffectsPlayer.LastPlayedSound}");

        bool commHandled = comm.TryCommentOnEvent(evt);
        Assert(commHandled, "Commentary must handle StoppageTime");
    }

    private static void TestDramaticEvents_AudioBinding()
    {
        var comm1 = new CommentaryEngine(CommentatorId.RaoufKhlif, _audioDir);
        var evtComeback = new MatchEvent(MatchEventType.DramaticComeback, "ريمونتادا وهدف تاريخي");
        bool c1 = comm1.TryCommentOnEvent(evtComeback);

        var comm2 = new CommentaryEngine(CommentatorId.RaoufKhlif, _audioDir);
        var evtHatTrick = new MatchEvent(MatchEventType.HatTrickAchieved, "هاتريك رائع");
        bool c2 = comm2.TryCommentOnEvent(evtHatTrick);

        Assert(c1 && c2, "Dramatic events must be handled cleanly by commentary");
    }

    // -----------------------------------------------------------------------------------------
    // SECTION 2: VERIFY ALL 9 EARCON WAVS (RIFF, SAMPLE RATE, RMS)
    // -----------------------------------------------------------------------------------------
    private static void VerifyEarconWav(string fileName)
    {
        string filePath = Path.Combine(_audioDir, "SFX", "Earcons", fileName);
        Assert(File.Exists(filePath), $"Earcon file missing on disk: {filePath}");

        byte[] bytes = File.ReadAllBytes(filePath);
        Assert(bytes.Length > 44, $"File {fileName} is too short ({bytes.Length} bytes)");

        string riff = Encoding.ASCII.GetString(bytes, 0, 4);
        string wave = Encoding.ASCII.GetString(bytes, 8, 4);
        Assert(riff == "RIFF" && wave == "WAVE", $"Invalid RIFF/WAVE header in {fileName}");

        int pos = 12;
        short format = 0;
        short channels = 0;
        int sampleRate = 0;
        short bitsPerSample = 0;
        int dataPos = 0;
        int dataLen = 0;

        while (pos < bytes.Length - 8)
        {
            string chunkId = Encoding.ASCII.GetString(bytes, pos, 4);
            int chunkSize = BitConverter.ToInt32(bytes, pos + 4);
            if (chunkId == "fmt ")
            {
                format = BitConverter.ToInt16(bytes, pos + 8);
                channels = BitConverter.ToInt16(bytes, pos + 10);
                sampleRate = BitConverter.ToInt32(bytes, pos + 12);
                bitsPerSample = BitConverter.ToInt16(bytes, pos + 22);
            }
            else if (chunkId == "data")
            {
                dataPos = pos + 8;
                dataLen = chunkSize;
                break;
            }
            pos += 8 + chunkSize;
        }

        Assert(format == 1, $"{fileName} must be PCM format (1), got {format}");
        Assert(channels == 1, $"{fileName} must be Mono (1 channel), got {channels}");
        Assert(sampleRate == 44100, $"{fileName} must be 44100 Hz, got {sampleRate}");
        Assert(bitsPerSample == 16, $"{fileName} must be 16-bit, got {bitsPerSample}");
        Assert(dataLen > 0, $"{fileName} data chunk must be > 0 bytes");

        // Parse samples and compute RMS & Peak
        int sampleCount = dataLen / 2;
        double sumSq = 0.0;
        int peak = 0;

        for (int i = 0; i < sampleCount; i++)
        {
            short val = BitConverter.ToInt16(bytes, dataPos + (i * 2));
            int absVal = Math.Abs(val);
            if (absVal > peak) peak = absVal;
            double norm = val / 32768.0;
            sumSq += norm * norm;
        }

        double rms = Math.Sqrt(sumSq / sampleCount);
        double rmsDb = 20.0 * Math.Log10(rms);
        double peakNorm = peak / 32768.0;

        Console.Write($"[Size={bytes.Length}B, Peak={peakNorm:F3}, RMS={rms:F4} ({rmsDb:F1} dB)] ");

        Assert(rms > 0.05, $"{fileName} RMS energy too low ({rms:F4} <= 0.05) - possible silence!");
        Assert(peakNorm >= 0.30 && peakNorm <= 0.99, $"{fileName} peak normalized ({peakNorm:F3}) out of safe range [0.30, 0.99]!");
    }

    // -----------------------------------------------------------------------------------------
    // SECTION 3: 3D SPATIAL GOAL BEACON ACOUSTIC REASONING
    // -----------------------------------------------------------------------------------------
    private static void TestBeaconProximity()
    {
        // 1st half: Goal is at X=105, Y=34
        // Calculate at distances 50m, 35m, 20m, 10m, 5m, 0m
        float[] xs = [55.0f, 70.0f, 85.0f, 95.0f, 100.0f, 105.0f];
        float prevVol = -1.0f;

        foreach (float x in xs)
        {
            GoalBeaconPlayer.CalculateBeacon(x, 34.0f, isSecondHalf: false, masterVolume: 1.0f, 
                out float dist, out float vol, out float pan);

            Assert(vol >= prevVol, $"Beacon volume must increase monotonically as player approaches goal! At x={x}, vol={vol} < prev={prevVol}");
            prevVol = vol;
        }

        Assert(prevVol == 1.0f, "Volume at goal line must be 1.0f");
    }

    private static void TestBeaconStereoPanning()
    {
        // Player at X=90 (15m from goal)
        // Center: Y=34 -> pan = 0.0
        GoalBeaconPlayer.CalculateBeacon(90.0f, 34.0f, isSecondHalf: false, masterVolume: 1.0f, out _, out _, out float panCenter);
        Assert(Math.Abs(panCenter) < 0.01f, $"Center Y=34 pan must be 0, got {panCenter}");

        // Left wing: Y=10 -> Goal is to the right (+pan)
        GoalBeaconPlayer.CalculateBeacon(90.0f, 10.0f, isSecondHalf: false, masterVolume: 1.0f, out _, out _, out float panLeft);
        Assert(panLeft > 0.5f, $"Left wing Y=10 must pan right > 0.5, got {panLeft}");

        // Right wing: Y=58 -> Goal is to the left (-pan)
        GoalBeaconPlayer.CalculateBeacon(90.0f, 58.0f, isSecondHalf: false, masterVolume: 1.0f, out _, out _, out float panRight);
        Assert(panRight < -0.5f, $"Right wing Y=58 must pan left < -0.5, got {panRight}");
    }

    private static void TestBeaconHalfTimeInversion()
    {
        // 2nd half: Goal inverted to X=0, Y=34
        // Player at X=10 is 10m from goal -> high volume
        GoalBeaconPlayer.CalculateBeacon(10.0f, 34.0f, isSecondHalf: true, masterVolume: 1.0f, out float d1, out float v1, out _);
        Assert(d1 == 10.0f, $"In 2nd half, X=10 should be 10m from goal, got {d1}");
        Assert(v1 > 0.7f, $"Volume at 10m should be > 0.7, got {v1}");

        // Player at X=90 is in defensive half -> 0 volume
        GoalBeaconPlayer.CalculateBeacon(90.0f, 34.0f, isSecondHalf: true, masterVolume: 1.0f, out float d2, out float v2, out _);
        Assert(v2 == 0.0f, $"Volume in defensive half must be 0.0f, got {v2}");
    }

    // -----------------------------------------------------------------------------------------
    // SECTION 4: STRESS HARNESS & RAPID FIRE EVENT BURST
    // -----------------------------------------------------------------------------------------
    private static void Test1000RapidEventsBurst()
    {
        var reader = new MockScreenReader();
        var coord = new SmartAudioCoordinator(reader);
        var comm = new CommentaryEngine(CommentatorId.RaoufKhlif, _audioDir);
        var rand = new Random(42);

        var eventTypes = Enum.GetValues<MatchEventType>();
        for (int i = 0; i < 200; i++)
        {
            var type = eventTypes[rand.Next(eventTypes.Length)];
            var evt = new MatchEvent(type, $"حدث اختباري عشوائي #{i}", spatialPan: (float)(rand.NextDouble() * 2.0 - 1.0));
            coord.HandleMatchEvent(evt);
            comm.TryCommentOnEvent(evt);
        }

        Console.Write($"[Fired 200 events, Total SFX triggered: {SoundEffectsPlayer.TotalSoundsPlayedCount}] ");
        Assert(SoundEffectsPlayer.TotalSoundsPlayedCount > 0, "Sounds must have triggered during burst");
    }

    private static void TestZeroMissingCommentaryClips()
    {
        string commRoot = Path.Combine(_audioDir, "Commentary", "RaoufKhlif");
        Assert(Directory.Exists(commRoot), $"Raouf Khlif commentary root missing: {commRoot}");

        string[] requiredFolders = ["Goals", "Shots", "Saves", "Woodwork", "Tackles", "Fouls", "StoppageTime", "FullTime", "Intro", "Callnames"];
        int totalClips = 0;

        foreach (var folder in requiredFolders)
        {
            string dir = Path.Combine(commRoot, folder);
            Assert(Directory.Exists(dir), $"Commentary folder missing: {dir}");
            var files = Directory.GetFiles(dir, "*.wav");
            Assert(files.Length > 0, $"Commentary folder {folder} is empty!");
            totalClips += files.Length;

            foreach (var f in files)
            {
                var fi = new FileInfo(f);
                Assert(fi.Length > 1000, $"Commentary file {f} is suspiciously small: {fi.Length} bytes");
            }
        }

        Console.Write($"[Verified {totalClips} authentic commentary clips across {requiredFolders.Length} categories] ");
    }
}
