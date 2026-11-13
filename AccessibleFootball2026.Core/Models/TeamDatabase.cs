using System;
using System.Collections.Generic;
using System.Linq;

namespace AccessibleFootball2026.Core.Models;

/// <summary>
/// قاعدة البيانات الشاملة المعتمدة رسمياً لموسم 2026/2027 (Master Facade)
/// تعتمد 100% على ملف JSON الموحد للمستخدم (529 فريق) مع دعم التوافق الكامل مع الكود السابق.
/// </summary>
public static class TeamDatabase
{
    public static List<Team> GetAllAvailableTeams()
    {
        var fromJson = JsonDatabaseProvider.AllTeams;
        if (fromJson.Count > 0) return new List<Team>(fromJson);
        return GetAllClubs().Concat(GetAllNationalTeams()).ToList();
    }

    /// <summary>
    /// جلب الأندية التابعة لدوري محلي محدد
    /// </summary>
    public static List<Team> GetClubsByLeague(LeagueType league)
    {
        var fromJson = JsonDatabaseProvider.GetClubsByLeague(league);
        if (fromJson != null && fromJson.Count > 0) return fromJson;

        return league switch
        {
            LeagueType.EgyptianPremierLeague => EgyptianLeagueDatabase.GetAllClubs(),
            LeagueType.SaudiProLeague => SaudiLeagueDatabase.GetAllClubs(),
            LeagueType.EnglishPremierLeague => EnglishPremierLeagueDatabase.GetAllClubs(),
            LeagueType.SpanishLaLiga => SpanishLaLigaDatabase.GetAllClubs(),
            LeagueType.ItalianSerieA => ItalianSerieADatabase.GetAllClubs(),
            LeagueType.GermanBundesliga => GermanBundesligaDatabase.GetAllClubs(),
            LeagueType.FrenchLigue1 => FrenchLigue1Database.GetAllClubs(),
            LeagueType.RestOfWorld => RestOfWorldClubsDatabase.GetAllClubs(),
            _ => new List<Team>()
        };
    }

    /// <summary>
    /// جلب جميع الأندية من قاعدة البيانات
    /// </summary>
    public static List<Team> GetAllClubs()
    {
        var fromJson = JsonDatabaseProvider.GetAllClubs();
        if (fromJson != null && fromJson.Count > 0) return fromJson;

        var clubs = new List<Team>();
        clubs.AddRange(EgyptianLeagueDatabase.GetAllClubs());
        clubs.AddRange(SaudiLeagueDatabase.GetAllClubs());
        clubs.AddRange(EnglishPremierLeagueDatabase.GetAllClubs());
        clubs.AddRange(SpanishLaLigaDatabase.GetAllClubs());
        clubs.AddRange(ItalianSerieADatabase.GetAllClubs());
        clubs.AddRange(GermanBundesligaDatabase.GetAllClubs());
        clubs.AddRange(FrenchLigue1Database.GetAllClubs());
        clubs.AddRange(RestOfWorldClubsDatabase.GetAllClubs());
        return clubs;
    }

    /// <summary>
    /// جلب المنتخبات الوطنية حسب القارة
    /// </summary>
    public static List<Team> GetNationalTeamsByContinent(ContinentType continent)
    {
        return NationalTeamDatabase.GetNationalTeamsByContinent(continent);
    }

    /// <summary>
    /// جلب جميع المنتخبات الوطنية
    /// </summary>
    public static List<Team> GetAllNationalTeams()
    {
        var fromJson = JsonDatabaseProvider.GetAllNationalTeams();
        if (fromJson != null && fromJson.Count > 0) return fromJson;

        return NationalTeamDatabase.GetAllNationalTeams();
    }

