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
        new("Riazor","أبانكا ريازور","Estadio ABANCA-RIAZOR",false,new[] {"Deportivo A Coruña"},"https://www.laliga.com/en-GB/clubs/rc-deportivo"),
        new("LaCartuja","لا كارتوخا","Estadio La Cartuja",false,new[] {"Real Betis Balompié"},"https://www.laliga.com/en-GB/clubs/real-betis"),
        new("Anoeta","أنويتا (ريالي أرينا)","Anoeta (Reale Arena)",false,new[] {"Real Sociedad"},"https://www.laliga.com/en-GB/clubs/real-sociedad"),
        new("SanMames","سان ماميس","Estadio San Mamés",false,new[] {"Athletic Bilbao"},"https://www.laliga.com/en-GB/clubs/athletic-club"),
        new("RCDE","آر سي دي إي","RCDE Stadium",false,new[] {"RCD Espanyol Barcelona"},"https://www.laliga.com/en-GB/clubs/rcd-espanyol"),
        new("LaRosaleda","لا روساليدا","La Rosaleda",false,new[] {"Málaga CF"},"https://www.laliga.com/en-GB/clubs/malaga-cf"),
        new("ElSadar","إل سادار","Estadio El Sadar",false,new[] {"CA Osasuna"},"https://www.laliga.com/en-GB/clubs/c-a-osasuna"),
        new("SanchezPizjuan","رامون سانشيز بيزخوان","Ramón Sánchez-Pizjuán",false,new[] {"Sevilla FC"},"https://www.laliga.com/en-GB/clubs/sevilla-fc"),
        new("Coliseum","كوليسيوم","Coliseum",false,new[] {"Getafe CF"},"https://www.laliga.com/en-GB/clubs/getafe-cf"),
        new("CiutatValencia","سيوتات دي فالنسيا","Estadio Ciutat de València",false,new[] {"Levante UD"},"https://www.laliga.com/en-GB/clubs/levante-ud"),
        new("Vallecas","فاييكاس","Estadio de Vallecas",false,new[] {"Rayo Vallecano"},"https://www.laliga.com/en-GB/clubs/rayo-vallecano/next-matches"),
        new("JohanCruijff","يوهان كرويف أرينا","Johan Cruijff ArenA",false,new[] {"Ajax Amsterdam"},"https://english.ajax.nl/club/stadium"),
        new("Philips","فيليبس","Philips Stadion",false,new[] {"PSV Eindhoven"},"https://www.psv.nl/ticketinfo/stadionplattegrond"),
        new("CelticPark","سيلتيك بارك","Celtic Park",false,new[] {"Celtic FC"},"https://www.celticfc.com/visit-celtic-park/getting-to-celtic-park/celtic-park-maps/"),
        new("Dragao","دراجاو","Estádio do Dragão",false,new[] {"FC Porto"},"https://www.fcporto.pt/pt/clube/estadio-do-dragao"),
        new("Alvalade","جوزيه ألفالادي","Estádio José Alvalade",false,new[] {"Sporting CP"},"https://backoffice.sporting.pt/pt/clube/infraestruturas/estadio-jose-alvalade"),
        new("Saracoglu","تشوباني شوكرو ساراج أوغلو","Chobani Stadyumu Fenerbahçe Şükrü Saracoğlu Spor Kompleksi",false,new[] {"Fenerbahce"},"https://www.fenerbahce.org/kulup/tesislerimiz/ulker-stadyumu-fenerbahce-sukru-saracoglu-spor-kompleksi"),
        new("JanBreydel","يان بريدل","Jan Breydel",false,new[] {"Club Brugge KV"},"https://www.clubbrugge.be/en/club-brugge-arrival-at-jan-breydel"),
        new("Allwyn","ألوين أرينا","Allwyn Arena",false,new[] {"AEK Athens"},"https://www.aekfc.gr/gdp/agia-sofia-44091.htm?lang=en&path=-291101721"),
        new("GrolschVeste","دي جرولش فيسته","De Grolsch Veste",false,new[] {"FC Twente Enschede"},"https://fctwente.nl/de-grolsch-veste"),
        new("Groupama","جروباما","Groupama Stadium",false,new[] {"Olympique Lyon"},"https://www.gsph24.com/ligue-1-2026-2027-la-liste-des-18-stades-de-la-saison"),
        new("PierreMauroy","ديكاتلون أرينا بيير موروا","Decathlon Arena - Stade Pierre-Mauroy",false,new[] {"LOSC Lille"},"https://www.gsph24.com/ligue-1-2026-2027-la-liste-des-18-stades-de-la-saison"),
        new("AllianzRiviera","أليانز ريفييرا","Allianz Riviera",false,new[] {"OGC Nice"},"https://www.gsph24.com/ligue-1-2026-2027-la-liste-des-18-stades-de-la-saison"),
        new("Roazhon","روازون بارك","Roazhon Park",false,new[] {"Stade Rennais FC"},"https://www.gsph24.com/ligue-1-2026-2027-la-liste-des-18-stades-de-la-saison"),
        new("ToulouseStadium","ستاديوم تولوز","Stadium de Toulouse",false,new[] {"FC Toulouse"},"https://www.gsph24.com/ligue-1-2026-2027-la-liste-des-18-stades-de-la-saison"),
        new("Moustoir","لو موستوار","Stade du Moustoir",false,new[] {"FC Lorient"},"https://www.gsph24.com/ligue-1-2026-2027-la-liste-des-18-stades-de-la-saison"),
        new("FrancisLeBle","فرانسيس لو بليه","Stade Francis-Le Blé",false,new[] {"Stade Brestois 29"},"https://www.gsph24.com/ligue-1-2026-2027-la-liste-des-18-stades-de-la-saison"),
        new("JuventusAllianz","أليانز ستاديوم","Allianz Stadium",false,new[] {"Juventus FC"},"https://images.legaseriea.it/image/private/fl_attachment/prd/atagcfmsdksksrhe3amr.pdf"),
        new("DallAra","ريناتو دال آرا","Renato Dall’Ara",false,new[] {"Bologna FC 1909"},"https://images.legaseriea.it/image/private/fl_attachment/prd/atagcfmsdksksrhe3amr.pdf"),
        new("Balaidos","أبانكا بالايدوس","Estadio ABANCA Balaídos",false,new[] {"Celta de Vigo"},"https://www.laliga.com/en-GB/clubs/rc-celta"),
        new("Wembley","ويمبلي","Wembley",true,new[] {"England"},"https://www.wembleystadium.com/about/stadium-facts-and-features"),
        new("HampdenPark","هامبدن بارك (باركليز هامبدن)","Hampden Park (Barclays Hampden)",true,new[] {"Scotland"},"https://www.scottishfa.co.uk/en/barclays-hampden"),
        new("CardiffCity","كارديف سيتي","Cardiff City Stadium",true,new[] {"Wales"},"https://faw.cymru/news/venues-confirmed-for-nations-league-fixtures/"),
        new("Parken","باركن","Parken Stadium",true,new[] {"Denmark"},"https://faw.cymru/news/venues-confirmed-for-nations-league-fixtures/"),
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
public sealed record TeamChantPool(string TeamName,string LabelPrefix,int FirstIndex,int LastIndex,string SourceUrl,bool NationalTeams=false);
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
        new("Deportivo A Coruña","SP_R0111_",564,565,MapSource),
        new("Real Betis Balompié","SP_R0194_",665,666,MapSource),
        new("Real Sociedad","SP_R0196_",670,676,MapSource),
        new("Athletic Bilbao","SP_R0258_",714,721,MapSource),
        new("RCD Espanyol Barcelona","SP_R0259_",722,723,MapSource),
        new("Málaga CF","SP_R0260_",724,726,MapSource),
        new("CA Osasuna","SP_R0263_",732,733,MapSource),
        new("Sevilla FC","SP_R0265_",734,740,MapSource),
        new("Getafe CF","SP_R0362_",763,764,MapSource),
        new("Levante UD","SP_R0366_",765,771,MapSource),
        new("Rayo Vallecano","SP_R0370_",772,776,MapSource),
        new("Ajax Amsterdam","SP_R0116_",582,587,MapSource,false),
        new("PSV Eindhoven","SP_R0118_",588,591,MapSource,false),
        new("Celtic FC","SP_R0131_",627,630,MapSource,false),
        new("FC Porto","SP_R0192_",663,663,MapSource,false),
        new("Sporting CP","SP_R0193_",664,664,MapSource,false),
        new("Fenerbahce","SP_R0197_",677,678,MapSource,false),
        new("Club Brugge KV","SP_R0269_",749,749,MapSource,false),
        new("AEK Athens","SP_R0270_",750,751,MapSource,false),
        new("FC Twente Enschede","SP_R0250_",712,712,MapSource,false),
        new("Olympique Lyon","SP_R0181_",647,651,MapSource,false),
        new("LOSC Lille","SP_R0213_",679,683,MapSource,false),
        new("OGC Nice","SP_R0217_",686,687,MapSource,false),
        new("Stade Rennais FC","SP_R0218_",688,692,MapSource,false),
        new("FC Toulouse","SP_R0221_",696,700,MapSource,false),
        new("FC Lorient","SP_R0414_",777,779,MapSource,false),
        new("Stade Brestois 29","SP_R1329_",808,808,MapSource,false),
        new("Juventus FC","SP_R0120_",598,603,MapSource,false),
        new("Bologna FC 1909","SP_R0186_",652,654,MapSource,false),
        new("Celta de Vigo","SP_R0195_",667,669,MapSource,false),
        new("England","SP_R0005_",453,456,MapSource,true),
        new("Scotland","SP_R0003_",449,450,MapSource,true),
        new("Wales","SP_R0004_",451,452,MapSource,true),
        new("Denmark","SP_R0015_",481,484,MapSource,true),
    };
    public static TeamChantPool? Find(Team team) =>
        Pools.FirstOrDefault(p=>p.NationalTeams==team.IsNationalTeam && p.TeamName.Equals(team.Name,StringComparison.OrdinalIgnoreCase));
}
