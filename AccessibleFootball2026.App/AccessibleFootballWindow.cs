using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AccessibleFootball2026.App.Menus;
using AccessibleFootball2026.Audio;
using AccessibleFootball2026.Audio.Anthems;
using AccessibleFootball2026.Audio.Commentary;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Input;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;
using AccessibleFootball2026.Core.Tactics;
using AccessibleFootball2026.Input;

namespace AccessibleFootball2026.App;

/// <summary>
/// النافذة الرئيسية لتطبيق كرة القدم الصوتية (Window-Based GUI - Zero Terminal):
/// واجهة رسومية خالية تماماً من شاشات الكونسول أو موجه الأوامر
/// مصممة خصيصاً للمكفوفين بأعلى توافق مع NVDA وتجربة صوتية متكاملة 100%
/// </summary>
public class AccessibleFootballWindow : Form
{
    private readonly IScreenReader _screenReader;
    private readonly MatchEventBus _eventBus;
    private readonly KeyboardInputHandler _inputHandler;
    private readonly CommentaryEngine _commentaryEngine;
    private readonly SmartAudioCoordinator _coordinator;
    private readonly AnthemEngine _anthemEngine;

    // حالة القوائم
    private readonly Stack<AccessibleMenu> _menuHistory = new();
    private AccessibleMenu? _currentMenu = null;
    private bool _isInStartupIntro = true;

    // حالة المباراة الحية
    private MatchEngine? _activeMatch = null;
    private System.Windows.Forms.Timer? _gameLoopTimer = null;
    private DateTime _lastFrameTime = DateTime.UtcNow;

    // عناصر واجهة العرض للمرافقين المبصرين
    private Label _titleLabel = null!;
    private Label _statusLabel = null!;
    private Label _hintLabel = null!;
    private ListBox _displayListBox = null!;

    public AccessibleFootballWindow(
        IScreenReader screenReader,
        MatchEventBus eventBus,
        KeyboardInputHandler inputHandler,
        CommentaryEngine commentaryEngine,
        SmartAudioCoordinator coordinator,
        AnthemEngine anthemEngine)
    {
        _screenReader = screenReader;
        _eventBus = eventBus;
        _inputHandler = inputHandler;
        _commentaryEngine = commentaryEngine;
        _coordinator = coordinator;
        _anthemEngine = anthemEngine;

        InitializeWindowComponents();
        SetupAudioEvents();
    }

