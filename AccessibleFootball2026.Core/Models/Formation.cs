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

    Formation4141 = 9,
    Formation3421 = 10,
    Formation4222 = 11,

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
        FormationType.Formation541_Bus,
        FormationType.Formation4141,
        FormationType.Formation3421,
        FormationType.Formation4222
    };

    public static string ToArabicName(this FormationType type) => type switch
    {
        FormationType.Formation433_Attack => "4-3-3 (وسط متدرج وصانع ألعاب)",
        FormationType.Formation433_Defend => "4-3-3 (ارتكاز واثنان وسط)",
        FormationType.Formation4231_Wide => "4-2-3-1 (ارتكاز مزدوج)",
        FormationType.Formation442_Flat => "4-4-2 (وسط مسطح)",
        FormationType.Formation41212_Diamond => "4-1-2-1-2 (وسط ماسي)",
        FormationType.Formation352_Wings => "3-5-2 (ظهيرا جناح)",
        FormationType.Formation532_Counter => "5-3-2 (كتلة منخفضة)",
        FormationType.Formation343_AllOut => "3-4-3 (جناحان وظهيرا جناح)",
        FormationType.Formation541_Bus => "5-4-1 (كتلة منخفضة)",
        FormationType.Formation4141 => "4-1-4-1 (ارتكاز ووسط عريض)",
        FormationType.Formation3421 => "3-4-2-1 (صانعا ألعاب)",
        FormationType.Formation4222 => "4-2-2-2 (صانعا ألعاب ومهاجمان)",
        _ => "4-3-3"
    };

    public static string ToEnglishName(this FormationType type) => type switch
    {
        FormationType.Formation433_Attack => "4-3-3 Staggered midfield",
        FormationType.Formation433_Defend => "4-3-3 Holding / two eights",
        FormationType.Formation4231_Wide => "4-2-3-1 Wide",
        FormationType.Formation442_Flat => "4-4-2 Flat",
        FormationType.Formation41212_Diamond => "4-1-2-1-2 Diamond",
        FormationType.Formation352_Wings => "3-5-2 Wing-backs",
        FormationType.Formation532_Counter => "5-3-2 Low block",
        FormationType.Formation343_AllOut => "3-4-3 Wide forwards",
        FormationType.Formation541_Bus => "5-4-1 Low block",
        FormationType.Formation4141 => "4-1-4-1 Wide midfield",
        FormationType.Formation3421 => "3-4-2-1 Two tens",
        FormationType.Formation4222 => "4-2-2-2 Two tens / two strikers",
        _ => "4-3-3"
    };

    public static FormationRequirement GetRequirement(FormationType type) => type switch
    {
        FormationType.Formation433_Attack => new(
            FormationType.Formation433_Attack,
            "4-3-3 (وسط متدرج)",
            "4-3-3 Staggered midfield",
            "صانع ألعاب هجومي (AMF)",
            "ارتكاز يحمي الدفاع ولاعب وسط يربط اللعب وصانع ألعاب متقدم، وجناحان يدعمان مهاجماً واحداً. يحتاج تقدم الظهيرين إلى تغطية.",
            false,
            95
        ),
        FormationType.Formation433_Defend => new(
            FormationType.Formation433_Defend,
            "4-3-3 (ارتكاز واثنان وسط)",
            "4-3-3 Holding / two eights",
            "محور ارتكاز دفاعي (DMF)",
            "تتطلب لاعب وسط دفاعي قوي بدنياً ومدافعاً صريحاً (DMF) لحماية ظهر الدفاع.",
            false,
            75
        ),
        FormationType.Formation4231_Wide => new(
            FormationType.Formation4231_Wide, "4-2-3-1", "4-2-3-1",
            "ارتكاز مزدوج وصانع ألعاب وجناحان", "تحتاج ثنائي وسط يغطي الدفاع، وصانع ألعاب بين الخطوط وجناحين يدعمان المهاجم الوحيد.", false, 85),
        FormationType.Formation442_Flat => new(
            FormationType.Formation442_Flat,
            "4-4-2 (وسط مسطح)",
            "4-4-2 Flat",
            "أجنحة كلاسيكية متوازنة",
            "تعتمد على الانتشار العريض والعرضيات والتناغم بين ثنائي الهجوم.",
            false,
            75
        ),
        FormationType.Formation41212_Diamond => new(
            FormationType.Formation41212_Diamond,
            "4-1-2-1-2 (وسط ماسي)",
            "4-1-2-1-2 Diamond",
            "صانع ألعاب في القمة وارتكاز في القاعدة",
            "تتطلب صانع ألعاب حر (AMF) وأظهرة سريعة لتعويض غياب الأجنحة الصريحة.",
            false,
            80
        ),
        FormationType.Formation532_Counter => new(
            FormationType.Formation532_Counter,
            "5-3-2 (كتلة منخفضة)",
            "5-3-2 Low block",
            "3 قلوب دفاع + ظهيري جناح",
            "إعداد كتلة منخفضة بثلاثة قلوب دفاع وظهيري جناح وثلاثي وسط ومهاجمين للمرتدات. الضغط العالي يعاقب التناغم في هذا الإعداد داخل اللعبة.",
            true,
            45
        ),
        FormationType.Formation541_Bus => new(
            FormationType.Formation541_Bus,
            "5-4-1 (كتلة منخفضة)",
            "5-4-1 Low block",
            "5 مدافعين + 4 وسط ومهاجم وحيد",
            "إعداد كتلة منخفضة بخماسي دفاع ورباعي وسط. يحتاج المهاجم الوحيد دعماً عند استعادة الكرة؛ الضغط العالي يعاقب التناغم في هذا الإعداد داخل اللعبة.",
            true,
            35
        ),
        FormationType.Formation352_Wings => new(
            FormationType.Formation352_Wings,
            "3-5-2 (ظهيرا جناح)",
            "3-5-2 Wing-backs",
            "ظهيري جناح برئة حديدية",
            "تتطلب أجنحة دفاعية بلياقة عالية لتغطية كامل طرفي الملعب.",
            false,
            80
        ),
        FormationType.Formation343_AllOut => new(
            FormationType.Formation343_AllOut,
            "3-4-3 (جناحان وظهيرا جناح)",
            "3-4-3 Wide forwards",
            "ثلاثي هجومي وأربعة وسط",
            "تحتاج ظهيري جناح لتوفير العرض والعودة، وثنائي وسط يحمي ثلاثة مدافعين خلف ثلاثي الهجوم.",
            false,
            100
        ),
        FormationType.Formation4141 => new(FormationType.Formation4141,"4-1-4-1","4-1-4-1","ارتكاز وأربعة وسط ومهاجم واحد","ارتكاز يحمي بين الخطوط، ولاعبا طرف يعودان مع الظهيرين، ووسط يدعم المهاجم المعزول.",false,80),
        FormationType.Formation3421 => new(FormationType.Formation3421,"3-4-2-1","3-4-2-1","ظهيرا جناح وصانعا ألعاب","ظهيرا جناح يغطيان الطرفين، وثنائي وسط يحمي الدفاع، وصانعا ألعاب خلف مهاجم واحد.",false,90),
        FormationType.Formation4222 => new(FormationType.Formation4222,"4-2-2-2","4-2-2-2","ارتكاز مزدوج وصانعا ألعاب ومهاجمان","الظهيران يوفران العرض، وثنائي الارتكاز يغطي خلفهما، وصانعا الألعاب يربطان اللعب بالمهاجمين.",false,90),
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

    public static void ApplyFormation(Team team, FormationType type, bool assignPositions = false)
    {
        bool isHome = team.MatchAttackingRight ?? team.Side == TeamSide.Home;
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
        var positions = GetFormationPositions(type);

        for (int i = 0; i < outfield.Count && i < slots.Count; i++)
        {
            outfield[i].HomePosition = MapPos(slots[i].X, slots[i].Y);
            if (assignPositions) outfield[i].CurrentAssignedPosition = positions[i];
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

    private static SpecificPosition[] GetFormationPositions(FormationType type) => type switch
    {
        FormationType.Formation433_Attack => new[] { SpecificPosition.LB, SpecificPosition.CB, SpecificPosition.CB, SpecificPosition.RB, SpecificPosition.CDM, SpecificPosition.CM, SpecificPosition.CAM, SpecificPosition.LW, SpecificPosition.ST, SpecificPosition.RW },
        FormationType.Formation4231_Wide => new[] { SpecificPosition.LB, SpecificPosition.CB, SpecificPosition.CB, SpecificPosition.RB, SpecificPosition.CDM, SpecificPosition.CDM, SpecificPosition.CAM, SpecificPosition.LM, SpecificPosition.ST, SpecificPosition.RM },
        FormationType.Formation442_Flat => new[] { SpecificPosition.LB, SpecificPosition.CB, SpecificPosition.CB, SpecificPosition.RB, SpecificPosition.LM, SpecificPosition.CM, SpecificPosition.CM, SpecificPosition.RM, SpecificPosition.ST, SpecificPosition.ST },
        FormationType.Formation41212_Diamond => new[] { SpecificPosition.LB, SpecificPosition.CB, SpecificPosition.CB, SpecificPosition.RB, SpecificPosition.CDM, SpecificPosition.CM, SpecificPosition.CM, SpecificPosition.CAM, SpecificPosition.ST, SpecificPosition.ST },
        FormationType.Formation352_Wings => new[] { SpecificPosition.CB, SpecificPosition.CB, SpecificPosition.CB, SpecificPosition.LWB, SpecificPosition.CM, SpecificPosition.CAM, SpecificPosition.CM, SpecificPosition.RWB, SpecificPosition.ST, SpecificPosition.ST },
        FormationType.Formation532_Counter => new[] { SpecificPosition.LWB, SpecificPosition.CB, SpecificPosition.CB, SpecificPosition.CB, SpecificPosition.RWB, SpecificPosition.CM, SpecificPosition.CDM, SpecificPosition.CM, SpecificPosition.ST, SpecificPosition.ST },
        FormationType.Formation343_AllOut => new[] { SpecificPosition.CB, SpecificPosition.CB, SpecificPosition.CB, SpecificPosition.LWB, SpecificPosition.CM, SpecificPosition.CM, SpecificPosition.RWB, SpecificPosition.LW, SpecificPosition.ST, SpecificPosition.RW },
        FormationType.Formation541_Bus => new[] { SpecificPosition.LWB, SpecificPosition.CB, SpecificPosition.CB, SpecificPosition.CB, SpecificPosition.RWB, SpecificPosition.LM, SpecificPosition.CM, SpecificPosition.CM, SpecificPosition.RM, SpecificPosition.ST },
        FormationType.Formation4141 => new[] { SpecificPosition.LB, SpecificPosition.CB, SpecificPosition.CB, SpecificPosition.RB, SpecificPosition.CDM, SpecificPosition.LM, SpecificPosition.CM, SpecificPosition.CM, SpecificPosition.RM, SpecificPosition.ST },
        FormationType.Formation3421 => new[] { SpecificPosition.CB, SpecificPosition.CB, SpecificPosition.CB, SpecificPosition.LWB, SpecificPosition.CDM, SpecificPosition.CDM, SpecificPosition.RWB, SpecificPosition.CAM, SpecificPosition.CAM, SpecificPosition.ST },
        FormationType.Formation4222 => new[] { SpecificPosition.LB, SpecificPosition.CB, SpecificPosition.CB, SpecificPosition.RB, SpecificPosition.CDM, SpecificPosition.CDM, SpecificPosition.CAM, SpecificPosition.CAM, SpecificPosition.ST, SpecificPosition.ST },
        _ => new[] { SpecificPosition.LB, SpecificPosition.CB, SpecificPosition.CB, SpecificPosition.RB, SpecificPosition.CDM, SpecificPosition.CM, SpecificPosition.CM, SpecificPosition.LW, SpecificPosition.ST, SpecificPosition.RW }
    };

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
        FormationType.Formation41212_Diamond => new()
        {
            new(22,12), new(18,26), new(18,42), new(22,56),
            new(36,34), new(47,24), new(47,44), new(58,34), new(74,28), new(74,40)
        },
        FormationType.Formation343_AllOut => new()
        {
            new(19,20), new(17,34), new(19,48),
            new(45,12), new(43,26), new(43,42), new(45,56), new(72,14), new(76,34), new(72,54)
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
        FormationType.Formation4141 => new()
        {
            new(22,12),new(18,26),new(18,42),new(22,56),new(35,34),new(49,12),new(48,26),new(48,42),new(49,56),new(74,34)
        },
        FormationType.Formation3421 => new()
        {
            new(19,20),new(17,34),new(19,48),new(43,9),new(39,27),new(39,41),new(43,59),new(60,25),new(60,43),new(76,34)
        },
        FormationType.Formation4222 => new()
        {
            new(22,12),new(18,26),new(18,42),new(22,56),new(37,27),new(37,41),new(57,24),new(57,44),new(75,28),new(75,40)
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
