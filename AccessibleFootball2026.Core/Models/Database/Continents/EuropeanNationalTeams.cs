namespace AccessibleFootball2026.Core.Models;

/// <summary>
/// منتخبات القارة الأوروبية (UEFA) المعتمدة رسمياً من فيفا لموسم 2026/2027 (24 منتخباً)
/// موثقة بالكامل من واقع Transfermarkt و SofaScore و 365scores
/// بقوائم موسعة (22 إلى 23 لاعباً لكل منتخب)
/// استبعاد قطعي وصارم لأي وجود لمنتخب الكيان الصهيوني
/// </summary>
public static class EuropeanNationalTeams
{
    public static List<Team> GetAllNationalTeams()
    {
        return new List<Team>
        {
            CreateFrance(),
            CreateEngland(),
            CreateSpain(),
            CreateGermany(),
            CreateItaly(),
            CreatePortugal(),
            CreateNetherlands(),
            CreateCroatia(),
            CreateBelgium(),
            CreateTurkey(),
            CreateSwitzerland(),
            CreateDenmark(),
            CreateAustria(),
            CreateNorway(),
            CreatePoland(),
            CreateUkraine(),
            CreateScotland(),
            CreateSweden(),
            CreateSerbia(),
            CreateCzechia(),
            CreateHungary(),
            CreateRomania(),
            CreateGreece(),
            CreateSlovakia()
        };
    }

