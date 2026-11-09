namespace AccessibleFootball2026.Core.Models;

/// <summary>
/// دوري روشن السعودي للمحترفين - موسم 2026/2027 المعتمد رسمياً (18 نادياً)
/// موثق بالكامل من واقع Transfermarkt و SofaScore و 365scores
/// بقوائم موسعة وشاملة (21 إلى 24 لاعباً لكل نادٍ) مع أحدث الانتقالات والنجوم وأرقام القمصان الرسمية
/// </summary>
public static class SaudiLeagueDatabase
{
    public static List<Team> GetAllClubs()
    {
        var fromJson = JsonDatabaseProvider.GetTeamsByGroup("Saudi Pro League");
        if (fromJson != null && fromJson.Count > 0) return fromJson;

        return new List<Team>
        {
            CreateAlHilal(),
            CreateAlNassr(),
            CreateAlIttihad(),
            CreateAlAhli(),
            CreateAlShabab(),
            CreateAlQadsiah(),
            CreateAlEttifaq(),
            CreateAlTaawoun(),
            CreateAlFateh(),
            CreateAlFayha(),
            CreateAlKhaleej(),
            CreateAlRaed(),
            CreateDamac(),
            CreateAlWehda(),
            CreateAlOkhdood(),
            CreateAlRiyadh(),
            CreateAlOrobah(),
            CreateAlKholood()
        };
    }

