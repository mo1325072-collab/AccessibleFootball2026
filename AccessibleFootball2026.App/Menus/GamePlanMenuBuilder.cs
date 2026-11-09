using AccessibleFootball2026.Audio;
using AccessibleFootball2026.Audio.Commentary;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;
using AccessibleFootball2026.Core.Tactics;

namespace AccessibleFootball2026.App.Menus;

/// <summary>
/// منشئ قوائم خطة اللعب التفاعلية للمكفوفين (Game Plan):
/// يتيح استعراض وتعديل التشكيلة الأساسية (11 لاعب)، تبديل اللاعبين، تغيير المراكز،
/// وتعيين المتخصصين (القائد، ضربات الجزاء، الفاولات، الركنيات)، وتغيير خطة اللعب.
/// </summary>
public static class GamePlanMenuBuilder
{
    public static AccessibleMenu CreatePreMatchMenu(
        Team userTeam, 
        IScreenReader screenReader, 
        Action<AccessibleMenu> pushMenu, 
        Action popMenu, 
        Action onStartMatch)
    {
        var menu = new AccessibleMenu($"خطة لعب {userTeam.Name}", screenReader);

        menu.AddItem("بدء المباراة", () =>
        {
            screenReader.Speak("انطلاق المباراة بالتشكيلة والخيارات المعتمدة");
            onStartMatch();
        });

        menu.AddItem("التشكيلة الأساسية", () =>
        {
            var lineupMenu = CreateLineupMenu(userTeam, screenReader, pushMenu, popMenu);
            pushMenu(lineupMenu);
        });

        menu.AddItem("دكة البدلاء", () =>
        {
            var benchMenu = CreateBenchMenu(userTeam, screenReader, pushMenu, popMenu);
            pushMenu(benchMenu);
        });

        menu.AddItem("أدوار المتخصصين", () =>
        {
            var specMenu = CreateSpecialistsMenu(userTeam, screenReader, pushMenu, popMenu);
            pushMenu(specMenu);
        });

        menu.AddItem("تغيير الخطة التكتيكية", () =>
        {
            var formMenu = CreateFormationMenu(userTeam, screenReader, popMenu);
            pushMenu(formMenu);
        });

        menu.AddItem("الأسلوب والتكتيك (الشرايط وطريقة الدفاع)", () =>
        {
            var tactMenu = CreateTacticsMenu(userTeam, screenReader, popMenu);
            pushMenu(tactMenu);
        });

        menu.AddItem("تقرير الفريق", () =>
        {
            string cap = userTeam.Captain?.Name ?? (userTeam.Players.Count > 0 ? userTeam.Players[0].Name : "غير محدد");
            string pen = userTeam.PenaltyTaker?.Name ?? (userTeam.Players.Count > 9 ? userTeam.Players[9].Name : "غير محدد");
            string report = $"تقرير فريق {userTeam.Name}: الخطة التكتيكية: {userTeam.CurrentFormation.ToArabicName()}. " +
                           $"القائد: {cap}. مسدد ضربات الجزاء: {pen}. " +
                           $"عدد اللاعبين الأساسيين: {userTeam.Players.Count}، وعدد البدلاء: {userTeam.Bench.Count}.";
            screenReader.Speak(report);
        });

        menu.AddItem("رجوع", () => popMenu());

        return menu;
    }

