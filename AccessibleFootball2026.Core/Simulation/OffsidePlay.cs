using System.Numerics;
using AccessibleFootball2026.Core.Models;
namespace AccessibleFootball2026.Core.Simulation;

/// <summary>Law 11 in the game's point-position model: evaluate at the kick, penalise on actual involvement.</summary>
public static class OffsidePosition
{
    public static float? Margin(bool right,float attackerX,float ballX,IEnumerable<float> opponents)
    {
        var positions=(right?opponents.OrderByDescending(x=>x):opponents.OrderBy(x=>x)).ToArray();
        if(positions.Length<2 || (right?attackerX<=Pitch.HalfLength:attackerX>=Pitch.HalfLength))return null;
        float line=right?Math.Max(ballX,positions[1]):Math.Min(ballX,positions[1]);
        float margin=right?attackerX-line:line-attackerX;
        return margin>0?margin:null;
    }
}
public partial class MatchEngine
{
    private sealed record OffsideCandidate(Player Player,Team Team,float Margin,Vector2 Spot);
    private readonly Dictionary<Player,OffsideCandidate> _offsidePositions=new();
    private OffsideCandidate? _offsideOffence;
    private float _offsideReviewDeadline;
    private Player? _offsideExemptKicker;
    private void SnapshotOffsideAtKick(Player kicker)
    {
        if(_offsideOffence!=null)return;
        _offsidePositions.Clear();
        var team=TeamOf(kicker);if(team==null)return;
        if(_offsideExemptKicker==kicker){_offsideExemptKicker=null;return;}
        _offsideExemptKicker=null;
        bool right=team==HomeTeam?UserAttackingRight:!UserAttackingRight;
        var opponents=(team==HomeTeam?AwayTeam:HomeTeam).Players.Where(p=>p.IsActiveOnPitch).Select(p=>p.Position2D.X).ToArray();
        foreach(var player in team.Players.Where(p=>p.IsActiveOnPitch&&p!=kicker))
            if(OffsidePosition.Margin(right,player.Position2D.X,Ball.Position.X,opponents) is { } margin)
                _offsidePositions[player]=new OffsideCandidate(player,team,margin,player.Position2D);
    }
    private void ObserveOffsideTouch(Player player,bool deliberatelyControlled)
    {
        if(_offsidePositions.TryGetValue(player,out var candidate))
        {
            _offsideOffence=candidate with{Spot=player.Position2D};
            // Close calls can complete the attack under the game's existing VAR option.
            _offsideReviewDeadline=_elapsedSeconds+(MatchSettings.Current.AiVarEnabled && candidate.Margin<=.2f?3.5f:0);
        }
        else if(deliberatelyControlled && _offsidePositions.Count>0 && TeamOf(player)!=_offsidePositions.Values.First().Team)
        { _offsidePositions.Clear();_offsideOffence=null; }
    }
    private bool ReviewPendingOffside()
    {
        if(_offsideOffence==null || _elapsedSeconds<_offsideReviewDeadline)return false;
        PenaliseOffside(false);return true;
    }
    private void PenaliseOffside(bool goalDisallowed)
    {
        if(_offsideOffence is not { } incident)return;
        _offsideOffence=null;_offsidePositions.Clear();
        Publish(new MatchEvent(goalDisallowed?MatchEventType.GoalDisallowed:MatchEventType.Offside,
            goalDisallowed?"الهدف غير محتسب للتسلل":"تسلل بعد لمس الكرة",CurrentZone,subjectPlayer:incident.Player),
            ("offside.margin",incident.Margin),("offside.goalDisallowed",goalDisallowed));
        RequestAnalysisReplay(MatchEventType.OffsideAnalysis,Snapshot().With(("offside.margin",incident.Margin),("offside.goalDisallowed",goalDisallowed)));
        var defending=incident.Team==HomeTeam?AwayTeam:HomeTeam;
        var taker=defending.Players.FirstOrDefault(p=>p.IsActiveOnPitch&&p.Role!=PlayerRole.Goalkeeper);
        if(taker!=null)AwardFoulRestart(taker,incident.Spot,indirect:true);
    }
}
