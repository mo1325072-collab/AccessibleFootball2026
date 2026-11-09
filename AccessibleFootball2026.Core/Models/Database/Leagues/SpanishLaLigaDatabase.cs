namespace AccessibleFootball2026.Core.Models;

/// <summary>
/// الدوري الإسباني (La Liga) - موسم 2026/2027 المعتمد رسمياً (20 نادياً)
/// موثق بالكامل من واقع Transfermarkt و SofaScore و 365scores
/// بقوائم موسعة وشاملة (22 إلى 23 لاعباً لكل نادٍ) مع أحدث الانتقالات والنجوم وأرقام القمصان الرسمية
/// </summary>
public static class SpanishLaLigaDatabase
{
    public static List<Team> GetAllClubs()
    {
        var fromJson = JsonDatabaseProvider.GetTeamsByGroup("LaLiga (Spain)");
        if (fromJson != null && fromJson.Count > 0) return fromJson;

        return new List<Team>
        {
            CreateRealMadrid(),
            CreateBarcelona(),
            CreateAtleticoMadrid(),
            CreateAthleticBilbao(),
            CreateRealSociedad(),
            CreateVillarreal(),
            CreateGirona(),
            CreateRealBetis(),
            CreateSevilla(),
            CreateValencia(),
            CreateCeltaVigo(),
            CreateOsasuna(),
            CreateGetafe(),
            CreateMallorca(),
            CreateRayoVallecano(),
            CreateEspanyol(),
            CreateLasPalmas(),
            CreateLeganes(),
            CreateRealValladolid(),
            CreateDeportivoAlaves()
        };
    }

