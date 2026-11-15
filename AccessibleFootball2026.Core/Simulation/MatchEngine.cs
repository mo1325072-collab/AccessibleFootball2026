using System.Numerics;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Input;

namespace AccessibleFootball2026.Core.Simulation;

public enum MatchState
{
    InPlay,
    GoalCelebration,
    Paused,
    HalfTime,
    PenaltyShootout,
    GameOver
}

public enum CrossStyle { Center, FarPost, Ground, GroundFarPost }
public enum PassType
{
    ShortPass,
    ThroughBall,
    Cross
}

/// <summary>
/// معلومات حالة التمريرة أثناء تحليق الكرة أو تدحرجها على العشب نحو المستلم
/// </summary>
public class PassTransitInfo
{
    public Player Sender { get; set; } = null!;
    public Player IntendedReceiver { get; set; } = null!;
    public Vector2 TargetPosition { get; set; }
    public PassType PassType { get; set; }
    public float PassSpeed { get; set; }
    public float ElapsedTime { get; set; } = 0.0f;
    public float EstimatedDuration { get; set; }
    public bool IsActive { get; set; } = true;

    // توفير التوافق مع العقود البرمجية المشتركة
    public Player Receiver => IntendedReceiver;
    public Vector2 TargetPoint => TargetPosition;
    public PassType Type => PassType;
    public float EstimatedArrivalTime => EstimatedDuration;
}

/// <summary>
/// محرك المباراة المتكامل: يدير قوانين وتوقيت المباراة مع تطبيق قاعدة:
/// عدم استئناف اللعب بعد الهدف إلا بعد انتهاء المعلق من صرخته، ثم إعلان قارئ الشاشة لمسجل الهدف والأسيست.
/// </summary>
public partial class MatchEngine
{
    public Pitch Pitch { get; } = new();
    public Ball Ball { get; } = new();
    public Team HomeTeam { get; }
    public Team AwayTeam { get; }
    public MatchEventBus EventBus { get; }

    public Player ActiveUserPlayer { get; private set; }
    public PitchZone CurrentZone { get; private set; } = PitchZone.Midfield;

    public PassTransitInfo? ActivePassTransit { get; private set; }
    public bool IsPassInTransit => ActivePassTransit != null && ActivePassTransit.IsActive;

    public float MatchTimeMinutes { get; private set; } = 0.0f;
    public MatchState State { get; set; } = MatchState.InPlay;
    public bool IsExtraTime { get; private set; } = false;
    public bool IsExtraTimeStartInterval { get; private set; }
    public bool UserAttackingRight { get; set; } = true;
    public bool HalfTimeReached { get; private set; } = false;
    public void SetMatchTimeMinutes(float minutes) => MatchTimeMinutes = minutes;

    public event Action? OnNearBoundary;
    public event Action<int>? OnCrowdVolumeChanged;

    public float TackleCooldownRemaining => _tackleCooldownTimer;
    public float SlideDurationRemaining => _slideDurationTimer;
    public bool IsSliding => _slideDurationTimer > 0.0f && _slidingPlayer == ActiveUserPlayer;
    public float PossessionGraceRemaining => _possessionGraceTimer;
    public float AiTackleCooldownRemaining => _aiTackleCooldownTimer;

    private float _tackleCooldownTimer = 0.0f;
    private float _slideDurationTimer = 0.0f;
    private float _possessionGraceTimer = 0.0f;
    private float _aiTackleCooldownTimer = 0.0f;
    private Player? _slidingPlayer;
    private bool _slideContactResolved;
    private Vector2 _slideImpulseVelocity = Vector2.Zero;
    private float _switchCooldownSeconds;

    public void GrantPossessionGrace(float seconds = 1.4f)
    {
        _possessionGraceTimer = Math.Max(_possessionGraceTimer, seconds);
        _aiTackleCooldownTimer = Math.Max(_aiTackleCooldownTimer, seconds + 0.6f);
    }

    private bool _stoppage1stHalfAnnounced = false;
    private bool _stoppage2ndHalfAnnounced = false;
    private bool _fullTimeAnnounced = false;
    private int _stoppageMinutes1st = 2;
    private int _stoppageMinutes2nd = 3;

    public bool Stoppage1stHalfAnnounced => _stoppage1stHalfAnnounced;
    public bool Stoppage2ndHalfAnnounced => _stoppage2ndHalfAnnounced;
    public bool FullTimeAnnounced => _fullTimeAnnounced;
    public int StoppageMinutes1st => _stoppageMinutes1st;
    public int StoppageMinutes2nd => _stoppageMinutes2nd;
    public void SetStoppageMinutes(int firstHalf, int secondHalf)
    {
        _stoppageMinutes1st = Math.Max(0, firstHalf);
        _stoppageMinutes2nd = Math.Max(0, secondHalf);
    }

    private float _boundaryTimer = 0.0f;
    private float _crowdUpdateTimer = 0.0f;
    private float _currentCrowdVolume = 500.0f;
    private bool _isShotInProgress = false;
    private float _shotTargetGoalX = 0.0f;   // المرمى المستهدف بالتسديدة الجارية
    private bool _shotGkResolved = false;    // هل حُسمت مواجهة الحارس لهذه التسديدة
    // Defensive approach assistance blend strength (simulation parameter; tests can zero it for baseline).
    private float _defensiveApproachAssistStrength = 0.4f;
    private Player? _shotShooter = null;     // مسدد التسديدة الجارية
    private float _celebrationTimer = 0.0f;
    public bool AutomaticGoalRestart { get; set; } = true;
    public void CompleteGoalCelebration()
    {
        if (State != MatchState.GoalCelebration) return;
        _celebrationTimer = 0;
        AdvanceGoalCelebration(0);
    }
    private void AdvanceGoalCelebration(float deltaTime)
    {
        _celebrationTimer -= deltaTime;
        if (_celebrationTimer > 0) return;
        Team kickoffTeam = HomeTeam.Score > _lastRecordedHomeScore ? AwayTeam : HomeTeam;
        _lastRecordedHomeScore = HomeTeam.Score;
        _lastRecordedAwayScore = AwayTeam.Score;
        ResumePlayAfterGoalCelebration(kickoffTeam);
        Publish(new MatchEvent(MatchEventType.KickOff, $"ركلة البداية لصالح {kickoffTeam.Name}", PitchZone.Midfield));
    }
    private float _gkHoldTimer = 0.0f;
    private float _ballFreeTimer = 0.0f;
    private float _kickoffProtectionTimer = 4.0f;
    private int _lastRecordedHomeScore = 0;
    private int _lastRecordedAwayScore = 0;
    private bool _homeTeamWasTrailing = false;
    private bool _awayTeamWasTrailing = false;

    private MatchState _stateBeforePause = MatchState.InPlay;
    public void PauseMatch()
    {
        if (State is MatchState.HalfTime or MatchState.GameOver or MatchState.Paused) return;
        BeginSubstitutionStoppage();
        _inputHandler.Reset();
        _stateBeforePause = State;
        State = MatchState.Paused;
        Publish(new MatchEvent(MatchEventType.MatchPaused, "إيقاف مؤقت"));
    }

    public void ResumeMatch()
    {
        if (PendingInjuryPlayer != null) return;
        bool intervalEnded = State == MatchState.HalfTime;
        if (State != MatchState.HalfTime && State != MatchState.Paused) return;
        State = intervalEnded ? MatchState.InPlay : _stateBeforePause;
        if (intervalEnded) IsExtraTimeStartInterval = false;

        if (intervalEnded) Publish(new MatchEvent(MatchEventType.KickOff, "بداية الشوط"));
    }

    public bool CanMakeSubstitution(Team team) => team.CanMakeSubstitution(State == MatchState.HalfTime);

