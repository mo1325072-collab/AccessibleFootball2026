using System;
using System.Collections.Generic;
using System.Linq;

namespace AccessibleFootball2026.Core.Models;

/// <summary>
/// مستوى شراسة وتدخلات العرقلة
/// </summary>
public enum TacklingAggression
{
    Cautious = 0,  // حذر: يبقى على قدميه، بدون زحلقة، نسبة فاولات شبه معدومة، صفر بطاقات، يمنح الخصم مساحة
    Normal = 1,    // عادي: تدخلات متوازنة قياسية
    Moderate = 2,  // متوسط: احتكاك بدني ومضايقة كتف لكتف قوية
    Aggressive = 3,// حماسي: انزلاقات شرسة واستخلاص حاسم مع ارتفاع احتمالية الإنذارات
    Reckless = 4   // متهور: طيران على الكرة بكل ثقل، استخلاص عنيف، خطر بطاقات حمراء وإصابات بالغة للمدافع والخصم
}

/// <summary>
/// بؤرة الهجوم وأسلوب اللعب المعتمد
/// </summary>
public enum AttackingFocus
{
    Balanced = 0,     // متوازن
    Wings = 1,        // اللعب على الأجناب والعرضيات
    Central = 2,      // التمريرات البينية وعمق الملعب (تيكي تاكا)
    LongBall = 3,     // الكرات الطويلة المباشرة على المهاجم المحطة
    ShootOnSight = 4, // التسديد عن بعد عند فتح أي مساحة في حدود 25-30 متر
    CounterAttack = 5 // الهجمات المرتدة الخاطفة واستغلال المساحات في ضهر الخصم
}

/// <summary>
/// نتيجة تقييم التناغم والانسجام التكتيكي
/// </summary>
public record struct TacticalSynergyResult(
    int SynergyScore,              // من 0 إلى 100
    float AdvantageMultiplier,     // معامل التأثير على الأداء الميداني (0.65 إلى 1.35)
    IReadOnlyList<string> Conflicts, // التناقضات المكتشفة التي تسببت في عقاب تكتيكي
    IReadOnlyList<string> PositiveSynergies // نقاط التناغم المثالي التي تمنح الفريق تفوقاً
);

/// <summary>
/// نموذج التكتيك التفاعلي للفريق (Team Tactics)
/// يضم شرايط الضغط، التمرير، الريتم، عمق الدفاع، العرقلة، مصيدة التسلل، وبؤرة الهجوم
/// مع ربط كامل بمنحنى الأعمار واستهلاك اللياقة وحساب التناغم التكتيكي.
/// </summary>
public class TeamTactics
{
    // 1. شريط مستوى الضغط (Pressing Intensity: 0 - 100)
    // 0-25: متراجع | 26-50: متوازن | 51-75: نشط ومتقدم | 76-100: ضغط عالي خانق (Gegenpressing)
    private int _pressingIntensity = 45;
    public int PressingIntensity
    {
        get => _pressingIntensity;
        set => _pressingIntensity = Math.Clamp(value, 0, 100);
    }

    // 2. شريط عمق خط الدفاع (Defensive Line Depth: 0 - 100)
    // 0-25: تكتل تحت الكرة (Low Block) | 26-50: متوسط | 51-75: متقدم | 76-100: خط دفاع عالي جداً (High Line)
    private int _defensiveLineDepth = 45;
    public int DefensiveLineDepth
    {
        get => _defensiveLineDepth;
        set => _defensiveLineDepth = Math.Clamp(value, 0, 100);
    }

    // 3. شريط أسلوب التمرير (Passing Directness: 0 - 100)
    // 0-25: بناء قصير من الـ 18 | 26-50: متوازن | 51-75: مباشر | 76-100: كرات طويلة للأمام
    private int _passingDirectness = 40;
    public int PassingDirectness
    {
        get => _passingDirectness;
        set => _passingDirectness = Math.Clamp(value, 0, 100);
    }

    // 4. شريط سرعة الإيقاع (Tempo / Speed of Play: 0 - 100)
    // 0-25: بطيء وهادئ وتدوير للكرة | 26-50: متزن | 51-75: سريع | 76-100: ون تاتش وسرعة قصوى
    private int _tempo = 50;
    public int Tempo
    {
        get => _tempo;
        set => _tempo = Math.Clamp(value, 0, 100);
    }

