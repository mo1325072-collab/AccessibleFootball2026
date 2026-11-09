using System.Collections.Generic;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Audio.Commentary;

public static class CommentatorBank
{
    public static List<CommentatorProfile> GetAllCommentators()
    {
        return new List<CommentatorProfile>
        {
            CreateRaoufKhlif()
        };
    }

    public static CommentatorProfile CreateRaoufKhlif()
    {
        var p = new CommentatorProfile(
            CommentatorId.RaoufKhlif,
            "رؤوف خليف",
            "يوزع، نار يا حبيبي نار!",
            "المعلق الصوتي الرسمي المعتمد بمكتبة الصوتيات الأصلية المستخرجة من PES 2013",
            "تونس / شمال إفريقيا"
        );
        p.AddPhrase("Goal", "يوزع... هدددددددفففففففف! يا عيني يا ليلي! نار يا حبيبي نار في الشباك!");
        p.AddPhrase("Goal", "جول جول جول! تصويبة لا تصد ولا ترد من عالم آخر!");
        p.AddPhrase("Goal", "يا سلاااام سلم! هدف يدرس في فنون الكرة الساحرة!");
        p.AddPhrase("OwnGoal", "كارثة في الدفاع! في مرماه بالخطأ! صدمة غير متوقعة وهدف عكسي مؤسف!");
        p.AddPhrase("Tackle", "استخلاص رائع للكرة وتدخل نظيف في منتهى الذكاء!");
        p.AddPhrase("Shot", "يصوب كرة قوية وخطيرة كادت تباغت الحارس!");
        p.AddPhrase("Miss", "فرصة محققة تضيع بسنتيمترات قليلة! يا للروعة ويا للحسرة!");
        p.AddPhrase("Save", "حارس عملاق يمنع هدفاً مؤكداً بتصدٍ أسطوري!");
        p.AddPhrase("Woodwork", "في القائم! العارضة تتعاطف وتمنع هدفاً رائعاً!");
        p.AddPhrase("Foul", "مخالفة واضحة وصافرة من حكم اللقاء.");
        p.AddPhrase("HalfTime", "صافرة نهاية الشوط الأول للمباراة.");
        p.AddPhrase("FullTime", "صافرة النهاية ونهاية المباراة. شكراً على هذه الوجبة الكروية الممتعة!");
        p.AddPhrase("StoppageTime", "الحكم يشير إلى الوقت المحتسب بدلاً من الضائع.");
        p.AddPhrase("Injury", "سقوط اللاعب على أرضية الميدان وتدخل الجهاز الطبي.");
        return p;
    }
}