    public bool MakeSubstitution(Team team, Player playerOut, Player playerIn)
    {
        if(team!=HomeTeam&&team!=AwayTeam)return false;
        if (PendingInjuryPlayer != null && !_resolvingInjuryChoice) return false;
        if(playerOut.IsSentOff||playerIn.IsSentOff||!team.Players.Contains(playerOut)||!team.Bench.Contains(playerIn)||!CanMakeSubstitution(team))return false;
        Publish(new MatchEvent(MatchEventType.SubstitutionPlanned, $"تجهيز تبديل {playerOut.Name} بـ {playerIn.Name}", subjectPlayer:playerIn));
        BeginSubstitutionStoppage();
        bool success = team.MakeSubstitution(playerOut, playerIn, isHalfTime: State == MatchState.HalfTime, isMatchActive: true);
        if (success)
        {
            if (_runUpPlayer == playerOut) { _runUpPlayer = null; _runUpRemaining = 0; }
            // A replacement takes the outgoing player's live receiving slot, not the
            // historical identity of the player who already kicked the ball.
            if (ActivePassTransit is { } pass && pass.IntendedReceiver == playerOut)
                pass.IntendedReceiver = playerIn;
            if (_commentaryPass is { } pendingPass && pendingPass.Target == playerOut)
                _commentaryPass = pendingPass with { Target = playerIn };
            if (Ball.Owner == playerOut)
            {
                Ball.CatchOrTackle(playerIn);
                if (LastKicker == playerOut) LastKicker = playerIn;
            }
            if (Restart is { } restart && restart.Taker == playerOut)
                Restart = restart with { Taker = playerIn };
            if (ActiveUserPlayer == playerOut)
            {
                ActiveUserPlayer = playerIn;
            }
            Publish(new MatchEvent(MatchEventType.SubstitutionMade, $"تبديل: خروج {playerOut.Name} ودخول {playerIn.Name} ({team.SubstitutionsUsed}/{team.MaxSubstitutions})", subjectPlayer: playerIn),
                ("substitution.playerRating", PerformanceOf(playerOut).Rating),
                ("substitution.reason", playerOut.InjuryStatus == PlayerInjuryStatus.SevereInjury ? "severeInjury" : playerOut.InjuryStatus == PlayerInjuryStatus.MinorKnock ? "minorInjury" : "tactical"),
                ("restart.held", AwaitingStoppageConfirmation), ("restart.teamName", Restart?.Team.Name),
                ("restart.takerName", Restart?.Taker.Name));
        }
        return success;
    }

    public void TriggerHalfTime()
    {
        if (HalfTimeReached || State == MatchState.GameOver) return;
        State = MatchState.HalfTime;
        AwaitingStoppageConfirmation = false;
        CompleteAnalysisReplay();
        HalfTimeReached = true;
        SwapPlayingEnds();
        ActivePassTransit = null;
        Restart = null;
        _commentaryPass = null;
        _isShotInProgress = false;
        _shotContextValid = false;
        PreviousKicker = null;
        foreach (var p in HomeTeam.Players.Concat(AwayTeam.Players))
            p.CurrentStamina = Math.Min(p.BaseStamina, p.CurrentStamina + 10);
        // The away team takes the second kickoff. No play runs until the interval menu starts it.
        PlaceKickoff(AwayTeam);
        CheckPitchZone();
        Publish(new MatchEvent(MatchEventType.HalfTime,
            $"نهاية الشوط الأول. {HomeTeam.Name} {HomeTeam.Score}، {AwayTeam.Name} {AwayTeam.Score}. تبديل المرميين. اختر بدء الشوط الثاني من قائمة المباراة."), ("match.half", 1));
        RunNativeAutomation();
    }

    public void TriggerExtraTime()
    {
        if (IsExtraTime || State == MatchState.GameOver) return;
        IsExtraTime = true;
        HomeTeam.IsExtraTime = true;
        AwayTeam.IsExtraTime = true;
        MatchTimeMinutes = 90.0f;
        SwapPlayingEnds();
        PlaceKickoff(AwayTeam);
        IsExtraTimeStartInterval = true;
        State = MatchState.HalfTime;
        BeginSubstitutionStoppage();
        Publish(new MatchEvent(MatchEventType.ExtraTimeStarted, "نهاية الوقت الأصلي بالتعادل! الاحتكام للأشواط الإضافية (+1 تبديل إضافي وفترة توقف رابعة)"));
        RunNativeAutomation();
    }

    public void ReportTacticalRadar()
    {
        bool own = Ball.Owner is { } owner && HomeTeam.Players.Contains(owner);
        var target = own == UserAttackingRight ? Pitch.RightGoalCenter : Pitch.LeftGoalCenter;
        int distance = (int)Vector2.Distance(Ball.Position2D, target);
        string description = Ball.Owner is { } holder
            ? LocalizationService.Text($"{holder.Name}، {distance} متر", $"{holder.Name}, {distance} metres")
            : LocalizationService.Text("الكرة حرة", "Loose ball");
        Publish(new MatchEvent(MatchEventType.TacticalRadarInfo, description, CurrentZone));
    }

    private readonly IInputHandler _inputHandler;

    // تتبع آخر لاعبين لمسوا الكرة لحساب صانع الهدف (الأسيست)
    public Player? LastKicker { get; private set; }
    public Player? PreviousKicker { get; private set; }

    public MatchEngine(Team homeTeam, Team awayTeam, MatchEventBus eventBus, IInputHandler inputHandler)
    {
        HomeTeam = homeTeam;
        AwayTeam = awayTeam;
        HomeTeam.Side = TeamSide.Home;
        AwayTeam.Side = TeamSide.Away;
        EventBus = eventBus;
        _inputHandler = inputHandler;
        Ball.ControlChanged += _inputHandler.InvalidatePossessionActions;

        HomeTeam.MatchAttackingRight = true;
        AwayTeam.MatchAttackingRight = false;
        FormationManager.ApplyFormation(HomeTeam, HomeTeam.CurrentFormation);
        FormationManager.ApplyFormation(AwayTeam, AwayTeam.CurrentFormation);

        string? lineupIssue = HomeTeam.GetMatchStartIssue() ?? AwayTeam.GetMatchStartIssue();
        if (lineupIssue != null) throw new ArgumentException(lineupIssue);
        ActiveUserPlayer = HomeTeam.Players.First(p => p.IsActiveOnPitch);
        PlaceKickoff(HomeTeam, HomeTeam.Players.ElementAtOrDefault(9));

    }

    public void Update(float deltaTime)
    {
        if (!float.IsFinite(deltaTime) || deltaTime <= 0) return;
        // Ball/body/keeper/rule checks share a bounded simulation clock, not the render interval.
        // Stoppages get one update only; no queued real-time remainder can leak through a hold.
        if (State != MatchState.InPlay || Restart != null || AnalysisReplayPending || VideoReviewPending || AwaitingStoppageConfirmation)
        { Tick(deltaTime); return; }
        var framePositions = HomeTeam.Players.Concat(AwayTeam.Players).ToDictionary(p => p, p => p.Position2D);
        float remaining = deltaTime;
        while (remaining > .000001f)
        {
            float step = Math.Min(.02f, remaining);
            Tick(step);
            remaining -= step;
            if (State != MatchState.InPlay || Restart != null || AnalysisReplayPending || VideoReviewPending || AwaitingStoppageConfirmation) break;
        }
        if (remaining <= .000001f && State == MatchState.InPlay && Restart == null)
            foreach (var player in framePositions.Keys) player.Velocity = (player.Position2D - framePositions[player]) / deltaTime;
    }

