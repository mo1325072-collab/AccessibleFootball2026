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
    public bool UserAttackingRight { get; set; } = true;
    public bool HalfTimeReached { get; private set; } = false;
    public void SetMatchTimeMinutes(float minutes) => MatchTimeMinutes = minutes;

    public event Action? OnNearBoundary;
    public event Action<int>? OnCrowdVolumeChanged;

    public float TackleCooldownRemaining => _tackleCooldownTimer;
    public float SlideDurationRemaining => _slideDurationTimer;
    public bool IsSliding => _slideDurationTimer > 0.0f;
    public float PossessionGraceRemaining => _possessionGraceTimer;
    public float AiTackleCooldownRemaining => _aiTackleCooldownTimer;

    private float _tackleCooldownTimer = 0.0f;
    private float _slideDurationTimer = 0.0f;
    private float _possessionGraceTimer = 0.0f;
    private float _aiTackleCooldownTimer = 0.0f;
    private Vector2 _slideImpulseVelocity = Vector2.Zero;
    private DateTime _lastSwitchTime = DateTime.MinValue;

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

    public void PauseMatch()
    {
        State = MatchState.Paused;
        Publish(new MatchEvent(MatchEventType.MatchPaused, "إيقاف مؤقت"));
    }

    public void ResumeMatch()
    {
        bool intervalEnded = State == MatchState.HalfTime;
        State = MatchState.InPlay;
        HomeTeam.ResetStoppageWindowFlag();
        AwayTeam.ResetStoppageWindowFlag();
        if (intervalEnded) Publish(new MatchEvent(MatchEventType.KickOff, "بداية الشوط"));
    }

    public bool CanMakeSubstitution(Team team) => team.CanMakeSubstitution(State == MatchState.HalfTime);

    public bool MakeSubstitution(Team team, Player playerOut, Player playerIn)
    {
        if(team!=HomeTeam&&team!=AwayTeam)return false;
        if(playerOut.IsSentOff||playerIn.IsSentOff||!team.Players.Contains(playerOut)||!team.Bench.Contains(playerIn)||!CanMakeSubstitution(team))return false;
        Publish(new MatchEvent(MatchEventType.SubstitutionPlanned, $"تجهيز تبديل {playerOut.Name} بـ {playerIn.Name}", subjectPlayer:playerIn));
        bool success = team.MakeSubstitution(playerOut, playerIn, isHalfTime: State == MatchState.HalfTime, isMatchActive: true);
        if (success)
        {
            if (ActiveUserPlayer == playerOut)
            {
                ActiveUserPlayer = playerIn;
            }
            Publish(new MatchEvent(MatchEventType.SubstitutionMade, $"تبديل: خروج {playerOut.Name} ودخول {playerIn.Name} ({team.SubstitutionsUsed}/{team.MaxSubstitutions})", subjectPlayer: playerIn),
                ("substitution.playerRating", PerformanceOf(playerOut).Rating),
                ("substitution.reason", playerOut.InjuryStatus == PlayerInjuryStatus.MinorKnock ? "minorInjury" : "tactical"));
        }
        return success;
    }

    public void TriggerHalfTime()
    {
        State = MatchState.HalfTime;
        HalfTimeReached = true;
        UserAttackingRight = false;

        // Swap HomePosition and Position2D for all players across the pitch (تجاهل اللاعبين المطرودين خارج الملعب)
        foreach (var player in HomeTeam.Players)
        {
            if (player.IsSentOff || player.IsOffPitch)
            {
                player.Position2D = new Vector2(-100f, -100f);
                player.HomePosition = new Vector2(-100f, -100f);
                continue;
            }
            player.Position2D = new Vector2(Pitch.Length - player.Position2D.X, Pitch.Width - player.Position2D.Y);
            player.HomePosition = new Vector2(Pitch.Length - player.HomePosition.X, Pitch.Width - player.HomePosition.Y);
            player.FacingDirection = new Vector2(-player.FacingDirection.X, -player.FacingDirection.Y);
        }

        foreach (var player in AwayTeam.Players)
        {
            if (player.IsSentOff || player.IsOffPitch)
            {
                player.Position2D = new Vector2(-100f, -100f);
                player.HomePosition = new Vector2(-100f, -100f);
                continue;
            }
            player.Position2D = new Vector2(Pitch.Length - player.Position2D.X, Pitch.Width - player.Position2D.Y);
            player.HomePosition = new Vector2(Pitch.Length - player.HomePosition.X, Pitch.Width - player.HomePosition.Y);
            player.FacingDirection = new Vector2(-player.FacingDirection.X, -player.FacingDirection.Y);
        }

        // استعادة طفيفة للياقة البدنية بين الشوطين (+10 نقاط طاقة)
        foreach (var p in HomeTeam.Players.Concat(AwayTeam.Players))
        {
            p.CurrentStamina = Math.Min(p.BaseStamina, p.CurrentStamina + 10.0f);
        }

        // اختيار لاعب ميداني نشط غير مطرود لتنفيذ ركلة بداية الشوط الثاني
        var kicker = HomeTeam.Players.FirstOrDefault(p => !p.IsSentOff && !p.IsOffPitch && p.Role != PlayerRole.Goalkeeper)
            ?? HomeTeam.Players.FirstOrDefault(p => !p.IsSentOff && !p.IsOffPitch)
            ?? HomeTeam.Players[0];

        ActiveUserPlayer = kicker;
        ActiveUserPlayer.Position2D = new Vector2(Pitch.CenterSpot.X, Pitch.CenterSpot.Y);
        ActiveUserPlayer.FacingDirection = new Vector2(-1.0f, 0.0f);
        Ball.Position = new Vector3(Pitch.CenterSpot.X, Pitch.CenterSpot.Y, 0.0f);
        Ball.Velocity = Vector3.Zero;
        Ball.CatchOrTackle(ActiveUserPlayer);
        GrantPossessionGrace(1.5f);
        LastKicker = ActiveUserPlayer;
        PreviousKicker = null;
        _kickoffProtectionTimer = 4.0f;

        // Push away players out of center circle towards their side (X < CenterSpot.X)
        foreach (var opp in AwayTeam.Players)
        {
            if (opp.Position2D.X > Pitch.CenterSpot.X - 11.0f)
            {
                opp.Position2D = new Vector2(Pitch.CenterSpot.X - 12.0f, opp.Position2D.Y);
            }
        }

        CheckPitchZone();

        string halfTimeMsg = $"صافرة نهاية الشوط الأول! النتيجة: {HomeTeam.Name} {HomeTeam.Score} - {AwayTeam.Score} {AwayTeam.Name}. تبديل المرميين للشوط الثاني.";
        Publish(new MatchEvent(MatchEventType.HalfTime, halfTimeMsg), ("match.half", 1));
    }

    public void TriggerExtraTime()
    {
        IsExtraTime = true;
        HomeTeam.IsExtraTime = true;
        AwayTeam.IsExtraTime = true;
        MatchTimeMinutes = 90.0f;
        SwapPlayingEnds();
        PlaceKickoff(AwayTeam);
        Publish(new MatchEvent(MatchEventType.ExtraTimeStarted, "نهاية الوقت الأصلي بالتعادل! الاحتكام للأشواط الإضافية (+1 تبديل إضافي وفترة توقف رابعة)"));
        Publish(new MatchEvent(MatchEventType.KickOff, "بداية الشوط الإضافي الأول"));
    }

    public void ReportTacticalRadar()
    {
        if (Ball.Owner != null && HomeTeam.Players.Contains(Ball.Owner))
        {
            float distToGoal = UserAttackingRight ? (Pitch.Length - Ball.Position2D.X) : Ball.Position2D.X;
            string radarMsg = $"{Ball.Owner.Name}، {CurrentZone.ToArabicAnnouncement()}، المرمى: {(int)distToGoal} م";
            Publish(new MatchEvent(MatchEventType.TacticalRadarInfo, radarMsg, CurrentZone));
        }
        else if (Ball.Owner != null && AwayTeam.Players.Contains(Ball.Owner))
        {
            float distToOurGoal = UserAttackingRight ? Ball.Position2D.X : (Pitch.Length - Ball.Position2D.X);
            string radarMsg = $"{Ball.Owner.Name} ({AwayTeam.Name})، {CurrentZone.ToArabicAnnouncement()}، مرماك: {(int)distToOurGoal} م";
            Publish(new MatchEvent(MatchEventType.TacticalRadarInfo, radarMsg, CurrentZone));
        }
        else
        {
            Publish(new MatchEvent(MatchEventType.TacticalRadarInfo, $"كرة حرة في {CurrentZone.ToArabicAnnouncement()}.", CurrentZone));
        }
    }

    private readonly IInputHandler _inputHandler;
    private DateTime _lastRadarTime = DateTime.MinValue;

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

        HomeTeam.SetupFormation433();
        AwayTeam.SetupFormation433();

        ActiveUserPlayer = HomeTeam.Players[9];
        ActiveUserPlayer.Position2D = new Vector2(Pitch.CenterSpot.X, Pitch.CenterSpot.Y);
        Ball.Position = new Vector3(Pitch.CenterSpot.X, Pitch.CenterSpot.Y, 0.0f);
        Ball.CatchOrTackle(ActiveUserPlayer);
        GrantPossessionGrace(1.5f);
        LastKicker = ActiveUserPlayer;

        // دفع لاعبي الخصم خارج دائرة منتصف الملعب عند انطلاق اللقاء وضبط اتجاههم نحو مرمى المضيف
        foreach (var opp in AwayTeam.Players)
        {
            opp.FacingDirection = new Vector2(-1.0f, 0.0f);
            if (opp.Position2D.X < Pitch.CenterSpot.X + 11.0f)
            {
                opp.Position2D = new Vector2(Pitch.CenterSpot.X + 12.0f, opp.Position2D.Y);
            }
        }
    }

    public void Update(float deltaTime)
    {
        if (State == MatchState.GoalCelebration)
        {
            if (AutomaticGoalRestart) AdvanceGoalCelebration(deltaTime);
            return;
        }

        if (State != MatchState.InPlay || AnalysisReplayPending) return;
        CaptureReplayFrame();
        TrackPlayFrame(deltaTime);

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
                _extraTimeHalfReached = true;
                State = MatchState.HalfTime;
                SwapPlayingEnds();
                PlaceKickoff(HomeTeam);
                Publish(new MatchEvent(MatchEventType.ExtraTimeHalfTime, "نهاية الشوط الإضافي الأول"), ("match.half", 3));
                return;
            }
            if (MatchTimeMinutes >= 120.0f)
            {
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
            ActiveUserPlayer.Position2D += _slideImpulseVelocity * deltaTime;
            ActiveUserPlayer.Position2D = new Vector2(
                Math.Clamp(ActiveUserPlayer.Position2D.X, 0, Pitch.Length),
                Math.Clamp(ActiveUserPlayer.Position2D.Y, 0, Pitch.Width)
            );
        }

        if (UpdateAdvantage()) return;
        bool runUp=UpdateRunUp(deltaTime);
        if (Restart!=null && _runUpPlayer!=null)return;
        if (Restart!=null && UpdateRestart(deltaTime)) return;
        var positionsBefore=HomeTeam.Players.Concat(AwayTeam.Players).ToDictionary(p=>p,p=>p.Position2D);
        if(!runUp) ProcessUserInput(deltaTime);
        if (State != MatchState.InPlay || Restart != null) return;
        UpdatePlayersAI(deltaTime);
        foreach(var player in positionsBefore.Keys)
            player.Velocity=deltaTime>0?(player.Position2D-positionsBefore[player])/deltaTime:Vector2.Zero;
        if (Restart != null || ReviewPendingOffside()) return;
        Vector3 previousBall = Ball.Position;
        Ball.Update(deltaTime);
        CheckShotBlock(previousBall);
        UpdateClearanceOutcome();
        CheckPitchZone();
        CheckRefereeRules();

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
            State = MatchState.Paused;
            Publish(new MatchEvent(MatchEventType.MatchPaused, "إيقاف مؤقت"));
            return;
        }

        if (actions.Contains(GameAction.SlowRunUp)) BeginSlowRunUp();
        if (actions.Contains(GameAction.RequestLineup)) RequestLineup();
        // الرادار التكتيكي اللحظي على الطلب (Space أو C)
        if (actions.Contains(GameAction.TacticalRadar))
        {
            if (DateTime.UtcNow - _lastRadarTime > TimeSpan.FromMilliseconds(1200))
            {
                _lastRadarTime = DateTime.UtcNow;
                ReportTacticalRadar();
            }
        }

        Vector2 moveDir = Vector2.Zero;
        if (_slideDurationTimer <= 0.0f)
        {
            float moveSpeed = (ActiveUserPlayer.Speed / 7.5f);
            if (actions.Contains(GameAction.Sprint))
            {
                moveSpeed *= 1.6f;
            }

            if (actions.Contains(GameAction.MoveUp)) moveDir.Y -= 1.0f;
            if (actions.Contains(GameAction.MoveDown)) moveDir.Y += 1.0f;
            if (actions.Contains(GameAction.MoveLeft)) moveDir.X -= 1.0f;
            if (actions.Contains(GameAction.MoveRight)) moveDir.X += 1.0f;

            if (hasPossession && Ball.Owner == ActiveUserPlayer)
            {
                if (actions.Contains(GameAction.MoveUp))
                {
                    AdjustAimingAngle(-0.35f);
                }
                else if (actions.Contains(GameAction.MoveDown))
                {
                    AdjustAimingAngle(0.35f);
                }
            }

            if (moveDir.Length() > 0)
            {
                moveDir = Vector2.Normalize(moveDir);
                if (hasPossession && Ball.Owner == ActiveUserPlayer && (actions.Contains(GameAction.MoveUp) || actions.Contains(GameAction.MoveDown)))
                {
                    // FacingDirection تم ضبطها بدقة مثلثية في AdjustAimingAngle ولا تحتاج لإعادة تطبيع مشوهة
                }
                else
                {
                    ActiveUserPlayer.FacingDirection = moveDir;
                }
                ActiveUserPlayer.Position2D += moveDir * moveSpeed * deltaTime;
                ActiveUserPlayer.Position2D = new Vector2(
                    Math.Clamp(ActiveUserPlayer.Position2D.X, 0, Pitch.Length),
                    Math.Clamp(ActiveUserPlayer.Position2D.Y, 0, Pitch.Width)
                );

                // فحص الاقتراب من حدود الملعب وخطوط التماس
                _boundaryTimer += deltaTime;
                if (_boundaryTimer >= 0.8f)
                {
                    bool nearTouchline = ActiveUserPlayer.Position2D.Y <= 2.5f || ActiveUserPlayer.Position2D.Y >= Pitch.Width - 2.5f;
                    bool nearGoalLine = ActiveUserPlayer.Position2D.X <= 2.5f || ActiveUserPlayer.Position2D.X >= Pitch.Length - 2.5f;
                    if (nearTouchline || nearGoalLine)
                    {
                        _boundaryTimer = 0.0f;
                        OnNearBoundary?.Invoke();
                    }
                }
            }
        }

        if (actions.Contains(GameAction.Header)) TryExecuteTechnique(ShotTechnique.Header);
        else if (actions.Contains(GameAction.BicycleKick)) TryExecuteTechnique(ShotTechnique.Bicycle);
        else if (actions.Contains(GameAction.Volley)) TryExecuteTechnique(ShotTechnique.Volley);
        else if (actions.Contains(GameAction.LobShot)) TryExecuteTechnique(ShotTechnique.Lob);
        else if (actions.Contains(GameAction.CurledShot)) TryExecuteTechnique(ShotTechnique.Curled);

        if (hasPossession && Ball.Owner == ActiveUserPlayer)
        {
            if (actions.Contains(GameAction.Clearance)) ExecuteClearance();
            else if (actions.Contains(GameAction.GroundCross)) ExecuteCross(CrossStyle.Ground);
            else if (actions.Contains(GameAction.FarPostCross)) ExecuteCross(actions.Contains(GameAction.Sprint)?CrossStyle.GroundFarPost:CrossStyle.FarPost);
            else if (actions.Contains(GameAction.ShortPass))
            {
                ExecutePass(moveDir, isThroughBall: false);
            }
            else if(actions.Contains(GameAction.LoftedThroughBall))
            {
                ExecutePass(moveDir,isThroughBall:true,lofted:true);
            }
            else if (actions.Contains(GameAction.ThroughBall))
            {
                ExecuteThroughBall(moveDir);
            }
            else if (actions.Contains(GameAction.Cross))
            {
                ExecuteCross();
            }
            else if (actions.Contains(GameAction.Shoot))
            {
                ExecuteShot();
            }
        }
        else
        {
            if (actions.Contains(GameAction.SwitchPlayer))
            {
                SwitchToClosestPlayer();
            }

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

    public void ExecutePass(Vector2 aimDir, bool isThroughBall = false, bool lofted = false)
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
            float score = dot * 100.0f - (dist * 0.5f);

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

        float passSpeed = isThroughBall ? 22.0f : 18.0f;

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

    public void ExecuteCross(CrossStyle style = CrossStyle.Center)
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
        crossDir = Vector2.Normalize(boxCenter - ActiveUserPlayer.Position2D);
        bool ground = style is CrossStyle.Ground or CrossStyle.GroundFarPost;
        Ball.Kick(crossDir, horizontalSpeed: ground ? 23 : 19, verticalSpeed: ground ? .1f : 6.5f, ActiveUserPlayer);
        BeginCommentaryPass(ActiveUserPlayer, null, boxCenter, "cross", ground ? "ground" : "high", true);
        if(_attackOrigin!="corner")_attackOrigin = "cross";
        Publish(new MatchEvent(MatchEventType.BallKicked, $"عرضية عالية من {ActiveUserPlayer.Name} داخل منطقة الجزاء!", CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: ActiveUserPlayer), ("action.kind", "cross"), ("cross.height", ground ? "ground" : "high"), ("cross.target", style is CrossStyle.FarPost or CrossStyle.GroundFarPost ? "farPost" : "center"), ("cross.targetY", boxCenter.Y));
    }

    public void ExecuteShot() => TryExecuteTechnique(ShotTechnique.Driven);

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
        // F9: منع التبديل السريع جداً إذا تكرر الاستدعاء في نفس الإطار (Debounce guard)
        DateTime now = DateTime.UtcNow;
        if ((now - _lastSwitchTime).TotalMilliseconds < 200)
        {
            return;
        }
        _lastSwitchTime = now;

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
            Publish(new MatchEvent(MatchEventType.TacticalRadarInfo, $"{closest.Name} - {GetArabicPositionName(closest.CurrentAssignedPosition)}"));
        }
    }

    /// <summary>
    /// تبديل التحكم إلى أقرب مدافع
    /// </summary>
    public void SwitchToNearestDefender() => SwitchToClosestPlayer();

    public void ExecuteStandTackle()
    {
        if (_tackleCooldownTimer > 0.0f) return;
        _tackleCooldownTimer = 0.4f;

        PerformTackleAttempt(isSlideTackle: false, reach: 1.8f);
    }

    public void ExecuteSlideTackle()
    {
        if (_tackleCooldownTimer > 0.0f) return;
        _tackleCooldownTimer = 0.9f;
        _slideDurationTimer = 0.3f;

        Vector2 facing = ActiveUserPlayer.FacingDirection.Length() > 0.1f 
            ? Vector2.Normalize(ActiveUserPlayer.FacingDirection) 
            : new Vector2(1.0f, 0.0f);
        _slideImpulseVelocity = facing * 4.0f; // F10: إضافة قوة اندفاع +4.0م/ث في اتجاه الوجه لمدة 0.3 ثانية

        PerformTackleAttempt(isSlideTackle: true, reach: 2.8f);
    }

    public void AttemptTackle()
    {
        ExecuteStandTackle();
    }

    private void PerformTackleAttempt(bool isSlideTackle, float reach)
    {
        // ميزة إضافية لمدى الاستخلاص للمدافعين المتميزين (Defending Reach Bonus)
        float defReachBonus = Math.Max(0.0f, (ActiveUserPlayer.Defending - 65) * 0.012f);
        reach += defReachBonus;

        Vector2 ball2D = new(Ball.Position.X, Ball.Position.Y);
        Player? carrier = (Ball.Owner != null && AwayTeam.Players.Contains(Ball.Owner)) ? Ball.Owner : null;

        float distToOpponent = carrier != null 
            ? Vector2.Distance(ActiveUserPlayer.Position2D, carrier.Position2D) 
            : float.MaxValue;

        // If carrier is holding the ball and ball position equals carrier position, project ball to carrier's feet
        Vector2 effectiveBall2D = ball2D;
        if (carrier != null && Vector2.Distance(ball2D, carrier.Position2D) < 0.05f)
        {
            Vector2 carrierFacing = carrier.FacingDirection.Length() > 0.1f 
                ? Vector2.Normalize(carrier.FacingDirection) 
                : new Vector2(-1.0f, 0.0f);
            effectiveBall2D = carrier.Position2D + carrierFacing * 0.35f;
        }

        float distToBall = Vector2.Distance(ActiveUserPlayer.Position2D, effectiveBall2D);
        float reachCheckDist = carrier != null ? distToOpponent : distToBall;

        // F11: إذا كان التدخل بعيداً عن المدى المطلوب، إعلان المسافة الرقمية بدقة وبدون تلميحات تعليمية
        if (reachCheckDist > reach)
        {
            if (isSlideTackle)
            {
                Publish(new MatchEvent(MatchEventType.TackleAttempted, "انزلاق", CurrentZone, spatialPan: CalculateBallPan()));
            }

            string outOfReachMsg = carrier != null 
                ? $"بعيد عن حامل الكرة: {distToOpponent:F1} متر"
                : $"بعيد: {distToBall:F1} م";
            Publish(new MatchEvent(MatchEventType.TacticalRadarInfo, outOfReachMsg));
            return;
        }

        // إذا كان هناك حامل للكرة من الفريق المنافس
        if (carrier != null)
        {
            // المعادلة الحتمية العادلة لاستخلاص الكرة: مقارنة القرب من الكرة والقرب من جسم الخصم
            if (distToBall < distToOpponent)
            {
                // استخلاص ناجح واستعادة الكرة بنسبة 100%
                Ball.CatchOrTackle(ActiveUserPlayer);
                RecordBallTouch(ActiveUserPlayer);
                GrantPossessionGrace(1.4f);

                // إبعاد الخصم المستخلص منه الكرة قليلاً لمنع التداخل الفوري
                Vector2 pushAway = carrier.Position2D - ActiveUserPlayer.Position2D;
                if (pushAway.Length() > 0.05f)
                {
                    carrier.Position2D += Vector2.Normalize(pushAway) * 1.5f;
                }

                string tackleStyle = isSlideTackle ? "بانزلاق أرضي" : "بتدخل هادئ";
                string successDesc = $"استخلاص: {ActiveUserPlayer.Name} ({tackleStyle})";
                Publish(new MatchEvent(MatchEventType.UserTackleSuccessful, successDesc, CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: ActiveUserPlayer),("tackle.sliding",isSlideTackle));
            }
            else
            {
                // ارتكاب خطأ / فاول لكون اللاعب أقرب لجسم الخصم منه للكرة
                Ball.CatchOrTackle(carrier);
                Ball.Velocity = Vector3.Zero;

                Vector2 carrierFacing = carrier.FacingDirection.Length() > 0.1f 
                    ? Vector2.Normalize(carrier.FacingDirection) 
                    : new Vector2(-1.0f, 0.0f);
                Vector2 tacklerFacing = ActiveUserPlayer.FacingDirection.Length() > 0.1f 
                    ? Vector2.Normalize(ActiveUserPlayer.FacingDirection) 
                    : new Vector2(1.0f, 0.0f);
                Vector2 carrierToTackler = ActiveUserPlayer.Position2D - carrier.Position2D;
                float behindDot = carrierToTackler.Length() > 0.05f 
                    ? Vector2.Dot(carrierFacing, Vector2.Normalize(carrierToTackler)) 
                    : 0.0f;
                bool isFromBehind = behindDot < -0.2f || Vector2.Dot(carrierFacing, tacklerFacing) > 0.3f;

                string foulDesc = $"خطأ ضد {ActiveUserPlayer.Name} لعرقلة غير مشروعة{(isFromBehind ? " من الخلف" : "")}!";
                Publish(new MatchEvent(MatchEventType.FoulCommitted, foulDesc, CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: ActiveUserPlayer), ("foul.intentional", false));

                HandleFoulSanction(ActiveUserPlayer, carrier, isSlideTackle, isFromBehind);
            }
        }
        else
        {
            // الكرة حرة (بدون مالك) وضمن مدى الاستخلاص
            Ball.CatchOrTackle(ActiveUserPlayer);
            RecordBallTouch(ActiveUserPlayer);
            GrantPossessionGrace(1.4f);

            string tackleStyle = isSlideTackle ? "بانزلاق أرضي" : "سيطرة هادئة";
            string recoverDesc = $"استحواذ: {ActiveUserPlayer.Name} ({tackleStyle})";
            Publish(new MatchEvent(MatchEventType.UserTackleSuccessful, recoverDesc, CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: ActiveUserPlayer),("tackle.sliding",isSlideTackle));
        }
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
    );

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
        int? forcedFoulRoll = null)
    {
        // 1. حساب الكفاءة والقدرة التقديرية للمدافع (Tackler Skill Score):
        // الدفاع (50%) + القوة البدنية (30%) + السرعة (20%) + معامل الإرهاق
        float tacklerStaminaFactor = Math.Clamp(tackler.Stamina / 100.0f, 0.60f, 1.0f);
        float tacklerSkill = ((tackler.Defending * 0.50f) + (tackler.Physical * 0.30f) + (tackler.Speed * 0.20f)) * tacklerStaminaFactor;

        // 2. حساب الكفاءة والقدرة التقديرية لحامل الكرة (Carrier Skill Score):
        // المراوغة والتحكم (45%) + القوة البدنية لحجز الكرة (35%) + السرعة وخفة الحركة (20%) + معامل الإرهاق
        float carrierStaminaFactor = Math.Clamp(carrier.Stamina / 100.0f, 0.60f, 1.0f);
        float carrierSkill = ((carrier.Dribbling * 0.45f) + (carrier.Physical * 0.35f) + (carrier.Speed * 0.20f)) * carrierStaminaFactor;

        // تطبيق معامل التناغم التكتيكي لحامل الكرة (بما في ذلك توافق الخطة مع التكتيك)
        var carrierSynergy = carrierTeam.Tactics.CalculateSynergy(carrierTeam.CurrentFormation);
        carrierSkill *= carrierSynergy.AdvantageMultiplier;

        // تطبيق معامل التناغم التكتيكي للمدافع
        var tacklerSynergy = tacklerTeam.Tactics.CalculateSynergy(tacklerTeam.CurrentFormation);

        // قاعدة ذهبية: مستحيل ندي الأدفانتيدج التكتيكي للفريق الأضعف مهارياً لو بيلعب رجل لرجل (Man-to-Man)
        // لأن الفوارق الفردية في الـ 1v1 بدون تغطية منطقة تفضح ضعف المدافع أمام المهاجم المهاري
        float tacklerAdvantage = tacklerTeam.Tactics.Marking == DefensiveMarking.ManToMan && tacklerSkill < carrierSkill
            ? Math.Min(tacklerSynergy.AdvantageMultiplier, 1.0f)
            : tacklerSynergy.AdvantageMultiplier;
        tacklerSkill *= tacklerAdvantage;

        // فارق الطاقات الأساسي بين المدافع وحامل الكرة (Skill Differential)
        float deltaSkill = tacklerSkill - carrierSkill;

        // 3. التعديلات التكتيكية والموقعية (Tactical & Positional Modifiers):
        float tacticalModifier = 0.0f;
        List<string> reasons = new();

        Vector2 relVec = carrier.Position2D - tackler.Position2D;
        float distToCarrier = relVec.Length();
        Vector2 dirToCarrier = distToCarrier > 0.05f 
            ? Vector2.Normalize(relVec) 
            : (tackler.FacingDirection.Length() > 0.1f ? Vector2.Normalize(tackler.FacingDirection) : new Vector2(1.0f, 0.0f));

        Vector2 carrierFacing = carrier.FacingDirection.Length() > 0.1f 
            ? Vector2.Normalize(carrier.FacingDirection) 
            : new Vector2(1.0f, 0.0f);
        Vector2 tacklerFacing = tackler.FacingDirection.Length() > 0.1f 
            ? Vector2.Normalize(tackler.FacingDirection) 
            : dirToCarrier;

        // أ) حجز الكرة بالجسم (Body Shielding):
        // إذا كان ظهر حامل الكرة في اتجاه المدافع، فالكرة في الأمام محمية بجسده
        float shieldingDot = Vector2.Dot(carrierFacing, dirToCarrier);
        bool isShielding = shieldingDot > 0.25f;
        bool isFromBehind = shieldingDot > 0.45f || Vector2.Dot(carrierFacing, tacklerFacing) > 0.35f;

        if (isShielding)
        {
            float shieldBonus = -18.0f - Math.Max(0.0f, (carrier.Physical - 75) * 0.30f);
            tacticalModifier += shieldBonus;
            reasons.Add("حجز جسدي للكرة");
        }
        else if (Vector2.Dot(carrierFacing, tacklerFacing) < -0.35f)
        {
            // مواجهة مباشرة وجهاً لوجه
            tacticalModifier += 10.0f;
            reasons.Add("مواجهة رأس برأس");
        }
        else
        {
            // زاوية جانبية مائلة
            tacticalModifier -= 8.0f;
            reasons.Add("زاوية تدخل مائلة");
        }

        // ب) القرب من الكرة (Proximity to Ball):
        float distToBall = Vector2.Distance(tackler.Position2D, ballPos2D);
        if (distToBall < 0.65f)
        {
            tacticalModifier += 12.0f;
            reasons.Add("قرب محكم من الكرة");
        }
        else if (distToBall > 1.10f)
        {
            tacticalModifier -= 12.0f;
            reasons.Add("محاولة تدخل بأقصى مدى");
        }

        // ج) الضغط الجماعي والزيادة العددية للمدافعين (Supporting Pressers):
        int supportingPressers = tacklerTeam.Players.Count(p =>
            p != tackler &&
            !p.IsSentOff &&
            !p.IsOffPitch &&
            p.Role != PlayerRole.Goalkeeper &&
            Vector2.Distance(p.Position2D, carrier.Position2D) <= 3.6f);

        if (supportingPressers > 0)
        {
            // في دفاع المنطقة تكون المساندة الجماعية كاملة، بينما في رقابة رجل لرجل يلتزم الزملاء بمراقبة خصومهم
            float pressBonus = tacklerTeam.Tactics.Marking == DefensiveMarking.ManToMan
                ? Math.Min(supportingPressers, 2) * 3.5f
                : Math.Min(supportingPressers, 3) * 7.0f;
            tacticalModifier += pressBonus;
            reasons.Add($"ضغط جماعي ({supportingPressers} لاعب)");
        }

        // د) مساندة الزملاء لحامل الكرة لتفريغ الضغط (Carrier Outlets):
        int supportingOutlets = carrierTeam.Players.Count(p =>
            p != carrier &&
            !p.IsSentOff &&
            !p.IsOffPitch &&
            p.Role != PlayerRole.Goalkeeper &&
            Vector2.Distance(p.Position2D, carrier.Position2D) <= 4.0f);

        if (supportingOutlets > 0)
        {
            float outletBonus = Math.Min(supportingOutlets, 2) * 4.0f;
            tacticalModifier -= outletBonus;
            reasons.Add("دعم تكتيكي قريب");
        }

        // هـ) تأثير مستويات شراسة العرقلة (Tackling Aggression):
        float aggressionTackleBonus = 0.0f;
        float baseFoulRisk = 12.0f;
        bool isReckless = false;

        switch (tacklerTeam.Tactics.Tackling)
        {
            case TacklingAggression.Cautious:
                aggressionTackleBonus = -10.0f;
                baseFoulRisk = 2.0f; // شبه مستحيل يرتكب فاول
                reasons.Add("تدخل حذر");
                break;
            case TacklingAggression.Normal:
                aggressionTackleBonus = 0.0f;
                baseFoulRisk = 12.0f;
                break;
            case TacklingAggression.Moderate:
                aggressionTackleBonus = +8.0f;
                baseFoulRisk = 20.0f;
                reasons.Add("التحام بدني قوي");
                break;
            case TacklingAggression.Aggressive:
                aggressionTackleBonus = +16.0f;
                baseFoulRisk = 35.0f;
                reasons.Add("انزلاق حماسي شرس");
                break;
            case TacklingAggression.Reckless:
                aggressionTackleBonus = +25.0f;
                baseFoulRisk = 55.0f;
                isReckless = true;
                reasons.Add("اندفاع متهور");
                break;
        }
        tacticalModifier += aggressionTackleBonus;

        // و) تأثير شريط الضغط العالي ومصيدة التحضير القصير:
        // إذا كان فريق المدافع يضغط عالي جداً (75+) وحامل الكرة يبني من الخلف (شريط التمرير < 35):
        if (tacklerTeam.Tactics.PressingIntensity > 75 && carrierTeam.Tactics.PassingDirectness < 35)
        {
            tacticalModifier += 14.0f;
            reasons.Add("مصيدة الضغط العالي على بناء الهجمة القصير");
        }

        // ز) أسلوب وطريقة الدفاع (Zonal Marking vs Man-to-Man Marking):
        if (tacklerTeam.Tactics.Marking == DefensiveMarking.ManToMan)
        {
            // في رقابة رجل لرجل (Man-to-Man): صراعات ثنائية فردية بحتة
            if (deltaSkill < 0)
            {
                // الفارق الفردي يتضخم ضد المدافع الأضعف لغياب تغطية المنطقة
                tacticalModifier += (deltaSkill * 0.25f);
                reasons.Add("فشل رقابة رجل لرجل أمام تفوق المهارة الفردية للخصم");
            }
            else
            {
                // إذا كان المدافع متفوقاً أو نداً مهارياً وبدنياً، فالرقابة اللصيقة تخنق المهاجم
                tacticalModifier += 8.0f;
                reasons.Add("رقابة لصيقة رجل لرجل ناجحة");
            }
        }
        else // Zonal Marking (دفاع منطقة)
        {
            // في دفاع المنطقة، التكتل وتغطية المساحات يعوضان فوارق المهارة الفردية للفرق الأقل قوة
            if (deltaSkill < -8.0f)
            {
                tacticalModifier += 6.0f;
                reasons.Add("تغطية دفاع المنطقة تعوض فارق المهارة الفردية");
            }
        }

        // ح) تأثير أسلوب الهجمات المرتدة والون تاتش في التحولات:
        if (carrierTeam.Tactics.Focus == AttackingFocus.CounterAttack && carrierTeam.Tactics.Tempo >= 65)
        {
            tacticalModifier -= 14.0f;
            reasons.Add("سرعة التحول الهجومي والون تاتش");
        }

        // ط) تعديل مستوى الصعوبة (تعديل طفيف لليقظة دون إلغاء تأثير الطاقات):
        float diffModifier = diff switch
        {
            DifficultyLevel.Easy => -8.0f,
            DifficultyLevel.Normal => 0.0f,
            DifficultyLevel.Hard => +6.0f,
            DifficultyLevel.Legendary => +12.0f,
            _ => 0.0f
        };

        // حساب نسبة النجاح النهائية:
        // النسبة المرجعية في الصراع المتكافئ 42%
        float winProbability = 42.0f + (deltaSkill * 0.55f) + tacticalModifier + diffModifier;
        winProbability = Math.Clamp(winProbability, 5.0f, 92.0f);

        int roll = forcedRandomRoll ?? Random.Shared.Next(100);
        bool tacklerWon = roll < winProbability;

        // فحص ارتكاب خطأ
        bool isFoul = false;
        bool severeInjuryRisk = false;

        if (!tacklerWon)
        {
            float foulChance = baseFoulRisk;
            if (isFromBehind) foulChance += 25.0f;
            foulChance -= (tackler.Defending - 65) * 0.4f;
            foulChance = Math.Clamp(foulChance, 2.0f, 85.0f);

            int foulRoll = forcedFoulRoll ?? (forcedRandomRoll ?? Random.Shared.Next(100));
            isFoul = foulRoll < foulChance;

            if (isFoul && isReckless)
            {
                severeInjuryRisk = true; // خطر إصابة جسيمة بالغة عند التدخل المتهور
            }
        }
        else if (isReckless)
        {
            // حتى لو استخلص الكرة بتهور، هناك احتمال 20% لارتكاب مخالفة عنف زائد
            int recklessFoulRoll = forcedFoulRoll ?? (forcedRandomRoll ?? Random.Shared.Next(100));
            if (recklessFoulRoll < 20)
            {
                tacklerWon = false;
                isFoul = true;
                severeInjuryRisk = true;
            }
        }

        string reasonStr = reasons.Count > 0 ? string.Join("، ", reasons) : "صراع متكافئ";

        return new DuelResult(
            tacklerWon,
            isFoul,
            severeInjuryRisk,
            winProbability,
            tacklerSkill,
            carrierSkill,
            tacticalModifier,
            reasonStr
        );
    }

    /// <summary>
    /// تحديث استنزاف اللياقة البدنية للاعبين وفق منحنى الأعمار وشرايط الضغط التكتيكية
    /// - تحت 19 سنة: رئة لا تتعب (استنزاف 1.0x حتى عند الضغط الخانق 100%)
    /// - 19 إلى 24 سنة: طاقة شبابية (ينهج في آخر 10 دقائق فقط)
    /// - 24 إلى 29 سنة: قمة النضج (ينفد وقوده عند الدقيقة 60 عند الضغط العالي)
    /// - فوق 29 سنة (30+): خبرة (يجيب جاز بنهاية الشوط الأول عند الضغط العالي)
    /// </summary>
    public void UpdateStaminaDecay(float simulatedMinutesPassed)
    {
        if (simulatedMinutesPassed <= 0.0f) return;

        // معدل الاستنزاف الأساسي في الدقيقة الميدانية القياسية
        float baseDecayPerSimMinute = 28.0f / 90.0f; // يستهلك ~28 نقطة عبر 90 دقيقة في الظروف الطبيعية

        void DecayTeam(Team team)
        {
            // معامل الجهد البدني التكتيكي الإضافي عند الضغط العالي
            float pressingWorkload = team.Tactics.PressingIntensity > 50
                ? 1.0f + ((team.Tactics.PressingIntensity - 50) / 45.0f) * 1.15f
                : 1.0f;

            foreach (var player in team.Players)
            {
                if (player.IsSentOff || player.IsOffPitch) continue;

                // حراس المرمى استهلاكهم البدني طفيف جداً
                if (player.Role == PlayerRole.Goalkeeper)
                {
                    player.CurrentStamina = Math.Max(0.0f, player.CurrentStamina - (simulatedMinutesPassed * baseDecayPerSimMinute * 0.15f));
                    continue;
                }

                // حساب معامل الاستنزاف وفق منحنى الأعمار وشريط الضغط
                float ageMultiplier = team.Tactics.GetStaminaDecayMultiplier(player);

                // اللاعبين الشباب تحت 19 سنة لديهم قدرة تعافي لحظية واستنزاف غير مجهد
                float playerWorkload = player.Age < 19 ? 1.0f : pressingWorkload;

                // إجهاد إضافي في حالة السبرنت أو الجري السريع
                float movementSpeed = player.Velocity.Length();
                float sprintCostMultiplier = movementSpeed > 4.5f ? 1.35f : (movementSpeed > 2.0f ? 1.10f : 1.00f);

                float decay = simulatedMinutesPassed * baseDecayPerSimMinute * ageMultiplier * playerWorkload * sprintCostMultiplier;
                player.CurrentStamina = Math.Max(0.0f, player.CurrentStamina - decay);
            }
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
        double roll = forcedRoll >= 0.0 ? forcedRoll : Random.Shared.NextDouble();
        return roll < ComputeSaveProbability(shooter, goalkeeper);
    }

    /// <summary>
    /// وجهة تسديدة الكمبيوتر: مقص قريب أو بعيد (بتفضيل البعيد) مع انتشار مهاري،
    /// بدلاً من التصويب الدائم إلى قلب المرمى حيث يقف الحارس فيلتقط كل شيء.
    /// </summary>
    public static Vector2 ComputeCpuShotTarget(Vector2 shooterPos, bool attackingRight, double postChoiceRoll, double spreadRoll)
    {
        float goalX = attackingRight ? Pitch.Length : 0.0f;
        float goalCenterY = (Pitch.GoalYMin + Pitch.GoalYMax) / 2.0f;
        bool shooterAboveCenter = shooterPos.Y < goalCenterY;
        bool aimFarPost = postChoiceRoll < 0.65;
        bool aimHighSide = aimFarPost ? !shooterAboveCenter : shooterAboveCenter;
        float targetY = aimHighSide ? Pitch.GoalYMin + 1.3f : Pitch.GoalYMax - 1.3f;
        float spread = (float)(spreadRoll * 2.0 - 1.0) * 2.2f;
        targetY = Math.Clamp(targetY + spread, Pitch.GoalYMin - 1.0f, Pitch.GoalYMax + 1.0f);
        return new Vector2(goalX, targetY);
    }

    /// <summary>
    /// حارس المرمى المدافع عن مرمى التسديدة الجارية (بحسب فريق المسدد، لا اتجاه هجوم المستخدم).
    /// </summary>
    private Player? GetDefendingGoalkeeperForShot()
    {
        Team defendingTeam = (_shotShooter != null && AwayTeam.Players.Contains(_shotShooter)) ? HomeTeam : AwayTeam;
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

    public void HandleFoulSanction(Player tackler, Player carrier, bool isSlideTackle, bool isFromBehind)
    {
        bool isDogso = CheckDogso(tackler, carrier);
        _foulVictim=carrier;
        if(isSlideTackle) Count("stats.severeFouls");
        if (isSlideTackle && carrier.InjuryStatus == PlayerInjuryStatus.None)
            ApplyInjury(carrier,PlayerInjuryStatus.MinorKnock);

        if (isDogso)
        {
            IssueCard(tackler, isDirectRed: true, reason: "منع فرصة هدف محقق (DOGSO)");
        }
        else if (isFromBehind && isSlideTackle)
        {
            IssueCard(tackler, isDirectRed: true, reason: "عرقلة عنيفة من الخلف");
        }
        else
        {
            // بطاقة صفراء للتدخل المتهور أو تعطيل هجمة واعدة
            IssueCard(tackler, isDirectRed: false, reason: "تدخل متهور");
        }

        // فحص الإصابة على اللاعب المتعرض للعرقلة: التدخلات العنيفة والانزلاقية قد تسفر عن كدمات
        if (isSlideTackle && carrier.InjuryStatus == PlayerInjuryStatus.None)
        {
            ApplyInjury(carrier, PlayerInjuryStatus.MinorKnock);
        }
        _foulVictim=null;
        if (!TryPlayAdvantage(carrier,isSlideTackle,isDogso)) AwardFoulRestart(carrier);
    }

    public void IssueCard(Player player, bool isDirectRed, string reason = "")
    {
        Team playerTeam = HomeTeam.Players.Contains(player) ? HomeTeam : AwayTeam;

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
    }

    private void DismissPlayer(Player player, Team playerTeam)
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
        player.IsSentOff = true;
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
            _lastSwitchTime = DateTime.MinValue;
            SwitchToClosestPlayer();
            if (ActiveUserPlayer == player)
            {
                ActiveUserPlayer = playerTeam.Players.FirstOrDefault(p => !p.IsSentOff && !p.IsOffPitch && p.Role != PlayerRole.Goalkeeper)
                    ?? playerTeam.Players.FirstOrDefault(p => !p.IsSentOff && !p.IsOffPitch)
                    ?? playerTeam.Players[0];
            }
        }
    }

    public void ApplyInjury(Player player, PlayerInjuryStatus status)
    {
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

            bool substituted = ExecuteForcedSubstitution(playerTeam, player);
            if (!substituted)
            {
                // استنفاد التبديلات: يخرج اللاعب المصاب ويكمل الفريق بـ 10 لاعبين
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

                player.IsOffPitch = true;
                player.Position2D = new Vector2(-100f, -100f);
                player.HomePosition = new Vector2(-100f, -100f);
                player.Velocity = Vector2.Zero;

                if (ActiveUserPlayer == player)
                {
                    _lastSwitchTime = DateTime.MinValue;
                    SwitchToClosestPlayer();
                    if (ActiveUserPlayer == player)
                    {
                        ActiveUserPlayer = playerTeam.Players.FirstOrDefault(p => !p.IsSentOff && !p.IsOffPitch && p.Role != PlayerRole.Goalkeeper)
                            ?? playerTeam.Players.FirstOrDefault(p => !p.IsSentOff && !p.IsOffPitch)
                            ?? playerTeam.Players[0];
                    }
                }

                string exhaustedDesc = $"استنفد {playerTeam.Name} جميع التبديلات! يغادر {player.Name} الملعب مصاباً، والفريق يكمل بـ 10 لاعبين.";
                Publish(new MatchEvent(MatchEventType.PlayerInjured, exhaustedDesc, CurrentZone, spatialPan: CalculateBallPan()));
            }
        }
    }

    public bool ExecuteForcedSubstitution(Team team, Player injuredPlayer)
    {
        if (team.CanMakeSubstitution(State == MatchState.HalfTime) && team.Bench.Count > 0)
        {
            var replacement = team.Bench.FirstOrDefault(b => !b.IsSentOff && !b.IsOffPitch) ?? team.Bench[0];
            return MakeSubstitution(team, injuredPlayer, replacement);
        }
        return false;
    }

    private float CalculateBallPan()
    {
        return Math.Clamp((Ball.Position.Y - (Pitch.Width / 2.0f)) / (Pitch.Width / 2.0f), -1.0f, 1.0f);
    }

    private void UpdateCrowdProximity(float deltaTime)
    {
        float distToGoal = (AwayTeam.Side == TeamSide.Away) ? (Pitch.Length - Ball.Position2D.X) : Ball.Position2D.X;
        
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

        // 5. استلام الكرة التلقائي السلس عند وصول المستلم إلى الكرة (المسافة <= 2.2م والارتفاع <= 1.5م)
        if (distToBall <= 2.2f && Ball.Position.Z <= 1.5f)
        {
            if (!TryControlIncomingBall(receiver))
            {
                if (ActivePassTransit != null) ActivePassTransit.IsActive=false;
                ActivePassTransit=null;
                return;
            }
            GrantPossessionGrace(1.4f);
            ActiveUserPlayer = receiver;
            ActivePassTransit.IsActive = false;
            ActivePassTransit = null;

            string zoneDesc = CurrentZone.ToArabicAnnouncement();
            string receptionMsg = $"استلم {receiver.Name} الكرة بنجاح في {zoneDesc}.";
            Publish(new MatchEvent(
                MatchEventType.BallTrapped,
                receptionMsg,
                CurrentZone,
                spatialPan: CalculateBallPan(), subjectPlayer: receiver
            ));
            return;
        }

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
            float receiverSprintSpeed = (receiver.Speed / 6.5f) * 1.45f;
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
            float sprintSpeed = (receiver.Speed / 6.5f) * 1.45f;
            receiver.Position2D += runDir * sprintSpeed * deltaTime;
            receiver.Position2D = new Vector2(
                Math.Clamp(receiver.Position2D.X, 1.0f, Pitch.Length - 1.0f),
                Math.Clamp(receiver.Position2D.Y, 1.0f, Pitch.Width - 1.0f)
            );
        }

        if (distToBall > 0.1f)
        {
            receiver.FacingDirection = Vector2.Normalize(ball2D - receiver.Position2D);
        }
    }

    private void UpdatePlayersAI(float deltaTime)
    {
        Vector2 ball2D = new(Ball.Position.X, Ball.Position.Y);

        // تحديث التمريرة النشطة واعتراض المستلم أولاً قبل أي شيء
        if (ActivePassTransit != null && ActivePassTransit.IsActive)
        {
            UpdatePassTransit(deltaTime, ball2D);
        }

        // إذا كانت فترة حماية ركلة البداية جارية والكرة مع المستخدم، يقف الخصم في مناطقه لإعطاء فرصة للتوجيه والتمرير
        if (_kickoffProtectionTimer > 0.0f && Ball.Owner != null && HomeTeam.Players.Contains(Ball.Owner))
        {
            foreach (var opponent in AwayTeam.Players)
            {
                opponent.Position2D = Vector2.Lerp(opponent.Position2D, opponent.HomePosition, deltaTime * 1.2f);
            }
            return;
        }

        // 1. حارس مرمى ولاعبو الخصم (الكمبيوتر)
        foreach (var opponent in AwayTeam.Players)
        {
            if (opponent.IsSentOff || opponent.IsOffPitch) continue;

            if (opponent.Role == PlayerRole.Goalkeeper)
            {
                float targetY = Math.Clamp(ball2D.Y, Pitch.GoalYMin + 1.0f, Pitch.GoalYMax - 1.0f);
                float gkX = UserAttackingRight ? Pitch.Length - 4.0f : 4.0f;
                if(Ball.Owner!=opponent && Math.Abs(ball2D.X-gkX)<14 && Math.Abs(ball2D.Y-Pitch.Width/2)<16 && Ball.Position.Z<2.6f)
                    gkX=Math.Clamp(ball2D.X,UserAttackingRight?Pitch.Length-14:1,UserAttackingRight?Pitch.Length-1:14);
                opponent.Position2D = Vector2.Lerp(opponent.Position2D, new Vector2(gkX, targetY), deltaTime * 2.5f);

                if (Ball.Owner == opponent)
                {
                    _gkHoldTimer += deltaTime;
                    if (_gkHoldTimer >= 1.2f)
                    {
                        _gkHoldTimer = 0.0f;
                        Vector2 clearDir = UserAttackingRight 
                            ? new Vector2(-1.0f, ((float)Random.Shared.NextDouble() - 0.5f) * 0.4f)
                            : new Vector2(1.0f, ((float)Random.Shared.NextDouble() - 0.5f) * 0.4f);
                        SnapshotOffsideAtKick(opponent);
                        Ball.Kick(clearDir, 26.0f, 4.0f, opponent);
                        RecordBallTouch(opponent);
                        Publish(new MatchEvent(MatchEventType.BallKicked, $"حارس مرمى {AwayTeam.Name} يوزع الكرة عالية للأمام.", CurrentZone, spatialPan: CalculateBallPan(),subjectPlayer:opponent),("action.kind","pass"),("pass.height","high"));
                    }
                }
                else if (!_isShotInProgress && Vector2.Distance(opponent.Position2D, ball2D) < 2.0f && Ball.Position.Z < Pitch.GoalHeight)
                {
                    // التقاط الكرات العادية فقط: التسديدات تُحسم بمواجهة حارس واحدة مبنية
                    // على المهارات، لا بالتقاط تلقائي مؤكد 100%
                    _gkHoldTimer = 0.0f;
                    bool takenFromOpponent=Ball.Owner!=null&&HomeTeam.Players.Contains(Ball.Owner);
                    bool keeperRushed=Math.Abs(opponent.Position2D.X-(UserAttackingRight?Pitch.Length:0))>6;
                    Ball.CatchOrTackle(opponent);
                    RecordBallTouch(opponent);
                    if (ActivePassTransit != null)
                    {
                        ActivePassTransit.IsActive = false;
                        ActivePassTransit = null;
                    }
                    Publish(new MatchEvent(MatchEventType.BallTrapped,"حارس الخصم يمسك بالكرة",CurrentZone,subjectPlayer:opponent),
                        ("keeper.rushedOut",keeperRushed),("control.fromOpponent",takenFromOpponent),("save.held",true));
                }
                continue;
            }

            if (Ball.Owner == opponent)
            {
                Vector2 attackTarget = UserAttackingRight ? Pitch.LeftGoalCenter : Pitch.RightGoalCenter;
                Vector2 attackDir = Vector2.Normalize(attackTarget - opponent.Position2D);
                opponent.FacingDirection = attackDir;
                opponent.Position2D += attackDir * (opponent.Speed * 0.065f) * deltaTime;
                Ball.CatchOrTackle(opponent);

                float distToUserGoal = Vector2.Distance(opponent.Position2D, attackTarget);
                if (distToUserGoal < 24.0f)
                {
                    // تصويب الكمبيوتر نحو مقص المرمى مع انتشار مهاري، بدلاً من قلب المرمى
                    // دائماً حيث يقف الحارس فيلتقط كل التسديدات ولا يسجل الكمبيوتر أبداً
                    Vector2 shotTarget = ComputeCpuShotTarget(opponent.Position2D, !UserAttackingRight, Random.Shared.NextDouble(), Random.Shared.NextDouble());
                    Vector2 cpuShotDir = Vector2.Normalize(shotTarget - opponent.Position2D);
                    float cpuShotPower = 24.0f + (opponent.Shooting * 0.08f);
                    SnapshotOffsideAtKick(opponent);
                    Ball.Kick(cpuShotDir, cpuShotPower, 1.6f, opponent);
                    _isShotInProgress = true;
                    _shotTargetGoalX = shotTarget.X;
                    _shotGkResolved = false;
                    _shotShooter = opponent;
                    _currentShotTechnique = ShotTechnique.Driven;
                    _lastShotTargetY = shotTarget.Y;
                    Publish(new MatchEvent(MatchEventType.ShotTaken, $"تسديدة خطيرة من {opponent.Name} لاعب الخصم!", CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: opponent));
                }
                else if (!ActiveUserPlayer.IsSentOff && !ActiveUserPlayer.IsOffPitch && Vector2.Distance(ActiveUserPlayer.Position2D, opponent.Position2D) < 2.5f && Random.Shared.Next(100) < 5)
                {
                    var openTeammate = AwayTeam.Players.FirstOrDefault(p => p != opponent && p.Role != PlayerRole.Goalkeeper && 
                        (UserAttackingRight ? p.Position2D.X < opponent.Position2D.X : p.Position2D.X > opponent.Position2D.X));
                    if (openTeammate != null)
                    {
                        Vector2 passDir = Vector2.Normalize(openTeammate.Position2D - opponent.Position2D);
                        RecordBallTouch(opponent);
                        SnapshotOffsideAtKick(opponent);
                        Ball.Kick(passDir, 18.0f, 0.2f, opponent);
                        BeginCommentaryPass(opponent, openTeammate, openTeammate.Position2D, "short", "ground");
                        Publish(new MatchEvent(MatchEventType.BallKicked, $"تمريرة من {opponent.Name} إلى {openTeammate.Name}", CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: opponent));
                    }
                }
                continue;
            }

            float distToBall = Vector2.Distance(opponent.Position2D, ball2D);
            var diff = MatchSettings.Current.Difficulty;
            float speedMult = diff switch
            {
                DifficultyLevel.Easy => 0.045f,
                DifficultyLevel.Normal => 0.062f,
                DifficultyLevel.Hard => 0.085f,
                DifficultyLevel.Legendary => 0.105f,
                _ => 0.062f
            };

            float pressRange = diff switch
            {
                DifficultyLevel.Easy => 10.0f,
                DifficultyLevel.Normal => 13.5f,
                DifficultyLevel.Hard => 16.5f,
                DifficultyLevel.Legendary => 20.0f,
                _ => 13.5f
            };

            // توازن الذكاء الاصطناعي: أثناء التمريرة، يضغط فقط المدافع القريب في مسار التمريرة دون تكالب المدافعين البعيدين
            if (ActivePassTransit != null && ActivePassTransit.IsActive)
            {
                pressRange = Math.Min(pressRange, 8.0f);
            }

            if (distToBall < pressRange)
            {
                Vector2 pressDir = Vector2.Normalize(ball2D - opponent.Position2D);
                opponent.Position2D += pressDir * (opponent.Speed * speedMult * DefensiveReactionMultiplier(opponent)) * deltaTime;
                opponent.FacingDirection = pressDir;

                if (distToBall < 1.3f && Ball.Owner != opponent && (Ball.Owner == null || !AwayTeam.Players.Contains(Ball.Owner)))
                {
                    // فحص حماية الاستحواذ وفترة تهدئة قطع الكرة لمنع قطع الكرة المتكرر بسرعة غير بشرية
                    if (_possessionGraceTimer > 0.0f || _aiTackleCooldownTimer > 0.0f)
                    {
                        // الخصم يضغط ويلاحق فقط دون أن يقطع الكرة فورياً
                    }
                    else
                    {
                        var carrier = Ball.Owner;
                        if (carrier == null)
                        {
                            // كرة حرة يستحوذ عليها الخصم عند وصوله إليها
                            Ball.CatchOrTackle(opponent);
                            RecordBallTouch(opponent);
                            _aiTackleCooldownTimer = 1.6f;
                            if (ActivePassTransit != null)
                            {
                                ActivePassTransit.IsActive = false;
                                ActivePassTransit = null;
                            }
                            string desc = $"استحوذ {opponent.Name} على الكرة في {CurrentZone.ToArabicAnnouncement()}.";
                            Publish(new MatchEvent(MatchEventType.OpponentTackleSuccessful, desc, CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: opponent));
                        }
                        else
                        {
                            // صراع استخلاص الكرة التكتيكي والبدني المبني على الطاقات والتموضع وشراسة العرقلة
                            var duel = EvaluateTackleDuel(
                                tackler: opponent,
                                carrier: carrier,
                                ballPos2D: ball2D,
                                tacklerTeam: AwayTeam,
                                carrierTeam: HomeTeam,
                                diff: diff
                            );

                            _aiTackleCooldownTimer = 1.5f;

                            if (duel.TacklerWon)
                            {
                                Ball.CatchOrTackle(opponent);
                                RecordBallTouch(opponent);
                                _aiTackleCooldownTimer = 2.0f;
                                if (ActivePassTransit != null)
                                {
                                    ActivePassTransit.IsActive = false;
                                    ActivePassTransit = null;
                                }
                                string desc = $"قطع {opponent.Name} الكرة من {carrier.Name}! ({duel.TacticalReason})";
                                Publish(new MatchEvent(MatchEventType.OpponentTackleSuccessful, desc, CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: opponent));
                            }
                            else if (duel.IsFoul)
                            {
                                // ارتكب الخصم خطأ لصالحه أثناء محاولة التدخل غير الشرعي
                                _aiTackleCooldownTimer = 2.5f;
                                Ball.CatchOrTackle(carrier);
                                Ball.Velocity = Vector3.Zero;
                                string foulDesc = $"خطأ لصالح {carrier.Name} بعد عرقلة من {opponent.Name}!";
                                Publish(new MatchEvent(MatchEventType.FoulCommitted, foulDesc, CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: opponent), ("foul.intentional", false));
                                HandleFoulSanction(opponent, carrier, isSlideTackle: duel.IsSevereInjuryRisk, isFromBehind: true);

                                if (duel.IsSevereInjuryRisk && carrier.InjuryStatus == PlayerInjuryStatus.None)
                                {
                                    ApplyInjury(carrier, PlayerInjuryStatus.MinorKnock);
                                }
                            }
                            else
                            {
                                // حامل الكرة احتفظ بكرته بنجاح بفضل مهارته أو قوته البدنية أو حجز الكرة
                                _aiTackleCooldownTimer = 1.3f;
                                _dribblesWon++;
                                Publish(new MatchEvent(MatchEventType.DribbleSuccessful, $"{carrier.Name} يتجاوز محاولة الاستخلاص", CurrentZone, subjectPlayer: carrier),
                                    ("attack.defenderBeaten", true), ("attack.dribblesWon", _dribblesWon));
                            }
                        }
                    }
                }
            }
            else
            {
                opponent.Position2D = Vector2.Lerp(opponent.Position2D, opponent.HomePosition, deltaTime * 0.8f);
            }
        }

        // 2. لاعبو فريق المستخدم (HomeTeam)
        foreach (var teammate in HomeTeam.Players)
        {
            if (teammate.IsSentOff || teammate.IsOffPitch) continue;

            if (teammate == ActiveUserPlayer && (ActivePassTransit == null || !ActivePassTransit.IsActive || teammate != ActivePassTransit.IntendedReceiver))
            {
                continue;
            }

            if (ActivePassTransit != null && ActivePassTransit.IsActive && teammate == ActivePassTransit.IntendedReceiver)
            {
                // المستلم يتم توجيهه واعتراضه عبر UpdatePassTransit
                continue;
            }

            if (teammate.Role == PlayerRole.Goalkeeper)
            {
                float targetY = Math.Clamp(ball2D.Y, Pitch.GoalYMin + 1.0f, Pitch.GoalYMax - 1.0f);
                float gkX = UserAttackingRight ? 4.0f : Pitch.Length - 4.0f;
                if(Ball.Owner!=teammate && Math.Abs(ball2D.X-gkX)<14 && Math.Abs(ball2D.Y-Pitch.Width/2)<16 && Ball.Position.Z<2.6f)
                    gkX=Math.Clamp(ball2D.X,UserAttackingRight?1:Pitch.Length-14,UserAttackingRight?14:Pitch.Length-1);
                teammate.Position2D = Vector2.Lerp(teammate.Position2D, new Vector2(gkX, targetY), deltaTime * 2.5f);

                if (Ball.Owner == teammate)
                {
                    _gkHoldTimer += deltaTime;
                    if (_gkHoldTimer >= 3.0f)
                    {
                        _gkHoldTimer = 0.0f;
                        Vector2 clearDir = UserAttackingRight 
                            ? new Vector2(1.0f, ((float)Random.Shared.NextDouble() - 0.5f) * 0.4f)
                            : new Vector2(-1.0f, ((float)Random.Shared.NextDouble() - 0.5f) * 0.4f);
                        SnapshotOffsideAtKick(teammate);
                        Ball.Kick(clearDir, 26.0f, 4.0f, teammate);
                        RecordBallTouch(teammate);
                        Publish(new MatchEvent(MatchEventType.BallKicked, $"حارسك {teammate.Name} يوزع الكرة عالية للأمام.", CurrentZone, spatialPan: CalculateBallPan(),subjectPlayer:teammate),("action.kind","pass"),("pass.height","high"));
                    }
                }
                else if (!_isShotInProgress && Vector2.Distance(teammate.Position2D, ball2D) < 2.5f && Ball.Position.Z < Pitch.GoalHeight && (Ball.Owner == null || AwayTeam.Players.Contains(Ball.Owner)))
                {
                    _gkHoldTimer = 0.0f;
                    bool takenFromOpponent=Ball.Owner!=null&&AwayTeam.Players.Contains(Ball.Owner);
                    bool keeperRushed=Math.Abs(teammate.Position2D.X-(UserAttackingRight?0:Pitch.Length))>6;
                    Ball.CatchOrTackle(teammate);
                    RecordBallTouch(teammate);
                    ActiveUserPlayer = teammate;
                    if (ActivePassTransit != null)
                    {
                        ActivePassTransit.IsActive = false;
                        ActivePassTransit = null;
                    }
                    Publish(new MatchEvent(MatchEventType.BallTrapped, $"حارس مرماك {teammate.Name} يمسك بالكرة بأمان.", CurrentZone, spatialPan: CalculateBallPan(),subjectPlayer:teammate),
                        ("keeper.rushedOut",keeperRushed),("control.fromOpponent",takenFromOpponent),("save.held",true));
                }
                continue;
            }

            Vector2 shiftedHome = teammate.HomePosition + new Vector2((ball2D.X - 52.5f) * 0.4f, 0);
            teammate.Position2D = Vector2.Lerp(teammate.Position2D, shiftedHome, deltaTime * DefensiveReactionMultiplier(teammate));

            if (Ball.State == BallState.Free && Ball.Position.Z <= 1.5f && !IsPassInTransit && Vector2.Distance(teammate.Position2D, ball2D) < 1.8f)
            {
                if (!TryControlIncomingBall(teammate)) continue;
                ActiveUserPlayer = teammate;
                if (ActivePassTransit != null)
                {
                    ActivePassTransit.IsActive = false;
                    ActivePassTransit = null;
                }
                string zoneDesc = CurrentZone.ToArabicAnnouncement();
                string receptionMsg = $"استلم {teammate.Name} الكرة في {zoneDesc}.";
                Publish(new MatchEvent(MatchEventType.BallTrapped, receptionMsg, CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: teammate));
            }
        }

        // 3. منع تجمد الكرة الحرة إطلاقاً: إذا سقطت الكرة في أي مكان بدون صاحب يجري أقرب لاعب إليها فوراً
        if (Ball.Owner == null && !IsPassInTransit && !_isShotInProgress)
        {
            _ballFreeTimer += deltaTime;
            Player? closestPlayer = null;
            float closestDist = float.MaxValue;
            var allOutfield = HomeTeam.Players.Where(p => p.Role != PlayerRole.Goalkeeper && !p.IsSentOff && !p.IsOffPitch)
                .Concat(AwayTeam.Players.Where(p => p.Role != PlayerRole.Goalkeeper && !p.IsSentOff && !p.IsOffPitch));

            foreach (var p in allOutfield)
            {
                // إذا كانت هناك تسديدة أو تمريرة، يستبعد الراكل أثناء انطلاق الكرة السريعة لمنعه من استردادها فور ركلها
                if (p == LastKicker && Ball.Velocity.Length() > 2.0f)
                {
                    continue;
                }

                // إذا كانت هناك تمريرة جارية، يتم استبعاد مرسل التمريرة أثناء انطلاق الكرة لمنعه من استردادها فور ركلها
                if (IsPassInTransit && p == ActivePassTransit?.Sender && (ActivePassTransit.ElapsedTime < 0.35f || Ball.Velocity.Length() > 0.5f))
                {
                    continue;
                }

                // أثناء التمريرة الجارية، لا يستولي زميل آخر على الكرة الموجهة للمستلم المستهدف
                if (IsPassInTransit && HomeTeam.Players.Contains(p) && p != ActivePassTransit?.IntendedReceiver)
                {
                    continue;
                }

                float d = Vector2.Distance(p.Position2D, ball2D);
                if (d < closestDist)
                {
                    closestDist = d;
                    closestPlayer = p;
                }
            }

            if (closestPlayer != null)
            {
                Vector2 toBall = ball2D - closestPlayer.Position2D;
                if (toBall.Length() > 0.1f)
                {
                    Vector2 chaseDir = Vector2.Normalize(toBall);
                    closestPlayer.Position2D += chaseDir * (closestPlayer.Speed * 0.09f) * deltaTime;
                }

                if (closestDist < 1.6f && Ball.Position.Z <= 1.5f)
                {
                    _ballFreeTimer = 0.0f;
                    if (!TryControlIncomingBall(closestPlayer)) return;
                    if (ActivePassTransit != null)
                    {
                        ActivePassTransit.IsActive = false;
                        ActivePassTransit = null;
                    }

                    if (HomeTeam.Players.Contains(closestPlayer))
                    {
                        ActiveUserPlayer = closestPlayer;
                        Publish(new MatchEvent(MatchEventType.BallTrapped, $"الكرة أصبحت مع لاعبك {closestPlayer.Name} في {CurrentZone.ToArabicAnnouncement()}.", CurrentZone, spatialPan: CalculateBallPan()));
                    }
                    else
                    {
                        Publish(new MatchEvent(MatchEventType.BallTrapped, $"الكرة مع {closestPlayer.Name} لاعب {AwayTeam.Name} في {CurrentZone.ToArabicAnnouncement()}.", CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: closestPlayer));
                    }
                }
            }
        }
        else
        {
            _ballFreeTimer = 0.0f;
        }
    }

    private void CheckPitchZone()
    {
        var newZone = Pitch.CalculateZone(Ball.Position, attackingTowardsRight: UserAttackingRight);
        if (newZone != CurrentZone)
        {
            CurrentZone = newZone;
            Publish(new MatchEvent(MatchEventType.BallZoneChanged, "تغير منطقة اللعب", CurrentZone));
        }
    }

    public void TriggerGoalkeeperSave(string desc = "تصدي رائع وخيالي من حارس المرمى!") =>
        Publish(new MatchEvent(MatchEventType.GoalkeeperSave, desc, CurrentZone, spatialPan: CalculateBallPan()));

    public void TriggerWoodworkHit(string desc = "في القائم! العارضة تمنع هدفاً محققاً!") =>
        Publish(new MatchEvent(MatchEventType.WoodworkHit, desc, CurrentZone, spatialPan: CalculateBallPan()));

    public void TriggerPenaltyAwarded(string desc = "ركلة جزاء!") =>
        Publish(new MatchEvent(MatchEventType.PenaltyAwarded, desc, CurrentZone, spatialPan: CalculateBallPan()));

    private void CheckRefereeRules()
    {
        // 0. فحص تصدي الحارس أو ارتطام الكرة بالقائم/العارضة أثناء التسديد
        if (_isShotInProgress)
        {
            float distToGoalX = Math.Abs(Ball.Position.X - _shotTargetGoalX);

            // فحص ارتطام القائم أو العارضة (Woodwork Hit)
            if (distToGoalX <= 1.2f)
            {
                bool hitLeftPost = Math.Abs(Ball.Position.Y - Pitch.GoalYMin) <= 0.45f && Ball.Position.Z <= Pitch.GoalHeight + 0.15f;
                bool hitRightPost = Math.Abs(Ball.Position.Y - Pitch.GoalYMax) <= 0.45f && Ball.Position.Z <= Pitch.GoalHeight + 0.15f;
                bool hitCrossbar = Ball.Position.Y >= Pitch.GoalYMin - 0.2f && Ball.Position.Y <= Pitch.GoalYMax + 0.2f && Math.Abs(Ball.Position.Z - Pitch.GoalHeight) <= 0.25f;

                if (hitLeftPost || hitRightPost || hitCrossbar)
                {
                    _isShotInProgress = false;
                    Ball.Velocity = new Vector3(-Ball.Velocity.X * 0.75f, Ball.Velocity.Y * 0.8f, Math.Max(1.5f, Ball.Velocity.Z * 0.5f));
                    Ball.Position = new Vector3(Math.Clamp(Ball.Position.X, 1.0f, Pitch.Length - 1.0f), Ball.Position.Y, Math.Clamp(Ball.Position.Z, 0.0f, Pitch.GoalHeight));
                    _lastShotWoodwork = hitCrossbar ? "crossbar" : "post";
                    Publish(new MatchEvent(MatchEventType.WoodworkHit, hitCrossbar ? "في العارضة!" : "في القائم!", CurrentZone, spatialPan: CalculateBallPan()), ("woodwork.part", _lastShotWoodwork));
                    return;
                }
            }

            // فحص تصدي حارس المرمى: مواجهة واحدة لكل تسديدة تُحسم بلفة واحدة تعتمد
            // على مهارة المسدد وقدرة الحارس، لا لفة 35% متكررة بكل إطار (~82% تصدٍ حتمي)
            if (!_shotGkResolved && distToGoalX <= 3.0f && Ball.Position.Y >= Pitch.GoalYMin - 0.5f && Ball.Position.Y <= Pitch.GoalYMax + 0.5f && Ball.Position.Z <= Pitch.GoalHeight)
            {
                Player? defendingGk = GetDefendingGoalkeeperForShot();
                if (defendingGk != null)
                {
                    float gkDist = Vector2.Distance(new Vector2(Ball.Position.X, Ball.Position.Y), defendingGk.Position2D);
                    if (gkDist <= 4.2f)
                    {
                        _shotGkResolved = true;
                        Player shooter = _shotShooter ?? LastKicker ?? defendingGk;
                        if (ResolveGoalkeeperSaveAttempt(shooter, defendingGk))
                        {
                            RememberShotDeflection();
                            _isShotInProgress = false;
                            float incomingSpeed = Ball.Velocity.Length();
                            bool held = incomingSpeed <= 25 && gkDist <= 2.4f;
                            bool hardSave=incomingSpeed>30 || gkDist>1.7f || Ball.Position.Z>1.3f;
                            bool rushedOut = Math.Abs(defendingGk.Position2D.X - _shotTargetGoalX) > 6;
                            bool keeperDived=gkDist>1.2f||Ball.Position.Z>1.2f;
                            if(keeperDived)defendingGk.Position2D=Ball.Position2D;
                            bool keeperPressured = (TeamOf(defendingGk) == HomeTeam ? AwayTeam : HomeTeam).Players
                                .Any(p => p.IsActiveOnPitch && Vector2.Distance(p.Position2D, defendingGk.Position2D) < 4);
                            var shotFacts = _lastShotContext;
                            if (held)
                            {
                                Ball.CatchOrTackle(defendingGk);
                                RecordBallTouch(defendingGk, deliberatelyControlled:false);
                                _gkHoldTimer = 1.0f;
                            }
                            else
                            {
                                Ball.Velocity = new Vector3(-Ball.Velocity.X * 0.35f, ((float)Random.Shared.NextDouble() - 0.5f) * 6.0f, 0.5f);
                                RecordBallTouch(defendingGk, deliberatelyControlled:false);
                            }
                            Publish(new MatchEvent(MatchEventType.GoalkeeperSave, $"تصدي من {defendingGk.Name}!", CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: defendingGk) { Context = shotFacts },
                                ("save.dived",keeperDived),("save.keeperCount",PerformanceOf(defendingGk).Saves+1),("save.held", held), ("save.dangerContinues", !held), ("save.difficulty", hardSave ? "hard" : "easy"),
                                ("keeper.rushedOut", rushedOut), ("keeper.underPressure", keeperPressured), ("shot.onTarget", true));
                            return;
                        }
                        // الحارس مُتغلَّب عليه: الكرة تكمل نحو المرمى أو الخشبات
                    }
                }
            }

            // تسديدة ميتة: فقدت الكرة سرعتها قبل بلوغ المرمى -> عودة للعب المفتوح
            if (Ball.Velocity.Length() < 1.0f && Ball.Position.Z <= 0.05f)
            {
                _isShotInProgress = false;
            }
        }

        // 1. فحص الهدف
        if (Pitch.IsGoal(Ball.Position, out bool forTeamAttackingRight))
        {
            if(_offsideOffence!=null) { PenaliseOffside(true);return; }
            _isShotInProgress = false;
            if (ActivePassTransit != null)
            {
                ActivePassTransit.IsActive = false;
                ActivePassTransit = null;
            }
            bool forHomeTeam = UserAttackingRight ? forTeamAttackingRight : !forTeamAttackingRight;
            Team scoringTeam = forHomeTeam ? HomeTeam : AwayTeam;
            scoringTeam.Score++;

            // حساب المسجل والأسيست والهدف العكسي
            Player scorer = LastKicker ?? (forHomeTeam ? HomeTeam.Players[9] : AwayTeam.Players[9]);
            bool deflectedOnTarget=_onTargetDeflectedShooter is {} originalShooter && TeamOf(originalShooter)==scoringTeam && _elapsedSeconds-_deflectionRecordedAt<=10;
            if(deflectedOnTarget)scorer=_onTargetDeflectedShooter!;
            bool isOwnGoal = (forHomeTeam && AwayTeam.Players.Contains(scorer)) ||
                             (!forHomeTeam && HomeTeam.Players.Contains(scorer));

            Player? assistCandidate=deflectedOnTarget?_onTargetDeflectedAssister:PreviousKicker;
            Player? assister = !isOwnGoal && assistCandidate != null && assistCandidate != scorer &&
                                scoringTeam.Players.Contains(assistCandidate) ? assistCandidate : null;

            if (!isOwnGoal) scorer.MatchGoalsCount++;

            var goalDetails = new GoalDetails(
                scorer: scorer,
                assister: assister,
                isOwnGoal: isOwnGoal,
                minute: Math.Max(1, (int)MatchTimeMinutes),
                homeScore: HomeTeam.Score,
                awayScore: AwayTeam.Score,
                scoringTeam: scoringTeam
            );

            // تجميد اللعب فوراً للاحتفال بالهدف لمدة 4 ثوانٍ ثم استئناف تلقائي
            State = MatchState.GoalCelebration;
            _celebrationTimer = 4.0f;

            Publish(new MatchEvent(
                MatchEventType.GoalScored,
                $"هدف لصالح {scoringTeam.Name}!",
                CurrentZone,
                goalDetails
            ));

            // 1. فحص الهاتريك (Hat-Trick)
            if (!isOwnGoal)
            {
                if (scorer.MatchGoalsCount == 3)
                {
                    Publish(new MatchEvent(
                        MatchEventType.HatTrickAchieved,
                        $"هاتريك تاريخي للاعب {scorer.Name}!",
                        CurrentZone,
                        goalDetails
                    ));
                }
            }

            // 2. فحص الريمونتادا / العودة الدرامية (Dramatic Comeback)
            if (forHomeTeam && _homeTeamWasTrailing && HomeTeam.Score > AwayTeam.Score && MatchTimeMinutes >= 70.0f)
            {
                _homeTeamWasTrailing = false;
                Publish(new MatchEvent(
                    MatchEventType.DramaticComeback,
                    $"ريمونتادا جنونية وغير متوقعة لصالح {HomeTeam.Name}!",
                    CurrentZone,
                    goalDetails
                ));
            }
            else if (!forHomeTeam && _awayTeamWasTrailing && AwayTeam.Score > HomeTeam.Score && MatchTimeMinutes >= 70.0f)
            {
                _awayTeamWasTrailing = false;
                Publish(new MatchEvent(
                    MatchEventType.DramaticComeback,
                    $"ريمونتادا جنونية وغير متوقعة لصالح {AwayTeam.Name}!",
                    CurrentZone,
                    goalDetails
                ));
            }

            // تحديث حالة التأخر في النتيجة للفريقين
            if (HomeTeam.Score < AwayTeam.Score) _homeTeamWasTrailing = true;
            if (AwayTeam.Score < HomeTeam.Score) _awayTeamWasTrailing = true;

            return;
        }

        // 2. خروج الكرة خارج حدود الملعب (ضربات مرمى، ركنيات، رميات تماس واقعية)
        if (Pitch.IsOutOfBounds(Ball.Position, out bool isTouchline, out bool isGoalLine))
        {
            if (ActivePassTransit != null)
            {
                ActivePassTransit.IsActive = false;
                ActivePassTransit = null;
            }

            if (LastKicker == _lastMiscontroller && _elapsedSeconds <= _miscontrolUntil + 2)
            {
                Publish(new MatchEvent(MatchEventType.ControlLost,"اللمسة الأخيرة أخرجت الكرة",CurrentZone,subjectPlayer:_lastMiscontroller),
                    ("control.pressured",_miscontrolPressured),("control.outOfBounds",true));
                _lastMiscontroller=null;
            }
            _lastOutWasShot=_isShotInProgress;
            Publish(new MatchEvent(MatchEventType.BallOut,"الكرة خارج الملعب",CurrentZone));
            if (isGoalLine)
            {
                if (_isShotInProgress)
                {
                    _isShotInProgress = false;
                    Publish(new MatchEvent(MatchEventType.MissedChance, Ball.Position.Z > Pitch.GoalHeight ? "تسديدة فوق المرمى" : "تسديدة خارج المرمى", CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: _shotShooter),
                        ("miss.direction", Ball.Position.Z > Pitch.GoalHeight ? "over" : "wide"),
                        ("miss.margin", Math.Max(0, Ball.Position.Z > Pitch.GoalHeight ? Ball.Position.Z - Pitch.GoalHeight : Math.Max(Pitch.GoalYMin - Ball.Position.Y, Ball.Position.Y - Pitch.GoalYMax))));
                    RequestAnalysisReplay(MatchEventType.MissAnalysis,Snapshot().With(("attack.dribblesWon",_dribblesWon)));
                }

                bool crossedNearHomeGoal = UserAttackingRight ? (Ball.Position.X <= 0) : (Ball.Position.X >= Pitch.Length);
                bool lastTouchHome = (LastKicker != null && HomeTeam.Players.Contains(LastKicker));

                if (crossedNearHomeGoal)
                {
                    if (lastTouchHome)
                    {
                        AwardCornerKick(forHomeTeam: false);
                    }
                    else
                    {
                        AwardGoalKick(forHomeTeam: true);
                    }
                }
                else
                {
                    if (lastTouchHome)
                    {
                        AwardGoalKick(forHomeTeam: false);
                    }
                    else
                    {
                        AwardCornerKick(forHomeTeam: true);
                    }
                }
            }
            else if (isTouchline)
            {
                _isShotInProgress = false;
                bool lastTouchHome = (LastKicker != null && HomeTeam.Players.Contains(LastKicker));
                AwardThrowIn(forHomeTeam: !lastTouchHome);
            }
        }
    }

    private bool _lastOutWasShot;
    private void AwardGoalKick(bool forHomeTeam)
    {
        _isShotInProgress = false;
        bool homeAtLeft = UserAttackingRight;
        if (forHomeTeam)
        {
            var gk = HomeTeam.Players[0];
            float gkX = homeAtLeft ? 5.0f : Pitch.Length - 5.0f;
            gk.Position2D = new Vector2(gkX, Pitch.Width / 2.0f);
            Ball.Position = new Vector3(gkX, Pitch.Width / 2.0f, 0.0f);
            Ball.CatchOrTackle(gk);
            RecordBallTouch(gk);
            _offsidePositions.Clear(); _offsideOffence=null; _offsideExemptKicker=gk;
            ActiveUserPlayer = gk;
            _kickoffProtectionTimer = 2.5f;
            GrantPossessionGrace(2.0f);
            Publish(new MatchEvent(MatchEventType.GoalKick, $"ضربة مرمى: {gk.Name}", CurrentZone, spatialPan: CalculateBallPan()),("restart.cause","goalLineExit"),("restart.missedShot",_lastOutWasShot));
        }
        else
        {
            var gk = AwayTeam.Players[0];
            float gkX = homeAtLeft ? Pitch.Length - 5.0f : 5.0f;
            gk.Position2D = new Vector2(gkX, Pitch.Width / 2.0f);
            Ball.Position = new Vector3(gkX, Pitch.Width / 2.0f, 0.0f);
            Ball.CatchOrTackle(gk);
            RecordBallTouch(gk);
            _offsidePositions.Clear(); _offsideOffence=null; _offsideExemptKicker=gk;
            _gkHoldTimer = 0.5f;
            Publish(new MatchEvent(MatchEventType.GoalKick, $"ضربة مرمى لصالح {AwayTeam.Name}.", CurrentZone, spatialPan: CalculateBallPan()),("restart.cause","goalLineExit"),("restart.missedShot",_lastOutWasShot));
        }
    }

    private void AwardCornerKick(bool forHomeTeam)
    {
        _isShotInProgress = false;
        float cornerY = Ball.Position.Y > Pitch.Width / 2.0f ? Pitch.Width - 1.0f : 1.0f;
        bool homeAttacksRight = UserAttackingRight;
        if (forHomeTeam)
        {
            float cornerX = homeAttacksRight ? Pitch.Length - 1.0f : 1.0f;
            var taker = (!HomeTeam.Players[9].IsSentOff && !HomeTeam.Players[9].IsOffPitch)
                ? HomeTeam.Players[9]
                : HomeTeam.Players.FirstOrDefault(p => !p.IsSentOff && !p.IsOffPitch && p.Role != PlayerRole.Goalkeeper)
                  ?? HomeTeam.Players.FirstOrDefault(p => !p.IsSentOff && !p.IsOffPitch)
                  ?? HomeTeam.Players[0];

            taker.Position2D = new Vector2(cornerX, cornerY);
            Ball.Position = new Vector3(cornerX, cornerY, 0.0f);
            Ball.CatchOrTackle(taker);
            RecordBallTouch(taker);
            _offsidePositions.Clear(); _offsideOffence=null; _offsideExemptKicker=taker;
            ActiveUserPlayer = taker;
            _kickoffProtectionTimer = 2.5f;
            GrantPossessionGrace(2.0f);
            _attackOrigin = "corner";
            Restart=new PendingRestart(RestartKind.Corner,HomeTeam,taker,taker.Position2D);_restartReadySeconds=0;
            Publish(new MatchEvent(MatchEventType.CornerKick, $"ضربة ركنية: {taker.Name}", CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: taker), ("restart.phase", "ready"), ("restart.cause", "defensiveTouch"));
        }
        else
        {
            float cornerX = homeAttacksRight ? 1.0f : Pitch.Length - 1.0f;
            var taker = (!AwayTeam.Players[9].IsSentOff && !AwayTeam.Players[9].IsOffPitch)
                ? AwayTeam.Players[9]
                : AwayTeam.Players.FirstOrDefault(p => !p.IsSentOff && !p.IsOffPitch && p.Role != PlayerRole.Goalkeeper)
                  ?? AwayTeam.Players.FirstOrDefault(p => !p.IsSentOff && !p.IsOffPitch)
                  ?? AwayTeam.Players[0];

            taker.Position2D = new Vector2(cornerX, cornerY);
            Ball.Position = new Vector3(cornerX, cornerY, 0.0f);
            Ball.CatchOrTackle(taker);
            RecordBallTouch(taker);
            _offsidePositions.Clear(); _offsideOffence=null; _offsideExemptKicker=taker;
            _attackOrigin="corner";
            Restart=new PendingRestart(RestartKind.Corner,AwayTeam,taker,taker.Position2D);_restartReadySeconds=0;
            Publish(new MatchEvent(MatchEventType.CornerKick,$"ضربة ركنية لصالح {AwayTeam.Name}",CurrentZone,subjectPlayer:taker),
                ("restart.phase","ready"),("restart.cause","defensiveTouch"));
        }
    }

    private void AwardThrowIn(bool forHomeTeam)
    {
        _isShotInProgress = false;
        float touchX = Math.Clamp(Ball.Position.X, 5.0f, Pitch.Length - 5.0f);
        float touchY = Ball.Position.Y > Pitch.Width / 2.0f ? Pitch.Width - 0.5f : 0.5f;

        if (forHomeTeam)
        {
            var taker = HomeTeam.Players.Where(p => !p.IsSentOff && !p.IsOffPitch)
                .OrderBy(p => Vector2.Distance(p.Position2D, new Vector2(touchX, touchY)))
                .FirstOrDefault() ?? HomeTeam.Players[0];
            taker.Position2D = new Vector2(touchX, touchY);
            Ball.Position = new Vector3(touchX, touchY, 0.0f);
            Ball.CatchOrTackle(taker);
            RecordBallTouch(taker);
            _offsidePositions.Clear(); _offsideOffence=null; _offsideExemptKicker=taker;
            ActiveUserPlayer = taker;
            _kickoffProtectionTimer = 2.5f;
            GrantPossessionGrace(2.0f);
            Publish(new MatchEvent(MatchEventType.ThrowIn, $"رمية تماس: {taker.Name}", CurrentZone, spatialPan: CalculateBallPan()));
        }
        else
        {
            var taker = AwayTeam.Players.Where(p => !p.IsSentOff && !p.IsOffPitch)
                .OrderBy(p => Vector2.Distance(p.Position2D, new Vector2(touchX, touchY)))
                .FirstOrDefault() ?? AwayTeam.Players[0];
            taker.Position2D = new Vector2(touchX, touchY);
            Ball.Position = new Vector3(touchX, touchY, 0.0f);
            Ball.CatchOrTackle(taker);
            RecordBallTouch(taker);
            _offsidePositions.Clear(); _offsideOffence=null; _offsideExemptKicker=taker;
            var teammate = AwayTeam.Players.FirstOrDefault(p => p != taker && !p.IsSentOff && !p.IsOffPitch && p.Role != PlayerRole.Goalkeeper);
            if (teammate != null)
            {
                Vector2 throwDir = Vector2.Normalize(teammate.Position2D - taker.Position2D);
                SnapshotOffsideAtKick(taker);
                Ball.Kick(throwDir, 14.0f, 1.0f, taker);
            }
            Publish(new MatchEvent(MatchEventType.ThrowIn, $"رمية تماس لصالح {AwayTeam.Name}.", CurrentZone, spatialPan: CalculateBallPan()));
        }
    }

    /// <summary>
    /// استئناف اللعب وركلة البداية (Kickoff) فقط بعد انتهاء صرخة المعلق وإعلان قارئ الشاشة لمسجل الهدف
    /// </summary>
    public void ResumePlayAfterGoalCelebration(Team kickoffTeam)
    {
        Ball.Position = new Vector3(Pitch.CenterSpot.X, Pitch.CenterSpot.Y, 0.0f);
        Ball.Velocity = Vector3.Zero;
        var kicker = kickoffTeam.Players.FirstOrDefault(p => !p.IsSentOff && !p.IsOffPitch && p.Role != PlayerRole.Goalkeeper)
            ?? kickoffTeam.Players.FirstOrDefault(p => !p.IsSentOff && !p.IsOffPitch)
            ?? kickoffTeam.Players[0];

        kicker.Position2D = new Vector2(Pitch.CenterSpot.X, Pitch.CenterSpot.Y);
        Ball.CatchOrTackle(kicker);
        LastKicker = kicker;
        PreviousKicker = null;

        if (kickoffTeam == HomeTeam) ActiveUserPlayer = kicker;
        _kickoffProtectionTimer = 3.5f;
        GrantPossessionGrace(2.0f);
        State = MatchState.InPlay;
    }
}

