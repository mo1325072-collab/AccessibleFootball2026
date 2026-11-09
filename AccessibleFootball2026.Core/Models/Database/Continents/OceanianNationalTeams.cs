namespace AccessibleFootball2026.Core.Models;

/// <summary>
/// منتخبات قارة أوقيانوسيا (OFC) المعتمدة رسمياً من فيفا لموسم 2026/2027 (6 منتخبات)
/// موثقة بالكامل من واقع Transfermarkt و SofaScore و 365scores
/// بقوائم موسعة (22 إلى 23 لاعباً لكل منتخب)
/// </summary>
public static class OceanianNationalTeams
{
    public static List<Team> GetAllNationalTeams()
    {
        return new List<Team>
        {
            CreateNewZealand(),
            CreateFiji(),
            CreateNewCaledonia(),
            CreateTahiti(),
            CreateSolomonIslands(),
            CreateVanuatu()
        };
    }

    // 1. نيوزيلندا (New Zealand) - 23 لاعباً
    public static Team CreateNewZealand()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (15001, "أليكس بولسيتش", 1, SpecificPosition.GK, 60, 78, 18, 83, 82, null),
            (15002, "ليبيراتو كاكاسي", 13, SpecificPosition.LB, 85, 80, 68, 81, 80, new[] { SpecificPosition.LWB }),
            (15003, "مايكل بوكسال", 4, SpecificPosition.CB, 72, 74, 45, 83, 86, null),
            (15004, "تايلر بيندون", 5, SpecificPosition.CB, 78, 76, 42, 83, 83, new[] { SpecificPosition.RB }),
            (15005, "تيم باين", 2, SpecificPosition.RB, 82, 77, 60, 79, 81, new[] { SpecificPosition.CB }),
            (15006, "جو بيل", 8, SpecificPosition.CDM, 78, 83, 72, 83, 82, new[] { SpecificPosition.CM }),
            (15007, "ماركو ستامينيتش", 6, SpecificPosition.CM, 80, 83, 76, 82, 84, new[] { SpecificPosition.CDM }),
            (15008, "ماثيو جاربيت", 10, SpecificPosition.CAM, 82, 84, 80, 65, 78, new[] { SpecificPosition.CM }),
            (15009, "ساربفيت سينغ", 11, SpecificPosition.LW, 84, 84, 80, 52, 72, new[] { SpecificPosition.RW, SpecificPosition.CAM }),
            (15010, "كريس وود", 9, SpecificPosition.ST, 78, 78, 89, 52, 91, null),
            (15011, "إليجاه جاست", 7, SpecificPosition.RW, 86, 80, 78, 52, 74, new[] { SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (15012, "ماكس كروكومبي", 12, SpecificPosition.GK, 58, 74, 15, 80, 81, null),
            (15013, "تومي سميث", 14, SpecificPosition.CB, 68, 73, 45, 81, 84, null),
            (15014, "فين سورمان", 15, SpecificPosition.CB, 76, 72, 40, 80, 83, null),
            (15015, "سام ساتون", 3, SpecificPosition.LB, 83, 75, 58, 77, 76, null),
            (15016, "دالتون ويلكينز", 16, SpecificPosition.LB, 82, 74, 55, 76, 76, null),
            (15017, "أليكس روفير", 17, SpecificPosition.CDM, 76, 78, 68, 79, 82, null),
            (15018, "كام هاويسون", 18, SpecificPosition.CM, 78, 80, 74, 76, 78, null),
            (15019, "بن واين", 19, SpecificPosition.ST, 84, 74, 80, 45, 80, null),
            (15020, "أليكس غريف", 20, SpecificPosition.ST, 84, 73, 79, 45, 78, null),
            (15021, "كوستا بارباروسيس", 21, SpecificPosition.RW, 85, 78, 79, 48, 74, new[] { SpecificPosition.ST }),
            (15022, "أوليفر سيل", 22, SpecificPosition.GK, 56, 68, 15, 75, 75, null),
            (15023, "كالوم ماكوات", 23, SpecificPosition.LW, 85, 79, 78, 50, 74, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_NZL", "نيوزيلندا", FormationType.Formation4231_Wide, true, starters, bench);
    }

    // 2. فيجي (Fiji) - 22 لاعباً
    public static Team CreateFiji()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (15031, "أيسييا بوليبولوا", 1, SpecificPosition.GK, 60, 75, 18, 80, 81, null),
            (15032, "فيليبي بارافيلابو", 3, SpecificPosition.LB, 82, 74, 58, 77, 78, null),
            (15033, "إلويا تويوافو", 4, SpecificPosition.CB, 76, 72, 40, 80, 84, null),
            (15034, "أكانيسي سالابابا", 5, SpecificPosition.CB, 75, 71, 38, 80, 83, null),
            (15035, "ليكيليكي نابينو", 2, SpecificPosition.RB, 83, 75, 55, 77, 78, null),
            (15036, "تيبيتا فوروساوا", 6, SpecificPosition.CDM, 78, 78, 66, 80, 82, null),
            (15037, "سيتاريكي هيوز", 8, SpecificPosition.CM, 80, 80, 74, 77, 78, null),
            (15038, "نبيل بيج", 10, SpecificPosition.CAM, 82, 82, 78, 55, 74, null),
            (15039, "روي كريشنا", 9, SpecificPosition.ST, 86, 82, 86, 52, 80, new[] { SpecificPosition.LW, SpecificPosition.RW }),
            (15040, "سيروانيا غونيليفو", 11, SpecificPosition.ST, 83, 72, 80, 45, 83, null),
            (15041, "توماس دن", 7, SpecificPosition.RW, 85, 78, 77, 50, 75, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (15042, "أكانيسي سيكولا", 12, SpecificPosition.GK, 58, 70, 15, 77, 77, null),
            (15043, "سيميون نابينو", 13, SpecificPosition.CB, 74, 70, 38, 78, 81, null),
            (15044, "ستيرلينغ فاسكونسيلوس", 14, SpecificPosition.CB, 75, 70, 38, 78, 82, null),
            (15045, "إيفان كومار", 15, SpecificPosition.LB, 81, 72, 52, 75, 75, null),
            (15046, "أبتيكار أزاد", 16, SpecificPosition.CDM, 76, 76, 64, 78, 80, null),
            (15047, "ميريل ناند", 17, SpecificPosition.CM, 78, 78, 70, 75, 76, null),
            (15048, "إيتواناي تاماتانيسوا", 18, SpecificPosition.CAM, 80, 78, 74, 50, 70, null),
            (15049, "كريستوفر واساسالا", 19, SpecificPosition.LW, 84, 76, 76, 45, 76, null),
            (15050, "إيتوني دوجي", 20, SpecificPosition.ST, 82, 70, 78, 40, 81, null),
            (15051, "باتريك غابرييل", 21, SpecificPosition.ST, 81, 68, 76, 40, 79, null),
            (15052, "جيروم نارايان", 22, SpecificPosition.GK, 56, 66, 15, 74, 74, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_FIJ", "فيجي", FormationType.Formation442, true, starters, bench);
    }

    // 3. كاليدونيا الجديدة (New Caledonia) - 22 لاعباً
    public static Team CreateNewCaledonia()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (15061, "روكي نييكين", 1, SpecificPosition.GK, 60, 76, 18, 81, 82, null),
            (15062, "إيميل بينتي", 3, SpecificPosition.LB, 83, 75, 58, 78, 78, null),
            (15063, "فينسنت فاكواثي", 4, SpecificPosition.CB, 76, 72, 40, 81, 84, null),
            (15064, "برنارد إيوا", 5, SpecificPosition.CB, 75, 71, 38, 80, 83, null),
            (15065, "مارتن ماكالو", 2, SpecificPosition.RB, 84, 76, 58, 78, 78, null),
            (15066, "سيزر زيو", 6, SpecificPosition.CDM, 78, 79, 68, 81, 83, null),
            (15067, "بيير باوا", 8, SpecificPosition.CM, 80, 81, 74, 78, 79, null),
            (15068, "سيباستيان كولدرون", 10, SpecificPosition.CAM, 82, 82, 78, 55, 72, null),
            (15069, "جورج غوب فنيبيج", 7, SpecificPosition.LW, 87, 81, 82, 50, 75, new[] { SpecificPosition.RW, SpecificPosition.ST }),
            (15070, "ماكومبو كاتي", 9, SpecificPosition.ST, 83, 72, 81, 45, 83, null),
            (15071, "لاتيفي لوكوني", 11, SpecificPosition.RW, 86, 78, 78, 48, 74, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (15072, "ميكائيل أوليتش", 12, SpecificPosition.GK, 58, 70, 15, 77, 77, null),
            (15073, "جان باتيست ويتش", 13, SpecificPosition.CB, 74, 70, 38, 78, 81, null),
            (15074, "غابرييل فاكواثي", 14, SpecificPosition.CB, 74, 70, 38, 78, 81, null),
            (15075, "ويليام كونيي", 15, SpecificPosition.LB, 81, 73, 52, 75, 75, null),
            (15076, "فونوا كايك", 16, SpecificPosition.CDM, 76, 76, 65, 78, 80, null),
            (15077, "سيمون لوانغو", 17, SpecificPosition.CM, 78, 78, 72, 76, 76, null),
            (15078, "مورغان ماتيلو", 18, SpecificPosition.CAM, 80, 79, 75, 50, 70, null),
            (15079, "تيتوان بوكولوت", 19, SpecificPosition.RW, 85, 76, 75, 45, 72, null),
            (15080, "جان جاك واماترا", 20, SpecificPosition.ST, 82, 70, 78, 40, 81, null),
            (15081, "بول إيريك وينسي", 21, SpecificPosition.LW, 84, 75, 74, 42, 70, null),
            (15082, "توماس نيديتيكين", 22, SpecificPosition.GK, 56, 66, 15, 74, 74, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_NCL", "كاليدونيا الجديدة", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 4. تاهيتي (Tahiti) - 22 لاعباً
    public static Team CreateTahiti()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (15091, "تيهاوي تيهاو", 1, SpecificPosition.GK, 60, 76, 18, 81, 82, null),
            (15092, "ماتيا باوما", 3, SpecificPosition.LB, 83, 76, 60, 78, 78, null),
            (15093, "كيفين تيفيتو", 4, SpecificPosition.CB, 76, 72, 40, 81, 84, null),
            (15094, "تياونوي تيهاو", 5, SpecificPosition.CB, 76, 76, 52, 82, 84, new[] { SpecificPosition.ST }),
            (15095, "تاوماتا فاهيروا", 2, SpecificPosition.RB, 84, 76, 58, 78, 78, null),
            (15096, "ألفين تيهاو", 6, SpecificPosition.CDM, 78, 80, 72, 81, 83, new[] { SpecificPosition.CM }),
            (15097, "فرانك باباو", 8, SpecificPosition.CM, 79, 81, 74, 78, 79, null),
            (15098, "ريتوتيا هانايي", 10, SpecificPosition.CAM, 82, 82, 78, 55, 72, null),
            (15099, "إيدي كاسبارد", 7, SpecificPosition.RW, 86, 79, 79, 50, 75, null),
            (15100, "تياونوي تيهاو جونيور", 9, SpecificPosition.ST, 83, 74, 83, 45, 83, null),
            (15101, "مانواري لوسيان", 11, SpecificPosition.LW, 86, 78, 78, 48, 74, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (15102, "أنيل ديبار", 12, SpecificPosition.GK, 58, 70, 15, 77, 77, null),
            (15103, "موري تاهياوتوا", 13, SpecificPosition.CB, 74, 70, 38, 78, 81, null),
            (15104, "تيري إيتيرايرا", 14, SpecificPosition.CB, 74, 70, 38, 78, 81, null),
            (15105, "هاومانا مارواو", 15, SpecificPosition.LB, 81, 72, 52, 75, 75, null),
            (15106, "تيوتيا كويري", 16, SpecificPosition.CDM, 76, 76, 64, 78, 80, null),
            (15107, "أرييتاي تاهيتو", 17, SpecificPosition.CM, 78, 78, 70, 76, 76, null),
            (15108, "ألكساندر تاهي", 18, SpecificPosition.CAM, 80, 78, 74, 50, 70, null),
            (15109, "رونو تيماريي", 19, SpecificPosition.LW, 84, 76, 75, 45, 72, null),
            (15110, "روفي تيهاو", 20, SpecificPosition.ST, 82, 70, 78, 40, 81, null),
            (15111, "فيري فيليبي", 21, SpecificPosition.RW, 84, 75, 74, 45, 72, null),
            (15112, "ميشيل كوانت", 22, SpecificPosition.GK, 56, 66, 15, 74, 74, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_TAH", "تاهيتي", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 5. جزر سليمان (Solomon Islands) - 22 لاعباً
    public static Team CreateSolomonIslands()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (15121, "فيليب مانغو", 1, SpecificPosition.GK, 60, 76, 18, 81, 82, null),
            (15122, "ديفيد سوباتي", 3, SpecificPosition.LB, 83, 75, 58, 78, 78, null),
            (15123, "ليون كافيتيري", 4, SpecificPosition.CB, 76, 72, 40, 81, 84, null),
            (15124, "ألفين هاو", 5, SpecificPosition.CB, 76, 72, 40, 81, 84, null),
            (15125, "جاني كاوكي", 2, SpecificPosition.RB, 84, 76, 58, 78, 78, null),
            (15126, "أتيندا بولا", 6, SpecificPosition.CDM, 78, 79, 68, 81, 83, null),
            (15127, "ويليام كوماسيلي", 8, SpecificPosition.CM, 80, 81, 74, 78, 79, null),
            (15128, "جاري كاوا", 10, SpecificPosition.CAM, 83, 83, 79, 55, 72, null),
            (15129, "رافائيل ليي", 9, SpecificPosition.ST, 87, 78, 84, 48, 80, new[] { SpecificPosition.RW, SpecificPosition.LW }),
            (15130, "جاجو تيفيتا", 11, SpecificPosition.ST, 84, 74, 81, 45, 82, null),
            (15131, "ميكا ليلفافا", 7, SpecificPosition.RW, 87, 78, 78, 48, 74, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (15132, "تيموثي سابا", 12, SpecificPosition.GK, 58, 70, 15, 77, 77, null),
            (15133, "مايكل بوتي", 13, SpecificPosition.CB, 74, 70, 38, 78, 81, null),
            (15134, "هدسون فاوكابو", 14, SpecificPosition.CB, 74, 70, 38, 78, 81, null),
            (15135, "برينس تاها", 15, SpecificPosition.LB, 81, 72, 52, 75, 75, null),
            (15136, "كارلوس ليوما", 16, SpecificPosition.CDM, 76, 76, 64, 78, 80, null),
            (15137, "إدوين سيكيلا", 17, SpecificPosition.CM, 78, 78, 70, 76, 76, null),
            (15138, "فرانك كابوي", 18, SpecificPosition.CAM, 80, 78, 74, 50, 70, null),
            (15139, "روستون غيغي", 19, SpecificPosition.LW, 85, 76, 75, 45, 72, null),
            (15140, "ألفين كولو", 20, SpecificPosition.ST, 82, 70, 78, 40, 81, null),
            (15141, "جوردان إيتيكي", 21, SpecificPosition.ST, 81, 68, 76, 40, 79, null),
            (15142, "جورج تانغيس", 22, SpecificPosition.GK, 56, 66, 15, 74, 74, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_SOL", "جزر سليمان", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 6. فانواتو (Vanuatu) - 22 لاعباً
    public static Team CreateVanuatu()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (15151, "ديك كالوكالو", 1, SpecificPosition.GK, 60, 76, 18, 81, 82, null),
            (15152, "جوناثان سبوكيجاكولو", 3, SpecificPosition.LB, 83, 75, 58, 78, 78, null),
            (15153, "برايان كالتوك", 4, SpecificPosition.CB, 78, 74, 48, 84, 87, null),
            (15154, "جايسون توماس", 5, SpecificPosition.CB, 76, 72, 40, 81, 84, null),
            (15155, "تيموثي بوي", 2, SpecificPosition.RB, 84, 76, 58, 78, 78, null),
            (15156, "جون أكريك", 6, SpecificPosition.CDM, 78, 79, 68, 81, 83, null),
            (15157, "ميتشل كالتوك", 8, SpecificPosition.CM, 80, 81, 74, 78, 79, null),
            (15158, "بونغ كالو", 10, SpecificPosition.CAM, 83, 83, 79, 58, 74, new[] { SpecificPosition.ST }),
            (15159, "أزيريا سورومون", 9, SpecificPosition.ST, 84, 74, 82, 48, 82, null),
            (15160, "أليكس سانيل", 11, SpecificPosition.ST, 83, 73, 81, 45, 83, null),
            (15161, "كلود أرو", 7, SpecificPosition.RW, 86, 78, 78, 48, 74, new[] { SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (15162, "ماسينغ كالتوك", 12, SpecificPosition.GK, 58, 70, 15, 77, 77, null),
            (15163, "تاسو كالتوك", 13, SpecificPosition.CB, 74, 70, 38, 78, 81, null),
            (15164, "سيلوين فاتو", 14, SpecificPosition.CB, 74, 70, 38, 78, 81, null),
            (15165, "كيران بيترس", 15, SpecificPosition.LB, 81, 72, 52, 75, 75, null),
            (15166, "غودين تينين", 16, SpecificPosition.CDM, 76, 76, 64, 78, 80, null),
            (15167, "رونالدو تينين", 17, SpecificPosition.CM, 78, 78, 70, 76, 76, null),
            (15168, "جان باسكال كالتوك", 18, SpecificPosition.CAM, 80, 78, 74, 50, 70, null),
            (15169, "كيسي كالتوك", 19, SpecificPosition.LW, 84, 76, 75, 45, 72, null),
            (15170, "توني كالتوك", 20, SpecificPosition.ST, 82, 70, 78, 40, 81, null),
            (15171, "بيل كالتوك", 21, SpecificPosition.ST, 81, 68, 76, 40, 79, null),
            (15172, "أنتوني تيفا", 22, SpecificPosition.GK, 56, 66, 15, 74, 74, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_VAN", "فانواتو", FormationType.Formation433_Holding, true, starters, bench);
    }
}
