using System.Numerics;
using AccessibleFootball2026.Core.Models;
namespace AccessibleFootball2026.Core.Simulation;

public partial class MatchEngine
{
    /// <summary>Phase-dependent supporting target, not a forced player teleport or a new assigned position.</summary>
    public Vector2 GetFormationSupportTarget(Team team, Player player)
    {
        if ((team != HomeTeam && team != AwayTeam) || !team.Players.Contains(player) || !player.IsActiveOnPitch)
            return player.HomePosition;
        bool right = team == HomeTeam ? UserAttackingRight : !UserAttackingRight;
        float forward = right ? 1 : -1;
        var target = player.HomePosition;
        var position = player.CurrentAssignedPosition;
        bool ownBall = TeamHasBall(team), defending = OpponentHasBall(team);
        if (player.Role == PlayerRole.Defender)
            target.X += forward * (team.Tactics.DefensiveLineDepth - 45) * (team.Tactics.DefensiveLineDepth>45?.48f:.18f);
        if (ownBall && player.Role is PlayerRole.Midfielder or PlayerRole.Forward && position != SpecificPosition.CDM)
        {
            target.X += forward * Math.Max(0,team.Tactics.PressingIntensity-50)*.16f;
            if(team.Tactics.Focus==AttackingFocus.Central)target.Y=Pitch.Width/2+(target.Y-Pitch.Width/2)*.60f;
            if(team.Tactics.Focus==AttackingFocus.CounterAttack&&IsCounterTransition(team))
                target.X+=forward*(team.Tactics.Tempo/100f)*12;
        }
        if (defending && team.Tactics.OffsideTrap && team.Tactics.DefensiveLineDepth>=40 &&
            position is SpecificPosition.CB or SpecificPosition.LB or SpecificPosition.RB &&
            player.CurrentStamina>=FatiguedStamina && ConcentrationOf(player)>=50)
            target.X+=forward*5; // Coordinated anticipation. Actual kick-time positions still decide offside.
        if (defending && team.Tactics.Marking==DefensiveMarking.ManToMan)
        {
            var markers=team.Players.Where(p=>p.IsActiveOnPitch&&p.Role!=PlayerRole.Goalkeeper).ToArray();
            var opponent=team==HomeTeam?AwayTeam:HomeTeam;
            var marks=opponent.Players.Where(p=>p.IsActiveOnPitch&&p.Role!=PlayerRole.Goalkeeper).OrderBy(p=>right?p.Position2D.X:-p.Position2D.X).ToArray();
            int index=Array.IndexOf(markers,player);
            if(index>=0&&index<marks.Length)
                target=Vector2.Lerp(target,marks[index].Position2D-new Vector2(forward*1.5f,0),.85f);
        }
        if (position is SpecificPosition.LWB or SpecificPosition.RWB)
        {
            // Wing-backs supply width in possession and recover into the back line without it.
            target.X += forward * (ownBall ? 12 : defending ? -12 : 0);
            if (ownBall) target.Y = target.Y < Pitch.Width / 2 ? 6 : Pitch.Width - 6;
            if(defending)
            {
                // A recovery means joining the real centre-back line, not just subtracting a fixed distance.
                float centre=team.Players.Where(p=>p.IsActiveOnPitch&&p.CurrentAssignedPosition==SpecificPosition.CB)
                    .Select(p=>right?p.HomePosition.X:Pitch.Length-p.HomePosition.X).DefaultIfEmpty(18).Average();
                float local=centre+(team.Tactics.DefensiveLineDepth-45)*(team.Tactics.DefensiveLineDepth>45?.48f:.18f);
                if(team.Tactics.OffsideTrap&&team.Tactics.DefensiveLineDepth>=40&&player.CurrentStamina>=FatiguedStamina&&ConcentrationOf(player)>=50)local+=5;
                target.X=right?local:Pitch.Length-local;
            }
        }
        if (position is SpecificPosition.LB or SpecificPosition.RB && ownBall &&
            team.CurrentFormation is FormationType.Formation41212_Diamond or FormationType.Formation4222)
            target.X += forward * 12; // Narrow midfield shapes obtain their width from full-backs.
        if (position is SpecificPosition.LM or SpecificPosition.RM && ownBall && team.CurrentFormation == FormationType.Formation4141)
            target.X += forward * 14;
        if (position == SpecificPosition.CDM && defending)
        {
            // Hold a screen ahead of the centre-backs rather than following every ball into attack.
            float local = right ? target.X : Pitch.Length - target.X;
            float line = 18 + (team.Tactics.DefensiveLineDepth-45)*(team.Tactics.DefensiveLineDepth>45?.48f:.18f);
            float screen = Math.Max(Math.Min(local,42),line+8);
            target.X = right ? screen : Pitch.Length-screen;
        }
        if (player.Role is PlayerRole.Midfielder or PlayerRole.Forward && position != SpecificPosition.CDM)
            target.X += (Ball.Position.X - Pitch.HalfLength) * .3f;
        if(defending && player.Role is PlayerRole.Midfielder or PlayerRole.Forward && position!=SpecificPosition.CDM)
        {
            float local=right?target.X:Pitch.Length-target.X;
            local*=Math.Min(1,.5f+team.Tactics.PressingIntensity*.005f);
            target.X=right?local:Pitch.Length-local;
        }
        target = ApplyWideDiscipline(player, team, target);
        return new Vector2(Math.Clamp(target.X, 2, Pitch.Length - 2), Math.Clamp(target.Y, 2, Pitch.Width - 2));
    }

    private bool ShouldTacticalPress(Team team,Player player,float baseRange)
    {
        if(Ball.Owner!=null&&!OpponentHasBall(team))return false;
        if(Ball.Owner==null && _commentaryPass is {} pass && TeamOf(pass.Sender)==team)return false;
        var ball=Ball.Position2D;
        int count=team.Tactics.PressingIntensity>75?4:team.Tactics.PressingIntensity>50?3:team.Tactics.PressingIntensity>25?2:1;
        float range=TacticalPressRange(baseRange,team)*FatigueMovementMultiplier(player);
        if(IsPassInTransit)range=Math.Min(range,8);
        return Vector2.Distance(player.Position2D,ball)<range&&
            team.Players.Where(p=>p.IsActiveOnPitch&&p.Role!=PlayerRole.Goalkeeper)
                .OrderBy(p=>Vector2.DistanceSquared(p.Position2D,ball)).ThenBy(p=>p.Id).Take(count).Contains(player);
    }

    public static float TacticalPressRange(float baseRange, Team team) =>
        baseRange * (.55f + team.Tactics.PressingIntensity * .01f);
}