    /// <summary>
    /// البحث عن أي فريق أو منتخب بواسطة المعرف
    /// </summary>
    public static Team? GetTeamById(string id)
    {
        var jsonTeam = JsonDatabaseProvider.GetTeamById(id);
        if (jsonTeam != null) return jsonTeam;

        return GetAllAvailableTeams().FirstOrDefault(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// التحقق الرقابي الصارم من خلو كافة الأندية والمنتخبات من الكيان الصهيوني
    /// </summary>
    public static bool VerifyZeroExcludedTeams()
    {
        var all = GetAllAvailableTeams();
        foreach (var t in all)
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

    // =========================================================================
    // دوال التوافقية للفرق السابقة (Legacy Factory Methods with JSON Database Priority)
    // =========================================================================

    public static Team CreateRealMadrid() => JsonDatabaseProvider.GetTeamByName("Real Madrid") ?? SpanishLaLigaDatabase.CreateRealMadrid();
    public static Team CreateBarcelona() => JsonDatabaseProvider.GetTeamByName("FC Barcelona") ?? SpanishLaLigaDatabase.CreateBarcelona();
    public static Team CreateLiverpool() => JsonDatabaseProvider.GetTeamByName("Liverpool FC") ?? EnglishPremierLeagueDatabase.CreateLiverpool();
    public static Team CreateManchesterCity() => JsonDatabaseProvider.GetTeamByName("Manchester City") ?? EnglishPremierLeagueDatabase.CreateManchesterCity();
    public static Team CreateArsenal() => JsonDatabaseProvider.GetTeamByName("Arsenal FC") ?? EnglishPremierLeagueDatabase.CreateArsenal();
    public static Team CreateBayernMunich() => JsonDatabaseProvider.GetTeamByName("Bayern Munich") ?? GermanBundesligaDatabase.CreateBayernMunich();
    public static Team CreateParisSaintGermain() => JsonDatabaseProvider.GetTeamByName("Paris Saint-Germain") ?? FrenchLigue1Database.CreateParisSaintGermain();
    public static Team CreateAlAhly() => JsonDatabaseProvider.GetTeamByName("Al Ahly FC") ?? EgyptianLeagueDatabase.CreateAlAhly();
    public static Team CreateZamalek() => JsonDatabaseProvider.GetTeamByName("Zamalek SC") ?? EgyptianLeagueDatabase.CreateZamalek();
    public static Team CreatePyramids() => JsonDatabaseProvider.GetTeamByName("Pyramids FC") ?? EgyptianLeagueDatabase.CreatePyramids();

    public static Team CreateEgypt() => JsonDatabaseProvider.GetTeamByName("Egypt") ?? AfricanNationalTeams.CreateEgypt();
    public static Team CreateMorocco() => JsonDatabaseProvider.GetTeamByName("Morocco") ?? AfricanNationalTeams.CreateMorocco();
    public static Team CreateSaudiArabia() => JsonDatabaseProvider.GetTeamByName("Saudi Arabia") ?? AsianNationalTeams.CreateSaudiArabia();
    public static Team CreateAlgeria() => JsonDatabaseProvider.GetTeamByName("Algeria") ?? AfricanNationalTeams.CreateAlgeria();
    public static Team CreateTunisia() => JsonDatabaseProvider.GetTeamByName("Tunisia") ?? AfricanNationalTeams.CreateTunisia();
    public static Team CreateFrance() => JsonDatabaseProvider.GetTeamByName("France") ?? EuropeanNationalTeams.CreateFrance();
    public static Team CreateArgentina() => JsonDatabaseProvider.GetTeamByName("Argentina") ?? SouthAmericanNationalTeams.CreateArgentina();
    public static Team CreateBrazil() => JsonDatabaseProvider.GetTeamByName("Brazil") ?? SouthAmericanNationalTeams.CreateBrazil();
    public static Team CreateEngland() => JsonDatabaseProvider.GetTeamByName("England") ?? EuropeanNationalTeams.CreateEngland();
    public static Team CreateSpain() => JsonDatabaseProvider.GetTeamByName("Spain") ?? EuropeanNationalTeams.CreateSpain();
}


/// <summary>A sourced ground may belong to several production teams, and a team may use several grounds.
/// Coverage is deliberately partial. Never create missing stadium facts from team names or roster counts.</summary>
public sealed record StadiumDefinition(string Key, string ArabicName, string EnglishName,
    bool NationalTeams, string[] TeamNames, string SourceUrl)
{
    public string DisplayName => LocalizationService.Text(ArabicName, EnglishName);
    public bool IsGroundFor(Team team) => team.IsNationalTeam == NationalTeams &&
        TeamNames.Contains(team.Name, StringComparer.OrdinalIgnoreCase);
}

public static class StadiumCatalog
{
    // Checked2026-11-13. These are supported grounds, not claims that nations use only one venue.
    private static readonly StadiumDefinition[] Verified =
    {
        new("SantiagoBernabeu","سانتياجو برنابيو","Santiago Bernabeu",false,new[] {"Real Madrid"},"https://bernabeu.realmadrid.com/"),
        new("CampNou","سبوتيفاي كامب نو","Spotify Camp Nou",false,new[] {"FC Barcelona"},"https://www.fcbarcelona.com/en/card/4510623/spotify-camp-nou-202627-pass"),
        new("SanSiro","سان سيرو (جوزيبي مياتزا)","San Siro (Giuseppe Meazza)",false,new[] {"Inter Milan","AC Milan"},"https://sansirostadium.com/the-stadium/?lang=en"),
        new("Wembley","ويمبلي","Wembley",true,new[] {"England"},"https://www.wembleystadium.com/about/stadium-facts-and-features"),
        new("HampdenPark","هامبدن بارك (باركليز هامبدن)","Hampden Park (Barclays Hampden)",true,new[] {"Scotland"},"https://www.scottishfa.co.uk/en/barclays-hampden"),
        new("CardiffCity","كارديف سيتي","Cardiff City Stadium",true,new[] {"Wales"},"https://faw.cymru/news/venues-confirmed-for-nations-league-fixtures/"),
        new("Parken","باركن","Parken Stadium",true,new[] {"Denmark"},"https://faw.cymru/news/venues-confirmed-for-nations-league-fixtures/"),
        new("Ullevaal","أوليفول","Ullevaal Stadium",true,new[] {"Norway"},"https://faw.cymru/news/venues-confirmed-for-nations-league-fixtures/"),
    };

    public static IReadOnlyList<StadiumDefinition> GetAvailableStadiums()
    {
        // Only the actual production JSON universe, never a fallback team invented to fill a venue.
        var teams = JsonDatabaseProvider.AllTeams;
        return Verified.Where(v => teams.Any(v.IsGroundFor)).ToList();
    }

    public static StadiumDefinition? Find(string key) =>
        GetAvailableStadiums().FirstOrDefault(s => s.Key == key);

    public static IReadOnlyList<Team> GetTeamsAwaitingGroundEvidence() =>
        JsonDatabaseProvider.AllTeams.Where(t => !Verified.Any(s => s.IsGroundFor(t))).ToList();
}
