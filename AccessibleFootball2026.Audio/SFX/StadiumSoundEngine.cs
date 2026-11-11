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
    public string ActiveStadiumKey { get; set; } = "SantiagoBernabeu";
    public string ActiveStadiumName => ActiveStadiumKey switch
    {
        "SantiagoBernabeu" => "ملعب سانتياجو برنابيو (معقل ريال مدريد)",
        "CampNou" => "ملعب كامب نو (معقل برشلونة)",
        _ => "ملعب محايد (صوت جماهير محايد فقط)"
    };

    public bool ShouldPlayChantForTeam(Team team)
    {
        // الشرط المزدوج الصارم:
        // 1. الفريق يلعب على أرضه وصاحب الأرض (Home Team)
        // 2. الملعب الحالي هو ملعبه الرسمي الحقيقي الموثق
        if (ActiveHomeTeam == null || ActiveHomeTeam != team)
            return false;

        bool isRealMadrid = team.Id == "ESP_RMA" || team.Id == "173" || 
                            team.Name.Contains("Real Madrid", StringComparison.OrdinalIgnoreCase) || 
                            team.Name.Contains("مدريد", StringComparison.OrdinalIgnoreCase);

        bool isBarcelona = team.Id == "ESP_BAR" || team.Id == "153" || 
                           team.Name.Contains("Barcelona", StringComparison.OrdinalIgnoreCase) || 
                           team.Name.Contains("برشلونة", StringComparison.OrdinalIgnoreCase);

        if (ActiveStadiumKey == "SantiagoBernabeu" && isRealMadrid)
            return true;

        if (ActiveStadiumKey == "CampNou" && isBarcelona)
            return true;

        return false;
    }

    public StadiumSoundEngine(string? baseDir = null)
    {
        string baseAudio = baseDir ?? ResolveBaseAudioDir();
        _chantsDir = Path.Combine(baseAudio, "SFX", "Chants");
        _acousticsDir = Path.Combine(baseAudio, "SFX", "StadiumAcoustics");
    }

    private static string ResolveBaseAudioDir()
    {
        string dDriveAudio = @"D:\AccessibleFootball2026\Assets\Audio";
        if (Directory.Exists(dDriveAudio)) return dDriveAudio;

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

        // ربط هوية الملعب الحقيقي في PES 2013 بالملف الصوتي للملعب
        string acousticFile = ActiveStadiumKey switch
        {
            "SantiagoBernabeu" => Path.Combine(_acousticsDir, "Acoustic_3.wav"), // سانتياجو برنابيو في PES 2013
            "CampNou" => Path.Combine(_acousticsDir, "Acoustic_2.wav"),          // كامب نو في PES 2013
            _ => Path.Combine(_acousticsDir, "Acoustic_10.wav")                  // ملعب محايد بصوت جماهير PES 2013 عام
        };

        if (!File.Exists(acousticFile))
        {
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
                // أحداث حقيقية حاسمة تتجاوز كبح التكرار: احتفال الهدف يقطع أي هتاف جارٍ
                PlayTeamChant(ActiveHomeTeam, ChantSlot.GoalCelebration, force: true);
            }
            else
            {
                // هدف للخصم -> صافرات استهجان من جماهير الأرض
                PlayTeamChant(ActiveHomeTeam, ChantSlot.DiscontentWhistle, force: true);
            }
            return;
        }

        // 2. خطأ ارتكبه الخصم ضد صاحب الأرض
        if (matchEvent.Type == MatchEventType.FoulCommitted)
        {
            PlayTeamChant(ActiveHomeTeam, ChantSlot.DiscontentWhistle, force: true);
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

    public void PlayTeamChant(Team team, ChantSlot slot, bool force = false)
    {
        // كبح التكرار: الحلقة الرئيسية تستدعي فحص السياق كل إطار (50 مرة/ثانية)،
        // وبدون هذا الكبح يعاد فتح قناة الهتاف كل إطار فيسمع المستخدم تقطيعاً متصلاً
        if (!force && DateTime.UtcNow - _lastChantPlayTime < _minChantInterval) return;

        if (!Directory.Exists(_chantsDir)) return;

        // التحقق من الشرط المزدوج: إذا لم يتحقق الشرط، يظل صوت الجماهير محايداً تماماً بدون أي هتاف خاص
        if (!ShouldPlayChantForTeam(team)) return;

        bool isRealMadrid = team.Id == "ESP_RMA" || team.Id == "173" || 
                            team.Name.Contains("Real Madrid", StringComparison.OrdinalIgnoreCase) || 
                            team.Name.Contains("مدريد", StringComparison.OrdinalIgnoreCase);

        bool isBarcelona = team.Id == "ESP_BAR" || team.Id == "153" || 
                           team.Name.Contains("Barcelona", StringComparison.OrdinalIgnoreCase) || 
                           team.Name.Contains("برشلونة", StringComparison.OrdinalIgnoreCase);

        string teamId = isRealMadrid ? "173" : (isBarcelona ? "153" : team.Id);

        string chantPath = Path.Combine(_chantsDir, $"Chant_{teamId}.wav");
        if (!File.Exists(chantPath))
        {
            chantPath = Path.Combine(_chantsDir, "SP_C", $"Chant_{teamId}.wav");
        }
        if (!File.Exists(chantPath))
        {
            chantPath = Path.Combine(_chantsDir, "Clubs", $"Chant_{teamId}.wav");
        }
        if (!File.Exists(chantPath))
        {
            chantPath = Path.Combine(_chantsDir, "Clubs", $"{team.Id}_{(int)slot:D2}.wav");
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