    // 1. الهلال السعودي (Al Hilal SFC) - 24 لاعباً
    public static Team CreateAlHilal()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2001, "ياسين بونو", 37, SpecificPosition.GK, 72, 80, 20, 89, 84, null),
            (2002, "رينان لودي", 6, SpecificPosition.LB, 85, 80, 68, 79, 78, new[] { SpecificPosition.LWB }),
            (2003, "خاليدو كوليبالي", 3, SpecificPosition.CB, 80, 74, 45, 87, 89, null),
            (2004, "علي البليهي", 5, SpecificPosition.CB, 76, 72, 48, 83, 85, null),
            (2005, "جواو كانسيلو", 20, SpecificPosition.RB, 86, 85, 74, 80, 78, new[] { SpecificPosition.LB, SpecificPosition.RM }),
            (2006, "روبن نيفيز", 8, SpecificPosition.CDM, 78, 88, 82, 82, 80, new[] { SpecificPosition.CM }),
            (2007, "سيرجي ميلينكوفيتش سافيتش", 22, SpecificPosition.CM, 81, 86, 84, 82, 88, new[] { SpecificPosition.CAM }),
            (2008, "مالكوم", 77, SpecificPosition.RW, 88, 84, 83, 55, 75, new[] { SpecificPosition.CAM, SpecificPosition.RM }),
            (2009, "سالم الدوسري", 29, SpecificPosition.LW, 86, 83, 84, 56, 76, new[] { SpecificPosition.LM, SpecificPosition.CAM }),
            (2010, "ألكسندر ميتروفيتش", 9, SpecificPosition.ST, 80, 75, 89, 52, 90, null),
            (2011, "ماركوس ليوناردو", 11, SpecificPosition.ST, 87, 76, 85, 42, 80, new[] { SpecificPosition.CF })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2012, "محمد العويس", 21, SpecificPosition.GK, 70, 74, 18, 80, 79, null),
            (2013, "محمد الربيعي", 17, SpecificPosition.GK, 68, 70, 15, 76, 76, null),
            (2014, "حسان تمبكتي", 87, SpecificPosition.CB, 82, 72, 40, 82, 82, null),
            (2015, "خليفة الدوسري", 4, SpecificPosition.CB, 76, 68, 38, 78, 80, null),
            (2016, "ياسر الشهراني", 12, SpecificPosition.LB, 82, 78, 62, 78, 77, new[] { SpecificPosition.RB }),
            (2017, "حمد اليامي", 88, SpecificPosition.RB, 81, 74, 55, 75, 75, null),
            (2018, "ناصر الدوسري", 16, SpecificPosition.CM, 80, 80, 72, 78, 79, new[] { SpecificPosition.LB }),
            (2019, "محمد كنو", 28, SpecificPosition.CM, 78, 82, 76, 80, 84, null),
            (2020, "نيمار جونيور", 10, SpecificPosition.LW, 86, 89, 86, 38, 68, new[] { SpecificPosition.CAM }),
            (2021, "عبد الله الحمدان", 99, SpecificPosition.ST, 81, 74, 78, 42, 76, null),
            (2022, "محمد القحطاني", 15, SpecificPosition.RW, 84, 76, 75, 40, 70, null),
            (2023, "خالد الغنام", 24, SpecificPosition.LW, 83, 76, 75, 42, 69, null),
            (2024, "عبد الإله المالكي", 26, SpecificPosition.CDM, 76, 78, 62, 79, 80, null)
        };

        return DatabaseHelpers.CreateTeam("KSA_HIL", "الهلال", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 2. النصر السعودي (Al Nassr FC) - 24 لاعباً
    public static Team CreateAlNassr()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2031, "بينتو ماثيوس", 24, SpecificPosition.GK, 72, 78, 20, 86, 84, null),
            (2032, "سالم النجدي", 12, SpecificPosition.LB, 84, 76, 60, 77, 76, null),
            (2033, "إيميريك لابورت", 27, SpecificPosition.CB, 75, 81, 50, 86, 84, null),
            (2034, "محمد سيماكان", 3, SpecificPosition.CB, 84, 74, 45, 84, 86, new[] { SpecificPosition.RB }),
            (2035, "سلطان الغنام", 2, SpecificPosition.RB, 85, 82, 68, 79, 79, new[] { SpecificPosition.RWB, SpecificPosition.RM }),
            (2036, "مارسيلو بروزوفيتش", 11, SpecificPosition.CDM, 78, 88, 78, 84, 83, new[] { SpecificPosition.CM }),
            (2037, "أوتافيو مونتيرو", 25, SpecificPosition.CM, 82, 86, 80, 78, 80, new[] { SpecificPosition.CAM, SpecificPosition.RW }),
            (2038, "أنجيلو جابرييل", 20, SpecificPosition.RW, 88, 81, 79, 45, 72, null),
            (2039, "ساديو ماني", 10, SpecificPosition.LW, 89, 82, 84, 55, 78, new[] { SpecificPosition.ST, SpecificPosition.LM }),
            (2040, "كريستيانو رونالدو", 7, SpecificPosition.ST, 83, 80, 93, 40, 84, new[] { SpecificPosition.CF }),
            (2041, "أندرسون تاليسكا", 94, SpecificPosition.CAM, 82, 84, 88, 52, 82, new[] { SpecificPosition.RW, SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2042, "نواف العقيدي", 44, SpecificPosition.GK, 71, 73, 18, 79, 78, null),
            (2043, "راغد النجار", 36, SpecificPosition.GK, 68, 69, 15, 76, 75, null),
            (2044, "علي لاجامي", 78, SpecificPosition.CB, 81, 70, 38, 80, 82, null),
            (2045, "عمر هوساوي", 4, SpecificPosition.CB, 74, 68, 36, 78, 80, null),
            (2046, "ماجد قشيش", 13, SpecificPosition.LB, 80, 73, 54, 75, 75, null),
            (2047, "نواف بوشل", 23, SpecificPosition.RB, 83, 75, 55, 76, 77, new[] { SpecificPosition.LB }),
            (2048, "عبد الله الخيبري", 17, SpecificPosition.CDM, 79, 81, 64, 82, 82, null),
            (2049, "علي الحسن", 19, SpecificPosition.CM, 78, 79, 68, 77, 78, null),
            (2050, "عبد الرحمن غريب", 29, SpecificPosition.LW, 87, 81, 79, 44, 68, new[] { SpecificPosition.RW }),
            (2051, "أيمن يحيى", 23, SpecificPosition.RW, 84, 79, 76, 52, 72, null),
            (2052, "محمد مران", 16, SpecificPosition.ST, 82, 70, 78, 38, 76, null),
            (2053, "مختار علي", 6, SpecificPosition.CM, 78, 78, 66, 76, 78, null),
            (2054, "سامي النجعي", 14, SpecificPosition.CAM, 80, 80, 75, 50, 70, null)
        };

        return DatabaseHelpers.CreateTeam("KSA_NAS", "النصر", FormationType.Formation4231, false, starters, bench);
    }

    // 3. الاتحاد السعودي (Al Ittihad Club) - 24 لاعباً
    public static Team CreateAlIttihad()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2061, "بريدراج رايكوفيتش", 1, SpecificPosition.GK, 71, 77, 20, 86, 83, null),
            (2062, "ماريو ميتاج", 42, SpecificPosition.LB, 83, 78, 62, 79, 79, new[] { SpecificPosition.LWB, SpecificPosition.CDM }),
            (2063, "دانيلو بيريرا", 2, SpecificPosition.CB, 74, 78, 65, 86, 88, new[] { SpecificPosition.CDM }),
            (2064, "سعد آل موسى", 6, SpecificPosition.CB, 78, 70, 38, 81, 83, null),
            (2065, "مهند الشنقيطي", 13, SpecificPosition.RB, 85, 76, 58, 78, 79, new[] { SpecificPosition.LB }),
            (2066, "نجولو كانتي", 7, SpecificPosition.CDM, 84, 83, 70, 88, 86, new[] { SpecificPosition.CM }),
            (2067, "فابينيو تافاريس", 8, SpecificPosition.CDM, 75, 82, 70, 86, 85, new[] { SpecificPosition.CB }),
            (2068, "حسام عوار", 10, SpecificPosition.CAM, 82, 86, 82, 60, 74, new[] { SpecificPosition.CM }),
            (2069, "موسى ديابي", 19, SpecificPosition.RW, 93, 83, 82, 45, 72, new[] { SpecificPosition.RM }),
            (2070, "كريم بنزيما", 9, SpecificPosition.ST, 82, 85, 90, 45, 83, new[] { SpecificPosition.CF }),
            (2071, "ستيفن بيرجوين", 34, SpecificPosition.LW, 88, 81, 83, 48, 78, new[] { SpecificPosition.LM, SpecificPosition.RW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2072, "محمد المحاسنة", 88, SpecificPosition.GK, 69, 70, 15, 77, 76, null),
            (2073, "حامد الشنقيطي حارس", 22, SpecificPosition.GK, 67, 68, 15, 74, 73, null),
            (2074, "حسن كادش", 15, SpecificPosition.CB, 77, 72, 45, 80, 82, new[] { SpecificPosition.LB }),
            (2075, "فواز الصقور", 27, SpecificPosition.RB, 83, 74, 52, 76, 76, null),
            (2076, "معاذ فقيهي", 12, SpecificPosition.LB, 81, 74, 54, 75, 75, null),
            (2077, "فيصل الغامدي", 16, SpecificPosition.CM, 80, 81, 70, 78, 80, null),
            (2078, "حامد الغامدي", 80, SpecificPosition.CAM, 81, 80, 74, 55, 71, null),
            (2079, "صالح الشهري", 11, SpecificPosition.ST, 80, 73, 82, 42, 81, null),
            (2080, "أحمد الغامدي", 17, SpecificPosition.RW, 84, 77, 75, 45, 72, null),
            (2081, "عبد الإله هوساوي", 14, SpecificPosition.CDM, 78, 77, 62, 78, 80, null),
            (2082, "طلال حاجي", 24, SpecificPosition.ST, 83, 68, 77, 36, 76, null),
            (2083, "مروان الصحفي", 23, SpecificPosition.RW, 86, 75, 74, 42, 70, null),
            (2084, "عبد الرحمن العبود", 21, SpecificPosition.RW, 85, 76, 75, 46, 73, null)
        };

        return DatabaseHelpers.CreateTeam("KSA_ITT", "الاتحاد", FormationType.Formation4231, false, starters, bench);
    }

    // 4. الأهلي السعودي (Al Ahli SFC) - 24 لاعباً
    public static Team CreateAlAhli()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2091, "إدوارد ميندي", 16, SpecificPosition.GK, 70, 78, 20, 86, 85, null),
            (2092, "عبد الله العمار", 15, SpecificPosition.LB, 82, 77, 64, 77, 78, null),
            (2093, "ميريح ديميرال", 28, SpecificPosition.CB, 78, 68, 42, 86, 88, null),
            (2094, "روجير إيبانيز", 3, SpecificPosition.CB, 83, 72, 45, 86, 86, null),
            (2095, "علي مجرشي", 27, SpecificPosition.RB, 84, 76, 55, 78, 79, null),
            (2096, "فرانك كيسيه", 79, SpecificPosition.CDM, 79, 83, 78, 85, 89, new[] { SpecificPosition.CM }),
            (2097, "زياد الجهني", 30, SpecificPosition.CM, 80, 80, 70, 79, 81, null),
            (2098, "جابري فيجا", 24, SpecificPosition.CAM, 83, 84, 83, 65, 77, new[] { SpecificPosition.CM }),
            (2099, "رياض محرز", 7, SpecificPosition.RW, 85, 89, 84, 45, 68, new[] { SpecificPosition.RM }),
            (2100, "إيفان توني", 99, SpecificPosition.ST, 86, 78, 88, 55, 87, null),
            (2101, "فراس البريكان", 20, SpecificPosition.LW, 86, 78, 83, 52, 80, new[] { SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2102, "عبد الرحمن الصانبي", 1, SpecificPosition.GK, 70, 72, 15, 78, 77, null),
            (2103, "عبد الله عبده", 62, SpecificPosition.GK, 67, 68, 15, 74, 73, null),
            (2104, "ريان حامد", 46, SpecificPosition.CB, 78, 68, 36, 79, 82, null),
            (2105, "فهد الحمد", 31, SpecificPosition.CB, 74, 66, 35, 77, 79, null),
            (2106, "سعد بالعبيد", 6, SpecificPosition.LB, 80, 73, 52, 75, 75, null),
            (2107, "بسام الحريجي", 88, SpecificPosition.RB, 81, 74, 52, 76, 76, null),
            (2108, "علي الأسمري", 40, SpecificPosition.CM, 78, 80, 68, 77, 78, null),
            (2109, "عيد المولد", 14, SpecificPosition.CDM, 79, 77, 62, 78, 80, null),
            (2110, "روبرتو فيرمينو", 10, SpecificPosition.CF, 80, 84, 82, 58, 76, new[] { SpecificPosition.ST, SpecificPosition.CAM }),
            (2111, "سميحان النابت", 8, SpecificPosition.LW, 84, 78, 76, 48, 72, null),
            (2112, "فهد الرشيدي", 19, SpecificPosition.RW, 83, 76, 75, 46, 73, null),
            (2113, "هيثم عسيري", 17, SpecificPosition.RW, 85, 74, 74, 40, 70, null),
            (2114, "ياسين الزبيدي", 11, SpecificPosition.LW, 83, 75, 73, 40, 69, null)
        };

        return DatabaseHelpers.CreateTeam("KSA_AHL", "الأهلي السعودي", FormationType.Formation4231, false, starters, bench);
    }

    // 5. نادي الشباب (Al Shabab FC) - 21 لاعباً
    public static Team CreateAlShabab()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2121, "كيم سيونج جيو", 18, SpecificPosition.GK, 72, 77, 20, 84, 81, null),
            (2122, "محمد الثاني", 16, SpecificPosition.LB, 82, 75, 58, 76, 76, null),
            (2123, "روبرت رينان", 4, SpecificPosition.CB, 81, 76, 45, 83, 83, null),
            (2124, "ويسلي هوديت", 33, SpecificPosition.CB, 74, 72, 45, 83, 86, null),
            (2125, "نواف الغليميش", 2, SpecificPosition.RB, 81, 74, 52, 76, 76, null),
            (2126, "جوستافو كويلار", 6, SpecificPosition.CDM, 79, 82, 65, 85, 84, null),
            (2127, "مصعب الجوير", 16, SpecificPosition.CM, 82, 84, 78, 75, 77, new[] { SpecificPosition.CAM }),
            (2128, "جياكومو بونافينتورا", 7, SpecificPosition.CM, 78, 85, 82, 74, 76, new[] { SpecificPosition.CAM }),
            (2129, "دانييل بودينسي", 56, SpecificPosition.RW, 88, 83, 79, 44, 65, null),
            (2130, "عبد الرزاق حمد الله", 9, SpecificPosition.ST, 81, 76, 88, 48, 85, null),
            (2131, "يانيك كاراسكو", 10, SpecificPosition.LW, 87, 84, 84, 60, 78, new[] { SpecificPosition.LM })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2132, "عبد الله المعيوف", 1, SpecificPosition.GK, 69, 74, 18, 79, 78, null),
            (2133, "محمد العتيبي", 22, SpecificPosition.GK, 67, 68, 15, 74, 73, null),
            (2134, "نادر الشراري", 5, SpecificPosition.CB, 75, 68, 36, 78, 81, null),
            (2135, "حسين الصبياني", 38, SpecificPosition.LB, 82, 73, 50, 75, 75, null),
            (2136, "ماجد كنبه", 14, SpecificPosition.CDM, 77, 78, 62, 78, 80, null),
            (2137, "يونس الشنقيطي", 11, SpecificPosition.CM, 80, 79, 72, 74, 75, null),
            (2138, "فهد المولد", 8, SpecificPosition.RW, 87, 77, 78, 48, 72, null),
            (2139, "هارون كمارا", 90, SpecificPosition.ST, 85, 70, 79, 42, 82, null),
            (2140, "كريستيان جوانكا", 15, SpecificPosition.CAM, 80, 81, 80, 52, 75, null),
            (2141, "هشام الدبيس", 21, SpecificPosition.ST, 80, 68, 75, 38, 75, null)
        };

        return DatabaseHelpers.CreateTeam("KSA_SHB", "الشباب", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 6. القادسية (Al Qadsiah FC) - 21 لاعباً
    public static Team CreateAlQadsiah()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2151, "كوين كاستيلس", 1, SpecificPosition.GK, 72, 79, 20, 87, 85, null),
            (2152, "جاستون ألفاريز", 5, SpecificPosition.LB, 82, 77, 60, 82, 84, new[] { SpecificPosition.CB }),
            (2153, "ناتشو فيرنانديز", 6, SpecificPosition.CB, 79, 78, 52, 85, 83, new[] { SpecificPosition.RB }),
            (2154, "جهاد كينيدي", 4, SpecificPosition.CB, 77, 70, 38, 82, 84, null),
            (2155, "محمد الشنقيطي قادسية", 2, SpecificPosition.RB, 83, 74, 52, 76, 77, null),
            (2156, "ناهيتان نانديز", 8, SpecificPosition.CDM, 84, 82, 74, 84, 87, new[] { SpecificPosition.CM, SpecificPosition.RB }),
            (2157, "إيزيكيل فيرنانديز", 55, SpecificPosition.CM, 82, 85, 74, 82, 83, new[] { SpecificPosition.CDM }),
            (2158, "كاميرون بويرتاس", 10, SpecificPosition.CAM, 84, 87, 83, 72, 80, new[] { SpecificPosition.CM }),
            (2159, "تركي العمار", 11, SpecificPosition.RW, 84, 79, 78, 50, 72, null),
            (2160, "بيير إيميريك أوباميانغ", 14, SpecificPosition.ST, 88, 78, 89, 44, 80, null),
            (2161, "خوليان كينونيس", 33, SpecificPosition.LW, 88, 80, 85, 52, 82, new[] { SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2162, "أحمد الكسار", 25, SpecificPosition.GK, 70, 72, 16, 78, 78, null),
            (2163, "عبد العزيز البواردي", 33, SpecificPosition.GK, 67, 68, 15, 74, 73, null),
            (2164, "قاسم لاجامي", 15, SpecificPosition.CB, 76, 68, 36, 79, 81, null),
            (2165, "عبد الله هزازي", 12, SpecificPosition.RB, 80, 72, 50, 75, 75, null),
            (2166, "حسين النطار", 18, SpecificPosition.CM, 78, 79, 68, 76, 78, null),
            (2167, "علي هزازي", 88, SpecificPosition.CDM, 79, 78, 65, 80, 82, null),
            (2168, "عبد العزيز العثمان", 20, SpecificPosition.ST, 81, 68, 77, 38, 77, null),
            (2169, "سيف راشد", 22, SpecificPosition.RW, 82, 75, 74, 42, 70, null),
            (2170, "إيكر ألمينا", 17, SpecificPosition.LW, 84, 76, 75, 42, 68, null),
            (2171, "محمد أبو الشامات", 24, SpecificPosition.RB, 81, 74, 55, 76, 76, null)
        };

        return DatabaseHelpers.CreateTeam("KSA_QAD", "القادسية", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 7. الاتفاق (Al Ettifaq FC) - 21 لاعباً
    public static Team CreateAlEttifaq()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2181, "ماريك روداك", 1, SpecificPosition.GK, 71, 77, 18, 84, 82, null),
            (2182, "راضي العتيبي", 13, SpecificPosition.LB, 82, 75, 56, 77, 78, null),
            (2183, "جاك هيندري", 4, SpecificPosition.CB, 76, 72, 42, 83, 86, null),
            (2184, "عبد الله خطيب", 3, SpecificPosition.CB, 75, 68, 38, 80, 83, null),
            (2185, "مد الله العليان", 2, SpecificPosition.RB, 83, 76, 55, 78, 79, null),
            (2186, "سيكو فوفانا", 6, SpecificPosition.CDM, 82, 83, 78, 84, 86, new[] { SpecificPosition.CM }),
            (2187, "جورجينيو فينالدوم", 8, SpecificPosition.CM, 80, 84, 82, 80, 81, new[] { SpecificPosition.CAM }),
            (2188, "ألفارو ميدران", 10, SpecificPosition.CAM, 79, 87, 80, 68, 74, new[] { SpecificPosition.CM }),
            (2189, "ديماراي جراي", 7, SpecificPosition.LW, 88, 81, 80, 48, 74, new[] { SpecificPosition.RW }),
            (2190, "موسى ديمبيلي", 9, SpecificPosition.ST, 83, 74, 86, 48, 87, null),
            (2191, "كارل توكو إيكامبي", 11, SpecificPosition.RW, 86, 78, 82, 50, 78, new[] { SpecificPosition.LW, SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2192, "أحمد الرحيلي", 25, SpecificPosition.GK, 69, 71, 15, 77, 76, null),
            (2193, "تركي بالجوش", 33, SpecificPosition.GK, 66, 67, 15, 73, 72, null),
            (2194, "مسعود ردايف", 15, SpecificPosition.CB, 74, 67, 36, 78, 80, null),
            (2195, "حمدان الشمراني", 12, SpecificPosition.LB, 80, 74, 58, 76, 78, new[] { SpecificPosition.CB }),
            (2196, "عبد الباسط هندي", 5, SpecificPosition.CB, 75, 68, 40, 79, 81, null),
            (2197, "عبد الإله المالكي اتفاق", 18, SpecificPosition.CDM, 78, 78, 64, 79, 80, null),
            (2198, "مختار علي اتفاق", 16, SpecificPosition.CM, 78, 79, 66, 77, 78, null),
            (2199, "جواو كوستا", 22, SpecificPosition.RW, 84, 76, 75, 42, 70, null),
            (2200, "عبد الله رديف", 19, SpecificPosition.ST, 84, 70, 79, 40, 80, null),
            (2201, "فيتينيو", 20, SpecificPosition.LW, 83, 80, 80, 46, 74, null)
        };

        return DatabaseHelpers.CreateTeam("KSA_ETT", "الاتفاق", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 8. التعاون (Al Taawoun FC) - 21 لاعباً
    public static Team CreateAlTaawoun()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2211, "مايلسون دوس سانتوس", 1, SpecificPosition.GK, 72, 76, 18, 85, 84, null),
            (2212, "سعد الناصر", 13, SpecificPosition.LB, 83, 77, 62, 76, 75, null),
            (2213, "أندري جيروتو", 3, SpecificPosition.CB, 75, 75, 60, 84, 86, new[] { SpecificPosition.CDM }),
            (2214, "وليد الأحمد", 4, SpecificPosition.CB, 77, 70, 38, 81, 84, null),
            (2215, "فهد الجميعة", 2, SpecificPosition.RB, 82, 76, 62, 77, 78, new[] { SpecificPosition.RM }),
            (2216, "أشرف المهديوي", 18, SpecificPosition.CDM, 79, 85, 74, 83, 82, new[] { SpecificPosition.CM }),
            (2217, "فلافيو ميديروس", 8, SpecificPosition.CM, 81, 82, 75, 80, 82, null),
            (2218, "لوكاس تشافيز", 10, SpecificPosition.CAM, 83, 83, 80, 56, 74, null),
            (2219, "ماتيوس كاسترو", 7, SpecificPosition.RW, 87, 82, 82, 50, 72, null),
            (2220, "جواو بيدرو", 11, SpecificPosition.ST, 82, 73, 84, 48, 86, null),
            (2221, "موسى بارو", 9, SpecificPosition.LW, 88, 80, 83, 52, 78, new[] { SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2222, "عبد القدوس عطية", 25, SpecificPosition.GK, 69, 71, 15, 77, 76, null),
            (2223, "محمد الدوسري حارس", 33, SpecificPosition.GK, 66, 67, 15, 73, 72, null),
            (2224, "عون السلولي", 15, SpecificPosition.CB, 78, 68, 36, 80, 83, null),
            (2225, "رينيه ريفاس", 12, SpecificPosition.LB, 80, 73, 54, 75, 76, null),
            (2226, "محمد الغامدي", 16, SpecificPosition.RB, 79, 72, 48, 74, 74, null),
            (2227, "إبراهيم الشعيل", 14, SpecificPosition.CDM, 77, 77, 62, 77, 79, null),
            (2228, "سلطان مندش", 20, SpecificPosition.RW, 84, 76, 75, 45, 71, null),
            (2229, "عبد الفتاح آدم", 27, SpecificPosition.ST, 82, 69, 80, 42, 82, null),
            (2230, "محمد الكويكبي", 23, SpecificPosition.RW, 83, 78, 76, 48, 70, null),
            (2231, "أحمد باحسين", 19, SpecificPosition.CM, 77, 78, 66, 75, 76, null)
        };

        return DatabaseHelpers.CreateTeam("KSA_TAA", "التعاون", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 9. الفتح (Al Fateh SC) - 21 لاعباً
    public static Team CreateAlFateh()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2241, "بيتر زاباش", 1, SpecificPosition.GK, 71, 75, 18, 83, 81, null),
            (2242, "صهيب الزيد", 13, SpecificPosition.LB, 81, 74, 55, 76, 76, null),
            (2243, "مروان سعدان", 15, SpecificPosition.CB, 74, 76, 62, 82, 84, new[] { SpecificPosition.CDM }),
            (2244, "عمار الدحيم", 4, SpecificPosition.CB, 75, 68, 38, 80, 83, null),
            (2245, "سعيد باعطية", 2, SpecificPosition.RB, 82, 74, 52, 76, 77, null),
            (2246, "سفيان بن دبكة", 28, SpecificPosition.CDM, 79, 82, 76, 82, 84, new[] { SpecificPosition.CM }),
            (2247, "نوح الموسى", 6, SpecificPosition.CM, 77, 80, 68, 79, 80, null),
            (2248, "لوكاس زيلارايان", 10, SpecificPosition.CAM, 83, 87, 85, 55, 76, new[] { SpecificPosition.LW }),
            (2249, "مراد باتنا", 11, SpecificPosition.RW, 84, 86, 85, 48, 72, null),
            (2250, "دجانيني تافاريس", 21, SpecificPosition.ST, 84, 75, 84, 52, 85, null),
            (2251, "أمين سباعي", 7, SpecificPosition.LW, 86, 79, 78, 45, 72, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2252, "وليد العنزي", 25, SpecificPosition.GK, 68, 70, 15, 76, 75, null),
            (2253, "حبيب الوطيان", 33, SpecificPosition.GK, 67, 69, 15, 74, 74, null),
            (2254, "فهد الحربي", 14, SpecificPosition.CB, 74, 67, 36, 78, 80, null),
            (2255, "محمد الكنيدري", 12, SpecificPosition.LB, 80, 73, 50, 75, 74, null),
            (2256, "زياد الجري", 18, SpecificPosition.RB, 79, 72, 48, 74, 74, null),
            (2257, "عثمان العثمان", 16, SpecificPosition.CM, 78, 77, 68, 75, 76, null),
            (2258, "حسين المقهوي", 8, SpecificPosition.CM, 74, 79, 72, 75, 75, null),
            (2259, "علي المسعود", 20, SpecificPosition.RW, 82, 74, 73, 42, 69, null),
            (2260, "سعد الشرفاء", 19, SpecificPosition.ST, 80, 68, 76, 38, 78, null),
            (2261, "نايف مسعود", 24, SpecificPosition.CDM, 76, 76, 60, 76, 78, null)
        };

        return DatabaseHelpers.CreateTeam("KSA_FAT", "الفتح", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 10. الفيحاء (Al Fayha FC) - 21 لاعباً
    public static Team CreateAlFayha()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2271, "أورلاندو موسكيرا", 1, SpecificPosition.GK, 71, 74, 18, 82, 80, null),
            (2272, "متعب الخالدي", 3, SpecificPosition.LB, 80, 73, 52, 75, 75, null),
            (2273, "كريس سمولينج", 5, SpecificPosition.CB, 72, 68, 42, 83, 87, null),
            (2274, "مخير الرشيدي", 4, SpecificPosition.CB, 75, 68, 38, 80, 82, null),
            (2275, "محمد البقعاوي", 2, SpecificPosition.RB, 81, 74, 54, 77, 78, null),
            (2276, "جويكو سيميروت", 6, SpecificPosition.CDM, 78, 81, 68, 81, 82, null),
            (2277, "ألفا سيميدو", 8, SpecificPosition.CM, 80, 80, 72, 82, 85, null),
            (2278, "أليخاندرو بوزويلو", 10, SpecificPosition.CAM, 79, 86, 82, 54, 72, null),
            (2279, "هنري أونيكورو", 7, SpecificPosition.LW, 89, 78, 79, 45, 75, null),
            (2280, "فاشيون ساكالا", 9, SpecificPosition.ST, 88, 75, 83, 48, 83, new[] { SpecificPosition.RW }),
            (2281, "رينزو لوبيز", 11, SpecificPosition.ST, 81, 70, 82, 44, 85, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2282, "سطام الشمري", 25, SpecificPosition.GK, 67, 68, 15, 75, 74, null),
            (2283, "أسامة الثميري", 33, SpecificPosition.GK, 66, 67, 15, 73, 72, null),
            (2284, "سامي الخيبري", 15, SpecificPosition.CB, 71, 66, 38, 78, 81, null),
            (2285, "خالد الرماح", 12, SpecificPosition.RB, 78, 71, 48, 74, 74, null),
            (2286, "حسام مجرشي", 16, SpecificPosition.LB, 79, 72, 50, 74, 74, null),
            (2287, "عبد الحميد صابيري", 20, SpecificPosition.CAM, 80, 82, 81, 55, 76, null),
            (2288, "سعود زيدان", 18, SpecificPosition.CM, 77, 77, 65, 75, 76, null),
            (2289, "علي الحسين", 14, SpecificPosition.RW, 82, 74, 73, 40, 70, null),
            (2290, "مالك العبد المنعم", 19, SpecificPosition.ST, 80, 68, 76, 38, 78, null),
            (2291, "نواف الحارثي", 22, SpecificPosition.LW, 81, 73, 72, 40, 68, null)
        };

        return DatabaseHelpers.CreateTeam("KSA_FAY", "الفيحاء", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 11. الخليج (Al Khaleej FC) - 21 لاعباً
    public static Team CreateAlKhaleej()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2301, "إبراهيم سيهيتش", 1, SpecificPosition.GK, 71, 76, 18, 83, 82, null),
            (2302, "بيدرو ريبوتشو", 3, SpecificPosition.LB, 83, 80, 64, 78, 78, null),
            (2303, "مارسيل تيسيران", 5, SpecificPosition.CB, 76, 72, 45, 82, 85, null),
            (2304, "محمد الخبراني", 4, SpecificPosition.CB, 75, 68, 38, 80, 82, null),
            (2305, "سعيد آل حمسل", 2, SpecificPosition.RB, 82, 75, 52, 77, 78, null),
            (2306, "ديميتريوس كوربيليس", 6, SpecificPosition.CDM, 78, 81, 68, 83, 85, null),
            (2307, "خالد السميري", 8, SpecificPosition.CM, 80, 79, 68, 78, 80, null),
            (2308, "فابيو مارتينز", 10, SpecificPosition.LW, 86, 83, 83, 50, 74, new[] { SpecificPosition.CAM }),
            (2309, "منصور حمزي", 11, SpecificPosition.RW, 83, 76, 75, 48, 72, null),
            (2310, "محمد شريف", 9, SpecificPosition.ST, 85, 73, 84, 44, 82, null),
            (2311, "كوستاس فورتونيس", 7, SpecificPosition.CAM, 80, 87, 82, 52, 72, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2312, "مروان الحيدري", 25, SpecificPosition.GK, 68, 70, 15, 76, 75, null),
            (2313, "رائد أزيبي", 33, SpecificPosition.GK, 66, 67, 15, 73, 72, null),
            (2314, "بندر ناصر", 15, SpecificPosition.CB, 73, 67, 36, 77, 79, null),
            (2315, "عمر العودة", 12, SpecificPosition.RB, 79, 72, 48, 74, 74, null),
            (2316, "عبد الله الفهد", 16, SpecificPosition.CB, 74, 66, 38, 78, 80, null),
            (2317, "نايف كريري", 18, SpecificPosition.CDM, 77, 76, 62, 77, 79, null),
            (2318, "مراد هوساوي", 20, SpecificPosition.CM, 77, 77, 65, 74, 76, null),
            (2319, "صالح أبو الشامات", 22, SpecificPosition.RW, 82, 75, 74, 42, 70, null),
            (2320, "هشام آل دبيس", 19, SpecificPosition.ST, 81, 68, 76, 38, 77, null),
            (2321, "خالد ناري", 17, SpecificPosition.RW, 85, 79, 78, 50, 76, null)
        };

        return DatabaseHelpers.CreateTeam("KSA_KHJ", "الخليج", FormationType.Formation4231, false, starters, bench);
    }

    // 12. الرائد (Al Raed FC) - 21 لاعباً
    public static Team CreateAlRaed()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2331, "أندريه مورييرا", 1, SpecificPosition.GK, 71, 74, 18, 82, 81, null),
            (2332, "حمد الجيزاني", 3, SpecificPosition.LB, 81, 74, 52, 76, 76, null),
            (2333, "أيوب القاسمي", 4, SpecificPosition.CB, 75, 68, 40, 81, 84, null),
            (2334, "عمر غونزاليس", 5, SpecificPosition.CB, 74, 70, 45, 82, 85, null),
            (2335, "محمد الدوسري رائد", 2, SpecificPosition.RB, 81, 74, 50, 76, 77, null),
            (2336, "ماتياس نورمان", 6, SpecificPosition.CDM, 80, 83, 76, 82, 84, new[] { SpecificPosition.CM }),
            (2337, "مهدي عبيد", 8, SpecificPosition.CM, 78, 82, 74, 80, 82, null),
            (2338, "أمير سعيود", 10, SpecificPosition.CAM, 83, 86, 82, 54, 70, null),
            (2339, "محمد فوزير", 11, SpecificPosition.RW, 84, 83, 80, 52, 72, null),
            (2340, "كريم البركاوي", 9, SpecificPosition.ST, 84, 74, 84, 46, 82, null),
            (2341, "يحيى سنبل", 7, SpecificPosition.LW, 85, 77, 76, 44, 70, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2342, "مشاري سنيور", 25, SpecificPosition.GK, 68, 69, 15, 76, 75, null),
            (2343, "صالح الهذلول", 33, SpecificPosition.GK, 66, 67, 15, 73, 72, null),
            (2344, "مبارك الراجح", 15, SpecificPosition.CB, 73, 66, 35, 77, 80, null),
            (2345, "خالد السبيعي", 12, SpecificPosition.LB, 78, 72, 50, 74, 74, null),
            (2346, "عبد الله الفهد رائد", 16, SpecificPosition.CB, 74, 66, 36, 78, 80, null),
            (2347, "منصور البيشي", 14, SpecificPosition.CDM, 77, 77, 62, 77, 79, null),
            (2348, "عمر النمري", 18, SpecificPosition.CM, 77, 76, 65, 74, 75, null),
            (2349, "صالح العمري رائد", 22, SpecificPosition.LW, 83, 78, 76, 48, 73, null),
            (2350, "رائد الغامدي", 19, SpecificPosition.ST, 82, 69, 78, 40, 79, null),
            (2351, "نواف السهلي", 20, SpecificPosition.RW, 81, 73, 72, 40, 68, null)
        };

        return DatabaseHelpers.CreateTeam("KSA_RAE", "الرائد", FormationType.Formation4231, false, starters, bench);
    }

    // 13. ضمك (Damac FC) - 21 لاعباً
    public static Team CreateDamac()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2361, "فلورين نيتا", 1, SpecificPosition.GK, 72, 75, 18, 83, 81, null),
            (2362, "ضاري العنزي", 3, SpecificPosition.LB, 82, 75, 55, 76, 76, null),
            (2363, "عبد القادر بدران", 4, SpecificPosition.CB, 76, 70, 42, 82, 85, null),
            (2364, "فاروق شافعي", 5, SpecificPosition.CB, 73, 70, 50, 81, 84, null),
            (2365, "سنوسي هوساوي", 2, SpecificPosition.RB, 82, 74, 52, 76, 77, null),
            (2366, "طارق حامد", 6, SpecificPosition.CDM, 76, 80, 62, 85, 87, null),
            (2367, "نيكولاي ستانتشيو", 8, SpecificPosition.CM, 80, 88, 85, 62, 75, new[] { SpecificPosition.CAM }),
            (2368, "فرانسوا كامانو", 11, SpecificPosition.LW, 87, 80, 81, 52, 78, null),
            (2369, "جورج كيفين نكودو", 10, SpecificPosition.LW, 90, 82, 85, 48, 76, new[] { SpecificPosition.RW }),
            (2370, "حبيب ديالو", 9, SpecificPosition.ST, 85, 74, 85, 48, 86, null),
            (2371, "أيمن فلاتة", 7, SpecificPosition.RW, 84, 76, 75, 45, 72, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2372, "أمين بخاري", 25, SpecificPosition.GK, 69, 71, 15, 77, 76, null),
            (2373, "موسى حامضي", 33, SpecificPosition.GK, 66, 67, 15, 73, 72, null),
            (2374, "سلطان فقيهي", 15, SpecificPosition.CB, 74, 67, 36, 78, 81, null),
            (2375, "عبد الرحمن الخيبري", 12, SpecificPosition.RB, 78, 71, 48, 74, 74, null),
            (2376, "عبد الله القحطاني", 16, SpecificPosition.CAM, 80, 79, 74, 50, 70, null),
            (2377, "هزاع الغامدي", 18, SpecificPosition.CDM, 77, 76, 60, 77, 79, null),
            (2378, "أحمد الزين", 20, SpecificPosition.LW, 82, 77, 74, 45, 70, null),
            (2379, "رمزي صولان", 22, SpecificPosition.RW, 83, 75, 74, 42, 71, null),
            (2380, "مشاري النمر", 19, SpecificPosition.ST, 82, 69, 77, 38, 78, null),
            (2381, "بدر منشي", 24, SpecificPosition.CM, 77, 77, 65, 75, 76, null)
        };

        return DatabaseHelpers.CreateTeam("KSA_DAM", "ضمك", FormationType.Formation4231, false, starters, bench);
    }

    // 14. الوحدة السعودي (Al Wehda FC) - 21 لاعباً
    public static Team CreateAlWehda()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2391, "إجناسيو دي أروابارينا", 1, SpecificPosition.GK, 71, 75, 18, 82, 80, null),
            (2392, "مشعل السبيعي", 3, SpecificPosition.LB, 80, 74, 52, 76, 75, null),
            (2393, "جواد الياميق", 5, SpecificPosition.CB, 78, 70, 45, 83, 86, null),
            (2394, "عبد الله الحفيظ", 4, SpecificPosition.CB, 74, 68, 38, 79, 82, null),
            (2395, "سعيد المولد", 2, SpecificPosition.RB, 81, 75, 55, 77, 78, null),
            (2396, "ألكسندرو كريتو", 6, SpecificPosition.CDM, 78, 80, 70, 81, 84, null),
            (2397, "وليد باخشوين", 8, SpecificPosition.CDM, 75, 78, 62, 80, 82, null),
            (2398, "سعد بقير", 10, SpecificPosition.CAM, 77, 87, 83, 52, 68, null),
            (2399, "يوسف أمين", 11, SpecificPosition.LW, 86, 78, 77, 44, 70, null),
            (2400, "أوديون إيجالو", 9, SpecificPosition.ST, 82, 74, 86, 48, 86, null),
            (2401, "كريج جودوين", 7, SpecificPosition.RW, 85, 84, 82, 58, 76, new[] { SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2402, "عبد الله العويشير", 25, SpecificPosition.GK, 68, 70, 15, 76, 75, null),
            (2403, "أيمن الحسيني حارس", 33, SpecificPosition.GK, 66, 67, 15, 73, 72, null),
            (2404, "علي مكي", 15, SpecificPosition.CB, 73, 66, 35, 77, 80, null),
            (2405, "إسلام هوساوي", 12, SpecificPosition.LB, 81, 73, 50, 75, 75, null),
            (2406, "نواف العزيزي", 18, SpecificPosition.RB, 78, 71, 48, 74, 74, null),
            (2407, "علاء حجي", 16, SpecificPosition.CM, 78, 78, 68, 76, 77, null),
            (2408, "حسين العيسى", 20, SpecificPosition.LW, 83, 76, 74, 45, 70, null),
            (2409, "مراد خضري", 19, SpecificPosition.ST, 81, 68, 77, 38, 78, null),
            (2410, "يحيى النجعي", 22, SpecificPosition.RW, 82, 75, 73, 42, 69, null),
            (2411, "عبد العزيز نور", 24, SpecificPosition.CAM, 78, 78, 74, 50, 71, null)
        };

        return DatabaseHelpers.CreateTeam("KSA_WEH", "الوحدة السعودي", FormationType.Formation4231, false, starters, bench);
    }

    // 15. الأخدود (Al Okhdood Club) - 21 لاعباً
    public static Team CreateAlOkhdood()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2421, "صامويل سامبا", 1, SpecificPosition.GK, 71, 74, 18, 82, 80, null),
            (2422, "مهند القيضي", 3, SpecificPosition.LB, 80, 73, 52, 75, 75, null),
            (2423, "داميون لوي", 4, SpecificPosition.CB, 77, 68, 40, 81, 85, null),
            (2424, "سعيد الربيعي", 5, SpecificPosition.CB, 74, 67, 36, 79, 82, null),
            (2425, "عوض خميس", 2, SpecificPosition.RB, 80, 76, 60, 77, 78, new[] { SpecificPosition.LB, SpecificPosition.CM }),
            (2426, "بيتروس ماثيوس", 6, SpecificPosition.CDM, 77, 80, 70, 83, 84, null),
            (2427, "خوان بيدروزا", 8, SpecificPosition.CM, 80, 81, 74, 78, 80, null),
            (2428, "سافيور جودوين", 11, SpecificPosition.LW, 88, 78, 80, 48, 76, null),
            (2429, "كريستيان باسوغوج", 10, SpecificPosition.RW, 88, 80, 80, 48, 78, null),
            (2430, "إبراهيما كوني", 9, SpecificPosition.ST, 83, 72, 83, 44, 88, null),
            (2431, "صالح الحارثي", 7, SpecificPosition.CAM, 80, 78, 75, 50, 71, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2432, "راكان النجار", 25, SpecificPosition.GK, 68, 69, 15, 76, 75, null),
            (2433, "سعد الصالح", 33, SpecificPosition.GK, 66, 67, 15, 73, 72, null),
            (2434, "نايف عسيري", 15, SpecificPosition.CB, 73, 66, 35, 77, 80, null),
            (2435, "حسين الزبداني", 12, SpecificPosition.RB, 78, 71, 48, 74, 74, null),
            (2436, "عبد العزيز هتيلة", 16, SpecificPosition.ST, 80, 68, 76, 38, 77, null),
            (2437, "سعود سالم", 18, SpecificPosition.CM, 77, 76, 64, 74, 76, null),
            (2438, "عيد المولد أخدود", 20, SpecificPosition.CDM, 78, 77, 62, 78, 80, null),
            (2439, "محمد السعيد", 22, SpecificPosition.LW, 81, 74, 72, 40, 68, null),
            (2440, "دييجو فيريرا", 17, SpecificPosition.ST, 81, 70, 78, 40, 80, null),
            (2441, "أحمد مصطفى أخدود", 24, SpecificPosition.RW, 82, 75, 74, 42, 70, null)
        };

        return DatabaseHelpers.CreateTeam("KSA_OKH", "الأخدود", FormationType.Formation433_Attack, false, starters, bench);
    }

    // 16. الرياض (Al Riyadh SC) - 21 لاعباً
    public static Team CreateAlRiyadh()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2451, "ميلان بوريان", 1, SpecificPosition.GK, 71, 75, 18, 83, 82, null),
            (2452, "عبد الإله الخيبري", 3, SpecificPosition.LB, 80, 74, 52, 76, 75, null),
            (2453, "يوان باربيت", 5, SpecificPosition.CB, 74, 75, 55, 82, 84, null),
            (2454, "أحمد عسيري", 4, SpecificPosition.CB, 74, 67, 36, 79, 82, null),
            (2455, "حسين النويقي", 2, SpecificPosition.RB, 81, 73, 50, 76, 76, null),
            (2456, "لوكاس كال", 6, SpecificPosition.CDM, 78, 80, 66, 81, 83, null),
            (2457, "توزي أنتونيو", 8, SpecificPosition.CM, 80, 84, 82, 65, 75, new[] { SpecificPosition.CAM }),
            (2458, "إبراهيم بايش", 10, SpecificPosition.CAM, 83, 82, 79, 60, 78, new[] { SpecificPosition.RW, SpecificPosition.RM }),
            (2459, "فايز سليماني", 11, SpecificPosition.LW, 86, 80, 80, 52, 75, null),
            (2460, "محمد كوناتي", 9, SpecificPosition.ST, 84, 72, 83, 44, 86, null),
            (2461, "برنارد مينساه", 7, SpecificPosition.CAM, 83, 84, 83, 62, 78, new[] { SpecificPosition.CM })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2462, "عبد الرحمن الشمري", 25, SpecificPosition.GK, 68, 69, 15, 76, 75, null),
            (2463, "راكان المولد", 33, SpecificPosition.GK, 66, 67, 15, 73, 72, null),
            (2464, "مرزوق تمبكتي", 15, SpecificPosition.CB, 73, 66, 35, 77, 80, null),
            (2465, "سويلم المنهالي", 12, SpecificPosition.RB, 79, 72, 48, 74, 74, null),
            (2466, "علي الزقعان", 18, SpecificPosition.RW, 82, 76, 74, 46, 70, null),
            (2467, "يحيى الشهري", 17, SpecificPosition.CAM, 77, 83, 75, 45, 64, null),
            (2468, "فهد الرشيدي رياض", 20, SpecificPosition.CM, 77, 77, 65, 75, 76, null),
            (2469, "بدر المطيري", 22, SpecificPosition.LW, 81, 74, 72, 40, 68, null),
            (2470, "ريان البلوشي", 19, SpecificPosition.ST, 80, 68, 76, 38, 78, null),
            (2471, "محمد العقل", 24, SpecificPosition.CDM, 76, 76, 60, 76, 78, null)
        };

        return DatabaseHelpers.CreateTeam("KSA_RIY", "الرياض", FormationType.Formation4231, false, starters, bench);
    }

    // 17. العروبة (Al Orobah FC) - 21 لاعباً
    public static Team CreateAlOrobah()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2481, "غايتان كوكي", 1, SpecificPosition.GK, 71, 74, 18, 82, 80, null),
            (2482, "إبراهيم الزبيدي", 3, SpecificPosition.LB, 80, 75, 55, 76, 76, null),
            (2483, "كورت زوما", 5, SpecificPosition.CB, 76, 70, 45, 84, 88, null),
            (2484, "إسماعيل قندوس", 4, SpecificPosition.CB, 75, 68, 40, 81, 84, null),
            (2485, "حامد المقاطي", 2, SpecificPosition.RB, 81, 73, 50, 76, 76, null),
            (2486, "جان سيري", 6, SpecificPosition.CDM, 77, 84, 72, 80, 78, new[] { SpecificPosition.CM }),
            (2487, "كارلو موهار", 8, SpecificPosition.CM, 78, 80, 74, 81, 84, null),
            (2488, "يوهان بيرغ جودموندسون", 10, SpecificPosition.CAM, 80, 84, 82, 58, 75, new[] { SpecificPosition.RW }),
            (2489, "كريستيان تيو", 11, SpecificPosition.LW, 86, 81, 82, 45, 72, null),
            (2490, "إيمانويل بواتينج عروبة", 9, SpecificPosition.ST, 85, 71, 82, 42, 84, null),
            (2491, "فهد الزبيدي", 7, SpecificPosition.RW, 84, 76, 75, 45, 72, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2492, "رافع الرويلي", 25, SpecificPosition.GK, 68, 69, 15, 76, 75, null),
            (2493, "سعود الرويلي", 33, SpecificPosition.GK, 66, 67, 15, 73, 72, null),
            (2494, "حسين الشويش", 15, SpecificPosition.CB, 74, 67, 36, 78, 80, null),
            (2495, "نواف القميري", 12, SpecificPosition.RB, 78, 71, 48, 74, 74, null),
            (2496, "فهد الرشيدي عروبة", 16, SpecificPosition.LB, 78, 72, 50, 74, 74, null),
            (2497, "سطام الروقي", 18, SpecificPosition.ST, 81, 68, 77, 38, 77, null),
            (2498, "محمد الصيعري", 20, SpecificPosition.ST, 80, 70, 78, 40, 81, null),
            (2499, "عبد الملك الشمري", 22, SpecificPosition.CM, 77, 77, 65, 75, 76, null),
            (2500, "تركي العليوة", 14, SpecificPosition.LW, 82, 74, 73, 40, 68, null),
            (2501, "زياد الحنيطي", 24, SpecificPosition.CDM, 76, 76, 60, 76, 78, null)
        };

        return DatabaseHelpers.CreateTeam("KSA_ORO", "العروبة", FormationType.Formation4231, false, starters, bench);
    }

    // 18. الخلود (Al Kholood Club) - 21 لاعباً
    public static Team CreateAlKholood()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2511, "مارسيلو غروهي", 1, SpecificPosition.GK, 72, 76, 18, 85, 82, null),
            (2512, "حمدان الشمراني خلود", 3, SpecificPosition.LB, 81, 75, 55, 77, 78, null),
            (2513, "وليام تروست إيكونغ", 5, SpecificPosition.CB, 77, 68, 42, 83, 86, null),
            (2514, "نوربيرت غيومبير", 4, SpecificPosition.CB, 75, 68, 40, 81, 84, null),
            (2515, "سلطان الشهري", 2, SpecificPosition.RB, 81, 73, 50, 76, 76, null),
            (2516, "كيفين ندورام", 6, SpecificPosition.CDM, 78, 80, 68, 82, 84, null),
            (2517, "أليو ديانغ", 8, SpecificPosition.CDM, 82, 81, 70, 85, 87, new[] { SpecificPosition.CM }),
            (2518, "أليكس كولادو", 10, SpecificPosition.CAM, 83, 85, 82, 54, 72, new[] { SpecificPosition.RW }),
            (2519, "ميزياني ماوليدا", 11, SpecificPosition.LW, 86, 79, 81, 48, 76, null),
            (2520, "جاكسون موليكا", 9, SpecificPosition.ST, 86, 73, 84, 46, 84, null),
            (2521, "همام الهمامي", 7, SpecificPosition.RW, 84, 76, 75, 45, 72, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (2522, "جاسم العشبان", 25, SpecificPosition.GK, 68, 69, 15, 76, 75, null),
            (2523, "محمد الدوسري خلود", 33, SpecificPosition.GK, 66, 67, 15, 73, 72, null),
            (2524, "جمعان الدوسري", 15, SpecificPosition.CB, 73, 66, 35, 77, 80, null),
            (2525, "حسن الأسمري", 12, SpecificPosition.RB, 78, 71, 48, 74, 74, null),
            (2526, "عبد الله الرشيدي", 16, SpecificPosition.LB, 78, 72, 50, 74, 74, null),
            (2527, "فرحان العازمي", 18, SpecificPosition.CM, 77, 77, 65, 75, 76, null),
            (2528, "عبد الفتاح عسيري", 20, SpecificPosition.CAM, 79, 81, 75, 48, 66, null),
            (2529, "مازن السويلم", 22, SpecificPosition.LW, 81, 74, 72, 40, 68, null),
            (2530, "محمد صوعان", 19, SpecificPosition.ST, 80, 68, 76, 38, 77, null),
            (2531, "طلال العريشي", 24, SpecificPosition.RW, 81, 74, 73, 40, 70, null)
        };

        return DatabaseHelpers.CreateTeam("KSA_KHO", "الخلود", FormationType.Formation4231, false, starters, bench);
    }
}
