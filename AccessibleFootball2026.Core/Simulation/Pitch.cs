using System.Numerics;

namespace AccessibleFootball2026.Core.Simulation;

/// <summary>
/// هندسة الملعب وقوانين الأبعاد ومناطق التسلل والأهداف المعتمدة دولياً
/// </summary>
public class Pitch
{
    public const float Length = 105.0f; // طول الملعب (متر)
    public const float Width = 68.0f;   // عرض الملعب (متر)
    public const float HalfLength = Length / 2.0f; // منتصف الملعب (52.5 متر)

    public const float GoalWidth = 7.32f;
    public const float GoalHeight = 2.44f;
    public const float GoalYMin = (Width - GoalWidth) / 2.0f; // 30.34
    public const float GoalYMax = GoalYMin + GoalWidth;       // 37.66

    // منطقة الجزاء (16.5 متر)
    public const float PenaltyBoxDepth = 16.5f;
    public const float PenaltyBoxWidth = 40.32f;
    public const float PenaltyYMin = (Width - PenaltyBoxWidth) / 2.0f;
    public const float PenaltyYMax = PenaltyYMin + PenaltyBoxWidth;

    public Vector2 CenterSpot => new(Length / 2.0f, Width / 2.0f);
    public Vector2 LeftGoalCenter => new(0.0f, Width / 2.0f);
    public Vector2 RightGoalCenter => new(Length, Width / 2.0f);

    /// <summary>
    /// حساب المنطقة التكتيكية الحالية للكرة بناءً على اتجاه هجوم الفريق
    /// </summary>
    public PitchZone CalculateZone(Vector3 ballPos, bool attackingTowardsRight)
    {
        float x = attackingTowardsRight ? ballPos.X : (Length - ballPos.X);
        float y = ballPos.Y;

        // في نص ملعبك
        if (x < 35.0f)
        {
            return PitchZone.OwnThird;
        }

        // وسط الملعب
        if (x < 70.0f)
        {
            return PitchZone.Midfield;
        }

        // الأجنحة
        if (y < 14.0f)
        {
            return PitchZone.LeftWing;
        }
        if (y > 54.0f)
        {
            return PitchZone.RightWing;
        }

        // منطقة جزاء الخصم
        if (x >= (Length - PenaltyBoxDepth) && y >= PenaltyYMin && y <= PenaltyYMax)
        {
            return PitchZone.OpponentPenaltyBox;
        }

        // الثلث الهجومي
        return PitchZone.AttackingThird;
    }

    public bool IsGoal(Vector3 ballPos, out bool forTeamAttackingRight)
    {
        forTeamAttackingRight = false;

        // هدف في المرمى الأيمن (لصالح الفريق المهاجم يميناً)
        if (ballPos.X >= Length && ballPos.Y >= GoalYMin && ballPos.Y <= GoalYMax && ballPos.Z <= GoalHeight)
        {
            forTeamAttackingRight = true;
            return true;
        }

        // هدف في المرمى الأيسر
        if (ballPos.X <= 0.0f && ballPos.Y >= GoalYMin && ballPos.Y <= GoalYMax && ballPos.Z <= GoalHeight)
        {
            forTeamAttackingRight = false;
            return true;
        }

        return false;
    }

    public bool IsOutOfBounds(Vector3 ballPos, out bool isTouchline, out bool isGoalLine)
    {
        isTouchline = ballPos.Y < 0.0f || ballPos.Y > Width;
        isGoalLine = ballPos.X < 0.0f || ballPos.X > Length;
        return isTouchline || isGoalLine;
    }
}
