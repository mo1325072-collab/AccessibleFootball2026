using System.Numerics;
using AccessibleFootball2026.Core.Commentary;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Core.Simulation;

public partial class MatchEngine
{
    private readonly Queue<ReplayFrame> _replayFrames = new();
    public GoalReplay? LastGoalReplay { get; private set; }
    private void CaptureReplayFrame()
    {
        _replayFrames.Enqueue(new ReplayFrame(MatchTimeMinutes, Ball.Position, Ball.Velocity, Ball.Owner?.Id, _elapsedSeconds));
        while (_replayFrames.Count > 500) _replayFrames.Dequeue();
    }

    private CommentaryContext _lastShotContext = CommentaryContext.Empty;
    private bool _shotContextValid;
    private float _shotContextMinute;
    private string? _possessionTeamId;
    private float _possessionStarted;
    private Vector2 _possessionOrigin;
    private readonly Dictionary<string, int> _commentaryStatistics = new(StringComparer.Ordinal);

    private Team? TeamOf(Player? player) => player == null ? null :
        HomeTeam.Players.Contains(player) || HomeTeam.Bench.Contains(player) ? HomeTeam : AwayTeam.Players.Contains(player) || AwayTeam.Bench.Contains(player) ? AwayTeam : null;

    private void Count(string fact) => _commentaryStatistics[fact] = _commentaryStatistics.GetValueOrDefault(fact) + 1;

    private CommentaryContext Snapshot()
    {
        var c = CommentaryContext.Empty.With(
            ("match.mode", Mode == MatchMode.CupFinal ? "knockout" : "friendly"),
            ("match.rivalry", RivalryScenario), ("weather.condition", WeatherCondition),
            ("match.minute", Math.Max(0, (int)MatchTimeMinutes)),
            ("match.extraTime", IsExtraTime),
            ("match.half", IsExtraTime ? (_extraTimeHalfReached ? 4 : 3) : HalfTimeReached ? 2 : 1),
            ("match.finished", State == MatchState.GameOver),
            ("score.home", HomeTeam.Score), ("score.away", AwayTeam.Score),
            ("score.total", HomeTeam.Score + AwayTeam.Score),
            ("score.draw", HomeTeam.Score == AwayTeam.Score),
            ("score.margin", Math.Abs(HomeTeam.Score - AwayTeam.Score)),
            ("team.home.id", HomeTeam.Id), ("team.away.id", AwayTeam.Id),
            ("team.home.name", HomeTeam.Name), ("team.away.name", AwayTeam.Name),
            ("ball.x", Ball.Position.X), ("ball.y", Ball.Position.Y), ("ball.height", Ball.Position.Z));
        foreach (var key in new[] { "stats.shots", "stats.saves", "stats.fouls", "stats.corners", "stats.misses", "stats.cards" })
            c = c.With((key, _commentaryStatistics.GetValueOrDefault(key)));
        foreach (var pair in _commentaryStatistics) c = c.With((pair.Key, pair.Value));
        return c;
    }

