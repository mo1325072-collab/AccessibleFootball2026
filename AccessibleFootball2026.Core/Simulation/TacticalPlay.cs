using System.Numerics;
using AccessibleFootball2026.Core.Models;
namespace AccessibleFootball2026.Core.Simulation;

public partial class MatchEngine
{
    // These are explicit simulation thresholds, not medical or real-football measurements.
    public const float FatiguedStamina = 40;
    public const float ExhaustedStamina = 25;
    private readonly Dictionary<Player,float> _tacticalDecisionWait = new();
    private string? _transitionTeamId;
    private float _transitionUntil;
    private string? _tacticalLastOwnerTeamId;

    public static float FatigueMovementMultiplier(Player player) =>
        .55f + .45f * Math.Clamp(player.CurrentStamina / FatiguedStamina, 0, 1);

    /// <summary>Per minute of actual high-press exposure. A substitute starts with their own remaining energy.</summary>
    public static float TacticalStaminaRate(TeamTactics tactics, Player player)
    {
        if (player.Role == PlayerRole.Goalkeeper) return 28f / 90 * .15f;
        int age = player.SimulationAge; // Explicit neutral simulation band when source age is unknown.
        float tempoCost = .9f + tactics.Tempo * .002f;
        if (tactics.PressingIntensity > 75)
        {
            float intensity = (tactics.PressingIntensity - 76) / 24f;
            if (age < 19) return 28f / 90; // Requested youth protection from the extra pressing load.
            // The user confirmed on 13 November 2026 that age 24 belongs to the youth group.
            float demand = .7f * intensity + .3f * tactics.Tempo / 100;
            if (age <= 24) return (110 - FatiguedStamina) / (90 - 10 * demand);
            if (age <= 29) return 85f / 60;
            return 85f / (45 - 5 * demand);
        }
        if (tactics.PressingIntensity > 50 && age >= 30)
            return (110 - FatiguedStamina) / 60 * tempoCost * (.85f + (tactics.PressingIntensity - 50) * .006f);
        return 28f / 90 * tactics.GetStaminaDecayMultiplier(player) * tempoCost;
    }

    private void ObserveTacticalPossession()
    {
        var team=TeamOf(Ball.Owner);
        if(team==null)return;
        if(_tacticalLastOwnerTeamId!=null && _tacticalLastOwnerTeamId!=team.Id)
        { _transitionTeamId=team.Id;_transitionUntil=_elapsedSeconds+6; }
        _tacticalLastOwnerTeamId=team.Id;
    }
    public bool IsCounterTransition(Team team) => _transitionTeamId==team.Id && _elapsedSeconds<=_transitionUntil;
    public float TacticalDecisionInterval(Team team,Player player) =>
        (2.6f-team.Tactics.Tempo*.021f) / Math.Max(.55f,FatigueMovementMultiplier(player));

    /// <summary>Score an available real receiver; never spawn an outlet or override a human's chosen pass type.</summary>
    public float TacticalPassScore(Team team,Player passer,Player receiver)
    {
        bool right=team==HomeTeam?UserAttackingRight:!UserAttackingRight;
        var opposition=team==HomeTeam?AwayTeam:HomeTeam;
        var delta=receiver.Position2D-passer.Position2D;
        float distance=delta.Length(),progress=delta.X*(right?1:-1);
        float desired=8+team.Tactics.PassingDirectness*.4f;
        float free=opposition.Players.Where(p=>p.IsActiveOnPitch).Select(p=>Vector2.Distance(p.Position2D,receiver.Position2D)).DefaultIfEmpty(20).Min();
        float score=-Math.Abs(distance-desired)*1.4f+progress*.35f+Math.Min(free,12)*2;
        // Cost of an actual defender in the lane. Lofting a direct pass can bypass a nearby presser.
        foreach(var p in opposition.Players.Where(p=>p.IsActiveOnPitch && p.Role!=PlayerRole.Goalkeeper))
        {
            float t=delta.LengthSquared()>.001f?Math.Clamp(Vector2.Dot(p.Position2D-passer.Position2D,delta)/delta.LengthSquared(),0,1):0;
            if(t>.08f&&t<.92f&&Vector2.Distance(p.Position2D,passer.Position2D+delta*t)<1.5f)
                score-=team.Tactics.PassingDirectness>75?4:22;
        }
        switch(team.Tactics.Focus)
        {
            case AttackingFocus.Wings: score+=(Math.Abs(receiver.Position2D.Y-Pitch.Width/2)>20?24:-8);break;
            case AttackingFocus.Central: score+=(Math.Abs(receiver.Position2D.Y-Pitch.Width/2)<14?20:-12);break;
            case AttackingFocus.LongBall: score+=progress*.8f+(receiver.Role==PlayerRole.Forward?18:0);break;
            case AttackingFocus.CounterAttack: score+=progress*(IsCounterTransition(team)?1.4f:.55f);break;
        }
        // Being ahead of the line is a real cost, not automatic immunity from offside.
        var margin=OffsidePosition.Margin(right,receiver.Position2D.X,passer.Position2D.X,opposition.Players.Where(p=>p.IsActiveOnPitch).Select(p=>p.Position2D.X));
        if(margin.HasValue)score-=45;
        return score;
    }

