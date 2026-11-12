namespace AccessibleFootball2026.Core.Models;

public enum CompetitionType
{
    DomesticLeague,
    KnockoutCup,
    International
}

public class Tournament
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string DisplayName => LocalizationService.CurrentLanguage == UiLanguage.Arabic ? Name : Id switch
    {
        "EPL_EG" => "Egyptian Premier League", "SPL_SA" => "Saudi Pro League",
        "EPL_UK" => "Premier League", "LALIGA" => "La Liga", "SERIEA" => "Serie A",
        "BUNDES" => "Bundesliga", "LIGUE1" => "Ligue 1", "CAF_CL" => "CAF Champions League",
        "CAF_CC" => "CAF Confederation Cup", "UEFA_CL" or "UCL" => "UEFA Champions League",
        "UEFA_EL" => "UEFA Europa League", "UEFA_ECL" => "UEFA Conference League",
        "NAT_ALL" => "National teams", "WC2026" => "World Cup 2026", _ => Name
    };
    public CompetitionType Type { get; set; }
    public string Region { get; set; }
    public List<Team> Teams { get; set; } = new();

    public Tournament(string id, string name, CompetitionType type, string region)
    {
        Id = id;
        Name = name;
        Type = type;
        Region = region;
    }
}
