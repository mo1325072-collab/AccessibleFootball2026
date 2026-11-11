using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;
using AccessibleFootball2026.Input;

namespace AccessibleFootball2026.Tests;

/// <summary>
/// اختبارات انحدار الجولة الرابعة من الإصلاحات (مراجعة اللعب والتشغيل الحية):
/// 23A: ربط قاعدة البيانات - القاعدة الأكمل (الأكثر فرقاً) تفوز على أي ملف قديم
/// 23B: مواجهة الحارس - لفة واحدة لكل تسديدة مبنية على المهارات
/// 23C: تصويب الكمبيوتر نحو المقاص مع انتشار بدلاً من قلب المرمى دائماً
/// 23D: مصيدة التسلسل لا تُقيَّم ولا تُعلن إلا والمهاجم خلف خط الدفاع
/// 23E: تغيير مستوى صوت الموسيقى لا يبدأ الموسيقى أثناء المباراة
/// 23F: تجاوز شاشة البداية فوراً عند تعذر تشغيل النشيد
/// 23G: التقاط حارس المستخدم التلقائي يحترم ارتفاع الكرة ولا يلتهم التسديدات
/// </summary>
public static class Milestone9GameplayFairnessTests
{
    public static void RunAll(Action<string, Action> runTest)
    {
        runTest("23A. Database Binding: Most Complete Database Wins Over Older Smaller Files", Test23A_DatabaseBindingPrefersMostCompleteFile);
        runTest("23B. Goalkeeper Save Duel: Attribute-Based Single Roll With Bounds", Test23B_SaveProbabilityBoundsAndDeterminism);
        runTest("23C. CPU Shot Aim: Corners With Spread, Never Always Goal Center", Test23C_CpuShotTargetSpread);
        runTest("23D. Offside Trap: No Evaluation Or Announcement While Attacker Onside", Test23D_OffsideTrapRequiresAttackerBeyondLine);
        runTest("23E. Music Volume Change Never Starts Menu Music Mid-Match", Test23E_ApplyMusicVolumeNoStartContract);
        runTest("23F. Startup Intro Skips Immediately When Anthem Cannot Play", Test23F_AnthemFailureSkipsIntroContract);
        runTest("23G. User GK Auto-Catch Respects Ball Height And Excludes Shots", Test23G_UserGkAutoCatchContract);
        runTest("23H. Combined Database Schema: Exact Nov 8 Artifact Layout Parses", Test23H_CombinedDatabaseSchemaContract);
        runTest("23I. Combined Database: Unknown Player Age (null) Must Not Invalidate The File", Test23I_NullAgeDoesNotInvalidateDatabase);
    }

