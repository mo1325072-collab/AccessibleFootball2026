namespace AccessibleFootball2026.Core.Models;

/// <summary>
/// وضع درافت الأندية الأربعة (4-Clubs Random Selection Match):
/// يختار المستخدم 4 أندية لفريقه، و 4 أندية لفريق الكمبيوتر،
/// ثم يجري الكمبيوتر سحباً عشوائياً متوازناً لتشكيلة كل فريق،
/// مع تفعيل حظر التكرار الصوتي (Anti-Clone Guard) لضمان عدم وجود أي لاعب مكرر بين الفريقين.
/// </summary>
public class DreamTeamBuilder
{
    public List<Team> UserSourceClubs { get; } = new();
    public List<Team> CpuSourceClubs { get; } = new();

    public Team UserDreamTeam { get; }
    public Team CpuDreamTeam { get; }

    public HashSet<int> PickedPlayerIds { get; } = new();

    public DreamTeamBuilder(string userTeamName = "نجوم وضع الـ 4 فرق", string cpuTeamName = "نجوم أندية الكمبيوتر")
    {
        UserDreamTeam = new Team("DRAFT_USER", userTeamName, TeamSide.Home);
        CpuDreamTeam = new Team("DRAFT_CPU", cpuTeamName, TeamSide.Away);
    }

    public bool AddUserSourceClub(Team club)
    {
        if (club.IsNationalTeam) return false;
        if (UserSourceClubs.Count < 4 && !UserSourceClubs.Any(c => c.Id == club.Id))
        {
            UserSourceClubs.Add(club);
            return true;
        }
        return false;
    }

    public bool AddCpuSourceClub(Team club)
    {
        if (club.IsNationalTeam) return false;
        if (CpuSourceClubs.Count < 4 && !CpuSourceClubs.Any(c => c.Id == club.Id))
        {
            CpuSourceClubs.Add(club);
            return true;
        }
        return false;
    }

    /// <summary>
    /// إجراء السحب العشوائي الكامل للتشكيلتين الأساسيتين والاحتياط
    /// </summary>
    public void DraftBothTeams(Random? rng = null)
    {
        rng ??= new Random();
        PickedPlayerIds.Clear();

        DraftSingleTeam(UserDreamTeam, UserSourceClubs, rng);
        DraftSingleTeam(CpuDreamTeam, CpuSourceClubs, rng);
    }

    private void DraftSingleTeam(Team targetTeam, List<Team> sourceClubs, Random rng)
    {
        targetTeam.Players.Clear();
        targetTeam.Bench.Clear();

        var allGks = new List<Player>();
        var allDefs = new List<Player>();
        var allMids = new List<Player>();
        var allFwds = new List<Player>();

        foreach (var club in sourceClubs)
        {
            foreach (var p in club.Players.Concat(club.Bench))
            {
                p.ClubOrigin = club.Name;
                p.Condition = (PlayerCondition)rng.Next(0, 5);

                switch (p.Role)
                {
                    case PlayerRole.Goalkeeper: allGks.Add(p); break;
                    case PlayerRole.Defender: allDefs.Add(p); break;
                    case PlayerRole.Midfielder: allMids.Add(p); break;
                    case PlayerRole.Forward: allFwds.Add(p); break;
                }
            }
        }

        // 1. التشكيل الأساسي (1 حارس، 4 دفاع، 3 وسط، 3 هجوم)
        DraftLineToTeam(targetTeam, allGks, 1, rng, isStartingXI: true);
        DraftLineToTeam(targetTeam, allDefs, 4, rng, isStartingXI: true);
        DraftLineToTeam(targetTeam, allMids, 3, rng, isStartingXI: true);
        DraftLineToTeam(targetTeam, allFwds, 3, rng, isStartingXI: true);

        // 2. دكة البدلاء (1 حارس، 2 دفاع، 2 وسط، 2 هجوم)
        DraftLineToTeam(targetTeam, allGks, 1, rng, isStartingXI: false);
        DraftLineToTeam(targetTeam, allDefs, 2, rng, isStartingXI: false);
        DraftLineToTeam(targetTeam, allMids, 2, rng, isStartingXI: false);
        DraftLineToTeam(targetTeam, allFwds, 2, rng, isStartingXI: false);

        targetTeam.SetupFormation433();
    }

    private void DraftLineToTeam(Team targetTeam, List<Player> pool, int count, Random rng, bool isStartingXI)
    {
        var available = pool.Where(p => !PickedPlayerIds.Contains(p.Id)).OrderBy(_ => rng.Next()).ToList();
        int added = 0;
        foreach (var p in available)
        {
            if (added >= count) break;
            if (isStartingXI)
            {
                targetTeam.Players.Add(p);
            }
            else
            {
                targetTeam.Bench.Add(p);
            }
            PickedPlayerIds.Add(p.Id);
            added++;
        }
    }
}
