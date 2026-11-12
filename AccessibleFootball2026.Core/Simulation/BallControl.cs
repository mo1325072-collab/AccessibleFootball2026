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
        if (receiver==_runUpPlayer)return false;
        if (Ball.Owner != null || !receiver.IsActiveOnPitch || Ball.Position.Z > 1.5f || Vector2.Distance(receiver.Position2D,Ball.Position2D)>2.2f) return false;
        if (_elapsedSeconds < _miscontrolUntil && receiver == _lastMiscontroller) return false;
        var team=TeamOf(receiver);
        if (team==null) return false;
        bool pressured=(team==HomeTeam?AwayTeam:HomeTeam).Players.Any(p=>p.IsActiveOnPitch&&Vector2.Distance(p.Position2D,receiver.Position2D)<3);
        float speed=Ball.Velocity.Length();
        double errorChance = !pressured && speed <= 22 ? 0 : Math.Clamp((100-receiver.Dribbling)*.001 + Math.Max(0,speed-22)*.006+(pressured?.08:0),0,.35);
        if ((testRoll??Random.Shared.NextDouble()) < errorChance)
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

    public void RequestLineup()
    {
        string lineup=string.Join(LocalizationService.Text("، ", ", "),HomeTeam.Players.Where(p=>p.IsActiveOnPitch)
            .Select(p=>LocalizationService.Text($"{p.Name} رقم {p.ShirtNumber}", $"{p.Name}, number {p.ShirtNumber}")));
        Publish(new MatchEvent(MatchEventType.LineupRequested,LocalizationService.Text($"تشكيلة {HomeTeam.Name}: {lineup}", $"{HomeTeam.Name} lineup: {lineup}")));
    }
}
