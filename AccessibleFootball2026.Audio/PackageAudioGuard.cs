namespace AccessibleFootball2026.Audio;

/// <summary>Read-only checks before initialization can create fallback files and hide package omissions.</summary>
public static class PackageAudioGuard
{
    public static IReadOnlyList<string> RequiredPaths { get; } = new[]
    {
        "Menu/Move.wav", "Menu/Select.wav", "Menu/MenuMusic.mp3", "SFX/Beacon/GoalBeacon.wav",
        "SFX/Kicks/Pass_Center.wav", "SFX/Kicks/Shot_Center.wav", "SFX/Kicks/Cross_Center.wav",
        "SFX/Chants/chants_index.json", "SFX/StadiumAcoustics/Acoustic_3.wav",
        "Commentary/FaresAwad/commentary_context.json"
    }.Concat(new[]{"TackleWon","TackleMissed","BallLost","Foul","Penalty","Corner","GoalKick","ThrowIn","PlayerSwitch"}
        .Select(cue=>"SFX/Earcons/"+cue+".wav")).ToArray();

    public static IReadOnlyList<string> FindMissing(string audioRoot)
    {
        var missing=new List<string>();
        foreach(string relative in RequiredPaths)
        {
            string path=Path.Combine(audioRoot,relative);
            if(!File.Exists(path)||new FileInfo(path).Length==0)missing.Add(relative);
        }
        string whistle=Path.Combine(audioRoot,"SFX","Whistles");
        if(!Directory.Exists(whistle)||!Directory.EnumerateFiles(whistle,"*.wav").Any(p=>new FileInfo(p).Length>0))
            missing.Add("SFX/Whistles/*.wav");
        return missing;
    }
}
