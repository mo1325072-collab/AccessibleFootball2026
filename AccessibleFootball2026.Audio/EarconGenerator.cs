using System;
using System.IO;
using System.Text;

namespace AccessibleFootball2026.Audio;

/// <summary>
/// مولد نغمات التنبيه التكتيكية الموسيقية الثمانية (8 Tonal Musical Earcons)
/// يوفر نغمات موسيقية صافية ومتميزة لتنبيه اللاعب الكفيف فورياً بدون أي إزعاج أو كلام اصطناعي
/// </summary>
public static class EarconGenerator
{
    public static void EnsureAllEarcons(string earconsDir)
    {
        Directory.CreateDirectory(earconsDir);
        EnsureEarconExists(Path.Combine(earconsDir, "TackleWon.wav"), "TackleWon");
        EnsureEarconExists(Path.Combine(earconsDir, "BallLost.wav"), "BallLost");
        EnsureEarconExists(Path.Combine(earconsDir, "Foul.wav"), "Foul");
        EnsureEarconExists(Path.Combine(earconsDir, "Penalty.wav"), "Penalty");
        EnsureEarconExists(Path.Combine(earconsDir, "Corner.wav"), "Corner");
        EnsureEarconExists(Path.Combine(earconsDir, "GoalKick.wav"), "GoalKick");
        EnsureEarconExists(Path.Combine(earconsDir, "ThrowIn.wav"), "ThrowIn");
        EnsureEarconExists(Path.Combine(earconsDir, "PlayerSwitch.wav"), "PlayerSwitch");
        EnsureEarconExists(Path.Combine(earconsDir, "TackleMissed.wav"), "TackleMissed");
    }

    public static void EnsureEarconExists(string path, string earconName)
    {
        if (File.Exists(path) && new FileInfo(path).Length > 100)
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        switch (earconName)
        {
            case "TackleWon":
                GenerateTackleWon(path);
                break;
            case "BallLost":
                GenerateBallLost(path);
                break;
            case "Foul":
                GenerateFoul(path);
                break;
            case "Penalty":
                GeneratePenalty(path);
                break;
            case "Corner":
                GenerateCorner(path);
                break;
            case "GoalKick":
                GenerateGoalKick(path);
                break;
            case "ThrowIn":
                GenerateThrowIn(path);
                break;
            case "PlayerSwitch":
                GeneratePlayerSwitch(path);
                break;
            case "TackleMissed":
                GenerateTackleMissed(path);
                break;
        }
    }

    private static void WriteWav(string path, short[] samples, int sampleRate = 44100)
    {
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

    // 1. TackleWon: Ascending major fifth C5 (523.25 Hz) -> G5 (783.99 Hz), 140ms
    public static void GenerateTackleWon(string path)
    {
        int sr = 44100;
        int n = (int)(sr * 0.14);
        short[] s = new short[n];
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / sr;
            double f = (t < 0.06) ? 523.25 : 783.99;
            double env = Math.Exp(-t * 22.0);
            double wave = Math.Sin(2.0 * Math.PI * f * t) + 0.3 * Math.Sin(2.0 * Math.PI * f * 2.0 * t);
            s[i] = (short)Math.Clamp(wave * env * 22000, -32768, 32767);
        }
        WriteWav(path, s, sr);
    }