    // 5. شراسة العرقلة والتدخلات (Tackling Commitment)
    public TacklingAggression Tackling { get; set; } = TacklingAggression.Normal;

    // 6. مصيدة التسلل (Offside Trap)
    public bool OffsideTrap { get; set; } = false;

    // 7. بؤرة الهجوم وأسلوب اللعب (Attacking Focus)
    public AttackingFocus Focus { get; set; } = AttackingFocus.CounterAttack;

    /// <summary>
    /// حساب معامل استنزاف لياقة اللاعب بناءً على عمره الدقيق وشريط الضغط
    /// يطبق القواعد الفيزيائية الحقيقية:
    /// - أقل من 19 سنة: رئة لا تتعب، لا يتأثر بالضغط العالي
    /// - 19 إلى 24 سنة: طاقة شبابية قصوى، ينهج في آخر 10 دقائق فقط
    /// - 24 إلى 29 سنة: قمة النضج، ينفد وقوده عند الدقيقة 60
    /// - فوق 29 سنة (30+): خبرة بلا مجهود إضافي، يجيب جاز بنهاية الشوط الأول (دقيقة 40-45)
    /// </summary>
    public float GetStaminaDecayMultiplier(Player player)
    {
        int age = player.Age > 0 ? player.Age : 25;

        // النطاق 1: ضغط متراجع (0 - 25) -> استهلاك منخفض جداً لجميع الأعمار
        if (_pressingIntensity <= 25)
        {
            return 0.70f;
        }

        // النطاق 2: ضغط متوازن (26 - 50) -> استهلاك طبيعي
        if (_pressingIntensity <= 50)
        {
            float ratio = (_pressingIntensity - 25) / 25.0f;
            if (age < 19) return Math.Clamp(0.70f + ratio * 0.15f, 0.70f, 0.85f);
            if (age <= 24) return Math.Clamp(0.75f + ratio * 0.20f, 0.75f, 0.95f);
            if (age <= 29) return Math.Clamp(0.80f + ratio * 0.20f, 0.80f, 1.00f);
            return Math.Clamp(0.85f + ratio * 0.30f, 0.85f, 1.15f); // 30+
        }

        // النطاق 3: ضغط نشط ومتقدم (51 - 75)
        if (_pressingIntensity <= 75)
        {
            float ratio = (_pressingIntensity - 50) / 25.0f;
            if (age < 19) return Math.Clamp(0.85f + ratio * 0.05f, 0.85f, 0.90f);
            if (age <= 24) return Math.Clamp(0.95f + ratio * 0.10f, 0.95f, 1.05f);
            if (age <= 29) return Math.Clamp(1.00f + ratio * 0.25f, 1.00f, 1.25f);
            return Math.Clamp(1.15f + ratio * 0.45f, 1.15f, 1.60f); // 30+ يبدأ يجهد في الشوط الثاني
        }

        // النطاق 4: ضغط عالي خانق (76 - 100) -> تفعيل منحنى الإرهاق الصارم
        float highRatio = (_pressingIntensity - 75) / 25.0f;

        if (age < 19)
        {
            // تحت 19 سنة: لا يتأثر بالضغط العالي، يستمر بكامل طاقته 90 دقيقة
            return 1.00f;
        }
        else if (age <= 24)
        {
            // 19 - 24: ينهج فقط في آخر 10 دقائق (معدل استهلاك متوازن يفرغ في الدقائق 80-90)
            return Math.Clamp(1.05f + highRatio * 0.15f, 1.05f, 1.20f);
        }
        else if (age <= 29)
        {
            // 24 - 29: يجيب جاز عند الدقيقة 60
            return Math.Clamp(1.25f + highRatio * 0.30f, 1.25f, 1.55f);
        }
        else
        {
            // فوق 29 سنة (30+): يجيب جاز بنهاية الشوط الأول (الدقيقة 40-45)
            return Math.Clamp(1.60f + highRatio * 0.80f, 1.60f, 2.40f);
        }
    }

