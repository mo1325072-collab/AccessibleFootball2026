using System.Numerics;
using System.Reflection;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Input;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;
using AccessibleFootball2026.Audio;
using AccessibleFootball2026.Audio.Commentary;
using AccessibleFootball2026.App.Menus;

MatchSettings.Current.ExtraTimeEnabled = false;
MatchSettings.Current.PenaltyShootoutEnabled = false;
int passed = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception(name); Console.WriteLine("PASS " + name); passed++; }
object? Invoke(object obj, string name, params object?[] args) => obj.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(obj, args);
void Set(object obj, string name, object value) => obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(obj, value);
Team Make(string id, TeamSide side)
{
    var team = new Team(id, id, side);
    for (int i = 0; i < 11; i++) team.Players.Add(new Player((side == TeamSide.Home ? 0 : 100) + i, id + i, i + 1,
        i == 0 ? SpecificPosition.GK : i < 5 ? SpecificPosition.CB : i < 8 ? SpecificPosition.CM : SpecificPosition.ST, 75, 75, 75, 75, 75));
    team.AssignDefaultRoles();
    return team;
}
MatchEngine New() => new(Make("home", TeamSide.Home), Make("away", TeamSide.Away), new MatchEventBus(), new Input());
var m = New();
m.SetStoppageMinutes(0, 0); m.SetMatchTimeMinutes(44.99f); m.Update(.2f);
Check(m.State == MatchState.HalfTime && m.HalfTimeReached, "first half reaches real interval");
Check(!m.UserAttackingRight && m.HomeTeam.MatchAttackingRight == false, "ends swapped");
Check(m.HomeTeam.Players[0].HomePosition.X > 90 && m.AwayTeam.Players[0].HomePosition.X < 10, "keepers defend switched goals");
Check(m.AwayTeam.Players.Contains(m.Ball.Owner!), "other team takes second kickoff");
var time = m.MatchTimeMinutes; m.Update(120);
Check(m.MatchTimeMinutes == time, "interval freezes simulation clock");
m.HomeTeam.ChangeFormation(FormationType.Formation442_Flat);
Check(m.HomeTeam.Players[0].HomePosition.X > 90, "changing formation cannot undo side swap");
m.TriggerHalfTime(); Check(!m.UserAttackingRight, "halftime trigger is idempotent");
var reader = new Reader(); using var commentary = new CommentaryEngine(baseDir: Path.GetTempPath());
AccessibleMenu? opened = null;
var halfMenu = GamePlanMenuBuilder.CreatePauseMenu(m, reader, commentary, x => opened = x, () => {}, () => m.ResumeMatch(), () => {});
Check(halfMenu.GetItem(0).Title == "بدء الشوط الثاني", "interval alone offers StartSecondHalf");
halfMenu.SelectCurrent(); Check(m.State == MatchState.InPlay, "explicit interval action starts second half");
m.PauseMatch();
var pause = GamePlanMenuBuilder.CreatePauseMenu(m, reader, commentary, x => opened = x, () => {}, () => m.ResumeMatch(), () => {});
Check(pause.GetItem(0).Title == "استئناف المباراة", "ordinary pause never offers StartSecondHalf");
int statIndex = Enumerable.Range(0, pause.Count).First(i => pause.GetItem(i).Title == "إحصائيات المباراة");
while (pause.GetCurrentIndex() != statIndex) pause.NavigateNext();
pause.SelectCurrent();
Check(opened != null && opened.Count == 26, "statistics are a 25-row menu plus Back");
opened!.Open(); opened.NavigateNext(); opened.NavigateNext();
Check(opened.GetCurrentIndex() == 2 && reader.Spoken.Last().Contains("home") && reader.Spoken.Last().Contains("away"), "DownArrow selects both-team formation item");
m.ResumeMatch(); m.SetMatchTimeMinutes(89.99f); m.Update(.2f);
Check(m.State == MatchState.GameOver, "second half ends at regulation threshold");

