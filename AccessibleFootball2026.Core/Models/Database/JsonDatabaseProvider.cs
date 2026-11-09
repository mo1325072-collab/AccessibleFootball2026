using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;

namespace AccessibleFootball2026.Core.Models;

public class JsonPlayerDbRoot
{
    public string? artifact { get; set; }
    public string? season { get; set; }
    public JsonIndex? index { get; set; }
    public List<JsonTeamEntry>? teams { get; set; }
}

public class JsonIndex
{
    public List<JsonGroupInfo>? groups { get; set; }
}

public class JsonGroupInfo
{
    public string? group { get; set; }
    public string? group_ar { get; set; }
    public int team_count { get; set; }
}

public class JsonTeamEntry
{
    public string? team { get; set; }
    public string? type { get; set; }
    public string? group { get; set; }
    public string? edition { get; set; }
    public List<JsonPlayerEntry>? players { get; set; }
}

public class JsonPlayerEntry
{
    public string? name { get; set; }
    public int age { get; set; }
    public string? position { get; set; }
    public int power { get; set; }
}

/// <summary>
/// مزود قاعدة البيانات الموحد المعتمد 100% على ملف JSON الخاص بالمستخدم (529 فريق و13 مسابقة)
/// يحمل البيانات تلقائياً ويحولها إلى نماذج Team و Player ويسند التشكيلات والقدرات الواقعية.
/// </summary>
public static class JsonDatabaseProvider
{
    private static readonly object _initLock = new();
    private static bool _isInitialized = false;

