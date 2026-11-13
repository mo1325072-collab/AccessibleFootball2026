using System.Text;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Audio.Anthems;

/// <summary>Approved Champions League intro playback. National anthem feature is canceled.</summary>
public class AnthemEngine
{
    private readonly IScreenReader _screenReader;
    private readonly string _assetsBaseDir;
    private volatile bool _stopRequested;
    public string? LastPlayedAnthem { get; private set; }
    public bool WasSkipped { get; private set; }
    public bool IsAnthemPlaying { get; private set; }

    public AnthemEngine(IScreenReader screenReader, string? assetsDir = null)
    {
        _screenReader = screenReader;
        _assetsBaseDir = assetsDir ?? Path.Combine(SoundEffectsPlayer.BaseAudioDirectory, "Anthems");
        Directory.CreateDirectory(_assetsBaseDir);
    }

    // National anthem feature canceled by the user. Legacy entrypoints stay inert for callers.
    public static string? ResolveNationalAnthemCode(Team team) => null;

    private string? FindAnthem(string id)
    {
        if (id != "ChampionsLeague") return null;
        string stem = Path.Combine(_assetsBaseDir,id);
        return File.Exists(stem+".mp3") ? stem+".mp3" : File.Exists(stem+".wav") ? stem+".wav" : null;
    }

    private bool StartAnthem(string? path, string id)
    {
        StopAllSounds();
        _stopRequested = false; WasSkipped = false; LastPlayedAnthem = null;
        float volume = SoundEffectsPlayer.GetEffectiveMusicVolume();
        if (path == null || volume <= .001f) return false;
        string type = SoundEffectsPlayer.GetMciType(path);
        if (MciPlayback.SendString($"open \"{path}\" type {type} alias anthem",null,0,IntPtr.Zero) != 0) return false;
        if (MciPlayback.SendString($"setaudio anthem volume to {(int)(volume*1000)}",null,0,IntPtr.Zero) != 0 ||
            MciPlayback.SendString("play anthem from 0",null,0,IntPtr.Zero) != 0)
        { StopAllSounds(); return false; }
        LastPlayedAnthem=id; IsAnthemPlaying=true; return true;
    }

    public void StopAllSounds()
    {
        _stopRequested=true; IsAnthemPlaying=false;
        MciPlayback.SendString("stop anthem",null,0,IntPtr.Zero);
        MciPlayback.SendString("close anthem",null,0,IntPtr.Zero);
    }

    public bool PlayChampionsLeagueSample() => StartAnthem(FindAnthem("ChampionsLeague"),"ChampionsLeague");
    public bool TryPlayNationalAnthemSample(string teamId) => false;
    public void PlayNationalAnthemSample(string? teamId=null) { }

    public bool HasPlaybackCompleted => !IsAnthemPlaying || PlaybackHasCompleted();

    private bool PlaybackHasCompleted()
    {
        var status=new StringBuilder(64);
        int result=MciPlayback.SendString("status anthem mode",status,status.Capacity,IntPtr.Zero);
        // A failed device/status must not strand the player on a silent ceremony.
        return result != 0 || !status.ToString().Trim().Equals("playing",StringComparison.OrdinalIgnoreCase);
    }

    private bool CheckSkip(Func<bool>? skipChecker)
    {
        if (skipChecker?.Invoke() != true) return false;
        WasSkipped=true; StopAllSounds(); return true;
    }

    private async Task WaitForCompletionAsync(Func<bool>? skipChecker)
    {
        while (!_stopRequested)
        {
            if (CheckSkip(skipChecker) || PlaybackHasCompleted()) break;
            await Task.Delay(40).ConfigureAwait(false);
        }
        StopAllSounds();
    }

    public Task PlayMatchNationalAnthemsAsync(Team home, Team away, Func<bool>? skipChecker=null) => Task.CompletedTask;
    public void PlayMatchNationalAnthems(Team home, Team away, bool isAutomatedTest=false, Func<bool>? skipChecker=null) { }

    public void PlayChampionsLeagueIntro(bool isAutomatedTest=false, Func<bool>? skipChecker=null)
    {
        if (!isAutomatedTest && PlayChampionsLeagueSample()) WaitForCompletionAsync(skipChecker).GetAwaiter().GetResult();
    }
}