m = New();
var victim = m.HomeTeam.Players[9]; victim.Position2D = new Vector2(70, 34);
m.AwardFoulRestart(victim);
Check(m.Restart?.Team == m.HomeTeam && m.AwaitingStoppageConfirmation, "free kick is held with actual team and taker");
time = m.MatchTimeMinutes; var ball = m.Ball.Position;
m.Update(100);
Check(m.MatchTimeMinutes == time && m.Ball.Position == ball, "held foul cannot advance clock or ball");
Check(m.StatisticsFor(m.HomeTeam).FreeKicks == 1 && m.StatisticsFor(m.AwayTeam).FreeKicks == 0, "free kicks attributed to awarded team");
Check(m.DismissStoppageReplay() && m.Restart != null, "Enter dismisses hold without taking the free kick");
victim.Position2D = new Vector2(100, 34); m.AwardFoulRestart(victim);
Check(m.StatisticsFor(m.HomeTeam).Penalties == 1, "penalty measured from real incident geometry");
m.PauseMatch(); m.ResumeMatch(); Check(m.AwaitingStoppageConfirmation, "pause/resume does not bypass held stoppage");
m.DismissStoppageReplay();
Invoke(m, "AwardCornerKick", false);
Check(m.Restart?.Team == m.AwayTeam && m.StatisticsFor(m.AwayTeam).Corners == 1, "away corner correctly attributed");
m.DismissStoppageReplay(); Invoke(m, "AwardThrowIn", false);
Check(m.Restart?.Kind == RestartKind.ThrowIn && m.Ball.Owner != null, "AI throw not taken before Enter");
m.DismissStoppageReplay(); Invoke(m, "AwardGoalKick", true);
Check(m.StatisticsFor(m.HomeTeam).GoalKicks == 1 && m.Restart?.Taker == m.HomeTeam.Players[0], "goal kick records actual keeper");

m = New();
void Event(MatchEvent e, params (string, object?)[] facts) => Invoke(m, "Publish", e, facts);
Event(new MatchEvent(MatchEventType.ShotTaken, "shot", subjectPlayer: m.HomeTeam.Players[9]));
Event(new MatchEvent(MatchEventType.GoalkeeperSave, "save", subjectPlayer: m.AwayTeam.Players[0]));
Event(new MatchEvent(MatchEventType.GoalkeeperSave, "save", subjectPlayer: m.AwayTeam.Players[0]));
Check(m.StatisticsFor(m.HomeTeam).Shots == 1 && m.StatisticsFor(m.HomeTeam).ShotsOnTarget == 1, "shot on target counted once across repeated outcomes");
Event(new MatchEvent(MatchEventType.YellowCard, "yellow", subjectPlayer: m.HomeTeam.Players[2]));
Event(new MatchEvent(MatchEventType.RedCard, "second yellow", subjectPlayer: m.HomeTeam.Players[2]), ("card.kind", "secondYellow"));
Check(m.StatisticsFor(m.HomeTeam).YellowCards == 2 && m.StatisticsFor(m.HomeTeam).RedCards == 1, "second yellow counts both card types");
Invoke(m, "BeginCommentaryPass", m.HomeTeam.Players[9], m.HomeTeam.Players[8], new Vector2(50, 30), "short", "ground", false);
Invoke(m, "ResolveCommentaryPass", m.HomeTeam.Players[8]);
Check(m.StatisticsFor(m.HomeTeam).Passes == 1 && m.StatisticsFor(m.HomeTeam).CompletedPasses == 1, "pass stats attributed to sender not receiver");
Check(m.GetStatisticsAnnouncements().Any(x => x.StartsWith("الاستحواذ") && x.Contains("لم يسجل")), "no invented 50/50 before any measured possession");
Invoke(m, "TrackPlayFrame", 3f);
Check(m.GetStatisticsAnnouncements().Any(x => x.StartsWith("الاستحواذ") && x.Contains("100")), "possession derived from timed ownership");
Set(m, "_isShotInProgress", false); m.Ball.Position = new Vector3(Pitch.Length + .2f, Pitch.Width / 2, .5f);
Invoke(m, "CheckRefereeRules");
Check(m.State == MatchState.GoalCelebration && m.AwaitingStoppageConfirmation, "goal enters held celebration");
m.Update(100); Check(m.State == MatchState.GoalCelebration, "goal never auto-restarts before Enter");
m.PauseMatch(); m.ResumeMatch(); Check(m.State == MatchState.GoalCelebration, "pause preserves celebration state");
m.DismissStoppageReplay(); Check(m.State == MatchState.InPlay, "Enter releases goal and places actual kickoff");

