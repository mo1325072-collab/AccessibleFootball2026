namespace AccessibleFootball2026.Core.Models;

/// <summary>
/// البطولات القارية والدولية الكبرى للمنتخبات (FIFA & Continental Tournaments)
/// تشمل كأس العالم 2026 (48 منتخباً)، كأس الأمم الأوروبية (يورو 24 منتخباً - بدون إسرائيل)،
/// كأس الأمم الإفريقية (24 منتخباً)، كأس آسيا (24 منتخباً)، وكوبا أمريكا (16 منتخباً).
/// استبعاد قطعي ومطلق لأي أثر لمنتخب الكيان الصهيوني.
/// </summary>
public static class InternationalTournaments
{
    /// <summary>
    /// كأس العالم 2026 (FIFA World Cup 2026) - 48 منتخباً موزعاً على 12 مجموعة
    /// استبعاد قطعي وحازم لأي أثر لمنتخب الكيان الصهيوني
    /// </summary>
    public static List<Team> GetWorldCup2026Teams()
    {
        return new List<Team>
        {
            // إفريقيا (CAF) - 10 منتخبات
            AfricanNationalTeams.CreateEgypt(),
            AfricanNationalTeams.CreateMorocco(),
            AfricanNationalTeams.CreateSenegal(),
            AfricanNationalTeams.CreateAlgeria(),
            AfricanNationalTeams.CreateTunisia(),
            AfricanNationalTeams.CreateNigeria(),
            AfricanNationalTeams.CreateIvoryCoast(),
            AfricanNationalTeams.CreateCameroon(),
            AfricanNationalTeams.CreateGhana(),
            AfricanNationalTeams.CreateSouthAfrica(),

            // أوروبا (UEFA) - 16 منتخباً (خالية تماماً من إسرائيل)
            EuropeanNationalTeams.CreateFrance(),
            EuropeanNationalTeams.CreateEngland(),
            EuropeanNationalTeams.CreateSpain(),
            EuropeanNationalTeams.CreateGermany(),
            EuropeanNationalTeams.CreateItaly(),
            EuropeanNationalTeams.CreatePortugal(),
            EuropeanNationalTeams.CreateNetherlands(),
            EuropeanNationalTeams.CreateCroatia(),
            EuropeanNationalTeams.CreateBelgium(),
            EuropeanNationalTeams.CreateTurkey(),
            EuropeanNationalTeams.CreateSwitzerland(),
            EuropeanNationalTeams.CreateDenmark(),
            EuropeanNationalTeams.CreateAustria(),
            EuropeanNationalTeams.CreateNorway(),
            EuropeanNationalTeams.CreatePoland(),
            EuropeanNationalTeams.CreateUkraine(),

            // أمريكا الجنوبية (CONMEBOL) - 7 منتخبات
            SouthAmericanNationalTeams.CreateArgentina(),
            SouthAmericanNationalTeams.CreateBrazil(),
            SouthAmericanNationalTeams.CreateUruguay(),
            SouthAmericanNationalTeams.CreateColombia(),
            SouthAmericanNationalTeams.CreateEcuador(),
            SouthAmericanNationalTeams.CreateChile(),
            SouthAmericanNationalTeams.CreateParaguay(),

            // أمريكا الشمالية والوسطى (CONCACAF) - 6 منتخبات
            NorthAmericanNationalTeams.CreateUSA(),
            NorthAmericanNationalTeams.CreateMexico(),
            NorthAmericanNationalTeams.CreateCanada(),
            NorthAmericanNationalTeams.CreateCostaRica(),
            NorthAmericanNationalTeams.CreatePanama(),
            NorthAmericanNationalTeams.CreateJamaica(),

            // آسيا (AFC) - 8 منتخبات
            AsianNationalTeams.CreateSaudiArabia(),
            AsianNationalTeams.CreateJapan(),
            AsianNationalTeams.CreateSouthKorea(),
            AsianNationalTeams.CreateIran(),
            AsianNationalTeams.CreateAustralia(),
            AsianNationalTeams.CreateQatar(),
            AsianNationalTeams.CreateUAE(),
            AsianNationalTeams.CreateIraq(),

            // أوقيانوسيا (OFC) - منتخب واحد
            OceanianNationalTeams.CreateNewZealand()
        };
    }

    /// <summary>
    /// كأس الأمم الأوروبية (UEFA Euro) - 24 منتخباً
    /// استبعاد تام وصارم لمنتخب الكيان الصهيوني
    /// </summary>
    public static List<Team> GetEuroTeams()
    {
        return EuropeanNationalTeams.GetAllNationalTeams();
    }

    /// <summary>
    /// كأس الأمم الإفريقية (CAF Africa Cup of Nations) - 20 منتخباً
    /// </summary>
    public static List<Team> GetAfconTeams()
    {
        return AfricanNationalTeams.GetAllNationalTeams();
    }

    /// <summary>
    /// كأس آسيا (AFC Asian Cup) - 14 منتخباً
    /// </summary>
    public static List<Team> GetAsianCupTeams()
    {
        return AsianNationalTeams.GetAllNationalTeams();
    }

    /// <summary>
    /// كوبا أمريكا (Copa América) - 16 منتخباً (10 أمريكا الجنوبية + 6 أمريكا الشمالية)
    /// </summary>
    public static List<Team> GetCopaAmericaTeams()
    {
        var list = new List<Team>();
        list.AddRange(SouthAmericanNationalTeams.GetAllNationalTeams());
        list.Add(NorthAmericanNationalTeams.CreateUSA());
        list.Add(NorthAmericanNationalTeams.CreateMexico());
        list.Add(NorthAmericanNationalTeams.CreateCanada());
        list.Add(NorthAmericanNationalTeams.CreateCostaRica());
        list.Add(NorthAmericanNationalTeams.CreatePanama());
        list.Add(NorthAmericanNationalTeams.CreateJamaica());
        return list;
    }
}
