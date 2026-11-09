using System;
using System.Collections.Generic;
using System.Linq;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Tests;

/// <summary>
/// اختبارات المرحلة الخامسة الشاملة لقاعدة بيانات موسم 2026/2027 (Tests 17A through 17G)
/// تؤكد التفوير الكامل والبدء من الصفر طبقاً لتوجيهات المستخدم السيادية
/// وتضمن 132 نادياً في 7 دوريات كاملة، و20 نادياً لعمالقة العالم، و84 منتخباً عبر القارات الست،
/// وقوائم موسعة (20 إلى 25 لاعباً لكل نادٍ ومنتخب)، والتوافقية الكاملة 100%،
/// والاستبعاد القطعي الصارم لأي أثر لفرق أو منتخبات الكيان الصهيوني.
/// </summary>
public static class Milestone5DatabaseTests
{
    public static void RunAll(Action<string, Action> runTest)
    {
        runTest("17A. 7 Domestic Leagues Ground-Up Rebuild: Exactly 132 Clubs + 20 Rest of World Clubs", Test17A_SevenDomesticLeaguesAndRestOfWorldClubsCount);
        runTest("17B. Comprehensive Squad Depth & Sovereign Governance: 20 to 25 Players Per Club With Zero Truncation", Test17B_ComprehensiveSquadDepthAcrossAllClubs);
        runTest("17C. Continental Club Competitions Pool Integrity & Structure (UEFA & CAF)", Test17C_ContinentalClubCompetitionsPools);
        runTest("17D. FIFA National Teams Across All 6 Continents: Exactly 84 Distinct National Teams", Test17D_NationalTeamsAcrossSixContinents);
        runTest("17E. Major International Tournaments Integrity & Structure (World Cup, Euro, AFCON, Asian Cup, Copa)", Test17E_MajorInternationalTournamentsIntegrity);
        runTest("17F. Sovereign Regulatory Directive: 100% Strict Exclusion of Israel / Israeli Clubs Everywhere", Test17F_StrictExclusionOfIsraelAndIsraeliClubs);
        runTest("17G. Master Facade & Backward Compatibility: All 20 Legacy Factory Methods Operational", Test17G_MasterFacadeAndLegacyFactoryMethodsCompatibility);
    }

    /// <summary>
    /// 17A: التحقق من وجود الدوريات السبعة الكبرى بعدد أنديتها الدقيق (132 نادياً) بالإضافة لـ 20 نادياً لبقية العالم (إجمالي 152 نادياً)
    /// </summary>
    private static void Test17A_SevenDomesticLeaguesAndRestOfWorldClubsCount()
    {
        var egyptian = TeamDatabase.GetClubsByLeague(LeagueType.EgyptianPremierLeague);
        var saudi = TeamDatabase.GetClubsByLeague(LeagueType.SaudiProLeague);
        var english = TeamDatabase.GetClubsByLeague(LeagueType.EnglishPremierLeague);
        var spanish = TeamDatabase.GetClubsByLeague(LeagueType.SpanishLaLiga);
        var italian = TeamDatabase.GetClubsByLeague(LeagueType.ItalianSerieA);
        var german = TeamDatabase.GetClubsByLeague(LeagueType.GermanBundesliga);
        var french = TeamDatabase.GetClubsByLeague(LeagueType.FrenchLigue1);
        var restOfWorld = TeamDatabase.GetClubsByLeague(LeagueType.RestOfWorld);

        if (egyptian.Count != 18) throw new Exception($"Egyptian Premier League expected 18 clubs, got {egyptian.Count}");
        if (saudi.Count != 18) throw new Exception($"Saudi Pro League expected 18 clubs, got {saudi.Count}");
        if (english.Count != 20) throw new Exception($"English Premier League expected 20 clubs, got {english.Count}");
        if (spanish.Count != 20) throw new Exception($"Spanish La Liga expected 20 clubs, got {spanish.Count}");
        if (italian.Count != 20) throw new Exception($"Italian Serie A expected 20 clubs, got {italian.Count}");
        if (german.Count != 18) throw new Exception($"German Bundesliga expected 18 clubs, got {german.Count}");
        if (french.Count != 18) throw new Exception($"French Ligue 1 expected 18 clubs, got {french.Count}");
        if (restOfWorld.Count != 20) throw new Exception($"Rest of World expected 20 clubs, got {restOfWorld.Count}");

        int domesticTotal = egyptian.Count + saudi.Count + english.Count + spanish.Count + italian.Count + german.Count + french.Count;
        if (domesticTotal != 132) throw new Exception($"Total domestic league clubs must be exactly 132, got {domesticTotal}");

        var allClubs = TeamDatabase.GetAllClubs();
        if (allClubs.Count != 152) throw new Exception($"Total clubs in TeamDatabase must be exactly 152 (132 domestic + 20 RestOfWorld), got {allClubs.Count}");
    }

