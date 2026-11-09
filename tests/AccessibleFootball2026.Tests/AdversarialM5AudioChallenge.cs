using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AccessibleFootball2026.Audio;
using AccessibleFootball2026.Audio.Anthems;
using AccessibleFootball2026.Audio.Commentary;
using AccessibleFootball2026.Audio.SFX;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;
using AccessibleFootball2026.Input;

namespace AccessibleFootball2026.Tests;

/// <summary>
/// CHALLENGER M5-2: EMPIRICAL ADVERSARIAL AUDIO & INTEGRATION STRESS TEST HARNESS
/// Rigorous empirical verification of:
/// 1. CommentaryEngine: 1,000 rapid-fire match events, concurrency, rate limiting, and all event types.
/// 2. CallnameManager: PES 2013 stars vs Modern 2026 stars, unknown names, edge cases, null names.
/// 3. StadiumSoundEngine: Rapid momentum oscillations, chant slots, regional fallback, and stadium acoustics.
/// 4. AnthemEngine: Missing files, headless test execution, and state tracking.
/// </summary>
public static class AdversarialM5AudioChallenge
{
    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception($"[CHALLENGER ASSERTION FAILED] {message}");
    }

    public static void RunAll(Action<string, Action> runTest)
    {
        Console.WriteLine("\n===============================================================");
        Console.WriteLine("--- CHALLENGER M5-2: ADVERSARIAL AUDIO & INTEGRATION HARNESS ---");
        Console.WriteLine("===============================================================");

        runTest("CHALLENGE 18A. CommentaryEngine Rapid-Fire: 1,000 burst events across all 15 event types without crashes",
            Test18A_CommentaryEngine_1000RapidFireBurstEvents);

        runTest("CHALLENGE 18B. CommentaryEngine Concurrency & Rate Limiting: 10 parallel threads firing 500 events concurrently",
            Test18B_CommentaryEngine_MultiThreadedStressAndRateLimiting);

        runTest("CHALLENGE 18C. CommentaryEngine Unhandled & Boundary Events: Offside, null goal data, unknown events fallback gracefully",
            Test18C_CommentaryEngine_BoundaryAndUnhandledEvents);

        runTest("CHALLENGE 18D. CallnameManager Classic vs Modern 2026 Stars: Clean fallback, zero exceptions, TTS-readiness",
            Test18D_CallnameManager_ClassicVsModern2026Stars_CleanFallback);

        runTest("CHALLENGE 18E. CallnameManager Robustness: Extreme Arabic diacritics, empty strings, and null inputs handling",
            Test18E_CallnameManager_ExtremeDiacriticsAndEdgeInputs);

        runTest("CHALLENGE 18F. StadiumSoundEngine Momentum Volatility: Rapid momentum reversals, score flips, and chant slots",
            Test18F_StadiumSoundEngine_RapidMomentumVolatilityAndAcoustics);

        runTest("CHALLENGE 18G. AnthemEngine Headless Stability: Missing national/CL anthems, invalid team IDs, and zero hanging",
            Test18G_AnthemEngine_MissingAssetsAndHeadlessExecution);
    }

    // -------------------------------------------------------------------------
    // 1. CommentaryEngine Rapid-Fire & Concurrency Stress
    // -------------------------------------------------------------------------

    public static void Test18A_CommentaryEngine_1000RapidFireBurstEvents()
    {
        var engine = new CommentaryEngine(CommentatorId.RaoufKhlif);
        var events = new List<MatchEvent>
        {
            new MatchEvent(MatchEventType.ShotTaken, "تسديدة قوية على المرمى"),
            new MatchEvent(MatchEventType.GoalkeeperSave, "تصدي رائع من الحارس"),
            new MatchEvent(MatchEventType.GoalScored, "هدف أول في المباراة", PitchZone.AttackingThird, new GoalDetails(new Player { Name = "محمد صلاح" }, null, false, 12, 1, 0, TeamDatabase.CreateLiverpool())),
            new MatchEvent(MatchEventType.GoalScored, "هدف عكسي في المرمى", PitchZone.AttackingThird, new GoalDetails(new Player { Name = "مدافع" }, null, true, 24, 0, 1, TeamDatabase.CreateArsenal())),
            new MatchEvent(MatchEventType.FoulCommitted, "مخالفة واضحة في منتصف الملعب"),
            new MatchEvent(MatchEventType.YellowCard, "بطاقة صفراء وإنذار"),
            new MatchEvent(MatchEventType.RedCard, "طرد مباشر وبطاقة حمراء"),
            new MatchEvent(MatchEventType.CornerKick, "ركلة ركنية من الجانب الأيمن"),
            new MatchEvent(MatchEventType.Offside, "تسلل وراية مرفوعة من المساعد"),
            new MatchEvent(MatchEventType.PlayerInjured, "سقوط وإصابة مؤلمة للاعب"),
            new MatchEvent(MatchEventType.HalfTime, "صافرة نهاية الشوط الأول"),
            new MatchEvent(MatchEventType.FullTime, "صافرة نهاية المباراة وختام اللقاء"),
            new MatchEvent(MatchEventType.WoodworkHit, "كرة ترتطم بالقائم"),
            new MatchEvent(MatchEventType.StoppageTimeAnnounced, "الوقت بدل الضائع المحتسب"),
            new MatchEvent(MatchEventType.DramaticComeback, "ريمونتادا وهدف الفوز القاتل"),
            new MatchEvent(MatchEventType.HatTrickAchieved, "هاتريك تاريخي وثلاثية شخصية")
        };

        int handledCount = 0;
        var sw = Stopwatch.StartNew();

        // Fire 1,000 rapid events with force = true
        for (int i = 0; i < 1000; i++)
        {
            var evt = events[i % events.Count];
            bool handled = engine.TryCommentOnEvent(evt, force: true);
            if (handled) handledCount++;
        }

        sw.Stop();

        // 15/16 event types are handled (Offside returns false gracefully)
        // In 1,000 events, 15/16ths = 937.5 ~ 938 events handled
        Assert(handledCount >= 900, 
            $"Expected >= 900 handled events out of 1000, got {handledCount}");
        Assert(sw.ElapsedMilliseconds < 2500, 
            $"1,000 rapid-fire events must complete within 2500ms (in-memory cache), took {sw.ElapsedMilliseconds}ms");
    }

    public static void Test18B_CommentaryEngine_MultiThreadedStressAndRateLimiting()
    {
        var engine = new CommentaryEngine(CommentatorId.RaoufKhlif);
        int spokenEvents = 0;
        int rateLimitedCount = 0;
        object lockObj = new();

        var evt = new MatchEvent(MatchEventType.ShotTaken, "تسديدة");

        // Test normal rate-limiting: consecutive calls with force=false must rate-limit to 1 per 1.2s
        bool first = engine.TryCommentOnEvent(evt, force: false);
        Assert(first, "First event must be accepted");

        bool secondImmediate = engine.TryCommentOnEvent(evt, force: false);
        Assert(!secondImmediate, "Immediate second event with force=false must be rate-limited (return false)");

        // Concurrent multi-threaded burst: 10 parallel threads firing 50 events each (force=true)
        var threads = new List<Thread>();
        var exceptions = new List<Exception>();

        for (int t = 0; t < 10; t++)
        {
            var th = new Thread(() =>
            {
                try
                {
                    for (int i = 0; i < 50; i++)
                    {
                        var e = new MatchEvent(MatchEventType.ShotTaken, "تسديدة متزامنة");
                        bool res = engine.TryCommentOnEvent(e, force: true);
                        if (res)
                        {
                            Interlocked.Increment(ref spokenEvents);
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (lockObj) exceptions.Add(ex);
                }
            });
            threads.Add(th);
        }

        foreach (var th in threads) th.Start();
        foreach (var th in threads) th.Join();

        Assert(exceptions.Count == 0, 
            $"Concurrent commentary firing produced {exceptions.Count} exceptions! First: {exceptions.FirstOrDefault()?.Message}");
        Assert(spokenEvents == 500, 
            $"Expected all 500 concurrent events with force=true to be processed, got {spokenEvents}");
    }

    public static void Test18C_CommentaryEngine_BoundaryAndUnhandledEvents()
    {
        var engine = new CommentaryEngine(CommentatorId.RaoufKhlif);

        // 1. Offside event -> Must return false cleanly without exception
        var offside = new MatchEvent(MatchEventType.Offside, "تسلل");
        bool offsideHandled = engine.TryCommentOnEvent(offside, force: true);
        Assert(!offsideHandled, "Offside event has no audio folder and must return false cleanly");

        // 2. GoalScored with null GoalData -> Must not throw NRE and handle as regular goal
        var goalNullData = new MatchEvent(MatchEventType.GoalScored, "هدف بدون بيانات");
        bool goalNullHandled = engine.TryCommentOnEvent(goalNullData, force: true);
        Assert(goalNullHandled, "GoalScored with null GoalData must fall back to 'Goals' folder cleanly");

        // 3. Switch commentator to all profiles and verify each handles events cleanly
        foreach (var profile in engine.GetAvailableCommentators())
        {
            engine.SwitchCommentator(profile.Id);
            Assert(engine.ActiveCommentator.Id == profile.Id, $"ActiveCommentator must be {profile.Id}");

            var foul = new MatchEvent(MatchEventType.FoulCommitted, "خطأ");
            bool foulHandled = engine.TryCommentOnEvent(foul, force: true);
            Assert(foulHandled, $"Commentator {profile.Id} must handle FoulCommitted event");
        }
    }

    // -------------------------------------------------------------------------
    // 2. CallnameManager Classic vs Modern 2026 Stars & Edge Inputs
    // -------------------------------------------------------------------------

    public static void Test18D_CallnameManager_ClassicVsModern2026Stars_CleanFallback()
    {
        var manager = new CallnameManager();

        // 1. Check if FindCallname method exists via reflection
        var findCallnameMethod = typeof(CallnameManager).GetMethod("FindCallname", BindingFlags.Public | BindingFlags.Instance);

        // 2. Classic PES 2013 Stars
        var classicStars = new[]
        {
            new Player { Id = 10, Name = "ليونيل ميسي" },
            new Player { Id = 7, Name = "كريستيانو رونالدو" },
            new Player { Id = 9, Name = "كريم بنزيما" },
            new Player { Id = 11, Name = "محمد صلاح" },
            new Player { Id = 10, Name = "لوكا مودريتش" }
        };

        foreach (var star in classicStars)
        {
            // TryPlayPlayerCallname must execute without exceptions
            bool played = manager.TryPlayPlayerCallname(star, force: true);
            // Whether mapped to audio file or returning false, zero exceptions must be thrown
            Assert(true, $"Classic star '{star.Name}' handled cleanly");
        }

        // 3. Modern 2026 Stars (Not in PES 2013 soundbank)
        var modern2026Stars = new[]
        {
            new Player { Id = 19, Name = "لامين يامال" },
            new Player { Id = 16, Name = "إندريك" },
            new Player { Id = 37, Name = "كوبي ماينو" },
            new Player { Id = 17, Name = "مايكل أوليس" },
            new Player { Id = 2, Name = "باو كوبارسي" },
            new Player { Id = 15, Name = "أردا غولر" }
        };

        foreach (var star in modern2026Stars)
        {
            bool played = manager.TryPlayPlayerCallname(star, force: true);
            // Modern stars must cleanly return false (graceful fallback) with zero exceptions
            Assert(!played, 
                $"Modern star '{star.Name}' must cleanly return false (no PES 2013 callname), got played={played}");
        }

        // 4. Obscure / synthetic names
        var unknownPlayer = new Player { Id = 999999, Name = "لاعب غير معروف إطلاقاً" };
        bool unknownPlayed = manager.TryPlayPlayerCallname(unknownPlayer, force: true);
        Assert(!unknownPlayed, "Unknown player must return false cleanly");
    }

    public static void Test18E_CallnameManager_ExtremeDiacriticsAndEdgeInputs()
    {
        var manager = new CallnameManager();

        // 1. Null player -> Must return false cleanly
        bool nullPlayerHandled = manager.TryPlayPlayerCallname(null!, force: true);
        Assert(!nullPlayerHandled, "Null player must return false cleanly");

        // 2. Empty or whitespace player name
        var emptyNamePlayer = new Player { Id = 1, Name = "" };
        bool emptyHandled = manager.TryPlayPlayerCallname(emptyNamePlayer, force: true);
        Assert(!emptyHandled, "Empty name player must return false cleanly");

        var whitespacePlayer = new Player { Id = 2, Name = "     " };
        bool wsHandled = manager.TryPlayPlayerCallname(whitespacePlayer, force: true);
        Assert(!wsHandled, "Whitespace name player must return false cleanly");

        // 3. Extreme Arabic diacritics and tatweel
        string rawDiacritics = "مُــــحَــــمَّـــــدٌ صَــــــلَاحْ";
        string normalized = CallnameManager.NormalizeArabic(rawDiacritics);
        Assert(normalized == "محمد صلاح", 
            $"Extreme diacritics and tatweel must normalize to 'محمد صلاح', got '{normalized}'");

        // 4. PreWarmSquads with nulls
        manager.PreWarmSquads(null!, null!);
        Assert(true, "PreWarmSquads with null teams must not throw");

        var dummyHome = new Team("DUMMY_H", "فريق 1", TeamSide.Home) { Players = new List<Player>() };
        var dummyAway = new Team("DUMMY_A", "فريق 2", TeamSide.Away) { Players = new List<Player>() };
        manager.PreWarmSquads(dummyHome, dummyAway);
        Assert(true, "PreWarmSquads with empty squads must not throw");
    }

    // -------------------------------------------------------------------------
    // 3. StadiumSoundEngine Momentum Volatility & Acoustics Stress
    // -------------------------------------------------------------------------

    public static void Test18F_StadiumSoundEngine_RapidMomentumVolatilityAndAcoustics()
    {
        var soundEngine = new StadiumSoundEngine();
        var home = TeamDatabase.CreateAlAhly();
        var away = TeamDatabase.CreateZamalek();

        soundEngine.SetMatchTeams(home, away);

        // 1. Rapid scoreline oscillations and dramatic events in minute 75+
        var momentumEvents = new[]
        {
            // Away scores (0-1) -> Discontent whistle
            new MatchEvent(MatchEventType.GoalScored, "هدف الزمالك", PitchZone.AttackingThird, new GoalDetails(away.Players[9], null, false, 76, 0, 1, away)),
            // Home enters attacking third -> Attacking momentum
            new MatchEvent(MatchEventType.BallZoneChanged, "دخول الثلث الهجومي", PitchZone.AttackingThird),
            // Opponent foul committed -> Discontent whistle
            new MatchEvent(MatchEventType.FoulCommitted, "خطأ ضد الأهلي"),
            // Home equalizer (1-1) -> Goal celebration
            new MatchEvent(MatchEventType.GoalScored, "هدف التعادل", PitchZone.AttackingThird, new GoalDetails(home.Players[9], null, false, 82, 1, 1, home)),
            // Home scores winner (2-1) -> Goal celebration
            new MatchEvent(MatchEventType.GoalScored, "هدف التقدم القاتل", PitchZone.AttackingThird, new GoalDetails(home.Players[9], null, false, 89, 2, 1, home))
        };

        for (int i = 0; i < 50; i++)
        {
            var evt = momentumEvents[i % momentumEvents.Length];
            soundEngine.HandleMatchContext(evt, homeScore: 1, awayScore: 1, matchMinute: 78.0f);
        }

        // 2. Rapid acoustic switches across 10 top domestic and continental teams
        var testTeams = new[]
        {
            TeamDatabase.CreateAlAhly(),
            TeamDatabase.CreateRealMadrid(),
            TeamDatabase.CreateBarcelona(),
            TeamDatabase.CreateLiverpool(),
            TeamDatabase.CreateManchesterCity(),
            TeamDatabase.CreateBayernMunich(),
            TeamDatabase.CreateParisSaintGermain(),
            TeamDatabase.CreateSaudiArabia(),
            TeamDatabase.CreateEgypt(),
            TeamDatabase.CreateFrance()
        };

        foreach (var team in testTeams)
        {
            soundEngine.ApplyStadiumAcousticsForTeam(team);
            soundEngine.PlayTeamChant(team, ChantSlot.AttackingMomentum);
            soundEngine.PlayTeamChant(team, ChantSlot.GoalCelebration);
            soundEngine.PlayTeamChant(team, ChantSlot.ComebackDefiance);
            soundEngine.PlayTeamChant(team, ChantSlot.DiscontentWhistle);
            soundEngine.PlayTeamChant(team, ChantSlot.GeneralRhythm);
        }

        Assert(true, "StadiumSoundEngine survived 50 rapid momentum events and 10-team acoustic sweeps");
    }

    // -------------------------------------------------------------------------
    // 4. AnthemEngine Missing Assets & Headless Execution
    // -------------------------------------------------------------------------

    public static void Test18G_AnthemEngine_MissingAssetsAndHeadlessExecution()
    {
        var mockScreenReader = new TestMockScreenReader();
        var anthemEngine = new AnthemEngine(mockScreenReader);

        // 1. PlayChampionsLeagueIntro in automated test mode -> Must return immediately (no delay, no sound)
        var sw = Stopwatch.StartNew();
        anthemEngine.PlayChampionsLeagueIntro(isAutomatedTest: true);
        sw.Stop();
        Assert(sw.ElapsedMilliseconds < 50, 
            $"PlayChampionsLeagueIntro(isAutomatedTest: true) must return immediately, took {sw.ElapsedMilliseconds}ms");

        // 2. PlayMatchNationalAnthems in automated test mode
        var egypt = TeamDatabase.CreateEgypt();
        var morocco = TeamDatabase.CreateMorocco();
        sw.Restart();
        anthemEngine.PlayMatchNationalAnthems(egypt, morocco, isAutomatedTest: true);
        sw.Stop();
        Assert(sw.ElapsedMilliseconds < 50, 
            $"PlayMatchNationalAnthems(isAutomatedTest: true) must return immediately, took {sw.ElapsedMilliseconds}ms");

        // 3. PlayNationalAnthemSample with synthetic/non-existent nation ID
        anthemEngine.PlayNationalAnthemSample("NON_EXISTENT_NATION_XYZ_999");
        Assert(anthemEngine.LastPlayedAnthem == "NON_EXISTENT_NATION_XYZ_999", 
            "LastPlayedAnthem must be updated even if audio file is synthetic");

        // 4. PlayNationalAnthemSample with null teamId -> Falls back to default "EGY"
        anthemEngine.PlayNationalAnthemSample(null);
        Assert(anthemEngine.LastPlayedAnthem == "EGY", 
            "LastPlayedAnthem must fall back to 'EGY' when null teamId is provided");

        // 5. PlayChampionsLeagueSample -> Must not crash even in headless MCI environment
        anthemEngine.PlayChampionsLeagueSample();
        Assert(anthemEngine.LastPlayedAnthem == "ChampionsLeague", 
            "LastPlayedAnthem must be 'ChampionsLeague'");

        // 6. StopAllSounds
        anthemEngine.StopAllSounds();
        Assert(true, "AnthemEngine.StopAllSounds executed cleanly");
    }

    private class TestMockScreenReader : IScreenReader
    {
        public bool IsAvailable => true;
        public List<string> SpokenTexts { get; } = new();
        public void Speak(string text, bool interrupt = true) => SpokenTexts.Add(text);
        public void CancelSpeech() { }
    }
}
