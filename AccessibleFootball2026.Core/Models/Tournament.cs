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
