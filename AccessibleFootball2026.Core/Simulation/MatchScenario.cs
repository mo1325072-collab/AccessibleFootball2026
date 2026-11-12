using AccessibleFootball2026.Core.Commentary;
using AccessibleFootball2026.Core.Models;
namespace AccessibleFootball2026.Core.Simulation;

public enum MatchMode { Friendly, CupFinal }
public partial class MatchEngine
{
    public MatchMode Mode { get; set; } = MatchMode.Friendly;
    public bool RivalryScenario { get; set; }
    public string WeatherCondition { get; } = "clear"; // Current virtual pitch has no rain/snow simulation.
    private bool ExtraTimeAllowed => Mode == MatchMode.CupFinal || MatchSettings.Current.ExtraTimeEnabled;
    private bool ShootoutAllowed => Mode == MatchMode.CupFinal || MatchSettings.Current.PenaltyShootoutEnabled;
    private readonly List<GoalReplay> _goalHistory = new();
    public Team? TrophyWinner { get; private set; }

    public void CompleteShootout(int homeGoals, int awayGoals)
    {
        if (State != MatchState.PenaltyShootout || homeGoals == awayGoals) return;
        State = MatchState.GameOver;
        Publish(new MatchEvent(MatchEventType.ShootoutFinished,"نهاية ركلات الترجيح"),
            ("match.finished",true),("shootout.homeScore",homeGoals),("shootout.awayScore",awayGoals),("shootout.decided",true),("shootout.winner",homeGoals>awayGoals?"home":"away"));
        AwardFinalTrophy(homeGoals>awayGoals?HomeTeam:AwayTeam);
    }

    private void AwardFinalTrophy(Team winner)
    {
        if (Mode != MatchMode.CupFinal || TrophyWinner != null) return;
        TrophyWinner = winner;
        Publish(new MatchEvent(MatchEventType.TrophyWon,$"{winner.Name} يفوز بالكأس",subjectPlayer:winner.Players.FirstOrDefault(p=>p.IsActiveOnPitch)),
            ("competition.trophyAwarded",true),("competition.winner.id",winner.Id),("match.finished",true));
    }

    public GoalReplay? WinningGoalReplay()
    {
        if (State != MatchState.GameOver || HomeTeam.Score == AwayTeam.Score) return null;
        var winner=HomeTeam.Score>AwayTeam.Score?HomeTeam:AwayTeam;
        int losingScore=Math.Min(HomeTeam.Score,AwayTeam.Score);
        var replay=_goalHistory.LastOrDefault(r=>r.Goal.ScoringTeam==winner && (r.Goal.ScoringTeam==HomeTeam?r.Goal.HomeScore:r.Goal.AwayScore)==losingScore+1);
        return replay==null?null:new GoalReplay(replay.Frames,replay.Context.With(("goal.winner",true),("match.finished",true)),replay.Goal);
    }

    private void ReviewFinalMatch()
    {
        foreach(var team in new[]{HomeTeam,AwayTeam})
        {
            var best=team.Players.Concat(team.Bench).OrderByDescending(p=>PerformanceOf(p).Rating).FirstOrDefault();
            if(best!=null) Publish(new MatchEvent(MatchEventType.MatchAnalysis,"تحليل ختام المباراة",subjectPlayer:best),
                ("match.finished",true),("team.id",team.Id));
        }
        if (HomeTeam.Score != AwayTeam.Score) AwardFinalTrophy(HomeTeam.Score>AwayTeam.Score?HomeTeam:AwayTeam);
    }
}