    /// <summary>
    /// 17B: التحقق الإلزامي من عدم اختزال أي قائمة، واحتواء كل نادٍ على ما بين 20 إلى 25 لاعباً مع أرقام القمصان والقدرات المعتمدة
    /// </summary>
    private static void Test17B_ComprehensiveSquadDepthAcrossAllClubs()
    {
        var allClubs = TeamDatabase.GetAllClubs();

        foreach (var club in allClubs)
        {
            if (club.Players.Count != 11)
            {
                throw new Exception($"Club {club.Name} ({club.Id}) must have exactly 11 starters, but has {club.Players.Count}");
            }

            int totalPlayers = club.Players.Count + club.Bench.Count;
            if (totalPlayers < 20 || totalPlayers > 25)
            {
                throw new Exception($"Club {club.Name} ({club.Id}) violates sovereign squad depth mandate: required 20-25 players, found {totalPlayers} (Starters: {club.Players.Count}, Bench: {club.Bench.Count})");
            }

            // التأكد من وجود حارس مرمى أساسي وحارس بديل على الأقل
            bool hasStarterGk = club.Players.Any(p => p.Position == SpecificPosition.GK);
            if (!hasStarterGk) throw new Exception($"Club {club.Name} ({club.Id}) does not have a starting goalkeeper!");

            bool hasBenchGk = club.Bench.Any(p => p.Position == SpecificPosition.GK);
            if (!hasBenchGk) throw new Exception($"Club {club.Name} ({club.Id}) does not have a substitute goalkeeper on the bench!");

            // فحص صحة أرقام القمصان والقدرات التكتيكية لجميع اللاعبين
            var allSquad = club.Players.Concat(club.Bench).ToList();
            foreach (var player in allSquad)
            {
                if (string.IsNullOrWhiteSpace(player.Name)) throw new Exception($"Club {club.Name} has player with empty name!");
                if (player.KitNumber <= 0) throw new Exception($"Player {player.Name} in {club.Name} has invalid kit number: {player.KitNumber}");
                if (player.Pace <= 0) throw new Exception($"Player {player.Name} in {club.Name} has invalid Pace: {player.Pace}");
                if (player.Defending <= 0) throw new Exception($"Player {player.Name} in {club.Name} has invalid Defending: {player.Defending}");
                if (player.OverallRating <= 0) throw new Exception($"Player {player.Name} in {club.Name} has invalid OverallRating: {player.OverallRating}");
            }
        }
    }

    /// <summary>
    /// 17C: التحقق من بطولات الأندية القارية (دوري أبطال أوروبا، الدوري الأوروبي، دوري أبطال إفريقيا، كأس الكونفدرالية)
    /// </summary>
    private static void Test17C_ContinentalClubCompetitionsPools()
    {
        var ucl = TournamentDatabase.GetUEFAChampionsLeague();
        if (ucl.Count != 36) throw new Exception($"UEFA Champions League expected 36 clubs, got {ucl.Count}");

        var uel = TournamentDatabase.GetUEFAEuropaLeague();
        if (uel.Count != 32) throw new Exception($"UEFA Europa League expected 32 clubs, got {uel.Count}");

        var cafCl = TournamentDatabase.GetCAFChampionsLeague();
        if (cafCl.Count != 16) throw new Exception($"CAF Champions League expected 16 clubs, got {cafCl.Count}");

        var cafCc = TournamentDatabase.GetCAFConfederationCup();
        if (cafCc.Count != 16) throw new Exception($"CAF Confederation Cup expected 16 clubs, got {cafCc.Count}");

        foreach (var t in ucl.Concat(uel).Concat(cafCl).Concat(cafCc))
        {
            if (t.Players.Count + t.Bench.Count < 20)
            {
                throw new Exception($"Tournament club {t.Name} has less than 20 players ({t.Players.Count + t.Bench.Count})");
            }
        }
    }

    /// <summary>
    /// 17D: التحقق من المنتخبات الوطنية عبر القارات الست (إجمالي 84 منتخباً)
    /// </summary>
    private static void Test17D_NationalTeamsAcrossSixContinents()
    {
        var africa = NationalTeamDatabase.GetNationalTeamsByContinent(ContinentType.Africa);
        var europe = NationalTeamDatabase.GetNationalTeamsByContinent(ContinentType.Europe);
        var southAmerica = NationalTeamDatabase.GetNationalTeamsByContinent(ContinentType.SouthAmerica);
        var northAmerica = NationalTeamDatabase.GetNationalTeamsByContinent(ContinentType.NorthAmerica);
        var asia = NationalTeamDatabase.GetNationalTeamsByContinent(ContinentType.Asia);
        var oceania = NationalTeamDatabase.GetNationalTeamsByContinent(ContinentType.Oceania);

        if (africa.Count != 20) throw new Exception($"Africa expected 20 national teams, got {africa.Count}");
        if (europe.Count != 24) throw new Exception($"Europe expected 24 national teams, got {europe.Count}");
        if (southAmerica.Count != 10) throw new Exception($"South America expected 10 national teams, got {southAmerica.Count}");
        if (northAmerica.Count != 10) throw new Exception($"North America expected 10 national teams, got {northAmerica.Count}");
        if (asia.Count != 14) throw new Exception($"Asia expected 14 national teams, got {asia.Count}");
        if (oceania.Count != 6) throw new Exception($"Oceania expected 6 national teams, got {oceania.Count}");

        var allNational = NationalTeamDatabase.GetAllNationalTeams();
        if (allNational.Count != 84) throw new Exception($"Total national teams must be exactly 84, got {allNational.Count}");

        foreach (var nation in allNational)
        {
            if (!nation.IsNationalTeam) throw new Exception($"Team {nation.Name} must have IsNationalTeam=true");
            if (nation.Players.Count + nation.Bench.Count < 20)
            {
                throw new Exception($"National team {nation.Name} has less than 20 players ({nation.Players.Count + nation.Bench.Count})");
            }
        }
    }