    public static AccessibleMenu CreateLineupMenu(
        Team userTeam, 
        IScreenReader screenReader, 
        Action<AccessibleMenu> pushMenu, 
        Action popMenu,
        MatchEngine? match = null)
    {
        var menu = new AccessibleMenu("التشكيلة الأساسية", screenReader);

        for (int i = 0; i < userTeam.Players.Count; i++)
        {
            int playerIndex = i;
            var p = userTeam.Players[playerIndex];
            string itemTitle = $"{playerIndex + 1}. {p.Name}، {p.CurrentAssignedPosition}، طاقة {p.EffectiveOverallRating}، {p.ConditionDescription}";

            menu.AddItem(itemTitle, () =>
            {
                var optionsMenu = CreatePlayerActionMenu(userTeam, playerIndex, screenReader, pushMenu, popMenu, match);
                pushMenu(optionsMenu);
            });
        }

        menu.AddItem("سماع كامل التشكيلة الأساسية دفعة واحدة", () =>
        {
            string summary = "التشكيلة الأساسية: ";
            for (int i = 0; i < userTeam.Players.Count; i++)
            {
                var p = userTeam.Players[i];
                summary += $"{i + 1}: {p.Name} في مركز {p.CurrentAssignedPosition}. ";
            }
            screenReader.Speak(summary);
        });

        menu.AddItem("رجوع", () => popMenu());

        return menu;
    }

    private static AccessibleMenu CreatePlayerActionMenu(
        Team userTeam, 
        int playerIndex, 
        IScreenReader screenReader, 
        Action<AccessibleMenu> pushMenu, 
        Action popMenu,
        MatchEngine? match = null)
    {
        var player = userTeam.Players[playerIndex];
        var menu = new AccessibleMenu(player.Name, screenReader);

        menu.AddItem("تبديل الموضع", () =>
        {
            var swapMenu = new AccessibleMenu("اختر اللاعب المراد التبديل معه", screenReader);
            for (int j = 0; j < userTeam.Players.Count; j++)
            {
                if (j == playerIndex) continue;
                int targetIdx = j;
                var targetP = userTeam.Players[targetIdx];
                swapMenu.AddItem($"{targetP.Name}، مركز {targetP.CurrentAssignedPosition}، طاقة {targetP.EffectiveOverallRating}", () =>
                {
                    userTeam.SwapPositions(playerIndex, targetIdx);
                    screenReader.Speak($"تم تبديل الموضعين بنجاح بين {player.Name} و {targetP.Name}.");
                    popMenu(); // back to options
                    popMenu(); // back to lineup
                });
            }
            swapMenu.AddItem("إلغاء", () => popMenu());
            pushMenu(swapMenu);
        });

        menu.AddItem("استبدال بلاعب بديل", () =>
        {
            if (userTeam.Bench.Count == 0)
            {
                screenReader.Speak("لا يوجد أي بديل متاح على الدكة!");
                return;
            }

            if (match != null && !match.CanMakeSubstitution(userTeam))
            {
                if (userTeam.SubstitutionsUsed >= userTeam.MaxSubstitutions)
                {
                    screenReader.Speak($"تم استنفاد جميع التبديلات المسموحة ({userTeam.MaxSubstitutions} تبديلات)!");
                }
                else
                {
                    screenReader.Speak($"تم استنفاد جميع فترات التوقف المسموحة ({userTeam.MaxStoppageWindows} فترات توقف)!");
                }
                return;
            }

            var subMenu = new AccessibleMenu($"اختر البديل لـ {player.Name}", screenReader);
            for (int b = 0; b < userTeam.Bench.Count; b++)
            {
                var benchPlayer = userTeam.Bench[b];
                subMenu.AddItem($"{benchPlayer.Name}، مركزه المفضل {benchPlayer.PrimaryPosition}، طاقة {benchPlayer.EffectiveOverallRating}", () =>
                {
                    bool ok = match != null 
                        ? match.MakeSubstitution(userTeam, player, benchPlayer)
                        : userTeam.MakeSubstitution(player, benchPlayer, isMatchActive: false);

                    if (ok)
                    {
                        screenReader.Speak($"تم التبديل بنجاح: خروج {player.Name} ودخول {benchPlayer.Name} في مركز {benchPlayer.CurrentAssignedPosition}. (التبديلات: {userTeam.SubstitutionsUsed}/{userTeam.MaxSubstitutions})");
                    }
                    else
                    {
                        screenReader.Speak("تعذر إجراء التبديل: استنفدت التبديلات أو فترات التوقف المسموحة!");
                    }
                    popMenu();
                    popMenu();
                });
            }
            subMenu.AddItem("إلغاء", () => popMenu());
            pushMenu(subMenu);
        });

        menu.AddItem("تغيير المركز", () =>
        {
            var posMenu = new AccessibleMenu($"اختر المركز الجديد لـ {player.Name}", screenReader);
            var positions = new[] 
            { 
                SpecificPosition.GK, SpecificPosition.CB, SpecificPosition.LB, SpecificPosition.RB,
                SpecificPosition.CDM, SpecificPosition.CM, SpecificPosition.CAM,
                SpecificPosition.LW, SpecificPosition.RW, SpecificPosition.ST, SpecificPosition.CF
            };

            foreach (var pos in positions)
            {
                var targetPos = pos;
                posMenu.AddItem($"مركز {targetPos}", () =>
                {
                    player.CurrentAssignedPosition = targetPos;
                    screenReader.Speak($"تم تعيين {player.Name} في مركز {targetPos}، طاقته الحالية {player.EffectiveOverallRating}، {player.PositionSuitabilityMessage}");
                    popMenu();
                    popMenu();
                });
            }
            posMenu.AddItem("إلغاء", () => popMenu());
            pushMenu(posMenu);
        });

        menu.AddItem("تقرير المهارات", () =>
        {
            string details = $"اللاعب {player.Name}، رقم القميص {player.ShirtNumber}. " +
                            $"المركز الأساسي {player.PrimaryPosition}، يلعب حالياً كـ {player.CurrentAssignedPosition}. " +
                            $"السرعة {player.Speed}، التسديد {player.Shooting}، التمرير {player.Passing}، " +
                            $"المراوغة {player.Dribbling}، الدفاع {player.Defending}، القوة البدنية {player.Physical}. " +
                            $"التقييم الفعلي {player.EffectiveOverallRating}، الحالة البدنية {player.ConditionDescription}.";
            screenReader.Speak(details);
        });

        menu.AddItem("رجوع", () => popMenu());

        return menu;
    }