    private void Tick(float deltaTime)
    {
        if (!float.IsFinite(deltaTime) || deltaTime <= 0) return;
        if (UpdateVideoReview(deltaTime))
        {
            _inputHandler.InvalidatePossessionActions();
            _inputHandler.PollCurrentActions(Ball.Owner != null && HomeTeam.Players.Contains(Ball.Owner));
            return;
        }
        if (PendingInjuryPlayer != null)
        {
            _inputHandler.PollCurrentActions(Ball.Owner != null && HomeTeam.Players.Contains(Ball.Owner));
            return; // No clock, ball, CPU actions or optional auto-substitution before the user's choice.
        }
        StopIfTooFewPlayers();
        if (State == MatchState.GameOver) return;
        RunNativeAutomation();
        if (AwaitingStoppageConfirmation)
        {
            // Held incidents freeze play, not explicit information requests. Consume unsafe queued
            // actions here so a shot/tackle tapped during narration cannot fire after dismissal.
            var heldActions = _inputHandler.PollCurrentActions(Ball.Owner != null && HomeTeam.Players.Contains(Ball.Owner));
            if (heldActions.Contains(GameAction.TacticalRadar)) ReportTacticalRadar();
            _inputHandler.InvalidatePossessionActions();
            return;
        }
        if (State == MatchState.GoalCelebration)
        {
            if (AutomaticGoalRestart) AdvanceGoalCelebration(deltaTime);
            return;
        }

        if (State != MatchState.InPlay || AnalysisReplayPending) return;
        // Only commanded locomotion feeds collision severity. Interpolation/teleports do not.
        (_previousContactMotion,_contactMotion)=(_contactMotion,_previousContactMotion);
        _contactMotion.Clear();
        _switchCooldownSeconds = Math.Max(0, _switchCooldownSeconds - deltaTime);
        if (Restart == null) EndSubstitutionStoppage();
        CaptureReplayFrame();
        if (Restart == null) TrackPlayFrame(deltaTime);

        if (_kickoffProtectionTimer > 0.0f)
        {
            _kickoffProtectionTimer -= deltaTime;
        }

        // حساب سرعة مرور وقت المباراة وفق إعدادات المستخدم
        float durationMinutes = Math.Max(1, MatchSettings.Current.MatchDurationMinutes);
        float minuteScale = 90.0f / (durationMinutes * 60.0f);
        MatchTimeMinutes += deltaTime * minuteScale;

        float simulatedMinutesPassed = deltaTime * minuteScale;
        UpdateStaminaDecay(simulatedMinutesPassed);

        // إعلان الوقت بدل الضائع للشوط الأول عند 44:30
        if (!_stoppage1stHalfAnnounced && MatchTimeMinutes >= 44.5f && !HalfTimeReached)
        {
            _stoppage1stHalfAnnounced = true;
            Publish(new MatchEvent(MatchEventType.StoppageTimeAnnounced, $"الحكم يعلن عن {_stoppageMinutes1st} دقائق كوقت بدل ضائع للشوط الأول", CurrentZone), ("stoppage.minutes", _stoppageMinutes1st));
        }

        if (!HalfTimeReached && MatchTimeMinutes >= 45.0f + _stoppageMinutes1st)
        {
            MatchTimeMinutes = 45.0f;
            TriggerHalfTime();
            return;
        }

        // إعلان الوقت بدل الضائع للشوط الثاني عند 89:30
        if (!_stoppage2ndHalfAnnounced && MatchTimeMinutes >= 89.5f && !IsExtraTime)
        {
            _stoppage2ndHalfAnnounced = true;
            Publish(new MatchEvent(MatchEventType.StoppageTimeAnnounced, $"الحكم يعلن عن {_stoppageMinutes2nd} دقائق كوقت بدل ضائع للشوط الثاني", CurrentZone), ("stoppage.minutes", _stoppageMinutes2nd));
        }

        if (!IsExtraTime)
        {
            if (MatchTimeMinutes >= 90.0f + _stoppageMinutes2nd)
            {
                if (!_fullTimeAnnounced)
                {
                    InterruptAttempt();
                    _fullTimeAnnounced = true;
                    string ftMsg = $"صافرة نهاية المباراة! النتيجة النهائية: {HomeTeam.Name} {HomeTeam.Score} - {AwayTeam.Score} {AwayTeam.Name}";
                    bool continues = HomeTeam.Score == AwayTeam.Score && (ExtraTimeAllowed || ShootoutAllowed);
                    if(!continues)State=MatchState.GameOver;
                    Publish(new MatchEvent(continues ? MatchEventType.RegulationTimeEnded : MatchEventType.FullTime, ftMsg, CurrentZone), ("match.finished", !continues));
                }

                if (HomeTeam.Score == AwayTeam.Score)
                {
                    if (ExtraTimeAllowed)
                    {
                        TriggerExtraTime();
                        return;
                    }
                    else if (ShootoutAllowed)
                    {
                        State = MatchState.PenaltyShootout;
                        Publish(new MatchEvent(MatchEventType.PenaltyShootoutStarted, "نهاية الوقت الأصلي بالتعادل! الاحتكام مباشرة لركلات الترجيح"));
                        return;
                    }
                    else
                    {
                        State = MatchState.GameOver;
                        return;
                    }
                }
                else
                {
                    State = MatchState.GameOver;
                    return;
                }
            }
        }
        else
        {
            if (!_extraTimeHalfReached && MatchTimeMinutes >= 105.0f)
            {
                InterruptAttempt();
                _extraTimeHalfReached = true;
                MatchTimeMinutes = 105;
                State = MatchState.HalfTime;
                SwapPlayingEnds();
                PlaceKickoff(HomeTeam);
                Publish(new MatchEvent(MatchEventType.ExtraTimeHalfTime, "نهاية الشوط الإضافي الأول"), ("match.half", 3));
                RunNativeAutomation();
                return;
            }
            if (MatchTimeMinutes >= 120.0f)
            {
                InterruptAttempt();
                if (HomeTeam.Score == AwayTeam.Score && ShootoutAllowed)
                {
                    State = MatchState.PenaltyShootout;
                    Publish(new MatchEvent(MatchEventType.PenaltyShootoutStarted, "نهاية الوقت الإضافي بالتعادل! الاحتكام لركلات الترجيح"));
                    return;
                }
                else
                {
                    State = MatchState.GameOver;
                    Publish(new MatchEvent(MatchEventType.FullTime, $"نهاية المباراة بعد الوقت الإضافي: {HomeTeam.Score} - {AwayTeam.Score}", CurrentZone), ("match.finished", true));
                    return;
                }
            }
        }

        // تحديث مؤقتات الاستخلاص والانزلاق وحماية الاستحواذ
        if (_tackleCooldownTimer > 0.0f)
        {
            _tackleCooldownTimer = Math.Max(0.0f, _tackleCooldownTimer - deltaTime);
        }

        if (_possessionGraceTimer > 0.0f)
        {
            _possessionGraceTimer = Math.Max(0.0f, _possessionGraceTimer - deltaTime);
        }

        if (_aiTackleCooldownTimer > 0.0f)
        {
            _aiTackleCooldownTimer = Math.Max(0.0f, _aiTackleCooldownTimer - deltaTime);
        }

        if (_slideDurationTimer > 0.0f)
        {
            _slideDurationTimer = Math.Max(0.0f, _slideDurationTimer - deltaTime);
            if (_slideDurationTimer == 0) _slidingPlayer = null;
        }

        if (UpdateAdvantage()) return;
        bool runUp=UpdateRunUp(deltaTime);
        if (Restart!=null && _runUpPlayer!=null)return;
        if (Restart!=null && UpdateRestart(deltaTime)) return;
        if (_slideDurationTimer > 0 && _slidingPlayer is { IsActiveOnPitch: true } slider && slider != ActiveUserPlayer)
            MovePlayer(slider, _slideImpulseVelocity, Math.Min(deltaTime, _slideDurationTimer));
        var positionsBefore=HomeTeam.Players.Concat(AwayTeam.Players).ToDictionary(p=>p,p=>p.Position2D);
        if(!runUp) ProcessUserInput(deltaTime);
        if (State != MatchState.InPlay || Restart != null || VideoReviewPending) return;
        UpdatePlayersAI(deltaTime);
        if (_slideDurationTimer > 0 && !_slideContactResolved && _slidingPlayer is { IsActiveOnPitch: true } sliding &&
            Ball.Owner is { } challenged && TeamOf(sliding) != TeamOf(challenged) && Vector2.Distance(sliding.Position2D, challenged.Position2D) <= .85f)
        {
            _slideContactResolved = true;
            ResolveChallenge(sliding, challenged, true, manual: true);
        }
        if (State == MatchState.GameOver || VideoReviewPending) return;
        foreach(var player in positionsBefore.Keys)
            player.Velocity=deltaTime>0?(player.Position2D-positionsBefore[player])/deltaTime:Vector2.Zero;
        if (Restart != null || ReviewPendingOffside()) return;
        Vector3 previousBall = Ball.Position;
        Ball.Update(deltaTime);
        CheckShotBlock(previousBall);
        UpdateClearanceOutcome();
        CheckPitchZone();
        CheckRefereeRules(previousBall);
        if (State == MatchState.InPlay && Restart == null && !VideoReviewPending) CollectLooseBall();

        // ترشيد استدعاء صوت الجماهير لمنع تقطيع الصوت
        _crowdUpdateTimer += deltaTime;
        if (_crowdUpdateTimer >= 0.5f)
        {
            _crowdUpdateTimer = 0.0f;
            UpdateCrowdProximity(0.5f);
        }
    }

    private void ProcessUserInput(float deltaTime)
    {
        bool hasPossession = Ball.Owner != null && HomeTeam.Players.Contains(Ball.Owner);
        var actions = _inputHandler.PollCurrentActions(hasPossession);

        if (actions.Contains(GameAction.Pause))
        {
            PauseMatch();
            return;
        }

        if (actions.Contains(GameAction.TacticalRadar)) ReportTacticalRadar();
        // Switch is a separate edge, never a modifier of movement or a tackle key.
        // Apply it before locomotion so this same tick moves the selected defender.
        if (!hasPossession && actions.Contains(GameAction.SwitchPlayer)) SwitchToClosestPlayer();
        Vector2 moveDir = MoveUser(actions, hasPossession, deltaTime);


        if (hasPossession && Ball.Owner == ActiveUserPlayer)
        {
            if (actions.Contains(GameAction.ShortPass))
            {
                ExecutePass(moveDir, isThroughBall: false, charge01: _inputHandler.ConsumeReleasedCharge01(GameAction.ShortPass));
            }
            else if (actions.Contains(GameAction.ThroughBall))
            {
                ExecuteThroughBall(moveDir);
            }
            else if (actions.Contains(GameAction.Cross))
            {
                ExecuteCross(charge01: _inputHandler.ConsumeReleasedCharge01(GameAction.Cross));
            }
            else if (actions.Contains(GameAction.Shoot))
            {
                ExecuteShot(_inputHandler.ConsumeReleasedCharge01(GameAction.Shoot));
            }
        }
        else
        {
            if (actions.Contains(GameAction.StandTackle))
            {
                ExecuteStandTackle();
            }
            else if (actions.Contains(GameAction.SlideTackle))
            {
                ExecuteSlideTackle();
            }
        }
    }

