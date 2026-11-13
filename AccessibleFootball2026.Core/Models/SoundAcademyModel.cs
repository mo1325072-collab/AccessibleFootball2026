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
    public static List<SoundAcademyCategory> GetCategories() => new()
    {
        new("earcons", "نغمات التنبيه الدلالية", "Tactical Earcons"),
        new("sfx", "مؤثرات المباراة", "Match sounds"),
        new("atmosphere", "أجواء المدرجات", "Stadium crowd"),
        new("beacon", "منارة المرمى", "Goal beacon"),
        new("anthems", "الأناشيد", "Anthems")
    };

    public static List<SoundAcademyItem> GetItemsByCategory(string categoryId, Func<string, Action>? audioResolver = null) => categoryId switch
    {
        "earcons" => GetEarconItems(audioResolver),
        "sfx" => GetMatchSfxItems(audioResolver),
        "atmosphere" => GetAtmosphereItems(audioResolver),
        "beacon" => GetBeaconItems(audioResolver),
        "anthems" => GetAnthemItems(audioResolver),
        _ => new List<SoundAcademyItem>()
    };

    public static List<SoundAcademyItem> GetAllItems(Func<string, Action>? audioResolver = null)
    {
        var list = new List<SoundAcademyItem>();
        list.AddRange(GetEarconItems(audioResolver));
        list.AddRange(GetMatchSfxItems(audioResolver));
        list.AddRange(GetAtmosphereItems(audioResolver));
        list.AddRange(GetBeaconItems(audioResolver));
        list.AddRange(GetAnthemItems(audioResolver));
        return list;
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

    private static List<SoundAcademyItem> GetMatchSfxItems(Func<string, Action>? resolver) => new()
    {
        new(
            "BallKick",
            "sfx",
            "ركلة الكرة والتمرير (Ball Kick)",
            "Pass Kick Sound",
            "صوت ركل الكرة بالقدم في التمريرات العادية والأرضية وتدوير اللعب بين الزملاء.",
            "Physical foot-to-ball impact sound for standard ground passes and ball rotation.",
            resolver?.Invoke("BallKick")
        ),
        new(
            "PowerfulShot",
            "sfx",
            "التسديدة القوية الصاروخية (Powerful Shot)",
            "Powerful Shot Impact",
            "صوت ركلة عنيفة ومباشرة للكرة بكامل القوة نحو المرمى.",
            "Heavy explosive strike impact sound for powerful shots aimed at goal.",
            resolver?.Invoke("PowerfulShot")
        ),
        new(
            "GoalNet",
            "sfx",
            "اهتزاز الشباك بالهدف (Goal Net Vibration)",
            "Goal Net Rustle",
            "صوت ارتطام الكرة العنيف بالشباك وتمايل حبال المرمى عند تسجيل الأهداف.",
            "Authentic net impact sound of the ball hitting the back of the net on goals.",
            resolver?.Invoke("GoalNet")
        ),
        new(
            "WoodworkHit",
            "sfx",
            "ارتطام القائم والعارضة (Woodwork Post Hit)",
            "Woodwork Post / Crossbar Hit",
            "رنين معدني مدوٍ لارتطام الكرة بالقائم أو العارضة وحرمان اللاعب من هدف محقق.",
            "Resonant metallic clang of the ball rattling off the goal post or crossbar.",
            resolver?.Invoke("WoodworkHit")
        ),
        new(
            "RefereeWhistle",
            "sfx",
            "صافرة الحكم الرسمية (Referee Whistle)",
            "Referee Whistle",
            "صافرة الحكم لإيقاف اللعب في الأخطاء وبداية ونهاية الأشواط والمباراة.",
            "Sharp official referee whistle signaling fouls, half-time, and match stoppages.",
            resolver?.Invoke("RefereeWhistle")
        ),
        new(
            "GoalkeeperSave",
            "sfx",
            "تصدي حارس المرمى (Goalkeeper Save)",
            "Goalkeeper Diving Save",
            "صوت تصدي قفازات الحارس وإبعاد الكرة الصاروخية عن المرمى ببراعة.",
            "Fingertip glove deflection and ground impact sound of a goalkeeper making a save.",
            resolver?.Invoke("GoalkeeperSave")
        )
    };

    private static List<SoundAcademyItem> GetAtmosphereItems(Func<string, Action>? resolver) => new()
    {
        new(
            "CrowdAmbiance",
            "atmosphere",
            "صخب وهتاف المدرجات المستمر (Crowd Ambiance)",
            "Stadium Crowd Murmur & Ambiance",
            "صوت جماهير الملعب المستمر في الخلفية، يتفاعل ويعلو تدريجياً مع اقتراب الهجمة ويهدأ في الوسط.",
            "Continuous stadium crowd murmur that dynamically reacts to attacking build-ups.",
            resolver?.Invoke("CrowdAmbiance")
        ),
        new(
            "GoalRoar",
            "atmosphere",
            "زئير وهتاف الهدف العظيم (Goal Celebration Roar)",
            "Goal Roar & Crowd Celebration",
            "هتاف وزئير المدرجات الصاخب والمحتفل لحظة هز شباك الخصم بهدف.",
            "Massive roaring crowd eruption celebrating an authentic goal scored in the net.",
            resolver?.Invoke("GoalRoar")
        ),
        new(
            "MissGasp",
            "atmosphere",
            "تنهيدة وحسرة الفرصة الضائعة (Miss Murmur & Gasp)",
            "Miss Murmur & Collective Gasp",
            "تفاعل وحسرة الجماهير عند مرور تسديدة خطيرة بجوار القائم أو ضياع فرصة مؤكدة.",
            "Collective gasp and murmur of disappointment as a close shot misses.",
            resolver?.Invoke("MissGasp")
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
        )
    };

    private static List<SoundAcademyItem> GetAnthemItems(Func<string, Action>? resolver) => new()
    {
        new(
            "ChampionsLeagueAnthem",
            "anthems",
            "نشيد دوري أبطال أوروبا الرسمي (UEFA Champions League)",
            "UEFA Champions League Official Anthem",
            "النشيد الرسمي التاريخي لبطولة دوري أبطال أوروبا، يعزف عند انطلاق المباريات الكبرى والنهائيات.",
            "The iconic UEFA Champions League orchestral anthem played before premier continental matches.",
            resolver?.Invoke("ChampionsLeagueAnthem")
        )
    };
}
