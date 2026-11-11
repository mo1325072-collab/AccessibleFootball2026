using System;
using System.IO;
using System.Linq;
using System.Threading;
using AccessibleFootball2026.Audio;
using AccessibleFootball2026.Audio.Commentary;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Tests;

/// <summary>
/// اختبارات انحدار لإصلاح تنسيق تشغيل التعليق (تداخل الجمل) مع الحفاظ على سياسة
/// اختيار المقاطع القائمة كما هي (Tests 24A through 24H):
/// 24A: الأحداث الروتينية تبقى تُشغِّل مقاطع مجلدها المسجل كما قبل (لا تغيير في الاختيار)
/// 24B: الهدف يُشغِّل مقطعاً مسجلاً من مجلد الأهداف عند توفره
/// 24C: الهدف والهاتريك في نفس اللحظة يُسلسلان ولا يقطع أحدهما الآخر
/// 24D: جملة نهاية المباراة النصية تنتظر انتهاء مقطع الاحتفال ولا تتداخل معه
/// 24E: الأحداث غير الحاسمة تُسقط أثناء انشغال قناة التعليق
/// 24F: commentary_map.json الاختياري يقدَّم على مجلد الحدث عند وجوده فقط
/// 24G: الإعلان الرسمي للهدف ينتظر صمت المعلق التام ولا يتكلم فوقه
/// 24H: مقدمة المباراة تُشغِّل مقطعها المسجل كما قبل وتسقط نصياً عند غيابه
/// </summary>
public static class Milestone10CommentaryRepairTests
{
    public static void RunAll(Action<string, Action> runTest)
    {
        try
        {
            runTest("24A. Routine Event Still Plays Its Folder Clip (Selection Unchanged)", Test24A_RoutineEventFolderClipPreserved);
            runTest("24B. Goal Plays Recorded Celebration Clip When Present", Test24B_GoalPlaysRecordedClip);
            runTest("24C. Goal + HatTrick Same Tick Are Serialized, No Cutoff", Test24C_CriticalChainSerialized);
            runTest("24D. FullTime TTS Line Waits For Celebration Clip, No Overlap", Test24D_StoppageLineSerializedBehindClip);
            runTest("24E. Routine Event Dropped While Commentary Busy", Test24E_NonCriticalDroppedWhileBusy);
            runTest("24F. Optional commentary_map.json Pin Takes Precedence Only When Present", Test24F_ManifestPinPrecedence);
            runTest("24G. Official Goal Announcement Waits For Full Commentary Silence", Test24G_GoalAnnouncementWaitsForIdle);
            runTest("24H. Match Intro Plays Recorded Clip As Before, TTS When Absent", Test24H_IntroBehaviorPreserved);
            runTest("24I. Team Introduction Waits For Recorded Intro", Test24I_IntroAnnouncementWaits);
            runTest("24J. Returning To Menu Cancels Pending Goal Announcement", Test24J_GoalAnnouncementCancellation);
        }
        finally
        {
            SoundEffectsPlayer.ResetTelemetry();
        }
    }

    private static void Test24A_RoutineEventFolderClipPreserved()
    {
        string tempAudio = NewTempAudioDir();
        string shotsDir = Path.Combine(tempAudio, "Commentary", "RaoufKhlif", "Shots");
        Directory.CreateDirectory(shotsDir);
        string clip = Path.Combine(shotsDir, "Shot_1.wav");
        WriteTinyWav(clip, 2.0);

        SoundEffectsPlayer.ResetTelemetry();
        using var engine = new CommentaryEngine(CommentatorId.RaoufKhlif, tempAudio);

        bool speechTriggered = false;
        engine.OnCommentarySpoken += (name, phrase) => speechTriggered = true;

        bool handled = engine.TryCommentOnEvent(new MatchEvent(MatchEventType.ShotTaken, "تسديدة"), force: true);

        Assert(handled, "ShotTaken must be handled");
        Assert(WaitUntil(() => SoundEffectsPlayer.LastPlayedFilePath == clip, 3000),
            "ShotTaken must still play the recorded clip from its event folder (selection behavior unchanged)");
        Assert(!speechTriggered, "Recorded clip must keep precedence over TTS for routine events");

        Cleanup(tempAudio);
    }

