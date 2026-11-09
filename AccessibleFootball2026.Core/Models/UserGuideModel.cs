using System;
using System.Collections.Generic;

namespace AccessibleFootball2026.Core.Models;

public record UserGuideCategory(string Id, string ArabicTitle, string EnglishTitle);

public record UserGuideItem(
    string Id,
    string CategoryId,
    string KeyBinding,
    string ArabicTitle,
    string EnglishTitle,
    string ArabicDescription,
    string EnglishDescription)
{
    public string GetTitle(UiLanguage lang) => lang == UiLanguage.English ? EnglishTitle : ArabicTitle;
    public string GetDescription(UiLanguage lang) => lang == UiLanguage.English ? EnglishDescription : ArabicDescription;
}

public static class UserGuideDatabase
{
    public static List<UserGuideCategory> GetCategories() => new()
    {
        new("controls", "أزرار التحكم وحركات اللعب", "Controls & Gameplay Actions"),
        new("rules", "قوانين التحكيم والتبديلات (IFAB)", "IFAB Rules & Substitutions"),
        new("radar_beacon", "الرادار الصوتي ومنارة المرمى 3D", "Tactical Radar & 3D Spatial Goal Beacon"),
        new("penalties_extratime", "ركلات الترجيح والأشواط الإضافية", "Extra Time & Penalty Shootouts"),
        new("modes", "أوضاع اللعب والبطولات", "Game Modes & Tournaments")
    };

    public static List<UserGuideItem> GetItemsByCategory(string categoryId) => categoryId switch
    {
        "controls" => GetControlsItems(),
        "rules" => GetRulesItems(),
        "radar_beacon" => GetRadarBeaconItems(),
        "penalties_extratime" => GetExtraTimePenaltiesItems(),
        "modes" => GetModesItems(),
        _ => new List<UserGuideItem>()
    };

    public static List<UserGuideItem> GetAllItems()
    {
        var list = new List<UserGuideItem>();
        list.AddRange(GetControlsItems());
        list.AddRange(GetRulesItems());
        list.AddRange(GetRadarBeaconItems());
        list.AddRange(GetExtraTimePenaltiesItems());
        list.AddRange(GetModesItems());
        return list;
    }

