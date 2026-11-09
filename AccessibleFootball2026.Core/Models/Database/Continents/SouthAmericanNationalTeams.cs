namespace AccessibleFootball2026.Core.Models;

/// <summary>
/// منتخبات قارة أمريكا الجنوبية (CONMEBOL) المعتمدة رسمياً من فيفا لموسم 2026/2027 (10 منتخبات)
/// موثقة بالكامل من واقع Transfermarkt و SofaScore و 365scores
/// بقوائم موسعة (22 إلى 23 لاعباً لكل منتخب)
/// </summary>
public static class SouthAmericanNationalTeams
{
    public static List<Team> GetAllNationalTeams()
    {
        return new List<Team>
        {
            CreateArgentina(),
            CreateBrazil(),
            CreateUruguay(),
            CreateColombia(),
            CreateEcuador(),
            CreateChile(),
            CreateParaguay(),
            CreatePeru(),
            CreateVenezuela(),
            CreateBolivia()
        };
    }

    // 1. الأرجنتين (Argentina) - 23 لاعباً
    public static Team CreateArgentina()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (12001, "إيميليانو مارتينيز", 23, SpecificPosition.GK, 60, 84, 18, 90, 87, null),
            (12002, "نيكولاس تاليافيكو", 3, SpecificPosition.LB, 80, 78, 65, 82, 80, new[] { SpecificPosition.LWB }),
            (12003, "ليساندرو مارتينيز", 25, SpecificPosition.CB, 81, 85, 48, 88, 84, new[] { SpecificPosition.LB, SpecificPosition.CDM }),
            (12004, "كريستيان روميرو", 13, SpecificPosition.CB, 83, 76, 52, 89, 88, null),
            (12005, "ناهويل مولينا", 26, SpecificPosition.RB, 86, 81, 68, 81, 79, new[] { SpecificPosition.RWB }),
            (12006, "إنزو فيرنانديز", 8, SpecificPosition.CM, 80, 88, 81, 82, 81, new[] { SpecificPosition.CDM }),
            (12007, "أليكسيس ماك أليستير", 20, SpecificPosition.CM, 81, 88, 84, 82, 80, new[] { SpecificPosition.CAM, SpecificPosition.CDM }),
            (12008, "رودريجو دي بول", 7, SpecificPosition.CM, 82, 85, 78, 83, 85, new[] { SpecificPosition.RM }),
            (12009, "خوليان ألفاريز", 9, SpecificPosition.ST, 87, 83, 88, 60, 80, new[] { SpecificPosition.LW, SpecificPosition.CF }),
            (12010, "لاوتارو مارتينيز", 22, SpecificPosition.ST, 84, 79, 90, 52, 85, null),
            (12011, "ليونيل ميسي", 10, SpecificPosition.RW, 84, 95, 92, 45, 70, new[] { SpecificPosition.CF, SpecificPosition.CAM })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (12012, "خيرونيمو رولي", 1, SpecificPosition.GK, 60, 79, 18, 84, 80, null),
            (12013, "نيكولاس أوتاميندي", 19, SpecificPosition.CB, 68, 78, 55, 86, 86, null),
            (12014, "جيرمان بيزيلا", 6, SpecificPosition.CB, 70, 74, 45, 82, 84, null),
            (12015, "ماركوس أكونيا", 8, SpecificPosition.LB, 80, 82, 74, 80, 82, new[] { SpecificPosition.LM }),
            (12016, "جونزالو مونتييل", 4, SpecificPosition.RB, 84, 78, 64, 80, 80, null),
            (12017, "لياندرو باريديس", 5, SpecificPosition.CDM, 72, 88, 80, 82, 80, new[] { SpecificPosition.CM }),
            (12018, "جيوفاني لو سيلسو", 16, SpecificPosition.CM, 80, 86, 81, 74, 75, new[] { SpecificPosition.CAM }),
            (12019, "أليخاندرو جارناتشو", 17, SpecificPosition.LW, 91, 79, 81, 42, 74, new[] { SpecificPosition.RW }),
            (12020, "نيكولاس جونزاليس", 15, SpecificPosition.LW, 88, 80, 82, 65, 80, new[] { SpecificPosition.RW, SpecificPosition.LWB }),
            (12021, "فالنتين كاربوني", 21, SpecificPosition.CAM, 83, 83, 79, 50, 74, null),
            (12022, "والتر بينيتيز", 12, SpecificPosition.GK, 60, 80, 18, 85, 84, null),
            (12023, "تياجو ألمادا", 11, SpecificPosition.CAM, 85, 84, 81, 50, 68, new[] { SpecificPosition.LW })
        };

        return DatabaseHelpers.CreateTeam("NAT_ARG", "الأرجنتين", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 2. البرازيل (Brazil) - 23 لاعباً
    public static Team CreateBrazil()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (12031, "أليسون بيكر", 1, SpecificPosition.GK, 64, 86, 20, 90, 86, null),
            (12032, "وينديل", 6, SpecificPosition.LB, 84, 80, 66, 81, 80, new[] { SpecificPosition.LWB }),
            (12033, "جابرييل ماجالهايس", 14, SpecificPosition.CB, 81, 78, 48, 88, 87, null),
            (12034, "ماركينيوس", 4, SpecificPosition.CB, 78, 82, 56, 88, 83, new[] { SpecificPosition.CDM }),
            (12035, "دانيلو", 2, SpecificPosition.RB, 80, 82, 70, 83, 82, new[] { SpecificPosition.CB, SpecificPosition.LB }),
            (12036, "برونو جيماريش", 5, SpecificPosition.CDM, 81, 87, 80, 85, 84, new[] { SpecificPosition.CM }),
            (12037, "لوكاس باكيتا", 8, SpecificPosition.CM, 82, 87, 83, 76, 82, new[] { SpecificPosition.CAM }),
            (12038, "رودريجو جوس", 10, SpecificPosition.RW, 89, 85, 86, 52, 74, new[] { SpecificPosition.LW, SpecificPosition.ST }),
            (12039, "فينيسيوس جونيور", 7, SpecificPosition.LW, 96, 84, 89, 42, 78, new[] { SpecificPosition.ST }),
            (12040, "رافينيا", 11, SpecificPosition.CAM, 90, 87, 87, 65, 78, new[] { SpecificPosition.RW, SpecificPosition.LW }),
            (12041, "إندريك", 9, SpecificPosition.ST, 88, 77, 86, 45, 84, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (12042, "إيدرسون مورايس", 23, SpecificPosition.GK, 64, 91, 25, 88, 84, null),
            (12043, "إيدير ميليتاو", 3, SpecificPosition.CB, 85, 78, 52, 88, 85, new[] { SpecificPosition.RB }),
            (12044, "لوكاس بيرالدو", 15, SpecificPosition.CB, 76, 81, 45, 81, 78, new[] { SpecificPosition.LB }),
            (12045, "جييرمي أرانا", 16, SpecificPosition.LB, 85, 80, 68, 79, 78, null),
            (12046, "يان كوتو", 13, SpecificPosition.RB, 89, 81, 68, 77, 72, new[] { SpecificPosition.RWB }),
            (12047, "جواو جوميز", 18, SpecificPosition.CDM, 82, 80, 68, 84, 85, new[] { SpecificPosition.CM }),
            (12048, "دوجلاس لويز", 17, SpecificPosition.CM, 78, 85, 82, 81, 80, new[] { SpecificPosition.CDM }),
            (12049, "أندرياس بيريرا", 19, SpecificPosition.CAM, 81, 85, 81, 65, 76, null),
            (12050, "سافينيو", 20, SpecificPosition.RW, 91, 82, 80, 42, 72, new[] { SpecificPosition.LW }),
            (12051, "إيفانيلسون", 21, SpecificPosition.ST, 84, 76, 83, 45, 82, null),
            (12052, "بينتو", 12, SpecificPosition.GK, 60, 78, 15, 83, 83, null),
            (12053, "جابرييل مارتينيلي", 22, SpecificPosition.LW, 91, 80, 83, 52, 78, new[] { SpecificPosition.ST })
        };

        return DatabaseHelpers.CreateTeam("NAT_BRA", "البرازيل", FormationType.Formation4231_Wide, true, starters, bench);
    }

    // 3. أوروغواي (Uruguay) - 22 لاعباً
    public static Team CreateUruguay()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (12061, "سيرجيو روشيت", 1, SpecificPosition.GK, 60, 79, 18, 84, 84, null),
            (12062, "ماتياس أوليفيرا", 16, SpecificPosition.LB, 84, 78, 62, 83, 84, new[] { SpecificPosition.CB, SpecificPosition.LWB }),
            (12063, "خوسيه ماريا خيمينيز", 2, SpecificPosition.CB, 77, 74, 45, 86, 88, null),
            (12064, "رونالد أراوخو", 4, SpecificPosition.CB, 88, 75, 52, 89, 90, new[] { SpecificPosition.RB }),
            (12065, "ناهيتان نانديز", 8, SpecificPosition.RB, 85, 81, 74, 82, 85, new[] { SpecificPosition.RWB, SpecificPosition.CM }),
            (12066, "مانويل أوجارتي", 5, SpecificPosition.CDM, 81, 82, 65, 87, 88, new[] { SpecificPosition.CM }),
            (12067, "فيديريكو فالفيردي", 15, SpecificPosition.CM, 89, 89, 87, 84, 86, new[] { SpecificPosition.RW, SpecificPosition.CDM }),
            (12068, "نيكولاس دي لا كروز", 7, SpecificPosition.CM, 83, 86, 82, 76, 78, new[] { SpecificPosition.CAM }),
            (12069, "ماكسيميليانو أروخو", 20, SpecificPosition.LW, 86, 80, 78, 74, 79, new[] { SpecificPosition.LM, SpecificPosition.LB }),
            (12070, "داروين نونيز", 19, SpecificPosition.ST, 91, 78, 86, 50, 89, null),
            (12071, "فاكوندو بيليستري", 11, SpecificPosition.RW, 89, 79, 77, 52, 70, new[] { SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (12072, "فرانكو إسرائيل", 23, SpecificPosition.GK, 60, 78, 18, 83, 82, null),
            (12073, "سيباستيان كاسيريس", 3, SpecificPosition.CB, 79, 74, 40, 82, 83, null),
            (12074, "نيكولاس ماريتشال", 17, SpecificPosition.CB, 78, 72, 38, 80, 82, null),
            (12075, "ماتياس فينيا", 22, SpecificPosition.LB, 83, 78, 65, 79, 78, new[] { SpecificPosition.CB }),
            (12076, "جييرمو فاريلا", 14, SpecificPosition.RB, 84, 76, 58, 78, 78, null),
            (12077, "رودريجو بينتانكور", 6, SpecificPosition.CM, 80, 86, 78, 82, 82, new[] { SpecificPosition.CDM }),
            (12078, "إميليانو مارتينيز توريغروزا", 13, SpecificPosition.CDM, 78, 79, 68, 79, 82, null),
            (12079, "جورجيان دي أراسكايتا", 10, SpecificPosition.CAM, 80, 87, 83, 58, 74, new[] { SpecificPosition.LW }),
            (12080, "كريستيان أوليفيرا", 25, SpecificPosition.RW, 88, 77, 76, 45, 75, null),
            (12081, "لويس سواريز", 9, SpecificPosition.ST, 72, 85, 89, 52, 82, null),
            (12082, "سانتياغو ميلي", 12, SpecificPosition.GK, 58, 74, 15, 80, 80, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_URU", "أوروغواي", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 4. كولومبيا (Colombia) - 22 لاعباً
    public static Team CreateColombia()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (12091, "كاميلو فارجاس", 12, SpecificPosition.GK, 60, 80, 18, 84, 83, null),
            (12092, "يوهان موخيكا", 17, SpecificPosition.LB, 87, 81, 70, 79, 79, new[] { SpecificPosition.LWB }),
            (12093, "دافينسون سانشيز", 23, SpecificPosition.CB, 83, 75, 45, 86, 88, null),
            (12094, "جون لوكومي", 3, SpecificPosition.CB, 80, 78, 42, 85, 86, null),
            (12095, "دانييل مونيوز", 21, SpecificPosition.RB, 88, 81, 74, 82, 84, new[] { SpecificPosition.RWB, SpecificPosition.RM }),
            (12096, "جيفرسون ليرما", 16, SpecificPosition.CDM, 78, 82, 74, 85, 87, new[] { SpecificPosition.CM, SpecificPosition.CB }),
            (12097, "ريتشارد ريوس", 6, SpecificPosition.CM, 83, 84, 79, 81, 84, new[] { SpecificPosition.CDM }),
            (12098, "خاميس رودريجيز", 10, SpecificPosition.CAM, 74, 91, 86, 52, 74, new[] { SpecificPosition.CM }),
            (12099, "لويس دياز", 7, SpecificPosition.LW, 92, 82, 84, 52, 78, null),
            (12100, "جون كوردوبا", 24, SpecificPosition.ST, 84, 74, 85, 48, 89, null),
            (12101, "جون أرياس", 11, SpecificPosition.RW, 86, 84, 80, 68, 77, new[] { SpecificPosition.CAM, SpecificPosition.RM })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (12102, "دافيد أوسبينا", 1, SpecificPosition.GK, 60, 78, 18, 82, 80, null),
            (12103, "ياري مينا", 13, SpecificPosition.CB, 68, 73, 52, 83, 89, null),
            (12104, "كارلوس كويستا", 2, SpecificPosition.CB, 81, 74, 40, 82, 83, null),
            (12105, "ديفر ماتشادو", 26, SpecificPosition.LB, 87, 78, 70, 78, 80, null),
            (12106, "سانتياجو أرياس", 4, SpecificPosition.RB, 83, 78, 64, 79, 78, null),
            (12107, "كيفين كاستانيو", 5, SpecificPosition.CDM, 80, 82, 72, 81, 82, null),
            (12108, "ماتيوس أوريبي", 15, SpecificPosition.CM, 78, 82, 78, 80, 81, null),
            (12109, "خوان فرناندو كينتيرو", 20, SpecificPosition.CAM, 75, 88, 83, 48, 68, null),
            (12110, "لويس سينيستيرا", 18, SpecificPosition.LW, 88, 80, 81, 48, 75, new[] { SpecificPosition.RW }),
            (12111, "جون دوران", 14, SpecificPosition.ST, 87, 75, 85, 48, 88, null),
            (12112, "ألفارو مونتيرو", 22, SpecificPosition.GK, 58, 74, 15, 80, 82, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_COL", "كولومبيا", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 5. الإكوادور (Ecuador) - 22 لاعباً
    public static Team CreateEcuador()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (12121, "هيرنان جاليندز", 22, SpecificPosition.GK, 60, 78, 18, 83, 82, null),
            (12122, "بييرفي إستوبينان", 7, SpecificPosition.LB, 88, 82, 68, 82, 82, new[] { SpecificPosition.LWB }),
            (12123, "ويليام باتشو", 6, SpecificPosition.CB, 83, 76, 40, 85, 87, null),
            (12124, "بييرو هينكابي", 3, SpecificPosition.CB, 84, 80, 48, 86, 84, new[] { SpecificPosition.LB }),
            (12125, "فيليكس توريس", 2, SpecificPosition.CB, 81, 74, 45, 84, 85, new[] { SpecificPosition.RB }),
            (12126, "أنجيلو بريسيادو", 17, SpecificPosition.RWB, 89, 78, 66, 80, 81, new[] { SpecificPosition.RB, SpecificPosition.RM }),
            (12127, "مويسيس كايسيدو", 23, SpecificPosition.CDM, 84, 86, 75, 86, 85, new[] { SpecificPosition.CM }),
            (12128, "آلان فرانكو", 21, SpecificPosition.CM, 82, 82, 74, 81, 82, new[] { SpecificPosition.RM }),
            (12129, "كيندري بايز", 10, SpecificPosition.CAM, 85, 86, 82, 55, 72, new[] { SpecificPosition.RW }),
            (12130, "إينر فالنسيا", 13, SpecificPosition.ST, 85, 76, 84, 52, 82, new[] { SpecificPosition.LW }),
            (12131, "جيريمي سارمينتو", 16, SpecificPosition.LW, 88, 81, 78, 48, 72, new[] { SpecificPosition.RW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (12132, "ألكسندر دومينغيز", 1, SpecificPosition.GK, 58, 76, 15, 81, 84, null),
            (12133, "جويل أوردونيز", 4, SpecificPosition.CB, 82, 73, 38, 81, 83, null),
            (12134, "أندريس ميكولتا", 24, SpecificPosition.CB, 78, 72, 40, 80, 84, null),
            (12135, "لويان لوور", 15, SpecificPosition.LB, 84, 75, 58, 76, 76, null),
            (12136, "كارلوس جرويزو", 8, SpecificPosition.CDM, 78, 80, 65, 82, 82, null),
            (12137, "خوسيه سيفوينتيس", 5, SpecificPosition.CM, 80, 81, 76, 79, 81, null),
            (12138, "جون يبواه", 9, SpecificPosition.RW, 88, 80, 78, 45, 72, new[] { SpecificPosition.LW }),
            (12139, "أنخيل مينا", 15, SpecificPosition.RW, 80, 83, 81, 52, 72, null),
            (12140, "كيفين رودريجيز", 11, SpecificPosition.ST, 87, 72, 80, 42, 84, null),
            (12141, "جوردي كايسيدو", 19, SpecificPosition.ST, 83, 70, 80, 40, 88, null),
            (12142, "مويسيس راميريز", 12, SpecificPosition.GK, 58, 72, 15, 78, 78, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_ECU", "الإكوادور", FormationType.Formation343, true, starters, bench);
    }

    // 6. تشيلي (Chile) - 22 لاعباً
    public static Team CreateChile()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (12151, "برايان كورتيس", 1, SpecificPosition.GK, 60, 78, 18, 83, 82, null),
            (12152, "جابرييل سوازو", 2, SpecificPosition.LB, 83, 80, 68, 79, 79, new[] { SpecificPosition.LWB }),
            (12153, "باولو دياز", 5, SpecificPosition.CB, 78, 76, 50, 83, 84, new[] { SpecificPosition.RB }),
            (12154, "جييرمو ماريبان", 3, SpecificPosition.CB, 70, 75, 52, 83, 86, null),
            (12155, "ماوريسيو إيسلا", 4, SpecificPosition.RB, 80, 80, 66, 79, 78, new[] { SpecificPosition.RWB }),
            (12156, "إيريك بولجار", 13, SpecificPosition.CDM, 75, 83, 76, 83, 84, new[] { SpecificPosition.CM }),
            (12157, "مارسيلينو نونيز", 7, SpecificPosition.CM, 80, 84, 80, 78, 78, new[] { SpecificPosition.CAM }),
            (12158, "رودريجو إيتشيفيريا", 18, SpecificPosition.CM, 78, 80, 72, 81, 83, new[] { SpecificPosition.CDM }),
            (12159, "فيكتور دافيلا", 9, SpecificPosition.RW, 84, 81, 82, 58, 78, new[] { SpecificPosition.ST }),
            (12160, "إدواردو فارجاس", 11, SpecificPosition.ST, 82, 78, 84, 48, 78, null),
            (12161, "أليكسيس سانشيز", 10, SpecificPosition.LW, 82, 86, 85, 55, 76, new[] { SpecificPosition.ST, SpecificPosition.CAM })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (12162, "كلاوديو برافو", 12, SpecificPosition.GK, 58, 82, 18, 82, 78, null),
            (12163, "ماتياس كاتالان", 21, SpecificPosition.CB, 80, 74, 45, 80, 83, new[] { SpecificPosition.RB }),
            (12164, "إيجور ليتشنوفسكي", 16, SpecificPosition.CB, 76, 73, 42, 81, 84, null),
            (12165, "توماس جالداميس", 6, SpecificPosition.LB, 82, 75, 60, 78, 80, null),
            (12166, "فيليبي لويولا", 8, SpecificPosition.RB, 83, 77, 62, 78, 79, new[] { SpecificPosition.CM }),
            (12167, "دييجو فالديس", 15, SpecificPosition.CAM, 81, 84, 82, 62, 76, null),
            (12168, "داريو أوسوريو", 22, SpecificPosition.RW, 87, 81, 80, 52, 74, new[] { SpecificPosition.LW }),
            (12169, "كارلوس بالاسيوس", 20, SpecificPosition.CAM, 82, 83, 80, 55, 75, null),
            (12170, "بين بريريتون دياز", 14, SpecificPosition.ST, 84, 76, 82, 50, 84, new[] { SpecificPosition.LW }),
            (12171, "غونزالو تابيا", 17, SpecificPosition.ST, 85, 72, 78, 42, 78, null),
            (12172, "جابرييل أرياس", 23, SpecificPosition.GK, 60, 76, 15, 81, 80, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_CHI", "تشيلي", FormationType.Formation4231_Wide, true, starters, bench);
    }

    // 7. باراغواي (Paraguay) - 22 لاعباً
    public static Team CreateParaguay()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (12181, "روبرتو فيرنانديز", 12, SpecificPosition.GK, 60, 78, 18, 83, 82, null),
            (12182, "أغوستين سانديز", 13, SpecificPosition.LB, 82, 76, 60, 80, 81, new[] { SpecificPosition.LWB }),
            (12183, "عمر ألديريتي", 3, SpecificPosition.CB, 76, 78, 55, 84, 87, null),
            (12184, "جوستافو جوميز", 15, SpecificPosition.CB, 76, 76, 50, 86, 88, null),
            (12185, "خوان كاسيريس", 2, SpecificPosition.RB, 84, 76, 58, 80, 81, new[] { SpecificPosition.CB }),
            (12186, "أندريس كوباس", 14, SpecificPosition.CDM, 81, 81, 65, 85, 82, null),
            (12187, "ماتياس فيلاسانتي", 23, SpecificPosition.CM, 82, 82, 75, 83, 83, new[] { SpecificPosition.CDM }),
            (12188, "دييجو جوميز", 8, SpecificPosition.CM, 82, 82, 78, 80, 83, new[] { SpecificPosition.CAM }),
            (12189, "خوليو إنسيسو", 19, SpecificPosition.CAM, 86, 84, 83, 50, 74, new[] { SpecificPosition.LW, SpecificPosition.ST }),
            (12190, "أنطونيو سانابريا", 9, SpecificPosition.ST, 82, 76, 84, 48, 81, null),
            (12191, "ميغيل ألميرون", 10, SpecificPosition.RW, 90, 82, 81, 65, 76, new[] { SpecificPosition.LW, SpecificPosition.CAM })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (12192, "كارلوس كورونيل", 1, SpecificPosition.GK, 60, 76, 15, 81, 81, null),
            (12193, "فابيان بالبوينا", 5, SpecificPosition.CB, 72, 74, 45, 82, 86, null),
            (12194, "جونيور ألونسو", 6, SpecificPosition.CB, 78, 76, 50, 82, 84, new[] { SpecificPosition.LB }),
            (12195, "ماتياس إسبينوزا", 4, SpecificPosition.LB, 83, 76, 62, 77, 76, null),
            (12196, "إيفان راميريز", 21, SpecificPosition.RB, 84, 76, 60, 77, 77, null),
            (12197, "داميان بوبادييا", 16, SpecificPosition.CM, 80, 81, 75, 78, 80, null),
            (12198, "أليخاندرو روميرو جامارا", 17, SpecificPosition.CAM, 81, 85, 81, 55, 72, null),
            (12199, "رامون سوسا", 24, SpecificPosition.LW, 91, 80, 80, 52, 78, new[] { SpecificPosition.RW }),
            (12200, "أليكس أرسي", 18, SpecificPosition.ST, 82, 72, 82, 45, 84, null),
            (12201, "أنخيل روميرو", 11, SpecificPosition.LW, 83, 81, 81, 52, 75, null),
            (12202, "خوان إسبينولا", 22, SpecificPosition.GK, 58, 72, 15, 78, 78, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_PAR", "باراغواي", FormationType.Formation4231_Wide, true, starters, bench);
    }

    // 8. بيرو (Peru) - 22 لاعباً
    public static Team CreatePeru()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (12211, "بيدرو جاليسي", 1, SpecificPosition.GK, 60, 79, 18, 84, 83, null),
            (12212, "ألكسندر كالينس", 22, SpecificPosition.CB, 78, 75, 48, 83, 85, new[] { SpecificPosition.LB }),
            (12213, "كارلوس زامبرانو", 5, SpecificPosition.CB, 72, 74, 45, 84, 86, null),
            (12214, "ميغيل أروخو", 15, SpecificPosition.CB, 78, 74, 42, 82, 84, new[] { SpecificPosition.RB }),
            (12215, "ماركوس لوبيز", 4, SpecificPosition.LWB, 86, 78, 65, 78, 79, new[] { SpecificPosition.LB }),
            (12216, "لويس أدفينكولا", 17, SpecificPosition.RWB, 89, 79, 74, 80, 83, new[] { SpecificPosition.RB, SpecificPosition.RM }),
            (12217, "ريناتو تابيا", 13, SpecificPosition.CDM, 76, 82, 72, 84, 86, new[] { SpecificPosition.CB, SpecificPosition.CM }),
            (12218, "سيرجيو بينيا", 8, SpecificPosition.CM, 78, 84, 79, 76, 76, new[] { SpecificPosition.CAM }),
            (12219, "بييرو كيسبي", 10, SpecificPosition.CAM, 83, 84, 78, 62, 70, null),
            (12220, "جيانلوكا لابادولا", 9, SpecificPosition.ST, 81, 76, 84, 55, 83, null),
            (12221, "إديسون فلوريس", 20, SpecificPosition.ST, 82, 81, 82, 60, 76, new[] { SpecificPosition.LW, SpecificPosition.CAM })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (12222, "كارلوس كاسيدا", 12, SpecificPosition.GK, 58, 72, 15, 78, 78, null),
            (12223, "أندرسون سانتاماريا", 4, SpecificPosition.CB, 75, 73, 40, 80, 82, null),
            (12224, "ألدو كورزو", 3, SpecificPosition.CB, 78, 72, 40, 80, 81, new[] { SpecificPosition.RB }),
            (12225, "أوليفر سونيه", 19, SpecificPosition.RB, 85, 77, 62, 78, 78, new[] { SpecificPosition.LB }),
            (12226, "ويلدر كارتاخينا", 16, SpecificPosition.CDM, 78, 79, 68, 81, 83, null),
            (12227, "خيسوس كاستيلو", 23, SpecificPosition.CM, 80, 79, 72, 78, 80, null),
            (12228, "أندي بولو", 7, SpecificPosition.RWB, 88, 77, 72, 77, 78, null),
            (12229, "جواو جريمالدو", 14, SpecificPosition.RW, 89, 78, 76, 45, 70, new[] { SpecificPosition.LW }),
            (12230, "بريان رينا", 11, SpecificPosition.LW, 88, 77, 78, 48, 72, null),
            (12231, "باولو جيريرو", 9, SpecificPosition.ST, 65, 78, 85, 48, 84, null),
            (12232, "دييجو روميرو", 21, SpecificPosition.GK, 58, 70, 15, 77, 78, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_PER", "بيرو", FormationType.Formation352, true, starters, bench);
    }

    // 9. فنزويلا (Venezuela) - 22 لاعباً
    public static Team CreateVenezuela()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (12241, "رافائيل رومو", 22, SpecificPosition.GK, 60, 78, 18, 83, 84, null),
            (12242, "ميغيل نافارو", 15, SpecificPosition.LB, 85, 77, 60, 79, 78, null),
            (12243, "ناهويل فيراريسي", 2, SpecificPosition.CB, 78, 74, 42, 83, 84, null),
            (12244, "يوردان أوسوريو", 3, SpecificPosition.CB, 76, 74, 45, 83, 86, null),
            (12245, "جون أرامبورو", 4, SpecificPosition.RB, 85, 78, 62, 81, 82, new[] { SpecificPosition.CB, SpecificPosition.LB }),
            (12246, "يانخيل هيريرا", 6, SpecificPosition.CM, 80, 84, 82, 83, 85, new[] { SpecificPosition.CDM }),
            (12247, "خوسيه مارتينيز", 13, SpecificPosition.CDM, 80, 80, 68, 83, 84, null),
            (12248, "كريستيان كاسيريس جونيور", 8, SpecificPosition.CM, 80, 82, 74, 81, 81, null),
            (12249, "يفرسون سوتيلدو", 10, SpecificPosition.LW, 90, 84, 80, 45, 66, new[] { SpecificPosition.RW, SpecificPosition.CAM }),
            (12250, "سالومون روندون", 23, SpecificPosition.ST, 75, 78, 86, 55, 90, null),
            (12251, "إدوارد بيلو", 25, SpecificPosition.RW, 85, 80, 80, 55, 76, new[] { SpecificPosition.CAM })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (12252, "جويل جراتيرول", 1, SpecificPosition.GK, 58, 74, 15, 80, 80, null),
            (12253, "ويلكر أنخيل", 20, SpecificPosition.CB, 74, 72, 42, 81, 84, null),
            (12254, "كريستيان ماكون", 5, SpecificPosition.CB, 78, 73, 45, 79, 82, new[] { SpecificPosition.LB }),
            (12255, "ألكسندر جونزاليز", 21, SpecificPosition.RB, 82, 76, 60, 78, 78, null),
            (12256, "توماس رينكون", 8, SpecificPosition.CDM, 70, 78, 68, 81, 83, null),
            (12257, "دانييل بيريرا", 14, SpecificPosition.CM, 80, 80, 72, 78, 79, null),
            (12258, "كيرفين أندرادي", 7, SpecificPosition.CAM, 83, 81, 78, 50, 72, null),
            (12259, "جيفرسون سافارينو", 11, SpecificPosition.RW, 86, 82, 80, 52, 72, new[] { SpecificPosition.CAM }),
            (12260, "داروين ماتشيس", 9, SpecificPosition.LW, 88, 79, 81, 48, 76, null),
            (12261, "إريك راميريز", 19, SpecificPosition.ST, 83, 72, 80, 45, 84, null),
            (12262, "خوسيه كونتريراس", 12, SpecificPosition.GK, 56, 70, 15, 77, 78, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_VEN", "فنزويلا", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 10. بوليفيا (Bolivia) - 22 لاعباً
    public static Team CreateBolivia()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (12271, "غييرمو فيسكارا", 23, SpecificPosition.GK, 60, 78, 18, 83, 81, null),
            (12272, "روبرتو فيرنانديز", 17, SpecificPosition.LWB, 86, 78, 68, 77, 78, new[] { SpecificPosition.LB, SpecificPosition.LM }),
            (12273, "خوسيه ساجريدو", 21, SpecificPosition.CB, 76, 74, 45, 82, 83, new[] { SpecificPosition.LB }),
            (12274, "لويس هاكين", 4, SpecificPosition.CB, 76, 73, 42, 82, 85, null),
            (12275, "دييجو ميدينا", 3, SpecificPosition.RWB, 87, 77, 62, 78, 78, new[] { SpecificPosition.RB, SpecificPosition.RM }),
            (12276, "روبسون ماتيوس", 6, SpecificPosition.CM, 80, 81, 74, 79, 80, new[] { SpecificPosition.CDM }),
            (12277, "جابرييل فيلاميل", 16, SpecificPosition.CM, 81, 82, 77, 79, 81, null),
            (12278, "راميرو فاكا", 10, SpecificPosition.CAM, 82, 85, 82, 58, 74, new[] { SpecificPosition.CM }),
            (12279, "ميغيل تيرسيروس ميغيليتو", 7, SpecificPosition.RW, 88, 82, 83, 50, 70, new[] { SpecificPosition.CAM }),
            (12280, "كارميلو ألغارانياز", 11, SpecificPosition.ST, 84, 75, 82, 52, 83, null),
            (12281, "لوكاس شافيز", 18, SpecificPosition.LW, 86, 78, 77, 48, 72, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (12282, "كارلوس لامبي", 1, SpecificPosition.GK, 56, 76, 18, 81, 85, null),
            (12283, "مارسيلو سواريس", 2, SpecificPosition.CB, 78, 72, 40, 79, 82, null),
            (12284, "أدريان خوسينو", 5, SpecificPosition.CB, 74, 71, 40, 79, 83, null),
            (12285, "يومار روشا", 14, SpecificPosition.RB, 84, 74, 55, 76, 76, null),
            (12286, "بوريس سيسبييديس", 15, SpecificPosition.CDM, 76, 80, 70, 79, 80, null),
            (12287, "هيكتور كوييار", 22, SpecificPosition.CDM, 78, 78, 68, 80, 82, new[] { SpecificPosition.CB }),
            (12288, "أداليد تيرازاس", 20, SpecificPosition.CAM, 80, 81, 76, 52, 72, null),
            (12289, "خيسوس ساجريدو", 8, SpecificPosition.CB, 76, 72, 42, 78, 80, null),
            (12290, "برونو ميراندا", 9, SpecificPosition.ST, 82, 72, 79, 45, 81, null),
            (12291, "خوسيه مارتينيز", 19, SpecificPosition.ST, 83, 70, 77, 40, 80, null),
            (12292, "برونو بوفيدا", 12, SpecificPosition.GK, 56, 68, 15, 75, 75, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_BOL", "بوليفيا", FormationType.Formation343, true, starters, bench);
    }
}
