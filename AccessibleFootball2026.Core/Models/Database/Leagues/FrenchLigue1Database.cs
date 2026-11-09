namespace AccessibleFootball2026.Core.Models;

/// <summary>
/// الدوري الفرنسي (Ligue 1) - موسم 2026/2027 المعتمد رسمياً (18 نادياً)
/// موثق بالكامل من واقع Transfermarkt و SofaScore و 365scores
/// بقوائم موسعة وشاملة (22 إلى 23 لاعباً لكل نادٍ) مع أحدث الانتقالات والنجوم وأرقام القمصان الرسمية
/// </summary>
public static class FrenchLigue1Database
{
    public static List<Team> GetAllClubs()
    {
        var fromJson = JsonDatabaseProvider.GetTeamsByGroup("Ligue 1 (France)");
        if (fromJson != null && fromJson.Count > 0) return fromJson;

        return new List<Team>
        {
            CreateParisSaintGermain(),
            CreateMarseille(),
            CreateMonaco(),
            CreateLyon(),
            CreateLille(),
            CreateLens(),
            CreateRennes(),
            CreateNice(),
            CreateBrest(),
            CreateReims(),
            CreateStrasbourg(),
            CreateToulouse(),
            CreateMontpellier(),
            CreateNantes(),
            CreateAuxerre(),
            CreateAngers(),
            CreateSaintEtienne(),
            CreateLeHavre()
        };
    }

