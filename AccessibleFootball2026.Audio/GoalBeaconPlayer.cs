using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace AccessibleFootball2026.Audio;

/// <summary>
/// منارة صوت المرمى ثلاثية الأبعاد (3D Spatial Goal Beacon):
/// نغمة هادئة ودافئة غير مزعجة بتردد 528 هرتز (Solfeggio harmonic) ترشد اللاعب الكفيف
/// نحو مرمى الخصم بدقة ثلاثية الأبعاد مع التبديل التلقائي لاتجاه المرمى في الشوط الثاني
/// </summary>
public static class GoalBeaconPlayer
{

    private static string _beaconWavPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio", "SFX", "Beacon", "GoalBeacon.wav");
    private static DateTime _lastPulseTime = DateTime.MinValue;
    private static readonly TimeSpan _pulseInterval = TimeSpan.FromSeconds(1.5);
    private static bool _initialized = false;
    private static readonly object _lock = new();

    public const float MaxBeaconDistance = 52.5f; // المسافة القصوى لسماع المنارة (منتصف الملعب)
    public const float GoalCenterY = 34.0f;       // منتصف خط المرمى عرضياً
    public const float GoalRightX = 105.0f;       // مرمى الخصم في الشوط الأول
    public const float GoalLeftX = 0.0f;          // مرمى الخصم في الشوط الثاني

    public static void Initialize(string? baseDir = null)
    {
        lock (_lock)
        {
            if (!string.IsNullOrEmpty(baseDir))
            {
                _beaconWavPath = Path.Combine(baseDir, "SFX", "Beacon", "GoalBeacon.wav");
            }
            EnsureBeaconWavExists(_beaconWavPath);
            _initialized = true;
        }
    }

    public static void CalculateBeacon(float playerX, float playerY, bool isSecondHalf, float masterVolume, out float distance, out float volume, out float pan)
    {
        float targetGoalX = isSecondHalf ? GoalLeftX : GoalRightX;
        float dx = playerX - targetGoalX;
        float dy = playerY - GoalCenterY;
        distance = MathF.Sqrt(dx * dx + dy * dy);

        // 1. حساب مستوى الصوت (Volume Curve): صمت تام إذا كان اللاعب في نصف ملعبه الدفاعي (D > 52.5m)
        if (distance > MaxBeaconDistance)
        {
            volume = 0.0f;
        }
        else
        {
            float proximity = (MaxBeaconDistance - distance) / MaxBeaconDistance;
            // منحنى تصاعدي ناعم: (Proximity)^1.5
            volume = Math.Clamp(masterVolume * MathF.Pow(proximity, 1.5f), 0.0f, 1.0f);
        }

        // 2. حساب التوزيع المجسم ثلاثي الأبعاد (3D Stereo Panning):
        // في الشوط الأول: ينظر للأمام نحو X=105. إذا كان اللاعب في الأعلى (Y < 34)، فإن المرمى يقع ليمينه نسبياً
        float deltaY = GoalCenterY - playerY;
        float basePan = Math.Clamp(deltaY / GoalCenterY, -1.0f, 1.0f);

        // في الشوط الثاني: تنقلب الرؤية لأن اللاعب ينظر للخلف نحو X=0
        pan = isSecondHalf ? -basePan : basePan;
    }

    public static void UpdateBeacon(float playerX, float playerY, bool isSecondHalf, bool hasPossession, float masterVolume = 1.0f)
    {
        if (!hasPossession)
        {
            return;
        }

        CalculateBeacon(playerX, playerY, isSecondHalf, masterVolume, out float dist, out float vol, out float pan);

        if (vol <= 0.001f)
        {
            return;
        }

        DateTime now = DateTime.UtcNow;
        if (now - _lastPulseTime >= _pulseInterval)
        {
            _lastPulseTime = now;
            PlayBeaconPulse(vol, pan);
        }
    }

    private static float _pulseStrength, _pulsePan;
    public static void PlayBeaconPulse(float volume, float pan)
    {
        lock (_lock)
        {
            _pulseStrength = Math.Clamp(volume, 0, 1); _pulsePan = Math.Clamp(pan, -1, 1);
            float effective = _pulseStrength * SoundEffectsPlayer.GetEffectiveBeaconVolume();
            if (effective <= .001f) { Stop(); return; }
            if (!_initialized) Initialize();
            MciPlayback.SendString("close beacon_ch", null, 0, IntPtr.Zero);
            if (MciPlayback.SendString($"open \"{_beaconWavPath}\" type mpegvideo alias beacon_ch", null, 0, IntPtr.Zero) != 0) return;
            if (!ApplyLiveVolume()) { Stop(); return; }
            MciPlayback.SendString("play beacon_ch from 0", null, 0, IntPtr.Zero);
        }
    }

    public static bool ApplyLiveVolume()
    {
        lock (_lock)
        {
            int volume = Math.Clamp((int)(_pulseStrength * SoundEffectsPlayer.GetEffectiveBeaconVolume() * 1000), 0, 1000);
            if (volume == 0) { Stop(); return false; }
            int left = (int)(volume * (_pulsePan > 0 ? 1 - _pulsePan : 1));
            int right = (int)(volume * (_pulsePan < 0 ? 1 + _pulsePan : 1));
            bool applied = MciPlayback.SendString($"setaudio beacon_ch volume to {volume}", null, 0, IntPtr.Zero) == 0 &&
                MciPlayback.SendString($"setaudio beacon_ch left volume to {left}", null, 0, IntPtr.Zero) == 0 &&
                MciPlayback.SendString($"setaudio beacon_ch right volume to {right}", null, 0, IntPtr.Zero) == 0;
            if (!applied) Stop();
            return applied;
        }
    }

    public static void Stop()
    {
        lock (_lock)
        {
            MciPlayback.SendString("stop beacon_ch", null, 0, IntPtr.Zero);
            MciPlayback.SendString("close beacon_ch", null, 0, IntPtr.Zero);
        }
    }

    /// <summary>
    /// توليد نغمة 528 هرتز الدافئة المتناغمة (Harmonic Solfeggio Pulse)
    /// </summary>
    public static void EnsureBeaconWavExists(string path)
    {
        if (File.Exists(path) && new FileInfo(path).Length > 100)
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        int sampleRate = 44100;
        int frames = (int)(sampleRate * 0.20); // 200ms
        short[] s = new short[frames * 2];

        for (int i = 0; i < frames; i++)
        {
            double t = (double)i / sampleRate;
            // هبوط أسي سلس e^(-14t)
            double env = Math.Exp(-t * 14.0);
            // نغمة أساسية 528Hz مع توافقية فرعية دافئة 264Hz
            double fundamental = Math.Sin(2.0 * Math.PI * 528.0 * t);
            double subHarmonic = Math.Sin(2.0 * Math.PI * 264.0 * t) * 0.35;
            double wave = (fundamental + subHarmonic) * env * 22000;

            short sample = (short)Math.Clamp(wave, -32768, 32767);
            s[i * 2] = sample;     // Left
            s[i * 2 + 1] = sample; // Right
        }

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var bw = new BinaryWriter(fs);

        int channels = 2;
        int byteRate = sampleRate * channels * 2;
        int dataSize = s.Length * 2;

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

        for (int i = 0; i < s.Length; i++)
        {
            bw.Write(s[i]);
        }
    }
}
