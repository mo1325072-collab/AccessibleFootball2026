using System.Numerics;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Core.Simulation.Play;

public enum BallControlDecision { Unavailable, TooHigh, TooFar, LiveShot, Recovering, Miscontrol, Controlled }

/// <summary>One eligibility rule for open-play control. Restarts are explicit placements, not pickups.</summary>
public static class PossessionArbiter
{
    public static BallControlDecision Evaluate(Ball ball, Player receiver, bool liveShot, bool recovering,
        bool pressured, double roll, float reach = 2.2f)
    {
        if (ball.Owner != null || ball.State is BallState.OutOfBounds or BallState.InGoal || !receiver.IsActiveOnPitch) return BallControlDecision.Unavailable;
        if (liveShot) return BallControlDecision.LiveShot;
        if (recovering) return BallControlDecision.Recovering;
        if (ball.Position.Z > 1.5f) return BallControlDecision.TooHigh;
        if (Vector2.Distance(receiver.Position2D, ball.Position2D) > reach) return BallControlDecision.TooFar;
        float speed = ball.Velocity.Length();
        double error = !pressured && speed <= 22 ? 0 : Math.Clamp((100 - receiver.Dribbling) * .001 + Math.Max(0, speed - 22) * .006 + (pressured ? .08 : 0), 0, .35);
        return roll < error ? BallControlDecision.Miscontrol : BallControlDecision.Controlled;
    }

    /// <summary>Batch choice has stable ties and does not depend on home/away enumeration order.</summary>
    public static Player? NearestEligible(Ball ball, IEnumerable<Player> candidates, bool liveShot, float reach = 2.2f)
    {
        if (ball.Owner != null || ball.State is BallState.OutOfBounds or BallState.InGoal || liveShot || ball.Position.Z > 1.5f) return null;
        return candidates.Where(p => p.IsActiveOnPitch && Vector2.Distance(p.Position2D, ball.Position2D) <= reach)
            .OrderBy(p => Vector2.DistanceSquared(p.Position2D, ball.Position2D)).ThenBy(p => p.Id).FirstOrDefault();
    }
}
