using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Audio.Commentary;

/// <summary>
/// مدير مناداة اللاعبين بصوت المعلق (Zero-Latency Callname Manager):
/// يربط 13,610 ملف صوتي لأسماء اللاعبين بتشكيلات الأندية والمنتخبات
/// مع معالجة ذكية للأسماء العربية وتطبيع الحروف بدون أي تأخير زمني (O(1) Hash Map)
/// </summary>
public class CallnameManager
{
    private readonly string _callnamesDir;
    private readonly Dictionary<string, string> _callnameByNormalizedName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, string> _callnameByPlayerId = new();
    private readonly HashSet<string> _prewarmedSquadCallnames = new(StringComparer.OrdinalIgnoreCase);

    private DateTime _lastCallnameSpoken = DateTime.MinValue;
    private readonly TimeSpan _minCallnameInterval = TimeSpan.FromMilliseconds(1500);

    public CallnameManager(string? baseDir = null)
    {
        string baseAudio = baseDir ?? ResolveBaseAudioDir();
        string commCallnames = Path.Combine(baseAudio, "Commentary", "RaoufKhlif", "Callnames");
        string rootCallnames = Path.Combine(baseAudio, "Callnames");

        _callnamesDir = Directory.Exists(commCallnames) ? commCallnames : rootCallnames;
        InitializeIndex();
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

    private void InitializeIndex()
    {
        if (!Directory.Exists(_callnamesDir)) return;

        // قراءة الفهرس المحفوظ مسبقاً (callnames_index.csv) في أقل من 5 ميلي ثانية
        string indexFile = Path.Combine(_callnamesDir, "callnames_index.csv");
        if (File.Exists(indexFile))
        {
            foreach (var line in File.ReadLines(indexFile))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                var parts = line.Split(',');
                if (parts.Length >= 2)
                {
                    string name = parts[0].Trim();
                    string fileName = parts[1].Trim();
                    string fullPath = Path.Combine(_callnamesDir, fileName);

                    _callnameByNormalizedName[NormalizeArabic(name)] = fullPath;

                    if (parts.Length >= 3 && int.TryParse(parts[2], out int playerId))
                    {
                        _callnameByPlayerId[playerId] = fullPath;
                    }
                }
            }
        }
        else
        {
            // مسح سريع للملفات الموجودة في المجلد
            foreach (var file in Directory.EnumerateFiles(_callnamesDir, "*.wav"))
            {
                string fn = Path.GetFileNameWithoutExtension(file);
                _callnameByNormalizedName[NormalizeArabic(fn)] = file;
            }
        }
    }

    /// <summary>
    /// تسخين وتحضير أسماء لاعبي الفريقين المشاركين في المباراة النشطة فقط (Pre-Warming)
    /// لاستهلاك ذاكرة لا يتجاوز 3.5 ميجابايت بدلاً من تحميل 13 ألف ملف في الرام
    /// </summary>
    public void PreWarmSquads(Team home, Team away)
    {
        _prewarmedSquadCallnames.Clear();
        if (home?.Players != null)
        {
            foreach (var p in home.Players) RegisterSquadPlayer(p);
        }
        if (away?.Players != null)
        {
            foreach (var p in away.Players) RegisterSquadPlayer(p);
        }
    }

    private void RegisterSquadPlayer(Player p)
    {
        string norm = NormalizeArabic(p.Name);
        if (_callnameByNormalizedName.TryGetValue(norm, out string? path))
        {
            _prewarmedSquadCallnames.Add(path);
            _callnameByPlayerId[p.Id] = path;
        }
        else
        {
            // تجربة البحث باسم الشهرة الأخير (مثل: "صلاح" من "محمد صلاح")
            string[] tokens = p.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length > 1)
            {
                string lastNameNorm = NormalizeArabic(tokens[^1]);
                if (_callnameByNormalizedName.TryGetValue(lastNameNorm, out string? lastPath))
                {
                    _prewarmedSquadCallnames.Add(lastPath);
                    _callnameByPlayerId[p.Id] = lastPath;
                }
            }
        }
    }

    /// <summary>
    /// تشغيل نداء اسم اللاعب في التمرير أو التسديد أو الافتكاك بلحظية تامة
    /// </summary>
    public bool TryPlayPlayerCallname(Player player, bool force = false)
    {
        if (player == null) return false;

        // أولوية صوت المعلق: نداء الاسم لا يقطع تعليقاً جارياً ولا يحجب قناته عن الأحداث المهمة
        if (!force && SoundEffectsPlayer.IsCommentaryPlaying())
        {
            return false;
        }

        if (!force && DateTime.UtcNow - _lastCallnameSpoken < _minCallnameInterval)
        {
            return false;
        }

        string? audioPath = null;

        // 1. فحص مباشر بالمعرف الرقمي للاعب O(1)
        if (_callnameByPlayerId.TryGetValue(player.Id, out string? idPath) && File.Exists(idPath))
        {
            audioPath = idPath;
        }
        // 2. فحص بالاسم المطبع O(1)
        else
        {
            string norm = NormalizeArabic(player.Name);
            if (_callnameByNormalizedName.TryGetValue(norm, out string? namePath) && File.Exists(namePath))
            {
                audioPath = namePath;
            }
            else
            {
                // تجربة البحث باسم الشهرة الأخير
                string[] tokens = player.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length > 1)
                {
                    string lastNameNorm = NormalizeArabic(tokens[^1]);
                    if (_callnameByNormalizedName.TryGetValue(lastNameNorm, out string? lastPath) && File.Exists(lastPath))
                    {
                        audioPath = lastPath;
                    }
                }
            }
        }

        if (audioPath != null)
        {
            _lastCallnameSpoken = DateTime.UtcNow;
            SoundEffectsPlayer.PlayCommentaryAudio(audioPath);
            return true;
        }

        return false;
    }

    /// <summary>
    /// محرك تطبيع الحروف العربية والإنجليزية لإزالة التشكيل والفروق الهجائية
    /// </summary>
    public static string NormalizeArabic(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        Span<char> buffer = stackalloc char[text.Length];
        int writeIdx = 0;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            // 1. إزالة التشكيل (0x064B - 0x065F, 0x0670) والتطويل (0x0640)
            if ((c >= '\u064B' && c <= '\u065F') || c == '\u0670' || c == '\u0640')
            {
                continue;
            }

            // 2. توحيد الألفات والهمزات (أ, إ, آ, ٱ) -> ا
            if (c == 'أ' || c == 'إ' || c == 'آ' || c == 'ٱ')
            {
                c = 'ا';
            }
            // 3. توحيد الياء والألف اللينة (ى) -> ي
            else if (c == 'ى')
            {
                c = 'ي';
            }
            // 4. توحيد التاء المربوطة (ة) -> ه
            else if (c == 'ة')
            {
                c = 'ه';
            }

            buffer[writeIdx++] = char.ToLowerInvariant(c);
        }

        return new string(buffer[..writeIdx]).Trim();
    }
}