    private CommentaryContext CaptureShot(Player shooter, string method = "openPlay")
    {
        var team = TeamOf(shooter);
        if (team == null) return CommentaryContext.Empty;
        bool right = team == HomeTeam ? UserAttackingRight : !UserAttackingRight;
        var goal = right ? Pitch.RightGoalCenter : Pitch.LeftGoalCenter;
        float longitudinal = Math.Abs(goal.X - shooter.Position2D.X);
        float lateral = Math.Abs(goal.Y - shooter.Position2D.Y);
        var defendingPlayers = (team == HomeTeam ? AwayTeam : HomeTeam).Players;
        bool underPressure = defendingPlayers.Any(p => p.IsActiveOnPitch && Vector2.Distance(p.Position2D, shooter.Position2D) <= 3);
        var defenders = defendingPlayers
            .Count(p => p.IsActiveOnPitch && p.Role != PlayerRole.Goalkeeper &&
                (right ? p.Position2D.X > shooter.Position2D.X : p.Position2D.X < shooter.Position2D.X) &&
                Math.Abs(p.Position2D.Y - goal.Y) < 12);
        float progress = right ? shooter.Position2D.X - _possessionOrigin.X : _possessionOrigin.X - shooter.Position2D.X;
        return CommentaryContext.Empty.With(
            ("shot.method", method), ("shot.runUp",_executingRunUp??"normal"), ("player.composure", ComposureOf(shooter)), ("shot.underPressure", underPressure), ("shot.speed", Ball.Velocity.Length()), ("shot.distance", Vector2.Distance(shooter.Position2D, goal)),
            ("shot.angle", Math.Atan2(lateral, Math.Max(.01f, longitudinal)) * 180 / Math.PI),
            ("shot.oneOnOne", defenders == 0 && longitudinal <= 18 && lateral <= 12),
            ("shot.targetCorner", _lastShotTargetY.HasValue && (Math.Abs(_lastShotTargetY.Value - Pitch.GoalYMin) <= 1.0f || Math.Abs(_lastShotTargetY.Value - Pitch.GoalYMax) <= 1.0f)),
            ("shot.counter", _possessionTeamId == team.Id && MatchTimeMinutes - _possessionStarted <= 1 && progress >= 20),
            ("shot.player.id", shooter.Id), ("shot.player.name", shooter.Name), ("shot.team.id", team.Id));
    }

