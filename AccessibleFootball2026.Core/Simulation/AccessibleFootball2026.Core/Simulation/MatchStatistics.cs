using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Core.Simulation;

/// <summary>Measured events only. No inferred xG, invented possession or generated match facts.</summary>
public sealed class TeamMatchStatistics
{
    public int Shots { get; internal set; }
    public int ShotsOnTarget { get; internal set; }
    public int ShotsOffTarget { get; internal set; }
    public int Interceptions { get; internal set; }
    public int BlockedShots { get; internal set; }
    public int Woodwork { get; internal set; }
    public int Passes { get; internal set; }
    public int CompletedPasses { get; internal set; }
    public int Corners { get; internal set; }
    public int FreeKicks { get; internal set; }
    public int Penalties { get; internal set; }
    public int GoalKicks { get; internal set; }
    public int ThrowIns { get; internal set; }
    public int Fouls { get; internal set; }
    public int Offsides { get; internal set; }
    public int Saves { get; internal set; }
    public int Tackles { get; internal set; }
    public int YellowCards { get; internal set; }
    public int RedCards { get; internal set; }
}

public partial class MatchEngine
{
    private readonly TeamMatchStatistics _homeStatistics = new(), _awayStatistics = new();
    public TeamMatchStatistics StatisticsFor(Team team) => team == HomeTeam ? _homeStatistics :
        team == AwayTeam ? _awayStatistics : throw new ArgumentException("Team is not in this match", nameof(team));
    private bool _shotCountedOnTarget;
    private void MarkShotOnTarget(Team? team)
    {
        if (team == null || _shotCountedOnTarget) return;
        var stats = StatisticsFor(team);
        // A valid shot event must precede an outcome. Own goals are not attacking shots.
        if (stats.ShotsOnTarget < stats.Shots) { stats.ShotsOnTarget++; _shotCountedOnTarget = true; }
    }
    private void RecordMatchStatistics(MatchEvent e, (string Key, object? Value)[] facts)
    {
        var team = TeamOf(e.SubjectPlayer);
        if (e.Type == MatchEventType.ShotTaken) _shotCountedOnTarget = false;
        if (e.Type == MatchEventType.GoalScored && e.GoalData is { IsOwnGoal: false } goal)
            MarkShotOnTarget(goal.ScoringTeam);
        if (e.Type == MatchEventType.GoalkeeperSave && team != null)
            MarkShotOnTarget(team == HomeTeam ? AwayTeam : HomeTeam);
        if (e.Type == MatchEventType.PassCompleted && _passPerformanceActor is { } passer)
            team = TeamOf(passer);
        // A blocked shot belongs to the shooter, not to the defender who blocked it.
        if (e.Type is MatchEventType.ShotBlocked or MatchEventType.WoodworkHit)
            team = TeamOf(_shotShooter);
        if (team == null) return;
        var s = StatisticsFor(team);
        switch (e.Type)
        {
            case MatchEventType.ShotTaken: s.Shots++; break;
            case MatchEventType.MissedChance: s.ShotsOffTarget++; break;
            case MatchEventType.PassIntercepted: s.Interceptions++; break;
            case MatchEventType.ShotBlocked: s.BlockedShots++; break;
            case MatchEventType.WoodworkHit: s.Woodwork++; break;
            case MatchEventType.PassCompleted: s.CompletedPasses++; break;
            case MatchEventType.CornerKick: s.Corners++; break;
            case MatchEventType.FreeKickAwarded: s.FreeKicks++; break;
            case MatchEventType.PenaltyAwarded: s.Penalties++; break;
            case MatchEventType.GoalKick: s.GoalKicks++; break;
            case MatchEventType.ThrowIn: s.ThrowIns++; break;
            case MatchEventType.FoulCommitted:
                if (!facts.Any(f => f.Key == "referee.advantageRecalled" && f.Value is true)) s.Fouls++;
                break;
            case MatchEventType.Offside: case MatchEventType.GoalDisallowed:
                if (e.Context.TryGet("offside.margin", out _)) s.Offsides++;
                break;
            case MatchEventType.GoalkeeperSave: s.Saves++; break;
            case MatchEventType.TackleSuccessful: case MatchEventType.UserTackleSuccessful: case MatchEventType.OpponentTackleSuccessful: s.Tackles++; break;
            case MatchEventType.YellowCard: s.YellowCards++; break;
            case MatchEventType.RedCard:
                s.RedCards++;
                if (e.Context.TryGet("card.kind", out var kind) && kind == "secondYellow") s.YellowCards++;
                break;
        }
    }

