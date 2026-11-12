using System;
using System.Collections.Generic;

namespace AccessibleFootball2026.Core.Models;

/// <summary>
/// نظام الترجمة والدعم ثنائي اللغة (العربية / English)
/// يتيح تبديل لغة الواجهة والقوائم والنطق الصوتي فورياً وبدون إعادة تشغيل اللعبة
/// </summary>
public static class LocalizationService
{
    public static UiLanguage CurrentLanguage
    {
        get => MatchSettings.Current.Language;
        set => MatchSettings.Current.Language = value;
    }

    private static readonly Dictionary<string, (string Arabic, string English)> _strings = new()
    {
        // 1. القائمة الرئيسية (Main Menu)
        ["MainMenuTitle"] = ("القائمة الرئيسية", "Main Menu"),
        ["ExhibitionMatch"] = ("مباراة ودية", "Exhibition Match"),
        ["DraftMode"] = ("وضع الـ 4 فرق", "4-Team Draft Mode"),
        ["UserGuide"] = ("دليل المستخدم", "User Guide"),
        ["SoundAcademy"] = ("أكاديمية الأصوات", "Sound Academy"),
        ["Settings"] = ("إعدادات اللعبة والصوت", "Game & Audio Settings"),
        ["GamePlan"] = ("خطة اللعب", "Game Plan"),
        ["Tournaments"] = ("البطولات والدوريات", "Tournaments"),
        ["ExitGame"] = ("خروج", "Exit"),
        ["ExitFarewell"] = ("شكراً لك على اللعب. إلى اللقاء!", "Thank you for playing. Goodbye!"),
        ["Welcome"] = ("مرحباً بك في كرة القدم الصوتية 2026.", "Welcome to Accessible Football 2026."),
        ["Back"] = ("رجوع", "Back"),
        ["BackToMainMenu"] = ("رجوع للقائمة الرئيسية", "Back to Main Menu"),
        ["CancelAndBack"] = ("إلغاء والرجوع", "Cancel & Return"),
        ["SelectedPrefix"] = ("تم اختيار", "Selected:"),

        // 2. إعدادات اللعبة والصوت (Settings)
        ["SettingsTitle"] = ("إعدادات اللعبة والصوت", "Game & Audio Settings"),
        ["MasterVolume"] = ("مستوى الصوت العام", "Master Volume"),
        ["SfxVolume"] = ("مؤثرات اللعب", "SFX Volume"),
        ["CommentaryVolume"] = ("صوت التعليق", "Commentary Volume"),
        ["CrowdVolume"] = ("صوت الجماهير", "Crowd Volume"),
        ["GoalBeaconVolume"] = ("منارة المرمى", "Goal Beacon Volume"),
        ["MusicVolume"] = ("موسيقى القوائم", "Menu Music Volume"),
        ["Difficulty"] = ("مستوى الصعوبة", "Difficulty Level"),
        ["MatchDuration"] = ("مدة المباراة", "Match Duration"),
        ["MinutesSuffix"] = ("دقائق", "mins"),
        ["MinuteSingular"] = ("دقيقة", "min"),
        ["CommentaryVoice"] = ("المعلق الصوتي", "Commentary Voice"),
        ["ExtraTimeOnDraw"] = ("الأشواط الإضافية", "Extra Time"),
        ["PenaltyShootoutOnDraw"] = ("ركلات الترجيح", "Penalty Shootouts"),
        ["LanguageOption"] = ("اللغة", "Language"),
        ["SaveSettingsButton"] = ("حفظ الإعدادات", "Save Settings"),
        ["ResetDefaultsButton"] = ("استعادة الإعدادات الافتراضية", "Reset to Defaults"),
        ["SettingsSavedSpeech"] = ("تم حفظ الإعدادات بنجاح في ملف settings.json", "Settings successfully saved to settings.json"),
        ["SettingsResetSpeech"] = ("تمت استعادة الإعدادات الافتراضية بنجاح", "Settings successfully reset to defaults"),
        ["LanguageChangedToEnglish"] = ("Language changed to English.", "Language changed to English."),
        ["LanguageChangedToArabic"] = ("تم تغيير لغة اللعبة إلى العربية.", "تم تغيير لغة اللعبة إلى العربية."),

        // 3. دليل المستخدم (User Guide)
        ["UserGuideTitle"] = ("دليل المستخدم", "User Guide"),
        ["CategoryControls"] = ("أزرار التحكم وحركات اللعب", "Controls & Gameplay Actions"),
        ["CategoryRules"] = ("قوانين التحكيم والتبديلات", "IFAB Rules & Substitutions"),
        ["CategoryRadarBeacon"] = ("الرادار الصوتي ومنارة المرمى 3D", "Tactical Radar & 3D Spatial Goal Beacon"),
        ["CategoryExtraPenalties"] = ("ركلات الترجيح والأشواط الإضافية", "Extra Time & Penalty Shootouts"),

        // 4. أكاديمية الأصوات (Sound Academy)
        ["SoundAcademyTitle"] = ("تعلم أصوات اللعبة", "Sound Academy"),
        ["AcademyEarcons"] = ("نغمات التنبيه التكتيكية التسعة", "The 9 Tactical Earcons"),
        ["AcademyMatchSFX"] = ("مؤثرات المباراة الحقيقية", "Match Sound Effects"),
        ["AcademyAtmosphere"] = ("أجواء المدرجات وصخب الجماهير", "Stadium Atmosphere & Crowd"),
        ["AcademyBeacon"] = ("منارة المرمى ثلاثية الأبعاد 3D", "3D Spatial Goal Beacon"),
        ["AcademyAnthems"] = ("الأناشيد والافتتاحيات الرسمية", "Official Anthems & Ceremonies"),

        // 5. عناصر التحكم (Controls)
        ["ControlPass"] = ("زر S: التمرير الأرضي المباشر", "S Key: Direct Short Pass"),
        ["ControlCross"] = ("زر D: الرفعة والعرضيات العالية", "D Key: Cross & High Lob"),
        ["ControlShoot"] = ("زر W: التسديد القوي المباشر", "W Key: Powerful Shot on Goal"),
        ["ControlThrough"] = ("زر A: التمريرة البينية في المساحة", "A Key: Through Ball"),
        ["ControlSwitch"] = ("زر Ctrl: تبديل المدافع الأقرب", "Ctrl Key: Switch Defender"),
        ["ControlSprint"] = ("زر Shift: الجري السريع", "Shift Key: Sprint"),
        ["ControlRadar"] = ("زر Space: الرادار التكتيكي الصوتي", "Space Key: Tactical Radar Query"),
        ["ControlPause"] = ("زر Esc: الإيقاف المؤقت وخطة اللعب", "Esc Key: Pause & Game Plan"),
        ["ControlAiming"] = ("السهمان الأعلى والأسفل: توجيه زوايا التسديد والتمرير", "Up/Down Arrows: Aiming Angles"),

        // 6. القوانين (Rules)
        ["RuleSubs"] = ("قانون 3 - التبديلات (IFAB Law 3)", "Law 3 - Substitutions"),
        ["RuleSanctions"] = ("قانون 12 - الإنذارات والطرد (IFAB Law 12)", "Law 12 - Disciplinary Sanctions"),
        ["RuleInjuries"] = ("نظام الإصابات واللياقة البدنية", "Player Injuries & Stamina System"),

        // 7. الرادار والمنارة (Radar & Beacon)
        ["RadarExpl"] = ("الرادار التكتيكي الصوتي (Tactical Radar)", "Tactical Audio Radar"),
        ["BeaconExpl"] = ("منارة المرمى ثلاثية الأبعاد (3D Spatial Goal Beacon)", "3D Spatial Goal Beacon"),

        // 8. الإضافي وركلات الترجيح (Extra Time & Penalties)
        ["ExtraTimeExpl"] = ("الأشواط الإضافية (Extra Time)", "Extra Time Rules"),
        ["PenaltiesExpl"] = ("ركلات الترجيح (Penalty Shootouts)", "Penalty Shootout Rules")
    };

    public static string Text(string arabic, string english) =>
        CurrentLanguage == UiLanguage.English ? english : arabic;

    public static string GetString(string key)
    {
        if (_strings.TryGetValue(key, out var val))
        {
            return CurrentLanguage == UiLanguage.English ? val.English : val.Arabic;
        }
        return key;
    }

    public static string FormatDuration(int minutes)
    {
        if (CurrentLanguage == UiLanguage.English)
        {
            return $"{minutes} minutes";
        }
        return minutes >= 11 ? $"{minutes} دقيقة" : $"{minutes} دقائق";
    }

    public static string FormatPercent(int value)
    {
        return CurrentLanguage == UiLanguage.English ? $"{value}%" : $"{value}%";
    }
}
