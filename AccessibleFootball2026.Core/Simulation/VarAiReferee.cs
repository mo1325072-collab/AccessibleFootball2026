namespace AccessibleFootball2026.Core.Simulation;

public enum VarReviewType { OffsideCheck, PenaltyFoul, GoalLineTechnology }

public class VarDecisionResult
{
    public VarReviewType Type { get; set; }
    public bool EvidenceAvailable { get; set; }
    public bool Overturned { get; set; }
    public string AnnouncementArabic { get; set; } = string.Empty;
    public string AnnouncementEnglish { get; set; } = string.Empty;
    public float DecisionMarginCm { get; set; } = float.NaN;
}

/// <summary>
/// Legacy name-only preview API, not the match referee or a Qwen integration.
/// Names and an initial opinion contain no positional/contact evidence. Never invent a review.
/// Active match decisions use OffsidePlay, SetPiecePlay and the physical ball trajectory.
/// </summary>
public static class VarAiReferee
{
    private static VarDecisionResult Unavailable(VarReviewType type) => new()
    {
        Type = type, EvidenceAvailable = false, Overturned = false,
        AnnouncementArabic = "لا تتوفر بيانات موضعية لهذه المراجعة. القرار السابق لم يتغير.",
        AnnouncementEnglish = "No positional evidence is available for this review. The prior decision is unchanged."
    };
    public static VarDecisionResult ReviewOffside(string attackingPlayerName, bool initialCallOffside) => Unavailable(VarReviewType.OffsideCheck);
    public static VarDecisionResult ReviewPenalty(string fouledPlayerName, string defendingPlayerName, bool initialCallPenalty) => Unavailable(VarReviewType.PenaltyFoul);
    public static VarDecisionResult ReviewGoalLine(string defendingTeamName) => Unavailable(VarReviewType.GoalLineTechnology);
}
