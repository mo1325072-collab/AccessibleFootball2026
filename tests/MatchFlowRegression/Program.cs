using System.Numerics;
using System.Reflection;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Input;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;
using AccessibleFootball2026.Audio;
using AccessibleFootball2026.Audio.Commentary;
using AccessibleFootball2026.App.Menus;
using AccessibleFootball2026.Core.Tactics;

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
var groupedNational=Enum.GetValues<ContinentType>().SelectMany(NationalTeamDatabase.GetNationalTeamsByContinent).ToArray();
Check(groupedNational.Length==211&&groupedNational.Select(t=>t.Id).Distinct().Count()==211,"all 211 source national teams occur exactly once across six confederation menus");
Check(NationalTeamDatabase.GetNationalTeamsByContinent(ContinentType.Africa).Count==54&&NationalTeamDatabase.GetNationalTeamsByContinent(ContinentType.Europe).Count==55,"Africa and Europe categories match verified FIFA membership counts without changing source rosters");
Check(NationalTeamDatabase.GetNationalTeamsByContinent(ContinentType.Asia).Count==46&&NationalTeamDatabase.GetNationalTeamsByContinent(ContinentType.Oceania).Count==11,"Australia belongs to AFC and American Samoa belongs to OFC, not a guessed geographic grouping");
Check(NationalTeamConfederations.GetContinent("Australia")==ContinentType.Asia&&NationalTeamConfederations.GetContinent("American Samoa")==ContinentType.Oceania,"cross-geography football association membership uses official FIFA source");

// Actual team-selection navigation: per-call callbacks must reach the confederation submenu.
MatchSettings.Current.Language = UiLanguage.English;
var selectionReader = new Reader(); AccessibleMenu? selectionOpened = null;
var selection = new TeamSelectionManager(selectionReader);
var categories = selection.CreateHomeTeamSelectMenu(_ => {}, x => selectionOpened = x, () => {});
bool HasArabic(string text) => text.Any(c => char.IsLetter(c) && c >= '\u0600' && c <= '\u06ff');
Check(!HasArabic(categories.MenuTitle) && Enumerable.Range(0,categories.Count).All(i => !HasArabic(categories.GetItem(i).Title)), "English team categories contain no untranslated UI labels");
int nationalsIndex = Enumerable.Range(0,categories.Count).First(i => categories.GetItem(i).Title.StartsWith("National teams ("));
while(categories.GetCurrentIndex()!=nationalsIndex)categories.NavigateNext();categories.SelectCurrent();
Check(selectionOpened?.Count==8 && selectionOpened.MenuTitle=="National teams by confederation", "per-call navigation opens six confederations plus all and Back");
var confederations=selectionOpened!;
Check(Enumerable.Range(0,confederations.Count).All(i=>!HasArabic(confederations.GetItem(i).Title)),"all English confederation labels and Back are localized");
confederations.SelectCurrent();
Check(selectionOpened!=confederations && selectionOpened!.MenuTitle.StartsWith("Your team:"),"confederation selection reaches the actual keyboard team list");
string sourceName=selectionOpened!.GetItem(0).Title;selectionOpened.SelectCurrent();
Check(selection.SelectedHomeTeam?.Name==sourceName,"English UI preserves the selected original source team name");
var awayCategories=selection.CreateAwayTeamSelectMenu(_=>{},x=>selectionOpened=x,()=>{});
int allIndex=Enumerable.Range(0,awayCategories.Count).First(i=>awayCategories.GetItem(i).Title.StartsWith("All clubs"));
while(awayCategories.GetCurrentIndex()!=allIndex)awayCategories.NavigateNext();awayCategories.SelectCurrent();
Check(Enumerable.Range(0,selectionOpened!.Count-1).All(i=>selectionOpened.GetItem(i).Title!=sourceName),"opponent list still excludes the selected team after localization");
MatchSettings.Current.Language=UiLanguage.Arabic;
Check(selection.CreateHomeTeamSelectMenu(_=>{}).MenuTitle=="اختر فئة الفريق الأساسي","Arabic selection remains available after switching back");


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
string audioFixture=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+"-audio");
string portable=Path.Combine(audioFixture,"portable"), legacy=Path.Combine(audioFixture,"legacy");
Directory.CreateDirectory(portable);Directory.CreateDirectory(Path.Combine(legacy,"Commentary","FaresAwad"));
File.WriteAllText(Path.Combine(portable,"generated.wav"),"fixture");File.WriteAllText(Path.Combine(legacy,"Commentary","FaresAwad","commentary_context.json"),"{}");
Check(SoundEffectsPlayer.ResolveBestAudioBaseDirectory(portable,legacy)==legacy,"generated tones alone cannot shadow an installed commentary library");
Directory.CreateDirectory(Path.Combine(portable,"Commentary","FaresAwad"));File.WriteAllText(Path.Combine(portable,"Commentary","FaresAwad","commentary_context.json"),"{}");
Check(SoundEffectsPlayer.ResolveBestAudioBaseDirectory(portable,legacy)==portable,"portable packaged commentary takes priority over an older D-drive library");
Directory.Delete(audioFixture,true);


