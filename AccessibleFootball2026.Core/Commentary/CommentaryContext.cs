using System.Collections.ObjectModel;
using System.Globalization;

namespace AccessibleFootball2026.Core.Commentary;

/// <summary>An event-time snapshot. Missing facts remain unknown, never false or zero.</summary>
public sealed class CommentaryContext
{
    private readonly IReadOnlyDictionary<string, string> _facts;
    public static CommentaryContext Empty { get; } = new(new Dictionary<string, string>());
    public IReadOnlyDictionary<string, string> Facts => _facts;

    public CommentaryContext(IEnumerable<KeyValuePair<string, string>> facts)
    {
        _facts = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(facts, StringComparer.Ordinal));
    }

    public bool TryGet(string fact, out string value) => _facts.TryGetValue(fact, out value!);

    public CommentaryContext With(params (string Key, object? Value)[] facts)
    {
        var copy = new Dictionary<string, string>(_facts, StringComparer.Ordinal);
        foreach (var (key, value) in facts)
        {
            if (value == null) continue;
            copy[key] = value switch
            {
                bool b => b ? "true" : "false",
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture) ?? "",
                _ => value.ToString() ?? ""
            };
        }
        return new CommentaryContext(copy);
    }
}
