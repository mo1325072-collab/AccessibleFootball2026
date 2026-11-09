using System.Numerics;

namespace AccessibleFootball2026.Core.Models;

public enum FormationType
{
    Formation433_Attack,    // 4-3-3 هجومية
    Formation433_Defend,    // 4-3-3 متوازنة بمحورين
    Formation4231_Wide,     // 4-2-3-1 الشهيرة بالأجنحة
    Formation442_Flat,      // 4-4-2 الإنجليزية الكلاسيكية
    Formation41212_Diamond, // 4-4-2 دايموند في الوسط
    Formation352_Wings,     // 3-5-2 هجومية بثلاثي دفاعي
    Formation532_Counter,   // 5-3-2 دفاعية محكمة للمرتدات
    Formation343_AllOut,    // 3-4-3 هجوم كاسح

    // Aliases for compatibility across database schemas
    Formation433_Holding = Formation433_Defend,
    Formation4231 = Formation4231_Wide,
    Formation442 = Formation442_Flat,
    Formation532 = Formation532_Counter,
    Formation352 = Formation352_Wings,
    Formation343 = Formation343_AllOut,
    Formation433 = Formation433_Attack
}

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
        FormationType.Formation343_AllOut
    };

    public static string ToArabicName(this FormationType type) => type switch
    {
        FormationType.Formation433_Attack => "4-3-3 (هجومية صريحة بثلاثي هجومي)",
        FormationType.Formation433_Defend => "4-3-3 (متوازنة بثنائي ارتكاز)",
        FormationType.Formation4231_Wide => "4-2-3-1 (الخطة التكتيكية الأكثر توازناً)",
        FormationType.Formation442_Flat => "4-4-2 (الكلاسيكية بخطي دفاع ووسط متوازيين)",
        FormationType.Formation41212_Diamond => "4-1-2-1-2 (دايموند بالاعتماد على صانع ألعاب صريح)",
        FormationType.Formation352_Wings => "3-5-2 (ثلاثي دفاعي مع أجنحة طايرة)",
        FormationType.Formation532_Counter => "5-3-2 (دفاع حديدي محكم مع مرتدات سريعة)",
        FormationType.Formation343_AllOut => "3-4-3 (هجوم كاسح وضغط عالي)",
        _ => "4-3-3"
    };

    public static string ToEnglishName(this FormationType type) => type switch
    {
        FormationType.Formation433_Attack => "4-3-3 Attack",
        FormationType.Formation433_Defend => "4-3-3 Holding",
        FormationType.Formation4231_Wide => "4-2-3-1 Wide",
        FormationType.Formation442_Flat => "4-4-2 Flat",
        FormationType.Formation41212_Diamond => "4-1-2-1-2 Diamond",
        FormationType.Formation352_Wings => "3-5-2 Wings",
        FormationType.Formation532_Counter => "5-3-2 Counter",
        FormationType.Formation343_AllOut => "3-4-3 All Out",
        _ => "4-3-3"
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
