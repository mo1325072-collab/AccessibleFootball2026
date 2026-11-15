using System.Numerics;
using AccessibleFootball2026.Core.Input;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation.Play;

namespace AccessibleFootball2026.Core.Simulation;

public partial class MatchEngine
{
    // All locomotion uses the same swept-body solver. Intended motion, not separation correction,
    // is retained as referee evidence. Returning a position never transfers ball ownership.
    private void MovePlayer(Player player, Vector2 velocity, float dt)
    {
        float maxSpeed = player.Speed / 7.5f * FatigueMovementMultiplier(player) * 1.6f;
        if (velocity.LengthSquared() > maxSpeed * maxSpeed) velocity = Vector2.Normalize(velocity) * maxSpeed;
        var motion = PlayerMotion.Advance(player, velocity, dt, HomeTeam.Players.Concat(AwayTeam.Players));
        player.Position2D = motion.Position;
        player.Velocity = motion.Velocity;
        _contactMotion[player] = motion.IntendedVelocity;
    }

    private void MoveAutonomousPlayer(Player player, Vector2 velocity, float dt)
    {
        MovePlayer(player, PlayerMotion.AvoidBodies(player, velocity, HomeTeam.Players.Concat(AwayTeam.Players)), dt);
    }

    private Vector2 MoveUser(HashSet<GameAction> actions, bool hasPossession, float dt)
    {
        var actor = ActiveUserPlayer;
        var axis = new Vector2((actions.Contains(GameAction.MoveRight) ? 1 : 0) - (actions.Contains(GameAction.MoveLeft) ? 1 : 0),
            (actions.Contains(GameAction.MoveDown) ? 1 : 0) - (actions.Contains(GameAction.MoveUp) ? 1 : 0));
        if (axis.LengthSquared() > 1) axis = Vector2.Normalize(axis);
        if (_slideDurationTimer > 0 && _slidingPlayer == actor)
        {
            MovePlayer(actor, _slideImpulseVelocity, dt);
            return axis;
        }
        bool carrier = hasPossession && Ball.Owner == actor;
        if (carrier && axis.Y != 0) AdjustAimingAngle(axis.Y * .9f * dt);
        float speed = actor.Speed / 7.5f * FatigueMovementMultiplier(actor) * (actions.Contains(GameAction.Sprint) ? 1.6f : 1);
        var direction = axis;
        bool challenge = _tackleCooldownTimer <= 0 && (actions.Contains(GameAction.StandTackle) || actions.Contains(GameAction.SlideTackle));
        if (!hasPossession && !challenge && Ball.Owner is { } opponent && AwayTeam.Players.Contains(opponent))
        {
            direction = PlayerMotion.ContainmentDirection(actor, opponent, axis, speed, _defensiveApproachAssistStrength);
            if (Vector2.Distance(actor.Position2D, opponent.Position2D) <= 2 && axis.LengthSquared() > 0)
            {
                var face = opponent.Position2D - actor.Position2D;
                if (face.LengthSquared() > .0001f) actor.FacingDirection = Vector2.Normalize(face);
            }
            else if (direction.LengthSquared() > .0001f) actor.FacingDirection = direction;
        }
        else if ((!carrier || axis.Y == 0) && direction.LengthSquared() > .0001f) actor.FacingDirection = direction;
        MovePlayer(actor, direction * speed, dt);
        if (axis.LengthSquared() > 0)
        {
            _boundaryTimer += dt;
            bool boundary = actor.Position2D.Y <= 2.5f || actor.Position2D.Y >= Pitch.Width - 2.5f || actor.Position2D.X <= 2.5f || actor.Position2D.X >= Pitch.Length - 2.5f;
            if (boundary && _boundaryTimer >= .8f) { _boundaryTimer = 0; OnNearBoundary?.Invoke(); }
        }
        return axis;
    }
}