    private string _lastTouchMethod = "play";
    private void RecordBallTouch(Player kicker, bool deliberatelyControlled = true)
    {
        if (deliberatelyControlled && _attempts.Live != null) InterruptAttempt();
        ObserveOffsideTouch(kicker, deliberatelyControlled);
        _lastTouchMethod = "play";
        if (deliberatelyControlled)
        {
            _onTargetDeflectedShooter=null;_onTargetDeflectedAssister=null;
            TrackPossession(kicker);
            ResolveCommentaryPass(kicker);
        }
        if (LastKicker != kicker)
        {
            PreviousKicker = LastKicker;
            LastKicker = kicker;
        }
    }

    public void ExecuteThroughBall(Vector2 aimDir)
    {
        ExecutePass(aimDir, isThroughBall: true);
    }

    public void ExecutePass(Vector2 aimDir, bool isThroughBall = false, bool lofted = false, float charge01 = 0f)
    {
        if(State!=MatchState.InPlay || Ball.Owner!=ActiveUserPlayer)return;
        _kickoffProtectionTimer = 0.0f;
        Player? bestTarget = null;
        float bestScore = float.MinValue;
        Vector2 defaultForward = UserAttackingRight ? new Vector2(1.0f, 0.0f) : new Vector2(-1.0f, 0.0f);
        Vector2 passDir = aimDir.Length() > 0 ? aimDir : defaultForward;

        foreach (var teammate in HomeTeam.Players)
        {
            if (teammate == ActiveUserPlayer || teammate.IsSentOff || teammate.IsOffPitch) continue;

            Vector2 toTeammate = teammate.Position2D - ActiveUserPlayer.Position2D;
            float dist = toTeammate.Length();
            if (dist < 3.0f || dist > 45.0f) continue;

            float dot = Vector2.Dot(Vector2.Normalize(toTeammate), passDir);
            float score = dot * 100.0f - (dist * 0.5f) + TacticalPassScore(HomeTeam, ActiveUserPlayer, teammate) * .25f;

            bool isAdvancing = UserAttackingRight 
                ? (teammate.Position2D.X > ActiveUserPlayer.Position2D.X)
                : (teammate.Position2D.X < ActiveUserPlayer.Position2D.X);

            if (isThroughBall && isAdvancing)
            {
                score += 40.0f;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = teammate;
            }
        }

        if (bestTarget == null)
        {
            bestTarget = HomeTeam.Players.FirstOrDefault(p => p != ActiveUserPlayer && !p.IsSentOff && !p.IsOffPitch && p.Role != PlayerRole.Goalkeeper)
                ?? HomeTeam.Players.FirstOrDefault(p => p != ActiveUserPlayer && !p.IsSentOff && !p.IsOffPitch)
                ?? HomeTeam.Players[0];
        }

        RecordBallTouch(ActiveUserPlayer);

        Vector2 targetPos;
        if (isThroughBall)
        {
            Vector2 leadDir = bestTarget.FacingDirection.Length() > 0.1f
                ? Vector2.Normalize(bestTarget.FacingDirection)
                : defaultForward;
            float leadDist = Math.Clamp((bestTarget.Position2D - ActiveUserPlayer.Position2D).Length() * 0.35f, 4.0f, 9.0f);
            targetPos = bestTarget.Position2D + (leadDir * leadDist);
        }
        else
        {
            targetPos = bestTarget.Position2D;
        }

        // إبقاء الهدف داخل حدود الملعب
        targetPos.X = Math.Clamp(targetPos.X, 2.0f, Pitch.Length - 2.0f);
        targetPos.Y = Math.Clamp(targetPos.Y, 2.0f, Pitch.Width - 2.0f);

        Vector2 kickDirection = targetPos - ActiveUserPlayer.Position2D;
        float passDist = kickDirection.Length();
        if (passDist > 0.001f)
        {
            kickDirection = Vector2.Normalize(kickDirection);
        }
        else
        {
            kickDirection = defaultForward;
        }

        float passSpeed = (isThroughBall ? 22.0f : 18.0f) * (.90f + HomeTeam.Tactics.Tempo * .002f);
        // تمريرة مشحونة بالضغط المطوّل: نفس الهدف بسرعة أعلى تصل إلى 150%
        if (!isThroughBall && charge01 > 0f) passSpeed *= 1f + .5f * Math.Clamp(charge01, 0f, 1f);

        if (CheckOffsideTrap(HomeTeam, AwayTeam, bestTarget, targetPos))
        {
            return;
        }
        
        Ball.Kick(kickDirection, passSpeed, verticalSpeed: lofted ? Math.Clamp(passDist/passSpeed*4.905f,2.5f,8f) : .15f, ActiveUserPlayer);

        // تسجيل حالة التمريرة النشطة مع إبقاء تحكم المستخدم مؤقتاً حتى وصول الكرة للمستلم
        ActivePassTransit = new PassTransitInfo
        {
            Sender = ActiveUserPlayer,
            IntendedReceiver = bestTarget,
            TargetPosition = targetPos,
            PassType = isThroughBall ? PassType.ThroughBall : PassType.ShortPass,
            PassSpeed = passSpeed,
            EstimatedDuration = passSpeed > 0 ? passDist / passSpeed : 1.0f,
            ElapsedTime = 0.0f,
            IsActive = true
        };

        BeginCommentaryPass(ActiveUserPlayer, bestTarget, targetPos, isThroughBall ? "through" : "short", lofted ? "high" : "ground");
        string passDesc = isThroughBall 
            ? $"بينية إلى {bestTarget.Name} في {CurrentZone.ToArabicAnnouncement()}" 
            : $"تمريرة إلى {bestTarget.Name} في {CurrentZone.ToArabicAnnouncement()}";
        Publish(new MatchEvent(MatchEventType.BallKicked, passDesc, CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: ActiveUserPlayer), ("action.kind", "pass"), ("pass.type", isThroughBall ? "through" : "short"), ("pass.distance", passDist), ("pass.targetPlayer.id", bestTarget.Id));
    }

    public void AdjustAimingAngle(float deltaY)
    {
        float forwardX = UserAttackingRight ? 1.0f : -1.0f;
        if (ActiveUserPlayer.FacingDirection.X != 0)
        {
            forwardX = ActiveUserPlayer.FacingDirection.X > 0 ? 1.0f : -1.0f;
        }

        float targetY = Math.Clamp(ActiveUserPlayer.FacingDirection.Y + deltaY, -0.90f, 0.90f);
        float targetX = forwardX * MathF.Sqrt(Math.Max(0.001f, 1.0f - (targetY * targetY)));

        Vector2 normalized = Vector2.Normalize(new Vector2(targetX, targetY));
        float finalY = Math.Clamp(normalized.Y, -0.90f, 0.90f);
        float finalX = forwardX * MathF.Sqrt(Math.Max(0.001f, 1.0f - (finalY * finalY)));

        ActiveUserPlayer.FacingDirection = new Vector2(finalX, finalY);
    }

