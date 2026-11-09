namespace AccessibleFootball2026.Core.Models;

/// <summary>
/// منتخبات قارة أمريكا الشمالية والوسطى والكاريبي (CONCACAF) المعتمدة رسمياً من فيفا لموسم 2026/2027 (10 منتخبات)
/// موثقة بالكامل من واقع Transfermarkt و SofaScore و 365scores
/// بقوائم موسعة (22 إلى 23 لاعباً لكل منتخب)
/// </summary>
public static class NorthAmericanNationalTeams
{
    public static List<Team> GetAllNationalTeams()
    {
        return new List<Team>
        {
            CreateUSA(),
            CreateMexico(),
            CreateCanada(),
            CreateCostaRica(),
            CreatePanama(),
            CreateJamaica(),
            CreateHonduras(),
            CreateElSalvador(),
            CreateHaiti(),
            CreateTrinidadAndTobago()
        };
    }

    // 1. الولايات المتحدة الأمريكية (USA) - 23 لاعباً
    public static Team CreateUSA()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (13001, "مات تيرنر", 1, SpecificPosition.GK, 60, 78, 18, 83, 82, null),
            (13002, "أنتوني روبنسون", 5, SpecificPosition.LB, 90, 80, 64, 82, 82, new[] { SpecificPosition.LWB }),
            (13003, "تيم ريم", 13, SpecificPosition.CB, 65, 78, 45, 82, 80, null),
            (13004, "كريستوفر ريتشاردز", 3, SpecificPosition.CB, 80, 75, 45, 83, 84, null),
            (13005, "جو سكالي", 22, SpecificPosition.RB, 84, 78, 62, 80, 80, new[] { SpecificPosition.LB }),
            (13006, "تايلر آدامز", 4, SpecificPosition.CDM, 84, 80, 65, 85, 84, new[] { SpecificPosition.CM }),
            (13007, "ويستون ماكيني", 8, SpecificPosition.CM, 82, 83, 80, 82, 86, new[] { SpecificPosition.CAM, SpecificPosition.RM }),
            (13008, "يونس موسى", 6, SpecificPosition.CM, 84, 82, 75, 80, 82, new[] { SpecificPosition.CDM }),
            (13009, "كريستيان بوليسيتش", 10, SpecificPosition.LW, 89, 85, 85, 52, 74, new[] { SpecificPosition.RW, SpecificPosition.CAM }),
            (13010, "فولارين بالوجون", 20, SpecificPosition.ST, 86, 74, 83, 45, 78, null),
            (13011, "تيموثي ويا", 21, SpecificPosition.RW, 91, 79, 78, 65, 78, new[] { SpecificPosition.RWB, SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (13012, "إيثان هورفاث", 18, SpecificPosition.GK, 58, 72, 15, 79, 80, null),
            (13013, "مارك ماكنزي", 16, SpecificPosition.CB, 81, 74, 42, 81, 84, null),
            (13014, "كاميرون كارتر فيكرز", 2, SpecificPosition.CB, 78, 75, 45, 84, 88, null),
            (13015, "كريستوف لوند", 12, SpecificPosition.LB, 84, 75, 58, 77, 78, null),
            (13016, "مارلون فوسي", 23, SpecificPosition.RB, 86, 76, 60, 77, 78, null),
            (13017, "جوني كاردوسو", 15, SpecificPosition.CDM, 79, 82, 74, 82, 83, null),
            (13018, "مالك تيلمان", 17, SpecificPosition.CAM, 83, 84, 83, 65, 80, new[] { SpecificPosition.LW }),
            (13019, "بريندن آرونسون", 11, SpecificPosition.CAM, 85, 80, 76, 62, 72, new[] { SpecificPosition.RW }),
            (13020, "حاجي رايت", 14, SpecificPosition.ST, 85, 74, 82, 48, 86, new[] { SpecificPosition.LW }),
            (13021, "ريكاردو بيبي", 9, SpecificPosition.ST, 84, 74, 83, 42, 80, null),
            (13022, "باتريك شولتي", 25, SpecificPosition.GK, 58, 72, 15, 78, 78, null),
            (13023, "سيرجينو ديست", 7, SpecificPosition.RB, 89, 82, 72, 79, 74, new[] { SpecificPosition.LB })
        };

        return DatabaseHelpers.CreateTeam("NAT_USA", "الولايات المتحدة", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 2. المكسيك (Mexico) - 23 لاعباً
    public static Team CreateMexico()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (13031, "لويس مالاجون", 1, SpecificPosition.GK, 60, 79, 18, 84, 82, null),
            (13032, "خيسوس غالاردو", 23, SpecificPosition.LB, 83, 78, 70, 79, 80, new[] { SpecificPosition.LWB, SpecificPosition.LM }),
            (13033, "يوهان فاسكيز", 5, SpecificPosition.CB, 79, 75, 48, 84, 85, new[] { SpecificPosition.LB }),
            (13034, "سيزار مونتيس", 3, SpecificPosition.CB, 76, 76, 45, 84, 88, null),
            (13035, "خورخي سانشيز", 2, SpecificPosition.RB, 85, 77, 60, 80, 82, new[] { SpecificPosition.RWB }),
            (13036, "إديسون ألفاريز", 4, SpecificPosition.CDM, 76, 82, 68, 86, 88, new[] { SpecificPosition.CB }),
            (13037, "لويس تشافيز", 18, SpecificPosition.CM, 78, 86, 84, 80, 80, null),
            (13038, "أوربيلين بينيدا", 17, SpecificPosition.CAM, 83, 84, 80, 68, 74, new[] { SpecificPosition.LW, SpecificPosition.CM }),
            (13039, "ألكسيس فيجا", 10, SpecificPosition.LW, 85, 83, 83, 52, 78, new[] { SpecificPosition.ST }),
            (13040, "سانتياغو خيمينيز", 11, SpecificPosition.ST, 84, 76, 86, 45, 84, null),
            (13041, "سيسار هويرتا", 21, SpecificPosition.RW, 87, 81, 80, 52, 75, new[] { SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (13042, "غييرمو أوتشوا", 13, SpecificPosition.GK, 58, 78, 18, 82, 78, null),
            (13043, "إسرائيل رييس", 15, SpecificPosition.CB, 80, 74, 42, 80, 82, new[] { SpecificPosition.RB }),
            (13044, "خيسوس أوروزكو", 6, SpecificPosition.CB, 78, 73, 40, 80, 82, new[] { SpecificPosition.LB }),
            (13045, "خيراردو أرتيجا", 19, SpecificPosition.LB, 85, 78, 64, 78, 78, null),
            (13046, "جوليان أراوخو", 16, SpecificPosition.RB, 86, 78, 60, 78, 79, null),
            (13047, "إريك ليرة", 8, SpecificPosition.CDM, 79, 80, 68, 81, 82, null),
            (13048, "كارلوس رودريجيز", 14, SpecificPosition.CM, 78, 83, 75, 78, 74, null),
            (13049, "روبرتو ألفارادو", 25, SpecificPosition.RW, 84, 82, 80, 58, 76, new[] { SpecificPosition.CAM }),
            (13050, "دييجو لاينيز", 20, SpecificPosition.RW, 87, 80, 76, 45, 68, null),
            (13051, "هنري مارتين", 9, SpecificPosition.ST, 80, 77, 84, 55, 85, null),
            (13052, "راؤول رانخيل", 12, SpecificPosition.GK, 58, 72, 15, 79, 79, null),
            (13053, "جوليان كينونيس", 7, SpecificPosition.LW, 88, 78, 83, 52, 84, new[] { SpecificPosition.ST })
        };

        return DatabaseHelpers.CreateTeam("NAT_MEX", "المكسيك", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 3. كندا (Canada) - 22 لاعباً
    public static Team CreateCanada()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (13061, "ماكسيم كريبو", 16, SpecificPosition.GK, 60, 78, 18, 84, 83, null),
            (13062, "ألفونسو ديفيز", 19, SpecificPosition.LB, 95, 82, 74, 83, 82, new[] { SpecificPosition.LM, SpecificPosition.LW }),
            (13063, "ديريك كورنيليوس", 13, SpecificPosition.CB, 78, 74, 40, 83, 86, null),
            (13064, "مويس بومبيتو", 15, SpecificPosition.CB, 88, 74, 40, 83, 85, null),
            (13065, "أليستير جونستون", 2, SpecificPosition.RB, 84, 79, 62, 81, 83, new[] { SpecificPosition.RWB, SpecificPosition.CB }),
            (13066, "ستيفن أوستاكيو", 7, SpecificPosition.CM, 78, 84, 76, 82, 81, new[] { SpecificPosition.CDM }),
            (13067, "إسماعيل كونيه", 8, SpecificPosition.CM, 82, 82, 75, 80, 83, null),
            (13068, "جوناثان أوسوريو", 21, SpecificPosition.CM, 78, 82, 76, 78, 78, null),
            (13069, "جاكوب شافلبورغ", 14, SpecificPosition.LW, 91, 79, 78, 55, 76, new[] { SpecificPosition.LM }),
            (13070, "جوناثان ديفيد", 10, SpecificPosition.ST, 86, 79, 86, 45, 80, new[] { SpecificPosition.CF }),
            (13071, "كايل لارين", 9, SpecificPosition.ST, 83, 76, 83, 48, 85, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (13072, "داين سانت كلير", 1, SpecificPosition.GK, 60, 76, 15, 81, 82, null),
            (13073, "كمال ميلر", 4, SpecificPosition.CB, 76, 74, 45, 80, 84, new[] { SpecificPosition.LB }),
            (13074, "لوك دي فوجيرول", 5, SpecificPosition.CB, 78, 72, 38, 79, 80, null),
            (13075, "ريتشي لاريا", 22, SpecificPosition.RB, 87, 77, 68, 78, 79, new[] { SpecificPosition.LB, SpecificPosition.RM }),
            (13076, "صامويل أديكوجبي", 3, SpecificPosition.LB, 85, 76, 60, 77, 77, null),
            (13077, "صامويل بييت", 6, SpecificPosition.CDM, 74, 78, 62, 80, 84, null),
            (13078, "ماثيو شوينيير", 11, SpecificPosition.CM, 80, 80, 74, 77, 77, null),
            (13079, "علي أحمد", 20, SpecificPosition.LM, 87, 77, 72, 68, 74, new[] { SpecificPosition.CM }),
            (13080, "تاني أولواسيي", 25, SpecificPosition.ST, 85, 72, 80, 42, 82, null),
            (13081, "ثيو بير", 17, SpecificPosition.ST, 82, 70, 79, 42, 84, null),
            (13082, "توماس هاسال", 23, SpecificPosition.GK, 56, 68, 15, 75, 75, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_CAN", "كندا", FormationType.Formation442, true, starters, bench);
    }

    // 4. كوستاريكا (Costa Rica) - 22 لاعباً
    public static Team CreateCostaRica()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (13091, "باتريك سيكويرا", 23, SpecificPosition.GK, 60, 78, 18, 84, 82, null),
            (13092, "جوزيف مورا", 8, SpecificPosition.LWB, 83, 76, 62, 78, 78, new[] { SpecificPosition.LB }),
            (13093, "فرانسيسكو كالفو", 15, SpecificPosition.CB, 76, 78, 62, 83, 84, new[] { SpecificPosition.LB }),
            (13094, "خوان بابلو فارغاس", 3, SpecificPosition.CB, 74, 74, 45, 83, 86, null),
            (13095, "جاييل كاسكوس", 4, SpecificPosition.CB, 78, 72, 40, 81, 84, null),
            (13096, "هاكسل كيروس", 22, SpecificPosition.RWB, 85, 76, 60, 78, 78, new[] { SpecificPosition.RB }),
            (13097, "أورلاندو جالو", 14, SpecificPosition.CDM, 80, 81, 70, 82, 84, null),
            (13098, "براندون أغيليرا", 10, SpecificPosition.CM, 80, 84, 79, 68, 75, new[] { SpecificPosition.CAM }),
            (13099, "جيفرسون برينيس", 13, SpecificPosition.CM, 78, 80, 74, 80, 82, null),
            (13100, "مانفرد أوغالدي", 9, SpecificPosition.ST, 84, 76, 83, 52, 78, null),
            (13101, "ألفارو زامورا", 11, SpecificPosition.LW, 86, 78, 78, 50, 74, new[] { SpecificPosition.RW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (13102, "كيفين تشامورو", 1, SpecificPosition.GK, 58, 74, 15, 80, 80, null),
            (13103, "خوليو كاسكانتي", 6, SpecificPosition.CB, 76, 72, 42, 80, 83, null),
            (13104, "فرنان فافرون", 5, SpecificPosition.CB, 76, 72, 40, 79, 82, null),
            (13105, "جيرالد تيلور", 2, SpecificPosition.RB, 85, 75, 58, 77, 78, null),
            (13106, "أليخاندرو بران", 16, SpecificPosition.CM, 78, 78, 72, 78, 80, null),
            (13107, "خوسيمار ألكوسير", 17, SpecificPosition.LW, 87, 77, 76, 45, 74, null),
            (13108, "وارن مادريغال", 12, SpecificPosition.ST, 85, 74, 79, 45, 76, new[] { SpecificPosition.RW }),
            (13109, "كينيث فارغاس", 21, SpecificPosition.RW, 88, 76, 78, 48, 75, null),
            (13110, "أنتوني كونتريراس", 7, SpecificPosition.ST, 83, 72, 80, 45, 81, null),
            (13111, "جويل كامبل", 12, SpecificPosition.RW, 78, 83, 81, 52, 78, new[] { SpecificPosition.ST }),
            (13112, "ألكسندر ليزانو", 18, SpecificPosition.GK, 56, 68, 15, 75, 75, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_CRC", "كوستاريكا", FormationType.Formation352, true, starters, bench);
    }

    // 5. بنما (Panama) - 22 لاعباً
    public static Team CreatePanama()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (13121, "أورلاندو موسكيرا", 22, SpecificPosition.GK, 60, 78, 18, 83, 82, null),
            (13122, "إريك ديفيس", 15, SpecificPosition.LWB, 82, 80, 72, 79, 79, new[] { SpecificPosition.LB }),
            (13123, "كارلوس هارفي", 4, SpecificPosition.CB, 78, 74, 45, 82, 85, null),
            (13124, "خوسيه كوردوبا", 3, SpecificPosition.CB, 80, 76, 40, 84, 85, new[] { SpecificPosition.LB }),
            (13125, "إدغاردو فارينا", 24, SpecificPosition.CB, 78, 73, 40, 82, 86, null),
            (13126, "أمير موريلو", 23, SpecificPosition.RWB, 85, 78, 68, 80, 81, new[] { SpecificPosition.RB }),
            (13127, "أدالبيرتو كاراسكييا", 8, SpecificPosition.CM, 82, 86, 78, 80, 78, new[] { SpecificPosition.CAM }),
            (13128, "كريستيان مارتينيز", 6, SpecificPosition.CM, 80, 81, 72, 81, 82, null),
            (13129, "إدغار بارسيناس", 10, SpecificPosition.LW, 85, 83, 80, 58, 76, new[] { SpecificPosition.RW, SpecificPosition.CAM }),
            (13130, "خوسيه فاخاردو", 17, SpecificPosition.ST, 84, 74, 82, 50, 84, null),
            (13131, "إسماعيل دياز", 11, SpecificPosition.RW, 86, 80, 82, 50, 78, new[] { SpecificPosition.LW, SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (13132, "سيزار ساموديو", 1, SpecificPosition.GK, 58, 72, 15, 78, 78, null),
            (13133, "إيفان أندرسون", 2, SpecificPosition.CB, 80, 72, 40, 78, 80, null),
            (13134, "روديريك ميلر", 16, SpecificPosition.CB, 74, 71, 40, 79, 83, null),
            (13135, "عمر فالنسيا", 14, SpecificPosition.LB, 83, 74, 55, 76, 76, null),
            (13136, "عبد المرويس", 5, SpecificPosition.CDM, 78, 78, 68, 79, 82, null),
            (13137, "يوفاني ولش", 7, SpecificPosition.CM, 78, 79, 70, 79, 81, null),
            (13138, "سيزار يانيس", 13, SpecificPosition.CAM, 82, 81, 77, 52, 74, null),
            (13139, "فريدي جوندولا", 21, SpecificPosition.LW, 86, 77, 76, 45, 75, null),
            (13140, "إدواردو غيريرو", 9, SpecificPosition.ST, 85, 74, 81, 45, 82, null),
            (13141, "توماس رودريجيز", 19, SpecificPosition.ST, 82, 70, 78, 40, 80, null),
            (13142, "إدي رودريجيز", 12, SpecificPosition.GK, 56, 68, 15, 75, 75, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_PAN", "بنما", FormationType.Formation343, true, starters, bench);
    }

    // 6. جامايكا (Jamaica) - 22 لاعباً
    public static Team CreateJamaica()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (13151, "أندريه بليك", 1, SpecificPosition.GK, 60, 79, 18, 85, 84, null),
            (13152, "جريج لي", 3, SpecificPosition.LB, 84, 78, 66, 79, 80, new[] { SpecificPosition.LWB }),
            (13153, "إيثان بينوك", 5, SpecificPosition.CB, 77, 76, 45, 85, 88, null),
            (13154, "داميون لوي", 17, SpecificPosition.CB, 78, 73, 40, 82, 86, null),
            (13155, "ديكستر ليمبيكيزا", 2, SpecificPosition.RB, 86, 76, 58, 78, 79, null),
            (13156, "كيسي بالمر", 8, SpecificPosition.CM, 80, 84, 78, 76, 78, new[] { SpecificPosition.CAM }),
            (13157, "جويل لاتيبوديير", 6, SpecificPosition.CDM, 79, 78, 60, 82, 83, new[] { SpecificPosition.CB }),
            (13158, "بوبي ديكوردوفا ريد", 10, SpecificPosition.CAM, 84, 82, 80, 68, 76, new[] { SpecificPosition.ST, SpecificPosition.RW }),
            (13159, "ديماراي غراي", 7, SpecificPosition.LW, 89, 81, 82, 50, 74, new[] { SpecificPosition.RW }),
            (13160, "ميخائيل أنطونيو", 9, SpecificPosition.ST, 85, 75, 85, 55, 89, null),
            (13161, "ليون بايلي", 11, SpecificPosition.RW, 91, 83, 84, 45, 76, new[] { SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (13162, "جاهمايل وايت", 13, SpecificPosition.GK, 58, 72, 15, 78, 78, null),
            (13163, "ريتشارد كينغ", 4, SpecificPosition.CB, 76, 71, 38, 79, 82, null),
            (13164, "ديشون برنارد", 19, SpecificPosition.CB, 78, 72, 40, 81, 83, new[] { SpecificPosition.RB }),
            (13165, "أماري بيل", 20, SpecificPosition.LB, 83, 76, 60, 78, 78, null),
            (13166, "تايفون غراي", 22, SpecificPosition.RB, 84, 74, 55, 76, 76, null),
            (13167, "كاروي أندرسون", 15, SpecificPosition.CM, 80, 80, 72, 78, 80, null),
            (13168, "دانييل جونسون", 14, SpecificPosition.CM, 78, 81, 76, 76, 76, null),
            (13169, "رينالدو سيفاس", 16, SpecificPosition.LW, 88, 76, 75, 42, 74, null),
            (13170, "شامار نيكولسون", 11, SpecificPosition.ST, 80, 72, 82, 45, 87, null),
            (13171, "كافيه ستيوارت", 18, SpecificPosition.ST, 83, 70, 78, 40, 80, null),
            (13172, "كونيا بويس كلارك", 23, SpecificPosition.GK, 56, 68, 15, 75, 75, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_JAM", "جامايكا", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 7. هندوراس (Honduras) - 22 لاعباً
    public static Team CreateHonduras()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (13181, "إدريك مينخيفار", 1, SpecificPosition.GK, 60, 77, 18, 82, 82, null),
            (13182, "جوزيف روزاليس", 8, SpecificPosition.LB, 84, 78, 65, 78, 79, new[] { SpecificPosition.LM, SpecificPosition.CM }),
            (13183, "لويس فيجا", 4, SpecificPosition.CB, 76, 73, 42, 82, 85, new[] { SpecificPosition.LB }),
            (13184, "ديلفر مالدونادو", 2, SpecificPosition.CB, 78, 74, 40, 83, 85, null),
            (13185, "أندي ناخار", 14, SpecificPosition.RB, 85, 81, 72, 79, 78, new[] { SpecificPosition.RM, SpecificPosition.RWB }),
            (13186, "ديبي فلوريس", 20, SpecificPosition.CDM, 79, 81, 70, 83, 84, null),
            (13187, "خورخي ألفاريز", 23, SpecificPosition.CM, 80, 83, 76, 78, 76, null),
            (13188, "إدوين رودريجيز", 16, SpecificPosition.CAM, 83, 83, 80, 62, 75, new[] { SpecificPosition.RW }),
            (13189, "لويس بالما", 17, SpecificPosition.LW, 86, 83, 82, 52, 75, new[] { SpecificPosition.RW }),
            (13190, "أنتوني لوزانو شوكو", 9, SpecificPosition.ST, 82, 76, 82, 52, 84, null),
            (13191, "ريغوبيرتو ريفاس", 7, SpecificPosition.RW, 86, 80, 79, 55, 78, new[] { SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (13192, "مارلون ليكونيا", 12, SpecificPosition.GK, 58, 72, 15, 78, 78, null),
            (13193, "ديفروين نونيز", 3, SpecificPosition.CB, 76, 71, 38, 79, 82, null),
            (13194, "خوليان مارتينيز", 5, SpecificPosition.CB, 78, 72, 40, 79, 81, new[] { SpecificPosition.LB }),
            (13195, "ويسلي ديكاس", 15, SpecificPosition.LB, 82, 74, 55, 77, 78, null),
            (13196, "مايكل سانتوس", 6, SpecificPosition.RB, 83, 73, 52, 76, 76, null),
            (13197, "كارلوس بينيدا", 18, SpecificPosition.CDM, 77, 78, 66, 79, 80, null),
            (13198, "ألكسندر لوبيز", 10, SpecificPosition.CAM, 76, 84, 80, 52, 72, null),
            (13199, "ميشيل تشيرينوس", 11, SpecificPosition.LW, 87, 78, 77, 45, 68, null),
            (13200, "دوغلاس مارتينيز", 21, SpecificPosition.ST, 83, 71, 78, 42, 80, null),
            (13201, "برايان روشيز", 19, SpecificPosition.ST, 80, 72, 80, 42, 82, null),
            (13202, "لويس لوبيز", 22, SpecificPosition.GK, 58, 74, 15, 79, 79, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_HON", "هندوراس", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 8. السلفادور (El Salvador) - 22 لاعباً
    public static Team CreateElSalvador()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (13211, "ماريو جونزاليس", 1, SpecificPosition.GK, 60, 76, 18, 82, 81, null),
            (13212, "ألكسندر لارين", 13, SpecificPosition.LB, 81, 78, 68, 77, 76, new[] { SpecificPosition.LWB }),
            (13213, "رودي كلافييل", 3, SpecificPosition.CB, 76, 72, 40, 81, 84, null),
            (13214, "إريك كابالكويتا", 4, SpecificPosition.CB, 75, 73, 40, 81, 85, null),
            (13215, "برايان تاماكاس", 21, SpecificPosition.RB, 84, 76, 58, 78, 78, new[] { SpecificPosition.RWB }),
            (13216, "نارسيسو أوريانا", 6, SpecificPosition.CDM, 78, 80, 68, 82, 82, null),
            (13217, "كريستيان مارتينيز", 5, SpecificPosition.CM, 78, 80, 72, 80, 81, new[] { SpecificPosition.CDM }),
            (13218, "هارولد أوسوريو", 14, SpecificPosition.CM, 80, 81, 75, 76, 76, null),
            (13219, "خايرو هنريكيز", 7, SpecificPosition.RW, 86, 79, 78, 52, 74, new[] { SpecificPosition.LW }),
            (13220, "نيلسون بونيلا", 9, SpecificPosition.ST, 80, 74, 81, 45, 82, null),
            (13221, "كيفين رييس", 17, SpecificPosition.LW, 86, 77, 76, 48, 72, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (13222, "توماس روميرو", 12, SpecificPosition.GK, 58, 72, 15, 78, 78, null),
            (13223, "روبرتو دومينغيز", 2, SpecificPosition.CB, 74, 70, 38, 79, 82, null),
            (13224, "ميلفين كروز", 19, SpecificPosition.CB, 75, 70, 38, 78, 80, null),
            (13225, "دييجو فلوريس", 15, SpecificPosition.LB, 82, 73, 52, 75, 75, null),
            (13226, "تيرسو فلوريس", 18, SpecificPosition.RB, 82, 73, 50, 75, 75, null),
            (13227, "ميلفين كارتاخينا", 8, SpecificPosition.CM, 78, 78, 70, 77, 78, null),
            (13228, "ليوناردو مينخيفار", 10, SpecificPosition.CAM, 84, 80, 76, 48, 68, null),
            (13229, "سانتوس أورتيز", 11, SpecificPosition.RW, 84, 76, 74, 45, 72, null),
            (13230, "ستيفيل فاسكيز", 20, SpecificPosition.ST, 82, 70, 78, 40, 80, null),
            (13231, "برايان جيل", 22, SpecificPosition.ST, 83, 71, 79, 42, 82, null),
            (13232, "كيفين كارابانتيس", 23, SpecificPosition.GK, 56, 68, 15, 74, 75, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_SLV", "السلفادور", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 9. هايتي (Haiti) - 22 لاعباً
    public static Team CreateHaiti()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (13241, "جوني بلاسيد", 1, SpecificPosition.GK, 60, 78, 18, 82, 82, null),
            (13242, "أليكس كريستيان", 22, SpecificPosition.LB, 84, 76, 60, 78, 78, null),
            (13243, "ريكاردو أدي", 4, SpecificPosition.CB, 76, 73, 42, 82, 86, null),
            (13244, "جان كيفين دوفرن", 6, SpecificPosition.CB, 78, 73, 40, 81, 84, null),
            (13245, "كارلينس أركوس", 2, SpecificPosition.RB, 85, 77, 60, 79, 80, new[] { SpecificPosition.RWB }),
            (13246, "دانلي جان جاك", 14, SpecificPosition.CDM, 80, 82, 72, 82, 84, new[] { SpecificPosition.CM }),
            (13247, "كارل فريد سانتيه", 17, SpecificPosition.CM, 80, 80, 72, 80, 82, null),
            (13248, "كريستوفر أتيوس", 8, SpecificPosition.CAM, 82, 82, 78, 55, 74, null),
            (13249, "ديريك إتيان جونيور", 10, SpecificPosition.LW, 86, 80, 79, 52, 75, new[] { SpecificPosition.RW }),
            (13250, "فرانتزدي بيروت", 20, SpecificPosition.ST, 80, 72, 85, 48, 91, null),
            (13251, "دوكنز نازون", 9, SpecificPosition.ST, 83, 76, 84, 48, 86, new[] { SpecificPosition.RW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (13252, "ألكسندر بيير", 12, SpecificPosition.GK, 58, 72, 15, 78, 78, null),
            (13253, "جارفين ميتوشيلكا", 3, SpecificPosition.CB, 76, 71, 38, 78, 82, null),
            (13254, "فرانسوا دولسي", 5, SpecificPosition.CB, 75, 70, 38, 78, 81, null),
            (13255, "مارتن إكسبيرينس", 13, SpecificPosition.LB, 82, 73, 52, 75, 75, null),
            (13256, "ستيفين ساباه", 7, SpecificPosition.CM, 78, 78, 70, 77, 78, null),
            (13257, "بريان ألسيوس", 21, SpecificPosition.CDM, 76, 76, 64, 78, 81, null),
            (13258, "فابريس بيكو", 11, SpecificPosition.RW, 87, 78, 78, 48, 74, new[] { SpecificPosition.ST }),
            (13259, "موندسي برونير", 18, SpecificPosition.ST, 84, 71, 78, 40, 82, null),
            (13260, "لوينسي كانتيف", 19, SpecificPosition.LW, 86, 75, 75, 42, 72, null),
            (13261, "داهلينسي بيير", 15, SpecificPosition.ST, 82, 70, 76, 40, 78, null),
            (13262, "جوزويه دوفرجر", 23, SpecificPosition.GK, 56, 68, 15, 75, 75, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_HAI", "هايتي", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 10. ترينيداد وتوباغو (Trinidad and Tobago) - 22 لاعباً
    public static Team CreateTrinidadAndTobago()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (13271, "دينزل سميث", 22, SpecificPosition.GK, 60, 76, 18, 81, 82, null),
            (13272, "أندريه ريموند", 3, SpecificPosition.LB, 84, 76, 60, 78, 78, null),
            (13273, "أوبري ديفيد", 2, SpecificPosition.CB, 78, 74, 45, 82, 85, new[] { SpecificPosition.RB }),
            (13274, "شيلدون باتو", 4, SpecificPosition.CB, 72, 73, 45, 81, 85, null),
            (13275, "ألفين جونز", 16, SpecificPosition.RB, 82, 78, 72, 79, 82, new[] { SpecificPosition.RWB }),
            (13276, "أندريه رامبرساد", 18, SpecificPosition.CDM, 78, 80, 68, 81, 83, null),
            (13277, "نيفييل هاكشو", 15, SpecificPosition.CM, 78, 79, 70, 80, 83, new[] { SpecificPosition.CB }),
            (13278, "دوان موكيت", 8, SpecificPosition.CAM, 81, 81, 76, 60, 74, null),
            (13279, "ريال غارسيا", 11, SpecificPosition.LW, 86, 78, 76, 48, 72, null),
            (13280, "ليفي جارسيا", 10, SpecificPosition.ST, 89, 78, 85, 52, 87, new[] { SpecificPosition.RW }),
            (13281, "كيفون غودارد", 7, SpecificPosition.RW, 85, 77, 75, 50, 74, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (13282, "كريستوفر بيغوت", 1, SpecificPosition.GK, 58, 72, 15, 78, 78, null),
            (13283, "جوستن غارسيا", 17, SpecificPosition.CB, 76, 71, 38, 79, 82, null),
            (13284, "شانون غوميز", 6, SpecificPosition.RB, 84, 74, 55, 76, 76, new[] { SpecificPosition.LB }),
            (13285, "روسيل فيليبس", 5, SpecificPosition.LB, 81, 73, 50, 76, 76, null),
            (13286, "دانيل فيليبس", 14, SpecificPosition.CDM, 78, 78, 68, 79, 81, null),
            (13287, "أجاني فورشن", 20, SpecificPosition.CM, 80, 79, 72, 76, 76, null),
            (13288, "ستيفون غيلين", 13, SpecificPosition.CAM, 80, 79, 74, 50, 70, null),
            (13289, "مالكوم شو", 19, SpecificPosition.ST, 80, 70, 78, 42, 85, null),
            (13290, "ريان تيلفر", 9, SpecificPosition.ST, 83, 74, 79, 45, 80, new[] { SpecificPosition.LW }),
            (13291, "ناتانييل جيمس", 23, SpecificPosition.LW, 86, 76, 75, 42, 70, null),
            (13292, "جاباري سانت هيلير", 21, SpecificPosition.GK, 56, 68, 15, 75, 75, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_TRI", "ترينيداد وتوباغو", FormationType.Formation433_Holding, true, starters, bench);
    }
}
