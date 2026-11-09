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
public class MatchEngine
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
    private float _celebrationTimer = 0.0f;
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
        EventBus.Publish(new MatchEvent(MatchEventType.MatchPaused, "إيقاف مؤقت"));
    }

    public void ResumeMatch()
    {
        State = MatchState.InPlay;
        HomeTeam.ResetStoppageWindowFlag();
        AwayTeam.ResetStoppageWindowFlag();
    }

    public bool CanMakeSubstitution(Team team) => team.CanMakeSubstitution(State == MatchState.HalfTime);

    public bool MakeSubstitution(Team team, Player playerOut, Player playerIn)
    {
        bool success = team.MakeSubstitution(playerOut, playerIn, isHalfTime: State == MatchState.HalfTime, isMatchActive: true);
        if (success)
        {
            if (ActiveUserPlayer == playerOut)
            {
                ActiveUserPlayer = playerIn;
            }
            EventBus.Publish(new MatchEvent(MatchEventType.SubstitutionMade, $"تبديل: خروج {playerOut.Name} ودخول {playerIn.Name} ({team.SubstitutionsUsed}/{team.MaxSubstitutions})"));
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
        EventBus.Publish(new MatchEvent(MatchEventType.HalfTime, halfTimeMsg));
    }

    public void TriggerExtraTime()
    {
        IsExtraTime = true;
        HomeTeam.IsExtraTime = true;
        AwayTeam.IsExtraTime = true;
        MatchTimeMinutes = 90.0f;
        EventBus.Publish(new MatchEvent(MatchEventType.ExtraTimeStarted, "نهاية الوقت الأصلي بالتعادل! الاحتكام للأشواط الإضافية (+1 تبديل إضافي وفترة توقف رابعة)"));
    }

    public void ReportTacticalRadar()
    {
        if (Ball.Owner != null && HomeTeam.Players.Contains(Ball.Owner))
        {
            float distToGoal = UserAttackingRight ? (Pitch.Length - Ball.Position2D.X) : Ball.Position2D.X;
            string radarMsg = $"{Ball.Owner.Name}، {CurrentZone.ToArabicAnnouncement()}، المرمى: {(int)distToGoal} م";
            EventBus.Publish(new MatchEvent(MatchEventType.TacticalRadarInfo, radarMsg, CurrentZone));
        }
        else if (Ball.Owner != null && AwayTeam.Players.Contains(Ball.Owner))
        {
            float distToOurGoal = UserAttackingRight ? Ball.Position2D.X : (Pitch.Length - Ball.Position2D.X);
            string radarMsg = $"{Ball.Owner.Name} ({AwayTeam.Name})، {CurrentZone.ToArabicAnnouncement()}، مرماك: {(int)distToOurGoal} م";
            EventBus.Publish(new MatchEvent(MatchEventType.TacticalRadarInfo, radarMsg, CurrentZone));
        }
        else
        {
            EventBus.Publish(new MatchEvent(MatchEventType.TacticalRadarInfo, $"كرة حرة في {CurrentZone.ToArabicAnnouncement()}.", CurrentZone));
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
            _celebrationTimer -= deltaTime;
            if (_celebrationTimer <= 0.0f)
            {
                // استئناف اللعب وركلة البداية من منتصف الملعب لصالح الفريق المستقبل للهدف
                Team kickoffTeam = (HomeTeam.Score > _lastRecordedHomeScore) ? AwayTeam : HomeTeam;
                _lastRecordedHomeScore = HomeTeam.Score;
                _lastRecordedAwayScore = AwayTeam.Score;

                ResumePlayAfterGoalCelebration(kickoffTeam);
                EventBus.Publish(new MatchEvent(MatchEventType.BallKicked, $"صافرة الحكم وركلة البداية لصالح {kickoffTeam.Name} من دائرة منتصف الملعب.", PitchZone.Midfield));
            }
            return;
        }

        if (State != MatchState.InPlay) return;

        if (_kickoffProtectionTimer > 0.0f)
        {
            _kickoffProtectionTimer -= deltaTime;
        }

        // حساب سرعة مرور وقت المباراة وفق إعدادات المستخدم
        float durationMinutes = Math.Max(1, MatchSettings.Current.MatchDurationMinutes);
        float minuteScale = 90.0f / (durationMinutes * 60.0f);
        MatchTimeMinutes += deltaTime * minuteScale;

        // إعلان الوقت بدل الضائع للشوط الأول عند 44:30
        if (!_stoppage1stHalfAnnounced && MatchTimeMinutes >= 44.5f && !HalfTimeReached)
        {
            _stoppage1stHalfAnnounced = true;
            EventBus.Publish(new MatchEvent(MatchEventType.StoppageTimeAnnounced, $"الحكم يعلن عن {_stoppageMinutes1st} دقائق كوقت بدل ضائع للشوط الأول", CurrentZone));
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
            EventBus.Publish(new MatchEvent(MatchEventType.StoppageTimeAnnounced, $"الحكم يعلن عن {_stoppageMinutes2nd} دقائق كوقت بدل ضائع للشوط الثاني", CurrentZone));
        }

        if (!IsExtraTime)
        {
            if (MatchTimeMinutes >= 90.0f + _stoppageMinutes2nd)
            {
                if (!_fullTimeAnnounced)
                {
                    _fullTimeAnnounced = true;
                    string ftMsg = $"صافرة نهاية المباراة! النتيجة النهائية: {HomeTeam.Name} {HomeTeam.Score} - {AwayTeam.Score} {AwayTeam.Name}";
                    EventBus.Publish(new MatchEvent(MatchEventType.FullTime, ftMsg, CurrentZone));
                }

                if (HomeTeam.Score == AwayTeam.Score)
                {
                    if (MatchSettings.Current.ExtraTimeEnabled)
                    {
                        TriggerExtraTime();
                    }
                    else if (MatchSettings.Current.PenaltyShootoutEnabled)
                    {
                        State = MatchState.PenaltyShootout;
                        EventBus.Publish(new MatchEvent(MatchEventType.PenaltyShootoutStarted, "نهاية الوقت الأصلي بالتعادل! الاحتكام مباشرة لركلات الترجيح"));
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
            if (MatchTimeMinutes >= 120.0f)
            {
                if (HomeTeam.Score == AwayTeam.Score && MatchSettings.Current.PenaltyShootoutEnabled)
                {
                    State = MatchState.PenaltyShootout;
                    EventBus.Publish(new MatchEvent(MatchEventType.PenaltyShootoutStarted, "نهاية الوقت الإضافي بالتعادل! الاحتكام لركلات الترجيح"));
                    return;
                }
                else
                {
                    State = MatchState.GameOver;
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

        ProcessUserInput(deltaTime);
        UpdatePlayersAI(deltaTime);
        Ball.Update(deltaTime);
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
            EventBus.Publish(new MatchEvent(MatchEventType.MatchPaused, "إيقاف مؤقت"));
            return;
        }

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

        if (hasPossession && Ball.Owner == ActiveUserPlayer)
        {
            if (actions.Contains(GameAction.ShortPass))
            {
                ExecutePass(moveDir, isThroughBall: false);
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

    private void RecordBallTouch(Player kicker)
    {
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

    public void ExecutePass(Vector2 aimDir, bool isThroughBall = false)
    {
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
        
        Ball.Kick(kickDirection, passSpeed, verticalSpeed: 0.15f, ActiveUserPlayer);

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

        string passDesc = isThroughBall 
            ? $"بينية إلى {bestTarget.Name} في {CurrentZone.ToArabicAnnouncement()}" 
            : $"تمريرة إلى {bestTarget.Name} في {CurrentZone.ToArabicAnnouncement()}";
        EventBus.Publish(new MatchEvent(MatchEventType.BallKicked, passDesc, CurrentZone, spatialPan: CalculateBallPan()));
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

    private void ExecuteCross()
    {
        _kickoffProtectionTimer = 0.0f;
        RecordBallTouch(ActiveUserPlayer);
        Vector2 boxCenter = UserAttackingRight 
            ? new(Pitch.Length - 10.0f, Pitch.Width / 2.0f)
            : new(10.0f, Pitch.Width / 2.0f);

        // توجيه العرضية وفقاً لزاوية الأسهم للأعلى أو للأسفل (Up/Down Arrow Aiming)
        float crossOffset = ActiveUserPlayer.FacingDirection.Y * 16.0f;
        boxCenter.Y = Math.Clamp(boxCenter.Y + crossOffset, Pitch.GoalYMin - 6.0f, Pitch.GoalYMax + 6.0f);

        Vector2 crossDir = boxCenter - ActiveUserPlayer.Position2D;
        Ball.Kick(crossDir, horizontalSpeed: 19.0f, verticalSpeed: 6.5f, ActiveUserPlayer);
        EventBus.Publish(new MatchEvent(MatchEventType.BallKicked, $"عرضية عالية من {ActiveUserPlayer.Name} داخل منطقة الجزاء!", CurrentZone, spatialPan: CalculateBallPan()));
    }

    public void ExecuteShot()
    {
        _kickoffProtectionTimer = 0.0f;
        RecordBallTouch(ActiveUserPlayer);
        Vector2 targetGoal = UserAttackingRight ? Pitch.RightGoalCenter : Pitch.LeftGoalCenter;

        // توجيه زاوية التسديد نحو المقص العلوي أو السفلي وفقاً لاتجاه اللاعب والأسهم
        if (ActiveUserPlayer.FacingDirection.Y < -0.15f)
        {
            // زاوية علوية نحو المقص
            targetGoal.Y = Pitch.GoalYMin + 1.2f;
        }
        else if (ActiveUserPlayer.FacingDirection.Y > 0.15f)
        {
            // زاوية سفلية نحو المقص
            targetGoal.Y = Pitch.GoalYMax - 1.2f;
        }
        else
        {
            targetGoal.Y = (Pitch.GoalYMin + Pitch.GoalYMax) / 2.0f;
        }

        // تطبيق ضبط زاوية التوجيه الدقيق (Up/Down arrow aiming offset)
        float fineAim = ActiveUserPlayer.FacingDirection.Y * (Pitch.GoalYMax - Pitch.GoalYMin) * 0.35f;
        targetGoal.Y = Math.Clamp(targetGoal.Y + fineAim, Pitch.GoalYMin + 0.5f, Pitch.GoalYMax - 0.5f);

        float spread = (100 - ActiveUserPlayer.Shooting) * 0.03f * ((float)Random.Shared.NextDouble() * 2.0f - 1.0f);
        targetGoal.Y += spread;

        Vector2 shotDir = targetGoal - ActiveUserPlayer.Position2D;
        float shotPower = 30.0f + (ActiveUserPlayer.Shooting * 0.12f);
        Ball.Kick(shotDir, shotPower, verticalSpeed: 1.8f, ActiveUserPlayer);
        _isShotInProgress = true;
        EventBus.Publish(new MatchEvent(MatchEventType.ShotTaken, $"تسديدة قوية من {ActiveUserPlayer.Name} نحو المرمى!", CurrentZone, spatialPan: CalculateBallPan()));
    }

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
            EventBus.Publish(new MatchEvent(MatchEventType.TacticalRadarInfo, $"{closest.Name} - {GetArabicPositionName(closest.CurrentAssignedPosition)}"));
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
                EventBus.Publish(new MatchEvent(MatchEventType.TackleAttempted, "انزلاق", CurrentZone, spatialPan: CalculateBallPan()));
            }

            string outOfReachMsg = carrier != null 
                ? $"بعيد عن حامل الكرة: {distToOpponent:F1} متر"
                : $"بعيد: {distToBall:F1} م";
            EventBus.Publish(new MatchEvent(MatchEventType.TacticalRadarInfo, outOfReachMsg));
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
                EventBus.Publish(new MatchEvent(MatchEventType.UserTackleSuccessful, successDesc, CurrentZone, spatialPan: CalculateBallPan()));
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
                EventBus.Publish(new MatchEvent(MatchEventType.FoulCommitted, foulDesc, CurrentZone, spatialPan: CalculateBallPan()));

                // فحص احتساب ركلة جزاء إذا كانت العرقلة داخل منطقة جزاء الفريق المدافع
                bool isDefendingBox = UserAttackingRight
                    ? carrier.Position2D.X <= Pitch.PenaltyBoxDepth
                    : carrier.Position2D.X >= Pitch.Length - Pitch.PenaltyBoxDepth;
                bool isInBox = isDefendingBox && carrier.Position2D.Y >= Pitch.PenaltyYMin && carrier.Position2D.Y <= Pitch.PenaltyYMax;
                if (isInBox)
                {
                    EventBus.Publish(new MatchEvent(MatchEventType.PenaltyAwarded, "ركلة جزاء!", CurrentZone, spatialPan: CalculateBallPan()));
                }

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
            EventBus.Publish(new MatchEvent(MatchEventType.UserTackleSuccessful, recoverDesc, CurrentZone, spatialPan: CalculateBallPan()));
        }
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
    }

    public void IssueCard(Player player, bool isDirectRed, string reason = "")
    {
        Team playerTeam = HomeTeam.Players.Contains(player) ? HomeTeam : AwayTeam;

        if (isDirectRed)
        {
            DismissPlayer(player, playerTeam);

            string desc = string.IsNullOrEmpty(reason)
                ? $"بطاقة حمراء وطرد مباشر لـ {player.Name}! {playerTeam.Name} يكمل بـ 10 لاعبين."
                : $"بطاقة حمراء وطرد مباشر لـ {player.Name} ({reason})! {playerTeam.Name} يكمل بـ 10 لاعبين.";
            EventBus.Publish(new MatchEvent(MatchEventType.RedCard, desc, CurrentZone, spatialPan: CalculateBallPan()));
        }
        else
        {
            player.YellowCards++;
            if (player.YellowCards >= 2)
            {
                DismissPlayer(player, playerTeam);

                string desc = $"إنذار ثانٍ وبطاقة حمراء وطرد لـ {player.Name}! {playerTeam.Name} يكمل بـ 10 لاعبين.";
                EventBus.Publish(new MatchEvent(MatchEventType.RedCard, desc, CurrentZone, spatialPan: CalculateBallPan()));
            }
            else
            {
                string desc = string.IsNullOrEmpty(reason)
                    ? $"بطاقة صفراء وإنذار أول للاعب {player.Name}."
                    : $"بطاقة صفراء وإنذار أول للاعب {player.Name} ({reason}).";
                EventBus.Publish(new MatchEvent(MatchEventType.YellowCard, desc, CurrentZone, spatialPan: CalculateBallPan()));
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
            EventBus.Publish(new MatchEvent(MatchEventType.PlayerInjured, desc, CurrentZone, spatialPan: CalculateBallPan()));
        }
        else if (status == PlayerInjuryStatus.SevereInjury)
        {
            string desc = $"إصابة بالغة للاعب {player.Name}! تستدعي الخروج الإجباري.";
            EventBus.Publish(new MatchEvent(MatchEventType.PlayerInjured, desc, CurrentZone, spatialPan: CalculateBallPan()));

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
                EventBus.Publish(new MatchEvent(MatchEventType.PlayerInjured, exhaustedDesc, CurrentZone, spatialPan: CalculateBallPan()));
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
            Ball.CatchOrTackle(receiver);
            RecordBallTouch(receiver);
            GrantPossessionGrace(1.4f);
            ActiveUserPlayer = receiver;
            ActivePassTransit.IsActive = false;
            ActivePassTransit = null;

            string zoneDesc = CurrentZone.ToArabicAnnouncement();
            string receptionMsg = $"استلم {receiver.Name} الكرة بنجاح في {zoneDesc}.";
            EventBus.Publish(new MatchEvent(
                MatchEventType.BallTrapped,
                receptionMsg,
                CurrentZone,
                spatialPan: CalculateBallPan()
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
                        Ball.Kick(clearDir, 26.0f, 4.0f, opponent);
                        RecordBallTouch(opponent);
                        EventBus.Publish(new MatchEvent(MatchEventType.GoalKick, $"حارس مرمى {AwayTeam.Name} يشتت الكرة عالية للأمام نحو نصف الملعب.", CurrentZone, spatialPan: CalculateBallPan()));
                    }
                }
                else if (Vector2.Distance(opponent.Position2D, ball2D) < 2.0f && Ball.Position.Z < Pitch.GoalHeight)
                {
                    bool wasShot = _isShotInProgress;
                    _isShotInProgress = false;
                    _gkHoldTimer = 0.0f;
                    Ball.CatchOrTackle(opponent);
                    RecordBallTouch(opponent);
                    if (ActivePassTransit != null)
                    {
                        ActivePassTransit.IsActive = false;
                        ActivePassTransit = null;
                    }
                    if (wasShot)
                    {
                        EventBus.Publish(new MatchEvent(MatchEventType.MissedChance, "تصدي رائع من حارس المرمى!", CurrentZone, spatialPan: CalculateBallPan()));
                    }
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
                    Ball.Kick(attackDir, 26.0f, 1.2f, opponent);
                    EventBus.Publish(new MatchEvent(MatchEventType.BallKicked, $"تسديدة خطيرة من {opponent.Name} لاعب الخصم!", CurrentZone, spatialPan: CalculateBallPan()));
                }
                else if (!ActiveUserPlayer.IsSentOff && !ActiveUserPlayer.IsOffPitch && Vector2.Distance(ActiveUserPlayer.Position2D, opponent.Position2D) < 2.5f && new Random().Next(100) < 5)
                {
                    var openTeammate = AwayTeam.Players.FirstOrDefault(p => p != opponent && p.Role != PlayerRole.Goalkeeper && 
                        (UserAttackingRight ? p.Position2D.X < opponent.Position2D.X : p.Position2D.X > opponent.Position2D.X));
                    if (openTeammate != null)
                    {
                        Vector2 passDir = Vector2.Normalize(openTeammate.Position2D - opponent.Position2D);
                        Ball.Kick(passDir, 18.0f, 0.2f, opponent);
                        EventBus.Publish(new MatchEvent(MatchEventType.BallKicked, $"تمريرة من {opponent.Name} إلى {openTeammate.Name}", CurrentZone, spatialPan: CalculateBallPan()));
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
                opponent.Position2D += pressDir * (opponent.Speed * speedMult) * deltaTime;

                if (distToBall < 1.3f && Ball.Owner != opponent && (Ball.Owner == null || !AwayTeam.Players.Contains(Ball.Owner)))
                {
                    // فحص حماية الاستحواذ وفترة تهدئة قطع الكرة لمنع قطع الكرة المتكرر بسرعة غير بشرية
                    if (_possessionGraceTimer > 0.0f || _aiTackleCooldownTimer > 0.0f)
                    {
                        // الخصم يضغط ويلاحق فقط دون أن يقطع الكرة فورياً
                    }
                    else
                    {
                        int aiTackleChance = diff switch
                        {
                            DifficultyLevel.Easy => 15,
                            DifficultyLevel.Normal => 28,
                            DifficultyLevel.Hard => 45,
                            DifficultyLevel.Legendary => 65,
                            _ => 28
                        };

                        // تهدئة محاولة الاستخلاص للذكاء الاصطناعي لمنع تكرار المحاولة كل إطار (50Hz)
                        _aiTackleCooldownTimer = 1.6f;

                        if (new Random().Next(100) < aiTackleChance)
                        {
                            var previousOwner = Ball.Owner;
                            Ball.CatchOrTackle(opponent);
                            RecordBallTouch(opponent);
                            _aiTackleCooldownTimer = 2.0f;
                            if (ActivePassTransit != null)
                            {
                                ActivePassTransit.IsActive = false;
                                ActivePassTransit = null;
                            }
                            string desc = $"قطع كمبيوتر {opponent.Name} الكرة منك! الهجمة مع {AwayTeam.Name} في {CurrentZone.ToArabicAnnouncement()}.";
                            EventBus.Publish(new MatchEvent(MatchEventType.OpponentTackleSuccessful, desc, CurrentZone, spatialPan: CalculateBallPan()));
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
                        Ball.Kick(clearDir, 26.0f, 4.0f, teammate);
                        RecordBallTouch(teammate);
                        EventBus.Publish(new MatchEvent(MatchEventType.GoalKick, $"حارسك {teammate.Name} يشتت الكرة عالية للأمام نحو نصف الملعب.", CurrentZone, spatialPan: CalculateBallPan()));
                    }
                }
                else if (Vector2.Distance(teammate.Position2D, ball2D) < 2.5f && (Ball.Owner == null || AwayTeam.Players.Contains(Ball.Owner)))
                {
                    _gkHoldTimer = 0.0f;
                    Ball.CatchOrTackle(teammate);
                    RecordBallTouch(teammate);
                    ActiveUserPlayer = teammate;
                    if (ActivePassTransit != null)
                    {
                        ActivePassTransit.IsActive = false;
                        ActivePassTransit = null;
                    }
                    EventBus.Publish(new MatchEvent(MatchEventType.GoalKick, $"حارس مرماك {teammate.Name} يمسك بالكرة بأمان.", CurrentZone, spatialPan: CalculateBallPan()));
                }
                continue;
            }

            Vector2 shiftedHome = teammate.HomePosition + new Vector2((ball2D.X - 52.5f) * 0.4f, 0);
            teammate.Position2D = Vector2.Lerp(teammate.Position2D, shiftedHome, deltaTime * 1.0f);

            if (Ball.State == BallState.Free && Ball.Position.Z <= 1.5f && !IsPassInTransit && Vector2.Distance(teammate.Position2D, ball2D) < 1.8f)
            {
                Ball.CatchOrTackle(teammate);
                RecordBallTouch(teammate);
                ActiveUserPlayer = teammate;
                if (ActivePassTransit != null)
                {
                    ActivePassTransit.IsActive = false;
                    ActivePassTransit = null;
                }
                string zoneDesc = CurrentZone.ToArabicAnnouncement();
                string receptionMsg = $"استلم {teammate.Name} الكرة في {zoneDesc}.";
                EventBus.Publish(new MatchEvent(MatchEventType.BallTrapped, receptionMsg, CurrentZone, spatialPan: CalculateBallPan()));
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
                    Ball.CatchOrTackle(closestPlayer);
                    RecordBallTouch(closestPlayer);
                    if (ActivePassTransit != null)
                    {
                        ActivePassTransit.IsActive = false;
                        ActivePassTransit = null;
                    }

                    if (HomeTeam.Players.Contains(closestPlayer))
                    {
                        ActiveUserPlayer = closestPlayer;
                        EventBus.Publish(new MatchEvent(MatchEventType.BallTrapped, $"الكرة أصبحت مع لاعبك {closestPlayer.Name} في {CurrentZone.ToArabicAnnouncement()}.", CurrentZone, spatialPan: CalculateBallPan()));
                    }
                    else
                    {
                        EventBus.Publish(new MatchEvent(MatchEventType.BallKicked, $"الكرة مع {closestPlayer.Name} لاعب {AwayTeam.Name} في {CurrentZone.ToArabicAnnouncement()}.", CurrentZone, spatialPan: CalculateBallPan()));
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
            EventBus.Publish(new MatchEvent(MatchEventType.BallZoneChanged, "تغير منطقة اللعب", CurrentZone));
        }
    }

    public void TriggerGoalkeeperSave(string desc = "تصدي رائع وخيالي من حارس المرمى!") =>
        EventBus.Publish(new MatchEvent(MatchEventType.GoalkeeperSave, desc, CurrentZone, spatialPan: CalculateBallPan()));

    public void TriggerWoodworkHit(string desc = "في القائم! العارضة تمنع هدفاً محققاً!") =>
        EventBus.Publish(new MatchEvent(MatchEventType.WoodworkHit, desc, CurrentZone, spatialPan: CalculateBallPan()));

    public void TriggerPenaltyAwarded(string desc = "ركلة جزاء!") =>
        EventBus.Publish(new MatchEvent(MatchEventType.PenaltyAwarded, desc, CurrentZone, spatialPan: CalculateBallPan()));

    private void CheckRefereeRules()
    {
        // 0. فحص تصدي الحارس أو ارتطام الكرة بالقائم/العارضة أثناء التسديد
        if (_isShotInProgress)
        {
            float targetGoalX = UserAttackingRight ? Pitch.Length : 0.0f;
            float distToGoalX = Math.Abs(Ball.Position.X - targetGoalX);

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
                    EventBus.Publish(new MatchEvent(MatchEventType.WoodworkHit, "في القائم! العارضة تمنع هدفاً محققاً!", CurrentZone, spatialPan: CalculateBallPan()));
                    return;
                }
            }

            // فحص تصدي حارس المرمى (Goalkeeper Save)
            if (distToGoalX <= 3.0f && Ball.Position.Y >= Pitch.GoalYMin - 0.5f && Ball.Position.Y <= Pitch.GoalYMax + 0.5f && Ball.Position.Z <= Pitch.GoalHeight)
            {
                var defendingGk = UserAttackingRight ? AwayTeam.Players[0] : HomeTeam.Players[0];
                float gkDist = Vector2.Distance(new Vector2(Ball.Position.X, Ball.Position.Y), defendingGk.Position2D);
                if (gkDist <= 4.2f && Random.Shared.NextDouble() < 0.35)
                {
                    _isShotInProgress = false;
                    Ball.Velocity = new Vector3(-Ball.Velocity.X * 0.35f, ((float)Random.Shared.NextDouble() - 0.5f) * 6.0f, 0.5f);
                    EventBus.Publish(new MatchEvent(MatchEventType.GoalkeeperSave, "تصدي رائع وخيالي من حارس المرمى!", CurrentZone, spatialPan: CalculateBallPan()));
                    return;
                }
            }
        }

        // 1. فحص الهدف
        if (Pitch.IsGoal(Ball.Position, out bool forTeamAttackingRight))
        {
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
            bool isOwnGoal = (forHomeTeam && AwayTeam.Players.Contains(scorer)) ||
                             (!forHomeTeam && HomeTeam.Players.Contains(scorer));

            Player? assister = (PreviousKicker != null && PreviousKicker != scorer &&
                                scoringTeam.Players.Contains(PreviousKicker)) ? PreviousKicker : null;

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

            EventBus.Publish(new MatchEvent(
                MatchEventType.GoalScored,
                $"هدف لصالح {scoringTeam.Name}!",
                CurrentZone,
                goalDetails
            ));

            // 1. فحص الهاتريك (Hat-Trick)
            if (!isOwnGoal)
            {
                scorer.MatchGoalsCount++;
                if (scorer.MatchGoalsCount == 3)
                {
                    EventBus.Publish(new MatchEvent(
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
                EventBus.Publish(new MatchEvent(
                    MatchEventType.DramaticComeback,
                    $"ريمونتادا جنونية وغير متوقعة لصالح {HomeTeam.Name}!",
                    CurrentZone,
                    goalDetails
                ));
            }
            else if (!forHomeTeam && _awayTeamWasTrailing && AwayTeam.Score > HomeTeam.Score && MatchTimeMinutes >= 70.0f)
            {
                _awayTeamWasTrailing = false;
                EventBus.Publish(new MatchEvent(
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

            if (isGoalLine)
            {
                if (_isShotInProgress)
                {
                    _isShotInProgress = false;
                    EventBus.Publish(new MatchEvent(MatchEventType.MissedChance, "فرصة ضائعة تمر بجوار القائم وتخرج خارج المرمى!", CurrentZone, spatialPan: CalculateBallPan()));
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
            ActiveUserPlayer = gk;
            _kickoffProtectionTimer = 2.5f;
            GrantPossessionGrace(2.0f);
            EventBus.Publish(new MatchEvent(MatchEventType.GoalKick, $"ضربة مرمى: {gk.Name}", CurrentZone, spatialPan: CalculateBallPan()));
        }
        else
        {
            var gk = AwayTeam.Players[0];
            float gkX = homeAtLeft ? Pitch.Length - 5.0f : 5.0f;
            gk.Position2D = new Vector2(gkX, Pitch.Width / 2.0f);
            Ball.Position = new Vector3(gkX, Pitch.Width / 2.0f, 0.0f);
            Ball.CatchOrTackle(gk);
            RecordBallTouch(gk);
            _gkHoldTimer = 0.5f;
            EventBus.Publish(new MatchEvent(MatchEventType.GoalKick, $"ضربة مرمى لصالح {AwayTeam.Name}.", CurrentZone, spatialPan: CalculateBallPan()));
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
            ActiveUserPlayer = taker;
            _kickoffProtectionTimer = 2.5f;
            GrantPossessionGrace(2.0f);
            EventBus.Publish(new MatchEvent(MatchEventType.CornerKick, $"ضربة ركنية: {taker.Name}", CurrentZone, spatialPan: CalculateBallPan()));
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
            Vector2 boxCenter = homeAttacksRight ? new Vector2(8.0f, Pitch.Width / 2.0f) : new Vector2(Pitch.Length - 8.0f, Pitch.Width / 2.0f);
            Vector2 kickDir = Vector2.Normalize(boxCenter - taker.Position2D);
            Ball.Kick(kickDir, 20.0f, 5.0f, taker);
            EventBus.Publish(new MatchEvent(MatchEventType.CornerKick, $"ضربة ركنية لصالح {AwayTeam.Name} يرفعها داخل منطقة الجزاء!", CurrentZone, spatialPan: CalculateBallPan()));
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
            ActiveUserPlayer = taker;
            _kickoffProtectionTimer = 2.5f;
            GrantPossessionGrace(2.0f);
            EventBus.Publish(new MatchEvent(MatchEventType.ThrowIn, $"رمية تماس: {taker.Name}", CurrentZone, spatialPan: CalculateBallPan()));
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
            var teammate = AwayTeam.Players.FirstOrDefault(p => p != taker && !p.IsSentOff && !p.IsOffPitch && p.Role != PlayerRole.Goalkeeper);
            if (teammate != null)
            {
                Vector2 throwDir = Vector2.Normalize(teammate.Position2D - taker.Position2D);
                Ball.Kick(throwDir, 14.0f, 1.0f, taker);
            }
            EventBus.Publish(new MatchEvent(MatchEventType.ThrowIn, $"رمية تماس لصالح {AwayTeam.Name}.", CurrentZone, spatialPan: CalculateBallPan()));
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
