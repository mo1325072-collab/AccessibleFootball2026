using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Core.Simulation;

public partial class MatchEngine
{
    /// <summary>A retained result built only from this match's recorded state. Shootout goals are separate.</summary>
    public IReadOnlyList<string> GetCompletedMatchSummary(int? homePenalties = null, int? awayPenalties = null)
    {
        string T(string ar, string en) => LocalizationService.Text(ar, en);
        bool shootout = homePenalties.HasValue && awayPenalties.HasValue;
        bool completed = State == MatchState.GameOver && AbandonmentReason == null;
        int home = shootout ? homePenalties!.Value : HomeTeam.Score;
        int away = shootout ? awayPenalties!.Value : AwayTeam.Score;
        string result = !completed ? T("ملخص اللقاء عند التوقف", "Match summary at stoppage") : home > away
            ? T("نهاية المباراة. فاز فريقك", "Full time. Your team won") : home < away
            ? T("نهاية المباراة. خسر فريقك", "Full time. Your team lost") : T("نهاية المباراة. تعادل", "Full time. Draw");
        var lines = new List<string> { result };
        if (AbandonmentReason != null) lines.Add(AbandonmentReason);
        lines.Add(T($"نتيجة اللعب: {HomeTeam.Name} {HomeTeam.Score}، {AwayTeam.Name} {AwayTeam.Score}.",
            $"Match score: {HomeTeam.Name} {HomeTeam.Score}, {AwayTeam.Name} {AwayTeam.Score}."));
        if (shootout) lines.Add(T($"ركلات الترجيح: {HomeTeam.Name} {home}، {AwayTeam.Name} {away}.",
            $"Penalty shootout: {HomeTeam.Name} {home}, {AwayTeam.Name} {away}."));
        foreach (var replay in _goalHistory)
        {
            var goal = replay.Goal;
            lines.Add(T($"هدف: {goal.ScoringTeam.Name}. النتيجة وقت الهدف {goal.HomeScore} مقابل {goal.AwayScore}.",
                $"Goal: {goal.ScoringTeam.Name}. Score after goal {goal.HomeScore} - {goal.AwayScore}."));
        }
        lines.AddRange(GetStatisticsAnnouncements().Skip(1));
        return lines;
    }
}
