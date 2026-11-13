using System.Numerics;
namespace AccessibleFootball2026.Core.Simulation;

/// <summary>The menu shootout's ball/keeper resolution. Goal-mouth coordinates and flight time are explicit.</summary>
public sealed record ShootoutKickResult(bool Scored,bool Saved,bool KeeperHeld,Vector3 GoalLinePosition,float Speed,float FlightSeconds,string RunUp);
public static class ShootoutKick
{
    /// <summary>Actual continuous goal-line aim, power, shot error, keeper anticipation and time to reach.</summary>
    public static ShootoutKickResult ResolveAimed(float aimY, int keeperDive, int shooting, int keeping, bool placed, double accuracyRoll)
    {
        if (!float.IsFinite(aimY) || keeperDive is <0 or >2 || !double.IsFinite(accuracyRoll)) throw new ArgumentOutOfRangeException();
        float skill = Math.Clamp(shooting, 1, 100), keeperSkill = Math.Clamp(keeping, 1, 100);
        float error = (float)(Math.Clamp(accuracyRoll, 0, 1) * 2 - 1) * Math.Max(.35f, (110 - skill) * .04f) * (placed ? .3f : 1);
        float y = aimY + error;
        float speed = (placed ? 17 : 27) + skill * .05f;
        float flight = 11 / speed;
        float reaction = .30f - keeperSkill * .0013f;
        float available = Math.Max(0, flight + .16f - reaction); // Anticipated dive begins before ball release.
        float keeperY = (keeperDive == 0 ? 1 : keeperDive == 2 ? -1 : 0) * Math.Min(2.65f, available * 6.5f);
        bool onTarget = Math.Abs(y) < Pitch.GoalWidth / 2;
        float reach = .65f + keeperSkill * .006f;
        bool saved = onTarget && Math.Abs(y - keeperY) <= reach;
        bool held = saved && placed && Math.Abs(y - keeperY) < .8f;
        return new(onTarget && !saved, saved, held, new Vector3(Pitch.Length, y + Pitch.Width / 2, .7f), speed, flight, "normal");
    }

    public static ShootoutKickResult Resolve(int angle,int keeperDive,int shooting,int keeping,bool slow,double accuracyRoll)
    {
        if(angle is <0 or >2 || keeperDive is <0 or >2 || !double.IsFinite(accuracyRoll))throw new ArgumentOutOfRangeException();
        float target=angle==0?3.05f:angle==2?-3.05f:0;
        float error=(float)(Math.Clamp(accuracyRoll,0,1)*2-1)*Math.Max(.35f,(110-Math.Clamp(shooting,1,100))*.04f);
        float y=target+error;
        float z=angle==1?.7f:1.15f;
        float speed=(slow?19:27)+Math.Clamp(shooting,1,100)*.05f;
        float flight=11/speed;
        float keeperY=keeperDive==0?2.65f:keeperDive==2?-2.65f:0;
        bool onTarget=Math.Abs(y)<Pitch.GoalWidth/2 && z<Pitch.GoalHeight;
        float reach=.65f+Math.Clamp(keeping,1,100)*.012f;
        bool saved=onTarget&&Math.Abs(y-keeperY)<=reach;
        bool held=saved&&speed<=25&&Math.Abs(y-keeperY)<.9f;
        return new(onTarget&&!saved,saved,held,new Vector3(Pitch.Length,y+Pitch.Width/2,z),speed,flight,slow?"slow":"normal");
    }
}
