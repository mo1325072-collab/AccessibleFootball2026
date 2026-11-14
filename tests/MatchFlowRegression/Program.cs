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

var menuAudioRoot=Path.Combine(Path.GetTempPath(),"approved-ui-"+Guid.NewGuid());
var oldAudioRoot=SoundEffectsPlayer.BaseAudioDirectory;
SoundEffectsPlayer.Initialize(menuAudioRoot);
var menuBytes=new[]{("Move","4c884c7e50835ecb6327813c66e38aad1588cee416c3da769fb05db7050b14df"),("Select","8f9f9ebdbcd75b77ad2397f3611a285e7d5cfe98bfff033fbcbfd0ae449781ee")};
foreach(var (name,hash) in menuBytes){var uiHash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(menuAudioRoot,"Menu",name+".wav")))).ToLowerInvariant();if(uiHash!=hash)throw new Exception("approved UI sound changed: "+name);}
var uiCommands=new List<string>();
var mciHook=typeof(SoundEffectsPlayer).Assembly.GetType("AccessibleFootball2026.Audio.MciPlayback")!.GetField("TestCommand",BindingFlags.Static|BindingFlags.NonPublic)!;
Func<string,System.Text.StringBuilder?,int> uiTransport=(command,buffer)=>{uiCommands.Add(command);buffer?.Append("stopped");return command.Contains("type waveaudio")?1:0;};
mciHook.SetValue(null,uiTransport);
SoundEffectsPlayer.MasterVolumeSetting=100;SoundEffectsPlayer.SfxVolumeSetting=90;
var uiMenu=new AccessibleMenu("UI",new Reader());uiMenu.AddItem("one",()=>{});uiMenu.AddItem("two",()=>{});
uiMenu.NavigateNext();uiMenu.SelectCurrent();
if(!uiCommands.Contains("play ui_move from 0")||!uiCommands.Contains("play ui_select from 0")||!uiCommands.Any(x=>x.Contains("Move.wav")&&x.Contains("type mpegvideo"))||!uiCommands.Any(x=>x.Contains("Select.wav")&&x.Contains("type mpegvideo")))throw new Exception("real menu actions must activate approved Move and Select with compatible playback");
SoundEffectsPlayer.StopAll();mciHook.SetValue(null,null);SoundEffectsPlayer.Initialize(oldAudioRoot);
Console.WriteLine("PASS exact approved Move/Select v2 bytes and real menu navigation/confirmation playback command paths");
var cairoGround=StadiumCatalog.Find("CairoInternational");var egyptGroundTeam=JsonDatabaseProvider.GetTeamByName("Egypt")!;
if(cairoGround==null||!cairoGround.IsGroundFor(egyptGroundTeam)||!cairoGround.NationalTeams)throw new Exception("Cairo must be selectable and linked to actual Egypt national team");
if(!JsonDatabaseProvider.GetAllGroups().Any(group=>StadiumCatalog.GetGroundsForProductionGroup(group).Any(ground=>ground.Key=="CairoInternational")))throw new Exception("Cairo must appear through the actual grouped stadium selector");
if(cairoGround.IsGroundFor(TeamDatabase.CreateAlAhly()))throw new Exception("national Egypt link must not invent a club link");
Console.WriteLine("PASS Cairo selectable directly for Egypt and through production-group stadium selection without invented club/audio claims");
// Exclusive lineup input is a separate gate, not match possession/radar input.
foreach(int skipKey in new[]{13,32})
{
    var lineupGate=new AccessibleFootball2026.Core.Input.MatchLineupInput();lineupGate.Begin();
    if(!lineupGate.KeyDown(87,out var skipShot)||skipShot||!lineupGate.IsActive)throw new Exception("lineup consumes shot without starting gameplay");
    if(!lineupGate.KeyDown(skipKey,out var skip)||!skip)throw new Exception("Enter/Space must request lineup skip");
    lineupGate.Complete();
    if(!lineupGate.KeyDown(skipKey,out skip)||skip)throw new Exception("held skip cannot become gameplay action through repeat");
    if(!lineupGate.KeyUp(skipKey)||lineupGate.KeyDown(skipKey,out skip))throw new Exception("released skip key is available for next fresh gameplay action");
    lineupGate.Begin();lineupGate.KeyDown(skipKey,out skip);lineupGate.Complete();lineupGate.ReleaseAll();
    if(lineupGate.KeyDown(skipKey,out skip))throw new Exception("focus-loss reset must release skip latch");
    lineupGate.Begin();lineupGate.Complete();
    if(lineupGate.IsActive||lineupGate.KeyDown(87,out skip))throw new Exception("natural completion enables gameplay without a key");
}
Console.WriteLine("PASS lineup gate consumes controls, Enter/Space skips, held skip repeats cannot trigger radar, natural completion unlocks play");

