using System.Numerics;
using AccessibleFootball2026.Core.Models;
namespace AccessibleFootball2026.Core.Simulation;
public partial class MatchEngine
{
    private Player? _onTargetDeflectedShooter;
    private Player? _onTargetDeflectedAssister;
    private float _deflectionRecordedAt;
    // Predict the unchanged incoming ball using the game's actual physics, not a defender's identity.
    private void RememberShotDeflection()
    {
        if(!_isShotInProgress || _shotShooter==null)return;
        var incoming=new Ball{Position=Ball.Position,Velocity=Ball.Velocity,CurveRadiansPerSecond=Ball.CurveRadiansPerSecond};
        bool towardsRight=_shotTargetGoalX>Pitch.HalfLength;
        for(int i=0;i<600;i++)
        {
            incoming.Update(1f/60);
            if(towardsRight?incoming.Position.X>=Pitch.Length:incoming.Position.X<=0)
            {
                if(incoming.Position.Y>=Pitch.GoalYMin && incoming.Position.Y<=Pitch.GoalYMax && incoming.Position.Z<=Pitch.GoalHeight)
                {
                    _onTargetDeflectedShooter=_shotShooter;
                    _onTargetDeflectedAssister=PreviousKicker;
                    _deflectionRecordedAt=_elapsedSeconds;
                }
                return;
            }
            if(incoming.Velocity.Length()<.1f)return;
        }
    }
}
