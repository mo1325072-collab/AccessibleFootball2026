using System;
using System.IO;
using System.Linq;
using System.Numerics;
using AccessibleFootball2026.Audio;
using AccessibleFootball2026.Audio.Commentary;
using AccessibleFootball2026.Audio.SFX;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;
using AccessibleFootball2026.Input;

namespace AccessibleFootball2026.Tests;

/// <summary>
/// اختبارات انحدار لإصلاحات ربط التعليق والمؤثرات الصوتية بأحداث المباراة (Tests 22A through 22G):
/// 22A/22B: أصوات الركلات والصافرة الحقيقية المستخرجة من PES 2013 لها أولوية على التوليد الإجرائي
/// 22C: الحدث يحمل اللاعب صاحب اللمسة الفعلية (SubjectPlayer) لربط نداء الاسم الصحيح
/// 22D: سيطرة الخصم على كرة سائبة تنشر BallTrapped وليس BallKicked (لا صوت ركلة وهمي)
/// 22E: كبح تكرار الأهازيج داخل PlayTeamChant ضد الاستدعاء كل إطار (50Hz)
/// 22F: نداء الاسم لا يقطع التعليق الجاري ويعمل عندما تكون القناة حرة
/// 22G: نافذة انشغال قناة المعلق تقرأ المدة الحقيقية لمقطع WAV
/// </summary>
public static class Milestone8AudioIntegrationTests
{
    public static void RunAll(Action<string, Action> runTest)
    {
        try
        {
            RunAllCore(runTest);
        }
        finally
        {
            // إعادة الحالة العامة للمشغل لوضعها الطبيعي حتى لا ترث الأجنحة اللاحقة قناة مشغولة أو مجلداً مؤقتاً
            SoundEffectsPlayer.ResetTelemetry();
            SoundEffectsPlayer.Initialize(Path.Combine(FindWorkspaceRoot(), "Assets", "Audio"));
        }
    }

    private static void RunAllCore(Action<string, Action> runTest)
    {
        runTest("22A. Extracted PES Kick Sample Wins Over Procedural Placeholder (Pass SFX)", Test22A_ExtractedKickPreferredForPass);
        runTest("22B. Extracted PES Whistle Sample Wins Over Procedural Placeholder", Test22B_ExtractedWhistlePreferred);
        runTest("22C. Kick Events Carry SubjectPlayer (User Shot + Pass)", Test22C_KickEventsCarrySubjectPlayer);
        runTest("22D. Opponent Loose-Ball Pickup Publishes BallTrapped, Not BallKicked", Test22D_OpponentPickupIsBallTrapped);
        runTest("22E. Team Chant Enforces Internal Replay Throttle (50Hz Context Calls Safe)", Test22E_ChantReplayThrottle);
        runTest("22F. Callname Never Interrupts Live Commentary, Plays When Channel Free", Test22F_CallnameYieldsToCommentary);
        runTest("22G. Commentary Busy Window Reads Real WAV Duration", Test22G_WavDurationParsing);
    }

    private static void Test22A_ExtractedKickPreferredForPass()
    {
        string tempAudio = NewTempAudioDir();
        string kicksDir = Path.Combine(tempAudio, "SFX", "Kicks");
        Directory.CreateDirectory(kicksDir);
        WriteTinyWav(Path.Combine(kicksDir, "Kick_101.wav"), 0.15);

        // Initialize تولد العينات الإجرائية (Pass_Center وغيرها) في نفس المجلد - السيناريو الفعلي على جهاز المستخدم
        SoundEffectsPlayer.Initialize(tempAudio);
        Assert(File.Exists(Path.Combine(kicksDir, "Pass_Center.wav")), "Procedural placeholder must exist for this test to prove precedence");

        SoundEffectsPlayer.ResetTelemetry();
        SoundEffectsPlayer.PlayPassKick(0.0f);

        Assert(SoundEffectsPlayer.LastPlayedSound == "pass_kick",
            $"Pass SFX alias must stay 'pass_kick', got '{SoundEffectsPlayer.LastPlayedSound}'");
        Assert(SoundEffectsPlayer.LastPlayedFilePath != null
               && Path.GetFileName(SoundEffectsPlayer.LastPlayedFilePath).Equals("Kick_101.wav", StringComparison.OrdinalIgnoreCase),
            $"Real extracted PES kick must be played instead of the procedural placeholder, got '{SoundEffectsPlayer.LastPlayedFilePath}'");

        Cleanup(tempAudio);
    }