    /// <summary>
    /// 23A: السبب الجذري لشكوى "اللعبة دايماً بتجيب قاعدة البيانات القديمة":
    /// المحمّل كان يقبل أول players_database.json صالح (مجلد التشغيل / D:\)،
    /// فيحجب القاعدة المدمجة الجديدة (529 فريقاً) في مجلد التنزيلات.
    /// الاختيار الآن بالعدد الأكبر من الفرق، والتعادل للأسبق ترتيباً.
    /// </summary>
    private static void Test23A_DatabaseBindingPrefersMostCompleteFile()
    {
        var probed = new List<(string Path, int TeamCount, bool IsCombined)>
        {
            (@"C:\Game\players_database.json", 236, false),
            (@"C:\Users\mohamed farag\Downloads\players_database_combined-fdfd1d53.json", 529, true),
        };
        string? best = JsonDatabaseProvider.SelectBestDatabaseCandidate(probed);
        Assert(best == @"C:\Users\mohamed farag\Downloads\players_database_combined-fdfd1d53.json",
            $"The 529-team combined database must beat the older 236-team file. Got: {best}");

        // الترتيب المعكوس: القاعدة الأكبر تفوز أينما وقعت في القائمة
        var reversed = new List<(string Path, int TeamCount, bool IsCombined)>
        {
            (@"C:\Users\mohamed farag\Downloads\players_database_combined-fdfd1d53.json", 529, true),
            (@"C:\Game\players_database.json", 236, false),
        };
        Assert(JsonDatabaseProvider.SelectBestDatabaseCandidate(reversed) == @"C:\Users\mohamed farag\Downloads\players_database_combined-fdfd1d53.json",
            "Larger database must win regardless of candidate order");

        // أولوية صريحة للملف المدمج المقصود حتى أمام ملف قياسي أكبر
        var biggerStandard = new List<(string Path, int TeamCount, bool IsCombined)>
        {
            (@"C:\Game\players_database.json", 999, false),
            (@"C:\Users\mohamed farag\Downloads\players_database_combined-fdfd1d53.json", 529, true),
        };
        Assert(JsonDatabaseProvider.SelectBestDatabaseCandidate(biggerStandard) == @"C:\Users\mohamed farag\Downloads\players_database_combined-fdfd1d53.json",
            "The intended combined database must beat even a LARGER standard file");

        // بلا ملف مدمج: يفوز أكبر ملف قياسي (سلوك احتياطي حتمي)
        var standardsOnly = new List<(string Path, int TeamCount, bool IsCombined)>
        {
            (@"C:\Game\players_database.json", 236, false),
            (@"D:\AccessibleFootball2026\Assets\Data\players_database.json", 529, false),
        };
        Assert(JsonDatabaseProvider.SelectBestDatabaseCandidate(standardsOnly) == @"D:\AccessibleFootball2026\Assets\Data\players_database.json",
            "Without any combined file, the largest standard database must win");

        // التعادل في عدد الفرق: يفوز الأسبق في الترتيب (سلوك قديم محفوظ)
        var tie = new List<(string Path, int TeamCount, bool IsCombined)>
        {
            (@"D:\AccessibleFootball2026\Assets\Data\players_database.json", 300, false),
            (@"C:\Game\players_database.json", 300, false),
        };
        Assert(JsonDatabaseProvider.SelectBestDatabaseCandidate(tie) == @"D:\AccessibleFootball2026\Assets\Data\players_database.json",
            "Equal team counts must keep the earlier candidate");

        // لا مرشح صالح إطلاقاً: لا اختيار (يبقى السلوك الاحتياطي الحالي)
        Assert(JsonDatabaseProvider.SelectBestDatabaseCandidate(new List<(string, int, bool)>()) == null,
            "No valid candidate must yield no selection");
        Assert(JsonDatabaseProvider.SelectBestDatabaseCandidate(new List<(string, int, bool)> { (@"C:\x.json", 0, true) }) == null,
            "A zero-team probe must never be selected");
    }

