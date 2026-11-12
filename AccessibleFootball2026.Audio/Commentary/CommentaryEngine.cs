using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Audio.Commentary;

/// <summary>
/// محرك التعليق التفاعلي: يدير بنوك أصوات المعلقين، ويبث التعليق في اللحظة المناسبة،
/// يدعم تشغيل مقاطع المعلقين الحقيقية المسجلة (WAV و MP3)، أو قراءتها عبر محرك الصوت في حال عدم توفر التسجيل.
///
/// إصلاح تنسيق التشغيل (تداخل الجمل) بدون أي تغيير في سياسة اختيار المقاطع القائمة:
/// 1) كل المقاطع المسجلة تُشغَّل واحداً تلو الآخر عبر طابور تسلسلي: الحدث الحاسم (هدف/هاتريك/ريمونتادا)
///    لم يعد يقطع المقطع الجاري في نفس اللحظة، بل يُسلسل بعده، ولا يتداخل صوتان أبداً.
/// 2) الجمل النصية البديلة تنتظر انتهاء المقطع الجاري بدل النطق فوقه.
/// 3) ربط صريح اختياري عبر commentary_map.json بجانب مجلد المعلق (eventKey -> مقاطع محددة):
///    عند وجوده يقدَّم على مجلد الحدث؛ وبدونه يسري الترتيب الحالي كما هو تماماً.
/// </summary>
public class CommentaryEngine : IDisposable
{
    public CommentatorProfile ActiveCommentator { get; private set; }
    private readonly List<CommentatorProfile> _availableCommentators;
    private DateTime _lastCommentaryTime = DateTime.MinValue;
    private readonly TimeSpan _minIntervalBetweenLines = TimeSpan.FromMilliseconds(3200);
    private readonly string _baseCommentaryDir;

    private readonly Dictionary<string, string[]> _cachedFilesByDir = new(StringComparer.OrdinalIgnoreCase);

    // طابور التسلسل: لا يُشغَّل مقطع مسجل جديد قبل انتهاء الحالي (منع تداخل الجمل)
    private sealed class PendingLine
    {
        public string? ClipPath;
        public string? Phrase;
        public MatchEvent? Event;
        public Action? Callback;
        public Func<System.Threading.Tasks.Task>? AsyncCallback;
    }

    private readonly Queue<PendingLine> _pendingLines = new();
    private readonly object _queueLock = new();
    private const int MaxPendingLines = 6;
    private Timer? _dispatcher;
    private int _dispatchActive;
    private volatile bool _runningCallback;
    public object SpeechGate { get; } = new();
    public Func<bool>? IsReaderSpeaking { get; set; }
    public event Action? OnUrgentInterruption;
    public bool IsRecordedChannelBusy
    {
        get { lock (_queueLock) return SoundEffectsPlayer.IsCommentaryPlaying() ||
            (!_runningCallback && _pendingLines.Any(p => p.ClipPath != null || p.Phrase != null)); }
    }

    // خريطة ربط صريحة اختيارية (commentary_map.json): eventKey -> مسارات نسبية لمقاطع محددة
    private Dictionary<string, string[]>? _manifest;
    private bool _manifestLoaded;
    private ContextualCommentaryCatalog? _contextCatalog;
    private bool _contextCatalogPresent;
    // ملف ربط صريح موجود = تصنيف منتقى: يمنع السقوط إلى مجلدات فئات أخرى (مثل إصابة -> Fouls)
    private bool _manifestPresent;
    private readonly object _manifestLock = new();

    // اسم المعلق، الجملة المنطوقة، الحدث صاحب العلاقة
    public event Action<string, string>? OnCommentarySpoken;

    // يُطلق عند بدء تشغيل مقطع مسجل فعلياً (لإسكات قارئ الشاشة حتى لا يتداخل الصوتان)
    public event Action<string>? OnRecordedCommentaryStarted;

