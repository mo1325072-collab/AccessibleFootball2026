using System;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Core.Simulation;

public enum VarReviewType
{
    OffsideCheck,
    PenaltyFoul,
    GoalLineTechnology
}

public class VarDecisionResult
{
    public VarReviewType Type { get; set; }
    public bool Overturned { get; set; }
    public string AnnouncementArabic { get; set; } = string.Empty;
    public string AnnouncementEnglish { get; set; } = string.Empty;
    public float DecisionMarginCm { get; set; }
}

/// <summary>
/// تقنية حكم الفيديو المساعد بالذكاء الاصطناعي (AI VAR Referee):
/// فحص الحالات التحكيمية الجدلية (التسلل، ركلات الجزاء، وعبور خط المرمى)
/// مع الإعلانات الصوتية التفصيلية والمؤثرات الواقعية
/// </summary>
public static class VarAiReferee
{
    public static VarDecisionResult ReviewOffside(string attackingPlayerName, bool initialCallOffside)
    {
        // محاكاة خطوط التسلل الافتراضية ثلاثية الأبعاد بدقة السنتيمتر
        float margin = (float)(Random.Shared.Next(4, 38)); // 4cm to 38cm
        bool isOffside = initialCallOffside ? (Random.Shared.Next(100) < 85) : (Random.Shared.Next(100) < 25);

        string ar = isOffside
            ? $"تقنية الفيديو المساعد VAR: فحص حالة التسلل على {attackingPlayerName}... رسم الخطوط ثلاثية الأبعاد يؤكد: تسلل صحيح بمقدار {margin:F0} سم! إلغاء الهدف واحتساب تسلل."
            : $"تقنية الفيديو المساعد VAR: مراجعة خط التسلل على {attackingPlayerName}... المهاجم يقف على نفس الخط تماماً بفارق {margin:F0} سم! قرار نهائي: لا وجود للتسلل والهدف صحيح 100%!";

        string en = isOffside
            ? $"VAR Check: Offside confirmed on {attackingPlayerName} by {margin:F0} cm. Decision: Offside."
            : $"VAR Check: Overturned! {attackingPlayerName} is onside by {margin:F0} cm. Decision: Goal stands.";

        return new VarDecisionResult
        {
            Type = VarReviewType.OffsideCheck,
            Overturned = isOffside != initialCallOffside,
            AnnouncementArabic = ar,
            AnnouncementEnglish = en,
            DecisionMarginCm = margin
        };
    }

    public static VarDecisionResult ReviewPenalty(string fouledPlayerName, string defendingPlayerName, bool initialCallPenalty)
    {
        bool confirmedPenalty = initialCallPenalty ? (Random.Shared.Next(100) < 88) : (Random.Shared.Next(100) < 30);

        string ar = confirmedPenalty
            ? $"تقنية الـ VAR تراجع التدخل بين {defendingPlayerName} و {fouledPlayerName}... هناك عرقلة واضحة ومس بالقدم داخل منطقة الجزاء! الحكم يؤكد: ركلة جزاء مستحقة!"
            : $"تقنية الـ VAR تراجع التدخل داخل الصندوق... الإعادة تؤكد لعب المدافع {defendingPlayerName} للكرة أولاً ولا وجود لعرقلة متعمدة! قرار الحكم: استمرار اللعب بدون ركلة جزاء.";

        string en = confirmedPenalty
            ? $"VAR Review: Penalty confirmed! Clear trip on {fouledPlayerName} inside the penalty area."
            : $"VAR Review: No penalty. Defending challenge was clean and on the ball.";

        return new VarDecisionResult
        {
            Type = VarReviewType.PenaltyFoul,
            Overturned = confirmedPenalty != initialCallPenalty,
            AnnouncementArabic = ar,
            AnnouncementEnglish = en,
            DecisionMarginCm = 0
        };
    }

    public static VarDecisionResult ReviewGoalLine(string defendingTeamName)
    {
        float margin = (float)(Random.Shared.Next(2, 14));
        bool crossed = Random.Shared.Next(100) < 70;

        string ar = crossed
            ? $"حساسات تقنية خط المرمى تهتز في ساعة الحكم! الكرة تجاوزت خط مرمى {defendingTeamName} بالكامل بمقدار {margin:F0} سم! هدف شرعي مؤكد!"
            : $"تقنية خط المرمى: الكرة لم تعبر الخط بكامل محيطها وبقي {margin:F0} ملم ملامساً للخط! لا يوجد هدف واستمرار اللعب.";

        string en = crossed
            ? $"Goal-line technology: Ball completely crossed the line by {margin:F0} cm! GOAL awarded!"
            : $"Goal-line technology: No goal. Ball did not fully cross the goal line.";

        return new VarDecisionResult
        {
            Type = VarReviewType.GoalLineTechnology,
            Overturned = false,
            AnnouncementArabic = ar,
            AnnouncementEnglish = en,
            DecisionMarginCm = margin
        };
    }
}
