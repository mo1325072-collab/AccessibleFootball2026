using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text.Json;
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

        var screenReader = new RecordingReader();
        var eventBus = new MatchEventBus();
        inputHandler = new KeyboardInputHandler();
        var commentary = new CommentaryEngine(CommentatorId.RaoufKhlif, baseAudioDir);
        var coordinator = new SmartAudioCoordinator(screenReader);
        var anthem = new AnthemEngine(screenReader, anthemsDir);

        var window = new AccessibleFootballWindow(screenReader, eventBus, inputHandler, commentary, coordinator, anthem);
        typeof(AccessibleFootballWindow).GetField("_isInStartupIntro", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(window, false);
        window.Shown += (_, _) =>
        {
            typeof(AccessibleFootballWindow).GetField("_isInStartupIntro", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(window, false);
            window.OpenMainMenu();
        };
        window.Disposed += (_, _) =>
        {
            var timer = typeof(AccessibleFootballWindow).GetField("_gameLoopTimer", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(window) as System.Windows.Forms.Timer;
            timer?.Stop(); timer?.Dispose();
            commentary.Dispose();
        };
        return window;
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
            TestPreMatchLineupGate();
            TestBilingualSettingsPreserveControls();
            TestSettingsSaveReloadCancelAndFailure();
            TestInjuryChoiceWindowRouting();
            TestPauseSubmenuDoesNotStartMenuMusic();
            TestTerminalMatchRejectsPauseMenu();
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

    private sealed class RecordingReader : IScreenReader
    {
        public bool IsAvailable => true;
        public List<string> Spoken { get; } = new();
        public void Speak(string text, bool interrupt = true) => Spoken.Add(text);
        public void CancelSpeech() { }
        public System.Threading.Tasks.Task SpeakUntilFinishedAsync(string text, bool interrupt = true)
        { Speak(text, interrupt); return System.Threading.Tasks.Task.CompletedTask; }
    }

    private static void StartFixtureMatch(AccessibleFootballWindow window, Team home, Team away, bool finishLineup=true)
    {
        Field<AccessibleFootball2026.Audio.SFX.StadiumSoundEngine>(window,"_stadiumSoundEngine").ActiveStadiumKey="SantiagoBernabeu";
        typeof(AccessibleFootballWindow).GetMethod("StartInteractiveMatch",BindingFlags.NonPublic|BindingFlags.Instance)!
            .Invoke(window,new object[]{home,away,false,false,true});
        if(finishLineup)typeof(AccessibleFootballWindow).GetMethod("CompleteMatchLineup",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(window,null);
    }

    private static void TestPreMatchLineupGate()
    {
        var commands=new List<string>();string mode="playing";
        MciPlayback.TestCommand=(command,buffer)=>{commands.Add(command);buffer?.Append(mode);return 0;};
        try
        {
            foreach(var skipKey in new[]{Keys.Enter,Keys.Space})
            {
                using var window=CreateTestWindow(out var input);window.Show();
                StartFixtureMatch(window,TeamDatabase.CreateRealMadrid(),TeamDatabase.CreateBarcelona(),false);
                var timer=Field<System.Windows.Forms.Timer>(window,"_gameLoopTimer");timer.Stop();
                var gate=Field<AccessibleFootball2026.Core.Input.MatchLineupInput>(window,"_matchLineup");
                var match=Field<MatchEngine>(window,"_activeMatch");float before=match.MatchTimeMinutes;
                var tick=typeof(System.Windows.Forms.Timer).GetMethod("OnTick",BindingFlags.NonPublic|BindingFlags.Instance)!;
                // Real recorded opening and music may be absent from a minimal native fixture.
                // Keep the real timer's completion gate pending through the real commentary queue.
                var engine=Field<CommentaryEngine>(window,"_commentaryEngine");
                typeof(CommentaryEngine).GetField("_playbackSuspended",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(engine,true);
                tick.Invoke(timer,new object[]{EventArgs.Empty});
                Assert(gate.IsActive&&match.MatchTimeMinutes==before,"Real lineup timer freezes simulation before skip "+skipKey);
                var down=typeof(AccessibleFootballWindow).GetMethod("OnWindowKeyDown",BindingFlags.NonPublic|BindingFlags.Instance)!;
                var up=typeof(AccessibleFootballWindow).GetMethod("OnWindowKeyUp",BindingFlags.NonPublic|BindingFlags.Instance)!;
                var shot=new KeyEventArgs(Keys.W);down.Invoke(window,new object[]{window,shot});
                Assert(shot.Handled&&!shot.SuppressKeyPress&&gate.IsActive,"Lineup consumes gameplay keys without starting match");
                commands.Clear();var skip=new KeyEventArgs(skipKey);down.Invoke(window,new object[]{window,skip});
                Assert(!gate.IsActive&&skip.Handled&&commands.Contains("stop lineup_music")&&commands.Contains("close lineup_music")&&commands.Contains("stop comm_voice"),"Native skip closes lineup music/commentary before gameplay "+skipKey);
                int close=commands.IndexOf("close lineup_music"),whistle=commands.FindIndex(c=>c=="play whistle from 0");
                Assert(whistle<0||close<whistle,"Lineup audio closes before kickoff whistle");
                var held=new KeyEventArgs(skipKey);down.Invoke(window,new object[]{window,held});
                Assert(held.Handled&&!held.SuppressKeyPress,"Held skip repeat cannot enter gameplay/radar");
                up.Invoke(window,new object[]{window,new KeyEventArgs(skipKey)});
                var list=Field<ListBox>(window,"_displayListBox");Assert(list.Visible&&list.Enabled&&list.Focused,"Lineup completion restores live focus");
                timer.Stop();
            }
            using(var natural=CreateTestWindow(out var input))
            {
                natural.Show();mode="stopped";StartFixtureMatch(natural,TeamDatabase.CreateRealMadrid(),TeamDatabase.CreateBarcelona(),false);
                var timer=Field<System.Windows.Forms.Timer>(natural,"_gameLoopTimer");timer.Stop();
                var engine=Field<CommentaryEngine>(natural,"_commentaryEngine");engine.EndPlaybackScope();engine.ResumePlayback();
                typeof(System.Windows.Forms.Timer).GetMethod("OnTick",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(timer,new object[]{EventArgs.Empty});
                Assert(!Field<AccessibleFootball2026.Core.Input.MatchLineupInput>(natural,"_matchLineup").IsActive,"Real native timer completes lineup naturally when both channels finish");
            }
        }
        finally { MciPlayback.TestCommand=null; }
    }

    private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(target)!;

    private static void TestBilingualSettingsPreserveControls()
    {
        var previous = MatchSettings.Current.Language;
        try
        {
            MatchSettings.Current.Language = UiLanguage.Arabic;
            using var form = new AccessibleSettingsForm(new RecordingReader());
            form.Show();
            var language = Field<ComboBox>(form, "_cmbLanguage");
            var difficulty = Field<ComboBox>(form, "_cmbDifficulty");
            var duration = Field<ComboBox>(form, "_cmbDuration");
            var voice = Field<ComboBox>(form, "_cmbCommentaryVoice");
            var volume = Field<TrackBar>(form, "_tbMusicVolume");
            difficulty.SelectedIndex = 3; duration.SelectedIndex = 2; voice.SelectedIndex = 2; volume.Value = 37;
            language.SelectedIndex = 1;
            Assert(form.Text == "Game & Audio Settings", "Changing language relabels the current native settings dialog");
            Assert(difficulty.SelectedIndex == 3 && difficulty.SelectedItem?.ToString() == "Legendary", "Language change preserves Legendary selection");
            Assert(duration.SelectedIndex == 2 && voice.SelectedIndex == 2 && volume.Value == 37, "Language change retains unsaved duration, commentator and volume selections");
            Assert(Field<Button>(form, "_btnSave").Text == "Save Settings" && Field<TrackBar>(form,"_tbSfxVolume").AccessibleName == "SFX Volume", "Native buttons and accessible slider names switch language");
            language.SelectedIndex = 0;
            Assert(form.Text == "إعدادات اللعبة والصوت" && difficulty.SelectedIndex == 3, "Switching back restores Arabic labels without resetting controls");
            form.Close();
        }
        finally { MatchSettings.Current.Language = previous; }
    }

    private static void TestSettingsSaveReloadCancelAndFailure()
    {
        var backup = JsonSerializer.Deserialize<MatchSettings>(JsonSerializer.Serialize(MatchSettings.Current))!;
        string oldPath = MatchSettings.CustomSettingsFilePath;
        string directory = Path.Combine(Path.GetTempPath(), "native-settings-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        MatchSettings.CustomSettingsFilePath = Path.Combine(directory, "settings.json");
        try
        {
            var reader = new RecordingReader();
            using (var form = new AccessibleSettingsForm(reader))
            {
                form.Show();
                foreach (var name in new[] {"_tbMasterVolume", "_tbSfxVolume", "_tbCommentaryVolume", "_tbCrowdVolume", "_tbBeaconVolume", "_tbMusicVolume"}) Field<TrackBar>(form, name).Value = 0;
                Field<ComboBox>(form, "_cmbDifficulty").SelectedIndex = 3;
                Field<ComboBox>(form, "_cmbDuration").SelectedIndex = 4;
                Field<ComboBox>(form, "_cmbCommentaryVoice").SelectedIndex = 2;
                Field<ComboBox>(form, "_cmbLanguage").SelectedIndex = 1;
                foreach(var name in new[]{"_chkExtraTime","_chkPenalties","_chkAutoSubs","_chkAutoTactics","_chkAiVar"}) Field<CheckBox>(form,name).Checked = false;
                Field<Button>(form, "_btnSave").PerformClick();
                Assert(!form.Visible && File.Exists(MatchSettings.ActiveSettingsFilePath), "Native Save closes only after a persistent write");
            }
            MatchSettings.Current.ResetToDefaults(); MatchSettings.Load();
            using(var reopened = new AccessibleSettingsForm(reader))
            {
                reopened.Show();
                Assert(Field<ComboBox>(reopened,"_cmbDifficulty").SelectedIndex==3 && Field<ComboBox>(reopened,"_cmbDuration").SelectedIndex==4 && Field<ComboBox>(reopened,"_cmbCommentaryVoice").SelectedIndex==2, "Reopened native dialog retains saved Legendary, duration and commentary Off");
                Assert(new[]{"_tbMasterVolume","_tbSfxVolume","_tbCommentaryVolume","_tbCrowdVolume","_tbBeaconVolume","_tbMusicVolume"}.All(n=>Field<TrackBar>(reopened,n).Value==0), "Reopened native dialog retains zero on every volume slider");
                Assert(new[]{"_chkExtraTime","_chkPenalties","_chkAutoSubs","_chkAutoTactics","_chkAiVar"}.All(n=>!Field<CheckBox>(reopened,n).Checked), "All saved native checkbox choices survive reopening");
                Field<TrackBar>(reopened,"_tbMasterVolume").Value=73;
                Field<ComboBox>(reopened,"_cmbLanguage").SelectedIndex=0;
                reopened.Close();
            }
            Assert(MatchSettings.Current.MasterVolumePercent==0 && MatchSettings.Current.Language==UiLanguage.English && SoundEffectsPlayer.MasterVolumeSetting==0, "Close without Save cancels previews in settings and playback consumers");
            string blocker=Path.Combine(directory,"file");File.WriteAllText(blocker,"fixture");MatchSettings.CustomSettingsFilePath=Path.Combine(blocker,"settings.json");
            using(var failed = new AccessibleSettingsForm(reader))
            {
                failed.Show();Field<TrackBar>(failed,"_tbMasterVolume").Value=45;int before=reader.Spoken.Count;Field<Button>(failed,"_btnSave").PerformClick();
                Assert(failed.Visible && reader.Spoken.Skip(before).Any(t=>t.Contains("could not be saved")) && !reader.Spoken.Skip(before).Any(t=>t.Contains("successfully saved")), "Failed native Save stays open and announces failure, never success");failed.Close();
            }
        }
        finally { MatchSettings.CustomSettingsFilePath=oldPath;MatchSettings.Current.CopyFrom(backup);MatchSettings.Current.ApplyLive(); }
    }

    private static void TestPauseSubmenuDoesNotStartMenuMusic()
    {
        var commands = new List<string>();
        MciPlayback.TestCommand = (command, buffer) => { commands.Add(command); buffer?.Append("stopped"); return 0; };
        try
        {
            using var window = CreateTestWindow(out var input);
            window.Show();
            StartFixtureMatch(window, TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek());
            var timer = Field<System.Windows.Forms.Timer>(window, "_gameLoopTimer"); timer.Stop();
            var key = typeof(AccessibleFootballWindow).GetMethod("OnWindowKeyDown", BindingFlags.NonPublic | BindingFlags.Instance)!;
            key.Invoke(window, new object[] { window, new KeyEventArgs(Keys.Escape) });
            var pause = Field<AccessibleFootball2026.App.Menus.AccessibleMenu>(window, "_currentMenu");
            window.PushMenu(new AccessibleFootball2026.App.Menus.AccessibleMenu("Nested pause test", new RecordingReader()));
            commands.Clear();
            key.Invoke(window, new object[] { window, new KeyEventArgs(Keys.Escape) });
            Assert(ReferenceEquals(pause, Field<AccessibleFootball2026.App.Menus.AccessibleMenu>(window, "_currentMenu")), "Back from nested pause returns to the actual pause root");
            Assert(!commands.Any(c => c.StartsWith("play bgm_menu")), "Back from nested pause never starts menu music or stops match crowd");
            key.Invoke(window, new object[] { window, new KeyEventArgs(Keys.Escape) }); timer.Stop();
            Assert(Field<MatchEngine>(window, "_activeMatch").State == MatchState.InPlay && !commands.Any(c => c.StartsWith("play bgm_menu")), "Resume after nested pause stays in match audio context");
        }
        finally { MciPlayback.TestCommand = null; }
    }

    private static void TestTerminalMatchRejectsPauseMenu()
    {
        using var window = CreateTestWindow(out var input);
        window.Show();
        StartFixtureMatch(window, TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek());
        var match = Field<MatchEngine>(window, "_activeMatch");
        var timer = Field<System.Windows.Forms.Timer>(window, "_gameLoopTimer"); timer.Stop();
        typeof(MatchEngine).GetProperty("State")!.SetValue(match, MatchState.GameOver);
        var key = typeof(AccessibleFootballWindow).GetMethod("OnWindowKeyDown", BindingFlags.NonPublic | BindingFlags.Instance)!;
        foreach (var code in new[] { Keys.Escape, Keys.Enter, Keys.D })
        {
            var e = new KeyEventArgs(code); key.Invoke(window, new object[] { window, e });
            Assert(e.Handled && !e.SuppressKeyPress && match.State == MatchState.GameOver && !timer.Enabled && typeof(AccessibleFootballWindow).GetField("_currentMenu", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(window) == null,
                "Terminal handoff rejects pause/field key " + code);
        }
    }

    private static void TestInjuryChoiceWindowRouting()
    {
        using var window=CreateTestWindow(out var input);
        window.Show();
        var start=typeof(AccessibleFootballWindow).GetMethod("StartInteractiveMatch",BindingFlags.NonPublic|BindingFlags.Instance)!;
        StartFixtureMatch(window, TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek());
        var match=Field<MatchEngine>(window,"_activeMatch");var timer=Field<System.Windows.Forms.Timer>(window,"_gameLoopTimer");timer.Stop();
        var injured=match.HomeTeam.Players[9];match.HomeTeam.Bench.Clear();
        var first=new Player(9501,"First reserve",51,SpecificPosition.ST,99,99,99,99,99);var chosen=new Player(9502,"Chosen reserve",52,SpecificPosition.ST,60,60,60,60,60);match.HomeTeam.Bench.AddRange(new[]{first,chosen});
        match.ApplyInjury(injured,PlayerInjuryStatus.SevereInjury);
        // Invoke the real production timer callback, which must open the pending injury menu.
        typeof(System.Windows.Forms.Timer).GetMethod("OnTick",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(timer,new object[]{EventArgs.Empty});
        var menu=Field<AccessibleFootball2026.App.Menus.AccessibleMenu>(window,"_currentMenu");
        Assert(menu.MenuTitle.Contains(injured.Name) && !timer.Enabled && match.HomeTeam.SubstitutionsUsed==0, "Real timer opens named injury menu before any automatic replacement and stops ticking");
        var key=typeof(AccessibleFootballWindow).GetMethod("OnWindowKeyDown",BindingFlags.NonPublic|BindingFlags.Instance)!;
        key.Invoke(window,new object[]{window,new KeyEventArgs(Keys.Escape)});
        Assert(match.PendingInjuryPlayer==injured && ReferenceEquals(menu,Field<AccessibleFootball2026.App.Menus.AccessibleMenu>(window,"_currentMenu")), "Escape cannot dismiss injury choice and resume an injured player");
        key.Invoke(window,new object[]{window,new KeyEventArgs(Keys.Down)});key.Invoke(window,new object[]{window,new KeyEventArgs(Keys.Enter)});timer.Stop();
        Assert(match.PendingInjuryPlayer==null && match.HomeTeam.Players.Contains(chosen) && match.HomeTeam.Bench.Contains(first), "Native Down/Enter path commits the chosen reserve, not strongest-first");
        var list=Field<ListBox>(window,"_displayListBox");Assert(list.Visible && list.Enabled && list.Focused, "Match list focus is restored after the manual injury choice");
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
            Keys.D, Keys.W, Keys.S, Keys.A, Keys.Q, Keys.K, Keys.B, Keys.N, Keys.T, Keys.R, Keys.H, Keys.J, Keys.V, Keys.L, Keys.F,
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
            StartFixtureMatch(window, TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek());

            var keDown = new KeyEventArgs(key);
            onKeyDownMethod!.Invoke(window, new object[] { window, keDown });
            Assert(!keDown.SuppressKeyPress && keDown.Handled, $"Match KeyDown consumes press but permits release for {key}",
                $"SuppressKeyPress={keDown.SuppressKeyPress}, Handled={keDown.Handled}");

            var keUp = new KeyEventArgs(key);
            onKeyUpMethod!.Invoke(window, new object[] { window, keUp });
            Assert(!keUp.SuppressKeyPress && keUp.Handled, $"Match KeyUp release routing for {key}",
                $"SuppressKeyPress={keUp.SuppressKeyPress}, Handled={keUp.Handled}");
        }

        // Test Escape (pause trigger) during match
        {
            using var window = CreateTestWindow(out var inputHandler);
            StartFixtureMatch(window, TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek());

            var keEscape = new KeyEventArgs(Keys.Escape);
            onKeyDownMethod!.Invoke(window, new object[] { window, keEscape });
            Assert(keEscape.SuppressKeyPress && keEscape.Handled, "Match KeyDown suppression for Escape (Pause)",
                $"SuppressKeyPress={keEscape.SuppressKeyPress}, Handled={keEscape.Handled}");
        }

        // Q is the lofted through-ball command, not end-match.
        {
            using var window = CreateTestWindow(out var inputHandler);
            StartFixtureMatch(window, TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek());

            var keQ = new KeyEventArgs(Keys.Q);
            onKeyDownMethod!.Invoke(window, new object[] { window, keQ });
            Assert(keQ.SuppressKeyPress && keQ.Handled, "Match KeyDown suppression for Q (Lofted Through Ball)",
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
            // A no-op item avoids entering a modal dialog or changing modes during a suppression test.
            var testMenu = new AccessibleFootball2026.App.Menus.AccessibleMenu("Key routing", new RecordingReader());
            testMenu.AddItem("No-op", () => {});
            window.SetActiveMenu(testMenu);
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
        StartFixtureMatch(window, TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek());

        // B is assigned to low cross. Z and F1 remain unassigned.
        Keys[] unassignedKeys = new[] { Keys.Z, Keys.F1 };
        foreach (var key in unassignedKeys)
        {
            var ke = new KeyEventArgs(key);
            onKeyDownMethod!.Invoke(window, new object[] { window, ke });
            // Documenting current behavior: unassigned keys are currently unhandled
            Assert(!ke.SuppressKeyPress && !ke.Handled, $"Unassigned key {key} is not consumed");
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
        var listBox = (ListBox)(displayListBoxField!.GetValue(window) ?? throw new InvalidOperationException("Missing display list"));

        var startMatchMethod = typeof(AccessibleFootballWindow).GetMethod("StartInteractiveMatch", BindingFlags.NonPublic | BindingFlags.Instance);
        StartFixtureMatch(window, TeamDatabase.CreateAlAhly(), TeamDatabase.CreateZamalek());

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
        Assert(hklArabic != IntPtr.Zero && hklEnglish != IntPtr.Zero, "Both requested keyboard layouts loaded, not silently replaced by current layout");

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

        // 1. S key in possession -> ShortPass
        handler.Reset();
        handler.HandleKeyDown((int)Keys.S, isPossession: true);
        var actions = handler.PollCurrentActions(isPossession: true);
        Assert(actions.Contains(GameAction.ShortPass), "Key S in possession gives ShortPass");

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

        // 4. D crosses in possession; S is standing tackle out of possession.
        handler.Reset();
        handler.HandleKeyDown((int)Keys.D, isPossession: true);
        actions = handler.PollCurrentActions(isPossession: true);
        Assert(actions.Contains(GameAction.Cross), "Key D in possession gives Cross");

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

        // SCENARIO 1: Holding 'S' to Pass, then possession transitions to false
        handler.Reset();
        handler.HandleKeyDown((int)Keys.S, isPossession: true);

        // Frame 1: Consumes pass
        var frame1 = handler.PollCurrentActions(isPossession: true);
        Assert(frame1.Contains(GameAction.ShortPass), "Frame 1: ShortPass fired on rising edge");

        // Frame 2..10: User still holds 'D'. Windows sends repeated KeyDown events due to typematic repeat.
        // Possession flipped to false because ball was kicked.
        for (int i = 2; i <= 10; i++)
        {
            handler.HandleKeyDown((int)Keys.S, isPossession: false);
            var frameN = handler.PollCurrentActions(isPossession: false);
            Assert(!frameN.Contains(GameAction.StandTackle) && !frameN.Contains(GameAction.ShortPass),
                $"Frame {i}: Suppressed auto-repeat while holding S (No StandTackle)");
        }

        // KeyUp on S
        handler.HandleKeyUp((int)Keys.S);

        // Subsequent press triggers standing tackle now that key was released
        handler.HandleKeyDown((int)Keys.S, isPossession: false);
        var afterRelease = handler.PollCurrentActions(isPossession: false);
        Assert(afterRelease.Contains(GameAction.StandTackle), "Press after release correctly triggers StandTackle");

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
        var listBox = (ListBox)(displayListBoxField!.GetValue(window) ?? throw new InvalidOperationException("Missing display list"));
        Assert(listBox.IsHandleCreated, "_displayListBox native handle exists");

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
        StartFixtureMatch(window, homeTeam, awayTeam);

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

        // PHASE 5: Q remains gameplay, return through the explicit end-match path.
        var qDown = new KeyEventArgs(Keys.Q);
        onKeyDownMethod!.Invoke(window, new object[] { window, qDown });
        Assert(typeof(AccessibleFootballWindow).GetField("_activeMatch", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(window) != null, "Q cannot terminate the match");
        typeof(AccessibleFootballWindow).GetMethod("EndMatchAndReturnToMenu", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(window, new object[] { false });

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
        var listBox = (ListBox)(displayListBoxField!.GetValue(window) ?? throw new InvalidOperationException("Missing display list"));

        var homeTeam = TeamDatabase.CreateLiverpool();
        var awayTeam = TeamDatabase.CreateManchesterCity();
        var startMatchMethod = typeof(AccessibleFootballWindow).GetMethod("StartInteractiveMatch", BindingFlags.NonPublic | BindingFlags.Instance);
        StartFixtureMatch(window, homeTeam, awayTeam);

        var activeMatchField = typeof(AccessibleFootballWindow).GetField("_activeMatch", BindingFlags.NonPublic | BindingFlags.Instance);
        var match = (MatchEngine)activeMatchField!.GetValue(window)!;

        // Invoke the real registered timer callback instead of reproducing obsolete telemetry code.
        var timer = (System.Windows.Forms.Timer)typeof(AccessibleFootballWindow).GetField("_gameLoopTimer", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(window)!;
        timer.Stop();
        var before = listBox.Items.Cast<object>().Select(x => x.ToString()).ToArray();
        var timerTick = typeof(System.Windows.Forms.Timer).GetMethod("OnTick", BindingFlags.NonPublic | BindingFlags.Instance)!;
        for (int frame = 0; frame < 100; frame++)
        {
            typeof(AccessibleFootballWindow).GetField("_lastFrameTime", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(window, DateTime.UtcNow.AddMilliseconds(-20));
            timerTick.Invoke(timer, new object[] { EventArgs.Empty });
        }
        Assert(listBox.Visible && listBox.Enabled && listBox.Focused, "Real timer keeps live-match focus anchored");
        Assert(before.SequenceEqual(listBox.Items.Cast<object>().Select(x => x.ToString())), "Real timer never rewrites the focused live-match list");
        Assert(match.MatchTimeMinutes > 0, "Real timer advanced the actual engine");

        // Clean up match
        var endMatchMethod = typeof(AccessibleFootballWindow).GetMethod("EndMatchAndReturnToMenu", BindingFlags.NonPublic | BindingFlags.Instance);
        endMatchMethod!.Invoke(window, new object[] { false });
    }
}
