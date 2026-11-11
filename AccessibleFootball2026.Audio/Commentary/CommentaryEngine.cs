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
    }

    private readonly Queue<PendingLine> _pendingLines = new();
    private readonly object _queueLock = new();
    private const int MaxPendingLines = 6;
    private Timer? _dispatcher;
    private int _dispatchActive;

    // خريطة ربط صريحة اختيارية (commentary_map.json): eventKey -> مسارات نسبية لمقاطع محددة
    private Dictionary<string, string[]>? _manifest;
    private bool _manifestLoaded;
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
            }
        }
    }

    public List<CommentatorProfile> GetAvailableCommentators() => _availableCommentators;

    public bool PlayPreviewCatchphrase(CommentatorId id)
    {
        // تم إيقاف تشغيل الجمل العشوائية عند اختيار المعلق نهائياً
        return false;
    }

    public bool TryPlayMatchIntro()
    {
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
        lock (_queueLock)
        {
            if (_pendingLines.Count >= MaxPendingLines)
            {
                if (matchEvent?.Type == MatchEventType.FullTime) _pendingLines.Dequeue();
                else return;
            }
            _pendingLines.Enqueue(new PendingLine { ClipPath = clipPath, Event = matchEvent });
        }
        EnsureDispatcher();
        DispatchPending(null); // تشغيل فوري إن كانت القناة حرة، بلا انتظار نبضة المؤقت
    }

    private void EnqueuePhrase(string phrase, MatchEvent matchEvent)
    {
        lock (_queueLock)
        {
            if (_pendingLines.Count >= MaxPendingLines) return;
            _pendingLines.Enqueue(new PendingLine { Phrase = phrase, Event = matchEvent });
        }
        EnsureDispatcher();
    }

    private void DispatchPending(object? state)
    {
        if (Interlocked.Exchange(ref _dispatchActive, 1) == 1) return;
        try
        {
            if (SoundEffectsPlayer.IsCommentaryPlaying()) return;

            PendingLine? next;
            lock (_queueLock)
            {
                next = _pendingLines.Count > 0 ? _pendingLines.Dequeue() : null;
            }
            if (next == null) return;

            if (next.ClipPath != null)
            {
                _lastCommentaryTime = DateTime.UtcNow;
                SoundEffectsPlayer.PlayCommentaryAudio(next.ClipPath);
                OnRecordedCommentaryStarted?.Invoke(next.ClipPath);
            }
            else if (next.Callback != null)
            {
                next.Callback();
            }
            else if (next.Phrase != null && next.Event != null)
            {
                _lastCommentaryTime = DateTime.UtcNow;
                OnCommentarySpoken?.Invoke(ActiveCommentator.DisplayName, next.Phrase);
            }
        }
        catch { }
        finally
        {
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
                string path = Path.Combine(commentatorDir, "commentary_map.json");
                if (!File.Exists(path)) return;

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
        bool isCritical = matchEvent.Type is MatchEventType.GoalScored or MatchEventType.DramaticComeback or MatchEventType.HatTrickAchieved or MatchEventType.WoodworkHit or MatchEventType.RedCard or MatchEventType.FullTime;

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
            MatchEventType.YellowCard or MatchEventType.RedCard => "Cards",
            MatchEventType.PlayerInjured => "Injuries",
            MatchEventType.FoulCommitted or MatchEventType.PenaltyAwarded => "Fouls",
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
            MatchEventType.YellowCard or MatchEventType.RedCard => "Card",
            MatchEventType.PlayerInjured => "Injury",
            MatchEventType.FoulCommitted or MatchEventType.PenaltyAwarded => "Foul",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(eventKey)) return false;

        // 1. ربط صريح اختياري في commentary_map.json إن وُجد (لا يغيّر شيئاً بغيابه)
        string[] files = GetManifestFiles(eventKey);

        // 2. الترتيب الحالي كما هو: ملفات المعلق المسجلة في مجلد الحدث (ذاكرة فورية O(1) بدون تأخير قرص)
        if (files.Length == 0)
        {
            string dir = Path.Combine(_baseCommentaryDir, ActiveCommentator.Id.ToString(), eventFolder);
            files = GetCachedFiles(dir);

            // مسارات بديلة في حال عدم توفر المجلد التخصصي لدى بعض المعلقين
            if (files.Length == 0)
            {
                if (eventFolder == "Corners" || eventFolder == "Cards" || eventFolder == "Injuries")
                {
                    files = GetCachedFiles(Path.Combine(_baseCommentaryDir, ActiveCommentator.Id.ToString(), "Fouls"));
                }
                else if (eventFolder == "Misses")
                {
                    files = GetCachedFiles(Path.Combine(_baseCommentaryDir, ActiveCommentator.Id.ToString(), "Shots"));
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
        lock (_queueLock)
        {
            _dispatcher?.Dispose();
            _dispatcher = null;
            _pendingLines.Clear();
        }
    }
}