    public CommentaryEngine(CommentatorId defaultCommentator = CommentatorId.RaoufKhlif, string? baseDir = null)
    {
        _availableCommentators = CommentatorBank.GetAllCommentators();
        ActiveCommentator = _availableCommentators.Find(c => c.Id == defaultCommentator)
                            ?? _availableCommentators[0];

        string baseAudioDir = baseDir ?? ResolveBaseAudioDir();
        _baseCommentaryDir = Path.Combine(baseAudioDir, "Commentary");
        PreWarmCache();
        MatchSettings.OnSettingsChanged += ApplySettings;
    }

    private static bool CommentaryAudible => MatchSettings.Current.GetEffectiveCommentaryVolume() > .001f;
    private void ApplySettings(MatchSettings settings)
    {
        bool voiceChanged = ActiveCommentator.Id != settings.Commentator;
        if (voiceChanged) SwitchCommentator(settings.Commentator);
        if (voiceChanged || !CommentaryAudible)
        {
            lock (_queueLock)
            {
                var callbacks = _pendingLines.Where(line => line.Callback != null || line.AsyncCallback != null).ToArray();
                _pendingLines.Clear();
                foreach (var callback in callbacks) _pendingLines.Enqueue(callback);
            }
            SoundEffectsPlayer.StopCommentaryAudio();
        }
    }

    private void PreWarmCache()
    {
        if (!Directory.Exists(_baseCommentaryDir)) return;
        try
        {
            foreach (var commentatorDir in Directory.GetDirectories(_baseCommentaryDir))
            {
                foreach (var subDir in Directory.GetDirectories(commentatorDir))
                {
                    GetCachedFiles(subDir);
                }
            }
        }
        catch { }
    }