    private static readonly List<Team> _allTeams = new();
    private static readonly Dictionary<string, Team> _teamsByName = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Team> _teamsById = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, List<Team>> _teamsByGroup = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> _groupArabicNames = new(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<Team> AllTeams
    {
        get
        {
            EnsureInitialized();
            return _allTeams;
        }
    }

    public static void EnsureInitialized()
    {
        if (_isInitialized) return;
        lock (_initLock)
        {
            if (_isInitialized) return;
            LoadDatabase();
            _isInitialized = true;
        }
    }

    private static void LoadDatabase()
    {
        string? jsonPath = ResolveDatabasePath();
        if (jsonPath == null || !File.Exists(jsonPath))
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(jsonPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var root = JsonSerializer.Deserialize<JsonPlayerDbRoot>(json, options);
            if (root?.teams == null || root.teams.Count == 0) return;

            if (root.index?.groups != null)
            {
                foreach (var g in root.index.groups)
                {
                    if (!string.IsNullOrEmpty(g.group) && !string.IsNullOrEmpty(g.group_ar))
                    {
                        _groupArabicNames[g.group] = g.group_ar;
                    }
                }
            }

            int teamIndex = 1;
            foreach (var teamEntry in root.teams)
            {
                if (string.IsNullOrWhiteSpace(teamEntry.team)) continue;

                string teamName = teamEntry.team.Trim();
                string groupName = teamEntry.group?.Trim() ?? "Other";
                bool isNational = string.Equals(teamEntry.type, "national", StringComparison.OrdinalIgnoreCase) ||
                                  groupName.Contains("National", StringComparison.OrdinalIgnoreCase);

                string teamId = $"TEAM_{teamIndex:D4}_{SanitizeId(teamName)}";
                var team = new Team(teamId, teamName, TeamSide.Home)
                {
                    IsNationalTeam = isNational
                };

                // تحويل وتسكين اللاعبين
                var createdPlayers = new List<Player>();
                int shirtNumberCounter = 1;

                if (teamEntry.players != null)
                {
                    foreach (var p in teamEntry.players)
                    {
                        if (string.IsNullOrWhiteSpace(p.name)) continue;

                        var (specPos, role) = ParsePosition(p.position);
                        int power = Math.Clamp(p.power, 45, 99);
                        int age = p.age > 0 ? p.age : 25;

                        int spd, pas, sho, def, phy;
                        CalculatePlayerAttributes(role, power, out spd, out pas, out sho, out def, out phy);

                        int shirtNum = (specPos == SpecificPosition.GK && shirtNumberCounter == 1) 
                            ? 1 
                            : (shirtNumberCounter == 1 ? 2 : shirtNumberCounter);

                        var player = new Player(
                            id: createdPlayers.Count + 1,
                            name: p.name.Trim(),
                            shirtNumber: shirtNum,
                            pos: specPos,
                            spd: spd,
                            pas: pas,
                            sho: sho,
                            def: def,
                            phy: phy,
                            secondary: null,
                            club: teamName,
                            preferredFoot: PreferredFoot.Right,
                            age: age,
                            tacticalRole: PlayerTacticalRole.None
                        );

                        createdPlayers.Add(player);
                        shirtNumberCounter++;
                    }
                }

                // توزيع التشكيل الأساسي والاحتياطي
                AssignSquadHierarchy(team, createdPlayers);

                _allTeams.Add(team);
                _teamsByName[teamName] = team;
                _teamsById[teamId] = team;

                if (!_teamsByGroup.TryGetValue(groupName, out var groupList))
                {
                    groupList = new List<Team>();
                    _teamsByGroup[groupName] = groupList;
                }
                groupList.Add(team);

                teamIndex++;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[JsonDatabaseProvider] Error loading database: {ex.Message}");
        }
    }

    private static void AssignSquadHierarchy(Team team, List<Player> allPlayers)
    {
        if (allPlayers.Count == 0) return;

        // اختيار حارس أساسي (أعلى طاقة)
        var goalkeepers = allPlayers.Where(p => p.Role == PlayerRole.Goalkeeper).OrderByDescending(p => p.OverallRating).ToList();
        var defenders = allPlayers.Where(p => p.Role == PlayerRole.Defender).OrderByDescending(p => p.OverallRating).ToList();
        var midfielders = allPlayers.Where(p => p.Role == PlayerRole.Midfielder).OrderByDescending(p => p.OverallRating).ToList();
        var forwards = allPlayers.Where(p => p.Role == PlayerRole.Forward).OrderByDescending(p => p.OverallRating).ToList();

        var starters = new List<Player>();

        if (goalkeepers.Count > 0)
        {
            var gk = goalkeepers[0];
            gk.ShirtNumber = 1;
            starters.Add(gk);
        }

        // 4 مدافعين
        starters.AddRange(defenders.Take(4));

        // 3 خط وسط
        starters.AddRange(midfielders.Take(3));

        // 3 مهاجمين
        starters.AddRange(forwards.Take(3));

        // إذا كان الفريق لا يمتلك توزيعة كاملة، نكمل بأعلى اللاعبين المتبقين حتى 11
        if (starters.Count < 11)
        {
            var remainingOutfield = allPlayers.Except(starters).OrderByDescending(p => p.OverallRating);
            starters.AddRange(remainingOutfield.Take(11 - starters.Count));
        }

        var bench = allPlayers.Except(starters).ToList();

        // إعادة ترقيم القمصان لضمان عدم وجود أرقام مكررة
        var usedNumbers = new HashSet<int>();
        int curNum = 1;
        foreach (var p in starters.Concat(bench))
        {
            if (p.ShirtNumber > 0 && !usedNumbers.Contains(p.ShirtNumber))
            {
                usedNumbers.Add(p.ShirtNumber);
            }
            else
            {
                while (usedNumbers.Contains(curNum)) curNum++;
                p.ShirtNumber = curNum;
                usedNumbers.Add(curNum);
            }
        }

        team.Players.Clear();
        team.Players.AddRange(starters);
        team.Bench.Clear();
        team.Bench.AddRange(bench);

        // تعيين الكابتن لأعلى لاعب تقييماً
        var outfieldStarters = starters.Where(p => p.Role != PlayerRole.Goalkeeper).OrderByDescending(p => p.OverallRating).ToList();
        if (outfieldStarters.Count > 0)
        {
            team.Captain = outfieldStarters[0];
            team.Captain.IsCaptain = true;
            team.PenaltyTaker = outfieldStarters[0];
            team.FreeKickTaker = outfieldStarters[0];
            team.CornerTaker = outfieldStarters.Count > 1 ? outfieldStarters[1] : outfieldStarters[0];
        }

        team.SetupFormation433();
    }

    private static (SpecificPosition Pos, PlayerRole Role) ParsePosition(string? posStr)
    {
        if (string.IsNullOrWhiteSpace(posStr)) return (SpecificPosition.CM, PlayerRole.Midfielder);

        string p = posStr.Trim().ToLowerInvariant();
        if (p.Contains("goalkeeper") || p == "gk")
            return (SpecificPosition.GK, PlayerRole.Goalkeeper);

        if (p.Contains("centre-back") || p.Contains("center-back") || p == "cb")
            return (SpecificPosition.CB, PlayerRole.Defender);
        if (p.Contains("left-back") || p == "lb")
            return (SpecificPosition.LB, PlayerRole.Defender);
        if (p.Contains("right-back") || p == "rb")
            return (SpecificPosition.RB, PlayerRole.Defender);
        if (p.Contains("defender") || p == "df")
            return (SpecificPosition.CB, PlayerRole.Defender);

        if (p.Contains("defensive midfield") || p == "cdm")
            return (SpecificPosition.CDM, PlayerRole.Midfielder);
        if (p.Contains("attacking midfield") || p == "cam")
            return (SpecificPosition.CAM, PlayerRole.Midfielder);
        if (p.Contains("central midfield") || p.Contains("central midfielder") || p == "cm")
            return (SpecificPosition.CM, PlayerRole.Midfielder);
        if (p.Contains("left midfield") || p == "lm")
            return (SpecificPosition.LM, PlayerRole.Midfielder);
        if (p.Contains("right midfield") || p == "rm")
            return (SpecificPosition.RM, PlayerRole.Midfielder);
        if (p.Contains("midfielder") || p == "mf")
            return (SpecificPosition.CM, PlayerRole.Midfielder);

        if (p.Contains("left winger") || p == "lw")
            return (SpecificPosition.LW, PlayerRole.Forward);
        if (p.Contains("right winger") || p == "rw")
            return (SpecificPosition.RW, PlayerRole.Forward);
        if (p.Contains("centre-forward") || p.Contains("second striker") || p.Contains("striker") || p == "cf" || p == "st")
            return (SpecificPosition.CF, PlayerRole.Forward);
        if (p.Contains("forward") || p == "fw")
            return (SpecificPosition.CF, PlayerRole.Forward);

        return (SpecificPosition.CM, PlayerRole.Midfielder);
    }

    private static void CalculatePlayerAttributes(PlayerRole role, int power, out int spd, out int pas, out int sho, out int def, out int phy)
    {
        switch (role)
        {
            case PlayerRole.Goalkeeper:
                spd = 50;
                pas = Math.Max(50, power - 10);
                sho = 30;
                def = power;
                phy = power;
                break;
            case PlayerRole.Defender:
                spd = Math.Clamp(power - 2, 50, 95);
                pas = Math.Clamp(power - 5, 50, 95);
                sho = Math.Clamp(power - 20, 35, 90);
                def = power;
                phy = Math.Clamp(power + 2, 55, 98);
                break;
            case PlayerRole.Midfielder:
                spd = Math.Clamp(power, 50, 95);
                pas = Math.Clamp(power + 2, 55, 98);
                sho = Math.Clamp(power - 5, 50, 95);
                def = Math.Clamp(power - 5, 50, 95);
                phy = power;
                break;
            case PlayerRole.Forward:
            default:
                spd = Math.Clamp(power + 2, 55, 98);
                pas = Math.Clamp(power - 5, 50, 95);
                sho = Math.Clamp(power + 3, 55, 99);
                def = Math.Clamp(power - 25, 30, 80);
                phy = power;
                break;
        }
    }

    private static string SanitizeId(string name)
    {
        var chars = name.Where(char.IsLetterOrDigit).ToArray();
        string res = new string(chars).ToUpperInvariant();
        return res.Length > 12 ? res.Substring(0, 12) : res;
    }

    private static string? ResolveDatabasePath()
    {
        string p1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Data", "players_database.json");
        if (File.Exists(p1)) return p1;

        string p2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "players_database.json");
        if (File.Exists(p2)) return p2;

        string p3 = @"D:\AccessibleFootball2026\Assets\Data\players_database.json";
        if (File.Exists(p3)) return p3;

        string p4 = @"C:\Users\mohamed farag\Downloads\38833FF26BA1D.UnigramPreview_g9c9v27vpyspw!App\players_database_combined-fdfd1d53.json";
        if (File.Exists(p4)) return p4;

        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            string candidate = Path.Combine(dir, "AccessibleFootball2026.Core", "Data", "players_database.json");
            if (File.Exists(candidate)) return candidate;

            candidate = Path.Combine(dir, "Assets", "Data", "players_database.json");
            if (File.Exists(candidate)) return candidate;

            dir = Path.GetDirectoryName(dir);
        }

        return null;
    }

