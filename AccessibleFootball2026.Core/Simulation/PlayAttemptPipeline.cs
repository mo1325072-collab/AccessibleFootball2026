using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation.Play;
namespace AccessibleFootball2026.Core.Simulation;
public partial class MatchEngine
{
    private readonly AttemptLedger _attempts = new();
    public IReadOnlyList<ShotAttempt> Attempts => _attempts.Attempts;
    private readonly Dictionary<long, (Player Keeper, float ConfidenceCredit)> _provisionalSaveCredits = new();

    private void RevokeProvisionalSave(long attemptId)
    {
        if (!_provisionalSaveCredits.Remove(attemptId, out var credit)) return;
        PerformanceOf(credit.Keeper).RevokeSave();
        var team = TeamOf(credit.Keeper);
        if (team != null)
        {
            var statistics = StatisticsFor(team);
            statistics.Saves = Math.Max(0, statistics.Saves - 1);
            _teamSaves[team.Id] = Math.Max(0, _teamSaves.GetValueOrDefault(team.Id) - 1);
        }
        _commentaryStatistics["stats.saves"] = Math.Max(0, _commentaryStatistics.GetValueOrDefault("stats.saves") - 1);
        _confidence[credit.Keeper] = Math.Clamp(_confidence.GetValueOrDefault(credit.Keeper, 75) - credit.ConfidenceCredit, 10, 100);
    }

    private void InterruptAttempt()
    {
        if (_attempts.Live == null) return;
        _isShotInProgress = false;
        Publish(new MatchEvent(MatchEventType.ShotInterrupted, "توقفت المحاولة", CurrentZone,
            subjectPlayer: _attempts.Live.Shooter));
    }

    // Runs before ratings, stats or audio. The original attempt supplies attribution,
    // never the current owner, most recent keeper, event display text or a team total.
    private bool RegisterAttemptEvent(MatchEvent e)
    {
        if (e.Type == MatchEventType.ShotTaken && e.SubjectPlayer is { } shooter && TeamOf(shooter) is { } team)
        {
            InterruptAttempt();
            var attempt = _attempts.Begin(shooter, team, MatchTimeMinutes);
            _isShotInProgress = true;
            e.Context = e.Context.With(("attempt.id", attempt.Id), ("attempt.team", team.Id));
            return true;
        }
        AttemptOutcome? outcome = e.Type switch
        {
            MatchEventType.GoalkeeperSave => AttemptOutcome.Saved,
            MatchEventType.ShotBlocked => AttemptOutcome.Blocked,
            MatchEventType.WoodworkHit => AttemptOutcome.Woodwork,
            MatchEventType.MissedChance => AttemptOutcome.OffTarget,
            MatchEventType.ShotInterrupted => AttemptOutcome.Interrupted,
            MatchEventType.ShotAnnulled => AttemptOutcome.Annulled,
            MatchEventType.GoalScored when e.GoalData is { IsOwnGoal: true } => AttemptOutcome.OwnGoal,
            MatchEventType.GoalScored when e.GoalData is { IsOwnGoal: false } => AttemptOutcome.Goal,
            _ => null
        };
        if (outcome == null) return true;
        ShotAttempt? confirmedDeflection = null;
        if (outcome == AttemptOutcome.Goal && _attempts.Live == null && e.GoalData is { } goal)
        {
            // A real unregistered finish (carry or later touch) counts without inventing a kick.
            // A measured on-target shot that deflects into the net remains the same attempt.
            bool deflected = e.Context.TryGet("goal.deflectedShot", out var value) && value == "true";
            if (deflected) confirmedDeflection = _attempts.ConfirmDeflectedGoal(goal.Scorer, goal.ScoringTeam);
            if (confirmedDeflection != null) RevokeProvisionalSave(confirmedDeflection.Id);
            if (confirmedDeflection == null) _attempts.Begin(goal.Scorer, goal.ScoringTeam, MatchTimeMinutes);
        }
        var resolved = confirmedDeflection ?? _attempts.Resolve(outcome.Value);
        // Synthetic public audio triggers may still play without fabricating measured stats.
        if (resolved == null) return e.Type == MatchEventType.GoalScored || e.SubjectPlayer == null && e.Type is MatchEventType.GoalkeeperSave or MatchEventType.WoodworkHit;
        _isShotInProgress = false;
        if (resolved.Outcome == AttemptOutcome.Saved && e.SubjectPlayer is { } keeper)
        {
            float confidence = _confidence.GetValueOrDefault(keeper, 75);
            _provisionalSaveCredits[resolved.Id] = (keeper, Math.Clamp(confidence + 3, 10, 100) - confidence);
        }
        e.Context = e.Context.With(("attempt.id", resolved.Id), ("attempt.team", resolved.Team.Id),
            ("attempt.outcome", resolved.Outcome), ("attempt.shooter", resolved.Shooter.Id));
        return true;
    }

    private void SyncAttemptTotals()
    {
        foreach (var team in new[] { HomeTeam, AwayTeam })
        {
            var statistics = StatisticsFor(team);
            statistics.Shots = _attempts.OfficialShots(team);
            statistics.ShotsAnnulled = _attempts.Attempts.Count(a => a.Team == team && a.Outcome == AttemptOutcome.Annulled);
            statistics.ShotsInterrupted = _attempts.Attempts.Count(a => a.Team == team && a.Outcome == AttemptOutcome.Interrupted);
            statistics.ShotsOnTarget = _attempts.OnTarget(team);
            statistics.ShotsOffTarget = _attempts.Attempts.Count(a => a.Team == team && a.Outcome == AttemptOutcome.OffTarget);
            statistics.BlockedShots = _attempts.Attempts.Count(a => a.Team == team && a.Outcome == AttemptOutcome.Blocked);
            statistics.Woodwork = _attempts.Attempts.Count(a => a.Team == team && a.Outcome == AttemptOutcome.Woodwork);
            _teamShots[team.Id] = statistics.Shots;
        }
        _commentaryStatistics["stats.shots"] = _homeStatistics.Shots + _awayStatistics.Shots;
        _commentaryStatistics["stats.firstHalfShots"] = _attempts.Attempts.Count(a => a.Minute < 45 && a.Outcome is not (AttemptOutcome.Annulled or AttemptOutcome.Interrupted or AttemptOutcome.OwnGoal));
        _commentaryStatistics["stats.misses"] = _homeStatistics.ShotsOffTarget + _awayStatistics.ShotsOffTarget;
    }
}
