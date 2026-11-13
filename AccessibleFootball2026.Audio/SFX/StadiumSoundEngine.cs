using System;
using System.IO;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Audio.SFX;

public enum ChantSlot
{
    // Match triggers only. Original numeric suffixes do not establish acoustic meaning.
    GeneralRhythm = 1,
    AttackingMomentum = 2,
    GoalCelebration = 3,
    ComebackDefiance = 4,
    DiscontentWhistle = 5
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
    public string ActiveStadiumName => StadiumCatalog.Find(ActiveStadiumKey)?.DisplayName ??
        LocalizationService.Text("ملعب محايد", "Neutral stadium");

    public bool ShouldPlayChantForTeam(Team team)
    {
        // Both user conditions: participates in this match + owns the selected ground.
        // Home/Away is the player's team slot, not proof of the stadium's crowd identity.
        if (team.IsNationalTeam || (ActiveHomeTeam != team && ActiveAwayTeam != team))
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
        var crowdTeam = ActiveHomeTeam != null && ShouldPlayChantForTeam(ActiveHomeTeam) ? ActiveHomeTeam :
                        ActiveAwayTeam != null && ShouldPlayChantForTeam(ActiveAwayTeam) ? ActiveAwayTeam : null;
        if (crowdTeam == null) return;
        bool crowdIsHome = crowdTeam == ActiveHomeTeam;
        var opponent = crowdIsHome ? ActiveAwayTeam : ActiveHomeTeam;
        int crowdScore = crowdIsHome ? homeScore : awayScore;
        int opponentScore = crowdIsHome ? awayScore : homeScore;

        // 1. احتفال بهدف الفريق صاحب الأرض
        if (matchEvent.Type == MatchEventType.GoalScored && matchEvent.GoalData != null)
        {
            if (matchEvent.GoalData.ScoringTeam == crowdTeam)
            {
                // أحداث حقيقية حاسمة تتجاوز كبح التكرار: احتفال الهدف يقطع أي هتاف جارٍ
                PlayTeamChant(crowdTeam, ChantSlot.GoalCelebration, force: true);
            }
            else
            {
                // هدف للخصم -> صافرات استهجان من جماهير الأرض
                PlayTeamChant(crowdTeam, ChantSlot.DiscontentWhistle, force: true);
            }
            return;
        }

        // 2. خطأ ارتكبه الخصم ضد صاحب الأرض
        if (matchEvent.Type == MatchEventType.FoulCommitted)
        {
            // SubjectPlayer is the tackler. The ground owner's opponent may be either slot.
            // Missing actor evidence must not invent an opponent foul.
            if (matchEvent.SubjectPlayer != null && opponent != null &&
                opponent.Players.Contains(matchEvent.SubjectPlayer))
                PlayTeamChant(crowdTeam, ChantSlot.DiscontentWhistle, force: true);
            return;
        }

        // 3. تأخر صاحب الأرض بهدف في الدقائق الحاسمة (دقيقة 75+)
        if (matchMinute >= 75.0f && crowdScore < opponentScore)
        {
            PlayTeamChant(crowdTeam, ChantSlot.ComebackDefiance);
            return;
        }

        // 4. هجمة واعدة ودخول الثلث الهجومي
        if (matchEvent.Type == MatchEventType.BallZoneChanged && matchEvent.CurrentZone == PitchZone.AttackingThird)
        {
            PlayTeamChant(crowdTeam, ChantSlot.AttackingMomentum);
            return;
        }

        // 5. هتافات دورية عامة
        if (DateTime.UtcNow - _lastChantPlayTime > _minChantInterval)
        {
            PlayTeamChant(crowdTeam, ChantSlot.GeneralRhythm);
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

        // Team identity is supported by the PES2013 dt01 entry map and supplied labels.
        // Neither source documents goal/protest meaning for numbered suffixes.
        // Do not turn an unverified protest into a happy team chant or a whistle callback.
        if (slot == ChantSlot.DiscontentWhistle) return;
        string? chantPath = ResolveTeamChantPath(isRealMadrid ? "0109" : "0108");
        if (chantPath == null) return;
        _lastChantPlayTime = DateTime.UtcNow;
        SoundEffectsPlayer.PlayOneShotChant(chantPath);
    }

    private Dictionary<string, int>? _chantLabelIndex;
    private bool _chantLabelIndexLoaded;

    /// <summary>
    /// يحمّل فهرس الأهازيج الذي يكتبه المستخرج (رقم مقطع dt01.img إلى اسمه الأصلي من مقطع AINF).
    /// Labels identify entries, not the acoustic meaning of a numbered suffix.
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
            var entries = doc.RootElement;
            if (entries.ValueKind == System.Text.Json.JsonValueKind.Object &&
                entries.TryGetProperty("chants", out var chants)) entries = chants;
            if (entries.ValueKind != System.Text.Json.JsonValueKind.Array) return;
            foreach (var entry in entries.EnumerateArray())
            {
                if (entry.ValueKind != System.Text.Json.JsonValueKind.Object) continue;
                bool hasLabel = entry.TryGetProperty("label", out var lp) || entry.TryGetProperty("Label", out lp) ||
                                entry.TryGetProperty("konami_ainf_label", out lp);
                if (hasLabel && lp.ValueKind != System.Text.Json.JsonValueKind.String) continue;
                string label = hasLabel ? lp.GetString() ?? string.Empty : string.Empty;
                if (label.Length == 0) continue;
                bool hasIndex = entry.TryGetProperty("index", out var ip) || entry.TryGetProperty("Index", out ip);
                if (hasIndex && ip.ValueKind == System.Text.Json.JsonValueKind.Number && ip.TryGetInt32(out int idx) && idx >= 0)
                {
                    // Conflicting duplicate labels cannot silently redirect playback.
                    if (map.TryGetValue(label, out int prior) && prior != idx) { _chantLabelIndex = null; return; }
                    map[label] = idx;
                }
            }
            _chantLabelIndex = map;
        }
        catch { _chantLabelIndex = null; }
    }

    // Source: docs/REQUIREMENTS_AUDIT.md, Stage 9. Restrict to corroborated concrete
    // dt01 entry ranges, not every prefix-shaped label or a numeric team-ID fallback.
    private string? ResolveTeamChantPath(string originalTeamCode)
    {
        EnsureChantLabelIndexLoaded();
        if (_chantLabelIndex == null) return null;
        int first = originalTeamCode == "0109" ? 550 : 541;
        int last = originalTeamCode == "0109" ? 557 : 549;
        var candidates = new List<string>();
        foreach (var pair in _chantLabelIndex)
        {
            if (pair.Value < first || pair.Value > last ||
                !pair.Key.StartsWith($"SP_R{originalTeamCode}_", StringComparison.OrdinalIgnoreCase)) continue;
            foreach (string dir in new[] { _chantsDir, Path.Combine(_chantsDir, "SP_R"), Path.Combine(_chantsDir, "Clubs") })
            {
                string candidate = Path.Combine(dir, $"Chant_{pair.Value}.wav");
                if (File.Exists(candidate)) { candidates.Add(candidate); break; }
            }
        }
        return candidates.Count == 0 ? null : candidates[Random.Shared.Next(candidates.Count)];
    }
}
