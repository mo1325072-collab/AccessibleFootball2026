namespace AccessibleFootball2026.Core.Models;

/// <summary>On-demand explanation of executed instructions, not guaranteed goals or invented match statistics.</summary>
public static class TacticalEffectsSummary
{
    public static string Describe(Team team)
    {
        var t=team.Tactics;
        string T(string ar,string en)=>LocalizationService.Text(ar,en);
        string text=T("التناغم يبدأ100 ويخصم التعارضات؛ المضاعف هو الدرجة مقسومة100 في1.25 بحد أدنى0.40. الأوصاف الإيجابية لا تضيف نقاطاً فوق100. لا توجد نسبة مضمونة لصناعة الفرص أو الأهداف. ",
            "Coordination starts at 100 and deducts conflicts. Its multiplier is score/100 times 1.25, with a floor of 0.40. Positive descriptions do not add points above 100. No setting guarantees chances or goals. ");
        text+=t.PressingIntensity>75
            ?T("الضغط العالي يزيد عدد الملاحقين ونطاقهم ويتقدم الدعم؛ المكسب استعادة أعلى والتهديد بعد الاستعادة. التكلفة تعب حسب العمر ومساحة خلف الخط المتقدم. ","High press sends more players over a larger range, advancing support. It can recover the ball higher and create a shot after recovery, but costs age-dependent energy and space behind a high line. ")
            :t.PressingIntensity<=25
                ?T("الكتلة المنخفضة تعيد الوسط والهجوم للخلف وتوفر الطاقة، لكنها تقلل ملاحقة بناء الخصم وتترك له أرضاً أكثر. ","Low block drops midfield and forwards and saves energy, at the cost of less pressure on build-up and more territory for the opponent. ")
                :T("الضغط المتوسط يوازن الملاحقة والتراجع؛ الضغط النشط يوسع الملاحقة ويجهد الأكبر سناً في الشوط الثاني. ","Mid pressing balances recovery and retreat; active pressing extends the chase and tires older players in the second half. ");
        text+=t.Marking==DefensiveMarking.ManToMan
            ?T("رجل لرجل يتبع خصماً محدداً لكنه يفتح مساحة إذا سحب الخصم المدافع، ويكشف فارق المهارة. ","Man-to-man follows assigned opponents but can be dragged out of shape and exposes individual skill gaps. ")
            :T("المنطقة تحمي أهداف التمركز والممرات بدلاً من ملاحقة كل لاعب، لكنها لا تضمن إغلاق كل مستقبل حر. ","Zonal marking protects the shape and lanes rather than chasing each opponent, but does not guarantee every free receiver is covered. ");
        text+=t.PassingDirectness>75
            ?T("التمرير المباشر عند الكمبيوتر يفضل مخرجاً أبعد ويرفع الكرة لتجاوز الضغط، مع زمن طيران وكرة متنازع عليها. ","Direct CPU passing favours distant outlets and lifted flight to bypass pressure, but leaves an aerial contest and longer flight time. ")
            :T("التمرير الأقصر عند الكمبيوتر يفضل مخارج قريبة، لكنه يتعرض لاعتراض الممرات والضغط قرب منطقة الجزاء. ","Shorter CPU passing favours nearby outlets but risks lane interceptions and pressure around the penalty area. ");
        text+=T("الريتم السريع يقلل زمن القرار ويزيد سرعة المساندة؛ الهادئ يوفر الطاقة لكنه يتيح عودة الدفاع. اختيار الإنسان لنوع التمريرة والتسديدة يظل بيده. ","Fast tempo reduces decision time and speeds support; slow tempo saves energy but lets defenders recover. Human pass and shot commands remain under human control. ");
        if(t.OffsideTrap)text+=T("المصيدة تتقدم بالمدافعين الجاهزين؛ تحت40 لياقة أو ضعف التركيز يتأخر المدافع. مواقع لحظة الركل واللمس الفعلي هي التي تحكم التسلل، لا مكان استقبال متوقع. ","The trap advances ready defenders; below 40 stamina or with weak concentration a defender lags. Actual kick-time positions and involvement decide offside, not the predicted reception point. ");
        text+=t.Focus switch
        {
            AttackingFocus.Wings=>T("الأجناب توسع الانتشار وتوجه تمريرات الكمبيوتر للأطراف والعرضيات؛ الثمن بعد بعض المخارج عن العمق. ","Wings stretches the team and directs CPU passes wide and crosses from the wing, at the cost of fewer nearby central outlets. "),
            AttackingFocus.Central=>T("العمق يقرب المساندة ويفضل المخارج المركزية، لكنه يضيق العرض أمام تكتل الوسط. ","Central focus narrows support and favours central receivers, but loses width against a packed midfield. "),
            AttackingFocus.LongBall=>T("الطويل يفضل مهاجماً متقدماً، لكنه قد يفقد الكرة في صراع الاستقبال أو التسلل. ","Long-ball focus favours a forward outlet, but risks losing the reception duel or offside. "),
            AttackingFocus.ShootOnSight=>T("التسديد البعيد يتيح للكمبيوتر المحاولة من31متراً عند وجود مساحة؛ المسافة تصعب مسار التسديد ولا تمنح دقة أو أهدافاً مجانية. ","Shoot-on-sight lets the CPU shoot from 31 metres with space; distance still makes the actual shot harder, with no free accuracy or goals. "),
            AttackingFocus.CounterAttack=>T("المرتدة تسرع الدعم والمخارج الأمامية خلال6ثوان من استعادة فعلية، ثم ينتهي امتياز التحول. ","Counter-attacks advance support and forward outlets for six seconds after an actual regain; the transition benefit then expires. "),
            _=>T("المتوازن لا يفرض أولوية جناح أو عمق على مخارج الكمبيوتر. ","Balanced focus does not force a wide or central CPU outlet preference. ")
        };
        text+=T("الحذر يقلل الاندفاع ويترك وقتاً للحامل. الشراسة تزيد قوة المحاولة مع خطر الفاول والبطاقة والإصابة؛ التدخل اليدوي بالجسم قد يخالف تعليمة الحذر. ","Caution reduces commitment and gives the carrier time. Aggression strengthens the challenge with foul, card and injury costs; a human body-first tackle can override cautious instructions. ");
        return text;
    }
}
