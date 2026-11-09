namespace AccessibleFootball2026.Core.Models;

/// <summary>
/// واجهة إدارة المنتخبات الوطنية المعتمدة من الفيفا لموسم 2026/2027 عبر القارات الست
/// تضم 84 منتخباً وطنياً مع استبعاد تام ومطلق لمنتخب الكيان الصهيوني
/// </summary>
public static class NationalTeamDatabase
{
    public static List<Team> GetNationalTeamsByContinent(ContinentType continent)
    {
        return continent switch
        {
            ContinentType.Africa => AfricanNationalTeams.GetAllNationalTeams(),
            ContinentType.Europe => EuropeanNationalTeams.GetAllNationalTeams(),
            ContinentType.SouthAmerica => SouthAmericanNationalTeams.GetAllNationalTeams(),
            ContinentType.NorthAmerica => NorthAmericanNationalTeams.GetAllNationalTeams(),
            ContinentType.Asia => AsianNationalTeams.GetAllNationalTeams(),
            ContinentType.Oceania => OceanianNationalTeams.GetAllNationalTeams(),
            _ => new List<Team>()
        };
    }

    public static List<Team> GetAllNationalTeams()
    {
        var fromJson = JsonDatabaseProvider.GetAllNationalTeams();
        if (fromJson != null && fromJson.Count > 0) return fromJson;

        var all = new List<Team>();
        all.AddRange(AfricanNationalTeams.GetAllNationalTeams());
        all.AddRange(EuropeanNationalTeams.GetAllNationalTeams());
        all.AddRange(SouthAmericanNationalTeams.GetAllNationalTeams());
        all.AddRange(NorthAmericanNationalTeams.GetAllNationalTeams());
        all.AddRange(AsianNationalTeams.GetAllNationalTeams());
        all.AddRange(OceanianNationalTeams.GetAllNationalTeams());
        return all;
    }

    public static Team? GetTeamById(string id)
    {
        return GetAllNationalTeams().FirstOrDefault(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }
}
