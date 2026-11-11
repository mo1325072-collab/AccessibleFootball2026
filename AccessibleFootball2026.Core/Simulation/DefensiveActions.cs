using System.Numerics;
using AccessibleFootball2026.Core.Models;
namespace AccessibleFootball2026.Core.Simulation;
public partial class MatchEngine
{
    public bool ExecuteClearance()
    {
        var player = ActiveUserPlayer;
        if (State != MatchState.InPlay || Ball.Owner != player) return false;
        Vector2 direction=player.FacingDirection;
        if(direction.LengthSquared()<.01f)direction=new Vector2(UserAttackingRight?1:-1,0);
        direction=Vector2.Normalize(direction);
        var opponents=AwayTeam.Players.Where(p=>p.IsActiveOnPitch).ToArray();
        bool pressured=opponents.Any(p=>Vector2.Distance(p.Position2D,player.Position2D)<3);
        bool poor=pressured&&ComposureOf(player)<45;
        float speed=poor?12:32;
        Vector2 projected=player.Position2D+direction*(poor?8:25);
        bool danger=(UserAttackingRight?projected.X<30:projected.X>Pitch.Length-30)&&projected.Y>0&&projected.Y<Pitch.Width;
        RecordBallTouch(player);
        _lastTouchMethod = "clearance";
        SnapshotOffsideAtKick(player);
        Ball.Kick(direction,speed,poor?.5f:3.5f,player);
        _shotContextValid = false;
        _commentaryPass = null;
        _pendingClearance=player;_clearanceStarted=_elapsedSeconds;
        Publish(new MatchEvent(MatchEventType.BallKicked,"محاولة إبعاد الكرة",CurrentZone,subjectPlayer:player),("action.kind","clearance"));
        return true;
    }

    private Player? _pendingClearance;
    private float _clearanceStarted;
    private void UpdateClearanceOutcome()
    {
        if(_pendingClearance is not {} player)return;
        bool danger=(UserAttackingRight?Ball.Position.X<30:Ball.Position.X>Pitch.Length-30)&&Ball.Position.Y>0&&Ball.Position.Y<Pitch.Width;
        bool recaptured=Ball.Owner!=null && Ball.Owner!=player;
        bool outOfPlay=Pitch.IsOutOfBounds(Ball.Position,out _,out _);
        if(!danger||recaptured||outOfPlay||_elapsedSeconds-_clearanceStarted>4)
        {
            _pendingClearance=null;
            Publish(new MatchEvent(MatchEventType.Clearance,danger&&!outOfPlay?"الإبعاد لم ينه الخطر":"الكرة ابتعدت عن منطقة الخطر",CurrentZone,subjectPlayer:player),
                ("clearance.dangerContinues",danger&&!outOfPlay));
        }
    }

    private bool CheckShotBlock(Vector3 previousBall)
    {
        if (!_isShotInProgress || _shotShooter == null) return false;
        var attack = TeamOf(_shotShooter);
        if (attack == null) return false;
        var defence = attack == HomeTeam ? AwayTeam : HomeTeam;
        Vector3 travel = Ball.Position-previousBall;
        float lengthSquared = travel.LengthSquared();
        foreach(var defender in defence.Players.Where(p=>p.IsActiveOnPitch && p.Role!=PlayerRole.Goalkeeper))
        {
            Vector3 center = new(defender.Position2D,.8f);
            float t=lengthSquared>.0001f?Math.Clamp(Vector3.Dot(center-previousBall,travel)/lengthSquared,0,1):0;
            Vector3 contact=previousBall+travel*t;
            if(contact.Z<0 || contact.Z>1.8f || Vector2.Distance(new Vector2(contact.X,contact.Y),defender.Position2D)>.65f) continue;
            Ball.Position=contact;
            RememberShotDeflection();
            var normal=contact-center;
            if(normal.LengthSquared()<.0001f)normal=-Vector3.Normalize(Ball.Velocity);
            else normal=Vector3.Normalize(normal);
            // A square block rebounds; a glancing contact can keep moving goalward.
            // Direction comes from contact geometry, not an own-goal flag.
            var reflected=Vector3.Reflect(Ball.Velocity,normal);
            Ball.Velocity=(reflected*.65f+Ball.Velocity*.35f)*.55f;
            Ball.Velocity=new Vector3(Ball.Velocity.X,Ball.Velocity.Y,Math.Max(.25f,Ball.Velocity.Z));
            _isShotInProgress=false;
            RecordBallTouch(defender, deliberatelyControlled:false);
            _lastTouchMethod="deflection";
            Publish(new MatchEvent(MatchEventType.ShotBlocked,"المدافع يعترض التسديدة",CurrentZone,subjectPlayer:defender),
                ("tackle.deflected",true),("tackle.retained",false));
            return true;
        }
        return false;
    }
}
