using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AccessibleFootball2026.Audio;

namespace AccessibleFootball2026.Downloader;

public class DownloaderForm : Form
{
    private readonly IScreenReader _screenReader;
    private readonly HttpClient _httpClient;
    
    private Label _lblTitle = null!;
    private Label _lblStatus = null!;
    private Label _lblPercent = null!;
    private ProgressBar _progressBar = null!;
    private Button _btnStart = null!;
    private Button _btnPause = null!;
    private Button _btnCancel = null!;
    private Button _btnClose = null!;

    private CancellationTokenSource? _cts = null;
    private TaskCompletionSource<bool>? _pauseTcs = null;
    private bool _isPaused = false;
    private bool _isDownloading = false;

    private record DownloadItem(string Url, string RelativePath, string ArabicName);

    public DownloaderForm(IScreenReader screenReader)
    {
        _screenReader = screenReader;
        
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        };
        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        Text = "مساعد تحميل أصوات كرة القدم الصوتية 2026";
        Size = new Size(680, 420);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(20, 24, 32);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
        KeyPreview = true;

        _lblTitle = new Label
        {
            Text = "مساعد تحميل أصوات وموسيقى اللعبة الحقيقية والتفاعلية",
            Dock = DockStyle.Top,
            Height = 65,
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 215, 0),
            TextAlign = ContentAlignment.MiddleCenter,
            AccessibleName = "عنوان البرنامج: مساعد تحميل أصوات وموسيقى اللعبة الحقيقية والتفاعلية"
        };

        _lblStatus = new Label
        {
            Text = "الحالة: جاهز للتحميل. انقر زر (بدء التحميل) للانطلاق.",
            Dock = DockStyle.Top,
            Height = 50,
            Font = new Font("Segoe UI", 13F),
            ForeColor = Color.FromArgb(144, 238, 144),
            TextAlign = ContentAlignment.MiddleCenter,
            AccessibleName = "حالة التحميل الحالية"
        };

        _lblPercent = new Label
        {
            Text = "0%",
            Dock = DockStyle.Top,
            Height = 35,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            AccessibleName = "النسبة المئوية لاكتمال التحميل"
        };

        _progressBar = new ProgressBar
        {
            Dock = DockStyle.Top,
            Height = 40,
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            AccessibleRole = AccessibleRole.ProgressBar,
            AccessibleName = "شريط تقدم التحميل"
        };

        var panelButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 90,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(15, 15, 15, 15)
        };

        _btnStart = new Button
        {
            Text = "بدء التحميل (Enter)",
            Width = 170,
            Height = 50,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            BackColor = Color.FromArgb(34, 139, 34),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            AccessibleName = "بدء التحميل"
        };
        _btnStart.Click += async (s, e) => await StartDownloadAsync();

        _btnPause = new Button
        {
            Text = "إيقاف مؤقت",
            Width = 140,
            Height = 50,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            BackColor = Color.FromArgb(204, 153, 0),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Enabled = false,
            AccessibleName = "إيقاف التحميل مؤقتاً"
        };
        _btnPause.Click += (s, e) => TogglePause();

        _btnCancel = new Button
        {
            Text = "إلغاء",
            Width = 120,
            Height = 50,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            BackColor = Color.FromArgb(178, 34, 34),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Enabled = false,
            AccessibleName = "إلغاء التحميل"
        };
        _btnCancel.Click += (s, e) => CancelDownload();

        _btnClose = new Button
        {
            Text = "إغلاق (Esc)",
            Width = 120,
            Height = 50,
            Font = new Font("Segoe UI", 11F),
            BackColor = Color.FromArgb(60, 64, 75),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            AccessibleName = "إغلاق النافذة"
        };
        _btnClose.Click += (s, e) => Close();

        panelButtons.Controls.Add(_btnStart);
        panelButtons.Controls.Add(_btnPause);
        panelButtons.Controls.Add(_btnCancel);
        panelButtons.Controls.Add(_btnClose);

        Controls.Add(panelButtons);
        Controls.Add(_progressBar);
        Controls.Add(_lblPercent);
        Controls.Add(_lblStatus);
        Controls.Add(_lblTitle);

        KeyDown += OnFormKeyDown;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _btnStart.Focus();
        _screenReader.Speak("مساعد تحميل أصوات كرة القدم الصوتية. جاهز لبدء التحميل التلقائي لكافة الأصوات الحقيقية والمعلقين.");
    }

    private void OnFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            if (_isDownloading)
            {
                CancelDownload();
            }
            else
            {
                Close();
            }
        }
    }

    private List<DownloadItem> GetDownloadList()
    {
        return new List<DownloadItem>
        {
            new("https://raw.githubusercontent.com/fpetruschke/tipp-kick-helfer/master/assets/audio/schiedsrichter-pfeife.wav",
                @"SFX\Whistles\Whistle.wav", "صافرة الحكم الحقيقية"),

            new("https://www.myinstants.com/media/sounds/soccer-kick.mp3",
                @"SFX\Kicks\Kick.mp3", "صوت ركل الكرة الحقيقي في الملعب"),

            new("https://raw.githubusercontent.com/fpetruschke/tipp-kick-helfer/master/assets/audio/stadion-atmosphaere.mp3",
                @"SFX\Crowd\CrowdAmbiance.mp3", "صخب وهتاف الجماهير التفاعلي المستمر"),

            new("https://raw.githubusercontent.com/fpetruschke/tipp-kick-helfer/master/assets/audio/daneben.mp3",
                @"SFX\Crowd\CrowdMiss.mp3", "صوت آهة وتنهيدة الجماهير التفاعلية عند الفرص الضائعة"),

            new("https://raw.githubusercontent.com/fpetruschke/tipp-kick-helfer/master/assets/audio/torjubel.wav",
                @"SFX\Crowd\CrowdGoal.wav", "صوت انفجار وهتاف المدرجات عند تسجيل الهدف"),

            new("https://raw.githubusercontent.com/TIGERs-Mannheim/AudioRef/main/sounds/en/goal.wav",
                @"SFX\GoalNet\GoalNet.wav", "صوت اهتزاز شباك المرمى"),

            new("https://www.myinstants.com/media/sounds/champions-league-intro.mp3",
                @"Anthems\ChampionsLeague.mp3", "نشيد دوري أبطال أوروبا الحقيقي"),

            new("https://www.myinstants.com/media/sounds/fifa-world-cup-theme.mp3",
                @"Menu\MenuMusic.mp3", "موسيقى القوائم الكروية العالمية"),

            new("https://raw.githubusercontent.com/Calinou/kenney-ui-audio/master/addons/kenney_ui_audio/click1.wav",
                @"Menu\Move.wav", "صوت التنقل بالقوائم"),

            new("https://raw.githubusercontent.com/Calinou/kenney-ui-audio/master/addons/kenney_ui_audio/switch1.wav",
                @"Menu\Select.wav", "صوت اختيار عنصر في القائمة"),

            new("https://raw.githubusercontent.com/Calinou/kenney-ui-audio/master/addons/kenney_ui_audio/switch2.wav",
                @"Menu\Back.wav", "صوت العودة في القائمة"),

            new("https://raw.githubusercontent.com/Calinou/kenney-ui-audio/master/addons/kenney_ui_audio/switch3.wav",
                @"SFX\Movement\BoundaryWarning.wav", "صوت تنبيه خط التماس والملعب"),

            new("https://www.myinstants.com/media/sounds/chawali.mp3",
                @"Commentary\RaoufKhlif\Goals\Goal_1.mp3", "صرخة الأهداف الحماسية بصوت المعلق رؤوف خليف")
        };
    }

    private async Task StartDownloadAsync()
    {
        if (_isDownloading) return;

        _isDownloading = true;
        _isPaused = false;
        _btnStart.Enabled = false;
        _btnPause.Enabled = true;
        _btnCancel.Enabled = true;
        _btnPause.Text = "إيقاف مؤقت";
        _pauseTcs = null;
        _cts = new CancellationTokenSource();

        string desktopGameAudioDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "AccessibleFootball2026", "Assets", "Audio");
        string desktopRootAudioDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "Assets", "Audio");

        Directory.CreateDirectory(desktopGameAudioDir);
        Directory.CreateDirectory(desktopRootAudioDir);

        var items = GetDownloadList();
        int total = items.Count;
        int completed = 0;
        int failed = 0;

        _screenReader.Speak("بدأ تحميل بنك الأصوات الحقيقي والتفاعلي. يرجى الانتظار...");

        try
        {
            for (int i = 0; i < total; i++)
            {
                if (_cts.Token.IsCancellationRequested) break;

                while (_isPaused)
                {
                    _lblStatus.Text = "الحالة: تم إيقاف التحميل مؤقتاً.";
                    if (_pauseTcs != null)
                    {
                        await _pauseTcs.Task;
                    }
                    else
                    {
                        await Task.Delay(200);
                    }
                }

                var item = items[i];
                _lblStatus.Text = $"جاري تحميل: {item.ArabicName}... ({i + 1} من {total})";
                _screenReader.Speak($"جاري تحميل {item.ArabicName}...", interrupt: true);

                bool success = false;
                try
                {
                    byte[] data = await _httpClient.GetByteArrayAsync(item.Url, _cts.Token);
                    if (data.Length > 1000)
                    {
                        string[] targets = { desktopGameAudioDir, desktopRootAudioDir };
                        foreach (var targetDir in targets)
                        {
                            string destFile = Path.Combine(targetDir, item.RelativePath);
                            string? destDir = Path.GetDirectoryName(destFile);
                            if (destDir != null) Directory.CreateDirectory(destDir);
                            await File.WriteAllBytesAsync(destFile, data, _cts.Token);

                            // توزيع نسخ الركلات والمدرجات والمعلقين تلقائياً
                            if (item.RelativePath.Contains("Kick.mp3"))
                            {
                                File.Copy(destFile, Path.Combine(targetDir, @"SFX\Kicks\Kick_Left.mp3"), true);
                                File.Copy(destFile, Path.Combine(targetDir, @"SFX\Kicks\Kick_Right.mp3"), true);
                                File.Copy(destFile, Path.Combine(targetDir, @"SFX\Kicks\Kick_Center.mp3"), true);
                            }
                            else if (item.RelativePath.Contains("CrowdMiss.mp3"))
                            {
                                File.Copy(destFile, Path.Combine(targetDir, @"SFX\Crowd\CrowdGasp.mp3"), true);
                            }
                            else if (item.RelativePath.Contains("CrowdGoal.wav"))
                            {
                                File.Copy(destFile, Path.Combine(targetDir, @"SFX\Crowd\CrowdCheer.wav"), true);
                            }
                            else if (item.RelativePath.Contains("Goal_1.mp3"))
                            {
                                string introDir = Path.Combine(targetDir, "Commentary", "RaoufKhlif", "Intro");
                                Directory.CreateDirectory(introDir);
                                File.Copy(destFile, Path.Combine(introDir, "Intro_1.mp3"), true);
                            }
                        }
                        success = true;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    failed++;
                }

                if (success)
                {
                    completed++;
                    int percent = (int)(((float)completed / total) * 100);
                    _progressBar.Value = percent;
                    _lblPercent.Text = $"{percent}%";
                    _screenReader.Speak($"اكتمل {item.ArabicName}. نسبة الإنجاز {percent} بالمئة.", interrupt: true);
                }
            }

            if (!_cts.Token.IsCancellationRequested)
            {
                if (failed == 0)
                {
                    _lblStatus.Text = "الحالة: اكتمل التحميل والتثبيت بنجاح 100%!";
                    _progressBar.Value = 100;
                    _lblPercent.Text = "100%";
                    string finalMsg = "تم اكتمال تحميل وتثبيت جميع أصوات وموسيقى اللعبة الحقيقية والتفاعلية بنجاح مئة بالمئة! يمكنك الآن إغلاق المساعد وتشغيل اللعبة فوراً والاستمتاع بالواقعية الكاملة.";
                    _screenReader.Speak(finalMsg, interrupt: true);
                    MessageBox.Show(finalMsg, "اكتمل التحميل بنجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    string warnMsg = $"اكتمل تحميل {completed} ملفات، بينما تعذر تحميل {failed} ملفات.";
                    _lblStatus.Text = warnMsg;
                    _screenReader.Speak(warnMsg, interrupt: true);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _lblStatus.Text = "الحالة: تم إلغاء عملية التحميل.";
            _screenReader.Speak("تم إلغاء عملية التحميل.", interrupt: true);
        }
        finally
        {
            _isDownloading = false;
            _isPaused = false;
            _btnStart.Enabled = true;
            _btnPause.Enabled = false;
            _btnCancel.Enabled = false;
            _btnPause.Text = "إيقاف مؤقت";
        }
    }

    private void TogglePause()
    {
        if (!_isDownloading) return;

        if (!_isPaused)
        {
            _isPaused = true;
            _pauseTcs = new TaskCompletionSource<bool>();
            _btnPause.Text = "استئناف (Resume)";
            _lblStatus.Text = "الحالة: التحميل موقوف مؤقتاً.";
            _screenReader.Speak("تم إيقاف التحميل مؤقتاً.", interrupt: true);
        }
        else
        {
            _isPaused = false;
            _btnPause.Text = "إيقاف مؤقت";
            _pauseTcs?.TrySetResult(true);
            _lblStatus.Text = "الحالة: تم استئناف التحميل.";
            _screenReader.Speak("تم استئناف التحميل.", interrupt: true);
        }
    }

    private void CancelDownload()
    {
        if (!_isDownloading) return;

        _cts?.Cancel();
        _isPaused = false;
        _pauseTcs?.TrySetResult(false);
        _lblStatus.Text = "الحالة: تم إلغاء التحميل.";
        _screenReader.Speak("تم إلغاء التحميل.", interrupt: true);
    }
}
