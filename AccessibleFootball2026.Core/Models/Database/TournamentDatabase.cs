namespace AccessibleFootball2026.Core.Models;

/// <summary>
/// واجهة إدارة كافة البطولات القارية والدولية للأندية والمنتخبات لموسم 2026/2027
/// تشمل مسابقات اليويفا والكاف، وكأس العالم واليورو وأمم إفريقيا وكأس آسيا وكوبا أمريكا
/// استبعاد قطعي وحازم لأي أثر لفرق أو منتخبات الكيان الصهيوني
/// </summary>
public static class TournamentDatabase
{
    // بطولات الأندية القارية
    public static List<Team> GetUEFAChampionsLeague() => UEFACompetitions.GetChampionsLeagueClubs();
    public static List<Team> GetUEFAEuropaLeague() => UEFACompetitions.GetEuropaLeagueClubs();
    public static List<Team> GetCAFChampionsLeague() => CAFCompetitions.GetChampionsLeagueClubs();
    public static List<Team> GetCAFConfederationCup() => CAFCompetitions.GetConfederationCupClubs();

    // البطولات الدولية الكبرى للمنتخبات
    public static List<Team> GetWorldCup2026() => InternationalTournaments.GetWorldCup2026Teams();
    public static List<Team> GetEuro() => InternationalTournaments.GetEuroTeams();
    public static List<Team> GetAfcon() => InternationalTournaments.GetAfconTeams();
    public static List<Team> GetAsianCup() => InternationalTournaments.GetAsianCupTeams();
    public static List<Team> GetCopaAmerica() => InternationalTournaments.GetCopaAmericaTeams();

    // =========================================================================
    // دوال التوافقية السابقة لكائنات البطولات (Legacy Tournament Objects)
    // =========================================================================
    public static List<Tournament> GetAllTournaments()
    {
        var list = new List<Tournament>();

        void AddGroupTournament(string id, string defaultTitle, string groupKey, CompetitionType type, string region)
        {
            var teams = JsonDatabaseProvider.GetTeamsByGroup(groupKey);
            if (teams.Count > 0)
            {
                var tourney = new Tournament(id, $"{defaultTitle} ({teams.Count} فريقاً)", type, region);
                tourney.Teams.AddRange(teams);
                list.Add(tourney);
            }
        }

        AddGroupTournament("EPL_EG", "الدوري المصري الممتاز", "Egyptian Premier League", CompetitionType.DomesticLeague, "مصر");
        AddGroupTournament("SPL_SA", "دوري روشن السعودي للمحترفين", "Saudi Pro League", CompetitionType.DomesticLeague, "السعودية");
        AddGroupTournament("EPL_UK", "الدوري الإنجليزي الممتاز", "Premier League (England)", CompetitionType.DomesticLeague, "إنجلترا");
        AddGroupTournament("LALIGA", "الدوري الإسباني لا ليغا", "LaLiga (Spain)", CompetitionType.DomesticLeague, "إسبانيا");
        AddGroupTournament("SERIEA", "الدوري الإيطالي سيري آ", "Serie A (Italy)", CompetitionType.DomesticLeague, "إيطاليا");
        AddGroupTournament("BUNDES", "الدوري الألماني بوندسليغا", "Bundesliga (Germany)", CompetitionType.DomesticLeague, "ألمانيا");
        AddGroupTournament("LIGUE1", "الدوري الفرنسي ليغ 1", "Ligue 1 (France)", CompetitionType.DomesticLeague, "فرنسا");
        AddGroupTournament("CAF_CL", "دوري أبطال إفريقيا", "CAF Champions League (clubs outside Egyptian league)", CompetitionType.KnockoutCup, "إفريقيا");
        AddGroupTournament("CAF_CC", "كأس الكونفدرالية الإفريقية", "CAF Confederation Cup clubs", CompetitionType.KnockoutCup, "إفريقيا");
        AddGroupTournament("UEFA_CL", "دوري أبطال أوروبا", "UEFA Champions League (clubs outside the 7 leagues)", CompetitionType.KnockoutCup, "أوروبا");
        AddGroupTournament("UEFA_EL", "الدوري الأوروبي", "UEFA Europa League (clubs outside the 7 leagues)", CompetitionType.KnockoutCup, "أوروبا");
        AddGroupTournament("UEFA_ECL", "دوري المؤتمر الأوروبي", "UEFA Conference League (clubs outside the 7 leagues)", CompetitionType.KnockoutCup, "أوروبا");
        AddGroupTournament("NAT_ALL", "المنتخبات الوطنية العالمية", "National teams - current squads", CompetitionType.International, "عالمي");

        if (list.Count == 0)
        {
            list.Add(CreateEgyptianPremierLeague());
            list.Add(CreateEnglishPremierLeague());
            list.Add(CreateLaLiga());
            list.Add(CreateUEFAChampionsLeague());
            list.Add(CreateWorldCup2026());
        }

        return list;
    }