    public void ExecuteCross(CrossStyle style = CrossStyle.Center, float charge01 = 0f)
    {
        if(State!=MatchState.InPlay || Ball.Owner!=ActiveUserPlayer)return;
        _kickoffProtectionTimer = 0.0f;
        RecordBallTouch(ActiveUserPlayer);
        SnapshotOffsideAtKick(ActiveUserPlayer);
        Vector2 boxCenter = UserAttackingRight 
            ? new(Pitch.Length - 10.0f, Pitch.Width / 2.0f)
            : new(10.0f, Pitch.Width / 2.0f);

        // توجيه العرضية وفقاً لزاوية الأسهم للأعلى أو للأسفل (Up/Down Arrow Aiming)
        float crossOffset = ActiveUserPlayer.FacingDirection.Y * 16.0f;
        boxCenter.Y = Math.Clamp(boxCenter.Y + crossOffset, Pitch.GoalYMin - 6.0f, Pitch.GoalYMax + 6.0f);

        Vector2 crossDir = boxCenter - ActiveUserPlayer.Position2D;
        if (style is CrossStyle.FarPost or CrossStyle.GroundFarPost)
            boxCenter.Y = ActiveUserPlayer.Position2D.Y < Pitch.Width / 2 ? Pitch.GoalYMax - .5f : Pitch.GoalYMin + .5f;
        crossDir = boxCenter - ActiveUserPlayer.Position2D;
        crossDir = crossDir.LengthSquared() > .000001f ? Vector2.Normalize(crossDir) : new Vector2(UserAttackingRight ? 1 : -1, 0);
        bool ground = style is CrossStyle.Ground or CrossStyle.GroundFarPost;
        // عرضية مشحونة بالضغط المطوّل: كلما طال الضغط ارتفعت العرضية حتى تسقيطة كاملة
        float chargedVertical = 6.5f + 4.5f * Math.Clamp(charge01, 0f, 1f);
        Ball.Kick(crossDir, horizontalSpeed: ground ? 23 : 19, verticalSpeed: ground ? .1f : chargedVertical, ActiveUserPlayer);
        BeginCommentaryPass(ActiveUserPlayer, null, boxCenter, "cross", ground ? "ground" : "high", true);
        if(_attackOrigin!="corner")_attackOrigin = "cross";
        Publish(new MatchEvent(MatchEventType.BallKicked, $"عرضية عالية من {ActiveUserPlayer.Name} داخل منطقة الجزاء!", CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: ActiveUserPlayer), ("action.kind", "cross"), ("cross.height", ground ? "ground" : "high"), ("cross.target", style is CrossStyle.FarPost or CrossStyle.GroundFarPost ? "farPost" : "center"), ("cross.targetY", boxCenter.Y));
    }

    public void ExecuteShot(float charge01 = 0f) => TryExecuteTechnique(ShotTechnique.Driven, charge01);

    public static string GetArabicPositionName(SpecificPosition pos) => pos switch
    {
        SpecificPosition.GK => "حارس مرمى",
        SpecificPosition.CB => "قلب دفاع",
        SpecificPosition.LB => "ظهير أيسر",
        SpecificPosition.RB => "ظهير أيمن",
        SpecificPosition.LWB => "ظهير جناح أيسر",
        SpecificPosition.RWB => "ظهير جناح أيمن",
        SpecificPosition.CDM => "وسط دفاعي",
        SpecificPosition.CM => "وسط محور",
        SpecificPosition.CAM => "صانع ألعاب",
        SpecificPosition.LM => "جناح أيسر",
        SpecificPosition.RM => "جناح أيمن",
        SpecificPosition.LW => "جناح هجومي أيسر",
        SpecificPosition.RW => "جناح هجومي أيمن",
        SpecificPosition.ST => "رأس حربة",
        SpecificPosition.CF => "مهاجم متأخر",
        _ => pos.ToString()
    };

    public void SwitchToClosestPlayer()
    {
        if (_switchCooldownSeconds > 0) return;
        _switchCooldownSeconds = .2f;

        Player? closest = null;
        float minDist = float.MaxValue;
        Vector2 ball2D = new(Ball.Position.X, Ball.Position.Y);

        // F9: استبعاد اللاعب النشط الحالي وحارس المرمى واللاعبين المطرودين أو الخارجين للإصابة
        var candidates = HomeTeam.Players
            .Where(p => p != ActiveUserPlayer && p.Role != PlayerRole.Goalkeeper && !p.IsSentOff && !p.IsOffPitch)
            .ToList();

        if (candidates.Count == 0)
        {
            candidates = HomeTeam.Players.Where(p => p != ActiveUserPlayer && !p.IsSentOff && !p.IsOffPitch).ToList();
        }

        foreach (var p in candidates)
        {
            float d = Vector2.Distance(p.Position2D, ball2D);
            if (d < minDist)
            {
                minDist = d;
                closest = p;
            }
        }

        if (closest != null)
        {
            ActiveUserPlayer = closest;
            // إعلان عالي الإشارة وموجز: اسم اللاعب ومركزه فقط بدون حشو تعليمي
            Publish(new MatchEvent(MatchEventType.TacticalRadarInfo, $"{closest.Name} - {GetArabicPositionName(closest.CurrentAssignedPosition)}"), ("radar.reason", "playerSwitch"));
        }
    }

    /// <summary>
    /// تبديل التحكم إلى أقرب مدافع
    /// </summary>
    public void SwitchToNearestDefender() => SwitchToClosestPlayer();

    public void ExecuteStandTackle()
    {
        if (Ball.Owner != null && HomeTeam.Players.Contains(Ball.Owner) || _tackleCooldownTimer > 0.0f) return;
        _tackleCooldownTimer = 0.4f;

        PerformTackleAttempt(isSlideTackle: false, reach: 1.8f);
    }

    public void ExecuteSlideTackle()
    {
        if (Ball.Owner != null && HomeTeam.Players.Contains(Ball.Owner) || _tackleCooldownTimer > 0.0f) return;
        _tackleCooldownTimer = 0.9f;
        _slideDurationTimer = 0.3f;
        _slidingPlayer = ActiveUserPlayer;
        _slideContactResolved = false;

        Vector2 facing = ActiveUserPlayer.FacingDirection.Length() > 0.1f 
            ? Vector2.Normalize(ActiveUserPlayer.FacingDirection) 
            : new Vector2(1.0f, 0.0f);
        _slideImpulseVelocity = facing * Math.Max(4, ContactMotion(ActiveUserPlayer).Length()); // Preserve the real approach speed; no invented impact boost.

        PerformTackleAttempt(isSlideTackle: true, reach: 2.8f);
    }

    public void AttemptTackle()
    {
        ExecuteStandTackle();
    }

    private void PerformTackleAttempt(bool isSlideTackle, float reach)
    {
        var actor = ActiveUserPlayer;
        if (Ball.Owner is { } carrier && AwayTeam.Players.Contains(carrier))
        {
            ResolveChallenge(actor, carrier, isSlideTackle, manual: true);
            return;
        }
        // A defensive key on a loose ball uses the same eligibility as ordinary control.
        // It cannot swallow an airborne shot or acquire a distant ball.
        if (Ball.Owner == null && TryControlIncomingBall(actor))
        {
            GrantPossessionGrace(1.4f);
            Publish(new MatchEvent(MatchEventType.UserTackleSuccessful, $"استحواذ: {actor.Name}", CurrentZone,
                spatialPan: CalculateBallPan(), subjectPlayer: actor), ("tackle.sliding", isSlideTackle));
        }
        else Publish(new MatchEvent(MatchEventType.TackleAttempted, "محاولة استخلاص", CurrentZone,
            spatialPan: CalculateBallPan(), subjectPlayer: actor), ("tackle.won", false), ("tackle.sliding", isSlideTackle));
    }

    /// <summary>
    /// نتيجة صراع استخلاص الكرة التكتيكي والبدني
    /// </summary>
    public record struct DuelResult(
        bool TacklerWon,
        bool IsFoul,
        bool IsSevereInjuryRisk,
        float TacklerWinProbability,
        float TacklerSkill,
        float CarrierSkill,
        float TacticalModifier,
        string TacticalReason
    )
    {
        public bool FromBehind { get; init; }
        public bool SlidingCommitment { get; init; }
        public FoulIncident? Incident { get; init; }
    }

    /// <summary>
    /// نموذج صراع استخلاص الكرة التكتيكي والبدني (Dynamic Tactical & Attribute Duel Model)
    /// يقضي نهائياً على العشوائية الساذجة والنسب الثابتة، ويمنح الأفضلية الكاملة لمهارات اللاعبين
    /// وتفوقهم البدني، وتكتيكات التموضع الميداني، وحجز الكرة بالجسم، وضغط الزيادة العددية، ومستويات شراسة العرقلة.
    /// </summary>
    public static DuelResult EvaluateTackleDuel(
        Player tackler,
        Player carrier,
        Vector2 ballPos2D,
        Team tacklerTeam,
        Team carrierTeam,
        DifficultyLevel diff,
        int? forcedRandomRoll = null,
        int? forcedFoulRoll = null,
        bool counterTransitionActive = false,
        Vector2? tacklerContactMotion = null,
        Vector2? carrierContactMotion = null)
    {
        return Play.ChallengeResolver.Evaluate(tackler, carrier, ballPos2D, tacklerTeam, carrierTeam, diff,
            tacklerContactMotion ?? tackler.Velocity, carrierContactMotion ?? carrier.Velocity,
            sliding: tacklerTeam.Tactics.Tackling is TacklingAggression.Aggressive or TacklingAggression.Reckless,
            transition: counterTransitionActive, contestRoll: forcedRandomRoll, foulRoll: forcedFoulRoll);
    }

