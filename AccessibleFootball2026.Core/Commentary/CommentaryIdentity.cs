using System.Globalization;
using System.Text;
namespace AccessibleFootball2026.Core.Commentary;

/// <summary>Explicit whole-name bindings. Never infer a callname from a surname or a colliding numeric ID.</summary>
public static class CommentaryIdentity
{
    public static string? PlayerAlias(string name) => Normalize(name) switch
    {
        "cristiano ronaldo" or "كريستيانو رونالدو" => "CristianoRonaldo",
        "nico gonzalez" or "nicolas gonzalez" or "nicolas ivan gonzalez" or "نيكو جونزاليس" or "نيكو غونزاليس" => "NicoGonzalez",
        "desire doue" or "ديزري دوي" or "ديزيريه دويه" => "DesireDoue",
        "pau cubarsi" or "كوبارسي" => "PauCubarsi",
        "cristian romero" or "كريستيان روميرو" => "CristianRomero",
        "nicolas tagliafico" or "تاليافيكو" or "تالياڤيكو" => "NicolasTagliafico",
        "enzo fernandez" or "انزو فرنانديز" or "إنزو فرنانديز" => "EnzoFernandez",
        "fabian ruiz" or "فابيان رويز" => "FabianRuiz",
        "mikel oyarzabal" or "أويارزابال" or "أوريزابال" or "اويارزابال" => "MikelOyarzabal",
        "thibaut courtois" or "تيبو كورتوا" => "ThibautCourtois",
        "luka modric" or "لوكا مودريتش" => "LukaModric",
        "pedri" or "بيدري" => "Pedri",
        "michael olise" or "مايكيل أوليسيه" or "مايكل أوليسيه" or "مايكل أوليسي" => "MichaelOlise",
        "n'golo kante" or "كانتي" or "نجولو كانتي" or "إنجولو كانتي" => "NGoloKante",
        "ngolo kante" => "NGoloKante",
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

