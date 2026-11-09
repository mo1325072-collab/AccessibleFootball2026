using System;
using System.Linq;
using System.Numerics;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;
using AccessibleFootball2026.Input;

namespace AccessibleFootball2026.Tests;

/// <summary>
/// EMPIRICAL CHALLENGER (M2): Adversarial Stress Test Suite for IFAB Tactical Realism:
/// Law 3 Substitutions (Limits, Windows, Stoppages, HT, ET),
/// Law 12 Disciplinary Sanctions (Cards, DOGSO geometry, Severe Tackles from Behind),
/// 10-Man Team Play & Formation Reconfiguration, Player Injuries, and Dynamic Aiming Angles.
/// </summary>
public static class AdversarialM2TacticalChallenge
{
    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception($"Assertion Failed: {message}");
    }

    private static MatchEngine CreateMatch()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();
        return new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
    }

    public static void RunAll(Action<string, Action> runTest)
    {
        Console.WriteLine("\n--- EMPIRICAL CHALLENGER (M2): ADVERSARIAL TACTICAL STRESS TEST SUITE ---");

        runTest("13A. Law 3 Exhaustion: 5 subs in 1 single stoppage consumes exactly 1 window; blocks 6th sub",
            TestLaw3_Exhaust5SubsInSingleStoppage_Blocks6thSub);

        runTest("13B. Law 3 Window Exhaustion: 3 windows with 3 subs leaves 2 unused subs; blocks 4th window",
            TestLaw3_Exhaust3WindowsWith3Subs_Blocks4thWindowWithSubsRemaining);

        runTest("13C. Law 3 Half-Time Subs: Free of windows; allows 3 distinct stoppage opportunities in 2nd half",
            TestLaw3_HalfTimeSubs_FreeWindows_Allows3StoppagesInSecondHalf);

        runTest("13D. Law 3 Tactical Freedom: Repeated formation changes & position swaps consume zero windows",
            TestLaw3_TacticalSwapsAndFormations_AcrossMultiplePauses_ZeroWindowsConsumed);

        runTest("13E. Law 3 Extra Time: Grants 6th sub and 4th window; strictly blocks 7th sub and 5th window",
            TestLaw3_ExtraTime_Unlock6thSubAnd4thWindow_Blocks7thSubAnd5thWindow);

        runTest("13F. Law 12 Yellow Accumulation: 1st yellow stays active; 2nd yellow dismisses & transfers user control",
            TestLaw12_YellowCard_FirstStays_SecondDismissed_ActiveUserHandover);

        runTest("13G. Law 12 DOGSO Distance Boundary: Last defender at 44.9m is direct red; at 46m is yellow",
            TestLaw12_DOGSO_DistanceBoundary_45MThreshold);

        runTest("13H. Law 12 DOGSO Covering Defender: Covering defender between carrier and goal prevents direct red",
            TestLaw12_DOGSO_CoveringDefender_PreventsDirectRed);

        runTest("13I. Law 12 DOGSO 2nd Half: Side-switch geometry accurately targets RightGoal (X=105)",
            TestLaw12_DOGSO_SecondHalf_InvertedGoalDetection);

        runTest("13J. Law 12 Severe Tackle: Slide from behind is direct red; stand from behind or slide from front is yellow",
            TestLaw12_SevereTackleFromBehind_SlideVsStand);

        runTest("13K. 10-Man Play: FormationManager adapts all 8 formations for 10-man squad without exceptions",
            TestLaw12_TenManTeamPlay_FormationAdaptationAndSimulationStability);

        runTest("13L. Player Injuries: Minor knock reduces speed/stamina to 80%; severe injury forces sub or 10-man",
            TestInjuries_MinorKnock_DynamicMultiplierAndStamina_SevereForcedSub);

        runTest("13M. Aiming Angles: Continuous Up/Down adjusts Y within [-0.90, +0.90] and offsets shot/cross targets",
            TestAimingAngles_FineDirectionalControl_ClampingBounds);

        runTest("13N. Law 3/12 Integrity: Sent-off player cannot be substituted back into active play",
            TestLaw3_Law12_SentOffPlayer_CannotBeSubstituted);

        runTest("13O. 10-Man Dead-Ball Restarts: Restarts & HT must NEVER assign dismissed players",
            TestLaw12_TenManPlay_DeadBallRestarts_NeverAssignDismissedPlayer);

        runTest("13P. 10-Man Pass Targeting: Pass targeting fallback must strictly ignore dismissed players",
            TestLaw12_TenManPlay_PassTargeting_FallbackIgnoresDismissedPlayer);

        runTest("13Q. Ball Ownership on Dismissal: Carrier dismissal releases ball & prevents off-pitch teleportation",
            TestLaw12_CarrierDismissal_ReleasesBall_PreventsTeleportation);

        runTest("13R. Possession Grace & AI Tackle Pacing: Grace period prevents 50Hz AI tackles and enforces cooldown",
            TestPossessionGracePeriod_BlocksInstantAITackle_AndRateLimitsAI);
    }

    public static void TestLaw3_Exhaust5SubsInSingleStoppage_Blocks6thSub()
    {
        var match = CreateMatch();
        var team = match.HomeTeam;

        match.PauseMatch();
        Assert(team.StoppageWindowsUsed == 0, "Initial windows must be 0");
        Assert(team.SubstitutionsUsed == 0, "Initial subs must be 0");

        // Make all 5 allowed subs during the VERY FIRST stoppage window
        for (int i = 0; i < 5; i++)
        {
            var starter = team.Players[10 - i];
            var benchPlayer = team.Bench[0]; // will take first available bench
            bool ok = match.MakeSubstitution(team, starter, benchPlayer);
            Assert(ok, $"Sub {i + 1} in single stoppage must succeed");
            Assert(team.SubstitutionsUsed == i + 1, $"Subs used must be {i + 1}");
            Assert(team.StoppageWindowsUsed == 1, $"Stoppage windows used must remain exactly 1 throughout");
        }

        // Attempt 6th substitution in the same window (should fail: 5 subs limit reached!)
        Assert(!match.CanMakeSubstitution(team), "CanMakeSubstitution must be false after 5 subs");
        bool sub6SameWindow = match.MakeSubstitution(team, team.Players[5], team.Bench[0]);
        Assert(!sub6SameWindow, "6th substitution in same window must be blocked");
        Assert(team.SubstitutionsUsed == 5, "Substitutions used must remain capped at 5");

        // Resume match, then pause again for Stoppage 2
        match.ResumeMatch();
        match.PauseMatch();

        // Stoppage 2: Can they make a 6th sub? (Windows available = 2, but subs available = 0)
        Assert(!match.CanMakeSubstitution(team), "CanMakeSubstitution must be false in Stoppage 2 (subs exhausted)");
        bool sub6Window2 = match.MakeSubstitution(team, team.Players[4], team.Bench[0]);
        Assert(!sub6Window2, "6th substitution in Stoppage 2 must be blocked");
        Assert(team.StoppageWindowsUsed == 1, "StoppageWindowsUsed must remain 1 (no window consumed on blocked sub)");
    }

    public static void TestLaw3_Exhaust3WindowsWith3Subs_Blocks4thWindowWithSubsRemaining()
    {
        var match = CreateMatch();
        var team = match.HomeTeam;

        // Window 1: 1 sub
        match.PauseMatch();
        Assert(match.MakeSubstitution(team, team.Players[10], team.Bench[0]), "Sub 1 should succeed");
        match.ResumeMatch();

        // Window 2: 1 sub
        match.PauseMatch();
        Assert(match.MakeSubstitution(team, team.Players[9], team.Bench[0]), "Sub 2 should succeed");
        match.ResumeMatch();

        // Window 3: 1 sub
        match.PauseMatch();
        Assert(match.MakeSubstitution(team, team.Players[8], team.Bench[0]), "Sub 3 should succeed");
        match.ResumeMatch();

        Assert(team.SubstitutionsUsed == 3, "Total subs used must be 3");
        Assert(team.StoppageWindowsUsed == 3, "Total windows used must be 3");
        Assert(team.RemainingSubstitutions == 2, "Team has 2 substitutions theoretically remaining");
        Assert(team.RemainingStoppageWindows == 0, "Team has 0 stoppage windows remaining in 90m");

        // Stoppage 4: Attempt a sub! Even though team has 2 subs left, all 3 windows are consumed!
        match.PauseMatch();
        Assert(!match.CanMakeSubstitution(team), "CanMakeSubstitution must be false because all 3 windows are exhausted");
        bool sub4 = match.MakeSubstitution(team, team.Players[7], team.Bench[0]);
        Assert(!sub4, "4th stoppage window substitution must be blocked even when unused subs remain");
        Assert(team.SubstitutionsUsed == 3, "Subs used must remain 3");
        Assert(team.StoppageWindowsUsed == 3, "Windows used must remain 3");
    }

    public static void TestLaw3_HalfTimeSubs_FreeWindows_Allows3StoppagesInSecondHalf()
    {
        var match = CreateMatch();
        var team = match.HomeTeam;

        // Reach Half-Time
        match.TriggerHalfTime();
        Assert(match.State == MatchState.HalfTime, "Match must be at HalfTime");

        // Make 2 substitutions during Half-Time interval
        Assert(match.MakeSubstitution(team, team.Players[10], team.Bench[0]), "HT Sub 1 should succeed");
        Assert(match.MakeSubstitution(team, team.Players[9], team.Bench[0]), "HT Sub 2 should succeed");

        Assert(team.SubstitutionsUsed == 2, "Subs used at HT should be 2");
        Assert(team.StoppageWindowsUsed == 0, "HT substitutions must NOT consume any stoppage window (0 used)");
        Assert(team.RemainingStoppageWindows == 3, "All 3 stoppage windows must remain available for the second half");

        // Second Half begins
        match.ResumeMatch();
        Assert(match.State == MatchState.InPlay, "Match resumed in 2nd half");

        // Stoppage 1 in 2nd half: 1 sub (Window 1 used)
        match.PauseMatch();
        Assert(match.MakeSubstitution(team, team.Players[8], team.Bench[0]), "2nd half Sub 1 should succeed");
        Assert(team.StoppageWindowsUsed == 1, "Windows used should be 1");
        Assert(team.SubstitutionsUsed == 3, "Subs used should be 3");
        match.ResumeMatch();

        // Stoppage 2 in 2nd half: 1 sub (Window 2 used)
        match.PauseMatch();
        Assert(match.MakeSubstitution(team, team.Players[7], team.Bench[0]), "2nd half Sub 2 should succeed");
        Assert(team.StoppageWindowsUsed == 2, "Windows used should be 2");
        Assert(team.SubstitutionsUsed == 4, "Subs used should be 4");
        match.ResumeMatch();

        // Stoppage 3 in 2nd half: 1 sub (Window 3 used)
        match.PauseMatch();
        Assert(match.MakeSubstitution(team, team.Players[6], team.Bench[0]), "2nd half Sub 3 should succeed");
        Assert(team.StoppageWindowsUsed == 3, "Windows used should be 3");
        Assert(team.SubstitutionsUsed == 5, "Total subs used must be 5 (2 at HT + 3 in 2nd half)");
        match.ResumeMatch();

        // Stoppage 4: Attempt 6th sub (subs and windows both exhausted!)
        match.PauseMatch();
        Assert(!match.CanMakeSubstitution(team), "No more subs or windows allowed");
        Assert(!match.MakeSubstitution(team, team.Players[5], team.Bench[0]), "6th sub must be blocked");
    }

    public static void TestLaw3_TacticalSwapsAndFormations_AcrossMultiplePauses_ZeroWindowsConsumed()
    {
        var match = CreateMatch();
        var team = match.HomeTeam;

        for (int stoppage = 1; stoppage <= 5; stoppage++)
        {
            match.PauseMatch();

            // Swap player positions on pitch
            team.SwapPositions(1, 2);
            team.SwapPositions(7, 8);

            // Change formation
            var nextFormation = (FormationType)(stoppage % 8);
            team.ChangeFormation(nextFormation);

            Assert(team.StoppageWindowsUsed == 0, $"Stoppage {stoppage}: Tactical adjustments must NOT consume windows");
            Assert(team.SubstitutionsUsed == 0, $"Stoppage {stoppage}: Tactical adjustments must NOT increment subs");

            match.ResumeMatch();
        }

        Assert(team.RemainingSubstitutions == 5, "Team must still have all 5 substitutions intact");
        Assert(team.RemainingStoppageWindows == 3, "Team must still have all 3 stoppage windows intact");
    }

    public static void TestLaw3_ExtraTime_Unlock6thSubAnd4thWindow_Blocks7thSubAnd5thWindow()
    {
        var match = CreateMatch();
        var team = match.HomeTeam;

        // Exhaust all 5 subs across 3 windows in 90m
        match.PauseMatch();
        match.MakeSubstitution(team, team.Players[10], team.Bench[0]); // Win 1, sub 1
        match.MakeSubstitution(team, team.Players[9], team.Bench[0]);  // Win 1, sub 2
        match.ResumeMatch();

        match.PauseMatch();
        match.MakeSubstitution(team, team.Players[8], team.Bench[0]);  // Win 2, sub 3
        match.MakeSubstitution(team, team.Players[7], team.Bench[0]);  // Win 2, sub 4
        match.ResumeMatch();

        match.PauseMatch();
        match.MakeSubstitution(team, team.Players[6], team.Bench[0]);  // Win 3, sub 5
        match.ResumeMatch();

        Assert(team.SubstitutionsUsed == 5, "5 subs used");
        Assert(team.StoppageWindowsUsed == 3, "3 windows used");
        Assert(!match.CanMakeSubstitution(team), "Subs/windows exhausted in 90m");

        // Trigger Extra Time
        match.TriggerExtraTime();
        Assert(match.IsExtraTime, "Match must be in Extra Time");
        Assert(team.IsExtraTime, "Team must be in Extra Time");
        Assert(team.MaxSubstitutions == 6, "MaxSubstitutions must be 6 in Extra Time");
        Assert(team.MaxStoppageWindows == 4, "MaxStoppageWindows must be 4 in Extra Time");
        Assert(team.RemainingSubstitutions == 1, "Exactly 1 substitution remaining in Extra Time");
        Assert(team.RemainingStoppageWindows == 1, "Exactly 1 stoppage window remaining in Extra Time");
        Assert(match.CanMakeSubstitution(team), "CanMakeSubstitution must be true in Extra Time");

        // Stoppage 4: Make 6th sub
        match.PauseMatch();
        bool sub6 = match.MakeSubstitution(team, team.Players[5], team.Bench[0]);
        Assert(sub6, "6th substitution in Extra Time must succeed");
        Assert(team.SubstitutionsUsed == 6, "Total subs used must be 6");
        Assert(team.StoppageWindowsUsed == 4, "Total windows used must be 4");

        // Attempt 7th sub in Window 4: blocked (6 subs max reached)
        Assert(!match.CanMakeSubstitution(team), "Cannot make 7th sub");
        bool sub7SameWindow = match.MakeSubstitution(team, team.Players[4], team.Bench[0]);
        Assert(!sub7SameWindow, "7th sub in window 4 must be blocked");

        match.ResumeMatch();

        // Stoppage 5: Attempt sub in 5th window (blocked)
        match.PauseMatch();
        Assert(!match.CanMakeSubstitution(team), "CanMakeSubstitution must be false after 4 windows");
        bool subWindow5 = match.MakeSubstitution(team, team.Players[3], team.Bench[0]);
        Assert(!subWindow5, "Substitution in 5th window must be blocked");
    }

    public static void TestLaw12_YellowCard_FirstStays_SecondDismissed_ActiveUserHandover()
    {
        var match = CreateMatch();
        var controlledPlayer = match.ActiveUserPlayer;

        // 1st Yellow Card
        match.IssueCard(controlledPlayer, isDirectRed: false, reason: "عرقلة متهورة");
        Assert(controlledPlayer.YellowCards == 1, "YellowCards must be 1");
        Assert(!controlledPlayer.IsSentOff, "Must not be sent off on 1st yellow");
        Assert(controlledPlayer.IsActiveOnPitch, "Must remain active on pitch");
        Assert(match.ActiveUserPlayer == controlledPlayer, "ActiveUserPlayer should remain player after 1st yellow");

        // 2nd Yellow Card -> Red Card dismissal!
        match.IssueCard(controlledPlayer, isDirectRed: false, reason: "تدخل تكتيكي");
        Assert(controlledPlayer.YellowCards == 2, "YellowCards must be 2");
        Assert(controlledPlayer.IsSentOff, "Must be sent off on 2nd yellow");
        Assert(!controlledPlayer.IsActiveOnPitch, "Must not be active on pitch");
        Assert(controlledPlayer.Position2D.X < 0 && controlledPlayer.Position2D.Y < 0, "Sent-off player position must be outside pitch");

        // Active user player must have been transferred away from dismissed player
        Assert(match.ActiveUserPlayer != controlledPlayer, "ActiveUserPlayer must be reassigned when dismissed");
        Assert(match.ActiveUserPlayer.IsActiveOnPitch, "New ActiveUserPlayer must be active on pitch");
        Assert(match.ActiveUserPlayer.Role != PlayerRole.Goalkeeper, "New ActiveUserPlayer should not be goalkeeper");
    }

    public static void TestLaw12_DOGSO_DistanceBoundary_45MThreshold()
    {
        var match = CreateMatch();
        var tackler = match.HomeTeam.Players[3];
        var carrier = match.AwayTeam.Players[9];

        // Defending Left goal at X=0 in 1st half
        // Position all other defenders behind carrier (X > carrier.X) so tackler is last defender
        foreach (var def in match.HomeTeam.Players.Where(p => p != tackler && p.Role != PlayerRole.Goalkeeper))
        {
            def.Position2D = new Vector2(70.0f, def.Position2D.Y);
        }

        // Test 1: Carrier at 44.0m (<= 45.0m threshold) -> DOGSO!
        carrier.Position2D = new Vector2(44.0f, 34.0f);
        tackler.Position2D = new Vector2(44.5f, 34.0f);
        Assert(match.CheckDogso(tackler, carrier), "Carrier at 44.0m from goal with last defender must be DOGSO");

        // Test 2: Carrier at 46.0m (> 45.0m threshold) -> NOT DOGSO!
        carrier.Position2D = new Vector2(46.0f, 34.0f);
        tackler.Position2D = new Vector2(46.5f, 34.0f);
        Assert(!match.CheckDogso(tackler, carrier), "Carrier at 46.0m from goal is too far for DOGSO (>45m)");

        // Verify foul sanction at 46m issues YELLOW card (not direct red)
        match.HandleFoulSanction(tackler, carrier, isSlideTackle: false, isFromBehind: false);
        Assert(!tackler.IsSentOff, "Foul beyond 45m should NOT issue direct red card");
        Assert(tackler.YellowCards == 1, "Foul beyond 45m should issue yellow card");
    }

    public static void TestLaw12_DOGSO_CoveringDefender_PreventsDirectRed()
    {
        var match = CreateMatch();
        var tackler = match.HomeTeam.Players[3];
        var carrier = match.AwayTeam.Players[9];
        var coveringDefender = match.HomeTeam.Players[2];

        // Carrier at 25m from goal (defending X=0)
        carrier.Position2D = new Vector2(25.0f, 34.0f);
        tackler.Position2D = new Vector2(26.0f, 34.0f);

        // Position covering defender between carrier and goal (X=15m < 25m)
        coveringDefender.Position2D = new Vector2(15.0f, 34.0f);

        // Put all other defenders behind carrier (X > 25m)
        foreach (var def in match.HomeTeam.Players.Where(p => p != tackler && p != coveringDefender && p.Role != PlayerRole.Goalkeeper))
        {
            def.Position2D = new Vector2(60.0f, def.Position2D.Y);
        }

        bool isDogso = match.CheckDogso(tackler, carrier);
        Assert(!isDogso, "Covering defender at 15m between carrier (25m) and goal (0m) must invalidate DOGSO");

        match.HandleFoulSanction(tackler, carrier, isSlideTackle: false, isFromBehind: false);
        Assert(!tackler.IsSentOff, "Tackle with covering defender present must NOT issue direct red card");
        Assert(tackler.YellowCards == 1, "Tackle with covering defender present must issue yellow card");
    }

    public static void TestLaw12_DOGSO_SecondHalf_InvertedGoalDetection()
    {
        var match = CreateMatch();
        match.TriggerHalfTime();
        match.ResumeMatch();

        Assert(!match.UserAttackingRight, "In 2nd half, Home team defends Right goal (X=105)");

        var tackler = match.HomeTeam.Players[3];
        var carrier = match.AwayTeam.Players[9];

        // Carrier at X=80m, attacking Right goal at X=105m (distance = 25m <= 45m)
        carrier.Position2D = new Vector2(80.0f, 34.0f);
        tackler.Position2D = new Vector2(79.0f, 34.0f);

        // Position all other defenders behind carrier (X < 80m, away from defending goal X=105)
        foreach (var def in match.HomeTeam.Players.Where(p => p != tackler && p.Role != PlayerRole.Goalkeeper))
        {
            def.Position2D = new Vector2(40.0f, def.Position2D.Y);
        }

        // Case A: Last defender -> DOGSO!
        Assert(match.CheckDogso(tackler, carrier), "In 2nd half, last defender defending Right goal (X=105) must be DOGSO");

        // Case B: Place covering defender between carrier (80m) and Right goal (105m) -> e.g. at X=95m
        var coveringDef = match.HomeTeam.Players[2];
        coveringDef.Position2D = new Vector2(95.0f, 34.0f);
        Assert(!match.CheckDogso(tackler, carrier), "Covering defender at 95m (between 80m and 105m) must invalidate DOGSO in 2nd half");
    }

    public static void TestLaw12_SevereTackleFromBehind_SlideVsStand()
    {
        var match = CreateMatch();
        var tackler = match.HomeTeam.Players[4];
        var carrier = match.AwayTeam.Players[9];

        // Place covering defender so DOGSO is false
        match.HomeTeam.Players[2].Position2D = new Vector2(10.0f, 34.0f);
        carrier.Position2D = new Vector2(30.0f, 34.0f);
        tackler.Position2D = new Vector2(31.0f, 34.0f);

        // 1. Slide tackle from behind -> Direct Red!
        match.HandleFoulSanction(tackler, carrier, isSlideTackle: true, isFromBehind: true);
        Assert(tackler.IsSentOff, "Slide tackle from behind must result in direct red card dismissal");
        Assert(tackler.Position2D.X < 0, "Sent-off player moved off pitch");

        // 2. Standing tackle from behind -> Yellow card!
        var match2 = CreateMatch();
        var tackler2 = match2.HomeTeam.Players[4];
        var carrier2 = match2.AwayTeam.Players[9];
        match2.HomeTeam.Players[2].Position2D = new Vector2(10.0f, 34.0f);
        carrier2.Position2D = new Vector2(30.0f, 34.0f);

        match2.HandleFoulSanction(tackler2, carrier2, isSlideTackle: false, isFromBehind: true);
        Assert(!tackler2.IsSentOff, "Standing tackle from behind should NOT be direct red");
        Assert(tackler2.YellowCards == 1, "Standing tackle from behind should be yellow card");

        // 3. Slide tackle from front/side -> Yellow card!
        var match3 = CreateMatch();
        var tackler3 = match3.HomeTeam.Players[4];
        var carrier3 = match3.AwayTeam.Players[9];
        match3.HomeTeam.Players[2].Position2D = new Vector2(10.0f, 34.0f);
        carrier3.Position2D = new Vector2(30.0f, 34.0f);

        match3.HandleFoulSanction(tackler3, carrier3, isSlideTackle: true, isFromBehind: false);
        Assert(!tackler3.IsSentOff, "Slide tackle from front should NOT be direct red when not DOGSO");
        Assert(tackler3.YellowCards == 1, "Slide tackle from front should be yellow card");
    }

    public static void TestLaw12_TenManTeamPlay_FormationAdaptationAndSimulationStability()
    {
        var match = CreateMatch();
        var dismissedPlayer = match.HomeTeam.Players[3]; // Outfield CB

        // Dismiss player
        match.IssueCard(dismissedPlayer, isDirectRed: true, reason: "طرد مباشر");
        Assert(dismissedPlayer.IsSentOff, "Player must be sent off");
        Assert(dismissedPlayer.Position2D == new Vector2(-100f, -100f), "Player must be parked at (-100, -100)");

        // Test applying ALL 8 formations to the 10-man squad
        var allFormations = FormationManager.GetAllFormations();
        foreach (var formation in allFormations)
        {
            FormationManager.ApplyFormation(match.HomeTeam, formation);

            int onPitchCount = match.HomeTeam.Players.Count(p => p.Position2D.X >= 0 && p.Position2D.Y >= 0);
            Assert(onPitchCount == 10, $"Formation {formation} must place exactly 10 players on pitch, got {onPitchCount}");
            Assert(dismissedPlayer.Position2D == new Vector2(-100f, -100f), "Dismissed player must remain at (-100, -100)");
        }

        // Run simulation for 100 ticks (2.0s at 50Hz)
        for (int frame = 0; frame < 100; frame++)
        {
            match.Update(0.02f);
            Assert(dismissedPlayer.Position2D == new Vector2(-100f, -100f), "Dismissed player must never move during match update");
            Assert(dismissedPlayer.Velocity == Vector2.Zero, "Dismissed player velocity must remain zero");
            Assert(!dismissedPlayer.HasBall, "Dismissed player must never possess ball");
        }

        // Dismiss a 2nd player -> 9-man squad
        var secondDismissed = match.HomeTeam.Players[6];
        match.IssueCard(secondDismissed, isDirectRed: true, reason: "طرد ثان");
        FormationManager.ApplyFormation(match.HomeTeam, FormationType.Formation442_Flat);

        int nineManCount = match.HomeTeam.Players.Count(p => p.Position2D.X >= 0 && p.Position2D.Y >= 0);
        Assert(nineManCount == 9, $"Team with 2 dismissals must have exactly 9 players on pitch, got {nineManCount}");

        for (int frame = 0; frame < 50; frame++)
        {
            match.Update(0.02f);
        }
    }

    public static void TestInjuries_MinorKnock_DynamicMultiplierAndStamina_SevereForcedSub()
    {
        var match = CreateMatch();
        var player = match.HomeTeam.Players[9];

        int originalSpeed = player.Speed;
        int originalStamina = player.Stamina;

        // Minor Knock
        match.ApplyInjury(player, PlayerInjuryStatus.MinorKnock);
        Assert(player.InjuryStatus == PlayerInjuryStatus.MinorKnock, "Status must be MinorKnock");
        Assert(Math.Abs(player.InjuryMultiplier - 0.80f) < 0.001f, "InjuryMultiplier must be 0.80");

        int expectedSpeed = (int)(player.BaseSpeed * player.PositionEfficiencyMultiplier * player.ConditionMultiplier * 0.80f);
        Assert(player.Speed == expectedSpeed, $"Speed must be {expectedSpeed}, got {player.Speed}");
        Assert(player.Stamina == 80, $"Stamina must be 80, got {player.Stamina}");

        // Severe Injury with available subs
        var match2 = CreateMatch();
        var severelyInjured = match2.HomeTeam.Players[9];
        var incomingSub = match2.HomeTeam.Bench[0];

        match2.ApplyInjury(severelyInjured, PlayerInjuryStatus.SevereInjury);
        Assert(match2.HomeTeam.SubstitutionsUsed == 1, "Severe injury should execute forced substitution");
        Assert(match2.HomeTeam.Players.Contains(incomingSub), "Incoming sub should be on pitch");
        Assert(!match2.HomeTeam.Players.Contains(severelyInjured), "Injured player should be off pitch");

        // Severe Injury with exhausted subs
        var match3 = CreateMatch();
        var team3 = match3.HomeTeam;
        team3.SubstitutionsUsed = 5;
        team3.StoppageWindowsUsed = 3;

        var striker = team3.Players[9];
        match3.ApplyInjury(striker, PlayerInjuryStatus.SevereInjury);
        Assert(striker.IsOffPitch, "Player must be marked IsOffPitch when subs exhausted");
        Assert(!striker.IsActiveOnPitch, "Player must not be active on pitch");
        Assert(striker.Position2D.X < 0, "Player moved off pitch");
        int activeCount = team3.Players.Count(p => p.IsActiveOnPitch);
        Assert(activeCount == 10, $"Team must complete match with 10 players, got {activeCount}");
    }

    public static void TestAimingAngles_FineDirectionalControl_ClampingBounds()
    {
        var match = CreateMatch();
        var user = match.ActiveUserPlayer;
        match.Ball.CatchOrTackle(user);

        // Continuously adjust Up (negative delta Y)
        for (int i = 0; i < 10; i++)
        {
            match.AdjustAimingAngle(-0.35f);
        }
        Assert(user.FacingDirection.Y < -0.80f && user.FacingDirection.Y >= -0.90f, 
            $"Upward aiming angle must be clamped near -0.90f, got {user.FacingDirection.Y}");

        // Continuously adjust Down (positive delta Y)
        for (int i = 0; i < 10; i++)
        {
            match.AdjustAimingAngle(+0.35f);
        }
        Assert(user.FacingDirection.Y > 0.80f && user.FacingDirection.Y <= 0.90f, 
            $"Downward aiming angle must be clamped near +0.90f, got {user.FacingDirection.Y}");

        // Verify facing direction is normalized and facing towards opponent goal (X > 0)
        Assert(user.FacingDirection.X > 0.40f, "FacingDirection.X must remain positive (towards opponent goal)");
        float len = user.FacingDirection.Length();
        Assert(Math.Abs(len - 1.0f) < 0.01f, $"FacingDirection must be normalized (length={len})");
    }

    public static void TestLaw3_Law12_SentOffPlayer_CannotBeSubstituted()
    {
        var match = CreateMatch();
        var team = match.HomeTeam;
        var defender = team.Players[2];

        // Red Card dismissal
        match.IssueCard(defender, isDirectRed: true, reason: "طرد مباشر");
        Assert(defender.IsSentOff, "Defender is sent off");
        Assert(!defender.IsActiveOnPitch, "Defender is not active on pitch");

        match.PauseMatch();

        // Adversarial attempt: Can a coach substitute a sent-off player to return to 11 men?
        // Under IFAB Law 3 / Law 12, substituting a sent-off player is strictly prohibited!
        bool subAttempt = match.MakeSubstitution(team, defender, team.Bench[0]);
        
        // Check whether the engine allows or blocks this:
        if (subAttempt)
        {
            Console.WriteLine("\n[VULNERABILITY DETECTED]: Engine allowed substituting a sent-off player back onto pitch!");
        }
        Assert(!subAttempt, "IFAB Law 3 & Law 12: A player who has been sent off CANNOT be replaced or substituted!");
    }

    public static void TestLaw12_TenManPlay_DeadBallRestarts_NeverAssignDismissedPlayer()
    {
        var match = CreateMatch();
        var team = match.HomeTeam;
        var striker = team.Players[9];

        // Red Card dismissal on striker
        match.IssueCard(striker, isDirectRed: true, reason: "طرد مباشر");
        Assert(striker.IsSentOff, "Striker is sent off");
        Assert(striker.Position2D.X < 0, "Striker moved off pitch");

        // 1. Kickoff after goal celebration
        match.ResumePlayAfterGoalCelebration(team);
        if (striker.Position2D.X >= 0)
        {
            Console.WriteLine($"\n[VULNERABILITY DETECTED]: Kickoff placed sent-off player at {striker.Position2D}!");
        }
        Assert(striker.Position2D.X < 0, $"ResumePlayAfterGoalCelebration must NOT place sent-off striker on center circle (got X={striker.Position2D.X})");
        Assert(match.ActiveUserPlayer != striker, "ActiveUserPlayer must NOT be the sent-off striker on kickoff");

        // 2. Half-Time trigger
        match.TriggerHalfTime();
        if (match.ActiveUserPlayer == striker || striker.Position2D.X >= 0)
        {
            Console.WriteLine($"\n[VULNERABILITY DETECTED]: Half-time gave control of sent-off player at {striker.Position2D}!");
        }
        Assert(match.ActiveUserPlayer != striker, "TriggerHalfTime must NOT reset ActiveUserPlayer to sent-off striker");
        Assert(striker.Position2D.X < 0, $"TriggerHalfTime must NOT invert sent-off player back onto pitch (got X={striker.Position2D.X})");

        // 3. Corner Kick
        // Away player kicks ball out of bounds over home goal line
        match.Ball.Kick(new Vector2(1.0f, 0.0f), 10.0f, 0.0f, kicker: match.AwayTeam.Players[9]);
        match.Ball.Position = new Vector3(106.0f, 10.0f, 0.0f);
        match.Update(0.02f);
        if (striker.Position2D.X >= 0 || match.ActiveUserPlayer == striker)
        {
            Console.WriteLine($"\n[VULNERABILITY DETECTED]: Corner kick revived sent-off striker at {striker.Position2D}!");
        }
        Assert(striker.Position2D.X < 0, $"Corner kick must NOT place sent-off striker on corner arc (got X={striker.Position2D.X})");
        Assert(match.ActiveUserPlayer != striker, "ActiveUserPlayer for corner kick must NOT be the sent-off striker");
    }

    public static void TestLaw12_TenManPlay_PassTargeting_FallbackIgnoresDismissedPlayer()
    {
        var match = CreateMatch();
        var team = match.HomeTeam;
        var dismissedPlayer = team.Players[1]; // Left Back

        match.IssueCard(dismissedPlayer, isDirectRed: true, reason: "طرد مباشر");
        Assert(dismissedPlayer.IsSentOff, "Player is sent off");
        Assert(dismissedPlayer.Position2D.X < 0, "Player moved off pitch");

        match.Ball.CatchOrTackle(match.ActiveUserPlayer);

        // Execute a pass with aimDir pointing backward/out of range, triggering fallback selection
        match.ExecutePass(new Vector2(-1.0f, 0.0f));

        Assert(match.ActivePassTransit != null, "Pass transit should be initiated");
        if (match.ActivePassTransit != null && match.ActivePassTransit.IntendedReceiver == dismissedPlayer)
        {
            Console.WriteLine("\n[VULNERABILITY DETECTED]: Pass targeting fallback selected sent-off player as receiver!");
        }
        Assert(match.ActivePassTransit != null && match.ActivePassTransit.IntendedReceiver != dismissedPlayer, 
            "Fallback pass targeting must strictly exclude sent-off player!");
        Assert(!match.ActivePassTransit.IntendedReceiver.IsSentOff, 
            "IntendedReceiver cannot be a sent-off player!");
        Assert(!match.ActivePassTransit.IntendedReceiver.IsOffPitch, 
            "IntendedReceiver cannot be an off-pitch player!");
    }

    public static void TestLaw12_CarrierDismissal_ReleasesBall_PreventsTeleportation()
    {
        var match = CreateMatch();
        var carrier = match.ActiveUserPlayer;
        match.Ball.CatchOrTackle(carrier);
        Assert(match.Ball.Owner == carrier, "Carrier holds ball");

        // Issue direct red card to carrier
        match.IssueCard(carrier, isDirectRed: true, reason: "طرد مباشر");

        if (match.Ball.Owner == carrier)
        {
            Console.WriteLine("\n[VULNERABILITY DETECTED]: Carrier retained Ball.Owner after red card dismissal!");
        }
        Assert(match.Ball.Owner != carrier, "Dismissed player must immediately release ball ownership!");
        Assert(match.Ball.Position.X >= 0 && match.Ball.Position.Y >= 0, 
            $"Ball must not be teleported to dismissed player's off-pitch position (got {match.Ball.Position})");

        // Advance physics by 1 tick
        match.Ball.Update(0.02f);
        if (match.Ball.Position.X < 0)
        {
            Console.WriteLine($"\n[VULNERABILITY DETECTED]: Ball teleported off-pitch to {match.Ball.Position}!");
        }
        Assert(match.Ball.Position.X >= 0 && match.Ball.Position.Y >= 0, 
            $"Ball must remain on pitch after physics update (got {match.Ball.Position})");
    }

    public static void TestPossessionGracePeriod_BlocksInstantAITackle_AndRateLimitsAI()
    {
        var match = CreateMatch();
        var user = match.ActiveUserPlayer;
        match.Ball.CatchOrTackle(user);
        match.GrantPossessionGrace(1.4f);

        Assert(match.PossessionGraceRemaining >= 1.35f, 
            $"Possession grace must be active, remaining: {match.PossessionGraceRemaining}");

        // Move opposing defender right in user's face (0.4m away)
        var opponent = match.AwayTeam.Players.First(p => p.Role != PlayerRole.Goalkeeper);
        opponent.Position2D = user.Position2D + new Vector2(0.4f, 0.0f);

        // Run 40 ticks (800ms) - with 38% check per tick, unshielded ball would be stolen almost 100% of the time
        for (int i = 0; i < 40; i++)
        {
            match.Update(0.02f);
            Assert(match.Ball.Owner == user, $"Ball must NOT be stolen while PossessionGraceRemaining > 0 (Tick {i})");
        }

        Assert(match.PossessionGraceRemaining > 0.0f, "Grace timer should still be positive after 800ms");
    }
}
