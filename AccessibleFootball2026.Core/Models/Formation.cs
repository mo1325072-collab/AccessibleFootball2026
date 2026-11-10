using System.Numerics;

namespace AccessibleFootball2026.Core.Models;

public enum FormationType
{
    Formation433_Attack = 0,    // 4-3-3 A هجومية بصانع ألعاب AMF
    Formation433_Defend = 1,    // 4-3-3 B دفاعية/متوازنة بمحور ارتكاز صريح DMF
    Formation4231_Wide = 2,     // 4-2-3-1 الشهيرة بالأجنحة والارتكاز المزدوج
    Formation442_Flat = 3,      // 4-4-2 A الكلاسيكية بخطي دفاع ووسط متوازيين
    Formation41212_Diamond = 4, // 4-4-2 B الماسية (دايموند) بصانع ألعاب
    Formation352_Wings = 5,     // 3-5-2 هجومية بثلاثي دفاعي وأجنحة طايرة
    Formation532_Counter = 6,   // 5-3-2 دفاعية محكمة للمرتدات
    Formation343_AllOut = 7,    // 3-4-3 هجوم كاسح وضغط متقدم
    Formation541_Bus = 8,       // 5-4-1 ركن الأتوبيس والدفاع التام

    // تسميات شائعة ومطابقة دقيقة لطلب المستخدم:
    Formation433_A = Formation433_Attack,
    Formation433_B = Formation433_Defend,
    Formation442_A = Formation442_Flat,
    Formation442_B = Formation41212_Diamond,
    Formation541 = Formation541_Bus,

    // Aliases for compatibility across database schemas
    Formation433_Holding = Formation433_Defend,
    Formation4231 = Formation4231_Wide,
    Formation442 = Formation442_Flat,
    Formation532 = Formation532_Counter,
    Formation352 = Formation352_Wings,
    Formation343 = Formation343_AllOut,
    Formation433 = Formation433_Attack
}

public record FormationRequirement(
    FormationType Type,
    string ArabicName,
    string EnglishName,
    string KeyPositionArabic,
    string RequirementDescription,
    bool IsDefensive,
    int MaxRecommendedPressing
);

public static class FormationManager
{
    public static List<FormationType> GetAllFormations() => new()
    {
        FormationType.Formation433_Attack,
        FormationType.Formation433_Defend,
        FormationType.Formation4231_Wide,
        FormationType.Formation442_Flat,
        FormationType.Formation41212_Diamond,
        FormationType.Formation352_Wings,
        FormationType.Formation532_Counter,
        FormationType.Formation343_AllOut,
        FormationType.Formation541_Bus
    };

    public static string ToArabicName(this FormationType type) => type switch
    {
        FormationType.Formation433_Attack => "4-3-3 A (هجومية صريحة بصانع ألعاب AMF)",
        FormationType.Formation433_Defend => "4-3-3 B (دفاعية بمحور ارتكاز صريح DMF)",
        FormationType.Formation4231_Wide => "4-2-3-1 (المتوازنة بالارتكاز المزدوج والأجنحة)",
        FormationType.Formation442_Flat => "4-4-2 A (الكلاسيكية بخطي دفاع ووسط متوازيين)",
        FormationType.Formation41212_Diamond => "4-4-2 B (الماسية بصانع ألعاب صريح)",
        FormationType.Formation352_Wings => "3-5-2 (ثلاثي دفاعي مع أجنحة طايرة)",
        FormationType.Formation532_Counter => "5-3-2 (دفاع حديدي محكم للمرتدات)",
        FormationType.Formation343_AllOut => "3-4-3 (هجوم كاسح وضغط متقدم)",
        FormationType.Formation541_Bus => "5-4-1 (ركن الأتوبيس والتكتل الدفاعي المطلق)",
        _ => "4-3-3"
    };

    public static string ToEnglishName(this FormationType type) => type switch
    {
        FormationType.Formation433_Attack => "4-3-3 A (Attack / AMF)",
        FormationType.Formation433_Defend => "4-3-3 B (Holding / DMF)",
        FormationType.Formation4231_Wide => "4-2-3-1 Wide",
        FormationType.Formation442_Flat => "4-4-2 A Flat",
        FormationType.Formation41212_Diamond => "4-4-2 B Diamond",
        FormationType.Formation352_Wings => "3-5-2 Wings",
        FormationType.Formation532_Counter => "5-3-2 Counter",
        FormationType.Formation343_AllOut => "3-4-3 All Out",
        FormationType.Formation541_Bus => "5-4-1 Park the Bus",
        _ => "4-3-3"
    };