var direct = new DirectShootoutInput();
direct.KeyDown(0x26);direct.Tick(.2f);direct.KeyUp(0x26);
if(Math.Abs(direct.FacingY+.18f)>.001f)throw new Exception("up must use ordinary negative facing-Y turn rate");
var aimBefore=direct.AimY;direct.Tick(.2f);if(direct.AimY!=aimBefore)throw new Exception("released aim must stop turning");
direct.KeyDown(0x27);if(direct.AimY!=3.25f||!direct.KeyDown(0x57)||direct.KeyDown(0x57))throw new Exception("right+W must submit exactly one real corner shot");
var leftInput=new DirectShootoutInput();leftInput.KeyDown(0x25);if(leftInput.AimY!=-3.25f||!leftInput.KeyDown(0x53))throw new Exception("left+S must submit a placed corner shot");
var anticipated=ShootoutKick.ResolveAimed(3.25f,0,80,80,true,.5);
var wrongWay=ShootoutKick.ResolveAimed(3.25f,2,80,80,true,.5);
if(!anticipated.Saved||!wrongWay.Scored)throw new Exception("keeper anticipation must matter for a placed corner, not unconditional low-power saves");
var powerMisses=0;var placedMisses=0;
for(int i=0;i<=100;i++){double roll=i/100.0;if(!ShootoutKick.ResolveAimed(3.25f,2,50,70,false,roll).Scored)powerMisses++;if(!ShootoutKick.ResolveAimed(3.25f,2,50,70,true,roll).Scored)placedMisses++;}
if(placedMisses>=powerMisses)throw new Exception("precision placement must really reduce goal-mouth misses");
Console.WriteLine("PASS direct arrows/W/S, aim release, held-key edge, placement accuracy and keeper anticipation checks");
MatchSettings.Current.AiVarEnabled=false; // Legacy direct-decision fixtures; explicit VAR fixtures below enable review.
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
Check(interruptions == 0, "goals and penalties now also wait instead of preempting commentary");

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
var completedHomeBefore=m.HomeTeam.Score;var completedAwayBefore=m.AwayTeam.Score;
m.CompleteShootout(1,2);
var finalSummary=m.GetCompletedMatchSummary(1,2);
Check(m.State==MatchState.GameOver && finalSummary[0].Contains("خسر"),"shootout result explicitly reports the user's loss");
Check(m.HomeTeam.Score==completedHomeBefore && m.AwayTeam.Score==completedAwayBefore,"penalty totals never overwrite match goals or statistics");
Check(finalSummary.Any(x=>x.Contains("ركلات الترجيح")&&x.Contains("1")&&x.Contains("2")) && finalSummary.Any(x=>x.Contains("التسديدات")),"retained final summary includes distinct shootout score and actual match statistics");
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
Check(TacticalAiAdvisor.GenerateTacticalReport(m).Contains("بالقواعد المحلية")&&!TacticalAiAdvisor.GenerateTacticalReport(m).Contains("Qwen"),"tactical report uses code rules without model lookup or model branding");

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
Check(typeof(TacticalAiAdvisor).GetMethod("QueryNeuralQwenModel")==null && typeof(TacticalAiAdvisor).GetMethod("RunModelProcess",BindingFlags.Static|BindingFlags.NonPublic)==null,"current advisor has no model process or lookup API");

