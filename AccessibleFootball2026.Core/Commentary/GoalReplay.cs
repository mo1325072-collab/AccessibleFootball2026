using System.Collections.ObjectModel;
using System.Numerics;

namespace AccessibleFootball2026.Core.Commentary;

public sealed record ReplayFrame(float MatchMinute, Vector3 BallPosition, Vector3 BallVelocity, int? BallOwnerId, float ElapsedSeconds = 0);

/// <summary>Actual sampled ball flight, separate from live match state. It never advances physics or awards another goal.</summary>
public sealed class GoalReplay
{
    public IReadOnlyList<ReplayFrame> Frames { get; }
    public CommentaryContext Context { get; }
    public GoalDetails Goal { get; }
    public GoalReplay(IEnumerable<ReplayFrame> frames, CommentaryContext context, GoalDetails goal)
    {
        Frames = Array.AsReadOnly(frames.ToArray());
        Context = context.With(("replay.available", true));
        Goal = goal;
    }
}
