using System;
using System.Collections.Generic;
using System.Linq;

namespace AccessibleFootball2026.Core.Models;

/// <summary>
/// أسلوب وطريقة الدفاع (Defensive Marking)
/// </summary>
public enum DefensiveMarking
{
    Zonal = 0,   // دفاع منطقة (Zonal Marking): انتشار متماسك وتغطية مساحات، يعوض فوارق المهارة الفردية للفرق الضعيفة
    ManToMan = 1 // دفاع رجل لرجل (Man-to-Man Marking): صراعات ثنائية فردية بحتة، كارثي على الفرق الضعيفة أمام النجوم، وممتاز للمدافعين الأقوياء
}

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

public static class TacticsExtensions
{
    public static string ToDisplayName(this AttackingFocus value) => LocalizationService.CurrentLanguage == UiLanguage.English
        ? value switch { AttackingFocus.Balanced => "Balanced", AttackingFocus.Wings => "Wings and crosses", AttackingFocus.Central => "Central passing", AttackingFocus.LongBall => "Long balls", AttackingFocus.ShootOnSight => "Shoot on sight", AttackingFocus.CounterAttack => "Counter-attack", _ => "Balanced" }
        : value.ToArabicName();
    public static string ToDisplayName(this DefensiveMarking value) => LocalizationService.CurrentLanguage == UiLanguage.English
        ? value == DefensiveMarking.ManToMan ? "Man-to-man" : "Zonal" : value.ToArabicName();
    public static string ToDisplayName(this TacklingAggression value) => LocalizationService.CurrentLanguage == UiLanguage.English
        ? value.ToString() : value.ToArabicName();

    public static string ToArabicName(this AttackingFocus focus) => focus switch
    {
        AttackingFocus.Balanced => "متوازن",
        AttackingFocus.Wings => "أجناب وعرضيات",
        AttackingFocus.Central => "عمق وتيكي تاكا",
        AttackingFocus.LongBall => "كرات طويلة",
        AttackingFocus.ShootOnSight => "تسديد عن بعد",
        AttackingFocus.CounterAttack => "هجمات مرتدة خاطفة",
        _ => "متوازن"
    };

    public static string ToArabicName(this DefensiveMarking marking) => marking switch
    {
        DefensiveMarking.Zonal => "دفاع منطقة (Zonal)",
        DefensiveMarking.ManToMan => "دفاع رجل لرجل (Man-to-Man)",
        _ => "دفاع منطقة"
    };

    public static string ToArabicName(this TacklingAggression tackling) => tackling switch
    {
        TacklingAggression.Cautious => "حذر",
        TacklingAggression.Normal => "عادي",
        TacklingAggression.Moderate => "متوسط",
        TacklingAggression.Aggressive => "حماسي",
        TacklingAggression.Reckless => "متهور",
        _ => "عادي"
    };
}

/// <summary>
/// نتيجة تقييم التناغم والانسجام التكتيكي
/// </summary>
public record struct TacticalSynergyResult(
    int SynergyScore,              // من 0 إلى 100
    float AdvantageMultiplier,     // معامل تنسيق الالتحامات (0.40 إلى 1.25)
    IReadOnlyList<string> Conflicts, // التناقضات المكتشفة التي تسببت في عقاب تكتيكي
    IReadOnlyList<string> PositiveSynergies // نقاط التناغم المثالي التي تمنح الفريق تفوقاً
);

/// <summary>
/// نموذج التكتيك التفاعلي للفريق (Team Tactics)
/// يركز على شريطي التحكم الأساسيين:
/// 1. شريط الضغط (Pressing Intensity: 0 - 100) - يضبط تلقائياً عمق خط الدفاع تماشياً مع تقدم الفريق.
/// 2. شريط التمرير والريتم (Passing Game / Directness: 0 - 100) - يضبط تلقائياً سرعة نقل الكرة والريتم.
/// بالإضافة للخيارات التكتيكية المحورية: طريقة الدفاع (منطقة أو رجل لرجل)، شراسة العرقلة، مصيدة التسلل، وبؤرة الهجوم.
/// </summary>
public class TeamTactics
{
    private bool _defensiveLineExplicitlySet = false;
    private bool _tempoExplicitlySet = false;

