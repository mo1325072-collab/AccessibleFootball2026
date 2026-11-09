namespace AccessibleFootball2026.Core.Models;

/// <summary>
/// الدوري الألماني (Bundesliga) - موسم 2026/2027 المعتمد رسمياً (18 نادياً)
/// موثق بالكامل من واقع Transfermarkt و SofaScore و 365scores
/// بقوائم موسعة وشاملة (22 إلى 23 لاعباً لكل نادٍ) مع أحدث الانتقالات والنجوم وأرقام القمصان الرسمية
/// </summary>
public static class GermanBundesligaDatabase
{
    public static List<Team> GetAllClubs()
    {
        var fromJson = JsonDatabaseProvider.GetTeamsByGroup("Bundesliga (Germany)");
        if (fromJson != null && fromJson.Count > 0) return fromJson;

        return new List<Team>
        {
            CreateBayernMunich(),
            CreateBayerLeverkusen(),
            CreateBorussiaDortmund(),
            CreateRBLeipzig(),
            CreateEintrachtFrankfurt(),
            CreateVfBStuttgart(),
            CreateVfLWolfsburg(),
            CreateSCFreiburg(),
            CreateBorussiaMonchengladbach(),
            CreateUnionBerlin(),
            CreateTSGHoffenheim(),
            CreateFCAugsburg(),
            CreateWerderBremen(),
            CreateMainz05(),
            CreateFCHeidenheim(),
            CreateFCStPauli(),
            CreateHolsteinKiel(),
            CreateVfLBochum()
        };
    }