    private static void Test24B_GoalPlaysRecordedClip()
    {
        string tempAudio = NewTempAudioDir();
        string goalsDir = Path.Combine(tempAudio, "Commentary", "RaoufKhlif", "Goals");
        Directory.CreateDirectory(goalsDir);
        string clip = Path.Combine(goalsDir, "Goal_1.wav");
        WriteTinyWav(clip, 2.0);

        SoundEffectsPlayer.ResetTelemetry();
        using var engine = new CommentaryEngine(CommentatorId.RaoufKhlif, tempAudio);

        bool speechTriggered = false;
        engine.OnCommentarySpoken += (name, phrase) => speechTriggered = true;

        var goal = new MatchEvent(MatchEventType.GoalScored, "هدف", PitchZone.AttackingThird,
            new GoalDetails(new Player { Name = "محمد صلاح" }, null, false, 12, 1, 0, TeamDatabase.CreateLiverpool()));
        bool handled = engine.TryCommentOnEvent(goal);

        Assert(handled && goal.HandledByCommentary, "GoalScored must be handled by commentary");
        Assert(WaitUntil(() => SoundEffectsPlayer.LastPlayedFilePath == clip, 3000),
            "Goal must play the recorded celebration clip from the Goals folder");
        Assert(!speechTriggered, "Goal must not fall back to TTS when a celebration clip exists");
        Assert(SoundEffectsPlayer.IsCommentaryPlaying(), "Commentary channel must report busy while the goal clip plays");

        Cleanup(tempAudio);
    }

    private static void Test24C_CriticalChainSerialized()
    {
        string tempAudio = NewTempAudioDir();
        string goalsDir = Path.Combine(tempAudio, "Commentary", "RaoufKhlif", "Goals");
        Directory.CreateDirectory(goalsDir);
        WriteTinyWav(Path.Combine(goalsDir, "Goal_1.wav"), 2.0);
        WriteTinyWav(Path.Combine(goalsDir, "Goal_2.wav"), 2.0);

        SoundEffectsPlayer.ResetTelemetry();
        using var engine = new CommentaryEngine(CommentatorId.RaoufKhlif, tempAudio);

        var goal = new MatchEvent(MatchEventType.GoalScored, "هدف", PitchZone.AttackingThird,
            new GoalDetails(new Player { Name = "محمد صلاح" }, null, false, 60, 3, 0, TeamDatabase.CreateLiverpool()));
        var hatTrick = new MatchEvent(MatchEventType.HatTrickAchieved, "هاتريك", PitchZone.AttackingThird, goal.GoalData);

        Assert(engine.TryCommentOnEvent(goal), "Goal must be accepted");
        Assert(engine.TryCommentOnEvent(hatTrick), "Hat-trick must be accepted (queued, not dropped)");

        Thread.Sleep(400); // أقل من مدة المقطع الأول: الثاني يجب أن ينتظر ولا يقطع الأول
        Assert(SoundEffectsPlayer.TotalSoundsPlayedCount == 1,
            $"Second critical clip must NOT cut the first one off; played={SoundEffectsPlayer.TotalSoundsPlayedCount}");

        Assert(WaitUntil(() => SoundEffectsPlayer.TotalSoundsPlayedCount >= 2, 6000),
            "Queued hat-trick clip must play after the goal clip finishes (serialized)");

        Cleanup(tempAudio);
    }

