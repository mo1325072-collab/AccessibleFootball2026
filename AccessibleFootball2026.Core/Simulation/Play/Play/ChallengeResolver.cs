using System.Numerics;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Core.Simulation.Play;

/// <summary>A challenge is contact geometry followed by a deterministic skill contest.
/// Neither team identity nor an input modifier can bypass reach, shielding or a live shot.</summary>
public static class ChallengeResolver
{
    public static MatchEngine.DuelResult Evaluate(Player defender, Player carrier, Vector2 ball,
        Team defence, Team attack, DifficultyLevel difficulty, Vector2 defenderMotion, Vector2 carrierMotion,
        bool sliding = false, bool transition = false, int? contestRoll = null, int? foulRoll = null)
    {
        var offset = carrier.Position2D - defender.Position2D;
        float bodyDistance = offset.Length();
        if (Vector2.Distance(ball, carrier.Position2D) < .05f) ball = Ball.FeetOf(carrier);
        var path = ball - defender.Position2D;
        float ballDistance = path.Length();
        var direction = ballDistance > .001f ? path / ballDistance : Vector2.Zero;
        var facing = defender.FacingDirection.LengthSquared() > .001f ? Vector2.Normalize(defender.FacingDirection) : Vector2.Zero;
        var forward = carrier.FacingDirection.LengthSquared() > .001f ? Vector2.Normalize(carrier.FacingDirection) : Vector2.UnitX;
        bool behind = bodyDistance > .001f && Vector2.Dot(offset / bodyDistance, forward) > .4f;
        float bodyAlong = Vector2.Dot(offset, direction);
        bool bodyInPath = bodyAlong > 0 && bodyAlong < ballDistance && (offset - direction * bodyAlong).Length() < .27f;
        float reach = (sliding ? 2.2f : 1.35f) + Math.Clamp((defender.Defending - 65) * .006f, 0, .2f);
        bool inReach = defender.IsActiveOnPitch && carrier.IsActiveOnPitch && ballDistance <= reach;
        bool aimed = ballDistance < .4f || Vector2.Dot(facing, direction) >= .35f;
        var incident = MatchEngine.CaptureFoulIncident(defender, carrier, ball, reach,
            tacklerMotion: defenderMotion, carrierMotion: carrierMotion);
        // A tactical label never supplies physical speed. Contact and real approach determine fouls.
        bool contact = bodyDistance <= .85f;
        bool dangerous = contact && incident.Severity == MatchEngine.FoulContactSeverity.EndangersSafety;
        bool foul = contact && aimed && (dangerous || bodyInPath &&
            (sliding || incident.Severity == MatchEngine.FoulContactSeverity.Reckless));
        float health(Player player) => Math.Clamp(player.CurrentStamina / 100f, .60f, 1f);
        float d = (defender.Defending * .50f + defender.Physical * .30f + defender.Speed * .20f) * health(defender);
        float a = (carrier.Dribbling * .45f + carrier.Physical * .35f + carrier.Speed * .20f) * health(carrier);
        var synergy = defence.Tactics.CalculateSynergy(defence.CurrentFormation).AdvantageMultiplier;
        d *= defence.Tactics.Marking == DefensiveMarking.ManToMan && d < a ? Math.Min(1, synergy) : synergy;
        a *= attack.Tactics.CalculateSynergy(attack.CurrentFormation).AdvantageMultiplier;
        int pressers = defence.Players.Count(p => p != defender && p.IsActiveOnPitch && p.Role != PlayerRole.Goalkeeper && Vector2.Distance(p.Position2D, carrier.Position2D) < 3.6f);
        int outlets = attack.Players.Count(p => p != carrier && p.IsActiveOnPitch && p.Role != PlayerRole.Goalkeeper && Vector2.Distance(p.Position2D, carrier.Position2D) < 4);
        float support = Math.Min(3, pressers) * (defence.Tactics.Marking == DefensiveMarking.Zonal ? 7 : 3.5f) - Math.Min(2, outlets) * 4;
        float angle = behind ? -18 - Math.Max(0, (carrier.Physical - 75) * .30f)
            : Vector2.Dot(forward, facing) < -.35f ? 10 : -8;
        float proximity = ballDistance < .65f ? 12 : ballDistance > 1.10f ? -12 : 0;
        float modifier = support + angle + proximity;
        modifier += defence.Tactics.Tackling switch
        {
            TacklingAggression.Cautious => -10, TacklingAggression.Moderate => 8,
            TacklingAggression.Aggressive => 16, TacklingAggression.Reckless => 25, _ => 0
        };
        if (transition && attack.Tactics.Focus == AttackingFocus.CounterAttack && attack.Tactics.Tempo >= 65) modifier -= 14;
        bool right = attack.MatchAttackingRight ?? attack.Side == TeamSide.Home;
        float localX = right ? carrier.Position2D.X : Pitch.Length - carrier.Position2D.X;
        if (defence.Tactics.PressingIntensity > 75 && attack.Tactics.PassingDirectness < 35 && localX < 18) modifier += 14;
        if (defence.Tactics.Marking == DefensiveMarking.ManToMan) modifier += Math.Clamp((d - a) * .15f, -8, 6);
        float adjustment = difficulty switch { DifficultyLevel.Easy => -5, DifficultyLevel.Hard => 4, DifficultyLevel.Legendary => 7, _ => 0 };
        float score = Math.Clamp(42 + (d - a) * .55f + modifier + adjustment, 3, 95);
        bool won = inReach && aimed && !bodyInPath && !foul && (contestRoll is { } roll ? roll < score : score >= 50);
        // Legacy deterministic test seams cannot cause a remote foul or bypass actual ball reach.
        if (foulRoll is { } fr && contact && bodyInPath && fr < 15) { foul = true; won = false; }
        return new(won, foul, dangerous && foul, score, d, a, modifier,
            bodyInPath ? "الكرة محمية بالجسم" : !inReach ? "خارج مدى الكرة" : won ? "تفوق في الوصول والمهارة" : "احتفاظ بالكرة")
        { FromBehind = behind, SlidingCommitment = sliding, Incident = incident };
    }
}
