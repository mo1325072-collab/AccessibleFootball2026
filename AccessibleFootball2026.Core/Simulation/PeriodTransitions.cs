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
        HomeTeam.MatchAttackingRight = UserAttackingRight;
        AwayTeam.MatchAttackingRight = !UserAttackingRight;
        foreach (var player in HomeTeam.Players.Concat(AwayTeam.Players).Where(p => p.IsActiveOnPitch))
        {
            player.Position2D = new Vector2(Pitch.Length - player.Position2D.X, Pitch.Width - player.Position2D.Y);
            player.HomePosition = new Vector2(Pitch.Length - player.HomePosition.X, Pitch.Width - player.HomePosition.Y);
            player.FacingDirection = -player.FacingDirection;
        }
        _replayFrames.Clear();
    }

    private void ResetPeriodPlayState()
    {
        _videoDecision=null;_videoVerdict=null;_videoVerdictAnnounced=false;_videoSeconds=0;
        _tacticalDecisionWait.Clear();
        _transitionTeamId=null;_transitionUntil=0;_tacticalLastOwnerTeamId=null;
        AwaitingStoppageConfirmation = false;
        CompleteAnalysisReplay();
        ActivePassTransit = null;
        Restart = null;
        _commentaryPass = null;
        _isShotInProgress = false;
        _shotContextValid = false;
        _shotShooter = null;
        _offsidePositions.Clear();
        _offsideOffence = null;
        _offsideExemptKicker = null;
        _restartReadySeconds = 0;
        _gkHoldTimer = 0;
        _ballFreeTimer = 0;
        PreviousKicker = null;
        LastKicker = null;
        _advantage = null;
        _pendingClearance = null;
        _runUpPlayer = null;
        _runUpRemaining = 0;
        _executingRunUp = null;
    }

    private void PlaceKickoff(Team team, Player? preferredKicker = null)
    {
        ResetPeriodPlayState();
        // Law 8: everyone except the kicker starts in their own half.
        foreach (var squad in new[] { HomeTeam, AwayTeam })
        {
            bool attacksRight = squad == HomeTeam ? UserAttackingRight : !UserAttackingRight;
            foreach (var player in squad.Players.Where(p => p.IsActiveOnPitch))
            {
                player.Position2D = new Vector2(attacksRight ? Math.Min(player.HomePosition.X, Pitch.HalfLength - .5f) : Math.Max(player.HomePosition.X, Pitch.HalfLength + .5f), player.HomePosition.Y);
                player.Velocity = Vector2.Zero;
                player.FacingDirection = new Vector2(attacksRight ? 1 : -1, 0);
            }
        }
        var kicker = preferredKicker?.IsActiveOnPitch == true && team.Players.Contains(preferredKicker) ? preferredKicker : team.Players.FirstOrDefault(p => p.IsActiveOnPitch && p.Role != PlayerRole.Goalkeeper);
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
