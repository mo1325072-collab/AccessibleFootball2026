using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;
using AccessibleFootball2026.Core.Tactics;
using AccessibleFootball2026.Input;

namespace AccessibleFootball2026.Tests;

/// <summary>
/// MILESTONE 6: AI TACTICAL ADVISOR & AI VAR SUITE
/// Covers Tests 18A through 18E:
/// - 18A: AI Tactical Advisor generates contextual bilingual reports
/// - 18B: AI Tactical Advisor identifies fatigued players and suggests bench replacements
/// - 18C: AI Tactical Advisor dynamic formation adjustments (trailing vs leading)
/// - 18D: AI Auto-Substitutions executes safely under IFAB Law 3 limits
/// - 18E: AI VAR Referee reviews offsides, penalties, and goal-line technology
/// </summary>
public static class Milestone6AiAdvisorTests
{
    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception($"Assertion Failed: {message}");
    }

    public static void RunAllTests(Action<string, Action> runTest)
    {
        Console.WriteLine("\n--- MILESTONE 6: AI TACTICAL ADVISOR & AI VAR TEST SUITE ---");

        runTest("18A. AI Tactical Advisor: Generates contextual Arabic and English tactical briefings",
            TestAiAdvisor_GeneratesContextualBriefings);

        runTest("18B. AI Tactical Advisor: Identifies fatigued players and finds best positional bench replacement",
            TestAiAdvisor_IdentifiesFatiguedPlayersAndPositionalSubs);

        runTest("18C. AI Tactical Advisor: Dynamic formation recommendations based on score and time",
            TestAiAdvisor_DynamicFormationRecommendations);

        runTest("18D. AI Auto-Substitutions: Complies strictly with IFAB Law 3 limits and stamina thresholds",
            TestAiAutoSubstitutions_Law3Compliance);

        runTest("18E. AI VAR Referee: Reviews offsides, penalties, and goal-line technology with bilingual output",
            TestAiVarReferee_ReviewScenarios);

        runTest("18F. AI Dynamic Tactics: Trailing late triggers Gegenpress (85%) and direct attacking push",
            TestAiDynamicTactics_TrailingLate_PushesGegenpressAndLongBalls);

        runTest("18G. AI Dynamic Tactics: Underdog against superstar team enforces Zonal Marking and Low Block",
            TestAiDynamicTactics_UnderdogAgainstSuperstar_EnforcesZonalAndCounter);

        runTest("18H. AI Dynamic Tactics: Optional settings toggle controls user team auto-adjustment strictly",
            TestAiDynamicTactics_SettingsToggle_OptionalControl);

        runTest("18I. AI Dynamic Tactics: Facing opponent 85% high press counters with long balls into space",
            TestAiDynamicTactics_OpponentHighPressCounter_EscapesWithLongBalls);
    }

    private static void TestAiAdvisor_GeneratesContextualBriefings()
    {
        var home = TeamDatabase.CreateAlAhly();
        var away = TeamDatabase.CreateZamalek();
        var bus = new MatchEventBus();
        var input = new KeyboardInputHandler();
        var match = new MatchEngine(home, away, bus, input);

        match.SetMatchTimeMinutes(25.0f);
        string arReport = TacticalAiAdvisor.GenerateTacticalReport(match, UiLanguage.Arabic);
        Assert(arReport.Contains("تقرير المستشار التكتيكي بالذكاء الاصطناعي"), "Arabic report must contain AI advisor header");
        Assert(arReport.Contains("الدقيقة 25"), "Report must reflect current match minute");

        string enReport = TacticalAiAdvisor.GenerateTacticalReport(match, UiLanguage.English);
        Assert(enReport.Contains("AI Tactical Advisor Report at minute 25"), "English report must contain AI advisor header and minute");
    }

    private static void TestAiAdvisor_IdentifiesFatiguedPlayersAndPositionalSubs()
    {
        var home = TeamDatabase.CreateRealMadrid();
        var away = TeamDatabase.CreateBarcelona();
        var bus = new MatchEventBus();
        var input = new KeyboardInputHandler();
        var match = new MatchEngine(home, away, bus, input);

        // Fatigued winger
        var winger = home.Players.First(p => p.Role == PlayerRole.Forward && !p.IsSentOff);
        winger.BaseStamina = 45; // Low stamina

        var replacement = TacticalAiAdvisor.FindBestBenchReplacement(home, winger);
        Assert(replacement != null, "Must find replacement on bench");
        Assert(replacement != winger, "Replacement must be a bench player");
        Assert(!home.Players.Contains(replacement), "Replacement must not already be on the pitch");
    }

    private static void TestAiAdvisor_DynamicFormationRecommendations()
    {
        var home = TeamDatabase.CreateArsenal();
        var away = EnglishPremierLeagueDatabase.CreateChelsea();
        var bus = new MatchEventBus();
        var input = new KeyboardInputHandler();
        var match = new MatchEngine(home, away, bus, input);

        // Simulate trailing at minute 70 with 4-4-2 formation
        home.CurrentFormation = FormationType.Formation442_Flat;
        home.Score = 0;
        away.Score = 2;
        match.SetMatchTimeMinutes(70.0f);

        bool actionCalled = false;
        var recs = TacticalAiAdvisor.GetRecommendations(match, () => { actionCalled = true; });
        Assert(recs.Count > 0, "Must return tactical recommendations");

        var formRec = recs.FirstOrDefault(r => r.SuggestedFormation == FormationType.Formation433_Attack);
        Assert(formRec != null, "Trailing team must receive 4-3-3 attacking recommendation");

        // Apply recommendation
        formRec!.ApplyAction?.Invoke();
        Assert(actionCalled, "Applying recommendation must invoke onApplied callback");
        Assert(home.CurrentFormation == FormationType.Formation433_Attack, "Team formation must update to 4-3-3");
    }

    private static void TestAiAutoSubstitutions_Law3Compliance()
    {
        var home = TeamDatabase.CreateLiverpool();
        var away = TeamDatabase.CreateManchesterCity();
        var bus = new MatchEventBus();
        var input = new KeyboardInputHandler();
        var match = new MatchEngine(home, away, bus, input);

        // Disabled by default
        MatchSettings.Current.AutoSubstitutionsEnabled = false;
        match.SetMatchTimeMinutes(65.0f);
        var midfielder = home.Players.First(p => p.Role == PlayerRole.Midfielder);
        midfielder.BaseStamina = 50;

        bool executed = TacticalAiAdvisor.TryPerformAutoSubstitution(match, out string msg);
        Assert(!executed, "Auto sub must NOT execute when disabled in settings");

        // Enabled
        MatchSettings.Current.AutoSubstitutionsEnabled = true;
        executed = TacticalAiAdvisor.TryPerformAutoSubstitution(match, out msg);
        Assert(executed, "Auto sub must execute when enabled and player is fatigued past minute 60");
        Assert(!string.IsNullOrEmpty(msg), "Announcement must be generated");
        Assert(home.SubstitutionsUsed == 1, "Must consume 1 IFAB substitution");

        // Reset setting
        MatchSettings.Current.AutoSubstitutionsEnabled = false;
    }

    private static void TestAiVarReferee_ReviewScenarios()
    {
        // 1. Offside
        var offsideResult = VarAiReferee.ReviewOffside("كيليان مبابي", initialCallOffside: true);
        Assert(!string.IsNullOrEmpty(offsideResult.AnnouncementArabic), "Offside review must have Arabic text");
        Assert(!string.IsNullOrEmpty(offsideResult.AnnouncementEnglish), "Offside review must have English text");
        Assert(offsideResult.DecisionMarginCm > 0, "Offside check must have margin measurement");

        // 2. Penalty
        var penaltyResult = VarAiReferee.ReviewPenalty("لامين يامال", "أنطونيو روديغر", initialCallPenalty: true);
        Assert(!string.IsNullOrEmpty(penaltyResult.AnnouncementArabic), "Penalty review must have Arabic text");

        // 3. Goal-Line
        var goalLineResult = VarAiReferee.ReviewGoalLine("مانشستر سيتي");
        Assert(!string.IsNullOrEmpty(goalLineResult.AnnouncementArabic), "Goal-line review must have Arabic text");
        Assert(goalLineResult.DecisionMarginCm > 0, "Goal-line margin must be greater than zero");
    }

    private static void TestAiDynamicTactics_TrailingLate_PushesGegenpressAndLongBalls()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var match = new MatchEngine(homeTeam, awayTeam, new MatchEventBus(), new KeyboardInputHandler());

        homeTeam.Score = 0;
        awayTeam.Score = 1;
        match.SetMatchTimeMinutes(78.0f);

        var ideal = TacticalAiAdvisor.ComputeDynamicTactics(match, homeTeam, awayTeam);

        Assert(ideal.PressingIntensity >= 75, $"Trailing late in match must push Pressing to >= 75. Got: {ideal.PressingIntensity}");
        Assert(ideal.PassingDirectness >= 75, $"Trailing late in match must push Passing to >= 75. Got: {ideal.PassingDirectness}");
        Assert(ideal.Tackling == TacklingAggression.Aggressive, "Trailing late must trigger aggressive tackling");
    }

    private static void TestAiDynamicTactics_UnderdogAgainstSuperstar_EnforcesZonalAndCounter()
    {
        var weakTeam = new Team("W", "فريق متواضع", TeamSide.Home);
        for (int i = 0; i < 11; i++)
        {
            weakTeam.Players.Add(new Player(i + 1, $"لاعب {i + 1}", i + 1, SpecificPosition.CM, spd: 55, pas: 55, sho: 50, def: 52, phy: 55));
        }

        var eliteTeam = new Team("E", "فريق خارق", TeamSide.Away);
        for (int i = 0; i < 11; i++)
        {
            eliteTeam.Players.Add(new Player(i + 1, $"نجم {i + 1}", i + 1, SpecificPosition.CM, spd: 88, pas: 85, sho: 85, def: 85, phy: 86));
        }

        var match = new MatchEngine(weakTeam, eliteTeam, new MatchEventBus(), new KeyboardInputHandler());
        match.SetMatchTimeMinutes(25.0f);

        var ideal = TacticalAiAdvisor.ComputeDynamicTactics(match, weakTeam, eliteTeam);

        Assert(ideal.Marking == DefensiveMarking.Zonal, "Underdog team MUST strictly play Zonal Marking against superior team");
        Assert(ideal.PressingIntensity <= 30, $"Underdog must play Low Block (Pressing <= 30). Got: {ideal.PressingIntensity}");
        Assert(ideal.Focus == AttackingFocus.CounterAttack, "Underdog must focus on CounterAttack");
    }

    private static void TestAiDynamicTactics_SettingsToggle_OptionalControl()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var match = new MatchEngine(homeTeam, awayTeam, new MatchEventBus(), new KeyboardInputHandler());

        homeTeam.Score = 0;
        awayTeam.Score = 2;
        match.SetMatchTimeMinutes(80.0f);

        // 1. عند تعطيل الخيار في الإعدادات: التعديل التلقائي لا يمس فريق المستخدم
        MatchSettings.Current.AutoTacticalAdjustmentsEnabled = false;
        int origPress = homeTeam.Tactics.PressingIntensity;
        bool appliedDisabled = TacticalAiAdvisor.TryPerformDynamicTacticalAdjustment(match, homeTeam, out string ann1);

        Assert(!appliedDisabled, "When AutoTacticalAdjustmentsEnabled is false, user team tactics must not be altered");
        Assert(homeTeam.Tactics.PressingIntensity == origPress, "User team tactics must remain intact when disabled");
        Assert(string.IsNullOrEmpty(ann1), "Announcement must be empty when auto-adjust is disabled");

        // 2. عند تفعيل الخيار في الإعدادات: الذكاء الاصطناعي يتدخل تلقائياً ويطلق الإشعار الصوتي
        MatchSettings.Current.AutoTacticalAdjustmentsEnabled = true;
        bool appliedEnabled = TacticalAiAdvisor.TryPerformDynamicTacticalAdjustment(match, homeTeam, out string ann2);

        Assert(appliedEnabled, "When AutoTacticalAdjustmentsEnabled is true, AI must dynamically adjust tactics");
        Assert(homeTeam.Tactics.PressingIntensity >= 75, $"Pressing must be pushed to high block. Got: {homeTeam.Tactics.PressingIntensity}");
        Assert(!string.IsNullOrEmpty(ann2), "Announcement must describe the dynamic tactical shift");
        Assert(ann2.Contains("تعديل تكتيكي"), $"Announcement must mention tactical adjustment. Got: '{ann2}'");
    }

    private static void TestAiDynamicTactics_OpponentHighPressCounter_EscapesWithLongBalls()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var match = new MatchEngine(homeTeam, awayTeam, new MatchEventBus(), new KeyboardInputHandler());

        // الخصم يضغط عالي جداً بـ 85%
        awayTeam.Tactics.PressingIntensity = 85;
        match.SetMatchTimeMinutes(30.0f);

        var ideal = TacticalAiAdvisor.ComputeDynamicTactics(match, homeTeam, awayTeam);

        Assert(ideal.PassingDirectness >= 75, $"Facing opponent 85% high press must trigger long/direct passing (>= 75). Got: {ideal.PassingDirectness}");
        Assert(ideal.Focus == AttackingFocus.CounterAttack, "Facing high press must trigger CounterAttack to exploit space behind");
    }
}
