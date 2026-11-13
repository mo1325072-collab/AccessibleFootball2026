using System;
using System.Threading;
using System.Threading.Tasks;
using AccessibleFootball2026.Core;

namespace AccessibleFootball2026.Audio;

/// <summary>
/// منسق الصوت الذكي:
/// 1. خفض صوت الجماهير (Audio Ducking).
/// 2. منع التكرار مع المعلق.
/// 3. بعد الهدف: انتظار انتهاء صرخة المعلق الحماسية، ثم إعلان قارئ الشاشة لمسجل الهدف والأسيست، ثم استئناف اللعب.
/// 4. طابور التحديث الأخير للكرة (Latest-State Drop Queue).
/// </summary>
public class SmartAudioCoordinator
{
    private readonly IScreenReader _screenReader;
    private readonly object _lock = new();
    
    private PitchZone? _pendingZone = null;
    private DateTime _lastZoneChangeTime = DateTime.MinValue;
    private DateTime _lastTackleSpeechTime = DateTime.MinValue;
    private readonly TimeSpan _throttleWindow = TimeSpan.FromMilliseconds(400);
    private readonly TimeSpan _tackleThrottle = TimeSpan.FromMilliseconds(1600);

    /// <summary>
    /// بروتوكول الصمت التام لقارئ الشاشة أثناء اللعب النشط:
    /// صمت بنسبة 100% لجميع الأحداث الروتينية أثناء الجري والتمرير، ونطق الكلام فقط في القوائم أو عند طلب الرادار بـ Space
    /// </summary>
    public static bool SilenceLiveMatchSpeech { get; set; } = true;

    public event Action<float>? OnCrowdVolumeDucking;
    public event Action? OnGoalSequenceCompleted; // إشارة للمحرك باستئناف اللعب بعد اكتمال إعلان الهدف

    // يجيب: هل قناة المعلق حرة تماماً (لا مقطع يُشغَّل ولا جملة في الطابور)؟
    // يحقن من نافذة اللعبة نحو CommentaryEngine.IsIdle؛ عند غيابه يُكتفى بحالة القناة اللحظية.
    private readonly Func<bool>? _isCommentaryIdle;

    public SmartAudioCoordinator(IScreenReader screenReader, Func<bool>? isCommentaryIdle = null)
    {
        _screenReader = screenReader;
        _isCommentaryIdle = isCommentaryIdle;
    }

    public void NotifyBallZoneChanged(PitchZone newZone)
    {
        lock (_lock)
        {
            _pendingZone = newZone;
            _lastZoneChangeTime = DateTime.UtcNow;

            // أثناء سير المباراة الحي، يتم إسكات قارئ الشاشة تماماً لتجنب التلوث الصوتي
            // والمعلومات التكتيكية متاحة فوراً عند الطلب عبر مفتاح Space
            if (SilenceLiveMatchSpeech)
            {
                return;
            }

            Task.Delay(_throttleWindow).ContinueWith(_ =>
            {
                lock (_lock)
                {
                    if (_pendingZone.HasValue && (DateTime.UtcNow - _lastZoneChangeTime) >= _throttleWindow)
                    {
                        var zoneToAnnounce = _pendingZone.Value;
                        _pendingZone = null;
                        SpeakWithDucking(zoneToAnnounce.ToArabicAnnouncement());
                    }
                }
            });
        }
    }

