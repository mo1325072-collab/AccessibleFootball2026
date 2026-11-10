using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Audio.Commentary;

/// <summary>
/// محرك التعليق التفاعلي: يدير بنوك أصوات المعلقين، ويبث التعليق في اللحظة المناسبة،
/// يدعم تشغيل مقاطع المعلقين الحقيقية المسجلة (WAV و MP3)، أو قراءتها عبر محرك الصوت في حال عدم توفر التسجيل.
/// </summary>
public class CommentaryEngine
{
    public CommentatorProfile ActiveCommentator { get; private set; }
    private readonly List<CommentatorProfile> _availableCommentators;
    private DateTime _lastCommentaryTime = DateTime.MinValue;
    private readonly TimeSpan _minIntervalBetweenLines = TimeSpan.FromMilliseconds(3200);
    private readonly string _baseCommentaryDir;

    private readonly Dictionary<string, string[]> _cachedFilesByDir = new(StringComparer.OrdinalIgnoreCase);

    public event Action<string, string>? OnCommentarySpoken; // اسم المعلق، الجملة

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
            _lastCommentaryTime = DateTime.UtcNow;
            SoundEffectsPlayer.PlayCommentaryAudio(randFile);
            return true;
        }
        return false;
    }

    public bool TryCommentOnEvent(MatchEvent matchEvent, bool force = false)
    {
        bool isCritical = matchEvent.Type is MatchEventType.GoalScored or MatchEventType.DramaticComeback or MatchEventType.HatTrickAchieved or MatchEventType.WoodworkHit or MatchEventType.RedCard or MatchEventType.FullTime;

        if (!force && !isCritical)
        {
            if (DateTime.UtcNow - _lastCommentaryTime < _minIntervalBetweenLines || SoundEffectsPlayer.IsCommentaryPlaying())
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

        // 1. أولاً: فحص الملفات المسجلة الحقيقية للمعلق في مجلد الحدث (ذاكرة فورية O(1) بدون تأخير قرص)
        string dir = Path.Combine(_baseCommentaryDir, ActiveCommentator.Id.ToString(), eventFolder);
        var files = GetCachedFiles(dir);

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

        if (files.Length > 0)
        {
            _lastCommentaryTime = DateTime.UtcNow;
            matchEvent.HandledByCommentary = true;
            var randFile = files[Random.Shared.Next(files.Length)];
            SoundEffectsPlayer.PlayCommentaryAudio(randFile);
            return true;
        }

        // 2. إذا لم يكن هناك ملف مسجل بعد، نستخدم الجملة النصية عبر قارئ الشاشة
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
            _lastCommentaryTime = DateTime.UtcNow;
            matchEvent.HandledByCommentary = true;
            OnCommentarySpoken?.Invoke(ActiveCommentator.DisplayName, phrase);
            return true;
        }

        return false;
    }
}
