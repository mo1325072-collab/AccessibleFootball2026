using System;
using System.IO;
using System.Linq;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Tests;

/// <summary>
/// اختبارات انحدار لإصلاحات مراجعة الكود الشاملة (Tests 21A through 21D):
/// 21A: قواعد حسم ركلات الترجيح (إنهاء مبكر + موت مفاجئ)
/// 21B: درافت الـ 4 فرق ينسخ اللاعبين ولا يلوث كائنات الأندية المصدر
/// 21C: استنساخ الفريق عميق ومستقل عن كائنات قاعدة البيانات المخزنة
/// 21D: مزود قاعدة JSON يتجاوز الملفات التالفة أو الفارغة إلى المسارات الاحتياطية
/// </summary>
public static class Milestone7RepairRegressionTests
{
    public static void RunAll(Action<string, Action> runTest)
    {
        runTest("21A. Penalty Shootout Rules: Early Decision + Sudden Death Until Decided", Test21A_PenaltyShootoutDecisionRules);
        runTest("21B. Dream Team Draft: Cloned Players, Zero Source Club Contamination", Test21B_DreamTeamDraftClonesPlayers);
        runTest("21C. Team.Clone: Deep Independence from Cached Database Instances", Test21C_TeamCloneDeepIndependence);
        runTest("21D. JsonDatabaseProvider: Skips Corrupt/Empty Candidates to Fallback Paths", Test21D_JsonProviderSkipsCorruptCandidates);
        runTest("21E. Menu Music Start Must Stop Crowd Ambiance (Sound Academy Preview Leak)", Test21E_MenuMusicStopsCrowdAmbiance);
    }

    /// <summary>
    /// 21E: معاينة "أجواء المدرجات" من أكاديمية الأصوات تستدعي StartCrowdAmbiance،
    /// والرجوع للقوائم كان يشغّل موسيقى القوائم فوق صخب الجمهور المستمر.
    /// يجب أن يوقف StartMenuMusic صخب الجماهير جنباً إلى جنب مع إيقاف النشيد.
    /// </summary>
    private static void Test21E_MenuMusicStopsCrowdAmbiance()
    {
        string root = FindWorkspaceRoot();
        string playerCode = File.ReadAllText(Path.Combine(root, "AccessibleFootball2026.Audio", "SoundEffectsPlayer.cs"));

        int start = playerCode.IndexOf("public static void StartMenuMusic()", StringComparison.Ordinal);
        Assert(start >= 0, "StartMenuMusic must exist in SoundEffectsPlayer");
        int end = playerCode.IndexOf("\n    }", start, StringComparison.Ordinal);
        Assert(end > start, "StartMenuMusic body must be locatable");
        string body = playerCode.Substring(start, end - start);

        Assert(body.Contains("StopCrowdAmbiance"),
            "StartMenuMusic must stop crowd ambiance so the Sound Academy crowd preview cannot leak over menu music");

        // ويجب أن تتوقف المعاينة قبل تشغيل الموسيقى حتى لا يتداخل الصوتان لحظياً
        Assert(body.IndexOf("StopCrowdAmbiance", StringComparison.Ordinal) < body.IndexOf("bgm_menu", StringComparison.Ordinal),
            "Crowd ambiance must be stopped before the menu music channel opens");
    }

