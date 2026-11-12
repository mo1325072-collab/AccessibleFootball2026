using System.Numerics;
using System.Reflection;
using AccessibleFootball2026.Audio;
using AccessibleFootball2026.Audio.Anthems;
using AccessibleFootball2026.Audio.Commentary;
using AccessibleFootball2026.App.Menus;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Input;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;
int passed=0,failed=0;
void Check(bool ok,string name){Console.WriteLine((ok?"PASS ":"FAIL ")+name);if(ok)passed++;else failed++;}
Team Team(string id,TeamSide side=TeamSide.Home){var t=new Team(id,id,side);for(int i=0;i<18;i++){var p=new Player((side==TeamSide.Home?0:100)+i,id+i,i+1,i is 0 or 11?SpecificPosition.GK:i<5||i is 12 or 13?SpecificPosition.CB:i<8||i is 14 or 15?SpecificPosition.CM:SpecificPosition.ST,75,75,75,75,75);if(i<11)t.Players.Add(p);else t.Bench.Add(p);}t.SetupFormation433();return t;}
MatchSettings.Current.ResetToDefaults();MatchSettings.Current.ExtraTimeEnabled=false;MatchSettings.Current.PenaltyShootoutEnabled=false;
var input=new Input();MatchEngine Match()=>new(Team("h"),Team("a",TeamSide.Away),new MatchEventBus(),input);
// The draft must produce real unique squad members even when source clubs overlap.
var duplicateClub=Team("duplicate");duplicateClub.Bench.AddRange(duplicateClub.Players.Select(p=>p.Clone()));var draft=new DreamTeamBuilder();draft.AddUserSourceClub(duplicateClub);draft.AddCpuSourceClub(Team("other",TeamSide.Away));draft.DraftBothTeams(new Random(22));
Check(draft.UserDreamTeam.Players.Concat(draft.UserDreamTeam.Bench).Select(p=>p.Id).Distinct().Count()==draft.UserDreamTeam.Players.Count+draft.UserDreamTeam.Bench.Count,"overlapping draft source records cannot duplicate a player within one squad");
var firstCaptain=draft.UserDreamTeam.Captain;draft.UserDreamTeam.Score=4;draft.UserDreamTeam.SubstitutionsUsed=4;draft.DraftBothTeams(new Random(23));
Check(draft.UserDreamTeam.Players.Contains(draft.UserDreamTeam.Captain!),"redraw captain belongs to the newly drafted team, not previous objects");
Check(draft.UserDreamTeam.Players.Contains(draft.UserDreamTeam.PenaltyTaker!)&&draft.UserDreamTeam.Players.Contains(draft.UserDreamTeam.FreeKickTaker!)&&draft.UserDreamTeam.Players.Contains(draft.UserDreamTeam.CornerTaker!),"redraw specialist roles point to actual new starters");
Check(draft.UserDreamTeam.Score==0&&draft.UserDreamTeam.SubstitutionsUsed==0,"redraw discards prior match counters");
var selection=new DreamTeamBuilder();var clubs=Enumerable.Range(0,7).Select(i=>Team("club"+i)).ToList();
for(int i=0;i<4;i++)Check(selection.SelectSourceClubAt(false,i,clubs[i]),"draft selection accepts user step "+i);
for(int i=0;i<4;i++)Check(selection.SelectSourceClubAt(true,i,clubs[i]),"draft selection permits source clubs shared with opponent step "+i);
Check(selection.SelectSourceClubAt(false,1,clubs[5])&&selection.UserSourceClubs.Select(t=>t.Id).SequenceEqual(new[]{clubs[0].Id,clubs[5].Id})&&selection.CpuSourceClubs.Count==0,"back/reselect replaces numbered choice and invalidates later user/opponent choices");
Check(!selection.SelectSourceClubAt(false,2,clubs[0])&&selection.UserSourceClubs.Count==2,"duplicate club rejection does not advance or mutate the draft");
Check(!selection.SelectSourceClubAt(false,3,clubs[3])&&selection.UserSourceClubs.Count==2,"out-of-order stale draft action cannot skip a required choice");
Check(selection.SelectSourceClubAt(false,2,clubs[2])&&selection.SelectSourceClubAt(false,3,clubs[3]),"reselection can finish the remaining user choices");
Check(selection.SelectSourceClubAt(true,0,clubs[0])&&selection.SelectSourceClubAt(true,1,clubs[1])&&selection.SelectSourceClubAt(true,0,clubs[6])&&selection.CpuSourceClubs.Count==1&&selection.CpuSourceClubs[0]==clubs[6],"opponent Back/reselect replaces rather than appends to full source list");
// Menus must neither offer inactive specialists nor announce failed position swaps as successful.
foreach(var lang in new[]{UiLanguage.Arabic,UiLanguage.English})
{
 MatchSettings.Current.Language=lang;var game=Match();game.IssueCard(game.HomeTeam.Players[2],true);var sentOff=game.HomeTeam.Players[2];var reader=new Reader();var host=new Host();
 var specialists=GamePlanMenuBuilder.CreateSpecialistsMenu(game.HomeTeam,reader,host.Push,host.Pop);specialists.GetItem(0).OnSelect!();
 Check(Enumerable.Range(0,host.Current!.Count).All(i=>!host.Current.GetItem(i).Title.Contains(sentOff.Name)),"captain selection excludes sent-off players: "+lang);
 var lineup=GamePlanMenuBuilder.CreateLineupMenu(game.HomeTeam,reader,host.Push,host.Pop,game);lineup.GetItem(0).OnSelect!();var actions=host.Current!;actions.GetItem(0).OnSelect!();var swaps=host.Current!;
 Check(Enumerable.Range(0,swaps.Count).All(i=>!swaps.GetItem(i).Title.Contains(sentOff.Name)),"position-swap menu excludes inactive targets: "+lang);
 // Existing public menu action captured before a dismissal must revalidate at execution time.
 var valid=game.HomeTeam.Players[1];var swapItem=Enumerable.Range(0,swaps.Count).Select(swaps.GetItem).First(i=>i.Title.Contains(valid.Name));game.IssueCard(valid,true);reader.Spoken.Clear();swapItem.OnSelect!();
 Check(!reader.Spoken.Any(t=>t.Contains("swapped positions")||t.Contains("تم تبديل الموضعين بنجاح")),"stale position selection cannot falsely announce success: "+lang);
}
// A menu captured before a dismissal must also reject stale specialist actions.
foreach(var role in Enumerable.Range(0,4))
{
 var game=Match();var reader=new Reader();var host=new Host();var menu=GamePlanMenuBuilder.CreateSpecialistsMenu(game.HomeTeam,reader,host.Push,host.Pop);menu.GetItem(role).OnSelect!();
 var candidate=game.HomeTeam.Players[5];var action=Enumerable.Range(0,host.Current!.Count).Select(host.Current.GetItem).First(x=>x.Title.Contains(candidate.Name));
 game.IssueCard(candidate,true);action.OnSelect!();
 Check(new[]{game.HomeTeam.Captain,game.HomeTeam.PenaltyTaker,game.HomeTeam.FreeKickTaker,game.HomeTeam.CornerTaker}[role]!=candidate,"stale specialist selection revalidates role "+role);
}
// Formation selection must change both real field slots and the positions the AI consumes.
foreach(var (formation,expected) in new (FormationType,SpecificPosition[])[]{
 (FormationType.Formation343_AllOut,new[]{SpecificPosition.CB,SpecificPosition.CB,SpecificPosition.CB,SpecificPosition.LM,SpecificPosition.CM,SpecificPosition.CM,SpecificPosition.RM,SpecificPosition.LW,SpecificPosition.ST,SpecificPosition.RW}),
 (FormationType.Formation41212_Diamond,new[]{SpecificPosition.LB,SpecificPosition.CB,SpecificPosition.CB,SpecificPosition.RB,SpecificPosition.CDM,SpecificPosition.CM,SpecificPosition.CM,SpecificPosition.CAM,SpecificPosition.ST,SpecificPosition.ST}),
 (FormationType.Formation433_Defend,new[]{SpecificPosition.LB,SpecificPosition.CB,SpecificPosition.CB,SpecificPosition.RB,SpecificPosition.CDM,SpecificPosition.CM,SpecificPosition.CM,SpecificPosition.LW,SpecificPosition.ST,SpecificPosition.RW})})
{
 var t=Team("formation");var primary=t.Players.Select(p=>p.PrimaryPosition).ToArray();var reader=new Reader();var menu=GamePlanMenuBuilder.CreateFormationMenu(t,reader,()=>{});menu.GetItem(FormationManager.GetAllFormations().IndexOf(formation)).OnSelect!();
 Check(t.Players.Where(p=>p.Role!=PlayerRole.Goalkeeper).Select(p=>p.CurrentAssignedPosition).SequenceEqual(expected),"formation menu assigns actual consumer positions: "+formation);
 Check(primary.SequenceEqual(t.Players.Select(p=>p.PrimaryPosition)),"formation never rewrites source primary positions: "+formation);
 if(formation==FormationType.Formation343_AllOut)Check(t.Players.Count(p=>p.HomePosition.X<30)==4,"3-4-3 has exactly three defensive field slots plus keeper");
 if(formation==FormationType.Formation41212_Diamond)Check(t.Players.Count(p=>p.HomePosition.X>65)==2,"diamond has two physical striker slots, not three-winger fallback");
}
// Ready restarts still belong to the same explicit radar/lineup journey after the held replay.
var restartMatch=Match();restartMatch.AwardFoulRestart(restartMatch.HomeTeam.Players[9],new Vector2(75,34));restartMatch.DismissStoppageReplay();var restartEvents=new List<MatchEvent>();restartMatch.EventBus.OnMatchEvent+=restartEvents.Add;
input.Actions.Add(GameAction.TacticalRadar);restartMatch.Update(.02f);Check(restartEvents.Any(e=>e.Type==MatchEventType.TacticalRadarInfo),"radar is available while aiming a ready free kick, not only during its held replay");
input.Actions.Add(GameAction.RequestLineup);restartMatch.Update(.02f);Check(restartEvents.Any(e=>e.Type==MatchEventType.LineupRequested),"lineup request remains available during a ready restart");
var time=restartMatch.MatchTimeMinutes;var pos=restartMatch.Ball.Position;restartMatch.Update(float.NaN);Check(float.IsFinite(restartMatch.MatchTimeMinutes)&&float.IsFinite(restartMatch.Ball.Position.X),"invalid frame delta cannot poison a live match with NaN");
// Ready opponent restarts must not swallow explicit information requests or pause.
foreach(var kind in new[]{RestartKind.FreeKick,RestartKind.Penalty,RestartKind.Corner,RestartKind.ThrowIn,RestartKind.GoalKick})
{
 var game=Match();var events=new List<MatchEvent>();game.EventBus.OnMatchEvent+=events.Add;
 if(kind==RestartKind.Corner)typeof(MatchEngine).GetMethod("AwardCornerKick",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(game,new object[]{false});
 else if(kind==RestartKind.ThrowIn)typeof(MatchEngine).GetMethod("AwardThrowIn",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(game,new object[]{false});
 else if(kind==RestartKind.GoalKick)typeof(MatchEngine).GetMethod("AwardGoalKick",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(game,new object[]{false});
 else game.AwardFoulRestart(game.AwayTeam.Players[9],new Vector2(kind==RestartKind.Penalty?5:30,34));
 game.DismissStoppageReplay();input.Actions.UnionWith(new[]{GameAction.TacticalRadar,GameAction.RequestLineup});game.Update(.02f);
 Check(events.Any(e=>e.Type==MatchEventType.TacticalRadarInfo)&&events.Any(e=>e.Type==MatchEventType.LineupRequested),"ready opponent "+kind+" accepts radar and lineup");
 input.Actions.Add(GameAction.Pause);game.Update(.02f);Check(game.State==MatchState.Paused,"ready opponent "+kind+" honors pause without taking the kick");
}
// Formation effects survive new-match construction and halftime; source identity remains unchanged.
var custom=Team("custom");custom.ChangeFormation(FormationType.Formation343_AllOut);custom.Players[4].CurrentAssignedPosition=SpecificPosition.CDM;
var customGame=new MatchEngine(custom,Team("opposition",TeamSide.Away),new MatchEventBus(),input);
Check(custom.Players[4].CurrentAssignedPosition==SpecificPosition.CDM,"starting match preserves an individually edited position after formation choice");
var beforeEnds=custom.Players.Select(p=>p.CurrentAssignedPosition).ToArray();customGame.TriggerHalfTime();Check(beforeEnds.SequenceEqual(custom.Players.Select(p=>p.CurrentAssignedPosition)),"halftime mirrors physical slots without discarding individually assigned positions");
Check(custom.Players.All(p=>p.HomePosition.X>20),"3-4-3 actual formation slots mirror to the correct second-half end");
// Exercise the selected extracted WAV path, not just synthetic filename/pan telemetry.
var audioRoot=Path.Combine(Path.GetTempPath(),"journey-audio-"+Guid.NewGuid());
foreach(var relative in new[]{"SFX/Kicks/Kick_101.wav","SFX/Tackles/StandTackle.wav","SFX/Tackles/SlideTackle.wav"}){var path=Path.Combine(audioRoot,relative);Directory.CreateDirectory(Path.GetDirectoryName(path)!);File.WriteAllBytes(path,new byte[100]);}
var commands=new List<string>();MciPlayback.TestCommand=(command,buffer)=>{commands.Add(command);buffer?.Append("stopped");return 0;};
MatchSettings.Current.ResetToDefaults();MatchSettings.Current.MasterVolumePercent=50;MatchSettings.Current.SfxVolumePercent=40;SoundEffectsPlayer.Initialize(audioRoot);
foreach(var (play,alias) in new (Action<float>,string)[]{(SoundEffectsPlayer.PlayKick,"kick"),(SoundEffectsPlayer.PlayShotKick,"shot_kick"),(SoundEffectsPlayer.PlayPassKick,"pass_kick"),(SoundEffectsPlayer.PlayCrossKick,"cross_kick"),(SoundEffectsPlayer.PlayStandTackle,"stand_tackle"),(SoundEffectsPlayer.PlaySlideTackle,"slide_tackle")})
{
 foreach(var pan in new[]{-1f,0f,1f})
 {
  commands.Clear();play(pan);var at=commands.FindIndex(c=>c==$"play {alias} from 0");
  int left=pan>0?0:200,right=pan<0?0:200;
  Check(at>0&&commands.Take(at).Contains($"setaudio {alias} left volume to {left}")&&commands.Take(at).Contains($"setaudio {alias} right volume to {right}"),$"{alias} sets real left/right channels before playback at pan {pan}");
 }
 commands.Clear();MatchSettings.Current.MasterVolumePercent=25;MatchSettings.Current.ApplyLive();
 Check(commands.Contains($"setaudio {alias} left volume to 0")&&commands.Contains($"setaudio {alias} right volume to 100"),alias+" live volume preserves active right pan");
 MatchSettings.Current.MasterVolumePercent=50;MatchSettings.Current.ApplyLive();
}
// Academy previews replace each other and stop on Back, while explanations stay in the guide.
var anthem=new AnthemEngine(new Reader(),audioRoot);
{
 var reader=new Reader();var host=new Host();var academy=SoundAcademyMenuBuilder.CreateSoundAcademyMenu(reader,anthem,host.Push,host.Pop);host.Push(academy);
 var categories=SoundAcademyDatabase.GetCategories();var earcons=categories.FindIndex(c=>c.Id=="earcons");academy.GetItem(earcons).OnSelect!();var preview=host.Current!;
 commands.Clear();preview.GetItem(0).OnSelect!();Check(commands.Any(c=>c.StartsWith("play ")),"reachable academy earcon sample uses real playback path");
 commands.Clear();preview.GetItem(1).OnSelect!();Check(commands.Contains("close all"),"a second academy preview stops the prior looping sample first");
 commands.Clear();preview.GetItem(preview.Count-1).OnSelect!();Check(commands.Contains("close all")&&host.Current==academy,"academy Back stops previews and returns to its root");
 commands.Clear();host.Pop();Check(commands.Contains("close all"),"leaving academy closes any remaining preview channels");
 var guide=UserGuideMenuBuilder.CreateUserGuideMenu(reader,host.Push,host.Pop);var modes=UserGuideDatabase.GetCategories().FindIndex(c=>c.Id=="modes");guide.GetItem(modes).OnSelect!();reader.Spoken.Clear();host.Current!.GetItem(1).OnSelect!();Check(reader.Spoken.Any(t=>t.Contains("4")),"mode explanation remains reachable by selecting its guide entry");
}
commands.Clear();MciPlayback.TestCommand=(command,buffer)=>{commands.Add(command);return command.Contains("left volume")?1:0;};SoundEffectsPlayer.PlayKick(-1);
Check(!commands.Contains("play kick from 0"),"failed spatial-channel setup cannot play an incorrectly centered effect");
MciPlayback.TestCommand=null;
Console.WriteLine($"RESULT {passed} passed, {failed} failed. Native Windows UI/audio was not executed.");return failed==0?0:1;
sealed class Reader:IScreenReader{public List<string> Spoken=new();public bool IsAvailable=>true;public void Speak(string text,bool interrupt=true)=>Spoken.Add(text);public void CancelSpeech(){}public Task SpeakUntilFinishedAsync(string text,bool interrupt=true){Speak(text,interrupt);return Task.CompletedTask;}}
sealed class Input:IInputHandler{public HashSet<GameAction> Actions=new();public HashSet<GameAction> PollCurrentActions(bool possession){var x=Actions.ToHashSet();Actions.Clear();return x;}public bool IsActionTriggered(GameAction action,bool possession)=>Actions.Remove(action);}
sealed class Host{public AccessibleMenu? Current;public Stack<AccessibleMenu> History=new();public void Push(AccessibleMenu menu){if(Current!=null)History.Push(Current);Current=menu;menu.Open();}public void Pop(){Current?.Close();Current=History.Count>0?History.Pop():null;Current?.Open();}}
