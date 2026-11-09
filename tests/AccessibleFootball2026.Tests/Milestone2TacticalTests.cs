using System;
using System.Linq;
using System.Numerics;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Input;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;
using AccessibleFootball2026.Input;

namespace AccessibleFootball2026.Tests;

public static class Milestone2TacticalTests
{
    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception($"Assertion Failed: {message}");
    }

    private static MatchEngine CreateTestMatch()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();
        return new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
    }

    public static void RunAll(Action<string, Action> runTest)
    {
        Console.WriteLine("\n--- MILESTONE 2: IFAB TACTICAL REALISM TEST SUITE ---");

        runTest("12A. Law 3 Substitutions: 5 subs across 3 stoppage windows in 90m; block 4th window and 6th sub",
            TestLaw3_Substitutions_5SubsAcross3Windows_BlocksExhausted);

        runTest("12B. Law 3 Multiple Substitutions: Multiple subs in same stoppage count as 1 window",
            TestLaw3_MultipleSubsInSameStoppage_CountsAsSingleWindow);

        runTest("12C. Law 3 Half-Time & Tactical Changes: Half-time subs free of window; tactics/swaps free",
            TestLaw3_HalfTimeSubsAndTacticalChanges_DoNotConsumeWindow);

        runTest("12D. Law 3 Extra Time: +1 substitution (6th sub) and +1 stoppage window (4th window)",
            TestLaw3_ExtraTime_Grants6thSubAnd4thWindow);

        runTest("12E. Law 12 Disciplinary Sanctions: Yellow card caution; 2nd yellow -> Red card dismissal",
            TestLaw12_YellowCard_SecondYellowTriggersRedCardDismissal);

        runTest("12F. Law 12 Direct Red: DOGSO last defender & severe tackle from behind; 10-man team play",
            TestLaw12_DirectRed_DogsoAndFromBehind_10ManTeamPlay);

        runTest("12G. Player Injuries: Minor knock applies 20% speed/stamina penalty; severe injury forced sub or 10-man",
            TestInjuries_MinorKnockPenalty_SevereForcedSubOr10Man);

        runTest("12H. Aiming Angles: Up/Down arrow modifies player aiming vector before shot (W) or cross (D)",
            TestAimingAngles_UpDownArrow_ModifiesFacingDirectionAndTargeting);
    }

    public static void TestLaw3_Substitutions_5SubsAcross3Windows_BlocksExhausted()
    {
        var match = CreateTestMatch();
        var team = match.HomeTeam;

        Assert(team.SubstitutionsUsed == 0, "Initial subs used must be 0");
        Assert(team.StoppageWindowsUsed == 0, "Initial stoppage windows must be 0");
        Assert(team.MaxSubstitutions == 5, "Max subs in 90m must be 5");
        Assert(team.MaxStoppageWindows == 3, "Max stoppage windows must be 3");
        Assert(match.CanMakeSubstitution(team), "Should be able to make sub initially");

        // Stoppage Window 1: 1 sub
        match.PauseMatch();
        bool sub1 = match.MakeSubstitution(team, team.Players[10], team.Bench[0]);
        Assert(sub1, "Sub 1 should succeed");
        Assert(team.SubstitutionsUsed == 1, "Subs used should be 1");
        Assert(team.StoppageWindowsUsed == 1, "Windows used should be 1");
        match.ResumeMatch();

        // Stoppage Window 2: 1 sub
        match.PauseMatch();
        bool sub2 = match.MakeSubstitution(team, team.Players[9], team.Bench[0]);
        Assert(sub2, "Sub 2 should succeed");
        Assert(team.SubstitutionsUsed == 2, "Subs used should be 2");
        Assert(team.StoppageWindowsUsed == 2, "Windows used should be 2");
        match.ResumeMatch();

        // Stoppage Window 3: 1 sub
        match.PauseMatch();
        bool sub3 = match.MakeSubstitution(team, team.Players[8], team.Bench[0]);
        Assert(sub3, "Sub 3 should succeed");
        Assert(team.SubstitutionsUsed == 3, "Subs used should be 3");
        Assert(team.StoppageWindowsUsed == 3, "Windows used should be 3");
        match.ResumeMatch();

        // Stoppage Window 4 (Not allowed in regular time - max 3 windows exhausted!)
        match.PauseMatch();
        Assert(!match.CanMakeSubstitution(team), "CanMakeSubstitution must be false after 3 windows exhausted");
        bool sub4 = match.MakeSubstitution(team, team.Players[7], team.Bench[0]);
        Assert(!sub4, "4th window sub must be blocked in regular time");
        Assert(team.SubstitutionsUsed == 3, "Subs used must remain 3");
        Assert(team.StoppageWindowsUsed == 3, "Windows used must remain 3");
        match.ResumeMatch();

        // Test 5-sub cap: Reset team and use all 5 subs across 3 windows
        var match2 = CreateTestMatch();
        var team2 = match2.HomeTeam;
        match2.PauseMatch();
        match2.MakeSubstitution(team2, team2.Players[10], team2.Bench[0]); // Window 1, sub 1
        match2.MakeSubstitution(team2, team2.Players[9], team2.Bench[0]);  // Window 1, sub 2
        match2.ResumeMatch();

        match2.PauseMatch();
        match2.MakeSubstitution(team2, team2.Players[8], team2.Bench[0]);  // Window 2, sub 3
        match2.MakeSubstitution(team2, team2.Players[7], team2.Bench[0]);  // Window 2, sub 4
        match2.ResumeMatch();

        match2.PauseMatch();
        match2.MakeSubstitution(team2, team2.Players[6], team2.Bench[0]);  // Window 3, sub 5
        Assert(team2.SubstitutionsUsed == 5, "5 subs should be used");
        Assert(team2.StoppageWindowsUsed == 3, "3 windows should be used");

        // Attempt 6th sub in window 3: Subs exhausted!
        Assert(!match2.CanMakeSubstitution(team2), "CanMakeSubstitution must be false when 5 subs exhausted");
        bool sub6 = match2.MakeSubstitution(team2, team2.Players[5], team2.Bench[0]);
        Assert(!sub6, "6th sub must be blocked");
        Assert(team2.SubstitutionsUsed == 5, "Subs used must remain 5");
    }

    public static void TestLaw3_MultipleSubsInSameStoppage_CountsAsSingleWindow()
    {
        var match = CreateTestMatch();
        var team = match.HomeTeam;

        match.PauseMatch();
        Assert(team.StoppageWindowsUsed == 0, "Initial windows should be 0");
        Assert(!team.WindowConsumedThisStoppage, "WindowConsumedThisStoppage should be false before sub");

        // Make double substitution in the same stoppage
        bool sub1 = match.MakeSubstitution(team, team.Players[10], team.Bench[0]);
        Assert(sub1, "Sub 1 should succeed");
        Assert(team.SubstitutionsUsed == 1, "Subs used should be 1");
        Assert(team.StoppageWindowsUsed == 1, "Windows used should be 1");
        Assert(team.WindowConsumedThisStoppage, "WindowConsumedThisStoppage should be true after sub 1");

        bool sub2 = match.MakeSubstitution(team, team.Players[9], team.Bench[0]);
        Assert(sub2, "Sub 2 should succeed");
        Assert(team.SubstitutionsUsed == 2, "Subs used should be 2");
        Assert(team.StoppageWindowsUsed == 1, "Windows used must STILL be 1 (same stoppage opportunity)");

        bool sub3 = match.MakeSubstitution(team, team.Players[8], team.Bench[0]);
        Assert(sub3, "Sub 3 should succeed");
        Assert(team.SubstitutionsUsed == 3, "Subs used should be 3");
        Assert(team.StoppageWindowsUsed == 1, "Windows used must STILL be 1 for triple substitution");

        match.ResumeMatch();
        Assert(!team.WindowConsumedThisStoppage, "Resuming match must reset WindowConsumedThisStoppage");
    }

    public static void TestLaw3_HalfTimeSubsAndTacticalChanges_DoNotConsumeWindow()
    {
        var match = CreateTestMatch();
        var team = match.HomeTeam;

        match.TriggerHalfTime();
        Assert(match.State == MatchState.HalfTime, "State should be HalfTime");

        // Half-time substitution 1
        bool sub1 = match.MakeSubstitution(team, team.Players[10], team.Bench[0]);
        Assert(sub1, "Half-time sub 1 should succeed");
        Assert(team.SubstitutionsUsed == 1, "Subs used should be 1");
        Assert(team.StoppageWindowsUsed == 0, "Half-time sub must NOT consume a stoppage window (0 used)");

        // Half-time substitution 2
        bool sub2 = match.MakeSubstitution(team, team.Players[9], team.Bench[0]);
        Assert(sub2, "Half-time sub 2 should succeed");
        Assert(team.SubstitutionsUsed == 2, "Subs used should be 2");
        Assert(team.StoppageWindowsUsed == 0, "Half-time sub 2 must NOT consume a stoppage window (0 used)");

        // Tactical changes: swap positions of 2 outfield players on pitch
        var p1 = team.Players[1];
        var p2 = team.Players[2];
        bool swapped = team.SwapPositions(1, 2);
        Assert(swapped, "SwapPositions should succeed");
        Assert(team.Players[1] == p2 && team.Players[2] == p1, "Players positions should be swapped");
        Assert(team.SubstitutionsUsed == 2, "SwapPositions must NOT increase SubstitutionsUsed");
        Assert(team.StoppageWindowsUsed == 0, "SwapPositions must NOT consume stoppage window");

        // Tactical changes: change formation
        team.ChangeFormation(FormationType.Formation442_Flat);
        Assert(team.CurrentFormation == FormationType.Formation442_Flat, "Formation should be 4-4-2");
        Assert(team.SubstitutionsUsed == 2, "ChangeFormation must NOT increase SubstitutionsUsed");
        Assert(team.StoppageWindowsUsed == 0, "ChangeFormation must NOT consume stoppage window");
    }

    public static void TestLaw3_ExtraTime_Grants6thSubAnd4thWindow()
    {
        var match = CreateTestMatch();
        var team = match.HomeTeam;

        // Exhaust regular 5 subs across 3 windows
        match.PauseMatch();
        match.MakeSubstitution(team, team.Players[10], team.Bench[0]);
        match.MakeSubstitution(team, team.Players[9], team.Bench[0]);
        match.ResumeMatch();

        match.PauseMatch();
        match.MakeSubstitution(team, team.Players[8], team.Bench[0]);
        match.MakeSubstitution(team, team.Players[7], team.Bench[0]);
        match.ResumeMatch();

        match.PauseMatch();
        match.MakeSubstitution(team, team.Players[6], team.Bench[0]);
        match.ResumeMatch();

        Assert(team.SubstitutionsUsed == 5, "5 subs used");
        Assert(team.StoppageWindowsUsed == 3, "3 windows used");
        Assert(!match.CanMakeSubstitution(team), "No subs or windows remaining in regular time");

        // Trigger Extra Time
        match.TriggerExtraTime();
        Assert(match.IsExtraTime, "MatchEngine must be in Extra Time");
        Assert(team.IsExtraTime, "HomeTeam must be in Extra Time");
        Assert(team.MaxSubstitutions == 6, "Extra time grants 6th sub");
        Assert(team.MaxStoppageWindows == 4, "Extra time grants 4th stoppage window");
        Assert(match.CanMakeSubstitution(team), "Extra time must allow 6th sub in 4th window");

        // Make 6th sub in 4th window
        match.PauseMatch();
        bool sub6 = match.MakeSubstitution(team, team.Players[5], team.Bench[0]);
        Assert(sub6, "6th substitution in extra time should succeed");
        Assert(team.SubstitutionsUsed == 6, "Total subs used should be 6");
        Assert(team.StoppageWindowsUsed == 4, "Total windows used should be 4");

        // Attempt 7th sub
        Assert(!match.CanMakeSubstitution(team), "Cannot make 7th sub");
        bool sub7 = match.MakeSubstitution(team, team.Players[4], team.Bench[0]);
        Assert(!sub7, "7th sub must be blocked");
    }

    public static void TestLaw12_YellowCard_SecondYellowTriggersRedCardDismissal()
    {
        var match = CreateTestMatch();
        var defender = match.HomeTeam.Players[2]; // CB

        Assert(defender.YellowCards == 0, "Initial yellow cards must be 0");
        Assert(!defender.IsSentOff, "Initial sent-off must be false");

        // Issue first Yellow Card
        match.IssueCard(defender, isDirectRed: false, reason: "تدخل متهور");
        Assert(defender.YellowCards == 1, "Yellow cards should be 1");
        Assert(!defender.IsSentOff, "Player should NOT be sent off after 1st yellow");
        Assert(defender.IsActiveOnPitch, "Player should remain active on pitch");

        // Issue second Yellow Card -> triggers Red Card dismissal
        match.IssueCard(defender, isDirectRed: false, reason: "عرقلة تكتيكية");
        Assert(defender.YellowCards == 2, "Yellow cards should be 2");
        Assert(defender.IsSentOff, "Player MUST be sent off after 2nd yellow card");
        Assert(!defender.IsActiveOnPitch, "Player must NOT be active on pitch");
        Assert(defender.Position2D.X < 0, "Sent-off player position must be outside pitch");
    }

    public static void TestLaw12_DirectRed_DogsoAndFromBehind_10ManTeamPlay()
    {
        var match = CreateTestMatch();
        var tackler = match.HomeTeam.Players[3];
        var carrier = match.AwayTeam.Players[9];

        // Setup carrier through on goal at X=30 (defending goal at X=0)
        carrier.Position2D = new Vector2(30.0f, 34.0f);
        tackler.Position2D = new Vector2(31.0f, 34.0f);

        // Position all other defenders behind carrier (X > 30), so tackler is the LAST DEFENDER
        foreach (var def in match.HomeTeam.Players.Where(p => p != tackler && p.Role != PlayerRole.Goalkeeper))
        {
            def.Position2D = new Vector2(50.0f, def.Position2D.Y);
        }

        bool isDogso = match.CheckDogso(tackler, carrier);
        Assert(isDogso, "Foul by last defender with carrier on goal must be DOGSO");

        // Direct Red Card for DOGSO
        match.HandleFoulSanction(tackler, carrier, isSlideTackle: false, isFromBehind: false);
        Assert(tackler.IsSentOff, "DOGSO must issue a direct red card");
        Assert(!tackler.IsActiveOnPitch, "Sent-off tackler must not be active on pitch");
        Assert(tackler.Position2D.X < 0, "Sent-off tackler must be moved off pitch");

        // Team continues with 10 players: ApplyFormation formats without aborting
        FormationManager.ApplyFormation(match.HomeTeam, FormationType.Formation433_Attack);
        int onPitchPlayers = match.HomeTeam.Players.Count(p => p.Position2D.X >= 0 && p.Position2D.Y >= 0);
        Assert(onPitchPlayers == 10, $"Team should have exactly 10 on-pitch players, got {onPitchPlayers}");

        // Direct Red Card for severe tackle from behind
        var match2 = CreateTestMatch();
        var tackler2 = match2.AwayTeam.Players[2];
        var carrier2 = match2.HomeTeam.Players[9];
        match2.HandleFoulSanction(tackler2, carrier2, isSlideTackle: true, isFromBehind: true);
        Assert(tackler2.IsSentOff, "Severe slide tackle from behind must issue direct red card");
    }

    public static void TestInjuries_MinorKnockPenalty_SevereForcedSubOr10Man()
    {
        var match = CreateTestMatch();
        var player = match.HomeTeam.Players[9]; // Striker
        int normalSpeed = player.Speed;
        int normalStamina = player.Stamina;

        Assert(player.InjuryStatus == PlayerInjuryStatus.None, "Initial injury status should be None");

        // 1. Minor Knock (-20% speed and stamina penalty)
        match.ApplyInjury(player, PlayerInjuryStatus.MinorKnock);
        Assert(player.InjuryStatus == PlayerInjuryStatus.MinorKnock, "Status should be MinorKnock");
        Assert(Math.Abs(player.InjuryMultiplier - 0.80f) < 0.001f, "Injury multiplier must be 0.80");

        int expectedSpeed = (int)(player.BaseSpeed * player.PositionEfficiencyMultiplier * player.ConditionMultiplier * 0.80f);
        Assert(player.Speed == expectedSpeed, $"Speed under minor knock must be {expectedSpeed}, got {player.Speed}");
        Assert(player.Stamina == 80, $"Stamina under minor knock must be 80, got {player.Stamina}");
        Assert(player.Speed < normalSpeed, "Injured speed must be less than normal speed");
        Assert(player.Stamina < normalStamina, "Injured stamina must be less than normal stamina");

        // 2. Severe Injury with subs available -> Forced substitution executed
        var match2 = CreateTestMatch();
        var injuredPlayer = match2.HomeTeam.Players[8];
        var firstBench = match2.HomeTeam.Bench[0];

        match2.ApplyInjury(injuredPlayer, PlayerInjuryStatus.SevereInjury);
        Assert(match2.HomeTeam.SubstitutionsUsed == 1, "Severe injury should trigger forced substitution");
        Assert(match2.HomeTeam.Players.Contains(firstBench), "Bench player should have entered pitch");

        // 3. Severe Injury with subs exhausted -> Player leaves pitch, 10-man play
        var match3 = CreateTestMatch();
        var team3 = match3.HomeTeam;
        // Exhaust all 5 subs
        team3.SubstitutionsUsed = 5;
        team3.StoppageWindowsUsed = 3;

        var striker = team3.Players[9];
        match3.ApplyInjury(striker, PlayerInjuryStatus.SevereInjury);
        Assert(striker.IsOffPitch, "Injured player must leave pitch when subs are exhausted");
        Assert(!striker.IsActiveOnPitch, "Injured player must not be active on pitch");
        Assert(striker.Position2D.X < 0, "Injured player moved off pitch");

        int activeCount = team3.Players.Count(p => p.IsActiveOnPitch);
        Assert(activeCount == 10, $"Team must complete match with 10 active players, got {activeCount}");
    }

    public static void TestAimingAngles_UpDownArrow_ModifiesFacingDirectionAndTargeting()
    {
        var match = CreateTestMatch();
        var user = match.ActiveUserPlayer;
        match.Ball.CatchOrTackle(user);

        // Test Up arrow aiming
        match.AdjustAimingAngle(-0.40f);
        Assert(user.FacingDirection.Y < -0.20f, $"FacingDirection.Y should be negative (upward aim), got {user.FacingDirection.Y}");
        Assert(user.FacingDirection.X > 0.5f, "FacingDirection.X should remain oriented towards opponent goal");

        // Test Down arrow aiming
        match.AdjustAimingAngle(0.80f); // moves Y positive
        Assert(user.FacingDirection.Y > 0.20f, $"FacingDirection.Y should be positive (downward aim), got {user.FacingDirection.Y}");
        Assert(user.FacingDirection.X > 0.5f, "FacingDirection.X should remain oriented towards opponent goal");
    }
}
