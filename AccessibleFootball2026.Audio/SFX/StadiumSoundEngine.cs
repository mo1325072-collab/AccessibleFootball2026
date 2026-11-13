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

    public event Action<Team>? OnDiscontentWhistlePlayed;
    public Team? ActiveHomeTeam { get; private set; }
    public Team? ActiveAwayTeam { get; private set; }
    public string ActiveStadiumKey { get; set; } = "NeutralStadium";
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
        if (team.IsNationalTeam || ActiveHomeTeam == null || ActiveHomeTeam != team)
            return false;

        bool isRealMadrid = team.Id == "ESP_RMA" || team.Id == "173" || 
                            team.Name.Equals("Real Madrid", StringComparison.OrdinalIgnoreCase) || 
                            team.Name.Equals("ريال مدريد", StringComparison.OrdinalIgnoreCase);

        bool isBarcelona = team.Id == "ESP_BAR" || team.Id == "153" || 
                           (team.Name.Equals("Barcelona", StringComparison.OrdinalIgnoreCase) || team.Name.Equals("FC Barcelona", StringComparison.OrdinalIgnoreCase)) || 
                           team.Name.Equals("برشلونة", StringComparison.OrdinalIgnoreCase);

        if (ActiveStadiumKey == "SantiagoBernabeu" && isRealMadrid)
            return true;

        if (ActiveStadiumKey == "CampNou" && isBarcelona)
            return true;

        return false;
    }

    public StadiumSoundEngine(string? baseDir = null)
    {
        string baseAudio = baseDir ?? SoundEffectsPlayer.BaseAudioDirectory;
        _chantsDir = Path.Combine(baseAudio, "SFX", "Chants");
        _acousticsDir = Path.Combine(baseAudio, "SFX", "StadiumAcoustics");
    }



    public void SetMatchTeams(Team home, Team away)
    {
        _lastChantPlayTime = DateTime.MinValue;
        ActiveHomeTeam = home;
        ActiveAwayTeam = away;
        ApplyStadiumAcousticsForTeam(home);
    }

    public void ApplyStadiumAcousticsForTeam(Team homeTeam)
    {
        // ربط هوية الملعب الحقيقي في PES 2013 بالملف الصوتي للملعب
        string acousticFile = ActiveStadiumKey switch
        {
            "SantiagoBernabeu" => Path.Combine(_acousticsDir, "Acoustic_3.wav"), // سانتياجو برنابيو في PES 2013
            "CampNou" => Path.Combine(_acousticsDir, "Acoustic_2.wav"),          // كامب نو في PES 2013
            _ => Path.Combine(_acousticsDir, "Acoustic_10.wav")                  // ملعب محايد بصوت جماهير PES 2013 عام
        };

        if (File.Exists(acousticFile))
        {
            SoundEffectsPlayer.SetStadiumAcoustics(acousticFile);
        }
        else
        {
            // A missing ground must not silently become another stadium or retain last match's ground.
            // Stop clears the prior acoustic selection. General crowd ambience remains available.
            SoundEffectsPlayer.StopCrowdAmbiance();
            SoundEffectsPlayer.StartCrowdAmbiance();
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
            // SubjectPlayer is the tackler in MatchEngine. Only an away-team foul warrants
            // home crowd protest. Missing actor evidence must not invent an opponent foul.
            if (matchEvent.SubjectPlayer != null && ActiveAwayTeam != null &&
                ActiveAwayTeam.Players.Contains(matchEvent.SubjectPlayer))
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
                            team.Name.Equals("Real Madrid", StringComparison.OrdinalIgnoreCase) || 
                            team.Name.Equals("ريال مدريد", StringComparison.OrdinalIgnoreCase);

        bool isBarcelona = team.Id == "ESP_BAR" || team.Id == "153" || 
                           (team.Name.Equals("Barcelona", StringComparison.OrdinalIgnoreCase) || team.Name.Equals("FC Barcelona", StringComparison.OrdinalIgnoreCase)) || 
                           team.Name.Equals("برشلونة", StringComparison.OrdinalIgnoreCase);

        string teamId = isRealMadrid ? "173" : (isBarcelona ? "153" : team.Id);

        // التمييز الموثق الوحيد بين فئات الهتاف هو لاحقة الاسم الأصلي داخل dt01.img
        // (SP_C<team>_01..05 كما في توثيق ChantSlot)؛ يحفظه المستخرج في chants_index.json.
        // بدون هذا الفهرس لا يوجد على القرص أي تمييز فئات موثق، فنبقى على الهتاف العام للفريق
        // ولا نخترع نسبة أي مقطع إلى فئة.
        string? chantPath = ResolveSlotChantPath(teamId, slot);
        bool verifiedSlot=chantPath!=null;

        if (chantPath == null)
        {
            // السلوك العام السابق: ملف هتاف واحد للفريق يستخدم لكل الفئات
            string generic = Path.Combine(_chantsDir, $"Chant_{teamId}.wav");
            if (!File.Exists(generic))
            {
                generic = Path.Combine(_chantsDir, "SP_C", $"Chant_{teamId}.wav");
            }
            if (!File.Exists(generic))
            {
                generic = Path.Combine(_chantsDir, "Clubs", $"Chant_{teamId}.wav");
            }
            if (File.Exists(generic)) chantPath = generic;
        }

        if (chantPath != null && File.Exists(chantPath))
        {
            _lastChantPlayTime = DateTime.UtcNow;
            if(verifiedSlot && slot==ChantSlot.DiscontentWhistle)
            {
                if(SoundEffectsPlayer.TryPlayAudibleChant(chantPath))OnDiscontentWhistlePlayed?.Invoke(team);
            }
            else SoundEffectsPlayer.PlayOneShotChant(chantPath);
        }
    }

    private Dictionary<string, int>? _chantLabelIndex;
    private bool _chantLabelIndexLoaded;

    /// <summary>
    /// يحمّل فهرس الأهازيج الذي يكتبه المستخرج (رقم مقطع dt01.img إلى اسمه الأصلي من مقطع AINF).
    /// الاسم الأصلي وحده هو ما يخوّل نسبة مقطع إلى فئة عبر اللاحقة _01.._05.
    /// </summary>
    private void EnsureChantLabelIndexLoaded()
    {
        if (_chantLabelIndexLoaded) return;
        _chantLabelIndexLoaded = true;
        try
        {
            string mapPath = Path.Combine(_chantsDir, "chants_index.json");
            if (!File.Exists(mapPath)) return;
            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(mapPath));
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in doc.RootElement.EnumerateArray())
            {
                bool hasLabel = entry.TryGetProperty("label", out var lp) || entry.TryGetProperty("Label", out lp);
                string label = hasLabel ? lp.GetString() ?? string.Empty : string.Empty;
                if (label.StartsWith("A_", StringComparison.OrdinalIgnoreCase)) label = label[2..];
                if (label.Length == 0) continue;
                bool hasIndex = entry.TryGetProperty("index", out var ip) || entry.TryGetProperty("Index", out ip);
                if (hasIndex && ip.TryGetInt32(out int idx))
                    map[label] = idx;
            }
            _chantLabelIndex = map;
        }
        catch { _chantLabelIndex = null; }
    }

    /// <summary>
    /// يحل مسار هتاف خاص بفئة معينة للفريق عبر اسمه الأصلي الموثق، أو null إن لم يوجد فهرس أو مقطع مطابق.
    /// </summary>
    private string? ResolveSlotChantPath(string teamId, ChantSlot slot)
    {
        EnsureChantLabelIndexLoaded();
        if (_chantLabelIndex == null) return null;
        if (!_chantLabelIndex.TryGetValue($"SP_C{teamId}_{(int)slot:D2}", out int slotIndex)) return null;
        foreach (string dir in new[] { _chantsDir, Path.Combine(_chantsDir, "SP_C"), Path.Combine(_chantsDir, "Clubs") })
        {
            string candidate = Path.Combine(dir, $"Chant_{slotIndex}.wav");
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

}