// The later R1 controls in ORIGINAL_REQUEST supersede the earlier D/S draft mapping.
foreach(var pair in new[]{(0x53,GameAction.ShortPass),(0x44,GameAction.Cross),(0x57,GameAction.Shoot),(0x41,GameAction.ThroughBall),(0x11,GameAction.SwitchPlayer),(0xA2,GameAction.SwitchPlayer),(0xA3,GameAction.SwitchPlayer)})
{
    keyboard.Reset();keyboard.HandleKeyDown(0xA0,true);keyboard.HandleKeyDown(0x27,true);keyboard.HandleKeyDown(pair.Item1,true);
    Check(keyboard.PollCurrentActions(true).SetEquals(new[]{GameAction.Sprint,GameAction.MoveRight,pair.Item2}),"modifier movement and action coexist: "+pair.Item1);
    keyboard.HandleKeyDown(pair.Item1,false);
    Check(keyboard.PollCurrentActions(false).SetEquals(new[]{GameAction.Sprint,GameAction.MoveRight}),"held key cannot turn into tackle after possession changes: "+pair.Item1);
    keyboard.HandleKeyUp(pair.Item1);keyboard.HandleKeyUp(0xA0);keyboard.HandleKeyUp(0x27);
    Check(keyboard.PollCurrentActions(false).Count==0,"release emits no phantom action: "+pair.Item1);
}
foreach(var arrow in new[]{0x25,0x26,0x27,0x28})
{
 keyboard.Reset();keyboard.HandleKeyDown(arrow,true);var first=keyboard.PollCurrentActions(true);Check(first.Count==1&&keyboard.PollCurrentActions(true).SetEquals(first),"arrow remains continuous "+arrow);keyboard.HandleKeyUp(arrow);Check(keyboard.PollCurrentActions(true).Count==0,"arrow release stops movement "+arrow);
}

keyboard.Reset();keyboard.HandleKeyDown(0xA0,true);keyboard.HandleKeyDown(0xA1,true);keyboard.HandleKeyUp(0xA0);Check(keyboard.PollCurrentActions(true).Contains(GameAction.Sprint),"releasing leftShift does not cancel still-held rightShift");keyboard.HandleKeyUp(0xA1);Check(!keyboard.PollCurrentActions(true).Contains(GameAction.Sprint),"lastShift release ends sprint");
keyboard.Reset();keyboard.HandleKeyDown(0xA2,false);Check(keyboard.PollCurrentActions(false).Contains(GameAction.SwitchPlayer),"firstphysicalCtrl switches once");keyboard.HandleKeyDown(0xA3,false);keyboard.HandleKeyUp(0xA2);keyboard.HandleKeyDown(0xA3,false);Check(!keyboard.PollCurrentActions(false).Contains(GameAction.SwitchPlayer),"secondheldCtrl remains debounced after releasing firstCtrl");keyboard.HandleKeyUp(0xA3);keyboard.HandleKeyDown(0xA3,false);Check(keyboard.PollCurrentActions(false).Contains(GameAction.SwitchPlayer),"allCtrl released permits a fresh switch");

float AimHeld(float step,int frames)
{
 var keys=new AccessibleFootball2026.Input.KeyboardInputHandler();var game=new MatchEngine(Make("aimh",TeamSide.Home),Make("aima",TeamSide.Away),new MatchEventBus(),keys);var actor=game.ActiveUserPlayer;actor.FacingDirection=new Vector2(1,0);game.Ball.CatchOrTackle(actor);keys.HandleKeyDown(0x26,true);for(int f=0;f<frames;f++)Invoke(game,"ProcessUserInput",step);return actor.FacingDirection.Y;
}
var fineAim=AimHeld(.02f,10);var coarseAim=AimHeld(.1f,2);Check(Math.Abs(fineAim-coarseAim)<.001f&&fineAim<-.1f&&fineAim>-.3f,"held aim is gradual and frame-rate independent, not saturated after two frames");

