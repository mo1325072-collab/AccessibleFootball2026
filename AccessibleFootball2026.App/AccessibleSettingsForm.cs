using System;
using System.Drawing;
using System.Net.Http;
using System.Windows.Forms;
using System.Text.Json;
using AccessibleFootball2026.Audio;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.App;

/// <summary>
/// نافذة إعدادات اللعبة الرسمية بعناصر ويندوز الحقيقية (Real Windows Forms Accessible Dialog):
/// تشمل شرائط التمرير (TrackBar / Sliders) ومربعات التحديد (CheckBoxes)
/// ومربعات التحرير والسرد (ComboBoxes) وأزرار الحفظ والاستعادة (Buttons)
/// متوافقة 100% مع ناطق الشاشة NVDA والتنقل بـ Tab والأسهم وSpace.
/// </summary>
public class AccessibleSettingsForm : Form
{
    private readonly IScreenReader _screenReader;

    // عناصر التحكم الحقيقية
    private ComboBox _cmbLanguage = null!;
    private TrackBar _tbMasterVolume = null!;
    private TrackBar _tbSfxVolume = null!;
    private TrackBar _tbCommentaryVolume = null!;
    private TrackBar _tbCrowdVolume = null!;
    private TrackBar _tbBeaconVolume = null!;
    private TrackBar _tbMusicVolume = null!;

    private ComboBox _cmbDifficulty = null!;
    private ComboBox _cmbDuration = null!;

    private CheckBox _chkExtraTime = null!;
    private CheckBox _chkPenalties = null!;
    private CheckBox _chkAutoSubs = null!;
    private CheckBox _chkAutoTactics = null!;
    private CheckBox _chkAiVar = null!;
    private CheckBox _chkStartupUpdates = null!;

    private Button _btnSave = null!;
    private Button _btnReset = null!;
    private Button _btnClose = null!;

    private readonly int[] _durations = [3, 5, 10, 15, 20];
    private bool _isLoadingSettings = false;

    public AccessibleSettingsForm(IScreenReader screenReader)
    {
        _screenReader = screenReader;
        InitializeComponents();
        LoadCurrentSettings();
    }