    private static List<UserGuideItem> GetControlsItems() => new()
    {
        new(
            "short_pass",
            "controls",
            "S",
            "زر S: التمرير الأرضي المباشر",
            "S Key: Short Pass",
            "تمرير الكرة أرضياً نحو أقرب زميل متاح في اتجاه حركتك، ويتحرك الزميل بذكاء لاستقبالها واستلامها بسلاسة.",
            "Direct ground pass to the closest open teammate in your moving direction. Teammates actively intercept and receive the ball smoothly."
        ),
        new(
            "cross_lob",
            "controls",
            "D",
            "زر D: الرفعة والعرضيات العالية",
            "D Key: Cross & High Lob",
            "رفع الكرة عالياً في الهواء نحو منطقة الجزاء لاقتناص الكرات العرضية الرأسية أو إرسال كرات ساقطة خلف المدافعين.",
            "High lofted ball or cross into the opponent's penalty box for headed finishes or over defensive backlines."
        ),
        new(
            "shoot",
            "controls",
            "W",
            "زر W: التسديد القوي المباشر",
            "W Key: Powerful Shot",
            "إطلاق تسديدة صاروخية قوية ومباشرة نحو مرمى الخصم الحالي، وتتأثر دقة وقوة التسديدة بزاوية توجيه اللاعب.",
            "Unleashes a powerful direct shot towards the active opponent goal. Shot power and precision are affected by aiming angles."
        ),
        new(
            "through_ball",
            "controls",
            "A",
            "زر A: التمريرة البينية في المساحة",
            "A Key: Through Ball",
            "تمرير الكرة بين خطوط دفاع الخصم في المساحة الفارغة للزميل المنطلق بسرعة لكسر مصيدة التسلل والانفراد بالمرمى.",
            "Passes the ball into open space ahead of a sprinting teammate to break defensive lines and create 1-on-1 chances."
        ),
        new(
            "switch_defender",
            "controls",
            "Ctrl",
            "زر Ctrl: تبديل المدافع الأقرب",
            "Ctrl Key: Switch Defender",
            "التبديل الفوري والسلس لأقرب مدافع متمركز في الملعب، مصحوباً بنقرة صوتية خاطفة وبصمت تام لقارئ الشاشة لعدم تشتيت التركيز.",
            "Instantly switches control to the closest outfield defender with a subtle tactical earcon and complete screen reader silence."
        ),
        new(
            "sprint",
            "controls",
            "Shift",
            "زر Shift: الجري السريع",
            "Shift Key: Sprint",
            "الضغط مع الاستمرار أثناء التحرك بالأسهم يمنح اللاعب دفعة سرعة بدنية بنسبة 160% لاختراق دفاعات الخصم أو اللحاق بالمهاجمين.",
            "Holding Shift while moving with arrow keys boosts sprint speed by 160% to burst into open space or track back on defense."
        ),
        new(
            "tactical_radar",
            "controls",
            "Space",
            "زر Space: الرادار التكتيكي عند الطلب",
            "Space Key: Tactical Radar",
            "الضغط على المسافة ينطق تقريراً تكتيكياً فورياً وموجزاً: من معه الكرة، في أي منطقة، والمسافة بالأمتار لمرمى الخصم النشط.",
            "Pressing Space announces an on-demand high-signal telemetry report: who holds possession, pitch zone, and distance to opponent goal."
        ),
        new(
            "pause_gameplan",
            "controls",
            "Esc",
            "زر Escape: الإيقاف المؤقت وخطة اللعب",
            "Escape Key: Pause & Game Plan",
            "إيقاف المباراة فورياً وفتح قائمة المباراة التكتيكية لإجراء التبديلات، تغيير التشكيلة، ومراجعة إحصائيات اللقاء.",
            "Instantly pauses live gameplay and opens the in-match tactical menu to make substitutions, adjust formations, or check match stats."
        ),
        new(
            "aiming_angles",
            "controls",
            "Up/Down",
            "السهمان الأعلى والأسفل: توجيه زوايا التسديد والتمرير",
            "Up/Down Arrows: Aiming Angles",
            "الضغط على السهمين الأعلى والأسفل يغير زاوية ميلان ومواجهة اللاعب تجاه القائمين الأيمن والأيسر قبل التسديد (W) أو التمرير (S/D).",
            "Pressing Up and Down arrow keys dynamically modifies player facing and aiming angles towards the top and bottom corners of the goal."
        )
    };

