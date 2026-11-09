using System;
using System.IO;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Audio.SFX;

public enum ChantSlot
{
    GeneralRhythm = 1,       // SP_C*_01: أهازيج مستمرة
    AttackingMomentum = 2,   // SP_C*_02: هجمة واعدة في ثلث الخصم
    GoalCelebration = 3,     // SP_C*_03: فرحة واحتفال بهدف الفريق
    ComebackDefiance = 4,    // SP_C*_04: هتاف تشجيع عند التأخر في النتيجة
    DiscontentWhistle = 5    // SP_C*_05: صافرات استهجان عند خطأ الخصم
}

/// <summary>
/// محرك إدارة أهازيج الجماهير وديناميكيات صخب الملاعب (643 هتاف + 139 بيئة صوتية):
/// يختار الهتاف المناسب ديناميكياً بحسب هوية الفريق، الأرض/الجمهور، وسياق دراما المباراة
/// </summary>
public class StadiumSoundEngine
{
    private readonly string _chantsDir;
    private readonly string _acousticsDir;
    private DateTime _lastChantPlayTime = DateTime.MinValue;
    private readonly TimeSpan _minChantInterval = TimeSpan.FromSeconds(25);

    public Team? ActiveHomeTeam { get; private set; }
    public Team? ActiveAwayTeam { get; private set; }

    public StadiumSoundEngine(string? baseDir = null)
    {
        string baseAudio = baseDir ?? ResolveBaseAudioDir();
        _chantsDir = Path.Combine(baseAudio, "SFX", "Chants");
        _acousticsDir = Path.Combine(baseAudio, "SFX", "StadiumAcoustics");
    }

    private static string ResolveBaseAudioDir()
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "AccessibleFootball2026.sln")))
            {
                string slnAudio = Path.Combine(dir, "Assets", "Audio");
                if (Directory.Exists(slnAudio)) return slnAudio;
            }
            dir = Path.GetDirectoryName(dir);
        }

        string binAudio = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio");
        return binAudio;
    }

    public void SetMatchTeams(Team home, Team away)
    {
        ActiveHomeTeam = home;
        ActiveAwayTeam = away;
        ApplyStadiumAcousticsForTeam(home);
    }

    public void ApplyStadiumAcousticsForTeam(Team homeTeam)
    {
        if (!Directory.Exists(_acousticsDir)) return;

        // ربط هوية الملعب بالهندسة الصوتية ثلاثية الأبعاد
        string acousticFile = Path.Combine(_acousticsDir, $"Acoustic_{homeTeam.Id}.wav");
        if (!File.Exists(acousticFile))
        {
            // استخدام الملف الافتراضي أو أول ملف صوتي متاح في المجلد
            var files = Directory.GetFiles(_acousticsDir, "*.wav");
            acousticFile = files.Length > 0 ? files[0] : string.Empty;
        }

        if (File.Exists(acousticFile))
        {
            SoundEffectsPlayer.SetStadiumAcoustics(acousticFile);
        }
    }

    /// <summary>
    /// تشغيل الهتاف التكتيكي المناسب بحسب مجريات اللقاء
    /// </summary>
    public void HandleMatchContext(MatchEvent matchEvent, int homeScore, int awayScore, float matchMinute)
    {
        if (ActiveHomeTeam == null) return;

        // 1. احتفال بهدف الفريق صاحب الأرض
        if (matchEvent.Type == MatchEventType.GoalScored && matchEvent.GoalData != null)
        {
            if (matchEvent.GoalData.ScoringTeam == ActiveHomeTeam)
            {
                PlayTeamChant(ActiveHomeTeam, ChantSlot.GoalCelebration);
            }
            else
            {
                // هدف للخصم -> صافرات استهجان من جماهير الأرض
                PlayTeamChant(ActiveHomeTeam, ChantSlot.DiscontentWhistle);
            }
            return;
        }

        // 2. خطأ ارتكبه الخصم ضد صاحب الأرض
        if (matchEvent.Type == MatchEventType.FoulCommitted)
        {
            PlayTeamChant(ActiveHomeTeam, ChantSlot.DiscontentWhistle);
            return;
        }

        // 3. تأخر صاحب الأرض بهدف في الدقائق الحاسمة (دقيقة 75+)
        if (matchMinute >= 75.0f && homeScore < awayScore)
        {
            PlayTeamChant(ActiveHomeTeam, ChantSlot.ComebackDefiance);
            return;
        }

        // 4. هجمة واعدة ودخول الثلث الهجومي
        if (matchEvent.Type == MatchEventType.BallZoneChanged && matchEvent.CurrentZone == PitchZone.AttackingThird)
        {
            PlayTeamChant(ActiveHomeTeam, ChantSlot.AttackingMomentum);
            return;
        }

        // 5. هتافات دورية عامة
        if (DateTime.UtcNow - _lastChantPlayTime > _minChantInterval)
        {
            PlayTeamChant(ActiveHomeTeam, ChantSlot.GeneralRhythm);
        }
    }

    public void PlayTeamChant(Team team, ChantSlot slot)
    {
        if (!Directory.Exists(_chantsDir)) return;

        string chantPath = Path.Combine(_chantsDir, "Clubs", $"{team.Id}_{(int)slot:D2}.wav");
        if (!File.Exists(chantPath))
        {
            chantPath = Path.Combine(_chantsDir, "SP_C", $"Chant_{team.Id}.wav");
        }
        if (!File.Exists(chantPath))
        {
            // الرجوع إلى هتافات الدوري الثقافية الإقليمية (SP_R) أو العامة
            chantPath = ResolveRegionalChant(team, slot);
        }

        if (File.Exists(chantPath))
        {
            _lastChantPlayTime = DateTime.UtcNow;
            SoundEffectsPlayer.PlayOneShotChant(chantPath);
        }
    }

    private string ResolveRegionalChant(Team team, ChantSlot slot)
    {
        string regionalDir = Path.Combine(_chantsDir, "Regional");
        if (!Directory.Exists(regionalDir)) regionalDir = _chantsDir;

        var files = Directory.GetFiles(regionalDir, "*.wav");
        if (files.Length > 0)
        {
            int teamHash = team.Id != null ? team.Id.GetHashCode() : 0;
            int idx = Math.Abs(teamHash * 5 + (int)slot) % files.Length;
            return files[idx];
        }

        var anyChants = Directory.GetFiles(_chantsDir, "*.wav");
        return anyChants.Length > 0 ? anyChants[0] : string.Empty;
    }
}