    /// <summary>
    /// 23B: تصدي الحارس كان لفة 35% تتكرر بكل إطار داخل منطقة 3 أمتار،
    /// فتصل فعاليته لحوالي 82% مهما كانت مهارة المسدد أو الحارس.
    /// الآن مواجهة واحدة لكل تسديدة: حدود واضحة وتأثير حقيقي للمهارات.
    /// </summary>
    private static void Test23B_SaveProbabilityBoundsAndDeterminism()
    {
        var starStriker = MakePlayer(sho: 95, def: 40, pos: SpecificPosition.CF);
        var weakKeeper = MakePlayer(sho: 30, def: 60, pos: SpecificPosition.GK);
        float pStarVsWeak = MatchEngine.ComputeSaveProbability(starStriker, weakKeeper);
        Assert(pStarVsWeak <= 0.35f,
            $"Star striker (95) vs weak keeper (60) must concede saves rarely. Got: {pStarVsWeak}");

        var weakStriker = MakePlayer(sho: 55, def: 40, pos: SpecificPosition.CF);
        var starKeeper = MakePlayer(sho: 30, def: 95, pos: SpecificPosition.GK);
        float pWeakVsStar = MatchEngine.ComputeSaveProbability(weakStriker, starKeeper);
        Assert(pWeakVsStar >= 0.70f,
            $"Weak striker (55) vs star keeper (95) must be saved often. Got: {pWeakVsStar}");

        // التثبيت بين حدين: لا تصدٍ حتمي ولا استحالة
        var midStriker = MakePlayer(sho: 40, def: 40, pos: SpecificPosition.CF);
        var maxKeeper = MakePlayer(sho: 30, def: 99, pos: SpecificPosition.GK);
        Assert(Math.Abs(MatchEngine.ComputeSaveProbability(midStriker, maxKeeper) - 0.85f) < 0.001f,
            "Probability must clamp at 0.85 for the strongest keeper vs the weakest shot");
        var maxStriker = MakePlayer(sho: 99, def: 40, pos: SpecificPosition.CF);
        var minKeeper = MakePlayer(sho: 30, def: 45, pos: SpecificPosition.GK);
        Assert(Math.Abs(MatchEngine.ComputeSaveProbability(maxStriker, minKeeper) - 0.20f) < 0.001f,
            "Probability must clamp at 0.20 for the strongest shot vs the weakest keeper");

        // حتمية اللفة الواحدة: نفس الرقم يعطي نفس القرار، والحد الفاصل حقيقي
        Assert(MatchEngine.ResolveGoalkeeperSaveAttempt(weakStriker, starKeeper, forcedRoll: 0.01),
            "A roll far below the probability must be a save");
        Assert(!MatchEngine.ResolveGoalkeeperSaveAttempt(weakStriker, starKeeper, forcedRoll: 0.99),
            "A roll far above the probability must be a goal-bound shot");
    }

    /// <summary>
    /// 23C: تسديدات الكمبيوتر كانت تُصوَّب دائماً إلى مركز المرمى بالضبط حيث
    /// يلتقطها حارس المستخدم آلياً - فلا يسجل الكمبيوتر أبداً.
    /// الوجهة الآن مقص قريب أو بعيد مع انتشار، وبعض الكرات يخرج بجوار القائم.
    /// </summary>
    private static void Test23C_CpuShotTargetSpread()
    {
        var shooterPos = new Vector2(80.0f, Pitch.Width / 2.0f);
        float goalCenterY = (Pitch.GoalYMin + Pitch.GoalYMax) / 2.0f;

        var targets = new List<Vector2>();
        for (int i = 0; i < 40; i++)
        {
            targets.Add(MatchEngine.ComputeCpuShotTarget(
                shooterPos, attackingRight: false,
                postChoiceRoll: i / 40.0, spreadRoll: (i * 13 % 40) / 40.0));
        }

        Assert(targets.All(t => t.X == 0.0f),
            "CPU attacking the left goal must target the left goal line (X = 0)");
        Assert(targets.Any(t => t.Y < goalCenterY - 1.0f),
            "Some CPU shots must aim the upper post region");
        Assert(targets.Any(t => t.Y > goalCenterY + 1.0f),
            "Some CPU shots must aim the lower post region");
        Assert(targets.All(t => t.Y >= Pitch.GoalYMin - 1.0f && t.Y <= Pitch.GoalYMax + 1.0f),
            "CPU shot targets must stay within a plausible scoring window around the posts");
        Assert(targets.Select(t => t.Y).Distinct().Count() > 4,
            "CPU shot targets must vary, not collapse to a single aim point");

        // الهجوم نحو اليمين: خط المرمى الآخر
        var rightTargets = new List<Vector2>();
        for (int i = 0; i < 10; i++)
        {
            rightTargets.Add(MatchEngine.ComputeCpuShotTarget(shooterPos, attackingRight: true, i / 10.0, i / 10.0));
        }
        Assert(rightTargets.All(t => t.X == Pitch.Length),
            "CPU attacking the right goal must target the right goal line");
    }