    public static AccessibleMenu CreateBenchMenu(
        Team userTeam, 
        IScreenReader screenReader, 
        Action<AccessibleMenu> pushMenu, 
        Action popMenu,
        MatchEngine? match = null)
    {
        var menu = new AccessibleMenu("دكة البدلاء", screenReader);

        for (int i = 0; i < userTeam.Bench.Count; i++)
        {
            int benchIdx = i;
            var sub = userTeam.Bench[benchIdx];
            string itemTitle = $"{benchIdx + 1}. {sub.Name}، {sub.PrimaryPosition}، طاقة {sub.EffectiveOverallRating}، {sub.ConditionDescription}";

            menu.AddItem(itemTitle, () =>
            {
                if (match != null && !match.CanMakeSubstitution(userTeam))
                {
                    if (userTeam.SubstitutionsUsed >= userTeam.MaxSubstitutions)
                    {
                        screenReader.Speak($"تم استنفاد جميع التبديلات المسموحة ({userTeam.MaxSubstitutions} تبديلات)!");
                    }
                    else
                    {
                        screenReader.Speak($"تم استنفاد جميع فترات التوقف المسموحة ({userTeam.MaxStoppageWindows} فترات توقف)!");
                    }
                    return;
                }

                var replaceMenu = new AccessibleMenu($"اختر اللاعب الأساسي المراد خروجه ليدخل {sub.Name}", screenReader);
                for (int s = 0; s < userTeam.Players.Count; s++)
                {
                    var starter = userTeam.Players[s];
                    replaceMenu.AddItem($"{starter.Name}، مركز {starter.CurrentAssignedPosition}، طاقة {starter.EffectiveOverallRating}", () =>
                    {
                        bool ok = match != null 
                            ? match.MakeSubstitution(userTeam, starter, sub)
                            : userTeam.MakeSubstitution(starter, sub, isMatchActive: false);

                        if (ok)
                        {
                            screenReader.Speak($"تم التبديل بنجاح: خروج {starter.Name} ودخول {sub.Name} كـ {sub.CurrentAssignedPosition}. (التبديلات: {userTeam.SubstitutionsUsed}/{userTeam.MaxSubstitutions})");
                        }
                        else
                        {
                            screenReader.Speak("تعذر إجراء التبديل: استنفدت التبديلات أو فترات التوقف المسموحة!");
                        }
                        popMenu();
                        popMenu();
                    });
                }
                replaceMenu.AddItem("إلغاء", () => popMenu());
                pushMenu(replaceMenu);
            });
        }

        menu.AddItem("سماع دكة البدلاء", () =>
        {
            string summary = "دكة البدلاء: ";
            for (int i = 0; i < userTeam.Bench.Count; i++)
            {
                var p = userTeam.Bench[i];
                summary += $"{i + 1}: {p.Name} ({p.PrimaryPosition}). ";
            }
            screenReader.Speak(summary);
        });

        menu.AddItem("رجوع", () => popMenu());

        return menu;
    }

