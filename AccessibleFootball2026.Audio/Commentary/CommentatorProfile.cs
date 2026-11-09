using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Audio.Commentary;

public class CommentatorProfile
{
    public CommentatorId Id { get; }
    public string DisplayName { get; }
    public string Catchphrase { get; } // الجملة الشهيرة (مثل: يا رباااه)
    public string Description { get; }
    public string Region { get; }

    public Dictionary<string, List<string>> Phrases { get; } = new();

    public CommentatorProfile(CommentatorId id, string name, string catchphrase, string desc, string region)
    {
        Id = id;
        DisplayName = name;
        Catchphrase = catchphrase;
        Description = desc;
        Region = region;
    }

    public void AddPhrase(string eventKey, string phrase)
    {
        if (!Phrases.ContainsKey(eventKey))
        {
            Phrases[eventKey] = new List<string>();
        }
        Phrases[eventKey].Add(phrase);
    }

    public string? GetRandomPhrase(string eventKey)
    {
        if (Phrases.TryGetValue(eventKey, out var list) && list.Count > 0)
        {
            var rand = new Random();
            return list[rand.Next(list.Count)];
        }
        return null;
    }
}
