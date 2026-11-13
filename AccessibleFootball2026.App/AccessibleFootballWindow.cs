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
    private static string T(string ar, string en) => LocalizationService.Text(ar, en);

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
    private bool _isInMatchAnthems;
    private bool _skipMatchAnthems;

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
            Text = T("جاري التحميل...", "Loading"),
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

    private bool _replayDismissRequested;

    private void SetupAudioEvents()
    {
        // ربط الخفض الصوتي لصوت الجماهير عند تحدث قارئ الشاشة أو المعلق
        _coordinator.OnCrowdVolumeDucking += factor =>
        {
            SoundEffectsPlayer.SetCrowdDucking(factor);
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
            // خروج لاعب قدّم مباراة كبيرة: يبدأ تصفيق الجمهور الحقيقي أولاً، ثم يُروى التعليق بعده.
            // إن تعذر تشغيل التسجيل الحقيقي يبقى applause.played=false فيُغلق الشرط ولا يُدَّعى تصفيق لم يحدث.
            if (evt.Type == MatchEventType.SubstitutionMade &&
                evt.Context.TryGet("substitution.playerRating", out var ovationRating) &&
                double.TryParse(ovationRating, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var rating) && rating >= 8)
                evt.Context = evt.Context.With(("applause.played", SoundEffectsPlayer.PlayCrowdApplause()));
            _commentaryEngine.TryCommentOnEvent(evt,force:evt.Type is MatchEventType.CoachPraise or MatchEventType.CoachWideInstruction);
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
                    lock (_commentaryEngine.SpeechGate)
                    {
                        if (_commentaryEngine.IsIdle && (_screenReader is not PriorityScreenReader voice || !voice.IsSpeaking))
                            _callnameManager.TryPlayPlayerCallname(evt.SubjectPlayer);
                    }
                }
            }
        };

        _coordinator.OnGoalSequenceCompleted += () =>
        {
            if (!IsDisposed && IsHandleCreated) BeginInvoke(() =>
            {
                if (_activeMatch is { } goalMatch && _commentaryEngine.ActiveCommentator.Id == CommentatorId.FaresAwad)
                    goalMatch.RequestCoachPraiseForLastTeamGoal();
                // Completion enables Enter; it never restarts the match on its own.
            });
        };

        _commentaryEngine.OnCommentarySpoken += (commName, phrase) =>
        {
            _screenReader.Speak($"{commName}: {phrase}", interrupt: false);
        };

        // بدء مقطع مسجل يسكت قارئ الشاشة فوراً حتى لا يتداخل الصوتان

        FormClosed += (_, _) =>
        {
            _skipMatchAnthems = true;
            _anthemEngine.StopAllSounds();
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
        _displayListBox.Items.Add(T($"المباراة جارية الآن: {homeTeam.Name} ضد {awayTeam.Name}", $"Match: {homeTeam.Name} versus {awayTeam.Name}"));
        _displayListBox.Items.Add(T("التحكم: S للتمرير | D للرفع والعرضيات | W للتسديد | A للبينية | Shift للجري | Ctrl لتبديل المدافع | Space للرادار", "S pass | D cross | W shoot | A through ball | Shift sprint | Ctrl switch | Space radar"));
        _displayListBox.Items.Add(T("Q بينية عالية | K إبعاد | B عرضية أرضية | N القائم البعيد | T التشكيلة | R اقتراب بطيء ثم تسديد", "Q lofted through ball | K clear | B low cross | N far post | T lineup | R slow run-up"));
        _displayListBox.Items.Add(T("التسديد: H رأسية | J مقصية | V على الطائر | L ساقطة | F مقوسة", "H header | J bicycle kick | V volley | L chip | F curved shot"));
        _displayListBox.Items.Add(T("الدفاع: Ctrl لتبديل المدافع | S للاستخلاص الهادئ | W/D/A للاستخلاص القوي", "Defence: Ctrl switch | S standing tackle | W/D/A slide tackle"));
        _displayListBox.Items.Add(T("التكتيك: Space أو C للرادار التكتيكي | Esc للإيقاف المؤقت | الانسحاب من قائمة الإيقاف", "Space or C radar | Esc pause | End match in pause menu"));
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
        _currentMenu?.Close();
        if (_menuHistory.Count > 0)
        {
            var prev = _menuHistory.Pop();
            SetActiveMenu(prev);
            if (_menuHistory.Count == 0 && !_isInStartupIntro && _activeMatch == null)
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
        _gameLoopTimer?.Stop();
        GoalBeaconPlayer.Stop();
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

        menu.AddItem(T("نهائي كأس مخصص", "Custom cup final"), () => StartMatchWizardFlow(cupFinal: true));

        menu.AddItem(T("الكلاسيكو", "El Clasico"), () =>
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

        menu.AddItem(T("مكتبة تسجيلات فارس كاملة", "Fares recording library"),()=>PushMenu(CommentaryLibraryMenuBuilder.Create(_commentaryEngine,_screenReader,PushMenu,PopMenu)));

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
        var teamMenu = new AccessibleMenu(T("اختر فريقك في الكلاسيكو", "El Clasico team"), _screenReader);

        teamMenu.AddItem(T("ريال مدريد (أنت تلعب بريال مدريد ضد برشلونة)", "Real Madrid"), () =>
        {
            var rma = GetClasicoRealMadrid(TeamSide.Home);
            var bar = GetClasicoBarcelona(TeamSide.Away);
            ChooseStadiumFlow(rma, bar);
        });

        teamMenu.AddItem(T("برشلونة (أنت تلعب ببرشلونة ضد ريال مدريد)", "Barcelona"), () =>
        {
            var bar = GetClasicoBarcelona(TeamSide.Home);
            var rma = GetClasicoRealMadrid(TeamSide.Away);
            ChooseStadiumFlow(bar, rma);
        });

        teamMenu.AddItem(T("رجوع للقائمة الرئيسية", "Back to main menu"), () => PopMenu());
        PushMenu(teamMenu);
    }

    private void ChooseStadiumFlow(Team home, Team away, bool cupFinal = false, bool rivalry = true)
    {
        var stadiumMenu = new AccessibleMenu(T("اختر الملعب", "Select stadium"), _screenReader);
        void SelectGround(StadiumDefinition selected)
        {
            _stadiumSoundEngine.ActiveStadiumKey = selected.Key;
            _screenReader.Speak(T("تم اختيار ملعب ", "Selected stadium: ") + selected.DisplayName);
            StartInteractiveMatch(home, away, cupFinal, rivalry, stadiumSelected: true);
        }
        // Relevant grounds are one action away. Other grounds remain available as neutral venues,
        // grouped by the actual database's competitions instead of one very long spoken list.
        foreach (var stadium in StadiumCatalog.GetAvailableStadiums().Where(s => s.IsGroundFor(home) || s.IsGroundFor(away)))
        {
            var selected = stadium;
            stadiumMenu.AddItem(selected.DisplayName, () => SelectGround(selected));
        }
        stadiumMenu.AddItem(T("اختيار ملعب آخر", "Choose another stadium"), () =>
        {
            var otherMenu = new AccessibleMenu(T("الملاعب الأخرى", "Other stadiums"), _screenReader);
        foreach (var group in JsonDatabaseProvider.GetAllGroups())
        {
            var grounds = StadiumCatalog.GetGroundsForProductionGroup(group).Where(s => !s.IsGroundFor(home) && !s.IsGroundFor(away)).ToList();
            if (grounds.Count == 0) continue;
            string groupName = T(JsonDatabaseProvider.GetGroupArabicName(group), group);
            otherMenu.AddItem(groupName, () =>
            {
                var groupMenu = new AccessibleMenu(groupName, _screenReader);
                foreach (var ground in grounds)
                {
                    var selected = ground;
                    groupMenu.AddItem(selected.DisplayName, () => SelectGround(selected));
                }
                groupMenu.AddItem(T("رجوع", "Back"), () => PopMenu());
                PushMenu(groupMenu);
            });
        }
        otherMenu.AddItem(T("ملعب محايد", "Neutral stadium"), () =>
        {
            _stadiumSoundEngine.ActiveStadiumKey = "NeutralStadium";
            _screenReader.Speak(T("تم اختيار ملعب محايد. صوت الجماهير محايد.", "Neutral stadium selected. Neutral crowd."));
            StartInteractiveMatch(home, away, cupFinal, rivalry, stadiumSelected: true);
        });
            otherMenu.AddItem(T("رجوع", "Back"), () => PopMenu());
            PushMenu(otherMenu);
        });
        stadiumMenu.AddItem(T("رجوع لاختيار الفريق", "Back to team selection"), () => PopMenu());
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
        var menu = new AccessibleMenu(T($"اختر النادي رقم {clubNumber} من 4 لتشكيلة فريقك", $"Your club {clubNumber}/4"), _screenReader);
        var availableClubs = allClubs.Where(c => !builder.UserSourceClubs.Take(clubNumber - 1).Any(u => u.Id == c.Id)).ToList();

        foreach (var c in availableClubs)
        {
            var club = c;
            menu.AddItem(club.Name, () =>
            {
                if (!builder.SelectSourceClubAt(false, clubNumber - 1, club))
                {
                    _screenReader.Speak(T("تعذر اختيار هذا النادي. راجع الاختيارات السابقة.", "Cannot select this club. Check the previous choices."));
                    return;
                }
                _screenReader.Speak(T($"تم اختيار {club.Name} لفريقك ({clubNumber} من 4).", $"Your club {clubNumber}/4: {club.Name}"));

                if (clubNumber < 4)
                {
                    ChooseClubsForUserTeam(builder, allClubs, clubNumber + 1);
                }
                else
                {
                    _screenReader.Speak(T("تم اختيار الأندية الأربعة لفريقك بنجاح! الآن اختر 4 أندية لفريق الكمبيوتر الخصم.", "Your four clubs selected. Opponent clubs next."));
                    ChooseClubsForCpuTeam(builder, allClubs, 1);
                }
            });
        }

        menu.AddItem(T("رجوع للقائمة السابقة", "Back"), () => PopMenu());
        PushMenu(menu);
    }

    private void ChooseClubsForCpuTeam(DreamTeamBuilder builder, List<Team> allClubs, int clubNumber)
    {
        var menu = new AccessibleMenu(T($"اختر النادي رقم {clubNumber} من 4 لتشكيلة الكمبيوتر (الخصم)", $"Opponent club {clubNumber}/4"), _screenReader);
        var availableClubs = allClubs.Where(c => !builder.CpuSourceClubs.Take(clubNumber - 1).Any(cpu => cpu.Id == c.Id)).ToList();

        foreach (var c in availableClubs)
        {
            var club = c;
            menu.AddItem(club.Name, () =>
            {
                if (!builder.SelectSourceClubAt(true, clubNumber - 1, club))
                {
                    _screenReader.Speak(T("تعذر اختيار هذا النادي. راجع الاختيارات السابقة.", "Cannot select this club. Check the previous choices."));
                    return;
                }
                _screenReader.Speak(T($"تم اختيار {club.Name} للكمبيوتر ({clubNumber} من 4).", $"Opponent club {clubNumber}/4: {club.Name}"));

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

        menu.AddItem(T("رجوع", "Back"), () => PopMenu());
        PushMenu(menu);
    }

    private void ExecuteDraftAndShowPreMatch(DreamTeamBuilder builder)
    {
        builder.DraftBothTeams();

        string userClubsNames = string.Join("، ", builder.UserSourceClubs.Select(c => c.Name));
        string cpuClubsNames = string.Join("، ", builder.CpuSourceClubs.Select(c => c.Name));

        string stars = string.Join("، ", builder.UserDreamTeam.Players.Take(4).Select(p => $"{p.Name} ({p.CurrentAssignedPosition})"));
        string cpuStars = string.Join("، ", builder.CpuDreamTeam.Players.Take(4).Select(p => $"{p.Name} ({p.CurrentAssignedPosition})"));

        string msg = T($"فريقك: {userClubsNames}. اللاعبون: {stars}. الخصم: {cpuClubsNames}. اللاعبون: {cpuStars}. لا يوجد لاعب مكرر بين الفريقين.",
            $"Your clubs: {userClubsNames}. Players: {stars}. Opponent clubs: {cpuClubsNames}. Players: {cpuStars}. No player appears on both teams.");
        _screenReader.Speak(msg);

        ShowDraftPreMatchMenu(builder);
    }

    private void ShowDraftPreMatchMenu(DreamTeamBuilder builder)
    {
        var menu = new AccessibleMenu(T("خيارات وضع الـ 4 فرق", "Four-club draft"), _screenReader);

        menu.AddItem(T("بدء المباراة", "Start match"), () =>
        {
            var commMenu = CreateCommentatorSelectMenu(_ =>
            {
                StartInteractiveMatch(builder.UserDreamTeam, builder.CpuDreamTeam);
            });
            PushMenu(commMenu);
        });

        menu.AddItem(T("التشكيلة الأساسية", "Starting lineup"), () =>
        {
            var lineup = GamePlanMenuBuilder.CreateLineupMenu(builder.UserDreamTeam, _screenReader, PushMenu, PopMenu);
            PushMenu(lineup);
        });

        menu.AddItem(T("دكة البدلاء", "Bench"), () =>
        {
            var bench = GamePlanMenuBuilder.CreateBenchMenu(builder.UserDreamTeam, _screenReader, PushMenu, PopMenu);
            PushMenu(bench);
        });

        menu.AddItem(T("أدوار المتخصصين", "Specialist roles"), () =>
        {
            var spec = GamePlanMenuBuilder.CreateSpecialistsMenu(builder.UserDreamTeam, _screenReader, PushMenu, PopMenu);
            PushMenu(spec);
        });

        menu.AddItem(T("تغيير الخطة التكتيكية", "Change formation"), () =>
        {
            var form = GamePlanMenuBuilder.CreateFormationMenu(builder.UserDreamTeam, _screenReader, PopMenu);
            PushMenu(form);
        });

        menu.AddItem(T("إعادة السحب العشوائي", "Redraw players"), () =>
        {
            builder.DraftBothTeams();
            _screenReader.Speak(T("تمت إعادة السحب العشوائي بواسطة الكمبيوتر لتشكيلتي الفريقين بنجاح!", "Both lineups redrawn"));
        });

        menu.AddItem(T("إعادة اختيار الأندية", "Choose clubs again"), () =>
        {
            StartDreamTeamBuilderFlow();
        });

        menu.AddItem(T("رجوع", "Back"), () =>
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
        var languageBefore = MatchSettings.Current.Language;
        using var settingsForm = new AccessibleSettingsForm(_screenReader);
        settingsForm.ShowDialog(this);
        UpdateWindowHeadings();
        if (languageBefore != MatchSettings.Current.Language) OpenMainMenu();
        else RefreshMenuDisplay();
    }

    private AccessibleMenu CreateCommentatorSelectMenu(Action<CommentatorProfile> onSelected)
    {
        var menu = new AccessibleMenu(T("اختر المعلق الصوتي المعتمد للمباراة", "Select commentator"), _screenReader);
        var list = _commentaryEngine.GetAvailableCommentators();

        foreach (var c in list)
        {
            var current = c;
            menu.AddItem(T($"{current.DisplayName}، {current.Region}", $"{current.DisplayName}, {current.Region}"), () =>
            {
                _commentaryEngine.SwitchCommentator(current.Id);
                // حفظ الاختيار حتى يبقى معتمداً في المباريات القادمة ولا ترجعه مزامنة الإعدادات
                MatchSettings.Current.Commentator = current.Id;
                MatchSettings.Current.CommentaryVoice = current.Id == CommentatorId.FaresAwad ? CommentaryVoiceOption.FaresAwad : CommentaryVoiceOption.RaoufKhlif;
                MatchSettings.Current.CommentaryEnabled = true;
                MatchSettings.Current.Save();
                _screenReader.Speak(T($"تم اعتماد المعلق {current.DisplayName}", $"Commentator: {current.DisplayName}"));
                onSelected(current);
            });
        }

        menu.AddItem(T("رجوع", "Back"), () => PopMenu());
        return menu;
    }

    private void StartTournamentsBrowserFlow()
    {
        var tournaments = TournamentDatabase.GetAllTournaments();
        var menu = new AccessibleMenu(T("البطولات والدوريات", "Competitions"), _screenReader);

        foreach (var t in tournaments)
        {
            var currentTourney = t;
            menu.AddItem(currentTourney.DisplayName, () =>
            {
                var tourneyMenu = new AccessibleMenu(currentTourney.DisplayName, _screenReader);

                tourneyMenu.AddItem(T("الفرق المشاركة", "Participating teams"), () =>
                {
                    string names = string.Join("، ", currentTourney.Teams.Select(tm => tm.Name));
                    _screenReader.Speak(T($"المشاركون في {currentTourney.DisplayName} هم: {names}", $"{currentTourney.DisplayName}: {names}"));
                });

                tourneyMenu.AddItem(T("اختيار مباراة", "Choose matchup"), () =>
                {
                    var homeMenu = new AccessibleMenu(T("اختر فريقك", "Your team"), _screenReader);
                    foreach (var availableHome in currentTourney.Teams)
                    {
                        var team1 = availableHome;
                        homeMenu.AddItem(team1.Name, () =>
                        {
                            var awayMenu = new AccessibleMenu(T("اختر الخصم", "Opponent"), _screenReader);
                            foreach (var availableAway in currentTourney.Teams.Where(t => t.Id != team1.Id))
                            {
                                var team2 = availableAway;
                                awayMenu.AddItem(team2.Name, () => PushMenu(CreateCommentatorSelectMenu(_ =>
                                    StartInteractiveMatch(team1.Clone(TeamSide.Home), team2.Clone(TeamSide.Away)))));
                            }
                            awayMenu.AddItem(T("رجوع", "Back"), PopMenu);
                            PushMenu(awayMenu);
                        });
                    }
                    homeMenu.AddItem(T("رجوع", "Back"), PopMenu);
                    PushMenu(homeMenu);
                });

                tourneyMenu.AddItem(T("رجوع", "Back"), () => PopMenu());
                PushMenu(tourneyMenu);
            });
        }

        menu.AddItem(T("رجوع", "Back"), () => PopMenu());
        PushMenu(menu);
    }

    private void StartGamePlanFlow()
    {
        var teamManager = new TeamSelectionManager(_screenReader, PushMenu, PopMenu);
        var menu = teamManager.CreateHomeTeamSelectMenu(team =>
        {
            var planMenu = GamePlanMenuBuilder.CreatePreMatchMenu(team, _screenReader, PushMenu, PopMenu, onStartMatch: () =>
            {
                PopMenu();
            }, startMatch: false);
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
        string guide = T("دليل التحكم الكامل لأزرار اللعب: " +
                       "الأسهم للتحرك مع الكرة في الاتجاهات الأربعة وضبط زاوية التسديد نحو المرمى. " +
                       "زر Shift الأيسر للجري السريع والانطلاق في المساحات. " +
                       "زر S للتمرير الأرضي المباشر لأقرب زميل. " +
                       "زر D للرفع أو التمريرة العالية والعرضية إلى داخل منطقة الجزاء. " +
                       "زر W للتسديد القوي والمباشر نحو المرمى. " +
                       "زر A للتمريرة البينية في المساحة. " +
                       "زر المسافة أو C لتشغيل الرادار التكتيكي وسماع مكان الكرة والمسافة نحو المرمى. " +
                       "زر Escape لفتح قائمة الإيقاف المؤقت والتبديلات وتعديل التكتيك. " +
                       "زر Q للتمريرة البينية العالية. للانسحاب اختر إنهاء المباراة من قائمة الإيقاف المؤقت.", "Arrows move and aim. Shift sprints. S passes, D crosses, W shoots, A through ball, Q lofted through ball. Space or C radar. Escape pauses for tactics and substitutions.");
        _screenReader.Speak(guide);
    }

    // =========================================================================
    // محرك المباراة التفاعلي (Interactive Real-Time Match Engine)
    // =========================================================================

    private bool ShowPendingInjuryMenu()
    {
        if (_activeMatch?.PendingInjuryPlayer == null) return false;
        var match = _activeMatch;
        _gameLoopTimer?.Stop();
        _replayDismissRequested = false;
        _inputHandler.Reset();
        GoalBeaconPlayer.Stop();
        SoundEffectsPlayer.PauseCrowdAmbiance();
        _menuHistory.Clear();
        SetActiveMenu(GamePlanMenuBuilder.CreateInjurySubstitutionMenu(match, _screenReader, () =>
        {
            _inputHandler.Reset();
            _currentMenu = null;
            if (ShowPendingInjuryMenu()) return;
            SetupMatchDisplayListBox(match.HomeTeam, match.AwayTeam);
            if (match.State == MatchState.InPlay) SoundEffectsPlayer.ResumeCrowdAmbiance();
            _lastFrameTime = DateTime.UtcNow;
            _gameLoopTimer?.Start();
            // A held free kick/penalty/replay still requires its separate Enter confirmation.
        }));
        return true;
    }

    private async void StartInteractiveMatch(Team homeTeam, Team awayTeam, bool cupFinal = false, bool rivalry = false, bool stadiumSelected = false)
    {
        if (_isInMatchAnthems) return;
        string? lineupIssue = homeTeam.GetMatchStartIssue() ?? awayTeam.GetMatchStartIssue();
        if (lineupIssue != null)
        {
            // Keep the current selection/plan menu and describe the data gap; never invent missing players.
            _screenReader.Speak(lineupIssue);
            return;
        }
        if (!stadiumSelected)
        {
            ChooseStadiumFlow(homeTeam, awayTeam, cupFinal, rivalry);
            return;
        }
        _gameLoopTimer?.Stop();
        GoalBeaconPlayer.Stop();
        _commentaryEngine.ClearPendingLines();
        _coordinator.CancelPendingGoalSequence();
        _currentMenu = null;
        _menuHistory.Clear();
        _replayDismissRequested = false;
        _inputHandler.Reset();
        SetupMatchDisplayListBox(homeTeam, awayTeam);

        SoundEffectsPlayer.StopMenuMusic();

        // مزامنة المعلق المعتمد مع الإعدادات
        _commentaryEngine.SwitchCommentator(MatchSettings.Current.Commentator);

        if (homeTeam.IsNationalTeam || awayTeam.IsNationalTeam)
        {
            _isInMatchAnthems = true;
            _skipMatchAnthems = false;
            _screenReader.CancelSpeech();
            _statusLabel.Text = T("السلام الوطني. Enter أو Space للتخطي.", "National anthems. Enter or Space to skip.");
            try { await _anthemEngine.PlayMatchNationalAnthemsAsync(homeTeam, awayTeam, () => _skipMatchAnthems || IsDisposed); }
            finally { _isInMatchAnthems = false; _inputHandler.Reset(); }
            if (IsDisposed) return;
        }

        _stadiumSoundEngine.SetMatchTeams(homeTeam, awayTeam);
        _callnameManager.PreWarmSquads(homeTeam, awayTeam);

        // تشغيل صوت المدرجات الحقيقي المستمر وصافرة الحكم
        SoundEffectsPlayer.StartCrowdAmbiance();
        SoundEffectsPlayer.PlayWhistle();

        var match = new MatchEngine(homeTeam, awayTeam, _eventBus, _inputHandler) { Mode = cupFinal ? MatchMode.CupFinal : MatchMode.Friendly, RivalryScenario = rivalry };
        _activeMatch = match;
        match.OnAnalysisReplayReady += replay =>
        {
            _commentaryEngine.QueuePlayReplay(replay);
            _screenReader.Speak(T("إعادة اللقطة.", "Replay"));
            // Even when a replay sound is missing, keep the incident held for Enter.
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

        if(_commentaryEngine.ActiveCommentator.Id==CommentatorId.FaresAwad)match.RequestCoachWideInstruction(match.HomeTeam);
        bool introPlayed = _commentaryEngine.TryPlayMatchIntro(match.GetCommentarySnapshot().With(("crowd.active",SoundEffectsPlayer.IsCrowdActive)));
        if (!introPlayed)
        {
            string introSpeech = T($"{homeTeam.Name} ضد {awayTeam.Name}. الكرة مع {match.ActiveUserPlayer.Name}.",
                $"{homeTeam.Name} versus {awayTeam.Name}. {match.ActiveUserPlayer.Name} has the ball.");
            _screenReader.Speak(introSpeech);
        }
        else
        {
            _commentaryEngine.RunAfterRecordedCommentary(() =>
                _screenReader.Speak(T($"{homeTeam.Name} ضد {awayTeam.Name}. الكرة معك الآن مع {match.ActiveUserPlayer.Name}.", $"{homeTeam.Name} versus {awayTeam.Name}. {match.ActiveUserPlayer.Name} has the ball."), interrupt: false));
        }

        _titleLabel.Text = T($"{homeTeam.Name} ضد {awayTeam.Name}", $"{homeTeam.Name} versus {awayTeam.Name}");
        _statusLabel.Text = T($"النتيجة: {homeTeam.Score} - {awayTeam.Score} | المعلق: {_commentaryEngine.ActiveCommentator.DisplayName}", $"Score: {homeTeam.Score} - {awayTeam.Score} | Commentator: {_commentaryEngine.ActiveCommentator.DisplayName}");

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

            bool readerDone = _screenReader is not PriorityScreenReader priority || !priority.HasPendingSpeech;
            if (_replayDismissRequested && _commentaryEngine.IsIdle && readerDone && !_coordinator.HasPendingGoalAnnouncement)
            {
                if (_activeMatch.DismissStoppageReplay())
                {
                    _replayDismissRequested = false;
                    _inputHandler.Reset();
                    SoundEffectsPlayer.PlayWhistle();
                }
            }
            _activeMatch.Update(dt);
            if (ShowPendingInjuryMenu()) return;

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

            string matchStatus = T($"المباراة جارية (الدقيقة {(int)_activeMatch.MatchTimeMinutes}): {_activeMatch.HomeTeam.Name} {_activeMatch.HomeTeam.Score} - {_activeMatch.AwayTeam.Score} {_activeMatch.AwayTeam.Name}", $"Minute {(int)_activeMatch.MatchTimeMinutes}: {_activeMatch.HomeTeam.Name} {_activeMatch.HomeTeam.Score} - {_activeMatch.AwayTeam.Score} {_activeMatch.AwayTeam.Name}");
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

                string nextPeriodAnnouncement = _activeMatch.IsExtraTimeStartInterval ? T("انطلاق الشوط الإضافي الأول", "First extra-time period") : _activeMatch.IsExtraTime ? T("انطلاق الشوط الإضافي الثاني", "Second extra-time period") : T("صافرة انطلاق الشوط الثاني!", "Second half");
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
                        _commentaryEngine.RunAfterRecordedCommentary(() => _screenReader.Speak(nextPeriodAnnouncement));
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
        _currentMenu = null;
        _menuHistory.Clear();
        // The existing shootout-start recording must finish before menus or official speech.

        int userPenaltiesScored = 0;
        int cpuPenaltiesScored = 0;
        int userKicksTaken = 0;
        int cpuKicksTaken = 0;
        bool suddenDeathAnnounced = false;
        var homeEligible = new ShootoutRoster(match.HomeTeam);
        var awayEligible = new ShootoutRoster(match.AwayTeam);
        int eligibleCount = Math.Min(homeEligible.Players.Count, awayEligible.Players.Count);
        awayEligible.ReduceCpuTo(eligibleCount);
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
                _screenReader.Speak(T("تعادل الفريقان بعد 5 ركلات! ننتقل للموت المفاجئ: ركلة مقابل ركلة.", "Level after five penalties. Sudden death."));
            }

            var shotMenu = new AccessibleMenu(T($"ركلات الترجيح - الركلة رقم {userKicksTaken + 1} لفريقك ({userPenaltiesScored} - {cpuPenaltiesScored}) - اختر زاوية التسديد", $"Your penalty {userKicksTaken + 1}. Score {userPenaltiesScored} - {cpuPenaltiesScored}. Shot direction"), _screenReader);
            shotMenu.AddItem(T("التسديد في أقصى الزاوية اليمنى", "Shoot right"), () => ExecuteUserPenaltyShot(0));
            shotMenu.AddItem(T("التسديد بقوة في منتصف المرمى", "Shoot centre"), () => ExecuteUserPenaltyShot(1));
            shotMenu.AddItem(T("التسديد في أقصى الزاوية اليسرى", "Shoot left"), () => ExecuteUserPenaltyShot(2));
            shotMenu.AddItem(T("اقتراب بطيء والتسديد يميناً", "Slow run-up, right"), () => ExecuteUserPenaltyShot(0,true));
            shotMenu.AddItem(T("اقتراب بطيء والتسديد في المنتصف", "Slow run-up, centre"), () => ExecuteUserPenaltyShot(1,true));
            shotMenu.AddItem(T("اقتراب بطيء والتسديد يساراً", "Slow run-up, left"), () => ExecuteUserPenaltyShot(2,true));
            Ready(true);
            AfterCommentary(() => PushMenu(shotMenu));
        }

        async void ExecuteUserPenaltyShot(int angle,bool slow=false)
        {
            _currentMenu = null;
            _menuHistory.Clear();
            _inputHandler.Reset();
            if(slow)await Task.Delay(1200);
            if(_activeMatch!=match)return;
            var taker=homeEligible.Taker(userKicksTaken);
            if(taker==null)return;
            var keeper=match.AwayTeam.Players.FirstOrDefault(p=>p.IsActiveOnPitch&&p.Role==PlayerRole.Goalkeeper);
            var result=ShootoutKick.Resolve(angle,Random.Shared.Next(3),taker.Shooting,keeper?.Defending??50,slow,Random.Shared.NextDouble());
            SoundEffectsPlayer.PlayKick(angle==0?.8f:angle==2?-.8f:0);
            await Task.Delay(TimeSpan.FromSeconds(result.FlightSeconds));
            if(_activeMatch!=match)return;
            userKicksTaken++;
            if(result.Scored){userPenaltiesScored++;SoundEffectsPlayer.PlayGoalNet();SoundEffectsPlayer.PlayCrowdGoal();}
            else SoundEffectsPlayer.PlayCrowdMiss();
            string description=result.Scored?T("هدف في الشباك", "Goal"):result.KeeperHeld?T("الحارس يمسك الركلة", "Keeper holds"):result.Saved?T("الحارس يبعد الركلة", "Keeper saves"):T("التسديدة خارج المرمى", "Shot wide");
            Outcome(true,result.Scored,result.Saved,T($"{description}. النتيجة الآن: فريقك {userPenaltiesScored}، والخصم {cpuPenaltiesScored}.", $"{description}. Your team {userPenaltiesScored}, opponent {cpuPenaltiesScored}."),PlayCpuTurn,result);
        }

        void PlayCpuTurn()
        {
            if (IsShootoutDecided())
            {
                FinishShootout(match, userPenaltiesScored, cpuPenaltiesScored);
                return;
            }

            var diveMenu = new AccessibleMenu(T($"ركلات الترجيح - الركلة رقم {cpuKicksTaken + 1} للخصم ({userPenaltiesScored} - {cpuPenaltiesScored}) - اختر ارتماء حارسك", $"Opponent penalty {cpuKicksTaken + 1}. Score {userPenaltiesScored} - {cpuPenaltiesScored}. Keeper direction"), _screenReader);
            diveMenu.AddItem(T("الارتماء نحو الزاوية اليمنى", "Dive right"), () => ExecuteCpuPenaltyShot(0));
            diveMenu.AddItem(T("الثبات في منتصف المرمى", "Stay centre"), () => ExecuteCpuPenaltyShot(1));
            diveMenu.AddItem(T("الارتماء نحو الزاوية اليسرى", "Dive left"), () => ExecuteCpuPenaltyShot(2));
            Ready(false);
            AfterCommentary(() => PushMenu(diveMenu));
        }

        async void ExecuteCpuPenaltyShot(int userDive)
        {
            _currentMenu = null;
            _menuHistory.Clear();
            _inputHandler.Reset();
            bool slow=Random.Shared.Next(4)==0;
            if(slow)await Task.Delay(1200);
            if(_activeMatch!=match)return;
            var taker=awayEligible.Taker(cpuKicksTaken);
            if(taker==null)return;
            var keeper=match.HomeTeam.Players.FirstOrDefault(p=>p.IsActiveOnPitch&&p.Role==PlayerRole.Goalkeeper);
            int angle=Random.Shared.Next(3);
            var result=ShootoutKick.Resolve(angle,userDive,taker.Shooting,keeper?.Defending??50,slow,Random.Shared.NextDouble());
            SoundEffectsPlayer.PlayKick(angle==0?.8f:angle==2?-.8f:0);
            await Task.Delay(TimeSpan.FromSeconds(result.FlightSeconds));
            if(_activeMatch!=match)return;
            cpuKicksTaken++;
            if(result.Scored){cpuPenaltiesScored++;SoundEffectsPlayer.PlayGoalNet();}
            else SoundEffectsPlayer.PlayCrowdCheer();
            string description=result.Scored?T("هدف لصالح الخصم", "Opponent scores"):result.KeeperHeld?T("حارسك يمسك ركلة الخصم", "Your keeper holds"):result.Saved?T("حارسك يبعد ركلة الخصم", "Your keeper saves"):T("ركلة الخصم خارج المرمى", "Opponent shoots wide");
            Outcome(false,result.Scored,result.Saved,T($"{description}. النتيجة الآن: فريقك {userPenaltiesScored}، والخصم {cpuPenaltiesScored}.", $"{description}. Your team {userPenaltiesScored}, opponent {cpuPenaltiesScored}."),PlayUserTurn,result);
        }

        void FinishShootout(MatchEngine m, int uScore, int cScore)
        {
            m.CompleteShootout(uScore,cScore);
            AfterCommentary(async () =>
            {
            _activeMatch = null;
            _currentMenu = null;
            _inputHandler.Reset();
            _displayListBox.Visible = true;
            SoundEffectsPlayer.PlayWhistle();

            string outcome = uScore > cScore
                ? T($"مبرووووك الفوز العظيم! فاز فريق {m.HomeTeam.Name} بركلات الترجيح بنتيجة {uScore} مقابل {cScore}!", $"{m.HomeTeam.Name} win on penalties, {uScore} - {cScore}.")
                : T($"هارد لك! فاز فريق {m.AwayTeam.Name} بركلات الترجيح بنتيجة {cScore} مقابل {uScore}.", $"{m.AwayTeam.Name} win on penalties, {cScore} - {uScore}.");

            await _screenReader.SpeakUntilFinishedAsync(outcome);
            if (IsDisposed || _activeMatch != null) return;
            SoundEffectsPlayer.StartMenuMusic();
            OpenMainMenu();
            });
        }

        void PrepareEligiblePlayers()
        {
            if(homeEligible.Players.Count <= eligibleCount) { PlayUserTurn(); return; }
            var menu = GamePlanMenuBuilder.CreateShootoutEligibilityMenu(homeEligible, eligibleCount, _screenReader, () =>
            {
                _currentMenu=null;_menuHistory.Clear();PrepareEligiblePlayers();
            });
            PushMenu(menu);
        }

        _commentaryEngine.RunAfterRecordedCommentaryAsync(async () =>
        {
            await _screenReader.SpeakUntilFinishedAsync(T("انطلاق ركلات الترجيح الحاسمة! 5 ركلات لكل فريق.", "Penalty shootout. Five kicks each."));
            if (!IsDisposed && IsHandleCreated) BeginInvoke(() => { if (_activeMatch == match) PrepareEligiblePlayers(); });
        });
    }

    private async void EndMatchAndReturnToMenu(bool waitForCommentary = true)
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
            string finalSpeech = _activeMatch.AbandonmentReason is { } reason ? T($"{reason} النتيجة عند التوقف: {_activeMatch.HomeTeam.Name} {_activeMatch.HomeTeam.Score}، {_activeMatch.AwayTeam.Name} {_activeMatch.AwayTeam.Score}.", $"{reason} Score at stoppage: {_activeMatch.HomeTeam.Name} {_activeMatch.HomeTeam.Score}, {_activeMatch.AwayTeam.Name} {_activeMatch.AwayTeam.Score}.") : T($"نهاية المباراة! النتيجة النهائية: {_activeMatch.HomeTeam.Name} {_activeMatch.HomeTeam.Score}، مقابل {_activeMatch.AwayTeam.Score} لـ {_activeMatch.AwayTeam.Name}. العودة للقائمة الرئيسية.", $"Full time: {_activeMatch.HomeTeam.Name} {_activeMatch.HomeTeam.Score}, {_activeMatch.AwayTeam.Name} {_activeMatch.AwayTeam.Score}.");
            _activeMatch = null;
            _currentMenu = null;
            _menuHistory.Clear();
            await _screenReader.SpeakUntilFinishedAsync(finalSpeech);
            if (IsDisposed || _activeMatch != null) return;
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
        if (_isInMatchAnthems)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space || e.KeyCode == Keys.Escape)
            {
                _skipMatchAnthems = true;
                _anthemEngine.StopAllSounds();
            }
            e.Handled = true; e.SuppressKeyPress = true;
            return;
        }
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
                if (_activeMatch?.PendingInjuryPlayer != null)
                {
                    e.Handled = true; e.SuppressKeyPress = true;
                    _screenReader.Speak(T("اختر بديلاً للمصاب أو اختر الاستمرار دون بديل.", "Choose a substitute or choose to continue without one."));
                    return;
                }
                e.Handled = true;
                e.SuppressKeyPress = true;
                SoundEffectsPlayer.PlayMenuBack();

                if (_activeMatch != null && _menuHistory.Count == 0)
                {
                    // استئناف المباراة مباشرة من قائمة الإيقاف المؤقت الرئيسية
                    if (_activeMatch.State == MatchState.PenaltyShootout)
                    {
                        _screenReader.Speak(T("اختر التسديد أو ارتماء الحارس من قائمة ركلات الترجيح.", "Penalty direction pending"));
                        return;
                    }
                    bool wasHalfTime = _activeMatch.State == MatchState.HalfTime;
                    if (wasHalfTime)
                    {
                        _screenReader.Speak(T("اختر بدء الشوط الثاني من القائمة لاستئناف المباراة.", "Interval"));
                        return;
                    }
                    _currentMenu = null;
                    _inputHandler.Reset();
                    SetupMatchDisplayListBox(_activeMatch.HomeTeam, _activeMatch.AwayTeam);
                    _activeMatch.ResumeMatch();
                    SoundEffectsPlayer.ResumeCrowdAmbiance();
                    if (wasHalfTime)
                    {
                        SoundEffectsPlayer.PlayWhistle();
                        _commentaryEngine.RunAfterRecordedCommentary(() => _screenReader.Speak(_activeMatch?.IsExtraTime == true ? T("انطلاق الشوط الإضافي الثاني", "Second extra-time period") : T("صافرة انطلاق الشوط الثاني!", "Second half")));
                    }
                    else
                    {
                        _screenReader.Speak(T("استئناف اللقاء", "Resuming match"));
                    }
                    _lastFrameTime = DateTime.UtcNow;
                    _gameLoopTimer?.Start();
                    return;
                }

                _screenReader.Speak(T("رجوع", "Back"));
                PopMenu();
                return;
            }
            return;
        }

        // No field/pause routing during shootout resolution or the final commentary handoff.
        if (_activeMatch?.State is MatchState.PenaltyShootout or MatchState.GameOver)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }
        // 2. إذا كنا في مباراة حية جارية (التحكم بالملعب)
        if (_activeMatch != null)
        {
            if (e.KeyCode == Keys.Enter && (_activeMatch.AwaitingStoppageConfirmation ||
                _activeMatch.AnalysisReplayPending || _activeMatch.State == MatchState.GoalCelebration))
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                _inputHandler.Reset();
                _replayDismissRequested = true;
                return;
            }

            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                _inputHandler.Reset();
                _gameLoopTimer?.Stop();
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