    /// <summary>
    /// 23D: فحص كسر مصيدة التسلل كان يجري عشوائياً حتى والمهاجم في موقف سليم،
    /// فيعلن "كسر المصيدة! انفراد!" بلا سبب. الآن لا تقييم إلا خلف خط الدفاع.
    /// </summary>
    private static void Test23D_OffsideTrapRequiresAttackerBeyondLine()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();
        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);

        awayTeam.Tactics.OffsideTrap = true;

        int suspiciousAnnouncements = 0;
        eventBus.OnMatchEvent += evt =>
        {
            if (evt.Type == MatchEventType.Offside || evt.Description.Contains("مصيدة"))
            {
                suspiciousAnnouncements++;
            }
        };

        // خط دفاع الخصم متماسك عند X=70 في نصف ملعبه
        foreach (var defender in awayTeam.Players.Where(p => p.Role == PlayerRole.Defender))
        {
            defender.Position2D = new Vector2(70.0f, 34.0f);
        }

        var receiver = homeTeam.Players.First(p => p.Role == PlayerRole.Forward);

        // المهاجم داخل نصف ملعب الخصم لكن أمام خط الدفاع: موقف سليم تماماً
        Vector2 onsideTarget = match.UserAttackingRight ? new Vector2(60.0f, 34.0f) : new Vector2(45.0f, 34.0f);

        for (int i = 0; i < 30; i++)
        {
            bool result = match.CheckOffsideTrap(homeTeam, awayTeam, receiver, onsideTarget);
            Assert(!result, $"Trial {i}: an onside attacker must never be whistled by the trap");
        }

        Assert(suspiciousAnnouncements == 0,
            $"No offside or broken-trap announcement may fire for an onside attacker. Got: {suspiciousAnnouncements}");
    }

    /// <summary>
    /// 23E: ApplyMusicVolume كانت ترسل "play bgm_menu repeat" عند أي رفع للصوت،
    /// فتعديل الإعدادات من قائمة الإيقاف أثناء المباراة يشغّل موسيقى القوائم فوق اللعب.
    /// العقد: وجود حارس _menuMusicPlaying قبل أي أمر تشغيل.
    /// </summary>
    private static void Test23E_ApplyMusicVolumeNoStartContract()
    {
        string root = FindWorkspaceRoot();
        string playerCode = File.ReadAllText(Path.Combine(root, "AccessibleFootball2026.Audio", "SoundEffectsPlayer.cs"));

        int start = playerCode.IndexOf("public static void ApplyMusicVolume()", StringComparison.Ordinal);
        Assert(start >= 0, "ApplyMusicVolume must exist in SoundEffectsPlayer");
        int end = playerCode.IndexOf("\n    }", start, StringComparison.Ordinal);
        Assert(end > start, "ApplyMusicVolume body must be locatable");
        string body = playerCode.Substring(start, end - start);

        Assert(body.Contains("_menuMusicPlaying"),
            "ApplyMusicVolume must consult _menuMusicPlaying so a volume change never starts music");
        Assert(body.IndexOf("_menuMusicPlaying", StringComparison.Ordinal) < body.IndexOf("play bgm_menu repeat", StringComparison.Ordinal),
            "The _menuMusicPlaying guard must run before any play command");
    }

    /// <summary>
    /// 23F: عند فقدان ملف النشيد أو رفض جهاز الصوت، كانت اللعبة تبقى في شاشة
    /// بداية صامتة لا تستجيب إلا لـ Enter/Space - كارثة لمستخدم قارئ الشاشة.
    /// العقد: النشيد يعيد نجاحه، والنافذة تتجاوز الافتتاحية وتبدأ موسيقى القوائم عند الفشل.
    /// </summary>
    private static void Test23F_AnthemFailureSkipsIntroContract()
    {
        string root = FindWorkspaceRoot();
        string anthemCode = File.ReadAllText(Path.Combine(root, "AccessibleFootball2026.Audio", "Anthems", "AnthemEngine.cs"));
        Assert(anthemCode.Contains("public bool PlayChampionsLeagueSample()"),
            "PlayChampionsLeagueSample must report whether playback actually started");

        string windowCode = File.ReadAllText(Path.Combine(root, "AccessibleFootball2026.App", "AccessibleFootballWindow.cs"));
        int onShown = windowCode.IndexOf("protected override void OnShown", StringComparison.Ordinal);
        Assert(onShown >= 0, "OnShown must exist");
        int onShownEnd = windowCode.IndexOf("protected override void OnActivated", onShown, StringComparison.Ordinal);
        Assert(onShownEnd > onShown, "OnShown body must be locatable");
        string onShownBody = windowCode.Substring(onShown, onShownEnd - onShown);

        Assert(onShownBody.Contains("bool anthemStarted = _anthemEngine.PlayChampionsLeagueSample()"),
            "OnShown must capture the anthem playback result");
        Assert(onShownBody.Contains("_isInStartupIntro = false"),
            "OnShown must clear the startup intro when the anthem cannot play");
        Assert(onShownBody.Contains("StartMenuMusic"),
            "OnShown must start menu music when skipping the intro so the screen is never silent");
    }

    /// <summary>
    /// 23G: حارس مرمى المستخدم كان يلتقط آلياً وبنسبة 100% أي كرة تمر في نطاق 2.5 متر
    /// وبأي ارتفاع (حتى الكرات العالية العابرة فوقه)، ويلتهم تسديدات الكمبيوتر قبل
    /// أي مواجهة عادلة. العقد: فحص ارتفاع الكرة + استثناء التسديدات الجارية.
    /// </summary>
    private static void Test23G_UserGkAutoCatchContract()
    {
        string root = FindWorkspaceRoot();
        string engineCode = File.ReadAllText(Path.Combine(root, "AccessibleFootball2026.Core", "Simulation", "MatchEngine.cs"));

        int anchor = engineCode.IndexOf("يمسك بالكرة بأمان", StringComparison.Ordinal);
        Assert(anchor > 0, "User GK safe-catch announcement must exist");
        int blockStart = engineCode.LastIndexOf("else if", anchor, StringComparison.Ordinal);
        Assert(blockStart > 0 && anchor - blockStart < 1200, "User GK catch condition must be locatable");
        string condition = engineCode.Substring(blockStart, anchor - blockStart);

        Assert(condition.Contains("Pitch.GoalHeight"),
            "User GK auto-catch must respect ball height like the away keeper does");
        Assert(condition.Contains("!_isShotInProgress"),
            "User GK auto-catch must not swallow shots - they resolve via the single save duel");

        // التسديدات لا تستخدم لفة 35% المتكررة بكل إطار بعد الآن
        Assert(!engineCode.Contains("Random.Shared.NextDouble() < 0.35"),
            "The old per-frame 35% save re-roll must be gone");
        // تسديدة الكمبيوتر مسجلة كتسديدة جارية لها حارسها ومواجهتها
        Assert(engineCode.Contains("_shotShooter = opponent;"),
            "CPU shots must be tracked as shots in progress with the shooter recorded");
    }

    /// <summary>
    /// 23H: عقد المخطط الحرفي لقاعدة البيانات المدمجة المسلّمة في 8 نوفمبر 2026:
    /// مفاتيح الجذر artifact/season/fields/index/stats/teams، وكل فريق
    /// team/type/group/edition/competitions/players، وكل لاعب name/age/position/power.
    /// </summary>
    private static void Test23H_CombinedDatabaseSchemaContract()
    {
        string fixture = """
        {
          "artifact": "players_database_combined",
          "season": "2026/27",
          "fields": ["team", "type", "group", "edition", "competitions", "players"],
          "index": { "groups": [ { "group": "Egyptian Premier League", "group_ar": "الدوري المصري الممتاز", "teams": [ { "team": "الأهلي", "path": "teams[0]", "player_count": 2 } ] } ] },
          "stats": { "team_count": 3, "player_count": 4 },
          "teams": [
            {
              "team": "الأهلي", "type": "club", "group": "Egyptian Premier League", "edition": "2026-09",
              "competitions": ["Egyptian Premier League 2026/27"],
              "players": [
                { "name": "محمد الشناوي", "age": 37, "position": "Goalkeeper", "power": 85 },
                { "name": "محمد عبد المنعم", "age": 27, "position": "Centre-Back", "power": 82 }
              ]
            },
            {
              "team": "FC Canchungo", "type": "club", "group": "CAF Champions League (clubs outside Egyptian league)", "edition": "2026-09",
              "competitions": ["CAF Champions League 2026/27 (first preliminary round)"],
              "players": [
                { "name": "Test Keeper", "age": 25, "position": "GK", "power": 70 }
              ]
            },
            {
              "team": "Egypt", "type": "national", "group": "National teams - current squads", "edition": "2026-09",
              "competitions": [],
              "players": [
                { "name": "Mohamed Salah", "age": 34, "position": "Right Winger", "power": 92 }
              ]
            }
          ]
        }
        """;

        string path = Path.Combine(Path.GetTempPath(), "players_database_combined-m9fixture.json");
        File.WriteAllText(path, fixture);
        try
        {
            int teams = JsonDatabaseProvider.ProbeTeamCount(path);
            Assert(teams == 3, $"The exact Nov 8 combined schema must parse and count 3 teams. Got: {teams}");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static Player MakePlayer(int sho, int def, SpecificPosition pos)
    {
        return new Player(
            id: 1,
            name: "Test Player",
            shirtNumber: 9,
            pos: pos,
            spd: 80,
            pas: 80,
            sho: sho,
            def: def,
            phy: 80,
            secondary: null,
            club: "Test Club",
            preferredFoot: PreferredFoot.Right,
            age: 25,
            tacticalRole: PlayerTacticalRole.None
        );
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
    /// 23I: انحدار من الملف الفعلي: 483 لاعباً فيهم "age": null (العمر غير معروف)،
    /// وكان الحقل int غير قابل للقيمة الفارغة فيفشل تحليل الملف كله ويُحمّل صفر فرق.
    /// الآن العمر المجهول يُقرأ كقيمة فارغة ولا يبطل الملف، ولا يُعدَّل الملف الأصلي.
    /// </summary>
    private static void Test23I_NullAgeDoesNotInvalidateDatabase()
    {
        string fixture = "{ \"artifact\": \"x\", \"season\": \"2026/27\", \"teams\": [ { \"team\": \"El Mokawloon SC\", \"type\": \"club\", \"group\": \"Egyptian Premier League\", \"edition\": \"2026-09\", \"players\": [ "
            + "{ \"name\": \"Known Age\", \"age\": 30, \"position\": \"Goalkeeper\", \"power\": 66 }, "
            + "{ \"name\": \"Unknown Age\", \"age\": null, \"position\": \"Centre-Back\", \"power\": 68 } ] } ] }";
        string path = Path.Combine(Path.GetTempPath(), "players_database_combined-m9nullage.json");
        File.WriteAllText(path, fixture);
        try
        {
            Assert(JsonDatabaseProvider.ProbeTeamCount(path) == 1, "A file containing a null player age must still parse and count its team.");
            var root = System.Text.Json.JsonSerializer.Deserialize<JsonPlayerDbRoot>(fixture, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var players = root!.teams![0].players!;
            Assert(players[0].age == 30, "A known age must be preserved exactly.");
            Assert(players[1].age == null, "An unknown age must stay unknown (null) in the parsed entry, not become a made-up value.");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception($"ASSERT FAILED: {message}");
        }
    }
}