    private static void Test24D_StoppageLineSerializedBehindClip()
    {
        string tempAudio = NewTempAudioDir();
        string goalsDir = Path.Combine(tempAudio, "Commentary", "RaoufKhlif", "Goals");
        Directory.CreateDirectory(goalsDir);
        WriteTinyWav(Path.Combine(goalsDir, "Goal_1.wav"), 2.0);
        // مجلد FullTime غائب عمداً: يُستخدم المسار النصي البديل

        SoundEffectsPlayer.ResetTelemetry();
        using var engine = new CommentaryEngine(CommentatorId.RaoufKhlif, tempAudio);

        string? spokenPhrase = null;
        engine.OnCommentarySpoken += (name, phrase) => spokenPhrase = phrase;

        engine.TryCommentOnEvent(new MatchEvent(MatchEventType.GoalScored, "هدف", PitchZone.AttackingThird,
            new GoalDetails(new Player { Name = "محمد صلاح" }, null, false, 90, 1, 0, TeamDatabase.CreateLiverpool())));
        Assert(SoundEffectsPlayer.IsCommentaryPlaying(), "Goal clip must be playing");

        bool ftHandled = engine.TryCommentOnEvent(new MatchEvent(MatchEventType.FullTime, "صافرة النهاية"), force: true);
        Assert(ftHandled, "FullTime must be handled");

        Thread.Sleep(400);
        Assert(spokenPhrase == null, "FullTime line must NOT speak over the celebration clip (no overlap)");

        Assert(WaitUntil(() => spokenPhrase != null, 6000),
            "FullTime line must speak after the celebration clip ends");

        Cleanup(tempAudio);
    }

    private static void Test24E_NonCriticalDroppedWhileBusy()
    {
        string tempAudio = NewTempAudioDir();
        string goalsDir = Path.Combine(tempAudio, "Commentary", "RaoufKhlif", "Goals");
        Directory.CreateDirectory(goalsDir);
        WriteTinyWav(Path.Combine(goalsDir, "Goal_1.wav"), 2.0);

        SoundEffectsPlayer.ResetTelemetry();
        using var engine = new CommentaryEngine(CommentatorId.RaoufKhlif, tempAudio);

        engine.TryCommentOnEvent(new MatchEvent(MatchEventType.GoalScored, "هدف", PitchZone.AttackingThird,
            new GoalDetails(new Player { Name = "محمد صلاح" }, null, false, 12, 1, 0, TeamDatabase.CreateLiverpool())));

        bool routine = engine.TryCommentOnEvent(new MatchEvent(MatchEventType.ShotTaken, "تسديدة"));
        Assert(!routine, "Routine ShotTaken must be dropped while the commentary channel is busy");

        Cleanup(tempAudio);
    }

    private static void Test24F_ManifestPinPrecedence()
    {
        string tempAudio = NewTempAudioDir();
        string commDir = Path.Combine(tempAudio, "Commentary", "RaoufKhlif");
        string shotsDir = Path.Combine(commDir, "Shots");
        Directory.CreateDirectory(shotsDir);
        string folderClip = Path.Combine(shotsDir, "Shot_1.wav");
        string pinned = Path.Combine(shotsDir, "pinned.wav");
        WriteTinyWav(folderClip, 1.0);
        WriteTinyWav(pinned, 1.0);
        File.WriteAllText(Path.Combine(commDir, "commentary_map.json"), "{ \"Shot\": [\"Shots/pinned.wav\"] }");

        SoundEffectsPlayer.ResetTelemetry();
        using var engine = new CommentaryEngine(CommentatorId.RaoufKhlif, tempAudio);

        bool handled = engine.TryCommentOnEvent(new MatchEvent(MatchEventType.ShotTaken, "تسديدة"), force: true);

        Assert(handled, "ShotTaken with pin must be handled");
        Assert(WaitUntil(() => SoundEffectsPlayer.LastPlayedFilePath == pinned, 3000),
            "Explicit pin must take precedence over the event folder when commentary_map.json exists");

        Cleanup(tempAudio);
    }

    private static void Test24G_GoalAnnouncementWaitsForIdle()
    {
        var mock = new MockScreenReader();
        bool commentaryIdle = false;
        var coordinator = new SmartAudioCoordinator(mock, () => commentaryIdle);

        var goalData = new GoalDetails(new Player { Name = "محمد صلاح" }, null, false, 12, 1, 0, TeamDatabase.CreateLiverpool());
        var evt = new MatchEvent(MatchEventType.GoalScored, "هدف", PitchZone.AttackingThird, goalData);
        evt.HandledByCommentary = true;

        coordinator.HandleMatchEvent(evt);

        Thread.Sleep(1600); // التأخير الابتدائي 600ms + دورات انتظار: لا إعلان والمعلق لم يصمت بعد
        Assert(mock.SpokenTexts.Count == 0,
            $"Official announcement must NOT speak over live commentary; spoken={mock.SpokenTexts.Count}");

        commentaryIdle = true;
        Assert(WaitUntil(() => mock.SpokenTexts.Count == 1, 4000),
            "Official announcement must speak once the commentator is fully silent");
        Assert(mock.SpokenTexts[0].Contains("محمد صلاح"), "Announcement must name the scorer");
    }