    // 1. شريط مستوى الضغط الأساسي (Pressing Intensity: 0 - 100)
    // 0-25: متراجع (Low Block) | 26-50: متوازن | 51-75: نشط ومتقدم | 76-100: ضغط عالي خانق (Gegenpressing)
    private int _pressingIntensity = 45;
    public int PressingIntensity
    {
        get => _pressingIntensity;
        set
        {
            _pressingIntensity = Math.Clamp(value, 0, 100);
            if (!_defensiveLineExplicitlySet)
            {
                // عمق الدفاع يتبع تلقائياً شدة الضغط لتفادي الحشو
                _defensiveLineDepth = _pressingIntensity;
            }
        }
    }

    // شريط عمق خط الدفاع (مرتبط تلقائياً بشريط الضغط لتجنب تعقيد الخيارات على اللاعب)
    private int _defensiveLineDepth = 45;
    public int DefensiveLineDepth
    {
        get => _defensiveLineDepth;
        set
        {
            _defensiveLineDepth = Math.Clamp(value, 0, 100);
            _defensiveLineExplicitlySet = true;
        }
    }

    // 2. شريط أسلوب التمرير واللعب الأساسي (Passing Game / Directness: 0 - 100)
    // 0-25: بناء قصير من الـ 18 | 26-50: متوازن | 51-75: مباشر | 76-100: كرات طويلة للأمام
    private int _passingDirectness = 40;
    public int PassingDirectness
    {
        get => _passingDirectness;
        set
        {
            _passingDirectness = Math.Clamp(value, 0, 100);
            if (!_tempoExplicitlySet)
            {
                // سرعة الريتم تتبع تلقائياً سرعة وطبيعة الباس
                _tempo = _passingDirectness;
            }
        }
    }

    // سرعة الإيقاع والريتم (مرتبطة تلقائياً بطبيعة التمرير)
    private int _tempo = 50;
    public int Tempo
    {
        get => _tempo;
        set
        {
            _tempo = Math.Clamp(value, 0, 100);
            _tempoExplicitlySet = true;
        }
    }

    // طريقة وأسلوب الدفاع: دفاع منطقة (Zonal) أو رجل لرجل (Man-to-Man)
    public DefensiveMarking Marking { get; set; } = DefensiveMarking.Zonal;

    // شراسة العرقلة والتدخلات (Tackling Commitment)
    public TacklingAggression Tackling { get; set; } = TacklingAggression.Normal;

    // مصيدة التسلل (Offside Trap)
    public bool OffsideTrap { get; set; } = false;

    // بؤرة الهجوم وأسلوب اللعب (Attacking Focus)
    public AttackingFocus Focus { get; set; } = AttackingFocus.CounterAttack;