    public static Tournament CreateEgyptianPremierLeague()
    {
        var t = new Tournament("EPL_EG", "الدوري المصري الممتاز دوري النيل", CompetitionType.DomesticLeague, "مصر");
        t.Teams.AddRange(EgyptianLeagueDatabase.GetAllClubs());
        return t;
    }

    public static Tournament CreateEnglishPremierLeague()
    {
        var t = new Tournament("EPL_UK", "الدوري الإنجليزي الممتاز بريميرليج", CompetitionType.DomesticLeague, "إنجلترا");
        t.Teams.AddRange(EnglishPremierLeagueDatabase.GetAllClubs());
        return t;
    }

    public static Tournament CreateLaLiga()
    {
        var t = new Tournament("LALIGA", "الدوري الإسباني لا ليجا", CompetitionType.DomesticLeague, "إسبانيا");
        t.Teams.AddRange(SpanishLaLigaDatabase.GetAllClubs());
        return t;
    }

    public static Tournament CreateUEFAChampionsLeague()
    {
        var t = new Tournament("UCL", "دوري أبطال أوروبا 2026", CompetitionType.KnockoutCup, "أوروبا");
        t.Teams.AddRange(GetUEFAChampionsLeague());
        return t;
    }

    public static Tournament CreateWorldCup2026()
    {
        var t = new Tournament("WC2026", "كأس العالم 2026 فيفا", CompetitionType.International, "عالمي");
        t.Teams.AddRange(GetWorldCup2026());
        return t;
    }

    /// <summary>
    /// التحقق الصارم من خلو كافة البطولات من أي فرق أو منتخبات صهيونية
    /// </summary>
    public static bool VerifyZeroExcludedTeams()
    {
        var allTournamentTeams = new List<Team>();
        allTournamentTeams.AddRange(GetUEFAChampionsLeague());
        allTournamentTeams.AddRange(GetUEFAEuropaLeague());
        allTournamentTeams.AddRange(GetCAFChampionsLeague());
        allTournamentTeams.AddRange(GetCAFConfederationCup());
        allTournamentTeams.AddRange(GetWorldCup2026());
        allTournamentTeams.AddRange(GetEuro());
        allTournamentTeams.AddRange(GetAfcon());
        allTournamentTeams.AddRange(GetAsianCup());
        allTournamentTeams.AddRange(GetCopaAmerica());

        foreach (var t in allTournamentTeams)
        {
            var name = t.Name ?? "";
            var id = t.Id ?? "";
            if (name.Contains("إسرائيل") || name.Contains("اسرائيل") || name.Contains("Israel", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Maccabi", StringComparison.OrdinalIgnoreCase) || name.Contains("Hapoel", StringComparison.OrdinalIgnoreCase) ||
                id.Contains("ISR", StringComparison.OrdinalIgnoreCase) || id.Contains("MTA", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
