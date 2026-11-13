using System;
using System.Collections.Generic;

namespace AccessibleFootball2026.Core.Models;

public record SoundAcademyCategory(string Id, string ArabicTitle, string EnglishTitle);

public record SoundAcademyItem(
    string Id,
    string CategoryId,
    string ArabicName,
    string EnglishName,
    string ArabicDescription,
    string EnglishDescription,
    Action? PlayAudioAction = null)
{
    public string GetName(UiLanguage lang) => lang == UiLanguage.English ? EnglishName : ArabicName;
    public string GetDescription(UiLanguage lang) => lang == UiLanguage.English ? EnglishDescription : ArabicDescription;
}

public static class SoundAcademyDatabase
{
    // A flat academy, with no category navigation. Legacy lookups expose only allowed cues.
    public static List<SoundAcademyCategory> GetCategories() => new();
    public static List<SoundAcademyItem> GetItemsByCategory(string categoryId, Func<string, Action>? audioResolver=null) => categoryId switch
    {
        "earcons" => GetEarconItems(audioResolver),
        "beacon" => GetBeaconItems(audioResolver),
        _ => new()
    };
    public static List<SoundAcademyItem> GetAllItems(Func<string, Action>? audioResolver=null)
    {
        var items=GetEarconItems(audioResolver);
        items.AddRange(GetBeaconItems(audioResolver));
        return items;
    }

    private static List<SoundAcademyItem> GetEarconItems(Func<string, Action>? resolver) => new()
    {
        new(
            "TackleWon",
            "earcons",
            "1. استخلاص الكرة بنجاح (Tackle Won)",
            "1. Clean Tackle Won",
            "نغمة تصاعدية خماسية C5 إلى G5 تؤكد نجاح استخلاص الكرة وافتكاكها بأمان تحت أقدام لاعبك.",
            "Ascending major fifth (C5 to G5) confirming a clean tackle and immediate ball recovery.",
            resolver?.Invoke("TackleWon")
        ),
        new(
            "TackleMissed",
            "earcons",
            "2. فشل الاستخلاص لبُعد المسافة (Tackle Missed)",
            "2. Tackle Missed (Out of Reach)",
            "نغمة هابطة خافتة مع حفيف هوائي تشير إلى محاولة استخلاص في الهواء لبُعد المسافة عن الكرة.",
            "Subtle descending tone with air whoosh indicating tackle struck empty air due to distance.",
            resolver?.Invoke("TackleMissed")
        ),
        new(
            "BallLost",
            "earcons",
            "3. فقدان الكرة لصالح الخصم (Ball Lost)",
            "3. Ball Possession Lost",
            "نغمة هابطة ثلاثية Eb4 إلى C4 تنبهك فورياً بفقدان الاستحواذ على الكرة للخصم للتحول للدفاع.",
            "Descending minor third (Eb4 to C4) alerting you of possession turnover to defend.",
            resolver?.Invoke("BallLost")
        ),
        new(
            "Foul",
            "earcons",
            "4. خطأ تحكيمي وصافرة (Foul)",
            "4. Foul Called",
            "نغمة متنافرة ثلاثية F#4 و C5 تنبه بارتكاب خطأ تحكيمي وتوقف اللعب لاحتساب ركلة حرة.",
            "Dissonant tritone clash (F#4 and C5) alerting you of a referee foul call.",
            resolver?.Invoke("Foul")
        ),
        new(
            "Penalty",
            "earcons",
            "5. ركلة جزاء حاسمة (Penalty)",
            "5. Penalty Awarded",
            "نغمة درامية حماسية تصاعدية A4 إلى A5 تعلن عن احتساب ركلة جزاء داخل منطقة الجزاء.",
            "Dramatic ascending octave sweep (A4 to A5) signaling a crucial penalty kick awarded.",
            resolver?.Invoke("Penalty")
        ),
        new(
            "Corner",
            "earcons",
            "6. ركلة ركنية (Corner)",
            "6. Corner Kick Awarded",
            "تتابع نغمي ثلاثي صاعد G4 ثم C5 ثم E5 يعلن عن ركلة ركنية لصالح الفريق.",
            "Major arpeggio triad (G4 to C5 to E5) announcing a corner kick awarded.",
            resolver?.Invoke("Corner")
        ),
        new(
            "GoalKick",
            "earcons",
            "7. ضربة مرمى (Goal Kick)",
            "7. Goal Kick Awarded",
            "نغمة جهورية هادئة منخفضة C3 إلى G3 تعلن عن خروج الكرة لضربة مرمى.",
            "Low resonant descending fifth (C3 to G3) announcing a defensive goal kick.",
            resolver?.Invoke("GoalKick")
        ),
        new(
            "ThrowIn",
            "earcons",
            "8. رمية تماس (Throw-In)",
            "8. Throw-In",
            "نقرتان خشبيتان سريعتان تعلنان خروج الكرة لرمية تماس جانبية.",
            "Two swift wooden clicks signaling the ball crossed the touchline for a throw-in.",
            resolver?.Invoke("ThrowIn")
        ),
        new(
            "PlayerSwitch",
            "earcons",
            "9. تبديل المدافع التكتيكي (Player Switch)",
            "9. Defender Switch Micro-Click",
            "نقرة صوتية خاطفة بتردد 1100 هرتز تؤكد التبديل الفوري للمدافع الأقرب بصمت تام لقارئ الشاشة.",
            "Tactile 1100Hz micro-click confirming instant defender switch with screen reader silence.",
            resolver?.Invoke("PlayerSwitch")
        )
    };