// Requirements audit: regressions found by comparing source behavior with ORIGINAL_REQUEST and tactical specs.
m=New();var injured=m.HomeTeam.Players[0];var wrong=new Player(900,"Wrong forward",12,SpecificPosition.ST,99,99,99,99,99);
var injuryReserveKeeper=new Player(901,"Reserve keeper",13,SpecificPosition.GK,65,65,65,65,65);
m.HomeTeam.Bench.AddRange(new[]{wrong,injuryReserveKeeper});m.ApplyInjury(injured,PlayerInjuryStatus.SevereInjury);
Check(m.PendingInjuryPlayer==injured&&m.HomeTeam.SubstitutionsUsed==0&&m.HomeTeam.Bench.Contains(injuryReserveKeeper),"severe user-team injury waits for the user instead of choosing a compatible player automatically");
Check(m.ResolveInjurySubstitution(injured,injuryReserveKeeper)&&m.HomeTeam.Players.Contains(injuryReserveKeeper),"the explicitly selected reserve goalkeeper is the one who enters");
Check(m.HomeTeam.SubstitutionsUsed==1&&m.HomeTeam.StoppageWindowsUsed==1,"forced injury change consumes one legal substitution opportunity");
m=New();injured=m.HomeTeam.Players[0];m.HomeTeam.Bench.Add(wrong);m.ApplyInjury(injured,PlayerInjuryStatus.SevereInjury);
Check(m.PendingInjuryPlayer==injured&&m.HomeTeam.SubstitutionsUsed==0,"a nonmatching bench is still shown for human choice, never substituted automatically");
m.ResolveInjurySubstitution(injured,null);
Check(injured.IsOffPitch&&!injured.IsSentOff&&m.HomeTeam.SubstitutionsUsed==0,"unreplaceable injured keeper leaves without inventing an incompatible goalkeeper or a red card");
m=New();var severelyInjured=m.HomeTeam.Players[9];severelyInjured.Position2D=new Vector2(100,34);m.HomeTeam.PenaltyTaker=severelyInjured;
var replacement=new Player(902,"Replacement striker",14,SpecificPosition.ST,80,80,80,80,80);m.HomeTeam.Bench.Add(replacement);
var offender=m.AwayTeam.Players[2];offender.Position2D=new Vector2(99,34);m.AwayTeam.Tactics.Tackling=TacklingAggression.Reckless;
m.HandleFoulSanction(offender,severelyInjured,true,true);
Check(severelyInjured.InjuryStatus==PlayerInjuryStatus.SevereInjury&&m.PendingInjuryPlayer==severelyInjured&&!m.HomeTeam.Players.Contains(replacement),"actual reckless sliding foul opens a pending user injury choice");
Check(!m.DismissStoppageReplay(),"Enter for restart cannot bypass unresolved injury choice");
m.ResolveInjurySubstitution(severelyInjured,replacement);
Check(m.Restart?.Kind==RestartKind.Penalty&&m.Restart.Team==m.HomeTeam&&m.Restart.Taker==replacement&&m.Ball.Owner==replacement,"injured victim substitution preserves actual penalty and reassigns its taker");
m=New();severelyInjured=m.HomeTeam.Players[9];severelyInjured.Position2D=new Vector2(70,34);m.HomeTeam.FreeKickTaker=severelyInjured;m.HomeTeam.SubstitutionsUsed=5;
m.AwardFoulRestart(severelyInjured);m.ApplyInjury(severelyInjured,PlayerInjuryStatus.SevereInjury);
Check(m.Restart?.Taker.IsActiveOnPitch==true&&m.Restart.Taker!=severelyInjured&&m.Ball.Owner==m.Restart.Taker,"no-substitution injury cannot strand restart or possession on an off-pitch victim");
Check(m.HomeTeam.Players.Count(p=>p.IsActiveOnPitch)==10,"severe injury with exhausted replacements produces actual ten-player side");
var crowdA=New();int volumeA=0;crowdA.OnCrowdVolumeChanged+=v=>volumeA=v;crowdA.Ball.Owner=null;crowdA.Ball.Position=new Vector3(95,34,0);Invoke(crowdA,"UpdateCrowdProximity",.1f);
var crowdB=New();crowdB.TriggerHalfTime();int volumeB=0;crowdB.OnCrowdVolumeChanged+=v=>volumeB=v;crowdB.Ball.Owner=null;crowdB.Ball.Position=new Vector3(10,34,0);Invoke(crowdB,"UpdateCrowdProximity",.1f);
Check(volumeA==volumeB&&volumeA>500,"crowd proximity uses switched opponent goal, with symmetric first/second-half attacks");
var crowdC=New();int volumeC=0;crowdC.OnCrowdVolumeChanged+=v=>volumeC=v;crowdC.Ball.CatchOrTackle(crowdC.AwayTeam.Players[9]);crowdC.Ball.Position=new Vector3(10,34,0);Invoke(crowdC,"UpdateCrowdProximity",.1f);
Check(volumeC==volumeA,"crowd escalation follows the actual away team's attacking goal too");
m=New();var tackler=m.AwayTeam.Players[2];var carrier=m.HomeTeam.Players[9];tackler.Position2D=new Vector2(60,34);carrier.Position2D=new Vector2(61,34);
var duelSamples=Enumerable.Range(0,300).Select(_=>MatchEngine.EvaluateTackleDuel(tackler,carrier,new Vector2(61.4f,34),m.AwayTeam,m.HomeTeam,DifficultyLevel.Normal)).ToArray();
Check(duelSamples.Select(d=>(d.TacklerWon,d.IsFoul,d.IsSevereInjuryRisk)).Distinct().Count()==1,"same tackle geometry attributes and tactics are deterministic, without a random die");
reader=new Reader();var radarCoordinator=new SmartAudioCoordinator(reader);SmartAudioCoordinator.SilenceLiveMatchSpeech=true;
radarCoordinator.HandleMatchEvent(new MatchEvent(MatchEventType.TacticalRadarInfo,"الكرة مع Saint-Maximin - جناح"));
Check(reader.Spoken.Count==1,"explicit requested radar is not silenced because a player name contains a hyphen");
radarCoordinator.HandleMatchEvent(new MatchEvent(MatchEventType.TacticalRadarInfo,"Player switch") {Context=AccessibleFootball2026.Core.Commentary.CommentaryContext.Empty.With(("radar.reason","playerSwitch"))});
Check(reader.Spoken.Count==1,"semantic player-switch event remains silent in live match");
m=New();var priorSpeed=m.ActiveUserPlayer.Speed;var priorStamina=m.ActiveUserPlayer.Stamina;m.ApplyInjury(m.ActiveUserPlayer,PlayerInjuryStatus.MinorKnock);
Check(m.ActiveUserPlayer.Speed==priorSpeed*.8f&&m.ActiveUserPlayer.Stamina==priorStamina*.8f,"minor knock applies requested 20 percent speed and stamina reduction");
Check(TacticalAiAdvisor.GenerateTacticalReport(m).Contains("وليس نموذج Qwen"),"missing model fallback explicitly labels native rules instead of claiming Qwen inference");