    private static void Test24H_IntroBehaviorPreserved()
    {
        string tempAudio = NewTempAudioDir();
        string introDir = Path.Combine(tempAudio, "Commentary", "RaoufKhlif", "Intros");
        Directory.CreateDirectory(introDir);
        WriteTinyWav(Path.Combine(introDir, "Intro_1.wav"), 2.0);

        SoundEffectsPlayer.ResetTelemetry();
        using (var engine = new CommentaryEngine(CommentatorId.RaoufKhlif, tempAudio))
        {
            Assert(engine.TryPlayMatchIntro(), "Intro must play its recorded clip as before when present");
            Assert(WaitUntil(() => SoundEffectsPlayer.LastPlayedFilePath != null, 3000),
                "Recorded intro clip must actually start");
        }

        string emptyAudio = NewTempAudioDir();
        SoundEffectsPlayer.ResetTelemetry();
        using (var engine = new CommentaryEngine(CommentatorId.RaoufKhlif, emptyAudio))
        {
            Assert(!engine.TryPlayMatchIntro(), "Intro must report false when no recorded clip exists (TTS fallback path)");
        }

        Cleanup(tempAudio);
        Cleanup(emptyAudio);
    }

    private static void Test24I_IntroAnnouncementWaits()
    {
        string root = NewTempAudioDir();
        string dir = Path.Combine(root, "Commentary", "RaoufKhlif", "Intros");
        Directory.CreateDirectory(dir);
        WriteTinyWav(Path.Combine(dir, "Intro.wav"), 1.0);
        SoundEffectsPlayer.ResetTelemetry();
        using var engine = new CommentaryEngine(CommentatorId.RaoufKhlif, root);
        int calls = 0;
        engine.TryPlayMatchIntro();
        engine.RunAfterRecordedCommentary(() => Interlocked.Increment(ref calls));
        Thread.Sleep(300);
        Assert(calls == 0, "Team names must not speak over the recorded intro");
        Assert(WaitUntil(() => calls == 1, 3000), "Team introduction must follow recorded intro");
        Cleanup(root);
    }

    private static void Test24J_GoalAnnouncementCancellation()
    {
        var reader = new MockScreenReader();
        bool idle = false;
        var coordinator = new SmartAudioCoordinator(reader, () => idle);
        var goal = new MatchEvent(MatchEventType.GoalScored, "هدف", PitchZone.AttackingThird,
            new GoalDetails(new Player { Name = "محمد صلاح" }, null, false, 12, 1, 0, TeamDatabase.CreateLiverpool()));
        goal.HandledByCommentary = true;
        coordinator.HandleMatchEvent(goal);
        coordinator.CancelPendingGoalSequence();
        idle = true;
        Thread.Sleep(900);
        Assert(reader.SpokenTexts.Count == 0, "Cancelled match must not announce a stale goal in menus");
    }

    // ------------------------------------------------------------------
    private static bool WaitUntil(Func<bool> condition, int timeoutMs)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (condition()) return true;
            Thread.Sleep(50);
        }
        return condition();
    }

    private static string NewTempAudioDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "af2026_comm_rep_" + Guid.NewGuid().ToString("N"));
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

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException("ASSERT FAILED: " + message);
        }
    }

    private class MockScreenReader : IScreenReader
    {
        public bool IsAvailable => true;
        public System.Collections.Generic.List<string> SpokenTexts { get; } = new();
        public void Speak(string text, bool interrupt = true)
        {
            lock (SpokenTexts) SpokenTexts.Add(text);
        }
        public void CancelSpeech() { }
    }
}
