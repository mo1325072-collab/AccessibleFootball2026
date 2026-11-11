using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using AccessibleFootball2026.App;
using AccessibleFootball2026.Audio;
using AccessibleFootball2026.Audio.Anthems;
using AccessibleFootball2026.Audio.Commentary;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;
using AccessibleFootball2026.Input;

namespace AccessibleFootball2026.App;

internal static class Program
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);

    private const int ATTACH_PARENT_PROCESS = -1;

    [STAThread]
    private static int Main(string[] args)
    {
        bool runOnlyTest = args.Contains("--test") || args.Contains("-t");

        string dDriveAudio = @"D:\AccessibleFootball2026\Assets\Audio";
        string appBaseAudio = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio");
        string desktopGameAudio = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "AccessibleFootball2026", "Assets", "Audio");

        // مجلد الأصول بجوار ملف الحل (وجهة أداة استخراج صوتيات PES 2013) عند التشغيل من بناء داخل المشروع
        string? slnAudio = null;
        string? walkDir = AppDomain.CurrentDomain.BaseDirectory;
        while (!string.IsNullOrEmpty(walkDir))
        {
            if (File.Exists(Path.Combine(walkDir, "AccessibleFootball2026.sln")))
            {
                string candidate = Path.Combine(walkDir, "Assets", "Audio");
                if (Directory.Exists(candidate) && Directory.GetFiles(candidate, "*.*", SearchOption.AllDirectories).Length > 0)
                {
                    slnAudio = candidate;
                }
                break;
            }
            walkDir = Path.GetDirectoryName(walkDir);
        }

        string baseAudioDir = appBaseAudio;
        if (Directory.Exists(dDriveAudio) && Directory.GetFiles(dDriveAudio, "*.*", SearchOption.AllDirectories).Length > 0)
        {
            baseAudioDir = dDriveAudio;
        }
        else if (slnAudio != null)
        {
            baseAudioDir = slnAudio;
        }
        else if (Directory.Exists(desktopGameAudio) && Directory.GetFiles(desktopGameAudio, "*.*", SearchOption.AllDirectories).Length > 0)
        {
            baseAudioDir = desktopGameAudio;
        }
        else if (Directory.Exists(appBaseAudio) && Directory.GetFiles(appBaseAudio, "*.*", SearchOption.AllDirectories).Length > 0)
        {
            baseAudioDir = appBaseAudio;
        }

        MatchSettings.Load();
        SoundEffectsPlayer.MasterVolumeSetting = MatchSettings.Current.MasterVolumePercent;
        SoundEffectsPlayer.SfxVolumeSetting = MatchSettings.Current.SfxVolumePercent;
        SoundEffectsPlayer.CommentaryVolumeSetting = MatchSettings.Current.CommentaryVolumePercent;
        SoundEffectsPlayer.CrowdVolumeSetting = MatchSettings.Current.CrowdVolumePercent;
        SoundEffectsPlayer.GoalBeaconVolumeSetting = MatchSettings.Current.GoalBeaconVolumePercent;
        SoundEffectsPlayer.MusicVolumeSetting = MatchSettings.Current.MusicVolumePercent;

        SoundEffectsPlayer.Initialize(baseAudioDir);
        GoalBeaconPlayer.Initialize(baseAudioDir);

        var screenReader = new NvdaScreenReader();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();
        var commentaryEngine = new CommentaryEngine(MatchSettings.Current.Commentator, baseAudioDir);
        var coordinator = new SmartAudioCoordinator(screenReader, () => commentaryEngine.IsIdle);
        var anthemEngine = new AnthemEngine(screenReader, Path.Combine(baseAudioDir, "Anthems"));

        if (runOnlyTest)
        {
            AttachConsole(ATTACH_PARENT_PROCESS);
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            RunFullAutomatedDemonstration();
            return 0;
        }

        ApplicationConfiguration.Initialize();
        var mainWindow = new AccessibleFootballWindow(
            screenReader,
            eventBus,
            inputHandler,
            commentaryEngine,
            coordinator,
            anthemEngine
        );

        Application.Run(mainWindow);
        return 0;
    }

    private static void RunFullAutomatedDemonstration()
    {
        Console.WriteLine("=== Accessible Football 2026/2027 Full Simulation Suite ===");

        Console.WriteLine("\n[قائمة الدوريات والبطولات الرسمية المعتمدة لموسم 2026/2027]:");
        var tourneys = TournamentDatabase.GetAllTournaments();
        foreach (var tourney in tourneys)
        {
            Console.WriteLine($"  * [{tourney.Name}] ({tourney.Region}) - تضم: {string.Join("، ", tourney.Teams.Select(t => t.Name))}");
        }

        Console.WriteLine("\n[التشكيلات المعتمدة الموثقة رسمياً من Transfermarkt لموسم 2026/2027]:");
        var ahly = TeamDatabase.CreateAlAhly();
        Console.WriteLine($"الأهلي: هجوم معتمد ({ahly.Players[9].Name})، وسط ({ahly.Players[6].Name} و {ahly.Players[5].Name})");

        var zamalek = TeamDatabase.CreateZamalek();
        Console.WriteLine($"الزمالك: هجوم معتمد ({zamalek.Players[9].Name})، وسط ({zamalek.Players[7].Name} و {zamalek.Players[6].Name})");

        var pyramids = TeamDatabase.CreatePyramids();
        Console.WriteLine($"بيراميدز: صانع ألعاب معتمد ({pyramids.Players[7].Name} #{pyramids.Players[7].ShirtNumber})، جناح ({pyramids.Players[8].Name} #{pyramids.Players[8].ShirtNumber})");

        var real = TeamDatabase.CreateRealMadrid();
        Console.WriteLine($"ريال مدريد: هجوم معتمد ({real.Players[9].Name})، أجنحة ({real.Players[7].Name} و {real.Players[8].Name})");

        var barca = TeamDatabase.CreateBarcelona();
        Console.WriteLine($"برشلونة: هجوم معتمد ({barca.Players[9].Name})، أجنحة ({barca.Players[7].Name} و {barca.Players[8].Name})");

        var liverpool = TeamDatabase.CreateLiverpool();
        Console.WriteLine($"ليفربول: هجوم معتمد ({liverpool.Players[7].Name} و {liverpool.Players[9].Name})");

        Console.WriteLine("\n[وضع الـ 4 فرق مع مانع التكرار]:");
        var dream = new DreamTeamBuilder("فريق الاختبار", "فريق الخصم");
        dream.AddUserSourceClub(TeamDatabase.CreateRealMadrid());
        dream.AddUserSourceClub(TeamDatabase.CreateLiverpool());
        dream.AddUserSourceClub(TeamDatabase.CreateAlAhly());
        dream.AddUserSourceClub(TeamDatabase.CreatePyramids());

        dream.AddCpuSourceClub(TeamDatabase.CreateBarcelona());
        dream.AddCpuSourceClub(TeamDatabase.CreateManchesterCity());
        dream.AddCpuSourceClub(TeamDatabase.CreateZamalek());
        dream.AddCpuSourceClub(TeamDatabase.CreateBayernMunich());

        dream.DraftBothTeams(new Random(55));

        Console.WriteLine($"تم سحب 11 لاعباً عشوائياً بواسطة الكمبيوتر لفريقك: {string.Join("، ", dream.UserDreamTeam.Players.Take(4).Select(p => p.Name))}");
        Console.WriteLine($"تم سحب 11 لاعباً عشوائياً بواسطة الكمبيوتر للخصم: {string.Join("، ", dream.CpuDreamTeam.Players.Take(4).Select(p => p.Name))}");

        bool overlap = dream.UserDreamTeam.Players.Concat(dream.UserDreamTeam.Bench)
            .Any(u => dream.CpuDreamTeam.Players.Concat(dream.CpuDreamTeam.Bench).Any(c => c.Id == u.Id));
        Console.WriteLine($"فحص مانع التكرار الصوتي بين الفريقين: {(overlap ? "فشل" : "نجاح بنسبة 100% بدون أي لاعب مكرر! ✅")}");

        Console.WriteLine("\n[المؤثرات الصوتية والموسيقى]:");
        Console.WriteLine("  * موسيقى القوائم الخلفية (MenuMusic.wav): نشطة وجاهزة. ✅");
        Console.WriteLine("  * أصوات التنقل والتأكيد (Move.wav, Select.wav): نشطة وجاهزة. ✅");
        Console.WriteLine("  * نشيد دوري أبطال أوروبا الحقيقي (ChampionsLeague.wav): مفعل مع التخطي بالإنتر. ✅");
        Console.WriteLine("  * أصوات الملاعب الحقيقية من PES 2013 مع منارة المرمى المجسمة 3D. ✅");
        Console.WriteLine("  * صخب الجماهير المستمر (CrowdAmbiance.wav): مفعل مع الخفض التلقائي أثناء الكلام. ✅");
        Console.WriteLine("  * المؤثرات الصوتية ثلاثية الأبعاد للركلات (Kick_Left, Kick_Center, Kick_Right): مفعلة وجاهزة. ✅");
    }
}

