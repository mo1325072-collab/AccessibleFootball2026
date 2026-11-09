namespace AccessibleFootball2026.Core;

/// <summary>
/// المناطق التكتيكية للملعب (11 ضد 11)
/// </summary>
public enum PitchZone
{
    OwnThird,            // في نص ملعبك
    Midfield,            // وسط الملعب
    AttackingThird,      // الثلث الهجومي
    OpponentPenaltyBox,  // منطقة جزاء الخصم
    LeftWing,            // على الجناح الأيسر
    RightWing            // على الجناح الأيمن
}

public static class PitchZoneExtensions
{
    public static string ToArabicAnnouncement(this PitchZone zone) => zone switch
    {
        PitchZone.OwnThird => "في نص ملعبك",
        PitchZone.Midfield => "وسط الملعب",
        PitchZone.AttackingThird => "الثلث الهجومي",
        PitchZone.OpponentPenaltyBox => "منطقة الجزاء",
        PitchZone.LeftWing => "الجناح الأيسر",
        PitchZone.RightWing => "الجناح الأيمن",
        _ => string.Empty
    };
}
