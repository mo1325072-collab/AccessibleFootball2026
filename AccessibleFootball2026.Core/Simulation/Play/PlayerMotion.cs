using System.Numerics;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Core.Simulation.Play;

/// <summary>Shared kinematic movement. Every move sweeps the entire segment against player bodies.</summary>
public static class PlayerMotion
{
    public const float BodySeparation = .55f;
    public readonly record struct Move(Vector2 Position, Vector2 Velocity, Vector2 IntendedVelocity, bool Contact);

    public static Move Advance(Player actor, Vector2 velocity, float elapsed, IEnumerable<Player> obstacles)
    {
        var start = actor.Position2D;
        if (!actor.IsActiveOnPitch || !BallFlight.Finite(velocity) || !float.IsFinite(elapsed) || elapsed <= 0)
            return new(start, Vector2.Zero, Vector2.Zero, false);
        var remaining = velocity * elapsed;
        var position = start;
        bool contact = false;
        var bodies = obstacles.Where(p => p != actor && p.IsActiveOnPitch).OrderBy(p => p.Id).ToArray();
        // Resolve at most three contacts, sliding along the tangent rather than moving through a body.
        for (int iteration = 0; iteration < 3 && remaining.LengthSquared() > .0000001f; iteration++)
        {
            float earliest = 1;
            Vector2 normal = Vector2.Zero;
            foreach (var body in bodies)
            {
                var offset = position - body.Position2D;
                float distance = offset.Length();
                if (distance < BodySeparation - .0001f)
                {
                    var n = distance > .0001f ? offset / distance : Vector2.Normalize(remaining);
                    // Spawn/restart overlaps may leave freely, but cannot deepen penetration.
                    if (Vector2.Dot(remaining, n) >= 0) continue;
                    if (earliest > 0) { earliest = 0; normal = n; }
                    continue;
                }
                float a = remaining.LengthSquared(), b = Vector2.Dot(offset, remaining);
                float c = offset.LengthSquared() - BodySeparation * BodySeparation;
                float discriminant = b * b - a * c;
                if (b >= 0 || discriminant < 0) continue;
                float t = (-b - MathF.Sqrt(discriminant)) / a;
                if (t >= 0 && t < earliest)
                {
                    earliest = t;
                    normal = Vector2.Normalize(position + remaining * t - body.Position2D);
                }
            }
            position += remaining * (earliest >= 1 ? 1 : Math.Max(0, earliest - .0001f));
            if (earliest >= 1) break;
            contact = true;
            remaining *= 1 - earliest;
            remaining -= normal * Math.Min(0, Vector2.Dot(remaining, normal));
        }
        position = new(Math.Clamp(position.X, 0, Pitch.Length), Math.Clamp(position.Y, 0, Pitch.Width));
        return new(position, (position - start) / elapsed, velocity, contact);
    }

    public static Vector2 AvoidBodies(Player actor, Vector2 velocity, IEnumerable<Player> obstacles)
    {
        float speed = velocity.Length();
        if (speed < .01f) return velocity;
        var direction = velocity / speed;
        var side = new Vector2(-direction.Y, direction.X);
        var obstacle = obstacles.Where(p => p != actor && p.IsActiveOnPitch)
            .Select(p => new { Player = p, Offset = p.Position2D - actor.Position2D })
            .Where(p => Vector2.Dot(p.Offset, direction) > 0 && Vector2.Dot(p.Offset, direction) < Math.Clamp(speed * .4f, .8f, 2)
                && Math.Abs(Vector2.Dot(p.Offset, side)) < .65f)
            .OrderBy(p => p.Offset.LengthSquared()).ThenBy(p => p.Player.Id).FirstOrDefault();
        if (obstacle == null) return velocity;
        float sign = Vector2.Dot(obstacle.Offset, side) > .01f ? -1 : 1;
        return Vector2.Normalize(direction * .45f + side * sign) * speed;
    }

    /// <summary>Optional steering helps reach the ball side. It never tackles, accelerates or teleports.</summary>
    public static Vector2 ContainmentDirection(Player defender, Player carrier, Vector2 input, float speed, float strength)
    {
        if (input.LengthSquared() < .0001f || strength <= 0) return input;
        input = Vector2.Normalize(input);
        var toCarrier = carrier.Position2D - defender.Position2D;
        float distance = toCarrier.Length();
        if (distance < .0001f || Vector2.Dot(input, toCarrier / distance) < .85f) return input;
        var forward = carrier.Velocity.LengthSquared() > .36f ? Vector2.Normalize(carrier.Velocity) : carrier.FacingDirection;
        if (forward.LengthSquared() < .0001f) forward = Vector2.UnitX; else forward = Vector2.Normalize(forward);
        var target = carrier.Position2D + forward * .75f;
        var toTarget = target - defender.Position2D;
        float along = Vector2.Dot(toCarrier, forward);
        if (along > .15f && Vector2.Distance(defender.Position2D + forward * along, carrier.Position2D) < .6f)
        {
            var side = defender.Position2D - carrier.Position2D;
            side -= forward * Vector2.Dot(side, forward);
            side = side.LengthSquared() > .0001f ? Vector2.Normalize(side) : new Vector2(-forward.Y, forward.X);
            target = carrier.Position2D + side * 1.05f + forward * .3f;
        }
        else if (toTarget.LengthSquared() < .0625f) return Vector2.Zero;
        else target += carrier.Velocity * Math.Clamp(toTarget.Length() / Math.Max(speed, .1f), 0, .4f);
        var desired = target - defender.Position2D;
        if (desired.LengthSquared() < .0001f) return Vector2.Zero;
        var result = Vector2.Lerp(input, Vector2.Normalize(desired), Math.Clamp(strength + .5f * (1 - distance / 6), 0, .9f));
        return result.LengthSquared() > .0001f ? Vector2.Normalize(result) : Vector2.Zero;
    }
}
