using System.Numerics;
using AccessibleFootball2026.Core.Models;
namespace AccessibleFootball2026.Core.Simulation;
public partial class MatchEngine
{
    private Player? _foulVictim;
    private sealed record AdvantageIncident(Player Victim,Team Team,Vector2 Spot,float Deadline,int Score);
    private AdvantageIncident? _advantage;

    private bool UpdateAdvantage()
    {
        if (_advantage is not { } a) return false;
        if(a.Team.Score>a.Score) { _advantage=null;return false; }
        bool lost=Ball.Owner!=null && TeamOf(Ball.Owner)!=a.Team;
        if(!lost && _elapsedSeconds<a.Deadline) return false;
        bool right=a.Team==HomeTeam?UserAttackingRight:!UserAttackingRight;
        float progress=right?Ball.Position.X-a.Spot.X:a.Spot.X-Ball.Position.X;
        _advantage=null;
        if(!lost && progress>5) return false;
        Publish(new MatchEvent(MatchEventType.FoulCommitted,"لم تتحقق إتاحة الفرصة، العودة إلى الخطأ",CurrentZone,subjectPlayer:a.Victim),
            ("referee.advantageRecalled",true));
        AwardFoulRestart(a.Victim,a.Spot);
        return true;
    }

    private bool TryPlayAdvantage(Player carrier,bool severe,bool dogso)
    {
        var team=TeamOf(carrier);
        if(team==null || severe || dogso || Ball.Owner!=carrier)return false;
        bool right=team==HomeTeam?UserAttackingRight:!UserAttackingRight;
        float dist=Vector2.Distance(carrier.Position2D,right?Pitch.RightGoalCenter:Pitch.LeftGoalCenter);
        if(dist>45)return false;
        _advantage=new AdvantageIncident(carrier,team,carrier.Position2D,_elapsedSeconds+2.5f,team.Score);
        Publish(new MatchEvent(MatchEventType.Advantage,"الحكم يتيح الفرصة",CurrentZone,subjectPlayer:carrier),("referee.advantage",true));
        return true;
    }
}