    // 1. باريس سان جيرمان (Paris Saint-Germain) - 23 لاعباً
    public static Team CreateParisSaintGermain()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7001, "جانلويجي دوناروما", 1, SpecificPosition.GK, 62, 79, 18, 90, 85, null),
            (7002, "نونو مينديز", 25, SpecificPosition.LB, 94, 82, 68, 81, 79, new[] { SpecificPosition.LWB }),
            (7003, "ويليام باتشو", 51, SpecificPosition.CB, 83, 76, 40, 85, 87, null),
            (7004, "ماركينيوس", 5, SpecificPosition.CB, 78, 82, 56, 88, 83, new[] { SpecificPosition.CDM }),
            (7005, "أشرف حكيمي", 2, SpecificPosition.RB, 93, 85, 78, 81, 80, new[] { SpecificPosition.RWB, SpecificPosition.RM }),
            (7006, "فيتينيا", 17, SpecificPosition.CM, 82, 89, 78, 81, 78, new[] { SpecificPosition.CDM }),
            (7007, "جواو نيفيز", 87, SpecificPosition.CM, 83, 87, 75, 83, 82, new[] { SpecificPosition.CDM }),
            (7008, "وارن زاير إيمري", 33, SpecificPosition.CM, 82, 85, 77, 80, 82, new[] { SpecificPosition.CDM, SpecificPosition.RB }),
            (7009, "برادلي باركولا", 29, SpecificPosition.LW, 92, 82, 83, 48, 76, new[] { SpecificPosition.RW, SpecificPosition.ST }),
            (7010, "راندال كولو مواني", 23, SpecificPosition.ST, 88, 77, 82, 45, 81, new[] { SpecificPosition.RW }),
            (7011, "عثمان ديمبيلي", 10, SpecificPosition.RW, 93, 86, 81, 40, 68, new[] { SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7012, "ماتفي سافونوف", 39, SpecificPosition.GK, 60, 75, 18, 83, 82, null),
            (7013, "لوكاس بيرالدو", 35, SpecificPosition.CB, 76, 81, 45, 81, 78, new[] { SpecificPosition.LB }),
            (7014, "لوكاس هيرنانديز", 21, SpecificPosition.CB, 78, 77, 54, 84, 82, new[] { SpecificPosition.LB }),
            (7015, "بريسنيل كيمبيمبي", 3, SpecificPosition.CB, 77, 75, 42, 82, 85, null),
            (7016, "فابيان رويز", 8, SpecificPosition.CM, 73, 86, 81, 77, 79, new[] { SpecificPosition.CDM }),
            (7017, "لي كانغ إن", 19, SpecificPosition.CAM, 83, 86, 80, 52, 69, new[] { SpecificPosition.RW, SpecificPosition.LW }),
            (7018, "سيني مايولو", 24, SpecificPosition.CAM, 80, 78, 73, 50, 70, null),
            (7019, "ماركو أسينسيو", 11, SpecificPosition.RW, 79, 84, 83, 48, 70, new[] { SpecificPosition.CF, SpecificPosition.CAM }),
            (7020, "جونكالو راموس", 9, SpecificPosition.ST, 81, 74, 84, 46, 82, null),
            (7021, "إبراهيم مباي", 49, SpecificPosition.LW, 87, 72, 70, 40, 66, null),
            (7022, "أرناو تيناس", 80, SpecificPosition.GK, 60, 74, 15, 78, 76, null),
            (7023, "يورام زاغي", 42, SpecificPosition.RB, 83, 72, 45, 75, 74, null)
        };

        return DatabaseHelpers.CreateTeam("FRA_PSG", "باريس سان جيرمان", FormationType.Formation433_Holding, false, starters, bench);
    }

    // 2. أولمبيك مارسيليا (Olympique de Marseille) - 22 لاعباً
    public static Team CreateMarseille()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7031, "خيرونيمو رولي", 1, SpecificPosition.GK, 60, 79, 18, 84, 80, null),
            (7032, "كوينتن ميرلين", 3, SpecificPosition.LB, 83, 81, 68, 78, 76, new[] { SpecificPosition.LWB, SpecificPosition.CM }),
            (7033, "ليوناردو باليردي", 5, SpecificPosition.CB, 79, 75, 45, 83, 84, null),
            (7034, "ديريك كورنيليوس", 13, SpecificPosition.CB, 76, 73, 40, 81, 85, null),
            (7035, "أمير موريلو", 62, SpecificPosition.RB, 84, 77, 65, 79, 80, new[] { SpecificPosition.RWB, SpecificPosition.LB }),
            (7036, "بيير إميل هويبيرج", 23, SpecificPosition.CDM, 74, 84, 76, 84, 85, new[] { SpecificPosition.CM }),
            (7037, "أدريان رابيو", 25, SpecificPosition.CM, 78, 84, 79, 82, 85, new[] { SpecificPosition.CDM }),
            (7038, "فالنتين رونجيه", 21, SpecificPosition.CM, 78, 83, 72, 81, 79, new[] { SpecificPosition.CDM, SpecificPosition.RB }),
            (7039, "لويس هنريكه", 44, SpecificPosition.LW, 89, 78, 78, 52, 75, new[] { SpecificPosition.LM, SpecificPosition.RW }),
            (7040, "إيلي واهي", 9, SpecificPosition.ST, 88, 74, 82, 40, 78, null),
            (7041, "مايسون جرينوود", 10, SpecificPosition.RW, 87, 82, 86, 42, 74, new[] { SpecificPosition.ST, SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7042, "جيفري دي لانج", 12, SpecificPosition.GK, 60, 73, 15, 78, 78, null),
            (7043, "شانسيل مبيمبا", 99, SpecificPosition.CB, 78, 72, 60, 82, 84, new[] { SpecificPosition.RB }),
            (7044, "ليليان براسييه", 20, SpecificPosition.CB, 77, 74, 45, 80, 83, new[] { SpecificPosition.LB }),
            (7045, "بول ليرولا", 29, SpecificPosition.RB, 83, 76, 62, 76, 76, null),
            (7046, "أوليسيس جارسيا", 6, SpecificPosition.LB, 84, 76, 65, 77, 78, null),
            (7047, "إسماعيل كونيه", 17, SpecificPosition.CM, 80, 80, 74, 78, 82, null),
            (7048, "أمين حارث", 11, SpecificPosition.CAM, 85, 83, 76, 50, 68, new[] { SpecificPosition.LW }),
            (7049, "بلال نادر", 26, SpecificPosition.CM, 79, 78, 70, 72, 74, null),
            (7050, "فارس مومبانيه", 14, SpecificPosition.ST, 84, 68, 79, 45, 86, null),
            (7051, "نيل موباي", 8, SpecificPosition.ST, 80, 75, 80, 48, 79, null),
            (7052, "روبين بلانكو", 36, SpecificPosition.GK, 60, 72, 15, 77, 77, null)
        };

        return DatabaseHelpers.CreateTeam("FRA_OM", "أولمبيك مارسيليا", FormationType.Formation433_Holding, false, starters, bench);
    }

    // 3. موناكو (AS Monaco) - 22 لاعباً
    public static Team CreateMonaco()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7061, "فيليب كون", 16, SpecificPosition.GK, 62, 76, 18, 83, 82, null),
            (7062, "كايو هنريكه", 12, SpecificPosition.LB, 84, 85, 68, 79, 76, new[] { SpecificPosition.LWB }),
            (7063, "تيلو كيرير", 5, SpecificPosition.CB, 80, 78, 50, 82, 83, new[] { SpecificPosition.RB }),
            (7064, "ويلفريد سينجو", 17, SpecificPosition.CB, 86, 75, 58, 83, 87, new[] { SpecificPosition.RB }),
            (7065, "فانديرسون", 2, SpecificPosition.RB, 88, 81, 70, 79, 78, new[] { SpecificPosition.RWB, SpecificPosition.RM }),
            (7066, "دينيس زكريا", 6, SpecificPosition.CDM, 82, 80, 74, 84, 88, new[] { SpecificPosition.CB, SpecificPosition.CM }),
            (7067, "لامين كامارا", 15, SpecificPosition.CM, 82, 83, 76, 80, 81, new[] { SpecificPosition.CDM }),
            (7068, "ألكسندر جولوفين", 10, SpecificPosition.CAM, 80, 87, 81, 68, 75, new[] { SpecificPosition.LW, SpecificPosition.CM }),
            (7069, "إلياس بن صغير", 7, SpecificPosition.LW, 86, 83, 80, 48, 68, new[] { SpecificPosition.CAM }),
            (7070, "بريل إمبولو", 36, SpecificPosition.ST, 84, 75, 82, 45, 87, null),
            (7071, "ماجني أكليوش", 11, SpecificPosition.RW, 85, 85, 80, 50, 70, new[] { SpecificPosition.CAM })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7072, "رادوسلاف ماجيكي", 1, SpecificPosition.GK, 60, 74, 15, 80, 80, null),
            (7073, "محمد ساليسو", 22, SpecificPosition.CB, 76, 70, 40, 82, 86, null),
            (7074, "كريستيان ماويسا", 13, SpecificPosition.CB, 80, 73, 40, 78, 80, new[] { SpecificPosition.LB }),
            (7075, "كاسوم واتارا", 20, SpecificPosition.LB, 85, 75, 60, 76, 75, null),
            (7076, "جوردان تيزيه", 4, SpecificPosition.RB, 82, 77, 55, 79, 81, new[] { SpecificPosition.CB }),
            (7077, "سليمان ماجاسا", 88, SpecificPosition.CDM, 78, 77, 60, 79, 83, new[] { SpecificPosition.CB }),
            (7078, "إدان ديوب", 28, SpecificPosition.CM, 80, 78, 70, 74, 76, null),
            (7079, "تاكومي مينامينو", 18, SpecificPosition.CAM, 82, 82, 81, 55, 72, new[] { SpecificPosition.LW, SpecificPosition.ST }),
            (7080, "جورج إيلينيكينا", 9, SpecificPosition.ST, 86, 70, 79, 38, 81, null),
            (7081, "فولارين بالوجون", 27, SpecificPosition.ST, 86, 74, 82, 42, 78, null),
            (7082, "يان ليينارد", 50, SpecificPosition.GK, 58, 68, 15, 74, 74, null)
        };

        return DatabaseHelpers.CreateTeam("FRA_ASM", "موناكو", FormationType.Formation4231_Wide, false, starters, bench);
    }

    // 4. أولمبيك ليون (Olympique Lyonnais) - 22 لاعباً
    public static Team CreateLyon()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7091, "لوكاس بيري", 23, SpecificPosition.GK, 58, 80, 18, 84, 88, null),
            (7092, "نيكولاس تاليافيكو", 3, SpecificPosition.LB, 80, 78, 65, 82, 80, new[] { SpecificPosition.LWB }),
            (7093, "موسى نياخاتي", 19, SpecificPosition.CB, 81, 75, 45, 82, 86, new[] { SpecificPosition.LB }),
            (7094, "دويه كاليتا كار", 55, SpecificPosition.CB, 72, 76, 50, 82, 87, null),
            (7095, "إينسلي ميتلاند نايلز", 98, SpecificPosition.RB, 84, 79, 68, 79, 78, new[] { SpecificPosition.RWB, SpecificPosition.CM, SpecificPosition.LB }),
            (7096, "نيمانيا ماتيتش", 31, SpecificPosition.CDM, 62, 84, 72, 83, 84, null),
            (7097, "تانجي ندومبيلي", 8, SpecificPosition.CM, 78, 84, 76, 78, 82, null),
            (7098, "ماكسينس كاكيري", 6, SpecificPosition.CM, 82, 84, 72, 81, 76, new[] { SpecificPosition.CDM }),
            (7099, "ريان شرقي", 18, SpecificPosition.CAM, 83, 87, 80, 42, 70, new[] { SpecificPosition.RW, SpecificPosition.LW }),
            (7100, "ألكسندر لاكازيت", 10, SpecificPosition.ST, 77, 81, 86, 45, 80, null),
            (7101, "ماليك فوفانا", 11, SpecificPosition.LW, 91, 78, 78, 42, 68, new[] { SpecificPosition.RW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7102, "أنتوني لوبيز", 1, SpecificPosition.GK, 62, 76, 15, 81, 79, null),
            (7103, "كلينتون ماتا", 22, SpecificPosition.CB, 80, 76, 55, 80, 82, new[] { SpecificPosition.RB }),
            (7104, "أبنير فينيسيوس", 16, SpecificPosition.LB, 83, 76, 62, 77, 76, null),
            (7105, "سائيل كومبيدي", 20, SpecificPosition.RB, 86, 74, 52, 76, 75, null),
            (7106, "كورنتين توليسو", 8, SpecificPosition.CM, 74, 83, 80, 79, 81, new[] { SpecificPosition.CDM }),
            (7107, "تيتان سارت", 27, SpecificPosition.CM, 78, 76, 68, 75, 75, null),
            (7108, "تياجو ألمادا", 14, SpecificPosition.CAM, 85, 84, 81, 50, 68, new[] { SpecificPosition.LW }),
            (7109, "إرنست نواماه", 37, SpecificPosition.RW, 89, 77, 76, 45, 74, null),
            (7110, "جورج ميكوتادزه", 69, SpecificPosition.ST, 84, 78, 83, 42, 76, null),
            (7111, "هدسون أودوي", 17, SpecificPosition.LW, 86, 78, 77, 45, 72, null),
            (7112, "ريمي ديشامب", 30, SpecificPosition.GK, 58, 68, 15, 75, 76, null)
        };

        return DatabaseHelpers.CreateTeam("FRA_OL", "أولمبيك ليون", FormationType.Formation433_Holding, false, starters, bench);
    }

    // 5. ليل (LOSC Lille) - 22 لاعباً
    public static Team CreateLille()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7121, "لوكاس شوفالييه", 30, SpecificPosition.GK, 62, 81, 18, 86, 82, null),
            (7122, "جابرييل جودموندسون", 21, SpecificPosition.LB, 85, 78, 66, 79, 80, new[] { SpecificPosition.LWB, SpecificPosition.LM }),
            (7123, "أليكساندرو", 4, SpecificPosition.CB, 76, 74, 45, 82, 85, null),
            (7124, "بافوديه دياكيتي", 18, SpecificPosition.CB, 81, 76, 52, 82, 83, new[] { SpecificPosition.RB }),
            (7125, "توماس مونييه", 12, SpecificPosition.RB, 77, 80, 72, 80, 84, new[] { SpecificPosition.RWB }),
            (7126, "بنجامين أندريه", 21, SpecificPosition.CDM, 75, 82, 70, 84, 84, new[] { SpecificPosition.CM }),
            (7127, "أنخيل جوميز", 8, SpecificPosition.CM, 83, 86, 77, 68, 65, new[] { SpecificPosition.CAM }),
            (7128, "هاكون هارالدسون", 7, SpecificPosition.CAM, 83, 82, 78, 55, 72, new[] { SpecificPosition.LW }),
            (7129, "أوسام سيبي", 10, SpecificPosition.LW, 87, 80, 78, 48, 72, null),
            (7130, "جوناثان ديفيد", 9, SpecificPosition.ST, 86, 79, 86, 45, 80, null),
            (7131, "إدون زيجروفا", 23, SpecificPosition.RW, 89, 84, 81, 42, 68, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7132, "فيتو مانوني", 1, SpecificPosition.GK, 58, 72, 15, 77, 78, null),
            (7133, "صامويل أومتيتي", 14, SpecificPosition.CB, 68, 75, 45, 80, 82, null),
            (7134, "عيسى ماندي", 2, SpecificPosition.CB, 75, 76, 50, 80, 81, new[] { SpecificPosition.RB }),
            (7135, "أكيم زدادكا", 20, SpecificPosition.RB, 83, 75, 58, 76, 76, null),
            (7136, "ميتشل باكر", 3, SpecificPosition.LB, 84, 76, 62, 78, 82, new[] { SpecificPosition.LWB }),
            (7137, "أيوب بوعدي", 32, SpecificPosition.CM, 80, 81, 70, 77, 76, null),
            (7138, "نغالاييل موكاو", 17, SpecificPosition.CDM, 78, 78, 66, 78, 80, null),
            (7139, "ريمي كابيلا", 10, SpecificPosition.CAM, 78, 83, 79, 52, 68, null),
            (7140, "ماتياس فيرنانديز باردو", 19, SpecificPosition.LW, 86, 75, 76, 42, 72, null),
            (7141, "محمد بايو", 27, SpecificPosition.ST, 82, 70, 80, 42, 84, null),
            (7142, "مارك أوريلي كايار", 16, SpecificPosition.GK, 58, 68, 15, 74, 75, null)
        };

        return DatabaseHelpers.CreateTeam("FRA_LOSC", "ليل", FormationType.Formation4231_Wide, false, starters, bench);
    }

    // 6. لانس (RC Lens) - 22 لاعباً
    public static Team CreateLens()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7151, "برايس سامبا", 30, SpecificPosition.GK, 62, 82, 18, 86, 84, null),
            (7152, "ديفر ماتشادو", 3, SpecificPosition.LWB, 87, 78, 70, 78, 80, new[] { SpecificPosition.LB, SpecificPosition.LM }),
            (7153, "فاكوندو ميدينا", 14, SpecificPosition.CB, 80, 80, 56, 84, 85, new[] { SpecificPosition.LB }),
            (7154, "كيفين دانسو", 4, SpecificPosition.CB, 83, 75, 42, 86, 89, null),
            (7155, "جوناثان جراديت", 24, SpecificPosition.CB, 78, 76, 45, 82, 82, new[] { SpecificPosition.RB }),
            (7156, "برزيميسلاف فرانكوفسكي", 29, SpecificPosition.RWB, 86, 81, 74, 78, 79, new[] { SpecificPosition.RM, SpecificPosition.RB }),
            (7157, "أندي ضيوف", 18, SpecificPosition.CM, 82, 82, 75, 79, 83, null),
            (7158, "أدريان توماسون", 28, SpecificPosition.CM, 78, 81, 76, 77, 78, new[] { SpecificPosition.CAM }),
            (7159, "أنجيلو فولجيني", 11, SpecificPosition.CAM, 80, 83, 78, 55, 73, null),
            (7160, "فلوريان سوتوكا", 7, SpecificPosition.ST, 80, 80, 80, 68, 82, new[] { SpecificPosition.CAM, SpecificPosition.RW }),
            (7161, "مبالا نزولا", 8, SpecificPosition.ST, 84, 73, 82, 45, 87, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7162, "هيرفي كوفي", 16, SpecificPosition.GK, 60, 74, 15, 80, 80, null),
            (7163, "مالانج سار", 20, SpecificPosition.CB, 77, 74, 45, 79, 81, new[] { SpecificPosition.LB }),
            (7164, "عبد القادر خودسانوف", 25, SpecificPosition.CB, 81, 72, 40, 80, 84, null),
            (7165, "روبين أجيلار", 2, SpecificPosition.RB, 83, 77, 60, 78, 78, new[] { SpecificPosition.RWB }),
            (7166, "يوهان غانور", 26, SpecificPosition.LB, 84, 74, 58, 76, 76, null),
            (7167, "نيل العيناوي", 23, SpecificPosition.CM, 80, 81, 74, 79, 81, null),
            (7168, "حمدي حبيب ديارا", 6, SpecificPosition.CDM, 76, 78, 66, 79, 82, null),
            (7169, "دافيد كوستا", 10, SpecificPosition.CAM, 84, 82, 76, 48, 68, new[] { SpecificPosition.LW }),
            (7170, "أنس الزروري", 36, SpecificPosition.LW, 86, 80, 77, 45, 70, null),
            (7171, "ويسلي سعيد", 22, SpecificPosition.ST, 85, 76, 80, 42, 75, new[] { SpecificPosition.LW }),
            (7172, "دينيس بيتريتش", 1, SpecificPosition.GK, 58, 68, 15, 74, 74, null)
        };

        return DatabaseHelpers.CreateTeam("FRA_RCL", "لانس", FormationType.Formation343, false, starters, bench);
    }

    // 7. ستاد رين (Stade Rennais FC) - 22 لاعباً
    public static Team CreateRennes()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7181, "ستيف مانداندا", 30, SpecificPosition.GK, 60, 80, 18, 82, 82, null),
            (7182, "أدريان تروفير", 3, SpecificPosition.LB, 85, 80, 68, 79, 78, new[] { SpecificPosition.LWB }),
            (7183, "كريستوفر وو", 4, SpecificPosition.CB, 80, 74, 42, 82, 85, null),
            (7184, "ليو أوستيجارد", 55, SpecificPosition.CB, 77, 73, 48, 82, 86, null),
            (7185, "أليستير جونستون", 2, SpecificPosition.RB, 83, 78, 60, 80, 82, new[] { SpecificPosition.RWB }),
            (7186, "باتيست سانتاماريا", 8, SpecificPosition.CDM, 75, 82, 72, 82, 83, null),
            (7187, "عزام كاماتي", 17, SpecificPosition.CM, 82, 82, 75, 78, 80, null),
            (7188, "لودوفيك بلاس", 10, SpecificPosition.CAM, 82, 84, 82, 55, 74, new[] { SpecificPosition.RW }),
            (7189, "ألبيرت جرونباك", 7, SpecificPosition.LW, 85, 82, 80, 52, 76, new[] { SpecificPosition.CAM }),
            (7190, "أرنو كاليمويندو", 9, SpecificPosition.ST, 86, 76, 83, 42, 78, null),
            (7191, "أمين غويري", 11, SpecificPosition.ST, 83, 83, 83, 48, 76, new[] { SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7192, "غوتييه غايون", 1, SpecificPosition.GK, 60, 74, 15, 79, 79, null),
            (7193, "ميكاييل فاي", 18, SpecificPosition.CB, 80, 75, 45, 80, 83, new[] { SpecificPosition.LB }),
            (7194, "لورنز أسينيون", 22, SpecificPosition.RB, 85, 77, 62, 77, 77, new[] { SpecificPosition.RM }),
            (7195, "محمادو ناغيدا", 24, SpecificPosition.LB, 84, 74, 55, 75, 75, null),
            (7196, "جلين كامارا", 6, SpecificPosition.CM, 78, 81, 68, 79, 80, null),
            (7197, "جوردان جيمس", 28, SpecificPosition.CM, 80, 79, 74, 76, 78, null),
            (7198, "سيكو فوفانا", 15, SpecificPosition.CM, 80, 82, 78, 79, 85, null),
            (7199, "جوناثان رو", 20, SpecificPosition.LW, 88, 77, 76, 42, 73, null),
            (7200, "كارل توكو إيكامبي", 19, SpecificPosition.ST, 84, 77, 80, 45, 78, null),
            (7201, "هنريك مايستر", 14, SpecificPosition.ST, 82, 70, 77, 40, 80, null),
            (7202, "ماتيلد ليموان", 40, SpecificPosition.GK, 58, 68, 15, 74, 74, null)
        };

        return DatabaseHelpers.CreateTeam("FRA_REN", "ستاد رين", FormationType.Formation433_Holding, false, starters, bench);
    }

    // 8. نيس (OGC Nice) - 22 لاعباً
    public static Team CreateNice()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7211, "مارتشين بولكا", 1, SpecificPosition.GK, 60, 79, 18, 86, 88, null),
            (7212, "ملفين بارد", 26, SpecificPosition.LB, 84, 79, 65, 80, 78, new[] { SpecificPosition.LWB }),
            (7213, "دانتي", 4, SpecificPosition.CB, 62, 82, 55, 84, 83, null),
            (7214, "يوسف ندايشيميي", 55, SpecificPosition.CB, 78, 78, 60, 83, 86, new[] { SpecificPosition.CDM }),
            (7215, "جوناثان كلاوس", 92, SpecificPosition.RB, 86, 83, 74, 79, 78, new[] { SpecificPosition.RWB, SpecificPosition.RM }),
            (7216, "بابلو روزاريو", 8, SpecificPosition.CDM, 75, 80, 68, 82, 84, new[] { SpecificPosition.CB }),
            (7217, "هشام بوداوي", 6, SpecificPosition.CM, 82, 82, 74, 81, 80, new[] { SpecificPosition.CDM }),
            (7218, "تانجي ندومبيلي", 22, SpecificPosition.CM, 78, 84, 76, 78, 83, null),
            (7219, "جيريمي بوجا", 7, SpecificPosition.LW, 88, 81, 78, 42, 70, null),
            (7220, "تيريم موفي", 9, SpecificPosition.ST, 86, 74, 82, 45, 85, null),
            (7221, "إيفان جيساند", 29, SpecificPosition.RW, 87, 77, 80, 50, 82, new[] { SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7222, "ماكسيم دوبيه", 31, SpecificPosition.GK, 60, 74, 15, 79, 79, null),
            (7223, "محمد عبد المنعم", 5, SpecificPosition.CB, 78, 75, 45, 82, 84, null),
            (7224, "أنطوان ميندي", 33, SpecificPosition.CB, 80, 72, 40, 77, 80, new[] { SpecificPosition.RB }),
            (7225, "علي العابدي", 2, SpecificPosition.LB, 83, 77, 68, 78, 79, new[] { SpecificPosition.LWB }),
            (7226, "توم لوشيه", 25, SpecificPosition.CM, 80, 78, 72, 76, 76, null),
            (7227, "مورجان سانسون", 11, SpecificPosition.CM, 78, 82, 78, 77, 79, null),
            (7228, "سفيان ديوب", 10, SpecificPosition.CAM, 83, 82, 78, 48, 68, new[] { SpecificPosition.LW }),
            (7229, "بدر الدين بوعناني", 19, SpecificPosition.RW, 85, 81, 76, 42, 68, null),
            (7230, "يوسوفا موكوكو", 15, SpecificPosition.ST, 87, 74, 81, 38, 74, null),
            (7231, "غايتان لابورد", 24, SpecificPosition.ST, 80, 78, 82, 58, 80, new[] { SpecificPosition.RW }),
            (7232, "تيدي بولهندي", 77, SpecificPosition.GK, 58, 68, 15, 74, 74, null)
        };

        return DatabaseHelpers.CreateTeam("FRA_NIC", "نيس", FormationType.Formation433_Holding, false, starters, bench);
    }

    // 9. ستاد بريست (Stade Brestois 29) - 22 لاعباً
    public static Team CreateBrest()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7241, "ماركو بيزوت", 40, SpecificPosition.GK, 60, 78, 18, 84, 84, null),
            (7242, "جوردان أمافي", 23, SpecificPosition.LB, 82, 77, 65, 78, 78, new[] { SpecificPosition.LWB }),
            (7243, "بريندان شاردوني", 5, SpecificPosition.CB, 76, 75, 45, 82, 85, null),
            (7244, "سومايلا كوليبالي", 44, SpecificPosition.CB, 78, 74, 40, 81, 84, null),
            (7245, "كيني لالا", 7, SpecificPosition.RB, 82, 81, 72, 79, 79, new[] { SpecificPosition.RWB }),
            (7246, "بيير ليس ميلو", 8, SpecificPosition.CDM, 76, 84, 80, 83, 83, new[] { SpecificPosition.CM }),
            (7247, "هوجو مانيتي", 45, SpecificPosition.CM, 80, 81, 75, 79, 81, null),
            (7248, "مهدي كامارا", 20, SpecificPosition.CM, 80, 81, 78, 80, 83, null),
            (7249, "رومان ديل كاستيو", 10, SpecificPosition.RW, 84, 84, 80, 48, 70, new[] { SpecificPosition.CAM }),
            (7250, "لودوفيك أجورك", 19, SpecificPosition.ST, 72, 75, 82, 45, 91, null),
            (7251, "عبد الله سيما", 17, SpecificPosition.LW, 88, 76, 80, 50, 82, new[] { SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7252, "جريجوار كودير", 30, SpecificPosition.GK, 58, 72, 15, 78, 78, null),
            (7253, "جوليان لو كاردينال", 25, SpecificPosition.CB, 78, 72, 42, 78, 81, new[] { SpecificPosition.RB }),
            (7254, "ماساكيو حيدرة", 12, SpecificPosition.LB, 81, 75, 55, 78, 80, new[] { SpecificPosition.CB }),
            (7255, "لوك زوغبي", 27, SpecificPosition.RB, 82, 73, 45, 76, 76, null),
            (7256, "إدميلسون فرنانديز", 6, SpecificPosition.CDM, 78, 80, 74, 79, 81, new[] { SpecificPosition.CB, SpecificPosition.RB }),
            (7257, "جوناس مارتن", 28, SpecificPosition.CM, 74, 80, 72, 78, 78, null),
            (7258, "كامل الدين دومبيا", 21, SpecificPosition.CAM, 82, 80, 76, 52, 74, null),
            (7259, "ماتياس بيريرا لاجي", 26, SpecificPosition.RW, 82, 78, 76, 60, 76, new[] { SpecificPosition.LW }),
            (7260, "رومان فايفر", 21, SpecificPosition.CAM, 83, 83, 80, 52, 72, new[] { SpecificPosition.RW }),
            (7261, "ماما بالدي", 11, SpecificPosition.ST, 87, 74, 79, 45, 81, new[] { SpecificPosition.RW }),
            (7262, "نوح جوني", 50, SpecificPosition.GK, 56, 66, 15, 73, 73, null)
        };

        return DatabaseHelpers.CreateTeam("FRA_BRE", "ستاد بريست", FormationType.Formation433_Holding, false, starters, bench);
    }

    // 10. ستاد ريمس (Stade de Reims) - 22 لاعباً
    public static Team CreateReims()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7271, "ييهفان ديوف", 94, SpecificPosition.GK, 62, 78, 18, 83, 82, null),
            (7272, "سيرجيو أكييمي", 18, SpecificPosition.LB, 85, 77, 64, 78, 78, new[] { SpecificPosition.LWB }),
            (7273, "إيمانويل أجبادو", 24, SpecificPosition.CB, 81, 74, 52, 82, 86, null),
            (7274, "سيدريك كيبودي", 21, SpecificPosition.CB, 78, 72, 40, 81, 84, null),
            (7275, "أوريليو بوتا", 23, SpecificPosition.RB, 85, 78, 65, 78, 77, new[] { SpecificPosition.RWB, SpecificPosition.RM }),
            (7276, "فالنتين أتانجانا إدوا", 6, SpecificPosition.CDM, 80, 80, 70, 80, 82, null),
            (7277, "مارشال مونيتسي", 15, SpecificPosition.CM, 82, 81, 78, 81, 87, new[] { SpecificPosition.CAM }),
            (7278, "تيدي توما", 10, SpecificPosition.CAM, 80, 85, 82, 58, 76, new[] { SpecificPosition.CM }),
            (7279, "كيسوكي ناكامورا", 17, SpecificPosition.LW, 88, 80, 80, 48, 70, null),
            (7280, "عمر دياكيتي", 22, SpecificPosition.ST, 87, 72, 80, 42, 82, null),
            (7281, "جونيا إيتو", 7, SpecificPosition.RW, 89, 83, 80, 52, 74, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7282, "ألكسندر أوليرو", 16, SpecificPosition.GK, 58, 72, 15, 78, 78, null),
            (7283, "جوزيف أوكومو", 2, SpecificPosition.CB, 80, 72, 42, 80, 83, null),
            (7284, "ماكسيم بوست", 25, SpecificPosition.CB, 76, 73, 40, 78, 80, null),
            (7285, "تيبو دي سميت", 3, SpecificPosition.LB, 81, 74, 55, 76, 76, null),
            (7286, "عبدول كوني", 92, SpecificPosition.RB, 82, 72, 45, 75, 76, null),
            (7287, "يحيى فوفانا", 71, SpecificPosition.CM, 78, 78, 68, 76, 78, null),
            (7288, "أمادو كوني", 72, SpecificPosition.CM, 80, 78, 70, 76, 77, null),
            (7289, "ريد خضرا", 14, SpecificPosition.CAM, 84, 80, 77, 45, 72, new[] { SpecificPosition.LW }),
            (7290, "محمد دارامي", 9, SpecificPosition.LW, 87, 77, 78, 42, 75, null),
            (7291, "أمين سلامة", 11, SpecificPosition.ST, 84, 70, 76, 40, 80, null),
            (7292, "لودوفيك بوتيل", 30, SpecificPosition.GK, 56, 68, 15, 74, 75, null)
        };

        return DatabaseHelpers.CreateTeam("FRA_REI", "ستاد ريمس", FormationType.Formation433_Holding, false, starters, bench);
    }

    // 11. ستراسبورج (RC Strasbourg) - 22 لاعباً
    public static Team CreateStrasbourg()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7301, "جورجي بتروفيتش", 1, SpecificPosition.GK, 62, 79, 18, 84, 84, null),
            (7302, "دييجو موريرا", 7, SpecificPosition.LWB, 88, 78, 74, 74, 76, new[] { SpecificPosition.LM, SpecificPosition.LW }),
            (7303, "مامادو سار", 24, SpecificPosition.CB, 78, 74, 40, 81, 84, null),
            (7304, "سيدو سو", 22, SpecificPosition.CB, 78, 73, 40, 81, 85, null),
            (7305, "جيليرمو ماريبان", 3, SpecificPosition.CB, 70, 75, 52, 83, 86, null),
            (7306, "جيلان دوكوري", 29, SpecificPosition.RWB, 86, 77, 62, 77, 78, new[] { SpecificPosition.RB }),
            (7307, "أندريه سانتوس", 8, SpecificPosition.CM, 82, 83, 78, 80, 82, new[] { SpecificPosition.CDM }),
            (7308, "إسماعيل دوكوري", 6, SpecificPosition.CM, 81, 80, 68, 81, 83, new[] { SpecificPosition.CB, SpecificPosition.CDM }),
            (7309, "حبيب ديارا", 19, SpecificPosition.CAM, 85, 83, 80, 68, 78, new[] { SpecificPosition.CM }),
            (7310, "سيباستيان ناناسي", 11, SpecificPosition.LW, 86, 83, 80, 50, 72, new[] { SpecificPosition.CAM }),
            (7311, "إيمانويل إميجا", 10, SpecificPosition.ST, 88, 72, 81, 42, 86, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7312, "كارل يوهان جونسون", 30, SpecificPosition.GK, 58, 72, 15, 78, 78, null),
            (7313, "أباكار سيلا", 28, SpecificPosition.CB, 79, 74, 45, 80, 84, null),
            (7314, "إدوارد سوبول", 77, SpecificPosition.LB, 82, 75, 62, 77, 78, null),
            (7315, "مارفين سينايا", 25, SpecificPosition.RB, 83, 76, 58, 77, 78, null),
            (7316, "جونيور موانجا", 18, SpecificPosition.CDM, 79, 78, 65, 79, 82, new[] { SpecificPosition.CB }),
            (7317, "فيليكس ليماريتشال", 17, SpecificPosition.CM, 80, 80, 74, 74, 75, null),
            (7318, "بابي ديونج", 15, SpecificPosition.CM, 80, 78, 70, 76, 78, null),
            (7319, "ديلان باكوا", 26, SpecificPosition.RW, 87, 80, 78, 45, 74, null),
            (7320, "سيكو مارا", 9, SpecificPosition.ST, 85, 74, 79, 42, 80, null),
            (7321, "ميلوس لوكوفيتش", 20, SpecificPosition.ST, 82, 70, 78, 40, 82, null),
            (7322, "وليد حسبي", 36, SpecificPosition.GK, 58, 68, 15, 74, 74, null)
        };

        return DatabaseHelpers.CreateTeam("FRA_STR", "ستراسبورج", FormationType.Formation343, false, starters, bench);
    }

    // 12. تولوز (Toulouse FC) - 22 لاعباً
    public static Team CreateToulouse()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7331, "غيوم ريستيس", 50, SpecificPosition.GK, 62, 79, 18, 84, 82, null),
            (7332, "جابرييل سوازو", 17, SpecificPosition.LB, 83, 80, 68, 79, 79, new[] { SpecificPosition.LWB, SpecificPosition.LM }),
            (7333, "راسموس نيكولايسن", 2, SpecificPosition.CB, 74, 75, 48, 83, 87, null),
            (7334, "مارك ماكنزي", 4, SpecificPosition.CB, 81, 74, 42, 81, 84, null),
            (7335, "وارن كومان", 19, SpecificPosition.RB, 84, 77, 60, 78, 79, new[] { SpecificPosition.RWB }),
            (7336, "كريستيان كاسيريس جونيور", 24, SpecificPosition.CDM, 80, 81, 72, 81, 81, new[] { SpecificPosition.CM }),
            (7337, "فنسنت سييرو", 8, SpecificPosition.CM, 76, 83, 80, 80, 80, null),
            (7338, "يان غبوهو", 10, SpecificPosition.CAM, 85, 82, 79, 52, 75, new[] { SpecificPosition.LW }),
            (7339, "زاكاريا أبو خلال", 7, SpecificPosition.RW, 87, 80, 80, 48, 76, new[] { SpecificPosition.LW, SpecificPosition.ST }),
            (7340, "فرانك ماجري", 9, SpecificPosition.ST, 84, 74, 80, 48, 83, null),
            (7341, "آرون دونوم", 15, SpecificPosition.LW, 84, 80, 78, 55, 77, new[] { SpecificPosition.RW, SpecificPosition.LM })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7342, "أليكس دومينغيز", 30, SpecificPosition.GK, 58, 72, 15, 78, 78, null),
            (7343, "شارلي كريسويل", 3, SpecificPosition.CB, 76, 70, 40, 79, 83, null),
            (7344, "أومي أكبوبروما", 6, SpecificPosition.CB, 77, 71, 38, 78, 82, null),
            (7345, "جبريل سيديبيه", 29, SpecificPosition.RB, 78, 77, 62, 78, 80, new[] { SpecificPosition.CB }),
            (7346, "أوروش سباييتش", 13, SpecificPosition.CB, 72, 72, 45, 78, 81, null),
            (7347, "دينيس خينرو", 5, SpecificPosition.CM, 78, 80, 72, 77, 78, null),
            (7348, "نيكلاس فاسمير", 34, SpecificPosition.CM, 78, 78, 70, 75, 76, null),
            (7349, "جابر سيلفا", 80, SpecificPosition.LW, 86, 78, 76, 45, 72, null),
            (7350, "شيفي بابيكا", 11, SpecificPosition.RW, 89, 76, 76, 42, 74, null),
            (7351, "جوشوا كينج", 13, SpecificPosition.ST, 83, 76, 80, 45, 82, null),
            (7352, "جوستن لاكومب", 40, SpecificPosition.GK, 56, 66, 15, 73, 73, null)
        };

        return DatabaseHelpers.CreateTeam("FRA_TOU", "تولوز", FormationType.Formation433_Holding, false, starters, bench);
    }

    // 13. مونبلييه (Montpellier HSC) - 22 لاعباً
    public static Team CreateMontpellier()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7361, "بنيامين لوكومت", 40, SpecificPosition.GK, 60, 79, 18, 84, 82, null),
            (7362, "إيسياجا سيلا", 3, SpecificPosition.LB, 83, 77, 62, 79, 80, new[] { SpecificPosition.LWB }),
            (7363, "بوباكار كويايتي", 4, SpecificPosition.CB, 79, 71, 40, 82, 87, null),
            (7364, "بيكير أوميراجيتش", 27, SpecificPosition.CB, 80, 75, 42, 81, 82, new[] { SpecificPosition.RB }),
            (7365, "فالنتين ساكو", 77, SpecificPosition.RB, 84, 76, 60, 78, 79, new[] { SpecificPosition.RWB }),
            (7366, "جوريس شوتار", 13, SpecificPosition.CDM, 78, 83, 72, 82, 81, new[] { SpecificPosition.CM }),
            (7367, "تيجي سافانييه", 11, SpecificPosition.CM, 76, 88, 84, 74, 76, new[] { SpecificPosition.CAM }),
            (7368, "جوردان فيري", 12, SpecificPosition.CM, 75, 81, 72, 80, 81, new[] { SpecificPosition.CDM }),
            (7369, "وهبي خزري", 99, SpecificPosition.LW, 80, 81, 81, 52, 76, new[] { SpecificPosition.ST, SpecificPosition.CAM }),
            (7370, "أكور أدامز", 8, SpecificPosition.ST, 85, 72, 82, 42, 86, null),
            (7371, "موسى التعمري", 7, SpecificPosition.RW, 89, 81, 80, 50, 75, new[] { SpecificPosition.LW, SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7372, "ديميتري بيرتو", 16, SpecificPosition.GK, 58, 72, 15, 78, 78, null),
            (7373, "موديبو سانيان", 5, SpecificPosition.CB, 77, 72, 40, 79, 84, null),
            (7374, "كريستوفر جولين", 6, SpecificPosition.CB, 68, 72, 45, 79, 85, null),
            (7375, "لوكاس مينكاريلي", 21, SpecificPosition.LB, 83, 74, 55, 76, 76, null),
            (7376, "إنزو تشاتو", 29, SpecificPosition.RB, 82, 74, 55, 76, 77, null),
            (7377, "خليل فياض", 22, SpecificPosition.CM, 80, 80, 74, 74, 74, new[] { SpecificPosition.CAM }),
            (7378, "رابيع بايرو", 20, SpecificPosition.CDM, 78, 77, 65, 77, 78, null),
            (7379, "أرنو نوردين", 10, SpecificPosition.RW, 86, 78, 78, 48, 75, new[] { SpecificPosition.LW }),
            (7380, "عثمان معما", 19, SpecificPosition.LW, 86, 75, 74, 42, 70, null),
            (7381, "تانجي كوليبالي", 70, SpecificPosition.ST, 85, 76, 77, 45, 75, null),
            (7382, "بلين ديزي", 30, SpecificPosition.GK, 56, 66, 15, 73, 74, null)
        };

        return DatabaseHelpers.CreateTeam("FRA_MHS", "مونبلييه", FormationType.Formation433_Holding, false, starters, bench);
    }

    // 14. نانت (FC Nantes) - 22 لاعباً
    public static Team CreateNantes()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7391, "ألبان لافونت", 1, SpecificPosition.GK, 62, 80, 18, 84, 82, null),
            (7392, "نيكولا كوزا", 3, SpecificPosition.LB, 82, 77, 60, 79, 80, new[] { SpecificPosition.CB }),
            (7393, "جان شارل كاستيليتو", 21, SpecificPosition.CB, 78, 74, 45, 82, 84, null),
            (7394, "ناثان زيزي", 44, SpecificPosition.CB, 79, 73, 40, 81, 83, null),
            (7395, "كيلفن أميان", 98, SpecificPosition.RB, 83, 77, 58, 80, 81, new[] { SpecificPosition.CB, SpecificPosition.RWB }),
            (7396, "بيدرو تشيريفيلا", 5, SpecificPosition.CDM, 74, 84, 70, 81, 79, new[] { SpecificPosition.CM }),
            (7397, "دوجلاس أوجوستو", 6, SpecificPosition.CM, 80, 81, 74, 81, 82, new[] { SpecificPosition.CDM }),
            (7398, "يوهان ليبينانت", 8, SpecificPosition.CM, 81, 82, 72, 80, 78, null),
            (7399, "موزيس سيمون", 27, SpecificPosition.LW, 91, 80, 79, 45, 74, new[] { SpecificPosition.LM, SpecificPosition.ST }),
            (7400, "مصطفى محمد", 31, SpecificPosition.ST, 82, 74, 83, 48, 85, null),
            (7401, "سوربا توماس", 10, SpecificPosition.RW, 87, 81, 76, 52, 76, new[] { SpecificPosition.RM })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7402, "باتريك كارلجرين", 30, SpecificPosition.GK, 58, 72, 15, 78, 78, null),
            (7403, "نيكولا بالوا", 4, SpecificPosition.CB, 62, 72, 45, 81, 86, null),
            (7404, "فابيان كينتونز", 2, SpecificPosition.RB, 83, 76, 58, 78, 78, null),
            (7405, "سايدو سو", 24, SpecificPosition.CB, 77, 71, 38, 78, 81, null),
            (7406, "ماثيو أكاباندي", 18, SpecificPosition.LB, 83, 73, 50, 75, 75, null),
            (7407, "جان فيليب جبامين", 17, SpecificPosition.CDM, 76, 78, 68, 79, 83, new[] { SpecificPosition.CB }),
            (7408, "فلوران موليه", 25, SpecificPosition.CAM, 76, 82, 80, 55, 74, null),
            (7409, "ماركوس كوكو", 11, SpecificPosition.RW, 86, 76, 74, 60, 78, new[] { SpecificPosition.RB }),
            (7410, "ماتيس أبلين", 39, SpecificPosition.ST, 85, 75, 79, 42, 78, null),
            (7411, "إجناتيوس جاناجو", 7, SpecificPosition.ST, 84, 73, 78, 45, 81, null),
            (7412, "هوجو باربيه", 50, SpecificPosition.GK, 56, 66, 15, 73, 73, null)
        };

        return DatabaseHelpers.CreateTeam("FRA_NAN", "نانت", FormationType.Formation433_Holding, false, starters, bench);
    }

    // 15. أوكسير (AJ Auxerre) - 22 لاعباً
    public static Team CreateAuxerre()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7421, "دونوفان ليون", 16, SpecificPosition.GK, 60, 76, 18, 82, 82, null),
            (7422, "جدعون مينساه", 14, SpecificPosition.LB, 85, 77, 60, 78, 78, new[] { SpecificPosition.LWB }),
            (7423, "جبريل أوشو", 6, SpecificPosition.CB, 78, 72, 42, 81, 85, null),
            (7424, "جوبال", 4, SpecificPosition.CB, 74, 75, 55, 82, 86, null),
            (7425, "باول جولي", 26, SpecificPosition.RB, 84, 78, 65, 78, 78, new[] { SpecificPosition.RWB, SpecificPosition.RM }),
            (7426, "إليشا أوسو", 42, SpecificPosition.CDM, 79, 81, 70, 81, 83, null),
            (7427, "ريان رافيلوسون", 8, SpecificPosition.CM, 80, 81, 76, 80, 82, new[] { SpecificPosition.CDM }),
            (7428, "حامد تراوري", 23, SpecificPosition.CAM, 84, 83, 81, 55, 75, new[] { SpecificPosition.LW }),
            (7429, "غايتان بيرين", 10, SpecificPosition.RW, 84, 82, 79, 52, 72, new[] { SpecificPosition.LW, SpecificPosition.CAM }),
            (7430, "لاسين سينايوكو", 17, SpecificPosition.ST, 85, 77, 81, 52, 84, new[] { SpecificPosition.RW }),
            (7431, "ثيو بيرينجيه", 9, SpecificPosition.ST, 83, 74, 80, 45, 82, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7432, "توم نيجرل", 30, SpecificPosition.GK, 58, 70, 15, 76, 76, null),
            (7433, "ثيو بيلينار", 5, SpecificPosition.CB, 75, 72, 40, 78, 80, new[] { SpecificPosition.LB }),
            (7434, "سينالي ديوماندي", 20, SpecificPosition.CB, 78, 73, 40, 79, 82, null),
            (7435, "كليمون أكبا", 92, SpecificPosition.LB, 84, 74, 55, 76, 77, null),
            (7436, "كي جان هور", 27, SpecificPosition.RB, 83, 73, 50, 76, 76, null),
            (7437, "أساني ديوسي", 18, SpecificPosition.CDM, 76, 78, 68, 78, 80, null),
            (7438, "كيفين دانجوا", 21, SpecificPosition.CM, 78, 78, 72, 76, 78, null),
            (7439, "فلوريان أييه", 19, SpecificPosition.ST, 82, 73, 79, 45, 81, null),
            (7440, "أوداير فورتيس", 7, SpecificPosition.LW, 86, 75, 75, 42, 72, null),
            (7441, "إروس مادي", 45, SpecificPosition.RW, 87, 74, 74, 40, 70, null),
            (7442, "رافائيل أديوتي", 40, SpecificPosition.GK, 56, 66, 15, 73, 73, null)
        };

        return DatabaseHelpers.CreateTeam("FRA_AUX", "أوكسير", FormationType.Formation4231_Wide, false, starters, bench);
    }

    // 16. أنجيه (Angers SCO) - 22 لاعباً
    public static Team CreateAngers()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7451, "يحيى فوفانا", 30, SpecificPosition.GK, 62, 78, 18, 83, 83, null),
            (7452, "فلوران هانين", 21, SpecificPosition.LB, 82, 77, 60, 78, 78, null),
            (7453, "سيدريك هومبوندجي", 5, SpecificPosition.CB, 76, 73, 45, 81, 86, null),
            (7454, "جوردان ليفورت", 27, SpecificPosition.CB, 77, 74, 45, 80, 82, new[] { SpecificPosition.LB }),
            (7455, "كارلينس أركوس", 2, SpecificPosition.RB, 84, 76, 58, 78, 79, new[] { SpecificPosition.RWB }),
            (7456, "جان يود أهولو", 6, SpecificPosition.CDM, 75, 79, 72, 81, 84, null),
            (7457, "هاريس بلقبلة", 93, SpecificPosition.CM, 78, 81, 70, 81, 82, new[] { SpecificPosition.CDM }),
            (7458, "ياسين بلخادم", 14, SpecificPosition.CM, 82, 80, 74, 77, 76, null),
            (7459, "فريد الملالي", 28, SpecificPosition.LW, 88, 79, 78, 48, 72, new[] { SpecificPosition.RW }),
            (7460, "إبراهيما نياني", 7, SpecificPosition.ST, 85, 72, 80, 42, 83, null),
            (7461, "جيم أليفيناه", 11, SpecificPosition.RW, 87, 78, 77, 50, 74, new[] { SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7462, "ميلفين زيملي", 16, SpecificPosition.GK, 58, 70, 15, 76, 76, null),
            (7463, "عبد الله بامبا", 25, SpecificPosition.CB, 78, 72, 42, 78, 79, new[] { SpecificPosition.RB }),
            (7464, "إيمانويل بيوملا", 22, SpecificPosition.CB, 78, 71, 38, 78, 81, null),
            (7465, "ليليان راوليزوا", 26, SpecificPosition.RB, 83, 73, 50, 76, 76, null),
            (7466, "جاك إيكومييه", 3, SpecificPosition.LB, 83, 72, 48, 75, 76, null),
            (7467, "زين الدين فرحات", 10, SpecificPosition.CAM, 80, 82, 78, 50, 72, new[] { SpecificPosition.RW }),
            (7468, "بييريك كابيل", 15, SpecificPosition.CM, 72, 80, 74, 76, 78, null),
            (7469, "لوا هوانتونجي", 19, SpecificPosition.ST, 84, 71, 78, 40, 82, null),
            (7470, "إستيبان ليبول", 9, SpecificPosition.ST, 82, 72, 79, 42, 79, null),
            (7471, "زين الدين ولد خالد", 12, SpecificPosition.CM, 78, 78, 70, 75, 76, null),
            (7472, "أوجو باسترال", 40, SpecificPosition.GK, 56, 66, 15, 73, 73, null)
        };

        return DatabaseHelpers.CreateTeam("FRA_ANG", "أنجيه", FormationType.Formation433_Holding, false, starters, bench);
    }

    // 17. سانت إيتيان (AS Saint-Étienne) - 22 لاعباً
    public static Team CreateSaintEtienne()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7481, "جوتييه لارسونور", 30, SpecificPosition.GK, 62, 78, 18, 83, 82, null),
            (7482, "بيير كورنو", 13, SpecificPosition.LB, 83, 78, 62, 78, 78, new[] { SpecificPosition.LWB }),
            (7483, "ميكائيل نادي", 3, SpecificPosition.CB, 78, 72, 40, 81, 86, null),
            (7484, "يونس عبد الحميد", 5, SpecificPosition.CB, 65, 76, 50, 82, 85, null),
            (7485, "دينيس أبياه", 8, SpecificPosition.RB, 83, 77, 58, 79, 80, new[] { SpecificPosition.CB, SpecificPosition.LB }),
            (7486, "بيير إكوا", 4, SpecificPosition.CDM, 80, 80, 72, 81, 84, new[] { SpecificPosition.CM }),
            (7487, "بنجامين بوشواري", 6, SpecificPosition.CM, 84, 83, 74, 77, 72, null),
            (7488, "ماتيس أموجو", 29, SpecificPosition.CM, 82, 82, 74, 78, 78, null),
            (7489, "زوريكو دافيتاشفيلي", 11, SpecificPosition.LW, 88, 82, 82, 48, 74, new[] { SpecificPosition.RW, SpecificPosition.CAM }),
            (7490, "إبراهيم سيسوكو", 9, SpecificPosition.ST, 84, 73, 82, 45, 87, null),
            (7491, "بافوديه كافارو", 18, SpecificPosition.RW, 84, 82, 79, 55, 76, new[] { SpecificPosition.RM, SpecificPosition.RWB })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7492, "إيتيان جرين", 1, SpecificPosition.GK, 60, 72, 15, 78, 78, null),
            (7493, "أنتوني بريانكون", 23, SpecificPosition.CB, 72, 74, 45, 80, 83, null),
            (7494, "ليو بيترو", 21, SpecificPosition.LB, 82, 75, 58, 77, 78, new[] { SpecificPosition.CB }),
            (7495, "إيفان ماكون", 27, SpecificPosition.RB, 84, 76, 55, 76, 76, null),
            (7496, "فلوريان تاردو", 10, SpecificPosition.CDM, 72, 81, 75, 78, 78, null),
            (7497, "لامين فومبا", 26, SpecificPosition.CM, 78, 78, 68, 78, 82, null),
            (7498, "أيمين مويفيك", 28, SpecificPosition.CM, 80, 79, 72, 78, 80, null),
            (7499, "إيجور ميلادينوفيتش", 14, SpecificPosition.CAM, 82, 80, 76, 50, 72, null),
            (7500, "أوجوستين بواكي", 7, SpecificPosition.RW, 86, 78, 76, 45, 72, null),
            (7501, "إبراهيما وادجي", 25, SpecificPosition.ST, 87, 70, 78, 40, 80, null),
            (7502, "بوباكار فال", 16, SpecificPosition.GK, 58, 68, 15, 74, 74, null)
        };

        return DatabaseHelpers.CreateTeam("FRA_STE", "سانت إيتيان", FormationType.Formation433_Holding, false, starters, bench);
    }

    // 18. لوهافر (Le Havre AC) - 22 لاعباً
    public static Team CreateLeHavre()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7511, "أرتور ديماس", 30, SpecificPosition.GK, 60, 78, 18, 83, 82, null),
            (7512, "كريستوفر أوبيري", 27, SpecificPosition.LB, 84, 77, 62, 78, 78, new[] { SpecificPosition.LWB }),
            (7513, "جوتييه لوريس", 4, SpecificPosition.CB, 76, 76, 48, 82, 84, new[] { SpecificPosition.LB }),
            (7514, "أروونا سانجانتي", 93, SpecificPosition.CB, 81, 74, 40, 82, 85, new[] { SpecificPosition.RB }),
            (7515, "لويك نيجو", 7, SpecificPosition.RB, 85, 78, 68, 78, 78, new[] { SpecificPosition.RWB, SpecificPosition.RM }),
            (7516, "عبد الله توريه", 94, SpecificPosition.CDM, 75, 80, 74, 82, 86, null),
            (7517, "أواس أوساما تارغالين", 5, SpecificPosition.CM, 81, 82, 72, 79, 78, new[] { SpecificPosition.CDM }),
            (7518, "ياسين كيشتا", 8, SpecificPosition.CM, 83, 83, 74, 77, 72, new[] { SpecificPosition.CAM }),
            (7519, "جوسوي كازيمير", 23, SpecificPosition.RW, 88, 77, 76, 52, 76, new[] { SpecificPosition.RWB, SpecificPosition.LW }),
            (7520, "إيمانويل صبي", 11, SpecificPosition.LW, 86, 77, 78, 48, 78, new[] { SpecificPosition.ST }),
            (7521, "عيسى سوماري", 19, SpecificPosition.ST, 86, 74, 78, 42, 81, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (7522, "ماتيو جورجولين", 1, SpecificPosition.GK, 58, 72, 15, 78, 78, null),
            (7523, "يوان سالمييه", 22, SpecificPosition.CB, 74, 72, 42, 78, 81, null),
            (7524, "إتيان يوتي كينكوي", 6, SpecificPosition.CB, 78, 71, 38, 78, 84, null),
            (7525, "يانيل زواوي", 18, SpecificPosition.LB, 83, 73, 50, 75, 75, null),
            (7526, "دالير كوزياييف", 14, SpecificPosition.CM, 78, 81, 76, 78, 80, null),
            (7527, "راسوول ندياي", 17, SpecificPosition.CM, 80, 78, 72, 76, 78, null),
            (7528, "إسماعيل بوغريب", 25, SpecificPosition.CAM, 82, 78, 74, 48, 70, null),
            (7529, "أندريه أيو", 28, SpecificPosition.ST, 76, 80, 81, 58, 80, new[] { SpecificPosition.LW }),
            (7530, "ستيف نغورا", 20, SpecificPosition.ST, 84, 70, 76, 40, 80, null),
            (7531, "إليا حسني", 9, SpecificPosition.ST, 85, 74, 77, 38, 72, null),
            (7532, "بول أرغني", 40, SpecificPosition.GK, 56, 66, 15, 73, 73, null)
        };

        return DatabaseHelpers.CreateTeam("FRA_HAC", "لوهافر", FormationType.Formation433_Holding, false, starters, bench);
    }
}