    /// <summary>
    /// تحديث استنزاف اللياقة البدنية للاعبين وفق منحنى الأعمار وشرايط الضغط التكتيكية
    /// - تحت 19 سنة: رئة لا تتعب (استنزاف 1.0x حتى عند الضغط الخانق 100%)
    /// - 19 إلى 24 سنة: طاقة شبابية (ينهج في آخر 10 دقائق فقط)
    /// - 25 إلى 29 سنة: قمة النضج (ينفد وقوده عند الدقيقة 60 عند الضغط العالي)
    /// - فوق 29 سنة (30+): خبرة (يجيب جاز بنهاية الشوط الأول عند الضغط العالي)
    /// </summary>
    public void UpdateStaminaDecay(float simulatedMinutesPassed)
    {
        if (simulatedMinutesPassed <= 0.0f) return;

        if (!float.IsFinite(simulatedMinutesPassed)) return;
        void DecayTeam(Team team)
        {
            foreach (var player in team.Players.Where(p=>p.IsActiveOnPitch))
                player.CurrentStamina=Math.Max(0,player.CurrentStamina-simulatedMinutesPassed*TacticalStaminaRate(team.Tactics,player));
        }

        DecayTeam(HomeTeam);
        DecayTeam(AwayTeam);
    }

    /// <summary>
    /// احتمال تصدي الحارس لتسديدة: مواجهة واحدة محسومة بلفة واحدة تعتمد على
    /// مهارة التسديد للمهاجم وقدرة الحارس، وليست لفة متكررة بكل إطار.
    /// </summary>
    public static float ComputeSaveProbability(Player shooter, Player goalkeeper)
    {
        float probability = 0.50f + (goalkeeper.Defending - shooter.Shooting) * 0.006f;
        return Math.Clamp(probability, 0.20f, 0.85f);
    }

    /// <summary>
    /// حسم مواجهة حارس مع تسديدة بلفة واحدة؛ forcedRoll للاختبارات الحتمية.
    /// </summary>
    public static bool ResolveGoalkeeperSaveAttempt(Player shooter, Player goalkeeper, double forcedRoll = -1.0)
    {
        double roll = forcedRoll >= 0.0 ? forcedRoll : SimulationRandom.Next();
        return roll < ComputeSaveProbability(shooter, goalkeeper);
    }

    /// <summary>
    /// وجهة تسديدة الكمبيوتر: مقص قريب أو بعيد (بتفضيل البعيد) مع انتشار مهاري،
    /// بدلاً من التصويب الدائم إلى قلب المرمى حيث يقف الحارس فيلتقط كل شيء.
    /// </summary>
    public static Vector2 ComputeCpuShotTarget(Vector2 shooterPos, bool attackingRight, double postChoiceRoll, double spreadRoll, int shooting = 75, float stamina = 100)
    {
        float goalX = attackingRight ? Pitch.Length : 0.0f;
        float goalCenterY = (Pitch.GoalYMin + Pitch.GoalYMax) / 2.0f;
        bool shooterAboveCenter = shooterPos.Y < goalCenterY;
        bool aimFarPost = postChoiceRoll < 0.65;
        bool aimHighSide = aimFarPost ? !shooterAboveCenter : shooterAboveCenter;
        float targetY = aimHighSide ? Pitch.GoalYMin + 1.3f : Pitch.GoalYMax - 1.3f;
        float distance=Vector2.Distance(shooterPos,new Vector2(goalX,goalCenterY));
        float spreadWidth=2.2f+Math.Max(0,100-shooting)*.03f+Math.Max(0,distance-18)*.07f+Math.Max(0,40-stamina)*.03f;
        float spread = (float)(spreadRoll * 2.0 - 1.0) * spreadWidth;
        targetY = Math.Clamp(targetY + spread, Pitch.GoalYMin - 2.5f, Pitch.GoalYMax + 2.5f);
        return new Vector2(goalX, targetY);
    }

    /// <summary>
    /// حارس المرمى المدافع عن مرمى التسديدة الجارية (بحسب فريق المسدد، لا اتجاه هجوم المستخدم).
    /// </summary>
    private Player? GetDefendingGoalkeeperForShot()
    {
        Team defendingTeam = TeamOf(_shotShooter) == AwayTeam ? HomeTeam : AwayTeam;
        return defendingTeam.Players.FirstOrDefault(p => p.Role == PlayerRole.Goalkeeper && !p.IsSentOff && !p.IsOffPitch)
            ?? defendingTeam.Players.FirstOrDefault();
    }

    /// <summary>
    /// فحص مصيدة التسلل والتسلل الميداني وفق القانون 11
    /// عند تفعيل مصيدة التسلل:
    /// - إذا نجحت: يرفع الحكم الراية ويعلن التسلل وتمنح ركلة حرة غير مباشرة
    /// - إذا فشلت (تأخر مدافع أو ضعف تركيزه): ينكسر التسلل ويصبح المهاجم في انفراد صريح تماماً
    /// </summary>
    public bool CheckOffsideTrap(Team attackingTeam, Team defendingTeam, Player receiver, Vector2 targetPos)
    {
        // Register actual kick-time positions. A planned target position is not an offence.
        var kicker=Ball.Owner??LastKicker;
        if(kicker!=null && TeamOf(kicker)==attackingTeam)SnapshotOffsideAtKick(kicker);
        return false;
    }

    public bool CheckDogso(Player tackler, Player carrier)
    {
        Team tacklerTeam = HomeTeam.Players.Contains(tackler) ? HomeTeam : AwayTeam;
        bool tacklerDefendingRight = (tacklerTeam.Side == TeamSide.Home) ? !UserAttackingRight : UserAttackingRight;
        Vector2 defendingGoal = tacklerDefendingRight ? Pitch.RightGoalCenter : Pitch.LeftGoalCenter;

        float distToGoal = Vector2.Distance(carrier.Position2D, defendingGoal);
        // فرصة محققة للتسجيل: المسافة للمرمى أقل من 45 متراً
        if (distToGoal > 45.0f) return false;

        // فحص ما إذا كان هناك أي مدافع ميداني آخر بين حامل الكرة والمرمى
        int defendersBetween = tacklerTeam.Players.Count(p =>
            p != tackler &&
            p.Role != PlayerRole.Goalkeeper &&
            !p.IsSentOff &&
            !p.IsOffPitch &&
            (tacklerDefendingRight ? p.Position2D.X > carrier.Position2D.X : p.Position2D.X < carrier.Position2D.X));

        return defendersBetween == 0;
    }

    private Dictionary<Player,Vector2> _contactMotion = new();
    private Dictionary<Player,Vector2> _previousContactMotion = new();
    private Vector2 ContactMotion(Player player) => _contactMotion.TryGetValue(player,out var motion) ? motion
        : _previousContactMotion.GetValueOrDefault(player);
    private FoulIncident CaptureLiveFoulIncident(Player tackler,Player carrier,Vector2 ball,float reach=1.8f,Vector2? lunge=null)
        => CaptureFoulIncident(tackler,carrier,ball,reach,lunge,ContactMotion(tackler),ContactMotion(carrier));

    public enum FoulContactSeverity { Careless, Reckless, EndangersSafety }

    // Physical snapshot, not a tactical-aggression label. Keep it through a deferred VAR review.
    public sealed record FoulIncident(FoulContactSeverity Severity, bool BallChallenge, float ClosingSpeed);

