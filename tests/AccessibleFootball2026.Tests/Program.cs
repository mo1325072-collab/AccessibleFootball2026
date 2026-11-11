using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using AccessibleFootball2026.Audio;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Input;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;
using AccessibleFootball2026.Input;

namespace AccessibleFootball2026.Tests;

public static class Program
{
    private static int _passed = 0;
    private static int _failed = 0;

    public static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("===============================================================");
        Console.WriteLine("   ACCESSIBLE FOOTBALL 2026 - ADVERSARIAL INPUT TEST SUITE     ");
        Console.WriteLine("===============================================================");

        RunTest("1A. Hold 'S' (Pass) across 100 frames with possession loss -> Never Tackles", TestHoldS_PassThenLosePossession_NeverTackles);
        RunTest("1B. Hold 'W' (Shoot) across 100 frames with possession loss -> Never Tackles", TestHoldW_ShootThenLosePossession_NeverTackles);
        RunTest("1C. Hold 'A' (ThroughBall) across 50 frames with possession loss -> Never Tackles", TestHoldA_ThroughBallThenLosePossession_NeverTackles);
        RunTest("1D. Hold 'D' (Cross) across 50 frames with possession loss -> Never Tackles", TestHoldD_CrossThenLosePossession_NeverTackles);

        RunTest("2A. Hold 'Ctrl' (Switch) across 100 frames -> Exactly 1 switch, 0 at 50Hz", TestHoldCtrl_SingleSwitch_NoCyclingAt50Hz);
        RunTest("2B. Cross-Variant Control keys (VK_CONTROL, VK_LCONTROL, VK_RCONTROL) -> Single switch", TestCtrlKeyVariants_Suppression);

        RunTest("3A. Continuous Arrow keys + Shift across 50 frames -> Persistent vector", TestContinuousArrowAndShift_MovementVector);
        RunTest("3B. Continuous Arrow release sequence -> Correct vector updates", TestArrowReleaseSequence);
        RunTest("3C. Opposing Arrow keys (Left + Right) -> Cancellation to zero", TestOpposingArrowKeys_Cancellation);

        RunTest("4A. MatchEngine Integration: Hold 'W' shot -> 0 tackles across 50 ticks", TestMatchEngine_HoldW_NoTackles);
        RunTest("4B. MatchEngine Integration: Hold 'Ctrl' switch -> Exactly 1 player switch", TestMatchEngine_HoldCtrl_SingleSwitch);
        RunTest("4C. MatchEngine Integration: Arrow + Shift movement distance verification", TestMatchEngine_ContinuousMovementDistance);
        RunTest("4D. MatchEngine Integration: Concurrent Arrow movement while holding Ctrl", TestMatchEngine_ConcurrentMovementAndSwitch);

        RunTest("5A. Form Focus & Key Handling static contract verification", TestFormKeyHandlingAndFocus_StaticContract);

        RunTest("6A. Ball Physics: Linear deceleration allows 18m/s pass to travel > 20m without 6m wall", TestBall_LinearDeceleration_Overcomes6MeterWall);
        RunTest("6B. MatchEngine: ExecutePass initializes PassTransitInfo and tracks in-transit state", TestMatchEngine_ExecutePass_SetsPassTransitInfo);
        RunTest("6C. MatchEngine: Intended receiver actively computes lead intercept and moves towards pass", TestMatchEngine_ThroughBall_ReceiverActiveInterceptMovement);
        RunTest("6D. MatchEngine: Seamless auto-catch within 2.2m threshold and instant control handover", TestMatchEngine_Reception_AutoCatchWithinThresholdAndControlTransfer);
        RunTest("6E. Ball Physics: Vertical arc flight, bounce restitution, and ground clamping Z >= 0", TestBall_VerticalFlight_BouncesAndClampsGround);

        AdversarialM2Challenge.RunAll(RunTest);

        // Milestone 3: Defensive Switching, Tackling & Auditory Feedback (F9, F10, F11, F12)
        RunTest("8A. F9 Player Switching: Excludes ActiveUserPlayer and selects closest remaining outfield defender", TestF9_PlayerSwitching_ExcludesCurrentActivePlayer);
        RunTest("8B. F9 Player Switching: Speaks new defender name and position via TacticalRadarInfo", TestF9_PlayerSwitching_SpeaksNewDefenderNameAndPosition);
        RunTest("8C. F9 Player Switching: Rapid Ctrl tapping debounced against 50Hz speech floods", TestF9_PlayerSwitching_DebouncePrevents50HzFloods);
        RunTest("8D. F10 Distinct Standing Tackle: 1.8m reach, 0.4s cooldown, stationary controlled", TestF10_DistinctStandingTackle_ReachAndCooldownAndStationary);
        RunTest("8E. F10 Distinct Slide Tackle: 2.8m reach, +4.0m/s forward impulse for 0.3s, 0.9s cooldown", TestF10_DistinctSlideTackle_ReachAndImpulseAndCooldown);
        RunTest("8F. F10 Slide Tackle from Behind: Elevated foul probability when tackling from behind", TestF10_SlideTackleFromBehind_HigherFoulProbability);
        RunTest("8G. F11 Distance Announcements: Exact numerical format 'بعيد عن حامل الكرة: {dist:F1} متر'", TestF11_TackleDistanceAnnouncements_OutOfReach);
        RunTest("8H. F11 Tackle Success: Ball recovery handoff and spoken recovery announcement", TestF11_TackleSuccessAnnouncement_BallRecovery);
        RunTest("8I. F12 Auditory Confirmation: ShotTaken explicitly triggers powerful shot sound (No Silent Shots)", TestF12_AudioConfirmation_ShotTakenWiredToShotSound);
        RunTest("8J. F12 Auditory Confirmation: Distinct sounds for pass, cross, shot, standing tackle, slide tackle, and whistle", TestF12_AudioConfirmation_DistinctKickAndTackleSounds);
        RunTest("8K. F12 Procedural Audio: High-fidelity WAV files exist with valid RIFF headers", TestF12_ProceduralSoundSynthesis_WavFilesValidOnDisk);
        RunTest("8L. Zero-Clutter Directive: In-match announcements & menu titles strictly free of 'اضغط' tutorial hints", TestZeroClutter_InMatchAnnouncementsAndMenus_FreeOfTutorialInstructions);

        AdversarialM3Challenge.RunAll(RunTest);

        // Milestone 1: Core Keybindings, Fair Tackle & Half-Time Mechanics
        RunTest("10A. M1 Controls: S=Pass, D=Cross, W=Shoot, A=ThroughBall, Ctrl=Switch, Shift=Sprint, Space=Radar", TestM1_ControlsAlignment);
        RunTest("10B. M1 Pause: Escape key immediately pauses match and transitions to pause menu", TestM1_EscapeKey_PausesMatchAndTransitionsToPauseMenu);
        RunTest("10C. M1 Fair Tackle: Closer to ball than opponent -> 100% clean tackle won", TestM1_FairTackle_CloserToBall_WinsCleanTackle);
        RunTest("10D. M1 Fair Tackle: Closer to opponent body -> Deterministic foul called against user", TestM1_FairTackle_CloserToOpponent_CallsFoul);
        RunTest("10E. M1 Fair Tackle: Out of reach (>2.8m slide, >1.8m stand) announces distance without tutorial hints", TestM1_FairTackle_OutOfReach_AnnouncesDistance);
        RunTest("10F. M1 Half-Time: Match reaches 45:00 -> Pauses, State=HalfTime, swaps sides, mirrors players", TestM1_HalfTime_PausesAt45AndSwapsSides);
        RunTest("10G. M1 Half-Time: 2nd half attacks Left -> Left arrow moves forward, W shoots towards Left goal (X=0)", TestM1_HalfTime_SecondHalf_AttackLeft_MovementAndShooting);
        RunTest("10H. M1 Half-Time: 2nd half zones inverted (X<=35 AttackingThird) and Left goal scores for HomeTeam", TestM1_HalfTime_SecondHalf_InvertedZones_LeftGoalScoring);

        AdversarialM1Challenge.RunAll(RunTest);

        Milestone2TacticalTests.RunAll(RunTest);

        AdversarialM2TacticalChallenge.RunAll(RunTest);

        Milestone3AudioOverhaulTests.RunAll(RunTest);

        // AdversarialM3EmpiricalChallenge.RunAll(RunTest);

        Milestone4MenuAcademySettingsTests.RunAll(RunTest);

        Milestone5AudioOverhaulTests.RunAll(RunTest);

        Milestone5DatabaseTests.RunAll(RunTest);

        Milestone6AiAdvisorTests.RunAllTests(RunTest);

        AdversarialTacticalSlidersChallenge.RunAll(RunTest);

        Milestone7RepairRegressionTests.RunAll(RunTest);

        Milestone8AudioIntegrationTests.RunAll(RunTest);
        Milestone9GameplayFairnessTests.RunAll(RunTest);
        Milestone10CommentaryRepairTests.RunAll(RunTest);

        if (args.Any(a => a.Equals("--challenge-m5", StringComparison.OrdinalIgnoreCase) || a.Equals("--challenger", StringComparison.OrdinalIgnoreCase)))
        {
            AdversarialM5AudioChallenge.RunAll(RunTest);
        }

        Console.WriteLine("\n===============================================================");
        Console.WriteLine($"RESULTS: {_passed} PASSED, {_failed} FAILED (TOTAL: {_passed + _failed})");
        Console.WriteLine("===============================================================");