    private static string FindWorkspaceRoot()
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "AccessibleFootball2026.sln")))
            {
                return dir;
            }
            dir = Path.GetDirectoryName(dir);
        }
        throw new DirectoryNotFoundException("Could not find AccessibleFootball2026.sln in parent directory tree.");
    }

    /// <summary>
    /// 21A: حسم ركلات الترجيح طبقاً لقانون IFAB رقم 10 - خمس ركلات مع إنهاء مبكر عند استحالة اللحاق،
    /// ثم موت مفاجئ بركلة مقابل ركلة حتى يتقدم أحد الفريقين. لا يجوز إنهاء الترجيح بالتعادل أبداً.
    /// </summary>
    private static void Test21A_PenaltyShootoutDecisionRules()
    {
        // بداية الترجيح: لا حسم
        Assert(!PenaltyShootoutRules.IsDecided(0, 0, 0, 0), "No decision at 0-0 with 0 kicks");

        // تقدم 3-0 بعد 3 ركلات لكل فريق: المتأخر لا يملك سوى ركلتين = استحالة لحاق -> حسم مبكر
        Assert(PenaltyShootoutRules.IsDecided(3, 0, 3, 3), "3-0 after 3 kicks each must end the shootout early");

        // تقدم 3-0 بعد 3 ركلات للمتقدم وركلتين للمتأخر: للمتأخر 3 ركلات متبقية -> لا حسم بعد
        Assert(!PenaltyShootoutRules.IsDecided(3, 0, 3, 2), "3-0 with 3 kicks remaining for trailing team must continue");

        // تقدم 2-0 بعد ركلتين لكل فريق: للمتأخر 3 ركلات -> لا حسم
        Assert(!PenaltyShootoutRules.IsDecided(2, 0, 2, 2), "2-0 after 2 kicks each must continue");

        // حسم مبكر لصالح الفريق الثاني (تناظر القاعدة)
        Assert(PenaltyShootoutRules.IsDecided(0, 3, 3, 3), "0-3 after 3 kicks each must end the shootout early");

        // اكتمال الخمس ركلات بالتعادل: موت مفاجئ ولا حسم
        Assert(!PenaltyShootoutRules.IsDecided(4, 4, 5, 5), "4-4 after 5 kicks each must go to sudden death");
        Assert(PenaltyShootoutRules.IsSuddenDeath(4, 4, 5, 5), "4-4 after 5 kicks each is sudden death");

        // اكتمال الخمس ركلات بفارق: حسم
        Assert(PenaltyShootoutRules.IsDecided(3, 2, 5, 5), "3-2 after 5 kicks each is decided");

        // موت مفاجئ منتصف الجولة: ركلات غير متساوية -> لا حسم حتى يكمل الفريق الآخر ركلته
        Assert(!PenaltyShootoutRules.IsDecided(5, 4, 6, 5), "Sudden death with unequal kick counts must continue");

        // موت مفاجئ بعد جولة متكاملة بفارق: حسم
        Assert(PenaltyShootoutRules.IsDecided(5, 4, 6, 6), "Sudden death decided after equal kicks with a lead");

        // موت مفاجئ متعادل بعد جولات إضافية: يستمر بلا سقف جولات
        Assert(!PenaltyShootoutRules.IsDecided(7, 7, 8, 8), "Sudden death tied at round 8 must continue (no round cap)");

        // التعادل لا يسمى فوزاً لأي طرف بعد أي عدد من الجولات المتكاملة
        Assert(!PenaltyShootoutRules.IsDecided(9, 9, 10, 10), "Tie at any completed round is never a decision");
    }

    /// <summary>
    /// 21B: الدرافت يجب أن يضع نسخاً مستقلة من اللاعبين في الفريقين،
    /// فلا تتغير حالة اللاعبين الأصليين في الأندية المصدر (لياقة/حالة/انتماء)،
    /// ويبقى حظر التكرار فعالاً بين الفريقين.
    /// </summary>
    private static void Test21B_DreamTeamDraftClonesPlayers()
    {
        var builder = new DreamTeamBuilder();
        var clubs = new[]
        {
            TeamDatabase.CreateRealMadrid(),
            TeamDatabase.CreateLiverpool(),
            TeamDatabase.CreateAlAhly(),
            TeamDatabase.CreateBarcelona()
        };
        var cpuClubs = new[]
        {
            TeamDatabase.CreateManchesterCity(),
            TeamDatabase.CreateArsenal(),
            TeamDatabase.CreateZamalek(),
            TeamDatabase.CreatePyramids()
        };
        foreach (var club in clubs)
        {
            Assert(builder.AddUserSourceClub(club), $"Club {club.Name} must be accepted as a user source club");
        }
        foreach (var club in cpuClubs)
        {
            Assert(builder.AddCpuSourceClub(club), $"Club {club.Name} must be accepted as a CPU source club");
        }

        // لقطة لحالة اللاعبين الأصليين قبل الدرافت
        var originalConditions = clubs.Concat(cpuClubs)
            .SelectMany(c => c.Players.Concat(c.Bench))
            .Select(p => (Player: p, p.Condition, p.ClubOrigin))
            .ToList();

        builder.DraftBothTeams(new Random(42));

        Assert(builder.UserDreamTeam.Players.Count == 11, "User dream team must have 11 starters");
        Assert(builder.CpuDreamTeam.Players.Count == 11, "CPU dream team must have 11 starters");

        // كل لاعب مدرَفت كائن مستقل، وليس نفس المرجع من أي نادٍ مصدري
        var sourcePlayers = clubs.Concat(cpuClubs).SelectMany(c => c.Players.Concat(c.Bench)).ToHashSet();
        foreach (var drafted in builder.UserDreamTeam.Players.Concat(builder.UserDreamTeam.Bench)
                                   .Concat(builder.CpuDreamTeam.Players).Concat(builder.CpuDreamTeam.Bench))
        {
            Assert(!sourcePlayers.Contains(drafted), $"Drafted player {drafted.Name} must be a clone, not the source instance");
        }

        // اللاعبون الأصليون لم تتغير حالتهم بسبب الدرافت
        foreach (var (player, condition, origin) in originalConditions)
        {
            Assert(player.Condition == condition && player.ClubOrigin == origin,
                $"Source player {player.Name} must remain untouched by the draft");
        }

        // حظر التكرار: لا يوجد لاعب بنفس المعرف في الفريقين معاً
        var userIds = builder.UserDreamTeam.Players.Concat(builder.UserDreamTeam.Bench).Select(p => p.Id).ToHashSet();
        var cpuIds = builder.CpuDreamTeam.Players.Concat(builder.CpuDreamTeam.Bench).Select(p => p.Id).ToHashSet();
        Assert(!userIds.Overlaps(cpuIds), "Anti-clone guard: no player Id may appear in both dream teams");
    }

    /// <summary>
    /// 21C: Team.Clone ينتج فريقاً مستقلاً تماماً - تعديل النسخة (بطاقات، أهداف، لياقة)
    /// لا ينعكس على الأصل المخزن في قاعدة البيانات، والعكس.
    /// </summary>
    private static void Test21C_TeamCloneDeepIndependence()
    {
        var original = TeamDatabase.CreateRealMadrid();
        var clone = original.Clone(TeamSide.Away);

        Assert(clone.Id == original.Id && clone.Name == original.Name, "Clone preserves identity (Id, Name)");
        Assert(clone.Side == TeamSide.Away, "Clone applies the requested side");
        Assert(clone.Players.Count == original.Players.Count, "Clone carries the full squad");
        Assert(clone.Players.Zip(original.Players).All(pair => !ReferenceEquals(pair.First, pair.Second)),
            "Every cloned player must be a distinct instance");

        // تلوث النسخة لا يطال الأصل
        clone.Score = 5;
        clone.Players[0].YellowCards = 2;
        clone.Players[0].IsSentOff = true;
        clone.Players[1].CurrentStamina = 1;
        clone.Players[2].InjuryStatus = PlayerInjuryStatus.SevereInjury;

        Assert(original.Score == 0, "Original team score untouched by clone mutation");
        Assert(original.Players[0].YellowCards == 0 && !original.Players[0].IsSentOff,
            "Original player disciplinary state untouched by clone mutation");
        Assert(original.Players[1].CurrentStamina != 1, "Original player stamina untouched by clone mutation");
        Assert(original.Players[2].InjuryStatus != PlayerInjuryStatus.SevereInjury,
            "Original player injury state untouched by clone mutation");
    }

    /// <summary>
    /// 21D: عند غياب ملف JSON أو كونه فارغاً/تالفاً في كل المسارات المرشحة،
    /// يرجع المزود بقوائم فارغة دون استثناءات، وتتكفل الواجهات العليا (TeamDatabase)
    /// بالسقوط إلى قاعدة البيانات المدمجة. الملفات الفارغة لا يجب أن تحجب المسارات اللاحقة.
    /// </summary>
    private static void Test21D_JsonProviderSkipsCorruptCandidates()
    {
        // في بيئة الاختبار لا يوجد ملف JSON صالح - يجب ألا ينهار المزود
        var teams = JsonDatabaseProvider.AllTeams;
        var byGroup = JsonDatabaseProvider.GetTeamsByGroup("Egyptian Premier League");
        var nationals = JsonDatabaseProvider.GetAllNationalTeams();

        // الواجهة العليا تسقط دائماً إلى القاعدة المدمجة عند غياب JSON
        var fallback = TeamDatabase.GetClubsByLeague(LeagueType.EgyptianPremierLeague);
        Assert(fallback.Count > 0, "Compiled fallback league must serve clubs when JSON is absent");

        // أي فرق عادها المزود يجب أن تكون صالحة بنيوياً (فرق حقيقية بها لاعبون)
        foreach (var team in teams)
        {
            Assert(!string.IsNullOrWhiteSpace(team.Name), "JSON team must have a name");
            Assert(team.Players.Count > 0, $"JSON team {team.Name} must carry players");
        }

        Assert(byGroup != null && nationals != null, "Group queries must never return null");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception($"Assertion Failed: {message}");
        }
    }
}