    public static AccessibleMenu CreateSpecialistsMenu(
        Team userTeam, 
        IScreenReader screenReader, 
        Action<AccessibleMenu> pushMenu, 
        Action popMenu)
    {
        userTeam.AssignDefaultRoles();
        var menu = new AccessibleMenu("أدوار المتخصصين", screenReader);

        menu.AddItem($"قائد الفريق: {userTeam.Captain?.Name ?? "غير محدد"}", () =>
        {
            var capMenu = new AccessibleMenu("اختر قائد الفريق", screenReader);
            foreach (var p in userTeam.Players)
            {
                var candidate = p;
                capMenu.AddItem($"{candidate.Name}، {candidate.CurrentAssignedPosition}", () =>
                {
                    userTeam.Captain = candidate;
                    screenReader.Speak($"تم تعيين {candidate.Name} قائداً للفريق.");
                    popMenu();
                    popMenu();
                });
            }
            capMenu.AddItem("إلغاء", () => popMenu());
            pushMenu(capMenu);
        });

        menu.AddItem($"ركلات الجزاء: {userTeam.PenaltyTaker?.Name ?? "غير محدد"}", () =>
        {
            var penMenu = new AccessibleMenu("اختر مسدد ركلات الجزاء", screenReader);
            foreach (var p in userTeam.Players)
            {
                var candidate = p;
                penMenu.AddItem($"{candidate.Name}، تسديد {candidate.Shooting}", () =>
                {
                    userTeam.PenaltyTaker = candidate;
                    screenReader.Speak($"تم تعيين {candidate.Name} مسدداً أول لركلات الجزاء.");
                    popMenu();
                    popMenu();
                });
            }
            penMenu.AddItem("إلغاء", () => popMenu());
            pushMenu(penMenu);
        });

        menu.AddItem($"الركلات الحرة: {userTeam.FreeKickTaker?.Name ?? "غير محدد"}", () =>
        {
            var fkMenu = new AccessibleMenu("اختر مسدد الركلات الحرة", screenReader);
            foreach (var p in userTeam.Players)
            {
                var candidate = p;
                fkMenu.AddItem($"{candidate.Name}، تسديد {candidate.Shooting}، تمرير {candidate.Passing}", () =>
                {
                    userTeam.FreeKickTaker = candidate;
                    screenReader.Speak($"تم تعيين {candidate.Name} مسدداً للركلات الحرة المباشرة.");
                    popMenu();
                    popMenu();
                });
            }
            fkMenu.AddItem("إلغاء", () => popMenu());
            pushMenu(fkMenu);
        });

        menu.AddItem($"الركلات الركنية: {userTeam.CornerTaker?.Name ?? "غير محدد"}", () =>
        {
            var crnMenu = new AccessibleMenu("اختر منفذ الركلات الركنية", screenReader);
            foreach (var p in userTeam.Players)
            {
                var candidate = p;
                crnMenu.AddItem($"{candidate.Name}، تمرير {candidate.Passing}", () =>
                {
                    userTeam.CornerTaker = candidate;
                    screenReader.Speak($"تم تعيين {candidate.Name} منفذاً للركلات الركنية.");
                    popMenu();
                    popMenu();
                });
            }
            crnMenu.AddItem("إلغاء", () => popMenu());
            pushMenu(crnMenu);
        });

        menu.AddItem("رجوع", () => popMenu());

        return menu;
    }