    public void HandleMatchEvent(MatchEvent matchEvent)
    {
        // 1. الرادار التكتيكي اللحظي على الطلب (Space key)
        if (matchEvent.Type == MatchEventType.TacticalRadarInfo)
        {
            // إذا كان الحدث ناتجاً عن تبديل المدافع (يحتوي على "-") أثناء تفعيل الصمت التام
            // يتم تشغيل النغمة اللمسية الخاطفة فقط بدون نطق أي كلام إطلاقاً
            if (SilenceLiveMatchSpeech && matchEvent.Context.TryGet("radar.reason", out var reason) && reason == "playerSwitch")
            {
                SoundEffectsPlayer.PlayPlayerSwitch();
                return;
            }

            if (matchEvent.Context.TryGet("radar.reason", out var tackleReason) && tackleReason == "tackleMissed")
            {
                SoundEffectsPlayer.PlayTackleMissed();
                if (SilenceLiveMatchSpeech)
                {
                    return;
                }
            }

            SpeakWithDucking(matchEvent.Description, explicitRequest: true);
            return;
        }

        // 2. معالجة خاصة لحدث الهدف: اهتزاز الشباك وانفجار هتاف الجماهير وصرخة المعلق
        if (matchEvent.Type == MatchEventType.GoalScored && matchEvent.GoalData != null)
        {
            SoundEffectsPlayer.PlayGoalNet();
            SoundEffectsPlayer.PlayCrowdGoal();
            HandleGoalCelebrationSequence(matchEvent.GoalData, matchEvent.HandledByCommentary);
            return;
        }

        // 3. توجيه المؤثرات الصوتية ونغمات التنبيه التكتيكية الموسيقية الثمانية (SFX & Earcons)
        if (matchEvent.Type == MatchEventType.MissedChance)
        {
            SoundEffectsPlayer.PlayCrowdMiss();
        }
        else if (matchEvent.Type == MatchEventType.GoalkeeperSave)
        {
            SoundEffectsPlayer.TryPlayGoalkeeperSave(matchEvent.SpatialPan);
        }
        else if (matchEvent.Type == MatchEventType.WoodworkHit)
        {
            SoundEffectsPlayer.PlayPostHit();
        }
        else if (matchEvent.Type == MatchEventType.PenaltyAwarded)
        {
            SoundEffectsPlayer.PlayWhistle();
            SoundEffectsPlayer.PlayPenalty();
        }
        else if (matchEvent.Type == MatchEventType.CornerKick)
        {
            SoundEffectsPlayer.PlayCorner();
        }
        else if (matchEvent.Type == MatchEventType.GoalKick)
        {
            SoundEffectsPlayer.PlayGoalKick();
        }
        else if (matchEvent.Type == MatchEventType.ThrowIn)
        {
            SoundEffectsPlayer.PlayThrowIn();
        }
        else if (matchEvent.Type == MatchEventType.UserTackleSuccessful)
        {
            if (IsSlidingTackle(matchEvent))
            {
                SoundEffectsPlayer.PlaySlideTackle(matchEvent.SpatialPan);
            }
            else if (matchEvent.Context.TryGet("tackle.sliding", out _) || matchEvent.Description.Contains("هادئ") || matchEvent.Description.Contains("وقوف"))
            {
                SoundEffectsPlayer.PlayStandTackle(matchEvent.SpatialPan);
            }
            else
            {
                SoundEffectsPlayer.PlayTackleWon();
            }
        }
        else if (matchEvent.Type == MatchEventType.OpponentTackleSuccessful)
        {
            SoundEffectsPlayer.PlayBallLost();
        }
        else if (matchEvent.Type == MatchEventType.ShotTaken)
        {
            SoundEffectsPlayer.PlayShotKick(matchEvent.SpatialPan);
        }
        else if (matchEvent.Type == MatchEventType.BallKicked)
        {
            if (IsAirborneKick(matchEvent))
            {
                SoundEffectsPlayer.PlayCrossKick(matchEvent.SpatialPan);
            }
            else
            {
                SoundEffectsPlayer.PlayPassKick(matchEvent.SpatialPan);
            }
        }
        else if (matchEvent.Type == MatchEventType.BallTrapped)
        {
            SoundEffectsPlayer.PlayPassKick(matchEvent.SpatialPan);
        }
        else if (matchEvent.Type == MatchEventType.TackleSuccessful 
                 || matchEvent.Type == MatchEventType.TackleAttempted)
        {
            if (IsSlidingTackle(matchEvent))
            {
                SoundEffectsPlayer.PlaySlideTackle(matchEvent.SpatialPan);
            }
            else
            {
                SoundEffectsPlayer.PlayStandTackle(matchEvent.SpatialPan);
            }
        }
        else if (matchEvent.Type == MatchEventType.FoulCommitted)
        {
            SoundEffectsPlayer.PlayFoul();
            SoundEffectsPlayer.PlayWhistle();
        }
        else if (matchEvent.Type == MatchEventType.Offside 
                 || matchEvent.Type == MatchEventType.HalfTime 
                 || matchEvent.Type == MatchEventType.FullTime 
                 || matchEvent.Type == MatchEventType.StoppageTimeAnnounced)
        {
            SoundEffectsPlayer.PlayWhistle();
        }

        if (matchEvent.Type == MatchEventType.SubstitutionMade)
        {
            string text = matchEvent.Description;
            if (matchEvent.Context.TryGet("restart.held", out var held) && held == "true" &&
                matchEvent.Context.TryGet("restart.teamName", out var teamName) &&
                matchEvent.Context.TryGet("restart.takerName", out var takerName))
                text += $". استئناف اللعب لصالح {teamName}، {takerName} على التنفيذ. اضغط إنتر بعد التعليق.";
            SpeakWithDucking(text);
            return;
        }
        // Details come from the event-time team/taker facts, never guessed from a generic commentary clip.
        if (matchEvent.Type is MatchEventType.FreeKickAwarded or MatchEventType.PenaltyAwarded or MatchEventType.CornerKick
            or MatchEventType.GoalKick or MatchEventType.ThrowIn)
        {
            string kind = matchEvent.Type switch
            {
                MatchEventType.PenaltyAwarded => "ضربة جزاء",
                MatchEventType.CornerKick => "ضربة ركنية",
                MatchEventType.GoalKick => "ضربة مرمى",
                MatchEventType.ThrowIn => "رمية تماس",
                _ => "ركلة حرة"
            };
            string team = matchEvent.Context.TryGet("team.name", out var name) ? $" لصالح {name}" : "";
            string taker = matchEvent.SubjectPlayer is { } actor ? $". {actor.Name} على التنفيذ" : "";
            SpeakWithDucking($"{kind}{team}{taker}. اضغط إنتر لاستئناف اللعب بعد الإعادة.");
            return;
        }
        if (matchEvent.Type is MatchEventType.Offside or MatchEventType.GoalDisallowed)
        {
            SpeakWithDucking(matchEvent.Description + ". اضغط إنتر بعد الإعادة لاستئناف اللعب.");
            return;
        }

        // 4. إذا تكفل المعلق بالتعليق أو كان وضع الصمت التام مفعلاً
        if (matchEvent.HandledByCommentary)
        {
            return;
        }

        if (SilenceLiveMatchSpeech)
        {
            // في وضع الصمت التام: ينطق قارئ الشاشة فقط في التوقفات الرسمية الشاملة للمباراة
            if (matchEvent.Type == MatchEventType.HalfTime 
                || matchEvent.Type == MatchEventType.FullTime 
                || matchEvent.Type == MatchEventType.ExtraTimeStarted 
                || matchEvent.Type == MatchEventType.PenaltyShootoutStarted
                || matchEvent.Type == MatchEventType.StoppageTimeAnnounced)
            {
                SpeakWithDucking(matchEvent.Description);
            }
            return;
        }

        string textToSpeak = string.Empty;

        if (matchEvent.Type == MatchEventType.UserTackleSuccessful || matchEvent.Type == MatchEventType.OpponentTackleSuccessful)
        {
            DateTime now = DateTime.UtcNow;
            if ((now - _lastTackleSpeechTime) >= _tackleThrottle)
            {
                _lastTackleSpeechTime = now;
                textToSpeak = matchEvent.Description;
            }
        }
        else if (matchEvent.Type == MatchEventType.HalfTime || matchEvent.Type == MatchEventType.FullTime)
        {
            textToSpeak = matchEvent.Description;
        }
        else if (matchEvent.Type == MatchEventType.ExtraTimeStarted)
        {
            textToSpeak = "نهاية الوقت الأصلي بالتعادل! انطلاق الأشواط الإضافية الآن.";
        }
        else if (matchEvent.Type == MatchEventType.PenaltyShootoutStarted)
        {
            textToSpeak = "نهاية الوقت الإضافي بالتعادل! الاحتكام إلى ركلات الترجيح لحسم الفائز.";
        }
        else
        {
            textToSpeak = matchEvent.Type switch
            {
                MatchEventType.CornerKick => matchEvent.Description,
                MatchEventType.GoalKick => matchEvent.Description,
                MatchEventType.ThrowIn => matchEvent.Description,
                MatchEventType.ShotTaken => matchEvent.Description,
                MatchEventType.BallKicked => matchEvent.Description,
                MatchEventType.BallTrapped => matchEvent.Description,
                MatchEventType.FoulCommitted => "فاول",
                MatchEventType.Offside => "تسلل",
                _ => matchEvent.Description
            };
        }

        if (!string.IsNullOrWhiteSpace(textToSpeak))
        {
            SpeakWithDucking(textToSpeak);
        }
    }