bool busy = true; var actual = new Reader(); using var priority = new PriorityScreenReader(actual, () => busy);
priority.Speak("old one"); priority.Speak("old two"); priority.Speak("latest");
await Task.Delay(120); Check(actual.Spoken.Count == 0, "reader waits while commentary busy");
busy = false; await Task.Delay(150);
Check(actual.Spoken.SequenceEqual(new[] { "latest" }), "reader drops stale backlog and speaks latest only");
var blocking = new BlockingReader(); using var cancellable = new PriorityScreenReader(blocking, () => false);
cancellable.Speak("long"); await Task.Delay(30); cancellable.CancelSpeech(); await Task.Delay(50);
Check(!cancellable.IsSpeaking && blocking.CancelCount == 1, "urgent interruption releases reader channel immediately");
using var urgentEngine = new CommentaryEngine(baseDir: Path.GetTempPath()); int interruptions = 0;
urgentEngine.OnUrgentInterruption += () => interruptions++;
urgentEngine.TryCommentOnEvent(new MatchEvent(MatchEventType.HalfTime, "half"));
urgentEngine.TryCommentOnEvent(new MatchEvent(MatchEventType.RedCard, "red"));
Check(interruptions == 0, "halftime and cards cannot preempt commentary");
urgentEngine.TryCommentOnEvent(new MatchEvent(MatchEventType.GoalScored, "goal"));
urgentEngine.TryCommentOnEvent(new MatchEvent(MatchEventType.PenaltyAwarded, "penalty"));
Check(interruptions == 2, "only goal and penalty signal urgent preemption");

// Law 3 opportunity accounting and real automatic call timing.
void AddBench(Team t, int count = 8)
{
    for (int i = 0; i < count; i++) t.Bench.Add(new Player(500 + i, "bench" + i, 20 + i, SpecificPosition.ST, 80, 80, 80, 80, 80));
}
m = New(); AddBench(m.HomeTeam); m.PauseMatch();
Check(m.HomeTeam.StoppageWindowsUsed == 0, "opening pause or tactics alone spends no substitution opportunity");
var out1 = m.HomeTeam.Players[9]; var in1 = m.HomeTeam.Bench[0];
Check(m.MakeSubstitution(m.HomeTeam, out1, in1) && m.Ball.Owner == in1, "ball carrier substitution transfers ball ownership");
Check(m.HomeTeam.StoppageWindowsUsed == 1, "first confirmed substitution consumes one opportunity");
Check(!m.MakeSubstitution(m.HomeTeam, m.HomeTeam.Players[8], out1), "substituted-off player cannot re-enter");
Check(m.MakeSubstitution(m.HomeTeam, m.HomeTeam.Players[8], m.HomeTeam.Bench[1]), "second substitution in same stoppage allowed");
Check(m.HomeTeam.StoppageWindowsUsed == 1, "multiple changes in one stoppage count once");
m.ResumeMatch(); m.Update(.01f); m.PauseMatch();
Check(m.MakeSubstitution(m.HomeTeam, m.HomeTeam.Players[7], m.HomeTeam.Bench[2]), "second separate substitution stop");
m.ResumeMatch(); m.Update(.01f); m.PauseMatch();
Check(m.MakeSubstitution(m.HomeTeam, m.HomeTeam.Players[6], m.HomeTeam.Bench[3]), "third separate substitution stop");
m.ResumeMatch(); m.Update(.01f); m.PauseMatch();
Check(!m.CanMakeSubstitution(m.HomeTeam), "fourth regulation opportunity rejected even with one substitute remaining");
m.TriggerHalfTime();
Check(m.MakeSubstitution(m.HomeTeam, m.HomeTeam.Players[5], m.HomeTeam.Bench[4]), "halftime permits unused fifth substitute despite three used opportunities");
Check(m.HomeTeam.StoppageWindowsUsed == 3 && m.HomeTeam.SubstitutionsUsed == 5, "halftime adds no opportunity");
Check(!m.MakeSubstitution(m.HomeTeam, m.HomeTeam.Players[4], m.HomeTeam.Bench[5]), "sixth regulation substitute rejected");
m.ResumeMatch(); m.TriggerExtraTime();
Check(m.IsExtraTimeStartInterval && m.HomeTeam.MaxSubstitutions == 6 && m.HomeTeam.MaxStoppageWindows == 4, "extra time adds sixth substitute and one opportunity");
Check(m.MakeSubstitution(m.HomeTeam, m.HomeTeam.Players[4], m.HomeTeam.Bench[5]), "sixth permitted at extra-time interval");
Check(m.HomeTeam.StoppageWindowsUsed == 3 && m.HomeTeam.SubstitutionsUsed == 6, "pre-extra-time interval substitution is opportunity-free");
Check(!m.MakeSubstitution(m.HomeTeam, m.HomeTeam.Players[3], m.HomeTeam.Bench[6]), "seventh substitute rejected");