    /// <summary>
    /// حساب معامل استنزاف لياقة اللاعب بناءً على عمره الدقيق وشريط الضغط
    /// يطبق إعدادات توازن اللعبة المطلوبة، وليست حقائق فسيولوجية:
    /// - أقل من 19 سنة: رئة لا تتعب، لا يتأثر بالضغط العالي
    /// - 19 إلى 24 سنة: طاقة شبابية قصوى، ينهج في آخر 10 دقائق فقط
    /// - 25 إلى 29 سنة: قمة النضج، ينفد وقوده عند الدقيقة 60
    /// - فوق 29 سنة (30+): خبرة بلا مجهود إضافي، يجيب جاز بنهاية الشوط الأول (دقيقة 40-45)
    /// </summary>
    public float GetStaminaDecayMultiplier(Player player)
    {
        int age = player.SimulationAge; // Explicit neutral simulation band when source age is unknown.

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
            // 25 - 29: يجيب جاز عند الدقيقة 60
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
    public TacticalSynergyResult CalculateSynergy(FormationType formation = FormationType.Formation433_Attack)
    {
        int score = 100;
        var conflicts = new List<string>();
        var synergies = new List<string>();

        // 1. تناقض الكرات الطويلة مع شريط التمرير القصير
        if (Focus == AttackingFocus.LongBall && PassingDirectness < 35)
        {
            score -= 25;
            conflicts.Add(LocalizationService.Text("تناقض تكتيكي: اعتماد أسلوب الكرات الطويلة بينما شريط التمرير منخفض (تحضير قصير) مما يشتت اللاعبين!", "Long-ball focus conflicts with short build-up."));
        }

        // 2. تناقض التمريرات البينية وعمق الملعب مع الكرات الطويلة
        if (Focus == AttackingFocus.Central && PassingDirectness > 70)
        {
            score -= 20;
            conflicts.Add(LocalizationService.Text("تناقض تكتيكي: محاولة اختراق العمق بكرات طولية عشوائية تقتل دقة التمرير البيني!", "Central passing conflicts with very direct long balls."));
        }

        // 3. تناقض الضغط العالي مع خط دفاع متراجع جداً (فجوة خطوط قاتلة)
        if (PressingIntensity > 75 && DefensiveLineDepth < 35)
        {
            score -= 25;
            conflicts.Add(LocalizationService.Text("تفكك الخطوط: ضغط هجومي عال مع دفاع متراجع يباعد بين الخطوط ويترك مساحة في الوسط.", "High pressing with a deep defensive line leaves gaps between the lines."));
        }

        // 4. خطأ مصيدة التسلل مع خط دفاع متراجع داخل الـ 18
        if (OffsideTrap && DefensiveLineDepth < 40)
        {
            score -= 20;
            conflicts.Add(LocalizationService.Text("خلل دفاعي: تفعيل مصيدة التسلل مع خط دفاع متراجع وقريب من منطقة الجزاء يهدد بكسر التسلل وانفرادات كارثية!", "A deep defensive line weakens the offside trap."));
        }

        // 5. تناقض الهجمات المرتدة مع ريتم بطيء وتمرير قصير
        if (Focus == AttackingFocus.CounterAttack && PassingDirectness < 30 && Tempo < 40)
        {
            score -= 20;
            conflicts.Add(LocalizationService.Text("بطء التحول: الهجمات المرتدة تتطلب سرعة وتمريرات مباشرة، والريتم البطيء يمنح الخصم فرصة العودة والتنظيم!", "Slow, short passing delays counter-attacks."));
        }

        // 6. تناغم الضغط العالي الخانق (Gegenpressing Harmony)
        if (PressingIntensity >= 70 && DefensiveLineDepth >= 60 && (Tackling == TacklingAggression.Aggressive || Tackling == TacklingAggression.Moderate))
        {
            synergies.Add(LocalizationService.Text("تناغم الضغط العالي الخانق: تقارب الخطوط وشراسة الاستخلاص تخنق بناء الخصم من الخلف!", "High pressing, a high line and committed tackling work together."));
        }

        // 7. تناغم الدفاع المحكم والتحولات الصاعقة (Low Block & Counter Harmony)
        if (DefensiveLineDepth <= 35 && Focus == AttackingFocus.CounterAttack && Tempo >= 60 && PassingDirectness >= 50)
        {
            synergies.Add(LocalizationService.Text("تناغم الدفاع والتحول الخاطف: تكتل دفاعي منظم وانفجار ون-تاتش في المساحات خلف المدافعين!", "A deep block and quick direct counter-attacks work together."));
        }

        // 8. تناغم دفاع المنطقة والتكتل (Zonal Harmony)
        if (Marking == DefensiveMarking.Zonal && PressingIntensity <= 40)
        {
            synergies.Add(LocalizationService.Text("تناغم دفاع المنطقة المتكتل: تماسك جماعي يغلق المساحات البينية ويعوض فوارق المهارة الفردية!", "Zonal marking and a compact low block close passing lanes."));
        }

        // 9. تناغم الضغط العالي الخانق مع الرقابة اللصيقة
        if (Marking == DefensiveMarking.ManToMan && PressingIntensity >= 75)
        {
            synergies.Add(LocalizationService.Text("تناغم الضغط الخانق والرقابة الفردية: محاصرة كل لاعب خيار تمرير ومنع بناء الهجمة!", "High pressing and man-to-man marking limit passing options."));
        }

        // 8. تناغم التيكي تاكا والاستحواذ (Tiki-Taka Harmony)
        if (PassingDirectness <= 30 && Focus == AttackingFocus.Central && Tempo >= 65 && PressingIntensity >= 55)
        {
            synergies.Add(LocalizationService.Text("تناغم التيكي تاكا: انتشار ممتاز وتمريرات سريعة من لمسة ولمستين تضمن السيطرة على مجريات المباراة!", "Quick short central passing supports possession."));
        }

        // 10. فحص التناغم والتناقض الصارم مع الخطة (Formation Contradictions):
        // Game-design constraint for the named low-block presets, not a universal football law.
        var req = FormationManager.GetRequirement(formation);
        if (req.IsDefensive && PressingIntensity > 60)
        {
            score -= 30;
            conflicts.Add(LocalizationService.Text($"تناقض تكتيكي كارثي: الخطة الحالية ({req.ArabicName}) دفاعية متكتلة بخمسة مدافعين، ولكن شريط الضغط مضبوط على مستوى عالي ({PressingIntensity}%)! هذا يمزق الخطوط ويترك ثغرات شاسعة بين الدفاع والهجوم ويستنزف اللياقة دون جدوى!", "High pressing conflicts with this low-block preset in the game."));
        }

        // تناغم خطة 4-3-3A الهجومية مع الضغط العالي
        if (formation == FormationType.Formation433_Attack && PressingIntensity >= 70)
        {
            synergies.Add(LocalizationService.Text("تناغم 4-3-3 بوسط متدرج مع الضغط العالي.", "Attacking 4-3-3 and high pressing work together."));
        }

        // تناغم خطة 4-3-3B الدفاعية مع محور الارتكاز والتوازن
        if (formation == FormationType.Formation433_Defend && (Focus == AttackingFocus.CounterAttack || Focus == AttackingFocus.Balanced))
        {
            synergies.Add(LocalizationService.Text("تناغم 4-3-3 بمحور ارتكاز واثنين وسط مع التوازن والمرتدات.", "Holding 4-3-3 supports balanced play and counter-attacks."));
        }

        // تناغم خطة 5-3-2 و 5-4-1 مع التكتل الدفاعي والمرتدات
        if (req.IsDefensive && PressingIntensity <= 35 && Focus == AttackingFocus.CounterAttack)
        {
            synergies.Add(LocalizationService.Text($"تناغم التكتل الدفاعي الكاتيناتشو ({req.ArabicName}): 5 مدافعين في الزون مع مرتدات سريعة يمنح الفريق أعلى درجات الصلابة!", "A five-player back line supports a low block and counter-attacks."));
        }

        score = Math.Clamp(score, 10, 100);

        // معامل الأدفانتيدج التكتيكي المترتب على الانسجام
        // سكور 100 يعطي 1.25x أدفانتيدج، وسكور 75 أو أقل يعطي عقاباً تكتيكياً (< 1.0x)
        float advantageMult = (score / 100.0f) * 1.25f;
        advantageMult = Math.Clamp(advantageMult, 0.40f, 1.25f);

        return new TacticalSynergyResult(score, advantageMult, conflicts, synergies);
    }

    public void UseLinkedDefensiveLine()
    { _defensiveLineExplicitlySet=false;_defensiveLineDepth=_pressingIntensity; }
    public void UseLinkedTempo()
    { _tempoExplicitlySet=false;_tempo=_passingDirectness; }

    // Preserve whether each linked control was deliberately overridden. All fields are value types.
    public TeamTactics Clone() => (TeamTactics)MemberwiseClone();
}