    private void TrackPossession(Player player)
    {
        var team = TeamOf(player);
        if (_lastShotContext.TryGet("shot.player.id", out var previousShooter) && previousShooter != player.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)) _shotContextValid = false;
        if (team != null && _possessionTeamId != team.Id)
        {
            if (_possessionTeamId != null) _possessionChanges++;
            _possessionStartSeconds = _elapsedSeconds;
            _completedPasses = 0;
            _carriedDistance = 0;
            _dribblesWon = 0;
            _attackOrigin = "openPlay";
            _possessionTeamId = team.Id;
            _possessionStarted = MatchTimeMinutes;
            _possessionOrigin = player.Position2D;
        }
    }

    /// <summary>Snapshot before synchronous listeners run; no parsing of display descriptions.</summary>
    private void Publish(MatchEvent e, params (string Key, object? Value)[] facts)
    {
        if (e.Type == MatchEventType.ShotTaken)
        {
            if (e.SubjectPlayer != null)
            {
                _lastShotContext = CaptureShot(e.SubjectPlayer, _nextShotMethod ?? TechniqueKey).With(
                    ("shot.contact", _currentShotTechnique == ShotTechnique.Header ? "head" : _currentShotTechnique == ShotTechnique.Curled ? "insideFoot" : "instep"));
                _nextShotMethod = null;
                _lastShotWoodwork = null;
                _shotContextValid = true;
                _shotContextMinute = MatchTimeMinutes;
            }
            Count("stats.shots");
            var shootingTeam = TeamOf(e.SubjectPlayer);
            if (shootingTeam != null) _teamShots[shootingTeam.Id] = _teamShots.GetValueOrDefault(shootingTeam.Id) + 1;
        }
        if (e.Type == MatchEventType.GoalkeeperSave)
        {
            Count("stats.saves");
            var savingTeam = TeamOf(e.SubjectPlayer);
            if (savingTeam != null) _teamSaves[savingTeam.Id] = _teamSaves.GetValueOrDefault(savingTeam.Id) + 1;
        }
        if ((e.Type is MatchEventType.TackleSuccessful or MatchEventType.UserTackleSuccessful or MatchEventType.OpponentTackleSuccessful) && e.SubjectPlayer != null)
            _successfulTackles[e.SubjectPlayer.Id] = _successfulTackles.GetValueOrDefault(e.SubjectPlayer.Id) + 1;
        if (e.Type == MatchEventType.FoulCommitted && !facts.Any(f=>f.Key=="referee.advantageRecalled" && f.Value is true)) Count("stats.fouls");
        if (e.Type == MatchEventType.CornerKick) Count("stats.corners");
        if (e.Type == MatchEventType.MissedChance) Count("stats.misses");
        if (e.Type is MatchEventType.YellowCard or MatchEventType.RedCard) Count("stats.cards");
        bool recalledFoul=e.Type==MatchEventType.FoulCommitted && facts.Any(f=>f.Key=="referee.advantageRecalled" && f.Value is true);
        if(!recalledFoul)RecordPerformance(e);
        UpdateConfidence(e);
        if (e.Type == MatchEventType.ShotTaken && MatchTimeMinutes < 45) Count("stats.firstHalfShots");
        RecordReactionsFor(e);
        var snapshot = Snapshot();
        var c = new CommentaryContext(snapshot.Facts.Concat(PlayFacts(e).Facts).GroupBy(p => p.Key).Select(g => g.Last()));
        if (e.Type is MatchEventType.ShotTaken or MatchEventType.GoalScored or MatchEventType.HatTrickAchieved or MatchEventType.DramaticComeback or
            MatchEventType.GoalkeeperSave or MatchEventType.MissedChance or MatchEventType.WoodworkHit)
        {
            bool shotActorMatches = _shotContextValid && MatchTimeMinutes - _shotContextMinute <= 2 && (e.GoalData == null || (_lastShotContext.TryGet("shot.player.id", out var shotId) && shotId == e.GoalData.Scorer.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            if (shotActorMatches) c = new CommentaryContext(c.Facts.Concat(_lastShotContext.Facts).GroupBy(p => p.Key).Select(g => g.Last()));
        }
        if (_foulVictim != null && (e.Type is MatchEventType.RedCard or MatchEventType.YellowCard))
            c=c.With(("foul.injuredPlayer",_foulVictim.InjuryStatus!=PlayerInjuryStatus.None));
        if (e.SubjectPlayer != null)
        {
            c = c.With(("player.id", e.SubjectPlayer.Id), ("player.name", e.SubjectPlayer.Name),
                ("player.alias", CommentaryIdentity.PlayerAlias(e.SubjectPlayer.Name)),
                ("keeper.alias", e.SubjectPlayer.Role == PlayerRole.Goalkeeper ? CommentaryIdentity.PlayerAlias(e.SubjectPlayer.Name) : null));
            var team = TeamOf(e.SubjectPlayer);
            if (team != null) c = c.With(("team.id", team.Id), ("team.name", team.Name));
        }
        if (e.GoalData is { } g)
        {
            bool home = g.ScoringTeam == HomeTeam;
            int teamScore = home ? g.HomeScore : g.AwayScore;
            int opponentScore = home ? g.AwayScore : g.HomeScore;
            var defendingKeeper = (home ? AwayTeam : HomeTeam).Players.FirstOrDefault(p=>p.IsActiveOnPitch && p.Role==PlayerRole.Goalkeeper);
            bool positioningError = defendingKeeper != null && (Math.Abs(defendingKeeper.Position2D.X-(home == UserAttackingRight ? Pitch.Length : 0))>8 || Math.Abs(defendingKeeper.Position2D.Y-Ball.Position.Y)>Pitch.GoalWidth*.7f);
            c = c.With(("goal.opponentConcentration",_goalOpponentConcentration),("goal.keeperError", positioningError), ("goal.own.method", g.IsOwnGoal ? _lastTouchMethod : null),
                ("coach.opponent.reaction", CoachReactionOf((home ? AwayTeam : HomeTeam).Id)),
                ("goal.own", g.IsOwnGoal), ("goal.teamOrdinal", teamScore),
                ("goal.matchOrdinal", g.HomeScore + g.AwayScore), ("goal.equalizer", teamScore == opponentScore),
                ("goal.takesLead", teamScore == opponentScore + 1),
                ("goal.extendsLead", teamScore > opponentScore + 1),
                ("goal.playerOrdinal", g.IsOwnGoal ? 0 : g.Scorer.MatchGoalsCount),
                ("goal.player.alias", CommentaryIdentity.PlayerAlias(g.Scorer.Name)),
                ("goal.team.alias", CommentaryIdentity.TeamAlias(g.ScoringTeam.Name)),
                ("goal.player.id", g.Scorer.Id), ("goal.player.name", g.Scorer.Name),
                ("goal.team.id", g.ScoringTeam.Id), ("goal.team.name", g.ScoringTeam.Name),
                ("goal.teamScore", teamScore), ("goal.opponentScore", opponentScore),
                ("goal.minute", g.Minute),
                ("goal.teamShotsBefore", Math.Max(0, _teamShots.GetValueOrDefault(g.ScoringTeam.Id) - 1)),
                ("goal.opponentShotsBefore", _teamShots.GetValueOrDefault((home ? AwayTeam : HomeTeam).Id)),
                ("goal.opponentSaves", _teamSaves.GetValueOrDefault((home ? AwayTeam : HomeTeam).Id)),
                ("goal.teamPossession", c.TryGet("analysis.teamPossession", out var goalPossession) ? goalPossession : null),
                ("goal.teamRatingAdvantage", g.ScoringTeam.Players.Average(p=>p.BaseOverallRating) - (home ? AwayTeam : HomeTeam).Players.Average(p=>p.BaseOverallRating)),
                ("goal.woodwork", _lastShotWoodwork),
                ("goal.solo", _dribblesWon > 0 && _carriedDistance >= 20 && g.Assister == null),
                ("goal.assisted", g.Assister != null),
                ("goal.comeback", (home ? _homeTeamWasTrailing : _awayTeamWasTrailing) && teamScore > opponentScore));
        }
        foreach (var f in e.Context.Facts) c = c.With((f.Key, f.Value));
        e.Context = c.With(facts);
        if (e.Type == MatchEventType.GoalScored && e.GoalData != null)
        {
            CaptureReplayFrame();
            LastGoalReplay = new GoalReplay(_replayFrames, e.Context, e.GoalData);
            _goalHistory.Add(LastGoalReplay);
            _replayFrames.Clear();
        }
        RecordMatchStatistics(e, facts);
        HoldForStoppage(e);
        EventBus.Publish(e);
        if (e.Type == MatchEventType.FullTime) ReviewFinalMatch();
        if (e.Type is MatchEventType.CornerKick or MatchEventType.GoalKick or MatchEventType.ThrowIn or MatchEventType.HalfTime or MatchEventType.FullTime)
            _shotContextValid = false;
    }

    private GoalReplay? _praisedTeamGoal;
    /// <summary>A real audible coach response, not a third-person claim about an inferred face or emotion.
    /// Called only after the official goal announcement; the caller waits for its recording before restart.</summary>
    public bool RequestCoachPraiseForLastTeamGoal()
    {
        if (State != MatchState.GoalCelebration || LastGoalReplay is not {} replay || ReferenceEquals(_praisedTeamGoal,replay)) return false;
        var g=replay.Goal;
        if(g.IsOwnGoal || g.Assister==null || g.Assister==g.Scorer ||
           (g.ScoringTeam!=HomeTeam && g.ScoringTeam!=AwayTeam) ||
           !g.ScoringTeam.Players.Contains(g.Scorer) || !g.ScoringTeam.Players.Contains(g.Assister)) return false;
        _praisedTeamGoal=replay;
        Publish(new MatchEvent(MatchEventType.CoachPraise,"إشادة المدرب بالعمل الجماعي بعد هدف بصناعة زميل",goalData:g),
            ("coach.response","teamGoalPraise"),("coach.response.teamId",g.ScoringTeam.Id));
        return true;
    }

    /// <summary>Used for non-physics events (intro, shootout) with the same event-time score snapshot.</summary>
    public CommentaryContext GetCommentarySnapshot() => Snapshot();
}
