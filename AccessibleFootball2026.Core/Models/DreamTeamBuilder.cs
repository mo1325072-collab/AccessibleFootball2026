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

    public bool SelectSourceClubAt(bool opponent, int index, Team club)
    {
        var clubs = opponent ? CpuSourceClubs : UserSourceClubs;
        if (club.IsNationalTeam || index < 0 || index >= 4 || index > clubs.Count ||
            clubs.Take(index).Any(c => c.Id == club.Id)) return false;
        // Numbered menu steps are revisitable. A new choice replaces that step and
        // invalidates downstream choices rather than appending beyond four clubs.
        clubs.RemoveRange(index, clubs.Count - index);
        clubs.Add(club);
        if (!opponent) CpuSourceClubs.Clear();
        return true;
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
        targetTeam.Captain = targetTeam.PenaltyTaker = targetTeam.FreeKickTaker = targetTeam.CornerTaker = null;
        targetTeam.Score = targetTeam.SubstitutionsUsed = targetTeam.StoppageWindowsUsed = 0;
        targetTeam.IsExtraTime = targetTeam.WindowConsumedThisStoppage = false;
        targetTeam.MatchAttackingRight = null;
        targetTeam.Tactics = new TeamTactics();

        var allGks = new List<Player>();
        var allDefs = new List<Player>();
        var allMids = new List<Player>();
        var allFwds = new List<Player>();

        foreach (var club in sourceClubs)
        {
            foreach (var p in club.Players.Concat(club.Bench))
            {
                // نسخ مستقلة من اللاعبين: لا نعدّل كائنات الأندية المصدر (قد تكون مخزنة ومشتركة
                // من قاعدة JSON)، حتى لا تتسرب حالة الدرافت أو المباراة إلى الفرق الأصلية
                var clone = p.Clone();
                clone.ClubOrigin = club.Name;
                clone.Condition = (PlayerCondition)rng.Next(0, 5);

                switch (clone.Role)
                {
                    case PlayerRole.Goalkeeper: allGks.Add(clone); break;
                    case PlayerRole.Defender: allDefs.Add(clone); break;
                    case PlayerRole.Midfielder: allMids.Add(clone); break;
                    case PlayerRole.Forward: allFwds.Add(clone); break;
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
            // The same source player may occur in several club records or both lists.
            // Revalidate after each pick, not only in the initial pool snapshot.
            if (!PickedPlayerIds.Add(p.Id)) continue;
            if (isStartingXI)
            {
                targetTeam.Players.Add(p);
            }
            else
            {
                targetTeam.Bench.Add(p);
            }
            added++;
        }
    }
}
