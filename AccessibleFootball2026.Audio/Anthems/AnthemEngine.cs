using System;
using System.IO;
using System.Runtime.InteropServices;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Audio.Anthems;

/// <summary>
/// محرك الأناشيد الرسمية الحقيقية (Champions League & National Anthems):
/// تشغيل نشيد دوري أبطال أوروبا الحقيقي والأناشيد الوطنية مع إمكانية التخطي الفوري بـ Enter
/// </summary>
public class AnthemEngine
{
    private readonly IScreenReader _screenReader;
    private readonly string _assetsBaseDir;
    private volatile bool _stopRequested = false;

    public string? LastPlayedAnthem { get; private set; }
    public bool WasSkipped { get; private set; }
    public bool IsAnthemPlaying { get; private set; } = false;


    public AnthemEngine(IScreenReader screenReader, string? assetsDir = null)
    {
        _screenReader = screenReader;
        _assetsBaseDir = assetsDir ?? ResolveAssetsBaseDir();
        Directory.CreateDirectory(_assetsBaseDir);
    }

    private static string ResolveAssetsBaseDir()
    {
        string dDriveAnthems = @"D:\AccessibleFootball2026\Assets\Audio\Anthems";
        if (Directory.Exists(dDriveAnthems)) return dDriveAnthems;

        string binAnthems = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio", "Anthems");
        if (Directory.Exists(binAnthems)) return binAnthems;

        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "AccessibleFootball2026.sln")))
            {
                string slnAnthems = Path.Combine(dir, "Assets", "Audio", "Anthems");
                if (Directory.Exists(slnAnthems)) return slnAnthems;
            }
            dir = Path.GetDirectoryName(dir);
        }
        return binAnthems;
    }

    private static bool StartAnthem(string path, bool fromStart)
    {
        float volume = SoundEffectsPlayer.GetEffectiveMusicVolume();
        if (volume <= .001f) return false;
        string type = path.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ? "mpegvideo" : "waveaudio";
        MciPlayback.SendString("close anthem", null, 0, IntPtr.Zero);
        if (MciPlayback.SendString($"open \"{path}\" type {type} alias anthem", null, 0, IntPtr.Zero) != 0) return false;
        if (MciPlayback.SendString($"setaudio anthem volume to {(int)(volume * 1000)}", null, 0, IntPtr.Zero) != 0 ||
            MciPlayback.SendString(fromStart ? "play anthem from 0" : "play anthem", null, 0, IntPtr.Zero) != 0)
        { MciPlayback.SendString("close anthem", null, 0, IntPtr.Zero); return false; }
        return true;
    }

    public void StopAllSounds()
    {
        _stopRequested = true;
        try
        {
            MciPlayback.SendString("stop anthem", null, 0, IntPtr.Zero);
            MciPlayback.SendString("close anthem", null, 0, IntPtr.Zero);
        }
        catch { }
    }

    public void PlayChampionsLeagueIntro(bool isAutomatedTest = false, Func<bool>? skipChecker = null)
    {
        if (SoundEffectsPlayer.GetEffectiveMusicVolume() <= .001f) return;
        if (isAutomatedTest) return;

        _stopRequested = false;
        // بدون أي حديث من قارئ الشاشة احتراماً لطلب المستخدم وجمال النشيد

        string? anthemPath = null;
        if (File.Exists(Path.Combine(_assetsBaseDir, "ChampionsLeague.mp3")))
            anthemPath = Path.Combine(_assetsBaseDir, "ChampionsLeague.mp3");
        else if (File.Exists(Path.Combine(_assetsBaseDir, "ChampionsLeague.wav")))
            anthemPath = Path.Combine(_assetsBaseDir, "ChampionsLeague.wav");

        if (anthemPath != null)
        {
            LastPlayedAnthem = "ChampionsLeague";
            try
            {
                if (!StartAnthem(anthemPath, fromStart: false)) return;
                WaitForSkipOrCompletion(60000, skipChecker);
                StopAllSounds();
            }
            catch { }
        }
        else
        {
            LastPlayedAnthem = "ChampionsLeague";
            WaitForSkipOrCompletion(500, skipChecker);
        }
    }

    /// <summary>
    /// تشغيل نشيد دوري الأبطال الافتتاحي. يعيد true إذا بدأ التشغيل فعلاً،
    /// وfalse إذا كان الملف مفقوداً أو رفض جهاز الصوت فتحه/تشغيله، حتى يتجاوز
    /// المتصل شاشة البداية فوراً بدلاً من ترك المستخدم الكفيف أمام شاشة صامتة.
    /// </summary>
    public bool PlayChampionsLeagueSample()
    {
        if (SoundEffectsPlayer.GetEffectiveMusicVolume() <= .001f) return false;
        string? anthemPath = null;
        if (File.Exists(Path.Combine(_assetsBaseDir, "ChampionsLeague.mp3")))
            anthemPath = Path.Combine(_assetsBaseDir, "ChampionsLeague.mp3");
        else if (File.Exists(Path.Combine(_assetsBaseDir, "ChampionsLeague.wav")))
            anthemPath = Path.Combine(_assetsBaseDir, "ChampionsLeague.wav");

        if (anthemPath == null)
        {
            return false;
        }

        LastPlayedAnthem = "ChampionsLeague";
        try
        {
            if (!StartAnthem(anthemPath, fromStart: true)) return false;
            IsAnthemPlaying = true;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool TryPlayNationalAnthemSample(string teamId)
    {
        if (SoundEffectsPlayer.GetEffectiveMusicVolume() <= .001f) return false;
        var stem = Path.Combine(_assetsBaseDir, "National", teamId);
        var path = File.Exists(stem + ".mp3") ? stem + ".mp3" : File.Exists(stem + ".wav") ? stem + ".wav" : null;
        if (path == null) return false;
        try
        {
            if (!StartAnthem(path, fromStart: true)) return false;
            LastPlayedAnthem = teamId; IsAnthemPlaying = true; return true;
        }
        catch { return false; }
    }

    public void PlayNationalAnthemSample(string? teamId = "EGY")
    {
        if (SoundEffectsPlayer.GetEffectiveMusicVolume() <= .001f) return;
        string nationalDir = Path.Combine(_assetsBaseDir, "National");
        string id = teamId ?? "EGY";
        string? anthemPath = null;
        if (File.Exists(Path.Combine(nationalDir, $"{id}.mp3")))
            anthemPath = Path.Combine(nationalDir, $"{id}.mp3");
        else if (File.Exists(Path.Combine(nationalDir, $"{id}.wav")))
            anthemPath = Path.Combine(nationalDir, $"{id}.wav");
        else
        {
            // fallback to any national anthem in folder
            var files = Directory.Exists(nationalDir) ? Directory.GetFiles(nationalDir, "*.wav") : Array.Empty<string>();
            if (files.Length > 0) anthemPath = files[0];
        }

        if (anthemPath != null)
        {
            LastPlayedAnthem = id;
            try
            {
                if (!StartAnthem(anthemPath, fromStart: true)) return;
            }
            catch { }
        }
    }

    public void PlayMatchNationalAnthems(Team home, Team away, bool isAutomatedTest = false, Func<bool>? skipChecker = null)
    {
        if (isAutomatedTest) return;

        if (home.IsNationalTeam)
        {
            PlaySingleNationalAnthem(home, skipChecker);
        }

        if (away.IsNationalTeam)
        {
            PlaySingleNationalAnthem(away, skipChecker);
        }
    }

    private void PlaySingleNationalAnthem(Team nationalTeam, Func<bool>? skipChecker)
    {
        if (SoundEffectsPlayer.GetEffectiveMusicVolume() <= .001f) return;
        _stopRequested = false;
        // ترك الإعلان للمؤثر الصوتي للعبة دون تشويش من قارئ الشاشة

        string nationalDir = Path.Combine(_assetsBaseDir, "National");
        Directory.CreateDirectory(nationalDir);

        string? anthemPath = null;
        if (File.Exists(Path.Combine(nationalDir, $"{nationalTeam.Id}.mp3")))
            anthemPath = Path.Combine(nationalDir, $"{nationalTeam.Id}.mp3");
        else if (File.Exists(Path.Combine(nationalDir, $"{nationalTeam.Id}.wav")))
            anthemPath = Path.Combine(nationalDir, $"{nationalTeam.Id}.wav");

        if (anthemPath != null)
        {
            LastPlayedAnthem = nationalTeam.Id;
            try
            {
                if (!StartAnthem(anthemPath, fromStart: false)) return;
                WaitForSkipOrCompletion(4000, skipChecker);
                StopAllSounds();
            }
            catch { }
        }
        else
        {
            LastPlayedAnthem = nationalTeam.Id;
            WaitForSkipOrCompletion(400, skipChecker);
        }
    }

    private void WaitForSkipOrCompletion(int totalMs, Func<bool>? skipChecker)
    {
        int elapsed = 0;
        int step = 40;
        while (elapsed < totalMs && !_stopRequested)
        {
            if (skipChecker != null && skipChecker())
            {
                WasSkipped = true;
                _screenReader.Speak("تم التخطي");
                break;
            }

            try
            {
                if (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(intercept: true);
                    if (k.Key == ConsoleKey.Enter || k.Key == ConsoleKey.Escape || k.Key == ConsoleKey.Spacebar)
                    {
                        WasSkipped = true;
                        _screenReader.Speak("تم التخطي");
                        break;
                    }
                }
            }
            catch { }

            Thread.Sleep(step);
            elapsed += step;
        }
    }
}
