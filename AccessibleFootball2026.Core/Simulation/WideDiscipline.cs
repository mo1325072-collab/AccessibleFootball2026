using System.Numerics;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Core.Simulation;

/// <summary>
/// تعليمة الأجنحة الفعلية عند اختيار أسلوب «أجناب وعرضيات»: اللاعب الجناحي يلزم خط التماس
/// أثناء استحواذ فريقه، ثم يرجع للخلف فور فقدان الكرة. التسجيل الصوتي للمدرب لا يُشغَّل إلا
/// إذا كانت التعليمة مفعّلة فعلاً ولدى الفريق لاعب جناحي على أرض الملعب.
/// </summary>
public partial class MatchEngine
{
    private const float WideLineInset = 1.5f;
    private const float WideRecoverDistance = 12f;
    private readonly HashSet<string> _wideInstructed = new(StringComparer.Ordinal);

    private static bool IsWidePosition(SpecificPosition p) =>
        p is SpecificPosition.LM or SpecificPosition.RM or SpecificPosition.LW or SpecificPosition.RW
          or SpecificPosition.LWB or SpecificPosition.RWB;

    public bool WideDisciplineActive(Team team) =>
        (team == HomeTeam || team == AwayTeam) && team.Tactics.Focus == AttackingFocus.Wings &&
        team.Players.Any(p => !p.IsSentOff && !p.IsOffPitch && IsWidePosition(p.Position));

    private bool TeamHasBall(Team team) => Ball.Owner != null && team.Players.Contains(Ball.Owner);
    private bool OpponentHasBall(Team team) => Ball.Owner != null && !team.Players.Contains(Ball.Owner) &&
        (team == HomeTeam ? AwayTeam : HomeTeam).Players.Contains(Ball.Owner);

    /// <summary>Returns the player's actual target: on the touchline with the ball, dropped back without it.</summary>
    private Vector2 ApplyWideDiscipline(Player p, Team team, Vector2 target)
    {
        if (!IsWidePosition(p.Position) || !WideDisciplineActive(team)) return target;
        if (TeamHasBall(team))
        {
            float y = p.HomePosition.Y < Pitch.Width / 2f ? WideLineInset : Pitch.Width - WideLineInset;
            return new Vector2(target.X, y);
        }
        if (OpponentHasBall(team))
        {
            bool attacksRight = team == HomeTeam ? UserAttackingRight : !UserAttackingRight;
            float back = attacksRight ? -WideRecoverDistance : WideRecoverDistance;
            return new Vector2(Math.Clamp(target.X + back, 2f, Pitch.Length - 2f), target.Y);
        }
        return target;
    }

    /// <summary>Coach word to wide players, once per time the team's Wings focus is set. Returns false when the instruction is not really in force.</summary>
    public bool RequestCoachWideInstruction(Team team)
    {
        if (!WideDisciplineActive(team)) { _wideInstructed.Remove(team.Id); return false; }
        if (!_wideInstructed.Add(team.Id)) return false;
        Publish(new MatchEvent(MatchEventType.CoachWideInstruction, "تعليمات المدرب للأجنحة: الزم خط التماس ثم ارجع للخلف"),
            ("coach.instruction", "wideHoldThenRecover"), ("coach.instruction.teamId", team.Id),
            ("tactics.focus", "Wings"), ("wide.discipline.active", true));
        return true;
    }
}
