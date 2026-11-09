using System;
using System.IO;

namespace AccessibleFootball2026.Audio;

/// <summary>
/// مدير نغمات التنبيه التكتيكية الثمانية (Earcon Manager)
/// </summary>
public static class EarconManager
{
    public static void Initialize(string assetsBaseDir)
    {
        string earconsDir = Path.Combine(assetsBaseDir, "SFX", "Earcons");
        EarconGenerator.EnsureAllEarcons(earconsDir);
    }

    public static void Play(string earconName)
    {
        SoundEffectsPlayer.PlayEarcon(earconName);
    }
}
