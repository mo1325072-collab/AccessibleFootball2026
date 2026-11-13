namespace AccessibleFootball2026.Core.Models;

/// <summary>Law 10 eligibility is separate from the on-pitch squad and the substitution ledger.</summary>
public sealed class ShootoutRoster
{
    private readonly List<Player> _players;
    public IReadOnlyList<Player> Players => _players;
    public ShootoutRoster(Team team) => _players = team.Players.Where(p => p.IsActiveOnPitch).ToList();
    public bool Exclude(Player player, int targetCount)
    {
        if (targetCount < 1 || _players.Count <= targetCount || !_players.Contains(player) || player.Role == PlayerRole.Goalkeeper) return false;
        return _players.Remove(player);
    }
    public void ReduceCpuTo(int targetCount)
    {
        foreach (var player in _players.Where(p => p.Role != PlayerRole.Goalkeeper).OrderBy(p => p.Shooting).ToArray())
            if (_players.Count > targetCount) Exclude(player, targetCount);
    }
    public Player? Taker(int kicksTaken) => kicksTaken < 0 || _players.Count == 0 ? null : _players[kicksTaken % _players.Count];
}
