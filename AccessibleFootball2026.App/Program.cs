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

        if (runOnlyTest)
        {
            AttachConsole(ATTACH_PARENT_PROCESS);
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            try { return RunFullAutomatedDemonstration(); }
            catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        }


        // Distribution owns all assets. Never search another PC's Desktop, Downloads, D drive or source tree.
        string baseAudioDir = Path.Combine(AppContext.BaseDirectory,"Assets","Audio");
        if(!File.Exists(Path.Combine(baseAudioDir,"Commentary","FaresAwad","commentary_context.json")) ||
            !File.Exists(Path.Combine(AppContext.BaseDirectory,"players_database.json")))
        {
            MessageBox.Show("ملفات اللعبة المرفقة غير مكتملة. شغّل ملف اللعبة الكامل بدلاً من نسخ ملف التشغيل الداخلي وحده.\nGame content is incomplete. Run the full game package, not a copied internal executable.","AccessibleFootball2026");
            return 2;
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

        var nativeScreenReader = new NvdaScreenReader();
        var eventBus = new MatchEventBus();
        var inputHandler = new KeyboardInputHandler();
        var commentaryEngine = new CommentaryEngine(MatchSettings.Current.Commentator, baseAudioDir);
        var screenReader = new PriorityScreenReader(nativeScreenReader, () => commentaryEngine.IsRecordedChannelBusy, commentaryEngine.SpeechGate);
        commentaryEngine.IsReaderSpeaking = () => screenReader.IsSpeaking;
        commentaryEngine.OnUrgentInterruption += screenReader.CancelSpeech;
        var coordinator = new SmartAudioCoordinator(screenReader, () => commentaryEngine.IsIdle);
        var anthemEngine = new AnthemEngine(screenReader, Path.Combine(baseAudioDir, "Anthems"));


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

    private static int RunFullAutomatedDemonstration()
    {
        Console.WriteLine("=== Accessible Football 2026/2027 Roster and Draft Diagnostics ===");

        Console.WriteLine("\n[الدوريات والبطولات الموجودة في قاعدة البيانات المحلية]:");
        var tourneys = TournamentDatabase.GetAllTournaments();
        foreach (var tourney in tourneys)
        {
            Console.WriteLine($"  * [{tourney.Name}] ({tourney.Region}) - تضم: {string.Join("، ", tourney.Teams.Select(t => t.Name))}");
        }

        Console.WriteLine("\n[عينات من تشكيلات قاعدة البيانات المحلية]:");
        var ahly = TeamDatabase.CreateAlAhly();
        Console.WriteLine($"الأهلي: هجوم ({ahly.Players[9].Name})، وسط ({ahly.Players[6].Name} و {ahly.Players[5].Name})");

        var zamalek = TeamDatabase.CreateZamalek();
        Console.WriteLine($"الزمالك: هجوم ({zamalek.Players[9].Name})، وسط ({zamalek.Players[7].Name} و {zamalek.Players[6].Name})");

        var pyramids = TeamDatabase.CreatePyramids();
        Console.WriteLine($"بيراميدز: صانع ألعاب ({pyramids.Players[7].Name} #{pyramids.Players[7].ShirtNumber})، جناح ({pyramids.Players[8].Name} #{pyramids.Players[8].ShirtNumber})");

        var real = TeamDatabase.CreateRealMadrid();
        Console.WriteLine($"ريال مدريد: هجوم ({real.Players[9].Name})، أجنحة ({real.Players[7].Name} و {real.Players[8].Name})");

        var barca = TeamDatabase.CreateBarcelona();
        Console.WriteLine($"برشلونة: هجوم ({barca.Players[9].Name})، أجنحة ({barca.Players[7].Name} و {barca.Players[8].Name})");

        var liverpool = TeamDatabase.CreateLiverpool();
        Console.WriteLine($"ليفربول: هجوم ({liverpool.Players[7].Name} و {liverpool.Players[9].Name})");

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

        Console.WriteLine($"عدد لاعبي فريقك {dream.UserDreamTeam.Players.Count}: {string.Join("، ", dream.UserDreamTeam.Players.Take(4).Select(p => p.Name))}");
        Console.WriteLine($"عدد لاعبي الخصم {dream.CpuDreamTeam.Players.Count}: {string.Join("، ", dream.CpuDreamTeam.Players.Take(4).Select(p => p.Name))}");

        bool overlap = dream.UserDreamTeam.Players.Concat(dream.UserDreamTeam.Bench)
            .Any(u => dream.CpuDreamTeam.Players.Concat(dream.CpuDreamTeam.Bench).Any(c => c.Id == u.Id));
        Console.WriteLine($"فحص مانع التكرار الصوتي بين الفريقين: {(overlap ? "فشل" : "نجاح بنسبة 100% بدون أي لاعب مكرر! ✅")}");

        bool squadsComplete = dream.UserDreamTeam.Players.Count == 11 && dream.CpuDreamTeam.Players.Count == 11;
        Console.WriteLine($"Draft squad sizes: {(squadsComplete ? "PASS" : "FAIL")}");
        Console.WriteLine("Roster checks above did not test Windows UI, NVDA or sound playback. Engine smoke checks follow.");
        Console.WriteLine("Run tests/MatchFlowRegression for portable match-flow assertions; run AccessibleFootball2026.Tests on Windows for UI tests.");
        int simulationExit = SimulationDiagnostics.Run(Console.Out);
        return overlap || !squadsComplete || simulationExit != 0 ? 1 : 0;
    }
}