m=New();var runUpActor=m.ActiveUserPlayer;Check(m.BeginSlowRunUp(),"slow approach can begin before interval fixture");m.TriggerHalfTime();
Check(typeof(MatchEngine).GetField("_runUpPlayer",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(m)==null,"new period clears an unfinished prior-period run-up");
m=New();carrier=m.ActiveUserPlayer;carrier.Position2D=new Vector2(80,34);m.Ball.CatchOrTackle(carrier);
Check((bool)Invoke(m,"TryPlayAdvantage",carrier,false,false)!,"advantage fixture starts from a real retained attacking possession");
m.TriggerHalfTime();Check(typeof(MatchEngine).GetField("_advantage",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(m)==null,"new period clears an actual prior advantage incident");
m=New();m.ExecuteClearance();m.TriggerHalfTime();
Check(typeof(MatchEngine).GetField("_pendingClearance",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(m)==null,"new period cannot emit a stale prior-period clearance outcome");
var unsupportedReview=VarAiReferee.ReviewOffside("fixture player",true);
Check(!unsupportedReview.EvidenceAvailable&&!unsupportedReview.Overturned&&float.IsNaN(unsupportedReview.DecisionMarginCm),"name-only legacy VAR helper cannot fabricate a positional margin or overturn a call");
m=New();injured=m.HomeTeam.Players[9];replacement=new Player(903,"Injury replacement",14,SpecificPosition.ST,80,80,80,80,80);m.HomeTeam.Bench.Add(replacement);
m.ApplyInjury(injured,PlayerInjuryStatus.SevereInjury);m.ResolveInjurySubstitution(injured,replacement);int injurySubs=m.HomeTeam.SubstitutionsUsed;m.ApplyInjury(injured,PlayerInjuryStatus.SevereInjury);
Check(m.HomeTeam.SubstitutionsUsed==injurySubs&&m.AwayTeam.SubstitutionsUsed==0,"repeated injury to a substituted-off player cannot change the other team or consume another substitution");
if(!OperatingSystem.IsWindows())
{
    string fixture=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+"-model-fixture");Directory.CreateDirectory(fixture);
    string fake=Path.Combine(fixture,"fake-cli");
    string Call(string program,string prompt,int seconds=2)=>(string)typeof(TacticalAiAdvisor).GetMethod("RunModelProcess",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{program,"model with spaces.gguf",prompt,30,seconds})!;
    void Script(string body){File.WriteAllText(fake,"#!/bin/sh\n"+body+"\n");File.SetUnixFileMode(fake,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);}
    Script("printf 'fixture output only'; exit 7");Check(Call(fake,"test")=="","failed model process cannot be reported as successful inference");
    Script("i=0; while [ $i -lt 5000 ]; do echo diagnostic >&2; i=$((i+1)); done; printf 'fixture result'");
    Check(Call(fake,"name with \"quotes\" and spaces")=="fixture result","model process drains stderr alongside stdout without pipe deadlock");
    Script("while [ \"$1\" != '-p' ]; do shift; done; shift; printf '%s' \"$1\"");
    Check(Call(fake,"line one\nname with \"quotes\" and spaces")=="line one name with \"quotes\" and spaces","model prompt uses one argument preserving quotes spaces and line breaks");
    Script("sleep 12; printf 'late result'");var timer=System.Diagnostics.Stopwatch.StartNew();
    Check(Call(fake,"test",1)==""&&timer.Elapsed.TotalSeconds<5,"model timeout terminates process tree instead of leaving hanging inference");
    Directory.Delete(fixture,true);
}


// Build real menu objects in both languages, and exercise actions rather than dictionary-only tests.
var languageBefore=MatchSettings.Current.Language;MatchSettings.Current.Language=UiLanguage.English;
var uiReader=new Reader();var uiTeam=Make("English fixture",TeamSide.Home);AddBench(uiTeam);
bool ArabicUi(string value)=>value.Any(c=>char.IsLetter(c)&&c>='\u0600'&&c<='\u06ff');
void EnglishMenu(AccessibleMenu menu,string label)
{
    Check(!ArabicUi(menu.MenuTitle)&&Enumerable.Range(0,menu.Count).All(i=>!ArabicUi(menu.GetItem(i).Title)),label);
}
AccessibleMenu? uiOpened=null;
var pre=GamePlanMenuBuilder.CreatePreMatchMenu(uiTeam,uiReader,x=>uiOpened=x,()=>{},()=>{});
EnglishMenu(pre,"English pre-match menu labels");
EnglishMenu(GamePlanMenuBuilder.CreateLineupMenu(uiTeam,uiReader,x=>uiOpened=x,()=>{}),"English lineup includes localized condition descriptions");
EnglishMenu(GamePlanMenuBuilder.CreateBenchMenu(uiTeam,uiReader,x=>uiOpened=x,()=>{}),"English bench labels and conditions");
var roles=GamePlanMenuBuilder.CreateSpecialistsMenu(uiTeam,uiReader,x=>uiOpened=x,()=>{});
EnglishMenu(roles,"English captain and set-piece role labels");roles.SelectCurrent();EnglishMenu(uiOpened!,"English captain candidate menu");
var forms=GamePlanMenuBuilder.CreateFormationMenu(uiTeam,uiReader,()=>{});
EnglishMenu(forms,"English formation names");forms.SelectCurrent();Check(!ArabicUi(uiReader.Spoken.Last()),"English formation confirmation speaks selected language");
var tacticsMenu=GamePlanMenuBuilder.CreateTacticsMenu(uiTeam,uiReader,()=>{});
EnglishMenu(tacticsMenu,"English tactics display labels including marking and focus");
for(int i=0;i<tacticsMenu.Count-1;i++){tacticsMenu.SelectCurrent();Check(!ArabicUi(uiReader.Spoken.Last()),"English tactics action confirmation "+i);tacticsMenu.NavigateNext();}
var uiMatch=New();uiMatch.PauseMatch();
var enPause=GamePlanMenuBuilder.CreatePauseMenu(uiMatch,uiReader,commentary,x=>uiOpened=x,()=>{},()=>{},()=>{});
EnglishMenu(enPause,"English pause menu labels");
while(enPause.GetItem(enPause.GetCurrentIndex()).Title!="Match statistics")enPause.NavigateNext();enPause.SelectCurrent();
EnglishMenu(uiOpened!,"English measured statistics menu has no navigation tutorial");
Check(uiOpened!.MenuTitle=="Match statistics","statistics heading is concise rather than a key tutorial");
var radarBus=new MatchEventBus();MatchEvent? radarEvent=null;
radarBus.OnMatchEvent+=e=>{if(e.Type==MatchEventType.TacticalRadarInfo)radarEvent=e;};
var radarMatch=new MatchEngine(Make("Radar home",TeamSide.Home),Make("Radar away",TeamSide.Away),radarBus,new Input());
radarMatch.Ball.Position=new Vector3(75,0,0);radarMatch.ReportTacticalRadar();
int radarDistance=(int)Vector2.Distance(radarMatch.Ball.Position2D,radarMatch.Pitch.RightGoalCenter);
Check(radarEvent!=null&&!ArabicUi(radarEvent.Description)&&radarEvent.Description.Contains($"goal {radarDistance} m"),"English on-demand radar measures geometric distance, not horizontal-only distance");
radarMatch.TriggerHalfTime();radarMatch.ResumeMatch();radarMatch.Ball.CatchOrTackle(radarMatch.HomeTeam.Players[9]);radarMatch.Ball.Position=new Vector3(30,0,0);radarMatch.ReportTacticalRadar();
Check(radarEvent!.Description.Contains($"goal {(int)Vector2.Distance(radarMatch.Ball.Position2D,radarMatch.Pitch.LeftGoalCenter)} m"),"on-demand radar follows switched second-half goal");
radarMatch.AwardFoulRestart(radarMatch.HomeTeam.Players[9],new Vector2(70,34));radarMatch.ReportTacticalRadar();
Check(radarEvent!.Description.StartsWith("Free kick,")&&radarEvent.Description.Contains(radarMatch.Restart!.Taker.Name),"set-piece radar names actual restart and current taker");
var heldKeyboard=new AccessibleFootball2026.Input.KeyboardInputHandler();var heldBus=new MatchEventBus();var heldEvents=new List<MatchEvent>();heldBus.OnMatchEvent+=heldEvents.Add;
var heldMatch=new MatchEngine(Make("Held home",TeamSide.Home),Make("Held away",TeamSide.Away),heldBus,heldKeyboard);
heldMatch.AwardFoulRestart(heldMatch.HomeTeam.Players[9],new Vector2(60,34));
float heldTime=heldMatch.MatchTimeMinutes;var heldBall=heldMatch.Ball.Position;
heldKeyboard.HandleKeyDown(0x20,true);heldMatch.Update(.02f);
Check(heldEvents.Any(e=>e.Type==MatchEventType.TacticalRadarInfo&&e.Description.StartsWith("Free kick,")),"actual Space input works during held restart, not only direct radar method calls");
heldKeyboard.HandleKeyUp(0x20);heldKeyboard.HandleKeyDown(0x54,true);heldKeyboard.HandleKeyDown(0x57,true);heldMatch.Update(.02f);
Check(heldEvents.Any(e=>e.Type==MatchEventType.LineupRequested&&!ArabicUi(e.Description)),"explicit English lineup input remains available during held incidents");
Check(heldMatch.MatchTimeMinutes==heldTime&&heldMatch.Ball.Position==heldBall&&!heldEvents.Any(e=>e.Type==MatchEventType.ShotTaken),"held information requests cannot move play or execute a queued shot");
heldKeyboard.HandleKeyUp(0x54);heldKeyboard.HandleKeyUp(0x57);heldMatch.DismissStoppageReplay();heldMatch.Update(.02f);
Check(heldMatch.Restart!=null&&!heldEvents.Any(e=>e.Type==MatchEventType.ShotTaken),"shot tapped during held narration is consumed rather than firing after confirmation");
MatchSettings.Current.Language=UiLanguage.Arabic;radarMatch.ReportTacticalRadar();
Check(radarEvent!.Description.StartsWith("ركلة حرة"),"explicit radar switches back to Arabic");
MatchSettings.Current.Language=UiLanguage.English;
var uiPlayer=uiTeam.Players[0];uiPlayer.CurrentAssignedPosition=SpecificPosition.ST;
Check(!ArabicUi(uiPlayer.PositionSuitabilityMessage),"English out-of-position report and condition");
uiMatch.AwayTeam.Score=2;
Check(TacticalAiAdvisor.GetRecommendations(uiMatch,()=>{}).All(r=>!ArabicUi(r.Title)&&!ArabicUi(r.Description)),"English native tactical recommendations and descriptions");
var competition=new Tournament("UEFA_CL","دوري أبطال أوروبا",CompetitionType.KnockoutCup,"Europe");
Check(competition.DisplayName=="UEFA Champions League"&&competition.Name=="دوري أبطال أوروبا","competition UI translation leaves original source name intact");
MatchSettings.Current.Language=UiLanguage.Arabic;
Check(GamePlanMenuBuilder.CreateTacticsMenu(uiTeam,uiReader,()=>{}).MenuTitle.StartsWith("الأسلوب"),"switching back rebuilds Arabic tactics menu");
MatchSettings.Current.Language=languageBefore;

// Exercise shooting at both real goal ends and repeated discipline calls.
var shotMatch=New();var shooter=shotMatch.ActiveUserPlayer;shooter.Position2D=new Vector2(80,34);shotMatch.Ball.CatchOrTackle(shooter);
shooter.FacingDirection=new Vector2(1,0);shotMatch.AdjustAimingAngle(-.35f);
Check(shooter.FacingDirection.X>0&&shooter.FacingDirection.Y<0&&Math.Abs(shooter.FacingDirection.Length()-1)<.001f,"upward aiming preserves normalized first-half forward direction");
shotMatch.ExecuteShot();Check(shotMatch.Ball.Owner==null&&shotMatch.Ball.Velocity.X>0,"first-half actual shot physically travels toward the right goal");
shotMatch.TriggerHalfTime();shotMatch.ResumeMatch();shooter=shotMatch.HomeTeam.Players[9];Set(shotMatch,"<ActiveUserPlayer>k__BackingField",shooter);
shooter.Position2D=new Vector2(25,34);shooter.FacingDirection=new Vector2(-1,0);shotMatch.Ball.CatchOrTackle(shooter);shotMatch.AdjustAimingAngle(.35f);
Check(shooter.FacingDirection.X<0&&shooter.FacingDirection.Y>0,"downward aiming preserves second-half leftward attack");
shotMatch.ExecuteShot();Check(shotMatch.Ball.Owner==null&&shotMatch.Ball.Velocity.X<0,"second-half actual shot physically travels toward the left goal");
var disciplineBus=new MatchEventBus();var disciplineEvents=new List<MatchEvent>();disciplineBus.OnMatchEvent+=disciplineEvents.Add;
var discipline=new MatchEngine(Make("Card home",TeamSide.Home),Make("Card away",TeamSide.Away),disciplineBus,new Input());
var booked=discipline.HomeTeam.Players[2];discipline.IssueCard(booked,false);discipline.IssueCard(booked,false);
Check(booked.YellowCards==2&&booked.IsSentOff&&!booked.IsActiveOnPitch&&discipline.HomeTeam.Players.Count(p=>p.IsActiveOnPitch)==10,"second yellow dismisses exactly one player and opens a real vacancy");
int cardsBefore=disciplineEvents.Count(e=>e.Type is MatchEventType.YellowCard or MatchEventType.RedCard);discipline.IssueCard(booked,true);discipline.IssueCard(booked,false);
Check(disciplineEvents.Count(e=>e.Type is MatchEventType.YellowCard or MatchEventType.RedCard)==cardsBefore&&booked.YellowCards==2,"repeated sanctions on an already-dismissed player cannot duplicate cards or stats");
var outsider=Make("Other",TeamSide.Home).Players[3];discipline.IssueCard(outsider,true);
Check(!outsider.IsSentOff&&discipline.StatisticsFor(discipline.AwayTeam).RedCards==0&&discipline.AwayTeam.Players.Count(p=>p.IsActiveOnPitch)==11,"off-roster sanction cannot be attributed to the opposing team");

using(var smokeOutput = new StringWriter())
{
    Check(SimulationDiagnostics.Run(smokeOutput) == 0 && smokeOutput.ToString().Contains("0 failure(s)"), "production --test engine smoke checks run without Windows or audio startup");
}
// Actual injury -> accessible modal menu -> chosen (not strongest) bench player.
foreach (var language in new[]{UiLanguage.Arabic,UiLanguage.English})
{
    MatchSettings.Current.Language=language;MatchSettings.Current.AutoSubstitutionsEnabled=true;
    var injuryMatch=New();var menuVictim=injuryMatch.HomeTeam.Players[9];var strongest=new Player(1200,"Strongest",15,SpecificPosition.ST,99,99,99,99,99);var chosen=new Player(1201,"My choice",16,SpecificPosition.ST,60,60,60,60,60);
    injuryMatch.HomeTeam.Bench.AddRange(new[]{strongest,chosen});injuryMatch.Ball.CatchOrTackle(menuVictim);injuryMatch.ApplyInjury(menuVictim,PlayerInjuryStatus.SevereInjury);
    float frozenClock=injuryMatch.MatchTimeMinutes;var frozenBall=injuryMatch.Ball.Position;injuryMatch.Update(30);injuryMatch.RunNativeAutomation();
    Check(injuryMatch.MatchTimeMinutes==frozenClock&&injuryMatch.Ball.Position==frozenBall&&injuryMatch.HomeTeam.SubstitutionsUsed==0,"injury choice freezes actual clock/ball and optional automatic substitutions: "+language);
    Check(!TacticalAiAdvisor.TryPerformAutoSubstitution(injuryMatch,out _)&&!injuryMatch.MakeSubstitution(injuryMatch.HomeTeam,menuVictim,strongest),"optional assist or another substitution path cannot take the injury choice away: "+language);
    var injuryReader=new Reader();bool resolved=false;var injuryMenu=GamePlanMenuBuilder.CreateInjurySubstitutionMenu(injuryMatch,injuryReader,()=>resolved=true);injuryMenu.Open();
    Check(injuryMenu.MenuTitle.Contains(menuVictim.Name)&&injuryMenu.Count==3&&injuryReader.Spoken.Any(t=>t.Contains("Strongest")),"injury menu names the injured player and exposes the bench to speech: "+language);
    injuryMenu.NavigateNext();Check(injuryReader.Spoken.Last().Contains("My choice"),"arrow navigation announces the actual selected reserve: "+language);injuryMenu.SelectCurrent();
    Check(resolved&&injuryMatch.PendingInjuryPlayer==null&&injuryMatch.HomeTeam.Players.Contains(chosen)&&injuryMatch.HomeTeam.Bench.Contains(strongest),"Enter commits only the user's selected player, not the strongest: "+language);
    Check(injuryMatch.Ball.Owner==chosen&&injuryMatch.HomeTeam.SubstitutionsUsed==1&&injuryMatch.HomeTeam.StoppageWindowsUsed==1,"manual injury choice preserves possession and consumes exactly one legal change: "+language);
}
MatchSettings.Current.AutoSubstitutionsEnabled=false;MatchSettings.Current.Language=UiLanguage.Arabic;
var exhausted=New();exhausted.HomeTeam.SubstitutionsUsed=5;var exhaustedVictim=exhausted.HomeTeam.Players[9];exhausted.ApplyInjury(exhaustedVictim,PlayerInjuryStatus.SevereInjury);
var exhaustedReader=new Reader();bool acknowledged=false;var unavailableMenu=GamePlanMenuBuilder.CreateInjurySubstitutionMenu(exhausted,exhaustedReader,()=>acknowledged=true);unavailableMenu.Open();
Check(unavailableMenu.Count==1&&exhausted.HomeTeam.Players.Count(p=>p.IsActiveOnPitch)==10,"exhausted substitutions show a truthful continue-short option and actually remove the injured player");unavailableMenu.SelectCurrent();
Check(acknowledged&&exhausted.PendingInjuryPlayer==null&&exhausted.HomeTeam.SubstitutionsUsed==5,"acknowledging no legal reserve does not invent or charge a replacement");
// Source facts stay separate from game balancing and unknown fields.
if(dbArgument >= 0)
{
 var restoredDatabaseType=typeof(JsonDatabaseProvider);
 restoredDatabaseType.GetMethod("LoadDatabaseFromFile",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{args[dbArgument+1]});
 restoredDatabaseType.GetField("_isInitialized",BindingFlags.Static|BindingFlags.NonPublic)!.SetValue(null,true);
var sourcedAdditions = new[]{("FC Aiglons (Tchad)","Biwé Mangana"),("FC Aiglons (Tchad)","Abderamane Ahmat"),("Panthère Sportive du Ndé","Njoya Mohamed Malik"),("Panthère Sportive du Ndé","Mouen Adolphe Phillipe"),("AS ZAM","Harouna Doulla Younoussa"),("DC Virunga","Djodjo Mushila")};
foreach(var (teamName,playerName) in sourcedAdditions)
{
 var st=JsonDatabaseProvider.GetTeamByName(teamName)!;
 var sp=st.Players.Concat(st.Bench).Single(p=>p.Name==playerName);
 Check(sp.RatingIsEstimated&&sp.RatingProvenance.Contains("55")&&sp.DataSourceUrls.Length>0,"sourced addition carries explicit balance55provenance: "+playerName);
 Check(sp.HasKnownSourcePosition,"new addition has a documented position: "+playerName);
 if(playerName!="Abderamane Ahmat")Check(sp.Age==0&&!sp.HasKnownAge&&sp.SimulationAge==25,"unknown age remains unknown with explicit neutral simulation band: "+playerName);
 var copy=sp.Clone();Check(copy.RatingIsEstimated&&copy.DataSourceUrls.SequenceEqual(sp.DataSourceUrls)&&copy.Age==sp.Age,"provenance survives match clone: "+playerName);
 foreach(var language in new[]{UiLanguage.Arabic,UiLanguage.English})
 { LocalizationService.CurrentLanguage=language;Check(sp.DataProvenanceSummary.Contains(language==UiLanguage.English?"estimated":"تقديرية"),"estimate disclosed in accessible attributes: "+playerName+language); }
}
foreach(var n in new[]{"FC Aiglons (Tchad)","Panthère Sportive du Ndé"})Check(JsonDatabaseProvider.GetTeamByName(n)!.GetMatchStartIssue()==null,"sourced seven-player squad now passes real start guard: "+n);
foreach(var n in new[]{"AS ZAM","DC Virunga"})Check(JsonDatabaseProvider.GetTeamByName(n)!.GetMatchStartIssue()!=null,"incomplete sourced squad remains honestly blocked: "+n);
var unknownSource=JsonDatabaseProvider.GetTeamByName("FC Canchungo")!.Players.Concat(JsonDatabaseProvider.GetTeamByName("FC Canchungo")!.Bench).Where(p=>p.SourcePosition=="Unknown").ToList();
Check(unknownSource.Count==5&&unknownSource.All(p=>p.PrimaryPosition==SpecificPosition.Unknown&&!p.HasKnownSourcePosition),"five existing unknown source positions are not silently rewritten asCM");
Check(unknownSource.All(p=>p.Clone().PrimaryPosition==SpecificPosition.Unknown),"unknown source position survives clone");
LocalizationService.CurrentLanguage=UiLanguage.Arabic;
}
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