    // 1. فرنسا (France) - 23 لاعباً
    public static Team CreateFrance()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11001, "مايك ماينان", 16, SpecificPosition.GK, 62, 84, 18, 89, 85, null),
            (11002, "ثيو هيرنانديز", 22, SpecificPosition.LB, 93, 82, 75, 83, 85, new[] { SpecificPosition.LWB, SpecificPosition.LM }),
            (11003, "ويليام ساليبا", 4, SpecificPosition.CB, 84, 79, 45, 89, 87, null),
            (11004, "دايوت أوباميكانو", 5, SpecificPosition.CB, 84, 75, 42, 86, 88, null),
            (11005, "جول كوندي", 5, SpecificPosition.RB, 85, 81, 55, 88, 83, new[] { SpecificPosition.CB }),
            (11006, "أوريليان تشواميني", 8, SpecificPosition.CDM, 80, 85, 76, 88, 88, new[] { SpecificPosition.CM, SpecificPosition.CB }),
            (11007, "إدواردو كامافينجا", 6, SpecificPosition.CM, 84, 86, 75, 85, 84, new[] { SpecificPosition.CDM, SpecificPosition.LB }),
            (11008, "أنطوان جريزمان", 7, SpecificPosition.CAM, 78, 90, 88, 72, 75, new[] { SpecificPosition.CF, SpecificPosition.CM }),
            (11009, "كيليان مبابي", 10, SpecificPosition.LW, 97, 85, 93, 40, 80, new[] { SpecificPosition.ST }),
            (11010, "راندال كولو مواني", 12, SpecificPosition.ST, 88, 77, 82, 45, 81, new[] { SpecificPosition.RW }),
            (11011, "عثمان ديمبيلي", 11, SpecificPosition.RW, 93, 86, 81, 40, 68, new[] { SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11012, "برايس سامبا", 1, SpecificPosition.GK, 62, 82, 18, 86, 84, null),
            (11013, "إبراهيما كوناتي", 24, SpecificPosition.CB, 82, 75, 40, 87, 89, null),
            (11014, "بنجامين بافارد", 2, SpecificPosition.CB, 80, 81, 68, 84, 82, new[] { SpecificPosition.RB }),
            (11015, "فيرلاند ميندي", 3, SpecificPosition.LB, 90, 78, 65, 84, 85, null),
            (11016, "وارن زاير إيمري", 18, SpecificPosition.CM, 82, 85, 77, 80, 82, new[] { SpecificPosition.CDM, SpecificPosition.RB }),
            (11017, "أدريان رابيو", 14, SpecificPosition.CM, 78, 84, 79, 82, 85, new[] { SpecificPosition.CDM }),
            (11018, "يوسف فوفانا", 19, SpecificPosition.CDM, 80, 82, 75, 83, 85, null),
            (11019, "مايكل أوليسيه", 20, SpecificPosition.RW, 88, 87, 83, 55, 75, new[] { SpecificPosition.CAM }),
            (11020, "برادلي باركولا", 25, SpecificPosition.LW, 92, 82, 83, 48, 76, null),
            (11021, "ماركوس تورام", 15, SpecificPosition.ST, 87, 78, 84, 52, 86, new[] { SpecificPosition.LW }),
            (11022, "ألفونس أريولا", 23, SpecificPosition.GK, 60, 78, 15, 82, 82, null),
            (11023, "كينجسلي كومان", 26, SpecificPosition.LW, 91, 82, 79, 45, 68, new[] { SpecificPosition.RW })
        };

        return DatabaseHelpers.CreateTeam("NAT_FRA", "فرنسا", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 2. إنجلترا (England) - 23 لاعباً
    public static Team CreateEngland()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11031, "جوردان بيكفورد", 1, SpecificPosition.GK, 62, 86, 20, 86, 83, null),
            (11032, "لوك شو", 3, SpecificPosition.LB, 82, 82, 65, 82, 84, new[] { SpecificPosition.CB, SpecificPosition.LWB }),
            (11033, "جون ستونز", 5, SpecificPosition.CB, 76, 85, 52, 87, 82, new[] { SpecificPosition.CDM }),
            (11034, "مارك جويهي", 6, SpecificPosition.CB, 81, 78, 42, 85, 84, null),
            (11035, "كايل ووكر", 2, SpecificPosition.RB, 90, 78, 62, 85, 84, new[] { SpecificPosition.CB }),
            (11036, "ديكلان رايس", 4, SpecificPosition.CDM, 80, 84, 76, 88, 87, new[] { SpecificPosition.CM }),
            (11037, "كوبي ماينو", 26, SpecificPosition.CM, 80, 84, 75, 81, 79, new[] { SpecificPosition.CDM }),
            (11038, "جود بيلينجهام", 10, SpecificPosition.CAM, 83, 89, 88, 82, 87, new[] { SpecificPosition.CM }),
            (11039, "فيل فودين", 11, SpecificPosition.LW, 86, 89, 87, 58, 72, new[] { SpecificPosition.CAM, SpecificPosition.RW }),
            (11040, "هاري كين", 9, SpecificPosition.ST, 75, 88, 93, 50, 84, new[] { SpecificPosition.CF }),
            (11041, "بوكايو ساكا", 7, SpecificPosition.RW, 88, 86, 85, 65, 78, new[] { SpecificPosition.RM })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11042, "آرون رامسديل", 13, SpecificPosition.GK, 60, 80, 18, 82, 82, null),
            (11043, "إزري كونسا", 12, SpecificPosition.CB, 82, 76, 42, 83, 83, new[] { SpecificPosition.RB }),
            (11044, "لويس دانك", 15, SpecificPosition.CB, 68, 79, 52, 82, 86, null),
            (11045, "ترينت ألكسندر أرنولد", 8, SpecificPosition.RB, 81, 92, 81, 80, 76, new[] { SpecificPosition.RWB, SpecificPosition.CM }),
            (11046, "كيران تريبير", 12, SpecificPosition.LB, 78, 86, 74, 80, 76, new[] { SpecificPosition.RB }),
            (11047, "كونور جالاجير", 16, SpecificPosition.CM, 82, 81, 79, 82, 84, null),
            (11048, "آدم وارتون", 25, SpecificPosition.CDM, 78, 83, 72, 80, 80, null),
            (11049, "كول بالمر", 24, SpecificPosition.CAM, 84, 88, 87, 55, 76, new[] { SpecificPosition.RW }),
            (11050, "إيبيريتشي إيزي", 21, SpecificPosition.LW, 86, 84, 83, 58, 77, new[] { SpecificPosition.CAM }),
            (11051, "أولي واتكينز", 19, SpecificPosition.ST, 88, 79, 86, 48, 83, null),
            (11052, "دين هندرسون", 23, SpecificPosition.GK, 60, 77, 15, 81, 81, null),
            (11053, "إيفان توني", 17, SpecificPosition.ST, 84, 76, 84, 52, 86, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_ENG", "إنجلترا", FormationType.Formation4231_Wide, true, starters, bench);
    }

    // 3. إسبانيا (Spain) - 23 لاعباً
    public static Team CreateSpain()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11061, "أوناي سيمون", 23, SpecificPosition.GK, 60, 82, 18, 87, 84, null),
            (11062, "مارك كوكوريلا", 24, SpecificPosition.LB, 84, 81, 68, 83, 81, new[] { SpecificPosition.LWB }),
            (11063, "إيمريك لابورت", 14, SpecificPosition.CB, 76, 82, 54, 86, 85, null),
            (11064, "روبين لو نورماند", 3, SpecificPosition.CB, 78, 76, 45, 85, 85, null),
            (11065, "داني كارفاخال", 2, SpecificPosition.RB, 83, 82, 65, 86, 84, new[] { SpecificPosition.RWB }),
            (11066, "رودري", 16, SpecificPosition.CDM, 74, 91, 84, 89, 88, new[] { SpecificPosition.CM }),
            (11067, "فابيان رويز", 8, SpecificPosition.CM, 76, 87, 82, 80, 80, null),
            (11068, "داني أولمو", 10, SpecificPosition.CAM, 83, 87, 85, 62, 74, new[] { SpecificPosition.LW, SpecificPosition.RW }),
            (11069, "نيكو ويليامز", 17, SpecificPosition.LW, 94, 82, 82, 48, 76, new[] { SpecificPosition.RW }),
            (11070, "ألفارو موراتا", 7, SpecificPosition.ST, 82, 76, 85, 48, 81, null),
            (11071, "لامين يامال", 19, SpecificPosition.RW, 92, 88, 84, 45, 70, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11072, "دافيد رايا", 1, SpecificPosition.GK, 62, 84, 18, 86, 82, null),
            (11073, "باو كوبارسي", 5, SpecificPosition.CB, 78, 85, 45, 84, 79, null),
            (11074, "داني فيفيان", 4, SpecificPosition.CB, 81, 74, 40, 83, 84, null),
            (11075, "أليخاندرو جريمالدو", 12, SpecificPosition.LB, 85, 89, 81, 80, 74, new[] { SpecificPosition.LWB, SpecificPosition.LM }),
            (11076, "بيدرو بورو", 22, SpecificPosition.RB, 86, 82, 75, 80, 79, new[] { SpecificPosition.RWB }),
            (11077, "مارتن زوبيميندي", 18, SpecificPosition.CDM, 79, 85, 70, 85, 82, null),
            (11078, "ميكيل ميرينو", 6, SpecificPosition.CM, 78, 83, 80, 83, 85, null),
            (11079, "بيدري", 20, SpecificPosition.CM, 82, 90, 80, 75, 72, new[] { SpecificPosition.CAM }),
            (11080, "ميكيل أويارزابال", 21, SpecificPosition.LW, 82, 83, 84, 58, 78, new[] { SpecificPosition.ST }),
            (11081, "فيرران توريس", 11, SpecificPosition.RW, 85, 80, 82, 48, 76, new[] { SpecificPosition.ST, SpecificPosition.LW }),
            (11082, "أليكس ريميرو", 13, SpecificPosition.GK, 60, 78, 15, 82, 81, null),
            (11083, "خوسيلو", 9, SpecificPosition.ST, 72, 74, 84, 48, 86, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_ESP", "إسبانيا", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 4. ألمانيا (Germany) - 23 لاعباً
    public static Team CreateGermany()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11091, "مارك أندريه تير شتيجن", 1, SpecificPosition.GK, 62, 86, 20, 88, 83, null),
            (11092, "ماكسيميليان ميتلشتادت", 18, SpecificPosition.LB, 84, 82, 72, 80, 78, new[] { SpecificPosition.LWB }),
            (11093, "أنطونيو روديجر", 2, SpecificPosition.CB, 86, 76, 55, 88, 89, null),
            (11094, "جوناثان تاه", 4, SpecificPosition.CB, 81, 77, 45, 87, 89, null),
            (11095, "جوشوا كيميتش", 6, SpecificPosition.RB, 75, 91, 76, 84, 80, new[] { SpecificPosition.CDM, SpecificPosition.CM }),
            (11096, "روبرت أندريش", 23, SpecificPosition.CDM, 75, 81, 78, 85, 88, new[] { SpecificPosition.CM }),
            (11097, "ألكسندر بافلوفيتش", 25, SpecificPosition.CM, 78, 86, 74, 81, 79, new[] { SpecificPosition.CDM }),
            (11098, "إيلكاي جوندوجان", 21, SpecificPosition.CAM, 74, 89, 83, 75, 74, new[] { SpecificPosition.CM }),
            (11099, "فلوريان فيرتز", 17, SpecificPosition.LW, 86, 89, 85, 62, 74, new[] { SpecificPosition.CAM }),
            (11100, "كاي هافيرتز", 7, SpecificPosition.ST, 84, 84, 84, 58, 81, new[] { SpecificPosition.CAM, SpecificPosition.CF }),
            (11101, "جمال موسيالا", 10, SpecificPosition.RW, 90, 87, 86, 62, 74, new[] { SpecificPosition.CAM, SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11102, "أوليفر باومان", 12, SpecificPosition.GK, 60, 78, 15, 83, 82, null),
            (11103, "نيكو شلوتربيك", 15, SpecificPosition.CB, 82, 80, 52, 85, 86, new[] { SpecificPosition.LB }),
            (11104, "فالديمار أنطون", 16, SpecificPosition.CB, 80, 77, 45, 83, 85, null),
            (11105, "ديفيد راوم", 22, SpecificPosition.LB, 87, 84, 68, 79, 81, new[] { SpecificPosition.LWB }),
            (11106, "بنيامين هنريكس", 20, SpecificPosition.RB, 84, 80, 68, 80, 80, new[] { SpecificPosition.LB }),
            (11107, "باسكال جروس", 5, SpecificPosition.CM, 74, 87, 80, 79, 79, new[] { SpecificPosition.CDM }),
            (11108, "أنجيلو ستيلر", 19, SpecificPosition.CM, 78, 85, 75, 80, 80, null),
            (11109, "سيرج جنابري", 11, SpecificPosition.RW, 86, 82, 84, 52, 76, null),
            (11110, "ليروي ساني", 19, SpecificPosition.RW, 91, 82, 83, 45, 75, new[] { SpecificPosition.LW }),
            (11111, "نيكلاس فولكروج", 9, SpecificPosition.ST, 76, 77, 86, 48, 88, null),
            (11112, "ألكسندر نوبل", 24, SpecificPosition.GK, 60, 77, 15, 82, 81, null),
            (11113, "دينيز أونداف", 26, SpecificPosition.ST, 80, 82, 85, 50, 82, new[] { SpecificPosition.CF })
        };

        return DatabaseHelpers.CreateTeam("NAT_GER", "ألمانيا", FormationType.Formation4231_Wide, true, starters, bench);
    }

    // 5. إيطاليا (Italy) - 23 لاعباً
    public static Team CreateItaly()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11121, "جانلويجي دوناروما", 1, SpecificPosition.GK, 62, 79, 18, 90, 85, null),
            (11122, "فيديريكو ديماركو", 3, SpecificPosition.LB, 85, 87, 78, 81, 76, new[] { SpecificPosition.LWB, SpecificPosition.LM }),
            (11123, "أليساندرو باستوني", 23, SpecificPosition.CB, 81, 85, 45, 88, 85, new[] { SpecificPosition.LB }),
            (11124, "ريكاردو كالافيوري", 5, SpecificPosition.CB, 82, 84, 55, 85, 84, new[] { SpecificPosition.LB }),
            (11125, "جيوفاني دي لورينزو", 2, SpecificPosition.RB, 83, 81, 66, 83, 81, new[] { SpecificPosition.RWB, SpecificPosition.CB }),
            (11126, "نيكولو باريلا", 18, SpecificPosition.CM, 84, 87, 81, 84, 82, new[] { SpecificPosition.CAM }),
            (11127, "سامويلي ريتشي", 6, SpecificPosition.CDM, 80, 84, 72, 82, 80, new[] { SpecificPosition.CM }),
            (11128, "ساندرو تونالي", 8, SpecificPosition.CM, 84, 85, 78, 84, 85, new[] { SpecificPosition.CDM }),
            (11129, "لورينزو بيليجريني", 10, SpecificPosition.CAM, 80, 85, 82, 72, 77, new[] { SpecificPosition.CM }),
            (11130, "ماتيو ريتيغي", 19, SpecificPosition.ST, 83, 74, 86, 50, 84, null),
            (11131, "ماتيا زاكايني", 20, SpecificPosition.LW, 86, 83, 82, 60, 75, new[] { SpecificPosition.LM })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11132, "جولييلمو فيكاريو", 12, SpecificPosition.GK, 60, 80, 18, 85, 83, null),
            (11133, "أليساندرو بونجورنو", 4, SpecificPosition.CB, 79, 75, 40, 85, 87, null),
            (11134, "فيديريكو جاتي", 6, SpecificPosition.CB, 78, 73, 48, 83, 86, null),
            (11135, "ديستيني أودوجي", 15, SpecificPosition.LB, 88, 79, 68, 81, 83, new[] { SpecificPosition.LWB }),
            (11136, "راؤول بيلانوفا", 16, SpecificPosition.RB, 91, 78, 65, 79, 81, new[] { SpecificPosition.RWB }),
            (11137, "مانويل لوكاتيلي", 5, SpecificPosition.CDM, 75, 85, 76, 83, 83, null),
            (11138, "دافيد فراتيسي", 7, SpecificPosition.CM, 82, 81, 82, 78, 80, null),
            (11139, "نيكولو فاجيولي", 21, SpecificPosition.CM, 78, 84, 74, 78, 74, null),
            (11140, "فيديريكو كييزا", 14, SpecificPosition.RW, 89, 80, 84, 52, 76, new[] { SpecificPosition.LW, SpecificPosition.ST }),
            (11141, "جاكومو راسبادوري", 11, SpecificPosition.ST, 83, 81, 82, 45, 72, new[] { SpecificPosition.CAM, SpecificPosition.LW }),
            (11142, "أليكس ميريت", 22, SpecificPosition.GK, 60, 77, 15, 82, 80, null),
            (11143, "جيانلوكا سكاماكا", 9, SpecificPosition.ST, 78, 77, 86, 48, 88, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_ITA", "إيطاليا", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 6. البرتغال (Portugal) - 23 لاعباً
    public static Team CreatePortugal()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11151, "ديوجو كوستا", 22, SpecificPosition.GK, 64, 84, 20, 88, 84, null),
            (11152, "نونو مينديز", 19, SpecificPosition.LB, 94, 82, 68, 81, 79, new[] { SpecificPosition.LWB }),
            (11153, "روبين دياز", 4, SpecificPosition.CB, 76, 80, 45, 90, 88, null),
            (11154, "جونكالو إيناسيو", 14, SpecificPosition.CB, 80, 83, 50, 85, 84, new[] { SpecificPosition.LB }),
            (11155, "ديوجو دالوت", 5, SpecificPosition.RB, 85, 82, 72, 82, 81, new[] { SpecificPosition.LB, SpecificPosition.RWB }),
            (11156, "جواو بالينيا", 6, SpecificPosition.CDM, 75, 79, 68, 88, 90, null),
            (11157, "فيتينيا", 23, SpecificPosition.CM, 82, 89, 78, 81, 78, new[] { SpecificPosition.CDM }),
            (11158, "برونو فيرنانديز", 8, SpecificPosition.CAM, 78, 92, 87, 74, 80, new[] { SpecificPosition.CM }),
            (11159, "رافائيل لياو", 17, SpecificPosition.LW, 94, 82, 83, 42, 84, new[] { SpecificPosition.ST }),
            (11160, "كريستيانو رونالدو", 7, SpecificPosition.ST, 80, 80, 91, 45, 82, null),
            (11161, "برناردو سيلفا", 10, SpecificPosition.RW, 81, 91, 82, 70, 74, new[] { SpecificPosition.CM, SpecificPosition.CAM })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11162, "خوسيه سا", 12, SpecificPosition.GK, 60, 78, 15, 82, 82, null),
            (11163, "أنطونيو سيلفا", 24, SpecificPosition.CB, 81, 76, 42, 85, 84, null),
            (11164, "ريناتو فيغا", 13, SpecificPosition.CB, 80, 80, 60, 81, 85, new[] { SpecificPosition.LB, SpecificPosition.CDM }),
            (11165, "جواو كانسيلو", 20, SpecificPosition.RB, 85, 88, 76, 81, 75, new[] { SpecificPosition.LB, SpecificPosition.RWB }),
            (11166, "نيلسون سيميدو", 2, SpecificPosition.RB, 88, 78, 62, 79, 78, null),
            (11167, "جواو نيفيز", 15, SpecificPosition.CM, 83, 87, 75, 83, 82, new[] { SpecificPosition.CDM }),
            (11168, "روبين نيفيز", 18, SpecificPosition.CM, 74, 88, 82, 80, 78, null),
            (11169, "بيدرو نيتو", 25, SpecificPosition.RW, 92, 82, 79, 48, 74, new[] { SpecificPosition.LW }),
            (11170, "ديوجو جوتا", 21, SpecificPosition.ST, 86, 80, 85, 58, 80, new[] { SpecificPosition.LW, SpecificPosition.CF }),
            (11171, "جونكالو راموس", 9, SpecificPosition.ST, 81, 74, 84, 46, 82, null),
            (11172, "روي سيلفا", 1, SpecificPosition.GK, 58, 76, 15, 81, 80, null),
            (11173, "فرانسيسكو كونسيساو", 26, SpecificPosition.RW, 88, 81, 78, 48, 68, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_POR", "البرتغال", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 7. هولندا (Netherlands) - 23 لاعباً
    public static Team CreateNetherlands()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11181, "بارت فيربورجين", 1, SpecificPosition.GK, 62, 82, 18, 85, 83, null),
            (11182, "ناثان أكي", 5, SpecificPosition.LB, 82, 80, 55, 87, 83, new[] { SpecificPosition.CB }),
            (11183, "فيرجيل فان دايك", 4, SpecificPosition.CB, 79, 82, 60, 91, 90, null),
            (11184, "ستيفان دي فري", 6, SpecificPosition.CB, 74, 78, 45, 86, 84, null),
            (11185, "دينزل دومفريس", 22, SpecificPosition.RB, 87, 79, 74, 82, 88, new[] { SpecificPosition.RWB, SpecificPosition.RM }),
            (11186, "جيردي شوتن", 24, SpecificPosition.CDM, 78, 86, 72, 85, 83, new[] { SpecificPosition.CM }),
            (11187, "تياني ريندرز", 14, SpecificPosition.CM, 83, 86, 81, 79, 80, new[] { SpecificPosition.CAM }),
            (11188, "تشافي سيمونز", 7, SpecificPosition.CAM, 88, 86, 83, 62, 76, new[] { SpecificPosition.RW, SpecificPosition.LW }),
            (11189, "كودي جاكبو", 11, SpecificPosition.LW, 88, 83, 85, 52, 81, new[] { SpecificPosition.ST, SpecificPosition.CF }),
            (11190, "ممفيس ديباي", 10, SpecificPosition.ST, 83, 84, 85, 45, 80, new[] { SpecificPosition.CF, SpecificPosition.LW }),
            (11191, "دونيل مالين", 18, SpecificPosition.RW, 90, 78, 82, 42, 78, new[] { SpecificPosition.ST, SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11192, "مارك فليكين", 13, SpecificPosition.GK, 60, 80, 18, 83, 82, null),
            (11193, "ماتيس دي ليخت", 3, SpecificPosition.CB, 78, 76, 55, 86, 87, null),
            (11194, "ميكي فان دي فين", 15, SpecificPosition.CB, 92, 76, 45, 85, 86, new[] { SpecificPosition.LB }),
            (11195, "إيان ماتسن", 12, SpecificPosition.LB, 88, 81, 72, 79, 74, new[] { SpecificPosition.LWB }),
            (11196, "جيريمي فريمبونج", 20, SpecificPosition.RB, 95, 82, 78, 78, 72, new[] { SpecificPosition.RWB, SpecificPosition.RM, SpecificPosition.RW }),
            (11197, "ريان جرافينبيرش", 8, SpecificPosition.CM, 84, 86, 78, 82, 84, new[] { SpecificPosition.CDM }),
            (11198, "كوينتن تيمبر", 16, SpecificPosition.CM, 83, 84, 81, 82, 85, null),
            (11199, "جوي فيرمان", 17, SpecificPosition.CM, 74, 90, 82, 78, 79, null),
            (11200, "بريان بروبي", 19, SpecificPosition.ST, 87, 74, 84, 45, 92, null),
            (11201, "فاوت فيخهورست", 9, SpecificPosition.ST, 74, 75, 83, 55, 91, null),
            (11202, "جوستن بيلو", 23, SpecificPosition.GK, 60, 78, 15, 83, 82, null),
            (11203, "جوشوا زيركزي", 21, SpecificPosition.ST, 83, 82, 82, 48, 84, new[] { SpecificPosition.CF })
        };

        return DatabaseHelpers.CreateTeam("NAT_NED", "هولندا", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 8. كرواتيا (Croatia) - 23 لاعباً
    public static Team CreateCroatia()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11211, "دومينيك ليفاكوفيتش", 1, SpecificPosition.GK, 62, 80, 18, 86, 84, null),
            (11212, "يوسكو جفارديول", 4, SpecificPosition.LB, 85, 83, 70, 88, 87, new[] { SpecificPosition.CB }),
            (11213, "مارتين إيرليتش", 3, SpecificPosition.CB, 78, 74, 42, 83, 85, null),
            (11214, "جوسيب شوتالو", 6, SpecificPosition.CB, 80, 79, 45, 84, 84, null),
            (11215, "جوسيب ستانيسيتش", 2, SpecificPosition.RB, 81, 78, 55, 82, 82, new[] { SpecificPosition.CB }),
            (11216, "ماتيو كوفاسيتش", 8, SpecificPosition.CM, 84, 89, 75, 82, 80, new[] { SpecificPosition.CDM }),
            (11217, "لوكا مودريتش", 10, SpecificPosition.CM, 75, 92, 80, 78, 70, new[] { SpecificPosition.CAM }),
            (11218, "لوكا سوتشيتش", 25, SpecificPosition.CM, 80, 85, 82, 76, 80, new[] { SpecificPosition.CAM }),
            (11219, "إيفان بيريشيتش", 14, SpecificPosition.LW, 82, 84, 83, 72, 80, new[] { SpecificPosition.LWB, SpecificPosition.LM }),
            (11220, "أندريه كراماريتش", 9, SpecificPosition.ST, 80, 83, 85, 48, 76, new[] { SpecificPosition.CAM, SpecificPosition.CF }),
            (11221, "ماريو باشاليتش", 15, SpecificPosition.RW, 78, 82, 83, 75, 82, new[] { SpecificPosition.CAM, SpecificPosition.CM })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11222, "إيفيكا إيفوشيتش", 12, SpecificPosition.GK, 60, 76, 15, 81, 80, null),
            (11223, "مارين بونغراتشيتش", 5, SpecificPosition.CB, 80, 75, 42, 82, 85, null),
            (11224, "دوماجوي فيدا", 21, SpecificPosition.CB, 72, 73, 48, 80, 84, null),
            (11225, "بورنا سوسا", 19, SpecificPosition.LB, 83, 85, 68, 78, 76, new[] { SpecificPosition.LWB }),
            (11226, "جوسيب يورانوفيتش", 22, SpecificPosition.RB, 87, 79, 66, 79, 78, new[] { SpecificPosition.RWB }),
            (11227, "كريستيان ياكيتش", 17, SpecificPosition.CDM, 79, 79, 70, 81, 83, new[] { SpecificPosition.RB }),
            (11228, "مارتن باتورينا", 26, SpecificPosition.CAM, 83, 85, 79, 60, 72, new[] { SpecificPosition.CM }),
            (11229, "لوفرو ماجر", 7, SpecificPosition.RW, 82, 86, 82, 65, 75, new[] { SpecificPosition.CAM }),
            (11230, "لوكا إيفانوشيتس", 18, SpecificPosition.LW, 83, 82, 80, 50, 74, null),
            (11231, "أنته بوديمير", 16, SpecificPosition.ST, 74, 73, 84, 48, 87, null),
            (11232, "نديلكو لابروفيتش", 23, SpecificPosition.GK, 58, 74, 15, 79, 80, null),
            (11233, "إيجور ماتانوفيتش", 20, SpecificPosition.ST, 80, 72, 82, 45, 86, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_CRO", "كرواتيا", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 9. بلجيكا (Belgium) - 22 لاعباً
    public static Team CreateBelgium()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11241, "كوين كاستيلس", 1, SpecificPosition.GK, 60, 81, 18, 85, 83, null),
            (11242, "ماكسيم دي كويبر", 25, SpecificPosition.LB, 84, 82, 72, 79, 78, new[] { SpecificPosition.LWB }),
            (11243, "فاوت فايس", 4, SpecificPosition.CB, 77, 75, 45, 83, 85, null),
            (11244, "آرتور ثياتي", 3, SpecificPosition.CB, 80, 78, 55, 83, 84, new[] { SpecificPosition.LB }),
            (11245, "تيموثي كاستانيي", 21, SpecificPosition.RB, 84, 80, 68, 81, 80, new[] { SpecificPosition.LB, SpecificPosition.RWB }),
            (11246, "أمادو أونانا", 24, SpecificPosition.CDM, 82, 82, 74, 86, 89, new[] { SpecificPosition.CM }),
            (11247, "يوري تليمانس", 8, SpecificPosition.CM, 75, 88, 84, 78, 79, null),
            (11248, "كيفين دي بروين", 7, SpecificPosition.CAM, 75, 94, 88, 68, 76, new[] { SpecificPosition.CM }),
            (11249, "جيريمي دوكو", 22, SpecificPosition.LW, 95, 81, 78, 42, 75, new[] { SpecificPosition.RW }),
            (11250, "روميلو لوكاكو", 10, SpecificPosition.ST, 82, 76, 88, 45, 93, null),
            (11251, "لياندرو تروسار", 11, SpecificPosition.RW, 84, 84, 84, 52, 72, new[] { SpecificPosition.LW, SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11252, "ماتز سيلس", 12, SpecificPosition.GK, 60, 78, 15, 83, 82, null),
            (11253, "زنو ديباست", 2, SpecificPosition.CB, 79, 81, 52, 83, 84, new[] { SpecificPosition.RB }),
            (11254, "يان فيرتونخين", 5, SpecificPosition.CB, 65, 78, 55, 82, 81, null),
            (11255, "توماس مونييه", 15, SpecificPosition.RB, 77, 80, 72, 80, 84, null),
            (11256, "أوريل مانجالا", 18, SpecificPosition.CM, 80, 82, 74, 81, 83, new[] { SpecificPosition.CDM }),
            (11257, "آرني إنجيلز", 17, SpecificPosition.CM, 81, 84, 79, 80, 81, null),
            (11258, "شارل دي كيتيلايري", 14, SpecificPosition.CAM, 82, 84, 82, 60, 82, new[] { SpecificPosition.ST, SpecificPosition.RW }),
            (11259, "دودي لوكيباكيو", 19, SpecificPosition.RW, 89, 79, 80, 48, 78, null),
            (11260, "يوهان باكايوكو", 20, SpecificPosition.RW, 90, 83, 82, 45, 78, null),
            (11261, "لويس أوبيندا", 9, SpecificPosition.ST, 93, 76, 85, 42, 82, null),
            (11262, "توماس كامينسكي", 13, SpecificPosition.GK, 58, 74, 15, 80, 80, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_BEL", "بلجيكا", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 10. تركيا (Turkey) - 22 لاعباً
    public static Team CreateTurkey()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11271, "ميرت جونوك", 1, SpecificPosition.GK, 60, 79, 18, 83, 82, null),
            (11272, "فيردي كاديوغلو", 20, SpecificPosition.LB, 86, 84, 74, 82, 80, new[] { SpecificPosition.RB, SpecificPosition.LWB }),
            (11273, "عبد الكريم بارداكتشي", 14, SpecificPosition.CB, 74, 80, 62, 85, 87, null),
            (11274, "ميريه ديميرال", 3, SpecificPosition.CB, 80, 72, 45, 86, 88, null),
            (11275, "ميرت مولدور", 18, SpecificPosition.RB, 84, 79, 65, 80, 80, new[] { SpecificPosition.LB }),
            (11276, "إسماعيل يوكسيك", 16, SpecificPosition.CDM, 80, 80, 70, 82, 83, null),
            (11277, "هاكان تشالهان أوغلو", 10, SpecificPosition.CM, 76, 92, 86, 80, 76, new[] { SpecificPosition.CDM }),
            (11278, "أوركون كوكتشو", 6, SpecificPosition.CM, 78, 88, 84, 75, 79, new[] { SpecificPosition.CAM }),
            (11279, "كريم أكتوركوغلو", 7, SpecificPosition.LW, 88, 83, 82, 50, 74, new[] { SpecificPosition.RW }),
            (11280, "باريش ألبر يلماز", 21, SpecificPosition.ST, 91, 80, 81, 68, 86, new[] { SpecificPosition.RW }),
            (11281, "أردا جولر", 8, SpecificPosition.RW, 84, 89, 84, 52, 68, new[] { SpecificPosition.CAM })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11282, "ألتاي بايندير", 12, SpecificPosition.GK, 60, 78, 15, 82, 83, null),
            (11283, "شاغلار سويونجو", 4, SpecificPosition.CB, 78, 74, 45, 83, 85, null),
            (11284, "سامت أكايدين", 2, SpecificPosition.CB, 75, 71, 40, 80, 84, null),
            (11285, "زكي تشيليك", 22, SpecificPosition.RB, 83, 78, 62, 80, 80, null),
            (11286, "كان أيهان", 5, SpecificPosition.CDM, 78, 80, 68, 82, 83, new[] { SpecificPosition.CB, SpecificPosition.RB }),
            (11287, "صالح أوزجان", 15, SpecificPosition.CDM, 76, 81, 72, 81, 83, null),
            (11288, "عرفان كان قهوجي", 17, SpecificPosition.RW, 80, 86, 83, 60, 75, new[] { SpecificPosition.CAM }),
            (11289, "كنان يلدز", 19, SpecificPosition.LW, 87, 82, 83, 55, 76, new[] { SpecificPosition.ST }),
            (11290, "جنك توسون", 9, SpecificPosition.ST, 76, 75, 81, 48, 82, null),
            (11291, "سميح كيليتشوي", 24, SpecificPosition.ST, 85, 74, 82, 45, 82, null),
            (11292, "أوجورجان تشاكر", 23, SpecificPosition.GK, 60, 78, 15, 83, 82, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_TUR", "تركيا", FormationType.Formation4231_Wide, true, starters, bench);
    }

    // 11. سويسرا (Switzerland) - 22 لاعباً
    public static Team CreateSwitzerland()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11301, "يان سومر", 1, SpecificPosition.GK, 62, 85, 18, 87, 80, null),
            (11302, "ريكاردو رودريجيز", 13, SpecificPosition.LB, 75, 83, 72, 82, 81, new[] { SpecificPosition.CB }),
            (11303, "مانويل أكانجي", 5, SpecificPosition.CB, 85, 82, 50, 88, 87, new[] { SpecificPosition.RB, SpecificPosition.CDM }),
            (11304, "فابيان شير", 22, SpecificPosition.CB, 76, 84, 72, 84, 84, null),
            (11305, "سيلفان فيدمر", 3, SpecificPosition.RB, 83, 79, 68, 80, 81, new[] { SpecificPosition.RWB }),
            (11306, "جرانيت تشاكا", 10, SpecificPosition.CDM, 72, 90, 83, 85, 84, new[] { SpecificPosition.CM }),
            (11307, "ريمو فريولير", 8, SpecificPosition.CM, 78, 84, 78, 83, 82, null),
            (11308, "ميشيل أيبشير", 20, SpecificPosition.CM, 80, 82, 78, 80, 80, new[] { SpecificPosition.LWB, SpecificPosition.LM }),
            (11309, "روبن فارجاس", 17, SpecificPosition.LW, 86, 81, 80, 55, 74, new[] { SpecificPosition.RW }),
            (11310, "بريل إمبولو", 7, SpecificPosition.ST, 84, 75, 82, 45, 87, null),
            (11311, "دان ندوي", 19, SpecificPosition.RW, 90, 79, 80, 62, 80, new[] { SpecificPosition.RWB, SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11312, "جريجور كوبيل", 12, SpecificPosition.GK, 62, 80, 18, 88, 86, null),
            (11313, "نيكو إلفيدي", 4, SpecificPosition.CB, 78, 76, 45, 82, 84, null),
            (11314, "سيدريك زيزيجر", 15, SpecificPosition.CB, 76, 73, 40, 80, 85, null),
            (11315, "دينيس زكريا", 6, SpecificPosition.CDM, 82, 80, 74, 84, 88, new[] { SpecificPosition.CB }),
            (11316, "فنسنت سييرو", 16, SpecificPosition.CM, 76, 83, 80, 80, 80, null),
            (11317, "فابيان ريدر", 26, SpecificPosition.CAM, 82, 82, 79, 65, 78, new[] { SpecificPosition.RW }),
            (11318, "شيردان شاكيري", 23, SpecificPosition.CAM, 74, 87, 85, 50, 78, new[] { SpecificPosition.RW }),
            (11319, "ستيفن زوبير", 14, SpecificPosition.LW, 82, 82, 80, 58, 78, null),
            (11320, "زكي أمدوني", 25, SpecificPosition.ST, 84, 79, 81, 50, 78, null),
            (11321, "كوادوو دواه", 18, SpecificPosition.ST, 86, 72, 81, 42, 80, null),
            (11322, "يفون مفوجو", 21, SpecificPosition.GK, 60, 76, 15, 81, 80, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_SUI", "سويسرا", FormationType.Formation343, true, starters, bench);
    }

    // 12. الدنمارك (Denmark) - 22 لاعباً
    public static Team CreateDenmark()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11331, "كاسبر شمايكل", 1, SpecificPosition.GK, 60, 82, 18, 84, 83, null),
            (11332, "فيكتور كريستيانسن", 17, SpecificPosition.LWB, 84, 79, 64, 80, 79, new[] { SpecificPosition.LB }),
            (11333, "يانيك فيسترجارد", 3, SpecificPosition.CB, 64, 79, 52, 83, 91, null),
            (11334, "يواكيم أندرسن", 2, SpecificPosition.CB, 76, 83, 55, 84, 85, null),
            (11335, "أندرياس كريستنسن", 6, SpecificPosition.CB, 77, 84, 50, 86, 82, new[] { SpecificPosition.CDM }),
            (11336, "ألكسندر باه", 18, SpecificPosition.RWB, 86, 80, 68, 81, 82, new[] { SpecificPosition.RB }),
            (11337, "بيير إميل هويبيرج", 23, SpecificPosition.CDM, 74, 84, 76, 84, 85, new[] { SpecificPosition.CM }),
            (11338, "مورتن هيولماند", 21, SpecificPosition.CM, 78, 85, 75, 85, 86, null),
            (11339, "كريستيان إريكسن", 10, SpecificPosition.CAM, 72, 91, 83, 62, 70, new[] { SpecificPosition.CM }),
            (11340, "راسموس هويلوند", 9, SpecificPosition.ST, 88, 75, 84, 45, 85, null),
            (11341, "يوناس فيند", 19, SpecificPosition.ST, 78, 82, 82, 52, 85, new[] { SpecificPosition.CAM })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11342, "فريدريك رونو", 16, SpecificPosition.GK, 60, 78, 15, 83, 82, null),
            (11343, "فيكتور نيلسون", 13, SpecificPosition.CB, 76, 74, 40, 83, 85, null),
            (11344, "سيمون كيير", 4, SpecificPosition.CB, 65, 76, 45, 82, 83, null),
            (11345, "يواكيم ماهلي", 5, SpecificPosition.RB, 85, 80, 74, 80, 81, new[] { SpecificPosition.LB, SpecificPosition.RWB }),
            (11346, "توماس ديلاني", 8, SpecificPosition.CM, 75, 81, 75, 82, 84, null),
            (11347, "ميكيل دامسجارد", 14, SpecificPosition.LW, 84, 83, 78, 55, 72, new[] { SpecificPosition.CAM }),
            (11348, "أندرياس سكوف أولسن", 11, SpecificPosition.RW, 86, 82, 82, 52, 78, null),
            (11349, "يوسف بولسن", 20, SpecificPosition.ST, 82, 76, 79, 58, 86, null),
            (11350, "كاسبر دولبرج", 12, SpecificPosition.ST, 81, 77, 83, 45, 80, null),
            (11351, "ألبرت جرونباك", 25, SpecificPosition.CAM, 85, 82, 80, 52, 76, null),
            (11352, "مادس هيرمانسن", 22, SpecificPosition.GK, 60, 77, 15, 82, 81, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_DEN", "الدنمارك", FormationType.Formation343, true, starters, bench);
    }

    // 13. النمسا (Austria) - 22 لاعباً
    public static Team CreateAustria()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11361, "باتريك بينتز", 13, SpecificPosition.GK, 60, 78, 18, 82, 81, null),
            (11362, "فيليب مويني", 16, SpecificPosition.LB, 83, 78, 65, 79, 78, new[] { SpecificPosition.RB }),
            (11363, "كيفين دانسو", 4, SpecificPosition.CB, 83, 75, 42, 86, 89, null),
            (11364, "فيليب لينهارت", 15, SpecificPosition.CB, 78, 77, 45, 84, 83, null),
            (11365, "ستيفان بوش", 5, SpecificPosition.RB, 82, 79, 68, 82, 83, new[] { SpecificPosition.CB }),
            (11366, "نيكولاس سيوالد", 6, SpecificPosition.CDM, 82, 83, 72, 84, 82, new[] { SpecificPosition.CM }),
            (11367, "كونراد لايمر", 20, SpecificPosition.CM, 84, 80, 72, 83, 85, new[] { SpecificPosition.RB, SpecificPosition.CDM }),
            (11368, "مارسيل سابيتزر", 9, SpecificPosition.CAM, 81, 86, 85, 78, 82, new[] { SpecificPosition.CM, SpecificPosition.LW }),
            (11369, "كريستوف باومغارتنر", 19, SpecificPosition.CAM, 83, 84, 83, 68, 80, new[] { SpecificPosition.ST }),
            (11370, "ماركو أرناوتوفيتش", 7, SpecificPosition.ST, 76, 82, 85, 52, 86, null),
            (11371, "باتريك فيمر", 23, SpecificPosition.RW, 88, 80, 79, 65, 80, new[] { SpecificPosition.LW, SpecificPosition.RWB })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11372, "هاينتس ليندنر", 1, SpecificPosition.GK, 58, 74, 15, 80, 80, null),
            (11373, "ماكسيميليان فوبر", 2, SpecificPosition.CB, 79, 76, 50, 82, 84, new[] { SpecificPosition.LB }),
            (11374, "غيرنوت تراونر", 3, SpecificPosition.CB, 68, 77, 50, 83, 84, null),
            (11375, "ألكسندر براس", 8, SpecificPosition.LB, 85, 80, 72, 79, 79, new[] { SpecificPosition.LM, SpecificPosition.CM }),
            (11376, "فلوريان جريليتش", 10, SpecificPosition.CDM, 74, 84, 76, 81, 80, new[] { SpecificPosition.CB }),
            (11377, "رومانو شميد", 18, SpecificPosition.CAM, 84, 82, 78, 58, 70, new[] { SpecificPosition.RW }),
            (11378, "فلوريان كاينز", 11, SpecificPosition.LW, 80, 82, 78, 55, 74, null),
            (11379, "ميكايل غريغوريتش", 11, SpecificPosition.ST, 74, 78, 83, 55, 88, null),
            (11380, "أندرياس ويمان", 14, SpecificPosition.ST, 80, 76, 79, 52, 78, null),
            (11381, "ماتياس زايدل", 17, SpecificPosition.CAM, 80, 80, 76, 55, 75, null),
            (11382, "نيكلاس هيدل", 12, SpecificPosition.GK, 58, 72, 15, 78, 78, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_AUT", "النمسا", FormationType.Formation4231_Wide, true, starters, bench);
    }

    // 14. النرويج (Norway) - 22 لاعباً
    public static Team CreateNorway()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11391, "أورجان نيلاند", 1, SpecificPosition.GK, 60, 78, 18, 83, 82, null),
            (11392, "دافيد مولر وولف", 3, SpecificPosition.LB, 85, 78, 64, 79, 78, new[] { SpecificPosition.LWB }),
            (11393, "ليو أوستيجارد", 4, SpecificPosition.CB, 77, 73, 48, 82, 86, null),
            (11394, "أندرياس هانش أولسن", 15, SpecificPosition.CB, 78, 74, 42, 82, 84, null),
            (11395, "جوليان رييرسون", 22, SpecificPosition.RB, 84, 80, 68, 82, 84, new[] { SpecificPosition.LB, SpecificPosition.RWB }),
            (11396, "ساندر بيرج", 8, SpecificPosition.CDM, 76, 84, 74, 83, 87, new[] { SpecificPosition.CM }),
            (11397, "باتريك بيرج", 6, SpecificPosition.CM, 78, 83, 72, 81, 81, new[] { SpecificPosition.CDM }),
            (11398, "مارتن أوديغارد", 10, SpecificPosition.CAM, 82, 92, 85, 72, 74, new[] { SpecificPosition.CM }),
            (11399, "أنطونيو نوسا", 20, SpecificPosition.LW, 91, 81, 80, 45, 72, new[] { SpecificPosition.RW }),
            (11400, "إرلينغ هالاند", 9, SpecificPosition.ST, 91, 78, 94, 52, 91, null),
            (11401, "ألكسندر سورلوث", 11, SpecificPosition.RW, 84, 78, 86, 52, 88, new[] { SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11402, "ماتياس ديلغارد", 12, SpecificPosition.GK, 58, 72, 15, 78, 78, null),
            (11403, "كريستوفر آير", 21, SpecificPosition.CB, 78, 75, 45, 82, 86, new[] { SpecificPosition.RB }),
            (11404, "ستيغ هيدلستاد", 5, SpecificPosition.CB, 76, 72, 40, 79, 82, null),
            (11405, "ماركوس هولمغرين بيدرسن", 2, SpecificPosition.RB, 88, 76, 58, 78, 79, null),
            (11406, "مورتن ثورسبي", 16, SpecificPosition.CM, 78, 78, 72, 81, 86, null),
            (11407, "كريستيان ثورستفيدت", 18, SpecificPosition.CM, 78, 80, 78, 78, 83, new[] { SpecificPosition.CAM }),
            (11408, "أوسكار بوب", 7, SpecificPosition.RW, 87, 85, 80, 48, 68, new[] { SpecificPosition.CAM }),
            (11409, "ينس بيتر هوج", 17, SpecificPosition.LW, 84, 79, 77, 48, 74, null),
            (11410, "يورغن ستراند لارسن", 19, SpecificPosition.ST, 82, 74, 82, 48, 86, null),
            (11411, "أرون دونوم", 14, SpecificPosition.RW, 84, 80, 78, 55, 77, null),
            (11412, "إيغيل سيلفيك", 13, SpecificPosition.GK, 58, 72, 15, 78, 79, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_NOR", "النرويج", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 15. بولندا (Poland) - 22 لاعباً
    public static Team CreatePoland()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11421, "لوكاش سكوروبسكي", 12, SpecificPosition.GK, 60, 80, 18, 85, 83, null),
            (11422, "نيكولا زاليفسكي", 21, SpecificPosition.LWB, 88, 82, 75, 78, 74, new[] { SpecificPosition.LM, SpecificPosition.LB }),
            (11423, "ياكوب كيفيور", 14, SpecificPosition.CB, 80, 78, 48, 84, 83, new[] { SpecificPosition.LB }),
            (11424, "يان بيدناريك", 5, SpecificPosition.CB, 74, 75, 45, 83, 85, null),
            (11425, "برزيميسلاف فرانكوفسكي", 19, SpecificPosition.RWB, 86, 81, 74, 78, 79, new[] { SpecificPosition.RM }),
            (11426, "بيوتر زيلينسكي", 10, SpecificPosition.CM, 80, 89, 83, 76, 75, new[] { SpecificPosition.CAM }),
            (11427, "بارتوش سليز", 8, SpecificPosition.CDM, 78, 80, 70, 82, 84, null),
            (11428, "سيباستيان شيمانسكي", 20, SpecificPosition.CM, 85, 85, 83, 68, 77, new[] { SpecificPosition.CAM }),
            (11429, "كاسبر أوربانسكي", 26, SpecificPosition.CAM, 82, 84, 78, 65, 72, new[] { SpecificPosition.LW }),
            (11430, "روبرت ليفاندوفسكي", 9, SpecificPosition.ST, 78, 82, 92, 50, 85, null),
            (11431, "كارول سفيديرسكي", 7, SpecificPosition.ST, 82, 78, 82, 50, 80, new[] { SpecificPosition.CAM })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11432, "مارتشين بولكا", 1, SpecificPosition.GK, 60, 79, 18, 86, 88, null),
            (11433, "باول دافيدوفيتش", 3, SpecificPosition.CB, 75, 74, 42, 81, 84, null),
            (11434, "سيباستيان والوكييفيتش", 4, SpecificPosition.CB, 78, 73, 40, 81, 83, null),
            (11435, "تيموتوش بوخاتش", 2, SpecificPosition.LB, 85, 77, 65, 77, 78, null),
            (11436, "بارتوش بيريشينسكي", 18, SpecificPosition.RB, 81, 76, 58, 79, 80, null),
            (11437, "ياكوب مودير", 16, SpecificPosition.CM, 78, 82, 78, 80, 83, null),
            (11438, "داميان شيمانسكي", 6, SpecificPosition.CDM, 76, 79, 72, 80, 82, null),
            (11439, "ياكوب بيوتروفسكي", 17, SpecificPosition.CM, 80, 80, 78, 78, 84, null),
            (11440, "كريستوف بياتيك", 23, SpecificPosition.ST, 81, 72, 82, 45, 83, null),
            (11441, "آدم بوكسا", 11, SpecificPosition.ST, 78, 72, 82, 48, 87, null),
            (11442, "بارتلومي دراغوفسكي", 22, SpecificPosition.GK, 58, 74, 15, 80, 80, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_POL", "بولندا", FormationType.Formation352, true, starters, bench);
    }

    // 16. أوكرانيا (Ukraine) - 22 لاعباً
    public static Team CreateUkraine()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11451, "أندري لونين", 1, SpecificPosition.GK, 60, 82, 18, 85, 84, null),
            (11452, "فيتالي ميكولينكو", 16, SpecificPosition.LB, 84, 79, 64, 82, 82, new[] { SpecificPosition.LWB }),
            (11453, "إيليا زابارني", 13, SpecificPosition.CB, 80, 78, 42, 85, 86, null),
            (11454, "ميكولا ماتفيينكو", 22, SpecificPosition.CB, 78, 80, 48, 83, 81, new[] { SpecificPosition.LB }),
            (11455, "يوخيم كونوبليا", 2, SpecificPosition.RB, 84, 78, 62, 79, 79, new[] { SpecificPosition.RWB }),
            (11456, "تاراس ستيبانينكو", 6, SpecificPosition.CDM, 68, 80, 65, 84, 83, null),
            (11457, "ألكسندر زينتشينكو", 17, SpecificPosition.CM, 80, 87, 76, 80, 75, new[] { SpecificPosition.LB, SpecificPosition.CDM }),
            (11458, "جورجي سوداكوف", 7, SpecificPosition.CAM, 83, 86, 82, 65, 75, new[] { SpecificPosition.CM }),
            (11459, "ميخايلو مودريك", 10, SpecificPosition.LW, 95, 80, 79, 45, 72, null),
            (11460, "أرتيم دوفبيك", 11, SpecificPosition.ST, 84, 76, 87, 48, 88, null),
            (11461, "فيكتور تسيجانكوف", 15, SpecificPosition.RW, 86, 85, 83, 55, 74, new[] { SpecificPosition.CAM })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11462, "أناتولي تروبين", 12, SpecificPosition.GK, 60, 80, 18, 86, 88, null),
            (11463, "ماكسيم تالوفيروف", 3, SpecificPosition.CB, 76, 72, 40, 80, 85, null),
            (11464, "أوليكساندر سفاتوك", 4, SpecificPosition.CB, 75, 72, 40, 80, 83, null),
            (11465, "أوليكساندر تيمتشيك", 24, SpecificPosition.RB, 86, 76, 60, 78, 78, null),
            (11466, "فولوديمير برازكو", 18, SpecificPosition.CDM, 79, 82, 75, 81, 84, null),
            (11467, "ميكولا شابارينكو", 19, SpecificPosition.CM, 82, 85, 79, 76, 76, null),
            (11468, "روسلان مالينوفسكي", 8, SpecificPosition.CM, 75, 86, 85, 74, 82, new[] { SpecificPosition.CAM }),
            (11469, "أندريه يارمولينكو", 7, SpecificPosition.RW, 75, 84, 82, 52, 78, null),
            (11470, "رومان ياريمتشوك", 9, SpecificPosition.ST, 82, 76, 82, 48, 84, null),
            (11471, "فلاديسلاف فانات", 25, SpecificPosition.ST, 85, 74, 81, 42, 78, null),
            (11472, "هيورهي بوشتشان", 23, SpecificPosition.GK, 58, 76, 15, 81, 82, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_UKR", "أوكرانيا", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 17. اسكتلندا (Scotland) - 22 لاعباً
    public static Team CreateScotland()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11481, "أنجوس غان", 1, SpecificPosition.GK, 60, 78, 18, 83, 82, null),
            (11482, "أندرو روبرتسون", 3, SpecificPosition.LWB, 86, 86, 68, 84, 82, new[] { SpecificPosition.LB, SpecificPosition.LM }),
            (11483, "كيران تيرني", 6, SpecificPosition.CB, 84, 81, 65, 83, 82, new[] { SpecificPosition.LB }),
            (11484, "جاك هيندري", 13, SpecificPosition.CB, 78, 74, 45, 82, 85, null),
            (11485, "سكوت ماكينا", 5, SpecificPosition.CB, 74, 72, 40, 82, 87, null),
            (11486, "أنطوني رالستون", 2, SpecificPosition.RWB, 81, 75, 60, 78, 82, new[] { SpecificPosition.RB }),
            (11487, "كالوم ماكجريجور", 8, SpecificPosition.CDM, 78, 86, 78, 83, 81, new[] { SpecificPosition.CM }),
            (11488, "بيلي جيلمور", 14, SpecificPosition.CM, 80, 87, 72, 81, 74, new[] { SpecificPosition.CDM }),
            (11489, "سكوت ماكتوميناي", 4, SpecificPosition.CAM, 82, 82, 85, 84, 88, new[] { SpecificPosition.CM, SpecificPosition.ST }),
            (11490, "جون ماكجين", 7, SpecificPosition.CAM, 82, 84, 82, 81, 86, new[] { SpecificPosition.CM, SpecificPosition.RW }),
            (11491, "تشي آدامز", 10, SpecificPosition.ST, 84, 76, 82, 52, 84, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11492, "ليام كيلي", 12, SpecificPosition.GK, 58, 72, 15, 78, 78, null),
            (11493, "ريان بورتيوس", 15, SpecificPosition.CB, 76, 72, 45, 80, 85, null),
            (11494, "جرانت هانلي", 16, SpecificPosition.CB, 70, 70, 42, 80, 86, null),
            (11495, "جريج تايلور", 22, SpecificPosition.LB, 83, 80, 66, 79, 78, null),
            (11496, "كينى ماكلين", 23, SpecificPosition.CM, 76, 81, 74, 79, 81, null),
            (11497, "ريان كريستي", 11, SpecificPosition.CAM, 83, 83, 80, 74, 80, new[] { SpecificPosition.RW, SpecificPosition.CM }),
            (11498, "ستيوارت أرمسترونج", 17, SpecificPosition.CM, 80, 81, 78, 75, 78, null),
            (11499, "بين دوك", 20, SpecificPosition.RW, 91, 76, 74, 42, 70, null),
            (11500, "لورينس شانكلاند", 9, SpecificPosition.ST, 78, 74, 83, 45, 83, null),
            (11501, "تومي كونواي", 19, SpecificPosition.ST, 84, 72, 79, 42, 80, null),
            (11502, "زاندر كلارك", 21, SpecificPosition.GK, 58, 72, 15, 78, 79, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_SCO", "اسكتلندا", FormationType.Formation343, true, starters, bench);
    }

    // 18. السويد (Sweden) - 22 لاعباً
    public static Team CreateSweden()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11511, "روبن أولسن", 1, SpecificPosition.GK, 60, 78, 18, 83, 83, null),
            (11512, "لودفيج أوغوستينسون", 6, SpecificPosition.LB, 82, 80, 68, 80, 79, new[] { SpecificPosition.LWB }),
            (11513, "فيكتور ليندلوف", 3, SpecificPosition.CB, 76, 81, 48, 84, 82, null),
            (11514, "إيزاك هين", 4, SpecificPosition.CB, 84, 74, 40, 85, 88, null),
            (11515, "إميل كرافت", 2, SpecificPosition.RB, 82, 76, 58, 80, 81, new[] { SpecificPosition.CB }),
            (11516, "أنتون ساليتروس", 8, SpecificPosition.CM, 80, 82, 74, 79, 80, new[] { SpecificPosition.CDM }),
            (11517, "ياسين أياري", 18, SpecificPosition.CM, 82, 82, 76, 78, 78, null),
            (11518, "ديان كولوسيفسكي", 21, SpecificPosition.CAM, 86, 86, 83, 68, 84, new[] { SpecificPosition.RW, SpecificPosition.CM }),
            (11519, "ألكسندر إيساك", 11, SpecificPosition.ST, 89, 81, 88, 48, 80, new[] { SpecificPosition.LW }),
            (11520, "فيكتور جيوكيريس", 9, SpecificPosition.ST, 89, 82, 89, 55, 92, null),
            (11521, "أنتوني إيلانغا", 19, SpecificPosition.RW, 92, 78, 79, 48, 76, new[] { SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11522, "فيكتور يوهانسون", 12, SpecificPosition.GK, 60, 76, 15, 81, 82, null),
            (11523, "أليكس دوجلاس", 5, SpecificPosition.CB, 80, 72, 40, 79, 83, null),
            (11524, "كارل ستارفيلت", 13, SpecificPosition.CB, 76, 72, 42, 80, 84, null),
            (11525, "جابرييل جودموندسون", 14, SpecificPosition.LB, 85, 78, 66, 79, 80, new[] { SpecificPosition.LM }),
            (11526, "هوغو لارسون", 16, SpecificPosition.CM, 83, 83, 76, 80, 79, null),
            (11527, "ينس كايوستي", 15, SpecificPosition.CM, 82, 80, 74, 80, 84, null),
            (11528, "إميل فوسبرغ", 10, SpecificPosition.CAM, 78, 85, 82, 55, 74, new[] { SpecificPosition.LW }),
            (11529, "سيباستيان ناناسي", 22, SpecificPosition.LW, 86, 83, 80, 50, 72, null),
            (11530, "كين سيم Sema", 7, SpecificPosition.LWB, 84, 78, 74, 76, 82, null),
            (11531, "نيكلاس إلياسون", 17, SpecificPosition.RW, 85, 82, 76, 52, 74, null),
            (11532, "كريستوفر نوردفيلت", 23, SpecificPosition.GK, 58, 72, 15, 78, 78, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_SWE", "السويد", FormationType.Formation442, true, starters, bench);
    }

    // 19. صربيا (Serbia) - 22 لاعباً
    public static Team CreateSerbia()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11541, "بريدراج رايكوفيتش", 1, SpecificPosition.GK, 60, 80, 18, 85, 84, null),
            (11542, "ستراهينيا بافلوفيتش", 2, SpecificPosition.CB, 84, 76, 52, 86, 90, new[] { SpecificPosition.LB }),
            (11543, "نيكولا ميلينكوفيتش", 4, SpecificPosition.CB, 78, 76, 45, 86, 88, null),
            (11544, "ميلوس فيلكوفيتش", 13, SpecificPosition.CB, 76, 75, 42, 82, 83, null),
            (11545, "فيليب كوستيتش", 17, SpecificPosition.LWB, 86, 86, 78, 78, 82, new[] { SpecificPosition.LM }),
            (11546, "أندريا زيفكوفيتش", 14, SpecificPosition.RWB, 85, 83, 80, 72, 74, new[] { SpecificPosition.RM, SpecificPosition.RW }),
            (11547, "ساشا لوكيتش", 16, SpecificPosition.CM, 80, 83, 76, 82, 82, new[] { SpecificPosition.CDM }),
            (11548, "إيفان إيليتش", 19, SpecificPosition.CM, 78, 85, 79, 80, 78, null),
            (11549, "لازار ساماردزيتش", 20, SpecificPosition.CAM, 82, 86, 83, 60, 75, null),
            (11550, "ألكسندر ميتروفيتش", 9, SpecificPosition.ST, 78, 78, 88, 55, 92, null),
            (11551, "دوشان فلاهوفيتش", 7, SpecificPosition.ST, 84, 78, 88, 45, 86, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11552, "جورجي بتروفيتش", 12, SpecificPosition.GK, 62, 79, 18, 84, 84, null),
            (11553, "ستراهينيا إيراكوفيتش", 3, SpecificPosition.CB, 80, 74, 40, 80, 83, new[] { SpecificPosition.RB }),
            (11554, "سرديان بابيتش", 15, SpecificPosition.CB, 74, 72, 40, 81, 85, null),
            (11555, "كوستا نيديليكوفيتش", 21, SpecificPosition.RB, 88, 75, 58, 77, 78, null),
            (11556, "نيمانيا ماكسيموفيتش", 6, SpecificPosition.CDM, 78, 81, 74, 81, 83, null),
            (11557, "ماركو جروييتش", 8, SpecificPosition.CM, 72, 80, 75, 80, 86, null),
            (11558, "فيليكو بيرمانشيفيتش", 11, SpecificPosition.LW, 87, 81, 80, 52, 76, null),
            (11559, "ميات غاتشينوفيتش", 5, SpecificPosition.CM, 82, 80, 76, 75, 76, null),
            (11560, "لوكا يوفيتش", 18, SpecificPosition.ST, 81, 76, 83, 45, 82, null),
            (11561, "بيتار راتكوف", 22, SpecificPosition.ST, 80, 70, 78, 42, 84, null),
            (11562, "فانيا ميلينكوفيتش سافيتش", 23, SpecificPosition.GK, 58, 80, 20, 84, 89, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_SRB", "صربيا", FormationType.Formation352, true, starters, bench);
    }

    // 20. التشيك (Czechia) - 22 لاعباً
    public static Team CreateCzechia()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11571, "يندريش ستانيك", 1, SpecificPosition.GK, 60, 78, 18, 83, 84, null),
            (11572, "دافيد يوراسيك", 15, SpecificPosition.LWB, 86, 79, 70, 78, 80, new[] { SpecificPosition.LB }),
            (11573, "لاديسلاف كرييتشي", 4, SpecificPosition.CB, 78, 80, 70, 84, 87, new[] { SpecificPosition.CDM }),
            (11574, "روبن هراناتش", 3, SpecificPosition.CB, 78, 74, 42, 82, 84, null),
            (11575, "مارتن فيتيك", 2, SpecificPosition.CB, 78, 74, 40, 82, 85, null),
            (11576, "فلاديمير كوفال", 5, SpecificPosition.RWB, 82, 81, 65, 81, 82, new[] { SpecificPosition.RB }),
            (11577, "توماس سوتشيك", 22, SpecificPosition.CDM, 75, 81, 80, 85, 89, new[] { SpecificPosition.CM }),
            (11578, "لوكاس بروفود", 14, SpecificPosition.CM, 82, 83, 79, 78, 82, new[] { SpecificPosition.CAM, SpecificPosition.LM }),
            (11579, "أنتونين باراك", 7, SpecificPosition.CAM, 76, 83, 82, 70, 84, null),
            (11580, "باتريك شيك", 10, SpecificPosition.ST, 83, 78, 86, 48, 84, null),
            (11581, "يان كوشتا", 11, SpecificPosition.ST, 84, 75, 82, 55, 83, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11582, "ماتي كوفار", 16, SpecificPosition.GK, 60, 76, 18, 81, 82, null),
            (11583, "توماس هوليس", 18, SpecificPosition.CB, 76, 78, 62, 82, 84, new[] { SpecificPosition.CDM, SpecificPosition.RB }),
            (11584, "دافيد زيما", 6, SpecificPosition.CB, 78, 73, 40, 80, 82, null),
            (11585, "دافيد دوديرا", 17, SpecificPosition.RWB, 86, 78, 70, 77, 78, null),
            (11586, "ماتي رينيس", 12, SpecificPosition.LB, 83, 76, 62, 76, 78, null),
            (11587, "لوكاس تشيرف", 21, SpecificPosition.CM, 80, 80, 74, 79, 81, null),
            (11588, "بافل شولك", 25, SpecificPosition.CAM, 83, 82, 81, 60, 76, null),
            (11589, "فاسلاف تشيرني", 20, SpecificPosition.RW, 86, 82, 81, 50, 72, null),
            (11590, "آدم هلوزيك", 9, SpecificPosition.ST, 84, 80, 82, 52, 82, new[] { SpecificPosition.LW, SpecificPosition.CAM }),
            (11591, "توماس خوري", 19, SpecificPosition.ST, 72, 74, 82, 50, 92, null),
            (11592, "فيتيزسلاف ياروش", 23, SpecificPosition.GK, 60, 75, 15, 80, 80, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_CZE", "التشيك", FormationType.Formation352, true, starters, bench);
    }

    // 21. المجر (Hungary) - 22 لاعباً
    public static Team CreateHungary()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11601, "بيتر غولاتشي", 1, SpecificPosition.GK, 60, 80, 18, 85, 82, null),
            (11602, "ميلوش كيركيز", 11, SpecificPosition.LWB, 87, 79, 68, 80, 81, new[] { SpecificPosition.LB, SpecificPosition.LM }),
            (11603, "مارتون دارداي", 3, SpecificPosition.CB, 78, 78, 52, 82, 83, new[] { SpecificPosition.LB }),
            (11604, "ويلي أوربان", 6, SpecificPosition.CB, 74, 76, 52, 86, 88, null),
            (11605, "بوتوند بالوغ", 4, SpecificPosition.CB, 78, 73, 40, 81, 84, null),
            (11606, "بيندغوز بولا", 14, SpecificPosition.RWB, 85, 78, 70, 78, 79, new[] { SpecificPosition.RM }),
            (11607, "أندراس شافر", 13, SpecificPosition.CM, 81, 83, 76, 82, 83, null),
            (11608, "آدم ناجي", 8, SpecificPosition.CM, 76, 82, 68, 81, 79, new[] { SpecificPosition.CDM }),
            (11609, "دومينيك سوبوسلاي", 10, SpecificPosition.CAM, 85, 90, 87, 74, 82, new[] { SpecificPosition.CM, SpecificPosition.RW }),
            (11610, "رولاند سالاي", 20, SpecificPosition.CAM, 84, 80, 80, 60, 78, new[] { SpecificPosition.RW, SpecificPosition.LW }),
            (11611, "بارناباس فارجا", 19, SpecificPosition.ST, 80, 75, 84, 52, 86, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11612, "دينيس ديبوس", 12, SpecificPosition.GK, 58, 76, 15, 81, 81, null),
            (11613, "أتيلا سالاي", 2, SpecificPosition.CB, 76, 75, 45, 81, 85, new[] { SpecificPosition.LB }),
            (11614, "إندري بوتكا", 21, SpecificPosition.CB, 77, 72, 40, 79, 81, new[] { SpecificPosition.RB }),
            (11615, "زسولت ناغي", 18, SpecificPosition.LWB, 83, 77, 72, 77, 80, null),
            (11616, "لويك نيجو", 7, SpecificPosition.RWB, 85, 78, 68, 78, 78, null),
            (11617, "كالوم ستايلز", 17, SpecificPosition.CM, 80, 80, 74, 78, 78, new[] { SpecificPosition.LB }),
            (11618, "ميهالي كاتا", 16, SpecificPosition.CDM, 78, 78, 68, 79, 81, null),
            (11619, "دانييل غازداغ", 15, SpecificPosition.CAM, 82, 81, 80, 55, 75, null),
            (11620, "كيفين كسوبوث", 23, SpecificPosition.LW, 88, 76, 76, 45, 72, null),
            (11621, "مارتن آدم", 9, SpecificPosition.ST, 68, 70, 82, 48, 93, null),
            (11622, "بيتر زابانوس", 22, SpecificPosition.GK, 56, 70, 15, 76, 78, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_HUN", "المجر", FormationType.Formation343, true, starters, bench);
    }

    // 22. رومانيا (Romania) - 22 لاعباً
    public static Team CreateRomania()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11631, "فلورين نيتا", 1, SpecificPosition.GK, 60, 78, 18, 83, 82, null),
            (11632, "نيكوستور بانكو", 11, SpecificPosition.LB, 84, 79, 68, 79, 79, new[] { SpecificPosition.LWB }),
            (11633, "رادو دراجوشين", 3, SpecificPosition.CB, 82, 75, 42, 85, 87, null),
            (11634, "أندري بوركا", 15, SpecificPosition.CB, 76, 74, 45, 83, 86, null),
            (11635, "أندريه راتيو", 2, SpecificPosition.RB, 89, 78, 64, 80, 81, new[] { SpecificPosition.RWB }),
            (11636, "ماريوس مارين", 6, SpecificPosition.CDM, 78, 80, 70, 83, 84, null),
            (11637, "رازفان مارين", 18, SpecificPosition.CM, 78, 85, 83, 81, 81, new[] { SpecificPosition.CDM }),
            (11638, "نيكولاي ستانتشيو", 10, SpecificPosition.CAM, 80, 87, 85, 68, 76, new[] { SpecificPosition.CM }),
            (11639, "فلورينيل كومان", 17, SpecificPosition.LW, 86, 81, 81, 52, 76, new[] { SpecificPosition.RW }),
            (11640, "دينيس دراغوش", 19, SpecificPosition.ST, 85, 77, 83, 50, 81, new[] { SpecificPosition.LW }),
            (11641, "دينيس مان", 20, SpecificPosition.RW, 87, 83, 83, 55, 75, new[] { SpecificPosition.RM })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11642, "هوراتسيو مولدوفان", 12, SpecificPosition.GK, 60, 78, 15, 82, 82, null),
            (11643, "بوجدان راكوفيتان", 4, SpecificPosition.CB, 78, 72, 40, 80, 83, null),
            (11644, "يونوت نيديلتشيارو", 5, SpecificPosition.CB, 74, 71, 40, 79, 83, null),
            (11645, "فاسيلي موغوش", 22, SpecificPosition.RB, 81, 74, 58, 78, 82, null),
            (11646, "أدريان شوت", 8, SpecificPosition.CDM, 78, 80, 72, 79, 81, null),
            (11647, "داريوس أولارو", 21, SpecificPosition.CM, 82, 82, 80, 78, 80, new[] { SpecificPosition.CAM }),
            (11648, "إيانيس هاجي", 14, SpecificPosition.CAM, 80, 85, 81, 52, 72, new[] { SpecificPosition.RW, SpecificPosition.LW }),
            (11649, "فالنتين ميهايلا", 13, SpecificPosition.LW, 91, 78, 78, 48, 70, null),
            (11650, "جورج بوشكاش", 9, SpecificPosition.ST, 81, 72, 80, 48, 84, null),
            (11651, "دينيس أليبيك", 7, SpecificPosition.ST, 78, 78, 82, 45, 83, null),
            (11652, "ستيفان تارنوفانو", 16, SpecificPosition.GK, 58, 74, 15, 79, 80, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_ROU", "رومانيا", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 23. اليونان (Greece) - 22 لاعباً
    public static Team CreateGreece()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11661, "أوديسياس فلاخوديموس", 1, SpecificPosition.GK, 60, 80, 18, 84, 82, null),
            (11662, "كوستاس تسيميكاس", 21, SpecificPosition.LB, 85, 83, 68, 81, 78, new[] { SpecificPosition.LWB }),
            (11663, "كونستانتينوس مافروبانوس", 4, SpecificPosition.CB, 81, 74, 48, 85, 88, null),
            (11664, "بانايوتيس ريتسوس", 3, SpecificPosition.CB, 79, 76, 45, 83, 83, null),
            (11665, "جورج بالدوك", 2, SpecificPosition.RB, 84, 78, 62, 80, 81, new[] { SpecificPosition.RWB }),
            (11666, "مانوليس سيوبيس", 6, SpecificPosition.CDM, 82, 79, 65, 83, 85, null),
            (11667, "ديميتريوس كوربيليس", 8, SpecificPosition.CDM, 76, 81, 70, 82, 84, new[] { SpecificPosition.CM, SpecificPosition.CB }),
            (11668, "أنستاسيوس باكاسيتاس", 11, SpecificPosition.CAM, 78, 86, 85, 65, 80, new[] { SpecificPosition.CM }),
            (11669, "كريستوس تزوليس", 18, SpecificPosition.LW, 88, 80, 82, 50, 76, new[] { SpecificPosition.ST }),
            (11670, "فانجيليس بافليديس", 14, SpecificPosition.ST, 83, 78, 86, 45, 84, null),
            (11671, "جورجيوس ماسوراس", 7, SpecificPosition.RW, 85, 80, 81, 65, 78, new[] { SpecificPosition.LW, SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11672, "كونستانتينوس تزولاكيس", 12, SpecificPosition.GK, 60, 78, 15, 83, 82, null),
            (11673, "كونستانتينوس كولييراكيس", 5, SpecificPosition.CB, 80, 78, 52, 84, 85, null),
            (11674, "بانتيليس حاتزيدياكوس", 17, SpecificPosition.CB, 76, 75, 45, 81, 82, new[] { SpecificPosition.RB }),
            (11675, "ديميتريس جيانوليس", 3, SpecificPosition.LB, 84, 78, 62, 78, 77, null),
            (11676, "لازاروس روتا", 15, SpecificPosition.RB, 85, 76, 58, 78, 78, null),
            (11677, "سوتيريس أليكساندروبولوس", 16, SpecificPosition.CM, 82, 80, 72, 79, 81, null),
            (11678, "بيتروس مانتالوس", 20, SpecificPosition.CAM, 78, 83, 79, 62, 76, new[] { SpecificPosition.CM }),
            (11679, "كونستانتينوس كونستانتيلياس", 10, SpecificPosition.CAM, 85, 84, 80, 52, 70, new[] { SpecificPosition.LW }),
            (11680, "فوتيس يوانيديس", 9, SpecificPosition.ST, 83, 78, 84, 52, 86, null),
            (11681, "تاسوس دوفيكاس", 19, SpecificPosition.ST, 83, 74, 83, 45, 82, null),
            (11682, "كريستوس مانداس", 23, SpecificPosition.GK, 60, 76, 15, 81, 81, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_GRE", "اليونان", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 24. سلوفاكيا (Slovakia) - 22 لاعباً
    public static Team CreateSlovakia()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11691, "مارتن دوبرافكا", 1, SpecificPosition.GK, 60, 80, 18, 84, 82, null),
            (11692, "دافيد هانكو", 16, SpecificPosition.LB, 81, 84, 66, 86, 85, new[] { SpecificPosition.CB }),
            (11693, "ميلان شكرينيار", 14, SpecificPosition.CB, 76, 76, 45, 87, 87, null),
            (11694, "دينيس فافرو", 3, SpecificPosition.CB, 76, 75, 55, 84, 88, null),
            (11695, "بيتر بيكاريك", 2, SpecificPosition.RB, 78, 77, 55, 79, 78, null),
            (11696, "ستانيسلاف لوبوتكا", 22, SpecificPosition.CDM, 82, 88, 72, 86, 80, new[] { SpecificPosition.CM }),
            (11697, "أوندري دودا", 8, SpecificPosition.CM, 78, 85, 82, 72, 76, new[] { SpecificPosition.CAM }),
            (11698, "يوراي كوتشكا", 19, SpecificPosition.CM, 74, 81, 82, 82, 87, new[] { SpecificPosition.CDM }),
            (11699, "لوكاس هاراسلين", 17, SpecificPosition.LW, 87, 82, 82, 55, 78, null),
            (11700, "دافيد ستريليتس", 18, SpecificPosition.ST, 83, 76, 82, 48, 80, null),
            (11701, "إيفان شرانز", 26, SpecificPosition.RW, 84, 81, 83, 62, 80, new[] { SpecificPosition.ST, SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (11702, "ماريك روداك", 12, SpecificPosition.GK, 58, 74, 15, 80, 80, null),
            (11703, "نوربرت جيومبر", 6, SpecificPosition.CB, 75, 73, 40, 80, 84, new[] { SpecificPosition.RB }),
            (11704, "آدم أوبرت", 4, SpecificPosition.CB, 78, 74, 40, 80, 82, new[] { SpecificPosition.LB }),
            (11705, "فيرنون دي ماركو", 15, SpecificPosition.LB, 80, 75, 55, 78, 80, null),
            (11706, "باتريك هروشوفسكي", 13, SpecificPosition.CM, 76, 82, 74, 80, 78, new[] { SpecificPosition.CDM }),
            (11707, "ماتوش بيرو", 21, SpecificPosition.CM, 82, 80, 78, 80, 84, null),
            (11708, "توماس ريجو", 5, SpecificPosition.CM, 79, 81, 75, 77, 78, null),
            (11709, "توماس سوسلوف", 7, SpecificPosition.RW, 86, 82, 80, 58, 76, new[] { SpecificPosition.CAM }),
            (11710, "روبرت بوزينيك", 9, SpecificPosition.ST, 82, 73, 81, 48, 84, null),
            (11711, "ليو زاور", 24, SpecificPosition.LW, 86, 78, 76, 42, 72, null),
            (11712, "هنريش رافاس", 23, SpecificPosition.GK, 58, 72, 15, 78, 78, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_SVK", "سلوفاكيا", FormationType.Formation433_Holding, true, starters, bench);
    }
}
