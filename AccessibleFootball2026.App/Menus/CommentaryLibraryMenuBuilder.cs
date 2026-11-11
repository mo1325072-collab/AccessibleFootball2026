using AccessibleFootball2026.Audio;
using AccessibleFootball2026.Audio.Commentary;
namespace AccessibleFootball2026.App.Menus;
public static class CommentaryLibraryMenuBuilder
{
    private static string Status(string code)=>code switch
    {
        "reviewed"=>"مقاطع ذات ربط سياقي مرشح",
        "identity_pending"=>"أسماء لم تتأكد هويتها",
        "coach_voice_pending"=>"أصوات مدربين تحتاج مراجعة اللغة والمعنى",
        "speech_review_pending"=>"كلام لم يتأكد معناه",
        "non_speech_pending"=>"مقاطع غير صفرية لم يتحدد محتواها",
        "non_game_promo"=>"مقدمات ونسب التعليق لصاحبه، لا تستخدم أثناء المباراة",
        "context_asset_pending"=>"تحتاج مؤثرات جماهير أصلية وسياقاً مسموعاً مؤكداً",
        "context_emotion_pending"=>"تصف مشاعر لم يثبت سياقها في اللعبة",
        "context_visual_pending"=>"تصف تعبير وجه غير ممثل في اللعبة الحالية",
        "digital_zero"=>"الخانة الصامتة رقمياً وتكراراتها الأصلية",
        "nonzero_fragment"=>"شذرات صوتية قصيرة من الخانات الثنائية",
        _=>code
    };
    public static AccessibleMenu Create(CommentaryEngine commentary,IScreenReader reader,Action<AccessibleMenu> push,Action pop)
    {
        var library=commentary.LoadFaresLibrary();
        var menu=new AccessibleMenu("مكتبة التسجيلات الأصلية: الاستماع منفصل عن التشغيل التلقائي",reader);
        if(library.Error!=null)menu.AddItem("لم تثبت حزمة المكتبة الكاملة أو تعذر قراءتها",()=>reader.Speak("ثبت حزمة مكتبة فارس الكاملة بجوار أصول الصوت. التعليق القديم لم يحذف."));
        foreach(var group in library.Recordings.GroupBy(r=>r.ReviewStatus))
        {
            var recordings=group.ToArray();string title=Status(group.Key);
            menu.AddItem($"{title}: {recordings.Length} تسجيل",()=>
            {
                var pages=new AccessibleMenu(title,reader);
                for(int start=0;start<recordings.Length;start+=25)
                {
                    var pageItems=recordings.Skip(start).Take(25).ToArray();int from=start+1,to=Math.Min(start+25,recordings.Length);
                    pages.AddItem($"التسجيلات من {from} إلى {to}",()=>
                    {
                        var page=new AccessibleMenu($"{title}، من {from} إلى {to}",reader);
                        foreach(var item in pageItems)
                        {
                            var recording=item;
                            string description=recording.ReviewStatus=="reviewed"?recording.Transcript:"المعنى أو الاستخدام غير معتمد. الاستماع للأصل فقط.";
                            page.AddItem($"مقطع {recording.Slot}، {recording.OriginalSlots.Length} خانة أصلية. {description}",()=>
                            {
                                reader.CancelSpeech();
                                SoundEffectsPlayer.StopMenuMusic();
                                commentary.PlayLibraryRecording(library,recording);
                            },suppressSelectionSound:true);
                        }
                        page.AddItem("رجوع",pop);push(page);
                    });
                }
                pages.AddItem("رجوع",pop);push(pages);
            });
        }
        menu.AddItem("رجوع",pop);return menu;
    }
}
