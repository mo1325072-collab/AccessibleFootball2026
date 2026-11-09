using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using AccessibleFootball2026.App;
using AccessibleFootball2026.Audio;
using AccessibleFootball2026.Audio.Anthems;
using AccessibleFootball2026.Audio.Commentary;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Input;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;
using AccessibleFootball2026.Input;

namespace AccessibleFootball2026.Tests;

public static class Program
{
    private static int _passedCount = 0;
    private static int _failedCount = 0;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKeyEx(uint uCode, uint uMapType, IntPtr dwhkl);

    private const uint MAPVK_VK_TO_VSC = 0;
    private const uint MAPVK_VSC_TO_VK = 1;
    private const uint MAPVK_VK_TO_CHAR = 2;
    private const uint MAPVK_VSC_TO_VK_EX = 3;

    private static AccessibleFootballWindow CreateTestWindow(out KeyboardInputHandler inputHandler)
    {
        string baseAudioDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio");
        Directory.CreateDirectory(baseAudioDir);
        string anthemsDir = Path.Combine(baseAudioDir, "Anthems");
        Directory.CreateDirectory(anthemsDir);
        SoundEffectsPlayer.Initialize(baseAudioDir);

        var screenReader = new NvdaScreenReader();
        var eventBus = new MatchEventBus();
        inputHandler = new KeyboardInputHandler();
        var commentary = new CommentaryEngine(CommentatorId.RaoufKhlif, baseAudioDir);
        var coordinator = new SmartAudioCoordinator(screenReader);
        var anthem = new AnthemEngine(screenReader, anthemsDir);

        return new AccessibleFootballWindow(screenReader, eventBus, inputHandler, commentary, coordinator, anthem);
    }

    [STAThread]
    public static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("================================================================================");
        Console.WriteLine("   AccessibleFootball2026 — Milestone 1 Adversarial Verification Suite");
        Console.WriteLine("================================================================================");

