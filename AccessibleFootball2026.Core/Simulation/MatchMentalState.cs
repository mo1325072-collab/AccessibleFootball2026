using AccessibleFootball2026.Core.Models;
namespace AccessibleFootball2026.Core.Simulation;
public partial class MatchEngine
{
    private readonly Dictionary<Player,float> _confidence = new();
    public float ComposureOf(Player player)
    {
        float confidence=_confidence.GetValueOrDefault(player,75);
        float fatiguePenalty=Math.Max(0,40-player.CurrentStamina)*.5f;
        return Math.Clamp(confidence-fatiguePenalty,10,100);
    }
    public float ConcentrationOf(Player player)=>ComposureOf(player);
    public float DefensiveReactionMultiplier(Player player)=>.65f+ConcentrationOf(player)*.0035f;
    private float? _goalOpponentConcentration;
    private void UpdateConfidence(MatchEvent e)
    {
        if(e.Type==MatchEventType.GoalScored && e.GoalData is {} conceded)
        {
            var defending=conceded.ScoringTeam==HomeTeam?AwayTeam:HomeTeam;
            _goalOpponentConcentration=defending.Players.Where(p=>p.IsActiveOnPitch).Select(ConcentrationOf).DefaultIfEmpty(75).Average();
            foreach(var player in defending.Players.Where(p=>p.IsActiveOnPitch))
                _confidence[player]=Math.Clamp(_confidence.GetValueOrDefault(player,75)-6,10,100);
        }
        Player? actor=_passPerformanceActor ?? (e.GoalData is { IsOwnGoal:false } goal?goal.Scorer:e.SubjectPlayer);
        if(actor==null)return;
        float change=e.Type switch
        {
            MatchEventType.GoalScored=>10,
            MatchEventType.MissedChance=>-8,
            MatchEventType.PassIntercepted=>-3,
            MatchEventType.GoalkeeperSave=>3,
            _=>0
        };
        _confidence[actor]=Math.Clamp(_confidence.GetValueOrDefault(actor,75)+change,10,100);
    }
}