    private void InitializeComponents()
    {
        bool isArabic = MatchSettings.Current.Language == UiLanguage.Arabic;

        Text = isArabic ? "إعدادات اللعبة والصوت" : "Game & Audio Settings";
        Size = new Size(680, 720);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = Color.FromArgb(24, 28, 38);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
        KeyPreview = true;

        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            ColumnCount = 2,
            RowCount = 15,
            Padding = new Padding(20),
        };
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));

        int row = 0;

        // 1. اللغة (Language ComboBox)
        var lblLang = CreateLabel(isArabic ? "اللغة (Language):" : "Language:");
        _cmbLanguage = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Fill,
            AccessibleName = isArabic ? "اللغة" : "Language",
            BackColor = Color.FromArgb(36, 42, 56),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 11F)
        };
        _cmbLanguage.Items.AddRange(["العربية (Arabic)", "English (الإنجليزية)"]);
        // الإعدادات مسودة حتى الحفظ: لا تطبيق فوري ولا معاينة صوتية أثناء التنقل
        mainPanel.Controls.Add(lblLang, 0, row);
        mainPanel.Controls.Add(_cmbLanguage, 1, row++);

        // 2. الصوت العام (Master Volume TrackBar)
        var lblMaster = CreateLabel(isArabic ? "مستوى الصوت العام:" : "Master Volume:");
        _tbMasterVolume = CreateTrackBar(isArabic ? "مستوى الصوت العام" : "Master Volume");
        mainPanel.Controls.Add(lblMaster, 0, row);
        mainPanel.Controls.Add(_tbMasterVolume, 1, row++);

        // 3. مؤثرات اللعب (SFX Volume TrackBar)
        var lblSfx = CreateLabel(isArabic ? "مؤثرات اللعب والنغمات:" : "SFX & Earcons Volume:");
        _tbSfxVolume = CreateTrackBar(isArabic ? "مؤثرات اللعب والنغمات" : "SFX Volume");
        mainPanel.Controls.Add(lblSfx, 0, row);
        mainPanel.Controls.Add(_tbSfxVolume, 1, row++);

        // 4. التعليق الصوتي (Commentary Volume TrackBar)
        var lblComm = CreateLabel(isArabic ? "صوت التعليق:" : "Commentary Volume:");
        _tbCommentaryVolume = CreateTrackBar(isArabic ? "صوت التعليق" : "Commentary Volume");
        mainPanel.Controls.Add(lblComm, 0, row);
        mainPanel.Controls.Add(_tbCommentaryVolume, 1, row++);

        // 5. صوت الجماهير (Crowd Volume TrackBar)
        var lblCrowd = CreateLabel(isArabic ? "صوت الجماهير في الملعب:" : "Stadium Crowd Volume:");
        _tbCrowdVolume = CreateTrackBar(isArabic ? "صوت الجماهير في الملعب" : "Crowd Volume");
        mainPanel.Controls.Add(lblCrowd, 0, row);
        mainPanel.Controls.Add(_tbCrowdVolume, 1, row++);

        // 6. منارة المرمى 3D (Goal Beacon Volume TrackBar)
        var lblBeacon = CreateLabel(isArabic ? "منارة المرمى المجسمة 3D:" : "3D Goal Beacon Volume:");
        _tbBeaconVolume = CreateTrackBar(isArabic ? "منارة المرمى المجسمة 3D" : "Goal Beacon Volume");
        mainPanel.Controls.Add(lblBeacon, 0, row);
        mainPanel.Controls.Add(_tbBeaconVolume, 1, row++);

        // 7. موسيقى القوائم (Menu Music Volume TrackBar)
        var lblMusic = CreateLabel(isArabic ? "موسيقى القوائم والخلفية:" : "Menu Music Volume:");
        _tbMusicVolume = CreateTrackBar(isArabic ? "موسيقى القوائم والخلفية" : "Menu Music Volume");
        mainPanel.Controls.Add(lblMusic, 0, row);
        mainPanel.Controls.Add(_tbMusicVolume, 1, row++);

        // 8. مستوى الصعوبة (Difficulty ComboBox)
        var lblDiff = CreateLabel(isArabic ? "مستوى الصعوبة:" : "Difficulty Level:");
        _cmbDifficulty = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Fill,
            AccessibleName = isArabic ? "مستوى الصعوبة" : "Difficulty Level",
            BackColor = Color.FromArgb(36, 42, 56),
            ForeColor = Color.White
        };
        _cmbDifficulty.Items.AddRange(isArabic ? ["سهل (Easy)", "متوسط (Normal)", "محترف (Hard)", "أسطوري (Legendary)"] : ["Easy", "Normal", "Hard", "Legendary"]);
        mainPanel.Controls.Add(lblDiff, 0, row);
        mainPanel.Controls.Add(_cmbDifficulty, 1, row++);

        // 9. مدة المباراة (Match Duration ComboBox)
        var lblDur = CreateLabel(isArabic ? "مدة المباراة الحقيقية:" : "Match Duration:");
        _cmbDuration = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Fill,
            AccessibleName = isArabic ? "مدة المباراة الحقيقية" : "Match Duration",
            BackColor = Color.FromArgb(36, 42, 56),
            ForeColor = Color.White
        };
        foreach (int m in _durations)
        {
            _cmbDuration.Items.Add(isArabic ? $"{m} دقائق" : $"{m} mins");
        }
        mainPanel.Controls.Add(lblDur, 0, row);
        mainPanel.Controls.Add(_cmbDuration, 1, row++);

        // 11. الأشواط الإضافية (Extra Time CheckBox)
        _chkExtraTime = CreateCheckBox(isArabic ? "الأشواط الإضافية عند التعادل في مباريات الكؤوس" : "Extra Time on Draw");
        mainPanel.Controls.Add(_chkExtraTime, 0, row);
        mainPanel.SetColumnSpan(_chkExtraTime, 2);
        row++;

        // 12. ركلات الترجيح (Penalty Shootout CheckBox)
        _chkPenalties = CreateCheckBox(isArabic ? "ركلات الترجيح عند استمرار التعادل" : "Penalty Shootouts on Draw");
        mainPanel.Controls.Add(_chkPenalties, 0, row);
        mainPanel.SetColumnSpan(_chkPenalties, 2);
        row++;

        // 13. التبديلات التلقائية بالذكاء الاصطناعي (Auto-Subs CheckBox)
        _chkAutoSubs = CreateCheckBox(isArabic ? "تبديلات تلقائية حسب اللياقة وخطر الطرد" : "Automatic substitutions for fatigue or dismissal risk");
        mainPanel.Controls.Add(_chkAutoSubs, 0, row);
        mainPanel.SetColumnSpan(_chkAutoSubs, 2);
        row++;

        // 14. التعديلات التكتيكية التلقائية بالذكاء الاصطناعي (Auto-Tactics CheckBox)
        _chkAutoTactics = CreateCheckBox(isArabic ? "تعديلات تكتيكية تلقائية بقواعد اللعبة" : "Native-rule automatic tactical adjustments");
        mainPanel.Controls.Add(_chkAutoTactics, 0, row);
        mainPanel.SetColumnSpan(_chkAutoTactics, 2);
        row++;

        // 15. حكم الفيديو المساعد بالذكاء الاصطناعي (AI VAR CheckBox)
        _chkAiVar = CreateCheckBox(isArabic ? "مراجعة التسلل المتقارب بالقواعد المحلية" : "Native-rule close offside review");
        mainPanel.Controls.Add(_chkAiVar, 0, row);
        mainPanel.SetColumnSpan(_chkAiVar, 2);
        row++;

        _chkStartupUpdates = CreateCheckBox(isArabic ? "التحقق من وجود تحديثات عند فتح اللعبة" : "Check for updates when the game starts");
        mainPanel.Controls.Add(_chkStartupUpdates, 0, row);
        mainPanel.SetColumnSpan(_chkStartupUpdates, 2);
        row++;
        var updateButton = new Button
        {
            Text = isArabic ? "التحقق من وجود تحديثات" : "Check for updates",
            AccessibleName = isArabic ? "التحقق من وجود تحديثات" : "Check for updates",
            Dock = DockStyle.Fill,
            AutoSize = true,
            MinimumSize = new Size(200, 40)
        };
        updateButton.Click += async (_, _) =>
        {
            updateButton.Enabled = false;
            _screenReader.Speak(LocalizationService.Text("جار التحقق من وجود تحديثات.", "Checking for updates."));
            var result = await GameUpdateChecker.CheckAsync();
            if (IsDisposed) return;
            updateButton.Enabled = true;
            string updateTitle = LocalizationService.Text("تحديث اللعبة", "Game update");
            if (result.State == GameUpdateState.Available && result.Offer != null)
            {
                var offer = result.Offer;
                long megabytes = (offer.Asset.Size + 1024 * 1024 - 1) / (1024 * 1024);
                string message = offer.Kind == GameUpdateDownloadKind.CodePatch
                    ? LocalizationService.Text(
                        $"يتوفر إصدار جديد {result.Version}. حجم التحديث {megabytes} ميجابايت فقط، لأن الأصوات والبيانات لم تتغير وستبقى كما هي. هل تريد تنزيله وتثبيته الآن؟",
                        $"Version {result.Version} is available. The update is only {megabytes} MB, because sounds and data are unchanged and will stay in place. Download and install it now?")
                    : LocalizationService.Text(
                        $"يتوفر إصدار جديد {result.Version}. هذا تحديث كامل بحجم {megabytes} ميجابايت لأن محتوى اللعبة تغير. إعداداتك محفوظة ولن تتأثر. هل تريد تنزيله وتثبيته الآن؟",
                        $"Version {result.Version} is available. This is a full {megabytes} MB update because game content changed. Your settings are preserved and will not be affected. Download and install it now?");
                _screenReader.Speak(message);
                var answer = MessageBox.Show(this, message, updateTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (answer == DialogResult.Yes) await DownloadAndInstallAsync(offer, updateTitle);
                return;
            }
            string status = result.State switch
            {
                GameUpdateState.Current => LocalizationService.Text("نسختك هي الأحدث بين إصدارات اللعبة المنشورة القابلة للتثبيت.", "Your version is current among published installable game releases."),
                GameUpdateState.NoPublishedGame => LocalizationService.Text("لا يوجد إصدار لعبة رسمي قابل للتثبيت منشور حالياً. ملفات البناء ليست تحديثاً للعبة.", "No official installable game release has been published yet. Build inputs are not a game update."),
                _ => LocalizationService.Text("تعذر التحقق من التحديثات. تحقق من الاتصال وحاول مرة أخرى؛ لم يتم التأكد من حالة نسختك.", "Could not check for updates. Check your connection and retry; your version status is not confirmed.")
            };
            _screenReader.Speak(status);
            MessageBox.Show(this, status, updateTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        mainPanel.Controls.Add(updateButton, 0, row);
        mainPanel.SetColumnSpan(updateButton, 2);
        row++;
        mainPanel.RowCount = row;

        // 15. أزرار الحفظ والاستعادة والإلغاء
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(10)
        };

        _btnClose = new Button
        {
            Text = isArabic ? "إغلاق (Esc)" : "Close (Esc)",
            DialogResult = DialogResult.Cancel,
            Size = new Size(130, 40),
            BackColor = Color.FromArgb(45, 52, 68),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnClose.Click += (_, _) => Close();

        _btnReset = new Button
        {
            Text = isArabic ? "استعادة الافتراضيات" : "Reset Defaults",
            Size = new Size(170, 40),
            BackColor = Color.FromArgb(70, 50, 40),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnReset.Click += OnResetDefaultsClicked;

        _btnSave = new Button
        {
            Text = isArabic ? "حفظ الإعدادات" : "Save Settings",
            DialogResult = DialogResult.OK,
            Size = new Size(150, 40),
            BackColor = Color.FromArgb(34, 139, 34),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold)
        };
        _btnSave.Click += OnSaveSettingsClicked;

        buttonPanel.Controls.Add(_btnClose);
        buttonPanel.Controls.Add(_btnReset);
        buttonPanel.Controls.Add(_btnSave);

        Controls.Add(mainPanel);
        Controls.Add(buttonPanel);

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        };
    }

    private Label CreateLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(220, 225, 235),
            Font = new Font("Segoe UI", 11F, FontStyle.Bold)
        };
    }

    private TrackBar CreateTrackBar(string accessibleName)
    {
        return new TrackBar
        {
            Minimum = 0,
            Maximum = 100,
            TickFrequency = 10,
            SmallChange = 5,
            LargeChange = 10,
            Dock = DockStyle.Fill,
            AccessibleName = accessibleName,
            AccessibleRole = AccessibleRole.Slider
        };
    }

    private CheckBox CreateCheckBox(string text)
    {
        return new CheckBox
        {
            Text = text,
            Dock = DockStyle.Fill,
            AccessibleName = text,
            AccessibleRole = AccessibleRole.CheckButton,
            ForeColor = Color.FromArgb(230, 235, 245),
            Font = new Font("Segoe UI", 11F),
            Padding = new Padding(0, 5, 0, 5)
        };
    }

    private void LoadCurrentSettings()
    {
        _isLoadingSettings = true;
        try
        {
            var s = MatchSettings.Current;

            _cmbLanguage.SelectedIndex = s.Language == UiLanguage.English ? 1 : 0;
            _tbMasterVolume.Value = Math.Clamp(s.MasterVolumePercent, 0, 100);
            _tbSfxVolume.Value = Math.Clamp(s.SfxVolumePercent, 0, 100);
            _tbCommentaryVolume.Value = Math.Clamp(s.CommentaryVolumePercent, 0, 100);
            _tbCrowdVolume.Value = Math.Clamp(s.CrowdVolumePercent, 0, 100);
            _tbBeaconVolume.Value = Math.Clamp(s.GoalBeaconVolumePercent, 0, 100);
            _tbMusicVolume.Value = Math.Clamp(s.MusicVolumePercent, 0, 100);

            _cmbDifficulty.SelectedIndex = Math.Clamp((int)s.Difficulty, 0, _cmbDifficulty.Items.Count - 1);

            int durIdx = Array.IndexOf(_durations, s.MatchDurationMinutes);
            _cmbDuration.SelectedIndex = durIdx >= 0 ? durIdx : 1;

            _chkExtraTime.Checked = s.ExtraTimeEnabled;
            _chkPenalties.Checked = s.PenaltyShootoutEnabled;
            _chkAutoSubs.Checked = s.AutoSubstitutionsEnabled;
            _chkAutoTactics.Checked = s.AutoTacticalAdjustmentsEnabled;
            _chkAiVar.Checked = s.AiVarEnabled;
            _chkStartupUpdates.Checked = s.CheckForUpdatesOnStartup;
        }
        finally
        {
            _isLoadingSettings = false;
        }
    }

    private void RefreshLanguageLabels()
    {
        var pairs = new (string Arabic, string English)[]
        {
            ("إعدادات اللعبة والصوت", "Game & Audio Settings"),
            ("اللغة (Language):", "Language:"),
            ("اللغة", "Language"),
            ("مستوى الصوت العام:", "Master Volume:"),
            ("مستوى الصوت العام", "Master Volume"),
            ("مؤثرات اللعب والنغمات:", "SFX & Earcons Volume:"),
            ("مؤثرات اللعب والنغمات", "SFX Volume"),
            ("صوت التعليق:", "Commentary Volume:"),
            ("صوت التعليق", "Commentary Volume"),
            ("صوت الجماهير في الملعب:", "Stadium Crowd Volume:"),
            ("صوت الجماهير في الملعب", "Crowd Volume"),
            ("منارة المرمى المجسمة 3D:", "3D Goal Beacon Volume:"),
            ("منارة المرمى المجسمة 3D", "Goal Beacon Volume"),
            ("موسيقى القوائم والخلفية:", "Menu Music Volume:"),
            ("موسيقى القوائم والخلفية", "Menu Music Volume"),
            ("مستوى الصعوبة:", "Difficulty Level:"),
            ("مستوى الصعوبة", "Difficulty Level"),
            ("مدة المباراة الحقيقية:", "Match Duration:"),
            ("مدة المباراة الحقيقية", "Match Duration"),
            ("الأشواط الإضافية عند التعادل في مباريات الكؤوس", "Extra Time on Draw"),
            ("ركلات الترجيح عند استمرار التعادل", "Penalty Shootouts on Draw"),
            ("تبديلات تلقائية حسب اللياقة وخطر الطرد", "Automatic substitutions for fatigue or dismissal risk"),
            ("تعديلات تكتيكية تلقائية بقواعد اللعبة", "Native-rule automatic tactical adjustments"),
            ("مراجعة التسلل المتقارب بالقواعد المحلية", "Native-rule close offside review"),
            ("إغلاق (Esc)", "Close (Esc)"),
            ("استعادة الافتراضيات", "Reset Defaults"),
            ("حفظ الإعدادات", "Save Settings"),
            ("التحقق من وجود تحديثات", "Check for updates"),
            ("التحقق من وجود تحديثات عند فتح اللعبة", "Check for updates when the game starts"),
        };
        string Localize(string? text)
        {
            foreach (var pair in pairs)
                if (text == pair.Arabic || text == pair.English)
                    return LocalizationService.Text(pair.Arabic, pair.English);
            return text ?? string.Empty;
        }
        void Visit(Control control)
        {
            control.Text = Localize(control.Text);
            if (control.AccessibleName != null) control.AccessibleName = Localize(control.AccessibleName);
            foreach (Control child in control.Controls) Visit(child);
        }
        bool loadingBefore = _isLoadingSettings;
        _isLoadingSettings = true;
        try
        {
            Visit(this);
            int difficulty = _cmbDifficulty.SelectedIndex, duration = _cmbDuration.SelectedIndex;
            bool arabic = MatchSettings.Current.Language == UiLanguage.Arabic;
            _cmbDifficulty.Items.Clear();
            _cmbDifficulty.Items.AddRange(arabic ? ["سهل (Easy)", "متوسط (Normal)", "محترف (Hard)", "أسطوري (Legendary)"] : ["Easy", "Normal", "Hard", "Legendary"]);
            _cmbDuration.Items.Clear();
            foreach (int minutes in _durations) _cmbDuration.Items.Add(LocalizationService.FormatDuration(minutes));
            _cmbDifficulty.SelectedIndex = difficulty;
            _cmbDuration.SelectedIndex = duration;
        }
        finally { _isLoadingSettings = loadingBefore; }
    }

    private void OnResetDefaultsClicked(object? sender, EventArgs e)
    {
        // استعادة الافتراضيات تملأ المسودة فقط؛ لا تُطبَّق ولا تُحفَظ حتى يضغط المستخدم «حفظ الإعدادات»
        var defaults = new MatchSettings();
        _isLoadingSettings = true;
        try
        {
            _cmbLanguage.SelectedIndex = defaults.Language == UiLanguage.English ? 1 : 0;
            _tbMasterVolume.Value = Math.Clamp(defaults.MasterVolumePercent, 0, 100);
            _tbSfxVolume.Value = Math.Clamp(defaults.SfxVolumePercent, 0, 100);
            _tbCommentaryVolume.Value = Math.Clamp(defaults.CommentaryVolumePercent, 0, 100);
            _tbCrowdVolume.Value = Math.Clamp(defaults.CrowdVolumePercent, 0, 100);
            _tbBeaconVolume.Value = Math.Clamp(defaults.GoalBeaconVolumePercent, 0, 100);
            _tbMusicVolume.Value = Math.Clamp(defaults.MusicVolumePercent, 0, 100);
            _cmbDifficulty.SelectedIndex = Math.Clamp((int)defaults.Difficulty, 0, _cmbDifficulty.Items.Count - 1);
            int durIdx = Array.IndexOf(_durations, defaults.MatchDurationMinutes);
            _cmbDuration.SelectedIndex = durIdx >= 0 ? durIdx : 1;
            _chkExtraTime.Checked = defaults.ExtraTimeEnabled;
            _chkPenalties.Checked = defaults.PenaltyShootoutEnabled;
            _chkAutoSubs.Checked = defaults.AutoSubstitutionsEnabled;
            _chkAutoTactics.Checked = defaults.AutoTacticalAdjustmentsEnabled;
            _chkAiVar.Checked = defaults.AiVarEnabled;
            _chkStartupUpdates.Checked = defaults.CheckForUpdatesOnStartup;
        }
        finally { _isLoadingSettings = false; }
        SoundEffectsPlayer.PlayMenuSelect();
        _screenReader.Speak(MatchSettings.Current.Language == UiLanguage.Arabic
            ? "عُرضت القيم الافتراضية. اضغط حفظ الإعدادات لاعتمادها."
            : "Defaults shown. Press Save Settings to apply them.");
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // لا تعديل مباشر على الإعدادات أثناء التحرير؛ الإغلاق دون حفظ يترك كل شيء كما كان
        base.OnFormClosing(e);
    }

    private void OnSaveSettingsClicked(object? sender, EventArgs e)
    {
        var s = MatchSettings.Current;

        s.Language = _cmbLanguage.SelectedIndex == 1 ? UiLanguage.English : UiLanguage.Arabic;
        s.MasterVolumePercent = _tbMasterVolume.Value;
        s.SfxVolumePercent = _tbSfxVolume.Value;
        s.CommentaryVolumePercent = _tbCommentaryVolume.Value;
        s.CrowdVolumePercent = _tbCrowdVolume.Value;
        s.GoalBeaconVolumePercent = _tbBeaconVolume.Value;
        s.MusicVolumePercent = _tbMusicVolume.Value;

        s.Difficulty = (DifficultyLevel)_cmbDifficulty.SelectedIndex;
        s.MatchDurationMinutes = _durations[_cmbDuration.SelectedIndex];

        // المعلق المعتمد الوحيد حالياً هو فارس عوض؛ لا اختيار معلق ولا وضع صامت
        s.Commentator = CommentatorId.FaresAwad;
        s.CommentaryVoice = CommentaryVoiceOption.FaresAwad;
        s.CommentaryEnabled = true;

        s.ExtraTimeEnabled = _chkExtraTime.Checked;
        s.PenaltyShootoutEnabled = _chkPenalties.Checked;
        s.AutoSubstitutionsEnabled = _chkAutoSubs.Checked;
        s.AutoTacticalAdjustmentsEnabled = _chkAutoTactics.Checked;
        s.AiVarEnabled = _chkAiVar.Checked;
        s.CheckForUpdatesOnStartup = _chkStartupUpdates.Checked;

        if (!s.Save())
        {
            _screenReader.Speak(s.Language == UiLanguage.Arabic
                ? "تعذر حفظ الإعدادات. لم يتم حفظ التغييرات على القرص. يمكنك المحاولة مرة أخرى أو الإغلاق لإلغاء التغييرات."
                : "Settings could not be saved to disk. Try again, or close to cancel changes.");
            return;
        }
        SoundEffectsPlayer.PlayMenuSelect();
        _screenReader.Speak(s.Language == UiLanguage.Arabic
            ? "تم حفظ الإعدادات بنجاح في ملف settings.json"
            : "Settings successfully saved to settings.json.");

        Close();
    }

    /// <summary>
    /// Downloads a confirmed update with an accessible cancellable progress dialog, verifies its
    /// published hash, then either restarts the game to apply a code patch or hands over to the
    /// full setup wizard. Settings, sounds and data are never removed by this flow.
    /// </summary>
    private async System.Threading.Tasks.Task DownloadAndInstallAsync(GameUpdateOffer offer, string updateTitle)
    {
        using var cancellation = new CancellationTokenSource();
        using var progressForm = new UpdateDownloadProgressForm(_screenReader, offer, cancellation);
        progressForm.Show(this);
        string? downloadedPath = null;
        string? failure = null;
        var progress = new Progress<int>(percent => progressForm.ReportPercent(percent));
        try
        {
            downloadedPath = await GameUpdateDownloader.DownloadAsync(offer, progress, cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            failure = LocalizationService.Text("ألغيت التنزيل. لم يتغير شيء في اللعبة، ويمكنك المحاولة لاحقاً وسيكمل التنزيل من حيث توقف.", "Download cancelled. Nothing in the game changed; when you try again the download resumes where it stopped.");
        }
        catch (Exception error) when (error is HttpRequestException or InvalidDataException or IOException or UnauthorizedAccessException)
        {
            failure = LocalizationService.Text("فشل التنزيل أو التحقق من التحديث. لم يتم تثبيت أي شيء ولم تتغير نسختك. تحقق من الاتصال وحاول مرة أخرى.", "The download or its verification failed. Nothing was installed and your version is unchanged. Check your connection and try again.");
        }
        progressForm.Close();
        if (failure != null)
        {
            _screenReader.Speak(failure);
            MessageBox.Show(this, failure, updateTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (offer.Kind == GameUpdateDownloadKind.CodePatch)
        {
            try { await GameUpdateDownloader.StagePatchAsync(downloadedPath!, offer, CancellationToken.None); }
            catch (Exception error) when (error is HttpRequestException or InvalidDataException or IOException or UnauthorizedAccessException)
            {
                string stageFailure = LocalizationService.Text("تم تنزيل التحديث لكن فشل تجهيزه للتثبيت. لم تتغير نسختك الحالية.", "The update downloaded but could not be prepared for installation. Your current version is unchanged.");
                _screenReader.Speak(stageFailure);
                MessageBox.Show(this, stageFailure, updateTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string ready = LocalizationService.Text(
                "اكتمل التنزيل والتحقق منه. تثبيته الآن يعيد تشغيل اللعبة فوراً ويستغرق لحظات. لو اخترت لا، سيثبت تلقائياً عند فتح اللعبة في المرة القادمة. تثبيت الآن؟",
                "Download complete and verified. Installing now restarts the game and takes moments. If you choose No, it installs automatically the next time the game opens. Install now?");
            _screenReader.Speak(ready);
            if (MessageBox.Show(this, ready, updateTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (GameUpdateDownloader.TryStartApplier())
                {
                    _screenReader.Speak(LocalizationService.Text("جار تثبيت التحديث وإعادة تشغيل اللعبة.", "Installing the update and restarting the game."));
                    Application.Exit();
                }
                else
                {
                    string applierFailure = LocalizationService.Text(
                        "تعذر بدء تثبيت التحديث الآن. سيبقى محفوظاً وسيثبت عند فتح اللعبة في المرة القادمة.",
                        "The update could not start installing now. It stays downloaded and installs the next time the game opens.");
                    _screenReader.Speak(applierFailure);
                    MessageBox.Show(this, applierFailure, updateTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            return;
        }

        string setup = LocalizationService.Text(
            "اكتمل تنزيل حزمة التثبيت الكاملة والتحقق منها. سيغلق اللعبة ويفتح معالج التثبيت المعتاد، ويثبت فوق نسختك مع الحفاظ على إعداداتك. ابدأ التثبيت الآن؟",
            "The full setup package finished downloading and was verified. It closes the game and opens the usual setup wizard, installing over your copy while preserving your settings. Start setup now?");
        _screenReader.Speak(setup);
        if (MessageBox.Show(this, setup, updateTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            GameUpdateDownloader.LaunchInstaller(downloadedPath!, AppContext.BaseDirectory);
            Application.Exit();
        }
        else
        {
            _screenReader.Speak(LocalizationService.Text("احتُفظ بحزمة التثبيت في مجلد Updates داخل مجلد اللعبة ويمكنك تشغيلها في أي وقت.", "The setup package was kept in the Updates folder inside the game folder and can be run at any time."));
        }
    }
}

/// <summary>
/// Small accessible progress dialog for an update download: a labelled progress bar and a cancel
/// button, with spoken percentage milestones so a screen-reader user can follow without staring.
/// </summary>
public sealed class UpdateDownloadProgressForm : Form
{
    private readonly IScreenReader _screenReader;
    private readonly CancellationTokenSource _cancellation;
    private readonly ProgressBar _bar;
    private readonly Label _label;
    private int _lastSpoken = -1;

    public UpdateDownloadProgressForm(IScreenReader screenReader, GameUpdateOffer offer, CancellationTokenSource cancellation)
    {
        _screenReader = screenReader;
        _cancellation = cancellation;
        bool isArabic = MatchSettings.Current.Language == UiLanguage.Arabic;
        Text = isArabic ? "تنزيل التحديث" : "Downloading update";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Width = 420; Height = 180;
        _label = new Label
        {
            Dock = DockStyle.Top,
            Height = 40,
            TextAlign = ContentAlignment.MiddleCenter,
            AccessibleName = isArabic ? "حالة التنزيل" : "Download status",
            Text = isArabic ? $"جار تنزيل الإصدار {offer.Version}... 0%" : $"Downloading version {offer.Version}... 0%"
        };
        _bar = new ProgressBar { Dock = DockStyle.Top, Height = 30, Minimum = 0, Maximum = 100, AccessibleName = Text };
        var cancel = new Button
        {
            Dock = DockStyle.Bottom,
            Height = 40,
            Text = isArabic ? "إلغاء التنزيل" : "Cancel download",
            AccessibleName = isArabic ? "إلغاء التنزيل" : "Cancel download"
        };
        cancel.Click += (_, _) => { cancel.Enabled = false; _cancellation.Cancel(); };
        Controls.Add(_bar);
        Controls.Add(_label);
        Controls.Add(cancel);
    }

    public void ReportPercent(int percent)
    {
        if (IsDisposed) return;
        _bar.Value = Math.Clamp(percent, 0, 100);
        bool isArabic = MatchSettings.Current.Language == UiLanguage.Arabic;
        _label.Text = isArabic ? $"جار تنزيل التحديث... {percent}%" : $"Downloading update... {percent}%";
        int milestone = percent / 10 * 10;
        if (milestone != _lastSpoken && milestone % 20 == 0)
        {
            _lastSpoken = milestone;
            _screenReader.Speak(isArabic ? $"تم تنزيل {milestone} بالمئة" : $"{milestone} percent downloaded", interrupt: false);
        }
    }
}
