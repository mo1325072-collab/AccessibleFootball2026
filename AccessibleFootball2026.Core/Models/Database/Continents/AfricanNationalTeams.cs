namespace AccessibleFootball2026.Core.Models;

/// <summary>
/// منتخبات القارة الإفريقية (CAF) المعتمدة رسمياً من فيفا لموسم 2026/2027 (20 منتخباً)
/// موثقة بالكامل من واقع Transfermarkt و SofaScore و 365scores
/// بقوائم موسعة (21 إلى 23 لاعباً لكل منتخب)
/// </summary>
public static class AfricanNationalTeams
{
    public static List<Team> GetAllNationalTeams()
    {
        return new List<Team>
        {
            CreateEgypt(),
            CreateMorocco(),
            CreateSenegal(),
            CreateAlgeria(),
            CreateTunisia(),
            CreateNigeria(),
            CreateIvoryCoast(),
            CreateCameroon(),
            CreateGhana(),
            CreateSouthAfrica(),
            CreateMali(),
            CreateDRCongo(),
            CreateBurkinaFaso(),
            CreateGuinea(),
            CreateAngola(),
            CreateUganda(),
            CreateCapeVerde(),
            CreateZambia(),
            CreateGabon(),
            CreateSudan()
        };
    }

    // 1. مصر (Egypt) - 23 لاعباً
    public static Team CreateEgypt()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10001, "محمد الشناوي", 1, SpecificPosition.GK, 60, 78, 18, 86, 86, null),
            (10002, "محمد حمدي", 13, SpecificPosition.LB, 84, 78, 65, 80, 80, new[] { SpecificPosition.LWB }),
            (10003, "محمد عبد المنعم", 24, SpecificPosition.CB, 81, 76, 50, 85, 85, null),
            (10004, "رامي ربيعة", 5, SpecificPosition.CB, 76, 75, 48, 84, 85, null),
            (10005, "محمد هاني", 3, SpecificPosition.RB, 84, 78, 62, 80, 81, new[] { SpecificPosition.RWB }),
            (10006, "مروان عطية", 14, SpecificPosition.CDM, 80, 84, 68, 85, 84, new[] { SpecificPosition.CM }),
            (10007, "حمدي فتحي", 8, SpecificPosition.CDM, 78, 80, 75, 84, 87, new[] { SpecificPosition.CB }),
            (10008, "إمام عاشور", 22, SpecificPosition.CM, 83, 87, 85, 78, 82, new[] { SpecificPosition.CAM }),
            (10009, "محمود حسن تريزيجيه", 7, SpecificPosition.LW, 86, 82, 84, 65, 81, new[] { SpecificPosition.LM, SpecificPosition.ST }),
            (10010, "مصطفى محمد", 19, SpecificPosition.ST, 83, 75, 85, 50, 87, null),
            (10011, "محمد صلاح", 10, SpecificPosition.RW, 89, 88, 92, 50, 76, new[] { SpecificPosition.ST, SpecificPosition.CF })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10012, "مصطفى شوبير", 16, SpecificPosition.GK, 62, 77, 18, 83, 80, null),
            (10013, "ياسر إبراهيم", 6, SpecificPosition.CB, 74, 72, 45, 83, 86, null),
            (10014, "علي جبر", 2, SpecificPosition.CB, 68, 70, 42, 80, 84, null),
            (10015, "عمر كمال عبد الواحد", 4, SpecificPosition.RB, 86, 78, 76, 78, 80, new[] { SpecificPosition.RW, SpecificPosition.LB }),
            (10016, "أحمد فتوح", 12, SpecificPosition.LB, 85, 80, 68, 79, 77, null),
            (10017, "أحمد نبيل كوكا", 17, SpecificPosition.CDM, 80, 81, 70, 79, 78, new[] { SpecificPosition.LB }),
            (10018, "ناصر ماهر", 18, SpecificPosition.CAM, 82, 83, 78, 55, 70, null),
            (10019, "أحمد سيد زيزو", 21, SpecificPosition.RW, 88, 86, 83, 62, 80, new[] { SpecificPosition.LW, SpecificPosition.CM }),
            (10020, "عمر مرموش", 11, SpecificPosition.LW, 91, 84, 88, 50, 78, new[] { SpecificPosition.ST, SpecificPosition.CF }),
            (10021, "أسامة فيصل", 9, SpecificPosition.ST, 82, 72, 79, 45, 82, null),
            (10022, "المهدي سليمان", 23, SpecificPosition.GK, 58, 70, 15, 78, 78, null),
            (10023, "إبراهيم عادل", 15, SpecificPosition.LW, 87, 80, 80, 48, 74, new[] { SpecificPosition.RW })
        };

        return DatabaseHelpers.CreateTeam("NAT_EGY", "مصر", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 2. المغرب (Morocco) - 23 لاعباً
    public static Team CreateMorocco()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10031, "ياسين بونو", 1, SpecificPosition.GK, 62, 82, 18, 89, 85, null),
            (10032, "نصير مزراوي", 3, SpecificPosition.LB, 85, 83, 72, 83, 79, new[] { SpecificPosition.RB, SpecificPosition.LWB }),
            (10033, "نايف أكرد", 5, SpecificPosition.CB, 81, 78, 48, 86, 85, null),
            (10034, "شادي رياض", 4, SpecificPosition.CB, 79, 75, 42, 83, 84, null),
            (10035, "أشرف حكيمي", 2, SpecificPosition.RB, 93, 85, 78, 83, 81, new[] { SpecificPosition.RWB, SpecificPosition.RM }),
            (10036, "سفيان أمرابط", 4, SpecificPosition.CDM, 79, 83, 72, 86, 88, new[] { SpecificPosition.CM }),
            (10037, "عز الدين أوناحي", 8, SpecificPosition.CM, 83, 87, 78, 77, 70, new[] { SpecificPosition.CAM }),
            (10038, "بلال الخنوس", 23, SpecificPosition.CAM, 83, 85, 79, 58, 74, new[] { SpecificPosition.CM }),
            (10039, "سفيان رحيمي", 21, SpecificPosition.LW, 89, 83, 86, 55, 82, new[] { SpecificPosition.ST }),
            (10040, "يوسف النصيري", 19, SpecificPosition.ST, 85, 73, 85, 52, 88, null),
            (10041, "إبراهيم دياز", 10, SpecificPosition.RW, 87, 86, 84, 52, 70, new[] { SpecificPosition.CAM, SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10042, "منير المحمدي", 12, SpecificPosition.GK, 60, 76, 15, 82, 80, null),
            (10043, "عبد الكبير أبقار", 18, SpecificPosition.CB, 78, 74, 40, 82, 84, null),
            (10044, "يحيى عطية الله", 25, SpecificPosition.LB, 83, 79, 66, 80, 80, new[] { SpecificPosition.LWB }),
            (10045, "محمد شيبي", 27, SpecificPosition.RB, 84, 82, 68, 79, 78, new[] { SpecificPosition.RWB }),
            (10046, "أمير ريتشاردسون", 24, SpecificPosition.CM, 79, 82, 75, 80, 84, null),
            (10047, "إسماعيل صيباري", 11, SpecificPosition.CM, 83, 83, 80, 75, 84, new[] { SpecificPosition.CAM }),
            (10048, "إلياس بن صغير", 7, SpecificPosition.LW, 86, 83, 80, 48, 68, new[] { SpecificPosition.CAM }),
            (10049, "حكيم زياش", 22, SpecificPosition.RW, 77, 88, 83, 55, 68, new[] { SpecificPosition.CAM }),
            (10050, "إلياس أخوماش", 17, SpecificPosition.RW, 87, 81, 78, 48, 68, null),
            (10051, "أيوب الكعبي", 9, SpecificPosition.ST, 84, 75, 85, 48, 83, null),
            (10052, "المهدي بنعبيد", 16, SpecificPosition.GK, 58, 72, 15, 79, 79, null),
            (10053, "أسامة العزوزي", 6, SpecificPosition.CDM, 78, 80, 68, 82, 85, new[] { SpecificPosition.CB })
        };

        return DatabaseHelpers.CreateTeam("NAT_MAR", "المغرب", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 3. السنغال (Senegal) - 22 لاعباً
    public static Team CreateSenegal()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10061, "إدوارد ميندي", 16, SpecificPosition.GK, 60, 78, 18, 86, 85, null),
            (10062, "إسماعيل ياكوبس", 14, SpecificPosition.LB, 88, 78, 64, 81, 82, new[] { SpecificPosition.LWB }),
            (10063, "كاليدو كوليبالي", 3, SpecificPosition.CB, 76, 78, 50, 87, 88, null),
            (10064, "عبدو ديالو", 22, SpecificPosition.CB, 78, 79, 48, 83, 82, new[] { SpecificPosition.LB }),
            (10065, "كريبين دياتا", 15, SpecificPosition.RB, 88, 80, 75, 78, 79, new[] { SpecificPosition.RWB, SpecificPosition.RW }),
            (10066, "إدريسا جانا غاي", 5, SpecificPosition.CDM, 78, 83, 72, 85, 83, new[] { SpecificPosition.CM }),
            (10067, "بابي مطر سار", 17, SpecificPosition.CM, 83, 84, 79, 81, 82, new[] { SpecificPosition.CDM }),
            (10068, "لامين كامارا", 25, SpecificPosition.CM, 82, 84, 78, 80, 81, null),
            (10069, "ساديو ماني", 10, SpecificPosition.LW, 86, 83, 85, 55, 78, new[] { SpecificPosition.ST, SpecificPosition.CF }),
            (10070, "نيكولاس جاكسون", 7, SpecificPosition.ST, 88, 78, 84, 45, 83, null),
            (10071, "إسماعيلا سار", 18, SpecificPosition.RW, 91, 80, 80, 50, 80, new[] { SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10072, "موري دياو", 1, SpecificPosition.GK, 60, 74, 15, 80, 80, null),
            (10073, "ميكاييل فاي", 2, SpecificPosition.CB, 80, 75, 45, 80, 83, null),
            (10074, "فورموز ميندي", 4, SpecificPosition.CB, 82, 73, 40, 79, 82, new[] { SpecificPosition.RB }),
            (10075, "فودي بالو توريه", 12, SpecificPosition.LB, 86, 75, 60, 77, 77, null),
            (10076, "نامباليس ميندي", 6, SpecificPosition.CDM, 75, 81, 65, 81, 80, null),
            (10077, "بابي غايي", 26, SpecificPosition.CM, 78, 80, 75, 81, 84, null),
            (10078, "إليمان ندياي", 13, SpecificPosition.CAM, 85, 82, 80, 52, 74, new[] { SpecificPosition.ST, SpecificPosition.RW }),
            (10079, "حبيب ديالو", 20, SpecificPosition.ST, 82, 74, 82, 45, 82, null),
            (10080, "بولاي ديا", 9, SpecificPosition.ST, 85, 77, 83, 48, 81, new[] { SpecificPosition.RW }),
            (10081, "عبد الله سيما", 21, SpecificPosition.RW, 88, 76, 80, 50, 82, null),
            (10082, "سيني ديينغ", 23, SpecificPosition.GK, 58, 70, 15, 77, 78, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_SEN", "السنغال", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 4. الجزائر (Algeria) - 22 لاعباً
    public static Team CreateAlgeria()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10091, "أنتوني ماندريا", 1, SpecificPosition.GK, 60, 78, 18, 83, 82, null),
            (10092, "ريان آيت نوري", 15, SpecificPosition.LB, 87, 84, 72, 82, 78, new[] { SpecificPosition.LWB, SpecificPosition.LM }),
            (10093, "رامي بن سبعيني", 21, SpecificPosition.CB, 79, 80, 68, 84, 84, new[] { SpecificPosition.LB }),
            (10094, "عيسى ماندي", 2, SpecificPosition.CB, 76, 78, 52, 83, 81, new[] { SpecificPosition.RB }),
            (10095, "يوسف عطال", 20, SpecificPosition.RB, 89, 79, 74, 79, 75, new[] { SpecificPosition.RWB, SpecificPosition.RM }),
            (10096, "إسماعيل بن ناصر", 22, SpecificPosition.CDM, 80, 86, 76, 84, 82, new[] { SpecificPosition.CM }),
            (10097, "هشام بوداوي", 6, SpecificPosition.CM, 82, 83, 75, 82, 81, null),
            (10098, "حسام عوار", 11, SpecificPosition.CAM, 82, 86, 82, 65, 74, new[] { SpecificPosition.CM }),
            (10099, "محمد الأمين عمورة", 18, SpecificPosition.LW, 93, 79, 83, 48, 75, new[] { SpecificPosition.ST }),
            (10100, "بغداد بونجاح", 9, SpecificPosition.ST, 82, 76, 86, 55, 87, null),
            (10101, "رياض محرز", 7, SpecificPosition.RW, 81, 89, 86, 48, 68, new[] { SpecificPosition.CAM })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10102, "ألكسيس قندوز", 16, SpecificPosition.GK, 60, 76, 18, 81, 81, null),
            (10103, "محمد أمين توغاي", 4, SpecificPosition.CB, 76, 73, 42, 81, 84, null),
            (10104, "زين الدين بلعيد", 5, SpecificPosition.CB, 76, 74, 45, 81, 83, null),
            (10105, "جوان حجام", 3, SpecificPosition.LB, 85, 77, 62, 78, 77, null),
            (10106, "محمد فارسي", 25, SpecificPosition.RB, 85, 77, 64, 77, 76, null),
            (10107, "رامز زروقي", 14, SpecificPosition.CDM, 76, 81, 72, 81, 82, null),
            (10108, "فارس شايبي", 8, SpecificPosition.CAM, 83, 84, 80, 68, 78, new[] { SpecificPosition.LW, SpecificPosition.CM }),
            (10109, "سعيد بن رحمة", 10, SpecificPosition.LW, 84, 83, 82, 52, 72, null),
            (10110, "أنيس حاج موسى", 17, SpecificPosition.RW, 88, 81, 78, 45, 68, null),
            (10111, "أمين غويري", 19, SpecificPosition.ST, 83, 83, 83, 48, 76, new[] { SpecificPosition.LW }),
            (10112, "أسامة بن بوط", 23, SpecificPosition.GK, 58, 70, 15, 77, 78, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_ALG", "الجزائر", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 5. تونس (Tunisia) - 22 لاعباً
    public static Team CreateTunisia()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10121, "أمان الله مميش", 1, SpecificPosition.GK, 60, 78, 18, 83, 82, null),
            (10122, "علي العابدي", 2, SpecificPosition.LB, 84, 79, 70, 80, 80, new[] { SpecificPosition.LWB }),
            (10123, "منتصر الطالبي", 3, SpecificPosition.CB, 78, 77, 45, 84, 85, null),
            (10124, "ياسين مرياح", 4, SpecificPosition.CB, 75, 76, 52, 83, 85, null),
            (10125, "وجدي كشريدة", 20, SpecificPosition.RB, 85, 78, 62, 79, 78, new[] { SpecificPosition.RWB }),
            (10126, "عيسى العيدوني", 14, SpecificPosition.CDM, 80, 83, 76, 84, 85, new[] { SpecificPosition.CM }),
            (10127, "إلياس السخيري", 17, SpecificPosition.CDM, 76, 84, 76, 85, 84, new[] { SpecificPosition.CM }),
            (10128, "محمد علي بن رمضان", 15, SpecificPosition.CM, 81, 83, 80, 79, 82, new[] { SpecificPosition.CAM }),
            (10129, "حنبعل المجبري", 10, SpecificPosition.CAM, 82, 83, 76, 75, 78, new[] { SpecificPosition.CM, SpecificPosition.LW }),
            (10130, "سيف الدين الجزيري", 11, SpecificPosition.ST, 84, 75, 82, 50, 81, null),
            (10131, "إلياس سعد", 7, SpecificPosition.LW, 86, 80, 80, 52, 75, new[] { SpecificPosition.RW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10132, "بشير بن سعيد", 16, SpecificPosition.GK, 58, 74, 15, 80, 81, null),
            (10133, "ديلان برون", 6, SpecificPosition.CB, 75, 75, 48, 81, 83, new[] { SpecificPosition.RB }),
            (10134, "حمزة الجلاصي", 5, SpecificPosition.CB, 74, 74, 48, 81, 83, null),
            (10135, "علي معلول", 12, SpecificPosition.LB, 78, 85, 75, 78, 74, null),
            (10136, "يان فاليري", 21, SpecificPosition.RB, 85, 76, 58, 78, 80, new[] { SpecificPosition.CB }),
            (10137, "نادر الغندري", 13, SpecificPosition.CDM, 72, 78, 65, 80, 86, new[] { SpecificPosition.CB }),
            (10138, "حسام تقا", 8, SpecificPosition.CM, 82, 82, 75, 80, 80, null),
            (10139, "حمزة رفيعة", 18, SpecificPosition.CAM, 80, 82, 78, 62, 76, new[] { SpecificPosition.CM }),
            (10140, "نعيم السليتي", 23, SpecificPosition.LW, 82, 84, 80, 48, 68, null),
            (10141, "هيثم الجويني", 9, SpecificPosition.ST, 78, 72, 80, 45, 84, null),
            (10142, "أيمن دحمان", 22, SpecificPosition.GK, 58, 72, 15, 78, 78, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_TUN", "تونس", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 6. نيجيريا (Nigeria) - 22 لاعباً
    public static Team CreateNigeria()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10151, "ستانلي نوابالي", 23, SpecificPosition.GK, 60, 78, 18, 84, 84, null),
            (10152, "أولا أينا", 2, SpecificPosition.LB, 86, 80, 68, 81, 80, new[] { SpecificPosition.RB, SpecificPosition.LWB }),
            (10153, "كالفين باسي", 21, SpecificPosition.CB, 84, 76, 45, 84, 88, new[] { SpecificPosition.LB }),
            (10154, "سيمي أجايي", 6, SpecificPosition.CB, 78, 74, 42, 82, 85, null),
            (10155, "برايت أوساي صامويل", 12, SpecificPosition.RB, 91, 77, 65, 80, 81, new[] { SpecificPosition.RWB, SpecificPosition.RM }),
            (10156, "ويلفريد نديدي", 4, SpecificPosition.CDM, 78, 82, 68, 86, 86, new[] { SpecificPosition.CM }),
            (10157, "أليكس إيووبي", 17, SpecificPosition.CM, 82, 85, 79, 75, 78, new[] { SpecificPosition.CAM, SpecificPosition.RW }),
            (10158, "فرانك أونيكا", 8, SpecificPosition.CM, 82, 81, 72, 82, 84, new[] { SpecificPosition.CDM }),
            (10159, "أديمولا لوكمان", 11, SpecificPosition.LW, 88, 84, 85, 52, 74, new[] { SpecificPosition.ST, SpecificPosition.RW }),
            (10160, "فيكتور أوسيمين", 9, SpecificPosition.ST, 90, 76, 89, 48, 88, null),
            (10161, "صامويل تشوكويزي", 15, SpecificPosition.RW, 89, 82, 80, 48, 72, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10162, "مادوكا أوكوي", 1, SpecificPosition.GK, 60, 78, 18, 83, 84, null),
            (10163, "وليام تروست إيكونغ", 5, SpecificPosition.CB, 74, 75, 50, 83, 85, null),
            (10164, "شيدوزي أوازيم", 20, SpecificPosition.CB, 78, 73, 40, 80, 83, new[] { SpecificPosition.RB }),
            (10165, "زايدو سانوسي", 3, SpecificPosition.LB, 88, 73, 58, 78, 80, null),
            (10166, "رافاييل أونيديكا", 14, SpecificPosition.CDM, 78, 81, 70, 81, 83, null),
            (10167, "الحسن يوسف", 18, SpecificPosition.CM, 83, 80, 72, 79, 80, null),
            (10168, "موسيس سيمون", 13, SpecificPosition.LW, 91, 80, 79, 45, 74, null),
            (10169, "فيكتور بونيفاس", 22, SpecificPosition.ST, 86, 78, 86, 48, 88, null),
            (10170, "تايو أيونيي", 19, SpecificPosition.ST, 84, 72, 83, 45, 87, null),
            (10171, "كيليتشي إيهيناتشو", 7, SpecificPosition.ST, 78, 82, 83, 48, 79, new[] { SpecificPosition.CAM }),
            (10172, "فرانسيس أوزوهو", 16, SpecificPosition.GK, 58, 70, 15, 77, 78, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_NGA", "نيجيريا", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 7. كوت ديفوار (Ivory Coast) - 22 لاعباً
    public static Team CreateIvoryCoast()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10181, "يحيى فوفانا", 1, SpecificPosition.GK, 62, 78, 18, 83, 83, null),
            (10182, "جيسلان كونان", 3, SpecificPosition.LB, 85, 78, 62, 80, 80, new[] { SpecificPosition.LWB }),
            (10183, "إيفان نديكا", 21, SpecificPosition.CB, 81, 76, 45, 85, 86, new[] { SpecificPosition.LB }),
            (10184, "أوديلون كوسونو", 7, SpecificPosition.CB, 84, 76, 42, 85, 87, new[] { SpecificPosition.RB }),
            (10185, "ويلفريد سينجو", 17, SpecificPosition.RB, 86, 76, 58, 83, 87, new[] { SpecificPosition.CB, SpecificPosition.RWB }),
            (10186, "فرانك كيسي", 8, SpecificPosition.CDM, 80, 84, 80, 85, 88, new[] { SpecificPosition.CM }),
            (10187, "إبراهيم سنجاري", 18, SpecificPosition.CDM, 76, 82, 74, 84, 88, null),
            (10188, "سيكو فوفانا", 6, SpecificPosition.CM, 82, 83, 82, 81, 87, new[] { SpecificPosition.CAM }),
            (10189, "سيمون أدينغرا", 24, SpecificPosition.LW, 89, 81, 81, 52, 72, new[] { SpecificPosition.RW }),
            (10190, "سيباستيان هالير", 22, SpecificPosition.ST, 76, 78, 85, 52, 88, null),
            (10191, "نيكولاس بيبي", 19, SpecificPosition.RW, 86, 81, 82, 45, 75, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10192, "بادرا علي سانغاري", 16, SpecificPosition.GK, 58, 72, 15, 78, 78, null),
            (10193, "ويلي بولي", 12, SpecificPosition.CB, 72, 73, 45, 82, 87, null),
            (10194, "عثمان ديوماندي", 14, SpecificPosition.CB, 84, 77, 42, 85, 88, null),
            (10195, "سيرج أورييه", 17, SpecificPosition.RB, 80, 78, 68, 80, 82, null),
            (10196, "كريستوفر أوبيري", 25, SpecificPosition.LB, 84, 76, 60, 78, 78, null),
            (10197, "جان ميشيل سيري", 4, SpecificPosition.CM, 74, 85, 74, 79, 74, null),
            (10198, "حامد تراوري", 23, SpecificPosition.CAM, 84, 83, 81, 55, 75, null),
            (10199, "جيريمي بوجا", 10, SpecificPosition.LW, 88, 81, 78, 42, 70, null),
            (10200, "عمر دياكيتي", 13, SpecificPosition.ST, 87, 72, 80, 42, 82, null),
            (10201, "كريم كوناتي", 9, SpecificPosition.ST, 86, 73, 81, 40, 78, null),
            (10202, "تشارلز فولي أياي", 23, SpecificPosition.GK, 56, 68, 15, 75, 76, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_CIV", "كوت ديفوار", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 8. الكاميرون (Cameroon) - 22 لاعباً
    public static Team CreateCameroon()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10211, "أندريه أونانا", 24, SpecificPosition.GK, 62, 86, 20, 86, 84, null),
            (10212, "نوهو تولو", 5, SpecificPosition.LB, 84, 76, 58, 82, 85, new[] { SpecificPosition.CB }),
            (10213, "كريستوفر وو", 4, SpecificPosition.CB, 80, 74, 42, 82, 85, null),
            (10214, "مايكل نغادو نغادجوي", 19, SpecificPosition.CB, 72, 74, 48, 83, 87, null),
            (10215, "جاكسون تشاتشوا", 2, SpecificPosition.RB, 88, 78, 65, 80, 82, new[] { SpecificPosition.RWB, SpecificPosition.RM }),
            (10216, "أندريه فرانك زامبو أنغيسا", 8, SpecificPosition.CM, 82, 85, 77, 85, 86, new[] { SpecificPosition.CDM }),
            (10217, "كارلوس باليبا", 17, SpecificPosition.CDM, 84, 83, 75, 83, 84, new[] { SpecificPosition.CM }),
            (10218, "مارتن هونغلا", 18, SpecificPosition.CM, 78, 82, 72, 82, 83, new[] { SpecificPosition.CDM }),
            (10219, "جورج كيفين نكودو", 11, SpecificPosition.LW, 89, 80, 81, 48, 74, new[] { SpecificPosition.LM }),
            (10220, "فينسنت أبو بكر", 10, SpecificPosition.ST, 80, 78, 85, 50, 87, null),
            (10221, "برايان مبيومو", 20, SpecificPosition.RW, 88, 83, 84, 55, 82, new[] { SpecificPosition.ST, SpecificPosition.CAM })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10222, "فابريس أوندوا", 1, SpecificPosition.GK, 58, 74, 15, 80, 80, null),
            (10223, "هارولد موكودي", 6, SpecificPosition.CB, 74, 72, 40, 81, 86, null),
            (10224, "جان شارل كاستيليتو", 21, SpecificPosition.CB, 78, 74, 45, 82, 84, new[] { SpecificPosition.RB }),
            (10225, "دارلين يونغوا", 14, SpecificPosition.LB, 85, 75, 60, 76, 76, null),
            (10226, "كيلين كيفو", 25, SpecificPosition.RB, 84, 75, 55, 77, 78, null),
            (10227, "إيفان نيو", 15, SpecificPosition.CDM, 76, 78, 65, 80, 83, null),
            (10228, "بيير كوندي", 13, SpecificPosition.CM, 78, 80, 76, 78, 81, null),
            (10229, "مومي نغومالو", 7, SpecificPosition.RW, 85, 79, 78, 55, 78, new[] { SpecificPosition.LW }),
            (10230, "فرانك ماجري", 9, SpecificPosition.ST, 84, 74, 80, 48, 83, null),
            (10231, "فارس مومبانيه", 22, SpecificPosition.ST, 84, 68, 79, 45, 86, null),
            (10232, "ديفيس إيباسي", 16, SpecificPosition.GK, 58, 70, 15, 77, 78, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_CMR", "الكاميرون", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 9. غانا (Ghana) - 22 لاعباً
    public static Team CreateGhana()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10241, "لورانس أتي زيجي", 1, SpecificPosition.GK, 60, 77, 18, 82, 81, null),
            (10242, "جدعون مينساه", 14, SpecificPosition.LB, 85, 78, 62, 79, 78, new[] { SpecificPosition.LWB }),
            (10243, "محمد ساليسو", 4, SpecificPosition.CB, 78, 74, 42, 84, 87, null),
            (10244, "ألكسندر دجيكو", 23, SpecificPosition.CB, 78, 78, 50, 84, 85, null),
            (10245, "طارق طارق لامبتي", 2, SpecificPosition.RB, 91, 78, 65, 79, 72, new[] { SpecificPosition.RWB, SpecificPosition.RM }),
            (10246, "توماس بارتي", 5, SpecificPosition.CDM, 74, 85, 80, 85, 85, new[] { SpecificPosition.CM }),
            (10247, "إليشا أوسو", 8, SpecificPosition.CM, 80, 82, 72, 81, 83, new[] { SpecificPosition.CDM }),
            (10248, "محمد قدوس", 20, SpecificPosition.CAM, 88, 84, 85, 68, 84, new[] { SpecificPosition.RW, SpecificPosition.LW }),
            (10249, "أنطوان سيمينيو", 25, SpecificPosition.RW, 87, 79, 82, 58, 83, new[] { SpecificPosition.LW, SpecificPosition.ST }),
            (10250, "إينياكي ويليامز", 19, SpecificPosition.ST, 91, 79, 83, 50, 84, new[] { SpecificPosition.RW }),
            (10251, "جوردان أيو", 9, SpecificPosition.LW, 82, 81, 81, 62, 80, new[] { SpecificPosition.ST, SpecificPosition.RW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10252, "جوزيف وللاكوت", 16, SpecificPosition.GK, 58, 72, 15, 78, 78, null),
            (10253, "جيروم أوبوكو", 3, SpecificPosition.CB, 77, 72, 40, 80, 84, null),
            (10254, "أليدو سيدو", 15, SpecificPosition.RB, 84, 76, 55, 80, 82, new[] { SpecificPosition.CB }),
            (10255, "إبراهيم عثمان", 11, SpecificPosition.LW, 92, 76, 77, 42, 74, null),
            (10256, "ماجيد أشيميرو", 6, SpecificPosition.CM, 82, 81, 74, 78, 78, null),
            (10257, "إدريسو بابا", 21, SpecificPosition.CDM, 75, 79, 66, 81, 83, null),
            (10258, "كمال الدين سوليمانا", 22, SpecificPosition.LW, 92, 77, 76, 42, 70, null),
            (10259, "إرنست نواماه", 7, SpecificPosition.RW, 89, 77, 76, 45, 74, null),
            (10260, "براندون توماس أسانتي", 18, SpecificPosition.ST, 84, 72, 78, 45, 81, null),
            (10261, "عبد الفتاو إساهاكو", 17, SpecificPosition.RW, 90, 80, 80, 48, 75, null),
            (10262, "ريتشارد أوفوري", 12, SpecificPosition.GK, 56, 70, 15, 77, 78, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_GHA", "غانا", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 10. جنوب إفريقيا (South Africa) - 22 لاعباً
    public static Team CreateSouthAfrica()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10271, "رونوين ويليامز", 1, SpecificPosition.GK, 62, 82, 18, 85, 83, null),
            (10272, "أوبري موديبا", 6, SpecificPosition.LB, 84, 82, 74, 79, 78, new[] { SpecificPosition.LWB, SpecificPosition.LM }),
            (10273, "جرانت كيكانا", 4, SpecificPosition.CB, 78, 76, 45, 83, 85, null),
            (10274, "موتوبي مفالا", 14, SpecificPosition.CB, 76, 74, 45, 83, 87, new[] { SpecificPosition.CDM }),
            (10275, "خوليسو موداو", 20, SpecificPosition.RB, 88, 81, 68, 81, 82, new[] { SpecificPosition.RWB, SpecificPosition.RM }),
            (10276, "تيبوهو موكوينا", 13, SpecificPosition.CDM, 80, 85, 83, 83, 84, new[] { SpecificPosition.CM }),
            (10277, "سبفيلو سيثولي", 15, SpecificPosition.CDM, 79, 81, 72, 82, 84, new[] { SpecificPosition.CM }),
            (10278, "ثيمبا زواني", 11, SpecificPosition.CAM, 80, 85, 81, 55, 74, new[] { SpecificPosition.LW }),
            (10279, "إلياس موكوانا", 7, SpecificPosition.RW, 88, 79, 78, 48, 75, null),
            (10280, "لايل فوستر", 9, SpecificPosition.ST, 85, 76, 83, 48, 84, null),
            (10281, "أوزوين أبوليس", 17, SpecificPosition.LW, 86, 81, 79, 52, 72, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10282, "فيلي موثوا", 16, SpecificPosition.GK, 58, 72, 15, 78, 78, null),
            (10283, "سيابونغا نغيزانا", 5, SpecificPosition.CB, 80, 73, 40, 81, 84, null),
            (10284, "نكوسيناتسي سيبيسي", 2, SpecificPosition.CB, 78, 72, 38, 79, 82, null),
            (10285, "تيرانس ماشيجو", 18, SpecificPosition.LB, 85, 76, 60, 77, 76, null),
            (10286, "ثابيلو مورينا", 23, SpecificPosition.RB, 91, 78, 76, 65, 78, new[] { SpecificPosition.RW }),
            (10287, "ثالانثي مباثا", 8, SpecificPosition.CM, 80, 81, 75, 78, 80, null),
            (10288, "جايدن آدامز", 12, SpecificPosition.CM, 80, 80, 74, 76, 78, null),
            (10289, "باتريك ماسوانجانيي", 10, SpecificPosition.CAM, 83, 83, 80, 52, 74, new[] { SpecificPosition.LW }),
            (10290, "إقبال راينرز", 21, SpecificPosition.ST, 86, 75, 81, 45, 79, null),
            (10291, "بيرسي تاو", 19, SpecificPosition.RW, 86, 82, 81, 55, 75, new[] { SpecificPosition.ST, SpecificPosition.LW }),
            (10292, "ريكاردو غوس", 22, SpecificPosition.GK, 56, 68, 15, 75, 76, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_RSA", "جنوب إفريقيا", FormationType.Formation4231_Wide, true, starters, bench);
    }

    // 11. مالي (Mali) - 22 لاعباً
    public static Team CreateMali()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10301, "دجيجي ديارا", 16, SpecificPosition.GK, 60, 78, 18, 83, 82, null),
            (10302, "داودا جيندو", 3, SpecificPosition.LB, 85, 77, 62, 79, 78, null),
            (10303, "كيكي كوياتي", 5, SpecificPosition.CB, 79, 72, 40, 83, 87, null),
            (10304, "عبد الله ديابي", 6, SpecificPosition.CB, 78, 73, 40, 82, 85, null),
            (10305, "هاماري تراوري", 2, SpecificPosition.RB, 84, 80, 66, 81, 80, new[] { SpecificPosition.RWB }),
            (10306, "إيف بيسوما", 8, SpecificPosition.CDM, 82, 84, 75, 85, 85, new[] { SpecificPosition.CM }),
            (10307, "محمد كامارا", 4, SpecificPosition.CDM, 81, 83, 72, 84, 84, null),
            (10308, "أمادو هايدارا", 19, SpecificPosition.CM, 82, 84, 78, 82, 83, null),
            (10309, "نيني دورجيليس", 7, SpecificPosition.LW, 88, 80, 79, 50, 75, new[] { SpecificPosition.ST }),
            (10310, "إبراهيما كوني", 9, SpecificPosition.ST, 80, 72, 82, 45, 87, null),
            (10311, "أداما تراوري نوس", 14, SpecificPosition.RW, 85, 83, 80, 52, 76, new[] { SpecificPosition.CAM })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10312, "إسماعيل دياوارا", 1, SpecificPosition.GK, 58, 72, 15, 78, 78, null),
            (10313, "مامادو فوفانا", 15, SpecificPosition.CB, 76, 73, 42, 80, 83, null),
            (10314, "فودي فوديه فودي", 21, SpecificPosition.CB, 78, 71, 38, 78, 82, null),
            (10315, "أمادو دانتي", 13, SpecificPosition.LB, 84, 75, 58, 77, 77, null),
            (10316, "فالاي ساكو", 17, SpecificPosition.RB, 84, 76, 62, 78, 80, null),
            (10317, "أليو ديانج", 12, SpecificPosition.CDM, 80, 81, 74, 84, 87, new[] { SpecificPosition.CM }),
            (10318, "لاسانا كوليبالي", 18, SpecificPosition.CM, 78, 81, 72, 80, 83, null),
            (10319, "كاموري دومبيا", 10, SpecificPosition.CAM, 83, 84, 81, 55, 74, null),
            (10320, "سيكو كويتا", 20, SpecificPosition.ST, 86, 78, 81, 45, 76, null),
            (10321, "إل بلال توريه", 11, SpecificPosition.ST, 87, 74, 83, 45, 83, null),
            (10322, "أبو بكر دومبيا", 22, SpecificPosition.GK, 56, 68, 15, 75, 76, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_MLI", "مالي", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 12. الكونغو الديمقراطية (DR Congo) - 22 لاعباً
    public static Team CreateDRCongo()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10331, "ديميتري بيرتو", 16, SpecificPosition.GK, 60, 76, 18, 82, 82, null),
            (10332, "آرتور ماسواكو", 26, SpecificPosition.LB, 84, 82, 72, 79, 78, new[] { SpecificPosition.LWB, SpecificPosition.LM }),
            (10333, "شانسيل مبيمبا", 22, SpecificPosition.CB, 78, 75, 60, 84, 85, new[] { SpecificPosition.RB }),
            (10334, "هينوك إينونغا باكا", 2, SpecificPosition.CB, 78, 72, 40, 82, 85, null),
            (10335, "جيديون كالولو", 24, SpecificPosition.RB, 85, 77, 58, 79, 79, null),
            (10336, "صامويل موتوسامي", 8, SpecificPosition.CDM, 80, 82, 72, 82, 84, new[] { SpecificPosition.CM }),
            (10337, "تشارلز بيكيل", 6, SpecificPosition.CM, 80, 81, 74, 81, 83, null),
            (10338, "جايل كاكوتا", 10, SpecificPosition.CAM, 78, 85, 80, 52, 70, new[] { SpecificPosition.RW }),
            (10339, "ثيو بونغوندا", 11, SpecificPosition.RW, 88, 82, 81, 50, 72, new[] { SpecificPosition.LW }),
            (10340, "سيدريك باكامبو", 17, SpecificPosition.ST, 84, 76, 84, 45, 81, null),
            (10341, "يوان ويسا", 20, SpecificPosition.LW, 88, 80, 83, 50, 77, new[] { SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10342, "ليونيل مباسي", 1, SpecificPosition.GK, 60, 74, 15, 80, 80, null),
            (10343, "روكي بوشيري", 4, SpecificPosition.CB, 76, 70, 38, 79, 85, null),
            (10344, "ديلان باتوبينسيكا", 5, SpecificPosition.CB, 78, 73, 40, 80, 83, null),
            (10345, "يوريس كاييمبي", 12, SpecificPosition.LB, 85, 78, 68, 77, 76, null),
            (10346, "إيدو كايمبي", 14, SpecificPosition.CM, 80, 81, 76, 79, 81, null),
            (10347, "نغالاييل موكاو", 18, SpecificPosition.CDM, 78, 78, 66, 78, 80, null),
            (10348, "ميشاك إيليا", 13, SpecificPosition.RW, 92, 78, 78, 50, 75, new[] { SpecificPosition.ST }),
            (10349, "فيستون ماييلي", 19, SpecificPosition.ST, 85, 73, 82, 45, 84, null),
            (10350, "سيمون بانزا", 23, SpecificPosition.ST, 83, 75, 84, 45, 84, null),
            (10351, "سيلاس كاتفومبا مفومبا", 15, SpecificPosition.RW, 91, 78, 80, 48, 81, null),
            (10352, "باجيو سيادي", 21, SpecificPosition.GK, 56, 68, 15, 75, 76, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_COD", "الكونغو الديمقراطية", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 13. بوركينا فاسو (Burkina Faso) - 22 لاعباً
    public static Team CreateBurkinaFaso()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10361, "هيرفي كوفي", 16, SpecificPosition.GK, 62, 78, 18, 83, 82, null),
            (10362, "ستيف ياجو", 25, SpecificPosition.LB, 82, 76, 58, 80, 82, new[] { SpecificPosition.RB, SpecificPosition.CB }),
            (10363, "إدموند تابسوبا", 12, SpecificPosition.CB, 84, 82, 55, 86, 85, null),
            (10364, "إيسوفو دايو", 14, SpecificPosition.CB, 72, 74, 52, 83, 87, null),
            (10365, "عيسى كابوري", 9, SpecificPosition.RB, 89, 78, 62, 79, 80, new[] { SpecificPosition.RWB, SpecificPosition.RM }),
            (10366, "بلاتي توريه", 20, SpecificPosition.CDM, 80, 83, 72, 84, 83, new[] { SpecificPosition.CM }),
            (10367, "أداما غيرا", 18, SpecificPosition.CM, 76, 80, 68, 80, 82, null),
            (10368, "جوستافو سانجاري", 22, SpecificPosition.CM, 80, 82, 75, 78, 79, null),
            (10369, "بيرتراند تراوري", 10, SpecificPosition.RW, 85, 83, 82, 52, 76, new[] { SpecificPosition.ST }),
            (10370, "محمد كوناتي", 11, SpecificPosition.ST, 82, 72, 82, 45, 86, null),
            (10371, "دانغو واتارا", 15, SpecificPosition.LW, 90, 79, 81, 58, 82, new[] { SpecificPosition.RW, SpecificPosition.LB })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10372, "كيليان نيكييما", 1, SpecificPosition.GK, 58, 72, 15, 77, 78, null),
            (10373, "ناصر دجيغا", 4, SpecificPosition.CB, 80, 73, 40, 80, 83, null),
            (10374, "أداما ناغالو", 5, SpecificPosition.CB, 81, 75, 42, 80, 83, null),
            (10375, "عبد الرزاق غيبريه", 3, SpecificPosition.LB, 84, 75, 58, 76, 76, null),
            (10376, "ساشا بانسي", 6, SpecificPosition.CDM, 78, 79, 70, 79, 81, null),
            (10377, "ستيفان عزيز كي", 17, SpecificPosition.CAM, 82, 82, 80, 52, 74, null),
            (10378, "مامادي بانغري", 8, SpecificPosition.LW, 86, 78, 76, 45, 72, null),
            (10379, "سيدريك بادولو", 21, SpecificPosition.CAM, 83, 81, 77, 52, 72, null),
            (10380, "حسن باندي", 19, SpecificPosition.ST, 85, 75, 79, 45, 78, new[] { SpecificPosition.LW }),
            (10381, "عبد الفتاو باسان", 7, SpecificPosition.ST, 83, 72, 78, 42, 81, null),
            (10382, "سفيان فريد ويدراوغو", 23, SpecificPosition.GK, 56, 68, 15, 75, 76, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_BFA", "بوركينا فاسو", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 14. غينيا (Guinea) - 22 لاعباً
    public static Team CreateGuinea()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10391, "إبراهيم كوني", 22, SpecificPosition.GK, 60, 76, 18, 81, 82, null),
            (10392, "إيسياجا سيلا", 3, SpecificPosition.LB, 83, 77, 62, 80, 81, new[] { SpecificPosition.LWB }),
            (10393, "مختار دياكابي", 5, SpecificPosition.CB, 76, 74, 45, 83, 87, null),
            (10394, "جوليان جانفيير", 4, SpecificPosition.CB, 74, 72, 40, 82, 84, null),
            (10395, "أنطوان كونت", 2, SpecificPosition.RB, 84, 76, 58, 79, 80, null),
            (10396, "أمادو دياوارا", 6, SpecificPosition.CDM, 76, 82, 70, 82, 83, null),
            (10397, "موري كوناتي", 8, SpecificPosition.CDM, 76, 80, 68, 81, 85, null),
            (10398, "نابي كيتا", 18, SpecificPosition.CM, 78, 85, 80, 77, 72, new[] { SpecificPosition.CAM }),
            (10399, "أغويبو كامارا", 10, SpecificPosition.CAM, 84, 83, 80, 62, 76, new[] { SpecificPosition.RW, SpecificPosition.LW }),
            (10400, "سيرهو غيراسي", 9, SpecificPosition.ST, 84, 78, 88, 52, 87, null),
            (10401, "مورغان غيلافوغي", 11, SpecificPosition.RW, 86, 78, 80, 52, 80, new[] { SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10402, "موسى كامارا", 1, SpecificPosition.GK, 58, 72, 15, 78, 78, null),
            (10403, "سيدو سو", 13, SpecificPosition.CB, 78, 73, 40, 80, 84, null),
            (10404, "محمد علي كامارا", 17, SpecificPosition.CB, 78, 73, 40, 81, 84, null),
            (10405, "سيكو سيلا", 12, SpecificPosition.LB, 84, 75, 58, 76, 76, null),
            (10406, "عبد الله توريه", 23, SpecificPosition.CDM, 75, 80, 74, 82, 86, null),
            (10407, "سيدوبا سيسي", 20, SpecificPosition.CM, 80, 80, 74, 76, 78, null),
            (10408, "أليكس موريبا", 15, SpecificPosition.CM, 80, 82, 78, 78, 82, null),
            (10409, "فرانسوا كامانو", 7, SpecificPosition.LW, 86, 78, 78, 48, 76, null),
            (10410, "محمد بايو", 19, SpecificPosition.ST, 82, 70, 80, 42, 84, null),
            (10411, "فاكون بايو", 24, SpecificPosition.ST, 82, 70, 78, 40, 83, null),
            (10412, "ألي كايتا", 16, SpecificPosition.GK, 56, 68, 15, 75, 76, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_GUI", "غينيا", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 15. أنغولا (Angola) - 22 لاعباً
    public static Team CreateAngola()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10421, "نيبلو", 1, SpecificPosition.GK, 60, 76, 18, 82, 83, null),
            (10422, "تو كارنيرو", 13, SpecificPosition.LB, 83, 77, 60, 78, 78, null),
            (10423, "كيالوندا جاسبار", 6, SpecificPosition.CB, 78, 74, 42, 83, 86, null),
            (10424, "جوناثان بواتو", 3, SpecificPosition.CB, 76, 72, 40, 82, 85, null),
            (10425, "إدي أفونسو", 21, SpecificPosition.RB, 84, 77, 62, 78, 79, new[] { SpecificPosition.LB }),
            (10426, "شو", 23, SpecificPosition.CDM, 79, 82, 72, 83, 84, new[] { SpecificPosition.CM }),
            (10427, "فريدي", 16, SpecificPosition.CM, 78, 84, 78, 78, 76, new[] { SpecificPosition.CAM }),
            (10428, "مانويل كيلانو", 17, SpecificPosition.CM, 80, 81, 74, 79, 81, null),
            (10429, "جيلسون دالا", 10, SpecificPosition.LW, 87, 82, 84, 52, 76, new[] { SpecificPosition.ST }),
            (10430, "مابولولو", 19, SpecificPosition.ST, 82, 74, 85, 52, 86, null),
            (10431, "زيتو لوفومبو", 7, SpecificPosition.RW, 90, 79, 80, 48, 76, new[] { SpecificPosition.LW, SpecificPosition.ST })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10432, "أنتونيو دومينغوس", 12, SpecificPosition.GK, 58, 72, 15, 78, 78, null),
            (10433, "كلينتون ماتا", 2, SpecificPosition.CB, 80, 76, 55, 80, 82, new[] { SpecificPosition.RB }),
            (10434, "دافيد كارمو", 5, SpecificPosition.CB, 77, 74, 45, 82, 86, null),
            (10435, "نوريو فورتونا", 14, SpecificPosition.LB, 83, 76, 62, 77, 78, null),
            (10436, "إستريلا", 8, SpecificPosition.CM, 78, 79, 72, 78, 80, null),
            (10437, "بيني موكندي", 4, SpecificPosition.CDM, 78, 78, 68, 79, 81, null),
            (10438, "جيلبرتو", 20, SpecificPosition.RW, 88, 78, 77, 45, 74, null),
            (10439, "ميلسون", 11, SpecificPosition.LW, 88, 80, 78, 45, 72, null),
            (10440, "زيني", 9, SpecificPosition.ST, 84, 72, 80, 42, 82, null),
            (10441, "تشيكو بانزا", 15, SpecificPosition.ST, 84, 71, 78, 40, 80, null),
            (10442, "جيلسون", 22, SpecificPosition.GK, 56, 68, 15, 75, 75, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_ANG", "أنغولا", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 16. أوغندا (Uganda) - 22 لاعباً
    public static Team CreateUganda()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10451, "إسماعيل واتينغا", 19, SpecificPosition.GK, 60, 76, 18, 81, 81, null),
            (10452, "عزيز كايوندو", 13, SpecificPosition.LB, 84, 77, 60, 78, 78, null),
            (10453, "بيفيس موجابي", 5, SpecificPosition.CB, 76, 72, 42, 82, 86, null),
            (10454, "هاليدي لوالوا", 4, SpecificPosition.CB, 75, 72, 40, 81, 84, null),
            (10455, "كينيث سيماكولا", 2, SpecificPosition.RB, 82, 76, 58, 79, 80, new[] { SpecificPosition.CB, SpecificPosition.CDM }),
            (10456, "خالد أوتشو", 8, SpecificPosition.CDM, 76, 81, 70, 82, 84, new[] { SpecificPosition.CM }),
            (10457, "بوبوسي بياكاهانجا", 6, SpecificPosition.CM, 80, 80, 72, 79, 81, null),
            (10458, "ترافيس موتيابا", 10, SpecificPosition.CAM, 85, 83, 79, 52, 70, new[] { SpecificPosition.RW }),
            (10459, "دينيس أوميدي", 17, SpecificPosition.RW, 87, 78, 80, 48, 76, null),
            (10460, "محمد شعبان", 9, SpecificPosition.ST, 83, 72, 81, 45, 82, null),
            (10461, "روجرز ماتو", 11, SpecificPosition.LW, 88, 77, 78, 48, 75, null)
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10462, "تشارلز لوكواغو", 1, SpecificPosition.GK, 58, 72, 15, 78, 78, null),
            (10463, "تيموثي أوواني", 15, SpecificPosition.CB, 75, 71, 38, 79, 83, null),
            (10464, "إسحاق موليمي", 3, SpecificPosition.LB, 82, 74, 55, 76, 76, null),
            (10465, "نيكولاس وادادا", 14, SpecificPosition.RB, 83, 75, 58, 77, 77, null),
            (10466, "رونالد سيكيجاندا", 18, SpecificPosition.CM, 78, 78, 70, 77, 79, null),
            (10467, "ألان أوكيلو", 20, SpecificPosition.CAM, 82, 81, 76, 50, 70, null),
            (10468, "جود سيمبامبو", 7, SpecificPosition.RW, 86, 76, 75, 45, 74, null),
            (10469, "ستيفن موكوالا", 21, SpecificPosition.ST, 84, 71, 79, 42, 82, null),
            (10470, "فهد بايو", 22, SpecificPosition.ST, 81, 70, 78, 42, 83, null),
            (10471, "جويل سيرونجوجي", 12, SpecificPosition.CDM, 76, 76, 65, 78, 80, null),
            (10472, "نفالي مافابي", 23, SpecificPosition.GK, 56, 68, 15, 75, 75, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_UGA", "أوغندا", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 17. الرأس الأخضر (Cape Verde) - 22 لاعباً
    public static Team CreateCapeVerde()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10481, "فوزينيا", 1, SpecificPosition.GK, 60, 78, 18, 83, 82, null),
            (10482, "جواو باولو فيرنانديز", 8, SpecificPosition.LB, 84, 79, 68, 79, 78, new[] { SpecificPosition.CM }),
            (10483, "لوغان كوستا", 4, SpecificPosition.CB, 81, 75, 45, 84, 87, null),
            (10484, "روبرتو لوبيز بيكو", 5, SpecificPosition.CB, 76, 74, 45, 82, 85, null),
            (10485, "ستيفين موريرا", 23, SpecificPosition.RB, 85, 78, 62, 80, 81, new[] { SpecificPosition.CB }),
            (10486, "كيفين بينا", 26, SpecificPosition.CDM, 79, 82, 75, 83, 84, null),
            (10487, "جاميرو مونتيرو", 10, SpecificPosition.CM, 82, 83, 78, 79, 78, new[] { SpecificPosition.CAM }),
            (10488, "ديروي دوارتي", 18, SpecificPosition.CM, 80, 82, 76, 78, 80, null),
            (10489, "ريان مينديز", 7, SpecificPosition.LW, 86, 82, 82, 52, 75, new[] { SpecificPosition.RW, SpecificPosition.ST }),
            (10490, "بيبي", 20, SpecificPosition.ST, 84, 77, 83, 50, 83, null),
            (10491, "جوفاني كابرال", 11, SpecificPosition.RW, 88, 80, 81, 48, 77, new[] { SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10492, "مارسيو روزا", 12, SpecificPosition.GK, 58, 72, 15, 78, 78, null),
            (10493, "ستوبيرا", 2, SpecificPosition.CB, 72, 74, 50, 80, 83, new[] { SpecificPosition.LB }),
            (10494, "ديلان تافاريس", 16, SpecificPosition.LB, 83, 75, 58, 77, 77, null),
            (10495, "ويلي سيميدو", 77, SpecificPosition.RW, 87, 78, 77, 50, 75, null),
            (10496, "باتريك أندرادي", 6, SpecificPosition.CM, 78, 80, 74, 78, 80, null),
            (10497, "كينيت روشا ألفاريس", 14, SpecificPosition.CM, 80, 79, 74, 77, 78, null),
            (10498, "لاروس دوارتي", 21, SpecificPosition.CAM, 82, 81, 77, 52, 74, null),
            (10499, "جيلسون تافاريس", 9, SpecificPosition.ST, 85, 72, 79, 42, 82, null),
            (10500, "دايلون روشا ليفامينتو", 17, SpecificPosition.ST, 84, 73, 80, 42, 81, null),
            (10501, "تياجو كوريا بيبيتينهو", 19, SpecificPosition.LW, 86, 76, 76, 45, 73, null),
            (10502, "برونو فاريلا", 24, SpecificPosition.GK, 60, 76, 18, 82, 82, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_CPV", "الرأس الأخضر", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 18. زامبيا (Zambia) - 22 لاعباً
    public static Team CreateZambia()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10511, "تريسور موامبا ملينجا", 16, SpecificPosition.GK, 60, 76, 18, 82, 82, null),
            (10512, "لوبامبو موسوندا", 8, SpecificPosition.LB, 87, 78, 68, 78, 76, new[] { SpecificPosition.LM, SpecificPosition.LW }),
            (10513, "ستوبيلا سونزو", 13, SpecificPosition.CB, 68, 73, 50, 83, 87, null),
            (10514, "فرانكي موسوندا", 4, SpecificPosition.CB, 76, 73, 42, 81, 84, null),
            (10515, "بينديكت تشيبيشي", 26, SpecificPosition.RB, 83, 76, 58, 78, 79, null),
            (10516, "إيمانويل باندا", 12, SpecificPosition.CDM, 78, 82, 75, 81, 82, new[] { SpecificPosition.CM }),
            (10517, "كلفين كابومبو", 15, SpecificPosition.CM, 78, 80, 70, 80, 82, null),
            (10518, "كينغز كانغوا", 22, SpecificPosition.CAM, 85, 84, 82, 62, 74, new[] { SpecificPosition.CM }),
            (10519, "لاميك باندا", 11, SpecificPosition.LW, 91, 79, 79, 52, 75, new[] { SpecificPosition.RW }),
            (10520, "باتسون داكا", 20, SpecificPosition.ST, 89, 75, 84, 45, 82, null),
            (10521, "فاشيون ساكالا", 10, SpecificPosition.RW, 89, 80, 83, 50, 79, new[] { SpecificPosition.ST, SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10522, "لورانس مولينجا", 1, SpecificPosition.GK, 58, 72, 15, 78, 78, null),
            (10523, "كاباسو تشونجو", 5, SpecificPosition.CB, 74, 71, 40, 79, 83, null),
            (10524, "دومينيك تشاندا", 2, SpecificPosition.CB, 76, 70, 38, 78, 82, null),
            (10525, "رودريك كابوي", 23, SpecificPosition.LB, 82, 76, 62, 76, 76, null),
            (10526, "بينسون ساكالا", 6, SpecificPosition.CDM, 76, 78, 66, 78, 82, null),
            (10527, "غولدن مافوينتا", 14, SpecificPosition.CM, 78, 79, 72, 78, 80, null),
            (10528, "كليفورد مولينجا", 17, SpecificPosition.CAM, 80, 80, 76, 50, 72, null),
            (10529, "إدوارد تشيلوفيا", 7, SpecificPosition.RW, 88, 77, 76, 48, 74, null),
            (10530, "كينيدي موسوندا", 9, SpecificPosition.ST, 84, 72, 80, 42, 82, null),
            (10531, "غيلفت كامبامبا", 19, SpecificPosition.ST, 82, 70, 78, 40, 80, null),
            (10532, "فرانسيس موانزا", 18, SpecificPosition.GK, 56, 68, 15, 74, 75, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_ZAM", "زامبيا", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 19. الجابون (Gabon) - 22 لاعباً
    public static Team CreateGabon()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10541, "أنطوني مفا ميزوي", 1, SpecificPosition.GK, 60, 76, 18, 81, 81, null),
            (10542, "جاك إيكومييه", 3, SpecificPosition.LB, 84, 76, 58, 78, 78, null),
            (10543, "برونو إيكويلي مانجا", 5, SpecificPosition.CB, 68, 75, 48, 82, 85, null),
            (10544, "آرون أبيندانغوي", 4, SpecificPosition.CB, 76, 73, 42, 82, 85, null),
            (10545, "أنتوني أويونو", 2, SpecificPosition.RB, 85, 77, 60, 78, 78, null),
            (10546, "ماريو ليمينا", 18, SpecificPosition.CDM, 81, 84, 78, 84, 86, new[] { SpecificPosition.CM, SpecificPosition.CB }),
            (10547, "ديدييه إبراهيم ندونغ", 22, SpecificPosition.CM, 80, 82, 75, 81, 82, new[] { SpecificPosition.CDM }),
            (10548, "غويلور كانغا", 8, SpecificPosition.CAM, 82, 85, 83, 62, 75, new[] { SpecificPosition.CM }),
            (10549, "دينيس بوانغا", 20, SpecificPosition.LW, 89, 82, 86, 55, 80, new[] { SpecificPosition.ST, SpecificPosition.RW }),
            (10550, "بيير إيميريك أوباميانغ", 9, SpecificPosition.ST, 88, 80, 88, 45, 78, null),
            (10551, "جيم أليفيناه", 11, SpecificPosition.RW, 87, 78, 78, 52, 74, new[] { SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10552, "لوي أميكا أوتشانغا", 16, SpecificPosition.GK, 58, 72, 15, 78, 78, null),
            (10553, "سيدني أوبيزا", 6, SpecificPosition.CB, 75, 71, 38, 79, 82, null),
            (10554, "أليكس موكيتو موسوندا", 14, SpecificPosition.CB, 76, 72, 40, 79, 82, null),
            (10555, "ديفيد سامبيسا", 12, SpecificPosition.LB, 84, 75, 58, 76, 76, null),
            (10556, "ميدوين بيتيغي", 17, SpecificPosition.CDM, 78, 78, 68, 79, 80, null),
            (10557, "سيرج مارتينسون نغوالي", 15, SpecificPosition.CM, 78, 79, 72, 77, 79, null),
            (10558, "شافي بابيكا", 7, SpecificPosition.RW, 89, 76, 76, 42, 74, null),
            (10559, "ألان دوزييه", 10, SpecificPosition.LW, 85, 78, 76, 45, 72, null),
            (10560, "أكسل مايي", 19, SpecificPosition.ST, 82, 71, 78, 42, 82, null),
            (10561, "أورفيل مبولاي", 21, SpecificPosition.ST, 83, 70, 77, 40, 80, null),
            (10562, "جان نويل أمونومي", 23, SpecificPosition.GK, 56, 68, 15, 75, 76, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_GAB", "الجابون", FormationType.Formation433_Holding, true, starters, bench);
    }

    // 20. السودان (Sudan) - 22 لاعباً
    public static Team CreateSudan()
    {
        var starters = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10571, "محمد مصطفى", 16, SpecificPosition.GK, 60, 78, 18, 83, 82, null),
            (10572, "بخيت خميس", 3, SpecificPosition.LB, 83, 77, 60, 79, 79, null),
            (10573, "مصطفى كرشوم", 4, SpecificPosition.CB, 78, 76, 45, 84, 85, null),
            (10574, "عبد الرحمن كوكو", 5, SpecificPosition.CB, 78, 74, 40, 82, 85, null),
            (10575, "رمضان عجب", 2, SpecificPosition.RB, 83, 78, 68, 80, 80, new[] { SpecificPosition.RW, SpecificPosition.ST }),
            (10576, "والي الدين خضر بوغبا", 6, SpecificPosition.CDM, 80, 83, 76, 84, 85, new[] { SpecificPosition.CM }),
            (10577, "أبو عاقلة عبد الله", 8, SpecificPosition.CM, 80, 82, 74, 82, 83, new[] { SpecificPosition.CDM }),
            (10578, "صلاح عادل", 14, SpecificPosition.CM, 79, 80, 72, 81, 82, null),
            (10579, "ياسر مزمل", 17, SpecificPosition.RW, 89, 78, 79, 52, 78, new[] { SpecificPosition.ST, SpecificPosition.LW }),
            (10580, "محمد عبد الرحمن الغربال", 10, SpecificPosition.ST, 84, 78, 86, 48, 83, null),
            (10581, "سيف تيري", 9, SpecificPosition.ST, 86, 75, 83, 48, 85, new[] { SpecificPosition.LW })
        };

        var bench = new (int, string, int, SpecificPosition, int, int, int, int, int, SpecificPosition[]?)[]
        {
            (10582, "علي أبو عشرين", 1, SpecificPosition.GK, 58, 74, 15, 79, 80, null),
            (10583, "محمد أرنق", 15, SpecificPosition.CB, 76, 73, 40, 81, 84, null),
            (10584, "الطيب عبد الرازق", 12, SpecificPosition.CB, 75, 71, 40, 78, 82, null),
            (10585, "فارس عبد الله", 13, SpecificPosition.LB, 83, 78, 64, 78, 78, null),
            (10586, "عوض زايد", 20, SpecificPosition.RB, 83, 74, 55, 76, 76, null),
            (10587, "عماد الصيني", 21, SpecificPosition.CDM, 77, 78, 68, 79, 82, new[] { SpecificPosition.CB }),
            (10588, "عبد الرؤوف يعقوب", 18, SpecificPosition.CAM, 82, 84, 80, 55, 74, null),
            (10589, "شرف الدين شيبوب", 11, SpecificPosition.CAM, 80, 82, 78, 55, 76, null),
            (10590, "أبو بكر عيسى", 7, SpecificPosition.LW, 87, 79, 78, 48, 76, null),
            (10591, "جون مانو", 22, SpecificPosition.ST, 84, 72, 79, 42, 82, null),
            (10592, "منجد النيل", 23, SpecificPosition.GK, 56, 68, 15, 75, 75, null)
        };

        return DatabaseHelpers.CreateTeam("NAT_SUD", "السودان", FormationType.Formation433_Holding, true, starters, bench);
    }
}