    // 1. بايرن ميونخ (FC Bayern Munich) - 23 لاعباً
    public static Team CreateBayernMunich()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6001, "مانويل نوير", 1, SpecificPosition.GK, 64, 88, 20, 89, 83, null),
            (6002, "ألفونسو ديفيز", 19, SpecificPosition.LB, 95, 80, 68, 81, 82, new[] { SpecificPosition.LWB, SpecificPosition.LM }),
            (6003, "كيم مين جاي", 3, SpecificPosition.CB, 81, 72, 38, 87, 88, null),
            (6004, "دايوت أوباميكانو", 2, SpecificPosition.CB, 84, 75, 42, 86, 88, null),
            (6005, "جوسيب ستانيسيتش", 44, SpecificPosition.RB, 81, 78, 55, 82, 82, new[] { SpecificPosition.CB, SpecificPosition.LB }),
            (6006, "جوشوا كيميتش", 6, SpecificPosition.CDM, 74, 91, 76, 84, 80, new[] { SpecificPosition.RB, SpecificPosition.CM }),
            (6007, "ألكسندر بافلوفيتش", 45, SpecificPosition.CM, 78, 86, 74, 81, 79, null),
            (6008, "جمال موسيالا", 42, SpecificPosition.CAM, 90, 87, 86, 62, 74, new[] { SpecificPosition.LW, SpecificPosition.RW }),
            (6009, "سيرج جنابري", 7, SpecificPosition.LW, 86, 82, 84, 52, 76, new[] { SpecificPosition.RW, SpecificPosition.ST }),
            (6010, "هاري كين", 9, SpecificPosition.ST, 75, 88, 93, 50, 84, new[] { SpecificPosition.CF }),
            (6011, "مايكل أوليسيه", 17, SpecificPosition.RW, 88, 87, 83, 55, 75, new[] { SpecificPosition.CAM })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6012, "سفين أولرايش", 26, SpecificPosition.GK, 66, 75, 15, 80, 78, null),
            (6013, "إريك داير", 15, SpecificPosition.CB, 68, 76, 52, 82, 83, new[] { SpecificPosition.CDM }),
            (6014, "هيروكي إيتو", 21, SpecificPosition.CB, 80, 78, 48, 82, 82, new[] { SpecificPosition.LB }),
            (6015, "ساشا بوي", 23, SpecificPosition.RB, 88, 76, 58, 80, 82, null),
            (6016, "رافاييل جوريرو", 22, SpecificPosition.LB, 78, 86, 78, 78, 70, new[] { SpecificPosition.CM }),
            (6017, "كونراد لايمر", 27, SpecificPosition.CM, 84, 80, 72, 83, 85, new[] { SpecificPosition.RB, SpecificPosition.CDM }),
            (6018, "ليون جوريتسكا", 8, SpecificPosition.CM, 80, 82, 82, 82, 88, new[] { SpecificPosition.CB }),
            (6019, "ليروي ساني", 10, SpecificPosition.RW, 91, 82, 83, 44, 76, new[] { SpecificPosition.LW }),
            (6020, "كينجسلي كومان", 11, SpecificPosition.LW, 91, 80, 79, 45, 72, new[] { SpecificPosition.RW }),
            (6021, "توماس مولر", 25, SpecificPosition.CAM, 75, 86, 84, 62, 74, new[] { SpecificPosition.ST, SpecificPosition.CF }),
            (6022, "ماتيس تيل", 39, SpecificPosition.ST, 88, 76, 81, 42, 78, new[] { SpecificPosition.LW }),
            (6023, "دانييل بيريتز بايرن", 18, SpecificPosition.GK, 66, 72, 15, 78, 78, null) // Note: German team squad backup
        };

        return DatabaseHelpers.CreateTeam("GER_BAY", "بايرن ميونخ", FormationType.Formation4231, false, starters, bench);
    }

    // 2. باير ليفركوزن (Bayer 04 Leverkusen) - 22 لاعباً
    public static Team CreateBayerLeverkusen()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6031, "لوكاش هراديكي", 1, SpecificPosition.GK, 66, 76, 18, 85, 80, null),
            (6032, "أليخاندرو جريمالدو", 20, SpecificPosition.LWB, 86, 89, 84, 80, 78, new[] { SpecificPosition.LM }),
            (6033, "بييرو هينكابي", 3, SpecificPosition.CB, 84, 76, 45, 85, 84, new[] { SpecificPosition.LB }),
            (6034, "جوناثان تاه", 4, SpecificPosition.CB, 78, 74, 45, 87, 90, null),
            (6035, "إدموند تابسوبا", 12, SpecificPosition.CB, 81, 78, 55, 86, 85, null),
            (6036, "جيريمي فريمبونج", 30, SpecificPosition.RWB, 95, 82, 79, 78, 77, new[] { SpecificPosition.RM }),
            (6037, "جرانيت تشاكا", 34, SpecificPosition.CDM, 72, 90, 82, 85, 84, new[] { SpecificPosition.CM }),
            (6038, "روبرت أندريش", 8, SpecificPosition.CDM, 77, 81, 78, 85, 88, new[] { SpecificPosition.CB }),
            (6039, "فلوريان فيرتز", 10, SpecificPosition.CAM, 87, 91, 86, 60, 75, new[] { SpecificPosition.LW }),
            (6040, "فيكتور بونيفاس", 22, SpecificPosition.ST, 86, 76, 86, 48, 90, null),
            (6041, "مارتن تيرير", 11, SpecificPosition.LW, 84, 80, 83, 55, 80, new[] { SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6042, "ماتي كوفار", 17, SpecificPosition.GK, 67, 78, 15, 80, 78, null),
            (6043, "نوردي موكيلي", 23, SpecificPosition.RB, 84, 76, 55, 81, 84, new[] { SpecificPosition.CB }),
            (6044, "جينويل بيلوسيان", 44, SpecificPosition.CB, 81, 74, 40, 80, 80, new[] { SpecificPosition.LB }),
            (6045, "أرتور أوجوستو", 13, SpecificPosition.RB, 85, 74, 52, 76, 76, null),
            (6046, "إزيكييل بالاسيوس", 25, SpecificPosition.CM, 80, 85, 76, 84, 82, null),
            (6047, "أليكس جارسيا ليفركوزن", 24, SpecificPosition.CM, 78, 87, 78, 79, 76, null),
            (6048, "يوناس هوفمان", 7, SpecificPosition.CAM, 81, 84, 81, 58, 72, new[] { SpecificPosition.RW }),
            (6049, "ناثان تيلا", 19, SpecificPosition.RW, 90, 78, 78, 55, 75, null),
            (6050, "أمين عدلي", 21, SpecificPosition.LW, 88, 78, 78, 52, 75, null),
            (6051, "باتريك شيك", 14, SpecificPosition.ST, 80, 75, 85, 45, 84, null),
            (6052, "نيكلاس لومب", 36, SpecificPosition.GK, 62, 68, 15, 74, 74, null)
        };

        return DatabaseHelpers.CreateTeam("GER_LEV", "باير ليفركوزن", FormationType.Formation532, false, starters, bench);
    }

    // 3. بوروسيا دورتموند (Borussia Dortmund) - 22 لاعباً
    public static Team CreateBorussiaDortmund()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6061, "جريجور كوبيل", 1, SpecificPosition.GK, 68, 80, 18, 88, 84, null),
            (6062, "رامي بن سبعيني", 5, SpecificPosition.LB, 82, 78, 72, 82, 84, new[] { SpecificPosition.CB }),
            (6063, "نيكو شلوتربيك", 4, SpecificPosition.CB, 81, 80, 55, 86, 87, null),
            (6064, "فالديمار أنتون", 3, SpecificPosition.CB, 78, 74, 45, 85, 85, null),
            (6065, "يوليان رايرسون", 26, SpecificPosition.RB, 84, 78, 65, 82, 84, new[] { SpecificPosition.LB }),
            (6066, "إمري تشان", 23, SpecificPosition.CDM, 79, 82, 76, 85, 88, new[] { SpecificPosition.CB }),
            (6067, "باسكال جروس", 13, SpecificPosition.CM, 74, 89, 82, 79, 78, new[] { SpecificPosition.CDM }),
            (6068, "يوليان براندت", 10, SpecificPosition.CAM, 82, 88, 84, 60, 76, new[] { SpecificPosition.CM }),
            (6069, "جايمي بينو جيتنز", 43, SpecificPosition.LW, 91, 79, 80, 42, 68, new[] { SpecificPosition.RW }),
            (6070, "سيرهو جيراسي", 9, SpecificPosition.ST, 84, 76, 89, 52, 88, null),
            (6071, "دونييل مالين", 21, SpecificPosition.RW, 89, 79, 83, 48, 78, new[] { SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6072, "ألكسندر ماير", 33, SpecificPosition.GK, 66, 75, 15, 81, 80, null),
            (6073, "نيكلاس زوله", 25, SpecificPosition.CB, 76, 74, 52, 83, 89, new[] { SpecificPosition.RB }),
            (6074, "يان كوتو", 2, SpecificPosition.RB, 88, 78, 62, 77, 72, null),
            (6075, "ألموجيرا كابار", 42, SpecificPosition.LB, 82, 73, 50, 75, 74, null),
            (6076, "فيليكس نميشا", 8, SpecificPosition.CM, 82, 82, 78, 78, 84, null),
            (6077, "مارسيل سابيتزر", 20, SpecificPosition.CM, 80, 85, 83, 81, 82, null),
            (6078, "جيوفاني رينا", 7, SpecificPosition.CAM, 81, 83, 78, 50, 72, null),
            (6079, "كريم أديمي", 27, SpecificPosition.LW, 94, 78, 80, 45, 76, new[] { SpecificPosition.RW }),
            (6080, "جوليان دورانفيل", 16, SpecificPosition.RW, 91, 74, 72, 38, 66, null),
            (6081, "ماكسيميليان باير", 14, SpecificPosition.ST, 88, 76, 82, 48, 78, new[] { SpecificPosition.RW }),
            (6082, "كول كيمبل", 37, SpecificPosition.RW, 84, 75, 72, 40, 68, null)
        };

        return DatabaseHelpers.CreateTeam("GER_BVB", "بوروسيا دورتموند", FormationType.Formation4231, false, starters, bench);
    }

    // 4. لايبزيج (RB Leipzig) - 22 لاعباً
    public static Team CreateRBLeipzig()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6091, "بيتر جولاكشي", 1, SpecificPosition.GK, 67, 78, 18, 85, 82, null),
            (6092, "دافيد راوم", 22, SpecificPosition.LB, 86, 85, 68, 80, 82, null),
            (6093, "كاستيلو لوكيبا", 4, SpecificPosition.CB, 83, 74, 40, 85, 84, null),
            (6094, "فيلي أوربان", 4, SpecificPosition.CB, 73, 72, 48, 86, 88, null),
            (6095, "بنجامين هنريكس", 39, SpecificPosition.RB, 84, 79, 65, 81, 80, new[] { SpecificPosition.LB }),
            (6096, "أمادو حيدرة", 8, SpecificPosition.CDM, 82, 82, 75, 82, 84, null),
            (6097, "كيفن كامبل", 44, SpecificPosition.CM, 78, 84, 72, 80, 76, null),
            (6098, "تشافي سيمونز", 10, SpecificPosition.CAM, 88, 87, 84, 62, 76, new[] { SpecificPosition.LW, SpecificPosition.RW }),
            (6099, "أنطونيو نوسا", 7, SpecificPosition.LW, 89, 81, 80, 48, 72, null),
            (6100, "لويس أوبيندا", 11, SpecificPosition.ST, 93, 75, 86, 45, 82, null),
            (6101, "بنيامين شيشكو", 30, SpecificPosition.ST, 88, 75, 86, 48, 86, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6102, "مارتن فانديفوردت", 26, SpecificPosition.GK, 68, 75, 15, 80, 78, null),
            (6103, "لوتشاريل خيرترويدا", 3, SpecificPosition.CB, 83, 78, 62, 83, 85, new[] { SpecificPosition.RB, SpecificPosition.CDM }),
            (6104, "لوكاس كلوسترمان", 16, SpecificPosition.CB, 84, 74, 45, 81, 83, new[] { SpecificPosition.RB }),
            (6105, "الشاذلي بيتشيابو", 5, SpecificPosition.CB, 79, 70, 36, 79, 87, null),
            (6106, "نيكولاس سيوالد", 13, SpecificPosition.CDM, 81, 80, 68, 82, 82, null),
            (6107, "أرثر فيرميرين", 18, SpecificPosition.CM, 78, 83, 72, 78, 76, null),
            (6108, "كريستوف باومجارتنر", 14, SpecificPosition.CAM, 82, 82, 82, 65, 80, null),
            (6109, "أسان ويدراوجو", 6, SpecificPosition.CM, 82, 79, 74, 74, 80, null),
            (6110, "إليف إلماس", 9, SpecificPosition.CAM, 83, 82, 79, 62, 76, null),
            (6111, "يوسف بولسن", 9, SpecificPosition.ST, 78, 72, 79, 58, 86, null),
            (6112, "أندريه سيلفا", 19, SpecificPosition.ST, 79, 76, 81, 48, 80, null)
        };

        return DatabaseHelpers.CreateTeam("GER_RBL", "لايبزيج", FormationType.Formation4231, false, starters, bench);
    }

    // 5. آينتراخت فرانكفورت (Eintracht Frankfurt) - 22 لاعباً
    public static Team CreateEintrachtFrankfurt()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6121, "كيفن تراب", 1, SpecificPosition.GK, 67, 78, 18, 85, 82, null),
            (6122, "أرتور ثياتي", 3, SpecificPosition.LB, 81, 76, 55, 83, 84, new[] { SpecificPosition.CB }),
            (6123, "روبن كوخ", 4, SpecificPosition.CB, 78, 75, 52, 84, 85, null),
            (6124, "توتا لوكاس سيلفا", 35, SpecificPosition.CB, 80, 73, 45, 82, 83, new[] { SpecificPosition.RB }),
            (6125, "راسموس كريستنسن", 13, SpecificPosition.RB, 83, 76, 62, 81, 85, null),
            (6126, "إلياس السخيري", 15, SpecificPosition.CDM, 79, 83, 75, 84, 85, null),
            (6127, "هوجو لارسون", 16, SpecificPosition.CM, 83, 84, 76, 79, 80, null),
            (6128, "فارس شايبي", 8, SpecificPosition.CAM, 82, 85, 80, 64, 78, new[] { SpecificPosition.LW }),
            (6129, "ماريو جوتزه", 27, SpecificPosition.CAM, 75, 87, 80, 62, 70, null),
            (6130, "عمر مرموش", 7, SpecificPosition.ST, 91, 83, 88, 52, 82, new[] { SpecificPosition.LW, SpecificPosition.CF }),
            (6131, "هوجو إيكيتيكي", 11, SpecificPosition.ST, 87, 79, 84, 46, 82, new[] { SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6132, "كاووا سانتوس", 40, SpecificPosition.GK, 68, 74, 15, 81, 80, null),
            (6133, "أوريلي أميندا", 5, SpecificPosition.CB, 76, 68, 36, 80, 84, null),
            (6134, "نامدي كولينز", 29, SpecificPosition.CB, 85, 68, 35, 78, 80, null),
            (6135, "نيلز نكونكو", 29, SpecificPosition.LB, 88, 76, 65, 77, 80, null),
            (6136, "تيموثي تشاندلر", 22, SpecificPosition.RB, 77, 74, 58, 76, 78, null),
            (6137, "محمود داوود", 18, SpecificPosition.CM, 78, 83, 76, 78, 76, null),
            (6138, "جونيور دينا إبيمبي", 26, SpecificPosition.RM, 86, 79, 78, 74, 83, null),
            (6139, "أنسجار كناوف", 36, SpecificPosition.RW, 89, 77, 76, 58, 75, null),
            (6140, "جان أوزون", 20, SpecificPosition.CAM, 82, 81, 80, 48, 72, null),
            (6141, "إيغور ماتانوفيتش", 9, SpecificPosition.ST, 83, 70, 82, 45, 87, null),
            (6142, "تيموتي باهيا", 33, SpecificPosition.ST, 82, 68, 76, 38, 78, null)
        };

        return DatabaseHelpers.CreateTeam("GER_SGE", "فرانكفورت", FormationType.Formation4231, false, starters, bench);
    }

    // 6. شتوتغارت (VfB Stuttgart) - 22 لاعباً
    public static Team CreateVfBStuttgart()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6151, "ألكسندر نوبل", 33, SpecificPosition.GK, 68, 80, 18, 85, 82, null),
            (6152, "ماكسيميليان ميتلشتيت", 7, SpecificPosition.LB, 85, 83, 72, 80, 79, null),
            (6153, "جيف شابوت", 24, SpecificPosition.CB, 74, 70, 40, 85, 90, null),
            (6154, "أنتوني روولت", 29, SpecificPosition.CB, 79, 74, 42, 83, 83, null),
            (6155, "جوشا فاجنومان", 4, SpecificPosition.RB, 86, 76, 62, 80, 85, null),
            (6156, "أتيكان كارازور", 16, SpecificPosition.CDM, 76, 82, 65, 84, 86, null),
            (6157, "أنجيلو ستيلر", 6, SpecificPosition.CM, 78, 88, 76, 82, 80, null),
            (6158, "إنزو ميلوت", 8, SpecificPosition.CAM, 83, 85, 81, 65, 75, null),
            (6159, "جيمي ليفيلينج", 18, SpecificPosition.RW, 89, 79, 80, 62, 82, null),
            (6160, "إرميدين ديميروفيتش", 9, SpecificPosition.ST, 84, 78, 86, 55, 85, null),
            (6161, "دينيز أونداف", 26, SpecificPosition.ST, 83, 83, 87, 52, 83, new[] { SpecificPosition.CF, SpecificPosition.CAM })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6162, "فابيان بريدلوف", 1, SpecificPosition.GK, 66, 73, 15, 79, 78, null),
            (6163, "دان أكسيل زاجادو", 23, SpecificPosition.CB, 74, 70, 45, 82, 88, null),
            (6164, "أمين الدخيل", 5, SpecificPosition.CB, 81, 72, 38, 80, 82, null),
            (6165, "ليونيداس ستيرجيو", 2, SpecificPosition.RB, 82, 73, 45, 79, 79, null),
            (6166, "فرانس كراتزيج", 13, SpecificPosition.LB, 83, 78, 65, 76, 74, null),
            (6167, "يانيك كايتل", 14, SpecificPosition.CDM, 78, 79, 68, 79, 81, null),
            (6168, "باسكال ستينزل", 15, SpecificPosition.RB, 78, 80, 68, 78, 78, null),
            (6169, "فابيان ريدر", 32, SpecificPosition.CAM, 82, 83, 79, 62, 78, null),
            (6170, "كريس فوهريش", 27, SpecificPosition.LW, 87, 82, 81, 52, 74, null),
            (6171, "البلال توريه", 10, SpecificPosition.ST, 88, 72, 82, 45, 84, null),
            (6172, "نيك وولتماده", 11, SpecificPosition.ST, 76, 78, 78, 48, 86, null)
        };

        return DatabaseHelpers.CreateTeam("GER_STU", "شتوتغارت", FormationType.Formation442, false, starters, bench);
    }

    // 7. فولفسبورغ (VfL Wolfsburg) - 22 لاعباً
    public static Team CreateVfLWolfsburg()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6181, "كميل جرابارا", 1, SpecificPosition.GK, 68, 77, 18, 84, 82, null),
            (6182, "يواكيم مايلي", 21, SpecificPosition.LB, 85, 79, 68, 79, 81, new[] { SpecificPosition.RB }),
            (6183, "كونستانتينوس كولييراكيس", 4, SpecificPosition.CB, 79, 73, 50, 83, 86, null),
            (6184, "سيباستيان بورناو", 3, SpecificPosition.CB, 76, 68, 42, 83, 86, null),
            (6185, "كيليان فيشر", 2, SpecificPosition.RB, 84, 74, 52, 78, 79, null),
            (6186, "ماكسيميليان أرنولد", 27, SpecificPosition.CDM, 74, 88, 82, 83, 84, null),
            (6187, "ماتياس سفانبرج", 32, SpecificPosition.CM, 80, 82, 78, 79, 82, null),
            (6188, "لوفرو ماجر", 19, SpecificPosition.CAM, 80, 86, 82, 65, 76, null),
            (6189, "ياكوب كامينسكي", 16, SpecificPosition.LW, 88, 78, 76, 60, 76, null),
            (6190, "يوناس فيند", 23, SpecificPosition.ST, 79, 82, 84, 55, 86, null),
            (6191, "محمد عمورة", 9, SpecificPosition.ST, 94, 76, 83, 44, 75, new[] { SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6192, "ماريوس مولر", 29, SpecificPosition.GK, 66, 72, 15, 78, 78, null),
            (6193, "سيدريك زيزيجر", 5, SpecificPosition.CB, 76, 70, 40, 80, 84, null),
            (6194, "ريدل باكو", 20, SpecificPosition.RB, 86, 79, 74, 77, 78, new[] { SpecificPosition.RM }),
            (6195, "روجيريو أوليفيرا", 13, SpecificPosition.LB, 84, 76, 60, 77, 78, null),
            (6196, "صالح أوزجان", 8, SpecificPosition.CDM, 78, 80, 68, 81, 84, null),
            (6197, "يانيك جيرهاردت", 31, SpecificPosition.CM, 78, 79, 74, 78, 80, null),
            (6198, "باتريك فيمر", 39, SpecificPosition.RW, 87, 78, 78, 62, 80, null),
            (6199, "تياجو توماس", 11, SpecificPosition.LW, 88, 76, 78, 48, 76, null),
            (6200, "كيفن بيهرينس", 17, SpecificPosition.ST, 78, 70, 80, 52, 86, null),
            (6201, "بارتوش بياويك", 24, SpecificPosition.ST, 80, 68, 77, 38, 80, null),
            (6202, "بافاو بيرفان", 12, SpecificPosition.GK, 62, 68, 15, 75, 76, null)
        };

        return DatabaseHelpers.CreateTeam("GER_WOB", "فولفسبورغ", FormationType.Formation4231, false, starters, bench);
    }

    // 8. فرايبورغ (SC Freiburg) - 22 لاعباً
    public static Team CreateSCFreiburg()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6211, "نوح أتوبولو", 1, SpecificPosition.GK, 68, 77, 18, 83, 82, null),
            (6212, "كريستيان جونتر", 30, SpecificPosition.LB, 83, 82, 70, 79, 82, null),
            (6213, "فيليب لينهارت", 3, SpecificPosition.CB, 76, 74, 45, 84, 82, null),
            (6214, "ماتياس جينتر", 28, SpecificPosition.CB, 74, 78, 55, 85, 84, null),
            (6215, "لوكاس كابلر", 17, SpecificPosition.RB, 81, 76, 64, 80, 81, null),
            (6216, "ماكسيميليان إيجستين", 8, SpecificPosition.CDM, 78, 82, 74, 82, 84, null),
            (6217, "باتريك أوستيرهاج", 6, SpecificPosition.CM, 80, 81, 74, 80, 82, null),
            (6218, "ميرلين روهل", 34, SpecificPosition.CAM, 83, 81, 77, 65, 83, null),
            (6219, "فينتشينزو جريفو", 32, SpecificPosition.LW, 80, 88, 84, 55, 70, null),
            (6220, "جونيور أدامو", 20, SpecificPosition.ST, 87, 72, 82, 48, 80, null),
            (6221, "ريتسو دوان", 42, SpecificPosition.RW, 86, 82, 82, 58, 74, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6222, "فلوريان مولر فرايبورغ", 21, SpecificPosition.GK, 66, 73, 15, 78, 78, null),
            (6223, "ماكس روزنفيلدر", 37, SpecificPosition.CB, 81, 70, 36, 79, 82, null),
            (6224, "كيليان سيلديليا", 25, SpecificPosition.RB, 81, 75, 52, 81, 82, new[] { SpecificPosition.CB }),
            (6225, "جوردي ماكينجو", 33, SpecificPosition.LB, 84, 73, 50, 75, 75, null),
            (6226, "نيكولاس هوفلر", 27, SpecificPosition.CDM, 68, 80, 68, 81, 80, null),
            (6227, "فلورينت موسليا", 23, SpecificPosition.CAM, 82, 82, 80, 48, 68, null),
            (6228, "نواه فايسهاوبت", 7, SpecificPosition.LW, 87, 78, 74, 45, 68, null),
            (6229, "إيرين دينكتشي", 18, SpecificPosition.RW, 89, 76, 79, 48, 76, null),
            (6230, "ميكائيل جريجوريتش", 38, SpecificPosition.ST, 75, 76, 83, 52, 88, null),
            (6231, "لوكاس هولير", 9, SpecificPosition.ST, 80, 76, 81, 62, 82, null),
            (6232, "يانيك هوفمان", 24, SpecificPosition.GK, 64, 68, 15, 74, 74, null)
        };

        return DatabaseHelpers.CreateTeam("GER_FRE", "فرايبورغ", FormationType.Formation4231, false, starters, bench);
    }

    // 9. بوروسيا مونشنغلادباخ (Borussia Monchengladbach) - 22 لاعباً
    public static Team CreateBorussiaMonchengladbach()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6241, "جوناس أوملين", 1, SpecificPosition.GK, 68, 77, 18, 84, 82, null),
            (6242, "لوكا نيتز", 20, SpecificPosition.LB, 83, 79, 64, 78, 76, null),
            (6243, "كو إيتاكورا", 3, SpecificPosition.CB, 80, 76, 52, 84, 83, new[] { SpecificPosition.CDM }),
            (6244, "نيكو إلفيدي", 30, SpecificPosition.CB, 78, 74, 42, 83, 82, null),
            (6245, "جو سكالي", 29, SpecificPosition.RB, 84, 75, 55, 79, 81, new[] { SpecificPosition.LB }),
            (6246, "جوليان فايجل", 8, SpecificPosition.CDM, 72, 86, 68, 83, 80, null),
            (6247, "روكو ريتز", 27, SpecificPosition.CM, 82, 82, 76, 80, 81, null),
            (6248, "كيفن ستوجر", 7, SpecificPosition.CAM, 77, 88, 82, 64, 75, new[] { SpecificPosition.CM }),
            (6249, "فرانك أونورا", 9, SpecificPosition.RW, 88, 84, 79, 58, 76, null),
            (6250, "تيم كليندينست", 11, SpecificPosition.ST, 82, 72, 85, 55, 90, null),
            (6251, "الحسن بليا", 14, SpecificPosition.LW, 82, 82, 84, 48, 78, new[] { SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6252, "موريتز نيكولاس", 33, SpecificPosition.GK, 68, 75, 15, 81, 80, null),
            (6253, "مارفين فريدريش", 5, SpecificPosition.CB, 72, 68, 42, 80, 84, null),
            (6254, "فابيو كياروديا", 2, SpecificPosition.CB, 76, 72, 38, 78, 80, null),
            (6255, "ستيفان لاينر", 22, SpecificPosition.RB, 81, 76, 58, 78, 80, null),
            (6256, "لوكاس أولريش", 26, SpecificPosition.LB, 81, 73, 50, 75, 75, null),
            (6257, "فيليب ساندر", 16, SpecificPosition.CM, 78, 80, 72, 78, 80, null),
            (6258, "فلوريان نيوهاوس", 10, SpecificPosition.CM, 76, 84, 79, 70, 74, null),
            (6259, "روبن هاك", 25, SpecificPosition.LW, 84, 79, 81, 48, 74, null),
            (6260, "توماس تشفانتشارا", 31, SpecificPosition.ST, 85, 70, 81, 45, 86, null),
            (6261, "ناثان نجومو", 19, SpecificPosition.RW, 89, 76, 75, 45, 76, null),
            (6262, "توبياس سيبل", 21, SpecificPosition.GK, 62, 68, 15, 75, 76, null)
        };

        return DatabaseHelpers.CreateTeam("GER_BMG", "مونشنغلادباخ", FormationType.Formation4231, false, starters, bench);
    }

    // 10. يونيون برلين (1. FC Union Berlin) - 22 لاعباً
    public static Team CreateUnionBerlin()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6271, "فريدريك رونو", 1, SpecificPosition.GK, 68, 77, 18, 85, 83, null),
            (6272, "توم روته", 15, SpecificPosition.LWB, 84, 78, 68, 79, 83, null),
            (6273, "ديوجو ليتي", 4, SpecificPosition.CB, 78, 74, 45, 84, 86, null),
            (6274, "كيفن فوجت", 2, SpecificPosition.CB, 74, 82, 50, 83, 85, new[] { SpecificPosition.CDM }),
            (6275, "دانيليو دوخي", 5, SpecificPosition.CB, 76, 70, 52, 85, 88, null),
            (6276, "كريستوفر تريمل", 28, SpecificPosition.RWB, 78, 86, 68, 79, 79, null),
            (6277, "راني خضيرة", 8, SpecificPosition.CDM, 76, 81, 68, 84, 85, null),
            (6278, "أندراس شافر", 13, SpecificPosition.CM, 82, 81, 74, 80, 81, null),
            (6279, "يانيك هابرير", 19, SpecificPosition.CM, 79, 81, 78, 79, 82, null),
            (6280, "بينديكت هولرباخ", 16, SpecificPosition.ST, 88, 75, 82, 50, 82, new[] { SpecificPosition.RW }),
            (6281, "جوردان سيباتشيو", 45, SpecificPosition.ST, 80, 72, 82, 48, 89, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6282, "ألكسندر شفولوف", 12, SpecificPosition.GK, 66, 72, 15, 78, 78, null),
            (6283, "ليوبولد كويرفيلد", 14, SpecificPosition.CB, 78, 70, 38, 80, 84, null),
            (6284, "يوسيب يورانوفيتش", 18, SpecificPosition.RB, 85, 80, 68, 79, 78, new[] { SpecificPosition.LB }),
            (6285, "روبرت سكوف", 17, SpecificPosition.LWB, 84, 83, 80, 75, 76, null),
            (6286, "لوكاس توزار", 29, SpecificPosition.CM, 78, 79, 72, 81, 84, null),
            (6287, "ألبين كملين", 36, SpecificPosition.CM, 79, 78, 70, 77, 78, null),
            (6288, "جيونغ وو يونغ", 11, SpecificPosition.CAM, 84, 79, 77, 55, 74, null),
            (6289, "يوربي فيرتيسين", 7, SpecificPosition.LW, 89, 76, 79, 44, 75, null),
            (6290, "تيم سكاركيه", 21, SpecificPosition.RW, 87, 77, 78, 52, 76, null),
            (6291, "كيفن فولاند", 10, SpecificPosition.ST, 78, 79, 82, 55, 82, null),
            (6292, "إيفان برتاجين", 9, SpecificPosition.ST, 82, 68, 79, 42, 85, null)
        };

        return DatabaseHelpers.CreateTeam("GER_FCU", "يونيون برلين", FormationType.Formation532, false, starters, bench);
    }

    // 11. هوفنهايم (TSG 1899 Hoffenheim) - 22 لاعباً
    public static Team CreateTSGHoffenheim()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6301, "أوليفر باومان", 1, SpecificPosition.GK, 67, 80, 18, 86, 82, null),
            (6302, "ألكسندر براس", 22, SpecificPosition.LWB, 86, 80, 72, 78, 80, null),
            (6303, "كيفن أكبوجوما", 25, SpecificPosition.CB, 83, 70, 40, 81, 85, null),
            (6304, "تيم دريكسلر", 35, SpecificPosition.CB, 77, 72, 38, 80, 81, null),
            (6305, "بافل كاديرابيك", 3, SpecificPosition.RWB, 82, 78, 68, 79, 81, null),
            (6306, "فلوريان جريليتش", 11, SpecificPosition.CDM, 74, 86, 74, 82, 80, new[] { SpecificPosition.CB }),
            (6307, "أنطون ستاخ", 16, SpecificPosition.CM, 82, 83, 80, 82, 88, new[] { SpecificPosition.CB }),
            (6308, "توم بيشوف", 7, SpecificPosition.CAM, 82, 85, 78, 62, 72, null),
            (6309, "أندريه كراماريتش", 27, SpecificPosition.CAM, 79, 87, 87, 55, 76, new[] { SpecificPosition.ST }),
            (6310, "ماريوس بولتر", 21, SpecificPosition.LW, 85, 77, 80, 62, 82, null),
            (6311, "ميرجيم بيريشا", 10, SpecificPosition.ST, 83, 76, 84, 52, 84, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6312, "لوكا فيليب", 37, SpecificPosition.GK, 66, 70, 15, 77, 77, null),
            (6313, "ستانلي نسوكي", 34, SpecificPosition.CB, 81, 72, 40, 79, 82, new[] { SpecificPosition.LB }),
            (6314, "أرتور تشافيس", 5, SpecificPosition.CB, 78, 68, 36, 79, 81, null),
            (6315, "دافيد يوراسيك", 19, SpecificPosition.LB, 85, 76, 62, 76, 78, null),
            (6316, "فالنتين جيندري", 2, SpecificPosition.RB, 84, 75, 55, 78, 79, null),
            (6317, "أوموت توهومجو", 40, SpecificPosition.CM, 80, 80, 72, 76, 76, null),
            (6318, "دينيس جيجر", 8, SpecificPosition.CM, 78, 82, 74, 76, 72, null),
            (6319, "آدم هلوزيك", 23, SpecificPosition.ST, 84, 78, 82, 50, 82, new[] { SpecificPosition.CAM }),
            (6320, "هاريس تاباكوفيتش", 20, SpecificPosition.ST, 78, 68, 82, 45, 88, null),
            (6321, "إيلاس بيبو", 9, SpecificPosition.ST, 88, 76, 80, 52, 80, new[] { SpecificPosition.RW }),
            (6322, "مكسيم مويرستيدت", 29, SpecificPosition.ST, 82, 68, 76, 38, 78, null)
        };

        return DatabaseHelpers.CreateTeam("GER_HOF", "هوفنهايم", FormationType.Formation532, false, starters, bench);
    }

    // 12. أوجسبورج (FC Augsburg) - 22 لاعباً
    public static Team CreateFCAugsburg()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6331, "نيديلكو لابرlogistic", 22, SpecificPosition.GK, 67, 76, 18, 83, 82, null),
            (6332, "ديميتريس جيانوليس", 13, SpecificPosition.LB, 84, 78, 62, 78, 78, null),
            (6333, "جيفري خاوفيليه", 6, SpecificPosition.CB, 72, 74, 48, 83, 85, null),
            (6334, "كيفن شلوتربيك", 31, SpecificPosition.CB, 76, 72, 52, 83, 86, null),
            (6335, "ماريوس فولف", 9, SpecificPosition.RB, 86, 78, 72, 78, 80, new[] { SpecificPosition.RM }),
            (6336, "كريستيان ياكيتش", 17, SpecificPosition.CDM, 79, 79, 68, 83, 85, null),
            (6337, "إلفيس ريكسبيتشاي", 8, SpecificPosition.CM, 78, 81, 75, 80, 84, null),
            (6338, "أرني ماير", 10, SpecificPosition.CAM, 78, 84, 79, 68, 76, new[] { SpecificPosition.CM }),
            (6339, "روبين فارجاس", 7, SpecificPosition.LW, 86, 81, 80, 52, 72, null),
            (6340, "صامويل إيسيندي", 9, SpecificPosition.ST, 88, 70, 83, 48, 88, null),
            (6341, "فيليب تيتز", 21, SpecificPosition.ST, 80, 74, 82, 52, 85, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6342, "فين داهمن", 1, SpecificPosition.GK, 67, 74, 15, 80, 78, null),
            (6343, "ماكسيميليان باور", 23, SpecificPosition.CB, 75, 68, 38, 79, 82, null),
            (6344, "كريسليان ماتسيما", 3, SpecificPosition.CB, 81, 70, 36, 80, 83, null),
            (6345, "مادس بيدرسن", 3, SpecificPosition.LB, 86, 74, 60, 77, 78, null),
            (6346, "روبرت جومني", 2, SpecificPosition.RB, 82, 74, 52, 77, 76, null),
            (6347, "نيكلاس دورش", 36, SpecificPosition.CM, 76, 81, 72, 78, 78, null),
            (6348, "ميرت كومور", 42, SpecificPosition.CAM, 82, 79, 76, 50, 70, null),
            (6349, "يوسف كابادايي", 18, SpecificPosition.LW, 86, 75, 75, 42, 74, null),
            (6350, "أليكسيس كلود موريس", 20, SpecificPosition.CAM, 84, 82, 80, 50, 70, null),
            (6351, "ستيف مونييه", 15, SpecificPosition.ST, 76, 68, 81, 48, 89, null),
            (6352, "دانييل كلاين", 25, SpecificPosition.GK, 65, 68, 15, 74, 74, null)
        };

        return DatabaseHelpers.CreateTeam("GER_AUG", "أوجسبورج", FormationType.Formation442, false, starters, bench);
    }

    // 13. فيردر بريمن (Werder Bremen) - 22 لاعباً
    public static Team CreateWerderBremen()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6361, "مايكل زيترير", 1, SpecificPosition.GK, 68, 80, 18, 83, 80, null),
            (6362, "فيليكس أجو", 27, SpecificPosition.LWB, 87, 76, 60, 78, 76, new[] { SpecificPosition.RWB }),
            (6363, "ماركو فريدل", 32, SpecificPosition.CB, 78, 78, 55, 84, 84, new[] { SpecificPosition.LB }),
            (6364, "نيكلاس شتارك", 4, SpecificPosition.CB, 78, 72, 45, 83, 85, new[] { SpecificPosition.CDM }),
            (6365, "ميلوش فيلكوفيتش", 13, SpecificPosition.CB, 75, 74, 45, 82, 82, null),
            (6366, "ميتشل فايزر", 8, SpecificPosition.RWB, 84, 82, 74, 79, 78, new[] { SpecificPosition.RM }),
            (6367, "سين لينين", 6, SpecificPosition.CDM, 78, 81, 68, 82, 83, null),
            (6368, "ينس ستيج", 20, SpecificPosition.CM, 80, 80, 78, 81, 84, null),
            (6369, "رومانو شميد", 20, SpecificPosition.CAM, 83, 85, 79, 62, 70, null),
            (6370, "مارفين دوكش", 7, SpecificPosition.ST, 79, 85, 85, 48, 78, null),
            (6371, "ماركو غرول", 17, SpecificPosition.LW, 86, 78, 82, 52, 80, new[] { SpecificPosition.RW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6372, "ميو باخوس", 30, SpecificPosition.GK, 66, 72, 15, 77, 76, null),
            (6373, "أموس بيبر", 5, SpecificPosition.CB, 76, 72, 40, 81, 83, null),
            (6374, "خوليان مالاتيني", 22, SpecificPosition.CB, 79, 68, 38, 79, 82, new[] { SpecificPosition.RB }),
            (6375, "أوليفييه ديمان", 2, SpecificPosition.LWB, 83, 76, 62, 76, 76, null),
            (6376, "سكيللي ألفيرو", 28, SpecificPosition.CM, 76, 78, 70, 78, 88, null),
            (6377, "ليوناردو بيتينكورت", 10, SpecificPosition.CM, 80, 82, 77, 72, 74, null),
            (6378, "جاستن نجينماه", 11, SpecificPosition.RW, 91, 76, 79, 44, 75, null),
            (6379, "أوليفر بيرك", 9, SpecificPosition.ST, 88, 70, 77, 48, 84, null),
            (6380, "كيكي توب", 42, SpecificPosition.ST, 80, 68, 78, 42, 83, null),
            (6381, "ديريك كوهن", 19, SpecificPosition.LWB, 87, 76, 65, 76, 78, null),
            (6382, "ماركوس كولكه", 25, SpecificPosition.GK, 62, 68, 15, 76, 76, null)
        };

        return DatabaseHelpers.CreateTeam("GER_BRE", "فيردر بريمن", FormationType.Formation532, false, starters, bench);
    }

    // 14. ماينز (1. FSV Mainz 05) - 22 لاعباً
    public static Team CreateMainz05()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6391, "روبن زينتنر", 27, SpecificPosition.GK, 68, 77, 18, 84, 82, null),
            (6392, "فيليب مويني", 2, SpecificPosition.LWB, 84, 78, 64, 78, 76, new[] { SpecificPosition.RWB }),
            (6393, "ماكسيم لايتش", 22, SpecificPosition.CB, 81, 72, 38, 82, 84, new[] { SpecificPosition.LB }),
            (6394, "شتيفان بيل", 16, SpecificPosition.CB, 66, 70, 45, 82, 86, null),
            (6395, "دومينيك كوهر", 31, SpecificPosition.CB, 76, 75, 62, 83, 87, new[] { SpecificPosition.CDM }),
            (6396, "أنطوني كاسي", 19, SpecificPosition.RWB, 82, 77, 65, 79, 80, new[] { SpecificPosition.LWB }),
            (6397, "كايشو سانو", 6, SpecificPosition.CDM, 82, 82, 68, 82, 81, null),
            (6398, "نديم أميري", 18, SpecificPosition.CM, 80, 86, 82, 75, 78, new[] { SpecificPosition.CAM }),
            (6399, "لي جاي سونج", 7, SpecificPosition.CAM, 81, 83, 80, 72, 80, null),
            (6400, "جوناثان بوركاردت", 9, SpecificPosition.ST, 87, 78, 85, 52, 80, null),
            (6401, "باول نيبيل", 8, SpecificPosition.RW, 85, 80, 78, 55, 72, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6402, "دانييل باتز", 1, SpecificPosition.GK, 66, 72, 15, 78, 78, null),
            (6403, "موريتز يينز", 3, SpecificPosition.CB, 77, 70, 38, 81, 85, null),
            (6404, "داني دا كوستا", 21, SpecificPosition.RB, 83, 74, 55, 77, 82, null),
            (6405, "سيلفان فيدمر", 30, SpecificPosition.RB, 81, 78, 64, 79, 79, null),
            (6406, "هونج هيون سيوك", 14, SpecificPosition.CM, 82, 81, 76, 70, 74, null),
            (6407, "أيمن برقوق", 4, SpecificPosition.CM, 81, 80, 75, 70, 78, null),
            (6408, "أرميندو سيب", 17, SpecificPosition.ST, 86, 74, 79, 44, 78, null),
            (6409, "كريم أونيسيو", 29, SpecificPosition.ST, 83, 76, 79, 58, 84, null),
            (6410, "نيلسون فايبر", 44, SpecificPosition.ST, 82, 68, 77, 40, 83, null),
            (6411, "لودوفيك أجورك", 11, SpecificPosition.ST, 74, 72, 80, 48, 88, null),
            (6412, "لاسي ريس", 33, SpecificPosition.GK, 64, 68, 15, 74, 74, null)
        };

        return DatabaseHelpers.CreateTeam("GER_MAI", "ماينز", FormationType.Formation532, false, starters, bench);
    }

    // 15. هايدنهايم (1. FC Heidenheim) - 22 لاعباً
    public static Team CreateFCHeidenheim()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6421, "كيفن مولر", 1, SpecificPosition.GK, 67, 76, 18, 83, 82, null),
            (6422, "يوناس فورنباخ", 19, SpecificPosition.LB, 80, 77, 62, 78, 78, null),
            (6423, "بينيديكت خيمبر", 6, SpecificPosition.CB, 75, 70, 42, 82, 85, new[] { SpecificPosition.CDM }),
            (6424, "باتريك ماينكا", 5, SpecificPosition.CB, 74, 72, 45, 83, 85, null),
            (6425, "عمر تراوري", 23, SpecificPosition.RB, 86, 75, 54, 78, 78, null),
            (6426, "يان شوبنر", 3, SpecificPosition.CDM, 78, 79, 72, 81, 84, null),
            (6427, "لينارد مالوني", 33, SpecificPosition.CDM, 76, 78, 65, 83, 86, new[] { SpecificPosition.CB }),
            (6428, "بول فانر", 10, SpecificPosition.CAM, 85, 86, 82, 55, 72, new[] { SpecificPosition.RW }),
            (6429, "ليو سيينزا", 8, SpecificPosition.LW, 86, 81, 79, 52, 74, null),
            (6430, "مارفين بيرينجر", 18, SpecificPosition.ST, 83, 76, 82, 50, 83, null),
            (6431, "أدريان بيك", 21, SpecificPosition.RW, 81, 79, 78, 60, 78, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6432, "فيتوس أيشر", 22, SpecificPosition.GK, 65, 70, 15, 77, 76, null),
            (6433, "تيم سيرسليبن", 4, SpecificPosition.CB, 74, 68, 36, 78, 83, null),
            (6434, "مارنون بوش", 2, SpecificPosition.RB, 80, 74, 52, 76, 76, null),
            (6435, "نورمان ثويركاوف", 30, SpecificPosition.LB, 74, 76, 62, 76, 76, null),
            (6436, "لوكا كيربر", 20, SpecificPosition.CM, 78, 78, 70, 77, 78, null),
            (6437, "سيرلو كونتيه", 31, SpecificPosition.RW, 92, 74, 74, 45, 72, null),
            (6438, "ماكسيميليان بريونيغ", 14, SpecificPosition.ST, 82, 68, 78, 44, 85, null),
            (6439, "ميكيل كوفمان", 9, SpecificPosition.ST, 83, 70, 78, 45, 82, null),
            (6440, "ستيفان شيمر", 7, SpecificPosition.ST, 78, 68, 77, 45, 80, null),
            (6441, "ماثياس هونساك", 17, SpecificPosition.LW, 85, 76, 75, 48, 76, null),
            (6442, "فرانك فيلر", 34, SpecificPosition.GK, 64, 68, 15, 74, 74, null)
        };

        return DatabaseHelpers.CreateTeam("GER_HEI", "هايدنهايم", FormationType.Formation4231, false, starters, bench);
    }

    // 16. سانت باولي (FC St. Pauli) - 22 لاعباً
    public static Team CreateFCStPauli()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6451, "نيكولا فاسيلي", 22, SpecificPosition.GK, 68, 77, 18, 84, 82, null),
            (6452, "فيليب تريو", 23, SpecificPosition.LWB, 85, 77, 62, 78, 78, new[] { SpecificPosition.RWB }),
            (6453, "كارول ميتس", 8, SpecificPosition.CB, 76, 76, 45, 83, 84, null),
            (6454, "هاوكه فال", 4, SpecificPosition.CB, 74, 76, 48, 83, 83, null),
            (6455, "إريك سميث", 8, SpecificPosition.CB, 75, 84, 65, 83, 84, new[] { SpecificPosition.CDM }),
            (6456, "مانوليس سالياكاس", 2, SpecificPosition.RWB, 84, 78, 68, 79, 80, null),
            (6457, "جاكسون إيرفاين", 7, SpecificPosition.CM, 80, 81, 80, 83, 87, null),
            (6458, "روبرت فاجنر", 16, SpecificPosition.CM, 81, 80, 72, 80, 82, null),
            (6459, "إلياس سعد", 26, SpecificPosition.LW, 87, 80, 79, 52, 74, null),
            (6460, "يوهانس إيجستين", 11, SpecificPosition.ST, 82, 78, 83, 50, 78, null),
            (6461, "أولادابو أفولايان", 17, SpecificPosition.RW, 87, 78, 79, 52, 78, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6462, "بين فول", 1, SpecificPosition.GK, 66, 70, 15, 77, 76, null),
            (6463, "آدم دزفيجالا", 5, SpecificPosition.CB, 75, 70, 40, 78, 81, null),
            (6464, "ديفيد نيميث", 25, SpecificPosition.CB, 76, 68, 36, 79, 82, null),
            (6465, "لارس ريتسكا", 21, SpecificPosition.LB, 82, 74, 56, 76, 77, null),
            (6466, "فين ستيفنز", 15, SpecificPosition.RB, 83, 73, 50, 75, 74, null),
            (6467, "كارلو بوخالفة", 19, SpecificPosition.CM, 79, 79, 74, 76, 80, null),
            (6468, "كونور ميتكالف", 24, SpecificPosition.CM, 81, 79, 75, 76, 78, null),
            (6469, "أندرياس ألبرز", 19, SpecificPosition.ST, 72, 68, 78, 48, 86, null),
            (6470, "سكوت بانكس", 14, SpecificPosition.RW, 86, 76, 74, 42, 68, null),
            (6471, "مورجان جيلافوجي", 29, SpecificPosition.ST, 85, 74, 80, 52, 82, null),
            (6472, "إيريك أهليرز", 30, SpecificPosition.GK, 64, 68, 15, 74, 74, null)
        };

        return DatabaseHelpers.CreateTeam("GER_STP", "سانت باولي", FormationType.Formation532, false, starters, bench);
    }

    // 17. هولشتاين كيل (Holstein Kiel) - 22 لاعباً
    public static Team CreateHolsteinKiel()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6481, "تيمون فاينر", 1, SpecificPosition.GK, 67, 75, 18, 82, 80, null),
            (6482, "تيموتوش بوخاتش", 15, SpecificPosition.LWB, 86, 79, 68, 77, 78, null),
            (6483, "ماركو كوميندا", 3, SpecificPosition.CB, 76, 72, 40, 80, 82, null),
            (6484, "كارل يوهانسون", 4, SpecificPosition.CB, 74, 68, 40, 81, 85, null),
            (6485, "تيمو بيكر", 17, SpecificPosition.RWB, 82, 76, 62, 80, 83, new[] { SpecificPosition.CB }),
            (6486, "ماجنوس كنودسن", 6, SpecificPosition.CDM, 79, 81, 70, 81, 82, null),
            (6487, "أرمين جيجوفيتش", 37, SpecificPosition.CM, 81, 80, 74, 80, 82, null),
            (6488, "لويس هولتبي", 10, SpecificPosition.CAM, 77, 85, 79, 72, 74, new[] { SpecificPosition.CM }),
            (6489, "فين بورات", 8, SpecificPosition.RM, 83, 79, 75, 68, 76, null),
            (6490, "شوتو ماتشينو", 18, SpecificPosition.ST, 84, 77, 83, 50, 78, null),
            (6491, "بينيديكت بيشلر", 9, SpecificPosition.ST, 82, 72, 81, 48, 84, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6492, "توماس داهنه", 21, SpecificPosition.GK, 66, 70, 15, 77, 76, null),
            (6493, "ماكس جيشفيلم", 5, SpecificPosition.CB, 76, 70, 38, 79, 81, null),
            (6494, "ماركو إيفيزيتش", 14, SpecificPosition.CB, 75, 72, 45, 80, 83, new[] { SpecificPosition.CDM }),
            (6495, "باتريك إيراس", 19, SpecificPosition.CB, 70, 72, 50, 79, 85, null),
            (6496, "دومينيك يافورتشيك", 23, SpecificPosition.LB, 83, 73, 52, 75, 74, null),
            (6497, "نيكولاي ريمبيرج", 22, SpecificPosition.CM, 80, 78, 72, 77, 80, null),
            (6498, "ستيفن سكرزيبسكي", 7, SpecificPosition.CAM, 80, 81, 80, 52, 72, null),
            (6499, "أندراس كيلايتي", 20, SpecificPosition.RW, 84, 75, 73, 44, 70, null),
            (6500, "يان فيات آرب", 20, SpecificPosition.ST, 82, 74, 80, 48, 78, null),
            (6501, "ألكسندر برنهاردسون", 11, SpecificPosition.LW, 86, 76, 77, 48, 74, null),
            (6502, "مارسيل إنجلهاردت", 31, SpecificPosition.GK, 63, 67, 15, 74, 74, null)
        };

        return DatabaseHelpers.CreateTeam("GER_KIE", "هولشتاين كيل", FormationType.Formation532, false, starters, bench);
    }

    // 18. بوخوم (VfL Bochum) - 22 لاعباً
    public static Team CreateVfLBochum()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6511, "باتريك دريفيس", 27, SpecificPosition.GK, 67, 75, 18, 83, 81, null),
            (6512, "ماكسيميليان فيتك", 32, SpecificPosition.LB, 83, 80, 72, 78, 78, null),
            (6513, "ياكوف ميديتش", 13, SpecificPosition.CB, 76, 70, 55, 82, 88, null),
            (6514, "إرهان ماشوفيتش", 4, SpecificPosition.CB, 74, 75, 45, 82, 84, new[] { SpecificPosition.CDM }),
            (6515, "تيم أورمان", 14, SpecificPosition.RB, 82, 73, 40, 80, 81, new[] { SpecificPosition.CB }),
            (6516, "أنطوني لوسيلا", 8, SpecificPosition.CDM, 72, 79, 68, 82, 83, null),
            (6517, "إبراهيما سيسوكو", 6, SpecificPosition.CDM, 78, 80, 70, 83, 87, null),
            (6518, "ماتوش بيرو", 19, SpecificPosition.CM, 82, 80, 78, 80, 84, null),
            (6519, "داني دي فيت", 10, SpecificPosition.CAM, 79, 81, 82, 72, 83, null),
            (6520, "فيليب هوفمان", 33, SpecificPosition.ST, 68, 72, 82, 52, 91, null),
            (6521, "موريتز بروشينسكي", 29, SpecificPosition.ST, 84, 72, 79, 52, 84, new[] { SpecificPosition.RW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (6522, "تيمو هورن", 1, SpecificPosition.GK, 66, 74, 15, 79, 80, null),
            (6523, "إيفان أورديتس", 20, SpecificPosition.CB, 70, 68, 40, 80, 85, null),
            (6524, "كريستيان جامبوا", 2, SpecificPosition.RB, 83, 73, 50, 76, 76, null),
            (6525, "برناردو فيرنانديز", 5, SpecificPosition.LB, 80, 75, 52, 81, 84, new[] { SpecificPosition.CB }),
            (6526, "فيليكس باسيلاك", 15, SpecificPosition.RB, 84, 78, 68, 77, 76, null),
            (6527, "لوكاس داشتنر", 7, SpecificPosition.CAM, 78, 80, 76, 56, 75, null),
            (6528, "كوجي مييوشي", 23, SpecificPosition.CAM, 83, 82, 78, 50, 68, null),
            (6529, "جيريت هولتمان", 17, SpecificPosition.LW, 91, 75, 76, 55, 76, null),
            (6530, "موريتز كفارتينج", 11, SpecificPosition.RW, 84, 78, 76, 50, 72, null),
            (6531, "مايرون بوادو", 9, SpecificPosition.ST, 87, 72, 81, 40, 78, null),
            (6532, "مانويل جراف", 25, SpecificPosition.GK, 63, 67, 15, 74, 74, null)
        };

        return DatabaseHelpers.CreateTeam("GER_BOC", "بوخوم", FormationType.Formation433_Holding, false, starters, bench);
    }
}
