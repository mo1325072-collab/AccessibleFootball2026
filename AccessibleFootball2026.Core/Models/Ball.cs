using System.Numerics;
using AccessibleFootball2026.Core.Simulation.Play;

namespace AccessibleFootball2026.Core.Models;

public enum BallState { Free, Controlled, InFlight, OutOfBounds, InGoal }

/// <summary>The ball owns its control transition. Player flags cannot remain on an old owner.</summary>
public class Ball
{
    private Player? _owner;
    public Vector3 Position { get; set; } = new(52.5f, 34, 0);
    public Vector2 Position2D => new(Position.X, Position.Y);
    public Vector3 Velocity { get; set; }
    public float CurveRadiansPerSecond { get; set; }
    public BallState State { get; set; } = BallState.Free;
    public const float GroundDeceleration = BallFlight.RollingDeceleration;

    public event Action? ControlChanged;
    public Player? Owner
    {
        get => _owner;
        set
        {
            if (ReferenceEquals(_owner, value)) { if (value != null) SynchronizeControl(); return; }
            if (_owner != null) _owner.HasBall = false;
            _owner = value;
            if (value != null)
            {
                value.HasBall = true;
                SynchronizeControl();
            }
            else if (State == BallState.Controlled) State = BallState.Free;
            ControlChanged?.Invoke();
        }
    }

    public static Vector2 FeetOf(Player player)
    {
        var facing = player.FacingDirection;
        if (!BallFlight.Finite(facing) || facing.LengthSquared() < .0001f) facing = Vector2.UnitX;
        else facing = Vector2.Normalize(facing);
        return player.Position2D + facing * .35f;
    }

    private void SynchronizeControl()
    {
        if (_owner == null) return;
        Position = new Vector3(FeetOf(_owner), 0);
        Velocity = Vector3.Zero;
        CurveRadiansPerSecond = 0;
        State = BallState.Controlled;
    }

    public void UpdatePhysics(float deltaTime) => Update(deltaTime);
    public void Update(float deltaTime)
    {
        if (!float.IsFinite(deltaTime) || deltaTime <= 0) return;
        if (_owner != null)
        {
            if (_owner.IsActiveOnPitch) { SynchronizeControl(); return; }
            Owner = null;
        }
        if (State is BallState.OutOfBounds or BallState.InGoal) return;
        var result = BallFlight.Advance(Position, Velocity, CurveRadiansPerSecond, deltaTime);
        Position = result.Position;
        Velocity = result.Velocity;
        State = result.Airborne ? BallState.InFlight : BallState.Free;
    }

    public void Kick(Vector2 direction, float horizontalSpeed, float verticalSpeed, Player? kicker = null)
    {
        if (!BallFlight.Finite(direction) || !float.IsFinite(horizontalSpeed) || !float.IsFinite(verticalSpeed) || horizontalSpeed < 0) return;
        if (_owner != null) SynchronizeControl();
        if (direction.LengthSquared() > .000001f) direction = Vector2.Normalize(direction);
        else if (horizontalSpeed > 0) return;
        Owner = null;
        CurveRadiansPerSecond = 0;
        Velocity = new Vector3(direction * horizontalSpeed, verticalSpeed);
        State = verticalSpeed > .05f || Position.Z > .0001f ? BallState.InFlight : BallState.Free;
    }

    // This is a committed control transition. Eligibility belongs to PossessionArbiter, not this data model.
    public void CatchOrTackle(Player newOwner)
    {
        if (!newOwner.IsActiveOnPitch) return;
        Owner = newOwner;
    }
}
