using System.Numerics;
using AccessibleFootball2026.Core.Commentary;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Core.Simulation;

public partial class MatchEngine
{
    private float _elapsedSeconds;
    private float _homePossessionSeconds, _awayPossessionSeconds;
    private float _possessionStartSeconds;
    private int _possessionChanges;
    private int _completedPasses;
    private int _dribblesWon;
    private float _carriedDistance;
    private Player? _lastCarrier;
    private Vector2 _lastCarrierPosition;
    private readonly Dictionary<int, int> _successfulTackles = new();
    private readonly Dictionary<string, int> _teamShots = new(StringComparer.Ordinal);
    private Player? _passPerformanceActor;
    private readonly Dictionary<string, int> _teamPressRecoveries = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _teamSaves = new(StringComparer.Ordinal);
    private float _nextPlayCommentary;
    private bool _offBallPerspective;
    private bool _analysisAwayPerspective;

    private sealed record PendingPass(Player Sender, Player? Target, string Type, string Height, Vector2 Origin, Vector2 Destination, bool IsCross, float Started);
    private PendingPass? _commentaryPass;
    private string _attackOrigin = "openPlay";

    private void TrackPlayFrame(float dt)
    {
        if (dt <= 0 || !float.IsFinite(dt)) return;
        _elapsedSeconds += dt;
        var owner = Ball.Owner;
        var team = TeamOf(owner);
        if (team == HomeTeam) _homePossessionSeconds += dt;
        if (team == AwayTeam) _awayPossessionSeconds += dt;
        if (owner != null)
        {
            if (_lastCarrier == owner) _carriedDistance += Vector2.Distance(_lastCarrierPosition, owner.Position2D);
            _lastCarrier = owner;
            _lastCarrierPosition = owner.Position2D;
        }
        if (_elapsedSeconds >= _nextAnalysisSeconds)
        {
            _nextAnalysisSeconds = _elapsedSeconds + 35;
            _analysisAwayPerspective=!_analysisAwayPerspective;
            var analysisTeam=_analysisAwayPerspective?AwayTeam:HomeTeam;
            Publish(new MatchEvent(MatchEventType.MatchAnalysis, "تحليل سير اللعب", CurrentZone, subjectPlayer: analysisTeam.Players.FirstOrDefault(p=>p.IsActiveOnPitch)));
        }
        if (_elapsedSeconds >= _nextPlayCommentary && owner != null)
        {
            _nextPlayCommentary = _elapsedSeconds + 7;
            _offBallPerspective=!_offBallPerspective;
            var defending=team==HomeTeam?AwayTeam:HomeTeam;
            var subject=_offBallPerspective?defending.Players.FirstOrDefault(p=>p.IsActiveOnPitch&&p.Role!=PlayerRole.Goalkeeper):owner;
            Publish(new MatchEvent(MatchEventType.PossessionPlay, "تطور اللعب", CurrentZone, subjectPlayer: subject));
        }
    }

    private void BeginCommentaryPass(Player sender, Player? target, Vector2 destination, string type, string height, bool cross = false)
    {
        _commentaryPass = new PendingPass(sender, target, type, height, sender.Position2D, destination, cross, _elapsedSeconds);
    }

    private void ResolveCommentaryPass(Player receiver)
    {
        var pass = _commentaryPass;
        if (pass == null || receiver == pass.Sender || _elapsedSeconds - pass.Started > 8) return;
        bool completed = TeamOf(receiver) == TeamOf(pass.Sender);
        if (completed) _completedPasses++;
        if(!completed && TeamOf(receiver) is { } interceptingTeam && interceptingTeam.Tactics.PressingIntensity>=75)
        {
            _teamPressRecoveries[interceptingTeam.Id]=_teamPressRecoveries.GetValueOrDefault(interceptingTeam.Id)+1;
            _attackOrigin="pressRecovery";
        }
        _passPerformanceActor=pass.Sender;
        Publish(new MatchEvent(completed ? MatchEventType.PassCompleted : MatchEventType.PassIntercepted,
            completed ? "تمريرة مكتملة" : "تمريرة مقطوعة", CurrentZone, subjectPlayer: receiver),
            ("pass.type", pass.Type), ("pass.height", pass.Height), ("pass.receiverRole", receiver.Role == PlayerRole.Goalkeeper ? "goalkeeper" : "outfield"),
            ("pass.sender.id", pass.Sender.Id), ("pass.receiver.id", receiver.Id), ("pass.interceptedCross", pass.IsCross));
        _passPerformanceActor=null;
        if(!completed && pass.IsCross)
            Publish(new MatchEvent(MatchEventType.TackleSuccessful,"اعتراض المدافع للكرة العرضية",CurrentZone,subjectPlayer:receiver),
                ("tackle.interceptedCross",true),("tackle.retained",Ball.Owner==receiver));
        _passPerformanceActor=null;
        _commentaryPass = null;
    }