    public static FoulIncident CaptureFoulIncident(Player tackler, Player carrier, Vector2 ballPosition,
        float challengeReach = 1.8f, Vector2? lungeVelocity = null, Vector2? tacklerMotion = null, Vector2? carrierMotion = null)
    {
        var toVictim = carrier.Position2D - tackler.Position2D;
        var toBall = ballPosition - tackler.Position2D;
        if (Vector2.Distance(ballPosition, carrier.Position2D) < .05f)
        {
            var facing = carrier.FacingDirection.LengthSquared() > .01f
                ? Vector2.Normalize(carrier.FacingDirection) : Vector2.UnitX;
            toBall = carrier.Position2D + facing * .35f - tackler.Position2D;
        }
        // A lunge replaces movement in this engine. Never add the same impulse twice.
        var runningVelocity = tacklerMotion ?? tackler.Velocity;
        var contactVelocity = lungeVelocity is { } lunge && lunge.LengthSquared() > runningVelocity.LengthSquared()
            ? lunge : runningVelocity;
        var approach = contactVelocity - (carrierMotion ?? carrier.Velocity);
        float closing = toVictim.LengthSquared() > .0025f
            ? Math.Max(0, Vector2.Dot(approach, Vector2.Normalize(toVictim))) : approach.Length();
        // Game parameters, not IFAB numerical cutoffs. Scale against this player's normal
        // locomotion so a faster player is not punished merely for a higher Speed rating.
        // A closing impact above normal running is reckless. A fast committed approach plus
        // a much larger relative impact endangers safety (e.g. sprinting into oncoming traffic).
        // Victim motion alone cannot make a stationary tackler use excessive force.
        float tacklerApproach = toVictim.LengthSquared() > .0025f
            ? Vector2.Dot(contactVelocity, Vector2.Normalize(toVictim)) : contactVelocity.Length();
        float normalRun = Math.Max(4f, tackler.Speed / 7.5f);
        var severity = tacklerApproach < 1f ? FoulContactSeverity.Careless
            : closing >= normalRun * 1.9f && tacklerApproach >= normalRun * 1.5f ? FoulContactSeverity.EndangersSafety
            : closing >= normalRun * 1.25f ? FoulContactSeverity.Reckless : FoulContactSeverity.Careless;
        float ballDistance = toBall.Length();
        var ballDir = ballDistance > .01f ? toBall / ballDistance : Vector2.Zero;
        bool facingBall = ballDistance < .2f || (tackler.FacingDirection.LengthSquared() > .01f
            && Vector2.Dot(Vector2.Normalize(tackler.FacingDirection), ballDir) >= .5f);
        float bodyAlongPath = Vector2.Dot(toVictim, ballDir);
        float bodyPathDistance = (toVictim - ballDir * bodyAlongPath).Length();
        bool bodyBlocksBall = bodyAlongPath > 0 && bodyAlongPath < ballDistance && bodyPathDistance < .3f;
        bool ballChallenge = ballDistance <= challengeReach && facingBall && !bodyBlocksBall;
        return new FoulIncident(severity, ballChallenge, closing);
    }

    public void HandleFoulSanction(Player tackler, Player carrier, bool isSlideTackle, bool isFromBehind,
        FoulIncident? incident = null)
    {
        if (State == MatchState.GameOver) return;
        incident ??= CaptureLiveFoulIncident(tackler, carrier, Ball.Position2D);
        bool isDogso = CheckDogso(tackler, carrier);
        Team? carrierTeam = TeamOf(carrier);
        bool right = carrierTeam == HomeTeam ? UserAttackingRight : !UserAttackingRight;
        var spot = carrier.Position2D;
        bool penalty = (right ? spot.X >= Pitch.Length - Pitch.PenaltyBoxDepth : spot.X <= Pitch.PenaltyBoxDepth)
            && spot.Y >= Pitch.PenaltyYMin && spot.Y <= Pitch.PenaltyYMax;
        ApplyFoulIncident(tackler, carrier, incident, isDogso, penalty, spot);
    }

    private void ApplyFoulIncident(Player tackler, Player carrier, FoulIncident incident, bool isDogso,
        bool penalty, Vector2 spot)
    {
        bool seriousFoulPlay = incident.Severity == FoulContactSeverity.EndangersSafety;
        bool dogsoRed = isDogso && !(penalty && incident.BallChallenge);
        if ((seriousFoulPlay || dogsoRed) && BeginVideoReview(VideoReviewCategory.DirectRedCard,
            () => ApplyFoulIncident(tackler, carrier, incident, isDogso, penalty, spot),
            T($"قرار الفار: تأكيد المخالفة والطرد المباشر للاعب {tackler.Name}.",
                $"VAR decision: foul and direct red confirmed for {tackler.Name}."), "redCard")) return;
        _foulVictim = carrier;
        if (seriousFoulPlay) Count("stats.severeFouls");
        if (seriousFoulPlay)
            IssueCard(tackler, isDirectRed: true, reason: "لعب عنيف: تدخل يعرّض سلامة الخصم للخطر");
        else if (dogsoRed)
            IssueCard(tackler, isDirectRed: true, reason: "منع فرصة هدف محقق (DOGSO)");
        else if (isDogso || incident.Severity == FoulContactSeverity.Reckless)
            ReviewCardDecision(tackler,tackler,directRed:false,offenceConfirmed:true,clearErrorEvidence:false);
        // Careless contact carries no automatic card. Behind, slide, tactics and counter-transition
        // are not disciplinary categories. SPA/persistence need their own incident evidence;
        // neither a counter flag nor an arbitrary third-foul rule supplies that evidence.
        if (State == MatchState.GameOver) { _foulVictim = null; return; }
        if (!TryPlayAdvantage(carrier, incident.Severity != FoulContactSeverity.Careless, isDogso))
            AwardFoulRestart(carrier, spot);
        if (carrier.IsActiveOnPitch && incident.Severity != FoulContactSeverity.Careless)
            ApplyInjury(carrier, seriousFoulPlay ? PlayerInjuryStatus.SevereInjury : PlayerInjuryStatus.MinorKnock);
        _foulVictim = null;
    }

    public void IssueCard(Player player, bool isDirectRed, string reason = "")
    {
        Team? playerTeam = TeamOf(player);
        if (playerTeam == null || !player.IsActiveOnPitch) return;

        if(isDirectRed && BeginVideoReview(VideoReviewCategory.DirectRedCard,()=>IssueCard(player,true,reason),
            T($"قرار الفار: تأكيد البطاقة الحمراء للاعب {player.Name}.",$"VAR decision: red card confirmed for {player.Name}."),"redCard"))return;
        if (isDirectRed)
        {
            DismissPlayer(player, playerTeam);

            int remainingPlayers = playerTeam.Players.Count(p => !p.IsSentOff && !p.IsOffPitch);
            string desc = string.IsNullOrEmpty(reason)
                ? $"بطاقة حمراء وطرد مباشر لـ {player.Name}! {playerTeam.Name} يكمل بـ {remainingPlayers} لاعبين."
                : $"بطاقة حمراء وطرد مباشر لـ {player.Name} ({reason})! {playerTeam.Name} يكمل بـ {remainingPlayers} لاعبين.";
            Publish(new MatchEvent(MatchEventType.RedCard, desc, CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: player), ("card.kind", "directRed"));
        }
        else
        {
            player.YellowCards++;
            if (player.YellowCards >= 2)
            {
                DismissPlayer(player, playerTeam);

                int remainingPlayers = playerTeam.Players.Count(p => !p.IsSentOff && !p.IsOffPitch);
                string desc = $"إنذار ثانٍ وبطاقة حمراء وطرد لـ {player.Name}! {playerTeam.Name} يكمل بـ {remainingPlayers} لاعبين.";
                Publish(new MatchEvent(MatchEventType.RedCard, desc, CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: player), ("card.kind", "secondYellow"));
            }
            else
            {
                string desc = string.IsNullOrEmpty(reason)
                    ? $"بطاقة صفراء وإنذار أول للاعب {player.Name}."
                    : $"بطاقة صفراء وإنذار أول للاعب {player.Name} ({reason}).";
                Publish(new MatchEvent(MatchEventType.YellowCard, desc, CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: player), ("card.kind", "yellow"));
            }
        }
        StopIfTooFewPlayers();
    }

    private void DismissPlayer(Player player, Team playerTeam) => RemovePlayerFromPlay(player, playerTeam, sentOff: true);