    // =========================================================================
    // دوال الاستعلام العامة (Public Query APIs)
    // =========================================================================

    public static List<Team> GetTeamsByGroup(string groupName)
    {
        EnsureInitialized();
        if (_teamsByGroup.TryGetValue(groupName, out var list))
        {
            return new List<Team>(list);
        }
        return new List<Team>();
    }

    public static List<Team> GetAllClubs()
    {
        EnsureInitialized();
        return _allTeams.Where(t => !t.IsNationalTeam).ToList();
    }

    public static List<Team> GetAllNationalTeams()
    {
        EnsureInitialized();
        return _allTeams.Where(t => t.IsNationalTeam).ToList();
    }

    public static Team? GetTeamByName(string name)
    {
        EnsureInitialized();
        if (_teamsByName.TryGetValue(name, out var team)) return team;
        return _allTeams.FirstOrDefault(t => t.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
    }

    public static Team? GetTeamById(string id)
    {
        EnsureInitialized();
        if (_teamsById.TryGetValue(id, out var team)) return team;
        return _allTeams.FirstOrDefault(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public static List<Team> GetClubsByLeague(LeagueType league)
    {
        EnsureInitialized();
        string groupKey = league switch
        {
            LeagueType.EgyptianPremierLeague => "Egyptian Premier League",
            LeagueType.SaudiProLeague => "Saudi Pro League",
            LeagueType.EnglishPremierLeague => "Premier League (England)",
            LeagueType.SpanishLaLiga => "LaLiga (Spain)",
            LeagueType.ItalianSerieA => "Serie A (Italy)",
            LeagueType.GermanBundesliga => "Bundesliga (Germany)",
            LeagueType.FrenchLigue1 => "Ligue 1 (France)",
            LeagueType.RestOfWorld => "CAF Champions League (clubs outside Egyptian league)",
            _ => "Egyptian Premier League"
        };

        var teams = GetTeamsByGroup(groupKey);
        if (teams.Count > 0) return teams;

        // في حالة عدم التطابق الحرفي، البحث بالاسم الجزئي
        return _allTeams.Where(t => !t.IsNationalTeam).Take(20).ToList();
    }

    public static List<string> GetAllGroups()
    {
        EnsureInitialized();
        return _teamsByGroup.Keys.ToList();
    }

    public static string GetGroupArabicName(string group)
    {
        EnsureInitialized();
        return _groupArabicNames.TryGetValue(group, out var ar) ? ar : group;
    }
}