    private static List<UserGuideItem> GetRulesItems() => new()
    {
        new(
            "law3_subs",
            "rules",
            "Law 3",
            "قانون 3: خمس تبديلات في ثلاث فترات توقف",
            "Law 3: 5 Substitutions in 3 Windows",
            "يسمح لكل فريق بإجراء 5 تبديلات كحد أقصى خلال 3 فترات توقف للمباراة في الـ 90 دقيقة. تبديلات استراحة بين الشوطين لا تستهلك أي فترة توقف. تعديل الخطط والمراكز بدون تبديل لاعبين لا يستهلك أي فترة توقف. في الأشواط الإضافية يمنح كل فريق تبديلاً سادساً إضافياً وفترة توقف رابعة.",
            "Each team is permitted up to 5 substitutions across a maximum of 3 stoppage opportunities in regulation 90 minutes. Half-time substitutions do not consume a window. Tactical adjustments without substitutions never consume a window. In Extra Time, teams are granted a 6th sub and a 4th window."
        ),
        new(
            "law12_cards",
            "rules",
            "Law 12",
            "قانون 12: العقوبات الانضباطية وحالات الطرد DOGSO",
            "Law 12: Disciplinary Sanctions & DOGSO",
            "تشهر البطاقة الصفراء للتدخلات المتهورة أو إيقاف هجمة واعدة (SPA). حصول اللاعب على بطاقتين صفراوين يعني طرده بالبطاقة الحمراء. تشهر البطاقة الحمراء المباشرة للعرقلة العنيفة من الخلف أو حرمان الخصم من فرصة هدف محققة (DOGSO) كآخر مدافع. يغادر اللاعب المطرود الملعب ويكمل الفريق بـ 10 لاعبين مع تكييف التشكيلة تلقائياً.",
            "Yellow cards are issued for reckless challenges or stopping a promising attack (SPA). A second yellow produces an automatic red card dismissal. Direct red cards are shown for violent tackles from behind or Denying an Obvious Goal-Scoring Opportunity (DOGSO). Dismissed players leave the pitch; team continues with 10 players."
        ),
        new(
            "injuries_system",
            "rules",
            "Injuries",
            "نظام الإصابات واللياقة البدنية",
            "Player Injuries & Stamina System",
            "تؤدي التدخلات القوية أحياناً إلى كدمات خفيفة تخفض سرعة ولياقة اللاعب مؤقتاً بنسبة 20%. وفي حال الإصابة الشديدة، يستلزم إجراء تبديل إجباري، وإذا كانت التبديلات الـ 5 مستنفدة يكمل الفريق بـ 10 لاعبين.",
            "Hard tackles can inflict minor knocks that reduce player speed and stamina by 20%. Severe injuries force an immediate substitution; if all substitutions are exhausted, the team must complete the match with 10 players."
        )
    };

    private static List<UserGuideItem> GetRadarBeaconItems() => new()
    {
        new(
            "tactical_radar_guide",
            "radar_beacon",
            "Space",
            "الرادار التكتيكي الصوتي (Tactical Radar)",
            "Tactical Audio Radar",
            "يوفر إحاطة مكانية شاملة للمكفوفين عند الطلب بالضغط على زر المسافة، فينطق بدقة من معه الكرة (أنت أو زميلك أو الخصم)، المنطقة الحالية في الملعب، والمسافة الدقيقة لمرمى الخصم بالأمتار، مع صمت تام أثناء الجري بالكرة لعدم تشويش التجربة الصوتية.",
            "Provides complete spatial awareness on demand by pressing Space: announces ball carrier, pitch zone, and distance to opponent goal in meters, while keeping screen reader 100% silent during active ball running."
        ),
        new(
            "spatial_goal_beacon",
            "radar_beacon",
            "Beacon",
            "منارة المرمى ثلاثية الأبعاد (3D Spatial Goal Beacon)",
            "3D Spatial Goal Beacon",
            "نبضات صوتية هادئة ومتناغمة بتردد 528 هرتز (Harmonic Solfeggio) تسترشد بها نحو مرمى الخصم. يعلو صوت المنارة تدريجياً وبنعومة كلما اقتربت من المرمى وتهدأ في منتصف الملعب، وتتوزع مجسماً بين الأذنين (Stereo Panning) بناءً على موقعك العرضي، وتنقلب تلقائياً في الشوط الثاني نحو المرمى الأيسر.",
            "A gentle, non-intrusive harmonic 528Hz pulse guiding you directly towards the opponent's goal. Volume smoothly swells as you approach the attacking third, while stereo panning dynamically reflects your lateral angle, automatically flipping to target the Left goal in the second half."
        )
    };