m = New(); AddBench(m.HomeTeam); m.SetMatchTimeMinutes(65);
m.HomeTeam.Players[9].CurrentStamina = 20;
MatchSettings.Current.AutoSubstitutionsEnabled = true;
m.PauseMatch(); m.RunNativeAutomation();
Check(m.HomeTeam.SubstitutionsUsed == 0, "opening tactics does not trigger automatic substitution");
m.ResumeMatch(); m.AwardFoulRestart(m.HomeTeam.Players[9], new Vector2(60, 30)); m.Update(.02f);
Check(m.HomeTeam.SubstitutionsUsed == 1, "enabled automatic substitution now runs at real second-half stoppage");
m.Update(.02f); Check(m.HomeTeam.SubstitutionsUsed == 1, "automatic substitution runs once per stoppage not every frame");
MatchSettings.Current.AutoSubstitutionsEnabled = false;
m = New(); AddBench(m.HomeTeam); m.SetMatchTimeMinutes(70); m.HomeTeam.Players[9].CurrentStamina = 10;
m.AwardFoulRestart(m.HomeTeam.Players[9], new Vector2(60, 30)); m.Update(.02f);
Check(m.HomeTeam.SubstitutionsUsed == 0, "disabled automatic substitution stays disabled");
MatchSettings.Current.AutoTacticalAdjustmentsEnabled = true;
m = New(); m.SetMatchTimeMinutes(70); m.AwayTeam.Score = 3; m.RunNativeAutomation();
Check(m.HomeTeam.Tactics.PressingIntensity == 70, "enabled adaptive tactics react to score during match flow");
MatchSettings.Current.AutoTacticalAdjustmentsEnabled = false;
m = New(); m.SetMatchTimeMinutes(70); m.AwayTeam.Score = 3; m.HomeTeam.Tactics.PressingIntensity = 12; m.RunNativeAutomation();
Check(m.HomeTeam.Tactics.PressingIntensity == 12, "disabled adaptive tactics preserve user settings");
string musicDir = Path.Combine(Path.GetTempPath(), "menu-regression-" + Guid.NewGuid()); Directory.CreateDirectory(musicDir);
File.WriteAllBytes(Path.Combine(musicDir, "MenuMusic.mp3"), new byte[101]);
MenuAudioGenerator.EnsureMenuMusic(Path.Combine(musicDir, "MenuMusic.wav"));
Check(!File.Exists(Path.Combine(musicDir, "MenuMusic.wav")), "supplied menu MP3 prevents generated WAV shadow");
Directory.Delete(musicDir, true);

