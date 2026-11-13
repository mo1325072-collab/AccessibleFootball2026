using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AccessibleFootball2026.Core.Models;

public enum DifficultyLevel
{
    Easy,       // سهل
    Normal,     // متوسط
    Hard,       // صعب / محترف
    Legendary   // أسطوري
}

public enum UiLanguage
{
    Arabic,
    English
}

public enum CommentaryVoiceOption
{
    RaoufKhlif,
    Off,
    FaresAwad // يُلحق في النهاية: ملفات الإعدادات القديمة تخزن الرقم (0=رؤوف، 1=صامت)
}

public static class DifficultyExtensions
{
    public static string ToArabicName(this DifficultyLevel level) => level switch
    {
        DifficultyLevel.Easy => "سهل",
        DifficultyLevel.Normal => "متوسط",
        DifficultyLevel.Hard => "محترف",
        DifficultyLevel.Legendary => "أسطوري",
        _ => "متوسط"
    };

    public static string ToEnglishName(this DifficultyLevel level) => level switch
    {
        DifficultyLevel.Easy => "Easy",
        DifficultyLevel.Normal => "Normal",
        DifficultyLevel.Hard => "Hard",
        DifficultyLevel.Legendary => "Legendary",
        _ => "Normal"
    };

    public static string ToShortArabic(this DifficultyLevel level) => level switch
    {
        DifficultyLevel.Easy => "سهل",
        DifficultyLevel.Normal => "متوسط",
        DifficultyLevel.Hard => "صعب",
        DifficultyLevel.Legendary => "أسطوري",
        _ => "متوسط"
    };

    public static string ToLocalizedName(this DifficultyLevel level, UiLanguage language) =>
        language == UiLanguage.English ? level.ToEnglishName() : level.ToArabicName();
}

/// <summary>
/// إعدادات المباراة وخيارات الصوت والصعوبة والتوقيت مع الحفظ الدائم
/// </summary>
public class MatchSettings
{
    public static MatchSettings Current { get; } = new();

