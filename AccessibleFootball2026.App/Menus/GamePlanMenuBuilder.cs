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
    private static string T(string ar, string en) => LocalizationService.Text(ar, en);
    public static Action<Team>? OpenTacticsDialog { get; set; }

    public static AccessibleMenu CreateInjurySubstitutionMenu(MatchEngine match, IScreenReader reader, Action onResolved)
    {
        var injured = match.PendingInjuryPlayer ?? throw new InvalidOperationException("No injury choice is pending.");
        var team = match.HomeTeam;
        var menu = new AccessibleMenu(T($"إصابة {injured.Name}. اختر البديل من الدكة", $"{injured.Name} injured. Choose a substitute from the bench"), reader);
        if (match.CanMakeSubstitution(team))
        {
            foreach (var player in team.Bench.Where(team.CanEnterAsSubstitute).ToArray())
            {
                var candidate = player;
                string position = candidate.PrimaryPosition.ToString();
                menu.AddItem(T($"{candidate.Name}، مركز {position}، طاقة {candidate.EffectiveOverallRating}",
                    $"{candidate.Name}, position {position}, rating {candidate.EffectiveOverallRating}"), () =>
                {
                    if (!match.ResolveInjurySubstitution(injured, candidate))
                    { reader.Speak(T("هذا التبديل غير متاح. اختر بديلاً آخر.", "This substitution is unavailable. Choose another player.")); return; }
                    reader.Speak(T($"خروج {injured.Name} ودخول {candidate.Name}.", $"{injured.Name} off, {candidate.Name} on."));
                    onResolved();
                });
            }
        }
        bool available = menu.Count > 0;
        menu.AddItem(available ? T("الاستمرار دون بديل وخروج المصاب", "Continue without a substitute; injured player leaves")
            : T("لا يوجد تبديل قانوني متاح. المتابعة دون المصاب", "No legal substitution available. Continue without the injured player"), () =>
        {
            if (match.ResolveInjurySubstitution(injured, null)) onResolved();
        });
        return menu;
    }

    public static AccessibleMenu CreateShootoutEligibilityMenu(ShootoutRoster roster, int targetCount, IScreenReader reader, Action onExcluded)
    {
        var menu = new AccessibleMenu(T($"الترجيح: استبعاد {roster.Players.Count - targetCount} لاعبين", $"Shootout: exclude {roster.Players.Count - targetCount} players"), reader);
        foreach (var player in roster.Players.Where(p => p.Role != PlayerRole.Goalkeeper).ToArray())
        {
            var candidate = player;
            menu.AddItem(candidate.Name, () => { if (roster.Exclude(candidate, targetCount)) onExcluded(); });
        }
        return menu;
    }

    public static AccessibleMenu CreatePreMatchMenu(
        Team userTeam, 
        IScreenReader screenReader, 
        Action<AccessibleMenu> pushMenu, 
        Action popMenu, 
        Action onStartMatch,
        bool startMatch = true)
    {
        var menu = new AccessibleMenu(T($"خطة لعب {userTeam.Name}", $"Game plan: {userTeam.Name}"), screenReader);

        menu.AddItem(startMatch ? T("بدء المباراة", "Start match") : T("اعتماد الخطة لهذه الجلسة", "Apply plan for this session"), () =>
        {
            screenReader.Speak(startMatch ? T("انطلاق المباراة بالتشكيلة والخيارات المعتمدة", "Match starting") : T("تم اعتماد الخطة لهذه الجلسة", "Plan applied for this session"));
            onStartMatch();
        });

        menu.AddItem(T("التشكيلة الأساسية", "Starting lineup"), () =>
        {
            var lineupMenu = CreateLineupMenu(userTeam, screenReader, pushMenu, popMenu);
            pushMenu(lineupMenu);
        });

        menu.AddItem(T("دكة البدلاء", "Bench"), () =>
        {
            var benchMenu = CreateBenchMenu(userTeam, screenReader, pushMenu, popMenu);
            pushMenu(benchMenu);
        });

        menu.AddItem(T("أدوار المتخصصين", "Specialist roles"), () =>
        {
            var specMenu = CreateSpecialistsMenu(userTeam, screenReader, pushMenu, popMenu);
            pushMenu(specMenu);
        });

        menu.AddItem(T("تغيير الخطة التكتيكية", "Change formation"), () =>
        {
            var formMenu = CreateFormationMenu(userTeam, screenReader, popMenu);
            pushMenu(formMenu);
        });

        menu.AddItem(T("الأسلوب والتكتيك (الشرايط وطريقة الدفاع)", "Playing style and tactics"), () =>
        {
            if (OpenTacticsDialog != null) { OpenTacticsDialog(userTeam); return; }
            var tactMenu = CreateTacticsMenu(userTeam, screenReader, popMenu);
            pushMenu(tactMenu);
        });

        menu.AddItem(T("تقرير الفريق", "Team report"), () =>
        {
            string cap = userTeam.Captain?.Name ?? (userTeam.Players.Count > 0 ? userTeam.Players[0].Name : T("غير محدد", "Unassigned"));
            string pen = userTeam.PenaltyTaker?.Name ?? (userTeam.Players.Count > 9 ? userTeam.Players[9].Name : T("غير محدد", "Unassigned"));
            string report = T($"تقرير فريق {userTeam.Name}. الخطة: {userTeam.CurrentFormation.ToArabicName()}. القائد: {cap}. الجزاء: {pen}. الأساسيون: {userTeam.Players.Count}. البدلاء: {userTeam.Bench.Count}.",
                $"{userTeam.Name}. Formation: {userTeam.CurrentFormation.ToEnglishName()}. Captain: {cap}. Penalty taker: {pen}. Starters: {userTeam.Players.Count}. Bench: {userTeam.Bench.Count}.");
            screenReader.Speak(report);
        });

        menu.AddItem(T("رجوع", "Back"), () => popMenu());

        return menu;
    }

    public static AccessibleMenu CreateLineupMenu(
        Team userTeam, 
        IScreenReader screenReader, 
        Action<AccessibleMenu> pushMenu, 
        Action popMenu,
        MatchEngine? match = null)
    {
        var menu = new AccessibleMenu(T("التشكيلة الأساسية", "Starting lineup"), screenReader);

        for (int i = 0; i < userTeam.Players.Count; i++)
        {
            int playerIndex = i;
            var p = userTeam.Players[playerIndex];
            string itemTitle = T($"{playerIndex + 1}. {p.Name}، {p.CurrentAssignedPosition}، طاقة {p.EffectiveOverallRating}، {p.ConditionDescription}", $"{playerIndex + 1}. {p.Name}, {p.CurrentAssignedPosition}, rating {p.EffectiveOverallRating}, {p.ConditionDescription}");

            menu.AddItem(itemTitle, () =>
            {
                var optionsMenu = CreatePlayerActionMenu(userTeam, playerIndex, screenReader, pushMenu, popMenu, match);
                pushMenu(optionsMenu);
            });
        }

        menu.AddItem(T("سماع كامل التشكيلة الأساسية دفعة واحدة", "Read starting lineup"), () =>
        {
            string summary = T("التشكيلة الأساسية: ", "Starting lineup: ");
            for (int i = 0; i < userTeam.Players.Count; i++)
            {
                var p = userTeam.Players[i];
                summary += T($"{i + 1}: {p.Name} في مركز {p.CurrentAssignedPosition}. ", $"{i + 1}: {p.Name}, {p.CurrentAssignedPosition}. ");
            }
            screenReader.Speak(summary);
        });

        menu.AddItem(T("رجوع", "Back"), () => popMenu());

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

        menu.AddItem(T("تبديل الموضع", "Swap positions"), () =>
        {
            if (!player.IsActiveOnPitch || !userTeam.Players.Contains(player))
            {
                screenReader.Speak(T("هذا اللاعب لم يعد متاحاً في الملعب.", "This player is no longer available on the pitch."));
                return;
            }
            var swapMenu = new AccessibleMenu(T("اختر اللاعب المراد التبديل معه", "Swap with player"), screenReader);
            for (int j = 0; j < userTeam.Players.Count; j++)
            {
                if (j == playerIndex || !userTeam.Players[j].IsActiveOnPitch) continue;
                int targetIdx = j;
                var targetP = userTeam.Players[targetIdx];
                swapMenu.AddItem(T($"{targetP.Name}، مركز {targetP.CurrentAssignedPosition}، طاقة {targetP.EffectiveOverallRating}", $"{targetP.Name}, {targetP.CurrentAssignedPosition}, rating {targetP.EffectiveOverallRating}"), () =>
                {
                    int currentIndex = userTeam.Players.IndexOf(player);
                    int targetIndex = userTeam.Players.IndexOf(targetP);
                    if (!userTeam.SwapPositions(currentIndex, targetIndex))
                    {
                        screenReader.Speak(T("تعذر تبديل الموضعين: أحد اللاعبين لم يعد متاحاً.", "Cannot swap positions: a player is no longer available."));
                        return;
                    }
                    screenReader.Speak(T($"تم تبديل الموضعين بنجاح بين {player.Name} و {targetP.Name}.", $"{player.Name} and {targetP.Name} swapped positions."));
                    popMenu(); // back to options
                    popMenu(); // back to lineup
                });
            }
            swapMenu.AddItem(T("إلغاء", "Cancel"), () => popMenu());
            pushMenu(swapMenu);
        });

        menu.AddItem(T("استبدال بلاعب بديل", "Substitute player"), () =>
        {
            if (userTeam.Bench.Count == 0)
            {
                screenReader.Speak(T("لا يوجد أي بديل متاح على الدكة!", "No substitute available"));
                return;
            }

            if (match != null && !match.CanMakeSubstitution(userTeam))
            {
                if (userTeam.SubstitutionsUsed >= userTeam.MaxSubstitutions)
                {
                    screenReader.Speak(T($"تم استنفاد جميع التبديلات المسموحة ({userTeam.MaxSubstitutions} تبديلات)!", $"All {userTeam.MaxSubstitutions} substitutions used"));
                }
                else
                {
                    screenReader.Speak(T($"تم استنفاد جميع فترات التوقف المسموحة ({userTeam.MaxStoppageWindows} فترات توقف)!", $"All {userTeam.MaxStoppageWindows} substitution opportunities used"));
                }
                return;
            }

            var subMenu = new AccessibleMenu(T($"اختر البديل لـ {player.Name}", $"Substitute for {player.Name}"), screenReader);
            for (int b = 0; b < userTeam.Bench.Count; b++)
            {
                var benchPlayer = userTeam.Bench[b];
                if (match != null && !userTeam.CanEnterAsSubstitute(benchPlayer)) continue;
                subMenu.AddItem(T($"{benchPlayer.Name}، مركزه المفضل {benchPlayer.PrimaryPosition}، طاقة {benchPlayer.EffectiveOverallRating}", $"{benchPlayer.Name}, primary position {benchPlayer.PrimaryPosition}, rating {benchPlayer.EffectiveOverallRating}"), () =>
                {
                    bool ok = match != null 
                        ? match.MakeSubstitution(userTeam, player, benchPlayer)
                        : userTeam.MakeSubstitution(player, benchPlayer, isMatchActive: false);

                    if (ok)
                    {
                        screenReader.Speak(T($"تم التبديل بنجاح: خروج {player.Name} ودخول {benchPlayer.Name} في مركز {benchPlayer.CurrentAssignedPosition}. (التبديلات: {userTeam.SubstitutionsUsed}/{userTeam.MaxSubstitutions})", $"{player.Name} off, {benchPlayer.Name} on at {benchPlayer.CurrentAssignedPosition}. Substitutions {userTeam.SubstitutionsUsed}/{userTeam.MaxSubstitutions}"));
                    }
                    else
                    {
                        screenReader.Speak(T("تعذر إجراء التبديل: استنفدت التبديلات أو فترات التوقف المسموحة!", "Substitution unavailable: count or opportunity limit reached"));
                    }
                    popMenu();
                    popMenu();
                });
            }
            subMenu.AddItem(T("إلغاء", "Cancel"), () => popMenu());
            pushMenu(subMenu);
        });

        menu.AddItem(T("تغيير المركز", "Change position"), () =>
        {
            var posMenu = new AccessibleMenu(T($"اختر المركز الجديد لـ {player.Name}", $"Position for {player.Name}"), screenReader);
            var positions = new[] 
            { 
                SpecificPosition.GK, SpecificPosition.CB, SpecificPosition.LB, SpecificPosition.RB,
                SpecificPosition.CDM, SpecificPosition.CM, SpecificPosition.CAM,
                SpecificPosition.LW, SpecificPosition.RW, SpecificPosition.ST, SpecificPosition.CF
            };

            foreach (var pos in positions)
            {
                var targetPos = pos;
                posMenu.AddItem(T($"مركز {targetPos}", $"{targetPos}"), () =>
                {
                    player.CurrentAssignedPosition = targetPos;
                    screenReader.Speak(T($"تم تعيين {player.Name} في مركز {targetPos}، طاقته الحالية {player.EffectiveOverallRating}، {player.PositionSuitabilityMessage}", $"{player.Name}, {targetPos}, rating {player.EffectiveOverallRating}. {player.PositionSuitabilityMessage}"));
                    popMenu();
                    popMenu();
                });
            }
            posMenu.AddItem(T("إلغاء", "Cancel"), () => popMenu());
            pushMenu(posMenu);
        });

        menu.AddItem(T("تقرير المهارات", "Player attributes"), () =>
        {
            string details = T($"{player.Name}، رقم {player.ShirtNumber}. الأساسي {player.PrimaryPosition}، الحالي {player.CurrentAssignedPosition}. السرعة {player.Speed}، التسديد {player.Shooting}، التمرير {player.Passing}، المراوغة {player.Dribbling}، الدفاع {player.Defending}، القوة {player.Physical}. التقييم {player.EffectiveOverallRating}، {player.ConditionDescription}.",
                $"{player.Name}, shirt {player.ShirtNumber}. Primary {player.PrimaryPosition}, assigned {player.CurrentAssignedPosition}. Speed {player.Speed}, shooting {player.Shooting}, passing {player.Passing}, dribbling {player.Dribbling}, defending {player.Defending}, physical {player.Physical}. Rating {player.EffectiveOverallRating}, {player.ConditionDescription}.");
            screenReader.Speak(details + " " + player.DataProvenanceSummary);
        });

        menu.AddItem(T("رجوع", "Back"), () => popMenu());

        return menu;
    }

    public static AccessibleMenu CreateBenchMenu(
        Team userTeam, 
        IScreenReader screenReader, 
        Action<AccessibleMenu> pushMenu, 
        Action popMenu,
        MatchEngine? match = null)
    {
        var menu = new AccessibleMenu(T("دكة البدلاء", "Bench"), screenReader);

        for (int i = 0; i < userTeam.Bench.Count; i++)
        {
            int benchIdx = i;
            var sub = userTeam.Bench[benchIdx];
            if (match != null && !userTeam.CanEnterAsSubstitute(sub)) continue;
            string itemTitle = T($"{benchIdx + 1}. {sub.Name}، {sub.PrimaryPosition}، طاقة {sub.EffectiveOverallRating}، {sub.ConditionDescription}", $"{benchIdx + 1}. {sub.Name}, {sub.PrimaryPosition}, rating {sub.EffectiveOverallRating}, {sub.ConditionDescription}");

            menu.AddItem(itemTitle, () =>
            {
                if (match != null && !match.CanMakeSubstitution(userTeam))
                {
                    if (userTeam.SubstitutionsUsed >= userTeam.MaxSubstitutions)
                    {
                        screenReader.Speak(T($"تم استنفاد جميع التبديلات المسموحة ({userTeam.MaxSubstitutions} تبديلات)!", $"All {userTeam.MaxSubstitutions} substitutions used"));
                    }
                    else
                    {
                        screenReader.Speak(T($"تم استنفاد جميع فترات التوقف المسموحة ({userTeam.MaxStoppageWindows} فترات توقف)!", $"All {userTeam.MaxStoppageWindows} substitution opportunities used"));
                    }
                    return;
                }

                var replaceMenu = new AccessibleMenu(T($"اختر اللاعب الأساسي المراد خروجه ليدخل {sub.Name}", $"Player to replace with {sub.Name}"), screenReader);
                for (int s = 0; s < userTeam.Players.Count; s++)
                {
                    var starter = userTeam.Players[s];
                    replaceMenu.AddItem(T($"{starter.Name}، مركز {starter.CurrentAssignedPosition}، طاقة {starter.EffectiveOverallRating}", $"{starter.Name}, {starter.CurrentAssignedPosition}, rating {starter.EffectiveOverallRating}"), () =>
                    {
                        bool ok = match != null 
                            ? match.MakeSubstitution(userTeam, starter, sub)
                            : userTeam.MakeSubstitution(starter, sub, isMatchActive: false);

                        if (ok)
                        {
                            screenReader.Speak(T($"تم التبديل بنجاح: خروج {starter.Name} ودخول {sub.Name} كـ {sub.CurrentAssignedPosition}. (التبديلات: {userTeam.SubstitutionsUsed}/{userTeam.MaxSubstitutions})", $"{starter.Name} off, {sub.Name} on at {sub.CurrentAssignedPosition}. Substitutions {userTeam.SubstitutionsUsed}/{userTeam.MaxSubstitutions}"));
                        }
                        else
                        {
                            screenReader.Speak(T("تعذر إجراء التبديل: استنفدت التبديلات أو فترات التوقف المسموحة!", "Substitution unavailable: count or opportunity limit reached"));
                        }
                        popMenu();
                        popMenu();
                    });
                }
                replaceMenu.AddItem(T("إلغاء", "Cancel"), () => popMenu());
                pushMenu(replaceMenu);
            });
        }

        menu.AddItem(T("سماع دكة البدلاء", "Read bench"), () =>
        {
            string summary = T("دكة البدلاء: ", "Bench: ");
            for (int i = 0; i < userTeam.Bench.Count; i++)
            {
                var p = userTeam.Bench[i];
                summary += $"{i + 1}: {p.Name} ({p.PrimaryPosition}). ";
            }
            screenReader.Speak(summary);
        });

        menu.AddItem(T("رجوع", "Back"), () => popMenu());

        return menu;
    }

    public static AccessibleMenu CreateSpecialistsMenu(
        Team userTeam, 
        IScreenReader screenReader, 
        Action<AccessibleMenu> pushMenu, 
        Action popMenu)
    {
        userTeam.AssignDefaultRoles();
        var menu = new AccessibleMenu(T("أدوار المتخصصين", "Specialist roles"), screenReader);

        menu.AddItem(T("قائد الفريق", "Captain") + $": {userTeam.Captain?.Name ?? T("غير محدد", "Unassigned")}", () =>
        {
            var capMenu = new AccessibleMenu(T("اختر قائد الفريق", "Select captain"), screenReader);
            foreach (var p in userTeam.Players.Where(p => p.IsActiveOnPitch))
            {
                var candidate = p;
                capMenu.AddItem(T($"{candidate.Name}، {candidate.CurrentAssignedPosition}", $"{candidate.Name}, {candidate.CurrentAssignedPosition}"), () =>
                {
                    if (!userTeam.Players.Contains(candidate) || !candidate.IsActiveOnPitch)
                    {
                        screenReader.Speak(T("هذا اللاعب لم يعد متاحاً في الملعب.", "This player is no longer available on the pitch."));
                        return;
                    }
                    userTeam.Captain = candidate;
                    screenReader.Speak(T($"تم تعيين {candidate.Name} قائداً للفريق.", $"Captain: {candidate.Name}"));
                    popMenu();
                    popMenu();
                });
            }
            capMenu.AddItem(T("إلغاء", "Cancel"), () => popMenu());
            pushMenu(capMenu);
        });

        menu.AddItem(T("ركلات الجزاء", "Penalty taker") + $": {userTeam.PenaltyTaker?.Name ?? T("غير محدد", "Unassigned")}", () =>
        {
            var penMenu = new AccessibleMenu(T("اختر مسدد ركلات الجزاء", "Select penalty taker"), screenReader);
            foreach (var p in userTeam.Players.Where(p => p.IsActiveOnPitch))
            {
                var candidate = p;
                penMenu.AddItem(T($"{candidate.Name}، تسديد {candidate.Shooting}", $"{candidate.Name}, shooting {candidate.Shooting}"), () =>
                {
                    if (!userTeam.Players.Contains(candidate) || !candidate.IsActiveOnPitch)
                    {
                        screenReader.Speak(T("هذا اللاعب لم يعد متاحاً في الملعب.", "This player is no longer available on the pitch."));
                        return;
                    }
                    userTeam.PenaltyTaker = candidate;
                    screenReader.Speak(T($"تم تعيين {candidate.Name} مسدداً أول لركلات الجزاء.", $"Penalty taker: {candidate.Name}"));
                    popMenu();
                    popMenu();
                });
            }
            penMenu.AddItem(T("إلغاء", "Cancel"), () => popMenu());
            pushMenu(penMenu);
        });

        menu.AddItem(T("الركلات الحرة", "Free-kick taker") + $": {userTeam.FreeKickTaker?.Name ?? T("غير محدد", "Unassigned")}", () =>
        {
            var fkMenu = new AccessibleMenu(T("اختر مسدد الركلات الحرة", "Select free-kick taker"), screenReader);
            foreach (var p in userTeam.Players.Where(p => p.IsActiveOnPitch))
            {
                var candidate = p;
                fkMenu.AddItem(T($"{candidate.Name}، تسديد {candidate.Shooting}، تمرير {candidate.Passing}", $"{candidate.Name}, shooting {candidate.Shooting}, passing {candidate.Passing}"), () =>
                {
                    if (!userTeam.Players.Contains(candidate) || !candidate.IsActiveOnPitch)
                    {
                        screenReader.Speak(T("هذا اللاعب لم يعد متاحاً في الملعب.", "This player is no longer available on the pitch."));
                        return;
                    }
                    userTeam.FreeKickTaker = candidate;
                    screenReader.Speak(T($"تم تعيين {candidate.Name} مسدداً للركلات الحرة المباشرة.", $"Free-kick taker: {candidate.Name}"));
                    popMenu();
                    popMenu();
                });
            }
            fkMenu.AddItem(T("إلغاء", "Cancel"), () => popMenu());
            pushMenu(fkMenu);
        });

        menu.AddItem(T("الركلات الركنية", "Corner taker") + $": {userTeam.CornerTaker?.Name ?? T("غير محدد", "Unassigned")}", () =>
        {
            var crnMenu = new AccessibleMenu(T("اختر منفذ الركلات الركنية", "Select corner taker"), screenReader);
            foreach (var p in userTeam.Players.Where(p => p.IsActiveOnPitch))
            {
                var candidate = p;
                crnMenu.AddItem(T($"{candidate.Name}، تمرير {candidate.Passing}", $"{candidate.Name}, passing {candidate.Passing}"), () =>
                {
                    if (!userTeam.Players.Contains(candidate) || !candidate.IsActiveOnPitch)
                    {
                        screenReader.Speak(T("هذا اللاعب لم يعد متاحاً في الملعب.", "This player is no longer available on the pitch."));
                        return;
                    }
                    userTeam.CornerTaker = candidate;
                    screenReader.Speak(T($"تم تعيين {candidate.Name} منفذاً للركلات الركنية.", $"Corner taker: {candidate.Name}"));
                    popMenu();
                    popMenu();
                });
            }
            crnMenu.AddItem(T("إلغاء", "Cancel"), () => popMenu());
            pushMenu(crnMenu);
        });

        menu.AddItem(T("رجوع", "Back"), () => popMenu());

        return menu;
    }

    public static AccessibleMenu CreateFormationMenu(
        Team userTeam, 
        IScreenReader screenReader, 
        Action popMenu)
    {
        var menu = new AccessibleMenu(T("الخطة التكتيكية", "Formation"), screenReader);
        var formations = FormationManager.GetAllFormations();

        foreach (var f in formations)
        {
            var targetFormation = f;
            menu.AddItem(T(targetFormation.ToArabicName(), targetFormation.ToEnglishName()), () =>
            {
                userTeam.ChangeFormation(targetFormation);
                screenReader.Speak(T($"تم تطبيق خطة {targetFormation.ToArabicName()} بنجاح، وتوزيع تمركزات اللاعبين.", $"Formation: {targetFormation.ToEnglishName()}"));
                popMenu();
            });
        }

        menu.AddItem(T("متطلبات الخطة وملاءمة اللاعبين", "Formation requirements and player fit"), () =>
            screenReader.Speak(FormationSuitability.Requirements(userTeam.CurrentFormation) + " " + FormationSuitability.Describe(userTeam)));
        menu.AddItem(T("إلغاء", "Cancel"), () => popMenu());
        return menu;
    }

    public static AccessibleMenu CreateTacticsMenu(
        Team userTeam, 
        IScreenReader screenReader, 
        Action popMenu)
    {
        var menu = new AccessibleMenu(T($"الأسلوب والتكتيك - {userTeam.Name}", $"Style and tactics: {userTeam.Name}"), screenReader);

        menu.AddSlider(T("شريط الضغط", "Pressing"), userTeam.Tactics.PressingIntensity, 0, 100, 1, v =>
        {
            userTeam.Tactics.PressingIntensity = v;
            userTeam.Tactics.UseLinkedDefensiveLine();
        });
        menu.AddSlider(T("شريط التمرير", "Passing"), userTeam.Tactics.PassingDirectness, 0, 100, 1, v =>
        {
            userTeam.Tactics.PassingDirectness = v;
            userTeam.Tactics.UseLinkedTempo();
        });
        menu.AddComboBox(T("أسلوب العرقلة", "Tackling"), new List<string> {
            T("حذر", "Cautious"), T("عادي", "Normal"), T("متوسط", "Moderate"), T("حماسي", "Aggressive"), T("متهور", "Reckless")
        }, (int)userTeam.Tactics.Tackling, i => userTeam.Tactics.Tackling = (TacklingAggression)i);
        // Stable enum2 is the existing passing decision/support implementation, not Balanced.
        var focuses = new[] { AttackingFocus.Central, AttackingFocus.ShootOnSight, AttackingFocus.CounterAttack, AttackingFocus.Wings, AttackingFocus.LongBall };
        menu.AddComboBox(T("أسلوب اللعب", "Playing style"), new List<string> {
            T("التمريرات", "Passing"), T("التسديد عن بعد", "Long-range shooting"), T("الهجوم المرتد", "Counter-attack"), T("الأجناب", "Wings"), T("الكرات الطويلة", "Long balls")
        }, Array.IndexOf(focuses, userTeam.Tactics.Focus), i => userTeam.Tactics.Focus = focuses[i]);
        menu.AddComboBox(T("طريقة الدفاع", "Defensive marking"), new List<string> { T("دفاع منطقة", "Zonal marking"), T("دفاع رجل لرجل", "Man-to-man marking") },
            (int)userTeam.Tactics.Marking, i => userTeam.Tactics.Marking = (DefensiveMarking)i);
        menu.AddCheckbox(T("مصيدة التسلل", "Offside trap"), userTeam.Tactics.OffsideTrap, value => userTeam.Tactics.OffsideTrap = value);

        // 7. تقرير التناغم التكتيكي
        menu.AddItem(T("تقرير التناغم والانسجام التكتيكي", "Tactical synergy"), () =>
        {
            var synergy = userTeam.Tactics.CalculateSynergy(userTeam.CurrentFormation);
            string msg = T($"التناغم التكتيكي: {synergy.SynergyScore} من 100، معامل تنسيق الالتحامات: {synergy.AdvantageMultiplier:F2}x. ", $"Tactical synergy {synergy.SynergyScore}/100, duel coordination multiplier {synergy.AdvantageMultiplier:F2}x. ");
            if (synergy.PositiveSynergies.Count > 0)
            {
                msg += T("نقاط القوة: ", "Strengths: ") + string.Join("، ", synergy.PositiveSynergies) + ". ";
            }
            if (synergy.Conflicts.Count > 0)
            {
                msg += T("تحذيرات وتناقضات: ", "Conflicts: ") + string.Join("، ", synergy.Conflicts) + ". ";
            }
            screenReader.Speak(msg + TacticalEffectsSummary.Describe(userTeam));
        });

        menu.AddItem(T("رجوع", "Back"), () => popMenu());

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
            ? T($"استراحة بين الشوطين - {match.HomeTeam.Name} {match.HomeTeam.Score} - {match.AwayTeam.Score} {match.AwayTeam.Name}", $"Interval: {match.HomeTeam.Name} {match.HomeTeam.Score} - {match.AwayTeam.Score} {match.AwayTeam.Name}")
            : T("قائمة الإيقاف المؤقت", "Pause menu");
        var menu = new AccessibleMenu(pauseTitle, screenReader);

        string resumeLabel = match.State == MatchState.HalfTime ? (match.IsExtraTimeStartInterval ? T("بدء الشوط الإضافي الأول", "Start first extra-time period") : match.IsExtraTime ? T("بدء الشوط الإضافي الثاني", "Start second extra-time period") : T("بدء الشوط الثاني", "Start second half")) : T("استئناف المباراة", "Resume match");
        menu.AddItem(resumeLabel, () =>
        {
            bool wasHalfTime = match.State == MatchState.HalfTime;
            screenReader.Speak(wasHalfTime ? T("بدء الشوط التالي", "Starting next period") : T("استئناف اللقاء", "Resuming match"));
            // كلمة المدرب للأجنحة تُسمع فقط إن كانت تعليمة الأجناب مفعّلة فعلاً (مرة لكل تفعيل)
            if (commentaryEngine.ActiveCommentator.Id == CommentatorId.FaresAwad) match.RequestCoachWideInstruction(match.HomeTeam);
            onResumeMatch();
        });

        menu.AddItem(T("التشكيلة الأساسية", "Starting lineup"), () =>
        {
            var lineup = CreateLineupMenu(match.HomeTeam, screenReader, pushMenu, popMenu, match);
            pushMenu(lineup);
        });

        menu.AddItem(T("تبديل تكتيكي", "Substitutions"), () =>
        {
            if (!match.CanMakeSubstitution(match.HomeTeam))
            {
                if (match.HomeTeam.SubstitutionsUsed >= match.HomeTeam.MaxSubstitutions)
                {
                    screenReader.Speak(T($"تم استنفاد جميع التبديلات المسموحة ({match.HomeTeam.MaxSubstitutions} تبديلات)!", $"All {match.HomeTeam.MaxSubstitutions} substitutions used"));
                }
                else
                {
                    screenReader.Speak(T($"تم استنفاد جميع فترات التوقف المسموحة ({match.HomeTeam.MaxStoppageWindows} فترات توقف)!", $"All {match.HomeTeam.MaxStoppageWindows} substitution opportunities used"));
                }
                return;
            }

            var bench = CreateBenchMenu(match.HomeTeam, screenReader, pushMenu, popMenu, match);
            pushMenu(bench);
        });

        menu.AddItem(T("تغيير الخطة التكتيكية", "Change formation"), () =>
        {
            var form = CreateFormationMenu(match.HomeTeam, screenReader, popMenu);
            pushMenu(form);
        });

        menu.AddItem(T("الأسلوب والتكتيك (الشرايط وطريقة الدفاع)", "Playing style and tactics"), () =>
        {
            if (OpenTacticsDialog != null) { OpenTacticsDialog(match.HomeTeam); return; }
            var tactMenu = CreateTacticsMenu(match.HomeTeam, screenReader, popMenu);
            pushMenu(tactMenu);
        });

        menu.AddItem(T("المستشار التكتيكي", "Tactical advisor"), async () =>
        {
            var advisorMenu = new AccessibleMenu(T("المستشار التكتيكي", "Tactical advisor"), screenReader);
            var language = MatchSettings.Current.Language;
            string report = await Task.Run(() => TacticalAiAdvisor.GenerateTacticalReport(match, language));
            advisorMenu.AddItem(T("التقرير والتحليل التكتيكي الشامل", "Tactical report"), () =>
            {
                screenReader.Speak(report);
            });

            var recs = TacticalAiAdvisor.GetRecommendations(match, () =>
            {
                screenReader.Speak(T("تم تطبيق التوصية التكتيكية بنجاح!", "Tactical recommendation applied"));
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

            advisorMenu.AddItem(T("رجوع", "Back"), () => popMenu());
            pushMenu(advisorMenu);
        });

        menu.AddItem(T("إحصائيات المباراة", "Match statistics"), () =>
        {
            var stats = new AccessibleMenu(T("إحصائيات المباراة", "Match statistics"), screenReader);
            foreach (string line in match.GetStatisticsAnnouncements())
            {
                string item = line;
                stats.AddItem(item, () => screenReader.Speak(item));
            }
            stats.AddItem(T("رجوع", "Back"), popMenu);
            pushMenu(stats);
        });

        menu.AddItem(T("خروج للقائمة الرئيسية", "End match"), () =>
        {
            screenReader.Speak(T("تم إنهاء اللقاء والعودة إلى القائمة الرئيسية", "Match ended"));
            onExit();
        });

        return menu;
    }
}
