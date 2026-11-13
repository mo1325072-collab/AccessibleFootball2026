using System.Numerics;
using AccessibleFootball2026.Core.Models;
namespace AccessibleFootball2026.Core.Simulation;
public partial class MatchEngine
{
    private Player? _runUpPlayer;
    private float _runUpRemaining;
    private string? _executingRunUp;
    private Vector2 _runUpBallSpot;
    public bool BeginSlowRunUp()
    {
        if(State!=MatchState.InPlay || Ball.Owner!=ActiveUserPlayer || _runUpPlayer!=null)return false;
        if(Restart?.Kind is RestartKind.IndirectFreeKick or RestartKind.Corner)return false;
        _runUpPlayer=ActiveUserPlayer;
        _runUpBallSpot=ActiveUserPlayer.Position2D;
        var direction=ActiveUserPlayer.FacingDirection;
        if(direction.LengthSquared()<.01f)direction=new Vector2(UserAttackingRight?1:-1,0);
        ActiveUserPlayer.Position2D-=Vector2.Normalize(direction)*.8f;
        Ball.Owner=null;Ball.Position=new Vector3(_runUpBallSpot,0);Ball.Velocity=Vector3.Zero;
        _runUpRemaining=1.2f;
        return true;
    }
    private bool UpdateRunUp(float dt)
    {
        if(_runUpPlayer is not {} player)return false;
        if((Ball.Owner!=null && Ball.Owner!=player) || Vector2.Distance(Ball.Position2D,_runUpBallSpot)>1 || !player.IsActiveOnPitch || !HomeTeam.Players.Contains(player)){_runUpPlayer=null;return false;}
        // The player takes real short steps toward a stationary loose ball. Opponents remain live in open play.
        var delta=_runUpBallSpot-player.Position2D;
        float step=Math.Min(delta.Length(),dt*(.8f/1.2f));
        if(delta.LengthSquared()>.0001f)player.Position2D+=Vector2.Normalize(delta)*step;
        _runUpRemaining-=dt;
        if(_runUpRemaining>0)return true;
        _runUpPlayer=null;
        Ball.CatchOrTackle(player);
        _executingRunUp="slow";
        if(Restart!=null)TakeRestartShot();else TryExecuteTechnique(ShotTechnique.Driven);
        _executingRunUp=null;
        return true;
    }
}
