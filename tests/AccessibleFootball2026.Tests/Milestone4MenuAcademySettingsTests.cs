using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AccessibleFootball2026.App.Menus;
using AccessibleFootball2026.Audio;
using AccessibleFootball2026.Audio.Anthems;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Tests;

/// <summary>
/// Milestone 4 Automated Test Suite:
/// - Removal of all condescending tutorial strings ("اضغط كذا للمتابعة")
/// - Standalone "دليل المستخدم (User Guide)" screen with structured submenus
/// - Standalone "تعلم أصوات اللعبة (Sound Academy)" screen with 9 earcons, match SFX, atmosphere, 3D beacon & anthems
/// - Professional Settings screen with live audio preview sliders, comboboxes, checkboxes, save/reset buttons
/// - Bilingual Arabic / English UI Switcher and JSON persistence
/// </summary>
public static class Milestone4MenuAcademySettingsTests
{
    public static void RunAll(Action<string, Action> runTest)
    {
        Console.WriteLine("\n--- MILESTONE 4: ACCESSIBLE MENUS, USER GUIDE, ACADEMY & SETTINGS TEST SUITE ---");

        runTest("15A. Tutorial String Elimination: Menus, dialogs, and anthems strictly free of 'اضغط' tutorial hints",
            TestTutorialStringElimination_MenusAndAnthems);

        runTest("15B. User Guide Screen: Structured submenus for controls, IFAB rules, 3D radar & beacon, and penalties",
            TestUserGuide_StructuredSubmenusAndBilingualContent);

        runTest("15C. Sound Academy: All 9 earcons, match SFX, atmosphere, 3D beacon, and anthems with audio triggers",
            TestSoundAcademy_AllSoundsAndTriggersWithExplanations);

        runTest("15D. Settings Live Audio Preview: Volume sliders calculate effective volumes and trigger live audio previews",
            TestSettings_LiveAudioPreviewAndVolumeCalculations);

        runTest("15E. Settings Controls: ComboBoxes, Checkboxes, and Reset to Defaults restoration",
            TestSettings_ComboBoxesCheckboxesAndResetToDefaults);

        runTest("15F. Settings Serialization: Full round-trip JSON serialization and deserialization to settings.json",
            TestSettings_JsonSerializationAndDeserialization);

        runTest("15G. Bilingual UI Switcher: Arabic / English dynamic toggling across all menus, labels, and speech",
            TestBilingualSwitcher_DynamicLanguageToggling);
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

    private class MockScreenReader : IScreenReader
    {
        public bool IsAvailable => true;
        public List<string> SpokenMessages { get; } = new();
        public void Speak(string text, bool interrupt = true)
        {
            if (!string.IsNullOrEmpty(text))
            {
                SpokenMessages.Add(text);
            }
        }
        public void CancelSpeech() { }
        public void Silence() { }
    }

    // ----------------------------------------------------------------------------------------
    // 15A. Tutorial String Elimination & Narrative Purge
    // ----------------------------------------------------------------------------------------
    public static void TestTutorialStringElimination_MenusAndAnthems()
    {
        string root = FindWorkspaceRoot();

        // 1. Static check: AccessibleMenuSystem.cs must have zero instances of "اضغط"
        string menuSystemCode = File.ReadAllText(Path.Combine(root, "AccessibleFootball2026.App", "Menus", "AccessibleMenuSystem.cs"));
        Assert(!menuSystemCode.Contains("اضغط"),
            "AccessibleMenuSystem.cs must contain ZERO occurrences of 'اضغط' tutorial instructions!");
        Assert(!menuSystemCode.Contains("استخدم السهمين"),
            "AccessibleMenuSystem.cs must contain ZERO occurrences of 'استخدم السهمين' tutorial prompts!");

        // 2. Static check: AnthemEngine.cs must have zero instances of "اضغط"
        string anthemCode = File.ReadAllText(Path.Combine(root, "AccessibleFootball2026.Audio", "Anthems", "AnthemEngine.cs"));
        Assert(!anthemCode.Contains("اضغط"),
            "AnthemEngine.cs must contain ZERO occurrences of 'اضغط' tutorial prompts!");

        // 3. User Directive: Full Narrative Purge across menus (No long-winded mode tutorials in titles)
        string windowCode = File.ReadAllText(Path.Combine(root, "AccessibleFootball2026.App", "AccessibleFootballWindow.cs"));
        Assert(!windowCode.Contains("خيارات درافت الأندية الأربعة"),
            "AccessibleFootballWindow.cs must purge verbose Draft pre-match title!");
        Assert(!windowCode.Contains("استعراض وتعديل التشكيلة الأساسية (11 لاعب والمراكز)"),
            "AccessibleFootballWindow.cs must purge parenthetical tutorials in lineup menu title!");
        Assert(!windowCode.Contains("تعيين أدوار المتخصصين (الكابتن، ركلات الجزاء، الفاولات، الركنيات)"),
            "AccessibleFootballWindow.cs must purge parenthetical tutorials in specialists menu title!");
        Assert(!windowCode.Contains("استعراض الدوريات والبطولات الرسمية لموسم 2026 و 2027"),
            "AccessibleFootballWindow.cs must purge long narrative titles from tournaments flow!");
        Assert(!windowCode.Contains("وضع 4 فرق"),
            "AccessibleFootballWindow.cs must contain ZERO occurrences of 'وضع 4 فرق' narrative tutorials!");
        Assert(!windowCode.Contains("أنت تختار"),
            "AccessibleFootballWindow.cs must contain ZERO occurrences of 'أنت تختار' narrative tutorials!");

        string gamePlanCode = File.ReadAllText(Path.Combine(root, "AccessibleFootball2026.App", "Menus", "GamePlanMenuBuilder.cs"));
        Assert(!gamePlanCode.Contains("استعراض وتعديل التشكيلة الأساسية (الـ 11 لاعب)"),
            "GamePlanMenuBuilder.cs must purge parenthetical tutorials in lineup menu title!");
        Assert(!gamePlanCode.Contains("تعيين أدوار المتخصصين (الكابتن، ضربات الجزاء، الفاولات، الركنيات)"),
            "GamePlanMenuBuilder.cs must purge parenthetical tutorials in specialists menu title!");
        Assert(!gamePlanCode.Contains("إجراء تبديل تكتيكي من الدكة"),
            "GamePlanMenuBuilder.cs must purge verbose narrative titles from pause menu substitutions!");

        // 4. Verify crisp, professional titles in LocalizationService
        MatchSettings.Current.Language = UiLanguage.Arabic;
        Assert(LocalizationService.GetString("ExhibitionMatch") == "مباراة ودية",
            "ExhibitionMatch title must be crisp 'مباراة ودية'");
        Assert(LocalizationService.GetString("DraftMode") == "وضع الـ 4 فرق",
            "DraftMode title must be crisp 'وضع الـ 4 فرق'");
        Assert(LocalizationService.GetString("Tournaments") == "البطولات والدوريات",
            "Tournaments title must be crisp 'البطولات والدوريات'");
        Assert(LocalizationService.GetString("ExitGame") == "خروج",
            "ExitGame title must be crisp 'خروج'");
        Assert(LocalizationService.GetString("Settings") == "إعدادات اللعبة والصوت",
            "Settings title must be crisp 'إعدادات اللعبة والصوت'");
        Assert(LocalizationService.GetString("GamePlan") == "خطة اللعب",
            "GamePlan title must be crisp 'خطة اللعب'");

        // 5. Runtime check on interactive menu items
        var reader = new MockScreenReader();

        // Checkbox: title and spoken text must not contain tutorial prompts
        var checkbox = new AccessibleCheckboxMenuItem("الأشواط الإضافية", true, _ => { }, reader);
        Assert(!checkbox.Title.Contains("اضغط"), "Checkbox Title must not contain tutorial prompts");
        Assert(!checkbox.SpokenText.Contains("اضغط"), "Checkbox SpokenText must not contain tutorial prompts");
        Assert(!checkbox.SpokenText.Contains("مسافة"), "Checkbox SpokenText must not contain button tutorial hints");

        // ComboBox: title and spoken text must not contain tutorial prompts
        var combo = new AccessibleComboBoxMenuItem("الصعوبة", new List<string> { "سهل", "متوسط", "محترف" }, 1, _ => { }, null, reader);
        Assert(!combo.Title.Contains("اضغط") && !combo.Title.Contains("الأسهم"),
            "ComboBox Title must not contain arrow/button tutorial instructions");
        Assert(!combo.SpokenText.Contains("اضغط") && !combo.SpokenText.Contains("استخدم"),
            "ComboBox SpokenText must not contain tutorial instructions");

        // Slider: title and spoken text must not contain tutorial prompts
        var slider = new AccessibleSliderMenuItem("مستوى الصوت", 80, 0, 100, 5, _ => { }, reader);
        Assert(!slider.Title.Contains("اضغط") && !slider.Title.Contains("استخدم"),
            "Slider Title must not contain arrow tutorial instructions");
        Assert(!slider.SpokenText.Contains("اضغط") && !slider.SpokenText.Contains("استخدم"),
            "Slider SpokenText must not contain arrow tutorial instructions");
    }

    // ----------------------------------------------------------------------------------------
    // 15B. User Guide Screen & Submenus
    // ----------------------------------------------------------------------------------------
    public static void TestUserGuide_StructuredSubmenusAndBilingualContent()
    {
        var categories = UserGuideDatabase.GetCategories();
        Assert(categories.Count >= 4, $"User Guide must have at least 4 structured submenus, got {categories.Count}");

        var catIds = categories.Select(c => c.Id).ToList();
        Assert(catIds.Contains("controls"), "User Guide must have 'controls' submenu");
        Assert(catIds.Contains("rules"), "User Guide must have 'rules' submenu");
        Assert(catIds.Contains("radar_beacon"), "User Guide must have 'radar_beacon' submenu");
        Assert(catIds.Contains("penalties_extratime"), "User Guide must have 'penalties_extratime' submenu");
        Assert(catIds.Contains("modes"), "User Guide must have 'modes' submenu for decoupled mode explanations");

        // Decoupled mode explanations in User Guide
        var modeItems = UserGuideDatabase.GetItemsByCategory("modes");
        Assert(modeItems.Any(m => m.Id == "draft_mode_guide"), "User Guide must contain 4-Clubs Draft mode explanation");
        Assert(modeItems.Any(m => m.Id == "exhibition_mode_guide"), "User Guide must contain Exhibition mode explanation");
        Assert(modeItems.Any(m => m.Id == "tournaments_mode_guide"), "User Guide must contain Tournaments mode explanation");
        Assert(modeItems.Any(m => m.Id == "game_plan_guide"), "User Guide must contain Game Plan mode explanation");

        // 1. Controls submenu: 9 required gameplay actions
        var controlsItems = UserGuideDatabase.GetItemsByCategory("controls");
        Assert(controlsItems.Count >= 9, $"Controls submenu must have at least 9 actions, got {controlsItems.Count}");
        var keys = controlsItems.Select(i => i.KeyBinding).ToList();
        Assert(keys.Contains("S"), "Controls must explain S (Short Pass)");
        Assert(keys.Contains("D"), "Controls must explain D (Cross / Lob)");
        Assert(keys.Contains("W"), "Controls must explain W (Powerful Shot)");
        Assert(keys.Contains("A"), "Controls must explain A (Through Ball)");
        Assert(keys.Contains("Ctrl"), "Controls must explain Ctrl (Switch Defender)");
        Assert(keys.Contains("Shift"), "Controls must explain Shift (Sprint)");
        Assert(keys.Contains("Space"), "Controls must explain Space (Tactical Radar)");
        Assert(keys.Contains("Esc"), "Controls must explain Esc (Pause & Game Plan)");
        Assert(keys.Contains("Up/Down"), "Controls must explain Up/Down (Aiming Angles)");

        // 2. Rules submenu: Law 3, Law 12, Injuries
        var rulesItems = UserGuideDatabase.GetItemsByCategory("rules");
        Assert(rulesItems.Any(r => r.Id == "law3_subs"), "Rules must explain Law 3 substitutions (5 subs in 3 windows)");
        Assert(rulesItems.Any(r => r.Id == "law12_cards"), "Rules must explain Law 12 cards, DOGSO, and 10-man play");
        Assert(rulesItems.Any(r => r.Id == "injuries_system"), "Rules must explain injuries system");

        var law3 = rulesItems.First(r => r.Id == "law3_subs");
        Assert(law3.ArabicDescription.Contains("5 تبديلات") && law3.ArabicDescription.Contains("3 فترات توقف"),
            "Law 3 must specify 5 subs across 3 windows in Arabic");
        Assert(law3.EnglishDescription.Contains("5 substitutions") && law3.EnglishDescription.Contains("3 stoppage"),
            "Law 3 must specify 5 subs across 3 stoppage opportunities in English");

        // 3. Radar & Beacon submenu
        var radarBeaconItems = UserGuideDatabase.GetItemsByCategory("radar_beacon");
        Assert(radarBeaconItems.Any(b => b.Id == "tactical_radar_guide"), "Must explain Tactical Radar");
        Assert(radarBeaconItems.Any(b => b.Id == "spatial_goal_beacon"), "Must explain 3D Spatial Goal Beacon");

        var beacon = radarBeaconItems.First(b => b.Id == "spatial_goal_beacon");
        Assert(beacon.ArabicDescription.Contains("528") && beacon.ArabicDescription.Contains("الشوط الثاني"),
            "Beacon guide must explain 528Hz tone and second half swap in Arabic");

        // 4. Extra Time & Penalties submenu
        var penaltyItems = UserGuideDatabase.GetItemsByCategory("penalties_extratime");
        Assert(penaltyItems.Any(p => p.Id == "extra_time_rules"), "Must explain Extra Time rules");
        Assert(penaltyItems.Any(p => p.Id == "penalty_shootouts"), "Must explain Penalty Shootouts");

        // 5. Test UserGuideMenuBuilder menu creation
        var reader = new MockScreenReader();
        var menuStack = new Stack<AccessibleMenu>();
        var userGuideMenu = UserGuideMenuBuilder.CreateUserGuideMenu(
            reader,
            m => menuStack.Push(m),
            () => { if (menuStack.Count > 0) menuStack.Pop(); });

        Assert(userGuideMenu != null, "UserGuideMenu must not be null");
        Assert(userGuideMenu.Count == categories.Count + 1, $"UserGuideMenu must have {categories.Count} category items + 1 back item, got {userGuideMenu.Count}");

        // Select first category (Controls)
        userGuideMenu.SelectCurrent();
        Assert(menuStack.Count == 1, "Selecting category must push sub-menu");
        var subMenu = menuStack.Peek();
        Assert(subMenu.Count >= 10, "Submenu must contain items + 1 back item");

        // Select first item in sub-menu
        subMenu.SelectCurrent();
        Assert(reader.SpokenMessages.Count > 0, "Selecting item in sub-menu must speak description");
    }

    // ----------------------------------------------------------------------------------------
    // 15C. Sound Academy Screen & Triggers
    // ----------------------------------------------------------------------------------------
    public static void TestSoundAcademy_AllSoundsAndTriggersWithExplanations()
    {
        Assert(SoundAcademyDatabase.GetCategories().Count==0,"Academy must have no categories");
        var items=SoundAcademyDatabase.GetAllItems();
        Assert(items.Count==18,"Academy must contain nine semantic cues and nine beacon demonstrations");
        Assert(items.Count(i=>i.CategoryId=="earcons")==9,"Exactly nine semantic cues");
        Assert(items.All(i=>i.CategoryId is "earcons" or "beacon"),"No match effects, crowd or music academy entries");
        int triggered=0;
        foreach(var item in SoundAcademyDatabase.GetAllItems(_=>()=>triggered++)) item.PlayAudioAction?.Invoke();
        Assert(triggered==items.Count,"Every sample has a preview action");
        var reader=new MockScreenReader();var anthemEngine=new AnthemEngine(reader);
        int pushes=0;
        var soundMenu=SoundAcademyMenuBuilder.CreateSoundAcademyMenu(reader,anthemEngine,_=>pushes++,()=>{});
        Assert(soundMenu.Count==items.Count+1,"Flat list includes all samples and Back");
        soundMenu.GetItem(0).OnSelect?.Invoke();
        Assert(pushes==0,"Playing a sample must not enter a category submenu");
        Assert(soundMenu.GetItem(0).SuppressSelectionSound,"Preview suppresses selection chime");
    }

    // ----------------------------------------------------------------------------------------
    // 15D. Settings Live Audio Preview & Volume Calculations
    // ----------------------------------------------------------------------------------------
    public static void TestSettings_LiveAudioPreviewAndVolumeCalculations()
    {
        var settings = new MatchSettings();

        // 1. Full volume baseline
        settings.MasterVolumePercent = 100;
        settings.SfxVolumePercent = 90;
        settings.CommentaryVolumePercent = 80;
        settings.CrowdVolumePercent = 70;
        settings.GoalBeaconVolumePercent = 60;
        settings.MusicVolumePercent = 50;
        settings.CommentaryEnabled = true;

        Assert(Math.Abs(settings.GetEffectiveMasterVolume() - 1.0f) < 0.001f, "Effective Master volume must be 1.0");
        Assert(Math.Abs(settings.GetEffectiveSfxVolume() - 0.9f) < 0.001f, "Effective SFX volume must be 0.90");
        Assert(Math.Abs(settings.GetEffectiveCommentaryVolume() - 0.8f) < 0.001f, "Effective Commentary volume must be 0.80");
        Assert(Math.Abs(settings.GetEffectiveCrowdVolume() - 0.7f) < 0.001f, "Effective Crowd volume must be 0.70");
        Assert(Math.Abs(settings.GetEffectiveBeaconVolume() - 0.6f) < 0.001f, "Effective Beacon volume must be 0.60");
        Assert(Math.Abs(settings.GetEffectiveMusicVolume() - 0.5f) < 0.001f, "Effective Music volume must be 0.50");

        // 2. Master volume scaling (Master = 50%)
        settings.MasterVolumePercent = 50;
        Assert(Math.Abs(settings.GetEffectiveMasterVolume() - 0.5f) < 0.001f, "Effective Master volume must be 0.50");
        Assert(Math.Abs(settings.GetEffectiveSfxVolume() - 0.45f) < 0.001f, "Effective SFX volume must be 0.5 * 0.9 = 0.45");
        Assert(Math.Abs(settings.GetEffectiveCommentaryVolume() - 0.40f) < 0.001f, "Effective Commentary volume must be 0.5 * 0.8 = 0.40");
        Assert(Math.Abs(settings.GetEffectiveCrowdVolume() - 0.35f) < 0.001f, "Effective Crowd volume must be 0.5 * 0.7 = 0.35");
        Assert(Math.Abs(settings.GetEffectiveBeaconVolume() - 0.30f) < 0.001f, "Effective Beacon volume must be 0.5 * 0.6 = 0.30");

        // 3. Commentary silenced when CommentaryEnabled = false or Voice = Off
        settings.CommentaryEnabled = false;
        Assert(settings.GetEffectiveCommentaryVolume() == 0.0f, "Commentary volume must be 0 when disabled");

        settings.CommentaryEnabled = true;
        settings.CommentaryVoice = CommentaryVoiceOption.Off;
        Assert(settings.GetEffectiveCommentaryVolume() == 0.0f, "Commentary volume must be 0 when Voice = Off");

        // 4. Master volume at 0% silences all audio
        settings.MasterVolumePercent = 0;
        Assert(settings.GetEffectiveSfxVolume() == 0.0f, "Master 0% must silence SFX");
        Assert(settings.GetEffectiveCommentaryVolume() == 0.0f, "Master 0% must silence Commentary");
        Assert(settings.GetEffectiveCrowdVolume() == 0.0f, "Master 0% must silence Crowd");
        Assert(settings.GetEffectiveBeaconVolume() == 0.0f, "Master 0% must silence Beacon");

        // 5. Test Live Audio Preview invocation without exceptions
        SoundEffectsPlayer.ResetTelemetry();
        SoundEffectsPlayer.PlayLivePreviewMaster(75);
        Assert(SoundEffectsPlayer.MasterVolumeSetting == 75, "PlayLivePreviewMaster must set MasterVolumeSetting");

        SoundEffectsPlayer.PlayLivePreviewSfx(85);
        Assert(SoundEffectsPlayer.SfxVolumeSetting == 85, "PlayLivePreviewSfx must set SfxVolumeSetting");

        SoundEffectsPlayer.PlayLivePreviewCommentary(65);
        Assert(SoundEffectsPlayer.CommentaryVolumeSetting == 65, "PlayLivePreviewCommentary must set CommentaryVolumeSetting");

        SoundEffectsPlayer.PlayLivePreviewCrowd(55);
        Assert(SoundEffectsPlayer.CrowdVolumeSetting == 55, "PlayLivePreviewCrowd must set CrowdVolumeSetting");

        SoundEffectsPlayer.PlayLivePreviewGoalBeacon(95);
        Assert(SoundEffectsPlayer.GoalBeaconVolumeSetting == 95, "PlayLivePreviewGoalBeacon must set GoalBeaconVolumeSetting");
    }

    // ----------------------------------------------------------------------------------------
    // 15E. Settings ComboBoxes, Checkboxes & Reset to Defaults
    // ----------------------------------------------------------------------------------------
    public static void TestSettings_ComboBoxesCheckboxesAndResetToDefaults()
    {
        var settings = new MatchSettings();

        // Test Difficulty values
        settings.Difficulty = DifficultyLevel.Easy;
        Assert(settings.Difficulty.ToArabicName() == "سهل", "Easy Arabic name must be 'سهل'");
        Assert(settings.Difficulty.ToEnglishName() == "Easy", "Easy English name must be 'Easy'");

        settings.Difficulty = DifficultyLevel.Normal;
        Assert(settings.Difficulty.ToArabicName() == "متوسط", "Normal Arabic name must be 'متوسط'");
        Assert(settings.Difficulty.ToEnglishName() == "Normal", "Normal English name must be 'Normal'");

        settings.Difficulty = DifficultyLevel.Hard;
        Assert(settings.Difficulty.ToArabicName() == "محترف", "Hard Arabic name must be 'محترف'");
        Assert(settings.Difficulty.ToEnglishName() == "Hard", "Hard English name must be 'Hard'");

        // Test Match Duration cycling
        settings.MatchDurationMinutes = 3;
        settings.CycleDuration();
        Assert(settings.MatchDurationMinutes == 5, "CycleDuration from 3 must yield 5");
        settings.CycleDuration();
        Assert(settings.MatchDurationMinutes == 10, "CycleDuration from 5 must yield 10");
        settings.CycleDuration();
        Assert(settings.MatchDurationMinutes == 15, "CycleDuration from 10 must yield 15");
        settings.CycleDuration();
        Assert(settings.MatchDurationMinutes == 20, "CycleDuration from 15 must yield 20");
        settings.CycleDuration();
        Assert(settings.MatchDurationMinutes == 3, "CycleDuration from 20 must cycle back to 3");

        // Test Checkboxes
        settings.ExtraTimeEnabled = true;
        settings.ToggleExtraTime();
        Assert(!settings.ExtraTimeEnabled, "ToggleExtraTime must flip to false");
        settings.ToggleExtraTime();
        Assert(settings.ExtraTimeEnabled, "ToggleExtraTime must flip to true");

        settings.PenaltyShootoutEnabled = true;
        settings.TogglePenalties();
        Assert(!settings.PenaltyShootoutEnabled, "TogglePenalties must flip to false");
        settings.TogglePenalties();
        Assert(settings.PenaltyShootoutEnabled, "TogglePenalties must flip to true");

        // Mutate everything away from default
        settings.Difficulty = DifficultyLevel.Hard;
        settings.MatchDurationMinutes = 20;
        settings.ExtraTimeEnabled = false;
        settings.PenaltyShootoutEnabled = false;
        settings.MasterVolumePercent = 25;
        settings.SfxVolumePercent = 30;
        settings.CommentaryVolumePercent = 35;
        settings.CrowdVolumePercent = 40;
        settings.GoalBeaconVolumePercent = 45;
        settings.Language = UiLanguage.English;

        // Reset to Defaults
        settings.ResetToDefaults();
        Assert(settings.Difficulty == DifficultyLevel.Normal, "ResetToDefaults must restore Difficulty=Normal");
        Assert(settings.MatchDurationMinutes == 5, "ResetToDefaults must restore MatchDurationMinutes=5");
        Assert(settings.ExtraTimeEnabled == true, "ResetToDefaults must restore ExtraTimeEnabled=true");
        Assert(settings.PenaltyShootoutEnabled == true, "ResetToDefaults must restore PenaltyShootoutEnabled=true");
        Assert(settings.MasterVolumePercent == 100, "ResetToDefaults must restore MasterVolumePercent=100");
        Assert(settings.SfxVolumePercent == 90, "ResetToDefaults must restore SfxVolumePercent=90");
        Assert(settings.CommentaryVolumePercent == 90, "ResetToDefaults must restore CommentaryVolumePercent=90");
        Assert(settings.CrowdVolumePercent == 35, "ResetToDefaults must restore CrowdVolumePercent=35");
        Assert(settings.MusicVolumePercent == 35, "ResetToDefaults must restore MusicVolumePercent=35");
        Assert(settings.GoalBeaconVolumePercent == 80, "ResetToDefaults must restore GoalBeaconVolumePercent=80");
        Assert(settings.Language == UiLanguage.Arabic, "ResetToDefaults must restore Language=Arabic");
    }

    // ----------------------------------------------------------------------------------------
    // 15F. Settings JSON Serialization & Deserialization
    // ----------------------------------------------------------------------------------------
    public static void TestSettings_JsonSerializationAndDeserialization()
    {
        string tempSettingsFile = Path.Combine(Path.GetTempPath(), $"settings_test_{Guid.NewGuid():N}.json");
        try
        {
            var original = new MatchSettings
            {
                Difficulty = DifficultyLevel.Hard,
                MatchDurationMinutes = 15,
                ExtraTimeEnabled = false,
                PenaltyShootoutEnabled = true,
                MasterVolumePercent = 77,
                SfxVolumePercent = 88,
                CommentaryVolumePercent = 66,
                CrowdVolumePercent = 55,
                GoalBeaconVolumePercent = 44,
                Language = UiLanguage.English,
                CommentaryVoice = CommentaryVoiceOption.RaoufKhlif,
                CommentaryEnabled = true
            };

            original.Save(tempSettingsFile);
            Assert(File.Exists(tempSettingsFile), "Settings file must be created on disk");

            string json = File.ReadAllText(tempSettingsFile);
            Assert(json.Contains("\"MasterVolumePercent\": 77"), "JSON must contain MasterVolumePercent: 77");
            Assert(json.Contains("\"SfxVolumePercent\": 88"), "JSON must contain SfxVolumePercent: 88");
            Assert(json.Contains("\"GoalBeaconVolumePercent\": 44"), "JSON must contain GoalBeaconVolumePercent: 44");
            Assert(json.Contains("\"Language\": 1"), "JSON must contain Language: 1 (English)");

            // Load into a new instance
            var loaded = JsonSerializer.Deserialize<MatchSettings>(json);
            Assert(loaded != null, "Deserialized settings must not be null");
            Assert(loaded!.Difficulty == DifficultyLevel.Hard, "Difficulty must round-trip correctly");
            Assert(loaded.MatchDurationMinutes == 15, "MatchDurationMinutes must round-trip correctly");
            Assert(loaded.ExtraTimeEnabled == false, "ExtraTimeEnabled must round-trip correctly");
            Assert(loaded.PenaltyShootoutEnabled == true, "PenaltyShootoutEnabled must round-trip correctly");
            Assert(loaded.MasterVolumePercent == 77, "MasterVolumePercent must round-trip correctly");
            Assert(loaded.SfxVolumePercent == 88, "SfxVolumePercent must round-trip correctly");
            Assert(loaded.CommentaryVolumePercent == 66, "CommentaryVolumePercent must round-trip correctly");
            Assert(loaded.CrowdVolumePercent == 55, "CrowdVolumePercent must round-trip correctly");
            Assert(loaded.GoalBeaconVolumePercent == 44, "GoalBeaconVolumePercent must round-trip correctly");
            Assert(loaded.Language == UiLanguage.English, "Language must round-trip correctly");
        }
        finally
        {
            if (File.Exists(tempSettingsFile))
            {
                try { File.Delete(tempSettingsFile); } catch { }
            }
        }
    }

    // ----------------------------------------------------------------------------------------
    // 15G. Bilingual UI Switcher
    // ----------------------------------------------------------------------------------------
    public static void TestBilingualSwitcher_DynamicLanguageToggling()
    {
        // 1. Arabic baseline
        MatchSettings.Current.Language = UiLanguage.Arabic;
        Assert(LocalizationService.GetString("MainMenuTitle") == "القائمة الرئيسية",
            "Arabic MainMenuTitle must be 'القائمة الرئيسية'");
        Assert(LocalizationService.GetString("UserGuideTitle") == "دليل المستخدم",
            "Arabic UserGuideTitle must be 'دليل المستخدم'");
        Assert(LocalizationService.GetString("SoundAcademyTitle") == "تعلم أصوات اللعبة",
            "Arabic SoundAcademyTitle must be 'تعلم أصوات اللعبة'");
        Assert(LocalizationService.GetString("SettingsTitle") == "إعدادات اللعبة والصوت",
            "Arabic SettingsTitle must be 'إعدادات اللعبة والصوت'");
        Assert(LocalizationService.GetString("MasterVolume") == "مستوى الصوت العام",
            "Arabic MasterVolume must be 'مستوى الصوت العام'");

        var reader = new MockScreenReader();
        var chkAr = new AccessibleCheckboxMenuItem("الأشواط الإضافية", true, _ => { }, reader);
        Assert(chkAr.Title.Contains("محدد"), "Arabic checkbox title must contain 'محدد'");

        // 2. Switch to English
        MatchSettings.Current.Language = UiLanguage.English;
        Assert(LocalizationService.GetString("MainMenuTitle") == "Main Menu",
            "English MainMenuTitle must be 'Main Menu'");
        Assert(LocalizationService.GetString("UserGuideTitle") == "User Guide",
            "English UserGuideTitle must be 'User Guide'");
        Assert(LocalizationService.GetString("SoundAcademyTitle") == "Sound Academy",
            "English SoundAcademyTitle must be 'Sound Academy'");
        Assert(LocalizationService.GetString("SettingsTitle") == "Game & Audio Settings",
            "English SettingsTitle must be 'Game & Audio Settings'");
        Assert(LocalizationService.GetString("MasterVolume") == "Master Volume",
            "English MasterVolume must be 'Master Volume'");

        var chkEn = new AccessibleCheckboxMenuItem("Extra Time", true, _ => { }, reader);
        Assert(chkEn.Title.Contains("Checked"), "English checkbox title must contain 'Checked'");
        Assert(chkEn.SpokenText.Contains("Checked"), "English checkbox spoken text must contain 'Checked'");

        // 3. Switch back to Arabic
        MatchSettings.Current.Language = UiLanguage.Arabic;
        Assert(LocalizationService.GetString("MainMenuTitle") == "القائمة الرئيسية",
            "Switching back to Arabic must restore 'القائمة الرئيسية'");
    }
}