    // 1. ريال مدريد (Real Madrid) - 22 لاعباً
    public static Team CreateRealMadrid()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4001, "تيبو كورتوا", 1, SpecificPosition.GK, 70, 75, 20, 89, 85, null),
            (4002, "فيرلاند ميندي", 23, SpecificPosition.LB, 88, 76, 62, 84, 86, new[] { SpecificPosition.LWB }),
            (4003, "أنطونيو روديجر", 22, SpecificPosition.CB, 84, 71, 55, 87, 89, null),
            (4004, "إيدير ميليتاو", 3, SpecificPosition.CB, 85, 74, 52, 86, 83, new[] { SpecificPosition.RB }),
            (4005, "داني كارفخال", 2, SpecificPosition.RB, 81, 82, 65, 84, 82, new[] { SpecificPosition.RWB }),
            (4006, "أوريلين تشواميني", 14, SpecificPosition.CDM, 79, 82, 72, 85, 87, new[] { SpecificPosition.CB, SpecificPosition.CM }),
            (4007, "فيديريكو فالفيردي", 8, SpecificPosition.CM, 90, 85, 84, 82, 88, new[] { SpecificPosition.RW, SpecificPosition.RB }),
            (4008, "جود بيلينجهام", 5, SpecificPosition.CAM, 85, 88, 87, 80, 86, new[] { SpecificPosition.CM, SpecificPosition.ST }),
            (4009, "فينيسيوس جونيور", 7, SpecificPosition.LW, 96, 82, 85, 34, 74, new[] { SpecificPosition.RW, SpecificPosition.ST }),
            (4010, "كيليان مبابي", 9, SpecificPosition.ST, 97, 82, 91, 38, 79, new[] { SpecificPosition.LW, SpecificPosition.CF }),
            (4011, "رودريجو جويس", 11, SpecificPosition.RW, 90, 81, 82, 42, 68, new[] { SpecificPosition.LW, SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4012, "أندري لونين", 13, SpecificPosition.GK, 68, 74, 18, 83, 80, null),
            (4013, "فران جونزاليس", 26, SpecificPosition.GK, 66, 68, 15, 74, 78, null),
            (4014, "دافيد ألابا", 4, SpecificPosition.CB, 78, 84, 72, 84, 78, new[] { SpecificPosition.LB, SpecificPosition.CDM }),
            (4015, "فران جارسيا", 20, SpecificPosition.LB, 89, 76, 58, 77, 75, null),
            (4016, "لوكاس فاسكيز", 17, SpecificPosition.RB, 82, 80, 74, 78, 75, new[] { SpecificPosition.RW }),
            (4017, "إدواردو كامافينجا", 6, SpecificPosition.CM, 82, 84, 70, 83, 82, new[] { SpecificPosition.CDM, SpecificPosition.LB }),
            (4018, "لوكا مودريتش", 10, SpecificPosition.CM, 72, 91, 76, 70, 65, new[] { SpecificPosition.CAM }),
            (4019, "داني سيبايوس", 19, SpecificPosition.CM, 75, 83, 74, 75, 74, null),
            (4020, "أردا جولر", 15, SpecificPosition.CAM, 78, 86, 81, 45, 62, new[] { SpecificPosition.RW }),
            (4021, "إبراهيم دياز", 21, SpecificPosition.RW, 85, 82, 80, 40, 65, new[] { SpecificPosition.CAM, SpecificPosition.LW }),
            (4022, "إندريك فيليبي", 16, SpecificPosition.ST, 89, 70, 82, 35, 78, null)
        };

        return DatabaseHelpers.CreateTeam("ESP_RMA", "ريال مدريد", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 2. برشلونة (FC Barcelona) - 23 لاعباً
    public static Team CreateBarcelona()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4031, "مارك أندريه تير شتيجن", 1, SpecificPosition.GK, 68, 88, 20, 87, 80, null),
            (4032, "أليخاندرو بالدي", 3, SpecificPosition.LB, 92, 76, 55, 78, 77, null),
            (4033, "باو كوبارسي", 2, SpecificPosition.CB, 76, 84, 45, 83, 76, null),
            (4034, "رونالد أراوخو", 4, SpecificPosition.CB, 83, 72, 48, 86, 87, null),
            (4035, "جول كوندي", 23, SpecificPosition.RB, 84, 76, 45, 85, 81, new[] { SpecificPosition.CB }),
            (4036, "مارك كاسادو", 17, SpecificPosition.CDM, 80, 82, 65, 80, 81, null),
            (4037, "بيدري جونزاليس", 8, SpecificPosition.CM, 82, 90, 78, 72, 68, new[] { SpecificPosition.CAM }),
            (4038, "داني أولمو", 20, SpecificPosition.CAM, 82, 86, 84, 52, 70, new[] { SpecificPosition.CM, SpecificPosition.LW }),
            (4039, "رافينيا دياز", 11, SpecificPosition.LW, 90, 84, 85, 58, 76, new[] { SpecificPosition.RW, SpecificPosition.CAM }),
            (4040, "روبرت ليفاندوفسكي", 9, SpecificPosition.ST, 76, 78, 92, 42, 82, null),
            (4041, "لامين يامال", 19, SpecificPosition.RW, 92, 86, 83, 35, 65, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4042, "إينياكي بينيا", 13, SpecificPosition.GK, 67, 76, 18, 80, 76, null),
            (4043, "فويتشيك تشيزني", 25, SpecificPosition.GK, 66, 75, 18, 84, 82, null),
            (4044, "أندرياس كريستنسن", 15, SpecificPosition.CB, 74, 78, 45, 83, 80, new[] { SpecificPosition.CDM }),
            (4045, "إريك جارسيا", 24, SpecificPosition.CB, 74, 78, 48, 80, 77, new[] { SpecificPosition.RB }),
            (4046, "هيكتور فورت", 32, SpecificPosition.RB, 82, 74, 50, 76, 74, new[] { SpecificPosition.LB }),
            (4047, "جيرارد مارتن", 35, SpecificPosition.LB, 80, 73, 50, 75, 75, null),
            (4048, "فرينكي دي يونج", 21, SpecificPosition.CM, 82, 88, 74, 80, 79, new[] { SpecificPosition.CDM }),
            (4049, "جافي بابلو بايز", 6, SpecificPosition.CM, 82, 83, 76, 78, 82, new[] { SpecificPosition.CAM }),
            (4050, "فيرمين لوبيز", 16, SpecificPosition.CAM, 83, 81, 82, 60, 76, null),
            (4051, "فيران توريس", 7, SpecificPosition.RW, 84, 79, 81, 45, 76, new[] { SpecificPosition.ST, SpecificPosition.LW }),
            (4052, "أنسو فاتي", 10, SpecificPosition.LW, 84, 76, 80, 38, 68, null),
            (4053, "باو فيكتور", 18, SpecificPosition.ST, 82, 72, 78, 40, 76, null)
        };

        return DatabaseHelpers.CreateTeam("ESP_BAR", "برشلونة", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 3. أتلتيكو مدريد (Atletico Madrid) - 22 لاعباً
    public static Team CreateAtleticoMadrid()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4061, "يان أوبلاك", 1, SpecificPosition.GK, 66, 76, 20, 89, 85, null),
            (4062, "رينيلدو ماندافا", 23, SpecificPosition.LB, 84, 72, 45, 84, 85, new[] { SpecificPosition.CB }),
            (4063, "خوسيه ماريا خيمينيز", 2, SpecificPosition.CB, 76, 68, 45, 87, 88, null),
            (4064, "روبين لو نورماند", 24, SpecificPosition.CB, 77, 72, 38, 85, 84, null),
            (4065, "ماركوس يورينتي", 14, SpecificPosition.RB, 89, 80, 78, 81, 84, new[] { SpecificPosition.RWB, SpecificPosition.CM }),
            (4066, "كوكي ريسوريكسيون", 6, SpecificPosition.CDM, 72, 87, 75, 82, 79, new[] { SpecificPosition.CM }),
            (4067, "رودريجو دي بول", 5, SpecificPosition.CM, 78, 86, 79, 79, 82, null),
            (4068, "كونور جالاجير", 4, SpecificPosition.CM, 82, 80, 78, 82, 84, null),
            (4069, "أنطوان جريزمان", 7, SpecificPosition.CAM, 82, 89, 88, 70, 74, new[] { SpecificPosition.ST }),
            (4070, "خوليان ألفاريز", 19, SpecificPosition.ST, 87, 82, 87, 58, 80, new[] { SpecificPosition.CF, SpecificPosition.LW }),
            (4071, "ألكسندر سورلوث", 9, SpecificPosition.ST, 84, 73, 86, 48, 90, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4072, "خوان موسو", 13, SpecificPosition.GK, 68, 75, 18, 82, 80, null),
            (4073, "أكسيل فيتسيل", 20, SpecificPosition.CB, 62, 82, 65, 83, 81, new[] { SpecificPosition.CDM }),
            (4074, "كليمان لينجليه", 15, SpecificPosition.CB, 74, 78, 45, 80, 80, null),
            (4075, "سيزار أزبيليكويتا", 3, SpecificPosition.CB, 72, 76, 52, 81, 79, new[] { SpecificPosition.RB }),
            (4076, "ناهويل مولينا", 16, SpecificPosition.RB, 85, 78, 66, 78, 77, null),
            (4077, "صامويل لينو", 12, SpecificPosition.LM, 86, 80, 78, 65, 76, new[] { SpecificPosition.LWB }),
            (4078, "بابلو باريوس", 8, SpecificPosition.CM, 81, 82, 72, 78, 78, null),
            (4079, "رودريجو ريكيلمي", 17, SpecificPosition.LM, 85, 80, 77, 55, 72, null),
            (4080, "جوليانو سيميوني", 22, SpecificPosition.RW, 86, 76, 76, 58, 78, null),
            (4081, "أنخيل كوريا", 10, SpecificPosition.ST, 84, 81, 82, 48, 75, new[] { SpecificPosition.RW }),
            (4082, "خافي جالان", 21, SpecificPosition.LB, 82, 76, 58, 78, 76, null)
        };

        return DatabaseHelpers.CreateTeam("ESP_ATM", "أتلتيكو مدريد", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 4. أتلتيك بيلباو (Athletic Bilbao) - 22 لاعباً
    public static Team CreateAthleticBilbao()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4091, "أوناي سيمون", 1, SpecificPosition.GK, 66, 82, 18, 86, 80, null),
            (4092, "يوري بيرتشيتشي", 17, SpecificPosition.LB, 82, 78, 70, 81, 84, null),
            (4093, "داني فيفيان", 3, SpecificPosition.CB, 81, 71, 38, 85, 85, null),
            (4094, "يراي ألفاريز", 5, SpecificPosition.CB, 76, 72, 40, 83, 82, null),
            (4095, "أوسكار دي ماركوس", 18, SpecificPosition.RB, 81, 78, 65, 80, 78, null),
            (4096, "إينيجو رويز دي غالاريتا", 16, SpecificPosition.CDM, 75, 84, 70, 80, 76, new[] { SpecificPosition.CM }),
            (4097, "بينات برادوس", 24, SpecificPosition.CM, 81, 81, 68, 81, 80, null),
            (4098, "أويهان سانسيت", 8, SpecificPosition.CAM, 81, 84, 83, 64, 82, new[] { SpecificPosition.CM }),
            (4099, "نيكو ويليامز", 10, SpecificPosition.LW, 94, 81, 81, 40, 75, new[] { SpecificPosition.RW }),
            (4100, "جوركا جوروزيتا", 12, SpecificPosition.ST, 82, 76, 83, 48, 81, null),
            (4101, "إينياكي ويليامز", 9, SpecificPosition.RW, 92, 79, 82, 50, 84, new[] { SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4102, "خولين أغيريزابالا", 13, SpecificPosition.GK, 67, 75, 18, 81, 79, null),
            (4103, "أيتور باريديس", 4, SpecificPosition.CB, 78, 71, 42, 82, 82, null),
            (4104, "أندوني جوروسابيل", 2, SpecificPosition.RB, 82, 75, 52, 78, 77, null),
            (4105, "إينيجو ليكوي", 15, SpecificPosition.RB, 80, 75, 55, 77, 77, new[] { SpecificPosition.LB }),
            (4106, "أداما بويرو", 32, SpecificPosition.LB, 83, 72, 50, 75, 74, null),
            (4107, "ميكل فيسجا", 6, SpecificPosition.CDM, 68, 80, 72, 79, 83, null),
            (4108, "أندير هيريرا", 21, SpecificPosition.CM, 72, 82, 72, 76, 74, null),
            (4109, "ميكيل ياوريجيزار", 28, SpecificPosition.CM, 79, 78, 66, 76, 76, null),
            (4110, "أليكس بيرينجير", 7, SpecificPosition.LW, 84, 80, 79, 52, 72, new[] { SpecificPosition.RW }),
            (4111, "ألفارو دجالو", 11, SpecificPosition.RW, 88, 78, 78, 45, 75, null),
            (4112, "خافيير مارتون", 19, SpecificPosition.ST, 80, 68, 77, 38, 78, null)
        };

        return DatabaseHelpers.CreateTeam("ESP_ATH", "أتلتيك بيلباو", FormationType.Formation4231, false, starters, bench);
    }

    // 5. ريال سوسيداد (Real Sociedad) - 22 لاعباً
    public static Team CreateRealSociedad()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4121, "أليكس ريميرو", 1, SpecificPosition.GK, 68, 82, 18, 85, 80, null),
            (4122, "سيرجيو جوميز", 17, SpecificPosition.LB, 83, 83, 72, 78, 74, new[] { SpecificPosition.LW, SpecificPosition.CM }),
            (4123, "نايف أكرد", 21, SpecificPosition.CB, 79, 74, 45, 84, 85, null),
            (4124, "إيغور زوبيلديا", 5, SpecificPosition.CB, 74, 76, 45, 83, 82, new[] { SpecificPosition.CDM }),
            (4125, "هماري تراوري", 18, SpecificPosition.RB, 83, 76, 55, 80, 80, null),
            (4126, "مارتن زوبيميندي", 4, SpecificPosition.CDM, 78, 85, 72, 85, 81, new[] { SpecificPosition.CM }),
            (4127, "لوكا سوتشيتش", 24, SpecificPosition.CM, 80, 84, 80, 75, 78, null),
            (4128, "برايس مينديز", 23, SpecificPosition.CAM, 79, 86, 83, 70, 76, new[] { SpecificPosition.CM }),
            (4129, "تاكيفوسا كوبو", 14, SpecificPosition.RW, 89, 83, 81, 48, 68, null),
            (4130, "ميكيل أويارزابال", 10, SpecificPosition.LW, 83, 83, 84, 55, 78, new[] { SpecificPosition.ST }),
            (4131, "شيرالدو بيكر", 11, SpecificPosition.ST, 91, 74, 79, 42, 78, new[] { SpecificPosition.RW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4132, "أوناي ماريرو", 13, SpecificPosition.GK, 66, 70, 15, 76, 75, null),
            (4133, "جون باتشيكو", 20, SpecificPosition.CB, 76, 73, 40, 81, 81, null),
            (4134, "أريتز إلوستوندو", 6, SpecificPosition.CB, 75, 72, 45, 80, 81, new[] { SpecificPosition.RB }),
            (4135, "ألفارو أودريوزولا", 2, SpecificPosition.RB, 86, 74, 52, 76, 72, null),
            (4136, "آيهين مونيوز", 3, SpecificPosition.LB, 81, 75, 54, 77, 76, null),
            (4137, "بينات تورينتس", 22, SpecificPosition.CM, 78, 81, 72, 78, 77, null),
            (4138, "بابلو مارين", 28, SpecificPosition.CAM, 80, 79, 74, 55, 70, null),
            (4139, "أرسين زاخاريان", 8, SpecificPosition.CAM, 81, 83, 78, 55, 72, null),
            (4140, "أندير بارينيتشيا", 7, SpecificPosition.LW, 86, 79, 80, 48, 72, null),
            (4141, "أورري أوسكارسون", 9, SpecificPosition.ST, 84, 70, 82, 40, 82, null),
            (4142, "عمر صادق", 19, SpecificPosition.ST, 82, 68, 79, 38, 85, null)
        };

        return DatabaseHelpers.CreateTeam("ESP_RSO", "ريال سوسيداد", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 6. فياريال (Villarreal CF) - 22 لاعباً
    public static Team CreateVillarreal()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4151, "دييجو كوندي", 13, SpecificPosition.GK, 68, 77, 18, 83, 81, null),
            (4152, "سيرجي كاردونا", 23, SpecificPosition.LB, 84, 78, 62, 79, 80, null),
            (4153, "راؤول ألبيول", 3, SpecificPosition.CB, 66, 72, 45, 83, 82, null),
            (4154, "لوجان كوستا", 2, SpecificPosition.CB, 78, 70, 38, 83, 87, null),
            (4155, "كيكو فيمينيا", 17, SpecificPosition.RB, 83, 76, 56, 79, 78, null),
            (4156, "داني باريخو", 10, SpecificPosition.CM, 64, 90, 82, 72, 68, null),
            (4157, "بابي جي", 18, SpecificPosition.CDM, 80, 80, 70, 83, 85, null),
            (4158, "أليكس باينا", 16, SpecificPosition.LM, 84, 88, 83, 65, 78, new[] { SpecificPosition.CAM }),
            (4159, "يريمي بينو", 21, SpecificPosition.RM, 86, 80, 81, 55, 72, new[] { SpecificPosition.RW }),
            (4160, "أيوزي بيريز", 22, SpecificPosition.ST, 83, 81, 84, 52, 75, new[] { SpecificPosition.LW }),
            (4161, "ثييرنو باري", 15, SpecificPosition.ST, 86, 70, 82, 42, 86, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4162, "لويز جونيور", 1, SpecificPosition.GK, 68, 74, 18, 81, 80, null),
            (4163, "خوان فويث", 8, SpecificPosition.RB, 80, 78, 52, 83, 82, new[] { SpecificPosition.CB }),
            (4164, "إريك بايلي", 4, SpecificPosition.CB, 81, 68, 40, 81, 84, null),
            (4165, "ويلي كامبوالا", 5, SpecificPosition.CB, 82, 70, 36, 79, 82, null),
            (4166, "باو نافارو", 24, SpecificPosition.CB, 76, 72, 38, 78, 78, null),
            (4167, "سانتي كوميسانيا", 14, SpecificPosition.CM, 78, 81, 74, 80, 82, null),
            (4168, "رامون تيراتس", 20, SpecificPosition.CM, 78, 79, 70, 77, 78, null),
            (4169, "دينيس سواريز", 6, SpecificPosition.CAM, 78, 83, 76, 58, 68, null),
            (4170, "إلياس أخوماش", 11, SpecificPosition.RW, 86, 78, 76, 45, 68, null),
            (4171, "جيرارد مورينو", 7, SpecificPosition.ST, 81, 83, 86, 52, 77, new[] { SpecificPosition.RW }),
            (4172, "نيكولاس بيبي", 19, SpecificPosition.RW, 87, 79, 81, 42, 74, null)
        };

        return DatabaseHelpers.CreateTeam("ESP_VIL", "فياريال", FormationType.Formation442, false, starters, bench);
    }

    // 7. جيرونا (Girona FC) - 22 لاعباً
    public static Team CreateGirona()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4181, "باولو جازانيجا", 13, SpecificPosition.GK, 68, 78, 18, 83, 81, null),
            (4182, "ميغيل جوتيريز", 3, SpecificPosition.LB, 85, 83, 70, 79, 78, new[] { SpecificPosition.LWB }),
            (4183, "دالي بليند", 17, SpecificPosition.CB, 68, 86, 62, 82, 76, new[] { SpecificPosition.LB }),
            (4184, "دافيد لوبيز", 5, SpecificPosition.CB, 68, 78, 55, 82, 82, new[] { SpecificPosition.CDM }),
            (4185, "أليخاندرو فرانسيس", 16, SpecificPosition.RB, 81, 74, 48, 80, 80, new[] { SpecificPosition.CB }),
            (4186, "أوريول روميو", 14, SpecificPosition.CDM, 68, 78, 65, 82, 86, null),
            (4187, "يانجيل هيريرا", 21, SpecificPosition.CM, 79, 81, 78, 82, 85, null),
            (4188, "إيفان مارتن", 23, SpecificPosition.CAM, 80, 84, 78, 72, 76, new[] { SpecificPosition.CM }),
            (4189, "فيكتور تسيهانكوف", 8, SpecificPosition.RW, 86, 84, 83, 52, 72, null),
            (4190, "أبيل رويز", 19, SpecificPosition.ST, 83, 74, 82, 45, 80, null),
            (4191, "برايان جيل", 20, SpecificPosition.LW, 88, 81, 77, 52, 70, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4192, "باو لوبيز", 25, SpecificPosition.GK, 67, 75, 18, 80, 79, null),
            (4193, "لاديسلاف كريتشي", 18, SpecificPosition.CB, 76, 76, 70, 82, 86, new[] { SpecificPosition.CDM }),
            (4194, "خوانبي راميريز", 15, SpecificPosition.CB, 72, 70, 42, 79, 81, null),
            (4195, "أرناو مارتينيز", 4, SpecificPosition.RB, 80, 79, 60, 80, 78, new[] { SpecificPosition.CB }),
            (4196, "جون سوليس", 22, SpecificPosition.CM, 80, 78, 68, 77, 80, null),
            (4197, "دوني فان دي بيك", 6, SpecificPosition.CM, 78, 82, 78, 72, 74, new[] { SpecificPosition.CAM }),
            (4198, "ياسر أسبريلا", 10, SpecificPosition.CAM, 85, 81, 78, 45, 68, new[] { SpecificPosition.RW }),
            (4199, "بورتو مانزانيا", 24, SpecificPosition.RW, 84, 78, 79, 62, 78, null),
            (4200, "أرناو دانجوما", 11, SpecificPosition.LW, 87, 78, 81, 45, 76, new[] { SpecificPosition.ST }),
            (4201, "كريستيان ستواني", 7, SpecificPosition.ST, 70, 72, 84, 45, 83, null),
            (4202, "بويان ميوفسكي", 9, SpecificPosition.ST, 83, 70, 81, 40, 82, null)
        };

        return DatabaseHelpers.CreateTeam("ESP_GIR", "جيرونا", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 8. ريال بيتيس (Real Betis) - 23 لاعباً
    public static Team CreateRealBetis()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4211, "روي سيلفا", 1, SpecificPosition.GK, 68, 77, 18, 84, 80, null),
            (4212, "رومان بيرو", 15, SpecificPosition.LB, 83, 77, 65, 78, 80, null),
            (4213, "ناتان دي سوزا", 6, SpecificPosition.CB, 78, 72, 38, 82, 84, null),
            (4214, "دييجو يورينتي", 3, SpecificPosition.CB, 76, 75, 48, 82, 83, null),
            (4215, "يوسف سابالي", 23, SpecificPosition.RB, 83, 76, 58, 79, 78, null),
            (4216, "مارك روكا", 21, SpecificPosition.CDM, 74, 83, 74, 81, 80, null),
            (4217, "جوني كاردوسو", 4, SpecificPosition.CDM, 79, 81, 70, 83, 82, null),
            (4218, "جيوفاني لو سيلسو", 20, SpecificPosition.CAM, 80, 87, 83, 68, 75, new[] { SpecificPosition.CM }),
            (4219, "بابلو فورنالس", 18, SpecificPosition.RW, 81, 84, 78, 66, 75, null),
            (4220, "فيتور روكي", 8, SpecificPosition.ST, 88, 72, 83, 44, 80, null),
            (4221, "عبد الصمد الزلزولي", 10, SpecificPosition.LW, 90, 78, 80, 48, 74, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4222, "أدريان كاستيو", 13, SpecificPosition.GK, 62, 72, 15, 78, 78, null),
            (4223, "فران فيتيس", 25, SpecificPosition.GK, 66, 68, 15, 74, 74, null),
            (4224, "مارك بارترا", 5, SpecificPosition.CB, 74, 76, 55, 80, 79, null),
            (4225, "ريكاردو رودريجيز", 12, SpecificPosition.LB, 75, 82, 68, 80, 78, new[] { SpecificPosition.CB }),
            (4226, "هيكتور بيليرين", 2, SpecificPosition.RB, 85, 76, 52, 77, 72, null),
            (4227, "ويليام كارفاليو", 14, SpecificPosition.CM, 64, 85, 74, 78, 85, null),
            (4228, "سيرجي ألتيميرا", 16, SpecificPosition.CM, 80, 80, 70, 78, 78, null),
            (4229, "إيسكو ألاركون", 22, SpecificPosition.CAM, 75, 88, 82, 60, 72, null),
            (4230, "خوانمي خيمينيز", 7, SpecificPosition.LW, 82, 77, 81, 48, 72, null),
            (4231, "سيدريك باكامبو", 11, SpecificPosition.ST, 85, 72, 81, 45, 80, null),
            (4232, "أسان دياو", 38, SpecificPosition.RW, 88, 74, 76, 48, 78, null),
            (4233, "إيكر لوسادا", 19, SpecificPosition.CAM, 80, 79, 76, 50, 70, null)
        };

        return DatabaseHelpers.CreateTeam("ESP_BET", "ريال بيتيس", FormationType.Formation4231, false, starters, bench);
    }

    // 9. إشبيلية (Sevilla FC) - 22 لاعباً
    public static Team CreateSevilla()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4241, "أوريان نيلاند", 13, SpecificPosition.GK, 68, 76, 18, 83, 82, null),
            (4242, "أدريا بيدروسا", 3, SpecificPosition.LB, 85, 77, 65, 78, 76, null),
            (4243, "لويك بادي", 22, SpecificPosition.CB, 81, 72, 40, 84, 85, null),
            (4244, "ماركاو تفاريس", 23, SpecificPosition.CB, 74, 70, 42, 82, 85, null),
            (4245, "خوسيه أنخيل كارمونا", 32, SpecificPosition.RB, 82, 75, 54, 79, 80, null),
            (4246, "نيمانيا جوديل", 6, SpecificPosition.CDM, 74, 80, 76, 83, 85, new[] { SpecificPosition.CB }),
            (4247, "ألبرت سامبي لوكونجا", 12, SpecificPosition.CM, 80, 82, 72, 80, 80, null),
            (4248, "ساؤول نيجيز", 17, SpecificPosition.CM, 78, 81, 78, 80, 82, null),
            (4249, "دودي لوكيباكيو", 11, SpecificPosition.RW, 89, 79, 83, 48, 80, null),
            (4250, "إيزاك روميرو", 7, SpecificPosition.ST, 85, 76, 82, 55, 82, null),
            (4251, "شيديرا إيجوكي", 21, SpecificPosition.LW, 91, 78, 78, 42, 70, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4252, "ألفارو فيرنانديز", 1, SpecificPosition.GK, 67, 74, 15, 79, 78, null),
            (4253, "كيكي سالاس", 4, SpecificPosition.CB, 77, 72, 45, 80, 82, new[] { SpecificPosition.LB }),
            (4254, "تانغي نيانزو", 24, SpecificPosition.CB, 78, 70, 38, 80, 83, null),
            (4255, "جونزالو مونتييل", 15, SpecificPosition.RB, 81, 76, 58, 79, 78, null),
            (4256, "خوانلو سانشيز", 26, SpecificPosition.RB, 84, 78, 70, 78, 78, new[] { SpecificPosition.RM }),
            (4257, "لوسيان أجومي", 18, SpecificPosition.CDM, 78, 79, 65, 81, 82, null),
            (4258, "جبريل سو", 20, SpecificPosition.CM, 80, 81, 74, 79, 80, null),
            (4259, "سوسو خيسوس", 10, SpecificPosition.RW, 75, 85, 80, 52, 68, null),
            (4260, "جيرارد بيكي فرنانديز", 14, SpecificPosition.CAM, 82, 80, 78, 52, 70, null),
            (4261, "كيليتشي إيهيناتشو", 9, SpecificPosition.ST, 82, 76, 82, 44, 81, null),
            (4262, "فالنتين باركو", 19, SpecificPosition.LB, 85, 80, 68, 76, 74, null)
        };

        return DatabaseHelpers.CreateTeam("ESP_SEV", "إشبيلية", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 10. فالنسيا (Valencia CF) - 22 لاعباً
    public static Team CreateValencia()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4271, "جيورجي مامارداشفيلي", 25, SpecificPosition.GK, 68, 78, 18, 88, 86, null),
            (4272, "خوسيه جايا", 14, SpecificPosition.LB, 85, 82, 65, 81, 77, null),
            (4273, "كريستيان موسكيرا", 3, SpecificPosition.CB, 81, 73, 38, 84, 83, null),
            (4274, "سيزار تاريجا", 15, SpecificPosition.CB, 76, 68, 38, 82, 85, null),
            (4275, "تييري كوريا", 12, SpecificPosition.RB, 87, 75, 54, 79, 80, null),
            (4276, "بيبي خوسيه بيبيلو", 18, SpecificPosition.CDM, 78, 84, 78, 83, 81, null),
            (4277, "خافي جويرا", 8, SpecificPosition.CM, 82, 83, 78, 78, 82, null),
            (4278, "أندريه ألميدا", 10, SpecificPosition.CAM, 80, 85, 78, 62, 70, new[] { SpecificPosition.CM }),
            (4279, "دييجو لوبيز", 16, SpecificPosition.RW, 86, 79, 80, 56, 75, null),
            (4280, "هوغو دورو", 9, SpecificPosition.ST, 84, 74, 83, 55, 82, null),
            (4281, "لويس ريوخا", 22, SpecificPosition.LW, 86, 80, 78, 52, 74, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4282, "ستولي ديميتريفسكي", 13, SpecificPosition.GK, 68, 76, 18, 82, 80, null),
            (4283, "ياك إيدان جاسينوفسكي", 24, SpecificPosition.CB, 78, 72, 38, 79, 82, new[] { SpecificPosition.LB }),
            (4284, "ديميتري فولكييه", 20, SpecificPosition.RB, 82, 74, 52, 78, 82, null),
            (4285, "خيسوس فازكيز", 21, SpecificPosition.LB, 84, 74, 55, 76, 75, null),
            (4286, "هوجو جيامون", 6, SpecificPosition.CDM, 74, 80, 65, 80, 79, new[] { SpecificPosition.CB }),
            (4287, "إنزو بارينيتشيا", 5, SpecificPosition.CDM, 78, 80, 68, 81, 82, null),
            (4288, "سيرجي كانوس", 7, SpecificPosition.LW, 84, 78, 77, 50, 72, null),
            (4289, "فران بيريز", 23, SpecificPosition.RW, 87, 76, 75, 45, 68, null),
            (4290, "رافا مير", 11, SpecificPosition.ST, 83, 72, 82, 45, 86, null),
            (4291, "داني جوميز", 17, SpecificPosition.ST, 81, 70, 78, 40, 76, null),
            (4292, "خيرمان فاليرا", 30, SpecificPosition.RW, 84, 75, 74, 42, 68, null)
        };

        return DatabaseHelpers.CreateTeam("ESP_VAL", "فالنسيا", FormationType.Formation4231, false, starters, bench);
    }

    // 11. سيلتا فيجو (Celta Vigo) - 22 لاعباً
    public static Team CreateCeltaVigo()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4301, "فيسينتي غوايتا", 13, SpecificPosition.GK, 66, 76, 18, 83, 80, null),
            (4302, "أوسكار مينجويزا", 3, SpecificPosition.RB, 84, 82, 68, 80, 80, new[] { SpecificPosition.RWB, SpecificPosition.RM }),
            (4303, "كارل ستارفلت", 2, SpecificPosition.CB, 76, 70, 40, 82, 85, null),
            (4304, "ماركوس ألونسو", 20, SpecificPosition.CB, 74, 81, 72, 80, 80, new[] { SpecificPosition.LB }),
            (4305, "خافيير مانكيو", 22, SpecificPosition.RWB, 82, 75, 52, 78, 78, null),
            (4306, "فران بيلتران", 8, SpecificPosition.CDM, 80, 82, 70, 82, 79, null),
            (4307, "إيلاش موريبا", 6, SpecificPosition.CM, 80, 81, 75, 78, 83, null),
            (4308, "هوجو ألفاريز", 14, SpecificPosition.LWB, 86, 79, 74, 72, 72, null),
            (4309, "ويليوت سويدبيرج", 19, SpecificPosition.LW, 86, 80, 81, 50, 75, null),
            (4310, "بورخا إيجليسياس", 7, SpecificPosition.ST, 80, 74, 84, 52, 86, null),
            (4311, "ياجو أسباس", 10, SpecificPosition.ST, 80, 88, 87, 45, 68, new[] { SpecificPosition.RW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4312, "إيفان فيار", 1, SpecificPosition.GK, 66, 70, 15, 77, 76, null),
            (4313, "كارلوس دومينجيز", 24, SpecificPosition.CB, 75, 70, 36, 79, 81, null),
            (4314, "جايلسون سيكويرا", 16, SpecificPosition.CB, 74, 75, 60, 79, 82, new[] { SpecificPosition.CDM }),
            (4315, "ميهايلو ريستيتش", 21, SpecificPosition.LB, 82, 76, 64, 76, 78, null),
            (4316, "سيرجيو كاريرا", 5, SpecificPosition.RB, 81, 73, 50, 75, 75, null),
            (4317, "هوجو سوتيلو", 28, SpecificPosition.CM, 78, 80, 72, 74, 74, null),
            (4318, "لوكا دي لا توري", 23, SpecificPosition.CM, 80, 80, 72, 72, 75, null),
            (4319, "جوناثان بامبا", 17, SpecificPosition.LW, 86, 79, 78, 48, 74, null),
            (4320, "أناستاسيوس دوفيكاس", 9, SpecificPosition.ST, 84, 70, 82, 42, 82, null),
            (4321, "بابلو دوران", 18, SpecificPosition.ST, 82, 72, 78, 45, 76, null),
            (4322, "فرانكو سيرفي", 11, SpecificPosition.LW, 83, 78, 74, 55, 72, null)
        };

        return DatabaseHelpers.CreateTeam("ESP_CEL", "سيلتا فيجو", FormationType.Formation532, false, starters, bench);
    }

    // 12. أوساسونا (CA Osasuna) - 22 لاعباً
    public static Team CreateOsasuna()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4331, "سيرجيو هيريرا", 1, SpecificPosition.GK, 67, 78, 18, 83, 82, null),
            (4332, "أبيل بريتونيس", 22, SpecificPosition.LB, 84, 77, 62, 78, 80, null),
            (4333, "إنزو بويومو", 24, SpecificPosition.CB, 82, 72, 38, 83, 85, null),
            (4334, "أليخاندرو كاتينا", 4, SpecificPosition.CB, 72, 72, 48, 83, 86, null),
            (4335, "خيسوس أريسو", 12, SpecificPosition.RB, 86, 78, 60, 80, 82, null),
            (4336, "لوكاس تورو", 6, SpecificPosition.CDM, 74, 80, 68, 84, 87, null),
            (4337, "جون مونكايولا", 7, SpecificPosition.CM, 82, 81, 76, 81, 84, null),
            (4338, "أيمار أوروز", 10, SpecificPosition.CAM, 81, 85, 80, 62, 74, null),
            (4339, "بريان ساراجوسا", 19, SpecificPosition.LW, 92, 81, 81, 40, 66, null),
            (4340, "أنتي بوديمير", 17, SpecificPosition.ST, 76, 72, 86, 52, 88, null),
            (4341, "روبين جارسيا", 14, SpecificPosition.RW, 80, 83, 79, 60, 72, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4342, "أيتور فرنانديز", 13, SpecificPosition.GK, 66, 74, 15, 80, 78, null),
            (4343, "خورخي هيراندو", 28, SpecificPosition.CB, 77, 69, 36, 80, 83, null),
            (4344, "خوان كروز", 3, SpecificPosition.LB, 78, 73, 48, 79, 81, new[] { SpecificPosition.CB }),
            (4345, "روبين بينيا", 15, SpecificPosition.RB, 83, 76, 68, 76, 75, new[] { SpecificPosition.RW }),
            (4346, "ناتشو فيدال", 2, SpecificPosition.RB, 80, 73, 50, 76, 78, null),
            (4347, "بابلو إيبانيز", 8, SpecificPosition.CM, 78, 78, 72, 77, 80, null),
            (4348, "موي جوميز", 16, SpecificPosition.CM, 78, 83, 77, 65, 74, new[] { SpecificPosition.LW }),
            (4349, "كيكي بارخا", 11, SpecificPosition.RW, 84, 78, 76, 48, 70, null),
            (4350, "خوسيه أرناييز", 20, SpecificPosition.LW, 84, 77, 78, 45, 74, null),
            (4351, "راؤول جارسيا دي هارو", 23, SpecificPosition.ST, 80, 70, 80, 42, 83, null),
            (4352, "إيكر مونيوز", 18, SpecificPosition.CDM, 76, 78, 64, 78, 80, null)
        };

        return DatabaseHelpers.CreateTeam("ESP_OSA", "أوساسونا", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 13. خيتافي (Getafe CF) - 22 لاعباً
    public static Team CreateGetafe()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4361, "دافيد سوريا", 13, SpecificPosition.GK, 67, 77, 18, 84, 83, null),
            (4362, "دييجو ريكو", 16, SpecificPosition.LB, 81, 79, 64, 80, 81, null),
            (4363, "عمر ألديريتي", 15, SpecificPosition.CB, 76, 72, 50, 83, 87, null),
            (4364, "دجيني داكونام", 2, SpecificPosition.CB, 81, 72, 40, 84, 84, new[] { SpecificPosition.RB, SpecificPosition.CDM }),
            (4365, "خوان إيجليسياس", 21, SpecificPosition.RB, 81, 74, 52, 78, 79, null),
            (4366, "لويس ميلا", 5, SpecificPosition.CDM, 77, 85, 72, 81, 78, new[] { SpecificPosition.CM }),
            (4367, "ماورو أرامباري", 8, SpecificPosition.CM, 79, 82, 78, 83, 86, null),
            (4368, "كريسانتوس أوتشي", 6, SpecificPosition.CM, 82, 78, 76, 80, 87, new[] { SpecificPosition.ST }),
            (4369, "أليكس سولا", 11, SpecificPosition.RW, 87, 78, 74, 62, 76, null),
            (4370, "بورخا مايورال", 9, SpecificPosition.ST, 83, 75, 84, 48, 78, null),
            (4371, "كارليس بيريز", 17, SpecificPosition.LW, 86, 78, 80, 48, 74, new[] { SpecificPosition.RW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4372, "ييرزي ليتاشيك", 1, SpecificPosition.GK, 66, 70, 15, 77, 78, null),
            (4373, "خوان بيروكال", 4, SpecificPosition.CB, 75, 68, 38, 79, 82, null),
            (4374, "دومينغوس دوارتي", 22, SpecificPosition.CB, 72, 68, 40, 80, 83, null),
            (4375, "ألان نيوم", 12, SpecificPosition.RB, 77, 70, 45, 79, 84, null),
            (4376, "فابريتسيو أنجيليري", 3, SpecificPosition.LB, 82, 75, 58, 76, 76, null),
            (4377, "يلو سانتياجو", 20, SpecificPosition.CM, 78, 77, 66, 76, 78, null),
            (4378, "كارليس ألينيا", 18, SpecificPosition.CAM, 78, 82, 76, 58, 72, null),
            (4379, "ألفارو رودريجيز", 7, SpecificPosition.ST, 84, 68, 79, 44, 85, null),
            (4380, "بيرتوج يلديريم", 10, SpecificPosition.ST, 82, 68, 80, 42, 86, null),
            (4381, "بيتر فيديريكو", 19, SpecificPosition.RW, 85, 76, 74, 48, 70, null),
            (4382, "جون باتريك", 23, SpecificPosition.CM, 77, 76, 65, 74, 76, null)
        };

        return DatabaseHelpers.CreateTeam("ESP_GET", "خيتافي", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 14. ريال مايوركا (RCD Mallorca) - 22 لاعباً
    public static Team CreateMallorca()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4391, "دومينيك جريف", 13, SpecificPosition.GK, 67, 76, 18, 83, 82, null),
            (4392, "يوهان موخيكا", 22, SpecificPosition.LB, 88, 78, 65, 78, 79, null),
            (4393, "أنتونيو رايلو", 21, SpecificPosition.CB, 68, 70, 52, 84, 88, null),
            (4394, "مارتن فاليينت", 24, SpecificPosition.CB, 76, 70, 38, 83, 83, null),
            (4395, "بابلو مافيو", 23, SpecificPosition.RB, 84, 76, 58, 81, 82, null),
            (4396, "سامو كوستا", 12, SpecificPosition.CDM, 78, 79, 68, 84, 86, null),
            (4397, "مانو مورلانيس", 8, SpecificPosition.CM, 76, 83, 72, 78, 76, null),
            (4398, "سيرجي داردير", 10, SpecificPosition.CAM, 77, 86, 80, 68, 75, new[] { SpecificPosition.CM }),
            (4399, "داني رودريجيز", 14, SpecificPosition.LM, 80, 80, 77, 66, 78, null),
            (4400, "فيدات موريكي", 7, SpecificPosition.ST, 78, 72, 85, 55, 90, null),
            (4401, "كايل لارين", 17, SpecificPosition.ST, 84, 72, 82, 48, 85, new[] { SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4402, "إيفان كويار", 1, SpecificPosition.GK, 60, 68, 15, 76, 78, null),
            (4403, "خوسيه كوبيتي", 6, SpecificPosition.CB, 76, 68, 38, 80, 84, null),
            (4404, "سيب فان دير هايدن", 4, SpecificPosition.CB, 74, 68, 38, 79, 82, null),
            (4405, "ماتيو موري", 2, SpecificPosition.RB, 82, 74, 52, 76, 74, null),
            (4406, "توني لاتو", 3, SpecificPosition.LB, 83, 76, 58, 78, 76, null),
            (4407, "عمر ماسكاريل", 5, SpecificPosition.CDM, 74, 80, 65, 80, 81, null),
            (4408, "أنتونيو سانشيز", 18, SpecificPosition.CM, 80, 79, 74, 76, 78, null),
            (4409, "روبرت نافارو", 27, SpecificPosition.CAM, 83, 82, 78, 48, 68, null),
            (4410, "تشيكينيو روي", 11, SpecificPosition.LW, 87, 78, 77, 42, 72, null),
            (4411, "تاكوما أسانو", 19, SpecificPosition.RW, 88, 76, 78, 48, 74, null),
            (4412, "عبد الله دياباتي", 20, SpecificPosition.ST, 81, 68, 76, 38, 78, null)
        };

        return DatabaseHelpers.CreateTeam("ESP_MLL", "ريال مايوركا", FormationType.Formation442, false, starters, bench);
    }

    // 15. رايو فايكانو (Rayo Vallecano) - 22 لاعباً
    public static Team CreateRayoVallecano()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4421, "أوجوستو باتاليا", 13, SpecificPosition.GK, 68, 76, 18, 83, 82, null),
            (4422, "بيب تشافاريا", 3, SpecificPosition.LB, 84, 76, 60, 79, 78, null),
            (4423, "عبد المؤمن مومين", 16, SpecificPosition.CB, 82, 70, 42, 83, 85, null),
            (4424, "فلوريان لوجون", 24, SpecificPosition.CB, 74, 78, 66, 84, 86, null),
            (4425, "أندريه راتيو", 20, SpecificPosition.RB, 91, 76, 55, 81, 80, null),
            (4426, "أوسكار فالنتين", 23, SpecificPosition.CDM, 78, 79, 64, 83, 83, null),
            (4427, "أوناي لوبيز", 17, SpecificPosition.CM, 77, 83, 76, 78, 76, null),
            (4428, "إيسي بالازون", 7, SpecificPosition.RW, 84, 83, 82, 60, 75, new[] { SpecificPosition.CAM }),
            (4429, "خورخي دي فروتوس", 19, SpecificPosition.RM, 88, 79, 78, 55, 76, null),
            (4430, "سيرجيو كاميلو", 14, SpecificPosition.ST, 84, 76, 82, 52, 78, null),
            (4431, "ألفارو جارسيا", 18, SpecificPosition.LW, 90, 80, 81, 60, 76, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4432, "داني كارديناس", 1, SpecificPosition.GK, 67, 72, 15, 79, 78, null),
            (4433, "أريداني هيرنانديز", 5, SpecificPosition.CB, 68, 68, 42, 79, 84, null),
            (4434, "ألفونسو إسبينو", 22, SpecificPosition.LB, 83, 76, 60, 78, 80, null),
            (4435, "إيفان باليو", 2, SpecificPosition.RB, 81, 76, 52, 78, 78, null),
            (4436, "باتي سيس", 6, SpecificPosition.CDM, 77, 78, 70, 81, 85, null),
            (4437, "جيرارد جومباو", 4, SpecificPosition.CM, 74, 82, 74, 78, 82, null),
            (4438, "خاميس رودريجيز", 10, SpecificPosition.CAM, 75, 90, 85, 48, 72, null),
            (4439, "أوسكار تريجو", 8, SpecificPosition.CAM, 75, 82, 76, 62, 74, null),
            (4440, "أدريان إمباربا", 21, SpecificPosition.LW, 84, 81, 78, 52, 74, null),
            (4441, "راندي نتيكا", 11, SpecificPosition.ST, 80, 72, 78, 55, 85, null),
            (4442, "سيرجي جوارديولا", 9, SpecificPosition.ST, 80, 72, 79, 44, 80, null)
        };

        return DatabaseHelpers.CreateTeam("ESP_RAY", "رايو فايكانو", FormationType.Formation4231, false, starters, bench);
    }

    // 16. إسبانيول (RCD Espanyol) - 22 لاعباً
    public static Team CreateEspanyol()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4451, "خوان جارسيا", 1, SpecificPosition.GK, 70, 80, 18, 85, 82, null),
            (4452, "كارلوس روميرو", 14, SpecificPosition.LB, 83, 77, 62, 78, 77, null),
            (4453, "لياندرو كابريرا", 6, SpecificPosition.CB, 72, 70, 45, 82, 86, null),
            (4454, "ماراش كومبولا", 4, SpecificPosition.CB, 74, 70, 42, 82, 84, null),
            (4455, "عمر الهلالي", 23, SpecificPosition.RB, 84, 74, 52, 79, 80, null),
            (4456, "أليكس كرال", 20, SpecificPosition.CDM, 81, 80, 68, 82, 84, new[] { SpecificPosition.CM }),
            (4457, "خوسيه جراغيرا", 15, SpecificPosition.CDM, 77, 78, 65, 80, 82, null),
            (4458, "بول لوزانو", 10, SpecificPosition.CM, 78, 81, 72, 78, 76, null),
            (4459, "ألفارو تيخيرو", 12, SpecificPosition.RM, 82, 80, 68, 77, 76, new[] { SpecificPosition.RB }),
            (4460, "خافي بوادو", 7, SpecificPosition.ST, 84, 78, 83, 55, 80, new[] { SpecificPosition.LW }),
            (4461, "وليد شديرة", 16, SpecificPosition.ST, 84, 72, 81, 44, 84, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4462, "فرناندو باتشيكو", 13, SpecificPosition.GK, 66, 74, 15, 78, 78, null),
            (4463, "سيرجي جوميز", 3, SpecificPosition.CB, 70, 72, 40, 79, 81, null),
            (4464, "فيرناندو كاليرو", 5, SpecificPosition.CB, 74, 70, 40, 79, 81, null),
            (4465, "برايان أوليفان", 18, SpecificPosition.LB, 80, 76, 58, 77, 76, null),
            (4466, "ألفارو أجوادو", 8, SpecificPosition.CM, 77, 81, 72, 76, 75, null),
            (4467, "رافاييل باوزا", 37, SpecificPosition.CM, 77, 76, 65, 74, 74, null),
            (4468, "جوفري كاريراس", 17, SpecificPosition.RW, 86, 78, 77, 52, 74, null),
            (4469, "سالفادور سانشيز سالفي", 19, SpecificPosition.RW, 84, 76, 74, 50, 72, null),
            (4470, "إيرفان كاردونا", 24, SpecificPosition.ST, 83, 72, 79, 42, 78, null),
            (4471, "أليخو فيليز", 9, SpecificPosition.ST, 82, 68, 80, 44, 84, null),
            (4472, "أنتوني لوثانو", 11, SpecificPosition.ST, 82, 70, 78, 42, 82, null)
        };

        return DatabaseHelpers.CreateTeam("ESP_ESP", "إسبانيول", FormationType.Formation442, false, starters, bench);
    }

    // 17. لاس بالماس (UD Las Palmas) - 22 لاعباً
    public static Team CreateLasPalmas()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4481, "ياسبر سيلينسن", 1, SpecificPosition.GK, 67, 80, 18, 83, 80, null),
            (4482, "أليكس مونيوز", 23, SpecificPosition.LB, 80, 76, 64, 79, 80, new[] { SpecificPosition.CB }),
            (4483, "سكوت ماكينا", 15, SpecificPosition.CB, 74, 70, 40, 83, 87, null),
            (4484, "أليكس سواريز", 4, SpecificPosition.CB, 76, 72, 42, 82, 83, new[] { SpecificPosition.RB }),
            (4485, "فيتي روزادا", 22, SpecificPosition.RB, 85, 76, 60, 78, 76, null),
            (4486, "داريو إيسوجو", 18, SpecificPosition.CDM, 82, 80, 68, 82, 86, null),
            (4487, "كيريان رودريجيز", 20, SpecificPosition.CM, 78, 87, 80, 77, 78, null),
            (4488, "خوسيه كامبانيا", 8, SpecificPosition.CM, 74, 85, 76, 74, 72, null),
            (4489, "ألبرتو موليرو", 10, SpecificPosition.LW, 86, 85, 81, 52, 70, new[] { SpecificPosition.CAM }),
            (4490, "فابيو سيلفا", 9, SpecificPosition.ST, 84, 76, 82, 45, 79, null),
            (4491, "ساندرو راميريز", 19, SpecificPosition.RW, 85, 80, 82, 54, 78, new[] { SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4492, "دينكو هوركاش", 13, SpecificPosition.GK, 67, 72, 15, 78, 78, null),
            (4493, "ميكا مارمول", 3, SpecificPosition.CB, 80, 80, 45, 81, 80, new[] { SpecificPosition.LB }),
            (4494, "خوانما هيرتزوج", 28, SpecificPosition.CB, 76, 70, 45, 79, 81, null),
            (4495, "مارفين بارك", 2, SpecificPosition.RB, 89, 74, 62, 76, 74, new[] { SpecificPosition.RW }),
            (4496, "دالي سينكجرافين", 11, SpecificPosition.LB, 80, 77, 60, 76, 74, null),
            (4497, "إنزو لواوديس", 12, SpecificPosition.CM, 78, 81, 72, 76, 77, null),
            (4498, "عدنان يانوزاي", 7, SpecificPosition.RW, 81, 83, 78, 44, 70, null),
            (4499, "مانو فوستر", 14, SpecificPosition.CAM, 80, 81, 77, 52, 68, null),
            (4500, "بيدرو بيخينو", 24, SpecificPosition.RW, 83, 78, 77, 48, 72, null),
            (4501, "خايمي ماتا", 17, SpecificPosition.ST, 76, 72, 80, 52, 82, null),
            (4502, "مارك كاردونا", 19, SpecificPosition.ST, 81, 72, 78, 44, 78, null)
        };

        return DatabaseHelpers.CreateTeam("ESP_LPA", "لاس بالماس", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 18. ليجانيس (CD Leganes) - 22 لاعباً
    public static Team CreateLeganes()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4511, "ماركو دميتروفيتش", 13, SpecificPosition.GK, 67, 76, 18, 83, 84, null),
            (4512, "خافي هيرنانديز", 20, SpecificPosition.LB, 81, 76, 62, 79, 80, new[] { SpecificPosition.CB }),
            (4513, "ماتيا ناستاسيتش", 22, SpecificPosition.CB, 72, 74, 45, 82, 84, null),
            (4514, "سيرجيو جونزاليس", 6, SpecificPosition.CB, 75, 70, 42, 81, 83, null),
            (4515, "فالنتين روزييه", 2, SpecificPosition.RB, 85, 76, 56, 80, 81, null),
            (4516, "ريناتو تابيا", 5, SpecificPosition.CDM, 78, 81, 70, 84, 85, new[] { SpecificPosition.CB }),
            (4517, "داركو براشاناك", 8, SpecificPosition.CM, 78, 79, 74, 79, 80, null),
            (4518, "سيدوبا سيسي", 17, SpecificPosition.CM, 82, 79, 72, 79, 82, null),
            (4519, "خوان كروز ليجانيس", 11, SpecificPosition.RW, 86, 80, 82, 52, 74, null),
            (4520, "سيباستيان هالير", 18, SpecificPosition.ST, 78, 75, 85, 52, 89, null),
            (4521, "منير الحدادي", 23, SpecificPosition.LW, 83, 81, 81, 50, 72, new[] { SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4522, "خوان سوريانو", 1, SpecificPosition.GK, 67, 72, 15, 78, 78, null),
            (4523, "خورخي ساينز", 4, SpecificPosition.CB, 74, 68, 38, 80, 83, null),
            (4524, "جاكسون بوروزو", 3, SpecificPosition.CB, 78, 67, 36, 79, 84, null),
            (4525, "إنريك فرانكيسا", 15, SpecificPosition.LB, 85, 76, 62, 76, 76, null),
            (4526, "أدريا ألتيميرا", 12, SpecificPosition.RB, 84, 74, 52, 76, 75, null),
            (4527, "إيفان نيو", 14, SpecificPosition.CDM, 80, 79, 68, 81, 83, null),
            (4528, "جوليان تشيكو", 21, SpecificPosition.CM, 76, 78, 68, 76, 79, null),
            (4529, "روبرتو لوبيز", 7, SpecificPosition.CAM, 80, 81, 78, 52, 70, null),
            (4530, "داني رابا", 19, SpecificPosition.RW, 82, 80, 79, 50, 74, null),
            (4531, "دييجو جارسيا", 10, SpecificPosition.ST, 82, 70, 78, 42, 80, null),
            (4532, "ميجيل دي لا فوينتي", 9, SpecificPosition.ST, 84, 71, 79, 45, 82, null)
        };

        return DatabaseHelpers.CreateTeam("ESP_LEG", "ليجانيس", FormationType.Formation4231, false, starters, bench);
    }

    // 19. ريال بلد الوليد (Real Valladolid) - 22 لاعباً
    public static Team CreateRealValladolid()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4541, "كارل هاين", 13, SpecificPosition.GK, 68, 76, 18, 83, 81, null),
            (4542, "لوكاس روزا", 22, SpecificPosition.LB, 83, 76, 60, 78, 79, new[] { SpecificPosition.RB }),
            (4543, "دافيد توريس", 5, SpecificPosition.CB, 76, 72, 38, 81, 82, null),
            (4544, "إيراي كوميرت", 15, SpecificPosition.CB, 75, 73, 45, 82, 84, null),
            (4545, "لويس بيريز", 2, SpecificPosition.RB, 84, 78, 58, 79, 78, null),
            (4546, "ستيب يوريتش", 18, SpecificPosition.CDM, 76, 80, 66, 82, 85, null),
            (4547, "سليم أملاح", 21, SpecificPosition.CM, 80, 82, 79, 78, 84, new[] { SpecificPosition.CAM }),
            (4548, "كيكي بيريز", 8, SpecificPosition.CM, 79, 82, 76, 78, 79, null),
            (4549, "راؤول مورو", 11, SpecificPosition.LW, 89, 79, 78, 45, 70, null),
            (4550, "خوانمي لاتاسا", 14, SpecificPosition.ST, 81, 70, 82, 48, 88, null),
            (4551, "مامادو سيلا", 7, SpecificPosition.RW, 86, 75, 79, 48, 80, new[] { SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4552, "أندريه فيريرا", 1, SpecificPosition.GK, 66, 72, 15, 77, 77, null),
            (4553, "جينك أوزكاجار", 6, SpecificPosition.CB, 77, 72, 40, 80, 83, new[] { SpecificPosition.LB }),
            (4554, "جومارت باه", 24, SpecificPosition.CB, 77, 68, 36, 79, 81, null),
            (4555, "تشوكي إيفان", 28, SpecificPosition.CAM, 81, 79, 75, 48, 68, null),
            (4556, "إيفان سانشيز", 23, SpecificPosition.RW, 83, 80, 76, 52, 70, null),
            (4557, "فيكتور ميسيجير", 4, SpecificPosition.CM, 78, 79, 74, 77, 80, null),
            (4558, "ماريو مارتن", 12, SpecificPosition.CDM, 79, 78, 64, 79, 80, null),
            (4559, "داروين ماتشيس", 10, SpecificPosition.LW, 88, 78, 79, 48, 74, null),
            (4560, "أمات ندياي", 19, SpecificPosition.RW, 87, 76, 76, 52, 76, null),
            (4561, "ماركوس أندريه", 9, SpecificPosition.ST, 83, 72, 80, 42, 81, null),
            (4562, "روبرت كينيدي", 17, SpecificPosition.LW, 83, 77, 78, 48, 78, null)
        };

        return DatabaseHelpers.CreateTeam("ESP_VLL", "بلد الوليد", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 20. ديبورتيفو ألافيس (Deportivo Alaves) - 22 لاعباً
    public static Team CreateDeportivoAlaves()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4571, "أنطونيو سيفيرا", 1, SpecificPosition.GK, 68, 77, 18, 84, 82, null),
            (4572, "مانو سانشيز", 3, SpecificPosition.LB, 83, 78, 62, 79, 78, null),
            (4573, "عبد الكبير أبقار", 5, SpecificPosition.CB, 79, 68, 38, 83, 86, null),
            (4574, "ألكسندر سيدلار", 4, SpecificPosition.CB, 72, 72, 45, 81, 83, null),
            (4575, "ناهويل تيناجليا", 14, SpecificPosition.RB, 81, 75, 55, 81, 83, new[] { SpecificPosition.CB }),
            (4576, "أنطونيو بلانكو", 8, SpecificPosition.CDM, 78, 83, 70, 83, 81, new[] { SpecificPosition.CM }),
            (4577, "أندر جيفارا", 6, SpecificPosition.CM, 77, 82, 72, 81, 80, null),
            (4578, "جون جوريدي", 18, SpecificPosition.CAM, 79, 82, 80, 74, 80, new[] { SpecificPosition.CM }),
            (4579, "توماس كونيخني", 10, SpecificPosition.LW, 86, 79, 81, 55, 78, null),
            (4580, "كيكي جارسيا", 17, SpecificPosition.ST, 76, 73, 84, 55, 87, null),
            (4581, "كارلوس فيسينتي", 7, SpecificPosition.RW, 87, 81, 82, 60, 78, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (4582, "خيسوس أوونو", 13, SpecificPosition.GK, 66, 70, 15, 77, 76, null),
            (4583, "موسى ديارا", 22, SpecificPosition.CB, 78, 70, 38, 80, 83, new[] { SpecificPosition.LB }),
            (4584, "سانتياجو مورينيو", 12, SpecificPosition.CB, 81, 68, 36, 79, 82, new[] { SpecificPosition.RB }),
            (4585, "هوغو نوفوا", 2, SpecificPosition.RB, 86, 75, 62, 75, 72, new[] { SpecificPosition.RW }),
            (4586, "أدريان بيكا", 36, SpecificPosition.CB, 75, 67, 35, 78, 80, null),
            (4587, "خوان جوردان", 24, SpecificPosition.CM, 75, 84, 76, 78, 78, null),
            (4588, "كارلوس بينافيديز", 23, SpecificPosition.CDM, 77, 77, 65, 80, 84, null),
            (4589, "لوكا روميرو", 11, SpecificPosition.RW, 84, 80, 78, 44, 66, null),
            (4590, "ستويتشكوف خوان", 19, SpecificPosition.CAM, 80, 81, 81, 55, 76, new[] { SpecificPosition.ST }),
            (4591, "آسير فيلاليبري", 9, SpecificPosition.ST, 80, 71, 82, 48, 87, null),
            (4592, "توني مارتينيز", 20, SpecificPosition.ST, 82, 70, 81, 44, 82, null)
        };

        return DatabaseHelpers.CreateTeam("ESP_ALA", "ديبورتيفو ألافيس", FormationType.Formation4231, false, starters, bench);
    }
}
