using System;
using System.IO;
using System.Text;

namespace AccessibleFootball2026.Audio;

/// <summary>
/// مولد ملفات صوت القوائم (UI Navigation & Menu Music Generator):
/// يولد أصوات التنقل النقية وسريعة الاستجابة وموسيقى القوائم الهادئة تلقائياً إذا لم تكن موجودة.
/// </summary>
public static class MenuAudioGenerator
{
    public static void EnsureAllMenuAudio(string menuDir)
    {
        Directory.CreateDirectory(menuDir);
        EnsureMenuMove(Path.Combine(menuDir, "Move.wav"));
        EnsureMenuSelect(Path.Combine(menuDir, "Select.wav"));
        EnsureMenuBack(Path.Combine(menuDir, "Back.wav"));
        EnsureMenuMusic(Path.Combine(menuDir, "MenuMusic.wav"));
    }

    public static void EnsureMenuMove(string path)
    {
        if (File.Exists(path) && new FileInfo(path).Length > 100) return;
        int sr = 44100;
        int n = (int)(sr * 0.025); // 25ms tactile click
        short[] s = new short[n];
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / sr;
            double env = Math.Exp(-t * 180.0);
            double wave = Math.Sin(2.0 * Math.PI * 750.0 * t) * env * 12000;
            s[i] = (short)Math.Clamp(wave, -32768, 32767);
        }
        WriteWav(path, s, sr);
    }

    public static void EnsureMenuSelect(string path)
    {
        if (File.Exists(path) && new FileInfo(path).Length > 100) return;
        int sr = 44100;
        int n = (int)(sr * 0.07); // 70ms double chime
        short[] s = new short[n];
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / sr;
            double freq = t < 0.035 ? 523.25 : 783.99;
            double env = Math.Exp(-((t % 0.035) * 60.0));
            double wave = Math.Sin(2.0 * Math.PI * freq * t) * env * 14000;
            s[i] = (short)Math.Clamp(wave, -32768, 32767);
        }
        WriteWav(path, s, sr);
    }

    public static void EnsureMenuBack(string path)
    {
        if (File.Exists(path) && new FileInfo(path).Length > 100) return;
        int sr = 44100;
        int n = (int)(sr * 0.05); // 50ms soft tap
        short[] s = new short[n];
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / sr;
            double freq = 600.0 - (250.0 * (t / 0.05));
            double env = Math.Exp(-t * 90.0);
            double wave = Math.Sin(2.0 * Math.PI * freq * t) * env * 11000;
            s[i] = (short)Math.Clamp(wave, -32768, 32767);
        }
        WriteWav(path, s, sr);
    }

    public static void EnsureMenuMusic(string path)
    {
        // لا نستبدل موسيقى قوائم حقيقية وفّرها المستخدم (ملف مرفوع ضمن الحزمة) - نولّد فقط عند الغياب
        if (File.Exists(path) && new FileInfo(path).Length > 100) return;
        string mp3 = Path.ChangeExtension(path, ".mp3");
        if (File.Exists(mp3) && new FileInfo(mp3).Length > 100) return;
        int sampleRate = 44100;
        int seconds = 8;
        int frames = sampleRate * seconds;
        short[] s = new short[frames * 2];

        // Lo-fi progression الأصلية الهادئة: Dm9 -> Gm7 -> C7 -> Fmaj7
        double[] rootFreqs = { 146.83, 98.00, 130.81, 87.31 };

        for (int i = 0; i < frames; i++)
        {
            double t = (double)i / sampleRate;
            int bar = (int)(t / 2.0) % 4;
            double root = rootFreqs[bar];

            double bass = Math.Sin(2.0 * Math.PI * root * t) * 0.30;
            double chord = (Math.Sin(2.0 * Math.PI * root * 1.5 * t) +
                            Math.Sin(2.0 * Math.PI * root * 2.0 * t) +
                            Math.Sin(2.0 * Math.PI * root * 2.5 * t)) * 0.10;

            double beatTime = t % 0.5;
            double kick = (beatTime < 0.05) ? Math.Sin(2.0 * Math.PI * 55.0 * beatTime) * Math.Exp(-beatTime * 45.0) * 0.25 : 0.0;

            double hatTime = t % 0.25;
            double hat = (hatTime < 0.015) ? (new Random((int)(t * 1000)).NextDouble() - 0.5) * Math.Exp(-hatTime * 90.0) * 0.06 : 0.0;

            // مزيج هادئ وخافت جداً بحيث يظل صوت قارئ الشاشة واضحاً بنسبة 100%
            double mix = (bass + chord + kick + hat) * 0.40;
            short sample = (short)Math.Clamp(mix * 12000, -32000, 32000);

            s[i * 2] = sample;
            s[i * 2 + 1] = sample;
        }
        WriteWavStereo(path, s, sampleRate);
    }

    private static void WriteWavStereo(string path, short[] samples, int sampleRate = 44100)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var bw = new BinaryWriter(fs);

        int channels = 2;
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

    private static void WriteWav(string path, short[] samples, int sampleRate = 44100)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var bw = new BinaryWriter(fs);

        int channels = 1;
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
}