    public IReadOnlyList<string> GetStatisticsAnnouncements()
    {
        var h = _homeStatistics; var a = _awayStatistics;
        string Both(string label, object home, object away) => $"{label}: {HomeTeam.Name} {home}، مقابل {AwayTeam.Name} {away}";
        string Accuracy(TeamMatchStatistics s) => s.Passes == 0 ? "لا توجد تمريرات بعد" : $"{100.0 * s.CompletedPasses / s.Passes:0.0} بالمئة";
        float possession = _homePossessionSeconds + _awayPossessionSeconds;
        string hp = possession == 0 ? "لم يسجل استحواذ بعد" : $"{100 * _homePossessionSeconds / possession:0.0} بالمئة";
        string ap = possession == 0 ? "لم يسجل استحواذ بعد" : $"{100 * _awayPossessionSeconds / possession:0.0} بالمئة";
        return new[] {
            Both("النتيجة", HomeTeam.Score, AwayTeam.Score),
            $"الدقيقة {(int)MatchTimeMinutes}. {(IsExtraTime ? "الوقت الإضافي" : HalfTimeReached ? "الشوط الثاني" : "الشوط الأول")}",
            Both("الخطة", HomeTeam.CurrentFormation.ToArabicName(), AwayTeam.CurrentFormation.ToArabicName()),
            Both("الاستحواذ على الكرة أثناء اللعب الفعلي", hp, ap),
            Both("التسديدات", h.Shots, a.Shots), Both("التسديدات على المرمى", h.ShotsOnTarget, a.ShotsOnTarget),
            Both("التسديدات خارج المرمى", h.ShotsOffTarget, a.ShotsOffTarget),
            Both("التمريرات المعترضة", h.Interceptions, a.Interceptions),
            Both("التسديدات التي صدها المدافع", h.BlockedShots, a.BlockedShots), Both("القائم والعارضة", h.Woodwork, a.Woodwork),
            Both("تصديات الحارس", h.Saves, a.Saves), Both("التمريرات", h.Passes, a.Passes),
            Both("التمريرات المكتملة", h.CompletedPasses, a.CompletedPasses), Both("دقة التمرير", Accuracy(h), Accuracy(a)),
            Both("الركنيات", h.Corners, a.Corners), Both("الركلات الحرة", h.FreeKicks, a.FreeKicks),
            Both("ركلات الجزاء المحتسبة", h.Penalties, a.Penalties), Both("ضربات المرمى", h.GoalKicks, a.GoalKicks),
            Both("رميات التماس", h.ThrowIns, a.ThrowIns), Both("الأخطاء المرتكبة", h.Fouls, a.Fouls),
            Both("التسلل", h.Offsides, a.Offsides), Both("الاستخلاصات الناجحة", h.Tackles, a.Tackles),
            Both("البطاقات الصفراء", h.YellowCards, a.YellowCards), Both("البطاقات الحمراء", h.RedCards, a.RedCards),
            Both("التبديلات المستخدمة", HomeTeam.SubstitutionsUsed, AwayTeam.SubstitutionsUsed)
        };
    }

    public bool AwaitingStoppageConfirmation { get; private set; }
    private void HoldForStoppage(MatchEvent e)
    {
        if (e.Type is MatchEventType.GoalScored or MatchEventType.FreeKickAwarded or MatchEventType.PenaltyAwarded
            or MatchEventType.CornerKick or MatchEventType.GoalKick or MatchEventType.ThrowIn or MatchEventType.Offside or MatchEventType.GoalDisallowed)
        { BeginSubstitutionStoppage(); AwaitingStoppageConfirmation = true; }
    }
    /// <summary>The UI calls only after Enter and after the commentary/announcement finishes.</summary>
    public bool DismissStoppageReplay()
    {
        if (State is MatchState.Paused or MatchState.HalfTime or MatchState.GameOver) return false;
        if (!AwaitingStoppageConfirmation && !AnalysisReplayPending && State != MatchState.GoalCelebration) return false;
        AwaitingStoppageConfirmation = false;
        CompleteAnalysisReplay();
        if (State == MatchState.GoalCelebration) CompleteGoalCelebration();
        return true;
    }
}