    private void RemovePlayerFromPlay(Player player, Team playerTeam, bool sentOff)
    {
        // 1. فك ارتباط الكرة فوراً إذا كان اللاعب المستبعد هو حامل الكرة لمنع انتقالها خارج الملعب
        if (player.HasBall || Ball.Owner == player)
        {
            player.HasBall = false;
            Ball.Owner = null;
            Ball.State = BallState.Free;
            Ball.Velocity = Vector3.Zero;
            Ball.Position = new Vector3(
                Math.Clamp(player.Position2D.X, 1.0f, Pitch.Length - 1.0f),
                Math.Clamp(player.Position2D.Y, 1.0f, Pitch.Width - 1.0f),
                0.0f);
        }

        // 2. إلغاء أي تمريرة جارية إذا كان اللاعب المطرود طرفاً فيها
        if (ActivePassTransit != null && (ActivePassTransit.Sender == player || ActivePassTransit.IntendedReceiver == player))
        {
            ActivePassTransit.IsActive = false;
            ActivePassTransit = null;
        }

        // 3. طرد اللاعب ونقله خارج أرضية الملعب وتصفير سرعته
        player.IsSentOff |= sentOff;
        player.IsOffPitch = true;
        player.Position2D = new Vector2(-100f, -100f);
        player.HomePosition = new Vector2(-100f, -100f);
        player.Velocity = Vector2.Zero;

        // 4. إعادة تعيين الأدوار التكتيكية إذا كان المطرود يحمل أياً منها
        if (playerTeam.Captain == player) playerTeam.Captain = playerTeam.Players.FirstOrDefault(p => !p.IsSentOff && !p.IsOffPitch && p != player);
        if (playerTeam.PenaltyTaker == player) playerTeam.PenaltyTaker = playerTeam.Players.FirstOrDefault(p => !p.IsSentOff && !p.IsOffPitch && p != player);
        if (playerTeam.FreeKickTaker == player) playerTeam.FreeKickTaker = playerTeam.Players.FirstOrDefault(p => !p.IsSentOff && !p.IsOffPitch && p != player);
        if (playerTeam.CornerTaker == player) playerTeam.CornerTaker = playerTeam.Players.FirstOrDefault(p => !p.IsSentOff && !p.IsOffPitch && p != player);

        // 5. نقل تحكم المستخدم إذا كان اللاعب المطرود تحت التحكم حالياً مع تجاوز مانع التكرار (Debounce)
        if (ActiveUserPlayer == player)
        {
            _switchCooldownSeconds = 0;
            SwitchToClosestPlayer();
            if (ActiveUserPlayer == player)
            {
                ActiveUserPlayer = playerTeam.Players.FirstOrDefault(p => !p.IsSentOff && !p.IsOffPitch && p.Role != PlayerRole.Goalkeeper)
                    ?? playerTeam.Players.FirstOrDefault(p => !p.IsSentOff && !p.IsOffPitch)
                    ?? playerTeam.Players[0];
            }
        }
        if (Restart is { } pendingRestart && pendingRestart.Taker == player)
            ReassignUnavailableRestartTaker(pendingRestart);
    }

    public void ApplyInjury(Player player, PlayerInjuryStatus status)
    {
        if (State == MatchState.GameOver) return;
        if ((!HomeTeam.Players.Contains(player) && !AwayTeam.Players.Contains(player)) || !player.IsActiveOnPitch ||
            status == PlayerInjuryStatus.None || player.InjuryStatus == status) return;
        player.InjuryStatus = status;
        Team playerTeam = HomeTeam.Players.Contains(player) ? HomeTeam : AwayTeam;

        if (status == PlayerInjuryStatus.MinorKnock)
        {
            string desc = $"تعرض {player.Name} لكدمة خفيفة! (-20% سرعة ولياقة بدنية).";
            Publish(new MatchEvent(MatchEventType.PlayerInjured, desc, CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: player), ("injury.severity", status == PlayerInjuryStatus.MinorKnock ? "minor" : "severe"));
        }
        else if (status == PlayerInjuryStatus.SevereInjury)
        {
            string desc = $"إصابة بالغة للاعب {player.Name}! تستدعي الخروج الإجباري.";
            Publish(new MatchEvent(MatchEventType.PlayerInjured, desc, CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: player), ("injury.severity", status == PlayerInjuryStatus.MinorKnock ? "minor" : "severe"));

            if (playerTeam == HomeTeam)
            {
                BeginSubstitutionStoppage();
                _pendingInjuryChoices.Enqueue(player);
                if (!CanMakeSubstitution(playerTeam) || !playerTeam.Bench.Any(playerTeam.CanEnterAsSubstitute))
                    LeavePitchInjured(player, playerTeam);
                // The user, not a strongest-compatible-player heuristic, owns the replacement choice.
            }
            else if (!ExecuteForcedSubstitution(playerTeam, player))
            {
                LeavePitchInjured(player, playerTeam);
            }
        }
    }

    public bool ExecuteForcedSubstitution(Team team, Player injuredPlayer)
    {
        if (team == HomeTeam) return false; // Never silently choose for the human team.
        if (team.CanMakeSubstitution(State == MatchState.HalfTime) && team.Bench.Count > 0)
        {
            var replacement = AccessibleFootball2026.Core.Tactics.TacticalAiAdvisor.FindBestBenchReplacement(team, injuredPlayer);
            return replacement != null && MakeSubstitution(team, injuredPlayer, replacement);
        }
        return false;
    }

    private float CalculateBallPan()
    {
        return Math.Clamp((Ball.Position.Y - (Pitch.Width / 2.0f)) / (Pitch.Width / 2.0f), -1.0f, 1.0f);
    }

    private void UpdateCrowdProximity(float deltaTime)
    {
        bool attacksRight = Ball.Owner != null && AwayTeam.Players.Contains(Ball.Owner) ? !UserAttackingRight : UserAttackingRight;
        float distToGoal = attacksRight ? Pitch.Length - Ball.Position2D.X : Ball.Position2D.X;
        
        float targetVolume;
        if (distToGoal > 55.0f)
        {
            // نصف الملعب الدفاعي: مدرجات طبيعية هادئة
            targetVolume = 350.0f;
        }
        else if (distToGoal > 32.0f)
        {
            // دائرة منتصف الملعب وبناء اللعب: صوت متوازن
            targetVolume = 500.0f;
        }
        else if (distToGoal > 18.0f)
        {
            // الثلث الهجومي: تصاعد صخب وهتاف الجماهير مع اقتراب الهجمة
            targetVolume = 750.0f;
        }
        else
        {
            // داخل منطقة الجزاء أو أمام المرمى مباشرة: قمة الترقب والحماس
            targetVolume = 950.0f;
        }

        _currentCrowdVolume += (targetVolume - _currentCrowdVolume) * deltaTime * 2.5f;
        _currentCrowdVolume = Math.Clamp(_currentCrowdVolume, 250.0f, 1000.0f);

        OnCrowdVolumeChanged?.Invoke((int)_currentCrowdVolume);
    }

    private void UpdatePassTransit(float deltaTime, Vector2 ball2D)
    {
        if (ActivePassTransit == null || !ActivePassTransit.IsActive) return;

        ActivePassTransit.ElapsedTime += deltaTime;

        // التحقق من الحالات النهائية للكرة
        if (Ball.State == BallState.OutOfBounds || Ball.State == BallState.InGoal)
        {
            ActivePassTransit.IsActive = false;
            ActivePassTransit = null;
            return;
        }

        if (Ball.Owner != null)
        {
            if (Ball.Owner == ActivePassTransit.IntendedReceiver)
            {
                ActiveUserPlayer = ActivePassTransit.IntendedReceiver;
            }
            ActivePassTransit.IsActive = false;
            ActivePassTransit = null;
            return;
        }

        if (ActivePassTransit.ElapsedTime > 4.5f || (ActivePassTransit.ElapsedTime > 0.6f && Ball.Velocity.Length() < 0.15f))
        {
            ActivePassTransit.IsActive = false;
            ActivePassTransit = null;
            return;
        }

        var receiver = ActivePassTransit.IntendedReceiver;
        float distToBall = Vector2.Distance(receiver.Position2D, ball2D);

        // 4. حساب متجه القيادة والاعتراض والجري السريع نحو مسار الكرة القادمة
        Vector2 ballVel2D = new(Ball.Velocity.X, Ball.Velocity.Y);
        Vector2 interceptTarget;

        if (distToBall < 4.0f || ballVel2D.Length() < 1.0f)
        {
            // قريب من الكرة: التوجه المباشر للكرة
            interceptTarget = ball2D;
        }
        else
        {
            // اعتراض ذكي استباقي: التنبؤ بنقطة الالتقاء بالكرة
            float ballSpeed = ballVel2D.Length();
            float receiverSprintSpeed = (receiver.Speed / 6.5f) * 1.45f * FatigueMovementMultiplier(receiver);
            float estTime = distToBall / (receiverSprintSpeed + ballSpeed);
            estTime = Math.Clamp(estTime, 0.08f, 1.2f);

            Vector2 predictedBallPos = ball2D + (ballVel2D * estTime);
            predictedBallPos.X = Math.Clamp(predictedBallPos.X, 2.0f, Pitch.Length - 2.0f);
            predictedBallPos.Y = Math.Clamp(predictedBallPos.Y, 2.0f, Pitch.Width - 2.0f);

            interceptTarget = predictedBallPos;
        }

        Vector2 toTarget = interceptTarget - receiver.Position2D;
        if (toTarget.Length() > 0.08f)
        {
            Vector2 runDir = Vector2.Normalize(toTarget);
            float sprintSpeed = (receiver.Speed / 6.5f) * 1.45f * FatigueMovementMultiplier(receiver);
            MoveAutonomousPlayer(receiver, runDir * Math.Min(sprintSpeed, toTarget.Length() / Math.Max(deltaTime, .0001f)), deltaTime);
        }

        var facingBall = ball2D - receiver.Position2D;
        if (facingBall.LengthSquared() > .000001f)
        {
            receiver.FacingDirection = Vector2.Normalize(facingBall);
        }
    }

}
