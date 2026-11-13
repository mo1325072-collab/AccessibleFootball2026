using AccessibleFootball2026.Core.Models;
namespace AccessibleFootball2026.Core.Simulation;

/// <summary>IFAB 2026/27 categories. Corner correction is an optional immediate competition rule, not a delayed review.</summary>
public enum VideoReviewCategory { Goal, Penalty, DirectRedCard, ClearlyIncorrectSecondCaution, MistakenIdentity }
public partial class MatchEngine
{
    private static string T(string arabic,string english) => MatchSettings.Current.Language==UiLanguage.Arabic?arabic:english;
    private Action? _videoDecision;
    private float _videoSeconds;
    private Action? _videoVerdict;
    private bool _videoVerdictAnnounced;
    public Func<bool>? VideoAnnouncementPending { get; set; }
    private bool _applyingVideoDecision;
    public bool VideoReviewPending => _videoDecision != null;
    public const float VideoReviewDurationSeconds = 4f; // Presentation timing, not an IFAB time limit.
    private bool BeginVideoReview(VideoReviewCategory category, Action apply, string decision, string outcome)
    {
        if (!MatchSettings.Current.AiVarEnabled || _applyingVideoDecision) return false;
        if (VideoReviewPending) return true;
        _videoDecision=apply;
        _videoVerdict=()=>
        {
            Publish(new MatchEvent(MatchEventType.VideoReviewDecision,decision,CurrentZone),
                ("var.category",category.ToString()),("var.outcome",outcome));
        };
        _videoVerdictAnnounced=false;
        _videoSeconds=0;
        Publish(new MatchEvent(MatchEventType.VideoReviewStarted,
            T("جاري فحص اللقطة بواسطة حكم الفيديو المساعد. انتظر قرار الحكم.","VAR checking the incident. Waiting for the referee's decision."),CurrentZone),
            ("var.category",category.ToString()));
        return true;
    }
    /// <summary>Review a provisional card against the incident's observed offender and confirmed offence.
    /// No random wrong-player or invented foul generator. The live referee normally identifies correctly.</summary>
    public bool ReviewCardDecision(Player shownPlayer, Player observedOffender, bool directRed, bool offenceConfirmed, bool clearErrorEvidence)
    {
        if(TeamOf(shownPlayer)==null || TeamOf(observedOffender)==null || !observedOffender.IsActiveOnPitch)return false;
        bool mistaken=shownPlayer!=observedOffender;
        bool wrongSecond=shownPlayer.YellowCards==1 && !directRed && !offenceConfirmed;
        if((mistaken||wrongSecond)&&clearErrorEvidence&&MatchSettings.Current.AiVarEnabled)
        {
            var category=mistaken?VideoReviewCategory.MistakenIdentity:VideoReviewCategory.ClearlyIncorrectSecondCaution;
            string verdict=mistaken?T($"قرار الفار: تصحيح هوية اللاعب. المخالفة على {observedOffender.Name} وليست على {shownPlayer.Name}.",
                $"VAR decision: mistaken identity corrected. The offender is {observedOffender.Name}, not {shownPlayer.Name}."):
                T("قرار الفار: إلغاء الإنذار الثاني الخاطئ. لا توجد بطاقة حمراء.","VAR decision: incorrect second caution cancelled. No red card.");
            return BeginVideoReview(category,()=>{if(offenceConfirmed)IssueCard(observedOffender,directRed);},verdict,wrongSecond?"noRedCard":"identityCorrected");
        }
        if(offenceConfirmed)IssueCard(shownPlayer,directRed);
        return false;
    }

    private bool UpdateVideoReview(float dt)
    {
        if (_videoDecision is not {} decision) return false;
        // Consume unsafe taps without advancing the ball, players or match clock.
        _inputHandler.PollCurrentActions(Ball.Owner != null && HomeTeam.Players.Contains(Ball.Owner));
        if(State!=MatchState.InPlay)return true;
        if(VideoAnnouncementPending?.Invoke()==true)return true;
        _videoSeconds+=dt;
        if(_videoSeconds<VideoReviewDurationSeconds)return true;
        if(!_videoVerdictAnnounced)
        {
            _videoVerdictAnnounced=true;
            _videoVerdict?.Invoke();
            if(VideoAnnouncementPending?.Invoke()==true)return true;
        }
        _videoDecision=null;_videoVerdict=null;
        _applyingVideoDecision=true;
        try { decision(); } finally { _applyingVideoDecision=false; }
        return true;
    }
}
