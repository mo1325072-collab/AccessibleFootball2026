using System.Numerics;
using AccessibleFootball2026.Core.Input;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Core.Simulation;

public enum RestartKind { FreeKick, Penalty, IndirectFreeKick, Corner, ThrowIn, GoalKick }
public sealed record PendingRestart(RestartKind Kind, Team Team, Player Taker, Vector2 Spot);

public partial class MatchEngine
{
    public PendingRestart? Restart { get; private set; }
    private float _restartReadySeconds;
    private string? _nextShotMethod;

    /// <summary>Stop play and place an actual restart. Penalty area is measured in the attacking direction.</summary>
    public void AwardFoulRestart(Player victim, Vector2? foulSpot = null, bool indirect = false)
    {
        var team = TeamOf(victim);
        if (team == null || !victim.IsActiveOnPitch) return;
        bool right = team == HomeTeam ? UserAttackingRight : !UserAttackingRight;
        var incidentPosition=foulSpot??victim.Position2D;
        bool penalty = !indirect && (right ? incidentPosition.X >= Pitch.Length - Pitch.PenaltyBoxDepth : incidentPosition.X <= Pitch.PenaltyBoxDepth)
            && incidentPosition.Y >= Pitch.PenaltyYMin && incidentPosition.Y <= Pitch.PenaltyYMax;
        float boxLine=right?Pitch.Length-Pitch.PenaltyBoxDepth:Pitch.PenaltyBoxDepth;
        bool boundaryDoubt=!indirect && Math.Abs(incidentPosition.X-boxLine)<=.5f && incidentPosition.Y>=Pitch.PenaltyYMin && incidentPosition.Y<=Pitch.PenaltyYMax;
        if((penalty||boundaryDoubt) && BeginVideoReview(VideoReviewCategory.Penalty,()=>AwardFoulRestart(victim,incidentPosition,indirect),
            penalty?T($"قرار الفار: ركلة جزاء لصالح {team.Name}.",$"VAR decision: penalty awarded to {team.Name}."):
                T("قرار الفار: المخالفة خارج منطقة الجزاء. ركلة حرة وليست ركلة جزاء.","VAR decision: foul outside the penalty area. Free kick, no penalty."),penalty?"penalty":"noPenalty"))return;
        var designated = penalty ? team.PenaltyTaker : team.FreeKickTaker;
        var taker = designated?.IsActiveOnPitch == true ? designated : team.Players.FirstOrDefault(p => p.IsActiveOnPitch && p.Role != PlayerRole.Goalkeeper);
        if (taker == null) return;
        var spot = penalty ? new Vector2(right ? Pitch.Length - 11 : 11, Pitch.Width / 2) : incidentPosition;
        spot = new Vector2(Math.Clamp(spot.X, .5f, Pitch.Length - .5f), Math.Clamp(spot.Y, .5f, Pitch.Width - .5f));
        Restart = new PendingRestart(indirect ? RestartKind.IndirectFreeKick : penalty ? RestartKind.Penalty : RestartKind.FreeKick, team, taker, spot);
        _restartReadySeconds = 0;
        _isShotInProgress = false;
        _shotContextValid = false;
        _commentaryPass = null;
        ActivePassTransit = null;
        taker.Position2D = spot;
        taker.FacingDirection = new Vector2(right ? 1 : -1, 0);
        Ball.CatchOrTackle(taker);
        RecordBallTouch(taker);
        if (team == HomeTeam) ActiveUserPlayer = taker;
        foreach (var defender in (team == HomeTeam ? AwayTeam : HomeTeam).Players.Where(p => p.IsActiveOnPitch && p.Role != PlayerRole.Goalkeeper))
        {
            var delta = defender.Position2D - spot;
            if (delta.Length() < 9.15f)
            {
                if (delta.LengthSquared() < .01f) delta = new Vector2(right ? 1 : -1, 1);
                var pos = spot + Vector2.Normalize(delta) * 9.2f;
                defender.Position2D = new Vector2(Math.Clamp(pos.X, 0, Pitch.Length), Math.Clamp(pos.Y, 0, Pitch.Width));
            }
        }
        float distance = Vector2.Distance(spot, right ? Pitch.RightGoalCenter : Pitch.LeftGoalCenter);
        Publish(new MatchEvent(penalty ? MatchEventType.PenaltyAwarded : MatchEventType.FreeKickAwarded,
            indirect ? "ركلة حرة غير مباشرة" : penalty ? "ركلة جزاء" : "ركلة حرة", CurrentZone, subjectPlayer: taker),
            ("restart.phase", "ready"), ("foul.distanceToGoal", distance));
        if (!penalty) Publish(new MatchEvent(MatchEventType.FreeKickReady, "الاستعداد للركلة الحرة", CurrentZone, subjectPlayer: taker), ("restart.phase", "ready"), ("foul.distanceToGoal", distance));
    }