        return _failed > 0 ? 1 : 0;
    }

    private static void RunTest(string testName, Action testAction)
    {
        Console.Write($"[TEST] {testName} ... ");
        try
        {
            testAction();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("PASS ✅");
            Console.ResetColor();
            _passed++;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"FAIL ❌: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine(ex.StackTrace);
            _failed++;
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception($"Assertion Failed: {message}");
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

    // ----------------------------------------------------------------------------------------
    // 1. Action keys hold & possession loss tests
    // ----------------------------------------------------------------------------------------
    private static void TestHoldS_PassThenLosePossession_NeverTackles()
    {
        var handler = new KeyboardInputHandler();
        const int VK_KEY_S = 0x53;

        // Frame 1: In possession, user presses 'S'
        handler.HandleKeyDown(VK_KEY_S, isPossession: true);
        var f1Actions = handler.PollCurrentActions(isPossession: true);
        Assert(f1Actions.Contains(GameAction.ShortPass), "Frame 1 must trigger ShortPass on press");
        Assert(!f1Actions.Contains(GameAction.StandTackle), "Frame 1 must NOT trigger StandTackle");

        // Pass kicked -> possession lost!
        // Frames 2..100: User holds 'S'. OS sends auto-repeats or ticks pass while held.
        for (int frame = 2; frame <= 100; frame++)
        {
            // Simulate Windows sending KeyDown auto-repeats with isPossession = false
            handler.HandleKeyDown(VK_KEY_S, isPossession: false);
            var actions = handler.PollCurrentActions(isPossession: false);

            Assert(!actions.Contains(GameAction.StandTackle), 
                $"Frame {frame}: Holding 'S' after possession loss MUST NOT trigger StandTackle!");
            Assert(!actions.Contains(GameAction.SlideTackle), 
                $"Frame {frame}: Holding 'S' after possession loss MUST NOT trigger SlideTackle!");
            Assert(!actions.Contains(GameAction.ShortPass), 
                $"Frame {frame}: Holding 'S' must not auto-repeat ShortPass without release!");
        }

        // Frame 101: Release 'S'
        handler.HandleKeyUp(VK_KEY_S);
        var f101Actions = handler.PollCurrentActions(isPossession: false);
        Assert(f101Actions.Count == 0, "Release must generate no actions");

        // Frame 102: Fresh tap of 'S' while defending -> Should now legally trigger StandTackle
        handler.HandleKeyDown(VK_KEY_S, isPossession: false);
        var f102Actions = handler.PollCurrentActions(isPossession: false);
        Assert(f102Actions.Contains(GameAction.StandTackle), 
            "Fresh press of 'S' while defending must trigger StandTackle");
    }

    private static void TestHoldW_ShootThenLosePossession_NeverTackles()
    {
        var handler = new KeyboardInputHandler();
        const int VK_KEY_W = 0x57;

        // Frame 1: User shoots with 'W'
        handler.HandleKeyDown(VK_KEY_W, isPossession: true);
        var f1Actions = handler.PollCurrentActions(isPossession: true);
        Assert(f1Actions.Contains(GameAction.Shoot), "Frame 1 must trigger Shoot on press");
        Assert(!f1Actions.Contains(GameAction.SlideTackle), "Frame 1 must NOT trigger SlideTackle");

        // Shot taken -> ball in flight -> possession lost
        for (int frame = 2; frame <= 100; frame++)
        {
            handler.HandleKeyDown(VK_KEY_W, isPossession: false);
            var actions = handler.PollCurrentActions(isPossession: false);

            Assert(!actions.Contains(GameAction.SlideTackle), 
                $"Frame {frame}: Holding 'W' after shot MUST NOT trigger SlideTackle!");
            Assert(!actions.Contains(GameAction.Shoot), 
                $"Frame {frame}: Holding 'W' must not auto-repeat Shoot!");
        }

        handler.HandleKeyUp(VK_KEY_W);
    }

    private static void TestHoldA_ThroughBallThenLosePossession_NeverTackles()
    {
        var handler = new KeyboardInputHandler();
        const int VK_KEY_A = 0x41;

        handler.HandleKeyDown(VK_KEY_A, isPossession: true);
        var f1Actions = handler.PollCurrentActions(isPossession: true);
        Assert(f1Actions.Contains(GameAction.ThroughBall), "Frame 1 must trigger ThroughBall");

        for (int frame = 2; frame <= 50; frame++)
        {
            handler.HandleKeyDown(VK_KEY_A, isPossession: false);
            var actions = handler.PollCurrentActions(isPossession: false);
            Assert(!actions.Contains(GameAction.SlideTackle), 
                $"Frame {frame}: Holding 'A' after through ball MUST NOT trigger SlideTackle!");
        }

        handler.HandleKeyUp(VK_KEY_A);
    }

    private static void TestHoldD_CrossThenLosePossession_NeverTackles()
    {
        var handler = new KeyboardInputHandler();
        const int VK_KEY_D = 0x44;

        handler.HandleKeyDown(VK_KEY_D, isPossession: true);
        var f1Actions = handler.PollCurrentActions(isPossession: true);
        Assert(f1Actions.Contains(GameAction.Cross), "Frame 1 must trigger Cross");

        for (int frame = 2; frame <= 50; frame++)
        {
            handler.HandleKeyDown(VK_KEY_D, isPossession: false);
            var actions = handler.PollCurrentActions(isPossession: false);
            Assert(!actions.Contains(GameAction.SlideTackle), 
                $"Frame {frame}: Holding 'D' after cross MUST NOT trigger SlideTackle!");
        }

        handler.HandleKeyUp(VK_KEY_D);
    }

    // ----------------------------------------------------------------------------------------
    // 2. Control Key Switch Debouncing
    // ----------------------------------------------------------------------------------------
    private static void TestHoldCtrl_SingleSwitch_NoCyclingAt50Hz()
    {
        var handler = new KeyboardInputHandler();
        const int VK_CONTROL = 0x11;

        int totalSwitchesFired = 0;

        // Frame 1: Press Ctrl
        handler.HandleKeyDown(VK_CONTROL, isPossession: false);
        var f1Actions = handler.PollCurrentActions(isPossession: false);
        if (f1Actions.Contains(GameAction.SwitchPlayer)) totalSwitchesFired++;

        Assert(totalSwitchesFired == 1, "Frame 1 must trigger SwitchPlayer");

        // Frames 2..100: User holds Ctrl for 2 full seconds (100 frames at 50Hz)
        for (int frame = 2; frame <= 100; frame++)
        {
            handler.HandleKeyDown(VK_CONTROL, isPossession: false);
            var actions = handler.PollCurrentActions(isPossession: false);
            if (actions.Contains(GameAction.SwitchPlayer))
            {
                totalSwitchesFired++;
            }
        }

        Assert(totalSwitchesFired == 1, 
            $"Holding Ctrl fired {totalSwitchesFired} switches! Expected EXACTLY 1 switch (no 50Hz cycling).");

        // Release Ctrl and press again
        handler.HandleKeyUp(VK_CONTROL);
        handler.HandleKeyDown(VK_CONTROL, isPossession: false);
        var secondPressActions = handler.PollCurrentActions(isPossession: false);
        if (secondPressActions.Contains(GameAction.SwitchPlayer)) totalSwitchesFired++;

        Assert(totalSwitchesFired == 2, 
            $"Releasing and pressing Ctrl again must trigger 2nd switch. Total: {totalSwitchesFired}");
    }

    private static void TestCtrlKeyVariants_Suppression()
    {
        var handler = new KeyboardInputHandler();
        const int VK_CONTROL = 0x11;
        const int VK_LCONTROL = 0xA2;
        const int VK_RCONTROL = 0xA3;

        int switches = 0;

        // Press Left Control
        handler.HandleKeyDown(VK_LCONTROL, isPossession: false);
        var a1 = handler.PollCurrentActions(isPossession: false);
        if (a1.Contains(GameAction.SwitchPlayer)) switches++;

        // Windows auto-repeats generic VK_CONTROL while LCONTROL is held
        handler.HandleKeyDown(VK_CONTROL, isPossession: false);
        var a2 = handler.PollCurrentActions(isPossession: false);
        if (a2.Contains(GameAction.SwitchPlayer)) switches++;

        // Windows auto-repeats VK_RCONTROL while held
        handler.HandleKeyDown(VK_RCONTROL, isPossession: false);
        var a3 = handler.PollCurrentActions(isPossession: false);
        if (a3.Contains(GameAction.SwitchPlayer)) switches++;

        Assert(switches == 1, 
            $"Control variants must share hold state. Switches observed: {switches}, expected: 1");

        // Releasing any clears all variants
        handler.HandleKeyUp(VK_LCONTROL);
        handler.HandleKeyDown(VK_RCONTROL, isPossession: false);
        var a4 = handler.PollCurrentActions(isPossession: false);
        if (a4.Contains(GameAction.SwitchPlayer)) switches++;

        Assert(switches == 2, 
            $"Releasing and pressing RCONTROL must trigger new switch. Switches observed: {switches}");
    }

    // ----------------------------------------------------------------------------------------
    // 3. Continuous Arrow & Sprint Movement
    // ----------------------------------------------------------------------------------------
    private static void TestContinuousArrowAndShift_MovementVector()
    {
        var handler = new KeyboardInputHandler();
        const int VK_UP = 0x26;
        const int VK_RIGHT = 0x27;
        const int VK_LSHIFT = 0xA0;

        // Frame 1: Press UP, RIGHT, LSHIFT
        handler.HandleKeyDown(VK_UP, isPossession: true);
        handler.HandleKeyDown(VK_RIGHT, isPossession: true);
        handler.HandleKeyDown(VK_LSHIFT, isPossession: true);

        // Frames 1..50: Check that all 50 frames maintain MoveUp, MoveRight, Sprint
        for (int frame = 1; frame <= 50; frame++)
        {
            var actions = handler.PollCurrentActions(isPossession: true);
            Assert(actions.Contains(GameAction.MoveUp), $"Frame {frame} must have MoveUp");
            Assert(actions.Contains(GameAction.MoveRight), $"Frame {frame} must have MoveRight");
            Assert(actions.Contains(GameAction.Sprint), $"Frame {frame} must have Sprint");
        }
    }

    private static void TestArrowReleaseSequence()
    {
        var handler = new KeyboardInputHandler();
        const int VK_UP = 0x26;
        const int VK_RIGHT = 0x27;
        const int VK_LSHIFT = 0xA0;

        handler.HandleKeyDown(VK_UP, isPossession: true);
        handler.HandleKeyDown(VK_RIGHT, isPossession: true);
        handler.HandleKeyDown(VK_LSHIFT, isPossession: true);

        // Release UP
        handler.HandleKeyUp(VK_UP);
        var a1 = handler.PollCurrentActions(isPossession: true);
        Assert(!a1.Contains(GameAction.MoveUp), "MoveUp must be cleared after KeyUp");
        Assert(a1.Contains(GameAction.MoveRight), "MoveRight must remain held");
        Assert(a1.Contains(GameAction.Sprint), "Sprint must remain held");

        // Release LSHIFT
        handler.HandleKeyUp(VK_LSHIFT);
        var a2 = handler.PollCurrentActions(isPossession: true);
        Assert(!a2.Contains(GameAction.Sprint), "Sprint must be cleared after KeyUp");
        Assert(a2.Contains(GameAction.MoveRight), "MoveRight must remain held");

        // Release RIGHT
        handler.HandleKeyUp(VK_RIGHT);
        var a3 = handler.PollCurrentActions(isPossession: true);
        Assert(a3.Count == 0, "All continuous keys must be cleared");
    }

    private static void TestOpposingArrowKeys_Cancellation()
    {
        var handler = new KeyboardInputHandler();
        const int VK_LEFT = 0x25;
        const int VK_RIGHT = 0x27;

        handler.HandleKeyDown(VK_LEFT, isPossession: true);
        handler.HandleKeyDown(VK_RIGHT, isPossession: true);

        var actions = handler.PollCurrentActions(isPossession: true);
        Assert(actions.Contains(GameAction.MoveLeft), "Must contain MoveLeft");
        Assert(actions.Contains(GameAction.MoveRight), "Must contain MoveRight");

        // Simulate MatchEngine vector resolution
        Vector2 moveDir = Vector2.Zero;
        if (actions.Contains(GameAction.MoveLeft)) moveDir.X -= 1.0f;
        if (actions.Contains(GameAction.MoveRight)) moveDir.X += 1.0f;

        Assert(moveDir.X == 0.0f && moveDir.Y == 0.0f, 
            $"Opposing keys must cancel to zero vector. Got: ({moveDir.X}, {moveDir.Y})");
    }

    // ----------------------------------------------------------------------------------------
    // 4. Real MatchEngine Integration Tests
    // ----------------------------------------------------------------------------------------
    private static void TestMatchEngine_HoldW_NoTackles()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        int tackleEventsCount = 0;
        eventBus.OnMatchEvent += evt => {
            if (evt.Type == MatchEventType.UserTackleSuccessful || 
                evt.Description.Contains("استخلاص") || 
                evt.Description.Contains("أنت بعيد عن حامل الكرة"))
            {
                tackleEventsCount++;
            }
        };

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var userPlayer = match.ActiveUserPlayer;
        Assert(match.Ball.Owner == userPlayer, "Active user player starts with ball possession");

        const int VK_KEY_W = 0x57;

        // Frame 1: Press 'W' to shoot
        inputHandler.HandleKeyDown(VK_KEY_W, isPossession: true);
        match.Update(0.02f);

        // Shot taken -> Ball launched towards goal
        Assert(tackleEventsCount == 0, "Shot must not trigger tackle attempt");

        // Frames 2..50: Hold 'W' across 50 simulation ticks (1 full second at 50Hz)
        for (int i = 0; i < 50; i++)
        {
            bool isPossession = match.Ball.Owner != null && homeTeam.Players.Contains(match.Ball.Owner);
            inputHandler.HandleKeyDown(VK_KEY_W, isPossession);
            match.Update(0.02f);

            Assert(tackleEventsCount == 0, 
                $"Tick {i}: Holding 'W' after shot triggered tackle event! Count: {tackleEventsCount}");
        }

        inputHandler.HandleKeyUp(VK_KEY_W);
    }

    private static void TestMatchEngine_HoldCtrl_SingleSwitch()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        int manualSwitchEvents = 0;
        eventBus.OnMatchEvent += evt => {
            if (evt.Type == MatchEventType.TacticalRadarInfo && evt.Description.Contains("-"))
            {
                manualSwitchEvents++;
            }
        };

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);

        // Away team striker has ball firmly controlled
        var opponentPlayer = awayTeam.Players[9];
        match.Ball.CatchOrTackle(opponentPlayer);

        // Position opponent near defender Player 2 (Left Back)
        var defender = homeTeam.Players[2];
        opponentPlayer.Position2D = new Vector2(defender.Position2D.X + 3.0f, defender.Position2D.Y);
        match.Ball.Position = new Vector3(opponentPlayer.Position2D.X, opponentPlayer.Position2D.Y, 0.0f);

        var initialPlayer = match.ActiveUserPlayer; // Player 9

        const int VK_CONTROL = 0x11;

        // Frame 1: Press Ctrl
        inputHandler.HandleKeyDown(VK_CONTROL, isPossession: false);
        match.Update(0.02f);

        var switchedPlayer = match.ActiveUserPlayer;
        Assert(switchedPlayer != initialPlayer, "Pressing Ctrl must switch to outfield defender");
        Assert(manualSwitchEvents == 1, $"Expected exactly 1 switch event on Frame 1, got: {manualSwitchEvents}");

        // Frames 2..100: Hold Ctrl across 100 simulation ticks (2 seconds at 50Hz)
        for (int i = 0; i < 100; i++)
        {
            // Keep ball with opponent striker so match remains in defending state
            match.Ball.CatchOrTackle(opponentPlayer);

            inputHandler.HandleKeyDown(VK_CONTROL, isPossession: false);
            match.Update(0.02f);

            Assert(manualSwitchEvents == 1, 
                $"Tick {i}: Holding Ctrl fired extra switch events! Count: {manualSwitchEvents}");
        }

        inputHandler.HandleKeyUp(VK_CONTROL);
    }

    private static void TestMatchEngine_ContinuousMovementDistance()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var player = match.ActiveUserPlayer;
        player.Position2D = new Vector2(50.0f, 34.0f);

        const int VK_RIGHT = 0x27;
        const int VK_LSHIFT = 0xA0;

        inputHandler.HandleKeyDown(VK_RIGHT, isPossession: false);
        inputHandler.HandleKeyDown(VK_LSHIFT, isPossession: false);

        Vector2 startPos = player.Position2D;

        // Run 50 ticks of 0.02s (1.0 second total)
        for (int i = 0; i < 50; i++)
        {
            match.Update(0.02f);
        }

        Vector2 endPos = player.Position2D;
        float distanceMoved = endPos.X - startPos.X;

        // Base speed = Speed / 7.5; with sprint = base * 1.6
        float expectedSpeed = (player.Speed / 7.5f) * 1.6f;
        float expectedDistance = expectedSpeed * 1.0f;

        Assert(Math.Abs(distanceMoved - expectedDistance) < 0.1f, 
            $"Expected distance ~{expectedDistance:F2}m, but moved {distanceMoved:F2}m");

        inputHandler.HandleKeyUp(VK_RIGHT);
        inputHandler.HandleKeyUp(VK_LSHIFT);
    }

    private static void TestMatchEngine_ConcurrentMovementAndSwitch()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var opponent = awayTeam.Players[9];
        match.Ball.CatchOrTackle(opponent);

        const int VK_UP = 0x26;
        const int VK_LSHIFT = 0xA0;
        const int VK_CONTROL = 0x11;

        // Hold UP and LSHIFT continuously
        inputHandler.HandleKeyDown(VK_UP, isPossession: false);
        inputHandler.HandleKeyDown(VK_LSHIFT, isPossession: false);

        // Tap Ctrl to switch player while moving
        inputHandler.HandleKeyDown(VK_CONTROL, isPossession: false);

        var actions = inputHandler.PollCurrentActions(isPossession: false);
        Assert(actions.Contains(GameAction.MoveUp), "Must contain MoveUp");
        Assert(actions.Contains(GameAction.Sprint), "Must contain Sprint");
        Assert(actions.Contains(GameAction.SwitchPlayer), "Must contain SwitchPlayer");

        // Next frame: Ctrl held, Arrow + Shift still held
        inputHandler.HandleKeyDown(VK_CONTROL, isPossession: false);
        var nextActions = inputHandler.PollCurrentActions(isPossession: false);
        Assert(nextActions.Contains(GameAction.MoveUp), "MoveUp must remain active");
        Assert(nextActions.Contains(GameAction.Sprint), "Sprint must remain active");
        Assert(!nextActions.Contains(GameAction.SwitchPlayer), "SwitchPlayer must NOT repeat");

        inputHandler.HandleKeyUp(VK_UP);
        inputHandler.HandleKeyUp(VK_LSHIFT);
        inputHandler.HandleKeyUp(VK_CONTROL);
    }

    // ----------------------------------------------------------------------------------------
    // 5. Static contract verification for Window Focus & Key Handling
    // ----------------------------------------------------------------------------------------
    private static void TestFormKeyHandlingAndFocus_StaticContract()
    {
        string root = FindWorkspaceRoot();
        string windowSourcePath = Path.Combine(root, "AccessibleFootball2026.App", "AccessibleFootballWindow.cs");

        Assert(File.Exists(windowSourcePath), $"AccessibleFootballWindow.cs not found at: {windowSourcePath}");
        string code = File.ReadAllText(windowSourcePath);

        // 1. Verify KeyPreview is enabled
        Assert(code.Contains("KeyPreview = true;"), "Form must have KeyPreview = true");

        // 2. Verify KeyUp event is hooked
        Assert(code.Contains("KeyUp += OnWindowKeyUp;"), "Form must hook KeyUp += OnWindowKeyUp");

        // 3. Verify e.SuppressKeyPress = true is present in KeyDown
        Assert(code.Contains("e.SuppressKeyPress = true;"), "KeyDown must set e.SuppressKeyPress = true on match keys");

        // 4. Verify e.Handled = true is set
        Assert(code.Contains("e.Handled = true;"), "KeyDown and KeyUp must set e.Handled = true");

        // 5. Verify _displayListBox is NEVER hidden via Visible = false during match
        Assert(!code.Contains("_displayListBox.Visible = false"), 
            "_displayListBox.Visible = false MUST NOT exist in AccessibleFootballWindow.cs to maintain NVDA focus!");

        // 6. Verify IsMatchKey covers required keys
        Assert(code.Contains("Keys.Up or Keys.Down or Keys.Left or Keys.Right"), "IsMatchKey must handle arrow keys");
        Assert(code.Contains("Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey"), "IsMatchKey must handle Shift variants");
        Assert(code.Contains("Keys.ControlKey or Keys.LControlKey or Keys.RControlKey"), "IsMatchKey must handle Control variants");
        Assert(code.Contains("Keys.D or Keys.W or Keys.S or Keys.A"), "IsMatchKey must handle D, W, S, A");
        Assert(code.Contains("Keys.Space or Keys.C"), "IsMatchKey must handle Space and C");
    }

    // ----------------------------------------------------------------------------------------
    // 6. Milestone 2: Pass Physics & Reception Mechanics Tests
    // ----------------------------------------------------------------------------------------
    private static void TestBall_LinearDeceleration_Overcomes6MeterWall()
    {
        var ball = new Ball();
        Vector2 dir = new(1.0f, 0.0f);
        ball.Kick(dir, horizontalSpeed: 18.0f, verticalSpeed: 0.0f);

        float initialX = ball.Position.X;
        float dt = 0.02f; // 50Hz

        // Simulate 75 frames (1.5 seconds)
        for (int i = 0; i < 75; i++)
        {
            ball.Update(dt);
        }

        float distanceTraveled = ball.Position.X - initialX;
        // In the old broken friction (0.94 multiplier), max distance was ~6.0m!
        // With linear deceleration (2.5 m/s²), s = 18(1.5) - 0.5(2.5)(1.5)² = 27 - 2.81 = 24.19m.
        Assert(distanceTraveled > 20.0f, $"Ball should have traveled > 20m in 1.5s, but only traveled {distanceTraveled:F2}m.");
        Assert(ball.Velocity.Length() > 4.0f, $"Ball should still be moving smoothly at 1.5s, but velocity is {ball.Velocity.Length():F2} m/s.");
    }

    private static void TestMatchEngine_ExecutePass_SetsPassTransitInfo()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        Assert(!match.IsPassInTransit, "Match should not start with pass in transit");
        Assert(match.ActivePassTransit == null, "ActivePassTransit should be null initially");

        // Execute short pass aimed forward
        match.ExecutePass(new Vector2(1.0f, 0.0f), isThroughBall: false);

        Assert(match.IsPassInTransit, "IsPassInTransit must be true immediately after ExecutePass");
        Assert(match.ActivePassTransit != null, "ActivePassTransit must not be null");
        Assert(match.ActivePassTransit!.IsActive, "ActivePassTransit.IsActive must be true");
        Assert(match.ActivePassTransit.PassType == PassType.ShortPass, "PassType must be ShortPass");
        Assert(match.ActivePassTransit.Sender != null, "Sender must be set");
        Assert(match.ActivePassTransit.IntendedReceiver != null, "IntendedReceiver must be set");
        Assert(match.ActivePassTransit.IntendedReceiver != match.ActivePassTransit.Sender, "Receiver must differ from sender");
    }

    private static void TestMatchEngine_ThroughBall_ReceiverActiveInterceptMovement()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);

        // Execute through ball
        match.ExecuteThroughBall(new Vector2(1.0f, 0.0f));

        Assert(match.ActivePassTransit != null, "ActivePassTransit should be set");
        var receiver = match.ActivePassTransit!.IntendedReceiver;
        Vector2 initialPos = receiver.Position2D;

        // Step engine across 15 frames (0.3s) without any user keyboard inputs
        for (int i = 0; i < 15; i++)
        {
            match.Update(0.02f);
            if (!match.IsPassInTransit) break;
        }

        float distMoved = Vector2.Distance(receiver.Position2D, initialPos);
        Assert(distMoved > 0.3f, $"Receiver must actively sprint towards the pass trajectory (moved {distMoved:F2}m), NOT stand frozen!");
    }

    private static void TestMatchEngine_Reception_AutoCatchWithinThresholdAndControlTransfer()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        bool trappedEventFired = false;
        eventBus.OnMatchEvent += evt =>
        {
            if (evt.Type == MatchEventType.BallTrapped)
            {
                trappedEventFired = true;
            }
        };

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var passer = match.ActiveUserPlayer;
        var receiver = homeTeam.Players.First(p => p != passer && p.Role != PlayerRole.Goalkeeper);

        // Initiate pass
        match.ExecutePass(Vector2.Normalize(receiver.Position2D - passer.Position2D), isThroughBall: false);
        Assert(match.IsPassInTransit, "Pass must be in transit");

        // Manually position ball within reception distance (1.2m <= 2.2m threshold) from receiver
        match.Ball.Position = new Vector3(receiver.Position2D.X + 1.2f, receiver.Position2D.Y, 0.0f);
        match.Ball.Velocity = new Vector3(-2.0f, 0.0f, 0.0f);

        // Step engine frame
        match.Update(0.02f);

        // Verify reception
        Assert(match.Ball.Owner == receiver, "Receiver must have caught the ball upon reaching reception threshold");
        Assert(receiver.HasBall, "Receiver HasBall must be true");
        Assert(match.ActiveUserPlayer == receiver, "ActiveUserPlayer must immediately transfer to receiver");
        Assert(!match.IsPassInTransit, "Active pass transit must be cleared after reception");
        Assert(trappedEventFired, "BallTrapped event must be fired upon reception for audio/speech feedback");
    }

    private static void TestBall_VerticalFlight_BouncesAndClampsGround()
    {
        var ball = new Ball();
        // 1. Kick high airborne ball
        ball.Kick(new Vector2(1.0f, 0.0f), horizontalSpeed: 10.0f, verticalSpeed: 5.0f);
        Assert(ball.State == BallState.InFlight, "Ball must be InFlight when verticalSpeed > 0.5f");

        float maxZ = 0.0f;
        bool didBounce = false;
        float prevVelZ = ball.Velocity.Z;

        // Step physics for 150 frames (3.0s) to allow bounces to fully subside
        for (int frame = 0; frame < 150; frame++)
        {
            ball.Update(0.02f);
            Assert(ball.Position.Z >= 0.0f, $"Ball Position.Z ({ball.Position.Z:F4}) must never sink below ground (Z >= 0)");
            if (ball.Position.Z > maxZ)
            {
                maxZ = ball.Position.Z;
            }

            // Check bounce: previous frame was descending (Vel.Z < -1.0), now rising (Vel.Z > 0)
            if (prevVelZ < -1.0f && ball.Velocity.Z > 0.0f)
            {
                didBounce = true;
            }
            prevVelZ = ball.Velocity.Z;
        }

        Assert(maxZ > 1.0f, $"High ball must reach significant elevation (maxZ={maxZ:F2}m)");
        Assert(didBounce, "Ball must bounce upon falling impact with ground when |Velocity.Z| > 1.0 m/s");
        Assert(ball.Position.Z == 0.0f, "Ball must settle on ground (Z == 0) after bounces subside");

        // 2. Test low vertical speed pass (e.g. 0.15m/s lift from ExecutePass)
        var passBall = new Ball();
        passBall.Kick(new Vector2(1.0f, 0.0f), horizontalSpeed: 18.0f, verticalSpeed: 0.15f);
        for (int frame = 0; frame < 50; frame++)
        {
            passBall.Update(0.02f);
            Assert(passBall.Position.Z >= 0.0f, $"Pass ball Position.Z ({passBall.Position.Z:F4}) must NEVER drop below 0.0m");
        }
        Assert(passBall.Position.Z == 0.0f, "Pass ball must clamp to Z = 0.0m upon ground contact");
        Assert(passBall.Velocity.Z == 0.0f, "Pass ball must settle with Velocity.Z = 0.0m on ground");
    }

    // ----------------------------------------------------------------------------------------
    // 8. Milestone 3: Defensive Switching, Tackling & Auditory Feedback Tests (F9, F10, F11, F12)
    // ----------------------------------------------------------------------------------------
    private static void TestF9_PlayerSwitching_ExcludesCurrentActivePlayer()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var initialPlayer = match.ActiveUserPlayer;

        // Position ball and opponent far away
        match.Ball.Position = new Vector3(25.0f, 25.0f, 0.0f);

        // First switch: must exclude initialPlayer and goalkeeper
        match.SwitchToClosestPlayer();
        var firstSwitched = match.ActiveUserPlayer;

        Assert(firstSwitched != initialPlayer, 
            $"First switch must NOT select the already active player {initialPlayer.Name}");
        Assert(firstSwitched.Role != PlayerRole.Goalkeeper, 
            "Switching must select outfield players, never the goalkeeper");

        // Wait 250ms for debounce guard to expire
        System.Threading.Thread.Sleep(250);

        // Second switch: must exclude firstSwitched and select the next closest outfield player
        match.SwitchToClosestPlayer();
        var secondSwitched = match.ActiveUserPlayer;

        Assert(secondSwitched != firstSwitched, 
            $"Second switch must select a new teammate, not stay stuck on {firstSwitched.Name}");
        Assert(secondSwitched.Role != PlayerRole.Goalkeeper, 
            "Second switch must never select the goalkeeper");
    }

    private static void TestF9_PlayerSwitching_SpeaksNewDefenderNameAndPosition()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        string? announcedMsg = null;
        eventBus.OnMatchEvent += evt =>
        {
            if (evt.Type == MatchEventType.TacticalRadarInfo && evt.Description.Contains("-"))
            {
                announcedMsg = evt.Description;
            }
        };

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        match.SwitchToClosestPlayer();

        Assert(announcedMsg != null, "Switching player must publish a TacticalRadarInfo announcement");
        Assert(announcedMsg!.Contains(match.ActiveUserPlayer.Name), 
            $"Announcement must contain new player's name '{match.ActiveUserPlayer.Name}'. Got: '{announcedMsg}'");
        Assert(announcedMsg.Contains("-"), 
            $"Announcement must contain position separator '-'. Got: '{announcedMsg}'");
        Assert(!announcedMsg.Contains("اضغط"), 
            $"Announcement must not contain tutorial prompts. Got: '{announcedMsg}'");
    }

    private static void TestF9_PlayerSwitching_DebouncePrevents50HzFloods()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        int switchEventCount = 0;
        eventBus.OnMatchEvent += evt =>
        {
            if (evt.Type == MatchEventType.TacticalRadarInfo && evt.Description.Contains("-"))
            {
                switchEventCount++;
            }
        };

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);

        // Simulate rapid calls in the same game frame (or 50Hz spam with 0ms gap)
        for (int i = 0; i < 50; i++)
        {
            match.SwitchToClosestPlayer();
        }

        Assert(switchEventCount == 1, 
            $"Debounce guard must prevent 50Hz speech flooding. Fired: {switchEventCount}, Expected: exactly 1");
    }

    private static void TestF10_DistinctStandingTackle_ReachAndCooldownAndStationary()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var tackler = match.ActiveUserPlayer;
        tackler.Position2D = new Vector2(50.0f, 30.0f);

        var opponent = awayTeam.Players[9];
        // Place opponent at 1.5m (within 1.8m standing reach)
        opponent.Position2D = new Vector2(51.5f, 30.0f);
        match.Ball.Position = new Vector3(51.5f, 30.0f, 0.0f);
        match.Ball.CatchOrTackle(opponent);

        // Execute standing tackle
        match.ExecuteStandTackle();

        // 1. Verify 0.4s cooldown
        Assert(match.TackleCooldownRemaining > 0.35f && match.TackleCooldownRemaining <= 0.40f, 
            $"Standing tackle cooldown must be 0.4s. Got: {match.TackleCooldownRemaining:F3}s");

        // 2. Verify stationary (no slide duration or impulse)
        Assert(!match.IsSliding, "Standing tackle must be stationary, NOT sliding");
        Assert(match.SlideDurationRemaining == 0.0f, "Slide duration must remain 0 on standing tackle");

        // 3. Verify cooldown blocks immediate secondary tackle
        float remainingBefore = match.TackleCooldownRemaining;
        match.ExecuteStandTackle();
        Assert(match.TackleCooldownRemaining <= remainingBefore, 
            "Calling tackle while on cooldown must be ignored and not reset the timer");
    }

    private static void TestF10_DistinctSlideTackle_ReachAndImpulseAndCooldown()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var tackler = match.ActiveUserPlayer;
        tackler.Position2D = new Vector2(50.0f, 30.0f);
        tackler.FacingDirection = new Vector2(1.0f, 0.0f);

        var opponent = awayTeam.Players[9];
        // Place opponent at 2.4m (out of 1.8m standing reach, but within 2.8m slide reach!)
        opponent.Position2D = new Vector2(52.4f, 30.0f);
        match.Ball.Position = new Vector3(52.4f, 30.0f, 0.0f);
        match.Ball.CatchOrTackle(opponent);

        // Execute slide tackle
        match.ExecuteSlideTackle();

        // 1. Verify 0.9s recovery cooldown
        Assert(match.TackleCooldownRemaining > 0.85f && match.TackleCooldownRemaining <= 0.90f, 
            $"Slide tackle cooldown must be 0.9s. Got: {match.TackleCooldownRemaining:F3}s");

        // 2. Verify 0.3s slide duration
        Assert(match.IsSliding, "Slide tackle must set IsSliding = true");
        Assert(match.SlideDurationRemaining > 0.25f && match.SlideDurationRemaining <= 0.30f, 
            $"Slide duration must be 0.3s. Got: {match.SlideDurationRemaining:F3}s");

        // 3. Step physics across 5 frames (0.10s) and verify forward slide impulse (+4.0m/s)
        float startX = tackler.Position2D.X;
        for (int i = 0; i < 5; i++)
        {
            match.Update(0.02f);
        }

        float distanceMoved = tackler.Position2D.X - startX;
        // In 0.10s at +4.0m/s, expected forward slide distance is 0.40m
        Assert(distanceMoved > 0.30f && distanceMoved < 0.50f, 
            $"Slide impulse must carry player forward ~0.40m in 0.10s. Moved: {distanceMoved:F3}m");
    }

    private static void TestF10_SlideTackleFromBehind_HigherFoulProbability()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        int foulCount = 0;
        eventBus.OnMatchEvent += evt =>
        {
            if (evt.Type == MatchEventType.FoulCommitted && evt.Description.Contains("من الخلف"))
            {
                foulCount++;
            }
        };

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var tackler = match.ActiveUserPlayer;
        var opponent = awayTeam.Players[9];

        // Run 25 trials of slide tackle from behind
        for (int trial = 0; trial < 25; trial++)
        {
            // Clear cooldowns by advancing time
            for (int i = 0; i < 50; i++) match.Update(0.02f);

            // Position opponent carrier facing goal (1.0f, 0.0f)
            opponent.Position2D = new Vector2(50.0f, 30.0f);
            opponent.FacingDirection = new Vector2(1.0f, 0.0f);
            match.Ball.Position = new Vector3(50.0f, 30.0f, 0.0f);
            match.Ball.CatchOrTackle(opponent);

            // Position tackler directly behind opponent's back at 1.8m
            tackler.Position2D = new Vector2(48.2f, 30.0f);
            tackler.FacingDirection = new Vector2(1.0f, 0.0f);

            match.ExecuteSlideTackle();
        }

        // With 35% probability across 25 trials, at least 1 foul is virtually guaranteed (99.998%)
        Assert(foulCount > 0, $"Slide tackle from behind must have elevated foul risk. Fouls observed: {foulCount}");
    }

    private static void TestF11_TackleDistanceAnnouncements_OutOfReach()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        string? announcedMsg = null;
        eventBus.OnMatchEvent += evt =>
        {
            if (evt.Type == MatchEventType.TacticalRadarInfo && evt.Description.Contains("بعيد"))
            {
                announcedMsg = evt.Description;
            }
        };

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var tackler = match.ActiveUserPlayer;
        tackler.Position2D = new Vector2(50.0f, 30.0f);

        var opponent = awayTeam.Players[9];
        // Place opponent at 4.2m away
        opponent.Position2D = new Vector2(54.2f, 30.0f);
        match.Ball.Position = new Vector3(54.2f, 30.0f, 0.0f);
        match.Ball.CatchOrTackle(opponent);

        // Execute standing tackle (reach 1.8m) -> Out of range!
        match.ExecuteStandTackle();

        Assert(announcedMsg != null, "Out-of-range tackle must announce numerical distance");
        Assert(announcedMsg!.Contains("4.2"), 
            $"Announcement must contain exact numerical distance '4.2'. Got: '{announcedMsg}'");
        Assert(!announcedMsg.Contains("اضغط"), 
            $"Announcement must NOT contain tutorial hints. Got: '{announcedMsg}'");
    }

    private static void TestF11_TackleSuccessAnnouncement_BallRecovery()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        string? successMsg = null;
        eventBus.OnMatchEvent += evt =>
        {
            if (evt.Type == MatchEventType.UserTackleSuccessful)
            {
                successMsg = evt.Description;
            }
        };

        MatchSettings.Current.Difficulty = DifficultyLevel.Easy;
        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var tackler = match.ActiveUserPlayer;

        var opponent = awayTeam.Players[9];

        // Attempt tackle within 1.0m until won (approaching head-on closer to ball than opponent)
        for (int attempt = 0; attempt < 10; attempt++)
        {
            // Wait out cooldown
            for (int i = 0; i < 25; i++) match.Update(0.02f);

            tackler.Position2D = new Vector2(50.0f, 30.0f);
            tackler.FacingDirection = new Vector2(1.0f, 0.0f);

            opponent.Position2D = new Vector2(51.0f, 30.0f);
            opponent.FacingDirection = new Vector2(-1.0f, 0.0f);
            match.Ball.Position = new Vector3(50.8f, 30.0f, 0.0f);
            match.Ball.CatchOrTackle(opponent);

            match.ExecuteStandTackle();
            if (successMsg != null) break;
        }

        Assert(successMsg != null, "Successful tackle must publish UserTackleSuccessful event");
        Assert(match.Ball.Owner == tackler, "Ball owner must transfer to tackler upon tackle success");
        Assert(tackler.HasBall, "Tackler HasBall must be true");
        Assert(successMsg!.Contains("استخلاص") && successMsg.Contains(tackler.Name), 
            $"Success message must confirm ball recovery with player name. Got: '{successMsg}'");
    }

    private static void TestF12_AudioConfirmation_ShotTakenWiredToShotSound()
    {
        var screenReader = new TestScreenReader();
        var coordinator = new SmartAudioCoordinator(screenReader);

        SoundEffectsPlayer.ResetTelemetry();

        // Fire ShotTaken event
        var shotEvt = new MatchEvent(MatchEventType.ShotTaken, "تسديدة قوية من محمد صلاح نحو الشباك!", spatialPan: 0.25f);
        coordinator.HandleMatchEvent(shotEvt);

        // Verify shot_kick sound effect was played and not silent!
        Assert(SoundEffectsPlayer.LastPlayedSound == "shot_kick", 
            $"ShotTaken MUST trigger 'shot_kick' sound effect! Got: '{SoundEffectsPlayer.LastPlayedSound}'");
        Assert(Math.Abs(SoundEffectsPlayer.LastPlayedPan - 0.25f) < 0.01f, 
            $"Spatial pan must match event pan (0.25). Got: {SoundEffectsPlayer.LastPlayedPan}");
    }

    private static void TestF12_AudioConfirmation_DistinctKickAndTackleSounds()
    {
        var screenReader = new TestScreenReader();
        var coordinator = new SmartAudioCoordinator(screenReader);

        // 1. Pass kick
        SoundEffectsPlayer.ResetTelemetry();
        coordinator.HandleMatchEvent(new MatchEvent(MatchEventType.BallKicked, "تمريرة أرضية إلى صلاح", spatialPan: -0.5f));
        Assert(SoundEffectsPlayer.LastPlayedSound == "pass_kick", 
            $"Ground pass must trigger 'pass_kick'. Got: '{SoundEffectsPlayer.LastPlayedSound}'");

        // 2. Cross kick
        SoundEffectsPlayer.ResetTelemetry();
        coordinator.HandleMatchEvent(new MatchEvent(MatchEventType.BallKicked, "عرضية عالية داخل منطقة الجزاء", spatialPan: 0.5f));
        Assert(SoundEffectsPlayer.LastPlayedSound == "cross_kick", 
            $"Cross must trigger 'cross_kick'. Got: '{SoundEffectsPlayer.LastPlayedSound}'");

        // 3. Standing tackle
        SoundEffectsPlayer.ResetTelemetry();
        coordinator.HandleMatchEvent(new MatchEvent(MatchEventType.UserTackleSuccessful, "استخلصتَ الكرة بنجاح بتدخل هادئ بواسطة عبد المنعم!"));
        Assert(SoundEffectsPlayer.LastPlayedSound == "stand_tackle", 
            $"Standing tackle must trigger 'stand_tackle'. Got: '{SoundEffectsPlayer.LastPlayedSound}'");

        // 4. Slide tackle
        SoundEffectsPlayer.ResetTelemetry();
        coordinator.HandleMatchEvent(new MatchEvent(MatchEventType.UserTackleSuccessful, "استخلصتَ الكرة بنجاح بانزلاق أرضي متقن بواسطة هاني!"));
        Assert(SoundEffectsPlayer.LastPlayedSound == "slide_tackle", 
            $"Slide tackle must trigger 'slide_tackle'. Got: '{SoundEffectsPlayer.LastPlayedSound}'");

        // 5. Whistle
        SoundEffectsPlayer.ResetTelemetry();
        coordinator.HandleMatchEvent(new MatchEvent(MatchEventType.FoulCommitted, "خطأ ضد اللاعب"));
        Assert(SoundEffectsPlayer.LastPlayedSound == "whistle", 
            $"FoulCommitted must trigger 'whistle'. Got: '{SoundEffectsPlayer.LastPlayedSound}'");
    }

    private static void TestF12_ProceduralSoundSynthesis_WavFilesValidOnDisk()
    {
        SoundEffectsPlayer.Initialize();

        string? shot = SoundEffectsPlayer.ResolveAudioPath("SFX", "Kicks", "Shot");
        string? pass = SoundEffectsPlayer.ResolveAudioPath("SFX", "Kicks", "Pass");
        string? cross = SoundEffectsPlayer.ResolveAudioPath("SFX", "Kicks", "Cross");
        string? standTackle = SoundEffectsPlayer.ResolveAudioPath("SFX", "Tackles", "StandTackle");
        string? slideTackle = SoundEffectsPlayer.ResolveAudioPath("SFX", "Tackles", "SlideTackle");
        string? whistle = SoundEffectsPlayer.ResolveAudioPath("SFX", "Whistles", "Whistle");
        string? goalNet = SoundEffectsPlayer.ResolveAudioPath("SFX", "GoalNet", "GoalNet");

        string[] requiredFiles = { shot!, pass!, cross!, standTackle!, slideTackle!, whistle!, goalNet! };

        foreach (var file in requiredFiles)
        {
            Assert(!string.IsNullOrEmpty(file), "Required audio path must not be null or empty");
            Assert(File.Exists(file), $"Synthesized audio file must exist on disk: {file}");

            var fileInfo = new FileInfo(file);
            Assert(fileInfo.Length > 200, $"Audio file {Path.GetFileName(file)} must contain real waveform data (>200 bytes)");

            // Verify valid RIFF/WAVE header
            using var fs = File.OpenRead(file);
            byte[] header = new byte[12];
            fs.ReadExactly(header, 0, 12);
            string riff = System.Text.Encoding.ASCII.GetString(header, 0, 4);
            string wave = System.Text.Encoding.ASCII.GetString(header, 8, 4);
            Assert(riff == "RIFF" && wave == "WAVE", $"File {Path.GetFileName(file)} must have valid RIFF/WAVE header");
        }
    }

    private static void TestZeroClutter_InMatchAnnouncementsAndMenus_FreeOfTutorialInstructions()
    {
        string root = FindWorkspaceRoot();

        // 1. Static check: MatchEngine.cs must have zero occurrences of "اضغط"
        string matchEngineCode = File.ReadAllText(Path.Combine(root, "AccessibleFootball2026.Core", "Simulation", "MatchEngine.cs"));
        Assert(!matchEngineCode.Contains("اضغط"), 
            "MatchEngine.cs must contain ZERO occurrences of 'اضغط' (Zero-Clutter Directive)!");
        Assert(matchEngineCode.Contains("حارس مرماك {teammate.Name} يمسك بالكرة بأمان."), 
            "MatchEngine.cs line 1035 must have concise GK catch announcement without tutorial prompt!");

        // 2. Static check: GamePlanMenuBuilder.cs lineup and bench titles must have zero tutorial instructions
        string gamePlanCode = File.ReadAllText(Path.Combine(root, "AccessibleFootball2026.App", "Menus", "GamePlanMenuBuilder.cs"));
        Assert(!gamePlanCode.Contains("اضغط Enter على أي لاعب لتعديل مركزه أو تبديله"), 
            "GamePlanMenuBuilder.cs must not contain Lineup tutorial prompt in menu title!");
        Assert(!gamePlanCode.Contains("اضغط Enter لإدخال اللاعب للتشكيل"), 
            "GamePlanMenuBuilder.cs must not contain Bench tutorial prompt in menu title!");
        Assert(gamePlanCode.Contains("new AccessibleMenu(\"التشكيلة الأساسية\", screenReader)"), 
            "Lineup menu title must be clean 'التشكيلة الأساسية'");
        Assert(gamePlanCode.Contains("new AccessibleMenu(\"دكة البدلاء\", screenReader)"), 
            "Bench menu title must be clean 'دكة البدلاء'");

        // 3. Static check: AccessibleFootballWindow.cs kickoff intro must be free of tutorial listings
        string windowCode = File.ReadAllText(Path.Combine(root, "AccessibleFootball2026.App", "AccessibleFootballWindow.cs"));
        Assert(!windowCode.Contains("اضغط D للتمرير"), 
            "AccessibleFootballWindow.cs must not list button prompts in kickoff speech!");
        Assert(!windowCode.Contains("اضغط W للتسديد"), 
            "AccessibleFootballWindow.cs must not list button prompts in kickoff speech!");
        Assert(!windowCode.Contains("اضغط Ctrl للتبديل"), 
            "AccessibleFootballWindow.cs must not list button prompts in kickoff speech!");

        // 4. Runtime simulation check: comprehensive in-match event stream
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        var inMatchAnnouncements = new List<string>();
        eventBus.OnMatchEvent += evt =>
        {
            if (!string.IsNullOrEmpty(evt.Description))
            {
                inMatchAnnouncements.Add(evt.Description);
            }
        };

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);

        // A. Player switching
        match.SwitchToClosestPlayer();

        // B. Out-of-reach standing tackle
        match.ActiveUserPlayer.Position2D = new Vector2(10.0f, 10.0f);
        match.Ball.Position = new Vector3(40.0f, 40.0f, 0.0f);
        match.ExecuteStandTackle();

        // C. Out-of-reach slide tackle
        match.ExecuteSlideTackle();

        // D. Goalkeeper catch in open play
        var gk = homeTeam.Players.First(p => p.Role == PlayerRole.Goalkeeper);
        match.Ball.State = BallState.Free;
        match.Ball.Owner = null;
        match.Ball.Position = new Vector3(gk.Position2D.X + 1.0f, gk.Position2D.Y, 0.0f);
        match.Ball.Velocity = Vector3.Zero;
        match.Update(0.02f);

        // E. Dynamic updates across 50 frames
        for (int frame = 0; frame < 50; frame++)
        {
            match.Update(0.02f);
        }

        Assert(inMatchAnnouncements.Count > 0, "Match events should have been recorded");
        foreach (var msg in inMatchAnnouncements)
        {
            Assert(!msg.Contains("اضغط"), 
                $"CRITICAL DIRECTIVE VIOLATION: In-match announcement contains tutorial prompt 'اضغط': '{msg}'");
            Assert(!msg.Contains("Ctrl") && !msg.Contains("للتسديد") && !msg.Contains("للتبديل") && !msg.Contains("للتمرير"), 
                $"CRITICAL DIRECTIVE VIOLATION: In-match announcement contains tutorial button instructions: '{msg}'");
        }
    }

    // ----------------------------------------------------------------------------------------
    // 10. Milestone 1: Controls Realign, Pause Menu, Fair Tackle & Half-Time Tests
    // ----------------------------------------------------------------------------------------
    private static void TestM1_ControlsAlignment()
    {
        var handler = new KeyboardInputHandler();
        const int VK_KEY_S = 0x53;
        const int VK_KEY_D = 0x44;
        const int VK_KEY_W = 0x57;
        const int VK_KEY_A = 0x41;
        const int VK_SPACE = 0x20;
        const int VK_SHIFT = 0x10;
        const int VK_CONTROL = 0x11;

        // 1. Possession state (User has ball)
        handler.HandleKeyDown(VK_KEY_S, isPossession: true);
        var sPoss = handler.PollCurrentActions(isPossession: true);
        Assert(sPoss.Contains(GameAction.ShortPass), "In possession, 'S' must trigger ShortPass");
        handler.HandleKeyUp(VK_KEY_S);

        handler.HandleKeyDown(VK_KEY_D, isPossession: true);
        var dPoss = handler.PollCurrentActions(isPossession: true);
        Assert(dPoss.Contains(GameAction.Cross), "In possession, 'D' must trigger Cross");
        handler.HandleKeyUp(VK_KEY_D);

        handler.HandleKeyDown(VK_KEY_W, isPossession: true);
        var wPoss = handler.PollCurrentActions(isPossession: true);
        Assert(wPoss.Contains(GameAction.Shoot), "In possession, 'W' must trigger Shoot");
        handler.HandleKeyUp(VK_KEY_W);

        handler.HandleKeyDown(VK_KEY_A, isPossession: true);
        var aPoss = handler.PollCurrentActions(isPossession: true);
        Assert(aPoss.Contains(GameAction.ThroughBall), "In possession, 'A' must trigger ThroughBall");
        handler.HandleKeyUp(VK_KEY_A);

        handler.HandleKeyDown(VK_SPACE, isPossession: true);
        var spacePoss = handler.PollCurrentActions(isPossession: true);
        Assert(spacePoss.Contains(GameAction.TacticalRadar), "In possession, 'Space' must trigger TacticalRadar");
        handler.HandleKeyUp(VK_SPACE);

        handler.HandleKeyDown(VK_SHIFT, isPossession: true);
        var shiftPoss = handler.PollCurrentActions(isPossession: true);
        Assert(shiftPoss.Contains(GameAction.Sprint), "'Shift' must trigger Sprint");
        handler.HandleKeyUp(VK_SHIFT);

        // 2. Defending state (User does not have ball)
        handler.HandleKeyDown(VK_KEY_S, isPossession: false);
        var sDef = handler.PollCurrentActions(isPossession: false);
        Assert(sDef.Contains(GameAction.StandTackle), "When defending, 'S' must trigger StandTackle");
        handler.HandleKeyUp(VK_KEY_S);

        handler.HandleKeyDown(VK_KEY_D, isPossession: false);
        var dDef = handler.PollCurrentActions(isPossession: false);
        Assert(dDef.Contains(GameAction.SlideTackle), "When defending, 'D' must trigger SlideTackle");
        handler.HandleKeyUp(VK_KEY_D);

        handler.HandleKeyDown(VK_CONTROL, isPossession: false);
        var ctrlDef = handler.PollCurrentActions(isPossession: false);
        Assert(ctrlDef.Contains(GameAction.SwitchPlayer), "When defending, 'Ctrl' must trigger SwitchPlayer");
        handler.HandleKeyUp(VK_CONTROL);

        // 3. UI static contract verification
        string root = FindWorkspaceRoot();
        string windowSourcePath = Path.Combine(root, "AccessibleFootball2026.App", "AccessibleFootballWindow.cs");
        Assert(File.Exists(windowSourcePath), $"AccessibleFootballWindow.cs not found at: {windowSourcePath}");
        string windowCode = File.ReadAllText(windowSourcePath);
        Assert(windowCode.Contains("S للتمرير"), "UI must display S for passing");
        Assert(windowCode.Contains("D للرفع والعرضيات"), "UI must display D for crossing");
        Assert(windowCode.Contains("W للتسديد"), "UI must display W for shooting");
        Assert(windowCode.Contains("A للبينية"), "UI must display A for through ball");
        Assert(windowCode.Contains("Ctrl لتبديل المدافع"), "UI must display Ctrl for player switching");
        Assert(windowCode.Contains("Shift للجري"), "UI must display Shift for sprinting");
        Assert(windowCode.Contains("Space للرادار"), "UI must display Space for radar");
    }

    private static void TestM1_EscapeKey_PausesMatchAndTransitionsToPauseMenu()
    {
        const int VK_ESCAPE = 0x1B;

        // 1. Verify Escape key mapping to GameAction.Pause
        var standaloneHandler = new KeyboardInputHandler();
        standaloneHandler.HandleKeyDown(VK_ESCAPE, isPossession: false);
        var actions = standaloneHandler.PollCurrentActions(isPossession: false);
        Assert(actions.Contains(GameAction.Pause), "VK_ESCAPE (0x1B) must map to GameAction.Pause");

        // 2. Verify MatchEngine transitions to Paused state on GameAction.Pause
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var matchHandler = new KeyboardInputHandler();
        var match = new MatchEngine(homeTeam, awayTeam, eventBus, matchHandler);
        Assert(match.State == MatchState.InPlay, "Initial match state must be InPlay");

        // Press Escape on matchHandler and run update
        matchHandler.HandleKeyDown(VK_ESCAPE, isPossession: false);
        match.Update(0.02f);
        Assert(match.State == MatchState.Paused, "MatchEngine must transition to MatchState.Paused on Escape key");
        matchHandler.HandleKeyUp(VK_ESCAPE);

        // 3. Verify static contract in AccessibleFootballWindow.cs and GamePlanMenuBuilder.cs
        string root = FindWorkspaceRoot();
        string windowSourcePath = Path.Combine(root, "AccessibleFootball2026.App", "AccessibleFootballWindow.cs");
        string windowCode = File.ReadAllText(windowSourcePath);
        Assert(windowCode.Contains("Keys.Escape"), "AccessibleFootballWindow must handle Keys.Escape");
        Assert(windowCode.Contains("GamePlanMenuBuilder.CreatePauseMenu"), "Keys.Escape handler must instantiate PauseMenu via GamePlanMenuBuilder");

        string menuSourcePath = Path.Combine(root, "AccessibleFootball2026.App", "Menus", "GamePlanMenuBuilder.cs");
        string menuCode = File.ReadAllText(menuSourcePath);
        Assert(menuCode.Contains("CreatePauseMenu"), "GamePlanMenuBuilder must contain CreatePauseMenu");
        Assert(menuCode.Contains("قائمة الإيقاف المؤقت"), "CreatePauseMenu must provide pause menu title");
        Assert(menuCode.Contains("استئناف المباراة"), "CreatePauseMenu must provide resume match option");
    }

    private static void TestM1_FairTackle_CloserToBall_WinsCleanTackle()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        bool tackleWonFired = false;
        bool foulFired = false;
        string? tackleMsg = null;

        eventBus.OnMatchEvent += evt =>
        {
            if (evt.Type == MatchEventType.UserTackleSuccessful)
            {
                tackleWonFired = true;
                tackleMsg = evt.Description;
            }
            if (evt.Type == MatchEventType.FoulCommitted)
            {
                foulFired = true;
            }
        };

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var tackler = match.ActiveUserPlayer;
        var opponent = awayTeam.Players[9];

        // Position tackler at (50, 30) facing (1, 0)
        tackler.Position2D = new Vector2(50.0f, 30.0f);
        tackler.FacingDirection = new Vector2(1.0f, 0.0f);

        // Position opponent head-on at (51, 30) facing (-1, 0) towards tackler
        // The ball at opponent's feet is at X = 51.0 - 0.35 = 50.65m
        // Distance to ball = 0.65m; Distance to opponent = 1.00m -> distToBall < distToOpponent
        opponent.Position2D = new Vector2(51.0f, 30.0f);
        opponent.FacingDirection = new Vector2(-1.0f, 0.0f);
        match.Ball.Position = new Vector3(50.8f, 30.0f, 0.0f);
        match.Ball.CatchOrTackle(opponent);

        match.ExecuteStandTackle();

        Assert(tackleWonFired, "Deterministic fair tackle must win ball cleanly when closer to ball than opponent body");
        Assert(!foulFired, "Clean tackle must NEVER fire FoulCommitted");
        Assert(match.Ball.Owner == tackler, "Ball owner must transfer to tackler");
        Assert(tackler.HasBall, "Tackler HasBall must be true");
        Assert(tackleMsg != null && tackleMsg.Contains("استخلاص") && tackleMsg.Contains(tackler.Name),
            $"Tackle message must confirm clean recovery with player name. Got: '{tackleMsg}'");
    }

    private static void TestM1_FairTackle_CloserToOpponent_CallsFoul()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        bool foulFired = false;
        bool tackleWonFired = false;
        string? foulMsg = null;

        eventBus.OnMatchEvent += evt =>
        {
            if (evt.Type == MatchEventType.FoulCommitted)
            {
                foulFired = true;
                foulMsg = evt.Description;
            }
            if (evt.Type == MatchEventType.UserTackleSuccessful)
            {
                tackleWonFired = true;
            }
        };

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var tackler = match.ActiveUserPlayer;
        var opponent = awayTeam.Players[9];

        // Opponent carrier facing (1, 0) at (50, 30) with ball ahead at (50.35, 30)
        opponent.Position2D = new Vector2(50.0f, 30.0f);
        opponent.FacingDirection = new Vector2(1.0f, 0.0f);
        match.Ball.Position = new Vector3(50.0f, 30.0f, 0.0f);
        match.Ball.CatchOrTackle(opponent);

        // Tackler directly behind opponent's back at (48.2, 30) facing (1, 0)
        // Distance to opponent = 1.80m; Distance to ball = 2.15m -> distToOpponent < distToBall
        tackler.Position2D = new Vector2(48.2f, 30.0f);
        tackler.FacingDirection = new Vector2(1.0f, 0.0f);

        match.ExecuteSlideTackle();

        Assert(foulFired, "Deterministic fair tackle must call foul when tackler hits opponent body before ball");
        Assert(!tackleWonFired, "Foul tackle must NOT fire UserTackleSuccessful");
        Assert(match.Ball.Owner != tackler, "Tackler must NOT get ball on foul");
        Assert(foulMsg != null && foulMsg.Contains("خطأ ضد") && foulMsg.Contains("من الخلف"),
            $"Foul message must report illegal tackle from behind. Got: '{foulMsg}'");
    }

    private static void TestM1_FairTackle_OutOfReach_AnnouncesDistance()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        string? announcedMsg = null;
        eventBus.OnMatchEvent += evt =>
        {
            if (evt.Type == MatchEventType.TacticalRadarInfo && evt.Description.Contains("بعيد"))
            {
                announcedMsg = evt.Description;
            }
        };

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        var tackler = match.ActiveUserPlayer;
        tackler.Position2D = new Vector2(50.0f, 30.0f);

        var opponent = awayTeam.Players[9];
        // Place opponent at 3.5m away (beyond both 1.8m standing and 2.8m slide)
        opponent.Position2D = new Vector2(53.5f, 30.0f);
        match.Ball.Position = new Vector3(53.5f, 30.0f, 0.0f);
        match.Ball.CatchOrTackle(opponent);

        // Stand tackle (reach 1.8m) -> Out of range!
        match.ExecuteStandTackle();

        Assert(announcedMsg != null, "Out-of-range tackle must announce numerical distance via TacticalRadarInfo");
        Assert(announcedMsg!.Contains("3.5"),
            $"Announcement must contain exact numerical distance '3.5'. Got: '{announcedMsg}'");
        Assert(!announcedMsg.Contains("اضغط"),
            $"Zero-clutter directive: Announcement must strictly omit tutorial prompts 'اضغط'. Got: '{announcedMsg}'");
    }

    private static void TestM1_HalfTime_PausesAt45AndSwapsSides()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        bool halfTimeEventFired = false;
        eventBus.OnMatchEvent += evt =>
        {
            if (evt.Type == MatchEventType.HalfTime)
            {
                halfTimeEventFired = true;
            }
        };

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        Assert(match.UserAttackingRight, "First half must attack Right");
        Assert(!match.HalfTimeReached, "HalfTimeReached must initially be false");
        Assert(match.State == MatchState.InPlay, "Match must start in InPlay state");

        // Set match time to 45.0m + stoppage time
        match.SetMatchTimeMinutes(45.0f + match.StoppageMinutes1st);
        match.Update(0.02f);

        Assert(match.HalfTimeReached, "HalfTimeReached must be true at end of first half stoppage time");
        Assert(match.State == MatchState.HalfTime, "State must pause at MatchState.HalfTime");
        Assert(halfTimeEventFired, "HalfTime event must be published on eventBus");
        Assert(!match.UserAttackingRight, "UserAttackingRight must swap to false for 2nd half");
        Assert(match.Ball.Position2D == match.Pitch.CenterSpot, "Ball must be placed at center spot for 2nd half kickoff");
    }

    private static void TestM1_HalfTime_SecondHalf_AttackLeft_MovementAndShooting()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        string? radarInfo = null;
        eventBus.OnMatchEvent += evt =>
        {
            if (evt.Type == MatchEventType.TacticalRadarInfo && evt.Description.Contains("المرمى"))
            {
                radarInfo = evt.Description;
            }
        };

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        match.TriggerHalfTime();
        match.ResumeMatch();

        Assert(!match.UserAttackingRight, "2nd half must attack Left");

        // Place user attacker with ball at X = 25.0m
        var attacker = match.ActiveUserPlayer;
        attacker.Position2D = new Vector2(25.0f, 34.0f);
        attacker.FacingDirection = new Vector2(-1.0f, 0.0f);
        match.Ball.Position = new Vector3(25.0f, 34.0f, 0.0f);
        match.Ball.CatchOrTackle(attacker);

        // 1. Tactical Radar: distance to opponent goal at X = 0 must report 25m (NOT 105 - 25 = 80m)
        match.ReportTacticalRadar();
        Assert(radarInfo != null, "Tactical radar info must be published");
        Assert(radarInfo!.Contains("25"), 
            $"In 2nd half, radar distance to opponent goal (X=0) must be 25m. Got: '{radarInfo}'");

        // 2. Shoot: W key must target Left goal (negative X velocity)
        const int VK_KEY_W = 0x57;
        inputHandler.HandleKeyDown(VK_KEY_W, isPossession: true);
        match.Update(0.02f);
        inputHandler.HandleKeyUp(VK_KEY_W);

        Assert(match.Ball.Velocity.X < -5.0f,
            $"In 2nd half, shot must target Left goal with negative X velocity. Got: {match.Ball.Velocity.X:F1}");
    }

    private static void TestM1_HalfTime_SecondHalf_InvertedZones_LeftGoalScoring()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, inputHandler);
        match.TriggerHalfTime();
        match.ResumeMatch();

        // 1. Verify inverted zones:
        // In 2nd half attacking left: X <= 35m is AttackingThird, X >= 70m is OwnThird
        // Clear ball ownership so ball position at (20, 34) is tested accurately
        if (match.Ball.Owner != null)
        {
            match.Ball.Owner.HasBall = false;
            match.Ball.Owner = null;
        }
        match.Ball.Position = new Vector3(20.0f, 34.0f, 0.0f);
        match.Update(0.02f);
        Assert(match.CurrentZone == PitchZone.AttackingThird,
            $"In 2nd half, X=20m must be AttackingThird. Got: {match.CurrentZone}");

        if (match.Ball.Owner != null)
        {
            match.Ball.Owner.HasBall = false;
            match.Ball.Owner = null;
        }
        match.Ball.Position = new Vector3(52.5f, 34.0f, 0.0f);
        match.Update(0.02f);
        Assert(match.CurrentZone == PitchZone.Midfield,
            $"In 2nd half, X=52.5m must be Midfield. Got: {match.CurrentZone}");

        if (match.Ball.Owner != null)
        {
            match.Ball.Owner.HasBall = false;
            match.Ball.Owner = null;
        }
        match.Ball.Position = new Vector3(85.0f, 34.0f, 0.0f);
        match.Update(0.02f);
        Assert(match.CurrentZone == PitchZone.OwnThird,
            $"In 2nd half, X=85m must be OwnThird. Got: {match.CurrentZone}");

        // 2. Verify Left goal scoring awards point to HomeTeam (User team)
        if (match.Ball.Owner != null)
        {
            match.Ball.Owner.HasBall = false;
            match.Ball.Owner = null;
        }
        match.Ball.Position = new Vector3(-0.5f, 34.0f, 0.0f);
        match.Ball.Velocity = new Vector3(-5.0f, 0.0f, 0.0f);
        match.Update(0.02f);

        Assert(match.HomeTeam.Score == 1,
            $"In 2nd half, ball entering Left goal must award goal to HomeTeam. HomeScore: {match.HomeTeam.Score}");
        Assert(match.AwayTeam.Score == 0,
            $"AwayScore must remain 0. AwayScore: {match.AwayTeam.Score}");
    }

    private class TestScreenReader : IScreenReader
    {
        public bool IsAvailable => true;
        public List<string> SpokenTexts { get; } = new();
        public void Speak(string text, bool interrupt = true) => SpokenTexts.Add(text);
        public void CancelSpeech() {}
    }
}

