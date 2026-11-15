using System.Numerics;
using AccessibleFootball2026.Core.Models;
namespace AccessibleFootball2026.Core.Simulation;
public partial class MatchEngine
{
    private float _miscontrolUntil;
    private Player? _lastMiscontroller;
    private bool _miscontrolPressured;
    private float _nextAnalysisSeconds = 30;

    /// <summary>First-touch error produces a real loose ball. The chance is tied to incoming speed and pressure.</summary>
    public bool TryControlIncomingBall(Player receiver, double? testRoll = null)
    {
        if (receiver == _runUpPlayer) return false;
        var team = TeamOf(receiver);
        if (team == null) return false;
        bool pressured = (team == HomeTeam ? AwayTeam : HomeTeam).Players.Any(p => p.IsActiveOnPitch && Vector2.Distance(p.Position2D, receiver.Position2D) < 3);
        var decision = Play.PossessionArbiter.Evaluate(Ball, receiver, _isShotInProgress,
            _elapsedSeconds < _miscontrolUntil && receiver == _lastMiscontroller, pressured, testRoll ?? SimulationRandom.Next());
        if (decision is not (Play.BallControlDecision.Controlled or Play.BallControlDecision.Miscontrol)) return false;
        float speed = Ball.Velocity.Length();
        if (decision == Play.BallControlDecision.Miscontrol)
        {
            _lastMiscontroller=receiver;
            _miscontrolPressured=pressured;
            _miscontrolUntil=_elapsedSeconds+1;
            var incoming=Ball.Velocity;
            Vector2 direction=new(incoming.X,incoming.Y);
            if(direction.LengthSquared()<.01f)direction=receiver.FacingDirection;
            Ball.Kick(direction,Math.Max(3,speed*.45f),.25f,receiver);
            RecordBallTouch(receiver, deliberatelyControlled:false);
            Publish(new MatchEvent(MatchEventType.ControlLost,"لمسة أولى غير موفقة والكرة حرة",CurrentZone,subjectPlayer:receiver),
                ("control.pressured",pressured),("control.outOfBounds",false));
            return false;
        }
        Ball.CatchOrTackle(receiver);
        RecordBallTouch(receiver);
        return true;
    }

    private void CollectLooseBall()
    {
        if (Ball.Owner != null || _isShotInProgress) return;
        var candidates = HomeTeam.Players.Concat(AwayTeam.Players).Where(p =>
            (p != LastKicker || Ball.Velocity.Length() <= 2) &&
            (p != _lastMiscontroller || _elapsedSeconds >= _miscontrolUntil));
        var receiver = Play.PossessionArbiter.NearestEligible(Ball, candidates, _isShotInProgress);
        if (receiver == null || !TryControlIncomingBall(receiver)) return;
        _ballFreeTimer = 0;
        _gkHoldTimer = 0;
        if (ActivePassTransit != null) { ActivePassTransit.IsActive = false; ActivePassTransit = null; }
        if (HomeTeam.Players.Contains(receiver)) ActiveUserPlayer = receiver;
        GrantPossessionGrace(1.4f);
        Publish(new MatchEvent(MatchEventType.BallTrapped, $"استلم {receiver.Name} الكرة", CurrentZone,
            spatialPan: CalculateBallPan(), subjectPlayer: receiver), ("control.fromOpponent", false));
    }

    public void RequestLineup()
    {
        string lineup=string.Join(LocalizationService.Text("، ", ", "),HomeTeam.Players.Where(p=>p.IsActiveOnPitch)
            .Select(p=>LocalizationService.Text($"{p.Name} رقم {p.ShirtNumber}", $"{p.Name}, number {p.ShirtNumber}")));
        Publish(new MatchEvent(MatchEventType.LineupRequested,LocalizationService.Text($"تشكيلة {HomeTeam.Name}: {lineup}", $"{HomeTeam.Name} lineup: {lineup}")));
    }
}