    private string[] GetCachedFiles(string dir)
    {
        if (_cachedFilesByDir.TryGetValue(dir, out var files))
        {
            return files;
        }

        if (!Directory.Exists(dir))
        {
            _cachedFilesByDir[dir] = Array.Empty<string>();
            return Array.Empty<string>();
        }

        try
        {
            files = Directory.GetFiles(dir, "*.*")
                             .Where(f => f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                             .ToArray();
            _cachedFilesByDir[dir] = files;
            return files;
        }
        catch
        {
            _cachedFilesByDir[dir] = Array.Empty<string>();
            return Array.Empty<string>();
        }
    }

    private static string ResolveBaseAudioDir()
    {
        string dDriveAudio = @"D:\AccessibleFootball2026\Assets\Audio";
        if (Directory.Exists(dDriveAudio)) return dDriveAudio;

        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "AccessibleFootball2026.sln")))
            {
                string slnAudio = Path.Combine(dir, "Assets", "Audio");
                if (Directory.Exists(slnAudio)) return slnAudio;
            }
            dir = Path.GetDirectoryName(dir);
        }

        string binAudio = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio");
        return binAudio;
    }

    public CommentaryLibrary LoadFaresLibrary()=>new(Path.Combine(_baseCommentaryDir,"FaresAwad"));
    public bool PlayLibraryRecording(CommentaryLibrary library,CommentaryLibrary.Recording recording)
    {
        if (!CommentaryAudible) return false;
        if(!library.Recordings.Contains(recording))return false;
        var path=library.Resolve(recording.File);if(path==null)return false;
        EnqueueClip(path,null);return true;
    }

    public void SwitchCommentator(CommentatorId newId)
    {
        var found = _availableCommentators.Find(c => c.Id == newId);
        if (found != null)
        {
            ActiveCommentator = found;
            lock (_manifestLock)
            {
                _manifest = null;
                _manifestLoaded = false;
                _manifestPresent = false;
                _contextCatalog = null;
                _contextCatalogPresent = false;
            }
        }
    }

    public List<CommentatorProfile> GetAvailableCommentators() => _availableCommentators;

    public bool PlayPreviewCatchphrase(CommentatorId id)
    {
        // تم إيقاف تشغيل الجمل العشوائية عند اختيار المعلق نهائياً
        return false;
    }

    public bool TryPlayMatchIntro(AccessibleFootball2026.Core.Commentary.CommentaryContext? context = null)
    {
        if (!CommentaryAudible) return false;
        EnsureManifestLoaded();
        if (_contextCatalogPresent)
        {
            var clip = _contextCatalog?.Choose("Intro", context ?? AccessibleFootball2026.Core.Commentary.CommentaryContext.Empty);
            if (clip == null) return false;
            EnqueueClip(clip, null);
            return true;
        }
        string dir = Path.Combine(_baseCommentaryDir, ActiveCommentator.Id.ToString(), "Intro");
        var files = GetCachedFiles(dir);
        if (files.Length == 0)
        {
            dir = Path.Combine(_baseCommentaryDir, ActiveCommentator.Id.ToString(), "Intros");
            files = GetCachedFiles(dir);
        }

        if (files.Length > 0)
        {
            var randFile = files[Random.Shared.Next(files.Length)];
            EnqueueClip(randFile, null);
            return true;
        }
        return false;
    }

    /// <summary>هل قناة التعليق حرة تماماً: لا مقطع يُشغَّل ولا جملة تنتظر في الطابور.</summary>
    public bool IsIdle
    {
        get
        {
            lock (_queueLock)
            {
                if (_pendingLines.Count > 0 || Volatile.Read(ref _dispatchActive) != 0) return false;
            }
            return !SoundEffectsPlayer.IsCommentaryPlaying();
        }
    }

    // Used for match introductions: retain the spoken team information, but wait for the recorded intro.
    public void RunAfterRecordedCommentary(Action callback)
    {
        lock (_queueLock) { _pendingLines.Enqueue(new PendingLine { Callback = callback }); }
        EnsureDispatcher();
    }

    public void RunAfterRecordedCommentaryAsync(Func<System.Threading.Tasks.Task> callback)
    {
        lock(_queueLock){_pendingLines.Enqueue(new PendingLine{AsyncCallback=callback});}
        EnsureDispatcher();
    }

    public bool QueueGoalReplay(AccessibleFootball2026.Core.Commentary.GoalReplay replay)
    {
        if (!CommentaryAudible) return false;
        EnsureManifestLoaded();
        if (!_contextCatalogPresent) return false;
        string? path;
        try { path = GoalReplayAudio.Render(replay); } catch { return false; }
        if (path == null) return false;
        var analysis = new MatchEvent(MatchEventType.GoalAnalysis, "إعادة صوتية لمسار الهدف", goalData: replay.Goal) { Context = replay.Context };
        EnqueueClip(path, null);
        TryCommentOnEvent(analysis, force: true);
        return true;
    }

    public bool QueuePlayReplay(AccessibleFootball2026.Core.Simulation.PlayReplay replay)
    {
        if (!CommentaryAudible) return false;
        EnsureManifestLoaded();
        if(!_contextCatalogPresent)return false;
        string? path;
        try {path=GoalReplayAudio.RenderFrames(replay.Frames);} catch {return false;}
        if(path==null)return false;
        EnqueueClip(path,null);
        TryCommentOnEvent(replay.Analysis,force:true);
        return true;
    }

    public void ClearPendingLines()
    {
        lock (_queueLock) { _pendingLines.Clear(); }
    }

    private bool HasPendingLines
    {
        get { lock (_queueLock) { return _pendingLines.Count > 0; } }
    }

    private void EnsureDispatcher()
    {
        lock (_queueLock)
        {
            _dispatcher ??= new Timer(DispatchPending, null, 250, 250);
        }
    }

    private void EnqueueClip(string clipPath, MatchEvent? matchEvent)
    {
        if (!CommentaryAudible) return;
        lock (_queueLock)
        {
            // Admission throttling happens before this point. Never evict an ordered replay or completion callback.
            _pendingLines.Enqueue(new PendingLine { ClipPath = clipPath, Event = matchEvent });
        }
        EnsureDispatcher();
        DispatchPending(null); // تشغيل فوري إن كانت القناة حرة، بلا انتظار نبضة المؤقت
    }

    private void EnqueuePhrase(string phrase, MatchEvent matchEvent)
    {
        if (!CommentaryAudible) return;
        lock (_queueLock)
        {
            if (_pendingLines.Count >= MaxPendingLines) return;
            _pendingLines.Enqueue(new PendingLine { Phrase = phrase, Event = matchEvent });
        }
        EnsureDispatcher();
    }

    private async void DispatchPending(object? state)
    {
        if (Interlocked.Exchange(ref _dispatchActive, 1) == 1) return;
        try
        {
            PendingLine? next;
            lock (SpeechGate)
            {
                if (SoundEffectsPlayer.IsCommentaryPlaying() || IsReaderSpeaking?.Invoke() == true) return;
                lock (_queueLock) next = _pendingLines.Count > 0 ? _pendingLines.Dequeue() : null;
                if (next == null) return;
                if (next.ClipPath != null)
                {
                    if (CommentaryAudible && SoundEffectsPlayer.PlayCommentaryAudio(next.ClipPath))
                    {
                        _lastCommentaryTime = DateTime.UtcNow;
                        OnRecordedCommentaryStarted?.Invoke(next.ClipPath);
                    }
                    return;
                }
                _runningCallback = next.AsyncCallback != null || next.Callback != null;
            }
            if(next.AsyncCallback!=null)
            {
                _runningCallback = true;
                await next.AsyncCallback();
            }
            else if (next.Callback != null)
            {
                _runningCallback = true;
                next.Callback();
            }
            else if (CommentaryAudible && next.Phrase != null && next.Event != null)
            {
                _lastCommentaryTime = DateTime.UtcNow;
                OnCommentarySpoken?.Invoke(ActiveCommentator.DisplayName, next.Phrase);
            }
        }
        catch { }
        finally
        {
            _runningCallback = false;
            _dispatchActive = 0;
        }
    }

    private string[] GetManifestFiles(string eventKey)
    {
        EnsureManifestLoaded();
        if (_manifest != null && _manifest.TryGetValue(eventKey, out var files)) return files;
        return Array.Empty<string>();
    }

    private void EnsureManifestLoaded()
    {
        lock (_manifestLock)
        {
            if (_manifestLoaded) return;
            _manifestLoaded = true;
            try
            {
                string commentatorDir = Path.Combine(_baseCommentaryDir, ActiveCommentator.Id.ToString());
                string contextPath = Path.Combine(commentatorDir, "commentary_context.json");
                _contextCatalogPresent = File.Exists(contextPath);
                if (_contextCatalogPresent)
                {
                    _manifestPresent = true;
                    _contextCatalog = new ContextualCommentaryCatalog(commentatorDir);
                    return; // Even an invalid catalog must never fall through into unfiltered pools.
                }
                string path = Path.Combine(commentatorDir, "commentary_map.json");
                if (!File.Exists(path)) return;
                _manifestPresent = true;

                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                var map = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Value.ValueKind != JsonValueKind.Array) continue;
                    var list = prop.Value.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.String)
                        .Select(e => e.GetString()!)
                        .Where(rel => !string.IsNullOrWhiteSpace(rel) && !Path.IsPathRooted(rel) && !rel.Contains(".."))
                        .Select(rel => Path.Combine(commentatorDir, rel))
                        .Where(File.Exists)
                        .ToArray();
                    if (list.Length > 0) map[prop.Name] = list;
                }
                _manifest = map;
            }
            catch { }
        }
    }

    public bool TryCommentOnEvent(MatchEvent matchEvent, bool force = false)
    {
        if (!CommentaryAudible) return false;
        // Only a real goal or awarded penalty may cut a sentence. All other events queue or drop.
        if (matchEvent.Type is MatchEventType.GoalScored or MatchEventType.PenaltyAwarded)
        {
            ClearPendingLines();
            SoundEffectsPlayer.StopCommentaryAudio();
            OnUrgentInterruption?.Invoke();
        }
        bool isFinalAnalysis = matchEvent.Type == MatchEventType.MatchAnalysis && matchEvent.Context.TryGet("match.finished",out var finished) && finished=="true";
        bool isCritical = isFinalAnalysis || matchEvent.Type is MatchEventType.GoalScored or MatchEventType.DramaticComeback or MatchEventType.HatTrickAchieved or MatchEventType.WoodworkHit or MatchEventType.RedCard or MatchEventType.FullTime or MatchEventType.HalfTime or MatchEventType.ExtraTimeHalfTime or MatchEventType.PenaltyAwarded or MatchEventType.FreeKickAwarded;

        if (!force && !isCritical)
        {
            if (DateTime.UtcNow - _lastCommentaryTime < _minIntervalBetweenLines ||
                SoundEffectsPlayer.IsCommentaryPlaying() || HasPendingLines)
            {
                return false;
            }
        }

        string eventFolder = matchEvent.Type switch
        {
            MatchEventType.GoalScored or MatchEventType.DramaticComeback or MatchEventType.HatTrickAchieved => (matchEvent.GoalData?.IsOwnGoal == true) ? "OwnGoals" : "Goals",
            MatchEventType.UserTackleSuccessful or MatchEventType.OpponentTackleSuccessful or MatchEventType.TackleSuccessful => "Tackles",
            MatchEventType.ShotTaken => "Shots",
            MatchEventType.MissedChance => "Misses",
            MatchEventType.CornerKick => "Corners",
            MatchEventType.GoalkeeperSave => "Saves",
            MatchEventType.WoodworkHit => "Woodwork",
            MatchEventType.StoppageTimeAnnounced => "StoppageTime",
            MatchEventType.FullTime => "FullTime",
            MatchEventType.HalfTime => "HalfTime",
            MatchEventType.YellowCard => "Cards",
            MatchEventType.RedCard => "RedCards",
            MatchEventType.PlayerInjured => "Injuries",
            MatchEventType.FoulCommitted => "Fouls",
            MatchEventType.PenaltyAwarded => "Penalties",
            _ => string.Empty
        };

        string eventKey = matchEvent.Type switch
        {
            MatchEventType.GoalScored or MatchEventType.DramaticComeback or MatchEventType.HatTrickAchieved => (matchEvent.GoalData?.IsOwnGoal == true) ? "OwnGoal" : "Goal",
            MatchEventType.UserTackleSuccessful or MatchEventType.OpponentTackleSuccessful or MatchEventType.TackleSuccessful => "Tackle",
            MatchEventType.ShotTaken => "Shot",
            MatchEventType.MissedChance => "Miss",
            MatchEventType.CornerKick => "Corner",
            MatchEventType.GoalkeeperSave => "Save",
            MatchEventType.WoodworkHit => "Woodwork",
            MatchEventType.StoppageTimeAnnounced => "StoppageTime",
            MatchEventType.FullTime => "FullTime",
            MatchEventType.HalfTime => "HalfTime",
            MatchEventType.YellowCard => "Card",
            MatchEventType.RedCard => "RedCard",
            MatchEventType.PlayerInjured => "Injury",
            MatchEventType.FoulCommitted => "Foul",
            MatchEventType.PenaltyAwarded => "Penalty",
            _ => matchEvent.Type.ToString()
        };

        if (string.IsNullOrEmpty(eventKey)) return false;

        EnsureManifestLoaded();
        if (_contextCatalogPresent)
        {
            // The primary goal snapshot already includes player ordinal and comeback state.
            // Do not queue three random goal screams for one goal.
            if (matchEvent.Type is MatchEventType.HatTrickAchieved or MatchEventType.DramaticComeback) return false;
            var contextualClip = _contextCatalog?.Choose(eventKey, matchEvent.Context);
            if (contextualClip != null)
            {
                matchEvent.HandledByCommentary = true;
                EnqueueClip(contextualClip, matchEvent);
                return true;
            }
            // No matching recording means unknown or absent, not permission to ignore conditions.
        }

        // 1. ربط صريح اختياري في commentary_map.json إن وُجد (لا يغيّر شيئاً بغيابه)
        string[] files = GetManifestFiles(eventKey);

        // 2. الترتيب الحالي كما هو: ملفات المعلق المسجلة في مجلد الحدث (ذاكرة فورية O(1) بدون تأخير قرص)
        if (files.Length == 0 && !_contextCatalogPresent)
        {
            string dir = Path.Combine(_baseCommentaryDir, ActiveCommentator.Id.ToString(), eventFolder);
            files = GetCachedFiles(dir);

            // مسارات بديلة في حال عدم توفر المجلد التخصصي لدى بعض المعلقين
            if (files.Length == 0 && !_manifestPresent)
            {
                // المسارات البديلة بين الفئات تعمل فقط بغياب ربط صريح؛ مع وجود commentary_map.json
                // لا يجوز أن يُشغَّل مقطع من فئة مختلفة لحدث بلا مقطع (يُستخدم البديل النصي بدلاً منه)
                if (eventFolder == "Corners" || eventFolder == "Cards" || eventFolder == "Injuries")
                {
                    files = GetCachedFiles(Path.Combine(_baseCommentaryDir, ActiveCommentator.Id.ToString(), "Fouls"));
                }
                else if (eventFolder == "Misses")
                {
                    files = GetCachedFiles(Path.Combine(_baseCommentaryDir, ActiveCommentator.Id.ToString(), "Shots"));
                }
                else if (eventFolder == "RedCards")
                {
                    files = GetCachedFiles(Path.Combine(_baseCommentaryDir, ActiveCommentator.Id.ToString(), "Cards"));
                }
                else if (eventFolder == "Penalties")
                {
                    files = GetCachedFiles(Path.Combine(_baseCommentaryDir, ActiveCommentator.Id.ToString(), "Fouls"));
                }
            }
        }

        if (files.Length > 0)
        {
            matchEvent.HandledByCommentary = true;
            var randFile = files[Random.Shared.Next(files.Length)];
            // تسلسل بدل القطع الفوري: الحدث الحاسم لم يعد يغلق المقطع الجاري في نفس اللحظة
            EnqueueClip(randFile, matchEvent);
            return true;
        }

        // 3. إذا لم يكن هناك ملف مسجل بعد، نستخدم الجملة النصية عبر قارئ الشاشة
        string? phrase = ActiveCommentator.GetRandomPhrase(eventKey);
        if (string.IsNullOrEmpty(phrase))
        {
            phrase = eventKey switch
            {
                "Card" => ActiveCommentator.GetRandomPhrase("Foul"),
                "RedCard" => matchEvent.Context.TryGet("card.kind", out var kind) && kind == "secondYellow" ? "إنذار ثانٍ وبطاقة حمراء للاعب" : "بطاقة حمراء للاعب",
                "Penalty" => "ركلة جزاء! الحكم يشير إلى علامة الجزاء",
                "Corner" => ActiveCommentator.GetRandomPhrase("Foul"),
                "HalfTime" => "صافرة نهاية الشوط الأول للمباراة",
                "FullTime" => ActiveCommentator.GetRandomPhrase("FullTime") ?? "صافرة النهاية وختام المباراة",
                "Injury" => "إصابة وسقوط اللاعب وتوقف اللعب",
                _ => null
            };
        }

        if (!string.IsNullOrEmpty(phrase))
        {
            matchEvent.HandledByCommentary = true;
            if (SoundEffectsPlayer.IsCommentaryPlaying() || HasPendingLines)
            {
                // القناة مشغولة بمقطع مسجل: تُسلسل الجملة بعده حتى لا يتداخل الصوتان
                EnqueuePhrase(phrase, matchEvent);
            }
            else
            {
                _lastCommentaryTime = DateTime.UtcNow;
                OnCommentarySpoken?.Invoke(ActiveCommentator.DisplayName, phrase);
            }
            return true;
        }

        return false;
    }

    public void Dispose()
    {
        MatchSettings.OnSettingsChanged -= ApplySettings;
        lock (_queueLock)
        {
            _dispatcher?.Dispose();
            _dispatcher = null;
            _pendingLines.Clear();
        }
    }
}
