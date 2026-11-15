using System.Numerics;

namespace AccessibleFootball2026.Core.Simulation.Play;

/// <summary>Ball flight in metres and seconds. No ownership, scoring or commentary side effects.</summary>
public static class BallFlight
{
    public const float Gravity = 9.81f;
    public const float RollingDeceleration = 2.5f;
    public const float Restitution = .6f;
    // Equivalent to the original nominal 50 Hz air loss, now expressed per second.
    public static readonly float AirDragPerSecond = -MathF.Log(.985f) / .02f;
    public const float MaxStep = .005f;

    public readonly record struct Frame(Vector3 Position, Vector3 Velocity, bool Airborne);
    public static bool Finite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
    public static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);

    public static Frame Advance(Vector3 position, Vector3 velocity, float curve, float elapsed)
    {
        if (!Finite(position) || !Finite(velocity) || !float.IsFinite(curve) || !float.IsFinite(elapsed) || elapsed <= 0)
            return new(position, velocity, position.Z > 0 || velocity.Z > 0);
        position.Z = Math.Max(0, position.Z);
        // Bounded substeps resolve ground contacts without a frame-rate-dependent damping multiplier.
        int count = Math.Max(1, (int)MathF.Ceiling(elapsed / MaxStep));
        float dt = elapsed / count;
        for (int i = 0; i < count; i++)
        {
            bool flying = position.Z > .0001f || velocity.Z > .05f;
            if (flying)
            {
                float drag = MathF.Exp(-AirDragPerSecond * dt);
                var flat = Rotate(new Vector2(velocity.X, velocity.Y), curve * dt);
                var nextFlat = flat * drag;
                position.X += (flat.X + nextFlat.X) * .5f * dt;
                position.Y += (flat.Y + nextFlat.Y) * .5f * dt;
                position.Z += velocity.Z * dt - .5f * Gravity * dt * dt;
                velocity = new Vector3(nextFlat, velocity.Z - Gravity * dt);
                if (position.Z <= 0)
                {
                    position.Z = 0;
                    float rebound = Math.Max(0, -velocity.Z * Restitution);
                    velocity = new Vector3(nextFlat * .85f, rebound >= .6f ? rebound : 0);
                }
            }
            else
            {
                var flat = Rotate(new Vector2(velocity.X, velocity.Y), curve * dt);
                float speed = flat.Length();
                float nextSpeed = Math.Max(0, speed - RollingDeceleration * dt);
                var direction = speed > .00001f ? flat / speed : Vector2.Zero;
                position += new Vector3(direction * ((speed + nextSpeed) * .5f * dt), 0);
                velocity = new Vector3(direction * nextSpeed, 0);
            }
        }
        return new(position, velocity, position.Z > .0001f || velocity.Z > .05f);
    }

    private static Vector2 Rotate(Vector2 value, float angle)
    {
        float c = MathF.Cos(angle), s = MathF.Sin(angle);
        return new(value.X * c - value.Y * s, value.X * s + value.Y * c);
    }
}
