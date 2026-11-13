using System.Text;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Audio.Anthems;

/// <summary>Exact-file anthem playback. Match ceremonies wait for playback, never a four-second excerpt.</summary>
public class AnthemEngine
{
    private readonly IScreenReader _screenReader;
    private readonly string _assetsBaseDir;
    private volatile bool _stopRequested;
    public string? LastPlayedAnthem { get; private set; }
    public bool WasSkipped { get; private set; }
    public bool IsAnthemPlaying { get; private set; }

    // Names are the exact country names in players_database.json. Legacy NAT_ IDs use the same file codes.
    private static readonly Dictionary<string,string> NationalCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Egypt"]="EGY", ["Morocco"]="MAR", ["Algeria"]="ALG", ["Tunisia"]="TUN",
        ["Saudi Arabia"]="KSA", ["France"]="FRA", ["England"]="ENG", ["Spain"]="ESP",
        ["Argentina"]="ARG", ["Brazil"]="BRA"
    };

    public AnthemEngine(IScreenReader screenReader, string? assetsDir = null)
    {
        _screenReader = screenReader;
        _assetsBaseDir = assetsDir ?? Path.Combine(SoundEffectsPlayer.BaseAudioDirectory, "Anthems");
        Directory.CreateDirectory(_assetsBaseDir);
    }

    public static string? ResolveNationalAnthemCode(Team team)
    {
        if (!team.IsNationalTeam) return null;
        if (NationalCodes.TryGetValue(team.Name, out var code)) return code;
        string id = team.Id.StartsWith("NAT_", StringComparison.OrdinalIgnoreCase) ? team.Id[4..] : team.Id;
        return NationalCodes.Values.Contains(id, StringComparer.OrdinalIgnoreCase) ? id.ToUpperInvariant() : null;
    }

    private string? FindAnthem(string id, bool national = false)
    {
        // Never let an unknown ID become a path or substitute a different country's file.
        if (id.Length == 0 || id.Any(c => !char.IsAsciiLetterOrDigit(c))) return null;
        string stem = Path.Combine(national ? Path.Combine(_assetsBaseDir,"National") : _assetsBaseDir,id);
        return File.Exists(stem+".mp3") ? stem+".mp3" : File.Exists(stem+".wav") ? stem+".wav" : null;
    }

    private bool StartAnthem(string? path, string id)
    {
        StopAllSounds();
        _stopRequested = false; WasSkipped = false; LastPlayedAnthem = null;
        float volume = SoundEffectsPlayer.GetEffectiveMusicVolume();
        if (path == null || volume <= .001f) return false;
        string type = path.EndsWith(".mp3",StringComparison.OrdinalIgnoreCase) ? "mpegvideo" : "waveaudio";
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
    public bool TryPlayNationalAnthemSample(string teamId) => StartAnthem(FindAnthem(teamId,true),teamId);
    public void PlayNationalAnthemSample(string? teamId="EGY") => TryPlayNationalAnthemSample(teamId ?? "EGY");

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

    public async Task PlayMatchNationalAnthemsAsync(Team home, Team away, Func<bool>? skipChecker=null)
    {
        WasSkipped=false;
        foreach (var team in new[]{home,away})
        {
            if (skipChecker?.Invoke()==true) { WasSkipped=true; break; }
            string? code=ResolveNationalAnthemCode(team);
            if (code == null || !TryPlayNationalAnthemSample(code)) continue;
            await WaitForCompletionAsync(skipChecker).ConfigureAwait(false);
            if (WasSkipped || skipChecker?.Invoke()==true) break;
        }
    }

    // Compatibility for non-UI callers. WinForms uses the awaited method above so keyboard events keep running.
    public void PlayMatchNationalAnthems(Team home, Team away, bool isAutomatedTest=false, Func<bool>? skipChecker=null)
    {
        if (!isAutomatedTest) PlayMatchNationalAnthemsAsync(home,away,skipChecker).GetAwaiter().GetResult();
    }

    public void PlayChampionsLeagueIntro(bool isAutomatedTest=false, Func<bool>? skipChecker=null)
    {
        if (!isAutomatedTest && PlayChampionsLeagueSample()) WaitForCompletionAsync(skipChecker).GetAwaiter().GetResult();
    }
}
