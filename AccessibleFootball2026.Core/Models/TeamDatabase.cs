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