    private void UpdateCpuCarrier(Player carrier,float dt)
    {
        var team=AwayTeam;var goal=UserAttackingRight?Pitch.LeftGoalCenter:Pitch.RightGoalCenter;
        float distance=Vector2.Distance(carrier.Position2D,goal);
        float nearest=HomeTeam.Players.Where(p=>p.IsActiveOnPitch && p.Role!=PlayerRole.Goalkeeper).Select(p=>Vector2.Distance(p.Position2D,carrier.Position2D)).DefaultIfEmpty(100).Min();
        float wait=(_tacticalDecisionWait.TryGetValue(carrier,out var remaining)?remaining:TacticalDecisionInterval(team,carrier))-dt;
        _tacticalDecisionWait[carrier]=wait;
        float shotRange=team.Tactics.Focus==AttackingFocus.ShootOnSight?31:24;
        bool shoot=distance<shotRange && (nearest>1.5f || distance<16);
        if(wait<=0 && shoot)
        {
            Vector2 shotTarget=ComputeCpuShotTarget(carrier.Position2D,!UserAttackingRight,Random.Shared.NextDouble(),Random.Shared.NextDouble(),carrier.Shooting,carrier.CurrentStamina);
            var aim=shotTarget-carrier.Position2D;if(aim.LengthSquared()<.001f)aim=new Vector2(UserAttackingRight?-1:1,0);
            SnapshotOffsideAtKick(carrier);RecordBallTouch(carrier);
            Ball.Kick(Vector2.Normalize(aim),24+carrier.Shooting*.08f,1.6f,carrier);
            _isShotInProgress=true;_shotTargetGoalX=shotTarget.X;_shotGkResolved=false;_shotShooter=carrier;
            _currentShotTechnique=ShotTechnique.Driven;_lastShotTargetY=shotTarget.Y;
            _tacticalDecisionWait[carrier]=TacticalDecisionInterval(team,carrier);
            Publish(new MatchEvent(MatchEventType.ShotTaken,$"تسديدة من {carrier.Name}",CurrentZone,spatialPan:CalculateBallPan(),subjectPlayer:carrier),
                ("tactics.focus",team.Tactics.Focus),("shot.decisionRange",shotRange));
            return;
        }
        if(wait<=0)
        {
            var receiver=team.Players.Where(p=>p.IsActiveOnPitch&&p!=carrier&&p.Role!=PlayerRole.Goalkeeper&&Vector2.Distance(p.Position2D,carrier.Position2D)>4)
                .OrderByDescending(p=>TacticalPassScore(team,carrier,p)).ThenBy(p=>p.Id).FirstOrDefault();
            _tacticalDecisionWait[carrier]=TacticalDecisionInterval(team,carrier);
            if(receiver!=null)
            {
                var destination=receiver.Position2D;var delta=destination-carrier.Position2D;float length=delta.Length();
                bool direct=team.Tactics.PassingDirectness>75||team.Tactics.Focus==AttackingFocus.LongBall;
                bool cross=team.Tactics.Focus==AttackingFocus.Wings&&Math.Abs(carrier.Position2D.Y-Pitch.Width/2)>20&&distance<38;
                float speed=direct||cross?26:16+team.Tactics.Tempo*.06f;
                float height=direct||cross?Math.Clamp(length/speed*4.905f,2.5f,8):.15f;
                RecordBallTouch(carrier);SnapshotOffsideAtKick(carrier);
                Ball.Kick(delta/length,speed,height,carrier);
                BeginCommentaryPass(carrier,receiver,destination,direct?"long":cross?"cross":"short",direct||cross?"high":"ground",cross);
                Publish(new MatchEvent(MatchEventType.BallKicked,$"تمريرة من {carrier.Name} إلى {receiver.Name}",CurrentZone,spatialPan:CalculateBallPan(),subjectPlayer:carrier),
                    ("action.kind","pass"),("pass.height",direct||cross?"high":"ground"),("tactics.focus",team.Tactics.Focus));
                return;
            }
        }
        var toGoal=goal-carrier.Position2D;var direction=toGoal.LengthSquared()>.001f?Vector2.Normalize(toGoal):new Vector2(UserAttackingRight?-1:1,0);
        carrier.FacingDirection=direction;
        carrier.Position2D+=direction*(carrier.Speed*.065f*FatigueMovementMultiplier(carrier))*dt;
        Ball.CatchOrTackle(carrier);
    }
}