    private static void Test22B_ExtractedWhistlePreferred()
    {
        string tempAudio = NewTempAudioDir();
        string whistlesDir = Path.Combine(tempAudio, "SFX", "Whistles");
        Directory.CreateDirectory(whistlesDir);
        WriteTinyWav(Path.Combine(whistlesDir, "Whistle_55.wav"), 0.4);

        SoundEffectsPlayer.Initialize(tempAudio);
        Assert(File.Exists(Path.Combine(whistlesDir, "Whistle.wav")), "Procedural whistle placeholder must exist for this test to prove precedence");

        SoundEffectsPlayer.ResetTelemetry();
        SoundEffectsPlayer.PlayWhistle();

        Assert(SoundEffectsPlayer.LastPlayedSound == "whistle",
            $"Whistle alias must stay 'whistle', got '{SoundEffectsPlayer.LastPlayedSound}'");
        Assert(SoundEffectsPlayer.LastPlayedFilePath != null
               && Path.GetFileName(SoundEffectsPlayer.LastPlayedFilePath).Equals("Whistle_55.wav", StringComparison.OrdinalIgnoreCase),
            $"Real extracted PES whistle must be played instead of the procedural placeholder, got '{SoundEffectsPlayer.LastPlayedFilePath}'");

        Cleanup(tempAudio);
    }

    private static void Test22C_KickEventsCarrySubjectPlayer()
    {
        var eventBus = new MatchEventBus();
        MatchEvent? shotEvent = null;
        MatchEvent? passEvent = null;
        eventBus.OnMatchEvent += e =>
        {
            if (e.Type == MatchEventType.ShotTaken) shotEvent = e;
            if (e.Type == MatchEventType.BallKicked) passEvent = e;
        };

        var match = new MatchEngine(TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek(), eventBus, new KeyboardInputHandler());

        match.ExecuteShot();
        Assert(shotEvent != null, "ExecuteShot must publish ShotTaken");
        Assert(ReferenceEquals(shotEvent!.SubjectPlayer, match.ActiveUserPlayer),
            "ShotTaken must carry the shooting player as SubjectPlayer so the callname names the actual kicker");

        match.ExecutePass(new Vector2(1.0f, 0.0f), isThroughBall: false);
        Assert(passEvent != null, "ExecutePass must publish BallKicked");
        Assert(ReferenceEquals(passEvent!.SubjectPlayer, match.ActiveUserPlayer),
            "Pass BallKicked must carry the passer as SubjectPlayer");
    }

    private static void Test22D_OpponentPickupIsBallTrapped()
    {
        // عقدة ثابتة بأسلوب الريبو: سيطرة الخصم على كرة سائبة ليست ركلة،
        // ونشرها كـ BallKicked كان يشغّل صوت ركلة وهمياً ونداء اسم بلا لمسة فعلية
        string root = FindWorkspaceRoot();
        string engineCode = File.ReadAllText(Path.Combine(root, "AccessibleFootball2026.Core", "Simulation", "MatchEngine.cs"));

        int idx = engineCode.IndexOf("الكرة مع {closestPlayer.Name}", StringComparison.Ordinal);
        Assert(idx >= 0, "Opponent loose-ball pickup announcement must exist in MatchEngine");

        string segment = engineCode.Substring(Math.Max(0, idx - 400), Math.Min(400, idx));
        Assert(segment.Contains("MatchEventType.BallTrapped"),
            "Opponent loose-ball pickup must publish BallTrapped (possession), not BallKicked (kick)");
        Assert(!segment.Contains("MatchEventType.BallKicked"),
            "Opponent loose-ball pickup must NOT publish BallKicked - a phantom kick sound plays for a non-kick");
    }