    private static bool IsSlidingTackle(MatchEvent e)
    {
        if (e.Context.TryGet("tackle.sliding", out var sliding))
            return string.Equals(sliding,"true",StringComparison.OrdinalIgnoreCase);
        // Legacy producers without structured facts retain their old route.
        return e.Description.Contains("انزلاق") || e.Description.Contains("زحلقة");
    }

    private static bool IsAirborneKick(MatchEvent e)
    {
        if (e.Context.TryGet("action.kind", out var kind))
        {
            if (kind == "cross") return true;
            if (kind == "pass") return e.Context.TryGet("pass.height",out var height) && height == "high";
            return false;
        }
        return e.Description.Contains("عرضية") || e.Description.Contains("عالية") || e.Description.Contains("رفع الركنية");
    }

    private int _goalSequenceGeneration;
    public bool HasPendingGoalAnnouncement { get; private set; }

    public void CancelPendingGoalSequence() { Interlocked.Increment(ref _goalSequenceGeneration); HasPendingGoalAnnouncement = false; }

    private async void HandleGoalCelebrationSequence(GoalDetails goalData, bool handledByCommentary)
    {
        HasPendingGoalAnnouncement = true;
        int generation = Volatile.Read(ref _goalSequenceGeneration);
        await Task.Delay(handledByCommentary ? 600 : 500);
        // No fixed timeout that would speak over a longer sequence of recorded clips.
        while (generation == Volatile.Read(ref _goalSequenceGeneration))
        {
            bool idle = _isCommentaryIdle?.Invoke() ?? !SoundEffectsPlayer.IsCommentaryPlaying();
            if (idle) break;
            await Task.Delay(250);
        }
        if (generation != Volatile.Read(ref _goalSequenceGeneration)) return;
        BeginReaderDucking();
        try { if (AccessibleFootball2026.Core.Models.MatchSettings.Current.ScreenReaderSpokenEventsEnabled) await _screenReader.SpeakUntilFinishedAsync(goalData.ToOfficialAnnouncement() + ". اضغط إنتر لإنهاء الإعادة واستئناف المباراة."); }
        finally { EndReaderDucking(); }
        if (generation == Volatile.Read(ref _goalSequenceGeneration))
        { HasPendingGoalAnnouncement = false; OnGoalSequenceCompleted?.Invoke(); }
    }

    private readonly object _duckingLock = new();
    private int _readerDuckingUsers;
    private void BeginReaderDucking()
    {
        lock (_duckingLock)
            if (++_readerDuckingUsers == 1) OnCrowdVolumeDucking?.Invoke(.2f);
    }
    private void EndReaderDucking()
    {
        lock (_duckingLock)
            if (--_readerDuckingUsers == 0) OnCrowdVolumeDucking?.Invoke(1f);
    }

    private async void SpeakWithDucking(string text, bool explicitRequest = false)
    {
        if (!explicitRequest && !AccessibleFootball2026.Core.Models.MatchSettings.Current.ScreenReaderSpokenEventsEnabled) return;
        BeginReaderDucking();
        try { await _screenReader.SpeakUntilFinishedAsync(text, interrupt: true); }
        catch { /* A failed/cancelled reader still releases its own ducking claim. */ }
        finally { EndReaderDucking(); }
    }
}