    private void ReassignUnavailableRestartTaker(PendingRestart restart)
    {
        var team = restart.Team;
        Player? designated = restart.Kind switch
        {
            RestartKind.Penalty => team.PenaltyTaker,
            RestartKind.FreeKick or RestartKind.IndirectFreeKick => team.FreeKickTaker,
            RestartKind.Corner => team.CornerTaker,
            RestartKind.GoalKick => team.Players.FirstOrDefault(p => p.IsActiveOnPitch && p.Role == PlayerRole.Goalkeeper),
            _ => null
        };
        var taker = designated != null && team.Players.Contains(designated) && designated.IsActiveOnPitch ? designated :
            team.Players.FirstOrDefault(p => p.IsActiveOnPitch && p.Role != PlayerRole.Goalkeeper) ??
            team.Players.FirstOrDefault(p => p.IsActiveOnPitch);
        if (taker == null) { Restart = null; return; }
        Restart = restart with { Taker = taker };
        taker.Position2D = restart.Spot;
        Ball.CatchOrTackle(taker);
        RecordBallTouch(taker);
        if (team == HomeTeam) ActiveUserPlayer = taker;
    }

    private bool UpdateRestart(float dt)
    {
        if (Restart is not { } restart) return false;
        _restartReadySeconds += dt;
        if (restart.Team == HomeTeam && restart.Kind is RestartKind.ThrowIn or RestartKind.GoalKick)
        { Restart = null; return false; }
        var actions = _inputHandler.PollCurrentActions(restart.Team == HomeTeam);
        if (actions.Contains(GameAction.TacticalRadar)) ReportRequestedRadar();
        if (actions.Contains(GameAction.RequestLineup)) RequestLineup();
        if (actions.Contains(GameAction.Pause)) { PauseMatch(); return true; }
        if (restart.Kind is RestartKind.ThrowIn or RestartKind.GoalKick)
        {
            if (restart.Team == HomeTeam) { Restart = null; return false; }
            if (_restartReadySeconds < 2.5f) return true;
            var receiver = restart.Team.Players.FirstOrDefault(p => p.IsActiveOnPitch && p != restart.Taker && p.Role != PlayerRole.Goalkeeper);
            if (receiver != null)
            {
                Restart = null;
                RecordBallTouch(restart.Taker);
                SnapshotOffsideAtKick(restart.Taker);
                Ball.Kick(receiver.Position2D - restart.Spot, 18, restart.Kind == RestartKind.ThrowIn ? 1 : 2, restart.Taker);
                BeginCommentaryPass(restart.Taker, receiver, receiver.Position2D, "short", "high");
                Publish(new MatchEvent(MatchEventType.BallKicked, "تنفيذ استئناف اللعب", CurrentZone, subjectPlayer: restart.Taker));
            }
            return true;
        }
        if (restart.Team == HomeTeam)
        {
            if (actions.Contains(GameAction.MoveUp)) AdjustAimingAngle(-dt);
            if (actions.Contains(GameAction.MoveDown)) AdjustAimingAngle(dt);
            if(restart.Kind==RestartKind.Corner)
            {
                if(actions.Contains(GameAction.Cross)){Restart=null;ExecuteCross();}
                else if(actions.Contains(GameAction.GroundCross)){Restart=null;ExecuteCross(CrossStyle.Ground);}
                else if(actions.Contains(GameAction.FarPostCross)){Restart=null;ExecuteCross(actions.Contains(GameAction.Sprint)?CrossStyle.GroundFarPost:CrossStyle.FarPost);}
                else if(actions.Contains(GameAction.ShortPass)){Restart=null;ExecutePass(restart.Taker.FacingDirection);_attackOrigin="corner";}
                else if(actions.Contains(GameAction.Shoot)){Restart=null;TryExecuteTechnique(ShotTechnique.Driven);}
                return true;
            }
            if (actions.Contains(GameAction.SlowRunUp)) BeginSlowRunUp();
            else if (actions.Contains(GameAction.Shoot)) TakeRestartShot();
            else if (restart.Kind != RestartKind.Penalty && actions.Contains(GameAction.ShortPass))
            {
                Restart = null;
                Publish(new MatchEvent(MatchEventType.FreeKickTaken,"تنفيذ الركلة الحرة بالتمرير",CurrentZone,subjectPlayer:restart.Taker),("action.kind","pass"));
                ExecutePass(restart.Taker.FacingDirection);
                _attackOrigin = "freeKick";
            }
        }
        else if (_restartReadySeconds >= 2.5f)
        {
            if(restart.Kind==RestartKind.Corner)
            {
                bool right=restart.Team==HomeTeam?UserAttackingRight:!UserAttackingRight;
                Vector2 target=new(right?Pitch.Length-10:10,Pitch.Width/2);
                Restart=null;RecordBallTouch(restart.Taker);SnapshotOffsideAtKick(restart.Taker);
                Ball.Kick(target-restart.Spot,19,6.5f,restart.Taker);
                BeginCommentaryPass(restart.Taker,null,target,"cross","high",true);
                Publish(new MatchEvent(MatchEventType.BallKicked,"رفع الركنية إلى وسط المنطقة",CurrentZone,subjectPlayer:restart.Taker),
                    ("action.kind","cross"),("cross.target","center"),("cross.height","high"),("attack.origin","corner"));
            }
            else if(restart.Kind==RestartKind.IndirectFreeKick)
            {
                var target=restart.Team.Players.FirstOrDefault(p=>p.IsActiveOnPitch&&p!=restart.Taker);
                if(target!=null)
                {
                    Restart=null;
                    Publish(new MatchEvent(MatchEventType.FreeKickTaken,"تنفيذ الركلة الحرة غير المباشرة بالتمرير",CurrentZone,subjectPlayer:restart.Taker),("action.kind","pass"));
                    RecordBallTouch(restart.Taker);
                    SnapshotOffsideAtKick(restart.Taker);
                    Ball.Kick(target.Position2D-restart.Spot,18,.1f,restart.Taker);
                    BeginCommentaryPass(restart.Taker,target,target.Position2D,"short","ground");
                    Publish(new MatchEvent(MatchEventType.BallKicked,"تنفيذ الركلة غير المباشرة",CurrentZone,subjectPlayer:restart.Taker));
                }
            }
            else TakeRestartShot();
        }
        return true; // This update's time belongs to the stopped restart, never a full live-flight step too.
    }

