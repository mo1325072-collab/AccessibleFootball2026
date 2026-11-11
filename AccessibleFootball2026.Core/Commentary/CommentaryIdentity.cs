using System.Globalization;
using System.Text;
namespace AccessibleFootball2026.Core.Commentary;

/// <summary>Explicit whole-name bindings. Never infer a callname from a surname or a colliding numeric ID.</summary>
public static class CommentaryIdentity
{
    public static string? PlayerAlias(string name) => Normalize(name) switch
    {
        "cristiano ronaldo" or "كريستيانو رونالدو" => "CristianoRonaldo",
        "thibaut courtois" or "تيبو كورتوا" => "ThibautCourtois",
        "luka modric" or "لوكا مودريتش" => "LukaModric",
        _ => null
    };
    public static string? TeamAlias(string name) => Normalize(name) switch
    {
        "villarreal cf" or "villarreal" or "فياريال" => "Villarreal",
        _ => null
    };
    public static string Normalize(string name)
    {
        var text = name.Normalize(NormalizationForm.FormD);
        var result = new StringBuilder();
        foreach(char c in text)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                result.Append(char.ToLowerInvariant(c));
        return result.ToString().Normalize(NormalizationForm.FormC).Trim();
    }
}