    public static FormationRequirement GetRequirement(FormationType type) => type switch
    {
        FormationType.Formation433_Attack => new(
            FormationType.Formation433_Attack,
            "4-3-3 A (هجومية صريحة)",
            "4-3-3 A Attack",
            "صانع ألعاب هجومي (AMF)",
            "تتطلب صانع ألعاب متقدم مهاري (AMF) لصناعة اللعب، وتناسب الضغط العالي.",
            false,
            95
        ),
        FormationType.Formation433_Defend => new(
            FormationType.Formation433_Defend,
            "4-3-3 B (دفاعية/ارتكاز)",
            "4-3-3 B Holding",
            "محور ارتكاز دفاعي (DMF)",
            "تتطلب لاعب وسط دفاعي قوي بدنياً ومدافعاً صريحاً (DMF) لحماية ظهر الدفاع.",
            false,
            75
        ),
        FormationType.Formation442_Flat => new(
            FormationType.Formation442_Flat,
            "4-4-2 A (كلاسيكية)",
            "4-4-2 A Flat",
            "أجنحة كلاسيكية متوازنة",
            "تعتمد على الانتشار العريض والعرضيات والتناغم بين ثنائي الهجوم.",
            false,
            75
        ),
        FormationType.Formation41212_Diamond => new(
            FormationType.Formation41212_Diamond,
            "4-4-2 B (ماسية)",
            "4-4-2 B Diamond",
            "صانع ألعاب في القمة وارتكاز في القاعدة",
            "تتطلب صانع ألعاب حر (AMF) وأظهرة سريعة لتعويض غياب الأجنحة الصريحة.",
            false,
            80
        ),
        FormationType.Formation532_Counter => new(
            FormationType.Formation532_Counter,
            "5-3-2 (دفاعية محكمة)",
            "5-3-2 Counter",
            "3 قلوب دفاع + ظهيري جناح",
            "خطة دفاعية صريحة للتكتل تحت الكرة (ضغط 0-40) والمرتدات، يمنع الضغط العالي معها.",
            true,
            45
        ),
        FormationType.Formation541_Bus => new(
            FormationType.Formation541_Bus,
            "5-4-1 (ركن الأتوبيس)",
            "5-4-1 Bus",
            "5 مدافعين + 4 وسط ومهاجم وحيد",
            "تكتل دفاعي صارم لغلق كافة المنافذ أمام الفرق الكبرى، تتطلب ريتم بطيء وضغط متراجع تماماً.",
            true,
            35
        ),
        FormationType.Formation352_Wings => new(
            FormationType.Formation352_Wings,
            "3-5-2 (أجنحة طايرة)",
            "3-5-2 Wings",
            "ظهيري جناح برئة حديدية",
            "تتطلب أجنحة دفاعية بلياقة عالية لتغطية كامل طرفي الملعب.",
            false,
            80
        ),
        FormationType.Formation343_AllOut => new(
            FormationType.Formation343_AllOut,
            "3-4-3 (هجوم كاسح)",
            "3-4-3 All Out",
            "ثلاثي هجومي وأربعة وسط",
            "تتطلب ضغطاً متقدماً وهجوماً شاملاً للمخاطرة وقلب النتائج.",
            false,
            100
        ),
        _ => new(
            FormationType.Formation433_Attack,
            "4-3-3",
            "4-3-3",
            "وسط متوازن",
            "خطة قياسية متوازنة",
            false,
            80
        )
    };

