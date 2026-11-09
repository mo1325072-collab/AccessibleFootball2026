namespace AccessibleFootball2026.Core.Models;

/// <summary>
/// أدوات مساعدة لبناء الأندية والمنتخبات بقوائم كاملة وموسعة (20 إلى 25 لاعباً)
/// متوافقة 100% مع معايير Transfermarkt و SofaScore لموسم 2026/2027
/// </summary>
public static class DatabaseHelpers
{
    public static Team CreateTeam(
        string id,
        string name,
        FormationType formation,
        bool isNationalTeam,
        (int id, string name, int kit, SpecificPosition pos, int spd, int pas, int sho, int def, int phy, SpecificPosition[]? sec)[] starters,
        (int id, string name, int kit, SpecificPosition pos, int spd, int pas, int sho, int def, int phy, SpecificPosition[]? sec)[] bench)
    {
        var team = new Team(id, name, TeamSide.Home) { IsNationalTeam = isNationalTeam };

        foreach (var p in starters)
        {
            var secList = p.sec != null ? p.sec.ToList() : new List<SpecificPosition>();
            var player = new Player(p.id, p.name, p.kit, p.pos, p.spd, p.pas, p.sho, p.def, p.phy, secList, name);
            team.Players.Add(player);
        }

        foreach (var p in bench)
        {
            var secList = p.sec != null ? p.sec.ToList() : new List<SpecificPosition>();
            var player = new Player(p.id, p.name, p.kit, p.pos, p.spd, p.pas, p.sho, p.def, p.phy, secList, name);
            team.Bench.Add(player);
        }

        team.CurrentFormation = formation;
        team.AssignDefaultRoles();
        return team;
    }
}