// Real incident-driven VAR, not a name-only simulated verdict.
MatchSettings.Current.AiVarEnabled=true;
m=New();var varEvents=new List<MatchEvent>();m.EventBus.OnMatchEvent+=varEvents.Add;
var varVictim=m.HomeTeam.Players[9];var varSpot=new Vector2(99,34);
m.AwardFoulRestart(varVictim,varSpot);var varTime=m.MatchTimeMinutes;var varBall=m.Ball.Position;
Check(m.VideoReviewPending&&m.Restart==null,"penalty incident starts actual announced review before restart is awarded");
Check(varEvents.Count(e=>e.Type==MatchEventType.VideoReviewStarted)==1,"one audible start event per actual review");
m.Update(3);Check(m.VideoReviewPending&&m.MatchTimeMinutes==varTime&&m.Ball.Position==varBall,"VAR wait freezes actual ball and clock for several seconds");
Check(!m.DismissStoppageReplay(),"Enter cannot bypass pending video decision");
m.Update(1);Check(!m.VideoReviewPending&&m.Restart?.Kind==RestartKind.Penalty,"review commits actual penalty once after delay");
Check(varEvents.Count(e=>e.Type==MatchEventType.VideoReviewDecision)==1&&varEvents.Count(e=>e.Type==MatchEventType.PenaltyAwarded)==1,"penalty result and effect are emitted exactly once");
m=New();var sent=m.AwayTeam.Players[4];m.IssueCard(sent,true,"fixture serious foul");Check(m.VideoReviewPending&&!sent.IsSentOff,"red card waits for review instead of removing player early");m.Update(4);Check(sent.IsSentOff&&!m.VideoReviewPending,"confirmed red card changes real roster after decision");
m=New();m.Ball.Position=new Vector3(106,34,1);Invoke(m,"CheckRefereeRules");
Check(m.VideoReviewPending&&m.HomeTeam.Score==0,"goal check does not increment score before confirmation");m.Update(4);Check(m.HomeTeam.Score==1&&!m.VideoReviewPending,"confirmed goal increments real score exactly once");m.Update(4);Check(m.HomeTeam.Score==1,"goal review cannot rescore on a later update");
m=New();bool speechBusy=true;m.VideoAnnouncementPending=()=>speechBusy;m.AwardFoulRestart(m.HomeTeam.Players[9],new Vector2(99,34));m.Update(20);Check(m.VideoReviewPending&&m.Restart==null,"uncompleted start speech cannot be cut off by review timer");speechBusy=false;m.EventBus.OnMatchEvent+=e=>{if(e.Type==MatchEventType.VideoReviewDecision)speechBusy=true;};m.Update(4);Check(m.VideoReviewPending&&m.Restart==null,"verdict speech completes before actual decision effects can interrupt it");speechBusy=false;m.Update(.1f);Check(m.Restart?.Kind==RestartKind.Penalty,"completed verdict releases held penalty effect");
m=New();m.AwardFoulRestart(m.HomeTeam.Players[9],new Vector2(Pitch.Length-Pitch.PenaltyBoxDepth-.25f,34));Check(m.VideoReviewPending,"close outside-area foul supports penalty versus no-penalty review");m.Update(4);Check(m.Restart?.Kind==RestartKind.FreeKick,"geometry outside box results in actual free kick, not false penalty");
m=New();var wrongCard=m.AwayTeam.Players[3];var trueOffender=m.AwayTeam.Players[4];Check(m.ReviewCardDecision(wrongCard,trueOffender,false,true,true)&&m.VideoReviewPending,"observed mistaken identity triggers correction review");m.Update(4);Check(wrongCard.YellowCards==0&&trueOffender.YellowCards==1,"identity correction cards actual offender without punishing wrong player");
m=New();var reviewedBooked=m.AwayTeam.Players[3];reviewedBooked.YellowCards=1;Check(m.ReviewCardDecision(reviewedBooked,reviewedBooked,false,false,true),"clearly incorrect second caution is reviewable under2026/27");m.Update(4);Check(reviewedBooked.YellowCards==1&&!reviewedBooked.IsSentOff,"cancelled incorrect second caution preserves first caution and eleven players");
m=New();var offRunner=m.HomeTeam.Players[8];offRunner.Position2D=new Vector2(90.1f,34);for(int oi=0;oi<m.AwayTeam.Players.Count;oi++)m.AwayTeam.Players[oi].Position2D=new Vector2(oi==0?100:90,20+oi);
m.CheckOffsideTrap(m.HomeTeam,m.AwayTeam,offRunner,new Vector2(100,34));m.Ball.Owner=null;m.Ball.Position=new Vector3(offRunner.Position2D,0);m.TryControlIncomingBall(offRunner,testRoll:1);m.Ball.Owner=null;m.Ball.Position=new Vector3(105.1f,34,1);Invoke(m,"CheckRefereeRules");Check(m.VideoReviewPending&&m.HomeTeam.Score==0,"actual kick-time offside plus goal starts review with no premature score");m.Update(4);Check(m.HomeTeam.Score==0&&m.Restart?.Kind==RestartKind.IndirectFreeKick,"VAR disallowed goal produces actual defending indirect restart and no score");
m=New();m.AwardFoulRestart(m.HomeTeam.Players[9],new Vector2(99,34));m.TriggerHalfTime();Check(!m.VideoReviewPending,"period transition clears stale pending video decision");
MatchSettings.Current.AiVarEnabled=false;
m=New();m.AwardFoulRestart(m.HomeTeam.Players[9],new Vector2(99,34));Check(!m.VideoReviewPending&&m.Restart?.Kind==RestartKind.Penalty,"disabled VAR preserves direct referee decision without delay");

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
var sourcedAdditions = new (string Team,string Name,int Age,bool PositionKnown)[]{
("FC Aiglons (Tchad)","Biwé Mangana",0,true),
("FC Aiglons (Tchad)","Abderamane Ahmat",33,true),
("Panthère Sportive du Ndé","Njoya Mohamed Malik",0,true),
("Panthère Sportive du Ndé","Mouen Adolphe Phillipe",0,true),
("AS ZAM","Harouna Doulla Younoussa",0,true),
("DC Virunga","Djodjo Mushila",0,true),
("AS ZAM","Soumaila Moussa Mouslim",0,false),
("AS ZAM","Maman Bachir Moussa Oumarou",0,false),
("AS ZAM","Garba Hassane Ibrahim",0,false),
("AS ZAM","Abdoul Karim Moumouni",0,false),
("AS ZAM","Abdoulaye Garba Ali",0,false),
("AS ZAM","Mahamadou Nouridine Issoufou Tinni",0,false),
("AS ZAM","Ali Saibou Moumouni",0,false),
("AS ZAM","Mahamat Sanoussi",0,false),
("AS ZAM","Seydou Bartche Hamani",0,false),
("AS ZAM","Souman Abdoulaye Abdoul Wahabou",0,false),
("DC Virunga","Artivie Mananga Umba",0,false),
("DC Virunga","Jacques Nkunku Kumbundu",0,false),
("DC Virunga","Grace Gaga Yenga",0,false),
("DC Virunga","Josue Nsimba Mbabu",0,false),
("DC Virunga","Moise Bedi Mokonzo",0,false),
("DC Virunga","Michee Bafumba Isono",0,true),
("DC Virunga","Gedeon Nseka Mambu",0,false),
("DC Virunga","Blanchard Kakese Ntumba",0,false),
("DC Virunga","Elie Mbuyulu Bozana",0,true),
("AS Port-Louis 2000","Frederic Shane Arékion",0,true),
("AS Port-Louis 2000","Cyrille Ondoa",0,true),
("AS Port-Louis 2000","Julien Pellegrin",0,true),
("AS Port-Louis 2000","Wayne Parsooramen",0,true),
("AS Port-Louis 2000","Jovanni Vincent",0,true),
("AS Port-Louis 2000","Jeremie David",0,true),
("AS Port-Louis 2000","Aurelien Kawoa",0,true),
("AS Port-Louis 2000","Darel Arthée",0,true),
("AS Port-Louis 2000","Stéphane Mape",0,true),
("AS Port-Louis 2000","Hanshen Manaroo",0,true),
("AS Port-Louis 2000","Donovan Hortense",0,true),
("AS Port-Louis 2000","Azhar Beegun",0,true),
("AS Port-Louis 2000","Leroy Juliette",0,true),
("AS Port-Louis 2000","Peniel Yoboukoua",0,true),
("AS Port-Louis 2000","Didier Moundoubou",0,true),
("AS Port-Louis 2000","Jean Denis Desrougere",0,true),
("AS Port-Louis 2000","Mohamed Mountala",0,true),
("União Bissau","Hélder Martinho Có",0,true),
("União Bissau","Renickson Djassi",19,true),
("União Bissau","Etivaldo Mendes",23,true),
("União Bissau","Erikson",31,true),
("União Bissau","Lassana Aidará",20,true),
("União Bissau","Rachido Banora",0,true),
("União Bissau","Desiré Mendes",0,true),
("União Bissau","Sada Djop",0,true),
("União Bissau","Idrissa Embaló",0,true)};
foreach(var (teamName,playerName,knownAge,positionKnown) in sourcedAdditions)
{
 var st=JsonDatabaseProvider.GetTeamByName(teamName)!;var sp=st.Players.Concat(st.Bench).Single(p=>p.Name==playerName);
 Check(sp.RatingIsEstimated&&sp.DataSourceUrls.Length>0&&(sp.RatingProvenance.Contains("baseline55")||sp.RatingProvenance.Contains("User-approved random")),"sourced addition carries explicit estimated rating provenance: "+playerName);
 Check(sp.HasKnownSourcePosition==positionKnown && (positionKnown||sp.PrimaryPosition==SpecificPosition.Unknown),"source position certainty preserved: "+playerName);
 Check(knownAge>0?sp.Age==knownAge&&sp.HasKnownAge:sp.Age==0&&!sp.HasKnownAge&&sp.SimulationAge==25,"source age certainty preserved: "+playerName);
 var copy=sp.Clone();Check(copy.RatingIsEstimated&&copy.DataSourceUrls.SequenceEqual(sp.DataSourceUrls)&&copy.Age==sp.Age&&copy.PrimaryPosition==sp.PrimaryPosition,"source and uncertainty survive clone: "+playerName);
 foreach(var language in new[]{UiLanguage.Arabic,UiLanguage.English})
 { LocalizationService.CurrentLanguage=language;Check(sp.DataProvenanceSummary.Contains(language==UiLanguage.English?"estimated":"تقديرية"),"estimate disclosed in accessible attributes: "+playerName+language); }
}
var approvedRandomRatings=new Dictionary<string,int>{
["Soumaila Moussa Mouslim"]=53,
["Maman Bachir Moussa Oumarou"]=50,
["Garba Hassane Ibrahim"]=58,
["Abdoul Karim Moumouni"]=57,
["Abdoulaye Garba Ali"]=59,
["Mahamadou Nouridine Issoufou Tinni"]=54,
["Ali Saibou Moumouni"]=54,
["Mahamat Sanoussi"]=59,
["Seydou Bartche Hamani"]=58,
["Souman Abdoulaye Abdoul Wahabou"]=57,
["Artivie Mananga Umba"]=58,
["Jacques Nkunku Kumbundu"]=58,
["Grace Gaga Yenga"]=55,
["Josue Nsimba Mbabu"]=57,
["Moise Bedi Mokonzo"]=54,
["Michee Bafumba Isono"]=58,
["Gedeon Nseka Mambu"]=58,
["Blanchard Kakese Ntumba"]=56,
["Elie Mbuyulu Bozana"]=58,
["Frederic Shane Arékion"]=55,
["Cyrille Ondoa"]=56,
["Julien Pellegrin"]=59,
["Wayne Parsooramen"]=54,
["Jovanni Vincent"]=50,
["Jeremie David"]=55,
["Aurelien Kawoa"]=53,
["Darel Arthée"]=51,
["Stéphane Mape"]=50,
["Hanshen Manaroo"]=57,
["Donovan Hortense"]=52,
["Azhar Beegun"]=55,
["Leroy Juliette"]=57,
["Peniel Yoboukoua"]=51,
["Didier Moundoubou"]=59,
["Jean Denis Desrougere"]=53,
["Mohamed Mountala"]=51,
["Hélder Martinho Có"]=50,
["Renickson Djassi"]=56,
["Etivaldo Mendes"]=56,
["Erikson"]=53,
["Lassana Aidará"]=57,
["Rachido Banora"]=59,
["Desiré Mendes"]=58,
["Sada Djop"]=57,
["Idrissa Embaló"]=54};
foreach(var pair in approvedRandomRatings)
{
 var sp=JsonDatabaseProvider.AllTeams.SelectMany(t=>t.Players.Concat(t.Bench)).Single(p=>p.Name==pair.Key);
 Check(pair.Value>=50&&pair.Value<=59&&sp.RatingProvenance.Contains("estimate "+pair.Value+" in range50-59"),"new rating stays in approved inclusive range: "+pair.Key);
}
var firstRatingSnapshot=JsonDatabaseProvider.AllTeams.SelectMany(t=>t.Players.Concat(t.Bench)).Where(p=>approvedRandomRatings.ContainsKey(p.Name)).ToDictionary(p=>p.Name,p=>p.RatingProvenance);
restoredDatabaseType.GetMethod("LoadDatabaseFromFile",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{args[dbArgument+1]});
Check(JsonDatabaseProvider.AllTeams.SelectMany(t=>t.Players.Concat(t.Bench)).Where(p=>approvedRandomRatings.ContainsKey(p.Name)).All(p=>firstRatingSnapshot[p.Name]==p.RatingProvenance),"random estimates are assigned in source once, never rerolled by database reload");
foreach(var (n,total) in new[]{("AS ZAM",12),("DC Virunga",14),("AS Port-Louis 2000",22),("União Bissau",11)})
{
 var st=JsonDatabaseProvider.GetTeamByName(n)!;
 Check(st.Players.Count+st.Bench.Count==total&&st.GetMatchStartIssue()==null,"sourced roster count and real start guard: "+n);
 Check(st.Players.Concat(st.Bench).Select(p=>p.Name).Distinct().Count()==total,"no duplicate player names in restored roster: "+n);
 var opponent=TeamDatabase.CreateRealMadrid();var played=new MatchEngine(st.Clone(TeamSide.Home),opponent.Clone(TeamSide.Away),new MatchEventBus(),new Input());
 played.SetStoppageMinutes(0,0);
 var savedDuration=MatchSettings.Current.MatchDurationMinutes;MatchSettings.Current.MatchDurationMinutes=1;
 for(int f=0;f<12000&&played.State is not MatchState.GameOver and not MatchState.PenaltyShootout;f++)
 {
     if(played.State==MatchState.HalfTime)played.ResumeMatch();
     if(played.AwaitingStoppageConfirmation||played.AnalysisReplayPending||played.State==MatchState.GoalCelebration)played.DismissStoppageReplay();
     if(played.PendingInjuryPlayer!=null)played.ResolveInjurySubstitution(played.PendingInjuryPlayer,null);
     played.Update(.2f);
 }
 MatchSettings.Current.MatchDurationMinutes=savedDuration;
 Check(played.HomeTeam.Players.All(p=>float.IsFinite(p.Position2D.X)&&float.IsFinite(p.Position2D.Y))&&played.State is MatchState.GameOver or MatchState.PenaltyShootout,"full real-roster match completes without inventing unknown source identity: "+n);
}
foreach(var n in new[]{"FC Aiglons (Tchad)","Panthère Sportive du Ndé"})Check(JsonDatabaseProvider.GetTeamByName(n)!.GetMatchStartIssue()==null,"existing legal sourced squad remains playable: "+n);
Check(JsonDatabaseProvider.GetTeamByName("Medina United FA")!.GetMatchStartIssue()!=null,"Medina without a verified keeper remains honestly blocked");
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