    public static AccessibleMenu CreateFormationMenu(
        Team userTeam, 
        IScreenReader screenReader, 
        Action popMenu)
    {
        var menu = new AccessibleMenu("الخطة التكتيكية", screenReader);
        var formations = FormationManager.GetAllFormations();

        foreach (var f in formations)
        {
            var targetFormation = f;
            menu.AddItem(targetFormation.ToArabicName(), () =>
            {
                userTeam.ChangeFormation(targetFormation);
                screenReader.Speak($"تم تطبيق خطة {targetFormation.ToArabicName()} بنجاح، وتوزيع تمركزات اللاعبين.");
                popMenu();
            });
        }

        menu.AddItem("إلغاء", () => popMenu());
        return menu;
    }

    public static AccessibleMenu CreateTacticsMenu(
        Team userTeam, 
        IScreenReader screenReader, 
        Action popMenu)
    {
        var menu = new AccessibleMenu($"الأسلوب والتكتيك - {userTeam.Name}", screenReader);

        // 1. شريط مستوى الضغط
        menu.AddItem($"شريط الضغط: {userTeam.Tactics.PressingIntensity}%", () =>
        {
            userTeam.Tactics.PressingIntensity = userTeam.Tactics.PressingIntensity switch
            {
                <= 25 => 45,
                <= 50 => 75,
                <= 75 => 90,
                _ => 20
            };
            string tier = userTeam.Tactics.PressingIntensity <= 25 ? "دفاع متراجع تحت الكرة (Low Block)"
                : (userTeam.Tactics.PressingIntensity <= 50 ? "ضغط متوسط متوازن"
                : (userTeam.Tactics.PressingIntensity <= 75 ? "ضغط نشط ومتقدم"
                : "ضغط عالي خانق (Gegenpress)"));
            screenReader.Speak($"تم ضبط شريط الضغط عند {userTeam.Tactics.PressingIntensity}%: {tier}");
        });

        // 2. شريط أسلوب التمرير والريتم
        menu.AddItem($"شريط التمرير: {userTeam.Tactics.PassingDirectness}%", () =>
        {
            userTeam.Tactics.PassingDirectness = userTeam.Tactics.PassingDirectness switch
            {
                <= 25 => 45,
                <= 50 => 75,
                <= 75 => 90,
                _ => 20
            };
            string tier = userTeam.Tactics.PassingDirectness <= 25 ? "بناء هجمة قصير من الـ 18"
                : (userTeam.Tactics.PassingDirectness <= 50 ? "تمرير أرضي متوازن"
                : (userTeam.Tactics.PassingDirectness <= 75 ? "تمرير مباشر يكسر الخطوط"
                : "كرات طولية سريعة في ضهر الدفاع"));
            screenReader.Speak($"تم ضبط شريط التمرير عند {userTeam.Tactics.PassingDirectness}%: {tier}");
        });

        // 3. طريقة الدفاع
        menu.AddItem($"طريقة الدفاع: {userTeam.Tactics.Marking.ToArabicName()}", () =>
        {
            userTeam.Tactics.Marking = userTeam.Tactics.Marking == DefensiveMarking.Zonal
                ? DefensiveMarking.ManToMan
                : DefensiveMarking.Zonal;
            string detail = userTeam.Tactics.Marking == DefensiveMarking.Zonal
                ? "دفاع منطقة: تغطية جماعية تحمي الفريق وتغلق المساحات"
                : "دفاع رجل لرجل: صراعات فردية لصيقة، تتطلب مدافعين أقوياء في الـ 1v1";
            screenReader.Speak($"تم تغيير طريقة الدفاع إلى: {userTeam.Tactics.Marking.ToArabicName()}. {detail}");
        });

        // 4. شراسة العرقلة
        menu.AddItem($"شراسة العرقلة: {userTeam.Tactics.Tackling.ToArabicName()}", () =>
        {
            userTeam.Tactics.Tackling = userTeam.Tactics.Tackling switch
            {
                TacklingAggression.Cautious => TacklingAggression.Normal,
                TacklingAggression.Normal => TacklingAggression.Moderate,
                TacklingAggression.Moderate => TacklingAggression.Aggressive,
                TacklingAggression.Aggressive => TacklingAggression.Reckless,
                _ => TacklingAggression.Cautious
            };
            screenReader.Speak($"تم ضبط شراسة العرقلة على: {userTeam.Tactics.Tackling.ToArabicName()}");
        });

        // 5. مصيدة التسلل
        menu.AddItem($"مصيدة التسلل: {(userTeam.Tactics.OffsideTrap ? "مفعلة" : "معطلة")}", () =>
        {
            userTeam.Tactics.OffsideTrap = !userTeam.Tactics.OffsideTrap;
            screenReader.Speak($"مصيدة التسلل الآن: {(userTeam.Tactics.OffsideTrap ? "مفعلة" : "معطلة")}");
        });

        // 6. أسلوب وبؤرة الهجوم
        menu.AddItem($"أسلوب الهجوم: {userTeam.Tactics.Focus.ToArabicName()}", () =>
        {
            userTeam.Tactics.Focus = userTeam.Tactics.Focus switch
            {
                AttackingFocus.Balanced => AttackingFocus.Wings,
                AttackingFocus.Wings => AttackingFocus.Central,
                AttackingFocus.Central => AttackingFocus.LongBall,
                AttackingFocus.LongBall => AttackingFocus.ShootOnSight,
                AttackingFocus.ShootOnSight => AttackingFocus.CounterAttack,
                _ => AttackingFocus.Balanced
            };
            screenReader.Speak($"تم تغيير أسلوب الهجوم إلى: {userTeam.Tactics.Focus.ToArabicName()}");
        });

        // 7. تقرير التناغم التكتيكي
        menu.AddItem("تقرير التناغم والانسجام التكتيكي", () =>
        {
            var synergy = userTeam.Tactics.CalculateSynergy();
            string msg = $"التناغم التكتيكي: {synergy.SynergyScore} من 100، معامل الأداء: {synergy.AdvantageMultiplier:F2}x. ";
            if (synergy.PositiveSynergies.Count > 0)
            {
                msg += "نقاط القوة: " + string.Join("، ", synergy.PositiveSynergies) + ". ";
            }
            if (synergy.Conflicts.Count > 0)
            {
                msg += "تحذيرات وتناقضات: " + string.Join("، ", synergy.Conflicts) + ". ";
            }
            screenReader.Speak(msg);
        });

        menu.AddItem("رجوع", () => popMenu());

        return menu;
    }

