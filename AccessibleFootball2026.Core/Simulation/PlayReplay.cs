using AccessibleFootball2026.Core.Commentary;
namespace AccessibleFootball2026.Core.Simulation;
public sealed record PlayReplay(IReadOnlyList<ReplayFrame> Frames,MatchEvent Analysis);
public partial class MatchEngine
{
    public event Action<PlayReplay>? OnAnalysisReplayReady;
    public bool AnalysisReplayPending {get;private set;}
    public void CompleteAnalysisReplay()=>AnalysisReplayPending=false;
    private void RequestAnalysisReplay(MatchEventType type,CommentaryContext facts)
    {
        if(OnAnalysisReplayReady==null)return;
        var frames=_replayFrames.ToArray();
        if(frames.Length<2 || frames[^1].ElapsedSeconds<=frames[0].ElapsedSeconds)return;
        var analysis=new MatchEvent(type,"إعادة صوتية لمسار الكرة") {Context=facts.With(("replay.available",true))};
        AnalysisReplayPending=true;
        OnAnalysisReplayReady.Invoke(new PlayReplay(Array.AsReadOnly(frames),analysis));
    }
}