    /// <summary>
    /// 17E: التحقق من سلامة البطولات الدولية الكبرى (كأس العالم، اليورو، أمم إفريقيا، كأس آسيا، كوبا أمريكا)
    /// </summary>
    private static void Test17E_MajorInternationalTournamentsIntegrity()
    {
        var wc = TournamentDatabase.GetWorldCup2026();
        if (wc.Count != 48) throw new Exception($"FIFA World Cup 2026 expected 48 national teams, got {wc.Count}");

        var euro = TournamentDatabase.GetEuro();
        if (euro.Count != 24) throw new Exception($"UEFA Euro expected 24 national teams, got {euro.Count}");

        var afcon = TournamentDatabase.GetAfcon();
        if (afcon.Count != 20) throw new Exception($"AFCON expected 20 national teams, got {afcon.Count}");

        var asianCup = TournamentDatabase.GetAsianCup();
        if (asianCup.Count != 14) throw new Exception($"Asian Cup expected 14 national teams, got {asianCup.Count}");

        var copa = TournamentDatabase.GetCopaAmerica();
        if (copa.Count != 16) throw new Exception($"Copa America expected 16 national teams, got {copa.Count}");
    }

    /// <summary>
    /// 17F: التحقق الرقابي الحاسم: خلو قاعدة البيانات والبطولات تماماً 100% من أي أثر للكيان الصهيوني
    /// </summary>
    private static void Test17F_StrictExclusionOfIsraelAndIsraeliClubs()
    {
        if (!TeamDatabase.VerifyZeroExcludedTeams())
        {
            throw new Exception("CRITICAL INTEGRITY FAILURE: TeamDatabase contains excluded teams (Israel/Israeli clubs)!");
        }

        if (!TournamentDatabase.VerifyZeroExcludedTeams())
        {
            throw new Exception("CRITICAL INTEGRITY FAILURE: TournamentDatabase contains excluded teams (Israel/Israeli clubs)!");
        }

        var allAvailable = TeamDatabase.GetAllAvailableTeams();
        foreach (var team in allAvailable)
        {
            string name = team.Name ?? "";
            string id = team.Id ?? "";

            if (name.Contains("إسرائيل") || name.Contains("اسرائيل") ||
                name.IndexOf("Israel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Israeli", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Maccabi", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Hapoel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("ISR", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                throw new Exception($"SOVEREIGN DIRECTIVE VIOLATION: Excluded team detected: {name} (ID: {id})");
            }
        }
    }

    /// <summary>
    /// 17G: التحقق من التوافقية الكاملة 100% لكافة دوال المصنع السابقة الـ 20
    /// </summary>
    private static void Test17G_MasterFacadeAndLegacyFactoryMethodsCompatibility()
    {
        var legacyClubs = new List<Team>
        {
            TeamDatabase.CreateRealMadrid(),
            TeamDatabase.CreateBarcelona(),
            TeamDatabase.CreateLiverpool(),
            TeamDatabase.CreateManchesterCity(),
            TeamDatabase.CreateArsenal(),
            TeamDatabase.CreateBayernMunich(),
            TeamDatabase.CreateParisSaintGermain(),
            TeamDatabase.CreateAlAhly(),
            TeamDatabase.CreateZamalek(),
            TeamDatabase.CreatePyramids()
        };

        var legacyNations = new List<Team>
        {
            TeamDatabase.CreateEgypt(),
            TeamDatabase.CreateMorocco(),
            TeamDatabase.CreateSaudiArabia(),
            TeamDatabase.CreateAlgeria(),
            TeamDatabase.CreateTunisia(),
            TeamDatabase.CreateFrance(),
            TeamDatabase.CreateArgentina(),
            TeamDatabase.CreateBrazil(),
            TeamDatabase.CreateEngland(),
            TeamDatabase.CreateSpain()
        };

        if (legacyClubs.Count != 10) throw new Exception("Legacy clubs count mismatch");
        if (legacyNations.Count != 10) throw new Exception("Legacy nations count mismatch");

        foreach (var t in legacyClubs.Concat(legacyNations))
        {
            if (t == null) throw new Exception("Legacy factory returned null team");
            if (t.Players.Count != 11) throw new Exception($"Legacy team {t.Name} does not have 11 starters ({t.Players.Count})");
            if (t.Players.Count + t.Bench.Count < 20) throw new Exception($"Legacy team {t.Name} does not meet 20 players depth ({t.Players.Count + t.Bench.Count})");
        }
    }
}
