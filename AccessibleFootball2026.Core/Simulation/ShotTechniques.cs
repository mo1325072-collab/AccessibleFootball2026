using System.Numerics;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Core.Simulation;

public enum ShotTechnique { Driven, Header, Bicycle, Lob, Volley, Curled }

public partial class MatchEngine
{
    private float? _lastShotTargetY;
    private ShotTechnique _currentShotTechnique = ShotTechnique.Driven;

    /// <summary>Technique is a real physical action. A failed height/range check never emits its commentary event.</summary>
    public bool TryExecuteTechnique(ShotTechnique technique)
    {
        if (State != MatchState.InPlay || !ActiveUserPlayer.IsActiveOnPitch) return false;
        var shooter = ActiveUserPlayer;
        float range = Vector2.Distance(shooter.Position2D, Ball.Position2D);
        bool aerial = technique is ShotTechnique.Header or ShotTechnique.Bicycle or ShotTechnique.Volley;
        if (aerial)
        {
            if (Ball.Owner != null || range > 2.8f) return false;
            var (min, max) = technique switch
            {
                ShotTechnique.Header => (1.1f, 2.8f),
                ShotTechnique.Bicycle => (.9f, 2.1f),
                _ => (.25f, 1.4f)
            };
            if (Ball.Position.Z < min || Ball.Position.Z > max) return false;
        }
        else if (Ball.Owner != shooter) return false;

        RecordBallTouch(shooter);
        SnapshotOffsideAtKick(shooter);
        Vector2 goal = UserAttackingRight ? Pitch.RightGoalCenter : Pitch.LeftGoalCenter;
        float aim = Math.Clamp(shooter.FacingDirection.Y, -.9f, .9f);
        goal.Y = Math.Clamp(goal.Y + aim * 3.5f, Pitch.GoalYMin + .25f, Pitch.GoalYMax - .25f);
        float spread = (100 - shooter.Shooting) * (aerial ? .06f : .035f) + Math.Max(0,50-ComposureOf(shooter))*.07f;
        goal.Y += ((float)Random.Shared.NextDouble() * 2 - 1) * spread;
        float distance = Vector2.Distance(shooter.Position2D, goal);
        float speed = technique switch
        {
            ShotTechnique.Header => 18 + shooter.Physical * .07f,
            ShotTechnique.Bicycle => 22 + shooter.Shooting * .08f,
            ShotTechnique.Lob => Math.Clamp(distance * 1.3f, 12, 24),
            ShotTechnique.Volley => 27 + shooter.Shooting * .10f,
            ShotTechnique.Curled => 25 + shooter.Shooting * .08f,
            _ => 30 + shooter.Shooting * .12f
        };
        float vertical = technique switch
        {
            ShotTechnique.Header => -.8f,
            ShotTechnique.Bicycle => 1.0f,
            ShotTechnique.Lob => Math.Clamp(distance * .32f, 4, 8),
            ShotTechnique.Volley => .6f,
            ShotTechnique.Curled => 1.6f,
            _ => 1.8f
        };
        _lastShotTargetY = goal.Y;
        Ball.Kick(goal - shooter.Position2D, speed, vertical, shooter);
        if (technique == ShotTechnique.Curled)
            Ball.CurveRadiansPerSecond = (aim >= 0 ? -1 : 1) * .12f;
        _currentShotTechnique = technique;
        _isShotInProgress = true;
        _shotTargetGoalX = UserAttackingRight ? Pitch.Length : 0;
        _shotGkResolved = false;
        _shotShooter = shooter;
        _kickoffProtectionTimer = 0;
        Publish(new MatchEvent(MatchEventType.ShotTaken, $"محاولة على المرمى من {shooter.Name}", CurrentZone,
            spatialPan: CalculateBallPan(), subjectPlayer: shooter), ("shot.contact", technique == ShotTechnique.Header ? "head" : technique == ShotTechnique.Curled ? "insideFoot" : "instep"));
        return true;
    }

    private string TechniqueKey => _currentShotTechnique switch
    {
        ShotTechnique.Header => "header", ShotTechnique.Bicycle => "bicycle", ShotTechnique.Lob => "lob",
        ShotTechnique.Volley => "volley", ShotTechnique.Curled => "curled", _ => "openPlay"
    };
}
