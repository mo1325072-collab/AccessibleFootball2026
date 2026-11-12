using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Core.Simulation;

/// <summary>
/// حالات تفاعل يمثلها المحرك نفسه تمثيلاً صريحاً (مدرب/لاعب)، لكل حالة سبب فعلي من أحداث
/// المباراة ونافذة زمنية تنتهي. التعليق يروي الحالة الممثلة فقط: لا استدلال من النتيجة
/// وحدها، ولا ادعاء مشهد مرئي غير موجود في لعبة صوتية.
/// </summary>
public partial class MatchEngine
{
    private const float ReactionWindowSeconds = 90;
    private readonly Dictionary<string, (string Reaction, float UntilSeconds)> _coachReactions = new(StringComparer.Ordinal);
    private readonly Dictionary<int, (string Reaction, float UntilSeconds)> _playerReactions = new();

    private void SetCoachReaction(string teamId, string reaction) => _coachReactions[teamId] = (reaction, _elapsedSeconds + ReactionWindowSeconds);
    private void SetPlayerReaction(int playerId, string reaction) => _playerReactions[playerId] = (reaction, _elapsedSeconds + ReactionWindowSeconds);

    private string? CoachReactionOf(string teamId) =>
        _coachReactions.TryGetValue(teamId, out var r) && _elapsedSeconds <= r.UntilSeconds ? r.Reaction : null;

    private string? PlayerReactionOf(int playerId) =>
        _playerReactions.TryGetValue(playerId, out var r) && _elapsedSeconds <= r.UntilSeconds ? r.Reaction : null;

    /// <summary>يسجل التفاعل الممثل وسببه الحدث الفعلي الجاري؛ يُستدعى قبل بناء لقطة سياق الحدث.</summary>
    private void RecordReactionsFor(MatchEvent e)
    {
        if (e.Type == MatchEventType.GoalScored && e.GoalData is { } g)
        {
            // ScoringTeam هو الفريق المستفيد من الهدف؛ في الهدف الذاتي المسجل من الفريق المتلقي
            var concedingTeam = g.ScoringTeam == HomeTeam ? AwayTeam : HomeTeam;
            int scoringScore = g.ScoringTeam == HomeTeam ? g.HomeScore : g.AwayScore;
            int concedingScore = g.ScoringTeam == HomeTeam ? g.AwayScore : g.HomeScore;
            if (g.IsOwnGoal)
            {
                // حزن اللاعب صاحب اللمسة الفعلية في مرماه: حالة ممثلة سببها الهدف الذاتي نفسه
                SetPlayerReaction(g.Scorer.Id, "devastated");
            }
            else
            {
                if (scoringScore == concedingScore + 1) SetCoachReaction(g.ScoringTeam.Id, "joyfulLead");
                if (scoringScore == concedingScore) SetCoachReaction(concedingTeam.Id, "displeasedConceded");
            }
            int deficit = scoringScore - concedingScore;
            if (deficit >= 3) SetCoachReaction(concedingTeam.Id, "sad");
            else if (deficit >= 2) SetCoachReaction(concedingTeam.Id, "dejected");
        }
        if (e.Type == MatchEventType.MissedChance && e.SubjectPlayer != null && _shotContextValid &&
            _lastShotContext.TryGet("shot.underPressure", out var pressured) && pressured == "true")
        {
            // توتر المسدد تحت ضغط فعلي: حالة ممثلة سببها إهدار فرصة تحت ضغط مسجل
            SetPlayerReaction(e.SubjectPlayer.Id, "anxious");
        }
    }
}