    public bool TakeRestartShot()
    {
        if (Restart is not { } restart || State != MatchState.InPlay || restart.Kind is RestartKind.IndirectFreeKick or RestartKind.Corner) return false;
        _nextShotMethod = restart.Kind == RestartKind.Penalty ? "penalty" : "freeKick";
        _attackOrigin = _nextShotMethod;
        Restart = null;
        if(restart.Kind==RestartKind.FreeKick)Publish(new MatchEvent(MatchEventType.FreeKickTaken,"تنفيذ الركلة الحرة",CurrentZone,subjectPlayer:restart.Taker));
        if (restart.Team == HomeTeam)
        {
            bool result = TryExecuteTechnique(ShotTechnique.Driven);
            if (!result) _nextShotMethod = null;
            return result;
        }
        var shooter = restart.Taker;
        bool right = !UserAttackingRight;
        var goal = right ? Pitch.RightGoalCenter : Pitch.LeftGoalCenter;
        goal.Y += ((float)Random.Shared.NextDouble() * 2 - 1) * 3.2f;
        RecordBallTouch(shooter);
        SnapshotOffsideAtKick(shooter);
        _lastShotTargetY = goal.Y;
        Ball.Kick(goal - restart.Spot, 30 + shooter.Shooting * .08f, restart.Kind == RestartKind.Penalty ? 1.4f : 2.3f, shooter);
        _currentShotTechnique = ShotTechnique.Driven;
        _isShotInProgress = true;
        _shotTargetGoalX = goal.X;
        _shotShooter = shooter;
        _shotGkResolved = false;
        Publish(new MatchEvent(MatchEventType.ShotTaken, "تسديد الركلة الثابتة", CurrentZone, subjectPlayer: shooter));
        return true;
    }
}
