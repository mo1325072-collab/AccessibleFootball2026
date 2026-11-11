using System.Numerics;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Core.Simulation;

public partial class MatchEngine
{
    private bool _extraTimeHalfReached;
    private string? _lastShotWoodwork;

    private void SwapPlayingEnds()
    {
        UserAttackingRight = !UserAttackingRight;
        foreach (var player in HomeTeam.Players.Concat(AwayTeam.Players).Where(p => p.IsActiveOnPitch))
        {
            player.Position2D = new Vector2(Pitch.Length - player.Position2D.X, Pitch.Width - player.Position2D.Y);
            player.HomePosition = new Vector2(Pitch.Length - player.HomePosition.X, Pitch.Width - player.HomePosition.Y);
            player.FacingDirection = -player.FacingDirection;
        }
        _replayFrames.Clear();
    }

    private void PlaceKickoff(Team team)
    {
        var kicker = team.Players.FirstOrDefault(p => p.IsActiveOnPitch && p.Role != PlayerRole.Goalkeeper);
        if (kicker == null) return;
        kicker.Position2D = Pitch.CenterSpot;
        kicker.FacingDirection = new Vector2(team == HomeTeam == UserAttackingRight ? 1 : -1, 0);
        Ball.CatchOrTackle(kicker);
        RecordBallTouch(kicker);
        if (team == HomeTeam) ActiveUserPlayer = kicker;
        _kickoffProtectionTimer = 4;
        GrantPossessionGrace(1.5f);
        foreach (var opponent in (team == HomeTeam ? AwayTeam : HomeTeam).Players.Where(p => p.IsActiveOnPitch))
        {
            bool right = kicker.FacingDirection.X > 0;
            opponent.Position2D = new Vector2(right ? Math.Max(opponent.Position2D.X, Pitch.HalfLength + 11) : Math.Min(opponent.Position2D.X, Pitch.HalfLength - 11), opponent.Position2D.Y);
        }
    }
}
