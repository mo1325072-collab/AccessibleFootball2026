using System.Linq;
using AccessibleFootball2026.Core.Models;
namespace AccessibleFootball2026.Core.Simulation;

public partial class MatchEngine
{
    private bool _resolvingInjuryChoice;
    private readonly Queue<Player> _pendingInjuryChoices = new();
    public Player? PendingInjuryPlayer => _pendingInjuryChoices.Count > 0 ? _pendingInjuryChoices.Peek() : null;

    public bool ResolveInjurySubstitution(Player injured, Player? replacement)
    {
        if (PendingInjuryPlayer != injured || !HomeTeam.Players.Contains(injured) || injured.IsSentOff) return false;
        if (replacement != null)
        {
            if (!HomeTeam.CanEnterAsSubstitute(replacement) || !CanMakeSubstitution(HomeTeam)) return false;
            _resolvingInjuryChoice = true;
            try { if (!MakeSubstitution(HomeTeam, injured, replacement)) return false; }
            finally { _resolvingInjuryChoice = false; }
        }
        else if (!injured.IsOffPitch) LeavePitchInjured(injured, HomeTeam);
        if (_pendingInjuryChoices.Count > 0) _pendingInjuryChoices.Dequeue();
        return true;
    }

    private void LeavePitchInjured(Player player, Team team)
    {
        RemovePlayerFromPlay(player, team, sentOff: false);
        if (Restart is { } pending && pending.Taker == player)
        {
            var taker = team.Players.FirstOrDefault(p => p.IsActiveOnPitch && p.Role != PlayerRole.Goalkeeper)
                ?? team.Players.FirstOrDefault(p => p.IsActiveOnPitch);
            if (taker != null)
            {
                Restart = pending with { Taker = taker };
                taker.Position2D = pending.Spot;
                Ball.CatchOrTackle(taker); RecordBallTouch(taker);
                if (team == HomeTeam) ActiveUserPlayer = taker;
            }
        }
        int remaining = team.Players.Count(p => p.IsActiveOnPitch);
        Publish(new MatchEvent(MatchEventType.PlayerInjured,
            $"يغادر {player.Name} مصاباً دون بديل. يتبقى {remaining} لاعبين في {team.Name}.", CurrentZone, subjectPlayer: player),
            ("injury.severity", "severe"), ("injury.replaced", false));
        StopIfTooFewPlayers();
    }
}