    private static string GetDefaultSettingsPath()
    {
        try
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!string.IsNullOrEmpty(appData))
            {
                string dir = Path.Combine(appData, "AccessibleFootball2026");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "settings.json");
            }
        }
        catch { }
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
    }

    private static readonly string DefaultSettingsFilePath = GetDefaultSettingsPath();
    public static string CustomSettingsFilePath { get; set; } = string.Empty;
    public static string ActiveSettingsFilePath => !string.IsNullOrEmpty(CustomSettingsFilePath)
        ? CustomSettingsFilePath
        : DefaultSettingsFilePath;

    // 1. خيارات المباراة التكتيكية
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Normal;
    public int MatchDurationMinutes { get; set; } = 5; // مدة المباراة الحقيقية بالدقائق (3, 5, 10, 15, 20)
    public bool ExtraTimeEnabled { get; set; } = true;  // وقت إضافي عند التعادل
    public bool PenaltyShootoutEnabled { get; set; } = true; // ركلات ترجيح عند استمرار التعادل

    // 2. خيارات المعلق الصوتي المعتمد
    public CommentatorId Commentator { get; set; } = CommentatorId.FaresAwad;
    public CommentaryVoiceOption CommentaryVoice { get; set; } = CommentaryVoiceOption.FaresAwad;
    public int SettingsVersion { get; set; } // 0 في الملفات القديمة؛ 2 بعد ترقية المعلق الافتراضي
    public bool CommentaryEnabled { get; set; } = true;

    // 3. التحكم بمستويات الصوت (0 - 100)
    public int MasterVolumePercent { get; set; } = 100;
    public int SfxVolumePercent { get; set; } = 90;
    public int CommentaryVolumePercent { get; set; } = 90;
    public int CrowdVolumePercent { get; set; } = 35;
    public int GoalBeaconVolumePercent { get; set; } = 80;
    public int MusicVolumePercent { get; set; } = 35;
    public bool ScreenReaderSpokenEventsEnabled { get; set; } = true;

    // 4. خيارات اللغة
    public UiLanguage Language { get; set; } = UiLanguage.Arabic;

    // 5. ميزات الذكاء الاصطناعي التكتيكي والـ VAR (AI Tactical Advisor & AI VAR)
    public bool AutoSubstitutionsEnabled { get; set; } = false;
    public bool AutoTacticalAdjustmentsEnabled { get; set; } = false; // التعديل التكتيكي التلقائي الذكي حسب مجريات وسير اللعب
    public bool AiVarEnabled { get; set; } = true;
    public bool CheckForUpdatesOnStartup { get; set; } = false;

    // حسابات مستويات الصوت الفعالة (0.0f - 1.0f)
    public float GetEffectiveMasterVolume() => Math.Clamp(MasterVolumePercent / 100f, 0f, 1f);
    public float GetEffectiveSfxVolume() => GetEffectiveMasterVolume() * Math.Clamp(SfxVolumePercent / 100f, 0f, 1f);
    public float GetEffectiveCommentaryVolume() => CommentaryEnabled && CommentaryVoice != CommentaryVoiceOption.Off 
        ? GetEffectiveMasterVolume() * Math.Clamp(CommentaryVolumePercent / 100f, 0f, 1f) 
        : 0f;
    public float GetEffectiveCrowdVolume() => GetEffectiveMasterVolume() * Math.Clamp(CrowdVolumePercent / 100f, 0f, 1f);
    public float GetEffectiveBeaconVolume() => GetEffectiveMasterVolume() * Math.Clamp(GoalBeaconVolumePercent / 100f, 0f, 1f);
    public float GetEffectiveMusicVolume() => GetEffectiveMasterVolume() * Math.Clamp(MusicVolumePercent / 100f, 0f, 1f);

    public void ResetToDefaults()
    {
        Difficulty = DifficultyLevel.Normal;
        MatchDurationMinutes = 5;
        ExtraTimeEnabled = true;
        PenaltyShootoutEnabled = true;
        Commentator = CommentatorId.FaresAwad;
        CommentaryVoice = CommentaryVoiceOption.FaresAwad;
        CommentaryEnabled = true;
        MasterVolumePercent = 100;
        SfxVolumePercent = 90;
        CommentaryVolumePercent = 90;
        CrowdVolumePercent = 35;
        GoalBeaconVolumePercent = 80;
        MusicVolumePercent = 35;
        ScreenReaderSpokenEventsEnabled = true;
        Language = UiLanguage.Arabic;
        AutoSubstitutionsEnabled = false;
        AutoTacticalAdjustmentsEnabled = false;
        AiVarEnabled = true;
        CheckForUpdatesOnStartup = false;
    }

    public static event Action<MatchSettings>? OnSettingsChanged;

    public void ApplyLive()
    {
        OnSettingsChanged?.Invoke(this);
    }

    [JsonIgnore] public string? LastSaveError { get; private set; }

    public bool Save(string? targetPath = null)
    {
        string p = targetPath ?? ActiveSettingsFilePath;
        string? temporary = null;
        LastSaveError = null;
        try
        {
            SettingsVersion = 2; // An explicit save must never be treated as an unmigrated legacy choice.
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(this, options);
            string? dir = Path.GetDirectoryName(p);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            temporary = p + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(temporary, json);
            File.Move(temporary, p, overwrite: true);
        }
        catch (Exception error)
        {
            LastSaveError = error.Message;
            if (temporary != null) { try { File.Delete(temporary); } catch { } }
            return false;
        }
        ApplyLive();
        return true;
    }

    public static void Load(string? sourcePath = null)
    {
        string p = sourcePath ?? ActiveSettingsFilePath;
        try
        {
            if (File.Exists(p))
            {
                string json = File.ReadAllText(p);
                var loaded = JsonSerializer.Deserialize<MatchSettings>(json);
                if (loaded != null)
                {
                    Current.CopyFrom(loaded);
                    // ترقية لمرة واحدة: اعتماد فارس عوض معلقاً افتراضياً بطلب المستخدم؛
                    // رؤوف خليف يبقى متاحاً من قائمة اختيار المعلق ولا تتكرر الترقية بعد الإصدار 2
                    if (Current.SettingsVersion < 2)
                    {
                        if (Current.CommentaryEnabled && Current.CommentaryVoice != CommentaryVoiceOption.Off)
                        {
                            Current.Commentator = CommentatorId.FaresAwad;
                            Current.CommentaryVoice = CommentaryVoiceOption.FaresAwad;
                        }
                        Current.SettingsVersion = 2;
                        Current.Save(p);
                        return;
                    }
                    Current.ApplyLive();
                }
            }
        }
        catch { }
    }

    public void CopyFrom(MatchSettings source)
    {
                    Difficulty = source.Difficulty;
                    MatchDurationMinutes = source.MatchDurationMinutes;
                    ExtraTimeEnabled = source.ExtraTimeEnabled;
                    PenaltyShootoutEnabled = source.PenaltyShootoutEnabled;
                    Commentator = source.Commentator;
                    CommentaryVoice = source.CommentaryVoice;
                    CommentaryEnabled = source.CommentaryEnabled;
                    MasterVolumePercent = source.MasterVolumePercent;
                    SfxVolumePercent = source.SfxVolumePercent;
                    CommentaryVolumePercent = source.CommentaryVolumePercent;
                    CrowdVolumePercent = source.CrowdVolumePercent;
                    GoalBeaconVolumePercent = source.GoalBeaconVolumePercent;
                    MusicVolumePercent = source.MusicVolumePercent;
                    ScreenReaderSpokenEventsEnabled = source.ScreenReaderSpokenEventsEnabled;
                    Language = source.Language;
                    AutoSubstitutionsEnabled = source.AutoSubstitutionsEnabled;
                    AutoTacticalAdjustmentsEnabled = source.AutoTacticalAdjustmentsEnabled;
                    AiVarEnabled = source.AiVarEnabled;
                    CheckForUpdatesOnStartup = source.CheckForUpdatesOnStartup;
                    SettingsVersion = source.SettingsVersion;
    }

    public void ToggleAutoSubstitutions()
    {
        AutoSubstitutionsEnabled = !AutoSubstitutionsEnabled;
        Save();
    }

    public void ToggleAutoTacticalAdjustments()
    {
        AutoTacticalAdjustmentsEnabled = !AutoTacticalAdjustmentsEnabled;
        Save();
    }

    public void ToggleAiVar()
    {
        AiVarEnabled = !AiVarEnabled;
        Save();
    }

    public void CycleDifficulty()
    {
        Difficulty = Difficulty switch
        {
            DifficultyLevel.Easy => DifficultyLevel.Normal,
            DifficultyLevel.Normal => DifficultyLevel.Hard,
            DifficultyLevel.Hard => DifficultyLevel.Legendary,
            DifficultyLevel.Legendary => DifficultyLevel.Easy,
            _ => DifficultyLevel.Normal
        };
        Save();
    }

    public void CycleDuration()
    {
        MatchDurationMinutes = MatchDurationMinutes switch
        {
            3 => 5,
            5 => 10,
            10 => 15,
            15 => 20,
            20 => 3,
            _ => 5
        };
        Save();
    }

    public void ToggleExtraTime()
    {
        ExtraTimeEnabled = !ExtraTimeEnabled;
        Save();
    }

    public void TogglePenalties()
    {
        PenaltyShootoutEnabled = !PenaltyShootoutEnabled;
        Save();
    }
}
