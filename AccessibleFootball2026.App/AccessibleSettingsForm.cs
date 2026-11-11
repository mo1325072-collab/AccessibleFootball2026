using System;
using System.Drawing;
using System.Windows.Forms;
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
    private ComboBox _cmbCommentaryVoice = null!;

    private CheckBox _chkExtraTime = null!;
    private CheckBox _chkPenalties = null!;
    private CheckBox _chkAutoSubs = null!;
    private CheckBox _chkAutoTactics = null!;
    private CheckBox _chkAiVar = null!;

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
        _cmbLanguage.SelectedIndexChanged += OnLanguageChanged;
        mainPanel.Controls.Add(lblLang, 0, row);
        mainPanel.Controls.Add(_cmbLanguage, 1, row++);

        // 2. الصوت العام (Master Volume TrackBar)
        var lblMaster = CreateLabel(isArabic ? "مستوى الصوت العام:" : "Master Volume:");
        _tbMasterVolume = CreateTrackBar(isArabic ? "مستوى الصوت العام" : "Master Volume");
        _tbMasterVolume.ValueChanged += (_, _) =>
        {
            if (_isLoadingSettings) return;
            MatchSettings.Current.MasterVolumePercent = _tbMasterVolume.Value;
            SoundEffectsPlayer.MasterVolumeSetting = _tbMasterVolume.Value;
            SoundEffectsPlayer.PlayLivePreviewMaster(_tbMasterVolume.Value);
        };
        mainPanel.Controls.Add(lblMaster, 0, row);
        mainPanel.Controls.Add(_tbMasterVolume, 1, row++);

        // 3. مؤثرات اللعب (SFX Volume TrackBar)
        var lblSfx = CreateLabel(isArabic ? "مؤثرات اللعب والنغمات:" : "SFX & Earcons Volume:");
        _tbSfxVolume = CreateTrackBar(isArabic ? "مؤثرات اللعب والنغمات" : "SFX Volume");
        _tbSfxVolume.ValueChanged += (_, _) =>
        {
            if (_isLoadingSettings) return;
            MatchSettings.Current.SfxVolumePercent = _tbSfxVolume.Value;
            SoundEffectsPlayer.SfxVolumeSetting = _tbSfxVolume.Value;
            SoundEffectsPlayer.PlayLivePreviewSfx(_tbSfxVolume.Value);
        };
        mainPanel.Controls.Add(lblSfx, 0, row);
        mainPanel.Controls.Add(_tbSfxVolume, 1, row++);

        // 4. التعليق الصوتي (Commentary Volume TrackBar)
        var lblComm = CreateLabel(isArabic ? "صوت التعليق:" : "Commentary Volume:");
        _tbCommentaryVolume = CreateTrackBar(isArabic ? "صوت التعليق" : "Commentary Volume");
        _tbCommentaryVolume.ValueChanged += (_, _) =>
        {
            if (_isLoadingSettings) return;
            MatchSettings.Current.CommentaryVolumePercent = _tbCommentaryVolume.Value;
            SoundEffectsPlayer.CommentaryVolumeSetting = _tbCommentaryVolume.Value;
            SoundEffectsPlayer.PlayLivePreviewCommentary(_tbCommentaryVolume.Value);
        };
        mainPanel.Controls.Add(lblComm, 0, row);
        mainPanel.Controls.Add(_tbCommentaryVolume, 1, row++);

        // 5. صوت الجماهير (Crowd Volume TrackBar)
        var lblCrowd = CreateLabel(isArabic ? "صوت الجماهير في الملعب:" : "Stadium Crowd Volume:");
        _tbCrowdVolume = CreateTrackBar(isArabic ? "صوت الجماهير في الملعب" : "Crowd Volume");
        _tbCrowdVolume.ValueChanged += (_, _) =>
        {
            if (_isLoadingSettings) return;
            MatchSettings.Current.CrowdVolumePercent = _tbCrowdVolume.Value;
            SoundEffectsPlayer.CrowdVolumeSetting = _tbCrowdVolume.Value;
            SoundEffectsPlayer.PlayLivePreviewCrowd(_tbCrowdVolume.Value);
        };
        mainPanel.Controls.Add(lblCrowd, 0, row);
        mainPanel.Controls.Add(_tbCrowdVolume, 1, row++);

        // 6. منارة المرمى 3D (Goal Beacon Volume TrackBar)
        var lblBeacon = CreateLabel(isArabic ? "منارة المرمى المجسمة 3D:" : "3D Goal Beacon Volume:");
        _tbBeaconVolume = CreateTrackBar(isArabic ? "منارة المرمى المجسمة 3D" : "Goal Beacon Volume");
        _tbBeaconVolume.ValueChanged += (_, _) =>
        {
            if (_isLoadingSettings) return;
            MatchSettings.Current.GoalBeaconVolumePercent = _tbBeaconVolume.Value;
            SoundEffectsPlayer.GoalBeaconVolumeSetting = _tbBeaconVolume.Value;
            SoundEffectsPlayer.PlayLivePreviewGoalBeacon(_tbBeaconVolume.Value);
        };
        mainPanel.Controls.Add(lblBeacon, 0, row);
        mainPanel.Controls.Add(_tbBeaconVolume, 1, row++);

        // 7. موسيقى القوائم (Menu Music Volume TrackBar)
        var lblMusic = CreateLabel(isArabic ? "موسيقى القوائم والخلفية:" : "Menu Music Volume:");
        _tbMusicVolume = CreateTrackBar(isArabic ? "موسيقى القوائم والخلفية" : "Menu Music Volume");
        _tbMusicVolume.ValueChanged += (_, _) =>
        {
            if (_isLoadingSettings) return;
            MatchSettings.Current.MusicVolumePercent = _tbMusicVolume.Value;
            SoundEffectsPlayer.MusicVolumeSetting = _tbMusicVolume.Value;
            SoundEffectsPlayer.ApplyMusicVolume();
        };
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
        _cmbDifficulty.Items.AddRange(isArabic ? ["سهل (Easy)", "متوسط (Normal)", "محترف (Hard)"] : ["Easy", "Normal", "Hard"]);
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

        // 10. المعلق الصوتي المعتمد (Commentary Voice ComboBox)
        var lblVoice = CreateLabel(isArabic ? "المعلق الصوتي المعتمد:" : "Commentary Voice:");
        _cmbCommentaryVoice = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Fill,
            AccessibleName = isArabic ? "المعلق الصوتي المعتمد" : "Commentary Voice",
            BackColor = Color.FromArgb(36, 42, 56),
            ForeColor = Color.White
        };
        _cmbCommentaryVoice.Items.AddRange(isArabic ? ["فارس عوض (Fares Awad)", "رؤوف خليف (Raouf Khlif)", "صامت (بدون تعليق)"] : ["Fares Awad", "Raouf Khlif", "Off (Silent)"]);
        mainPanel.Controls.Add(lblVoice, 0, row);
        mainPanel.Controls.Add(_cmbCommentaryVoice, 1, row++);

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
        _chkAutoSubs = CreateCheckBox(isArabic ? "التبديلات التلقائية الذكية للاعبين المجهدين (AI Auto-Substitutions)" : "AI Auto-Substitutions for Fatigued Players");
        mainPanel.Controls.Add(_chkAutoSubs, 0, row);
        mainPanel.SetColumnSpan(_chkAutoSubs, 2);
        row++;

        // 14. التعديلات التكتيكية التلقائية بالذكاء الاصطناعي (Auto-Tactics CheckBox)
        _chkAutoTactics = CreateCheckBox(isArabic ? "التعديلات التكتيكية التلقائية الذكية حسب مجريات وسير اللعب (AI Dynamic Tactics)" : "AI Dynamic Tactical Adjustments on Game Flow");
        mainPanel.Controls.Add(_chkAutoTactics, 0, row);
        mainPanel.SetColumnSpan(_chkAutoTactics, 2);
        row++;

        // 15. حكم الفيديو المساعد بالذكاء الاصطناعي (AI VAR CheckBox)
        _chkAiVar = CreateCheckBox(isArabic ? "تقنية حكم الفيديو المساعد بالذكاء الاصطناعي (AI VAR Referee)" : "AI VAR Referee Technology");
        mainPanel.Controls.Add(_chkAiVar, 0, row);
        mainPanel.SetColumnSpan(_chkAiVar, 2);
        row++;

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

            _cmbDifficulty.SelectedIndex = Math.Clamp((int)s.Difficulty, 0, 2);

            int durIdx = Array.IndexOf(_durations, s.MatchDurationMinutes);
            _cmbDuration.SelectedIndex = durIdx >= 0 ? durIdx : 1;

            _cmbCommentaryVoice.SelectedIndex = (s.CommentaryVoice == CommentaryVoiceOption.Off || !s.CommentaryEnabled) ? 2
                : (s.Commentator == CommentatorId.FaresAwad ? 0 : 1);

            _chkExtraTime.Checked = s.ExtraTimeEnabled;
            _chkPenalties.Checked = s.PenaltyShootoutEnabled;
            _chkAutoSubs.Checked = s.AutoSubstitutionsEnabled;
            _chkAutoTactics.Checked = s.AutoTacticalAdjustmentsEnabled;
            _chkAiVar.Checked = s.AiVarEnabled;
        }
        finally
        {
            _isLoadingSettings = false;
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        var selectedLang = _cmbLanguage.SelectedIndex == 1 ? UiLanguage.English : UiLanguage.Arabic;
        if (selectedLang != MatchSettings.Current.Language)
        {
            MatchSettings.Current.Language = selectedLang;
            _screenReader.Speak(selectedLang == UiLanguage.English ? "Language changed to English." : "تم تغيير لغة اللعبة إلى العربية.");
        }
    }

    private void OnResetDefaultsClicked(object? sender, EventArgs e)
    {
        MatchSettings.Current.ResetToDefaults();
        SoundEffectsPlayer.MasterVolumeSetting = MatchSettings.Current.MasterVolumePercent;
        SoundEffectsPlayer.SfxVolumeSetting = MatchSettings.Current.SfxVolumePercent;
        SoundEffectsPlayer.CommentaryVolumeSetting = MatchSettings.Current.CommentaryVolumePercent;
        SoundEffectsPlayer.CrowdVolumeSetting = MatchSettings.Current.CrowdVolumePercent;
        SoundEffectsPlayer.GoalBeaconVolumeSetting = MatchSettings.Current.GoalBeaconVolumePercent;
        SoundEffectsPlayer.MusicVolumeSetting = MatchSettings.Current.MusicVolumePercent;
        SoundEffectsPlayer.ApplyMusicVolume();

        LoadCurrentSettings();
        SoundEffectsPlayer.PlayMenuSelect();
        _screenReader.Speak(MatchSettings.Current.Language == UiLanguage.Arabic
            ? "تمت استعادة الإعدادات الافتراضية بنجاح"
            : "Settings successfully reset to defaults.");
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

        if (_cmbCommentaryVoice.SelectedIndex == 2)
        {
            s.CommentaryVoice = CommentaryVoiceOption.Off;
            s.CommentaryEnabled = false;
        }
        else if (_cmbCommentaryVoice.SelectedIndex == 1)
        {
            s.Commentator = CommentatorId.RaoufKhlif;
            s.CommentaryVoice = CommentaryVoiceOption.RaoufKhlif;
            s.CommentaryEnabled = true;
        }
        else
        {
            s.Commentator = CommentatorId.FaresAwad;
            s.CommentaryVoice = CommentaryVoiceOption.FaresAwad;
            s.CommentaryEnabled = true;
        }

        s.ExtraTimeEnabled = _chkExtraTime.Checked;
        s.PenaltyShootoutEnabled = _chkPenalties.Checked;
        s.AutoSubstitutionsEnabled = _chkAutoSubs.Checked;
        s.AutoTacticalAdjustmentsEnabled = _chkAutoTactics.Checked;
        s.AiVarEnabled = _chkAiVar.Checked;

        s.Save();
        SoundEffectsPlayer.PlayMenuSelect();
        _screenReader.Speak(s.Language == UiLanguage.Arabic
            ? "تم حفظ الإعدادات بنجاح في ملف settings.json"
            : "Settings successfully saved to settings.json.");

        Close();
    }
}