    private static List<UserGuideItem> GetExtraTimePenaltiesItems() => new()
    {
        new(
            "extra_time_rules",
            "penalties_extratime",
            "Extra Time",
            "قواعد الأشواط الإضافية",
            "Extra Time Rules",
            "عند انتهاء الوقت الأصلي بالتعادل وتفعيل خيار الأشواط الإضافية، تلعب المباراة شوطين إضافيين مدة كل منهما نصف مدة الشوط العادي، مع تبديل المرميين ومنح كل فريق تبديلاً سادساً إضافياً وفترة توقف رابعة.",
            "When regulation ends in a draw and Extra Time is enabled, two extra periods are played (half duration each), teams swap sides, and an extra 6th substitution and 4th stoppage window are granted."
        ),
        new(
            "penalty_shootouts",
            "penalties_extratime",
            "Penalties",
            "نظام ركلات الترجيح الحاسمة",
            "Penalty Shootout Rules & Mechanics",
            "عند استمرار التعادل، تنطلق ركلات الترجيح بـ 5 ركلات لكل فريق. في دور التسديد تختار زاوية الركل (اليمين، الوسط، اليسار). وفي دور الدفاع تختار زاوية ارتماء حارسك لتصدي تسديدة الخصم، مع مؤثرات ارتداد الشباك وصخب الجماهير.",
            "If the score remains tied, a 5-round penalty shootout begins. When shooting, choose your target angle (right, center, left). When defending, pick your goalkeeper's diving direction to make heroic saves with crowd celebrations."
        )
    };

    private static List<UserGuideItem> GetModesItems() => new()
    {
        new(
            "exhibition_mode_guide",
            "modes",
            "Exhibition",
            "مباراة ودية (Exhibition Match)",
            "Exhibition Match Mode",
            "مباراة فردية سريعة وكاملة تختار فيها ناديك أو منتخبك المفضل في مواجهة خصم من اختيارك، مع تحديد المعلق الصوتي المعتمد وملعب اللقاء، وقواعد الأشواط الإضافية وركلات الترجيح.",
            "A fast and full single match where you select your preferred club or national team against any opponent, with your chosen commentator, extra time, and penalty shootout settings."
        ),
        new(
            "draft_mode_guide",
            "modes",
            "Draft",
            "وضع الـ 4 فرق (4-Clubs Mode)",
            "4-Clubs Match Mode",
            "وضع تفاعلي استراتيجي تختار فيه 4 أندية لتشكيلة فريقك و4 أندية لفريق الخصم، ثم يقوم الكمبيوتر بسحب قرعة عشوائية متوازنة لتشكيل الفريقين، مع تفعيل نظام مانع التكرار الصوتي لضمان عدم وجود أي لاعب مكرر.",
            "Strategic draft mode where you choose 4 clubs for your team and 4 for the CPU. A balanced random draft builds both squads, and the anti-duplication engine ensures no player is duplicated."
        ),
        new(
            "tournaments_mode_guide",
            "modes",
            "Tournaments",
            "البطولات والدوريات الرسمية (Tournaments & Leagues)",
            "Official Tournaments & Leagues",
            "استعراض شامل لجميع البطولات الرسمية المعتمدة لموسم 2026 و2027 بما في ذلك دوري أبطال أوروبا الحقيقي وكأس العالم والدوريات الكبرى، مع قوائم الأندية والأناشيد الرسمية.",
            "Full browser of all official competitions for 2026/2027 including the UEFA Champions League and World Cup, complete with official anthems and participating squads."
        ),
        new(
            "game_plan_guide",
            "modes",
            "Game Plan",
            "خطة اللعب وإدارة التكتيكات (Game Plan)",
            "Game Plan & Tactical Management",
            "إدارة تكتيكية كاملة لتشكيلتك: التشكيلة الأساسية (11 لاعباً)، دكة البدلاء، تغيير المراكز، تعيين المتخصصين (القائد، ضربات الجزاء، الفاولات، الركنيات)، والتبديل الفوري بين الخطط التكتيكية.",
            "Complete tactical control over your squad: starting 11 lineup, bench substitutions, position adjustments, assigning specialists (captain, penalties, free kicks, corners), and formation changes."
        )
    };
}
