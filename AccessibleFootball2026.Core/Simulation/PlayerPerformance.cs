using AccessibleFootball2026.Core.Models;
namespace AccessibleFootball2026.Core.Simulation;

/// <summary>In-game match rating, not a real-world player rating.</summary>
public sealed class MatchPerformance
{
    public int Goals { get; private set; }
    public int Saves { get; private set; }
    public int Tackles { get; private set; }
    public int CompletedPasses { get; private set; }
    public int LostPasses { get; private set; }
    public int Misses { get; private set; }
    public int Fouls { get; private set; }
    public float Rating => Math.Clamp(6 + Goals * 1.2f + Saves * .35f + Tackles * .15f + CompletedPasses * .03f - LostPasses * .08f - Misses * .12f - Fouls * .2f, 1, 10);
    internal void Record(MatchEventType type)
    {
        switch(type)
        {
            case MatchEventType.GoalScored: Goals++; break;
            case MatchEventType.GoalkeeperSave: Saves++; break;
            case MatchEventType.TackleSuccessful: case MatchEventType.UserTackleSuccessful: case MatchEventType.OpponentTackleSuccessful: Tackles++; break;
            case MatchEventType.PassCompleted: CompletedPasses++; break;
            case MatchEventType.PassIntercepted: LostPasses++; break;
            case MatchEventType.MissedChance: Misses++; break;
            case MatchEventType.FoulCommitted: Fouls++; break;
        }
    }
}
public partial class MatchEngine
{
    private readonly Dictionary<Player,MatchPerformance> _performances = new();
    public MatchPerformance PerformanceOf(Player player)
    {
        if (!_performances.TryGetValue(player, out var result)) _performances[player] = result = new MatchPerformance();
        return result;
    }
    private void RecordPerformance(MatchEvent e)
    {
        var actor = _passPerformanceActor ?? (e.GoalData is { IsOwnGoal:false } goal ? goal.Scorer : e.SubjectPlayer);
        if (actor != null) PerformanceOf(actor).Record(e.Type);
    }
}
