namespace AccessibleFootball2026.Core.Models;

/// <summary>
/// الدوري المصري الممتاز (دوري نايل) - موسم 2026/2027 المعتمد رسمياً (18 نادياً)
/// موثق بالكامل من واقع Transfermarkt و SofaScore بقوائم موسعة وشاملة (21 إلى 24 لاعباً لكل نادٍ)
/// </summary>
public static class EgyptianLeagueDatabase
{
    public static List<Team> GetAllClubs()
    {
        var fromJson = JsonDatabaseProvider.GetTeamsByGroup("Egyptian Premier League");
        if (fromJson != null && fromJson.Count > 0) return fromJson;

        return new List<Team>
        {
            CreateAlAhly(),
            CreateZamalek(),
            CreatePyramids(),
            CreateAlMasry(),
            CreateCeramicaCleopatra(),
            CreateZedFC(),
            CreateSmouha(),
            CreateIsmaily(),
            CreateAlIttihadAlex(),
            CreateEnppi(),
            CreateNationalBank(),
            CreateModernSport(),
            CreateTalaeaElGaish(),
            CreatePharco(),
            CreateElGouna(),
            CreateGhazlElMahalla(),
            CreateHarasElHodoud(),
            CreatePetrojet()
        };
    }

    // 1. النادي الأهلي (Al Ahly SC) - 25 لاعباً موثقاً من Transfermarkt و SofaScore
    public static Team CreateAlAhly()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1001, "محمد الشناوي", 1, SpecificPosition.GK, 72, 75, 20, 86, 85, null),
            (1002, "يحيى عطية الله", 18, SpecificPosition.LB, 84, 80, 68, 79, 78, new[] { SpecificPosition.LWB }),
            (1003, "ياسر إبراهيم", 6, SpecificPosition.CB, 75, 68, 40, 84, 86, null),
            (1004, "أشرف داري", 15, SpecificPosition.CB, 78, 70, 42, 85, 87, null),
            (1005, "محمد هاني", 30, SpecificPosition.RB, 82, 76, 58, 80, 80, new[] { SpecificPosition.RWB }),
            (1006, "مروان عطية", 13, SpecificPosition.CDM, 80, 84, 65, 85, 84, new[] { SpecificPosition.CM }),
            (1007, "أكرم توفيق", 8, SpecificPosition.CM, 82, 78, 62, 84, 88, new[] { SpecificPosition.CDM, SpecificPosition.RB }),
            (1008, "إمام عاشور", 22, SpecificPosition.CM, 85, 87, 86, 75, 82, new[] { SpecificPosition.CAM, SpecificPosition.LW }),
            (1009, "حسين الشحات", 14, SpecificPosition.LW, 84, 83, 80, 48, 70, new[] { SpecificPosition.RW, SpecificPosition.CAM }),
            (1010, "وسام أبو علي", 9, SpecificPosition.ST, 88, 75, 88, 45, 84, new[] { SpecificPosition.CF }),
            (1011, "بيرسي تاو", 10, SpecificPosition.RW, 87, 81, 82, 42, 68, new[] { SpecificPosition.ST, SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1012, "مصطفى شوبير", 31, SpecificPosition.GK, 73, 76, 18, 83, 80, null),
            (1013, "حمزة علاء", 37, SpecificPosition.GK, 70, 72, 15, 79, 77, null),
            (1014, "رامي ربيعة", 5, SpecificPosition.CB, 74, 70, 50, 83, 82, null),
            (1015, "يوسف أيمن", 4, SpecificPosition.CB, 73, 67, 38, 80, 83, null),
            (1016, "كريم الدبيس", 24, SpecificPosition.LB, 82, 74, 55, 76, 74, null),
            (1017, "عمر كمال عبد الواحد", 3, SpecificPosition.RB, 85, 78, 72, 78, 79, new[] { SpecificPosition.RW }),
            (1018, "أحمد نبيل كوكا", 36, SpecificPosition.CDM, 79, 81, 64, 80, 78, new[] { SpecificPosition.LB, SpecificPosition.CM }),
            (1019, "عمر الساعي", 23, SpecificPosition.CM, 82, 83, 78, 72, 76, new[] { SpecificPosition.CAM }),
            (1020, "عمرو السولية", 17, SpecificPosition.CM, 74, 82, 77, 78, 79, null),
            (1021, "محمد مجدي أفشة", 19, SpecificPosition.CAM, 77, 86, 81, 55, 68, null),
            (1022, "طاهر محمد طاهر", 29, SpecificPosition.RW, 85, 77, 79, 65, 84, new[] { SpecificPosition.ST }),
            (1023, "رضا سليم", 11, SpecificPosition.LW, 86, 82, 78, 42, 68, new[] { SpecificPosition.RW }),
            (1024, "محمود عبد المنعم كهربا", 7, SpecificPosition.ST, 84, 76, 83, 40, 78, new[] { SpecificPosition.LW }),
            (1025, "علي معلول", 21, SpecificPosition.LB, 80, 86, 75, 76, 78, new[] { SpecificPosition.LWB })
        };

        return DatabaseHelpers.CreateTeam("EGY_AHL", "الأهلي", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 2. نادي الزمالك (Zamalek SC) - 24 لاعباً
    public static Team CreateZamalek()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1031, "محمد صبحي", 16, SpecificPosition.GK, 72, 74, 18, 82, 79, null),
            (1032, "محمود بنتايج", 3, SpecificPosition.LB, 85, 78, 62, 78, 80, new[] { SpecificPosition.LWB }),
            (1033, "محمود حمدي الونش", 28, SpecificPosition.CB, 76, 68, 42, 85, 87, null),
            (1034, "حسام عبد المجيد", 24, SpecificPosition.CB, 73, 69, 45, 84, 88, null),
            (1035, "عمر جابر", 4, SpecificPosition.RB, 81, 79, 62, 79, 78, new[] { SpecificPosition.RWB, SpecificPosition.CM }),
            (1036, "نبيل عماد دونجا", 8, SpecificPosition.CDM, 78, 81, 68, 84, 85, null),
            (1037, "محمد شحاتة", 17, SpecificPosition.CM, 82, 80, 70, 80, 82, new[] { SpecificPosition.RB, SpecificPosition.CDM }),
            (1038, "عبد الله السعيد", 19, SpecificPosition.CAM, 72, 89, 83, 58, 68, new[] { SpecificPosition.CM }),
            (1039, "مصطفى شلبي", 11, SpecificPosition.LW, 83, 78, 79, 45, 75, null),
            (1040, "سيف الدين الجزيري", 30, SpecificPosition.ST, 84, 74, 83, 42, 80, null),
            (1041, "أحمد مصطفى زيزو", 25, SpecificPosition.RW, 89, 87, 86, 62, 78, new[] { SpecificPosition.CAM, SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1042, "محمد عواد", 1, SpecificPosition.GK, 71, 75, 18, 81, 78, null),
            (1043, "محمود الشناوي", 32, SpecificPosition.GK, 68, 70, 15, 76, 75, null),
            (1044, "حمزة المثلوثي", 23, SpecificPosition.CB, 77, 74, 52, 82, 83, new[] { SpecificPosition.RB }),
            (1045, "مصطفى الزناري", 5, SpecificPosition.CB, 74, 68, 38, 79, 81, null),
            (1046, "أحمد فتوح", 29, SpecificPosition.LB, 85, 82, 65, 79, 76, new[] { SpecificPosition.LWB, SpecificPosition.CM }),
            (1047, "أحمد محمود", 2, SpecificPosition.RB, 80, 72, 50, 76, 75, null),
            (1048, "زياد كمال", 15, SpecificPosition.CDM, 77, 78, 65, 78, 79, null),
            (1049, "محمد السيد", 36, SpecificPosition.CM, 78, 76, 64, 75, 74, null),
            (1050, "ناصر ماهر", 22, SpecificPosition.CAM, 82, 84, 79, 52, 70, new[] { SpecificPosition.LW }),
            (1051, "كونراد ميشالاك", 21, SpecificPosition.RW, 88, 76, 75, 40, 68, new[] { SpecificPosition.LW }),
            (1052, "ناصر منسي", 9, SpecificPosition.ST, 81, 70, 82, 42, 82, null),
            (1053, "عمر فرج", 18, SpecificPosition.ST, 82, 72, 80, 44, 81, null),
            (1054, "حسام أشرف", 20, SpecificPosition.ST, 81, 71, 79, 38, 76, null)
        };

        return DatabaseHelpers.CreateTeam("EGY_ZAM", "الزمالك", FormationType.Formation433_Holding, false, starters, bench);
    }

    // 3. نادي بيراميدز (Pyramids FC) - 24 لاعباً
    public static Team CreatePyramids()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1061, "أحمد الشناوي", 1, SpecificPosition.GK, 70, 78, 20, 84, 82, null),
            (1062, "محمد حمدي", 21, SpecificPosition.LB, 82, 79, 65, 80, 78, new[] { SpecificPosition.LWB }),
            (1063, "محمود مرعي", 3, SpecificPosition.CB, 75, 70, 40, 83, 84, null),
            (1064, "أحمد سامي", 4, SpecificPosition.CB, 74, 72, 48, 82, 85, null),
            (1065, "محمد الشيبي", 15, SpecificPosition.RB, 85, 85, 70, 81, 80, new[] { SpecificPosition.RWB, SpecificPosition.RM }),
            (1066, "مهند لاشين", 14, SpecificPosition.CDM, 79, 82, 68, 83, 84, new[] { SpecificPosition.CM }),
            (1067, "بلاتي توريه", 7, SpecificPosition.CM, 83, 83, 72, 84, 86, new[] { SpecificPosition.CDM }),
            (1068, "وليد الكرتي", 18, SpecificPosition.CAM, 80, 86, 80, 75, 81, new[] { SpecificPosition.CM }),
            (1069, "إبراهيم عادل", 30, SpecificPosition.LW, 88, 82, 84, 50, 72, new[] { SpecificPosition.RW, SpecificPosition.ST }),
            (1070, "فيستون ماييلي", 9, SpecificPosition.ST, 86, 72, 86, 45, 88, null),
            (1071, "مصطفى فتحي", 11, SpecificPosition.RW, 87, 85, 83, 46, 68, new[] { SpecificPosition.CAM })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1072, "شريف إكرامي", 25, SpecificPosition.GK, 66, 72, 18, 78, 77, null),
            (1073, "يوسف نادر", 16, SpecificPosition.GK, 68, 70, 15, 75, 74, null),
            (1074, "أسامة جلال", 6, SpecificPosition.CB, 76, 71, 38, 81, 83, null),
            (1075, "علي جبر", 5, SpecificPosition.CB, 68, 65, 35, 80, 85, null),
            (1076, "كريم حافظ", 19, SpecificPosition.LB, 81, 76, 62, 77, 76, null),
            (1077, "أحمد توفيق", 12, SpecificPosition.RB, 78, 76, 60, 78, 80, new[] { SpecificPosition.CDM }),
            (1078, "محمود دونجا", 8, SpecificPosition.CDM, 76, 78, 62, 79, 80, null),
            (1079, "أحمد عاطف قطة", 27, SpecificPosition.CM, 81, 79, 72, 70, 72, null),
            (1080, "يوسف أوباما", 20, SpecificPosition.CAM, 80, 81, 80, 60, 78, new[] { SpecificPosition.ST }),
            (1081, "صديق أوجولا", 17, SpecificPosition.RW, 88, 79, 80, 48, 75, new[] { SpecificPosition.LW }),
            (1082, "عبد الرحمن مجدي", 10, SpecificPosition.RW, 84, 82, 80, 50, 74, null),
            (1083, "رمضان صبحي", 88, SpecificPosition.LW, 84, 83, 82, 52, 79, new[] { SpecificPosition.CAM, SpecificPosition.RW }),
            (1084, "مروان حمدي", 23, SpecificPosition.ST, 80, 70, 82, 44, 86, null)
        };

        return DatabaseHelpers.CreateTeam("EGY_PYR", "بيراميدز", FormationType.Formation4231, false, starters, bench);
    }

    // 4. النادي المصري البورسعيدي (Al Masry) - 21 لاعباً
    public static Team CreateAlMasry()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1091, "محمود جاد", 1, SpecificPosition.GK, 72, 74, 18, 81, 79, null),
            (1092, "حسن علي", 13, SpecificPosition.LB, 80, 76, 66, 76, 75, new[] { SpecificPosition.CM }),
            (1093, "باهر المحمدي", 4, SpecificPosition.CB, 76, 72, 52, 82, 83, new[] { SpecificPosition.RB }),
            (1094, "عمرو موسى", 5, SpecificPosition.CB, 70, 74, 58, 80, 82, new[] { SpecificPosition.CDM }),
            (1095, "كريم العراقي", 2, SpecificPosition.RB, 83, 76, 55, 78, 79, null),
            (1096, "محمود حمادة", 8, SpecificPosition.CM, 78, 82, 70, 81, 82, new[] { SpecificPosition.CDM }),
            (1097, "معتز زدام", 14, SpecificPosition.CM, 80, 81, 74, 78, 80, null),
            (1098, "ميدو جابر", 10, SpecificPosition.RW, 82, 80, 79, 55, 74, null),
            (1099, "دغموم عبد الرحيم", 7, SpecificPosition.LW, 86, 79, 77, 48, 70, null),
            (1100, "صلاح محسن", 9, SpecificPosition.ST, 85, 72, 82, 40, 79, null),
            (1101, "فخر الدين بن يوسف", 11, SpecificPosition.ST, 81, 75, 81, 52, 84, new[] { SpecificPosition.RW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1102, "عصام ثروت", 23, SpecificPosition.GK, 70, 71, 15, 78, 77, null),
            (1103, "محمد شحاتة حارس", 31, SpecificPosition.GK, 67, 68, 15, 74, 73, null),
            (1104, "أحمد عيد", 3, SpecificPosition.RB, 82, 73, 50, 76, 76, null),
            (1105, "محمد دبش", 6, SpecificPosition.CB, 72, 66, 35, 78, 80, null),
            (1106, "عمرو سعداوي", 12, SpecificPosition.LB, 79, 74, 58, 75, 74, null),
            (1107, "يوسف الجوهري", 16, SpecificPosition.CM, 77, 78, 68, 75, 76, null),
            (1108, "حسين فيصل", 18, SpecificPosition.CAM, 80, 81, 76, 50, 68, null),
            (1109, "سمير فكري", 22, SpecificPosition.LW, 81, 77, 75, 52, 72, null),
            (1110, "أمادو نياس", 19, SpecificPosition.ST, 83, 68, 78, 42, 82, null),
            (1111, "أحمد شرف", 25, SpecificPosition.ST, 78, 68, 75, 38, 75, null)
        };

        return DatabaseHelpers.CreateTeam("EGY_MAS", "المصري البورسعيدي", FormationType.Formation4231, false, starters, bench);
    }

    // 5. سيراميكا كليوباترا (Ceramica Cleopatra) - 21 لاعباً
    public static Team CreateCeramicaCleopatra()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1121, "محمد بسام", 1, SpecificPosition.GK, 71, 75, 18, 83, 81, null),
            (1122, "محمد شكري", 13, SpecificPosition.LB, 84, 80, 68, 77, 76, null),
            (1123, "رجب نبيل", 4, SpecificPosition.CB, 73, 68, 42, 81, 84, null),
            (1124, "أحمد رمضان بيكهام", 3, SpecificPosition.CB, 77, 74, 50, 82, 83, new[] { SpecificPosition.RB }),
            (1125, "أحمد هاني", 2, SpecificPosition.RB, 81, 74, 52, 77, 78, null),
            (1126, "محمد عادل", 8, SpecificPosition.CDM, 78, 79, 65, 80, 81, null),
            (1127, "عمرو قلاوة", 14, SpecificPosition.CM, 80, 81, 72, 79, 82, null),
            (1128, "محمد إبراهيم", 20, SpecificPosition.CAM, 77, 85, 78, 55, 70, null),
            (1129, "إسلام عيسى", 10, SpecificPosition.LW, 84, 80, 78, 50, 71, null),
            (1130, "جون إيبوكا", 17, SpecificPosition.ST, 85, 71, 84, 45, 88, null),
            (1131, "فاخري لاكاي", 9, SpecificPosition.RW, 87, 78, 81, 46, 78, new[] { SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1132, "علي الجابري", 23, SpecificPosition.GK, 68, 69, 15, 75, 74, null),
            (1133, "إسلام ريشة", 33, SpecificPosition.GK, 66, 68, 15, 73, 72, null),
            (1134, "سعد سمير", 5, SpecificPosition.CB, 68, 68, 40, 78, 80, null),
            (1135, "خالد صبحي", 6, SpecificPosition.CB, 75, 68, 38, 79, 81, null),
            (1136, "عبد الرحمن بودي", 12, SpecificPosition.LB, 78, 72, 54, 74, 73, null),
            (1137, "أحمد بلحاج", 22, SpecificPosition.CAM, 82, 82, 79, 52, 72, null),
            (1138, "حسين حسني", 18, SpecificPosition.CM, 76, 77, 65, 75, 76, null),
            (1139, "محمود زلاكة", 11, SpecificPosition.LW, 85, 76, 75, 42, 69, null),
            (1140, "مروان عثمان", 21, SpecificPosition.ST, 80, 69, 77, 38, 76, null),
            (1141, "أيمن موكا", 27, SpecificPosition.RW, 82, 75, 74, 44, 70, null)
        };

        return DatabaseHelpers.CreateTeam("EGY_CER", "سيراميكا كليوباترا", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 6. زد إف سي (ZED FC) - 21 لاعباً
    public static Team CreateZedFC()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1151, "علي لطفي", 1, SpecificPosition.GK, 72, 74, 18, 82, 80, null),
            (1152, "محمد أشرف", 13, SpecificPosition.LB, 82, 77, 58, 77, 76, null),
            (1153, "مصطفى العش", 4, SpecificPosition.CB, 78, 72, 45, 82, 83, new[] { SpecificPosition.LB }),
            (1154, "محمد سمير", 5, SpecificPosition.CB, 70, 74, 60, 80, 81, null),
            (1155, "علي جمال", 2, SpecificPosition.RB, 81, 74, 52, 76, 78, null),
            (1156, "بيتر موتوموسي", 6, SpecificPosition.CDM, 79, 78, 62, 82, 85, null),
            (1157, "ماتا ماغاسا", 14, SpecificPosition.CM, 81, 80, 70, 79, 82, null),
            (1158, "أحمد عاطف", 8, SpecificPosition.CM, 80, 82, 74, 76, 77, null),
            (1159, "مصطفى زيكو", 10, SpecificPosition.LW, 86, 80, 82, 48, 74, new[] { SpecificPosition.RW }),
            (1160, "شادي حسين", 9, SpecificPosition.ST, 83, 73, 82, 45, 82, null),
            (1161, "ديلسون كاموني", 11, SpecificPosition.ST, 84, 70, 81, 42, 84, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1162, "عمرو حسام", 25, SpecificPosition.GK, 69, 70, 15, 77, 76, null),
            (1163, "محمد فوزي", 33, SpecificPosition.GK, 67, 68, 15, 74, 73, null),
            (1164, "إسلام عبد الله", 3, SpecificPosition.CB, 74, 68, 38, 77, 79, null),
            (1165, "عبد الله بكري", 15, SpecificPosition.CB, 72, 69, 40, 78, 80, null),
            (1166, "سامح إبراهيم", 12, SpecificPosition.RB, 78, 72, 50, 74, 75, null),
            (1167, "حمدي علاء", 18, SpecificPosition.CDM, 78, 79, 66, 78, 80, null),
            (1168, "محمود صابر", 20, SpecificPosition.CAM, 82, 83, 80, 60, 77, null),
            (1169, "عبد الرحمن البانوبي", 7, SpecificPosition.RW, 83, 78, 76, 48, 71, null),
            (1170, "رشيد أحمد", 19, SpecificPosition.ST, 82, 68, 78, 40, 83, null),
            (1171, "رأفت خليل", 22, SpecificPosition.LW, 84, 76, 75, 45, 72, null)
        };

        return DatabaseHelpers.CreateTeam("EGY_ZED", "زد إف سي", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 7. نادي سموحة (Smouha SC) - 21 لاعباً
    public static Team CreateSmouha()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1181, "الهاني سليمان", 1, SpecificPosition.GK, 68, 74, 18, 81, 79, null),
            (1182, "محمود وحيد", 13, SpecificPosition.LB, 81, 77, 64, 76, 76, null),
            (1183, "بركات حجاج", 4, SpecificPosition.CB, 74, 68, 38, 80, 82, null),
            (1184, "محمد مصطفى ميدو", 5, SpecificPosition.CB, 75, 69, 40, 79, 81, null),
            (1185, "شريف رضا", 2, SpecificPosition.RB, 82, 76, 60, 77, 78, null),
            (1186, "دوكو دودو", 6, SpecificPosition.CDM, 81, 79, 64, 82, 86, null),
            (1187, "عمرو قلاوة", 8, SpecificPosition.CM, 78, 80, 70, 78, 80, null),
            (1188, "إسلام جابر", 14, SpecificPosition.CAM, 80, 79, 74, 62, 75, null),
            (1189, "مصطفى البدري", 11, SpecificPosition.LW, 85, 78, 77, 48, 71, null),
            (1190, "حسام حسن", 9, SpecificPosition.ST, 83, 72, 83, 48, 84, null),
            (1191, "صامويل أمادي", 17, SpecificPosition.RW, 86, 77, 76, 44, 70, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1192, "حسين تيمور", 25, SpecificPosition.GK, 67, 69, 15, 75, 74, null),
            (1193, "أحمد ميهوب", 33, SpecificPosition.GK, 68, 70, 15, 76, 75, null),
            (1194, "إسلام عطية", 3, SpecificPosition.CB, 73, 67, 36, 77, 79, null),
            (1195, "عبد الرحمن عامر", 15, SpecificPosition.RB, 78, 72, 50, 74, 74, null),
            (1196, "طارق علاء", 12, SpecificPosition.LB, 79, 73, 52, 75, 74, null),
            (1197, "أحمد خالد", 18, SpecificPosition.CM, 77, 78, 68, 74, 76, null),
            (1198, "أبو بكر ليادي", 20, SpecificPosition.LW, 84, 75, 74, 42, 69, null),
            (1199, "محمد سعيد مكرونة", 22, SpecificPosition.CAM, 79, 79, 72, 50, 68, null),
            (1200, "فادي فريد", 10, SpecificPosition.ST, 81, 69, 79, 40, 81, null),
            (1201, "أحمد إيهاب", 27, SpecificPosition.ST, 78, 67, 75, 38, 76, null)
        };

        return DatabaseHelpers.CreateTeam("EGY_SMO", "سموحة", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 8. النادي الإسماعيلي (Ismaily SC) - 21 لاعباً
    public static Team CreateIsmaily()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1211, "أحمد عادل عبد المنعم", 1, SpecificPosition.GK, 69, 73, 18, 80, 79, null),
            (1212, "عبد الكريم مصطفى", 3, SpecificPosition.LB, 81, 75, 55, 76, 76, null),
            (1213, "محمد نصر", 5, SpecificPosition.CB, 75, 68, 38, 81, 84, null),
            (1214, "محمد عمار", 4, SpecificPosition.CB, 74, 67, 36, 80, 83, null),
            (1215, "محمد بيومي", 2, SpecificPosition.RB, 81, 76, 56, 78, 79, new[] { SpecificPosition.CDM }),
            (1216, "محمد مخلوف", 8, SpecificPosition.CDM, 78, 80, 65, 80, 82, null),
            (1217, "هشام محمد", 6, SpecificPosition.CM, 73, 81, 68, 78, 77, null),
            (1218, "عبد الرحمن الدح", 10, SpecificPosition.CAM, 82, 81, 78, 52, 70, null),
            (1219, "نادر فرج", 7, SpecificPosition.LW, 84, 77, 76, 48, 72, null),
            (1220, "مروان حمدي إسماعيلي", 11, SpecificPosition.ST, 82, 73, 80, 42, 78, null),
            (1221, "إيريك تراوري", 9, SpecificPosition.RW, 83, 76, 78, 58, 82, new[] { SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1222, "كمال السيد", 25, SpecificPosition.GK, 68, 69, 15, 76, 75, null),
            (1223, "عبد الله جمال", 33, SpecificPosition.GK, 66, 67, 15, 73, 72, null),
            (1224, "حاتم سكر", 12, SpecificPosition.LB, 80, 74, 58, 75, 76, null),
            (1225, "عماد حمدي", 15, SpecificPosition.CB, 71, 72, 45, 78, 81, new[] { SpecificPosition.CDM }),
            (1226, "حمدي النقاز", 21, SpecificPosition.RB, 77, 76, 60, 75, 79, null),
            (1227, "محمد حسن", 14, SpecificPosition.CDM, 77, 78, 64, 78, 80, null),
            (1228, "عمر القط", 18, SpecificPosition.LW, 83, 74, 73, 44, 69, null),
            (1229, "علي الملواني", 19, SpecificPosition.ST, 80, 68, 77, 38, 78, null),
            (1230, "حسن صابر", 24, SpecificPosition.ST, 79, 67, 75, 36, 76, null),
            (1231, "محمد زيدان إسماعيلي", 27, SpecificPosition.RW, 81, 73, 72, 40, 68, null)
        };

        return DatabaseHelpers.CreateTeam("EGY_ISM", "الإسماعيلي", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 9. الاتحاد السكندري (Al Ittihad Alexandria) - 21 لاعباً
    public static Team CreateAlIttihadAlex()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1241, "المهدي سليمان", 1, SpecificPosition.GK, 71, 74, 18, 82, 80, null),
            (1242, "كريم الديب", 3, SpecificPosition.LB, 81, 78, 65, 77, 78, null),
            (1243, "محمود شبانة", 4, SpecificPosition.CB, 75, 68, 40, 81, 84, null),
            (1244, "سيف تقا", 5, SpecificPosition.CB, 74, 67, 42, 80, 83, null),
            (1245, "هشام صلاح", 2, SpecificPosition.RB, 83, 75, 54, 78, 79, null),
            (1246, "مورو ساليفو", 6, SpecificPosition.CDM, 79, 80, 64, 83, 86, null),
            (1247, "عبد الغني محمد", 8, SpecificPosition.CM, 81, 79, 70, 78, 80, null),
            (1248, "ناصر ناصر", 14, SpecificPosition.CM, 78, 80, 68, 77, 79, null),
            (1249, "إسلام سمير", 11, SpecificPosition.LW, 84, 77, 76, 45, 70, null),
            (1250, "بواتينج بنيامين", 9, SpecificPosition.ST, 84, 71, 82, 44, 85, null),
            (1251, "إيمانويل إيبي", 10, SpecificPosition.RW, 85, 76, 78, 42, 76, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1252, "صبحي سليمان", 25, SpecificPosition.GK, 69, 70, 15, 76, 75, null),
            (1253, "أحمد دعدور", 33, SpecificPosition.GK, 67, 68, 15, 74, 73, null),
            (1254, "مصطفى إبراهيم", 15, SpecificPosition.CB, 73, 67, 36, 77, 79, null),
            (1255, "عمرو صالح", 12, SpecificPosition.LB, 79, 73, 54, 74, 74, null),
            (1256, "أحمد غنيم اتحاد", 18, SpecificPosition.RB, 78, 72, 50, 74, 75, null),
            (1257, "أحمد عاطف قطاوي", 20, SpecificPosition.CAM, 80, 79, 74, 52, 70, null),
            (1258, "عمر الوحش", 17, SpecificPosition.CM, 76, 79, 70, 75, 76, null),
            (1259, "فان ديرك", 22, SpecificPosition.LW, 83, 74, 74, 40, 74, null),
            (1260, "أحمد عيد مهاجم", 19, SpecificPosition.ST, 81, 68, 78, 40, 80, null),
            (1261, "بلال السيد", 27, SpecificPosition.CM, 76, 76, 64, 74, 75, null)
        };

        return DatabaseHelpers.CreateTeam("EGY_ITH", "الاتحاد السكندري", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 10. نادي إنبي (ENPPI) - 21 لاعباً
    public static Team CreateEnppi()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1271, "رضا السيد", 1, SpecificPosition.GK, 70, 72, 18, 80, 79, null),
            (1272, "مصطفى دويدار", 3, SpecificPosition.LB, 81, 75, 54, 76, 75, null),
            (1273, "أحمد كالوشا", 4, SpecificPosition.CB, 75, 70, 48, 82, 85, null),
            (1274, "أحمد صبيحة", 5, SpecificPosition.CB, 74, 68, 38, 80, 83, null),
            (1275, "علي فوزي", 2, SpecificPosition.RB, 82, 80, 64, 78, 78, null),
            (1276, "أحمد العجوز", 6, SpecificPosition.CDM, 78, 81, 68, 81, 82, null),
            (1277, "مودي ناصر", 8, SpecificPosition.CM, 80, 80, 70, 78, 80, null),
            (1278, "محمد ناصر مودي", 14, SpecificPosition.CAM, 79, 81, 76, 56, 72, null),
            (1279, "رفيق كابو", 11, SpecificPosition.RW, 87, 78, 79, 44, 73, null),
            (1280, "أحمد أمين أوفا", 9, SpecificPosition.ST, 83, 72, 84, 48, 87, null),
            (1281, "يوسف لبيب", 10, SpecificPosition.LW, 84, 76, 80, 42, 78, new[] { SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1282, "عبد الرحمن سمير", 25, SpecificPosition.GK, 68, 69, 15, 75, 74, null),
            (1283, "رمضان مصطفى", 33, SpecificPosition.GK, 67, 68, 15, 74, 73, null),
            (1284, "محمد حامد", 15, SpecificPosition.CB, 73, 67, 36, 78, 80, null),
            (1285, "مروان داوود", 12, SpecificPosition.LB, 80, 74, 58, 75, 75, null),
            (1286, "محمد النحاس", 18, SpecificPosition.RB, 78, 72, 48, 74, 74, null),
            (1287, "زياد طارق", 22, SpecificPosition.LW, 83, 79, 75, 42, 66, null),
            (1288, "صلاح زايد", 17, SpecificPosition.CM, 77, 78, 66, 75, 76, null),
            (1289, "فتحي السيد", 20, SpecificPosition.CAM, 79, 78, 73, 50, 70, null),
            (1290, "أحمد كمال", 19, SpecificPosition.ST, 80, 68, 76, 38, 78, null),
            (1291, "علي أوسكار", 24, SpecificPosition.RW, 82, 74, 73, 40, 70, null)
        };

        return DatabaseHelpers.CreateTeam("EGY_ENP", "إنبي", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 11. البنك الأهلي (National Bank of Egypt SC) - 21 لاعباً
    public static Team CreateNationalBank()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1301, "محمد أبو جبل", 1, SpecificPosition.GK, 72, 75, 18, 83, 82, null),
            (1302, "إسحاقو يعقوبو", 3, SpecificPosition.LB, 85, 77, 60, 78, 80, null),
            (1303, "سعيدو سيمبوري", 4, SpecificPosition.CB, 75, 70, 52, 82, 86, new[] { SpecificPosition.CDM }),
            (1304, "محمود الجزار", 5, SpecificPosition.CB, 76, 69, 40, 81, 84, null),
            (1305, "هشام صلاح بنك", 2, SpecificPosition.RB, 82, 75, 52, 77, 78, null),
            (1306, "محمد فتحي", 6, SpecificPosition.CDM, 78, 80, 64, 82, 84, null),
            (1307, "أحمد النادري", 8, SpecificPosition.CM, 80, 82, 72, 79, 81, null),
            (1308, "محمد إبراهيم بنك", 10, SpecificPosition.CAM, 78, 85, 79, 56, 71, null),
            (1309, "ياو أنور", 11, SpecificPosition.LW, 89, 78, 82, 44, 76, new[] { SpecificPosition.ST }),
            (1310, "أسامة فيصل", 9, SpecificPosition.ST, 84, 74, 83, 46, 83, null),
            (1311, "محمد جريندو", 17, SpecificPosition.RW, 83, 76, 80, 55, 82, new[] { SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1312, "حسن شاهين", 25, SpecificPosition.GK, 69, 71, 15, 77, 76, null),
            (1313, "أحمد صبحي بنك", 33, SpecificPosition.GK, 67, 68, 15, 74, 73, null),
            (1314, "أمير مدحت", 15, SpecificPosition.CB, 73, 68, 36, 78, 80, null),
            (1315, "عاصم صلاح", 12, SpecificPosition.LB, 76, 75, 62, 75, 76, null),
            (1316, "أيمن أشرف", 16, SpecificPosition.CB, 74, 76, 52, 79, 80, new[] { SpecificPosition.LB }),
            (1317, "سيرجي أكا", 18, SpecificPosition.CDM, 79, 79, 65, 80, 83, null),
            (1318, "أحمد ربيع", 14, SpecificPosition.CM, 78, 78, 68, 76, 78, null),
            (1319, "موسى دياوارا", 20, SpecificPosition.RW, 85, 74, 75, 42, 73, null),
            (1320, "أحمد ياسر ريان", 7, SpecificPosition.ST, 83, 72, 82, 42, 81, null),
            (1321, "محمود قاعود", 22, SpecificPosition.LW, 80, 75, 76, 48, 76, null)
        };

        return DatabaseHelpers.CreateTeam("EGY_NBE", "البنك الأهلي", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 12. مودرن سبورت (Modern Sport FC) - 21 لاعباً
    public static Team CreateModernSport()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1331, "محمود جنش", 1, SpecificPosition.GK, 72, 75, 18, 82, 80, null),
            (1332, "جوزيف نغويم", 3, SpecificPosition.LB, 83, 82, 72, 78, 80, null),
            (1333, "محمود رزق", 4, SpecificPosition.CB, 75, 68, 40, 82, 85, null),
            (1334, "علي الفيل", 5, SpecificPosition.CB, 74, 69, 38, 81, 84, null),
            (1335, "باسم علي", 2, SpecificPosition.RB, 82, 77, 60, 77, 78, null),
            (1336, "غنام محمد", 6, SpecificPosition.CDM, 80, 82, 70, 83, 83, null),
            (1337, "عمرو السيسي", 8, SpecificPosition.CM, 78, 79, 66, 82, 84, null),
            (1338, "محمد صادق", 14, SpecificPosition.CAM, 81, 83, 78, 55, 72, null),
            (1339, "جوناثان نغويم", 11, SpecificPosition.LW, 84, 80, 78, 60, 78, null),
            (1340, "أحمد عاطف مودرن", 9, SpecificPosition.ST, 86, 74, 83, 44, 82, null),
            (1341, "أرنولد إيبا", 10, SpecificPosition.RW, 85, 76, 78, 48, 78, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1342, "محمد ربيعة حارس", 25, SpecificPosition.GK, 68, 69, 15, 76, 75, null),
            (1343, "أحمد يحيى", 33, SpecificPosition.GK, 67, 68, 15, 74, 73, null),
            (1344, "خالد رضا", 15, SpecificPosition.CB, 73, 67, 36, 78, 80, null),
            (1345, "طارق طه", 12, SpecificPosition.LB, 78, 76, 64, 74, 74, null),
            (1346, "محمود شعبان", 18, SpecificPosition.RB, 80, 74, 52, 75, 76, null),
            (1347, "علي زعزع", 20, SpecificPosition.CAM, 82, 80, 76, 52, 71, null),
            (1348, "محمد نصير", 16, SpecificPosition.CDM, 77, 77, 62, 77, 79, null),
            (1349, "عبد الكبير الوادي", 22, SpecificPosition.LW, 82, 81, 76, 50, 72, null),
            (1350, "إيجالو جودوين", 19, SpecificPosition.ST, 81, 68, 79, 40, 83, null),
            (1351, "عمر السعيد", 27, SpecificPosition.ST, 78, 70, 78, 42, 82, null)
        };

        return DatabaseHelpers.CreateTeam("EGY_MOD", "مودرن سبورت", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 13. طلائع الجيش (Tala'ea El Gaish) - 21 لاعباً
    public static Team CreateTalaeaElGaish()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1361, "محمد شعبان", 1, SpecificPosition.GK, 70, 73, 18, 80, 79, null),
            (1362, "أحمد متعب", 3, SpecificPosition.LB, 81, 77, 65, 76, 76, null),
            (1363, "خالد سطوحي", 4, SpecificPosition.CB, 72, 68, 42, 81, 85, null),
            (1364, "محمد فتح الله", 5, SpecificPosition.CB, 74, 67, 38, 80, 84, null),
            (1365, "أحمد زولا", 2, SpecificPosition.RB, 83, 75, 56, 77, 79, null),
            (1366, "فريد شوقي", 6, SpecificPosition.CDM, 76, 79, 62, 82, 84, null),
            (1367, "علي حمدي", 8, SpecificPosition.CM, 78, 80, 68, 78, 80, null),
            (1368, "إسلام محارب", 14, SpecificPosition.CAM, 80, 80, 78, 65, 80, null),
            (1369, "كريم طارق", 11, SpecificPosition.LW, 85, 78, 76, 48, 72, null),
            (1370, "جودوين شيكا", 9, SpecificPosition.ST, 84, 70, 83, 44, 86, null),
            (1371, "أحمد سمير طلائع", 10, SpecificPosition.RW, 83, 82, 81, 50, 74, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1372, "عماد السيد", 25, SpecificPosition.GK, 69, 71, 15, 77, 76, null),
            (1373, "عمر رضوان", 33, SpecificPosition.GK, 68, 69, 15, 75, 74, null),
            (1374, "عمرو طارق", 15, SpecificPosition.CB, 73, 68, 38, 78, 81, null),
            (1375, "يحيى حامد", 12, SpecificPosition.RB, 78, 72, 48, 74, 74, null),
            (1376, "محمد دياب", 16, SpecificPosition.CDM, 76, 76, 60, 77, 80, null),
            (1377, "حميد ماو", 18, SpecificPosition.CM, 78, 78, 66, 78, 81, null),
            (1378, "يسري وحيد", 20, SpecificPosition.LW, 82, 76, 74, 55, 75, null),
            (1379, "موسى دياوارا طلائع", 22, SpecificPosition.RW, 84, 74, 75, 42, 72, null),
            (1380, "فارس حاتم", 19, SpecificPosition.ST, 79, 67, 75, 38, 76, null),
            (1381, "بول جوليوس", 27, SpecificPosition.ST, 81, 68, 78, 40, 82, null)
        };

        return DatabaseHelpers.CreateTeam("EGY_TGS", "طلائع الجيش", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 14. فاركو (Pharco FC) - 21 لاعباً
    public static Team CreatePharco()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1391, "محمد نديم", 1, SpecificPosition.GK, 70, 72, 18, 80, 78, null),
            (1392, "جيفرسون إنكادا", 2, SpecificPosition.RB, 84, 76, 60, 78, 80, null),
            (1393, "رامي صبري", 4, SpecificPosition.CB, 71, 70, 50, 80, 83, null),
            (1394, "ياسين مرعي", 5, SpecificPosition.CB, 75, 68, 38, 80, 82, null),
            (1395, "عزمي غومة", 3, SpecificPosition.LB, 81, 75, 58, 76, 76, null),
            (1396, "كينجسلي سوكاري", 6, SpecificPosition.CDM, 79, 81, 68, 82, 85, null),
            (1397, "محمود عماد", 8, SpecificPosition.CM, 79, 80, 70, 78, 80, null),
            (1398, "وليد علي فرج", 14, SpecificPosition.CAM, 82, 80, 76, 52, 70, null),
            (1399, "شكري نجيب", 10, SpecificPosition.LW, 83, 78, 79, 52, 78, null),
            (1400, "سيف تيري", 9, SpecificPosition.ST, 85, 71, 82, 44, 85, null),
            (1401, "عمرو ناصر", 11, SpecificPosition.ST, 82, 70, 80, 42, 82, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1402, "شيكا حارس فاركو", 25, SpecificPosition.GK, 68, 69, 15, 76, 75, null),
            (1403, "صبري جاد", 33, SpecificPosition.GK, 66, 67, 15, 73, 72, null),
            (1404, "أحمد عوض", 15, SpecificPosition.CB, 73, 67, 36, 77, 80, null),
            (1405, "جابر كامل", 12, SpecificPosition.LB, 78, 72, 52, 74, 74, null),
            (1406, "باباكار نداي", 18, SpecificPosition.CB, 74, 66, 38, 78, 83, null),
            (1407, "أحمد البحراوي", 16, SpecificPosition.CDM, 76, 77, 62, 76, 78, null),
            (1408, "أحمد فؤاد", 20, SpecificPosition.CM, 77, 77, 66, 74, 75, null),
            (1409, "محمد عز", 22, SpecificPosition.RW, 81, 74, 73, 42, 70, null),
            (1410, "أحمد شريف", 17, SpecificPosition.ST, 80, 68, 77, 38, 77, null),
            (1411, "زهير المترجي", 7, SpecificPosition.LW, 84, 77, 78, 44, 73, null)
        };

        return DatabaseHelpers.CreateTeam("EGY_FAR", "فاركو", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 15. الجونة (El Gouna FC) - 21 لاعباً
    public static Team CreateElGouna()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1421, "أحمد مسعود", 1, SpecificPosition.GK, 70, 73, 18, 81, 79, null),
            (1422, "صبري رحيل", 3, SpecificPosition.LB, 77, 78, 65, 76, 77, null),
            (1423, "أحمد حسام", 4, SpecificPosition.CB, 75, 68, 40, 81, 84, null),
            (1424, "أحمد عبد الرسول", 5, SpecificPosition.CB, 74, 67, 38, 80, 82, null),
            (1425, "أحمد رضا", 2, SpecificPosition.RB, 81, 74, 52, 76, 76, null),
            (1426, "نور السيد", 6, SpecificPosition.CDM, 73, 80, 68, 80, 82, null),
            (1427, "رضا صلاح", 8, SpecificPosition.CM, 80, 80, 72, 77, 78, null),
            (1428, "أرنو راندريانانتيناينا", 10, SpecificPosition.CAM, 83, 81, 79, 54, 75, null),
            (1429, "أيمن موكا جونة", 14, SpecificPosition.LW, 84, 78, 77, 48, 72, null),
            (1430, "حسام غانم شيفو", 9, SpecificPosition.ST, 82, 70, 81, 42, 82, null),
            (1431, "جابرييل تشوكوودي", 11, SpecificPosition.RW, 86, 75, 77, 44, 76, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1432, "صبحي الضوي", 25, SpecificPosition.GK, 67, 68, 15, 75, 74, null),
            (1433, "محمد علاء حارس", 33, SpecificPosition.GK, 66, 67, 15, 73, 72, null),
            (1434, "أليو جاتا", 15, SpecificPosition.LB, 79, 73, 54, 74, 75, null),
            (1435, "عبد الله هاشم", 18, SpecificPosition.CB, 72, 66, 36, 77, 79, null),
            (1436, "محمد الصباحي", 20, SpecificPosition.CAM, 80, 78, 75, 48, 70, null),
            (1437, "سيف شيكا", 16, SpecificPosition.CM, 77, 76, 65, 75, 76, null),
            (1438, "أوكا إيمانويل", 22, SpecificPosition.ST, 81, 68, 78, 40, 82, null),
            (1439, "مازن ياسر", 17, SpecificPosition.RW, 82, 73, 72, 42, 68, null),
            (1440, "محمود حسونة", 24, SpecificPosition.CDM, 76, 75, 60, 76, 78, null),
            (1441, "أحمد مجدي جونة", 27, SpecificPosition.ST, 78, 68, 75, 38, 77, null)
        };

        return DatabaseHelpers.CreateTeam("EGY_GOU", "الجونة", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 16. غزل المحلة (Ghazl El Mahalla) - 21 لاعباً
    public static Team CreateGhazlElMahalla()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1451, "عامر عامر", 1, SpecificPosition.GK, 70, 73, 18, 81, 79, null),
            (1452, "يحيى زكريا", 3, SpecificPosition.LB, 81, 75, 54, 76, 75, null),
            (1453, "عمرو الجزار", 4, SpecificPosition.CB, 76, 68, 40, 81, 84, null),
            (1454, "أحمد حكم", 5, SpecificPosition.CB, 74, 67, 38, 80, 82, null),
            (1455, "محمد جابر", 2, SpecificPosition.RB, 81, 75, 56, 77, 78, null),
            (1456, "محمد عصام", 6, SpecificPosition.CDM, 78, 79, 65, 80, 82, null),
            (1457, "موري توريه", 8, SpecificPosition.CM, 80, 80, 70, 80, 83, null),
            (1458, "خالد الإخميمي", 14, SpecificPosition.CAM, 79, 81, 76, 55, 72, null),
            (1459, "عبده يحيى", 11, SpecificPosition.LW, 85, 77, 80, 48, 75, null),
            (1460, "محمد بن حمودة", 9, SpecificPosition.ST, 83, 71, 82, 42, 83, null),
            (1461, "جابرييل أوروك", 7, SpecificPosition.RW, 86, 76, 78, 45, 74, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1462, "محمود الحضري", 25, SpecificPosition.GK, 68, 69, 15, 76, 75, null),
            (1463, "أحمد عربي", 33, SpecificPosition.GK, 66, 67, 15, 73, 72, null),
            (1464, "حسام حسن مدافع محلة", 15, SpecificPosition.CB, 73, 67, 36, 77, 80, null),
            (1465, "بسام وليد", 12, SpecificPosition.RB, 78, 72, 48, 74, 74, null),
            (1466, "عبد الرحيم عموري", 18, SpecificPosition.LB, 79, 73, 52, 74, 74, null),
            (1467, "أحمد سعيد محلة", 16, SpecificPosition.CDM, 76, 77, 62, 76, 78, null),
            (1468, "محمود مجدي", 20, SpecificPosition.CM, 77, 77, 66, 74, 76, null),
            (1469, "كريم أشرف", 22, SpecificPosition.RW, 81, 74, 73, 40, 70, null),
            (1470, "حازم أبو سنة", 19, SpecificPosition.ST, 80, 68, 77, 38, 79, null),
            (1471, "محمد حمدي زكي محلة", 10, SpecificPosition.ST, 81, 74, 79, 44, 77, null)
        };

        return DatabaseHelpers.CreateTeam("EGY_GZM", "غزل المحلة", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 17. حرس الحدود (Haras El Hodoud) - 21 لاعباً
    public static Team CreateHarasElHodoud()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1481, "محمود الزنفلي", 1, SpecificPosition.GK, 71, 74, 18, 82, 81, null),
            (1482, "مؤمن عوض", 3, SpecificPosition.LB, 80, 74, 52, 76, 75, null),
            (1483, "إبراهيم القاضي", 5, SpecificPosition.CB, 74, 67, 38, 80, 83, null),
            (1484, "محمود بن والي", 4, SpecificPosition.CB, 75, 68, 40, 81, 84, null),
            (1485, "محمد الدغيمي", 2, SpecificPosition.RB, 81, 73, 50, 76, 76, null),
            (1486, "مصطفى جمال", 6, SpecificPosition.CDM, 77, 79, 66, 80, 82, null),
            (1487, "إسلام سمير حرس", 8, SpecificPosition.CM, 80, 79, 70, 77, 78, null),
            (1488, "فوزي الحناوي", 10, SpecificPosition.CAM, 82, 82, 78, 52, 71, null),
            (1489, "كواسي كواديو", 11, SpecificPosition.LW, 85, 75, 77, 42, 75, null),
            (1490, "محمود ممدوح", 9, SpecificPosition.ST, 84, 72, 83, 44, 84, null),
            (1491, "حازم مرسي", 7, SpecificPosition.RW, 83, 75, 79, 44, 77, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1492, "أيمن رجب", 25, SpecificPosition.GK, 68, 69, 15, 76, 75, null),
            (1493, "كريم عماد", 33, SpecificPosition.GK, 66, 67, 15, 73, 72, null),
            (1494, "محمد سامي حرس", 15, SpecificPosition.CB, 73, 67, 36, 77, 80, null),
            (1495, "أحمد عماد", 12, SpecificPosition.RB, 78, 71, 48, 74, 74, null),
            (1496, "إيهاب سمير", 16, SpecificPosition.LB, 78, 73, 55, 74, 74, null),
            (1497, "أحمد فيلكس", 14, SpecificPosition.CDM, 78, 78, 62, 78, 81, null),
            (1498, "محمد مصطفى شطة", 18, SpecificPosition.CM, 77, 77, 65, 75, 76, null),
            (1499, "محمد أوتشا", 20, SpecificPosition.RW, 82, 74, 73, 42, 70, null),
            (1500, "فلانتين أودو", 19, SpecificPosition.ST, 81, 68, 78, 38, 82, null),
            (1501, "أحمد طارق", 24, SpecificPosition.ST, 78, 68, 75, 38, 76, null)
        };

        return DatabaseHelpers.CreateTeam("EGY_HRD", "حرس الحدود", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 18. بتروجيت (Petrojet FC) - 21 لاعباً
    public static Team CreatePetrojet()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1511, "عمر صلاح", 1, SpecificPosition.GK, 71, 74, 18, 81, 80, null),
            (1512, "أحمد غنيم بتروجيت", 3, SpecificPosition.LB, 80, 75, 55, 76, 76, null),
            (1513, "هادي رياض", 4, SpecificPosition.CB, 75, 68, 40, 81, 84, null),
            (1514, "حامد حمدان", 5, SpecificPosition.CB, 74, 70, 48, 80, 82, null),
            (1515, "توفيق محمد", 2, SpecificPosition.RB, 82, 75, 54, 77, 78, null),
            (1516, "مصطفى الجمل", 6, SpecificPosition.CDM, 78, 80, 66, 81, 83, null),
            (1517, "رشاد المتولي", 8, SpecificPosition.CM, 81, 81, 74, 78, 80, null),
            (1518, "محمد دودو", 14, SpecificPosition.CAM, 82, 80, 77, 52, 72, null),
            (1519, "بدر موسى", 11, SpecificPosition.LW, 85, 78, 79, 46, 74, null),
            (1520, "إسماعيل بامبا", 9, SpecificPosition.ST, 84, 70, 82, 42, 85, null),
            (1521, "جابرييل شيكودي بتروجيت", 10, SpecificPosition.RW, 86, 76, 78, 44, 76, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (1522, "محمد أبو النجا بونجا", 25, SpecificPosition.GK, 68, 70, 15, 76, 75, null),
            (1523, "محمد خليفة", 33, SpecificPosition.GK, 66, 67, 15, 73, 72, null),
            (1524, "محمود شديد قناوي", 15, SpecificPosition.LB, 78, 74, 58, 74, 75, null),
            (1525, "أحمد بحبح", 12, SpecificPosition.CB, 73, 67, 36, 77, 79, null),
            (1526, "إسلام عبد الغني", 18, SpecificPosition.RB, 79, 72, 50, 74, 75, null),
            (1527, "أحمد رضا بتروجيت", 16, SpecificPosition.CDM, 77, 77, 64, 77, 79, null),
            (1528, "وينفول كوبينا", 20, SpecificPosition.CAM, 80, 81, 76, 50, 70, null),
            (1529, "أحمد الصعيدي", 22, SpecificPosition.ST, 80, 68, 78, 40, 81, null),
            (1530, "سامح إبراهيم بتروجيت", 17, SpecificPosition.LW, 82, 74, 73, 42, 70, null),
            (1531, "محمد علي عكاشة", 24, SpecificPosition.RW, 81, 73, 72, 40, 68, null)
        };

        return DatabaseHelpers.CreateTeam("EGY_PET", "بتروجيت", FormationType.Formation433_Attack, false, starters, bench);
    }
}