    private static List<SoundAcademyItem> GetBeaconItems(Func<string, Action>? resolver) => new()
    {
        new(
            "BeaconCenter",
            "beacon",
            "منارة المرمى: متوازنة في المنتصف (Center Panning)",
            "3D Goal Beacon (Direct Center)",
            "نبضة منارة المرمى 528 هرتز متوازنة في المنتصف تماماً، تشير إلى أنك في مواجهة مباشرة مع المرمى للتسديد.",
            "Balanced 528Hz goal beacon pulse indicating player is directly aligned with goal center.",
            resolver?.Invoke("BeaconCenter")
        ),
        new(
            "BeaconLeft",
            "beacon",
            "منارة المرمى: في الأذن اليسرى (Left Ear Panning)",
            "3D Goal Beacon (Panned Left)",
            "نبضة منارة المرمى مائلة نحو الأذن اليسرى، تشير إلى أن المرمى يقع إلى يسارك على الجناح.",
            "Goal beacon pulse panned to the left ear indicating the goal is to your left side.",
            resolver?.Invoke("BeaconLeft")
        ),
        new(
            "BeaconRight",
            "beacon",
            "منارة المرمى: في الأذن اليمنى (Right Ear Panning)",
            "3D Goal Beacon (Panned Right)",
            "نبضة منارة المرمى مائلة نحو الأذن اليمنى، تشير إلى أن المرمى يقع إلى يمينك على الجناح.",
            "Goal beacon pulse panned to the right ear indicating the goal is to your right side.",
            resolver?.Invoke("BeaconRight")
        ),
        new("BeaconNear","beacon","منارة المرمى: قريب وقوي","Goal beacon: near and louder","عندما تقترب من المرمى تزداد قوة النبضة.","The pulse becomes louder as you approach the goal.",resolver?.Invoke("BeaconNear")),
        new("BeaconFar","beacon","منارة المرمى: بعيد وخافت","Goal beacon: far and softer","كلما ابتعدت تقل قوة النبضة حتى حد السماع.","The pulse becomes softer farther from goal, up to the audible limit.",resolver?.Invoke("BeaconFar")),
        new("BeaconOutOfRange","beacon","منارة المرمى: خارج مدى السماع (صمت)","Goal beacon: outside audible range (silence)","خارج نطاق 52.5 متر تسكت المنارة. هذه عينة صامتة مقصودة.","Outside 52.5 metres the beacon is silent. This preview is intentionally silent.",resolver?.Invoke("BeaconOutOfRange")),
        new("BeaconNoPossession","beacon","منارة المرمى: بدون استحواذ (صمت)","Goal beacon: no possession (silence)","بدون استحواذك لا تصدر المنارة نبضة. هذه عينة صامتة مقصودة.","Without your possession the beacon emits no pulse. This preview is intentionally silent.",resolver?.Invoke("BeaconNoPossession")),
        new("BeaconSecondHalfLeft","beacon","منارة المرمى: اتجاه الشوط الثاني لليسار","Goal beacon: second-half left direction","في الشوط الثاني ينعكس اتجاه المنارة مع تبديل جهة الهجوم.","In the second half panning reverses with the attacking direction.",resolver?.Invoke("BeaconSecondHalfLeft")),
        new("BeaconSecondHalfRight","beacon","منارة المرمى: اتجاه الشوط الثاني لليمين","Goal beacon: second-half right direction","بعد تبديل المرميين يكون الاتجاه بالنسبة لجهة هجومك الجديدة.","After switching ends the direction is relative to your new attacking goal.",resolver?.Invoke("BeaconSecondHalfRight"))
    };

}
