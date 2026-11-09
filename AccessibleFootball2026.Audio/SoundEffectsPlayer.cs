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
    [DllImport("winmm.dll", CharSet = CharSet.Auto)]
    private static extern int mciSendString(string command, StringBuilder? buffer, int bufferSize, IntPtr hwndCallback);

    private static string _baseAudioDir = Directory.Exists(@"D:\AccessibleFootball2026\Assets\Audio")
        ? @"D:\AccessibleFootball2026\Assets\Audio"
        : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio");
    private static readonly object _lock = new();

    private static bool _menuMusicPlaying = false;
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

    public static string? LastPlayedSound { get; private set; }
    public static float LastPlayedPan { get; private set; }
    public static int TotalSoundsPlayedCount { get; private set; } = 0;

    public static void ResetTelemetry()
    {
        LastPlayedSound = null;
        LastPlayedPan = 0.0f;
        TotalSoundsPlayedCount = 0;
        _commentaryBusyUntil = DateTime.MinValue;
    }

    public static void Initialize(string? dir = null)
    {
        if (!string.IsNullOrEmpty(dir)) _baseAudioDir = dir;
        EnsureDefaultAudioAssets();
    }

    private static void SendMci(string command)
    {
        try
        {
            mciSendString(command, null, 0, IntPtr.Zero);
        }
        catch { }
    }

    public static string? ResolveAudioPath(params string[] relativeParts)
    {
        string fullWithoutExt = Path.Combine(_baseAudioDir, Path.Combine(relativeParts));
        // نفضل ملفات WAV ثم MP3
        if (File.Exists(fullWithoutExt + ".wav")) return fullWithoutExt + ".wav";
        if (File.Exists(fullWithoutExt + ".mp3")) return fullWithoutExt + ".mp3";
        if (File.Exists(fullWithoutExt)) return fullWithoutExt;

        // توليد الملفات الصوتية تلقائياً إذا لم تكن موجودة
        EnsureDefaultAudioAssets();

        if (File.Exists(fullWithoutExt + ".wav")) return fullWithoutExt + ".wav";
        if (File.Exists(fullWithoutExt + ".mp3")) return fullWithoutExt + ".mp3";
        if (File.Exists(fullWithoutExt)) return fullWithoutExt;

        return null;
    }

    public static string GetMciType(string filePath)
    {
        return "mpegvideo";
    }

    // =========================================================================
    // 1. موسيقى القوائم (Menu BGM Loop)
    // =========================================================================
    public static void StartMenuMusic()
    {
        lock (_lock)
        {
            // إيقاف أي نشيد أو صوت افتتاحية عند الدخول إلى القوائم
            SendMci("stop anthem");
            SendMci("close anthem");

            if (_menuMusicPlaying) return;
            if (GetEffectiveMusicVolume() <= 0.001f) return;

            string? p = ResolveAudioPath("Menu", "MenuMusic");
            if (p != null)
            {
                string mciType = GetMciType(p);
                SendMci("close bgm_menu");
                SendMci($"open \"{p}\" type {mciType} alias bgm_menu");
                int vol = Math.Clamp((int)(GetEffectiveMusicVolume() * 1000), 0, 1000);
                SendMci($"setaudio bgm_menu volume to {vol}");
                SendMci("play bgm_menu repeat");
                _menuMusicPlaying = true;
            }
        }
    }

    public static void StopMenuMusic()
    {
        lock (_lock)
        {
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
            // إذا كان صوت الجماهير مضبوطاً على 0: صامت تماماً 100% ولا يتم تشغيله مطلقاً
            if (GetEffectiveCrowdVolume() <= 0.001f)
            {
                StopCrowdAmbiance();
                return;
            }

            if (_crowdPlaying) return;
            string? p = ResolveAudioPath("SFX", "Crowd", "CrowdAmbiance");
            if (p != null)
            {
                string mciType = GetMciType(p);
                SendMci("close crowd");
                SendMci($"open \"{p}\" type {mciType} alias crowd");
                int initialVol = Math.Clamp((int)(350 * GetEffectiveCrowdVolume()), 0, 1000);
                SendMci($"setaudio crowd volume to {initialVol}");
                SendMci("play crowd repeat");
                _crowdPlaying = true;
                _lastReportedCrowdVol = initialVol;
                _lastCrowdVolChange = DateTime.UtcNow;
            }
        }
    }

    public static void StopCrowdAmbiance()
    {
        lock (_lock)
        {
            SendMci("stop crowd");
            SendMci("close crowd");
            _crowdPlaying = false;
            _lastReportedCrowdVol = -1;
        }
    }

    public static void PauseCrowdAmbiance()
    {
        lock (_lock)
        {
            SendMci("pause crowd");
        }
    }

    public static void ResumeCrowdAmbiance()
    {
        lock (_lock)
        {
            if (GetEffectiveCrowdVolume() <= 0.001f)
            {
                StopCrowdAmbiance();
                return;
            }

            if (_crowdPlaying)
            {
                SendMci("resume crowd");
            }
            else
            {
                StartCrowdAmbiance();
            }
        }
    }

    public static void ApplyMusicVolume()
    {
        int vol = Math.Clamp((int)(GetEffectiveMusicVolume() * 1000), 0, 1000);
        if (vol <= 0)
        {
            SendMci("pause bgm_menu");
            SendMci("setaudio bgm_menu volume to 0");
        }
        else
        {
            SendMci("play bgm_menu repeat");
            SendMci($"setaudio bgm_menu volume to {vol}");
        }
    }

    public static void ApplyCrowdVolume()
    {
        float factor = GetEffectiveCrowdVolume();
        if (factor <= 0.001f)
        {
            StopCrowdAmbiance();
            return;
        }

        int vol = Math.Clamp((int)(factor * 1000), 0, 1000);
        SetCrowdVolume(vol, force: true);
    }

    public static void SetCrowdVolume(int volumeOut1000, bool force = false)
    {
        float settingFactor = GetEffectiveCrowdVolume();

        // إذا كان صوت الجماهير معطلاً (0%): نكتم ونوقف الصوت فوراً وبشكل قاطع
        if (settingFactor <= 0.001f)
        {
            if (_crowdPlaying)
            {
                StopCrowdAmbiance();
            }
            return;
        }

        if (!_crowdPlaying)
        {
            StartCrowdAmbiance();
        }

        int vol = Math.Clamp((int)(volumeOut1000 * settingFactor), 0, 1000);

        // منع تكرار أوامر MCI السريعة لمنع التقطيع الصوتي التام (Zero Stutter)
        DateTime now = DateTime.UtcNow;
        if (!force)
        {
            if (Math.Abs(vol - _lastReportedCrowdVol) < 30 && (now - _lastCrowdVolChange).TotalMilliseconds < 250)
            {
                return;
            }
        }

        _lastReportedCrowdVol = vol;
        _lastCrowdVolChange = now;

        // تنفيذ مباشر ومتزامن على نفس الـ Thread لضمان تعرف MCI على الـ Alias وعدم حدوث الخطأ 261
        SendMci($"setaudio crowd volume to {vol}");
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

    public static void PlayOneShotChant(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;
        PlayOneShot(filePath, "chant_ch");
    }

    public static void SetStadiumAcoustics(string acousticPath)
    {
        if (string.IsNullOrEmpty(acousticPath) || !File.Exists(acousticPath)) return;
        lock (_lock)
        {
            SendMci("stop crowd");
            SendMci("close crowd");
            string mciType = GetMciType(acousticPath);
            SendMci($"open \"{acousticPath}\" type {mciType} alias crowd");
            SendMci("play crowd repeat");
            SetCrowdVolume(800, force: true);
            _crowdPlaying = true;
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
    public static void PlayKick(float pan = 0.0f)
    {
        LastPlayedPan = pan;
        string fileName = pan switch
        {
            < -0.25f => "Kick_Left",
            > 0.25f => "Kick_Right",
            _ => "Kick_Center"
        };
        string? p = ResolveAudioPath("SFX", "Kicks", fileName) 
                 ?? ResolveAudioPath("SFX", "Kicks", "Kick")
                 ?? ResolveAudioPath("SFX", "Kicks", "Pass");
        if (p != null) PlayOneShot(p, "kick");
    }

    public static void PlayShotKick(float pan = 0.0f)
    {
        LastPlayedPan = pan;
        string fileName = pan switch
        {
            < -0.25f => "Shot_Left",
            > 0.25f => "Shot_Right",
            _ => "Shot_Center"
        };
        string? p = ResolveAudioPath("SFX", "Kicks", fileName) 
                 ?? ResolveAudioPath("SFX", "Kicks", "Shot") 
                 ?? ResolveAudioPath("SFX", "Kicks", "Kick_Center");
        if (p != null) PlayOneShot(p, "shot_kick");
    }

    public static void PlayPassKick(float pan = 0.0f)
    {
        LastPlayedPan = pan;
        string fileName = pan switch
        {
            < -0.25f => "Pass_Left",
            > 0.25f => "Pass_Right",
            _ => "Pass_Center"
        };
        string? p = ResolveAudioPath("SFX", "Kicks", fileName) 
                 ?? ResolveAudioPath("SFX", "Kicks", "Pass") 
                 ?? ResolveAudioPath("SFX", "Kicks", "Kick_Center");
        if (p != null) PlayOneShot(p, "pass_kick");
    }

    public static void PlayCrossKick(float pan = 0.0f)
    {
        LastPlayedPan = pan;
        string fileName = pan switch
        {
            < -0.25f => "Cross_Left",
            > 0.25f => "Cross_Right",
            _ => "Cross_Center"
        };
        string? p = ResolveAudioPath("SFX", "Kicks", fileName) 
                 ?? ResolveAudioPath("SFX", "Kicks", "Cross") 
                 ?? ResolveAudioPath("SFX", "Kicks", "Kick_Center");
        if (p != null) PlayOneShot(p, "cross_kick");
    }

    public static void PlayStandTackle(float pan = 0.0f)
    {
        LastPlayedPan = pan;
        string? p = ResolveAudioPath("SFX", "Tackles", "StandTackle")
                 ?? ResolveAudioPath("SFX", "Tackles", "Tackle")
                 ?? ResolveAudioPath("SFX", "Kicks", "Pass");
        if (p != null) PlayOneShot(p, "stand_tackle");
    }

    public static void PlaySlideTackle(float pan = 0.0f)
    {
        LastPlayedPan = pan;
        string? p = ResolveAudioPath("SFX", "Tackles", "SlideTackle")
                 ?? ResolveAudioPath("SFX", "Tackles", "Tackle")
                 ?? ResolveAudioPath("SFX", "Kicks", "Shot");
        if (p != null) PlayOneShot(p, "slide_tackle");
    }

    public static void PlayWhistle()
    {
        string? p = ResolveAudioPath("SFX", "Whistles", "Whistle");
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
        if (DateTime.UtcNow < _commentaryBusyUntil) return true;
        try
        {
            var sb = new StringBuilder(128);
            int err = mciSendString("status comm_voice mode", sb, sb.Capacity, IntPtr.Zero);
            if (err == 0 && sb.ToString().Equals("playing", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        catch { }
        return false;
    }

    public static void PlayCommentaryAudio(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;
        _commentaryBusyUntil = DateTime.UtcNow.AddSeconds(3.2);
        PlayOneShot(filePath, "comm_voice");
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
    }

    public static void PlayLivePreviewGoalBeacon(int beaconVal)
    {
        GoalBeaconVolumeSetting = Math.Clamp(beaconVal, 0, 100);
        float vol = GetEffectiveBeaconVolume();
        GoalBeaconPlayer.PlayBeaconPulse(vol, 0.0f);
    }

    private static void PlayOneShot(string filePath, string alias, string? soundName = null, float? customVolume = null)
    {
        LastPlayedSound = soundName ?? alias;
        TotalSoundsPlayedCount++;

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;
        try
        {
            string mciType = GetMciType(filePath);
            SendMci($"close {alias}");
            SendMci($"open \"{filePath}\" type {mciType} alias {alias}");
            if (customVolume.HasValue)
            {
                int vol = Math.Clamp((int)(customVolume.Value * 1000), 0, 1000);
                SendMci($"setaudio {alias} volume to {vol}");
            }
            else if (alias == "comm_voice")
            {
                int commVol = Math.Clamp((int)(GetEffectiveCommentaryVolume() * 1000), 0, 1000);
                SendMci($"setaudio {alias} volume to {commVol}");
            }
            else
            {
                int sfxVol = Math.Clamp((int)(GetEffectiveSfxVolume() * 1000), 0, 1000);
                SendMci($"setaudio {alias} volume to {sfxVol}");
            }
            SendMci($"play {alias} from 0");
        }
        catch { }
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
            try { creator(path); } catch { }
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
    }
}
