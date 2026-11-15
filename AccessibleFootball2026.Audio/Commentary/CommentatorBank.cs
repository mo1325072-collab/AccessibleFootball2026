using System.Collections.Generic;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Audio.Commentary;

public static class CommentatorBank
{
    public static List<CommentatorProfile> GetAllCommentators()
    {
        // فارس عوض هو المعلق الوحيد المعتمد حالياً بطلب المستخدم؛ رؤوف خليف أُزيل حتى إشعار آخر
        return new List<CommentatorProfile>
        {
            CreateFaresAwad()
        };
    }

    public static CommentatorProfile CreateFaresAwad()
    {
        var p = new CommentatorProfile(
            CommentatorId.FaresAwad,
            "فارس عوض",
            "يا رباااه!",
            "مكتبة تعليق فارس عوض (PES PSP) المصنّفة دلالياً من أرشيف المستخدم؛ تُستخدم هذه الجمل النصية فقط عند غياب مقطع مسجل للحدث",
            "الإمارات / الخليج"
        );
        p.AddPhrase("Goal", "يا رباااه! الكرة في الشباك! هدف يحرك الملعب ويشعل الأجواء!");
        p.AddPhrase("Goal", "الله الله الله! هدف يحرك الشباك ويشعل المدرجات!");
        p.AddPhrase("OwnGoal", "يا الله! هدف عكسي! الكرة في المرمى بالخطأ!");
        p.AddPhrase("Tackle", "يتقدم في توقيت مثالي ويفتك الكرة بشكل ممتاز!");
        p.AddPhrase("Shot", "فرصة خطيرة الآن والكرة في طريقها نحو المرمى!");
        p.AddPhrase("Miss", "أووووه أضاعها! كانت كرة قريبة جداً لكنها تخطئ المرمى!");
        p.AddPhrase("Save", "مستحيل هذه الكرة! أبعدها الحارس وأنقذ هدفاً محققاً!");
        p.AddPhrase("Woodwork", "الكرة في القائم! العارضة ترفض الهدف!");
        p.AddPhrase("Foul", "صافرة الحكم تعلن عن وجود خطأ.");
        p.AddPhrase("HalfTime", "الحكم يطلق صافرة نهاية الشوط الأول.");
        p.AddPhrase("FullTime", "صافرة النهاية تأتي لتعلن نهاية المباراة.");
        p.AddPhrase("StoppageTime", "الحكم يشير إلى الوقت المحتسب بدلاً من الضائع.");
        p.AddPhrase("Injury", "سقوط اللاعب على أرضية الملعب وتوقف اللعب.");
        p.AddPhrase("Card", "الحكم يخرج البطاقة في هذه اللقطة.");
        p.AddPhrase("RedCard", "بطاقة حمراء! طرد في هذه اللقطة!");
        p.AddPhrase("Penalty", "ركلة جزاء! الحكم يشير إلى علامة الجزاء!");
        return p;
    }
}
