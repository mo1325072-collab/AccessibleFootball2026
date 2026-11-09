namespace AccessibleFootball2026.Core.Models;

/// <summary>
/// منتخبات القارة الآسيوية (AFC) المعتمدة رسمياً من فيفا لموسم 2026/2027 (14 منتخباً)
/// موثقة بالكامل من واقع Transfermarkt و SofaScore و 365scores
/// بقوائم موسعة (22 إلى 23 لاعباً لكل منتخب)
/// </summary>
public static class AsianNationalTeams
{
    public static List<Team> GetAllNationalTeams()
    {
        return new List<Team>
        {
            CreateSaudiArabia(),
            CreateJapan(),
            CreateSouthKorea(),
            CreateIran(),
            CreateAustralia(),
            CreateQatar(),
            CreateUAE(),
            CreateIraq(),
            CreateJordan(),
            CreateUzbekistan(),
            CreateOman(),
            CreateBahrain(),
            CreateSyria(),
            CreateChina()
        };
    }

    // 1. المملكة العربية السعودية (Saudi Arabia) - 23 لاعباً
    public static Team CreateSaudiArabia()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (14001, "محمد العويس", 21, SpecificPosition.GK, 60, 78, 18, 85, 83, null),
            (14002, "ياسر الشهراني", 13, SpecificPosition.LB, 84, 80, 65, 80, 78, new[] { SpecificPosition.LWB }),
            (14003, "علي البليهي", 5, SpecificPosition.CB, 72, 75, 48, 85, 87, null),
            (14004, "حسان تمبكتي", 17, SpecificPosition.CB, 84, 76, 42, 86, 85, null),
            (14005, "سعود عبد الحميد", 12, SpecificPosition.RB, 91, 80, 65, 83, 84, new[] { SpecificPosition.RWB }),
            (14006, "عبد الله الخيبري", 15, SpecificPosition.CDM, 79, 82, 65, 83, 81, null),
            (14007, "ناصر الدوسري", 8, SpecificPosition.CM, 82, 83, 76, 80, 80, new[] { SpecificPosition.LB, SpecificPosition.CDM }),
            (14008, "محمد كنو", 23, SpecificPosition.CM, 78, 83, 80, 81, 88, null),
            (14009, "سالم الدوسري", 10, SpecificPosition.LW, 87, 86, 86, 60, 78, new[] { SpecificPosition.CAM, SpecificPosition.RW }),
            (14010, "فراس البريكان", 9, SpecificPosition.ST, 85, 76, 85, 52, 82, new[] { SpecificPosition.LW }),
            (14011, "أيمن يحيى", 18, SpecificPosition.RW, 88, 81, 80, 55, 74, new[] { SpecificPosition.LW, SpecificPosition.LB })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (14012, "أحمد الكسار", 1, SpecificPosition.GK, 60, 76, 18, 82, 82, null),
            (14013, "علي لاجامي", 4, SpecificPosition.CB, 80, 74, 40, 83, 84, new[] { SpecificPosition.RB }),
            (14014, "عون السلولي", 3, SpecificPosition.CB, 79, 72, 38, 81, 83, null),
            (14015, "متعب الحربي", 24, SpecificPosition.LB, 88, 78, 62, 79, 77, null),
            (14016, "سلطان الغنام", 2, SpecificPosition.RB, 85, 83, 70, 80, 78, new[] { SpecificPosition.RM }),
            (14017, "مختار علي", 6, SpecificPosition.CDM, 78, 81, 70, 80, 82, null),
            (14018, "فيصل الغامدي", 16, SpecificPosition.CM, 81, 82, 74, 80, 80, null),
            (14019, "مصعب الجوير", 7, SpecificPosition.CM, 80, 85, 81, 74, 76, new[] { SpecificPosition.CAM }),
            (14020, "عبد الرحمن غريب", 20, SpecificPosition.LW, 89, 82, 79, 45, 66, null),
            (14021, "صالح الشهري", 11, SpecificPosition.ST, 80, 75, 83, 48, 83, null),
            (14022, "راغد النجار", 22, SpecificPosition.GK, 58, 72, 15, 78, 78, null),
            (14023, "مروان الصحفي", 19, SpecificPosition.RW, 90, 78, 80, 48, 76, new[] { SpecificPosition.ST })
        };

        return DatabaseHelpers.CreateTeam("NAT_KSA", "السعودية", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 2. اليابان (Japan) - 23 لاعباً
    public static Team CreateJapan()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (14031, "زيون سوزوكي", 1, SpecificPosition.GK, 62, 79, 18, 84, 85, null),
            (14032, "كو إيتاكورا", 4, SpecificPosition.CB, 80, 78, 48, 85, 84, new[] { SpecificPosition.CDM }),
            (14033, "شوغو تانيغوتشي", 3, SpecificPosition.CB, 76, 76, 45, 84, 83, null),
            (14034, "كوكي ماتشيدا", 16, SpecificPosition.CB, 78, 75, 42, 83, 85, new[] { SpecificPosition.LB }),
            (14035, "كاورو ميتوما", 7, SpecificPosition.LWB, 90, 84, 82, 60, 75, new[] { SpecificPosition.LW, SpecificPosition.LM }),
            (14036, "ريتسو دوان", 10, SpecificPosition.RWB, 86, 84, 82, 65, 79, new[] { SpecificPosition.RW, SpecificPosition.RM }),
            (14037, "واتارو إندو", 6, SpecificPosition.CDM, 76, 82, 72, 87, 86, new[] { SpecificPosition.CM }),
            (14038, "هيديماسا موريتا", 5, SpecificPosition.CM, 80, 84, 74, 83, 82, null),
            (14039, "تاكيفوسا كوبو", 20, SpecificPosition.CAM, 89, 86, 83, 52, 68, new[] { SpecificPosition.RW }),
            (14040, "تاكومي مينامينو", 8, SpecificPosition.CAM, 82, 82, 81, 55, 72, new[] { SpecificPosition.ST, SpecificPosition.LW }),
            (14041, "أياسي أويدا", 9, SpecificPosition.ST, 84, 75, 84, 45, 82, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (14042, "كيسوكي أوساكو", 12, SpecificPosition.GK, 58, 74, 15, 80, 80, null),
            (14043, "تسويوشي واتانابي", 22, SpecificPosition.CB, 78, 74, 40, 82, 83, null),
            (14044, "يوكيناري سوغاوارا", 2, SpecificPosition.RB, 85, 81, 70, 79, 78, new[] { SpecificPosition.RWB }),
            (14045, "يوتا ناكاياما", 21, SpecificPosition.LB, 80, 76, 55, 78, 80, new[] { SpecificPosition.CB }),
            (14046, "أو تاناكا", 17, SpecificPosition.CM, 80, 82, 78, 80, 80, null),
            (14047, "ريو هاتاتي", 14, SpecificPosition.CM, 82, 84, 80, 78, 78, new[] { SpecificPosition.CAM }),
            (14048, "كيسوكي ناكامورا", 13, SpecificPosition.LW, 88, 80, 80, 48, 70, null),
            (14049, "جونيا إيتو", 14, SpecificPosition.RW, 89, 83, 80, 52, 74, null),
            (14050, "دايزن مايدا", 11, SpecificPosition.LW, 94, 77, 78, 65, 82, new[] { SpecificPosition.ST }),
            (14051, "كوكي أوغاوا", 19, SpecificPosition.ST, 82, 72, 81, 42, 81, null),
            (14052, "كونوسي تاني", 23, SpecificPosition.GK, 58, 72, 15, 78, 78, null),
            (14053, "هيروكي إيتو", 15, SpecificPosition.CB, 80, 78, 48, 82, 82, new[] { SpecificPosition.LB })
        };

        return DatabaseHelpers.CreateTeam("NAT_JPN", "اليابان", FormationType.Formation343, true, starters, bench);
    }

    // 3. كوريا الجنوبية (South Korea) - 23 لاعباً
    public static Team CreateSouthKorea()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (14061, "جو هيون وو", 21, SpecificPosition.GK, 60, 80, 18, 85, 82, null),
            (14062, "لي ميونغ جاي", 13, SpecificPosition.LB, 83, 78, 65, 79, 78, null),
            (14063, "كيم مين جاي", 4, SpecificPosition.CB, 81, 72, 38, 88, 89, null),
            (14064, "تشو يو مين", 3, SpecificPosition.CB, 78, 74, 40, 82, 84, null),
            (14065, "سول يونغ وو", 22, SpecificPosition.RB, 85, 79, 65, 80, 80, new[] { SpecificPosition.LB }),
            (14066, "بارك يونغ وو", 5, SpecificPosition.CDM, 78, 82, 70, 83, 84, new[] { SpecificPosition.CM }),
            (14067, "هوانغ إن بيوم", 6, SpecificPosition.CM, 82, 86, 78, 81, 80, null),
            (14068, "لي كانغ إن", 18, SpecificPosition.CAM, 83, 87, 81, 52, 69, new[] { SpecificPosition.RW, SpecificPosition.CM }),
            (14069, "سون هيونغ مين", 7, SpecificPosition.LW, 88, 85, 90, 48, 76, new[] { SpecificPosition.ST, SpecificPosition.CF }),
            (14070, "أوه سي هون", 19, SpecificPosition.ST, 80, 72, 82, 45, 87, null),
            (14071, "هوانغ هي تشان", 11, SpecificPosition.RW, 89, 80, 83, 55, 82, new[] { SpecificPosition.ST, SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (14072, "سونغ بوم كيون", 1, SpecificPosition.GK, 58, 74, 15, 80, 81, null),
            (14073, "جونغ سيونغ هيون", 15, SpecificPosition.CB, 76, 72, 40, 80, 83, null),
            (14074, "كيم جو سونغ", 2, SpecificPosition.CB, 78, 73, 38, 79, 81, null),
            (14075, "كيم مون هوان", 23, SpecificPosition.RB, 86, 76, 60, 78, 78, null),
            (14076, "بايك سونغ هو", 8, SpecificPosition.CM, 80, 82, 76, 78, 78, null),
            (14077, "وون دو جاي", 16, SpecificPosition.CDM, 76, 78, 65, 80, 83, new[] { SpecificPosition.CB }),
            (14078, "لي جاي سونغ", 10, SpecificPosition.CAM, 80, 84, 80, 70, 78, new[] { SpecificPosition.CM }),
            (14079, "باي جون هو", 14, SpecificPosition.LW, 85, 81, 78, 48, 70, null),
            (14080, "أوه هيون غيو", 20, SpecificPosition.ST, 85, 73, 82, 45, 84, null),
            (14081, "تشو غي سونغ", 9, SpecificPosition.ST, 82, 74, 83, 48, 86, null),
            (14082, "كيم جون هونغ", 12, SpecificPosition.GK, 56, 70, 15, 77, 78, null),
            (14083, "أوم جي سونغ", 17, SpecificPosition.LW, 87, 78, 76, 45, 70, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_KOR", "كوريا الجنوبية", FormationType.Formation4231_Wide, true, starters, bench);
    }

    // 4. إيران (Iran) - 22 لاعباً
    public static Team CreateIran()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (14091, "علي رضا بيرانوند", 1, SpecificPosition.GK, 58, 86, 20, 85, 88, null),
            (14092, "ميلاد محمدي", 5, SpecificPosition.LB, 85, 78, 62, 80, 80, new[] { SpecificPosition.LWB }),
            (14093, "شجاع خليل زاده", 4, SpecificPosition.CB, 74, 73, 45, 84, 85, null),
            (14094, "علي نعمتي", 3, SpecificPosition.CB, 76, 73, 40, 82, 83, new[] { SpecificPosition.LB }),
            (14095, "آريا يوسفي", 2, SpecificPosition.RB, 86, 77, 65, 79, 78, new[] { SpecificPosition.RM }),
            (14096, "سعيد عزت اللهي", 6, SpecificPosition.CDM, 76, 82, 74, 84, 87, new[] { SpecificPosition.CM }),
            (14097, "محمد قرباني", 8, SpecificPosition.CM, 80, 81, 72, 81, 82, null),
            (14098, "سامان قدوس", 14, SpecificPosition.CAM, 82, 85, 81, 68, 77, new[] { SpecificPosition.CM }),
            (14099, "محمد موهيبي", 17, SpecificPosition.RW, 86, 80, 82, 58, 82, new[] { SpecificPosition.LW }),
            (14100, "مهدي طارمي", 9, SpecificPosition.ST, 82, 82, 88, 52, 85, new[] { SpecificPosition.CF, SpecificPosition.LW }),
            (14101, "سردار أزمون", 20, SpecificPosition.ST, 84, 79, 87, 45, 82, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (14102, "بايام نيازمند", 12, SpecificPosition.GK, 58, 74, 15, 80, 81, null),
            (14103, "حسين كنعاني زادغان", 13, SpecificPosition.CB, 75, 73, 42, 82, 86, null),
            (14104, "محمد أمين حزباوي", 15, SpecificPosition.CB, 78, 73, 40, 81, 83, null),
            (14105, "أميد نورافكان", 21, SpecificPosition.LB, 80, 80, 74, 80, 82, new[] { SpecificPosition.CDM }),
            (14106, "صالح حرداني", 23, SpecificPosition.RB, 84, 76, 58, 78, 78, null),
            (14107, "روزبه جشمي", 18, SpecificPosition.CDM, 72, 78, 65, 82, 86, new[] { SpecificPosition.CB }),
            (14108, "علي رضا جهانبخش", 7, SpecificPosition.RW, 82, 84, 82, 60, 78, new[] { SpecificPosition.CAM }),
            (14109, "مهدي قائدي", 10, SpecificPosition.LW, 90, 81, 81, 45, 65, null),
            (14110, "شهريار موغانلو", 19, SpecificPosition.ST, 78, 70, 82, 45, 87, null),
            (14111, "علي قليزاده", 11, SpecificPosition.RW, 84, 83, 79, 48, 68, null),
            (14112, "حسين حسيني", 22, SpecificPosition.GK, 58, 72, 15, 78, 78, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_IRN", "إيران", FormationType.Formation442, true, starters, bench);
    }

    // 5. أستراليا (Australia) - 22 لاعباً
    public static Team CreateAustralia()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (14121, "ماثيو رايان", 1, SpecificPosition.GK, 60, 82, 18, 84, 82, null),
            (14122, "عزيز بيهيتش", 16, SpecificPosition.LB, 82, 78, 66, 80, 80, new[] { SpecificPosition.LWB }),
            (14123, "هاري سوتار", 19, SpecificPosition.CB, 68, 74, 50, 86, 93, null),
            (14124, "كاميرون بورغيس", 21, SpecificPosition.CB, 74, 74, 45, 83, 88, null),
            (14125, "لويس ميلر", 3, SpecificPosition.RB, 84, 76, 58, 80, 82, new[] { SpecificPosition.RWB }),
            (14126, "جاكسون إيرفين", 22, SpecificPosition.CM, 80, 83, 81, 83, 86, new[] { SpecificPosition.CDM }),
            (14127, "كيانو باكوس", 17, SpecificPosition.CDM, 80, 81, 70, 82, 82, null),
            (14128, "كونور ميتكالف", 8, SpecificPosition.CM, 80, 82, 77, 79, 81, null),
            (14129, "كريج جودوين", 23, SpecificPosition.LW, 83, 86, 84, 60, 76, new[] { SpecificPosition.LM }),
            (14130, "ميتشيل ديوك", 15, SpecificPosition.ST, 80, 74, 82, 58, 88, null),
            (14131, "مارتن بويل", 6, SpecificPosition.RW, 89, 80, 80, 52, 78, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (14132, "جو غاوتشي", 12, SpecificPosition.GK, 60, 76, 15, 82, 82, null),
            (14133, "كاي رولز", 4, SpecificPosition.CB, 78, 73, 40, 82, 83, new[] { SpecificPosition.LB }),
            (14134, "توماس دينغ", 2, SpecificPosition.CB, 80, 72, 38, 79, 81, new[] { SpecificPosition.RB }),
            (14135, "جوردان بوس", 5, SpecificPosition.LB, 86, 78, 68, 78, 79, new[] { SpecificPosition.LM }),
            (14136, "جيثرو هيرست", 13, SpecificPosition.RB, 84, 75, 55, 77, 78, null),
            (14137, "إيدن أونيل", 14, SpecificPosition.CDM, 78, 80, 72, 80, 82, null),
            (14138, "أيدين هروستيتش", 10, SpecificPosition.CAM, 78, 84, 81, 58, 74, null),
            (14139, "نيستوري إرانكوندا", 7, SpecificPosition.RW, 92, 78, 82, 45, 80, null),
            (14140, "كوسيني ينجي", 11, SpecificPosition.ST, 86, 72, 81, 45, 86, null),
            (14141, "آدم تاغارت", 9, SpecificPosition.ST, 82, 74, 82, 45, 78, null),
            (14142, "بول إيزو", 18, SpecificPosition.GK, 58, 72, 15, 78, 78, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_AUS", "أستراليا", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 6. قطر (Qatar) - 22 لاعباً
    public static Team CreateQatar()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (14151, "مشعل برشم", 22, SpecificPosition.GK, 62, 78, 18, 84, 81, null),
            (14152, "محمد وعد", 4, SpecificPosition.LWB, 84, 80, 68, 80, 80, new[] { SpecificPosition.LB, SpecificPosition.CM }),
            (14153, "لوكاس مينديز", 12, SpecificPosition.CB, 76, 82, 60, 84, 85, new[] { SpecificPosition.LB }),
            (14154, "بوعلام خوخي", 16, SpecificPosition.CB, 74, 81, 65, 84, 84, new[] { SpecificPosition.CDM }),
            (14155, "طارق سلمان", 5, SpecificPosition.CB, 79, 74, 40, 83, 82, null),
            (14156, "بيدرو ميجيل رورو", 2, SpecificPosition.RWB, 83, 78, 62, 82, 84, new[] { SpecificPosition.CB, SpecificPosition.RB }),
            (14157, "جاسم جابر", 20, SpecificPosition.CDM, 80, 81, 68, 82, 83, new[] { SpecificPosition.CB }),
            (14158, "أحمد فتحي", 6, SpecificPosition.CDM, 78, 81, 68, 83, 83, null),
            (14159, "حسن الهيدوس", 10, SpecificPosition.CAM, 78, 88, 84, 55, 72, new[] { SpecificPosition.RW, SpecificPosition.CM }),
            (14160, "المعز علي", 19, SpecificPosition.ST, 86, 78, 86, 55, 83, null),
            (14161, "أكرم عفيف", 11, SpecificPosition.LW, 88, 90, 87, 52, 75, new[] { SpecificPosition.CAM, SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (14162, "سعد الشيب", 1, SpecificPosition.GK, 58, 76, 15, 81, 80, null),
            (14163, "المهدي علي", 3, SpecificPosition.CB, 75, 72, 40, 80, 82, null),
            (14164, "بسام الراوي", 15, SpecificPosition.CB, 78, 78, 65, 81, 81, new[] { SpecificPosition.RB }),
            (14165, "همام الأمين", 14, SpecificPosition.LB, 85, 78, 62, 78, 80, null),
            (14166, "مصعب خضر", 13, SpecificPosition.RB, 84, 75, 55, 78, 78, null),
            (14167, "عبد العزيز حاتم", 8, SpecificPosition.CM, 78, 83, 80, 78, 78, null),
            (14168, "مصطفى طارق مشعل", 23, SpecificPosition.CM, 80, 81, 75, 78, 78, null),
            (14169, "إدميلسون جونيور", 17, SpecificPosition.LW, 87, 82, 82, 48, 74, null),
            (14170, "يوسف عبد الرزاق", 7, SpecificPosition.RW, 88, 78, 78, 50, 76, null),
            (14171, "أحمد علاء الدين", 9, SpecificPosition.ST, 83, 72, 80, 45, 82, null),
            (14172, "صلاح زكريا", 21, SpecificPosition.GK, 58, 72, 15, 78, 78, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_QAT", "قطر", FormationType.Formation352, true, starters, bench);
    }

    // 7. الإمارات (UAE) - 22 لاعباً
    public static Team CreateUAE()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (14181, "خالد عيسى", 17, SpecificPosition.GK, 60, 79, 18, 84, 81, null),
            (14182, "عبد الله إدريس", 3, SpecificPosition.LB, 83, 77, 60, 78, 78, null),
            (14183, "خليفة الحمادي", 12, SpecificPosition.CB, 78, 74, 42, 83, 85, null),
            (14184, "كوامي أوتون", 4, SpecificPosition.CB, 80, 75, 45, 83, 86, null),
            (14185, "خالد إبراهيم", 2, SpecificPosition.RB, 84, 77, 60, 79, 79, null),
            (14186, "يحيى نادر", 6, SpecificPosition.CDM, 80, 81, 72, 82, 82, null),
            (14187, "ماجد راشد", 8, SpecificPosition.CM, 80, 81, 74, 80, 80, null),
            (14188, "طحنون الزعابي", 10, SpecificPosition.CAM, 83, 83, 78, 60, 72, null),
            (14189, "حارب عبد الله", 7, SpecificPosition.LW, 89, 82, 82, 52, 75, new[] { SpecificPosition.RW }),
            (14190, "كايو كانيدو", 11, SpecificPosition.ST, 83, 81, 84, 55, 80, new[] { SpecificPosition.LW, SpecificPosition.RW }),
            (14191, "علي صالح", 9, SpecificPosition.RW, 86, 81, 81, 52, 74, new[] { SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (14192, "علي خصيف", 1, SpecificPosition.GK, 58, 76, 15, 81, 80, null),
            (14193, "محمد العطاس", 5, SpecificPosition.CB, 77, 74, 45, 81, 82, new[] { SpecificPosition.CDM }),
            (14194, "خالد الهاشمي", 13, SpecificPosition.CB, 76, 73, 40, 80, 82, null),
            (14195, "بندر الأحبابي", 15, SpecificPosition.RB, 86, 83, 72, 78, 76, new[] { SpecificPosition.RW }),
            (14196, "علي سالمين", 18, SpecificPosition.CDM, 76, 80, 68, 81, 82, null),
            (14197, "عبد الله رمضان", 14, SpecificPosition.CM, 78, 84, 76, 76, 74, null),
            (14198, "فابيو ليما", 19, SpecificPosition.RW, 82, 86, 85, 52, 76, new[] { SpecificPosition.CAM }),
            (14199, "يحيى الغساني", 20, SpecificPosition.LW, 88, 80, 80, 48, 74, null),
            (14200, "سلطان عادل", 23, SpecificPosition.ST, 84, 72, 80, 45, 82, null),
            (14201, "برونو أوليفيرا", 16, SpecificPosition.ST, 84, 74, 80, 42, 78, null),
            (14202, "حمد المقبالي", 22, SpecificPosition.GK, 56, 68, 15, 75, 75, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_UAE", "الإمارات", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 8. العراق (Iraq) - 22 لاعباً
    public static Team CreateIraq()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (14211, "جلال حسن", 12, SpecificPosition.GK, 60, 78, 18, 83, 82, null),
            (14212, "ميرخاس دوسكي", 3, SpecificPosition.LB, 85, 78, 62, 80, 80, new[] { SpecificPosition.LWB }),
            (14213, "ريبين سولاقا", 2, SpecificPosition.CB, 72, 74, 45, 83, 87, null),
            (14214, "مناف يونس", 6, SpecificPosition.CB, 76, 73, 40, 82, 85, null),
            (14215, "حسين علي", 4, SpecificPosition.RB, 85, 78, 64, 80, 80, new[] { SpecificPosition.RWB }),
            (14216, "أمير العماري", 16, SpecificPosition.CM, 80, 86, 78, 81, 81, new[] { SpecificPosition.CDM }),
            (14217, "أسامة رشيد", 8, SpecificPosition.CM, 76, 82, 75, 80, 80, null),
            (14218, "زيدان إقبال", 14, SpecificPosition.CAM, 83, 87, 80, 65, 74, new[] { SpecificPosition.CM }),
            (14219, "علي جاسم", 17, SpecificPosition.LW, 88, 84, 83, 50, 74, new[] { SpecificPosition.RW, SpecificPosition.CAM }),
            (14220, "أيمن حسين", 18, SpecificPosition.ST, 82, 75, 87, 55, 90, null),
            (14221, "إبراهيم بايش", 20, SpecificPosition.RW, 87, 82, 81, 68, 83, new[] { SpecificPosition.RM, SpecificPosition.CAM })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (14222, "فهد طالب", 1, SpecificPosition.GK, 58, 74, 15, 80, 80, null),
            (14223, "سعد ناطق", 4, SpecificPosition.CB, 72, 73, 45, 81, 86, null),
            (14224, "زيد تحسين", 15, SpecificPosition.CB, 78, 72, 40, 81, 84, null),
            (14225, "أحمد يحيى", 23, SpecificPosition.LB, 83, 76, 60, 77, 77, null),
            (14226, "مصطفى سعدون", 22, SpecificPosition.RB, 84, 76, 58, 78, 78, null),
            (14227, "أمجد عطوان", 11, SpecificPosition.CM, 78, 82, 78, 80, 82, null),
            (14228, "بشار رسن", 13, SpecificPosition.CAM, 81, 84, 79, 58, 72, null),
            (14229, "يوسف أمين", 7, SpecificPosition.LW, 88, 80, 78, 48, 72, null),
            (14230, "مهند علي ميمي", 10, SpecificPosition.ST, 85, 76, 84, 48, 82, null),
            (14231, "علي الحمادي", 9, SpecificPosition.ST, 85, 74, 82, 48, 85, null),
            (14232, "أحمد باسل", 21, SpecificPosition.GK, 58, 72, 15, 78, 78, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_IRQ", "العراق", FormationType.Formation4231_Wide, true, starters, bench);
    }

    // 9. الأردن (Jordan) - 22 لاعباً
    public static Team CreateJordan()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (14241, "يزيد أبو ليلى", 1, SpecificPosition.GK, 60, 79, 18, 84, 83, null),
            (14242, "محمد أبو حشيش", 2, SpecificPosition.LWB, 84, 77, 62, 79, 78, new[] { SpecificPosition.LB }),
            (14243, "يزن العرب", 5, SpecificPosition.CB, 76, 75, 48, 85, 88, null),
            (14244, "عبد الله نصيب ديارا", 3, SpecificPosition.CB, 78, 74, 42, 84, 86, null),
            (14245, "حسام أبو دهب", 19, SpecificPosition.CB, 76, 72, 40, 81, 84, null),
            (14246, "إحسان حداد", 23, SpecificPosition.RWB, 84, 80, 68, 81, 82, new[] { SpecificPosition.RB }),
            (14247, "نور الروابدة", 8, SpecificPosition.CDM, 80, 82, 74, 83, 83, new[] { SpecificPosition.CM }),
            (14248, "نزار الرشدان", 14, SpecificPosition.CM, 80, 81, 78, 82, 84, new[] { SpecificPosition.CDM }),
            (14249, "علي علوان", 9, SpecificPosition.LW, 87, 82, 84, 55, 82, new[] { SpecificPosition.ST }),
            (14250, "يزن النعيمات", 11, SpecificPosition.ST, 88, 78, 86, 52, 83, null),
            (14251, "موسى التعمري", 10, SpecificPosition.RW, 90, 84, 86, 52, 78, new[] { SpecificPosition.ST, SpecificPosition.CAM })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (14252, "عبد الله الفاخوري", 12, SpecificPosition.GK, 58, 74, 15, 80, 80, null),
            (14253, "سعد الروسان", 4, SpecificPosition.CB, 76, 71, 38, 79, 82, null),
            (14254, "فراس شلباية", 16, SpecificPosition.RB, 83, 78, 62, 78, 78, null),
            (14255, "براء مرعي", 21, SpecificPosition.CB, 75, 72, 40, 79, 81, null),
            (14256, "رجائي عايد", 6, SpecificPosition.CDM, 76, 81, 70, 80, 82, null),
            (14257, "إبراهيم سعادة", 18, SpecificPosition.CM, 80, 82, 76, 78, 78, null),
            (14258, "يوسف أبو جلبوش صاصا", 15, SpecificPosition.CAM, 82, 82, 78, 50, 72, null),
            (14259, "محمود مرضي", 13, SpecificPosition.LW, 84, 82, 82, 55, 76, new[] { SpecificPosition.CAM }),
            (14260, "محمد أبو زريق شرارة", 7, SpecificPosition.RW, 87, 80, 79, 45, 70, null),
            (14261, "عبد الله العطار", 20, SpecificPosition.ST, 82, 70, 78, 42, 82, null),
            (14262, "أحمد جعيدي", 22, SpecificPosition.GK, 56, 68, 15, 75, 75, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_JOR", "الأردن", FormationType.Formation343, true, starters, bench);
    }

    // 10. أوزبكستان (Uzbekistan) - 22 لاعباً
    public static Team CreateUzbekistan()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (14271, "أوتكير يوسوبوف", 1, SpecificPosition.GK, 60, 78, 18, 83, 82, null),
            (14272, "شيرزاد نصر الله ييف", 13, SpecificPosition.LWB, 84, 78, 65, 79, 79, new[] { SpecificPosition.LB }),
            (14273, "عبد القادر خودسانوف", 4, SpecificPosition.CB, 81, 74, 40, 84, 86, null),
            (14274, "رستم أشورماتوف", 3, SpecificPosition.CB, 76, 74, 45, 83, 85, null),
            (14275, "حسني الدين أليكولوف", 23, SpecificPosition.CB, 78, 73, 40, 82, 85, null),
            (14276, "حاجي أكبر عليجونوف", 2, SpecificPosition.RWB, 85, 79, 68, 79, 80, new[] { SpecificPosition.RB }),
            (14277, "أوديلجون هامروبيكوف", 7, SpecificPosition.CDM, 78, 82, 70, 83, 84, null),
            (14278, "أوتابيك شوكوروف", 9, SpecificPosition.CM, 80, 84, 80, 82, 83, null),
            (14279, "جلال الدين ماشاريبوف", 10, SpecificPosition.LW, 85, 85, 82, 55, 74, new[] { SpecificPosition.CAM, SpecificPosition.RW }),
            (14280, "إلدور شومورودوف", 14, SpecificPosition.ST, 84, 78, 85, 52, 86, null),
            (14281, "عباسبيك فايزولاييف", 22, SpecificPosition.RW, 88, 86, 82, 50, 68, new[] { SpecificPosition.CAM, SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (14282, "عبد الواحد نعماتوف", 16, SpecificPosition.GK, 58, 74, 15, 80, 81, null),
            (14283, "فاروق سايفيف", 5, SpecificPosition.LB, 82, 77, 60, 78, 78, new[] { SpecificPosition.RB }),
            (14284, "محمد كودير هامرالييف", 18, SpecificPosition.CB, 78, 71, 38, 79, 82, null),
            (14285, "زافارمورود عبد الرحماتوف", 15, SpecificPosition.RB, 84, 76, 58, 77, 76, null),
            (14286, "عبد الرؤوف بورييف", 6, SpecificPosition.CDM, 80, 80, 72, 79, 80, null),
            (14287, "جمشيد اسكندروف", 8, SpecificPosition.CAM, 78, 83, 79, 52, 70, null),
            (14288, "ديور خولماتوف", 17, SpecificPosition.CM, 80, 81, 74, 77, 76, null),
            (14289, "خوجيمات إركينوف", 19, SpecificPosition.LW, 88, 78, 78, 48, 72, null),
            (14290, "حسين نورشاييف", 21, SpecificPosition.ST, 83, 72, 79, 42, 80, null),
            (14291, "بوبور عبد الخالقوف", 11, SpecificPosition.ST, 82, 73, 81, 45, 82, null),
            (14292, "بوتيرالي إيرغاشيف", 12, SpecificPosition.GK, 56, 70, 15, 77, 78, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_UZB", "أوزبكستان", FormationType.Formation343, true, starters, bench);
    }

    // 11. عُمان (Oman) - 22 لاعباً
    public static Team CreateOman()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (14301, "إبراهيم المخيني", 1, SpecificPosition.GK, 60, 78, 18, 83, 82, null),
            (14302, "أحمد الكعبي", 17, SpecificPosition.LB, 83, 78, 65, 79, 78, null),
            (14303, "محمد المسلمي", 5, SpecificPosition.CB, 74, 75, 45, 83, 85, null),
            (14304, "أحمد الخميسي", 6, SpecificPosition.CB, 76, 73, 40, 82, 85, null),
            (14305, "عبد العزيز الشموسي", 2, SpecificPosition.RB, 84, 76, 58, 78, 78, null),
            (14306, "حارب السعدي", 23, SpecificPosition.CDM, 78, 83, 70, 84, 84, new[] { SpecificPosition.CM }),
            (14307, "أرشد العلوي", 10, SpecificPosition.CM, 82, 83, 78, 79, 82, new[] { SpecificPosition.CAM }),
            (14308, "عبد الله فواز", 4, SpecificPosition.CM, 80, 82, 76, 78, 79, null),
            (14309, "صلاح اليحيائي", 20, SpecificPosition.CAM, 85, 85, 82, 55, 70, new[] { SpecificPosition.LW, SpecificPosition.RW }),
            (14310, "عصام الصبحي", 7, SpecificPosition.ST, 85, 74, 82, 48, 82, null),
            (14311, "محسن الغساني", 11, SpecificPosition.ST, 84, 75, 83, 48, 83, new[] { SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (14312, "فايز الرشيدي", 18, SpecificPosition.GK, 58, 74, 15, 80, 80, null),
            (14313, "خالد البريكي", 16, SpecificPosition.CB, 76, 72, 40, 80, 83, null),
            (14314, "غانم الحبشي", 3, SpecificPosition.CB, 75, 71, 38, 79, 81, null),
            (14315, "علي البوسعيدي", 14, SpecificPosition.LB, 81, 76, 62, 77, 76, null),
            (14316, "أمجد الحارثي", 13, SpecificPosition.RB, 85, 76, 60, 77, 78, null),
            (14317, "مصعب المعمري", 8, SpecificPosition.CM, 80, 80, 74, 76, 76, null),
            (14318, "تميم البلوشي", 19, SpecificPosition.CAM, 82, 80, 76, 50, 72, null),
            (14319, "زاهر الأغبري", 15, SpecificPosition.LW, 86, 79, 76, 50, 72, null),
            (14320, "عمر المالكي", 9, SpecificPosition.ST, 82, 72, 80, 45, 82, null),
            (14321, "جميل اليحمدي", 21, SpecificPosition.RW, 87, 80, 78, 62, 78, new[] { SpecificPosition.RB }),
            (14322, "بلال البلوشي", 22, SpecificPosition.GK, 56, 68, 15, 75, 75, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_OMA", "عُمان", FormationType.Formation41212_Diamond, true, starters, bench);
    }

    // 12. البحرين (Bahrain) - 22 لاعباً
    public static Team CreateBahrain()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (14331, "إبراهيم لطف الله", 22, SpecificPosition.GK, 60, 78, 18, 83, 82, null),
            (14332, "عبد الله الخلاصي", 18, SpecificPosition.LB, 84, 77, 62, 79, 78, null),
            (14333, "وليد الحيام", 3, SpecificPosition.CB, 74, 75, 45, 83, 84, new[] { SpecificPosition.LB }),
            (14334, "أمين بنعدي", 2, SpecificPosition.CB, 76, 73, 40, 82, 85, null),
            (14335, "فينسنت إيمانويل", 17, SpecificPosition.RB, 85, 76, 58, 79, 80, null),
            (14336, "عباس العصفور", 14, SpecificPosition.CDM, 80, 81, 70, 82, 82, null),
            (14337, "سيد ضياء سعيد", 4, SpecificPosition.CM, 81, 82, 76, 81, 80, new[] { SpecificPosition.LB }),
            (14338, "كميل الأسود", 10, SpecificPosition.CAM, 82, 85, 83, 62, 76, new[] { SpecificPosition.CM, SpecificPosition.RW }),
            (14339, "محمد مرهون", 8, SpecificPosition.LW, 86, 84, 84, 55, 78, new[] { SpecificPosition.CAM }),
            (14340, "مهدي عبد الجبار", 9, SpecificPosition.ST, 82, 74, 83, 48, 84, null),
            (14341, "علي مدن", 7, SpecificPosition.RW, 88, 84, 82, 52, 74, new[] { SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (14342, "سيد محمد جعفر", 1, SpecificPosition.GK, 58, 76, 15, 80, 80, null),
            (14343, "حمد الشمسان", 5, SpecificPosition.CB, 76, 72, 40, 80, 83, null),
            (14344, "سيد مهدي باقر", 16, SpecificPosition.CB, 76, 71, 38, 79, 82, null),
            (14345, "هزاع علي", 23, SpecificPosition.LB, 82, 75, 58, 76, 76, null),
            (14346, "محمد عادل", 6, SpecificPosition.RB, 83, 75, 56, 77, 77, null),
            (14347, "موسيس أتيدي", 15, SpecificPosition.CDM, 78, 79, 68, 80, 84, null),
            (14348, "إبراهيم الختال", 11, SpecificPosition.CAM, 82, 80, 76, 50, 74, null),
            (14349, "مهدي حميدان", 20, SpecificPosition.RW, 85, 78, 78, 55, 76, null),
            (14350, "عبد الله يوسف هلال", 13, SpecificPosition.ST, 78, 74, 82, 52, 89, null),
            (14351, "إسماعيل عبد اللطيف", 19, SpecificPosition.ST, 80, 75, 82, 45, 76, null),
            (14352, "عمار محمد", 21, SpecificPosition.GK, 56, 68, 15, 75, 75, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_BHR", "البحرين", FormationType.Formation4231_Wide, true, starters, bench);
    }

    // 13. سوريا (Syria) - 22 لاعباً
    public static Team CreateSyria()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (14361, "أحمد مدنية", 22, SpecificPosition.GK, 60, 78, 18, 83, 82, null),
            (14362, "مؤيد العجان", 3, SpecificPosition.LB, 82, 79, 72, 79, 80, new[] { SpecificPosition.LWB }),
            (14363, "ثائر كروما", 2, SpecificPosition.CB, 74, 76, 50, 83, 85, new[] { SpecificPosition.CDM }),
            (14364, "أيهم أوسو", 13, SpecificPosition.CB, 80, 76, 42, 84, 85, null),
            (14365, "عبد الرحمن ويس", 24, SpecificPosition.RB, 85, 78, 62, 80, 80, new[] { SpecificPosition.RWB }),
            (14366, "إيزيكيل هام", 18, SpecificPosition.CM, 80, 83, 76, 81, 80, new[] { SpecificPosition.CDM }),
            (14367, "خليل إلياس", 4, SpecificPosition.CDM, 81, 83, 72, 84, 83, new[] { SpecificPosition.CM }),
            (14368, "عمار رمضان", 12, SpecificPosition.LW, 87, 82, 80, 52, 74, new[] { SpecificPosition.CAM }),
            (14369, "محمود المواس", 10, SpecificPosition.RW, 86, 83, 82, 58, 78, new[] { SpecificPosition.CAM, SpecificPosition.ST }),
            (14370, "عمر خربين", 7, SpecificPosition.ST, 83, 81, 87, 50, 84, null),
            (14371, "بابلو صباغ", 21, SpecificPosition.ST, 84, 75, 84, 52, 87, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (14372, "إبراهيم عالمة", 1, SpecificPosition.GK, 58, 74, 15, 80, 81, null),
            (14373, "عمرو جنيات", 15, SpecificPosition.RB, 82, 76, 60, 77, 78, null),
            (14374, "عمرو ميداني", 5, SpecificPosition.CB, 74, 71, 40, 79, 83, null),
            (14375, "خالد كردغلي", 16, SpecificPosition.LB, 83, 74, 55, 76, 76, null),
            (14376, "إلمار أبراهام", 6, SpecificPosition.CDM, 78, 79, 68, 80, 81, null),
            (14377, "فهد اليوسف", 8, SpecificPosition.CM, 80, 80, 74, 78, 78, null),
            (14378, "إبراهيم هيسار", 25, SpecificPosition.LW, 85, 81, 80, 55, 78, null),
            (14379, "محمد الحلاق", 11, SpecificPosition.RW, 86, 78, 76, 45, 70, null),
            (14380, "عمر السومة", 9, SpecificPosition.ST, 75, 78, 88, 50, 88, null),
            (14381, "علاء الدالي", 19, SpecificPosition.ST, 83, 72, 79, 42, 80, null),
            (14382, "طه موسى", 23, SpecificPosition.GK, 56, 68, 15, 75, 75, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_SYR", "سوريا", FormationType.Formation442, true, starters, bench);
    }

    // 14. الصين (China) - 22 لاعباً
    public static Team CreateChina()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (14391, "يان جونلينغ", 1, SpecificPosition.GK, 60, 78, 18, 83, 82, null),
            (14392, "لي لي", 4, SpecificPosition.LB, 83, 76, 60, 79, 79, null),
            (14393, "تشو تشينجي", 5, SpecificPosition.CB, 78, 74, 40, 83, 85, null),
            (14394, "جيانغ جوانغتاي براونينغ", 2, SpecificPosition.CB, 80, 73, 40, 84, 87, null),
            (14395, "قاو تشون يي", 3, SpecificPosition.RB, 83, 76, 58, 80, 81, new[] { SpecificPosition.CB }),
            (14396, "وانغ شانغيوان", 6, SpecificPosition.CDM, 78, 81, 72, 82, 83, null),
            (14397, "لي يوانيي", 21, SpecificPosition.CM, 80, 82, 76, 79, 81, null),
            (14398, "شي بينغفي", 20, SpecificPosition.CAM, 82, 83, 79, 58, 74, null),
            (14399, "وو لي", 7, SpecificPosition.RW, 88, 81, 85, 52, 75, new[] { SpecificPosition.ST }),
            (14400, "تشانغ يونينغ", 9, SpecificPosition.ST, 80, 76, 84, 52, 86, null),
            (14401, "وي شيه هاو", 11, SpecificPosition.LW, 87, 82, 82, 52, 74, new[] { SpecificPosition.RW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (14402, "وانغ دالاي", 14, SpecificPosition.GK, 58, 76, 18, 81, 82, null),
            (14403, "جيانغ شينغلونغ", 16, SpecificPosition.CB, 76, 72, 40, 81, 84, null),
            (14404, "تشانغ لينبينغ", 55, SpecificPosition.CB, 74, 74, 50, 80, 83, new[] { SpecificPosition.RB }),
            (14405, "ليو يانغ", 19, SpecificPosition.LB, 84, 76, 62, 77, 77, null),
            (14406, "يانغ تسيشيانغ", 15, SpecificPosition.RB, 83, 74, 55, 76, 77, null),
            (14407, "هوانغ تشنغ يو", 8, SpecificPosition.CDM, 78, 78, 65, 80, 81, null),
            (14408, "تشو تشنجييه", 13, SpecificPosition.CM, 80, 80, 74, 77, 78, null),
            (14409, "لين ليانغمينغ", 17, SpecificPosition.LW, 86, 78, 78, 50, 76, null),
            (14410, "فيرناندينيو", 18, SpecificPosition.RW, 86, 80, 80, 48, 75, null),
            (14411, "بيهرام عبد الولي", 23, SpecificPosition.ST, 84, 71, 79, 45, 84, null),
            (14412, "باو ياشيونغ", 12, SpecificPosition.GK, 56, 70, 15, 77, 77, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_CHN", "الصين", FormationType.Formation442, true, starters, bench);
    }
}