    private void InitializeWindowComponents()
    {
        Text = "Accessible Football 2026 - كرة القدم الصوتية للمكفوفين";
        Size = new Size(850, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(20, 24, 32);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
        KeyPreview = true;

        _titleLabel = new Label
        {
            Text = "Accessible Football 2026/2027",
            Dock = DockStyle.Top,
            Height = 55,
            Font = new Font("Segoe UI", 18F, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 215, 0),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _statusLabel = new Label
        {
            Text = "جاري التحميل...",
            Dock = DockStyle.Top,
            Height = 40,
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = Color.FromArgb(144, 238, 144),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _hintLabel = new Label
        {
            Text = "التحكم: الأسهم للتنقل | Enter للتأكيد | Esc للرجوع | Space للرادار التكتيكي",
            Dock = DockStyle.Bottom,
            Height = 45,
            Font = new Font("Segoe UI", 10F),
            ForeColor = Color.LightGray,
            TextAlign = ContentAlignment.MiddleCenter
        };

        _displayListBox = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(28, 34, 46),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 14F),
            BorderStyle = BorderStyle.None,
            ItemHeight = 35
        };

        Controls.Add(_displayListBox);
        Controls.Add(_statusLabel);
        Controls.Add(_titleLabel);
        Controls.Add(_hintLabel);

        KeyDown += OnWindowKeyDown;
        KeyUp += OnWindowKeyUp;
    }

    private void SetupAudioEvents()
    {
        // ربط الخفض الصوتي لصوت الجماهير عند تحدث قارئ الشاشة أو المعلق
        _coordinator.OnCrowdVolumeDucking += factor =>
        {
            if (SoundEffectsPlayer.GetEffectiveCrowdVolume() <= 0.001f) return;
            SoundEffectsPlayer.SetCrowdVolume((int)(factor * 350));
        };

        // معالجة أحداث المباراة أولاً بالمعلق ثم بقارئ الشاشة
        _eventBus.OnMatchEvent += evt =>
        {
            _commentaryEngine.TryCommentOnEvent(evt);
            _coordinator.HandleMatchEvent(evt);
        };

        _commentaryEngine.OnCommentarySpoken += (commName, phrase) =>
        {
            _screenReader.Speak($"{commName}: {phrase}", interrupt: false);
        };
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        // 1. تشغيل نشيد دوري أبطال أوروبا الكامل كافتتاحية للعبة (Startup)
        _isInStartupIntro = true;
        _anthemEngine.PlayChampionsLeagueSample();
        // 2. الترحيب الصوتي للمكفوفين
        _screenReader.Speak("أهلاً بك في Accessible Football 2026. نشيد دوري أبطال أوروبا الكامل. اضغط على مفاتيح الأسهم للاختيار، والمسافة للاستماع.");

        // 4. بناء القائمة الرئيسية
        OpenMainMenu();
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        if (_displayListBox != null && _displayListBox.CanFocus)
        {
            _displayListBox.Focus();
        }
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        _inputHandler.Reset();
    }

    private void SetupMatchDisplayListBox(Team homeTeam, Team awayTeam)
    {
        _displayListBox.Visible = true;
        _displayListBox.Enabled = true;
        _displayListBox.Items.Clear();
        _displayListBox.Items.Add($"المباراة جارية الآن: {homeTeam.Name} ضد {awayTeam.Name}");
        _displayListBox.Items.Add("التحكم: S للتمرير | D للرفع والعرضيات | W للتسديد | A للبينية | Shift للجري | Ctrl لتبديل المدافع | Space للرادار");
        _displayListBox.Items.Add("الدفاع: Ctrl لتبديل المدافع | S للاستخلاص الهادئ | W/D/A للاستخلاص القوي");
        _displayListBox.Items.Add("التكتيك: Space أو C للرادار التكتيكي | Esc للإيقاف المؤقت | Q للانسحاب");
        _displayListBox.SelectedIndex = 0;
        if (_displayListBox.CanFocus)
        {
            _displayListBox.Focus();
        }
    }

    // =========================================================================
    // نظام القوائم الصوتية التفاعلية
    // =========================================================================

    public void SetActiveMenu(AccessibleMenu menu)
    {
        _currentMenu = menu;
        _displayListBox.Visible = true;
        _displayListBox.Enabled = true;
        _displayListBox.Focus();
        _statusLabel.Text = menu.MenuTitle;
        RefreshMenuDisplay();
        menu.Open();
    }

    public void PushMenu(AccessibleMenu menu)
    {
        if (_currentMenu != null)
        {
            _menuHistory.Push(_currentMenu);
        }
        SetActiveMenu(menu);
    }

    public void PopMenu()
    {
        if (_menuHistory.Count > 0)
        {
            var prev = _menuHistory.Pop();
            SetActiveMenu(prev);
            if (_menuHistory.Count == 0 && !_isInStartupIntro)
            {
                SoundEffectsPlayer.StartMenuMusic();
            }
        }
        else
        {
            OpenMainMenu();
        }
    }

    private void RefreshMenuDisplay()
    {
        if (_currentMenu == null) return;
        _displayListBox.Items.Clear();
        for (int i = 0; i < _currentMenu.Count; i++)
        {
            _displayListBox.Items.Add(_currentMenu.GetItem(i).Title);
        }
        if (_displayListBox.Items.Count > 0)
        {
            _displayListBox.SelectedIndex = _currentMenu.GetCurrentIndex();
        }
    }

    private void UpdateWindowHeadings()
    {
        Text = MatchSettings.Current.Language == UiLanguage.English
            ? "Accessible Football 2026 - Audio Football for the Blind"
            : "Accessible Football 2026 - كرة القدم الصوتية للمكفوفين";

        _titleLabel.Text = "Accessible Football 2026/2027";

        _hintLabel.Text = MatchSettings.Current.Language == UiLanguage.English
            ? "Controls: Arrows navigate | Enter confirms | Esc returns | Space tactical radar"
            : "التحكم: الأسهم للتنقل | Enter للتأكيد | Esc للرجوع | Space للرادار التكتيكي";
    }

    public void OpenMainMenu()
    {
        UpdateWindowHeadings();
        _menuHistory.Clear();
        if (!_isInStartupIntro)
        {
            SoundEffectsPlayer.StartMenuMusic();
        }
        var menu = new AccessibleMenu(LocalizationService.GetString("MainMenuTitle"), _screenReader);

        menu.AddItem(LocalizationService.GetString("ExhibitionMatch"), () =>
        {
            StartMatchWizardFlow();
        });

        menu.AddItem(LocalizationService.GetString("DraftMode"), () =>
        {
            StartDreamTeamBuilderFlow();
        });

        menu.AddItem(LocalizationService.GetString("UserGuide"), () =>
        {
            StartUserGuideFlow();
        });

        menu.AddItem(LocalizationService.GetString("SoundAcademy"), () =>
        {
            StartSoundAcademyFlow();
        });

        menu.AddItem(LocalizationService.GetString("Settings"), () =>
        {
            StartMatchSettingsFlow();
        });

        menu.AddItem(LocalizationService.GetString("GamePlan"), () =>
        {
            StartGamePlanFlow();
        });

        menu.AddItem(LocalizationService.GetString("Tournaments"), () =>
        {
            StartTournamentsBrowserFlow();
        });

        menu.AddItem(LocalizationService.GetString("ExitGame"), () =>
        {
            SoundEffectsPlayer.StopAll();
            _screenReader.Speak(LocalizationService.GetString("ExitFarewell"));
            Close();
        });

        SetActiveMenu(menu);
    }

    private void StartUserGuideFlow()
    {
        var menu = UserGuideMenuBuilder.CreateUserGuideMenu(_screenReader, PushMenu, PopMenu);
        PushMenu(menu);
    }

    private void StartSoundAcademyFlow()
    {
        SoundEffectsPlayer.StopMenuMusic();
        var menu = SoundAcademyMenuBuilder.CreateSoundAcademyMenu(_screenReader, _anthemEngine, PushMenu, PopMenu);
        PushMenu(menu);
    }

    // =========================================================================
    // معالج خطوات بدء المباراة الشامل (Match Wizard Flow)
    // =========================================================================

    private void StartMatchWizardFlow()
    {
        var teamManager = new TeamSelectionManager(_screenReader, PushMenu, PopMenu);
        Team? selectedHome = null;
        Team? selectedAway = null;

        var homeSelectMenu = teamManager.CreateHomeTeamSelectMenu(home =>
        {
            selectedHome = home;
            var awaySelectMenu = teamManager.CreateAwayTeamSelectMenu(away =>
            {
                selectedAway = away;
                var commSelectMenu = CreateCommentatorSelectMenu(commentator =>
                {
                    var preMatchMenu = GamePlanMenuBuilder.CreatePreMatchMenu(selectedHome, _screenReader, PushMenu, PopMenu, onStartMatch: () =>
                    {
                        StartInteractiveMatch(selectedHome, selectedAway);
                    });
                    PushMenu(preMatchMenu);
                });
                PushMenu(commSelectMenu);
            });
            PushMenu(awaySelectMenu);
        });

        PushMenu(homeSelectMenu);
    }

    // =========================================================================
    // وضع درافت الأندية الأربعة التفاعلي الشامل
    // =========================================================================

    private void StartDreamTeamBuilderFlow()
    {
        var builder = new DreamTeamBuilder();
        var allClubs = TeamDatabase.GetAllClubs();

        // 1. يختار المستخدم 4 أندية لفريقه
        ChooseClubsForUserTeam(builder, allClubs, 1);
    }

    private void ChooseClubsForUserTeam(DreamTeamBuilder builder, List<Team> allClubs, int clubNumber)
    {
        var menu = new AccessibleMenu($"اختر النادي رقم {clubNumber} من 4 لتشكيلة فريقك", _screenReader);
        var availableClubs = allClubs.Where(c => !builder.UserSourceClubs.Any(u => u.Id == c.Id)).ToList();

        foreach (var c in availableClubs)
        {
            var club = c;
            menu.AddItem(club.Name, () =>
            {
                builder.AddUserSourceClub(club);
                _screenReader.Speak($"تم اختيار {club.Name} لفريقك ({clubNumber} من 4).");

                if (clubNumber < 4)
                {
                    ChooseClubsForUserTeam(builder, allClubs, clubNumber + 1);
                }
                else
                {
                    _screenReader.Speak("تم اختيار الأندية الأربعة لفريقك بنجاح! الآن اختر 4 أندية لفريق الكمبيوتر الخصم.");
                    ChooseClubsForCpuTeam(builder, allClubs, 1);
                }
            });
        }

        menu.AddItem("رجوع للقائمة السابقة", () => PopMenu());
        PushMenu(menu);
    }

    private void ChooseClubsForCpuTeam(DreamTeamBuilder builder, List<Team> allClubs, int clubNumber)
    {
        var menu = new AccessibleMenu($"اختر النادي رقم {clubNumber} من 4 لتشكيلة الكمبيوتر (الخصم)", _screenReader);
        var availableClubs = allClubs.Where(c => !builder.CpuSourceClubs.Any(cpu => cpu.Id == c.Id)).ToList();

        foreach (var c in availableClubs)
        {
            var club = c;
            menu.AddItem(club.Name, () =>
            {
                builder.AddCpuSourceClub(club);
                _screenReader.Speak($"تم اختيار {club.Name} للكمبيوتر ({clubNumber} من 4).");

                if (clubNumber < 4)
                {
                    ChooseClubsForCpuTeam(builder, allClubs, clubNumber + 1);
                }
                else
                {
                    ExecuteDraftAndShowPreMatch(builder);
                }
            });
        }

        menu.AddItem("رجوع", () => PopMenu());
        PushMenu(menu);
    }

    private void ExecuteDraftAndShowPreMatch(DreamTeamBuilder builder)
    {
        builder.DraftBothTeams();

        string userClubsNames = string.Join("، ", builder.UserSourceClubs.Select(c => c.Name));
        string cpuClubsNames = string.Join("، ", builder.CpuSourceClubs.Select(c => c.Name));

        string stars = string.Join("، ", builder.UserDreamTeam.Players.Take(4).Select(p => $"{p.Name} ({p.CurrentAssignedPosition})"));
        string cpuStars = string.Join("، ", builder.CpuDreamTeam.Players.Take(4).Select(p => $"{p.Name} ({p.CurrentAssignedPosition})"));

        string msg = $"اكتمل سحب القرعة العشوائية بواسطة الكمبيوتر بنجاح! " +
                     $"تشكيلة فريقك من أندية: {userClubsNames}. أبرز نجومك: {stars}. " +
                     $"تشكيلة الخصم من أندية: {cpuClubsNames}. أبرز نجومه: {cpuStars}. " +
                     $"نظام مانع التكرار الصوتي مفعّل: لا يوجد أي لاعب مكرر بين الفريقين.";
        _screenReader.Speak(msg);

        ShowDraftPreMatchMenu(builder);
    }

    private void ShowDraftPreMatchMenu(DreamTeamBuilder builder)
    {
        var menu = new AccessibleMenu("خيارات وضع الـ 4 فرق", _screenReader);

        menu.AddItem("بدء المباراة", () =>
        {
            var commMenu = CreateCommentatorSelectMenu(_ =>
            {
                StartInteractiveMatch(builder.UserDreamTeam, builder.CpuDreamTeam);
            });
            PushMenu(commMenu);
        });

        menu.AddItem("التشكيلة الأساسية", () =>
        {
            var lineup = GamePlanMenuBuilder.CreateLineupMenu(builder.UserDreamTeam, _screenReader, PushMenu, PopMenu);
            PushMenu(lineup);
        });

        menu.AddItem("دكة البدلاء", () =>
        {
            var bench = GamePlanMenuBuilder.CreateBenchMenu(builder.UserDreamTeam, _screenReader, PushMenu, PopMenu);
            PushMenu(bench);
        });

        menu.AddItem("أدوار المتخصصين", () =>
        {
            var spec = GamePlanMenuBuilder.CreateSpecialistsMenu(builder.UserDreamTeam, _screenReader, PushMenu, PopMenu);
            PushMenu(spec);
        });

        menu.AddItem("تغيير الخطة التكتيكية", () =>
        {
            var form = GamePlanMenuBuilder.CreateFormationMenu(builder.UserDreamTeam, _screenReader, PopMenu);
            PushMenu(form);
        });

        menu.AddItem("إعادة السحب العشوائي", () =>
        {
            builder.DraftBothTeams();
            _screenReader.Speak("تمت إعادة السحب العشوائي بواسطة الكمبيوتر لتشكيلتي الفريقين بنجاح!");
        });

        menu.AddItem("إعادة اختيار الأندية", () =>
        {
            StartDreamTeamBuilderFlow();
        });

        menu.AddItem("رجوع", () =>
        {
            OpenMainMenu();
        });

        PushMenu(menu);
    }

    // =========================================================================
    // إعدادات المباراة واللعبة الصوتية الشاملة
    // =========================================================================

    private void StartMatchSettingsFlow()
    {
        using var settingsForm = new AccessibleSettingsForm(_screenReader);
        settingsForm.ShowDialog(this);
        UpdateWindowHeadings();
        RefreshMenuDisplay();
    }

    private AccessibleMenu CreateCommentatorSelectMenu(Action<CommentatorProfile> onSelected)
    {
        var menu = new AccessibleMenu("اختر المعلق الصوتي المعتمد للمباراة", _screenReader);
        var list = _commentaryEngine.GetAvailableCommentators();

        foreach (var c in list)
        {
            var current = c;
            menu.AddItem($"{current.DisplayName}، {current.Region}", () =>
            {
                _commentaryEngine.SwitchCommentator(current.Id);
                _screenReader.Speak($"تم اعتماد المعلق {current.DisplayName}");
                onSelected(current);
            });
        }

        menu.AddItem("رجوع", () => PopMenu());
        return menu;
    }

    private void StartTournamentsBrowserFlow()
    {
        var tournaments = TournamentDatabase.GetAllTournaments();
        var menu = new AccessibleMenu("البطولات والدوريات", _screenReader);

        foreach (var t in tournaments)
        {
            var currentTourney = t;
            menu.AddItem(currentTourney.Name, () =>
            {
                var tourneyMenu = new AccessibleMenu(currentTourney.Name, _screenReader);

                tourneyMenu.AddItem("الفرق المشاركة", () =>
                {
                    string names = string.Join("، ", currentTourney.Teams.Select(tm => tm.Name));
                    _screenReader.Speak($"المشاركون في {currentTourney.Name} هم: {names}");
                });

                tourneyMenu.AddItem("بدء مباراة", () =>
                {
                    if (currentTourney.Teams.Count >= 2)
                    {
                        var team1 = currentTourney.Teams[0];
                        var team2 = currentTourney.Teams[1];
                        var commMenu = CreateCommentatorSelectMenu(_ =>
                        {
                            StartInteractiveMatch(team1, team2);
                        });
                        PushMenu(commMenu);
                    }
                });

                tourneyMenu.AddItem("رجوع", () => PopMenu());
                PushMenu(tourneyMenu);
            });
        }

        menu.AddItem("رجوع", () => PopMenu());
        PushMenu(menu);
    }

    private void StartGamePlanFlow()
    {
        var teamManager = new TeamSelectionManager(_screenReader, PushMenu, PopMenu);
        var menu = teamManager.CreateHomeTeamSelectMenu(team =>
        {
            var planMenu = GamePlanMenuBuilder.CreatePreMatchMenu(team, _screenReader, PushMenu, PopMenu, onStartMatch: () =>
            {
                _screenReader.Speak("تم حفظ الخطة التكتيكية بنجاح!");
                PopMenu();
            });
            PushMenu(planMenu);
        });
        PushMenu(menu);
    }

    private void StartCommentarySettingsFlow()
    {
        var menu = CreateCommentatorSelectMenu(_ => PopMenu());
        PushMenu(menu);
    }

    private void ShowControlsGuide()
    {
        string guide = "دليل التحكم الكامل لأزرار اللعب: " +
                       "الأسهم للتحرك مع الكرة في الاتجاهات الأربعة وضبط زاوية التسديد نحو المرمى. " +
                       "زر Shift الأيسر للجري السريع والانطلاق في المساحات. " +
                       "زر S للتمرير الأرضي المباشر لأقرب زميل. " +
                       "زر D للرفع أو التمريرة العالية والعرضية إلى داخل منطقة الجزاء. " +
                       "زر W للتسديد القوي والمباشر نحو المرمى. " +
                       "زر A للتمريرة البينية في المساحة. " +
                       "زر المسافة أو C لتشغيل الرادار التكتيكي وسماع مكان الكرة والمسافة نحو المرمى. " +
                       "زر Escape لفتح قائمة الإيقاف المؤقت والتبديلات وتعديل التكتيك. " +
                       "وزر Q للانسحاب والعودة للقائمة الرئيسية.";
        _screenReader.Speak(guide);
    }

    // =========================================================================
    // محرك المباراة التفاعلي (Interactive Real-Time Match Engine)
    // =========================================================================

    private void StartInteractiveMatch(Team homeTeam, Team awayTeam)
    {
        _currentMenu = null;
        _menuHistory.Clear();
        _inputHandler.Reset();
        SetupMatchDisplayListBox(homeTeam, awayTeam);

        SoundEffectsPlayer.StopMenuMusic();

        // مزامنة المعلق المعتمد مع الإعدادات
        _commentaryEngine.SwitchCommentator(MatchSettings.Current.Commentator);

        if (homeTeam.IsNationalTeam || awayTeam.IsNationalTeam)
        {
            _anthemEngine.PlayMatchNationalAnthems(homeTeam, awayTeam);
        }

        // تشغيل صوت المدرجات الحقيقي المستمر وصافرة الحكم
        SoundEffectsPlayer.StartCrowdAmbiance();
        SoundEffectsPlayer.PlayWhistle();

        var match = new MatchEngine(homeTeam, awayTeam, _eventBus, _inputHandler);
        _activeMatch = match;

        match.OnNearBoundary += () =>
        {
            SoundEffectsPlayer.PlayBoundaryWarning();
        };

        // تفاعل صخب الجماهير التلقائي: يعلو مع اقتراب الهجمة ويهدأ في منتصف الملعب
        match.OnCrowdVolumeChanged += vol =>
        {
            SoundEffectsPlayer.SetCrowdVolume(vol);
        };

        string introSpeech = $"انطلاق المباراة! {homeTeam.Name} ضد {awayTeam.Name}، بصوت المعلق {_commentaryEngine.ActiveCommentator.DisplayName}. " +
                             $"الكرة معك الآن مع {match.ActiveUserPlayer.Name} في دائرة منتصف الملعب.";
        _screenReader.Speak(introSpeech);

        _titleLabel.Text = $"{homeTeam.Name} ضد {awayTeam.Name}";
        _statusLabel.Text = $"النتيجة: {homeTeam.Score} - {awayTeam.Score} | المعلق: {_commentaryEngine.ActiveCommentator.DisplayName}";

        _lastFrameTime = DateTime.UtcNow;

        _gameLoopTimer?.Dispose();
        _gameLoopTimer = new System.Windows.Forms.Timer { Interval = 20 }; // 50 updates per second
        _gameLoopTimer.Tick += (s, ev) =>
        {
            if (_activeMatch == null) return;

            DateTime now = DateTime.UtcNow;
            float dt = (float)(now - _lastFrameTime).TotalSeconds;
            _lastFrameTime = now;

            _activeMatch.Update(dt);

            // منارة صوت المرمى ثلاثية الأبعاد (3D Spatial Goal Beacon)
            if (_activeMatch.State == MatchState.InPlay)
            {
                bool hasPossession = _activeMatch.Ball.Owner == _activeMatch.ActiveUserPlayer;
                GoalBeaconPlayer.UpdateBeacon(
                    _activeMatch.ActiveUserPlayer.Position2D.X,
                    _activeMatch.ActiveUserPlayer.Position2D.Y,
                    isSecondHalf: !_activeMatch.UserAttackingRight,
                    hasPossession: hasPossession
                );
            }

            string matchStatus = $"المباراة جارية (الدقيقة {(int)_activeMatch.MatchTimeMinutes}): {_activeMatch.HomeTeam.Name} {_activeMatch.HomeTeam.Score} - {_activeMatch.AwayTeam.Score} {_activeMatch.AwayTeam.Name}";
            // أثناء اللعب النشط: نحدث _statusLabel المرئي فقط ونمنع تعديل _displayListBox لمنع تشويش قارئ الشاشة NVDA
            if (_activeMatch.State != MatchState.InPlay)
            {
                if (_displayListBox.Items.Count > 0 && (string)_displayListBox.Items[0] != matchStatus)
                {
                    _displayListBox.Items[0] = matchStatus;
                }
            }

            if (_activeMatch.State == MatchState.HalfTime && _currentMenu == null)
            {
                _inputHandler.Reset();

                if (MatchSettings.Current.AutoSubstitutionsEnabled &&
                    TacticalAiAdvisor.TryPerformAutoSubstitution(_activeMatch, out string autoSubMsg))
                {
                    _screenReader.Speak(autoSubMsg);
                }

                var halfTimeMenu = GamePlanMenuBuilder.CreatePauseMenu(
                    _activeMatch, 
                    _screenReader, 
                    _commentaryEngine,
                    PushMenu,
                    PopMenu,
                    onResumeMatch: () =>
                    {
                        _currentMenu = null;
                        _inputHandler.Reset();
                        SetupMatchDisplayListBox(_activeMatch.HomeTeam, _activeMatch.AwayTeam);
                        _activeMatch.ResumeMatch();
                    },
                    onExit: () =>
                    {
                        EndMatchAndReturnToMenu();
                    });

                PushMenu(halfTimeMenu);
                return;
            }

            if (_activeMatch.State == MatchState.PenaltyShootout)
            {
                _gameLoopTimer?.Stop();
                StartInteractivePenaltyShootout(_activeMatch);
                return;
            }

            if (_activeMatch.State == MatchState.GameOver)
            {
                EndMatchAndReturnToMenu();
            }
        };
        _gameLoopTimer.Start();
    }

    private void StartInteractivePenaltyShootout(MatchEngine match)
    {
        SoundEffectsPlayer.StopCrowdAmbiance();
        SoundEffectsPlayer.PlayWhistle();
        _screenReader.Speak("انطلاق ركلات الترجيح الحاسمة! 5 ركلات لكل فريق لحسم بطل المباراة.");

        int userPenaltiesScored = 0;
        int cpuPenaltiesScored = 0;
        int round = 1;

        void PlayUserTurn()
        {
            if (round > 5 && userPenaltiesScored != cpuPenaltiesScored)
            {
                FinishShootout(match, userPenaltiesScored, cpuPenaltiesScored);
                return;
            }

            var shotMenu = new AccessibleMenu($"ركلات الترجيح - الركلة رقم {round} لفريقك ({userPenaltiesScored} - {cpuPenaltiesScored}) - اختر زاوية التسديد", _screenReader);
            shotMenu.AddItem("التسديد في أقصى الزاوية اليمنى", () => ExecuteUserPenaltyShot(0));
            shotMenu.AddItem("التسديد بقوة في منتصف المرمى", () => ExecuteUserPenaltyShot(1));
            shotMenu.AddItem("التسديد في أقصى الزاوية اليسرى", () => ExecuteUserPenaltyShot(2));
            PushMenu(shotMenu);
        }

        void ExecuteUserPenaltyShot(int angle)
        {
            PopMenu();
            int keeperDive = Random.Shared.Next(3);
            SoundEffectsPlayer.PlayKick(angle == 0 ? 0.8f : (angle == 2 ? -0.8f : 0.0f));

            if (angle != keeperDive)
            {
                userPenaltiesScored++;
                SoundEffectsPlayer.PlayGoalNet();
                SoundEffectsPlayer.PlayCrowdGoal();
                _screenReader.Speak($"جوووووول! هدف رائع في الشباك! النتيجة الآن: فريقك {userPenaltiesScored}، والخصم {cpuPenaltiesScored}.");
            }
            else
            {
                SoundEffectsPlayer.PlayCrowdMiss();
                _screenReader.Speak($"تصدى لها الحارس ببراعة! ركلة ضائعة! النتيجة الآن: فريقك {userPenaltiesScored}، والخصم {cpuPenaltiesScored}.");
            }

            Task.Delay(1800).ContinueWith(_ =>
            {
                if (!IsDisposed && IsHandleCreated)
                {
                    Invoke(() => PlayCpuTurn());
                }
            });
        }

        void PlayCpuTurn()
        {
            var diveMenu = new AccessibleMenu($"ركلات الترجيح - الركلة رقم {round} للخصم ({userPenaltiesScored} - {cpuPenaltiesScored}) - اختر ارتماء حارسك", _screenReader);
            diveMenu.AddItem("الارتماء نحو الزاوية اليمنى", () => ExecuteCpuPenaltyShot(0));
            diveMenu.AddItem("الثبات في منتصف المرمى", () => ExecuteCpuPenaltyShot(1));
            diveMenu.AddItem("الارتماء نحو الزاوية اليسرى", () => ExecuteCpuPenaltyShot(2));
            PushMenu(diveMenu);
        }

        void ExecuteCpuPenaltyShot(int userDive)
        {
            PopMenu();
            int cpuShot = Random.Shared.Next(3);
            SoundEffectsPlayer.PlayKick(cpuShot == 0 ? 0.8f : (cpuShot == 2 ? -0.8f : 0.0f));

            if (userDive == cpuShot)
            {
                SoundEffectsPlayer.PlayCrowdCheer();
                _screenReader.Speak($"الله على تصدي تاريخي! حارسك تصدى لركلة الخصم بنجاح! النتيجة الآن: فريقك {userPenaltiesScored}، والخصم {cpuPenaltiesScored}.");
            }
            else
            {
                cpuPenaltiesScored++;
                SoundEffectsPlayer.PlayGoalNet();
                _screenReader.Speak($"هدف لصالح الخصم في ركلات الترجيح. النتيجة الآن: فريقك {userPenaltiesScored}، والخصم {cpuPenaltiesScored}.");
            }

            round++;
            Task.Delay(1800).ContinueWith(_ =>
            {
                if (!IsDisposed && IsHandleCreated)
                {
                    Invoke(() =>
                    {
                        if (round > 5 && userPenaltiesScored != cpuPenaltiesScored)
                        {
                            FinishShootout(match, userPenaltiesScored, cpuPenaltiesScored);
                        }
                        else if (round > 7)
                        {
                            FinishShootout(match, userPenaltiesScored, cpuPenaltiesScored);
                        }
                        else
                        {
                            PlayUserTurn();
                        }
                    });
                }
            });
        }

        void FinishShootout(MatchEngine m, int uScore, int cScore)
        {
            _activeMatch = null;
            _currentMenu = null;
            _inputHandler.Reset();
            _displayListBox.Visible = true;
            SoundEffectsPlayer.PlayWhistle();

            string outcome = uScore > cScore
                ? $"مبرووووك الفوز العظيم! فاز فريق {m.HomeTeam.Name} بركلات الترجيح بنتيجة {uScore} مقابل {cScore}!"
                : $"هارد لك! فاز فريق {m.AwayTeam.Name} بركلات الترجيح بنتيجة {cScore} مقابل {uScore}.";

            _screenReader.Speak(outcome);
            SoundEffectsPlayer.StartMenuMusic();
            OpenMainMenu();
        }

        PlayUserTurn();
    }

    private void EndMatchAndReturnToMenu()
    {
        _gameLoopTimer?.Stop();
        _gameLoopTimer?.Dispose();
        _gameLoopTimer = null;
        _inputHandler.Reset();

        if (_activeMatch != null)
        {
            SoundEffectsPlayer.StopCrowdAmbiance();
            SoundEffectsPlayer.PlayWhistle();
            string finalSpeech = $"نهاية المباراة! النتيجة النهائية: {_activeMatch.HomeTeam.Name} {_activeMatch.HomeTeam.Score}، مقابل {_activeMatch.AwayTeam.Score} لـ {_activeMatch.AwayTeam.Name}. العودة للقائمة الرئيسية.";
            _screenReader.Speak(finalSpeech);
            _activeMatch = null;
        }

        _displayListBox.Visible = true;
        SoundEffectsPlayer.StartMenuMusic();
        OpenMainMenu();
    }

    // =========================================================================
    // معالجة مفاتيح الكيبورد الفورية
    // =========================================================================

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (_isInStartupIntro)
        {
            _isInStartupIntro = false;
            _anthemEngine.StopAllSounds();
            SoundEffectsPlayer.StartMenuMusic();
            OpenMainMenu();
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }
        // 1. أولوية القوائم: إذا كنا داخل أي قائمة (الرئيسية، التشكيلة، الإعدادات، أو الإيقاف المؤقت داخل الماتش)
        if (_currentMenu != null)
        {
            if (e.KeyCode == Keys.Up)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                _currentMenu.NavigatePrevious();
                if (_displayListBox.Items.Count > 0)
                {
                    _displayListBox.SelectedIndex = _currentMenu.GetCurrentIndex();
                }
                return;
            }
            if (e.KeyCode == Keys.Down)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                _currentMenu.NavigateNext();
                if (_displayListBox.Items.Count > 0)
                {
                    _displayListBox.SelectedIndex = _currentMenu.GetCurrentIndex();
                }
                return;
            }
            if (e.KeyCode == Keys.Left)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                _currentMenu.NavigateLeft();
                RefreshMenuDisplay();
                return;
            }
            if (e.KeyCode == Keys.Right)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                _currentMenu.NavigateRight();
                RefreshMenuDisplay();
                return;
            }
            if (e.KeyCode == Keys.Space)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                _currentMenu.ToggleCurrent();
                RefreshMenuDisplay();
                return;
            }
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                _currentMenu.SelectCurrent();
                RefreshMenuDisplay();
                return;
            }
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                SoundEffectsPlayer.PlayMenuBack();

                if (_activeMatch != null && _menuHistory.Count == 0)
                {
                    // استئناف المباراة مباشرة من قائمة الإيقاف المؤقت الرئيسية
                    _currentMenu = null;
                    SetupMatchDisplayListBox(_activeMatch.HomeTeam, _activeMatch.AwayTeam);
                    _activeMatch.ResumeMatch();
                    _screenReader.Speak("استئناف اللقاء");
                    return;
                }

                _screenReader.Speak("رجوع");
                PopMenu();
                return;
            }
            return;
        }

        // 2. إذا كنا في مباراة حية جارية (التحكم بالملعب)
        if (_activeMatch != null)
        {
            if (e.KeyCode == Keys.Q)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                EndMatchAndReturnToMenu();
                return;
            }

            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                _inputHandler.Reset();
                _gameLoopTimer?.Stop();
                SoundEffectsPlayer.StopCommentaryAudio();
                SoundEffectsPlayer.PauseCrowdAmbiance();
                _activeMatch.PauseMatch();
                _displayListBox.Visible = true;

                var pauseMenu = GamePlanMenuBuilder.CreatePauseMenu(
                    _activeMatch, 
                    _screenReader, 
                    _commentaryEngine,
                    PushMenu,
                    PopMenu,
                    onResumeMatch: () =>
                    {
                        _currentMenu = null;
                        _inputHandler.Reset();
                        SetupMatchDisplayListBox(_activeMatch.HomeTeam, _activeMatch.AwayTeam);
                        _activeMatch.ResumeMatch();
                        SoundEffectsPlayer.ResumeCrowdAmbiance();
                        _lastFrameTime = DateTime.UtcNow;
                        _gameLoopTimer?.Start();
                    },
                    onExit: () =>
                    {
                        EndMatchAndReturnToMenu();
                    });

                PushMenu(pauseMenu);
                return;
            }

            // فحص مفاتيح المباراة الفورية وتمريرها لمعالج المدخلات
            if (IsMatchKey(e.KeyCode))
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                bool isPossession = _activeMatch.Ball.Owner != null && 
                                    _activeMatch.HomeTeam.Players.Contains(_activeMatch.Ball.Owner);

                _inputHandler.HandleKeyDown((int)e.KeyCode, isPossession);
                return;
            }
        }
    }

    private void OnWindowKeyUp(object? sender, KeyEventArgs e)
    {
        if (_activeMatch != null && IsMatchKey(e.KeyCode))
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            _inputHandler.HandleKeyUp((int)e.KeyCode);
        }
    }

    private static bool IsMatchKey(Keys key) => key switch
    {
        Keys.Up or Keys.Down or Keys.Left or Keys.Right => true,
        Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey or Keys.Shift => true,
        Keys.ControlKey or Keys.LControlKey or Keys.RControlKey or Keys.Control => true,
        Keys.D or Keys.W or Keys.S or Keys.A => true,
        Keys.Space or Keys.C => true,
        _ => false
    };
}
