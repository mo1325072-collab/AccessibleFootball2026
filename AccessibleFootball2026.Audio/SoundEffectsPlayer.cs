using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace AccessibleFootball2026.Audio;

/// <summary>
/// مشغل الصوتيات متعدد القنوات فائق الجودة والسرعة (Multi-Channel Audio Engine عبر Win32 MCI):
/// يشغل عدة مسارات صوتية وموسيقية في نفس اللحظة بدون مقاطعة:
/// - صوت صخب وهتاف الجماهير المستمر في الخلفية (Crowd Ambience Loop)
/// - ركلات ودحرجة الكرة ثلاثية الأبعاد (3D Spatial Panning)
/// - موسيقى القوائم الهادئة (Menu Music Loop)
/// - صافرة الحكم واهتزاز الشباك
/// مع دعم صيغ WAV و MP3 الحقيقية تلقائياً وبدون أي تحويل معقد
/// </summary>
public static class SoundEffectsPlayer
{

    private static string _baseAudioDir = Path.Combine(AppContext.BaseDirectory,"Assets","Audio");
    private static readonly object _lock = new();

    private static bool _menuMusicPlaying = false;
    private static bool _menuMusicRequested;
    private static bool _crowdPlaying = false;
    private static int _lastReportedCrowdVol = -1;
    private static DateTime _lastCrowdVolChange = DateTime.MinValue;

    public static int MasterVolumeSetting { get; set; } = 100; // 0 - 100
    public static int SfxVolumeSetting { get; set; } = 90; // 0 - 100
    public static int CommentaryVolumeSetting { get; set; } = 90; // 0 - 100
    public static int CrowdVolumeSetting { get; set; } = 35; // 0 - 100 (هادئ وواقعي 35%)
    public static int GoalBeaconVolumeSetting { get; set; } = 80; // 0 - 100
    public static int MusicVolumeSetting { get; set; } = 35; // 0 - 100 (موسيقى هادئة 35%)

    public static float GetEffectiveMasterVolume() => Math.Clamp(MasterVolumeSetting / 100.0f, 0.0f, 1.0f);
    public static float GetEffectiveSfxVolume() => GetEffectiveMasterVolume() * Math.Clamp(SfxVolumeSetting / 100.0f, 0.0f, 1.0f);
    public static float GetEffectiveCommentaryVolume() => GetEffectiveMasterVolume() * Math.Clamp(CommentaryVolumeSetting / 100.0f, 0.0f, 1.0f);
    public static float GetEffectiveCrowdVolume() => GetEffectiveMasterVolume() * Math.Clamp(CrowdVolumeSetting / 100.0f, 0.0f, 1.0f);
    public static float GetEffectiveBeaconVolume() => GetEffectiveMasterVolume() * Math.Clamp(GoalBeaconVolumeSetting / 100.0f, 0.0f, 1.0f);
    public static float GetEffectiveMusicVolume() => GetEffectiveMasterVolume() * Math.Clamp(MusicVolumeSetting / 100.0f, 0.0f, 1.0f);

    private static bool _crowdPaused;
    private static bool _crowdRequested;
    private static string? _stadiumAcousticPath;
    private static int _crowdIntensity = 350;
    private static float _crowdDucking = 1f;
    private static bool _commentaryEnabled = true;
    private static readonly Dictionary<string, float> _activeOneShots = new();
    private static readonly Dictionary<string, float> _activeOneShotPans = new();
    private static float ChannelVolume(string alias) => alias == "comm_voice" ? (_commentaryEnabled ? GetEffectiveCommentaryVolume() : 0)
        : alias == "chant_ch" || alias.StartsWith("crowd_", StringComparison.Ordinal) ? GetEffectiveCrowdVolume() : GetEffectiveSfxVolume();
    public static bool IsCrowdActive => _crowdPlaying && !_crowdPaused && GetEffectiveCrowdVolume() > .001f;
    public static string? LastPlayedSound { get; private set; }
    public static string? LastPlayedFilePath { get; private set; }

    private static readonly List<string> _syntheticAssetsGenerated = new();
    /// <summary>مسارات العينات التي ولّدها المحرك إجرائياً خلال هذه الجلسة لأن الأصل المستخرج كان غائباً (تشخيص فقط)</summary>
    public static IReadOnlyList<string> SyntheticAssetsGenerated => _syntheticAssetsGenerated;
    /// <summary>
    /// true إذا كان آخر مسار رجعه ResolveAudioPath يحمل اسماً من مجموعة التوليد الإجرائي.
    /// العينات المستخرجة من أصول PES تحمل أسماء رقمية (Kick_101.wav)، أما الأسماء الوصفية فيكتبها المولد
    /// المحلي وحده (لذلك يتجاهلها ResolveExtractedSample)، فوجودها يعني عينة مولدة حتى لو كتبتها جلسة سابقة.
    /// لا يشمل موسيقى القوائم والإياركونات فلهما مولدات خاصة غير مراقبة هنا.
    /// </summary>
    public static bool LastResolveWasSynthetic { get; private set; }

