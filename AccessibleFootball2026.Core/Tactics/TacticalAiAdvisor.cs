using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;

namespace AccessibleFootball2026.Core.Tactics;

/// <summary>
/// المستشار التكتيكي بالذكاء الاصطناعي (AI Tactical Advisor):
/// يحلل مجريات المباراة، وإجهاد اللاعبين، وثغرات الخصم، ويقترح تبديلات وخططاً ذكية
/// يدعم وضع الاستشارة المباشرة في قائمة الإيقاف المؤقت، والتبديل التلقائي الاختياري
/// </summary>
public class TacticalAiAdvisor
{
    public class TacticalRecommendation
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public FormationType? SuggestedFormation { get; set; }
        public Player? PlayerToSubOut { get; set; }
        public Player? PlayerToSubIn { get; set; }
        public Action? ApplyAction { get; set; }
    }

    public static string LlamaCliPath => @"D:\AccessibleFootball2026\Models\llama-bin\llama-cli.exe";
    public static string ModelPath => @"D:\AccessibleFootball2026\Models\qwen2.5-3b-instruct-q4_k_m.gguf";

    public static bool IsNeuralModelAvailable => File.Exists(LlamaCliPath) && File.Exists(ModelPath) && new FileInfo(ModelPath).Length > 1_500_000_000;

    public static string QueryNeuralQwenModel(string prompt, int maxTokens = 30, int timeoutSeconds = 8)
    {
        if (!IsNeuralModelAvailable) return string.Empty;

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = LlamaCliPath,
                Arguments = $"-m \"{ModelPath}\" -p \"{prompt.Replace("\"", "\\\"")}\" -n {maxTokens} -c 512 --single-turn --simple-io --no-display-prompt -t 4",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process == null) return string.Empty;

            // إرسال EOF فوري لـ StandardInput لمنع انتظار الإدخال التفاعلي
            process.StandardInput.Close();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            bool finished = process.WaitForExit(timeoutSeconds * 1000);

            if (!finished)
            {
                try { process.Kill(); } catch { }
                return string.Empty;
            }

            string output = outputTask.GetAwaiter().GetResult();

            int assistantIdx = output.IndexOf("<|im_start|>assistant");
            if (assistantIdx >= 0)
            {
                output = output.Substring(assistantIdx + "<|im_start|>assistant".Length);
            }
            int promptIdx = output.IndexOf("[ Prompt:");
            if (promptIdx >= 0)
            {
                output = output.Substring(0, promptIdx);
            }
            int exitIdx = output.IndexOf("Exiting...");
            if (exitIdx >= 0)
            {
                output = output.Substring(0, exitIdx);
            }

            string clean = output.Trim();
            // إزالة الأسطر الفارغة الزائدة
            clean = System.Text.RegularExpressions.Regex.Replace(clean, @"\s+", " ");
            return clean;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// توليد تقرير تكتيكي صوتي شامل للمستخدم بلغة اللعبة المعتمدة
    /// </summary>
    public static string GenerateTacticalReport(MatchEngine match, UiLanguage language = UiLanguage.Arabic)
    {
        int minute = (int)match.MatchTimeMinutes;
        int homeScore = match.HomeTeam.Score;
        int awayScore = match.AwayTeam.Score;
        int goalDiff = homeScore - awayScore;

        var sb = new StringBuilder();

        if (language == UiLanguage.Arabic)
        {
            // فحص إمكانية التوليد المباشر من نموذج Qwen 2.5 3B
            if (IsNeuralModelAvailable)
            {
                var tired = match.HomeTeam.Players
                    .Where(p => p.Role != PlayerRole.Goalkeeper && !p.IsSentOff && !p.IsOffPitch)
                    .OrderBy(p => p.Stamina)
                    .FirstOrDefault();

                string tiredInfo = tired != null ? $"{tired.Name} بطاقة {tired.Stamina}%" : "لا يوجد إجهاد كبير";
                string qwenPrompt = $"<|im_start|>system\nأنت مستشار كروي تكتيكي خبير لمباراة كرة قدم تفاعلية للمكفوفين. تحدث في جملتين موجزتين ومباشرتين بالعربية لتقديم نصيحة تكتيكية للمدرب.<|im_end|>\n<|im_start|>user\nالمباراة بين فريقنا {match.HomeTeam.Name} والخصم {match.AwayTeam.Name}. النتيجة الحالية: {homeScore} - {awayScore}. الدقيقة: {minute}. أكثر لاعب مجهد: {tiredInfo}. ما هي نصيحتك للتبديل والخطة الآن؟<|im_end|>\n<|im_start|>assistant\n";

                string neuralOutput = QueryNeuralQwenModel(qwenPrompt);
                if (!string.IsNullOrWhiteSpace(neuralOutput) && neuralOutput.Length > 10)
                {
                    sb.Append($"تقرير المستشار التكتيكي بالذكاء الاصطناعي (Qwen 2.5 3B) في الدقيقة {minute}: {neuralOutput} ");
                }
            }

            if (sb.Length == 0)
            {
                sb.Append($"تقرير المستشار التكتيكي بالذكاء الاصطناعي في الدقيقة {minute}: ");

                // 1. تقييم الموقف الحالي للنتيجة
                if (goalDiff > 1)
                {
                    sb.Append($"فريقك متقدم بفارق مريح ({homeScore} - {awayScore}). السيطرة التكتيكية ممتازة. ");
                }
                else if (goalDiff == 1)
                {
                    sb.Append($"فريقك متقدم بهدف وحيد ({homeScore} - {awayScore}). حافظ على التركيز الدفاعي وتقارب الخطوط. ");
                }
                else if (goalDiff == 0)
                {
                    sb.Append($"النتيجة متعادلة ({homeScore} - {awayScore}). المعركة محتدمة في منتصف الملعب. ");
                }
                else if (goalDiff == -1)
                {
                    sb.Append($"فريقك متأخر بهدف ({homeScore} - {awayScore}). نحتاج لزيادة الكثافة الهجومية والتمرير السريع للأمام. ");
                }
                else
                {
                    sb.Append($"فريقك متأخر بفارق هدفين أو أكثر ({homeScore} - {awayScore}). مطلوب الضغط العالي والمخاطرة التكتيكية المباشرة. ");
                }
            }

            // 2. تحليل إجهاد اللاعبين وقوانين IFAB
            int subsLeft = match.HomeTeam.MaxSubstitutions - match.HomeTeam.SubstitutionsUsed;
            int windowsLeft = match.HomeTeam.MaxStoppageWindows - match.HomeTeam.StoppageWindowsUsed;

            var tiredPlayers = match.HomeTeam.Players
                .Where(p => p.Role != PlayerRole.Goalkeeper && !p.IsSentOff && !p.IsOffPitch)
                .OrderBy(p => p.Stamina)
                .Take(2)
                .ToList();

            if (tiredPlayers.Count > 0 && minute >= 55 && subsLeft > 0 && windowsLeft > 0)
            {
                var mostTired = tiredPlayers[0];
                sb.Append($"حالة اللياقة: اللاعب {mostTired.Name} ({mostTired.CurrentAssignedPosition}) بحاجة للراحة. ");
                
                var bestSub = FindBestBenchReplacement(match.HomeTeam, mostTired);
                if (bestSub != null)
                {
                    sb.Append($"نوصي بدخول {bestSub.Name} للحفاظ على حيوية التشكيل. ");
                }
            }

            // 3. توصية الخطة التكتيكية
            if (minute >= 75 && goalDiff > 0 && match.HomeTeam.CurrentFormation != FormationType.Formation532_Counter)
            {
                sb.Append("نصيحة تكتيكية: التحول إلى خطة 5-3-2 لتأمين العمق الدفاعي وإنهاء المباراة بالفوز.");
            }
            else if (minute >= 65 && goalDiff < 0 && match.HomeTeam.CurrentFormation != FormationType.Formation433_Attack)
            {
                sb.Append("نصيحة تكتيكية: التحول إلى خطة 4-3-3 الهجومية وتكثيف العرضيات والتسديد من خارج المنطقة.");
            }
            else
            {
                sb.Append($"خطة فريقك الحالية ({match.HomeTeam.CurrentFormation.ToArabicName()}) متوازنة وفعالة.");
            }
        }
        else
        {
            sb.Append($"AI Tactical Advisor Report at minute {minute}: ");

            if (goalDiff > 0)
                sb.Append($"Your team is leading ({homeScore} - {awayScore}). Solid tactical game. ");
            else if (goalDiff == 0)
                sb.Append($"Match is level ({homeScore} - {awayScore}). Intense midfield battle. ");
            else
                sb.Append($"Your team is trailing ({homeScore} - {awayScore}). Increase offensive tempo and forward passes. ");

            int subsLeft = match.HomeTeam.MaxSubstitutions - match.HomeTeam.SubstitutionsUsed;
            if (minute >= 55 && subsLeft > 0)
            {
                var tired = match.HomeTeam.Players
                    .Where(p => p.Role != PlayerRole.Goalkeeper && !p.IsSentOff && !p.IsOffPitch)
                    .OrderBy(p => p.Stamina)
                    .FirstOrDefault();

                if (tired != null)
                {
                    var sub = FindBestBenchReplacement(match.HomeTeam, tired);
                    if (sub != null)
                        sb.Append($"Fitness alert: {tired.Name} needs rest. Recommend bringing on {sub.Name}. ");
                }
            }

            sb.Append($"Current system: {FormationManager.ToEnglishName(match.HomeTeam.CurrentFormation)}.");
        }

        return sb.ToString();
    }

    /// <summary>
    /// جلب قائمة التوصيات التكتيكية القابلة للتنفيذ المباشر بنقرة زر
    /// </summary>
    public static List<TacticalRecommendation> GetRecommendations(MatchEngine match, Action onApplied)
    {
        var list = new List<TacticalRecommendation>();
        int minute = (int)match.MatchTimeMinutes;
        int goalDiff = match.HomeTeam.Score - match.AwayTeam.Score;

        // 1. توصية تبديل بدني تكتيكي
        if (match.CanMakeSubstitution(match.HomeTeam))
        {
            var tiredPlayer = match.HomeTeam.Players
                .Where(p => p.Role != PlayerRole.Goalkeeper && !p.IsSentOff && !p.IsOffPitch)
                .OrderBy(p => p.Stamina)
                .FirstOrDefault();

            if (tiredPlayer != null)
            {
                var bestSub = FindBestBenchReplacement(match.HomeTeam, tiredPlayer);
                if (bestSub != null)
                {
                    list.Add(new TacticalRecommendation
                    {
                        Title = $"إجراء تبديل تكتيكي: خروج {tiredPlayer.Name} ودخول {bestSub.Name}",
                        Description = $"تجديد الدماء في مركز {tiredPlayer.CurrentAssignedPosition} لرفع الفعالية والضغط.",
                        PlayerToSubOut = tiredPlayer,
                        PlayerToSubIn = bestSub,
                        ApplyAction = () =>
                        {
                            match.MakeSubstitution(match.HomeTeam, tiredPlayer, bestSub);
                            onApplied();
                        }
                    });
                }
            }
        }

        // 2. توصية تغيير الخطة التكتيكية
        if (goalDiff < 0)
        {
            var suggestedFormation = match.HomeTeam.CurrentFormation == FormationType.Formation433_Attack
                ? FormationType.Formation343_AllOut
                : FormationType.Formation433_Attack;

            list.Add(new TacticalRecommendation
            {
                Title = suggestedFormation == FormationType.Formation343_AllOut
                    ? "التحول إلى خطة 3-4-3 الهجومية الكاسحة (All Out Attack)"
                    : "التحول إلى خطة 4-3-3 الهجومية (ضغط عالي)",
                Description = "زيادة عدد المهاجمين والأجنحة للعودة في النتيجة وتكثيف الكرات داخل الصندوق.",
                SuggestedFormation = suggestedFormation,
                ApplyAction = () =>
                {
                    match.HomeTeam.CurrentFormation = suggestedFormation;
                    FormationManager.ApplyFormation(match.HomeTeam, suggestedFormation);
                    onApplied();
                }
            });
        }
        else if (goalDiff > 0 && minute >= 60 && match.HomeTeam.CurrentFormation != FormationType.Formation532_Counter)
        {
            list.Add(new TacticalRecommendation
            {
                Title = "التحول إلى خطة 5-3-2 الدفاعية (هجمات مرتدة)",
                Description = "تأمين الخط الخلفي بخمسة مدافعين وإغلاق المساحات لضمان الفوز.",
                SuggestedFormation = FormationType.Formation532_Counter,
                ApplyAction = () =>
                {
                    match.HomeTeam.CurrentFormation = FormationType.Formation532_Counter;
                    FormationManager.ApplyFormation(match.HomeTeam, FormationType.Formation532_Counter);
                    onApplied();
                }
            });
        }
        else if (match.HomeTeam.CurrentFormation != FormationType.Formation4231_Wide)
        {
            list.Add(new TacticalRecommendation
            {
                Title = "التحول إلى خطة 4-2-3-1 المتوازنة",
                Description = "تحكم كامل في منطقة العمليات واستحواذ مرن على الكرة.",
                SuggestedFormation = FormationType.Formation4231_Wide,
                ApplyAction = () =>
                {
                    match.HomeTeam.CurrentFormation = FormationType.Formation4231_Wide;
                    FormationManager.ApplyFormation(match.HomeTeam, FormationType.Formation4231_Wide);
                    onApplied();
                }
            });
        }

        // 3. توصية تعديل الشرايط والأسلوب التكتيكي الذكي
        var idealTactics = ComputeDynamicTactics(match, match.HomeTeam, match.AwayTeam);
        bool needsAdjustment = Math.Abs(match.HomeTeam.Tactics.PressingIntensity - idealTactics.PressingIntensity) >= 15 ||
                               Math.Abs(match.HomeTeam.Tactics.PassingDirectness - idealTactics.PassingDirectness) >= 15 ||
                               match.HomeTeam.Tactics.Marking != idealTactics.Marking ||
                               match.HomeTeam.Tactics.Focus != idealTactics.Focus;

        if (needsAdjustment)
        {
            string desc = idealTactics.PressingIntensity <= 30
                ? $"خفض الضغط لـ {idealTactics.PressingIntensity} ودفاع منطقة وتمرير مباشر لتأمين التقدم وتوفير طاقة كبار السن."
                : (idealTactics.PressingIntensity >= 75
                    ? $"رفع الضغط لـ {idealTactics.PressingIntensity} والكرات المباشرة لقلب النتيجة واقتناص التعادل."
                    : $"تعديل شريط الضغط لـ {idealTactics.PressingIntensity} وشريط التمرير لـ {idealTactics.PassingDirectness} لتحقيق التوازن.");

            list.Add(new TacticalRecommendation
            {
                Title = $"تطبيق التكتيك المقترح: ضغط ({idealTactics.PressingIntensity})، تمرير ({idealTactics.PassingDirectness})، {idealTactics.Marking.ToArabicName()}",
                Description = desc,
                ApplyAction = () =>
                {
                    match.HomeTeam.Tactics.PressingIntensity = idealTactics.PressingIntensity;
                    match.HomeTeam.Tactics.PassingDirectness = idealTactics.PassingDirectness;
                    match.HomeTeam.Tactics.Marking = idealTactics.Marking;
                    match.HomeTeam.Tactics.Focus = idealTactics.Focus;
                    match.HomeTeam.Tactics.Tackling = idealTactics.Tackling;
                    match.HomeTeam.Tactics.OffsideTrap = idealTactics.OffsideTrap;
                    onApplied();
                }
            });
        }

        return list;
    }

    /// <summary>
    /// البحث عن أنسب بديل من دكة البدلاء لنفس المركز أو الدور
    /// </summary>
    public static Player? FindBestBenchReplacement(Team team, Player playerToReplace)
    {
        var availableBench = team.Bench.Where(p => !p.IsSentOff && !p.IsOffPitch).ToList();
        if (availableBench.Count == 0) return null;

        // مطابقة المركز الدقيق أولاً
        var exactMatch = availableBench.FirstOrDefault(p => p.CurrentAssignedPosition == playerToReplace.CurrentAssignedPosition);
        if (exactMatch != null) return exactMatch;

        // مطابقة المراكز الثانوية
        var secondaryMatch = availableBench.FirstOrDefault(p => p.SecondaryPositions != null && p.SecondaryPositions.Contains(playerToReplace.CurrentAssignedPosition));
        if (secondaryMatch != null) return secondaryMatch;

        // مطابقة الدور العام (مدافع، وسط، مهاجم)
        var roleMatch = availableBench.FirstOrDefault(p => p.Role == playerToReplace.Role);
        if (roleMatch != null) return roleMatch;

        // أي لاعب متاح غير الحارس
        return availableBench.FirstOrDefault(p => p.Role != PlayerRole.Goalkeeper) ?? availableBench[0];
    }

    /// <summary>
    /// تنفيذ التبديلات التلقائية بالذكاء الاصطناعي إذا كانت مفعلة في الإعدادات
    /// </summary>
    public static bool TryPerformAutoSubstitution(MatchEngine match, out string announcement)
    {
        announcement = string.Empty;
        if (!MatchSettings.Current.AutoSubstitutionsEnabled) return false;
        if (!match.CanMakeSubstitution(match.HomeTeam)) return false;

        // يتم التبديل التلقائي فقط بعد الدقيقة 60 للاعبين الأكثر إرهاقاً
        if (match.MatchTimeMinutes < 60.0f) return false;

        var tired = match.HomeTeam.Players
            .Where(p => p.Role != PlayerRole.Goalkeeper && !p.IsSentOff && !p.IsOffPitch)
            .OrderBy(p => p.Stamina)
            .FirstOrDefault();

        if (tired == null || tired.Stamina > 70) return false;

        var sub = FindBestBenchReplacement(match.HomeTeam, tired);
        if (sub == null) return false;

        bool ok = match.MakeSubstitution(match.HomeTeam, tired, sub);
        if (ok)
        {
            announcement = $"تبديل تلقائي ذكي: خروج {tired.Name} ودخول {sub.Name} في الدقيقة {(int)match.MatchTimeMinutes}.";
            return true;
        }

        return false;
    }

    /// <summary>
    /// حساب الخطة التكتيكية المثالية بالذكاء الاصطناعي بناءً على سير ومجريات اللعب
    /// يحلل النتيجة، الدقيقة، فارق المهارة بين الفريقين، مستوى إجهاد كبار السن، وتكتيك الخصم
    /// </summary>
    public static TeamTactics ComputeDynamicTactics(MatchEngine match, Team team, Team opponent)
    {
        var plan = new TeamTactics();
        float minute = match.MatchTimeMinutes;
        int goalDiff = team.Score - opponent.Score;

        float teamAvgRating = team.Players.Count > 0 
            ? (float)team.Players.Average(p => (p.Speed + p.Defending + p.Physical + p.Dribbling + p.Passing + p.Shooting) / 6.0) 
            : 75.0f;
        float oppAvgRating = opponent.Players.Count > 0 
            ? (float)opponent.Players.Average(p => (p.Speed + p.Defending + p.Physical + p.Dribbling + p.Passing + p.Shooting) / 6.0) 
            : 75.0f;
        bool isUnderdog = oppAvgRating > teamAvgRating + 6.0f;
        
        float teamAvgStamina = team.Players.Count > 0 ? (float)team.Players.Average(p => p.CurrentStamina) : 100.0f;
        int tiredVeterans = team.Players.Count(p => p.Age >= 30 && p.CurrentStamina < 60.0f);
        int oppPressing = opponent.Tactics.PressingIntensity;

        // 1. أسلوب وطريقة الدفاع (Defensive Marking):
        // قاعدة كروية أساسية: إذا كان الفريق أضعف من الخصم، يُمنع تماماً دفاع رجل لرجل (Man-to-Man)
        // لأن الفوارق الفردية في الـ 1v1 بدون تغطية منطقة تتسبب في كارثة؛ لذا يُفرض دفاع المنطقة (Zonal Marking).
        if (isUnderdog)
        {
            plan.Marking = DefensiveMarking.Zonal;
        }
        else if (goalDiff > 0 && minute >= 65)
        {
            // عند التقدم بالنتيجة، دفاع المنطقة يضمن تماسك الخطوط وتضييق المساحات
            plan.Marking = DefensiveMarking.Zonal;
        }
        else if (teamAvgRating >= oppAvgRating + 4.0f && minute >= 55 && goalDiff <= 0)
        {
            // فريق قوي هجومياً ودفاعياً يبحث عن التعويض أو الفوز: الرقابة الفردية تخنق مفاتيح لعب الخصم
            plan.Marking = DefensiveMarking.ManToMan;
        }
        else
        {
            plan.Marking = DefensiveMarking.Zonal;
        }

        // 2. شريط مستوى الضغط (Pressing Intensity: 0 - 100):
        if (goalDiff < 0 && minute >= 75)
        {
            // متأخر في الدقائق الأخيرة: ضغط عالي كاسح
            plan.PressingIntensity = teamAvgStamina > 55 ? 85 : 75;
        }
        else if (goalDiff < 0 && minute >= 55)
        {
            // متأخر في الشوط الثاني: ضغط نشط ومتقدم
            plan.PressingIntensity = 70;
        }
        else if (goalDiff > 0 && minute >= 70)
        {
            // متقدم في أواخر اللقاء: تراجع تحت الكرة وتوفير اللياقة (Low Block)
            plan.PressingIntensity = 25;
        }
        else if (tiredVeterans >= 2 || teamAvgStamina < 60)
        {
            // إنقاذ لياقة كبار السن من الانهيار
            plan.PressingIntensity = 35;
        }
        else if (isUnderdog)
        {
            // الفريق الأقل إمكانيات يلعب تكتل تحت الكرة
            plan.PressingIntensity = 25;
        }
        else
        {
            plan.PressingIntensity = 48; // متوازن
        }

        // 3. شريط أسلوب التمرير والريتم (Passing Game: 0 - 100):
        if (oppPressing >= 75)
        {
            // إذا كان الخصم يضغط عالي جداً (75+): الهروب من الضغط بكرات طولية ومباشرة في المساحات الخالية (40 متر) خلف دفاعه
            plan.PassingDirectness = 80;
        }
        else if (goalDiff > 0 && minute >= 65)
        {
            // عند التقدم بالنتيجة: تمريرات مباشرة للتحولات الخاطفة
            plan.PassingDirectness = 70;
        }
        else if (goalDiff < 0 && minute >= 75)
        {
            // متأخر في نهاية المباراة: كرات طويلة مباشرة داخل الصندوق
            plan.PassingDirectness = 82;
        }
        else
        {
            plan.PassingDirectness = 45;
        }

        // 4. شراسة العرقلة (Tackling Aggression):
        if (goalDiff < 0 && minute >= 75)
        {
            plan.Tackling = TacklingAggression.Aggressive;
        }
        else if (goalDiff > 1 && minute >= 70)
        {
            plan.Tackling = TacklingAggression.Cautious; // تجنب الإنذارات والفاولات الخطرة
        }
        else
        {
            plan.Tackling = TacklingAggression.Normal;
        }

        // 5. مصيدة التسلل (Offside Trap):
        plan.OffsideTrap = plan.PressingIntensity >= 75 && !isUnderdog;

        // 6. بؤرة الهجوم وأسلوب اللعب (Attacking Focus):
        if (isUnderdog || oppPressing >= 75 || (goalDiff > 0 && minute >= 60))
        {
            plan.Focus = AttackingFocus.CounterAttack;
        }
        else if (goalDiff < 0 && minute >= 75)
        {
            plan.Focus = AttackingFocus.ShootOnSight;
        }
        else if (team.CurrentFormation == FormationType.Formation433_Attack)
        {
            plan.Focus = AttackingFocus.Wings;
        }
        else
        {
            plan.Focus = AttackingFocus.Balanced;
        }

        return plan;
    }

    /// <summary>
    /// تطبيق التعديل التكتيكي التلقائي الديناميكي الذكي بناءً على سير ومجريات اللعب
    /// </summary>
    public static bool TryPerformDynamicTacticalAdjustment(MatchEngine match, Team team, out string announcement, bool forceAi = false)
    {
        announcement = string.Empty;
        bool isHome = team == match.HomeTeam;
        var opponent = isHome ? match.AwayTeam : match.HomeTeam;

        // إذا كان فريق المستخدم والخاصية غير مفعلة في الإعدادات، لا يتم التعديل التلقائي
        if (isHome && !MatchSettings.Current.AutoTacticalAdjustmentsEnabled && !forceAi)
        {
            return false;
        }

        var ideal = ComputeDynamicTactics(match, team, opponent);
        var current = team.Tactics;

        // التحقق من وجود فارق تكتيكي جوهري يستدعي التدخل
        bool pressingChanged = Math.Abs(current.PressingIntensity - ideal.PressingIntensity) >= 12;
        bool passingChanged = Math.Abs(current.PassingDirectness - ideal.PassingDirectness) >= 12;
        bool markingChanged = current.Marking != ideal.Marking;
        bool focusChanged = current.Focus != ideal.Focus;
        bool tacklingChanged = current.Tackling != ideal.Tackling;

        if (!pressingChanged && !passingChanged && !markingChanged && !focusChanged && !tacklingChanged)
        {
            return false;
        }

        // تطبيق التكتيك الجديد
        current.PressingIntensity = ideal.PressingIntensity;
        current.PassingDirectness = ideal.PassingDirectness;
        current.Marking = ideal.Marking;
        current.Focus = ideal.Focus;
        current.Tackling = ideal.Tackling;
        current.OffsideTrap = ideal.OffsideTrap;

        int minute = (int)match.MatchTimeMinutes;
        string teamName = isHome ? "المدرب المساعد لفريقك" : $"مدرب {team.Name}";

        if (ideal.PressingIntensity <= 30 && ideal.Focus == AttackingFocus.CounterAttack)
        {
            announcement = $"تعديل تكتيكي ذكي ({teamName}) في الدقيقة {minute}: التراجع لدفاع منطقة متكتل (ضغط {ideal.PressingIntensity}) والاعتماد على الهجمات المرتدة.";
        }
        else if (ideal.PressingIntensity >= 75)
        {
            announcement = $"تعديل تكتيكي ذكي ({teamName}) في الدقيقة {minute}: رفع الضغط العالي إلى {ideal.PressingIntensity} والكرات الطولية لقلب النتيجة.";
        }
        else if (opponent.Tactics.PressingIntensity >= 75 && ideal.PassingDirectness >= 75)
        {
            announcement = $"تعديل تكتيكي ذكي ({teamName}) في الدقيقة {minute}: الاعتماد على كرات طولية سريعة لضرب المساحة في ضهر دفاع الخصم المتقدم.";
        }
        else
        {
            announcement = $"تعديل تكتيكي ذكي ({teamName}) في الدقيقة {minute}: ضبط شريط الضغط عند {ideal.PressingIntensity} وأسلوب اللعب إلى {ideal.Focus.ToArabicName()}.";
        }

        return true;
    }
}
