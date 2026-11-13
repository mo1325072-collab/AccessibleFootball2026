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
        new("Mestalla","ميستايا","Camp de Mestalla",false,new[] {"Valencia CF"},"https://www.laliga.com/en-GB/clubs/valencia-cf"),
        new("Ceramica","لا سيراميكا","Estadio de la Cerámica",false,new[] {"Villarreal CF"},"https://www.laliga.com/en-GB/clubs/villarreal-cf"),
        new("LaRosaleda","لا روساليدا","La Rosaleda",false,new[] {"Málaga CF"},"https://www.laliga.com/en-GB/clubs/malaga-cf"),
        new("Mendizorroza","مينديزوروزا","Mendizorroza",false,new[] {"Deportivo Alavés"},"https://www.laliga.com/en-GB/clubs/d-alaves"),
        new("Metropolitano","رياض إير ميتروبوليتانو","Riyadh Air Metropolitano",false,new[] {"Atlético de Madrid"},"https://www.laliga.com/en-GB/clubs/atletico-de-madrid"),
        new("LaCartuja","لا كارتوخا","Estadio La Cartuja",false,new[] {"Real Betis Balompié"},"https://www.laliga.com/en-GB/clubs/real-betis"),
        new("MartinezValero","مارتينيز فاليرو","Estadio Martínez Valero",false,new[] {"Elche CF"},"https://www.laliga.com/en-GB/clubs/elche-c-f"),
        new("ElSadar","إل سادار","Estadio El Sadar",false,new[] {"CA Osasuna"},"https://www.laliga.com/en-GB/clubs/c-a-osasuna"),
        new("CiutatValencia","سيوتات دي فالنسيا","Estadio Ciutat de València",false,new[] {"Levante UD"},"https://www.laliga.com/en-GB/clubs/levante-ud"),
        new("Vallecas","فاييكاس","Estadio de Vallecas",false,new[] {"Rayo Vallecano"},"https://www.laliga.com/en-GB/clubs/rayo-vallecano/next-matches"),
        new("SanchezPizjuan","رامون سانشيز بيزخوان","Ramón Sánchez-Pizjuán",false,new[] {"Sevilla FC"},"https://www.laliga.com/en-GB/clubs/sevilla-fc"),
        new("Coliseum","كوليسيوم","Coliseum",false,new[] {"Getafe CF"},"https://www.laliga.com/en-GB/clubs/getafe-cf"),
        new("SanMames","سان ماميس","Estadio San Mamés",false,new[] {"Athletic Bilbao"},"https://www.laliga.com/en-GB/clubs/athletic-club"),
        new("ElSardinero","إل ساردينيرو","El Sardinero",false,new[] {"Racing Santander"},"https://www.laliga.com/en-GB/clubs/r-racing-club"),
        new("Anoeta","أنويتا (ريالي أرينا)","Anoeta (Reale Arena)",false,new[] {"Real Sociedad"},"https://www.laliga.com/en-GB/clubs/real-sociedad"),
        new("RCDE","آر سي دي إي","RCDE Stadium",false,new[] {"RCD Espanyol Barcelona"},"https://www.laliga.com/en-GB/clubs/rcd-espanyol"),
        new("Riazor","أبانكا ريازور","Estadio ABANCA-RIAZOR",false,new[] {"Deportivo A Coruña"},"https://www.laliga.com/en-GB/clubs/rc-deportivo"),
        new("Balaidos","أبانكا بالايدوس","Estadio ABANCA Balaídos",false,new[] {"Celta de Vigo"},"https://www.laliga.com/en-GB/clubs/rc-celta"),
        new("SanSiro","سان سيرو (جوزيبي مياتزا)","San Siro (Giuseppe Meazza)",false,new[] {"Inter Milan","AC Milan"},"https://sansirostadium.com/the-stadium/?lang=en"),
        new("Wembley","ويمبلي","Wembley",true,new[] {"England"},"https://www.wembleystadium.com/about/stadium-facts-and-features"),
        new("HampdenPark","هامبدن بارك (باركليز هامبدن)","Hampden Park (Barclays Hampden)",true,new[] {"Scotland"},"https://www.scottishfa.co.uk/en/barclays-hampden"),
        new("CardiffCity","كارديف سيتي","Cardiff City Stadium",true,new[] {"Wales"},"https://faw.cymru/news/venues-confirmed-for-nations-league-fixtures/"),
        new("Parken","باركن","Parken Stadium",true,new[] {"Denmark"},"https://faw.cymru/news/venues-confirmed-for-nations-league-fixtures/"),
        new("Ullevaal","أوليفول","Ullevaal Stadium",true,new[] {"Norway"},"https://faw.cymru/news/venues-confirmed-for-nations-league-fixtures/"),
        new("Emirates","الإمارات","Emirates Stadium",false,new[] {"Arsenal FC"},"https://www.premierleague.com/en/news/4725666/all-you-need-to-know-about-the-20-premier-league-stadiums/"),
        new("VillaPark","فيلا بارك","Villa Park",false,new[] {"Aston Villa"},"https://www.premierleague.com/en/news/4725666/all-you-need-to-know-about-the-20-premier-league-stadiums/"),
        new("Vitality","فيتاليتي (دين كورت)","Vitality Stadium (Dean Court)",false,new[] {"AFC Bournemouth"},"https://www.premierleague.com/en/news/4725666/all-you-need-to-know-about-the-20-premier-league-stadiums/"),
        new("Gtech","جي تك كوميونيتي","Gtech Community Stadium",false,new[] {"Brentford FC"},"https://www.premierleague.com/en/news/4725666/all-you-need-to-know-about-the-20-premier-league-stadiums/"),
        new("Amex","أمريكان إكسبريس (أميكس)","American Express Stadium (Amex)",false,new[] {"Brighton & Hove Albion"},"https://www.premierleague.com/en/news/4725666/all-you-need-to-know-about-the-20-premier-league-stadiums/"),
        new("StamfordBridge","ستامفورد بريدج","Stamford Bridge",false,new[] {"Chelsea FC"},"https://www.premierleague.com/en/news/4725666/all-you-need-to-know-about-the-20-premier-league-stadiums/"),
        new("CoventryArena","كوفنتري بيلدنج سوسايتي أرينا","Coventry Building Society Arena",false,new[] {"Coventry City"},"https://www.premierleague.com/en/news/4725666/all-you-need-to-know-about-the-20-premier-league-stadiums/"),
        new("SelhurstPark","سيلهرست بارك","Selhurst Park",false,new[] {"Crystal Palace"},"https://www.premierleague.com/en/news/4725666/all-you-need-to-know-about-the-20-premier-league-stadiums/"),
        new("HillDickinson","هيل ديكنسون","Hill Dickinson Stadium",false,new[] {"Everton FC"},"https://www.premierleague.com/en/news/4725666/all-you-need-to-know-about-the-20-premier-league-stadiums/"),
        new("CravenCottage","كرافن كوتاج","Craven Cottage",false,new[] {"Fulham FC"},"https://www.premierleague.com/en/news/4725666/all-you-need-to-know-about-the-20-premier-league-stadiums/"),
        new("MKM","إم كيه إم","MKM Stadium",false,new[] {"Hull City"},"https://www.premierleague.com/en/news/4725666/all-you-need-to-know-about-the-20-premier-league-stadiums/"),
        new("PortmanRoad","بورتمان رود","Portman Road",false,new[] {"Ipswich Town"},"https://www.premierleague.com/en/news/4725666/all-you-need-to-know-about-the-20-premier-league-stadiums/"),
        new("EllandRoad","إيلاند رود","Elland Road",false,new[] {"Leeds United"},"https://www.premierleague.com/en/news/4725666/all-you-need-to-know-about-the-20-premier-league-stadiums/"),
        new("Anfield","أنفيلد","Anfield",false,new[] {"Liverpool FC"},"https://www.premierleague.com/en/news/4725666/all-you-need-to-know-about-the-20-premier-league-stadiums/"),
        new("Etihad","الاتحاد","Etihad Stadium",false,new[] {"Manchester City"},"https://www.premierleague.com/en/news/4725666/all-you-need-to-know-about-the-20-premier-league-stadiums/"),
        new("OldTrafford","أولد ترافورد","Old Trafford",false,new[] {"Manchester United"},"https://www.premierleague.com/en/news/4725666/all-you-need-to-know-about-the-20-premier-league-stadiums/"),
        new("StJamesPark","سانت جيمس بارك","St James' Park",false,new[] {"Newcastle United"},"https://www.premierleague.com/en/news/4725666/all-you-need-to-know-about-the-20-premier-league-stadiums/"),
        new("CityGround","سيتي جراوند","City Ground",false,new[] {"Nottingham Forest"},"https://www.premierleague.com/en/news/4725666/all-you-need-to-know-about-the-20-premier-league-stadiums/"),
        new("StadiumOfLight","ستاديوم أوف لايت","Stadium of Light",false,new[] {"Sunderland AFC"},"https://www.premierleague.com/en/news/4725666/all-you-need-to-know-about-the-20-premier-league-stadiums/"),
        new("TottenhamStadium","توتنهام هوتسبير","Tottenham Hotspur Stadium",false,new[] {"Tottenham Hotspur"},"https://www.premierleague.com/en/news/4725666/all-you-need-to-know-about-the-20-premier-league-stadiums/"),
        new("NewBalance","نيو بالانس أرينا","New Balance Arena",false,new[] {"Atalanta BC"},"https://images.legaseriea.it/image/private/fl_attachment/prd/atagcfmsdksksrhe3amr.pdf"),
        new("DallAra","ريناتو دال آرا","Renato Dall'Ara",false,new[] {"Bologna FC 1909"},"https://images.legaseriea.it/image/private/fl_attachment/prd/atagcfmsdksksrhe3amr.pdf"),
        new("UnipolDomus","أونيبول دوموس","Unipol Domus",false,new[] {"Cagliari Calcio"},"https://images.legaseriea.it/image/private/fl_attachment/prd/atagcfmsdksksrhe3amr.pdf"),
        new("Sinigaglia","جوزيبي سينيجاليا","Giuseppe Sinigaglia",false,new[] {"Como 1907"},"https://images.legaseriea.it/image/private/fl_attachment/prd/atagcfmsdksksrhe3amr.pdf"),
        new("Franchi","أرتيميو فرانكي","Artemio Franchi",false,new[] {"ACF Fiorentina"},"https://images.legaseriea.it/image/private/fl_attachment/prd/atagcfmsdksksrhe3amr.pdf"),
        new("Stirpe","بينيتو ستيربي","Benito Stirpe",false,new[] {"Frosinone Calcio"},"https://images.legaseriea.it/image/private/fl_attachment/prd/atagcfmsdksksrhe3amr.pdf"),
        new("Ferraris","لويجي فيراريس","Luigi Ferraris",false,new[] {"Genoa CFC"},"https://images.legaseriea.it/image/private/fl_attachment/prd/atagcfmsdksksrhe3amr.pdf"),
        new("JuveAllianz","أليانز ستاديوم (يوفنتوس)","Allianz Stadium (Juventus)",false,new[] {"Juventus FC"},"https://images.legaseriea.it/image/private/fl_attachment/prd/atagcfmsdksksrhe3amr.pdf"),
        new("OlimpicoRome","الأولمبيكو (روما)","Stadio Olimpico (Rome)",false,new[] {"SS Lazio","AS Roma"},"https://images.legaseriea.it/image/private/fl_attachment/prd/atagcfmsdksksrhe3amr.pdf"),
        new("ViaDelMare","إيتوري جياردينييرو (فيا ديل ماري)","Ettore Giardiniero - Via del Mare",false,new[] {"US Lecce"},"https://images.legaseriea.it/image/private/fl_attachment/prd/atagcfmsdksksrhe3amr.pdf"),
        new("Brianteo","بريانتيـو","Brianteo",false,new[] {"AC Monza"},"https://images.legaseriea.it/image/private/fl_attachment/prd/atagcfmsdksksrhe3amr.pdf"),
        new("Maradona","دييجو أرماندو مارادونا","Diego Armando Maradona",false,new[] {"SSC Napoli"},"https://images.legaseriea.it/image/private/fl_attachment/prd/atagcfmsdksksrhe3amr.pdf"),
        new("Tardini","إنيو تارديني","Ennio Tardini",false,new[] {"Parma Calcio 1913"},"https://images.legaseriea.it/image/private/fl_attachment/prd/atagcfmsdksksrhe3amr.pdf"),
        new("Mapei","مابي ستاديوم","Mapei Stadium",false,new[] {"US Sassuolo"},"https://images.legaseriea.it/image/private/fl_attachment/prd/atagcfmsdksksrhe3amr.pdf"),
        new("GrandeTorino","أولمبيكو جراندي تورينو","Olimpico Grande Torino",false,new[] {"Torino FC"},"https://images.legaseriea.it/image/private/fl_attachment/prd/atagcfmsdksksrhe3amr.pdf"),
        new("Bluenergy","فريولي (بلوينرجي)","Friuli / Bluenergy Stadium",false,new[] {"Udinese Calcio"},"https://images.legaseriea.it/image/private/fl_attachment/prd/atagcfmsdksksrhe3amr.pdf"),
        new("Penzo","بيير لويجي بينزو","Pier Luigi Penzo",false,new[] {"Venezia FC"},"https://images.legaseriea.it/image/private/fl_attachment/prd/atagcfmsdksksrhe3amr.pdf"),
        new("BayernAllianz","أليانز أرينا","Allianz Arena",false,new[] {"FC Bayern Munich"},"https://www.bundesliga.com/en/bundesliga/clubs"),
        new("SignalIduna","سيجنال إيدونا بارك","Signal Iduna Park",false,new[] {"Borussia Dortmund"},"https://www.bundesliga.com/en/bundesliga/clubs"),
        new("RedBull","ريد بول أرينا","Red Bull Arena",false,new[] {"RB Leipzig"},"https://www.bundesliga.com/en/bundesliga/clubs"),
        new("MHPArena","إم إتش بي أرينا","MHPArena",false,new[] {"VfB Stuttgart"},"https://www.bundesliga.com/en/bundesliga/clubs"),
        new("SNP","إس إن بي أرينا","SNP Arena",false,new[] {"TSG 1899 Hoffenheim"},"https://www.bundesliga.com/en/bundesliga/clubs"),
        new("BayArena","باي أرينا","BayArena",false,new[] {"Bayer 04 Leverkusen"},"https://www.bundesliga.com/en/bundesliga/clubs"),
        new("EuropaPark","يوروبا بارك","Europa-Park Stadion",false,new[] {"SC Freiburg"},"https://www.bundesliga.com/en/bundesliga/clubs"),
        new("DeutscheBank","دويتشه بانك بارك","Deutsche Bank Park",false,new[] {"Eintracht Frankfurt"},"https://www.bundesliga.com/en/bundesliga/clubs"),
        new("WWK","دبليو دبليو كيه أرينا","WWK Arena",false,new[] {"FC Augsburg"},"https://www.bundesliga.com/en/bundesliga/clubs"),
        new("Mewa","ميوا أرينا","MEWA Arena",false,new[] {"1.FSV Mainz 05"},"https://www.bundesliga.com/en/bundesliga/clubs"),
        new("AlteForsterei","آن دير ألتن فورستراي","An der Alten Försterei",false,new[] {"1.FC Union Berlin"},"https://www.bundesliga.com/en/bundesliga/clubs"),
        new("BorussiaPark","إيستا بوروسيا بارك","ista-Borussia-Park",false,new[] {"Borussia Mönchengladbach"},"https://www.bundesliga.com/en/bundesliga/clubs"),
        new("Volkspark","فولكسبارك","Volksparkstadion",false,new[] {"Hamburger SV"},"https://www.bundesliga.com/en/bundesliga/clubs"),
        new("RheinEnergie","راين إنرجي","RheinEnergieSTADION",false,new[] {"1.FC Köln"},"https://www.bundesliga.com/en/bundesliga/clubs"),
        new("Weser","فيزرشتاديون","Weserstadion",false,new[] {"SV Werder Bremen"},"https://www.bundesliga.com/en/bundesliga/clubs"),
        new("Veltins","فيلتينس أرينا","VELTINS-Arena",false,new[] {"FC Schalke 04"},"https://www.bundesliga.com/en/bundesliga/clubs"),
        new("Ursapharm","أورسافارم أرينا","URSAPHARM-Arena",false,new[] {"SV 07 Elversberg"},"https://www.bundesliga.com/en/bundesliga/clubs"),
        new("HomeDeluxe","هوم ديلوكس أرينا","Home Deluxe Arena",false,new[] {"SC Paderborn 07"},"https://www.bundesliga.com/en/bundesliga/clubs"),
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

    public static IReadOnlyList<Team> GetTeamsAwaitingGroundEvidence() =>
        JsonDatabaseProvider.AllTeams.Where(t => !Verified.Any(s => s.IsGroundFor(t))).ToList();
}
