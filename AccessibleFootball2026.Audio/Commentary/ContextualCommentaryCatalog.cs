using System.Globalization;
using System.Text.Json;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Commentary;

namespace AccessibleFootball2026.Audio.Commentary;

/// <summary>Fail-closed contextual selection, independent of audio playback and screen readers.</summary>
public sealed class ContextualCommentaryCatalog
{
    public sealed class Requirement
    {
        public string Fact { get; set; } = "";
        public string Op { get; set; } = "eq";
        public string Value { get; set; } = "";

        public bool Matches(CommentaryContext context)
        {
            if (!context.TryGet(Fact, out var actual)) return false;
            if (Op == "eq") return string.Equals(actual, Value, StringComparison.Ordinal);
            if (Op == "ne") return !string.Equals(actual, Value, StringComparison.Ordinal);
            if (!double.TryParse(actual, NumberStyles.Float, CultureInfo.InvariantCulture, out var a) ||
                !double.TryParse(Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var b) ||
                !double.IsFinite(a) || !double.IsFinite(b)) return false;
            return Op switch { "gt" => a > b, "ge" => a >= b, "lt" => a < b, "le" => a <= b, _ => false };
        }
    }

    public sealed class Entry
    {
        public string Id { get; set; } = "";
        public string File { get; set; } = "";
        public string Event { get; set; } = "";
        public string Transcript { get; set; } = "";
        public string Review { get; set; } = "";
        public Requirement[] All { get; set; } = Array.Empty<Requirement>();
        public int Priority { get; set; }
    }

    public sealed class Document
    {
        public int Version { get; set; }
        public Entry[] Clips { get; set; } = Array.Empty<Entry>();
    }

    private readonly Dictionary<string, Queue<string>> _recentByEvent = new(StringComparer.Ordinal);
    private readonly object _selectionLock = new();
    private readonly Dictionary<string,int> _playCounts=new(StringComparer.Ordinal);

    public IReadOnlyList<Entry> Entries { get; }
    public string Root { get; }
    public bool IsValid { get; }
    public string? Error { get; }

    public ContextualCommentaryCatalog(string root)
    {
        Root = Path.GetFullPath(root);
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var doc = JsonSerializer.Deserialize<Document>(System.IO.File.ReadAllText(Path.Combine(root, "commentary_context.json")), options);
            if (doc == null || doc.Version != 1) throw new InvalidDataException("Unsupported commentary catalog version");
            if (doc.Clips.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count() != doc.Clips.Length)
                throw new InvalidDataException("Duplicate clip IDs");
            foreach (var c in doc.Clips)
            {
                if (string.IsNullOrWhiteSpace(c.Id) || string.IsNullOrWhiteSpace(c.Event) || string.IsNullOrWhiteSpace(c.Transcript) ||
                    c.Review != "verified" || ResolvePath(c.File) == null || c.All == null ||
                    c.All.Any(r => string.IsNullOrWhiteSpace(r.Fact) || !new[] { "eq", "ne", "gt", "ge", "lt", "le" }.Contains(r.Op)))
                    throw new InvalidDataException("Invalid or unreviewed contextual clip: " + c.Id);
            }
            Entries = Array.AsReadOnly(doc.Clips);
            IsValid = true;
        }
        catch (Exception ex)
        {
            Entries = Array.Empty<Entry>();
            Error = ex.Message;
        }
    }

    private string? ResolvePath(string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative) || relative.Contains('\\') || relative.Split('/').Any(p => p is ".." or "." or "")) return null;
        var full = Path.GetFullPath(Path.Combine(Root, relative));
        if (!full.StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
            !(full.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) || full.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))) return null;
        return System.IO.File.Exists(full) ? full : null;
    }

    public IReadOnlyList<Entry> Eligible(string eventKey, CommentaryContext context)
    {
        if (!IsValid) return Array.Empty<Entry>();
        var entries = Entries.Where(c => c.Event == eventKey && c.All.All(r => r.Matches(context)) && ResolvePath(c.File) != null).ToArray();
        if (entries.Length == 0) return entries;
        // Truth conditions decide eligibility. Specificity must not permanently starve safe generic takes.
        var priority = entries.Max(c => c.Priority);
        return entries.Where(c => c.Priority == priority).ToArray();
    }

    public string? Choose(string eventKey, CommentaryContext context)
    {
        lock (_selectionLock)
        {
            var entries = Eligible(eventKey, context);
            if (entries.Count == 0) return null;
            if (!_recentByEvent.TryGetValue(eventKey, out var recent)) _recentByEvent[eventKey] = recent = new Queue<string>();
            var fresh = entries.Where(e => !recent.Contains(e.Id)).ToArray();
            // Every distinct eligible recording stays available; history affects rotation, never catalog membership.
            while (fresh.Length == 0 && recent.Count > 0)
            {
                recent.Dequeue();
                fresh = entries.Where(e => !recent.Contains(e.Id)).ToArray();
            }
            int minimum=fresh.Min(e=>_playCounts.GetValueOrDefault(e.Id));
            fresh=fresh.Where(e=>_playCounts.GetValueOrDefault(e.Id)==minimum).ToArray();
            int specificity=fresh.Max(e=>e.All.Length);
            fresh=fresh.Where(e=>e.All.Length==specificity).ToArray();
            var chosen = fresh[Random.Shared.Next(fresh.Length)];
            _playCounts[chosen.Id]=_playCounts.GetValueOrDefault(chosen.Id)+1;
            recent.Enqueue(chosen.Id);
            while (recent.Count > Math.Max(1, Math.Min(8, entries.Count - 1))) recent.Dequeue();
            return ResolvePath(chosen.File);
        }
    }
}