// User-selected replacement policy: strength in the target position, bookings/fouls and keepers.
MatchSettings.Current.AutoSubstitutionsEnabled = true;
m = New(); m.SetMatchTimeMinutes(20); m.HomeTeam.Players[9].CurrentStamina = 20;
var weak = new Player(700, "weak", 20, SpecificPosition.ST, 50, 50, 50, 50, 50);
var strong = new Player(701, "strong", 21, SpecificPosition.ST, 90, 90, 90, 90, 90);
m.HomeTeam.Bench.AddRange(new[] { weak, strong });
m.AwardFoulRestart(m.HomeTeam.Players[9], new Vector2(60, 30)); m.Update(.02f);
Check(m.HomeTeam.Players.Contains(strong) && !m.HomeTeam.Players.Contains(weak), "strongest exact-position replacement selected before minute60");
m = New(); m.HomeTeam.Players[9].CurrentStamina = 20;
var secondary = new Player(702, "secondary", 22, SpecificPosition.CM, 90, 90, 90, 90, 90, new List<SpecificPosition> { SpecificPosition.ST });
m.HomeTeam.Bench.Add(secondary); m.AwardFoulRestart(m.HomeTeam.Players[9], new Vector2(60, 30)); m.Update(.02f);
Check(m.HomeTeam.Players.Contains(secondary) && secondary.CurrentAssignedPosition == SpecificPosition.ST, "supported secondary-position replacement used when no exact available");
m = New(); m.HomeTeam.Players[9].CurrentStamina = 20;
m.HomeTeam.Bench.Add(new Player(703, "incompatible", 23, SpecificPosition.GK, 99, 99, 99, 99, 99));
m.AwardFoulRestart(m.HomeTeam.Players[9], new Vector2(60, 30)); m.Update(.02f);
Check(m.HomeTeam.SubstitutionsUsed == 0, "no incompatible emergency fallback in automatic selection");
m = New(); m.HomeTeam.Players[0].CurrentStamina = 20;
var reserveKeeper = new Player(704, "reserve keeper", 24, SpecificPosition.GK, 85, 85, 85, 85, 85);
m.HomeTeam.Bench.Add(reserveKeeper); m.AwardFoulRestart(m.HomeTeam.Players[9], new Vector2(60, 30)); m.Update(.02f);
Check(m.HomeTeam.Players.Contains(reserveKeeper), "goalkeepers are not excluded from automatic substitutions");
m = New(); AddBench(m.HomeTeam); var atRisk = m.HomeTeam.Players[9]; atRisk.YellowCards = 1;
for (int i = 0; i < 3; i++) Event(new MatchEvent(MatchEventType.FoulCommitted, "foul", subjectPlayer: atRisk));
m.AwardFoulRestart(m.HomeTeam.Players[8], new Vector2(60, 30)); m.Update(.02f);
Check(!m.HomeTeam.Players.Contains(atRisk), "booked player with three real recorded fouls is replaced");
m = New(); AddBench(m.HomeTeam); var sentOff = m.HomeTeam.Players[9]; sentOff.IsSentOff = true; sentOff.CurrentStamina = 0;
m.AwardFoulRestart(m.HomeTeam.Players[8], new Vector2(60, 30)); m.Update(.02f);
Check(m.HomeTeam.SubstitutionsUsed == 0 && m.HomeTeam.Players.Contains(sentOff), "red-card vacancy is never filled by automatic replacement");
MatchSettings.Current.AutoSubstitutionsEnabled = false;

// Broad audit: database/draft, input, persistence, and complete period boundaries.
var keyboard = new AccessibleFootball2026.Input.KeyboardInputHandler();
keyboard.HandleKeyDown(0x51, true);
Check(keyboard.PollCurrentActions(true).Contains(GameAction.LoftedThroughBall), "Q is a lofted through ball in the input handler");
keyboard.HandleKeyDown(0x51, true);
Check(!keyboard.PollCurrentActions(true).Contains(GameAction.LoftedThroughBall), "held action cannot auto-repeat");
keyboard.HandleKeyUp(0x51); keyboard.HandleKeyDown(0x51, true);
Check(keyboard.PollCurrentActions(true).Contains(GameAction.LoftedThroughBall), "released action can trigger again");
keyboard.HandleKeyDown(0x26, true); keyboard.HandleKeyDown(0xA0, true);
Check(keyboard.PollCurrentActions(true).SetEquals(new[] {GameAction.MoveUp, GameAction.Sprint}), "movement and sprint remain continuous together");
keyboard.Reset(); Check(keyboard.PollCurrentActions(true).Count == 0, "pause/focus reset clears held keys and pending actions");

