using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Core.Simulation.Play;

public enum AttemptOutcome { Goal, Saved, Blocked, Woodwork, OffTarget, Annulled, Interrupted, OwnGoal }
public sealed record ShotAttempt(long Id, Player Shooter, Team Team, AttemptOutcome? Outcome)
{
    public float Minute { get; init; }
}

/// <summary>Every attempt has one identity and at most one terminal result. No predicted outcomes.</summary>
public sealed class AttemptLedger
{
    private long _nextId;
    private readonly List<ShotAttempt> _attempts = new();
    public IReadOnlyList<ShotAttempt> Attempts => _attempts.AsReadOnly();
    public ShotAttempt? Live { get; private set; }

    public ShotAttempt Begin(Player shooter, Team team, float minute = 0)
    {
        if (!team.Players.Contains(shooter) && !team.Bench.Contains(shooter))
            throw new ArgumentException("Shooter does not belong to this team", nameof(shooter));
        if (Live != null) throw new InvalidOperationException("Resolve the live attempt before starting another");
        Live = new(++_nextId, shooter, team, null) { Minute = minute };
        _attempts.Add(Live);
        return Live;
    }

    public ShotAttempt? Resolve(AttemptOutcome outcome, long? expectedId = null)
    {
        var live = Live;
        if (live == null || expectedId.HasValue && live.Id != expectedId.Value) return null;
        var result = live with { Outcome = outcome };
        _attempts[^1] = result;
        Live = null;
        return result;
    }

    // A deflection is a measured contact, not a second launch. If the unchanged attempt
    // later crosses the goal line without new control, the goal replaces its provisional
    // contact result. Interrupted/annulled attempts can never be revived this way.
    public ShotAttempt? ConfirmDeflectedGoal(Player shooter, Team team)
    {
        if (Live != null || _attempts.Count == 0) return null;
        var previous = _attempts[^1];
        if (previous.Shooter != shooter || previous.Team != team || previous.Outcome is not
            (AttemptOutcome.Saved or AttemptOutcome.Blocked or AttemptOutcome.Woodwork)) return null;
        var goal = previous with { Outcome = AttemptOutcome.Goal };
        _attempts[^1] = goal;
        return goal;
    }

    public int OfficialShots(Team team) => _attempts.Count(a => a.Team == team && a.Outcome is not (AttemptOutcome.Annulled or AttemptOutcome.Interrupted or AttemptOutcome.OwnGoal));
    public int OnTarget(Team team) => _attempts.Count(a => a.Team == team && a.Outcome is AttemptOutcome.Goal or AttemptOutcome.Saved);
}
