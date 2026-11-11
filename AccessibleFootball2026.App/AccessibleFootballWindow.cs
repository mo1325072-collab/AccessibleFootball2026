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
using AccessibleFootball2026.Audio.SFX;
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
    private readonly StadiumSoundEngine _stadiumSoundEngine = new();
    private readonly CallnameManager _callnameManager = new();

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

        _stadiumSoundEngine.OnDiscontentWhistlePlayed += team =>
        {
            if(_activeMatch is not {} current)return;
            var context=current.GetCommentarySnapshot().With(("crowd.protesting",true),("team.id",team.Id),
                ("analysis.result",team.Score<(team==current.HomeTeam?current.AwayTeam:current.HomeTeam).Score?"loss":"other"));
            _commentaryEngine.TryCommentOnEvent(new MatchEvent(MatchEventType.RefereeProtest,"صافرات استهجان من الجمهور") {Context=context},force:true);
        };

        // معالجة أحداث المباراة أولاً بالمعلق ثم بقارئ الشاشة
        _eventBus.OnMatchEvent += evt =>
        {
            evt.Context = evt.Context.With(("crowd.active", SoundEffectsPlayer.IsCrowdActive));
            _commentaryEngine.TryCommentOnEvent(evt);
            if (evt.Type == MatchEventType.GoalScored && _activeMatch?.LastGoalReplay is { } replay)
                _commentaryEngine.QueueGoalReplay(replay);
            _coordinator.HandleMatchEvent(evt);
            if (evt.Type == MatchEventType.LineupRequested)
                _commentaryEngine.RunAfterRecordedCommentary(() => _screenReader.Speak(evt.Description));
            if (_activeMatch != null)
            {
                _stadiumSoundEngine.HandleMatchContext(evt, _activeMatch.HomeTeam.Score, _activeMatch.AwayTeam.Score, _activeMatch.MatchTimeMinutes);
                // نداء اسم اللاعب صاحب اللمسة الفعلية (تمريرة/تسديدة/عرضية من أي فريق) وليس لاعب المستخدم دائماً
                if (_commentaryEngine.ActiveCommentator.Id==CommentatorId.RaoufKhlif && evt.Type == MatchEventType.BallKicked && evt.SubjectPlayer != null)
                {
                    _callnameManager.TryPlayPlayerCallname(evt.SubjectPlayer);
                }
            }
        };

        _coordinator.OnGoalSequenceCompleted += () =>
        {
            if (!IsDisposed && IsHandleCreated) BeginInvoke(() => _activeMatch?.CompleteGoalCelebration());
        };

        _commentaryEngine.OnCommentarySpoken += (commName, phrase) =>
        {
            _screenReader.Speak($"{commName}: {phrase}", interrupt: false);
        };

        // بدء مقطع مسجل يسكت قارئ الشاشة فوراً حتى لا يتداخل الصوتان
        _commentaryEngine.OnRecordedCommentaryStarted += _ => _screenReader.CancelSpeech();
        FormClosed += (_, _) =>
        {
            _coordinator.CancelPendingGoalSequence();
            _commentaryEngine.Dispose();
        };
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        // 1. تشغيل نشيد دوري أبطال أوروبا الكامل كافتتاحية للعبة (Startup)
        // لا يتحدث قارئ الشاشة مطلقاً في شاشة البداية احتراماً لنقاء النشيد
        _isInStartupIntro = true;
        bool anthemStarted = _anthemEngine.PlayChampionsLeagueSample();
        if (!anthemStarted)
        {
            // النشيد غير متاح (ملف مفقود أو جهاز صوت رافض): لا نترك المستخدم الكفيف
            // أمام شاشة صامتة لا تستجيب إلا لـ Enter/Space - نتجاوز الافتتاحية فوراً
            _isInStartupIntro = false;
            SoundEffectsPlayer.StartMenuMusic();
        }

        // 2. بناء القائمة الرئيسية (ستظهر وتبدأ موسيقاها بعد ضغط Enter أو Space)
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
        _displayListBox.Items.Add("Q بينية عالية | K إبعاد | B عرضية أرضية | N القائم البعيد | T التشكيلة | R اقتراب بطيء ثم تسديد");
        _displayListBox.Items.Add("التسديد: H رأسية | J مقصية | V على الطائر | L ساقطة | F مقوسة");
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
        _commentaryEngine.ClearPendingLines();
        _coordinator.CancelPendingGoalSequence();
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

        menu.AddItem("نهائي كأس مخصص (أشواط إضافية وترجيح عند التعادل)", () => StartMatchWizardFlow(cupFinal: true));

        menu.AddItem("الكلاسيكو (ريال مدريد × برشلونة)", () =>
        {
            StartClasicoWizardFlow();
        });

        menu.AddItem(LocalizationService.GetString("DraftMode"), () =>
        {
            StartDreamTeamBuilderFlow();
        });

        menu.AddItem(LocalizationService.GetString("Tournaments"), () =>
        {
            StartTournamentsBrowserFlow();
        });

        menu.AddItem(LocalizationService.GetString("GamePlan"), () =>
        {
            StartGamePlanFlow();
        });

        menu.AddItem(LocalizationService.GetString("Settings"), () =>
        {
            StartMatchSettingsFlow();
        });

        menu.AddItem(LocalizationService.GetString("SoundAcademy"), () =>
        {
            StartSoundAcademyFlow();
        });

        menu.AddItem("مكتبة تسجيلات فارس كاملة",()=>PushMenu(CommentaryLibraryMenuBuilder.Create(_commentaryEngine,_screenReader,PushMenu,PopMenu)));

        menu.AddItem(LocalizationService.GetString("UserGuide"), () =>
        {
            StartUserGuideFlow();
        });

        menu.AddItem(LocalizationService.GetString("ExitGame"), () =>
        {
            SoundEffectsPlayer.StopAll();
            _screenReader.Speak(LocalizationService.GetString("ExitFarewell"));
            Close();
        });

        SetActiveMenu(menu);
    }

    private static Team GetClasicoRealMadrid(TeamSide side)
    {
        var dbTeam = JsonDatabaseProvider.GetTeamByName("Real Madrid");
        if (dbTeam != null)
        {
            var cloned = dbTeam.Clone(side);
            cloned.SetupFormation433();
            return cloned;
        }
        var fallback = SpanishLaLigaDatabase.CreateRealMadrid();
        fallback.Side = side;
        return fallback;
    }

    private static Team GetClasicoBarcelona(TeamSide side)
    {
        var dbTeam = JsonDatabaseProvider.GetTeamByName("FC Barcelona") ?? JsonDatabaseProvider.GetTeamByName("Barcelona");
        if (dbTeam != null)
        {
            var cloned = dbTeam.Clone(side);
            cloned.SetupFormation433();
            return cloned;
        }
        var fallback = SpanishLaLigaDatabase.CreateBarcelona();
        fallback.Side = side;
        return fallback;
    }

    private void StartClasicoWizardFlow()
    {
        var teamMenu = new AccessibleMenu("اختر فريقك في الكلاسيكو", _screenReader);

        teamMenu.AddItem("ريال مدريد (أنت تلعب بريال مدريد ضد برشلونة)", () =>
        {
            var rma = GetClasicoRealMadrid(TeamSide.Home);
            var bar = GetClasicoBarcelona(TeamSide.Away);
            ChooseStadiumFlow(rma, bar);
        });

        teamMenu.AddItem("برشلونة (أنت تلعب ببرشلونة ضد ريال مدريد)", () =>
        {
            var bar = GetClasicoBarcelona(TeamSide.Home);
            var rma = GetClasicoRealMadrid(TeamSide.Away);
            ChooseStadiumFlow(bar, rma);
        });

        teamMenu.AddItem("رجوع للقائمة الرئيسية", () => PopMenu());
        PushMenu(teamMenu);
    }

    private void ChooseStadiumFlow(Team home, Team away)
    {
        var stadiumMenu = new AccessibleMenu("اختر ملعب المباراة لاختبار شرط الأهازيج والجماهير", _screenReader);

        stadiumMenu.AddItem("ملعب سانتياجو برنابيو (معقل ريال مدريد)", () =>
        {
            _stadiumSoundEngine.ActiveStadiumKey = "SantiagoBernabeu";
            _screenReader.Speak("تم اختيار ملعب سانتياجو برنابيو.");
            StartInteractiveMatch(home, away, rivalry: true);
        });

        stadiumMenu.AddItem("ملعب كامب نو (معقل برشلونة)", () =>
        {
            _stadiumSoundEngine.ActiveStadiumKey = "CampNou";
            _screenReader.Speak("تم اختيار ملعب كامب نو.");
            StartInteractiveMatch(home, away, rivalry: true);
        });

        stadiumMenu.AddItem("ملعب محايد (لاختبار الجماهير بصوت محايد وبدون أهازيج خاصة)", () =>
        {
            _stadiumSoundEngine.ActiveStadiumKey = "NeutralStadium";
            _screenReader.Speak("تم اختيار ملعب محايد. صوت الجماهير سيعمل بشكل محايد تماماً.");
            StartInteractiveMatch(home, away, rivalry: true);
        });

        stadiumMenu.AddItem("رجوع لاختيار الفريق", () => PopMenu());
        PushMenu(stadiumMenu);
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

    private void StartMatchWizardFlow(bool cupFinal = false)
    {
        var teamManager = new TeamSelectionManager(_screenReader, PushMenu, PopMenu);
        Team? selectedHome = null;
        Team? selectedAway = null;

        var homeSelectMenu = teamManager.CreateHomeTeamSelectMenu(home =>
        {
            // نسخة مستقلة: قاعدة البيانات JSON ترجع كائنات مخزنة مشتركة،
            // واللعب عليها مباشرة يترك حالة المباراة السابقة (بطاقات، إصابات، لياقة) داخل القاعدة
            selectedHome = home.Clone(TeamSide.Home);
            var awaySelectMenu = teamManager.CreateAwayTeamSelectMenu(away =>
            {
                selectedAway = away.Clone(TeamSide.Away);
                var commSelectMenu = CreateCommentatorSelectMenu(commentator =>
                {
                    var preMatchMenu = GamePlanMenuBuilder.CreatePreMatchMenu(selectedHome, _screenReader, PushMenu, PopMenu, onStartMatch: () =>
                    {
                        StartInteractiveMatch(selectedHome, selectedAway, cupFinal: cupFinal);
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
                // حفظ الاختيار حتى يبقى معتمداً في المباريات القادمة ولا ترجعه مزامنة الإعدادات
                MatchSettings.Current.Commentator = current.Id;
                MatchSettings.Current.CommentaryVoice = current.Id == CommentatorId.FaresAwad ? CommentaryVoiceOption.FaresAwad : CommentaryVoiceOption.RaoufKhlif;
                MatchSettings.Current.CommentaryEnabled = true;
                MatchSettings.Current.Save();
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
                            // نسخ مستقلة حتى لا ترث مباراة جديدة حالة مباراة سابقة من نفس الكائنات
                            StartInteractiveMatch(team1.Clone(TeamSide.Home), team2.Clone(TeamSide.Away));
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

    private void StartInteractiveMatch(Team homeTeam, Team awayTeam, bool cupFinal = false, bool rivalry = false)
    {
        _commentaryEngine.ClearPendingLines();
        _coordinator.CancelPendingGoalSequence();
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

        _stadiumSoundEngine.SetMatchTeams(homeTeam, awayTeam);
        _stadiumSoundEngine.ApplyStadiumAcousticsForTeam(homeTeam);
        _callnameManager.PreWarmSquads(homeTeam, awayTeam);

        // تشغيل صوت المدرجات الحقيقي المستمر وصافرة الحكم
        SoundEffectsPlayer.StartCrowdAmbiance();
        SoundEffectsPlayer.PlayWhistle();

        var match = new MatchEngine(homeTeam, awayTeam, _eventBus, _inputHandler) { Mode = cupFinal ? MatchMode.CupFinal : MatchMode.Friendly, RivalryScenario = rivalry };
        _activeMatch = match;
        match.OnAnalysisReplayReady += replay =>
        {
            if(!_commentaryEngine.QueuePlayReplay(replay)){match.CompleteAnalysisReplay();return;}
            _commentaryEngine.RunAfterRecordedCommentary(()=>
            {
                if(!IsDisposed && IsHandleCreated)BeginInvoke(()=>
                {
                    if(_activeMatch==match)match.CompleteAnalysisReplay();
                });
            });
        };

        match.OnNearBoundary += () =>
        {
            SoundEffectsPlayer.PlayBoundaryWarning();
        };

        // تفاعل صخب الجماهير التلقائي: يعلو مع اقتراب الهجمة ويهدأ في منتصف الملعب
        match.OnCrowdVolumeChanged += vol =>
        {
            SoundEffectsPlayer.SetCrowdVolume(vol);
        };

        bool introPlayed = _commentaryEngine.TryPlayMatchIntro(match.GetCommentarySnapshot().With(("crowd.active",SoundEffectsPlayer.IsCrowdActive)));
        if (!introPlayed)
        {
            string introSpeech = $"انطلاق المباراة! {homeTeam.Name} ضد {awayTeam.Name}، بصوت المعلق {_commentaryEngine.ActiveCommentator.DisplayName}. " +
                                 $"الكرة معك الآن مع {match.ActiveUserPlayer.Name} في دائرة منتصف الملعب.";
            _screenReader.Speak(introSpeech);
        }
        else
        {
            _commentaryEngine.RunAfterRecordedCommentary(() =>
                _screenReader.Speak($"{homeTeam.Name} ضد {awayTeam.Name}. الكرة معك الآن مع {match.ActiveUserPlayer.Name}.", interrupt: false));
        }

        _titleLabel.Text = $"{homeTeam.Name} ضد {awayTeam.Name}";
        _statusLabel.Text = $"النتيجة: {homeTeam.Score} - {awayTeam.Score} | المعلق: {_commentaryEngine.ActiveCommentator.DisplayName}";

        _lastFrameTime = DateTime.UtcNow;

        if (_activeMatch != null) _activeMatch.AutomaticGoalRestart = false;
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

                // فحص الأهازيج الدورية بحسب سياق اللعب
                _stadiumSoundEngine.HandleMatchContext(
                    new MatchEvent(MatchEventType.BallZoneChanged, "سياق اللعب", _activeMatch.CurrentZone),
                    _activeMatch.HomeTeam.Score,
                    _activeMatch.AwayTeam.Score,
                    _activeMatch.MatchTimeMinutes
                );
            }

            string matchStatus = $"المباراة جارية (الدقيقة {(int)_activeMatch.MatchTimeMinutes}): {_activeMatch.HomeTeam.Name} {_activeMatch.HomeTeam.Score} - {_activeMatch.AwayTeam.Score} {_activeMatch.AwayTeam.Name}";
            _statusLabel.Text=matchStatus;
            // أثناء اللعب النشط: نحدث _statusLabel المرئي فقط ونمنع تعديل _displayListBox لمنع تشويش قارئ الشاشة NVDA
            if (_currentMenu == null && _activeMatch.State != MatchState.InPlay)
            {
                if (_displayListBox.Items.Count > 0 && (string)_displayListBox.Items[0] != matchStatus)
                {
                    _displayListBox.Items[0] = matchStatus;
                }
            }

            if (_activeMatch.State == MatchState.HalfTime && _currentMenu == null)
            {
                _inputHandler.Reset();
                _gameLoopTimer?.Stop();
                SoundEffectsPlayer.PauseCrowdAmbiance();
                SoundEffectsPlayer.PlayWhistle();

                if (MatchSettings.Current.AutoSubstitutionsEnabled &&
                    TacticalAiAdvisor.TryPerformAutoSubstitution(_activeMatch, out string autoSubMsg))
                {
                    _commentaryEngine.RunAfterRecordedCommentaryAsync(()=>_screenReader.SpeakUntilFinishedAsync(autoSubMsg));
                }

                if (MatchSettings.Current.AutoTacticalAdjustmentsEnabled &&
                    TacticalAiAdvisor.TryPerformDynamicTacticalAdjustment(_activeMatch, _activeMatch.HomeTeam, out string autoTacticMsg))
                {
                    var coachMatch=_activeMatch;
                    _commentaryEngine.RunAfterRecordedCommentaryAsync(async () =>
                    {
                        await _screenReader.SpeakUntilFinishedAsync(autoTacticMsg);
                        if(_activeMatch==coachMatch)
                            _commentaryEngine.TryCommentOnEvent(new MatchEvent(MatchEventType.CoachInstruction,"كلمة المدرب للاعبين")
                            {Context=coachMatch.GetCommentarySnapshot().With(("analysis.coachInstructionSpoken",true))},force:true);
                    });
                }

                // الذكاء الاصطناعي للخصم يقوم دائماً بالتعديل التكتيكي الذكي بين الشوطين
                TacticalAiAdvisor.TryPerformDynamicTacticalAdjustment(_activeMatch, _activeMatch.AwayTeam, out _, forceAi: true);

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
                        SoundEffectsPlayer.ResumeCrowdAmbiance();
                        SoundEffectsPlayer.PlayWhistle();
                        _commentaryEngine.RunAfterRecordedCommentary(() => _screenReader.Speak(_activeMatch?.IsExtraTime == true ? "انطلاق الشوط الإضافي الثاني" : "صافرة انطلاق الشوط الثاني!"));
                        _lastFrameTime = DateTime.UtcNow;
                        _gameLoopTimer?.Start();
                    },
                    onExit: () =>
                    {
                        EndMatchAndReturnToMenu();
                    });

                var pausedMatch = _activeMatch;
                _commentaryEngine.RunAfterRecordedCommentary(() =>
                {
                    if (!IsDisposed && IsHandleCreated) BeginInvoke(() =>
                    {
                        if (_activeMatch == pausedMatch && pausedMatch.State == MatchState.HalfTime) PushMenu(halfTimeMenu);
                    });
                });
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
        // The existing shootout-start recording must finish before menus or official speech.

        int userPenaltiesScored = 0;
        int cpuPenaltiesScored = 0;
        int userKicksTaken = 0;
        int cpuKicksTaken = 0;
        bool suddenDeathAnnounced = false;
        var readyContext = AccessibleFootball2026.Core.Commentary.CommentaryContext.Empty;

        void AfterCommentary(Action action)
        {
            _commentaryEngine.RunAfterRecordedCommentary(() =>
            {
                if (!IsDisposed && IsHandleCreated) BeginInvoke(() =>
                {
                    if (_activeMatch == match) action();
                });
            });
        }

        void Ready(bool home)
        {
            readyContext = AccessibleFootball2026.Core.Commentary.ShootoutCommentary.BeforeKick(
                match.GetCommentarySnapshot(), home, userPenaltiesScored, cpuPenaltiesScored, userKicksTaken, cpuKicksTaken);
            _commentaryEngine.TryCommentOnEvent(new MatchEvent(MatchEventType.ShootoutKickReady, "الاستعداد لركلة الترجيح") { Context = readyContext }, force: true);
        }

        void Outcome(bool home, bool scored, bool saved, string announcement, Action next, ShootoutKickResult result)
        {
            var context = AccessibleFootball2026.Core.Commentary.ShootoutCommentary.AfterKick(readyContext, home, scored, saved,
                userPenaltiesScored, cpuPenaltiesScored, userKicksTaken, cpuKicksTaken,result.RunUp,result.KeeperHeld);
            var type = scored ? MatchEventType.ShootoutGoal : saved ? MatchEventType.ShootoutSave : MatchEventType.ShootoutMiss;
            _commentaryEngine.TryCommentOnEvent(new MatchEvent(type, announcement) { Context = context }, force: true);
            _commentaryEngine.RunAfterRecordedCommentaryAsync(async () =>
            {
                await _screenReader.SpeakUntilFinishedAsync(announcement);
                if (!IsDisposed && IsHandleCreated) BeginInvoke(() => { if (_activeMatch == match) next(); });
            });
        }

        // قواعد الحسم موثقة ومحمولة في PenaltyShootoutRules (IFAB Law 10) مع تغطية اختبارات انحدار
        bool IsShootoutDecided() => PenaltyShootoutRules.IsDecided(userPenaltiesScored, cpuPenaltiesScored, userKicksTaken, cpuKicksTaken);

        void PlayUserTurn()
        {
            if (IsShootoutDecided())
            {
                FinishShootout(match, userPenaltiesScored, cpuPenaltiesScored);
                return;
            }

            if (userKicksTaken >= 5 && !suddenDeathAnnounced)
            {
                suddenDeathAnnounced = true;
                _screenReader.Speak("تعادل الفريقان بعد 5 ركلات! ننتقل للموت المفاجئ: ركلة مقابل ركلة.");
            }

            var shotMenu = new AccessibleMenu($"ركلات الترجيح - الركلة رقم {userKicksTaken + 1} لفريقك ({userPenaltiesScored} - {cpuPenaltiesScored}) - اختر زاوية التسديد", _screenReader);
            shotMenu.AddItem("التسديد في أقصى الزاوية اليمنى", () => ExecuteUserPenaltyShot(0));
            shotMenu.AddItem("التسديد بقوة في منتصف المرمى", () => ExecuteUserPenaltyShot(1));
            shotMenu.AddItem("التسديد في أقصى الزاوية اليسرى", () => ExecuteUserPenaltyShot(2));
            shotMenu.AddItem("اقتراب بطيء والتسديد يميناً", () => ExecuteUserPenaltyShot(0,true));
            shotMenu.AddItem("اقتراب بطيء والتسديد في المنتصف", () => ExecuteUserPenaltyShot(1,true));
            shotMenu.AddItem("اقتراب بطيء والتسديد يساراً", () => ExecuteUserPenaltyShot(2,true));
            Ready(true);
            AfterCommentary(() => PushMenu(shotMenu));
        }

        async void ExecuteUserPenaltyShot(int angle,bool slow=false)
        {
            PopMenu();
            _inputHandler.Reset();
            if(slow)await Task.Delay(1200);
            if(_activeMatch!=match)return;
            var takers=match.HomeTeam.Players.Where(p=>p.IsActiveOnPitch).ToArray();
            var taker=takers[userKicksTaken%takers.Length];
            var keeper=match.AwayTeam.Players.FirstOrDefault(p=>p.IsActiveOnPitch&&p.Role==PlayerRole.Goalkeeper);
            var result=ShootoutKick.Resolve(angle,Random.Shared.Next(3),taker.Shooting,keeper?.Defending??50,slow,Random.Shared.NextDouble());
            SoundEffectsPlayer.PlayKick(angle==0?.8f:angle==2?-.8f:0);
            await Task.Delay(TimeSpan.FromSeconds(result.FlightSeconds));
            if(_activeMatch!=match)return;
            userKicksTaken++;
            if(result.Scored){userPenaltiesScored++;SoundEffectsPlayer.PlayGoalNet();SoundEffectsPlayer.PlayCrowdGoal();}
            else SoundEffectsPlayer.PlayCrowdMiss();
            string description=result.Scored?"هدف في الشباك":result.KeeperHeld?"الحارس يمسك الركلة":result.Saved?"الحارس يبعد الركلة":"التسديدة خارج المرمى";
            Outcome(true,result.Scored,result.Saved,$"{description}. النتيجة الآن: فريقك {userPenaltiesScored}، والخصم {cpuPenaltiesScored}.",PlayCpuTurn,result);
        }

        void PlayCpuTurn()
        {
            if (IsShootoutDecided())
            {
                FinishShootout(match, userPenaltiesScored, cpuPenaltiesScored);
                return;
            }

            var diveMenu = new AccessibleMenu($"ركلات الترجيح - الركلة رقم {cpuKicksTaken + 1} للخصم ({userPenaltiesScored} - {cpuPenaltiesScored}) - اختر ارتماء حارسك", _screenReader);
            diveMenu.AddItem("الارتماء نحو الزاوية اليمنى", () => ExecuteCpuPenaltyShot(0));
            diveMenu.AddItem("الثبات في منتصف المرمى", () => ExecuteCpuPenaltyShot(1));
            diveMenu.AddItem("الارتماء نحو الزاوية اليسرى", () => ExecuteCpuPenaltyShot(2));
            Ready(false);
            AfterCommentary(() => PushMenu(diveMenu));
        }

        async void ExecuteCpuPenaltyShot(int userDive)
        {
            PopMenu();
            _inputHandler.Reset();
            bool slow=Random.Shared.Next(4)==0;
            if(slow)await Task.Delay(1200);
            if(_activeMatch!=match)return;
            var takers=match.AwayTeam.Players.Where(p=>p.IsActiveOnPitch).ToArray();
            var taker=takers[cpuKicksTaken%takers.Length];
            var keeper=match.HomeTeam.Players.FirstOrDefault(p=>p.IsActiveOnPitch&&p.Role==PlayerRole.Goalkeeper);
            int angle=Random.Shared.Next(3);
            var result=ShootoutKick.Resolve(angle,userDive,taker.Shooting,keeper?.Defending??50,slow,Random.Shared.NextDouble());
            SoundEffectsPlayer.PlayKick(angle==0?.8f:angle==2?-.8f:0);
            await Task.Delay(TimeSpan.FromSeconds(result.FlightSeconds));
            if(_activeMatch!=match)return;
            cpuKicksTaken++;
            if(result.Scored){cpuPenaltiesScored++;SoundEffectsPlayer.PlayGoalNet();}
            else SoundEffectsPlayer.PlayCrowdCheer();
            string description=result.Scored?"هدف لصالح الخصم":result.KeeperHeld?"حارسك يمسك ركلة الخصم":result.Saved?"حارسك يبعد ركلة الخصم":"ركلة الخصم خارج المرمى";
            Outcome(false,result.Scored,result.Saved,$"{description}. النتيجة الآن: فريقك {userPenaltiesScored}، والخصم {cpuPenaltiesScored}.",PlayUserTurn,result);
        }

        void FinishShootout(MatchEngine m, int uScore, int cScore)
        {
            m.CompleteShootout(uScore,cScore);
            var finalContext = m.GetCommentarySnapshot().With(("shootout.homeScore", uScore), ("shootout.awayScore", cScore),
                ("shootout.decided", true), ("shootout.winner", uScore > cScore ? "home" : "away"));
            _commentaryEngine.TryCommentOnEvent(new MatchEvent(MatchEventType.ShootoutFinished, "نهاية ركلات الترجيح") { Context = finalContext }, force: true);
            AfterCommentary(() =>
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
            });
        }

        _commentaryEngine.RunAfterRecordedCommentaryAsync(async () =>
        {
            await _screenReader.SpeakUntilFinishedAsync("انطلاق ركلات الترجيح الحاسمة! 5 ركلات لكل فريق.");
            if (!IsDisposed && IsHandleCreated) BeginInvoke(() => { if (_activeMatch == match) PlayUserTurn(); });
        });
    }

    private void EndMatchAndReturnToMenu(bool waitForCommentary = true)
    {
        _gameLoopTimer?.Stop();
        _gameLoopTimer?.Dispose();
        _gameLoopTimer = null;
        _inputHandler.Reset();
        if (waitForCommentary && _activeMatch is { State: MatchState.GameOver } ending)
        {
            if (ending.WinningGoalReplay() is { } winningReplay) _commentaryEngine.QueueGoalReplay(winningReplay);
            _commentaryEngine.RunAfterRecordedCommentary(() =>
            {
                if (!IsDisposed && IsHandleCreated) BeginInvoke(() =>
                {
                    if (_activeMatch == ending) EndMatchAndReturnToMenu(false);
                });
            });
            return;
        }
        _coordinator.CancelPendingGoalSequence();
        _commentaryEngine.ClearPendingLines();

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
            // نشيد دوري الأبطال لا يتوقف إلا بعد ضغط Enter أو Space حصراً
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                _isInStartupIntro = false;
                _anthemEngine.StopAllSounds();
                SoundEffectsPlayer.StartMenuMusic();
                OpenMainMenu();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
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
                    bool wasHalfTime = _activeMatch.State == MatchState.HalfTime;
                    _currentMenu = null;
                    _inputHandler.Reset();
                    SetupMatchDisplayListBox(_activeMatch.HomeTeam, _activeMatch.AwayTeam);
                    _activeMatch.ResumeMatch();
                    SoundEffectsPlayer.ResumeCrowdAmbiance();
                    if (wasHalfTime)
                    {
                        SoundEffectsPlayer.PlayWhistle();
                        _commentaryEngine.RunAfterRecordedCommentary(() => _screenReader.Speak(_activeMatch?.IsExtraTime == true ? "انطلاق الشوط الإضافي الثاني" : "صافرة انطلاق الشوط الثاني!"));
                    }
                    else
                    {
                        _screenReader.Speak("استئناف اللقاء");
                    }
                    _lastFrameTime = DateTime.UtcNow;
                    _gameLoopTimer?.Start();
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
            if (e.KeyCode == Keys.Space)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                string ballStatus = _activeMatch.Ball.Owner != null
                    ? $"الكرة مع {_activeMatch.Ball.Owner.Name} ({_activeMatch.Ball.Owner.CurrentAssignedPosition})"
                    : (_activeMatch.IsPassInTransit ? "الكرة في تمريرة" : "الكرة حرة");

                string zoneStatus = _activeMatch.CurrentZone.ToArabicAnnouncement();

                string matchInfo = $"{ballStatus}. {zoneStatus}. الدقيقة {(int)_activeMatch.MatchTimeMinutes}. النتيجة: {_activeMatch.HomeTeam.Name} {_activeMatch.HomeTeam.Score}، {_activeMatch.AwayTeam.Name} {_activeMatch.AwayTeam.Score}.";
                _screenReader.Speak(matchInfo, interrupt: true);
                return;
            }

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
        Keys.D or Keys.W or Keys.S or Keys.A or Keys.H or Keys.J or Keys.V or Keys.L or Keys.F or Keys.K or Keys.B or Keys.N or Keys.T or Keys.R or Keys.Q => true,
        Keys.Space or Keys.C => true,
        _ => false
    };
}