        try
        {
            TestKeyEventArgsSuppressKeyPressMechanics();
            TestFormLevelMatchKeysSuppression();
            TestFormLevelMenuKeysSuppression();
            TestFormLevelUnassignedKeysBehavior();
            TestTabKeyFocusBehavior();
            TestWin32ArabicVsEnglishLayoutVirtualKeyCodes();
            TestKeyboardInputHandlerArabicAndEnglishEquivalence();
            TestKeyboardInputHandlerAutoRepeatSuppression();
            TestAccessibleFocusRetentionAcrossMatchLifecycle();
            TestGameLoopTelemetryUpdateDoesNotDisruptFocus();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FATAL UNHANDLED EXCEPTION]: {ex}");
            Console.ResetColor();
            _failedCount++;
        }

        Console.WriteLine("\n================================================================================");
        Console.WriteLine($"Test Results: {_passedCount} PASSED, {_failedCount} FAILED");
        Console.WriteLine("================================================================================");

        return _failedCount == 0 ? 0 : 1;
    }

    private static void Assert(bool condition, string testName, string details = "")
    {
        if (condition)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[PASS] ");
            Console.ResetColor();
            Console.WriteLine($"{testName} {(string.IsNullOrEmpty(details) ? "" : "— " + details)}");
            _passedCount++;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("[FAIL] ");
            Console.ResetColor();
            Console.WriteLine($"{testName} — FAILED! {details}");
            _failedCount++;
        }
    }

    // -------------------------------------------------------------------------
    // TEST 1: KeyEventArgs SuppressKeyPress & Handled mechanics
    // -------------------------------------------------------------------------
    private static void TestKeyEventArgsSuppressKeyPressMechanics()
    {
        Console.WriteLine("\n--- TEST 1: WinForms KeyEventArgs SuppressKeyPress Mechanics ---");

        var e = new KeyEventArgs(Keys.D);
        Assert(!e.SuppressKeyPress && !e.Handled, "KeyEventArgs default state", "SuppressKeyPress and Handled should start false.");

        e.SuppressKeyPress = true;
        Assert(e.SuppressKeyPress, "KeyEventArgs.SuppressKeyPress set", "SuppressKeyPress should be true.");
        Assert(e.Handled, "KeyEventArgs.Handled cascade", "In WinForms, setting SuppressKeyPress=true automatically sets Handled=true.");
    }

    // -------------------------------------------------------------------------
    // TEST 2A: Form-Level KeyDown & KeyUp suppression on all active match keys
    // -------------------------------------------------------------------------
    private static void TestFormLevelMatchKeysSuppression()
    {
        Console.WriteLine("\n--- TEST 2A: Form-Level KeyDown & KeyUp Suppression on Match Keys ---");

        Keys[] matchKeys = new[]
        {
            Keys.Up, Keys.Down, Keys.Left, Keys.Right,
            Keys.D, Keys.W, Keys.S, Keys.A,
            Keys.Space, Keys.C,
            Keys.ControlKey, Keys.LControlKey, Keys.RControlKey,
            Keys.ShiftKey, Keys.LShiftKey, Keys.RShiftKey
        };

        var startMatchMethod = typeof(AccessibleFootballWindow).GetMethod("StartInteractiveMatch", BindingFlags.NonPublic | BindingFlags.Instance);
        var onKeyDownMethod = typeof(AccessibleFootballWindow).GetMethod("OnWindowKeyDown", BindingFlags.NonPublic | BindingFlags.Instance);
        var onKeyUpMethod = typeof(AccessibleFootballWindow).GetMethod("OnWindowKeyUp", BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var key in matchKeys)
        {
            using var window = CreateTestWindow(out var inputHandler);
            startMatchMethod!.Invoke(window, new object[] { TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek() });

            var keDown = new KeyEventArgs(key);
            onKeyDownMethod!.Invoke(window, new object[] { window, keDown });
            Assert(keDown.SuppressKeyPress && keDown.Handled, $"Match KeyDown suppression for {key}",
                $"SuppressKeyPress={keDown.SuppressKeyPress}, Handled={keDown.Handled}");

            var keUp = new KeyEventArgs(key);
            onKeyUpMethod!.Invoke(window, new object[] { window, keUp });
            Assert(keUp.SuppressKeyPress && keUp.Handled, $"Match KeyUp suppression for {key}",
                $"SuppressKeyPress={keUp.SuppressKeyPress}, Handled={keUp.Handled}");
        }

        // Test Escape (pause trigger) during match
        {
            using var window = CreateTestWindow(out var inputHandler);
            startMatchMethod!.Invoke(window, new object[] { TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek() });

            var keEscape = new KeyEventArgs(Keys.Escape);
            onKeyDownMethod!.Invoke(window, new object[] { window, keEscape });
            Assert(keEscape.SuppressKeyPress && keEscape.Handled, "Match KeyDown suppression for Escape (Pause)",
                $"SuppressKeyPress={keEscape.SuppressKeyPress}, Handled={keEscape.Handled}");
        }

        // Test Q (end match trigger) during match
        {
            using var window = CreateTestWindow(out var inputHandler);
            startMatchMethod!.Invoke(window, new object[] { TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek() });

            var keQ = new KeyEventArgs(Keys.Q);
            onKeyDownMethod!.Invoke(window, new object[] { window, keQ });
            Assert(keQ.SuppressKeyPress && keQ.Handled, "Match KeyDown suppression for Q (End Match)",
                $"SuppressKeyPress={keQ.SuppressKeyPress}, Handled={keQ.Handled}");
        }
    }

    // -------------------------------------------------------------------------
    // TEST 2B: Form-Level KeyDown suppression on Menu keys
    // -------------------------------------------------------------------------
    private static void TestFormLevelMenuKeysSuppression()
    {
        Console.WriteLine("\n--- TEST 2B: Form-Level KeyDown Suppression on Menu Keys ---");

        using var window = CreateTestWindow(out var inputHandler);
        window.OpenMainMenu();

        var onKeyDownMethod = typeof(AccessibleFootballWindow).GetMethod("OnWindowKeyDown", BindingFlags.NonPublic | BindingFlags.Instance);

        Keys[] menuKeys = new[] { Keys.Up, Keys.Down, Keys.Left, Keys.Right, Keys.Space, Keys.Enter, Keys.Escape };
        foreach (var key in menuKeys)
        {
            var ke = new KeyEventArgs(key);
            onKeyDownMethod!.Invoke(window, new object[] { window, ke });
            Assert(ke.SuppressKeyPress && ke.Handled, $"Menu KeyDown suppression for {key}",
                $"SuppressKeyPress={ke.SuppressKeyPress}, Handled={ke.Handled}");
        }
    }

    // -------------------------------------------------------------------------
    // TEST 2C: Form-Level behavior on unassigned keys
    // -------------------------------------------------------------------------
    private static void TestFormLevelUnassignedKeysBehavior()
    {
        Console.WriteLine("\n--- TEST 2C: Form-Level Behavior on Unassigned Keys ---");

        var startMatchMethod = typeof(AccessibleFootballWindow).GetMethod("StartInteractiveMatch", BindingFlags.NonPublic | BindingFlags.Instance);
        var onKeyDownMethod = typeof(AccessibleFootballWindow).GetMethod("OnWindowKeyDown", BindingFlags.NonPublic | BindingFlags.Instance);

        using var window = CreateTestWindow(out var inputHandler);
        startMatchMethod!.Invoke(window, new object[] { TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek() });

        // During match, test unassigned keys like 'B', 'Z', 'F1'
        Keys[] unassignedKeys = new[] { Keys.B, Keys.Z, Keys.F1 };
        foreach (var key in unassignedKeys)
        {
            var ke = new KeyEventArgs(key);
            onKeyDownMethod!.Invoke(window, new object[] { window, ke });
            // Documenting current behavior: unassigned keys are currently unhandled
            bool isSuppressed = ke.SuppressKeyPress && ke.Handled;
            Console.WriteLine($"[INFO] Unassigned key {key} during match: SuppressKeyPress={ke.SuppressKeyPress}, Handled={ke.Handled}");
        }
    }

    // -------------------------------------------------------------------------
    // TEST 2D: Tab key focus behavior
    // -------------------------------------------------------------------------
    private static void TestTabKeyFocusBehavior()
    {
        Console.WriteLine("\n--- TEST 2D: Tab Key Focus Behavior ---");

        using var window = CreateTestWindow(out var inputHandler);
        window.Show();

        var displayListBoxField = typeof(AccessibleFootballWindow).GetField("_displayListBox", BindingFlags.NonPublic | BindingFlags.Instance);
        var listBox = (ListBox)displayListBoxField!.GetValue(window)!;

        var startMatchMethod = typeof(AccessibleFootballWindow).GetMethod("StartInteractiveMatch", BindingFlags.NonPublic | BindingFlags.Instance);
        startMatchMethod!.Invoke(window, new object[] { TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek() });

        Assert(listBox.Focused, "Before Tab: _displayListBox has focus");

        // Simulate Tab navigation on the Form
        window.SelectNextControl(listBox, forward: true, tabStopOnly: true, nested: true, wrap: true);
        Assert(listBox.Focused, "After Tab: _displayListBox retains focus (only TabStop control)");
    }

    // -------------------------------------------------------------------------
    // TEST 3: Win32 Arabic vs English layout Virtual Key Codes
    // -------------------------------------------------------------------------
    private static void TestWin32ArabicVsEnglishLayoutVirtualKeyCodes()
    {
        Console.WriteLine("\n--- TEST 3: Win32 Arabic (00000401) vs English (00000409) Layout Virtual Key Codes ---");

        // Load Arabic 101 and English US keyboard layouts
        IntPtr hklArabic = LoadKeyboardLayout("00000401", 0);
        IntPtr hklEnglish = LoadKeyboardLayout("00000409", 0);

        Console.WriteLine($"Arabic layout handle: 0x{hklArabic.ToInt64():X8}, English layout handle: 0x{hklEnglish.ToInt64():X8}");

        var scanCodeToExpectedVK = new Dictionary<uint, (uint expectedVk, string name)>
        {
            { 0x20, (0x44, "D / ي") },
            { 0x11, (0x57, "W / ص") },
            { 0x1F, (0x53, "S / س") },
            { 0x1E, (0x41, "A / ش") },
            { 0x2E, (0x43, "C / ؤ") },
            { 0x39, (0x20, "Space") },
            { 0x01, (0x1B, "Escape") },
            { 0x1D, (0x11, "Control") },
            { 0x2A, (0x10, "Shift") }
        };

        foreach (var kvp in scanCodeToExpectedVK)
        {
            uint scanCode = kvp.Key;
            uint expectedVk = kvp.Value.expectedVk;
            string keyName = kvp.Value.name;

            uint vkEnglish = (hklEnglish != IntPtr.Zero) 
                ? MapVirtualKeyEx(scanCode, MAPVK_VSC_TO_VK, hklEnglish) 
                : MapVirtualKeyEx(scanCode, MAPVK_VSC_TO_VK, IntPtr.Zero);

            uint vkArabic = (hklArabic != IntPtr.Zero) 
                ? MapVirtualKeyEx(scanCode, MAPVK_VSC_TO_VK, hklArabic) 
                : MapVirtualKeyEx(scanCode, MAPVK_VSC_TO_VK, IntPtr.Zero);

            Assert(vkEnglish == expectedVk, $"English VK mapping for {keyName}", $"ScanCode=0x{scanCode:X2} -> VK=0x{vkEnglish:X2} (expected 0x{expectedVk:X2})");
            Assert(vkArabic == expectedVk, $"Arabic VK mapping for {keyName}", $"ScanCode=0x{scanCode:X2} -> VK=0x{vkArabic:X2} (expected 0x{expectedVk:X2})");
            Assert(vkEnglish == vkArabic, $"Arabic vs English VK invariance for {keyName}", $"VK_EN=0x{vkEnglish:X2} == VK_AR=0x{vkArabic:X2}");
        }
    }

    // -------------------------------------------------------------------------
    // TEST 4: KeyboardInputHandler Arabic and English Virtual Key Equivalence
    // -------------------------------------------------------------------------
    private static void TestKeyboardInputHandlerArabicAndEnglishEquivalence()
    {
        Console.WriteLine("\n--- TEST 4: KeyboardInputHandler Virtual Key Equivalence ---");

        var handler = new KeyboardInputHandler();

        // 1. D key (VK 0x44) in possession -> ShortPass
        handler.Reset();
        handler.HandleKeyDown((int)Keys.D, isPossession: true);
        var actions = handler.PollCurrentActions(isPossession: true);
        Assert(actions.Contains(GameAction.ShortPass), "Key D in possession gives ShortPass");

        // 2. D key (VK 0x44) out of possession -> SlideTackle
        handler.Reset();
        handler.HandleKeyDown((int)Keys.D, isPossession: false);
        actions = handler.PollCurrentActions(isPossession: false);
        Assert(actions.Contains(GameAction.SlideTackle), "Key D out of possession gives SlideTackle");

        // 3. W key (VK 0x57) in possession -> Shoot
        handler.Reset();
        handler.HandleKeyDown((int)Keys.W, isPossession: true);
        actions = handler.PollCurrentActions(isPossession: true);
        Assert(actions.Contains(GameAction.Shoot), "Key W in possession gives Shoot");

        // 4. S key (VK 0x53) in possession -> Cross; out of possession -> StandTackle
        handler.Reset();
        handler.HandleKeyDown((int)Keys.S, isPossession: true);
        actions = handler.PollCurrentActions(isPossession: true);
        Assert(actions.Contains(GameAction.Cross), "Key S in possession gives Cross");

        handler.Reset();
        handler.HandleKeyDown((int)Keys.S, isPossession: false);
        actions = handler.PollCurrentActions(isPossession: false);
        Assert(actions.Contains(GameAction.StandTackle), "Key S out of possession gives StandTackle");

        // 5. A key (VK 0x41) in possession -> ThroughBall
        handler.Reset();
        handler.HandleKeyDown((int)Keys.A, isPossession: true);
        actions = handler.PollCurrentActions(isPossession: true);
        Assert(actions.Contains(GameAction.ThroughBall), "Key A in possession gives ThroughBall");

        // 6. Switch player variants (VK_CONTROL, VK_LCONTROL, VK_RCONTROL)
        foreach (var ctrlKey in new[] { (int)Keys.ControlKey, (int)Keys.LControlKey, (int)Keys.RControlKey })
        {
            handler.Reset();
            handler.HandleKeyDown(ctrlKey, isPossession: false);
            actions = handler.PollCurrentActions(isPossession: false);
            Assert(actions.Contains(GameAction.SwitchPlayer), $"ControlKey variant 0x{ctrlKey:X2} triggers SwitchPlayer");
        }
    }

    // -------------------------------------------------------------------------
    // TEST 5: Auto-repeat suppression on discrete actions
    // -------------------------------------------------------------------------
    private static void TestKeyboardInputHandlerAutoRepeatSuppression()
    {
        Console.WriteLine("\n--- TEST 5: Auto-repeat & Holding Stress Testing ---");

        var handler = new KeyboardInputHandler();

        // SCENARIO 1: Holding 'D' to Pass, then possession transitions to false
        handler.Reset();
        handler.HandleKeyDown((int)Keys.D, isPossession: true);

        // Frame 1: Consumes pass
        var frame1 = handler.PollCurrentActions(isPossession: true);
        Assert(frame1.Contains(GameAction.ShortPass), "Frame 1: ShortPass fired on rising edge");

        // Frame 2..10: User still holds 'D'. Windows sends repeated KeyDown events due to typematic repeat.
        // Possession flipped to false because ball was kicked.
        for (int i = 2; i <= 10; i++)
        {
            handler.HandleKeyDown((int)Keys.D, isPossession: false);
            var frameN = handler.PollCurrentActions(isPossession: false);
            Assert(!frameN.Contains(GameAction.SlideTackle) && !frameN.Contains(GameAction.ShortPass),
                $"Frame {i}: Suppressed auto-repeat while holding D (No SlideTackle)");
        }

        // KeyUp on D
        handler.HandleKeyUp((int)Keys.D);

        // Subsequent press triggers slide tackle now that key was released
        handler.HandleKeyDown((int)Keys.D, isPossession: false);
        var afterRelease = handler.PollCurrentActions(isPossession: false);
        Assert(afterRelease.Contains(GameAction.SlideTackle), "Press after release correctly triggers SlideTackle");

        // SCENARIO 2: Holding Ctrl to switch player does not rapid-fire across frames
        handler.Reset();
        handler.HandleKeyDown((int)Keys.ControlKey, isPossession: false);
        var ctrlFrame1 = handler.PollCurrentActions(isPossession: false);
        Assert(ctrlFrame1.Contains(GameAction.SwitchPlayer), "Ctrl Frame 1: SwitchPlayer fires once");

        for (int i = 2; i <= 5; i++)
        {
            handler.HandleKeyDown((int)Keys.ControlKey, isPossession: false);
            var ctrlFrameN = handler.PollCurrentActions(isPossession: false);
            Assert(!ctrlFrameN.Contains(GameAction.SwitchPlayer), $"Ctrl Frame {i}: SwitchPlayer auto-repeat suppressed");
        }

        // SCENARIO 3: Continuous keys (Arrows & Shift) DO NOT get consumed
        handler.Reset();
        handler.HandleKeyDown((int)Keys.Up, isPossession: true);
        handler.HandleKeyDown((int)Keys.ShiftKey, isPossession: true);

        for (int i = 1; i <= 5; i++)
        {
            var moveFrame = handler.PollCurrentActions(isPossession: true);
            Assert(moveFrame.Contains(GameAction.MoveUp) && moveFrame.Contains(GameAction.Sprint),
                $"Continuous Frame {i}: MoveUp and Sprint remain active while held");
        }

        handler.HandleKeyUp((int)Keys.Up);
        handler.HandleKeyUp((int)Keys.ShiftKey);
        var moveAfterKeyUp = handler.PollCurrentActions(isPossession: true);
        Assert(!moveAfterKeyUp.Contains(GameAction.MoveUp) && !moveAfterKeyUp.Contains(GameAction.Sprint),
            "MoveUp and Sprint cease immediately upon KeyUp");
    }

    // -------------------------------------------------------------------------
    // TEST 6: Accessible focus retention across match lifecycle
    // -------------------------------------------------------------------------
    private static void TestAccessibleFocusRetentionAcrossMatchLifecycle()
    {
        Console.WriteLine("\n--- TEST 6: Accessible Focus Retention across Match Lifecycle ---");

        using var window = CreateTestWindow(out var inputHandler);
        window.Show(); // Make form visible to allow focus

        var displayListBoxField = typeof(AccessibleFootballWindow).GetField("_displayListBox", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert(displayListBoxField != null, "_displayListBox field lookup");
        var listBox = (ListBox)displayListBoxField!.GetValue(window)!;
        Assert(listBox != null, "_displayListBox instance exists");

        // PHASE 1: Initial Menu
        window.OpenMainMenu();
        Assert(listBox.Visible, "Phase 1 (Menu): _displayListBox.Visible == true");
        Assert(listBox.Enabled, "Phase 1 (Menu): _displayListBox.Enabled == true");
        Assert(listBox.Items.Count > 0, $"Phase 1 (Menu): Items populated ({listBox.Items.Count} items)");
        Assert(listBox.SelectedIndex >= 0, $"Phase 1 (Menu): SelectedIndex={listBox.SelectedIndex} (NVDA has focused item)");

        // PHASE 2: Start Match
        var homeTeam = TeamDatabase.CreateRealMadrid();
        var awayTeam = TeamDatabase.CreateBarcelona();
        var startMatchMethod = typeof(AccessibleFootballWindow).GetMethod("StartInteractiveMatch", BindingFlags.NonPublic | BindingFlags.Instance);
        startMatchMethod!.Invoke(window, new object[] { homeTeam, awayTeam });

        Assert(listBox.Visible, "Phase 2 (Match Start): _displayListBox.Visible == true");
        Assert(listBox.Enabled, "Phase 2 (Match Start): _displayListBox.Enabled == true");
        Assert(listBox.Items.Count >= 4, $"Phase 2 (Match Start): Items count >= 4 (actual {listBox.Items.Count})");
        Assert(listBox.SelectedIndex == 0, $"Phase 2 (Match Start): SelectedIndex={listBox.SelectedIndex}");
        Assert(listBox.Focused, "Phase 2 (Match Start): _displayListBox.Focused == true (Focus anchored)");

        // PHASE 3: Pause Match via Escape
        var onKeyDownMethod = typeof(AccessibleFootballWindow).GetMethod("OnWindowKeyDown", BindingFlags.NonPublic | BindingFlags.Instance);
        var escDown = new KeyEventArgs(Keys.Escape);
        onKeyDownMethod!.Invoke(window, new object[] { window, escDown });

        Assert(listBox.Visible, "Phase 3 (Pause Menu): _displayListBox.Visible == true");
        Assert(listBox.Enabled, "Phase 3 (Pause Menu): _displayListBox.Enabled == true");
        Assert(listBox.Items.Count > 0, $"Phase 3 (Pause Menu): Pause items populated ({listBox.Items.Count} items)");
        Assert(listBox.SelectedIndex >= 0, $"Phase 3 (Pause Menu): SelectedIndex={listBox.SelectedIndex}");

        // PHASE 4: Resume Match via Escape in pause menu
        var escResume = new KeyEventArgs(Keys.Escape);
        onKeyDownMethod!.Invoke(window, new object[] { window, escResume });

        Assert(listBox.Visible, "Phase 4 (Resume Match): _displayListBox.Visible == true");
        Assert(listBox.Enabled, "Phase 4 (Resume Match): _displayListBox.Enabled == true");
        Assert(listBox.Items.Count >= 4, $"Phase 4 (Resume Match): Match telemetry restored ({listBox.Items.Count} items)");
        Assert(listBox.SelectedIndex == 0, $"Phase 4 (Resume Match): SelectedIndex={listBox.SelectedIndex}");
        Assert(listBox.Focused, "Phase 4 (Resume Match): _displayListBox.Focused == true");

        // PHASE 5: Return to Menu via Q
        var qDown = new KeyEventArgs(Keys.Q);
        onKeyDownMethod!.Invoke(window, new object[] { window, qDown });

        Assert(listBox.Visible, "Phase 5 (Returned to Menu): _displayListBox.Visible == true");
        Assert(listBox.Enabled, "Phase 5 (Returned to Menu): _displayListBox.Enabled == true");
        Assert(listBox.Items.Count > 0, $"Phase 5 (Returned to Menu): Main menu items restored ({listBox.Items.Count} items)");
    }

    // -------------------------------------------------------------------------
    // TEST 7: Game loop 50Hz telemetry updates do not disrupt focus or selection
    // -------------------------------------------------------------------------
    private static void TestGameLoopTelemetryUpdateDoesNotDisruptFocus()
    {
        Console.WriteLine("\n--- TEST 7: Game Loop Telemetry Update Focus Stability ---");

        using var window = CreateTestWindow(out var inputHandler);
        window.Show();

        var displayListBoxField = typeof(AccessibleFootballWindow).GetField("_displayListBox", BindingFlags.NonPublic | BindingFlags.Instance);
        var listBox = (ListBox)displayListBoxField!.GetValue(window)!;

        var homeTeam = TeamDatabase.CreateLiverpool();
        var awayTeam = TeamDatabase.CreateManchesterCity();
        var startMatchMethod = typeof(AccessibleFootballWindow).GetMethod("StartInteractiveMatch", BindingFlags.NonPublic | BindingFlags.Instance);
        startMatchMethod!.Invoke(window, new object[] { homeTeam, awayTeam });

        var activeMatchField = typeof(AccessibleFootballWindow).GetField("_activeMatch", BindingFlags.NonPublic | BindingFlags.Instance);
        var match = (MatchEngine)activeMatchField!.GetValue(window)!;

        // Simulate 100 frames (2 seconds at 50Hz)
        for (int frame = 0; frame < 100; frame++)
        {
            match.Update(0.02f);

            // Emulate what _gameLoopTimer.Tick does
            string matchStatus = $"المباراة جارية (الدقيقة {(int)match.MatchTimeMinutes}): {match.HomeTeam.Name} {match.HomeTeam.Score} - {match.AwayTeam.Score} {match.AwayTeam.Name}";
            if (listBox.Items.Count > 0 && (string)listBox.Items[0] != matchStatus)
            {
                listBox.Items[0] = matchStatus;
            }
        }

        Assert(listBox.Visible, "After 100 ticks: _displayListBox remains Visible");
        Assert(listBox.Enabled, "After 100 ticks: _displayListBox remains Enabled");
        Assert(listBox.SelectedIndex == 0, $"After 100 ticks: SelectedIndex remains {listBox.SelectedIndex}");
        Assert(listBox.Focused, "After 100 ticks: _displayListBox remains Focused");
        Assert(((string)listBox.Items[0]).Contains("المباراة جارية"), "Telemetry updated properly in-place without rebuilding ListBox");

        // Clean up match
        var endMatchMethod = typeof(AccessibleFootballWindow).GetMethod("EndMatchAndReturnToMenu", BindingFlags.NonPublic | BindingFlags.Instance);
        endMatchMethod!.Invoke(window, null);
    }
}
