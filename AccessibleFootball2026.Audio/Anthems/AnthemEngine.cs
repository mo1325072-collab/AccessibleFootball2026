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

    [DllImport("winmm.dll", CharSet = CharSet.Auto)]
    private static extern int mciSendString(string command, string? buffer, int bufferSize, IntPtr hwndCallback);

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

    public void StopAllSounds()
    {
        _stopRequested = true;
        try
        {
            mciSendString("stop anthem", null, 0, IntPtr.Zero);
            mciSendString("close anthem", null, 0, IntPtr.Zero);
        }
        catch { }
    }

    public void PlayChampionsLeagueIntro(bool isAutomatedTest = false, Func<bool>? skipChecker = null)
    {
        if (isAutomatedTest) return;

        _stopRequested = false;
        _screenReader.Speak("Accessible Football 2026. نشيد دوري أبطال أوروبا الرسمي.");

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
                string mciType = anthemPath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ? "mpegvideo" : "waveaudio";
                mciSendString("close anthem", null, 0, IntPtr.Zero);
                mciSendString($"open \"{anthemPath}\" type {mciType} alias anthem", null, 0, IntPtr.Zero);
                mciSendString("play anthem", null, 0, IntPtr.Zero);
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

    public void PlayChampionsLeagueSample()
    {
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
                string mciType = anthemPath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ? "mpegvideo" : "waveaudio";
                mciSendString("close anthem", null, 0, IntPtr.Zero);
                mciSendString($"open \"{anthemPath}\" type {mciType} alias anthem", null, 0, IntPtr.Zero);
                mciSendString("play anthem from 0", null, 0, IntPtr.Zero);
            }
            catch { }
        }
    }

    public void PlayNationalAnthemSample(string? teamId = "EGY")
    {
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
            var files = Directory.GetFiles(nationalDir, "*.wav");
            if (files.Length > 0) anthemPath = files[0];
        }

        if (anthemPath != null)
        {
            LastPlayedAnthem = id;
            try
            {
                string mciType = anthemPath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ? "mpegvideo" : "waveaudio";
                mciSendString("close anthem", null, 0, IntPtr.Zero);
                mciSendString($"open \"{anthemPath}\" type {mciType} alias anthem", null, 0, IntPtr.Zero);
                mciSendString("play anthem from 0", null, 0, IntPtr.Zero);
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
        _stopRequested = false;
        string announcement = $"السلام الوطني لـ {nationalTeam.Name}.";
        _screenReader.Speak(announcement);

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
                string mciType = anthemPath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ? "mpegvideo" : "waveaudio";
                mciSendString("close anthem", null, 0, IntPtr.Zero);
                mciSendString($"open \"{anthemPath}\" type {mciType} alias anthem", null, 0, IntPtr.Zero);
                mciSendString("play anthem", null, 0, IntPtr.Zero);
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
