using AccessibleFootball2026.Audio;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Audio.Commentary;
namespace AccessibleFootball2026.App.Menus;
public static class CommentaryLibraryMenuBuilder
{
    private static string T(string ar, string en) => LocalizationService.Text(ar, en);
    private static string Status(string code)=>code switch
    {
        "reviewed"=>T("مقاطع ذات ربط سياقي مرشح", "Recordings with candidate context mappings"),
        "identity_pending"=>T("أسماء لم تتأكد هويتها", "Unverified names"),
        "coach_voice_pending"=>T("أصوات مدربين تحتاج مراجعة اللغة والمعنى", "Coach voices awaiting language and meaning review"),
        "speech_review_pending"=>T("كلام لم يتأكد معناه", "Speech awaiting meaning review"),
        "non_speech_pending"=>T("مقاطع غير صفرية لم يتحدد محتواها", "Non-silent recordings awaiting review"),
        "user_excluded"=>T("مقاطع استبعدها المستخدم من المطلوب ومن التشغيل التلقائي، محفوظة للاستماع فقط", "User-excluded recordings, listening only"),
        "non_game_promo"=>T("مقدمات ونسب التعليق لصاحبه، لا تستخدم أثناء المباراة", "Introductions and credits, not match commentary"),
        "context_asset_pending"=>T("تحتاج مؤثرات جماهير أصلية وسياقاً مسموعاً مؤكداً", "Awaiting original crowd effects and verified context"),
        "context_history_pending"=>T("تصف واقعة تاريخية لا تثبتها مباراة عادية", "Historical claims not established in this match"),
        "context_emotion_pending"=>T("تصف مشاعر لم يثبت سياقها في اللعبة", "Emotional context not established"),
        "context_visual_pending"=>T("تصف تعبير وجه غير ممثل في اللعبة الحالية", "Facial expressions not represented in this game"),
        "digital_zero"=>T("الخانة الصامتة رقمياً وتكراراتها الأصلية", "Digitally silent slots and original duplicates"),
        "nonzero_fragment"=>T("شذرات صوتية قصيرة من الخانات الثنائية", "Short non-silent binary fragments"),
        _=>code
    };
    public static AccessibleMenu Create(CommentaryEngine commentary,IScreenReader reader,Action<AccessibleMenu> push,Action pop)
    {
        var library=commentary.LoadFaresLibrary();
        var menu=new AccessibleMenu(T("مكتبة التسجيلات الأصلية: الاستماع منفصل عن التشغيل التلقائي", "Original recording library"),reader);
        if(library.Error!=null)menu.AddItem(T("لم تثبت حزمة المكتبة الكاملة أو تعذر قراءتها", "Recording library unavailable"),()=>reader.Speak(T("ثبت حزمة مكتبة فارس الكاملة بجوار أصول الصوت. التعليق القديم لم يحذف.", "The full Fares library is not installed beside the audio assets. Existing commentary is preserved.")));
        foreach(var group in library.Recordings.GroupBy(r=>r.ReviewStatus))
        {
            var recordings=group.ToArray();string title=Status(group.Key);
            menu.AddItem(T($"{title}: {recordings.Length} تسجيل", $"{title}: {recordings.Length} recordings"),()=>
            {
                var pages=new AccessibleMenu(title,reader);
                for(int start=0;start<recordings.Length;start+=25)
                {
                    var pageItems=recordings.Skip(start).Take(25).ToArray();int from=start+1,to=Math.Min(start+25,recordings.Length);
                    pages.AddItem(T($"التسجيلات من {from} إلى {to}", $"Recordings {from} to {to}"),()=>
                    {
                        var page=new AccessibleMenu(T($"{title}، من {from} إلى {to}", $"{title}, {from} to {to}"),reader);
                        foreach(var item in pageItems)
                        {
                            var recording=item;
                            string description=recording.ReviewStatus=="reviewed"?recording.Transcript:T("المعنى أو الاستخدام غير معتمد. الاستماع للأصل فقط.", "Meaning or use is not approved. Original listening only.");
                            page.AddItem(T($"مقطع {recording.Slot}، {recording.OriginalSlots.Length} خانة أصلية. {description}", $"Clip {recording.Slot}, {recording.OriginalSlots.Length} original slots. {description}"),()=>
                            {
                                reader.CancelSpeech();
                                SoundEffectsPlayer.StopMenuMusic();
                                commentary.PlayLibraryRecording(library,recording);
                            },suppressSelectionSound:true);
                        }
                        page.AddItem(T("رجوع", "Back"),pop);push(page);
                    });
                }
                pages.AddItem(T("رجوع", "Back"),pop);push(pages);
            });
        }
        menu.AddItem(T("رجوع", "Back"),pop);return menu;
    }
}
