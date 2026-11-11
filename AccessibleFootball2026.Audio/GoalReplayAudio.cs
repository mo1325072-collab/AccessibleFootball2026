using System.Text;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Commentary;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;

namespace AccessibleFootball2026.Audio;

/// <summary>A labelled sonification of the recorded ball trajectory, never a second simulation.</summary>
public static class GoalReplayAudio
{
    public static string? Render(GoalReplay replay)
        => RenderFrames(replay.Frames);

    public static string? RenderFrames(IReadOnlyList<ReplayFrame> recordedFrames)
    {
        if (recordedFrames.Count < 2) return null;
        // Last five recorded seconds. Time and geometry come from captured frames.
        float end = recordedFrames[^1].ElapsedSeconds;
        var frames = recordedFrames.Where(f => f.ElapsedSeconds >= end - 5).ToArray();
        float start = frames[0].ElapsedSeconds;
        float duration = end - start;
        if (duration <= 0 || !float.IsFinite(duration)) return null;
        const int rate = 22050;
        int sampleCount = (int)(duration * rate);
        int frameIndex = 0;
        string folder = Path.Combine(Path.GetTempPath(), "AccessibleFootball2026", "Replay");
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, Guid.NewGuid().ToString("N")+".wav");
        using var writer = new BinaryWriter(File.Create(path));
        writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + sampleCount * 4);
        writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)2);
        writer.Write(rate); writer.Write(rate * 4); writer.Write((short)4); writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(sampleCount * 4);
        double phase = 0;
        for (int i = 0; i < sampleCount; i++)
        {
            float at = start + (float)i / rate;
            while (frameIndex + 1 < frames.Length && frames[frameIndex + 1].ElapsedSeconds <= at) frameIndex++;
            var frame = frames[frameIndex];
            double pan = Math.Clamp(frame.BallPosition.X / Pitch.Length * 2 - 1, -1, 1);
            double frequency = 280 + Math.Clamp(frame.BallPosition.Z, 0, 12) * 65;
            phase += 2 * Math.PI * frequency / rate;
            double pulse = .45 + .55 * Math.Pow(Math.Sin(2 * Math.PI * 5 * i / rate), 2);
            double envelope = Math.Min(1, i / 441.0) * Math.Min(1, (sampleCount - i) / 441.0);
            double sample = Math.Sin(phase) * pulse * envelope * 4500;
            writer.Write((short)(sample * Math.Sqrt((1 - pan) / 2)));
            writer.Write((short)(sample * Math.Sqrt((1 + pan) / 2)));
        }
        return path;
    }
}