    // 2. BallLost: Descending minor third Eb4 (311.13 Hz) -> C4 (261.63 Hz), 160ms
    public static void GenerateBallLost(string path)
    {
        int sr = 44100;
        int n = (int)(sr * 0.16);
        short[] s = new short[n];
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / sr;
            double f = (t < 0.07) ? 311.13 : 261.63;
            double env = Math.Exp(-t * 20.0);
            double wave = Math.Sin(2.0 * Math.PI * f * t) + 0.25 * Math.Sin(2.0 * Math.PI * f * 0.5 * t);
            s[i] = (short)Math.Clamp(wave * env * 20000, -32768, 32767);
        }
        WriteWav(path, s, sr);
    }

    // 3. Foul: Dissonant tritone clash F#4 (369.99 Hz) + C5 (523.25 Hz), 200ms
    public static void GenerateFoul(string path)
    {
        int sr = 44100;
        int n = (int)(sr * 0.20);
        short[] s = new short[n];
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / sr;
            double env = (t < 0.02) ? (t / 0.02) : Math.Exp(-(t - 0.02) * 16.0);
            double tremolo = 1.0 + 0.3 * Math.Sin(2.0 * Math.PI * 30.0 * t);
            double w1 = Math.Sin(2.0 * Math.PI * 369.99 * t);
            double w2 = Math.Sin(2.0 * Math.PI * 523.25 * t);
            double wave = (w1 + w2) * 0.5 * tremolo;
            s[i] = (short)Math.Clamp(wave * env * 23000, -32768, 32767);
        }
        WriteWav(path, s, sr);
    }

    // 4. Penalty: Dramatic ascending harmonic octave A4 (440 Hz) -> A5 (880 Hz), 350ms
    public static void GeneratePenalty(string path)
    {
        int sr = 44100;
        int n = (int)(sr * 0.35);
        short[] s = new short[n];
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / sr;
            double env = (t < 0.04) ? (t / 0.04) : Math.Exp(-(t - 0.04) * 8.0);
            double f = 440.0 + (440.0 * (t / 0.35));
            double wave = Math.Sin(2.0 * Math.PI * f * t) + 0.4 * Math.Sin(2.0 * Math.PI * f * 2.0 * t) + 0.15 * Math.Sin(2.0 * Math.PI * f * 3.0 * t);
            s[i] = (short)Math.Clamp(wave * env * 24000, -32768, 32767);
        }
        WriteWav(path, s, sr);
    }

    // 5. Corner: Major triad arpeggio G4 (392.0) -> C5 (523.25) -> E5 (659.25), 180ms
    public static void GenerateCorner(string path)
    {
        int sr = 44100;
        int n = (int)(sr * 0.18);
        short[] s = new short[n];
        double[] freqs = [392.00, 523.25, 659.25];
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / sr;
            int noteIdx = Math.Min(2, (int)(t / 0.06));
            double f = freqs[noteIdx];
            double env = Math.Exp(-((t % 0.06) * 25.0));
            double wave = Math.Sin(2.0 * Math.PI * f * t) + 0.25 * Math.Sin(2.0 * Math.PI * f * 2.0 * t);
            s[i] = (short)Math.Clamp(wave * env * 21000, -32768, 32767);
        }
        WriteWav(path, s, sr);
    }

    // 6. GoalKick: Low resonant descending fifth C3 (130.81 Hz) -> G3 (196.00 Hz), 240ms
    public static void GenerateGoalKick(string path)
    {
        int sr = 44100;
        int n = (int)(sr * 0.24);
        short[] s = new short[n];
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / sr;
            double f = (t < 0.10) ? 130.81 : 196.00;
            double env = Math.Exp(-t * 12.0);
            double wave = Math.Sin(2.0 * Math.PI * f * t) + 0.35 * Math.Sin(2.0 * Math.PI * f * 2.0 * t);
            s[i] = (short)Math.Clamp(wave * env * 22000, -32768, 32767);
        }
        WriteWav(path, s, sr);
    }

    // 7. ThrowIn: Two-stroke wooden clicks 750 Hz -> 600 Hz, 80ms
    public static void GenerateThrowIn(string path)
    {
        int sr = 44100;
        int n = (int)(sr * 0.08);
        short[] s = new short[n];
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / sr;
            double f = (t < 0.04) ? 750.0 : 600.0;
            double env = Math.Exp(-((t % 0.04) * 70.0));
            double wave = Math.Sin(2.0 * Math.PI * f * t);
            s[i] = (short)Math.Clamp(wave * env * 18000, -32768, 32767);
        }
        WriteWav(path, s, sr);
    }

    // 8. PlayerSwitch: Tactile micro-click blip 1100 Hz, 30ms
    public static void GeneratePlayerSwitch(string path)
    {
        int sr = 44100;
        int n = (int)(sr * 0.03);
        short[] s = new short[n];
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / sr;
            double env = Math.Exp(-t * 90.0);
            double wave = Math.Sin(2.0 * Math.PI * 1100.0 * t);
            s[i] = (short)Math.Clamp(wave * env * 16000, -32768, 32767);
        }
        WriteWav(path, s, sr);
    }

    // 9. TackleMissed: Soft swift air whoosh with gentle descending tone 420 Hz -> 210 Hz, 130ms
    public static void GenerateTackleMissed(string path)
    {
        int sr = 44100;
        int n = (int)(sr * 0.13);
        short[] s = new short[n];
        var rand = new Random(707);
        double filterNoise = 0.0;
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / sr;
            double env = Math.Exp(-t * 26.0);
            double f = 420.0 - (210.0 * (t / 0.13)); // descending subtle tone
            double tone = Math.Sin(2.0 * Math.PI * f * t) * 0.45;
            double white = (rand.NextDouble() * 2.0 - 1.0);
            filterNoise = filterNoise * 0.82 + white * 0.18; // soft air friction whoosh
            double wave = (tone + filterNoise * 0.55) * env * 20000;
            s[i] = (short)Math.Clamp(wave, -32768, 32767);
        }
        WriteWav(path, s, sr);
    }
}