    private static void Test22E_ChantReplayThrottle()
    {
        // عقدة ثابتة: الحلقة الرئيسية تستدعي HandleMatchContext كل إطار؛ يجب أن يكبح PlayTeamChant نفسه التكرار
        string root = FindWorkspaceRoot();
        string engineCode = File.ReadAllText(Path.Combine(root, "AccessibleFootball2026.Audio", "SFX", "StadiumSoundEngine.cs"));

        int sig = engineCode.IndexOf("public void PlayTeamChant", StringComparison.Ordinal);
        Assert(sig >= 0, "PlayTeamChant must exist");
        Assert(engineCode.IndexOf("bool force = false", sig, StringComparison.Ordinal) - sig < 120,
            "PlayTeamChant must expose a force parameter so decisive real events bypass the throttle");

        string bodyStart = engineCode.Substring(sig, Math.Min(900, engineCode.Length - sig));
        Assert(bodyStart.Contains("_lastChantPlayTime < _minChantInterval"),
            "PlayTeamChant must enforce _minChantInterval internally - per-frame context calls must not restart the chant channel 50x/sec");

        // سلوكياً: استدعاءات متتالية سريعة بدون ملفات أصلية لا ترمي استثناءات
        var engine = new StadiumSoundEngine(Path.Combine(Path.GetTempPath(), "af2026_no_chants_" + Guid.NewGuid().ToString("N")));
        var team = TeamDatabase.CreateRealMadrid();
        engine.SetMatchTeams(team, TeamDatabase.CreateBarcelona());
        for (int i = 0; i < 100; i++)
        {
            engine.PlayTeamChant(team, ChantSlot.AttackingMomentum);
        }
        Assert(true, "100 rapid chant calls without assets must stay exception-free");
    }

    private static void Test22F_CallnameYieldsToCommentary()
    {
        string tempAudio = NewTempAudioDir();
        string callnamesDir = Path.Combine(tempAudio, "Callnames");
        Directory.CreateDirectory(callnamesDir);
        WriteTinyWav(Path.Combine(callnamesDir, "Callname_1.wav"), 0.5);
        File.WriteAllText(Path.Combine(callnamesDir, "callnames_index.csv"), "ليونيل ميسي,Callname_1.wav,10\n");

        string voiceClip = Path.Combine(tempAudio, "voice.wav");
        WriteTinyWav(voiceClip, 2.0);

        var manager = new CallnameManager(tempAudio);
        var messi = new Player { Id = 10, Name = "ليونيل ميسي" };

        SoundEffectsPlayer.ResetTelemetry();
        SoundEffectsPlayer.PlayCommentaryAudio(voiceClip);
        Assert(SoundEffectsPlayer.IsCommentaryPlaying(), "Commentary channel must report busy right after a commentary clip starts");

        Assert(!manager.TryPlayPlayerCallname(messi),
            "Callname must NOT interrupt or shadow a live commentary line");

        SoundEffectsPlayer.ResetTelemetry(); // يحرر نافذة الانشغال كأن المقطع انتهى
        Assert(manager.TryPlayPlayerCallname(messi),
            "Callname must play normally once the commentary channel is free");

        Cleanup(tempAudio);
    }

    private static void Test22G_WavDurationParsing()
    {
        string tempAudio = NewTempAudioDir();
        string shortClip = Path.Combine(tempAudio, "short.wav");
        string longClip = Path.Combine(tempAudio, "long.wav");
        WriteTinyWav(shortClip, 1.0);
        WriteTinyWav(longClip, 4.0);

        double shortSecs = SoundEffectsPlayer.GetWavDurationSeconds(shortClip);
        double longSecs = SoundEffectsPlayer.GetWavDurationSeconds(longClip);

        Assert(Math.Abs(shortSecs - 1.0) < 0.05, $"1s WAV must parse as ~1s, got {shortSecs:F3}s");
        Assert(Math.Abs(longSecs - 4.0) < 0.05, $"4s WAV must parse as ~4s, got {longSecs:F3}s");
        Assert(SoundEffectsPlayer.GetWavDurationSeconds(Path.Combine(tempAudio, "missing.wav")) == 0,
            "Missing file must parse as 0 seconds (fallback window applies)");

        Cleanup(tempAudio);
    }

    // ------------------------------------------------------------------
    private static string NewTempAudioDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "af2026_audio_int_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void Cleanup(string dir)
    {
        try { Directory.Delete(dir, recursive: true); } catch { }
    }

    private static void WriteTinyWav(string path, double seconds)
    {
        int sampleRate = 8000;
        int frames = Math.Max(1, (int)(sampleRate * seconds));
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var bw = new BinaryWriter(fs);
        int dataSize = frames * 2;
        bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + dataSize);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16);
        bw.Write((short)1);
        bw.Write((short)1);
        bw.Write(sampleRate);
        bw.Write(sampleRate * 2);
        bw.Write((short)2);
        bw.Write((short)16);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        bw.Write(dataSize);
        for (int i = 0; i < frames; i++)
        {
            bw.Write((short)(Math.Sin(2.0 * Math.PI * 440.0 * i / sampleRate) * 8000));
        }
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
        throw new DirectoryNotFoundException("Could not find AccessibleFootball2026.sln in parent directory tree.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception($"Assertion Failed: {message}");
        }
    }
}
