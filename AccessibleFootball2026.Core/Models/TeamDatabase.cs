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
/// Only chant-backed grounds are offered. Never create stadiums to fill a roster count.</summary>
public sealed record StadiumDefinition(string Key, string ArabicName, string EnglishName,
    bool NationalTeams, string[] TeamNames, string SourceUrl)
{
    public string DisplayName => LocalizationService.Text(ArabicName, EnglishName);
    public bool IsGroundFor(Team team) => team.IsNationalTeam == NationalTeams &&
        TeamNames.Contains(team.Name, StringComparer.OrdinalIgnoreCase);
}

public static class StadiumCatalog
{
    // Only grounds whose participating club has a corroborated, recovered chant pool.
    // Each offered club has a concrete historical dt01 range and supplied label/file pool.
    private static readonly StadiumDefinition[] Verified =
    {
        new("SantiagoBernabeu","سانتياجو برنابيو","Santiago Bernabeu",false,new[] {"Real Madrid"},"https://bernabeu.realmadrid.com/"),
        new("CampNou","سبوتيفاي كامب نو","Spotify Camp Nou",false,new[] {"FC Barcelona"},"https://www.fcbarcelona.com/en/card/4510623/spotify-camp-nou-202627-pass"),
        new("OldTrafford","أولد ترافورد","Old Trafford",false,new[] {"Manchester United"},"https://www.premierleague.com/en/news/4725666/all-you-need-to-know-about-the-20-premier-league-stadiums/"),
        new("SanSiro","سان سيرو (جوزيبي مياتزا)","San Siro (Giuseppe Meazza)",false,new[] {"Inter Milan","AC Milan"},"https://sansirostadium.com/the-stadium/?lang=en"),
        new("Mestalla","ميستايا","Camp de Mestalla",false,new[] {"Valencia CF"},"https://www.laliga.com/en-GB/clubs/valencia-cf"),
        new("ParcPrinces","حديقة الأمراء","Parc des Princes",false,new[] {"Paris Saint-Germain"},"https://www.gsph24.com/ligue-1-2026-2027-la-liste-des-18-stades-de-la-saison"),
        new("Velodrome","سيباك فيلودروم","CEPAC Vélodrome",false,new[] {"Olympique Marseille"},"https://www.federation.caisse-epargne.fr/actualites/le-stade-velodrome-devient-le-cepac-velodrome-une-alliance-historique-qui-illustre-lengagement-territorial-des-caisses-depargne/"),
        new("BayernAllianz","أليانز أرينا","Allianz Arena",false,new[] {"FC Bayern Munich"},"https://www.bundesliga.com/en/bundesliga/clubs"),
        new("Metropolitano","رياض إير ميتروبوليتانو","Riyadh Air Metropolitano",false,new[] {"Atlético de Madrid"},"https://www.laliga.com/en-GB/clubs/atletico-de-madrid"),
        new("OlimpicoRome","الأولمبيكو في روما","Stadio Olimpico",false,new[] {"SS Lazio","AS Roma"},"https://images.legaseriea.it/image/private/fl_attachment/prd/atagcfmsdksksrhe3amr.pdf"),
        new("Tardini","إنيو تارديني","Ennio Tardini",false,new[] {"Parma Calcio 1913"},"https://images.legaseriea.it/image/private/fl_attachment/prd/atagcfmsdksksrhe3amr.pdf"),
        new("Franchi","أرتيميو فرانكي","Artemio Franchi",false,new[] {"ACF Fiorentina"},"https://images.legaseriea.it/image/private/fl_attachment/prd/atagcfmsdksksrhe3amr.pdf"),
        new("Maradona","دييجو أرماندو مارادونا","Diego Armando Maradona",false,new[] {"SSC Napoli"},"https://images.legaseriea.it/image/private/fl_attachment/prd/atagcfmsdksksrhe3amr.pdf"),
        new("Bluenergy","فريولي (بلوينرجي)","Friuli / Bluenergy Stadium",false,new[] {"Udinese Calcio"},"https://images.legaseriea.it/image/private/fl_attachment/prd/atagcfmsdksksrhe3amr.pdf"),
        new("Ferraris","لويجي فيراريس","Luigi Ferraris",false,new[] {"Genoa CFC"},"https://images.legaseriea.it/image/private/fl_attachment/prd/atagcfmsdksksrhe3amr.pdf"),
    };

    public static IReadOnlyList<StadiumDefinition> GetAvailableStadiums()
    {
        // Only the actual production JSON universe, never a fallback team invented to fill a venue.
        var teams = JsonDatabaseProvider.AllTeams;
        return Verified.Where(v => teams.Any(v.IsGroundFor)).ToList();
    }

    public static IReadOnlyList<StadiumDefinition> GetGroundsForProductionGroup(string group)
    {
        var teams = JsonDatabaseProvider.GetTeamsByGroup(group);
        return GetAvailableStadiums().Where(s => teams.Any(s.IsGroundFor)).ToList();
    }

    public static StadiumDefinition? Find(string key) =>
        GetAvailableStadiums().FirstOrDefault(s => s.Key == key);

}


/// <summary>Historical original-slot attribution, checked against supplied labels and recovered files.
/// This is metadata corroboration, not an independent listening certification.</summary>
public sealed record TeamChantPool(string TeamName,string LabelPrefix,int FirstIndex,int LastIndex,string SourceUrl);
public static class TeamChantCatalog
{
    private const string MapSource="https://www.pesgaming.com/forums/threads/chant-map.92044/";
    public static readonly IReadOnlyList<TeamChantPool> Pools = new TeamChantPool[]
    {
        new("Real Madrid","SP_R0109_",550,557,MapSource),
        new("FC Barcelona","SP_R0108_",541,549,MapSource),
        new("Manchester United","SP_R0100_",535,540,MapSource),
        new("Inter Milan","SP_R0119_",592,597,MapSource),
        new("AC Milan","SP_R0121_",604,610,MapSource),
        new("Valencia CF","SP_R0110_",558,563,MapSource),
        new("Paris Saint-Germain","SP_R0114_",572,575,MapSource),
        new("Olympique Marseille","SP_R0113_",566,571,MapSource),
        new("FC Bayern Munich","SP_R0127_",624,626,MapSource),
        new("Atlético de Madrid","SP_R0172_",642,646,MapSource),
        new("SS Lazio","SP_R0122_",611,612,MapSource),
        new("AS Roma","SP_R0125_",618,623,MapSource),
        new("Parma Calcio 1913","SP_R0123_",613,614,MapSource),
        new("ACF Fiorentina","SP_R0124_",615,617,MapSource),
        new("SSC Napoli","SP_R0327_",760,762,MapSource),
        new("Udinese Calcio","SP_R0190_",656,662,MapSource),
        new("Genoa CFC","SP_R0323_",755,759,MapSource),
    };
    public static TeamChantPool? Find(Team team) => team.IsNationalTeam ? null :
        Pools.FirstOrDefault(p=>p.TeamName.Equals(team.Name,StringComparison.OrdinalIgnoreCase));
}
