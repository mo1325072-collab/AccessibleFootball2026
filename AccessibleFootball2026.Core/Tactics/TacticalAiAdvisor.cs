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

    private static string ResolveModelDirectory()
    {
        var candidates = new List<string> { Path.Combine(AppContext.BaseDirectory, "Models") };
        string? dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "AccessibleFootball2026.sln"))) candidates.Add(Path.Combine(dir, "Models"));
            dir = Path.GetDirectoryName(dir);
        }
        candidates.Add(@"D:\AccessibleFootball2026\Models");
        return candidates.FirstOrDefault(p => File.Exists(Path.Combine(p, "qwen2.5-3b-instruct-q4_k_m.gguf")) &&
            File.Exists(Path.Combine(p, "llama-bin", "llama-cli.exe"))) ?? candidates[0];
    }
    public static string LlamaCliPath => Path.Combine(ResolveModelDirectory(), "llama-bin", "llama-cli.exe");
    public static string ModelPath => Path.Combine(ResolveModelDirectory(), "qwen2.5-3b-instruct-q4_k_m.gguf");
    public static bool IsNeuralModelAvailable => File.Exists(LlamaCliPath) && File.Exists(ModelPath) && new FileInfo(ModelPath).Length > 1_500_000_000;

    public static string QueryNeuralQwenModel(string prompt, int maxTokens = 30, int timeoutSeconds = 8)
    {
        if (!IsNeuralModelAvailable) return string.Empty;
        return RunModelProcess(LlamaCliPath, ModelPath, prompt, maxTokens, timeoutSeconds);
    }

    private static string RunModelProcess(string executable, string model, string prompt, int maxTokens, int timeoutSeconds)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = executable,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };
            foreach (var arg in new[] { "-m", model, "-p", prompt, "-n", Math.Clamp(maxTokens, 1, 1024).ToString(), "-c", "2048", "--single-turn", "--simple-io", "--no-display-prompt", "-t", "4" })
                psi.ArgumentList.Add(arg);
            using var process = System.Diagnostics.Process.Start(psi);
            if (process == null) return string.Empty;
            process.StandardInput.Close();
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync(); // Drain both pipes; diagnostics must not deadlock inference.
            bool finished = process.WaitForExit(Math.Clamp(timeoutSeconds, 1, 120) * 1000);
            if (!finished)
            {
                try { process.Kill(entireProcessTree: true); process.WaitForExit(2000); } catch { }
                return string.Empty;
            }
            if (process.ExitCode != 0) return string.Empty;
            string output = outputTask.GetAwaiter().GetResult();
            _ = errorTask.GetAwaiter().GetResult();

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
                sb.Append($"تقرير تكتيكي بالقواعد المحلية، وليس نموذج Qwen، في الدقيقة {minute}: ");

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

            // 3. توصية الخطة التكتيكية وفحص التناقض
            var formReq = FormationManager.GetRequirement(match.HomeTeam.CurrentFormation);
            if (formReq.IsDefensive && match.HomeTeam.Tactics.PressingIntensity > 60)
            {
                sb.Append($"تحذير تكتيكي عاجل: خطتك الحالية ({formReq.ArabicName}) دفاعية ولكنك تلعب بضغط عالي ({match.HomeTeam.Tactics.PressingIntensity}%)! هذا يتعارض مع إعداد الكتلة المنخفضة الحالي ويخفض تنسيق الالتحامات في اللعبة. خفض الضغط أو اختر إعداداً آخر. ");
            }
            else if (minute >= 75 && goalDiff > 0 && match.HomeTeam.CurrentFormation != FormationType.Formation532_Counter)
            {
                sb.Append("نصيحة تكتيكية: التحول إلى خطة 5-3-2 الدفاعية لغلق العمق وتأمين الفوز بالمرتدات.");
            }
            else if (minute >= 65 && goalDiff < 0 && match.HomeTeam.CurrentFormation != FormationType.Formation433_Attack)
            {
                sb.Append("نصيحة تكتيكية: التحول إلى خطة 4-3-3 بوسط متدرج وصانع ألعاب متقدم لتكثيف الضغط العالي والعودة في النتيجة.");
            }
            else
            {
                sb.Append($"خطة فريقك الحالية ({match.HomeTeam.CurrentFormation.ToArabicName()}) متوازنة ومتوافقة تكتيكياً.");
            }
        }
        else
        {
            sb.Append($"Native-rule tactical report (not Qwen inference) at minute {minute}: ");

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
                        Title = LocalizationService.Text($"إجراء تبديل تكتيكي: خروج {tiredPlayer.Name} ودخول {bestSub.Name}", $"Substitute {tiredPlayer.Name} with {bestSub.Name}"),
                        Description = LocalizationService.Text($"تجديد الدماء في مركز {tiredPlayer.CurrentAssignedPosition} لرفع الفعالية والضغط.", $"Fresh player at {tiredPlayer.CurrentAssignedPosition}"),
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
                    ? LocalizationService.Text("التحول إلى 3-4-3 بجناحين وظهيري جناح", "Switch to 3-4-3 with wide forwards")
                    : LocalizationService.Text("التحول إلى خطة 4-3-3 الهجومية (ضغط عالي)", "Switch to 4-3-3 attack"),
                Description = LocalizationService.Text("زيادة عدد المهاجمين والأجنحة للعودة في النتيجة وتكثيف الكرات داخل الصندوق.", "Add forwards and wide players while trailing."),
                SuggestedFormation = suggestedFormation,
                ApplyAction = () =>
                {
                    match.HomeTeam.CurrentFormation = suggestedFormation;
                    FormationManager.ApplyFormation(match.HomeTeam, suggestedFormation, assignPositions: true);
                    onApplied();
                }
            });
        }
        else if (goalDiff > 0 && minute >= 60 && match.HomeTeam.CurrentFormation != FormationType.Formation532_Counter)
        {
            list.Add(new TacticalRecommendation
            {
                Title = LocalizationService.Text("التحول إلى خطة 5-3-2 الدفاعية (هجمات مرتدة)", "Switch to 5-3-2 counter"),
                Description = LocalizationService.Text("تأمين الخط الخلفي بخمسة مدافعين وإغلاق المساحات لضمان الفوز.", "Add a fifth defender while protecting the lead."),
                SuggestedFormation = FormationType.Formation532_Counter,
                ApplyAction = () =>
                {
                    match.HomeTeam.CurrentFormation = FormationType.Formation532_Counter;
                    FormationManager.ApplyFormation(match.HomeTeam, FormationType.Formation532_Counter, assignPositions: true);
                    onApplied();
                }
            });
        }
        else if (match.HomeTeam.CurrentFormation != FormationType.Formation4231_Wide)
        {
            list.Add(new TacticalRecommendation
            {
                Title = LocalizationService.Text("التحول إلى خطة 4-2-3-1 المتوازنة", "Switch to 4-2-3-1 balanced"),
                Description = LocalizationService.Text("تحكم كامل في منطقة العمليات واستحواذ مرن على الكرة.", "Balance midfield cover and possession."),
                SuggestedFormation = FormationType.Formation4231_Wide,
                ApplyAction = () =>
                {
                    match.HomeTeam.CurrentFormation = FormationType.Formation4231_Wide;
                    FormationManager.ApplyFormation(match.HomeTeam, FormationType.Formation4231_Wide, assignPositions: true);
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
                ? LocalizationService.Text($"خفض الضغط لـ {idealTactics.PressingIntensity} ودفاع منطقة وتمرير مباشر لتأمين التقدم وتوفير طاقة كبار السن.", $"Pressing {idealTactics.PressingIntensity}, zonal marking and direct passing to protect the lead and stamina.")
                : (idealTactics.PressingIntensity >= 75
                    ? LocalizationService.Text($"رفع الضغط لـ {idealTactics.PressingIntensity} والكرات المباشرة لقلب النتيجة واقتناص التعادل.", $"Pressing {idealTactics.PressingIntensity} and direct passing while trailing.")
                    : LocalizationService.Text($"تعديل شريط الضغط لـ {idealTactics.PressingIntensity} وشريط التمرير لـ {idealTactics.PassingDirectness} لتحقيق التوازن.", $"Pressing {idealTactics.PressingIntensity}, passing directness {idealTactics.PassingDirectness}."));

            list.Add(new TacticalRecommendation
            {
                Title = LocalizationService.Text($"تطبيق التكتيك المقترح: ضغط ({idealTactics.PressingIntensity})، تمرير ({idealTactics.PassingDirectness})، {idealTactics.Marking.ToArabicName()}", $"Apply pressing {idealTactics.PressingIntensity}, passing {idealTactics.PassingDirectness}, {idealTactics.Marking.ToDisplayName()}"),
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
        var position = playerToReplace.CurrentAssignedPosition;
        var available = team.Bench.Where(team.CanEnterAsSubstitute).ToArray();
        // Compare effective strength IN the required position without mutating the real bench player.
        int Strength(Player candidate)
        {
            var positioned = candidate.Clone();
            positioned.CurrentAssignedPosition = position;
            return positioned.EffectiveOverallRating;
        }
        Player? Strongest(IEnumerable<Player> players) => players.OrderByDescending(Strength)
            .ThenByDescending(p => p.Stamina).ThenBy(p => p.Id).FirstOrDefault();
        return Strongest(available.Where(p => p.PrimaryPosition == position)) ??
            Strongest(available.Where(p => p.SecondaryPositions.Contains(position)));
    }

    /// <summary>
    /// تنفيذ التبديلات التلقائية بالذكاء الاصطناعي إذا كانت مفعلة في الإعدادات
    /// </summary>
    public static bool TryPerformAutoSubstitution(MatchEngine match, out string announcement)
    {
        announcement = string.Empty;
        if (match.PendingInjuryPlayer != null || !MatchSettings.Current.AutoSubstitutionsEnabled) return false;
        if (!match.CanMakeSubstitution(match.HomeTeam)) return false;

        // Meaningful fatigue uses the existing 70% boundary, relative to the player's own capacity.
        // A booking plus three recorded fouls is the native second-yellow risk rule, not a prediction by Qwen.
        float Fitness(Player p) => 100f * p.Stamina / Math.Max(1, p.BaseStamina);
        bool CardRisk(Player p) => p.YellowCards > 0 && match.PerformanceOf(p).Fouls >= 3;
        var candidates = match.HomeTeam.Players.Where(p => p.IsActiveOnPitch &&
                (Fitness(p) <= 70 || CardRisk(p)))
            .OrderByDescending(CardRisk).ThenBy(Fitness).ThenBy(p => p.Id);
        foreach (var player in candidates)
        {
            var replacement = FindBestBenchReplacement(match.HomeTeam, player);
            if (replacement == null) continue; // Never put an incompatible player in a vacant position.
            string reason = CardRisk(player) ? "إنذار مع كثرة الأخطاء وخطر الإنذار الثاني" : "هبوط اللياقة";
            if (!match.MakeSubstitution(match.HomeTeam, player, replacement)) continue;
            announcement = $"تبديل تلقائي بسبب {reason}: خروج {player.Name} ودخول {replacement.Name} في الدقيقة {(int)match.MatchTimeMinutes}.";
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