bool KickoffLegal(MatchEngine game, Team kicking)
{
    return game.Ball.Position2D == game.Pitch.CenterSpot && game.Ball.Velocity == Vector3.Zero &&
        new[] { game.HomeTeam, game.AwayTeam }.All(t => t.Players.Where(p => p.IsActiveOnPitch && p != game.Ball.Owner).All(p =>
            (t == game.HomeTeam ? game.UserAttackingRight : !game.UserAttackingRight) ? p.Position2D.X < Pitch.HalfLength : p.Position2D.X > Pitch.HalfLength)) &&
        (kicking == game.HomeTeam ? game.AwayTeam : game.HomeTeam).Players.Where(p => p.IsActiveOnPitch).All(p => Vector2.Distance(p.Position2D,game.Pitch.CenterSpot) >= 9.15f);
}
m = New(); Check(KickoffLegal(m,m.HomeTeam), "initial kickoff places teams in own halves and opponents outside centre circle");
m.TriggerHalfTime(); Check(KickoffLegal(m,m.AwayTeam), "second-half kickoff obeys own-half and circle rules");
m.ResumeMatch(); m.AwardFoulRestart(m.HomeTeam.Players[9]);
Set(m,"_isShotInProgress",true); Set(m,"_shotContextValid",true);
m.TriggerExtraTime();
Check(m.State == MatchState.HalfTime && m.IsExtraTimeStartInterval && m.Restart == null && !m.AwaitingStoppageConfirmation, "extra time clears stale restart and held state");
Check((bool)m.GetType().GetField("_isShotInProgress",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(m)! == false, "extra time clears old shot state");
Check(KickoffLegal(m,m.AwayTeam), "extra-time kickoff correctly positioned");
var direction = m.UserAttackingRight; m.TriggerExtraTime(); Check(direction == m.UserAttackingRight, "extra-time transition is idempotent");
m.ResumeMatch(); m.SetMatchTimeMinutes(104.999f); m.Update(.2f);
Check(m.State == MatchState.HalfTime && m.MatchTimeMinutes == 105 && KickoffLegal(m,m.HomeTeam), "extra-time halftime freezes exactly at 105 with correct kickoff");
m.ResumeMatch(); m.SetMatchTimeMinutes(119.999f); m.Update(.2f);
Check(m.State == MatchState.GameOver, "drawn extra time ends without shootout when shootout disabled");
MatchSettings.Current.PenaltyShootoutEnabled = true;
m = New(); m.SetStoppageMinutes(0,0); m.TriggerHalfTime(); m.ResumeMatch(); m.SetMatchTimeMinutes(89.999f); m.Update(.2f);
Check(m.State == MatchState.PenaltyShootout, "tied regulation can go directly to shootout when extra time disabled");
Check(!PenaltyShootoutRules.IsDecided(4,3,6,5) && PenaltyShootoutRules.IsDecided(4,3,6,6), "sudden death must wait for equal kick counts");
Check(PenaltyShootoutRules.IsDecided(3,0,3,3) && !PenaltyShootoutRules.IsDecided(2,0,2,2), "five-kick shootout ends early only when catch-up impossible");
MatchSettings.Current.PenaltyShootoutEnabled = false;

string settingsPath = Path.Combine(Path.GetTempPath(),Guid.NewGuid()+"-settings.json");
MatchSettings.Current.AutoSubstitutionsEnabled = true; MatchSettings.Current.AutoTacticalAdjustmentsEnabled = true;
MatchSettings.Current.MusicVolumePercent = 27; MatchSettings.Current.Save(settingsPath);
MatchSettings.Current.AutoSubstitutionsEnabled = false; MatchSettings.Current.AutoTacticalAdjustmentsEnabled = false; MatchSettings.Current.MusicVolumePercent = 1;
MatchSettings.Load(settingsPath);
Check(MatchSettings.Current.AutoSubstitutionsEnabled && MatchSettings.Current.AutoTacticalAdjustmentsEnabled && MatchSettings.Current.MusicVolumePercent == 27, "settings save/load preserves opt-ins and volume through a real temporary file");
File.Delete(settingsPath); MatchSettings.Current.AutoSubstitutionsEnabled = false; MatchSettings.Current.AutoTacticalAdjustmentsEnabled = false;

// Optional production database audit uses the supplied file, never a fabricated roster.
int dbArgument = Array.IndexOf(args,"--database");
if(dbArgument >= 0)
{
    if(dbArgument+1 >= args.Length)throw new ArgumentException("--database needs a file path");
    var databaseType=typeof(JsonDatabaseProvider);
    databaseType.GetMethod("ClearLoadedTeams",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,null);
    databaseType.GetMethod("LoadDatabaseFromFile",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{args[dbArgument+1]});
    databaseType.GetField("_isInitialized",BindingFlags.Static|BindingFlags.NonPublic)!.SetValue(null,true);
    var production=JsonDatabaseProvider.AllTeams.ToArray();
    Check(production.Length>0,"production database parses and exposes real teams");
    var usable=production.Where(t=>t.GetMatchStartIssue()==null).ToArray();
    int started=0;foreach(var t in usable)
    {
        var game=new MatchEngine(t.Clone(TeamSide.Home),usable.First(x=>x.Id!=t.Id).Clone(TeamSide.Away),new MatchEventBus(),new Input());
        game.Update(.02f);started++;
    }
    Check(started==usable.Length,"all usable production teams start and update: "+started);
    var unusable=production.Where(t=>t.GetMatchStartIssue()!=null).ToArray();
    Check(unusable.All(t=>!string.IsNullOrWhiteSpace(t.GetMatchStartIssue())),"unusable production teams report a concrete roster/keeper issue: "+unusable.Length);
    Console.WriteLine("Production roster gaps: "+string.Join("; ",unusable.Select(t=>t.Name+" ("+t.Players.Count+" starters)")));
    var productionClubs=production.Where(t=>!t.IsNationalTeam).ToArray();bool allDraftsComplete=true;
    for(int seed=0;seed<100;seed++)
    {
        var rng=new Random(seed);var pool=productionClubs.OrderBy(_=>rng.Next()).Take(8).ToArray();var d=new DreamTeamBuilder();
        foreach(var t in pool.Take(4))d.AddUserSourceClub(t);foreach(var t in pool.Skip(4))d.AddCpuSourceClub(t);d.DraftBothTeams(rng);
        allDraftsComplete &= d.UserDreamTeam.Players.Count==11 && d.CpuDreamTeam.Players.Count==11;
    }
    Check(allDraftsComplete,"100 seeded real-database drafts produce complete starting elevens");
}

string dbPath = Path.Combine(Path.GetTempPath(),Guid.NewGuid()+"-teams.json");
var fixtureTeams = Enumerable.Range(0,8).Select(t => new {team="fixture club "+t, group="test",type="club",players=Enumerable.Range(0,24).Select(i=>new {name="player "+t+"-"+i, age=24,position=i<2?"GK":i<10?"CB":i<17?"CM":"ST",power=80}).ToArray()}).ToArray();
File.WriteAllText(dbPath,System.Text.Json.JsonSerializer.Serialize(new {teams=fixtureTeams}));
var dbType = typeof(JsonDatabaseProvider);
dbType.GetMethod("ClearLoadedTeams",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,null);
dbType.GetMethod("LoadDatabaseFromFile",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{dbPath});
dbType.GetField("_isInitialized",BindingFlags.Static|BindingFlags.NonPublic)!.SetValue(null,true);
var clubs=JsonDatabaseProvider.AllTeams.ToArray();
Check(clubs.Length == 8 && clubs.SelectMany(t=>t.Players.Concat(t.Bench)).Select(p=>p.Id).Distinct().Count() == 192, "JSON unrelated players have globally distinct IDs not squad-row IDs");
var draft = new DreamTeamBuilder(); foreach(var club in clubs.Take(4))draft.AddUserSourceClub(club);foreach(var club in clubs.Skip(4))draft.AddCpuSourceClub(club);
draft.DraftBothTeams(new Random(17));
Check(draft.UserDreamTeam.Players.Count == 11 && draft.CpuDreamTeam.Players.Count == 11 && draft.UserDreamTeam.Bench.Count == 7 && draft.CpuDreamTeam.Bench.Count == 7, "both four-club draft teams receive complete squads");
Check(!draft.UserDreamTeam.Players.Concat(draft.UserDreamTeam.Bench).Select(p=>p.Id).Intersect(draft.CpuDreamTeam.Players.Concat(draft.CpuDreamTeam.Bench).Select(p=>p.Id)).Any(), "draft squads are independent and duplicate-free");
File.Delete(dbPath);

var emergencyKeeper = new Player(990, "emergency", 29, SpecificPosition.CB, 70,70,70,70,70);
emergencyKeeper.CurrentAssignedPosition = SpecificPosition.GK;
Check(emergencyKeeper.Position == SpecificPosition.GK && emergencyKeeper.Role == PlayerRole.Goalkeeper, "GK enum zero remains an assigned position for emergency goalkeeper");
m = New(); m.HomeTeam.Players[0].IsSentOff = true; m.HomeTeam.Players[1].CurrentAssignedPosition = SpecificPosition.GK;
Invoke(m,"AwardGoalKick",true);
Check(m.Restart!.Taker == m.HomeTeam.Players[1] && m.Restart.Taker.IsActiveOnPitch, "goal kick uses active reassigned keeper not sent-off first squad entry");
m = New(); m.ResumePlayAfterGoalCelebration(m.AwayTeam);
Check(KickoffLegal(m,m.AwayTeam) && m.PreviousKicker == null, "goal restart repositions both teams and clears stale assist attribution");
var shortHome=Make("short",TeamSide.Home); shortHome.Players.RemoveRange(7,4);
var shortAway=Make("short-away",TeamSide.Away); shortAway.Players.RemoveRange(7,4);
var shortMatch = new MatchEngine(shortHome,shortAway,new MatchEventBus(),new Input());
Check(KickoffLegal(shortMatch,shortHome), "seven-player match starts without fixed index9 access");
shortHome.CornerTaker = null; Invoke(shortMatch,"AwardCornerKick",true);
Check(shortMatch.Restart!.Taker.IsActiveOnPitch, "short squad corner uses an available player without index9 access");
shortHome.Players.RemoveAt(6); bool rejected=false;
try { _ = new MatchEngine(shortHome,shortAway,new MatchEventBus(),new Input()); } catch(ArgumentException) { rejected=true; }
Check(rejected,"fewer than seven players rejected cleanly before kickoff");
for(int scenario=0;scenario<3;scenario++)
{
    MatchSettings.Current.ExtraTimeEnabled = scenario > 0; MatchSettings.Current.PenaltyShootoutEnabled = scenario == 2;
    MatchSettings.Current.MatchDurationMinutes = 1;
    m=New();m.SetStoppageMinutes(0,0);bool finite=true;
    for(int frame=0;frame<5000 && m.State is not MatchState.GameOver and not MatchState.PenaltyShootout;frame++)
    {
        if(m.State==MatchState.HalfTime)m.ResumeMatch();
        if(m.AwaitingStoppageConfirmation)m.DismissStoppageReplay();
        m.Update(.1f);
        finite &= float.IsFinite(m.Ball.Position.X) && float.IsFinite(m.Ball.Position.Y) &&
            m.HomeTeam.Players.Concat(m.AwayTeam.Players).All(p=>float.IsFinite(p.Position2D.X)&&float.IsFinite(p.Position2D.Y));
    }
    Check(finite && m.State is MatchState.GameOver or MatchState.PenaltyShootout, "whole-match smoke path completes with finite physics scenario "+scenario);
}
MatchSettings.Current.ExtraTimeEnabled=false;MatchSettings.Current.PenaltyShootoutEnabled=false;
m=New();m.HomeTeam.Players[0].CurrentAssignedPosition=SpecificPosition.CB;
Check(m.HomeTeam.GetMatchStartIssue()!=null,"missing goalkeeper produces a usable start-block reason");
m.HomeTeam.Players[1].CurrentAssignedPosition=SpecificPosition.GK;
Check(m.HomeTeam.GetMatchStartIssue()==null,"outfield reassigned as goalkeeper can satisfy lineup validation without adding a player");
Console.WriteLine($"{passed} assertions passed. No Windows/MCI/NVDA runtime was exercised.");

sealed class Input : IInputHandler
{
    public HashSet<GameAction> PollCurrentActions(bool possession) => new();
    public bool IsActionTriggered(GameAction action, bool possession) => false;
}
sealed class Reader : IScreenReader
{
    public bool IsAvailable => true;
    public List<string> Spoken { get; } = new();
    public void Speak(string text, bool interrupt = true) { lock (Spoken) Spoken.Add(text); }
    public void CancelSpeech() { }
    public Task SpeakUntilFinishedAsync(string text, bool interrupt = true) { Speak(text, interrupt); return Task.CompletedTask; }
}
sealed class BlockingReader : IScreenReader
{
    public bool IsAvailable => true;
    public int CancelCount;
    public void Speak(string text, bool interrupt = true) { }
    public void CancelSpeech() => CancelCount++;
    public Task SpeakUntilFinishedAsync(string text, bool interrupt = true) => Task.Delay(60000);
}
