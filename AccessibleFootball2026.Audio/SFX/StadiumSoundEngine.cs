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

    public event Action<Team>? OnDiscontentWhistlePlayed;
    public Team? ActiveHomeTeam { get; private set; }
    public Team? ActiveAwayTeam { get; private set; }
    public string ActiveStadiumKey { get; set; } = "";
    public string ActiveStadiumName => StadiumCatalog.Find(ActiveStadiumKey)?.DisplayName ??
        LocalizationService.Text("لم يتم اختيار ملعب", "No stadium selected");

    // Shared grounds: designated match home wins only when both participants own it.
    // A sole away-slot owner still wins at its own ground, per the general user rule.
    public Team? ResolveGroundCrowdTeam()
    {
        var ground = StadiumCatalog.Find(ActiveStadiumKey);
        if (ground == null) return null;
        if (ActiveHomeTeam != null && ground.IsGroundFor(ActiveHomeTeam)) return ActiveHomeTeam;
        if (ActiveAwayTeam != null && ground.IsGroundFor(ActiveAwayTeam)) return ActiveAwayTeam;
        return null;
    }

    public bool ShouldPlayChantForTeam(Team team)
    {
        // Both user conditions: participates in this match + owns the selected ground.
        // Home/Away is the player's team slot, not proof of the stadium's crowd identity.
        if (ResolveGroundCrowdTeam() != team)
            return false;

        return TeamChantCatalog.Find(team) != null;
    }

    public StadiumSoundEngine(string? baseDir = null)
    {
        string baseAudio = baseDir ?? SoundEffectsPlayer.BaseAudioDirectory;
        _chantsDir = Path.Combine(baseAudio, "SFX", "Chants");
        _acousticsDir = Path.Combine(baseAudio, "SFX", "StadiumAcoustics");
    }



    public void SetMatchTeams(Team home, Team away)
    {
        ActiveHomeTeam = home;
        ActiveAwayTeam = away;
        ApplyStadiumAcousticsForTeam(home);
    }

    public void ApplyStadiumAcousticsForTeam(Team homeTeam)
    {
        // No source establishes a real-stadium identity for these legacy Acoustic_N files.
        // Full listening identified music in 3 and 10. Never auto-select an unverified bed
        // (including CampNou's old 2); chants/reactions remain independent and audible.
        SoundEffectsPlayer.SetStadiumAcoustics(null);
    }

    /// <summary>
    /// تشغيل الهتاف التكتيكي المناسب بحسب مجريات اللقاء
    /// </summary>
    public void HandleMatchContext(MatchEvent matchEvent, int homeScore, int awayScore, float matchMinute)
    {
        var crowdTeam = ActiveHomeTeam != null && ShouldPlayChantForTeam(ActiveHomeTeam) ? ActiveHomeTeam :
                        ActiveAwayTeam != null && ShouldPlayChantForTeam(ActiveAwayTeam) ? ActiveAwayTeam : null;
        if (crowdTeam == null) return;
        // Goals and missed chances have their own crowd reaction in SmartAudioCoordinator.
        // Never replace those reactions with a team chant, or cut a playing chant for an event.
        if (matchEvent.Type == MatchEventType.GoalScored || matchEvent.Type == MatchEventType.MissedChance ||
            matchEvent.Type == MatchEventType.FoulCommitted) return;
        PlayTeamChant(crowdTeam, ChantSlot.GeneralRhythm);
    }

    public void PlayTeamChant(Team team, ChantSlot slot, bool force = false)
    {
        // Full clips, random with replacement, no fixed gap or event preemption.
        // force is retained for API compatibility, not permission to cut a clip.
        if (slot == ChantSlot.GoalCelebration || slot == ChantSlot.DiscontentWhistle ||
            SoundEffectsPlayer.IsTeamChantActive) return;

        if (!Directory.Exists(_chantsDir)) return;

        // التحقق من الشرط المزدوج: إذا لم يتحقق الشرط، يظل صوت الجماهير محايداً تماماً بدون أي هتاف خاص
        if (!ShouldPlayChantForTeam(team)) return;

        // Numbered suffixes do not prove event meaning. Use full random clips only.
        var pool=TeamChantCatalog.Find(team);
        if (pool == null) return;
        string? chantPath = ResolveTeamChantPath(pool);
        if (chantPath == null) return;
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
    private string? ResolveTeamChantPath(TeamChantPool pool)
    {
        EnsureChantLabelIndexLoaded();
        if (_chantLabelIndex == null) return null;
        var candidates = new List<string>();
        foreach (var pair in _chantLabelIndex)
        {
            // Exact user-identified Egypt recording takes precedence over the conflicting historical Juventus slot.
            if (pair.Value == 600 && pool.TeamName != "Egypt") continue;
            if (pair.Value < pool.FirstIndex || pair.Value > pool.LastIndex ||
                !pair.Key.StartsWith(pool.LabelPrefix, StringComparison.OrdinalIgnoreCase)) continue;
            foreach (string dir in new[] { _chantsDir, Path.Combine(_chantsDir, "SP_R"), Path.Combine(_chantsDir, "Clubs") })
            {
                string candidate = Path.Combine(dir, $"Chant_{pair.Value}.wav");
                if (File.Exists(candidate))
                {
                    if (pool.ConfirmedRecordingSha256 != null)
                    {
                        string hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(candidate))).ToLowerInvariant();
                        if (!hash.Equals(pool.ConfirmedRecordingSha256, StringComparison.OrdinalIgnoreCase)) continue;
                    }
                    candidates.Add(candidate); break;
                }
            }
        }
        return candidates.Count == 0 ? null : candidates[Random.Shared.Next(candidates.Count)];
    }
}