    public static AccessibleMenu CreatePauseMenu(
        MatchEngine match, 
        IScreenReader screenReader, 
        CommentaryEngine commentaryEngine, 
        Action<AccessibleMenu> pushMenu, 
        Action popMenu,
        Action onResumeMatch, 
        Action onExit)
    {
        string pauseTitle = match.State == MatchState.HalfTime
            ? $"استراحة بين الشوطين - {match.HomeTeam.Name} {match.HomeTeam.Score} - {match.AwayTeam.Score} {match.AwayTeam.Name}"
            : "قائمة الإيقاف المؤقت";
        var menu = new AccessibleMenu(pauseTitle, screenReader);

        string resumeLabel = match.State == MatchState.HalfTime ? "انطلاق الشوط الثاني" : "استئناف المباراة";
        menu.AddItem(resumeLabel, () =>
        {
            bool wasHalfTime = match.State == MatchState.HalfTime;
            match.ResumeMatch();
            screenReader.Speak(wasHalfTime ? "انطلاق الشوط الثاني" : "استئناف اللقاء");
            onResumeMatch();
        });

        menu.AddItem("التشكيلة الأساسية", () =>
        {
            var lineup = CreateLineupMenu(match.HomeTeam, screenReader, pushMenu, popMenu, match);
            pushMenu(lineup);
        });

        menu.AddItem("تبديل تكتيكي", () =>
        {
            if (!match.CanMakeSubstitution(match.HomeTeam))
            {
                if (match.HomeTeam.SubstitutionsUsed >= match.HomeTeam.MaxSubstitutions)
                {
                    screenReader.Speak($"تم استنفاد جميع التبديلات المسموحة ({match.HomeTeam.MaxSubstitutions} تبديلات)!");
                }
                else
                {
                    screenReader.Speak($"تم استنفاد جميع فترات التوقف المسموحة ({match.HomeTeam.MaxStoppageWindows} فترات توقف)!");
                }
                return;
            }

            var bench = CreateBenchMenu(match.HomeTeam, screenReader, pushMenu, popMenu, match);
            pushMenu(bench);
        });

        menu.AddItem("تغيير الخطة التكتيكية", () =>
        {
            var form = CreateFormationMenu(match.HomeTeam, screenReader, popMenu);
            pushMenu(form);
        });

        menu.AddItem("الأسلوب والتكتيك (الشرايط وطريقة الدفاع)", () =>
        {
            var tactMenu = CreateTacticsMenu(match.HomeTeam, screenReader, popMenu);
            pushMenu(tactMenu);
        });

        menu.AddItem("المستشار التكتيكي بالذكاء الاصطناعي", () =>
        {
            if (TacticalAiAdvisor.IsNeuralModelAvailable)
            {
                screenReader.Speak("جاري استشارة نموذج الذكاء الاصطناعي Qwen 2.5 وتحليل المباراة...");
            }
            var advisorMenu = new AccessibleMenu("المستشار التكتيكي بالذكاء الاصطناعي", screenReader);
            string report = TacticalAiAdvisor.GenerateTacticalReport(match, MatchSettings.Current.Language);
            advisorMenu.AddItem("التقرير والتحليل التكتيكي الشامل", () =>
            {
                screenReader.Speak(report);
            });

            var recs = TacticalAiAdvisor.GetRecommendations(match, () =>
            {
                screenReader.Speak("تم تطبيق التوصية التكتيكية بنجاح!");
                popMenu();
            });

            foreach (var r in recs)
            {
                var rec = r;
                advisorMenu.AddItem(rec.Title, () =>
                {
                    screenReader.Speak($"{rec.Title}. {rec.Description}");
                    rec.ApplyAction?.Invoke();
                });
            }

            advisorMenu.AddItem("رجوع", () => popMenu());
            pushMenu(advisorMenu);
        });

        menu.AddItem("إحصائيات المباراة", () =>
        {
            int minute = (int)match.MatchTimeMinutes;
            string ballOwner = match.Ball.Owner != null ? $"مع اللاعب {match.Ball.Owner.Name}" : "كرة حرة في العشب";
            string info = $"معلومات المباراة: النتيجة: {match.HomeTeam.Name} {match.HomeTeam.Score}، مقابل {match.AwayTeam.Score} لـ {match.AwayTeam.Name}. " +
                          $"الدقيقة {minute}. الكرة حالياً في {match.CurrentZone.ToArabicAnnouncement()}، {ballOwner}. " +
                          $"خطة فريقك: {match.HomeTeam.CurrentFormation.ToArabicName()}";
            screenReader.Speak(info);
        });

        menu.AddItem("خروج للقائمة الرئيسية", () =>
        {
            screenReader.Speak("تم إنهاء اللقاء والعودة إلى القائمة الرئيسية");
            onExit();
        });

        return menu;
    }
}
