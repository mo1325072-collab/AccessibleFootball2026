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
        string T(string ar, string en) => LocalizationService.Text(ar, en);
        var h = _homeStatistics; var a = _awayStatistics;
        string Both(string label, object home, object away) => T($"{label}: {HomeTeam.Name} {home}، مقابل {AwayTeam.Name} {away}", $"{label}: {HomeTeam.Name} {home}, {AwayTeam.Name} {away}");
        string Accuracy(TeamMatchStatistics s) => s.Passes == 0 ? T("لا توجد تمريرات بعد", "No passes recorded") : T($"{100.0 * s.CompletedPasses / s.Passes:0.0} بالمئة", $"{100.0 * s.CompletedPasses / s.Passes:0.0} percent");
        float possession = _homePossessionSeconds + _awayPossessionSeconds;
        string hp = possession == 0 ? T("لم يسجل استحواذ بعد", "No possession recorded") : T($"{100 * _homePossessionSeconds / possession:0.0} بالمئة", $"{100 * _homePossessionSeconds / possession:0.0} percent");
        string ap = possession == 0 ? T("لم يسجل استحواذ بعد", "No possession recorded") : T($"{100 * _awayPossessionSeconds / possession:0.0} بالمئة", $"{100 * _awayPossessionSeconds / possession:0.0} percent");
        return new[] {
            Both(T("النتيجة", "Score"), HomeTeam.Score, AwayTeam.Score),
            T($"الدقيقة {(int)MatchTimeMinutes}. {(IsExtraTime ? "الوقت الإضافي" : HalfTimeReached ? "الشوط الثاني" : "الشوط الأول")}", $"Minute {(int)MatchTimeMinutes}. {(IsExtraTime ? "Extra time" : HalfTimeReached ? "Second half" : "First half")}"),
            Both(T("الخطة", "Formation"), T(HomeTeam.CurrentFormation.ToArabicName(), HomeTeam.CurrentFormation.ToEnglishName()), T(AwayTeam.CurrentFormation.ToArabicName(), AwayTeam.CurrentFormation.ToEnglishName())),
            Both(T("الاستحواذ على الكرة أثناء اللعب الفعلي", "Possession during active play"), hp, ap),
            Both(T("التسديدات", "Shots"), h.Shots, a.Shots), Both(T("التسديدات على المرمى", "Shots on target"), h.ShotsOnTarget, a.ShotsOnTarget),
            Both(T("التسديدات خارج المرمى", "Shots off target"), h.ShotsOffTarget, a.ShotsOffTarget),
            Both(T("التمريرات المعترضة", "Interceptions"), h.Interceptions, a.Interceptions),
            Both(T("التسديدات التي صدها المدافع", "Blocked shots"), h.BlockedShots, a.BlockedShots), Both(T("القائم والعارضة", "Woodwork"), h.Woodwork, a.Woodwork),
            Both(T("تصديات الحارس", "Saves"), h.Saves, a.Saves), Both(T("التمريرات", "Passes"), h.Passes, a.Passes),
            Both(T("التمريرات المكتملة", "Completed passes"), h.CompletedPasses, a.CompletedPasses), Both(T("دقة التمرير", "Pass accuracy"), Accuracy(h), Accuracy(a)),
            Both(T("الركنيات", "Corners"), h.Corners, a.Corners), Both(T("الركلات الحرة", "Free kicks"), h.FreeKicks, a.FreeKicks),
            Both(T("ركلات الجزاء المحتسبة", "Penalties awarded"), h.Penalties, a.Penalties), Both(T("ضربات المرمى", "Goal kicks"), h.GoalKicks, a.GoalKicks),
            Both(T("رميات التماس", "Throw-ins"), h.ThrowIns, a.ThrowIns), Both(T("الأخطاء المرتكبة", "Fouls committed"), h.Fouls, a.Fouls),
            Both(T("التسلل", "Offsides"), h.Offsides, a.Offsides), Both(T("الاستخلاصات الناجحة", "Successful tackles"), h.Tackles, a.Tackles),
            Both(T("البطاقات الصفراء", "Yellow cards"), h.YellowCards, a.YellowCards), Both(T("البطاقات الحمراء", "Red cards"), h.RedCards, a.RedCards),
            Both(T("التبديلات المستخدمة", "Substitutions used"), HomeTeam.SubstitutionsUsed, AwayTeam.SubstitutionsUsed)
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
