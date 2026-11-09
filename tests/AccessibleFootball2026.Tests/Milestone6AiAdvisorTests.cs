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
}
