using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Commentary;

namespace AccessibleFootball2026.Core;

public enum MatchEventType
{
    BallZoneChanged,
    TackleSuccessful,
    UserTackleSuccessful,
    OpponentTackleSuccessful,
    GoalScored,
    CornerKick,
    GoalKick,
    ThrowIn,
    FoulCommitted,
    Offside,
    MatchPaused,
    TacticalRadarInfo,
    BallKicked,
    ShotTaken,
    MissedChance,
    ExtraTimeStarted,
    PenaltyShootoutStarted,
    BallTrapped,
    TackleAttempted,
    HalfTime,
    YellowCard,
    RedCard,
    PlayerInjured,
    SubstitutionMade,

    // Milestone 3 Audio Overhaul & Real Football Events
    GoalkeeperSave,          // Heroic save or diving deflection by goalkeeper
    WoodworkHit,             // Shot striking post or crossbar (metallic clang)
    StoppageTimeAnnounced,   // Added/injury time announced at 44:30 and 89:30
    PenaltyAwarded,          // Penalty kick awarded inside 18-yard box
    FullTime,                // Full time match whistle and final score
    DramaticComeback,        // Late dramatic comeback goal
    HatTrickAchieved,        // 3rd goal scored by player in match
    MatchIntro,
    KickOff,
    GoalAnalysis,
    MatchAnalysis,
    PossessionPlay,
    PassCompleted,
    PassIntercepted,
    RegulationTimeEnded,
    ExtraTimeHalfTime,
    ShootoutKickReady,
    ShootoutGoal,
    ShootoutSave,
    ShootoutMiss,
    ShootoutFinished,
    FreeKickAwarded, FreeKickReady, FreeKickTaken,
    DribbleSuccessful, Clearance, ShotBlocked, ControlLost,
    SubstitutionPlanned, RefereeProtest, GoalDisallowed, MissAnalysis, OffsideAnalysis,
    Advantage, BallOut, LineupRequested, TrophyWon, CoachInstruction, CoachPraise, CoachWideInstruction, MatchAbandoned, VideoReviewStarted, VideoReviewDecision
}

/// <summary>
/// تفاصيل الهدف المسجل (اللاعب، صانع الألعاب، هل هو هدف ذاتي، والدقيقة)
/// </summary>
public class GoalDetails
{
    public Player Scorer { get; set; }
    public Player? Assister { get; set; }
    public bool IsOwnGoal { get; set; }
    public int Minute { get; set; }
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public Team ScoringTeam { get; set; }

    public GoalDetails(Player scorer, Player? assister, bool isOwnGoal, int minute, int homeScore, int awayScore, Team scoringTeam)
    {
        Scorer = scorer;
        Assister = assister;
        IsOwnGoal = isOwnGoal;
        Minute = minute;
        HomeScore = homeScore;
        AwayScore = awayScore;
        ScoringTeam = scoringTeam;
    }

    /// <summary>
    /// صياغة الإعلان الرسمي بعد انتهاء صرخة المعلق
    /// </summary>
    public string ToOfficialAnnouncement()
    {
        if (IsOwnGoal)
        {
            return $"هدف لصالح {ScoringTeam.Name}، سجله {Scorer.Name} بالخطأ في مرماه. الدقيقة {Minute}. النتيجة الآن: {HomeScore} - {AwayScore}";
        }

        string announcement = $"هدف لصالح {ScoringTeam.Name}، سجله {Scorer.Name}";
        if (Assister != null && Assister != Scorer)
        {
            announcement += $"، بصناعة {Assister.Name}";
        }

        announcement += $". في الدقيقة {Minute}. النتيجة: {HomeScore} - {AwayScore}";
        return announcement;
    }
}

/// <summary>
/// الحدث التكتيكي في المباراة مزود بوسم التغطية الصوتية وتفاصيل الأهداف
/// </summary>
public class MatchEvent
{
    public MatchEventType Type { get; }
    public string Description { get; }
    public PitchZone CurrentZone { get; }
    public GoalDetails? GoalData { get; set; }
    public float SpatialPan { get; set; } = 0.0f;
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    /// <summary>
    /// اللاعب صاحب الفعل الفعلي (المررر أو المسدد أو المستلم) لربط نداء الاسم الصوتي باللاعب الصحيح
    /// </summary>
    public Player? SubjectPlayer { get; set; }

    public CommentaryContext Context { get; set; } = CommentaryContext.Empty;

    public bool HandledByCommentary { get; set; } = false;

    public MatchEvent(MatchEventType type, string description, PitchZone currentZone = PitchZone.Midfield, GoalDetails? goalData = null, float spatialPan = 0.0f, Player? subjectPlayer = null)
    {
        Type = type;
        Description = description;
        CurrentZone = currentZone;
        GoalData = goalData;
        SpatialPan = spatialPan;
        SubjectPlayer = subjectPlayer;
    }
}

public class MatchEventBus
{
    public event Action<MatchEvent>? OnMatchEvent;

    public void Publish(MatchEvent matchEvent)
    {
        OnMatchEvent?.Invoke(matchEvent);
    }
}