    public static void ApplyFormation(Team team, FormationType type)
    {
        bool isHome = team.Side == TeamSide.Home;
        Vector2 MapPos(float x, float y) => isHome ? new Vector2(x, y) : new Vector2(105.0f - x, 68.0f - y);

        if (team.Players.Count < 1) return;

        // استبعاد اللاعبين المطرودين أو الخارجين بسبب الإصابة
        var activePlayers = team.Players.Where(p => !p.IsSentOff && !p.IsOffPitch).ToList();
        if (activePlayers.Count == 0) return;

        // حارس المرمى ثابت دائماً
        var gk = activePlayers.FirstOrDefault(p => p.Role == PlayerRole.Goalkeeper) ?? activePlayers[0];
        gk.HomePosition = MapPos(5.0f, 34.0f);

        var outfield = activePlayers.Where(p => p != gk).ToList();
        var slots = GetFormationSlotCoordinates(type);

        for (int i = 0; i < outfield.Count && i < slots.Count; i++)
        {
            outfield[i].HomePosition = MapPos(slots[i].X, slots[i].Y);
        }

        foreach (var p in activePlayers)
        {
            p.Position2D = p.HomePosition;
        }

        foreach (var p in team.Players.Where(p => p.IsSentOff || p.IsOffPitch))
        {
            p.Position2D = new Vector2(-100f, -100f);
            p.HomePosition = new Vector2(-100f, -100f);
        }
    }

    private static List<Vector2> GetFormationSlotCoordinates(FormationType type) => type switch
    {
        FormationType.Formation433_Attack => new()
        {
            new(22.0f, 12.0f), // LB
            new(18.0f, 26.0f), // CB
            new(18.0f, 42.0f), // CB
            new(22.0f, 56.0f), // RB
            new(36.0f, 34.0f), // CDM
            new(48.0f, 22.0f), // CM
            new(50.0f, 46.0f), // CAM
            new(72.0f, 14.0f), // LW
            new(76.0f, 34.0f), // ST
            new(72.0f, 54.0f)  // RW
        },
        FormationType.Formation4231_Wide => new()
        {
            new(22.0f, 12.0f),
            new(18.0f, 26.0f),
            new(18.0f, 42.0f),
            new(22.0f, 56.0f),
            new(36.0f, 26.0f), // CDM
            new(36.0f, 42.0f), // CDM
            new(52.0f, 34.0f), // CAM
            new(60.0f, 14.0f), // LM
            new(76.0f, 34.0f), // ST
            new(60.0f, 54.0f)  // RM
        },
        FormationType.Formation442_Flat => new()
        {
            new(22.0f, 12.0f),
            new(18.0f, 26.0f),
            new(18.0f, 42.0f),
            new(22.0f, 56.0f),
            new(48.0f, 14.0f), // LM
            new(46.0f, 28.0f), // CM
            new(46.0f, 40.0f), // CM
            new(48.0f, 54.0f), // RM
            new(74.0f, 28.0f), // ST
            new(74.0f, 40.0f)  // ST
        },
        FormationType.Formation352_Wings => new()
        {
            new(19.0f, 20.0f), // CB
            new(17.0f, 34.0f), // CB
            new(19.0f, 48.0f), // CB
            new(40.0f, 10.0f), // LWB
            new(44.0f, 24.0f), // CM
            new(50.0f, 34.0f), // CAM
            new(44.0f, 44.0f), // CM
            new(40.0f, 58.0f), // RWB
            new(75.0f, 28.0f), // ST
            new(75.0f, 40.0f)  // ST
        },
        FormationType.Formation532_Counter => new()
        {
            new(20.0f, 10.0f), // LWB
            new(18.0f, 22.0f), // CB
            new(16.0f, 34.0f), // CB
            new(18.0f, 46.0f), // CB
            new(20.0f, 58.0f), // RWB
            new(42.0f, 24.0f), // CM
            new(40.0f, 34.0f), // CDM
            new(42.0f, 44.0f), // CM
            new(74.0f, 28.0f), // ST
            new(74.0f, 40.0f)  // ST
        },
        FormationType.Formation541_Bus => new()
        {
            new(20.0f, 10.0f), // LWB
            new(17.0f, 22.0f), // CB
            new(16.0f, 34.0f), // CB
            new(17.0f, 46.0f), // CB
            new(20.0f, 58.0f), // RWB
            new(38.0f, 12.0f), // LM
            new(36.0f, 26.0f), // CM
            new(36.0f, 42.0f), // CM
            new(38.0f, 56.0f), // RM
            new(70.0f, 34.0f)  // ST
        },
        _ => new()
        {
            new(22.0f, 12.0f),
            new(18.0f, 26.0f),
            new(18.0f, 42.0f),
            new(22.0f, 56.0f),
            new(36.0f, 34.0f),
            new(48.0f, 22.0f),
            new(48.0f, 46.0f),
            new(72.0f, 14.0f),
            new(76.0f, 34.0f),
            new(72.0f, 54.0f)
        }
    };
}