    /// <summary>
    /// حساب التناغم التكتيكي واكتشاف التناقضات بين الخيارات
    /// التحدي الأكبر للمدرب: التناقض يقلل الأدفانتيدج، والانسجام يمنح تفوقاً تكتيكياً
    /// </summary>
    public TacticalSynergyResult CalculateSynergy()
    {
        int score = 100;
        var conflicts = new List<string>();
        var synergies = new List<string>();

        // 1. تناقض الكرات الطويلة مع شريط التمرير القصير
        if (Focus == AttackingFocus.LongBall && PassingDirectness < 35)
        {
            score -= 25;
            conflicts.Add("تناقض تكتيكي: اعتماد أسلوب الكرات الطويلة بينما شريط التمرير منخفض (تحضير قصير) مما يشتت اللاعبين!");
        }

        // 2. تناقض التمريرات البينية وعمق الملعب مع الكرات الطويلة
        if (Focus == AttackingFocus.Central && PassingDirectness > 70)
        {
            score -= 20;
            conflicts.Add("تناقض تكتيكي: محاولة اختراق العمق بكرات طولية عشوائية تقتل دقة التمرير البيني!");
        }

        // 3. تناقض الضغط العالي مع خط دفاع متراجع جداً (فجوة خطوط قاتلة)
        if (PressingIntensity > 75 && DefensiveLineDepth < 35)
        {
            score -= 25;
            conflicts.Add("تفكك الخطوط: ضغط هجومي عالي جداً مع دفاع متراجع يترك مسافة 40 متراً خالية في وسط الملعب!");
        }

        // 4. خطأ مصيدة التسلل مع خط دفاع متراجع داخل الـ 18
        if (OffsideTrap && DefensiveLineDepth < 40)
        {
            score -= 20;
            conflicts.Add("خلل دفاعي: تفعيل مصيدة التسلل مع خط دفاع متراجع وقريب من منطقة الجزاء يهدد بكسر التسلل وانفرادات كارثية!");
        }

        // 5. تناقض الهجمات المرتدة مع ريتم بطيء وتمرير قصير
        if (Focus == AttackingFocus.CounterAttack && PassingDirectness < 30 && Tempo < 40)
        {
            score -= 20;
            conflicts.Add("بطء التحول: الهجمات المرتدة تتطلب سرعة وتمريرات مباشرة، والريتم البطيء يمنح الخصم فرصة العودة والتنظيم!");
        }

        // 6. تناغم الضغط العالي الخانق (Gegenpressing Harmony)
        if (PressingIntensity >= 70 && DefensiveLineDepth >= 60 && (Tackling == TacklingAggression.Aggressive || Tackling == TacklingAggression.Moderate))
        {
            synergies.Add("تناغم الضغط العالي الخانق: تقارب الخطوط وشراسة الاستخلاص تخنق بناء الخصم من الخلف!");
        }

        // 7. تناغم الدفاع المحكم والتحولات الصاعقة (Low Block & Counter Harmony)
        if (DefensiveLineDepth <= 35 && Focus == AttackingFocus.CounterAttack && Tempo >= 60 && PassingDirectness >= 50)
        {
            synergies.Add("تناغم الدفاع والتحول الخاطف: تكتل دفاعي منظم وانفجار ون-تاتش في المساحات خلف المدافعين!");
        }

        // 8. تناغم التيكي تاكا والاستحواذ (Tiki-Taka Harmony)
        if (PassingDirectness <= 30 && Focus == AttackingFocus.Central && Tempo >= 65 && PressingIntensity >= 55)
        {
            synergies.Add("تناغم التيكي تاكا: انتشار ممتاز وتمريرات سريعة من لمسة ولمستين تضمن السيطرة على مجريات المباراة!");
        }

        score = Math.Clamp(score, 10, 100);

        // معامل الأدفانتيدج التكتيكي المترتب على الانسجام
        // سكور 100 يعطي 1.25x أدفانتيدج، وسكور 75 أو أقل يعطي عقاباً تكتيكياً (< 1.0x)
        float advantageMult = (score / 100.0f) * 1.25f;
        advantageMult = Math.Clamp(advantageMult, 0.40f, 1.25f);

        return new TacticalSynergyResult(score, advantageMult, conflicts, synergies);
    }
}
