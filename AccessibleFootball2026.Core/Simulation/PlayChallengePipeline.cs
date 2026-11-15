using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation.Play;
namespace AccessibleFootball2026.Core.Simulation;
public partial class MatchEngine
{
    private bool ResolveChallenge(Player defender, Player carrier, bool sliding, bool manual = false)
    {
        var defence = TeamOf(defender); var attack = TeamOf(carrier);
        if (defence == null || attack == null || defence == attack || Ball.Owner != carrier || _isShotInProgress) return false;
        var outcome = ChallengeResolver.Evaluate(defender, carrier, Ball.Position2D, defence, attack,
            manual ? DifficultyLevel.Normal : MatchSettings.Current.Difficulty, ContactMotion(defender), ContactMotion(carrier),
            sliding, IsCounterTransition(attack));
        if (!manual) _aiTackleCooldownTimer = outcome.IsFoul ? 2.5f : outcome.TacklerWon ? 2 : 1.3f;
        if (outcome.IsFoul)
        {
            if (sliding && defender == _slidingPlayer) _slideContactResolved = true;
            Publish(new MatchEvent(MatchEventType.FoulCommitted, $"خطأ ضد {defender.Name}", CurrentZone,
                spatialPan: CalculateBallPan(), subjectPlayer: defender), ("foul.intentional", false));
            HandleFoulSanction(defender, carrier, sliding, outcome.FromBehind, outcome.Incident);
            return false;
        }
        if (outcome.TacklerWon)
        {
            if (sliding && defender == _slidingPlayer) _slideContactResolved = true;
            Ball.CatchOrTackle(defender);
            RecordBallTouch(defender);
            GrantPossessionGrace(1.4f);
            if (ActivePassTransit != null) { ActivePassTransit.IsActive = false; ActivePassTransit = null; }
            if (defence == HomeTeam) ActiveUserPlayer = defender;
            var type = manual ? MatchEventType.UserTackleSuccessful : defence == AwayTeam ? MatchEventType.OpponentTackleSuccessful : MatchEventType.BallTrapped;
            Publish(new MatchEvent(type, $"استخلص {defender.Name} الكرة من {carrier.Name}", CurrentZone,
                spatialPan: CalculateBallPan(), subjectPlayer: defender), ("tackle.sliding", sliding), ("tackle.won", true));
            return true;
        }
        Publish(new MatchEvent(MatchEventType.TackleAttempted, "محاولة استخلاص دون افتكاك الكرة", CurrentZone,
            spatialPan: CalculateBallPan(), subjectPlayer: defender), ("tackle.sliding", sliding), ("tackle.won", false));
        return false;
    }
}