    private CommentaryContext PlayFacts(MatchEvent e)
    {
        var player = e.SubjectPlayer ?? e.GoalData?.Scorer;
        var team = TeamOf(player) ?? (e.GoalData?.ScoringTeam) ?? HomeTeam;
        var opponent = team == HomeTeam ? AwayTeam : HomeTeam;
        float possessionTotal = _homePossessionSeconds + _awayPossessionSeconds;
        float possession = possessionTotal > 0 ? (team == HomeTeam ? _homePossessionSeconds : _awayPossessionSeconds) * 100 / possessionTotal : 50;
        float averageStamina = team.Players.Where(p => p.IsActiveOnPitch).Select(p => p.CurrentStamina).DefaultIfEmpty(100).Average();
        bool right = team == HomeTeam ? UserAttackingRight : !UserAttackingRight;
        Vector2 pos = player?.Position2D ?? Ball.Position2D;
        var defenders = opponent.Players.Where(p => p.IsActiveOnPitch && p.Role != PlayerRole.Goalkeeper).ToArray();
        bool pressured = defenders.Any(p => Vector2.Distance(p.Position2D, pos) <= 3);
        bool wing = pos.Y < 14 || pos.Y > 54;
        bool space = defenders.All(p => Vector2.Distance(p.Position2D, pos) > 5);
        float goalDistance = Vector2.Distance(pos, right ? Pitch.RightGoalCenter : Pitch.LeftGoalCenter);
        string result = team.Score > opponent.Score ? "win" : team.Score < opponent.Score ? "loss" : "draw";
        float progress = right ? pos.X - _possessionOrigin.X : _possessionOrigin.X - pos.X;
        var c = CommentaryContext.Empty.With(
            ("analysis.teamScore", team.Score), ("analysis.opponentScore", opponent.Score), ("analysis.result", result),
            ("analysis.pressRecoveries", _teamPressRecoveries.GetValueOrDefault(team.Id)),
            ("analysis.teamShots", _teamShots.GetValueOrDefault(team.Id)), ("analysis.teamPossession", possession),
            ("analysis.pressingIntensity", team.Tactics.PressingIntensity), ("analysis.averageStamina", averageStamina),
            ("analysis.hasPossession", TeamOf(Ball.Owner) == team),
            ("score.high", Math.Max(HomeTeam.Score, AwayTeam.Score)), ("score.low", Math.Min(HomeTeam.Score, AwayTeam.Score)),
            ("attack.dribblesWon", _dribblesWon), ("attack.defenderBeaten", _dribblesWon > 0),
            ("attack.completedPasses", _completedPasses), ("attack.carriedDistance", _carriedDistance),
            ("attack.progress", progress), ("attack.wing", wing), ("attack.spaceAhead", space),
            ("attack.easyChance", goalDistance < 12 && !pressured),
            ("match.upset", team.Score > opponent.Score && team.Players.Average(p=>p.BaseOverallRating)+10 < opponent.Players.Average(p=>p.BaseOverallRating)),
            ("attack.canCross", wing && goalDistance < 40), ("attack.dangerous", goalDistance < 30),
            ("attack.searchingSpace", _completedPasses >= 2), ("attack.supportArriving", team.Players.Any(p => p != player && p.IsActiveOnPitch && p.Velocity.Length() > 1 && Vector2.Distance(p.Position2D,pos)<20)),
            ("attack.canEqualize", team.Score + 1 == opponent.Score), ("attack.canTakeLead", team.Score == opponent.Score),
            ("attack.canExtendLead", team.Score > opponent.Score), ("attack.origin", _attackOrigin),
            ("attack.counter", _elapsedSeconds - _possessionStartSeconds < 12 && progress > 20),
            ("attack.continues", _possessionTeamId == team.Id), ("attack.mustRelease", pressured && goalDistance > 20),
            ("possession.seconds", _elapsedSeconds - _possessionStartSeconds), ("possession.changed", _possessionChanges > 0),
            ("control.pressured", pressured), ("pressure.high", pressured && opponent.Tactics.PressingIntensity >= 70),
            ("player.positioningGood", space),
            ("analysis.homeRating", HomeTeam.Players.Where(p=>p.IsActiveOnPitch).Select(p=>PerformanceOf(p).Rating).DefaultIfEmpty(6).Average()),
            ("match.ratingGap", Math.Abs(HomeTeam.Players.Average(p => p.BaseOverallRating) - AwayTeam.Players.Average(p => p.BaseOverallRating))));
        if (player != null)
        {
            c = c.With(("player.speed", player.Speed), ("player.goals", player.MatchGoalsCount), ("player.rating", PerformanceOf(player).Rating),
                ("player.role", player.Role == PlayerRole.Forward ? "attacker" : player.Role.ToString().ToLowerInvariant()),
                ("team.activePlayers", team.Players.Count(p => p.IsActiveOnPitch)),
                ("tackle.playerSuccessfulCount", _successfulTackles.GetValueOrDefault(player.Id)));
        }
        if (_commentaryPass is { } pass)
        {
            bool passRight = TeamOf(pass.Sender) == HomeTeam ? UserAttackingRight : !UserAttackingRight;
            c = c.With(("pass.type", pass.Type), ("pass.height", pass.Height), ("action.kind", pass.IsCross ? "cross" : "pass"),
                ("pass.backward", passRight ? pass.Destination.X < pass.Origin.X : pass.Destination.X > pass.Origin.X),
                ("pass.targetWing", pass.Destination.Y < 14 || pass.Destination.Y > 54),
                ("pass.targetRole", pass.Target?.Role == PlayerRole.Goalkeeper ? "goalkeeper" : "outfield"));
        }
        if (e.Type is MatchEventType.TackleSuccessful or MatchEventType.UserTackleSuccessful or MatchEventType.OpponentTackleSuccessful)
            c=c.With(("tackle.inOpponentHalf",right?pos.X>Pitch.HalfLength:pos.X<Pitch.HalfLength),
                ("tackle.inDangerZone",right?pos.X<18:pos.X>Pitch.Length-18),("tackle.legal",true),("tackle.retained",Ball.Owner==player));
        return c;
    }
}