    // الأسماء الوصفية التي يملكها EnsureDefaultAudioAssets (بدون امتداد)، بنفس صيغة relativeParts
    private static readonly HashSet<string> _syntheticRelativeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "SFX/Kicks/Shot", "SFX/Kicks/Shot_Center", "SFX/Kicks/Shot_Left", "SFX/Kicks/Shot_Right",
        "SFX/Kicks/Pass", "SFX/Kicks/Pass_Center", "SFX/Kicks/Pass_Left", "SFX/Kicks/Pass_Right",
        "SFX/Kicks/Cross", "SFX/Kicks/Cross_Center", "SFX/Kicks/Cross_Left", "SFX/Kicks/Cross_Right",
        "SFX/Kicks/Kick", "SFX/Kicks/Kick_Center", "SFX/Kicks/Kick_Left", "SFX/Kicks/Kick_Right",
        "SFX/Tackles/StandTackle", "SFX/Tackles/SlideTackle",
        "SFX/Whistles/Whistle", "SFX/GoalNet/GoalNet", "SFX/PostHit/PostHit"
    };
    public static float LastPlayedPan { get; private set; }
    public static int TotalSoundsPlayedCount { get; private set; } = 0;

    public static void ResetTelemetry()
    {
        LastPlayedSound = null;
        LastPlayedFilePath = null;
        LastPlayedPan = 0.0f;
        TotalSoundsPlayedCount = 0;
        _commentaryBusyUntil = DateTime.MinValue;
    }

    public static string ResolveBestAudioBaseDirectory(params string?[] candidates)
    {
        var paths = candidates.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        // Prefer a real commentary library in the caller's order. A generated menu tone alone
        // must not shadow an installed library, and an old D: install must not shadow a packaged one.
        foreach (string path in paths)
        {
            foreach (string bank in new[] { "FaresAwad", "RaoufKhlif" })
            {
                if (File.Exists(Path.Combine(path, "Commentary", bank, "commentary_context.json")) ||
                    File.Exists(Path.Combine(path, "Commentary", bank, "commentary_map.json"))) return path;
            }
        }
        foreach (string path in paths)
        {
            try { if (Directory.Exists(path) && Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Any()) return path; }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        return paths.FirstOrDefault() ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio");
    }

    public static string BaseAudioDirectory => _baseAudioDir;

    public static void Initialize(string? dir = null)
    {
        if (!string.IsNullOrEmpty(dir)) _baseAudioDir = dir;
        EnsureDefaultAudioAssets();
        AccessibleFootball2026.Core.Models.MatchSettings.OnSettingsChanged -= ApplyLiveSettings;
        AccessibleFootball2026.Core.Models.MatchSettings.OnSettingsChanged += ApplyLiveSettings;
        ApplyLiveSettings(AccessibleFootball2026.Core.Models.MatchSettings.Current);
    }

    public static void ApplyLiveSettings(AccessibleFootball2026.Core.Models.MatchSettings settings)
    {
        MasterVolumeSetting = settings.MasterVolumePercent;
        SfxVolumeSetting = settings.SfxVolumePercent;
        CommentaryVolumeSetting = settings.CommentaryVolumePercent;
        CrowdVolumeSetting = settings.CrowdVolumePercent;
        GoalBeaconVolumeSetting = settings.GoalBeaconVolumePercent;
        MusicVolumeSetting = settings.MusicVolumePercent;

        _commentaryEnabled = settings.CommentaryEnabled && settings.CommentaryVoice != AccessibleFootball2026.Core.Models.CommentaryVoiceOption.Off;
        ApplyMusicVolume();
        ApplyCrowdVolume();
        lock (_lock)
        {
            foreach (var alias in _activeOneShots.Keys.ToArray())
            {
                int volume = (int)(ChannelVolume(alias) * _activeOneShots[alias] * 1000);
                bool applied = ApplyOneShotVolume(alias, volume, _activeOneShotPans.TryGetValue(alias, out var pan) ? pan : null);
                if (volume == 0 || !applied) { SendMci($"stop {alias}"); SendMci($"close {alias}"); _activeOneShots.Remove(alias); _activeOneShotPans.Remove(alias); }
            }
        }
        if (!_commentaryEnabled || GetEffectiveCommentaryVolume() <= .001f) StopCommentaryAudio();
        GoalBeaconPlayer.ApplyLiveVolume();
        AnthemEngineVolume();
    }

    private static void AnthemEngineVolume() => SendMci($"setaudio anthem volume to {(int)(GetEffectiveMusicVolume() * 1000)}");

    private static void SendMci(string command)
    {
        try
        {
            MciPlayback.SendString(command, null, 0, IntPtr.Zero);
        }
        catch { }
    }

    public static string? ResolveAudioPath(params string[] relativeParts)
    {
        string fullWithoutExt = Path.Combine(_baseAudioDir, Path.Combine(relativeParts));
        bool syntheticName = _syntheticRelativeNames.Contains(string.Join('/', relativeParts));
        // نفضل ملفات WAV ثم MP3
        if (File.Exists(fullWithoutExt + ".wav")) { LastResolveWasSynthetic = syntheticName; return fullWithoutExt + ".wav"; }
        if (File.Exists(fullWithoutExt + ".mp3")) { LastResolveWasSynthetic = syntheticName; return fullWithoutExt + ".mp3"; }
        if (File.Exists(fullWithoutExt)) { LastResolveWasSynthetic = syntheticName; return fullWithoutExt; }

        // توليد الملفات الصوتية تلقائياً إذا لم تكن موجودة
        EnsureDefaultAudioAssets();

        if (File.Exists(fullWithoutExt + ".wav")) { LastResolveWasSynthetic = syntheticName || _syntheticAssetsGenerated.Contains(fullWithoutExt + ".wav"); return fullWithoutExt + ".wav"; }
        if (File.Exists(fullWithoutExt + ".mp3")) { LastResolveWasSynthetic = syntheticName || _syntheticAssetsGenerated.Contains(fullWithoutExt + ".mp3"); return fullWithoutExt + ".mp3"; }
        if (File.Exists(fullWithoutExt)) { LastResolveWasSynthetic = syntheticName || _syntheticAssetsGenerated.Contains(fullWithoutExt); return fullWithoutExt; }

        LastResolveWasSynthetic = false;
        return null;
    }

    /// <summary>
    /// حل مسار ملف صوتي موجود فعلاً بدون توليد أي عيّنات بديلة
    /// </summary>
    public static string? TryResolveExistingAudioPath(params string[] relativeParts)
    {
        string fullWithoutExt = Path.Combine(_baseAudioDir, Path.Combine(relativeParts));
        if (File.Exists(fullWithoutExt + ".wav")) return fullWithoutExt + ".wav";
        if (File.Exists(fullWithoutExt + ".mp3")) return fullWithoutExt + ".mp3";
        if (File.Exists(fullWithoutExt)) return fullWithoutExt;
        return null;
    }

    private static readonly Dictionary<string, string[]> _extractedDirCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// يختار مقطعاً حقيقياً مستخرجاً من مكتبة صوتيات PES 2013 بأسمائها الرقمية (مثل Kick_101.wav أو Whistle_12.wav)
    /// حتى تُستخدم العيّنات الأصلية بدل التوليد الإجرائي عند توفرها. يتجاهل الأسماء الوصفية المولدة محلياً.
    /// </summary>
    public static string? ResolveExtractedSample(string relativeDir, params string[] prefixes)
    {
        string dir = Path.Combine(_baseAudioDir, relativeDir);
        if (!_extractedDirCache.TryGetValue(dir, out var files))
        {
            try
            {
                files = Directory.Exists(dir)
                    ? Directory.GetFiles(dir, "*.*")
                        .Where(f => f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                        .ToArray()
                    : Array.Empty<string>();
            }
            catch { files = Array.Empty<string>(); }
            _extractedDirCache[dir] = files;
        }
        if (files.Length == 0) return null;

        var matches = files.Where(f =>
        {
            string name = Path.GetFileNameWithoutExtension(f);
            foreach (var prefix in prefixes)
            {
                if (name.Length > prefix.Length + 1
                    && name.StartsWith(prefix + "_", StringComparison.OrdinalIgnoreCase)
                    && name[(prefix.Length + 1)..].All(char.IsDigit))
                {
                    return true;
                }
            }
            return false;
        }).ToArray();

        return matches.Length > 0 ? matches[Random.Shared.Next(matches.Length)] : null;
    }

    /// <summary>
    /// مدة ملف WAV بالثواني من ترويسته (لضبط نافذة انشغال قناة المعلق على المدة الحقيقية للمقطع)
    /// </summary>
    public static double GetWavDurationSeconds(string filePath)
    {
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (fs.Length < 44) return 0;
            using var br = new BinaryReader(fs);
            if (br.ReadUInt32() != 0x46464952) return 0; // "RIFF"
            br.ReadUInt32();
            if (br.ReadUInt32() != 0x45564157) return 0; // "WAVE"

            int byteRate = 0;
            long dataSize = -1;
            while (fs.Position + 8 <= fs.Length)
            {
                uint chunkId = br.ReadUInt32();
                uint chunkSize = br.ReadUInt32();
                long nextChunk = fs.Position + chunkSize;
                if (chunkId == 0x20746D66 && chunkSize >= 16) // "fmt "
                {
                    br.ReadUInt16(); // audio format
                    br.ReadUInt16(); // channels
                    br.ReadUInt32(); // sample rate
                    byteRate = br.ReadInt32();
                }
                else if (chunkId == 0x61746164) // "data"
                {
                    dataSize = chunkSize;
                    break;
                }
                fs.Position = nextChunk;
            }
            if (byteRate > 0 && dataSize > 0) return (double)dataSize / byteRate;
        }
        catch { }
        return 0;
    }

    public static string GetMciType(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext == ".mp3" ? "mpegvideo" : "waveaudio";
    }

    // =========================================================================
    // 1. موسيقى القوائم (Menu BGM Loop)
    // =========================================================================
    public static void StartMenuMusic()
    {
        lock (_lock)
        {
            _menuMusicRequested = true;
            // إيقاف أي نشيد أو صوت افتتاحية عند الدخول إلى القوائم
            SendMci("stop anthem");
            SendMci("close anthem");

            // إيقاف صخب الجماهير أيضاً: معاينة "أجواء المدرجات" في أكاديمية الأصوات
            // كانت تتركه يعمل فوق موسيقى القوائم بعد الرجوع
            StopCrowdAmbiance();

            if (_menuMusicPlaying) return;
            if (GetEffectiveMusicVolume() <= 0.001f) return;

            // A supplied MP3 must win over a legacy synthetic WAV generated by an older build.
            string suppliedMp3 = Path.Combine(_baseAudioDir, "Menu", "MenuMusic.mp3");
            string? p = File.Exists(suppliedMp3) && new FileInfo(suppliedMp3).Length > 100
                ? suppliedMp3 : ResolveAudioPath("Menu", "MenuMusic");
            if (p != null)
            {
                SendMci("close bgm_menu");
                if (MciPlayback.SendString($"open \"{p}\" type mpegvideo alias bgm_menu", null, 0, IntPtr.Zero) != 0) return;
                int vol = Math.Clamp((int)(GetEffectiveMusicVolume() * 1000), 0, 1000);
                if (MciPlayback.SendString($"setaudio bgm_menu volume to {vol}", null, 0, IntPtr.Zero) != 0 ||
                    MciPlayback.SendString("play bgm_menu repeat", null, 0, IntPtr.Zero) != 0)
                { SendMci("close bgm_menu"); return; }
                _menuMusicPlaying = true;
            }
        }
    }

    public static void StopMenuMusic()
    {
        lock (_lock)
        {
            _menuMusicRequested = false;
            SendMci("stop bgm_menu");
            SendMci("close bgm_menu");
            _menuMusicPlaying = false;
        }
    }

    // =========================================================================
    // 2. صخب الجماهير المستمر في الملعب (Crowd Ambience Loop)
    // =========================================================================
    public static void StartCrowdAmbiance()
    {
        lock (_lock)
        {
            _crowdRequested = true;
            _crowdPaused = false;
            StartRequestedCrowd();
        }
    }

    private static void StartRequestedCrowd()
    {
        if (!_crowdRequested || _crowdPaused || _crowdPlaying || GetEffectiveCrowdVolume() <= .001f) return;
        string? path = _stadiumAcousticPath ?? ResolveAudioPath("SFX", "Crowd", "CrowdAmbiance");
        if (path == null) return;
        SendMci("close crowd");
        if (MciPlayback.SendString($"open \"{path}\" type mpegvideo alias crowd", null, 0, IntPtr.Zero) != 0) return;
        int volume = Math.Clamp((int)(_crowdIntensity * _crowdDucking * GetEffectiveCrowdVolume()), 0, 1000);
        if (MciPlayback.SendString($"setaudio crowd volume to {volume}", null, 0, IntPtr.Zero) != 0) { SendMci("close crowd"); return; }
        if (MciPlayback.SendString("play crowd repeat", null, 0, IntPtr.Zero) != 0) { SendMci("close crowd"); return; }
        _crowdPlaying = true;
        _lastReportedCrowdVol = volume;
        _lastCrowdVolChange = DateTime.UtcNow;
    }

    private static void CloseCrowdChannel()
    {
        SendMci("stop crowd"); SendMci("close crowd");
        _crowdPlaying = false;
        _lastReportedCrowdVol = -1;
    }

    public static void StopCrowdAmbiance()
    {
        lock (_lock)
        {
            _crowdRequested = false; _crowdPaused = false; _stadiumAcousticPath = null;
            _crowdIntensity = 350; _crowdDucking = 1;
            CloseCrowdChannel();
        }
    }

    public static void PauseCrowdAmbiance()
    {
        lock (_lock) { SendMci("pause crowd"); _crowdPaused = true; }
    }

    public static void ResumeCrowdAmbiance()
    {
        lock (_lock)
        {
            _crowdPaused = false;
            if (GetEffectiveCrowdVolume() <= .001f) { CloseCrowdChannel(); return; }
            if (_crowdPlaying) SendMci("resume crowd"); else StartRequestedCrowd();
        }
    }

    public static void ApplyMusicVolume()
    {
        int vol = Math.Clamp((int)(GetEffectiveMusicVolume() * 1000), 0, 1000);
        // تغيير مستوى الصوت لا يبدأ الموسيقى أبداً: تعديل الإعدادات من قائمة الإيقاف
        // أثناء المباراة (حيث تتوقف موسيقى القوائم عمداً) كان يشغّلها فوق اللعب
        if (!_menuMusicPlaying)
        {
            if (_menuMusicRequested && vol > 0) StartMenuMusic();
            return;
        }
        if (vol <= 0)
        {
            SendMci("pause bgm_menu");
            SendMci("setaudio bgm_menu volume to 0");
        }
        else
        {
            SendMci($"setaudio bgm_menu volume to {vol}");
            SendMci("play bgm_menu repeat");
        }
    }

    public static void ApplyCrowdVolume()
    {
        lock (_lock) UpdateCrowdChannel(force: true);
    }

    public static void SetCrowdVolume(int volumeOut1000, bool force = false)
    {
        lock (_lock)
        {
            _crowdIntensity = Math.Clamp(volumeOut1000, 0, 1000);
            UpdateCrowdChannel(force);
        }
    }

    public static void SetCrowdDucking(float factor)
    {
        lock (_lock) { _crowdDucking = Math.Clamp(factor, 0, 1); UpdateCrowdChannel(force: true); }
    }

    private static void UpdateCrowdChannel(bool force)
    {
        if (GetEffectiveCrowdVolume() <= .001f) { CloseCrowdChannel(); return; }
        if (!_crowdRequested || _crowdPaused) return; // Saving in a menu must not start a match ambience.
        StartRequestedCrowd();
        if (!_crowdPlaying) return;
        int volume = Math.Clamp((int)(_crowdIntensity * _crowdDucking * GetEffectiveCrowdVolume()), 0, 1000);
        var now = DateTime.UtcNow;
        if (!force && Math.Abs(volume - _lastReportedCrowdVol) < 30 && (now - _lastCrowdVolChange).TotalMilliseconds < 250) return;
        _lastReportedCrowdVol = volume; _lastCrowdVolChange = now;
        SendMci($"setaudio crowd volume to {volume}");
    }

    public static void PlayCrowdMiss()
    {
        if (GetEffectiveCrowdVolume() <= 0.001f) return;
        string? p = ResolveAudioPath("SFX", "Crowd", "CrowdMiss") 
                 ?? ResolveAudioPath("SFX", "Crowd", "CrowdGasp");
        if (p != null) PlayOneShot(p, "crowd_miss", customVolume: GetEffectiveCrowdVolume());
    }

    public static void PlayCrowdGoal()
    {
        if (GetEffectiveCrowdVolume() <= 0.001f) return;
        string? p = ResolveAudioPath("SFX", "Crowd", "CrowdGoal") 
                 ?? ResolveAudioPath("SFX", "Crowd", "CrowdCheer");
        if (p != null) PlayOneShot(p, "crowd_goal", customVolume: GetEffectiveCrowdVolume());
    }

    public static void PlayCrowdCheer()
    {
        if (GetEffectiveCrowdVolume() <= 0.001f) return;
        string? p = ResolveAudioPath("SFX", "Crowd", "CrowdCheer") 
                 ?? ResolveAudioPath("SFX", "Crowd", "CrowdGoal");
        if (p != null) PlayOneShot(p, "crowd_cheer", customVolume: GetEffectiveCrowdVolume());
    }

    /// <summary>
    /// تصفيق جماهير حقيقي (تسجيل فعلي مرخص CC0 في SFX/Crowd/CrowdApplause، ليس توليداً صناعياً).
    /// يعيد true فقط إذا وُجد التسجيل الحقيقي وبدأ تشغيله؛ غياب الملف يعني لا تصفيق ولا تعليق يصفه.
    /// </summary>
    public static bool PlayCrowdApplause()
    {
        if (GetEffectiveCrowdVolume() <= 0.001f) return false;
        string? p = ResolveAudioPath("SFX", "Crowd", "CrowdApplause");
        if (p == null || LastResolveWasSynthetic) return false;
        return PlayOneShot(p, "crowd_applause", customVolume: GetEffectiveCrowdVolume());
    }

    // A failed status query is not evidence of completion. StopAll still clears the channel.
    public static bool IsTeamChantActive
    {
        get
        {
            lock (_lock)
            {
                if (!_activeOneShots.ContainsKey("chant_ch")) return false;
                var mode = new System.Text.StringBuilder(64);
                int result = MciPlayback.SendString("status chant_ch mode", mode, 64, IntPtr.Zero);
                return result != 0 || !mode.ToString().Trim().Equals("stopped", StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    public static void PlayOneShotChant(string filePath)
    {
        lock (_lock)
        {
            if (IsTeamChantActive || string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;
            PlayOneShot(filePath, "chant_ch", customVolume: GetEffectiveCrowdVolume());
        }
    }

    public static bool TryPlayAudibleChant(string filePath) => PlayOneShot(filePath, "chant_ch", customVolume: GetEffectiveCrowdVolume());

    public static void SetStadiumAcoustics(string acousticPath)
    {
        if (string.IsNullOrEmpty(acousticPath) || !File.Exists(acousticPath)) return;
        lock (_lock)
        {
            CloseCrowdChannel();
            _stadiumAcousticPath = acousticPath;
            _crowdIntensity = 800;
            _crowdRequested = true; _crowdPaused = false;
            StartRequestedCrowd(); // Volume is applied before play, and zero never opens the channel.
        }
    }

    // =========================================================================
    // 3. تنبيه حدود الملعب (Touchline / Goal Line Alert)
    // =========================================================================
    public static void PlayBoundaryWarning()
    {
        string? p = ResolveAudioPath("SFX", "Movement", "BoundaryWarning");
        if (p != null) PlayOneShot(p, "boundary");
    }

    // =========================================================================
    // 4. الركلات وصافرة الحكم واهتزاز الشباك واستخلاص الكرة
    // =========================================================================
    /// <summary>Academy previews never substitute procedural match impacts for missing recordings.</summary>
    public static bool TryPlayAcademySample(string soundId)
    {
        if (new[] { "TackleWon", "TackleMissed", "BallLost", "Foul", "Penalty", "Corner", "GoalKick", "ThrowIn", "PlayerSwitch" }.Contains(soundId))
            return PlayOneShot(Path.Combine(_baseAudioDir, "SFX", "Earcons", soundId + ".wav"), "academy_earcon");
        string? path = soundId switch
        {
            "BallKick" or "PowerfulShot" => ResolveExtractedSample(Path.Combine("SFX", "Kicks"), "Kick", "Impact"),
            "RefereeWhistle" => ResolveExtractedSample(Path.Combine("SFX", "Whistles"), "Whistle"),
            "GoalNet" => ResolveExtractedSample(Path.Combine("SFX", "GoalNet"), "GoalNet"),
            "WoodworkHit" => ResolveOriginalMappedSample("SFX", "PostHit", "PostHit") ?? ResolveExtractedSample(Path.Combine("SFX", "PostHit"), "PostHit"),
            "GoalkeeperSave" => TryResolveExistingAudioPath("SFX", "Saves", "GoalkeeperSave"),
            "CrowdAmbiance" => TryResolveExistingAudioPath("SFX", "Crowd", "CrowdAmbiance"),
            "GoalRoar" => TryResolveExistingAudioPath("SFX", "Crowd", "CrowdGoal") ?? TryResolveExistingAudioPath("SFX", "Crowd", "CrowdCheer"),
            "MissGasp" => TryResolveExistingAudioPath("SFX", "Crowd", "CrowdMiss") ?? TryResolveExistingAudioPath("SFX", "Crowd", "CrowdGasp"),
            _ => null
        };
        // Only the documented recorded save path is accepted; a shot kick is not a save.
        return path != null && PlayOneShot(path, soundId is "CrowdAmbiance" or "GoalRoar" or "MissGasp" ? "crowd_academy" : "academy_sfx");
    }

    // Named files can also be generated fallbacks. Only known original bytes may
    // outrank numeric originals or be presented as original academy recordings.
    private static readonly Dictionary<string, string> OriginalMappedHashes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SFX/PostHit/PostHit"] = "6043c420a1d463d946725d1942bf8b2233f172de243ec85f2ae8f9431acdb755",
        ["SFX/Kicks/Kick_Center"] = "a8c938948d6080f4350f3c96c48eecb9ff229954880fe8c0f655b6f3640a5c89",
        ["SFX/Kicks/Shot_Center"] = "867752d427aabad52039331f94e622e4edd125d2470773cc3215d990ec93d9bc",
        ["SFX/Kicks/Pass_Center"] = "a8c938948d6080f4350f3c96c48eecb9ff229954880fe8c0f655b6f3640a5c89",
        ["SFX/Kicks/Cross_Center"] = "867752d427aabad52039331f94e622e4edd125d2470773cc3215d990ec93d9bc",
    };
    private static string? ResolveOriginalMappedSample(params string[] parts)
    {
        string key = string.Join('/', parts);
        string? path = TryResolveExistingAudioPath(parts);
        if (path == null || !OriginalMappedHashes.TryGetValue(key, out var expected)) return null;
        try
        {
            using var file = File.OpenRead(path);
            return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(file)).Equals(expected, StringComparison.OrdinalIgnoreCase) ? path : null;
        }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
    }

    public static void PlayKick(float pan = 0.0f)
    {
        LastPlayedPan = float.IsFinite(pan) ? Math.Clamp(pan, -1, 1) : 0;
        pan = LastPlayedPan;
        string fileName = "Kick_Center";
        string? p = ResolveOriginalMappedSample("SFX", "Kicks", fileName)
                 ?? ResolveExtractedSample(Path.Combine("SFX", "Kicks"), "Kick", "Impact")
                 ?? ResolveAudioPath("SFX", "Kicks", fileName) 
                 ?? ResolveAudioPath("SFX", "Kicks", "Kick")
                 ?? ResolveAudioPath("SFX", "Kicks", "Pass");
        if (p != null) PlayOneShot(p, "kick", pan: pan);
    }

    public static void PlayShotKick(float pan = 0.0f)
    {
        LastPlayedPan = float.IsFinite(pan) ? Math.Clamp(pan, -1, 1) : 0;
        pan = LastPlayedPan;
        string fileName = "Shot_Center";
        string? p = ResolveOriginalMappedSample("SFX", "Kicks", fileName)
                 ?? ResolveExtractedSample(Path.Combine("SFX", "Kicks"), "Kick", "Impact")
                 ?? ResolveAudioPath("SFX", "Kicks", fileName) 
                 ?? ResolveAudioPath("SFX", "Kicks", "Shot") 
                 ?? ResolveAudioPath("SFX", "Kicks", "Kick_Center");
        if (p != null) PlayOneShot(p, "shot_kick", pan: pan);
    }

    public static void PlayPassKick(float pan = 0.0f)
    {
        LastPlayedPan = float.IsFinite(pan) ? Math.Clamp(pan, -1, 1) : 0;
        pan = LastPlayedPan;
        string fileName = "Pass_Center";
        string? p = ResolveOriginalMappedSample("SFX", "Kicks", fileName)
                 ?? ResolveExtractedSample(Path.Combine("SFX", "Kicks"), "Kick", "Impact")
                 ?? ResolveAudioPath("SFX", "Kicks", fileName) 
                 ?? ResolveAudioPath("SFX", "Kicks", "Pass") 
                 ?? ResolveAudioPath("SFX", "Kicks", "Kick_Center");
        if (p != null) PlayOneShot(p, "pass_kick", pan: pan);
    }

    public static void PlayCrossKick(float pan = 0.0f)
    {
        LastPlayedPan = float.IsFinite(pan) ? Math.Clamp(pan, -1, 1) : 0;
        pan = LastPlayedPan;
        string fileName = "Cross_Center";
        string? p = ResolveOriginalMappedSample("SFX", "Kicks", fileName)
                 ?? ResolveExtractedSample(Path.Combine("SFX", "Kicks"), "Kick", "Impact")
                 ?? ResolveAudioPath("SFX", "Kicks", fileName) 
                 ?? ResolveAudioPath("SFX", "Kicks", "Cross") 
                 ?? ResolveAudioPath("SFX", "Kicks", "Kick_Center");
        if (p != null) PlayOneShot(p, "cross_kick", pan: pan);
    }

    /// <summary>No pass-kick or generated substitute is presented as a goalkeeper-save recording.</summary>
    public static bool TryPlayGoalkeeperSave(float pan = 0.0f)
    {
        string? path = TryResolveExistingAudioPath("SFX", "Saves", "GoalkeeperSave");
        return path != null && PlayOneShot(path, "keeper_save", pan: pan);
    }

    public static void PlayStandTackle(float pan = 0.0f)
    {
        LastPlayedPan = float.IsFinite(pan) ? Math.Clamp(pan, -1, 1) : 0;
        pan = LastPlayedPan;
        string? p = ResolveAudioPath("SFX", "Tackles", "StandTackle")
                 ?? ResolveAudioPath("SFX", "Tackles", "Tackle")
                 ?? ResolveExtractedSample(Path.Combine("SFX", "Kicks"), "Impact", "Kick")
                 ?? ResolveAudioPath("SFX", "Kicks", "Pass");
        if (p != null) PlayOneShot(p, "stand_tackle", pan: pan);
    }

    public static void PlaySlideTackle(float pan = 0.0f)
    {
        LastPlayedPan = float.IsFinite(pan) ? Math.Clamp(pan, -1, 1) : 0;
        pan = LastPlayedPan;
        string? p = ResolveAudioPath("SFX", "Tackles", "SlideTackle")
                 ?? ResolveAudioPath("SFX", "Tackles", "Tackle")
                 ?? ResolveExtractedSample(Path.Combine("SFX", "Kicks"), "Impact", "Kick")
                 ?? ResolveAudioPath("SFX", "Kicks", "Shot");
        if (p != null) PlayOneShot(p, "slide_tackle", pan: pan);
    }

    public static void PlayWhistle()
    {
        string? p = ResolveExtractedSample(Path.Combine("SFX", "Whistles"), "Whistle")
                 ?? ResolveAudioPath("SFX", "Whistles", "Whistle");
        if (p != null) PlayOneShot(p, "whistle");
    }

    public static void PlayGoalNet()
    {
        string? p = ResolveAudioPath("SFX", "GoalNet", "GoalNet");
        if (p != null) PlayOneShot(p, "goal_net");
    }

    public static void PlayPostHit()
    {
        string? p = ResolveAudioPath("SFX", "PostHit", "PostHit");
        if (p != null) PlayOneShot(p, "post_hit");
    }

    // =========================================================================
    // نغمات التنبيه التكتيكية الموسيقية الثمانية (8 Tonal Earcons)
    // =========================================================================
    public static void PlayEarcon(string earconName)
    {
        string earconPath = Path.Combine(_baseAudioDir, "SFX", "Earcons", earconName + ".wav");
        if (!File.Exists(earconPath))
        {
            EarconGenerator.EnsureEarconExists(earconPath, earconName);
        }

        PlayOneShot(earconPath, "earcon_ch", earconName);
    }

    public static void StopCommentaryAudio()
    {
        lock (_lock)
        {
            _commentaryBusyUntil = DateTime.MinValue;
            SendMci("stop comm_voice");
            SendMci("close comm_voice");
        }
    }

    public static void PlayTackleWon() => PlayEarcon("TackleWon");
    public static void PlayBallLost() => PlayEarcon("BallLost");
    public static void PlayFoul() => PlayEarcon("Foul");
    public static void PlayPenalty() => PlayEarcon("Penalty");
    public static void PlayCorner() => PlayEarcon("Corner");
    public static void PlayGoalKick() => PlayEarcon("GoalKick");
    public static void PlayThrowIn() => PlayEarcon("ThrowIn");
    public static void PlayPlayerSwitch() => PlayEarcon("PlayerSwitch");
    public static void PlayTackleMissed() => PlayEarcon("TackleMissed");

    // =========================================================================
    // 5. أصوات المعلقين الحقيقية المسجلة
    // =========================================================================
    private static DateTime _commentaryBusyUntil = DateTime.MinValue;

    public static bool IsCommentaryPlaying()
    {
        if (!_commentaryEnabled || GetEffectiveCommentaryVolume() <= .001f) return false;
        if (DateTime.UtcNow < _commentaryBusyUntil) return true;
        try
        {
            var sb = new StringBuilder(128);
            int err = MciPlayback.SendString("status comm_voice mode", sb, sb.Capacity, IntPtr.Zero);
            if (err == 0 && sb.ToString().Equals("playing", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        catch { }
        return false;
    }

    public static bool PlayCommentaryAudio(string filePath)
    {
        if (!_commentaryEnabled || GetEffectiveCommentaryVolume() <= .001f || string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return false;
        // نافذة الانشغال تساوي المدة الحقيقية للمقطع (نداءات الأسماء القصيرة لا تحجب التعليق 3 ثوانٍ كاملة)
        double seconds = filePath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ? GetWavDurationSeconds(filePath) : 0;
        if (seconds <= 0.05) seconds = 3.2;
        _commentaryBusyUntil = DateTime.UtcNow.AddSeconds(Math.Clamp(seconds, 0.3, 30.0));
        bool played = PlayOneShot(filePath, "comm_voice");
        if (!played) _commentaryBusyUntil = DateTime.MinValue;
        return played;
    }

    // =========================================================================
    // 6. أصوات القوائم
    // =========================================================================
    public static void PlayMenuMove()
    {
        string? p = ResolveAudioPath("Menu", "Move");
        if (p != null) PlayOneShot(p, "ui_move");
    }

    public static void PlayMenuSelect()
    {
        string? p = ResolveAudioPath("Menu", "Select");
        if (p != null) PlayOneShot(p, "ui_select");
    }

    public static void PlayMenuBack()
    {
        string? p = ResolveAudioPath("Menu", "Back");
        if (p != null) PlayOneShot(p, "ui_back");
    }

    // =========================================================================
    // المعاينة الحية لمستويات الصوت (Live Audio Previews)
    // =========================================================================
    public static void PlayLivePreviewMaster(int masterVal)
    {
        MasterVolumeSetting = Math.Clamp(masterVal, 0, 100);
        PlayEarcon("PlayerSwitch");
    }

    public static void PlayLivePreviewSfx(int sfxVal)
    {
        SfxVolumeSetting = Math.Clamp(sfxVal, 0, 100);
        PlayTackleWon();
    }

    public static void PlayLivePreviewCommentary(int commVal)
    {
        CommentaryVolumeSetting = Math.Clamp(commVal, 0, 100);
        // نغمة تأكيد هادئة بدون أي جمل عشوائية للمعلق تلبية لتوجيهات المستخدم
        PlayMenuSelect();
    }

    public static void PlayLivePreviewCrowd(int crowdVal)
    {
        CrowdVolumeSetting = Math.Clamp(crowdVal, 0, 100);
        ApplyCrowdVolume();
        PlayCrowdCheer(); // A bounded explicit preview, never a new ambience loop in the menu.
    }

    public static void PlayLivePreviewGoalBeacon(int beaconVal)
    {
        GoalBeaconVolumeSetting = Math.Clamp(beaconVal, 0, 100);
        GoalBeaconPlayer.PlayBeaconPulse(1f, 0.0f);
    }

    private static bool PlayOneShot(string filePath, string alias, string? soundName = null, float? customVolume = null, float? pan = null)
    {
        LastPlayedSound = soundName ?? alias; LastPlayedFilePath = filePath; TotalSoundsPlayedCount++;
        float volume = customVolume ?? ChannelVolume(alias);
        if (volume <= .001f || string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return false;
        lock (_lock)
        {
            string type = alias is "comm_voice" or "chant_ch" ? "mpegvideo" : GetMciType(filePath);
            SendMci($"close {alias}");
            if (MciPlayback.SendString($"open \"{filePath}\" type {type} alias {alias}", null, 0, IntPtr.Zero) != 0) return false;
            if (!ApplyOneShotVolume(alias, Math.Clamp((int)(volume * 1000), 0, 1000), pan) ||
                MciPlayback.SendString($"play {alias} from 0", null, 0, IntPtr.Zero) != 0) { SendMci($"close {alias}"); return false; }
            _activeOneShots[alias] = 1f;
            if (pan.HasValue) _activeOneShotPans[alias] = pan.Value;
            else _activeOneShotPans.Remove(alias);
            return true;
        }
    }

    private static bool ApplyOneShotVolume(string alias, int volume, float? pan)
    {
        if (MciPlayback.SendString($"setaudio {alias} volume to {volume}", null, 0, IntPtr.Zero) != 0) return false;
        if (!pan.HasValue) return true;
        int left = (int)(volume * (pan.Value > 0 ? 1 - pan.Value : 1));
        int right = (int)(volume * (pan.Value < 0 ? 1 + pan.Value : 1));
        return MciPlayback.SendString($"setaudio {alias} left volume to {left}", null, 0, IntPtr.Zero) == 0 &&
            MciPlayback.SendString($"setaudio {alias} right volume to {right}", null, 0, IntPtr.Zero) == 0;
    }

    // =========================================================================
    // 7. التوليد التلقائي للعينات الصوتية الواقعية عند عدم توفرها
    // =========================================================================
    private static void EnsureDefaultAudioAssets()
    {
        try
        {
            string menuDir = Path.Combine(_baseAudioDir, "Menu");
            MenuAudioGenerator.EnsureAllMenuAudio(menuDir);

            string earconsDir = Path.Combine(_baseAudioDir, "SFX", "Earcons");
            EarconGenerator.EnsureAllEarcons(earconsDir);

            string kicksDir = Path.Combine(_baseAudioDir, "SFX", "Kicks");
            string tacklesDir = Path.Combine(_baseAudioDir, "SFX", "Tackles");
            string whistlesDir = Path.Combine(_baseAudioDir, "SFX", "Whistles");
            string goalNetDir = Path.Combine(_baseAudioDir, "SFX", "GoalNet");
            string movementDir = Path.Combine(_baseAudioDir, "SFX", "Movement");

            Directory.CreateDirectory(kicksDir);
            Directory.CreateDirectory(tacklesDir);
            Directory.CreateDirectory(whistlesDir);
            Directory.CreateDirectory(goalNetDir);
            Directory.CreateDirectory(movementDir);

            // 1. تسديدة قوية (Shot)
            EnsureFile(Path.Combine(kicksDir, "Shot.wav"), p => CreateShot(p, 0.9f, 0.9f));
            EnsureFile(Path.Combine(kicksDir, "Shot_Center.wav"), p => CreateShot(p, 0.9f, 0.9f));
            EnsureFile(Path.Combine(kicksDir, "Shot_Left.wav"), p => CreateShot(p, 1.0f, 0.35f));
            EnsureFile(Path.Combine(kicksDir, "Shot_Right.wav"), p => CreateShot(p, 0.35f, 1.0f));

            // 2. تمريرة أرضية ناعمة (Pass)
            EnsureFile(Path.Combine(kicksDir, "Pass.wav"), p => CreatePass(p, 0.85f, 0.85f));
            EnsureFile(Path.Combine(kicksDir, "Pass_Center.wav"), p => CreatePass(p, 0.85f, 0.85f));
            EnsureFile(Path.Combine(kicksDir, "Pass_Left.wav"), p => CreatePass(p, 1.0f, 0.4f));
            EnsureFile(Path.Combine(kicksDir, "Pass_Right.wav"), p => CreatePass(p, 0.4f, 1.0f));

            // 3. ركلة عرضية عالية (Cross)
            EnsureFile(Path.Combine(kicksDir, "Cross.wav"), p => CreateCross(p, 0.85f, 0.85f));
            EnsureFile(Path.Combine(kicksDir, "Cross_Center.wav"), p => CreateCross(p, 0.85f, 0.85f));
            EnsureFile(Path.Combine(kicksDir, "Cross_Left.wav"), p => CreateCross(p, 1.0f, 0.4f));
            EnsureFile(Path.Combine(kicksDir, "Cross_Right.wav"), p => CreateCross(p, 0.4f, 1.0f));

            // 4. ركلة عامة (Kick)
            EnsureFile(Path.Combine(kicksDir, "Kick.wav"), p => CreatePass(p, 0.85f, 0.85f));
            EnsureFile(Path.Combine(kicksDir, "Kick_Center.wav"), p => CreatePass(p, 0.85f, 0.85f));
            EnsureFile(Path.Combine(kicksDir, "Kick_Left.wav"), p => CreatePass(p, 1.0f, 0.4f));
            EnsureFile(Path.Combine(kicksDir, "Kick_Right.wav"), p => CreatePass(p, 0.4f, 1.0f));

            // 5. استخلاص بالوقوف (Stand Tackle)
            EnsureFile(Path.Combine(tacklesDir, "StandTackle.wav"), p => CreateStandTackle(p, 0.9f, 0.9f));

            // 6. استخلاص بالانزلاق (Slide Tackle)
            EnsureFile(Path.Combine(tacklesDir, "SlideTackle.wav"), p => CreateSlideTackle(p, 0.9f, 0.9f));

            // 7. صافرة الحكم (Whistle)
            EnsureFile(Path.Combine(whistlesDir, "Whistle.wav"), CreateWhistle);

            // 8. اهتزاز الشباك (Goal Net)
            EnsureFile(Path.Combine(goalNetDir, "GoalNet.wav"), CreateGoalNet);

            // 9. ارتطام القائم (Post Hit)
            string postHitDir = Path.Combine(_baseAudioDir, "SFX", "PostHit");
            Directory.CreateDirectory(postHitDir);
            EnsureFile(Path.Combine(postHitDir, "PostHit.wav"), CreatePostHit);

            // 10. نغمات التنبيه التكتيكية الثمانية (8 Earcons)
            EarconGenerator.EnsureAllEarcons(Path.Combine(_baseAudioDir, "SFX", "Earcons"));
        }
        catch { }
    }

    private static void EnsureFile(string path, Action<string> creator)
    {
        if (!File.Exists(path) || new FileInfo(path).Length < 100)
        {
            try
            {
                creator(path);
                // نسجل كل عينة مولدة حتى لا يختفي الفرق بين المستخرج والمولد في التشخيص
                if (File.Exists(path) && !_syntheticAssetsGenerated.Contains(path))
                    _syntheticAssetsGenerated.Add(path);
            }
            catch { }
        }
    }

    private static void WriteWav(string filePath, short[] samples, int sampleRate = 44100, int channels = 2)
    {
        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var bw = new BinaryWriter(fs);

        int byteRate = sampleRate * channels * 2;
        int dataSize = samples.Length * 2;

        bw.Write(Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + dataSize);
        bw.Write(Encoding.ASCII.GetBytes("WAVE"));
        bw.Write(Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16);
        bw.Write((short)1); // PCM
        bw.Write((short)channels);
        bw.Write(sampleRate);
        bw.Write(byteRate);
        bw.Write((short)(channels * 2));
        bw.Write((short)16);
        bw.Write(Encoding.ASCII.GetBytes("data"));
        bw.Write(dataSize);

        for (int i = 0; i < samples.Length; i++)
        {
            bw.Write(samples[i]);
        }
    }

    private static void CreateShot(string path, float leftGain, float rightGain)
    {
        int sampleRate = 44100;
        int frames = (int)(sampleRate * 0.22); // 220ms
        short[] s = new short[frames * 2];
        var rand = new Random(101);

        for (int i = 0; i < frames; i++)
        {
            double t = (double)i / sampleRate;
            double env = Math.Exp(-t * 22.0);
            double f = 160.0 * Math.Exp(-t * 18.0) + 48.0;
            double wave = Math.Sin(2.0 * Math.PI * f * t) + 0.35 * Math.Sin(2.0 * Math.PI * (f * 0.5) * t);
            double impactClick = (t < 0.03) ? (rand.NextDouble() * 2.0 - 1.0) * Math.Exp(-t * 120.0) * 0.4 : 0.0;
            double sample = (wave * 0.75 + impactClick) * env * 27000;

            s[i * 2] = (short)Math.Clamp(sample * leftGain, -32000, 32000);
            s[i * 2 + 1] = (short)Math.Clamp(sample * rightGain, -32000, 32000);
        }
        WriteWav(path, s, sampleRate, 2);
    }

    private static void CreatePass(string path, float leftGain, float rightGain)
    {
        int sampleRate = 44100;
        int frames = (int)(sampleRate * 0.12); // 120ms
        short[] s = new short[frames * 2];

        for (int i = 0; i < frames; i++)
        {
            double t = (double)i / sampleRate;
            double env = Math.Exp(-t * 36.0);
            double f = 130.0 * Math.Exp(-t * 25.0) + 60.0;
            double wave = Math.Sin(2.0 * Math.PI * f * t) + 0.2 * Math.Sin(2.0 * Math.PI * f * 2.0 * t);
            double sample = wave * env * 21000;

            s[i * 2] = (short)Math.Clamp(sample * leftGain, -32000, 32000);
            s[i * 2 + 1] = (short)Math.Clamp(sample * rightGain, -32000, 32000);
        }
        WriteWav(path, s, sampleRate, 2);
    }

    private static void CreateCross(string path, float leftGain, float rightGain)
    {
        int sampleRate = 44100;
        int frames = (int)(sampleRate * 0.19); // 190ms
        short[] s = new short[frames * 2];
        var rand = new Random(202);

        for (int i = 0; i < frames; i++)
        {
            double t = (double)i / sampleRate;
            double env = Math.Exp(-t * 24.0);
            double f = 110.0 * Math.Exp(-t * 16.0) + 55.0;
            double wave = Math.Sin(2.0 * Math.PI * f * t);
            double swoosh = (rand.NextDouble() * 2.0 - 1.0) * Math.Sin(Math.PI * (t / 0.19)) * 0.25;
            double sample = (wave * 0.75 + swoosh) * env * 23000;

            s[i * 2] = (short)Math.Clamp(sample * leftGain, -32000, 32000);
            s[i * 2 + 1] = (short)Math.Clamp(sample * rightGain, -32000, 32000);
        }
        WriteWav(path, s, sampleRate, 2);
    }

    private static void CreateStandTackle(string path, float leftGain, float rightGain)
    {
        int sampleRate = 44100;
        int frames = (int)(sampleRate * 0.10); // 100ms
        short[] s = new short[frames * 2];
        var rand = new Random(303);

        for (int i = 0; i < frames; i++)
        {
            double t = (double)i / sampleRate;
            double env = Math.Exp(-t * 42.0);
            double f = 170.0 * Math.Exp(-t * 30.0) + 65.0;
            double thud = Math.Sin(2.0 * Math.PI * f * t);
            double scrape = (rand.NextDouble() * 2.0 - 1.0) * 0.35;
            double sample = (thud * 0.65 + scrape) * env * 24000;

            s[i * 2] = (short)Math.Clamp(sample * leftGain, -32000, 32000);
            s[i * 2 + 1] = (short)Math.Clamp(sample * rightGain, -32000, 32000);
        }
        WriteWav(path, s, sampleRate, 2);
    }

    private static void CreateSlideTackle(string path, float leftGain, float rightGain)
    {
        int sampleRate = 44100;
        int frames = (int)(sampleRate * 0.32); // 320ms
        short[] s = new short[frames * 2];
        var rand = new Random(404);

        double lp = 0.0;
        for (int i = 0; i < frames; i++)
        {
            double t = (double)i / sampleRate;
            double attack = (t < 0.04) ? (t / 0.04) : 1.0;
            double decay = Math.Exp(-(t - 0.04) * 7.5);
            double env = attack * Math.Clamp(decay, 0.0, 1.0);

            double white = rand.NextDouble() * 2.0 - 1.0;
            lp = lp * 0.85 + white * 0.15; // grass friction noise
            double rumble = Math.Sin(2.0 * Math.PI * 58.0 * t) * 0.25;
            double sample = (lp * 0.75 + rumble) * env * 25000;

            s[i * 2] = (short)Math.Clamp(sample * leftGain, -32000, 32000);
            s[i * 2 + 1] = (short)Math.Clamp(sample * rightGain, -32000, 32000);
        }
        WriteWav(path, s, sampleRate, 2);
    }

    private static void CreateWhistle(string path)
    {
        int sampleRate = 44100;
        int frames = (int)(sampleRate * 0.60); // 600ms
        short[] s = new short[frames * 2];

        for (int i = 0; i < frames; i++)
        {
            double t = (double)i / sampleRate;
            double env = (t < 0.03) ? (t / 0.03) : (t > 0.50 ? (0.60 - t) / 0.10 : 1.0);
            double trill = Math.Sin(2.0 * Math.PI * 24.0 * t) * 45.0;
            double w1 = Math.Sin(2.0 * Math.PI * (2800.0 + trill) * t);
            double w2 = Math.Sin(2.0 * Math.PI * (3150.0 + trill) * t);
            double sample = (w1 + w2) * 0.5 * env * 18000;

            s[i * 2] = (short)sample;
            s[i * 2 + 1] = (short)sample;
        }
        WriteWav(path, s, sampleRate, 2);
    }

    private static void CreateGoalNet(string path)
    {
        int sampleRate = 44100;
        int frames = (int)(sampleRate * 0.45); // 450ms
        short[] s = new short[frames * 2];
        var rand = new Random(505);

        for (int i = 0; i < frames; i++)
        {
            double t = (double)i / sampleRate;
            double env = (t < 0.02) ? (t / 0.02) : Math.Exp(-t * 8.0);
            double noise = (rand.NextDouble() * 2.0 - 1.0);
            double thud = Math.Sin(2.0 * Math.PI * 75.0 * t) * Math.Exp(-t * 20.0);
            double sample = ((noise * 0.5) + (thud * 0.8)) * env * 19000;

            s[i * 2] = (short)sample;
            s[i * 2 + 1] = (short)sample;
        }
        WriteWav(path, s, sampleRate, 2);
    }

    private static void CreatePostHit(string path)
    {
        int sampleRate = 44100;
        int frames = (int)(sampleRate * 0.55); // 550ms metallic ping
        short[] s = new short[frames * 2];
        var rand = new Random(606);

        for (int i = 0; i < frames; i++)
        {
            double t = (double)i / sampleRate;
            double env = Math.Exp(-t * 9.5);
            double ping1 = Math.Sin(2.0 * Math.PI * 920.0 * t);
            double ping2 = 0.55 * Math.Sin(2.0 * Math.PI * 1840.0 * t);
            double ping3 = 0.25 * Math.Sin(2.0 * Math.PI * 2760.0 * t);
            double impactThud = Math.Sin(2.0 * Math.PI * 140.0 * t) * Math.Exp(-t * 35.0);
            double metalNoise = (t < 0.015) ? (rand.NextDouble() * 2.0 - 1.0) * 0.4 : 0.0;

            double sample = (ping1 + ping2 + ping3 + impactThud + metalNoise) * env * 22000;
            short val = (short)Math.Clamp(sample, -32000, 32000);
            s[i * 2] = val;
            s[i * 2 + 1] = val;
        }
        WriteWav(path, s, sampleRate, 2);
    }

    public static void StopAll()
    {
        _commentaryBusyUntil = DateTime.MinValue;
        StopMenuMusic();
        StopCrowdAmbiance();
        SendMci("close all");
        lock (_lock) { _activeOneShots.Clear(); _activeOneShotPans.Clear(); }
    }
}
