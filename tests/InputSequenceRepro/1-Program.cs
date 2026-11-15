using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Input;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;
using AccessibleFootball2026.Input;

// Diagnostic, not a pass/fail balance target. Random.Shared is NOT seeded.
// Real database teams, actual event handler, dt=20ms. No Windows message pump or NVDA.
MatchSettings.CustomSettingsFilePath=Path.Combine(Path.GetTempPath(),"input-repro-"+Guid.NewGuid()+".json");
MatchSettings.Current.MatchDurationMinutes=10;
MatchSettings.Current.AiVarEnabled=false;
MatchSettings.Current.ExtraTimeEnabled=false;
MatchSettings.Current.PenaltyShootoutEnabled=false;
var database=JsonDatabaseProvider.AllTeams;
if(database.Count<2)throw new Exception("Real player database unavailable");
Console.WriteLine($"database={JsonDatabaseProvider.LoadedDatabasePath}, teams={database.Count}, unseeded Random.Shared");
foreach(var difficulty in Enum.GetValues<DifficultyLevel>())
{
 MatchSettings.Current.Difficulty=difficulty;
 var home=database[0].Clone(TeamSide.Home);var away=database[1].Clone(TeamSide.Away);
 var input=new SequenceInput();var match=new MatchEngine(home,away,new MatchEventBus(),input);
 input.Match=match;match.SetStoppageMinutes(0,0);
 int steps=0,homeOwned=0,awayKeeper=0,awayOutfield=0,loose=0;
 while(steps<600000 && match.State is not (MatchState.GameOver or MatchState.PenaltyShootout))
 {
   input.Tick(steps++);
   match.Update(.02f);
   if(match.State==MatchState.HalfTime)match.ResumeMatch();
   if(match.AwaitingStoppageConfirmation)match.DismissStoppageReplay();
   if(match.State!=MatchState.InPlay)continue;
   if(match.Ball.Owner==null)loose++;
   else if(home.Players.Contains(match.Ball.Owner))homeOwned++;
   else if(match.Ball.Owner.Role==PlayerRole.Goalkeeper)awayKeeper++;
   else awayOutfield++;
 }
 var h=match.StatisticsFor(home);var a=match.StatisticsFor(away);
 Console.WriteLine($"difficulty={difficulty} teams={home.Name}/{away.Name} state={match.State} minute={match.MatchTimeMinutes:F1} ticks={steps} score={home.Score}-{away.Score}");
 Console.WriteLine($"inplay owner ticks home={homeOwned} awayGK={awayKeeper} awayOutfield={awayOutfield} loose={loose}; passes={h.Passes}/{a.Passes}; fouls={h.Fouls}/{a.Fouls}; freeKicks={h.FreeKicks}/{a.FreeKicks}; S press/tackleActions={input.SPresses}/{input.TackleActions}; Ctrl press/actions={input.CtrlPresses}/{input.CtrlActions}");
 foreach(var line in match.GetStatisticsAnnouncements().Where(x=>x.Contains("الاستحواذ")))Console.WriteLine(line);
}
sealed class SequenceInput:IInputHandler
{
 public MatchEngine Match=null!;
 readonly KeyboardInputHandler kb=new();
 bool wasHome=true;public int SPresses,CtrlPresses,TackleActions,CtrlActions;
 public void Tick(int tick)
 {
  bool home=Match.Ball.Owner!=null&&Match.HomeTeam.Players.Contains(Match.Ball.Owner);
  if(tick==0){kb.HandleKeyDown(0x10,home);kb.HandleKeyDown(0x27,home);}
  if(wasHome&&!home){kb.HandleKeyDown(0x11,home);CtrlPresses++;}
  else kb.HandleKeyUp(0x11);
  if(tick%8==0){kb.HandleKeyDown(0x53,home);SPresses++;}
  if(tick%8==1)kb.HandleKeyUp(0x53);
  wasHome=home;
 }
 public HashSet<GameAction> PollCurrentActions(bool possession){var a=kb.PollCurrentActions(possession);if(a.Contains(GameAction.StandTackle))TackleActions++;if(a.Contains(GameAction.SwitchPlayer))CtrlActions++;return a;}
 public bool IsActionTriggered(GameAction a,bool p)=>false;
}
