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
    public int? age { get; set; }
    public string? position { get; set; }
    public int power { get; set; }
    public bool power_estimated { get; set; }
    public string? power_provenance { get; set; }
    public string[]? source_urls { get; set; }
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

    /// <summary>
    /// مسار ملف قاعدة البيانات الذي تم تحميله فعلاً (للتشخيص والاختبارات).
    /// </summary>
    public static string? LoadedDatabasePath { get; private set; }

    private static void LoadDatabase()
    {
        // نفحص كل المسارات المرشحة ونختار قاعدة البيانات الأكمل (الأكثر فرقاً)،
        // لا أول ملف يُقرأ بنجاح: أي ملف players_database.json قديم في مجلد التشغيل
        // أو على القرص D:\ كان يحجب قاعدة البيانات المدمجة الجديدة (529 فريقاً) للمستخدم،
        // فتظهر اللعبة دائماً بالقاعدة القديمة. عند التعادل في عدد الفرق يفوز المسار الأسبق.
        var probed = new List<(string Path, int TeamCount, bool IsCombined)>();
        foreach (var jsonPath in GetCandidatePaths())
        {
            int teamCount = ProbeTeamCount(jsonPath);
            if (teamCount > 0)
            {
                bool isCombined = Path.GetFileName(jsonPath).StartsWith("players_database_combined", StringComparison.OrdinalIgnoreCase);
                probed.Add((jsonPath, teamCount, isCombined));
            }
        }

        string? selected = SelectBestDatabaseCandidate(probed);
        if (selected == null)
        {
            return;
        }

        try
        {
            LoadDatabaseFromFile(selected);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[JsonDatabaseProvider] Error loading database from {selected}: {ex.Message}");
        }

        if (_allTeams.Count > 0)
        {
            LoadedDatabasePath = selected;
            return;
        }

        ClearLoadedTeams();
    }

    /// <summary>
    /// عدد الفرق في ملف قاعدة بيانات مرشح دون تحميله؛ -1 لملف غير موجود أو تالف أو بلا فرق.
    /// </summary>
    public static int ProbeTeamCount(string jsonPath)
    {
        try
        {
            if (!File.Exists(jsonPath)) return -1;
            string json = File.ReadAllText(jsonPath);
            var root = JsonSerializer.Deserialize<JsonPlayerDbRoot>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (root?.teams == null || root.teams.Count == 0) return -1;
            return root.teams.Count;
        }
        catch
        {
            return -1;
        }
    }

    /// <summary>
    /// اختيار الملف الفائز من المرشحين المفحوصين:
    /// 1) أولوية صريحة لقاعدة البيانات المدمجة المقصودة (players_database_combined-*.json)
    ///    على أي ملف قياسي players_database.json حتى لو كان القياسي أكبر،
    ///    لأن الملف المدمج هو الذي بناه المستخدم وسلّمه في 8 نوفمبر 2026.
    /// 2) داخل كل فئة يفوز الأكثر فرقاً، وعند التعادل يفوز الأسبق في القائمة.
    /// 3) بلا أي مرشح مدمج صالح: يفوز أكبر ملف قياسي صالح (سلوك احتياطي حتمي).
    /// </summary>
    public static string? SelectBestDatabaseCandidate(IReadOnlyList<(string Path, int TeamCount, bool IsCombined)> probedCandidates)
    {
        string? best = null;
        int bestCount = 0;
        bool bestIsCombined = false;
        foreach (var (path, count, isCombined) in probedCandidates)
        {
            bool better = count > 0
                && (best == null
                    || (isCombined && !bestIsCombined)
                    || (isCombined == bestIsCombined && count > bestCount));
            if (better)
            {
                best = path;
                bestCount = count;
                bestIsCombined = isCombined;
            }
        }
        return best;
    }

    private static void ClearLoadedTeams()
    {
        _allTeams.Clear();
        _teamsByName.Clear();
        _teamsById.Clear();
        _teamsByGroup.Clear();
        _groupArabicNames.Clear();
    }

    // Sourced additions are separate from the untouched original user database.
    // 55 is a disclosed game-balance baseline, never a measured ability claim.
    private static void ApplySourcedRosterAdditions(JsonPlayerDbRoot root)
    {
        if(root.teams == null) return;
        var additions = new (string Team, JsonPlayerEntry Player)[]
        {
            ("FC Aiglons (Tchad)",new JsonPlayerEntry { name="Biwé Mangana", age=null, position="Goalkeeper", power=55, power_estimated=true, power_provenance="Estimated gameplay balance baseline55. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://www.tecormedias.com/biwe-mangana-lascension-du-dernier-rempart-du-football-tchadien/","https://lesportif.td/les-aiglons-fc-nouveaux-maitres-du-football-tchadien-retour-sur-une-saison-2025-2026-historique/"}}),
            ("FC Aiglons (Tchad)",new JsonPlayerEntry { name="Abderamane Ahmat", age=33, position="Centre-Back", power=55, power_estimated=true, power_provenance="Estimated gameplay balance baseline55. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://www.transfermarkt.fr/fc-aiglons-tchad-/startseite/verein/113820"}}),
            ("Panthère Sportive du Ndé",new JsonPlayerEntry { name="Njoya Mohamed Malik", age=null, position="Goalkeeper", power=55, power_estimated=true, power_provenance="Estimated gameplay balance baseline55. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://dailysports.net/fr/news/elite-one-panthere-fc-annonce-sa-7e-recrue-estivale/"}}),
            ("Panthère Sportive du Ndé",new JsonPlayerEntry { name="Mouen Adolphe Phillipe", age=null, position="Midfielder", power=55, power_estimated=true, power_provenance="Estimated gameplay balance baseline55. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://dailysports.net/fr/news/elite-one-panthere-fc-annonce-sa-7e-recrue-estivale/"}}),
            ("AS ZAM",new JsonPlayerEntry { name="Harouna Doulla Younoussa", age=null, position="Goalkeeper", power=55, power_estimated=true, power_provenance="Estimated gameplay balance baseline55. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://africatopsports.com/football/coupe-caf-asko-as-zam-les-compositions-officielles/","https://togofoot.tg/prel-coupe-caf-sans-forcer-asko-passe-au-second-tour/"}}),
            ("DC Virunga",new JsonPlayerEntry { name="Djodjo Mushila", age=null, position="Goalkeeper", power=55, power_estimated=true, power_provenance="Estimated gameplay balance baseline55. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://africatopsports.com/caf/coupe-caf-les-compositions-officielles-de-dc-virunga-vs-c-d-primeiro-de-agosto/","https://congoprofond.net/dc-virunga-verrouille-son-heros-djodjo-mushila-reste-pour-trois-ans/"}}),
            // AS ZAM (Niger): remaining starters from the official XI vs ASKO de Kara, CAF Confederation Cup first preliminary round second leg, 13 Sep 2026.
            ("AS ZAM",new JsonPlayerEntry { name="Soumaila Moussa Mouslim", age=null, position=null, power=53, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 53 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://africatopsports.com/football/coupe-caf-asko-as-zam-les-compositions-officielles/","https://togofoot.tg/prel-coupe-caf-sans-forcer-asko-passe-au-second-tour/"}}),
            ("AS ZAM",new JsonPlayerEntry { name="Maman Bachir Moussa Oumarou", age=null, position=null, power=50, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 50 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://africatopsports.com/football/coupe-caf-asko-as-zam-les-compositions-officielles/","https://togofoot.tg/prel-coupe-caf-sans-forcer-asko-passe-au-second-tour/"}}),
            ("AS ZAM",new JsonPlayerEntry { name="Garba Hassane Ibrahim", age=null, position=null, power=58, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 58 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://africatopsports.com/football/coupe-caf-asko-as-zam-les-compositions-officielles/","https://togofoot.tg/prel-coupe-caf-sans-forcer-asko-passe-au-second-tour/"}}),
            ("AS ZAM",new JsonPlayerEntry { name="Abdoul Karim Moumouni", age=null, position=null, power=57, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 57 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://africatopsports.com/football/coupe-caf-asko-as-zam-les-compositions-officielles/","https://togofoot.tg/prel-coupe-caf-sans-forcer-asko-passe-au-second-tour/"}}),
            ("AS ZAM",new JsonPlayerEntry { name="Abdoulaye Garba Ali", age=null, position=null, power=59, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 59 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://africatopsports.com/football/coupe-caf-asko-as-zam-les-compositions-officielles/","https://togofoot.tg/prel-coupe-caf-sans-forcer-asko-passe-au-second-tour/"}}),
            ("AS ZAM",new JsonPlayerEntry { name="Mahamadou Nouridine Issoufou Tinni", age=null, position=null, power=54, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 54 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://africatopsports.com/football/coupe-caf-asko-as-zam-les-compositions-officielles/","https://togofoot.tg/prel-coupe-caf-sans-forcer-asko-passe-au-second-tour/"}}),
            ("AS ZAM",new JsonPlayerEntry { name="Ali Saibou Moumouni", age=null, position=null, power=54, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 54 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://africatopsports.com/football/coupe-caf-asko-as-zam-les-compositions-officielles/","https://togofoot.tg/prel-coupe-caf-sans-forcer-asko-passe-au-second-tour/"}}),
            ("AS ZAM",new JsonPlayerEntry { name="Mahamat Sanoussi", age=null, position=null, power=59, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 59 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://africatopsports.com/football/coupe-caf-asko-as-zam-les-compositions-officielles/","https://togofoot.tg/prel-coupe-caf-sans-forcer-asko-passe-au-second-tour/"}}),
            ("AS ZAM",new JsonPlayerEntry { name="Seydou Bartche Hamani", age=null, position=null, power=58, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 58 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://africatopsports.com/football/coupe-caf-asko-as-zam-les-compositions-officielles/","https://togofoot.tg/prel-coupe-caf-sans-forcer-asko-passe-au-second-tour/"}}),
            ("AS ZAM",new JsonPlayerEntry { name="Souman Abdoulaye Abdoul Wahabou", age=null, position=null, power=57, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 57 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://africatopsports.com/football/coupe-caf-asko-as-zam-les-compositions-officielles/","https://togofoot.tg/prel-coupe-caf-sans-forcer-asko-passe-au-second-tour/"}}),
            // DC Virunga: official XI vs C.D. Primeiro de Agosto, CAF Confederation Cup second preliminary round first leg, 18 Oct 2026. Article documents Mushila in goal, a four-man defence and the attacking duo Bafumba Isono / Mbuyulu Bozana; other positions not assigned by the source. Claude Gisaurvel Ngongara from the same sheet is the already-listed Claude Ngongara and is not duplicated.
            ("DC Virunga",new JsonPlayerEntry { name="Artivie Mananga Umba", age=null, position=null, power=58, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 58 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://africatopsports.com/caf/coupe-caf-les-compositions-officielles-de-dc-virunga-vs-c-d-primeiro-de-agosto/"}}),
            ("DC Virunga",new JsonPlayerEntry { name="Jacques Nkunku Kumbundu", age=null, position=null, power=58, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 58 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://africatopsports.com/caf/coupe-caf-les-compositions-officielles-de-dc-virunga-vs-c-d-primeiro-de-agosto/"}}),
            ("DC Virunga",new JsonPlayerEntry { name="Grace Gaga Yenga", age=null, position=null, power=55, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 55 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://africatopsports.com/caf/coupe-caf-les-compositions-officielles-de-dc-virunga-vs-c-d-primeiro-de-agosto/"}}),
            ("DC Virunga",new JsonPlayerEntry { name="Josue Nsimba Mbabu", age=null, position=null, power=57, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 57 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://africatopsports.com/caf/coupe-caf-les-compositions-officielles-de-dc-virunga-vs-c-d-primeiro-de-agosto/"}}),
            ("DC Virunga",new JsonPlayerEntry { name="Moise Bedi Mokonzo", age=null, position=null, power=54, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 54 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://africatopsports.com/caf/coupe-caf-les-compositions-officielles-de-dc-virunga-vs-c-d-primeiro-de-agosto/"}}),
            ("DC Virunga",new JsonPlayerEntry { name="Michee Bafumba Isono", age=null, position="Striker", power=58, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 58 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://africatopsports.com/caf/coupe-caf-les-compositions-officielles-de-dc-virunga-vs-c-d-primeiro-de-agosto/"}}),
            ("DC Virunga",new JsonPlayerEntry { name="Gedeon Nseka Mambu", age=null, position=null, power=58, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 58 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://africatopsports.com/caf/coupe-caf-les-compositions-officielles-de-dc-virunga-vs-c-d-primeiro-de-agosto/"}}),
            ("DC Virunga",new JsonPlayerEntry { name="Blanchard Kakese Ntumba", age=null, position=null, power=56, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 56 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://africatopsports.com/caf/coupe-caf-les-compositions-officielles-de-dc-virunga-vs-c-d-primeiro-de-agosto/"}}),
            ("DC Virunga",new JsonPlayerEntry { name="Elie Mbuyulu Bozana", age=null, position="Striker", power=58, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 58 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://africatopsports.com/caf/coupe-caf-les-compositions-officielles-de-dc-virunga-vs-c-d-primeiro-de-agosto/"}}),
            // AS Port-Louis 2000: current squad as listed by Sofascore (team page current through the Oct 2026 CAF Confederation Cup second preliminary round). Marvine Donovan Samane is the already-listed Marvin Samane and is not duplicated.
            ("AS Port-Louis 2000",new JsonPlayerEntry { name="Frederic Shane Arékion", age=null, position="Goalkeeper", power=55, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 55 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://www.sofascore.com/football/team/as-port-louis-2000/246935"}}),
            ("AS Port-Louis 2000",new JsonPlayerEntry { name="Cyrille Ondoa", age=null, position="Goalkeeper", power=56, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 56 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://www.sofascore.com/football/team/as-port-louis-2000/246935"}}),
            ("AS Port-Louis 2000",new JsonPlayerEntry { name="Julien Pellegrin", age=null, position="Goalkeeper", power=59, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 59 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://www.sofascore.com/football/team/as-port-louis-2000/246935"}}),
            ("AS Port-Louis 2000",new JsonPlayerEntry { name="Wayne Parsooramen", age=null, position="Defender", power=54, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 54 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://www.sofascore.com/football/team/as-port-louis-2000/246935"}}),
            ("AS Port-Louis 2000",new JsonPlayerEntry { name="Jovanni Vincent", age=null, position="Defender", power=50, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 50 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://www.sofascore.com/football/team/as-port-louis-2000/246935"}}),
            ("AS Port-Louis 2000",new JsonPlayerEntry { name="Jeremie David", age=null, position="Defender", power=55, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 55 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://www.sofascore.com/football/team/as-port-louis-2000/246935"}}),
            ("AS Port-Louis 2000",new JsonPlayerEntry { name="Aurelien Kawoa", age=null, position="Midfielder", power=53, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 53 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://www.sofascore.com/football/team/as-port-louis-2000/246935"}}),
            ("AS Port-Louis 2000",new JsonPlayerEntry { name="Darel Arthée", age=null, position="Midfielder", power=51, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 51 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://www.sofascore.com/football/team/as-port-louis-2000/246935"}}),
            ("AS Port-Louis 2000",new JsonPlayerEntry { name="Stéphane Mape", age=null, position="Midfielder", power=50, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 50 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://www.sofascore.com/football/team/as-port-louis-2000/246935"}}),
            ("AS Port-Louis 2000",new JsonPlayerEntry { name="Hanshen Manaroo", age=null, position="Midfielder", power=57, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 57 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://www.sofascore.com/football/team/as-port-louis-2000/246935"}}),
            ("AS Port-Louis 2000",new JsonPlayerEntry { name="Donovan Hortense", age=null, position="Midfielder", power=52, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 52 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://www.sofascore.com/football/team/as-port-louis-2000/246935"}}),
            ("AS Port-Louis 2000",new JsonPlayerEntry { name="Azhar Beegun", age=null, position="Midfielder", power=55, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 55 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://www.sofascore.com/football/team/as-port-louis-2000/246935"}}),
            ("AS Port-Louis 2000",new JsonPlayerEntry { name="Leroy Juliette", age=null, position="Striker", power=57, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 57 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://www.sofascore.com/football/team/as-port-louis-2000/246935"}}),
            ("AS Port-Louis 2000",new JsonPlayerEntry { name="Peniel Yoboukoua", age=null, position="Striker", power=51, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 51 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://www.sofascore.com/football/team/as-port-louis-2000/246935"}}),
            ("AS Port-Louis 2000",new JsonPlayerEntry { name="Didier Moundoubou", age=null, position="Striker", power=59, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 59 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://www.sofascore.com/football/team/as-port-louis-2000/246935"}}),
            ("AS Port-Louis 2000",new JsonPlayerEntry { name="Jean Denis Desrougere", age=null, position="Striker", power=53, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 53 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://www.sofascore.com/football/team/as-port-louis-2000/246935"}}),
            ("AS Port-Louis 2000",new JsonPlayerEntry { name="Mohamed Mountala", age=null, position="Striker", power=51, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 51 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://www.sofascore.com/football/team/as-port-louis-2000/246935"}}),
            // União Bissau (UDIB): documented 2025/26 squad members. Keeper Hélder Martinho Có named in the 18 Apr 2026 league match report; GSA 2026 listing gives four more with ages; signings and cup-final scorer from fut245/ogologb reports. GSA's Bacari Sissé is the already-listed Bacari Cissé and is not duplicated.
            ("União Bissau",new JsonPlayerEntry { name="Hélder Martinho Có", age=null, position="Goalkeeper", power=50, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 50 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://fut245.com/2026/04/18/udib-bate-flamengo-de-pefine-e-regressa-aos-triunfos/"}}),
            ("União Bissau",new JsonPlayerEntry { name="Renickson Djassi", age=19, position="Defender", power=56, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 56 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://globalsportsarchive.com/en/soccer/team/udib/2943/squad"}}),
            ("União Bissau",new JsonPlayerEntry { name="Etivaldo Mendes", age=23, position="Defender", power=56, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 56 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://globalsportsarchive.com/en/soccer/team/udib/2943/squad"}}),
            ("União Bissau",new JsonPlayerEntry { name="Erikson", age=31, position="Midfielder", power=53, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 53 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://globalsportsarchive.com/en/soccer/athlete/erikson/231927/","https://globalsportsarchive.com/en/soccer/team/udib/2943/squad"}}),
            ("União Bissau",new JsonPlayerEntry { name="Lassana Aidará", age=20, position="Midfielder", power=57, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 57 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://globalsportsarchive.com/en/soccer/athlete/lassana-aidara/7950195/","https://globalsportsarchive.com/en/soccer/team/udib/2943/squad","http://ogologb.net/2026/03/30/udib-quebra-jejum-de-tres-jogos-e-aplica-terceira-derrota-seguida-ao-sporting-de-pedro-dias/"}}),
            ("União Bissau",new JsonPlayerEntry { name="Rachido Banora", age=null, position="Defender", power=59, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 59 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://fut245.com/2026/04/16/rachido-e-panobo-aguardam-ultimos-detalhes-para-reforcar-a-udib/","https://fut245.com/2026/06/25/sporting-cgb-e-udib-empatam-no-fecho-da-25-a-jornada-da-primeira-liga/"}}),
            ("União Bissau",new JsonPlayerEntry { name="Desiré Mendes", age=null, position="Striker", power=58, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 58 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://fut245.com/2026/08/16/udib-vence-fc-canchungo-e-conquista-taca-de-guine/"}}),
            ("União Bissau",new JsonPlayerEntry { name="Sada Djop", age=null, position="Attacking Midfield", power=57, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 57 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://fut245.com/2026/10/15/mercado-de-transferencias-sada-djop-e-idrissa-embalo-assinam-pela-udib/"}}),
            ("União Bissau",new JsonPlayerEntry { name="Idrissa Embaló", age=null, position="Midfielder", power=54, power_estimated=true, power_provenance="User-approved random gameplay balance estimate 54 in range50-59, assigned once on 2026-11-14 and retained in source. Not an externally sourced ability rating; individual attributes derived by existing role-based rules.", source_urls=new[] {"https://fut245.com/2026/10/15/mercado-de-transferencias-sada-djop-e-idrissa-embalo-assinam-pela-udib/"}}),

        };
        foreach(var (teamName, player) in additions)
        {
            var team=root.teams.FirstOrDefault(t=>string.Equals(t.team,teamName,StringComparison.OrdinalIgnoreCase)&&t.type=="club");
            if(team == null) continue;
            team.players ??= new List<JsonPlayerEntry>();
            // A future source update wins over this checkpoint, without duplicate insertion.
            string NormalizeName(string? n) => string.Concat((n ?? "").Normalize(System.Text.NormalizationForm.FormD).Where(c=>!char.IsWhiteSpace(c)&&char.GetUnicodeCategory(c)!=System.Globalization.UnicodeCategory.NonSpacingMark)).ToLowerInvariant();
            if(!team.players.Any(p=>NormalizeName(p.name)==NormalizeName(player.name))) team.players.Add(player);
        }
    }

    private static void LoadDatabaseFromFile(string jsonPath)
    {
            string json = File.ReadAllText(jsonPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var root = JsonSerializer.Deserialize<JsonPlayerDbRoot>(json, options);
            if (root?.teams == null || root.teams.Count == 0) return;
            ApplySourcedRosterAdditions(root);

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
            // Source rows have no global player id. Reuse only an exact normalized name/age identity;
            // never reuse a squad-row number across unrelated teams.
            var playerIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int nextPlayerId = 1;
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
                        int age = p.age is > 0 ? p.age.Value : 0;

                        int spd, pas, sho, def, phy;
                        CalculatePlayerAttributes(role, power, out spd, out pas, out sho, out def, out phy);

                        int shirtNum = (specPos == SpecificPosition.GK && shirtNumberCounter == 1) 
                            ? 1 
                            : (shirtNumberCounter == 1 ? 2 : shirtNumberCounter);

                        string identity = string.Join(" ", p.name.Trim().Normalize().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)) + "|" + age;
                        if (!playerIds.TryGetValue(identity, out int playerId))
                            playerIds[identity] = playerId = nextPlayerId++;
                        var player = new Player(
                            id: playerId,
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

                        player.RatingIsEstimated = p.power_estimated;
                        player.RatingProvenance = p.power_provenance ?? string.Empty;
                        player.DataSourceUrls = p.source_urls ?? Array.Empty<string>();
                        player.SourcePosition = p.position ?? string.Empty;
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
        if (string.IsNullOrWhiteSpace(posStr)) return (SpecificPosition.Unknown, PlayerRole.Unknown);

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

        return (SpecificPosition.Unknown, PlayerRole.Unknown);
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

    private static List<string> GetCandidatePaths() => new()
    {
        Path.Combine(AppContext.BaseDirectory,"players_database.json"),
        Path.Combine(AppContext.BaseDirectory,"Assets","Data","players_database.json")
    };

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
