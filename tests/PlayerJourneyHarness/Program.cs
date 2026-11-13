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
 (FormationType.Formation343_AllOut,new[]{SpecificPosition.CB,SpecificPosition.CB,SpecificPosition.CB,SpecificPosition.LWB,SpecificPosition.CM,SpecificPosition.CM,SpecificPosition.RWB,SpecificPosition.LW,SpecificPosition.ST,SpecificPosition.RW}),
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
// Deterministic boundary geometry: a pressing defender is already exactly at the cached ball point.
var colocated=Match();var presser=colocated.AwayTeam.Players[2];var carrier=colocated.AwayTeam.Players[9];carrier.Position2D=new Vector2(70,34);colocated.Ball.CatchOrTackle(carrier);presser.Position2D=colocated.Ball.Position2D;
typeof(MatchEngine).GetMethod("UpdatePlayersAI",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(colocated,new object[]{.1f});
Check(float.IsFinite(presser.Position2D.X)&&float.IsFinite(presser.Position2D.Y),"coincident pressing defender never normalizes a zero-length direction into NaN");
var coincidentCross=Match();coincidentCross.ActiveUserPlayer.Position2D=new Vector2(Pitch.Length-10,Pitch.Width/2);coincidentCross.ActiveUserPlayer.FacingDirection=Vector2.UnitX;coincidentCross.Ball.CatchOrTackle(coincidentCross.ActiveUserPlayer);coincidentCross.ExecuteCross();
Check(float.IsFinite(coincidentCross.Ball.Velocity.X)&&float.IsFinite(coincidentCross.Ball.Velocity.Y),"cross from exact center target uses a finite forward direction");
var goalLine=Match();var goalCarrier=goalLine.AwayTeam.Players[9];goalCarrier.Position2D=new Vector2(0,Pitch.Width/2);goalLine.Ball.CatchOrTackle(goalCarrier);typeof(MatchEngine).GetMethod("UpdatePlayersAI",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(goalLine,new object[]{.1f});
Check(float.IsFinite(goalCarrier.Position2D.X)&&float.IsFinite(goalLine.Ball.Velocity.X),"CPU carrier at exact goal center cannot turn its run and shot into NaN");
// Pause, substitute a pass participant, resume: no stale bench player may remain the receiver.
var passSub=Match();passSub.ActiveUserPlayer.Position2D=new Vector2(45,34);passSub.Ball.CatchOrTackle(passSub.ActiveUserPlayer);
passSub.ExecutePass(Vector2.UnitX);var transit=passSub.ActivePassTransit!;var outgoingReceiver=transit.IntendedReceiver;var incomingReceiver=passSub.HomeTeam.Bench.First(p=>p.Role!=PlayerRole.Goalkeeper);passSub.PauseMatch();
Check(passSub.MakeSubstitution(passSub.HomeTeam,outgoingReceiver,incomingReceiver),"pass participant can be substituted in pause");passSub.ResumeMatch();
Check(passSub.ActivePassTransit==null||passSub.HomeTeam.Players.Contains(passSub.ActivePassTransit.IntendedReceiver),"pass transit never points at a substituted-off bench receiver after resume");
var benchPos=outgoingReceiver.Position2D;for(int i=0;i<15;i++)passSub.Update(.02f);
Check(outgoingReceiver.Position2D==benchPos&&passSub.Ball.Owner!=outgoingReceiver&&passSub.ActiveUserPlayer!=outgoingReceiver,"substituted receiver cannot keep chasing or receive user control from an old pass");
// A dismissed set-piece taker is replaced before confirmation, not left owning a dead restart.
var takerCard=Match();takerCard.AwardFoulRestart(takerCard.HomeTeam.Players[9],new Vector2(70,34));var dismissedTaker=takerCard.Restart!.Taker;takerCard.IssueCard(dismissedTaker,true);takerCard.DismissStoppageReplay();
Check(takerCard.Restart==null||takerCard.Restart.Taker.IsActiveOnPitch,"dismissal cannot leave a ready restart assigned to an off-pitch taker");
foreach(bool goalLineExit in new[]{false,true})
{
 var lastTouch=Match();lastTouch.ActiveUserPlayer.Position2D=new Vector2(50,34);lastTouch.Ball.CatchOrTackle(lastTouch.ActiveUserPlayer);var outgoing=lastTouch.ActiveUserPlayer;lastTouch.ExecutePass(Vector2.UnitX);lastTouch.PauseMatch();lastTouch.MakeSubstitution(lastTouch.HomeTeam,outgoing,lastTouch.HomeTeam.Bench.First());lastTouch.ResumeMatch();
 lastTouch.Ball.Owner=null;lastTouch.Ball.Position=goalLineExit?new Vector3(Pitch.Length+1,5,0):new Vector3(50,-1,0);typeof(MatchEngine).GetMethod("CheckRefereeRules",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(lastTouch,null);
 Check(lastTouch.Restart?.Team==lastTouch.AwayTeam,"last touch by substituted home player still awards opponent "+(goalLineExit?"goal kick":"throw-in"));
}
var flyingShot=Match();var awayShooter=flyingShot.AwayTeam.Players[9];typeof(MatchEngine).GetField("_shotShooter",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(flyingShot,awayShooter);flyingShot.PauseMatch();flyingShot.MakeSubstitution(flyingShot.AwayTeam,awayShooter,flyingShot.AwayTeam.Bench.First());
var defendingKeeper=(Player?)typeof(MatchEngine).GetMethod("GetDefendingGoalkeeperForShot",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(flyingShot,null);
Check(defendingKeeper!=null&&flyingShot.HomeTeam.Players.Contains(defendingKeeper),"substituting an away shooter cannot switch the flying shot to the wrong defending goalkeeper");
// Historical touch identity survives player replacement until the resulting goal is resolved.
var ownGoalSub=Match();var ownEvents=new List<MatchEvent>();ownGoalSub.EventBus.OnMatchEvent+=ownEvents.Add;var ownTouch=ownGoalSub.ActiveUserPlayer;ownGoalSub.ExecutePass(Vector2.UnitX);ownGoalSub.PauseMatch();ownGoalSub.MakeSubstitution(ownGoalSub.HomeTeam,ownTouch,ownGoalSub.HomeTeam.Bench.First());ownGoalSub.ResumeMatch();ownGoalSub.Ball.Owner=null;ownGoalSub.Ball.Position=new Vector3(-.1f,Pitch.Width/2,1);
typeof(MatchEngine).GetMethod("CheckRefereeRules",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(ownGoalSub,null);
Check(ownEvents.Last(e=>e.Type==MatchEventType.GoalScored).GoalData!.IsOwnGoal&&ownTouch.MatchGoalsCount==0,"substituted last toucher remains correctly attributed for an own goal");
var assistSub=Match();var assistEvents=new List<MatchEvent>();assistSub.EventBus.OnMatchEvent+=assistEvents.Add;var actualPasser=assistSub.ActiveUserPlayer;assistSub.ExecutePass(Vector2.UnitX);var actualScorer=assistSub.ActivePassTransit!.IntendedReceiver;assistSub.Ball.CatchOrTackle(actualScorer);typeof(MatchEngine).GetMethod("RecordBallTouch",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(assistSub,new object[]{actualScorer,true});assistSub.PauseMatch();assistSub.MakeSubstitution(assistSub.HomeTeam,actualPasser,assistSub.HomeTeam.Bench.First());assistSub.ResumeMatch();assistSub.Ball.Owner=null;assistSub.Ball.Position=new Vector3(Pitch.Length+.1f,Pitch.Width/2,1);typeof(MatchEngine).GetMethod("CheckRefereeRules",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(assistSub,null);
Check(assistEvents.Last(e=>e.Type==MatchEventType.GoalScored).GoalData!.Assister==actualPasser,"real assister is retained when substituted between the pass and goal");
Check(assistSub.RequestCoachPraiseForLastTeamGoal(),"coach recognizes actual team goal after the assister leaves the pitch");
var runSub=Match();var runOut=runSub.ActiveUserPlayer;runSub.BeginSlowRunUp();runSub.PauseMatch();runSub.MakeSubstitution(runSub.HomeTeam,runOut,runSub.HomeTeam.Bench.First());runSub.ResumeMatch();typeof(MatchEngine).GetMethod("UpdateRunUp",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(runSub,new object[]{1.3f});
Check(runSub.Ball.Owner!=runOut&&typeof(MatchEngine).GetField("_runUpPlayer",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(runSub)==null,"substitution cancels old player's pending run-up without bench possession");
foreach(var language in new[]{UiLanguage.Arabic,UiLanguage.English})
{
 MatchSettings.Current.Language=language;var r=new Reader();int done=0;var session=GamePlanMenuBuilder.CreatePreMatchMenu(Team("session"),r,_=>{},()=>{},()=>done++,startMatch:false);session.GetItem(0).OnSelect!();
 Check(done==1&&session.GetItem(0).Title.Contains(language==UiLanguage.English?"session":"الجلسة")&&r.Spoken.All(t=>!t.Contains("Match starting")&&!t.Contains("انطلاق المباراة")),"standalone plan applies session choices without pretending to start a match: "+language);
 var ordinary=GamePlanMenuBuilder.CreatePreMatchMenu(Team("normal"),r,_=>{},()=>{},()=>{});Check(ordinary.GetItem(0).Title==(language==UiLanguage.English?"Start match":"بدء المباراة"),"actual pre-match plan retains Start match: "+language);
}
foreach(bool home in new[]{true,false})
{
 var shortGame=Match();shortGame.Mode=MatchMode.CupFinal;shortGame.HomeTeam.Score=2;var events=new List<MatchEvent>();shortGame.EventBus.OnMatchEvent+=events.Add;var team=home?shortGame.HomeTeam:shortGame.AwayTeam;
 foreach(var player in team.Players.Where(p=>p.Role!=PlayerRole.Goalkeeper).Take(4).ToArray())shortGame.IssueCard(player,true);
 Check(shortGame.State!=MatchState.GameOver,"seven available players still permit match play: "+home);
 shortGame.IssueCard(team.Players.First(p=>p.IsActiveOnPitch&&p.Role!=PlayerRole.Goalkeeper),true);var clock=shortGame.MatchTimeMinutes;shortGame.Update(.2f);shortGame.ResumeMatch();
 Check(shortGame.State==MatchState.GameOver&&shortGame.MatchTimeMinutes==clock&&shortGame.AbandonmentReason!=null,"six players stop the match permanently with a stated reason: "+home);
 Check(shortGame.HomeTeam.Score==2&&shortGame.AwayTeam.Score==0&&shortGame.TrophyWinner==null&&shortGame.WinningGoalReplay()==null&&!events.Any(e=>e.Type is MatchEventType.FullTime or MatchEventType.TrophyWon),"abandonment never invents a final score, cup winner or winning-goal replay: "+home);
 Check(events.Count(e=>e.Type==MatchEventType.MatchAbandoned)==1&&!shortGame.AwaitingStoppageConfirmation&&shortGame.PendingInjuryPlayer==null,"abandonment publishes once and clears blocking incident state: "+home);
}
var injuryShort=Match();foreach(var p in injuryShort.HomeTeam.Players.Where(p=>p.Role!=PlayerRole.Goalkeeper).Take(4).ToArray())injuryShort.IssueCard(p,true);var lastInjured=injuryShort.HomeTeam.Players.First(p=>p.IsActiveOnPitch&&p.Role!=PlayerRole.Goalkeeper);injuryShort.ApplyInjury(lastInjured,PlayerInjuryStatus.SevereInjury);
Check(injuryShort.State!=MatchState.GameOver&&injuryShort.PendingInjuryPlayer==lastInjured,"seventh player's severe injury waits for legal replacement choice before abandonment");
injuryShort.ResolveInjurySubstitution(lastInjured,null);Check(injuryShort.State==MatchState.GameOver&&injuryShort.PendingInjuryPlayer==null,"choosing to continue with six instead stops with truthful abandonment");
var shootShort=Match();shootShort.State=MatchState.PenaltyShootout;foreach(var p in shootShort.HomeTeam.Players.Where(p=>p.Role!=PlayerRole.Goalkeeper).Take(5).ToArray())shootShort.IssueCard(p,true);shootShort.Update(.1f);Check(shootShort.State==MatchState.PenaltyShootout&&shootShort.AbandonmentReason==null,"post-match shootout does not use seven-player abandonment threshold");
var eligibilityGame=Match();eligibilityGame.IssueCard(eligibilityGame.AwayTeam.Players[8],true);eligibilityGame.IssueCard(eligibilityGame.AwayTeam.Players[9],true);var homeRoster=new ShootoutRoster(eligibilityGame.HomeTeam);var awayRoster=new ShootoutRoster(eligibilityGame.AwayTeam);int target=awayRoster.Players.Count;
Check(!homeRoster.Exclude(eligibilityGame.HomeTeam.Players[0],target),"shootout equalization keeps the actual goalkeeper eligible");
var excluded=eligibilityGame.HomeTeam.Players[5];Check(homeRoster.Exclude(excluded,target)&&homeRoster.Exclude(eligibilityGame.HomeTeam.Players[6],target)&&!homeRoster.Exclude(eligibilityGame.HomeTeam.Players[7],target),"human equalization accepts chosen players only until opponent eligible count");
Check(homeRoster.Players.Count==awayRoster.Players.Count&&!homeRoster.Players.Contains(excluded)&&eligibilityGame.HomeTeam.Players.Contains(excluded)&&eligibilityGame.HomeTeam.SubstitutionsUsed==0,"shootout exclusion changes eligibility, not squads or substitution counts");
Check(Enumerable.Range(0,target).Select(k=>homeRoster.Taker(k)).Distinct().Count()==target&&homeRoster.Taker(target)==homeRoster.Taker(0),"every eligible player kicks once before the next cycle");
Check(awayRoster.Players.All(p=>p.IsActiveOnPitch)&&!awayRoster.Players.Contains(eligibilityGame.AwayTeam.Players[8]),"dismissed players cannot reappear in shootout rotation");
var cpuRoster=new ShootoutRoster(eligibilityGame.HomeTeam);cpuRoster.ReduceCpuTo(target);Check(cpuRoster.Players.Count==target&&cpuRoster.Players.Any(p=>p.Role==PlayerRole.Goalkeeper),"CPU equalization uses the same legal count and preserves goalkeeper");
var emptyTeam=Team("empty");emptyTeam.Players.Clear();Check(new ShootoutRoster(emptyTeam).Taker(0)==null&&homeRoster.Taker(-1)==null,"invalid shootout roster/index fails closed rather than dividing by zero");
foreach(var language in new[]{UiLanguage.Arabic,UiLanguage.English})
{
 MatchSettings.Current.Language=language;var squad=Team("select-penalties");var roster=new ShootoutRoster(squad);int selections=0;var menu=GamePlanMenuBuilder.CreateShootoutEligibilityMenu(roster,10,new Reader(),()=>selections++);
 Check(menu.Count==10&&!Enumerable.Range(0,menu.Count).Select(menu.GetItem).Any(item=>item.Title==squad.Players[0].Name),"shootout exclusion menu contains named legal choices, never the keeper: "+language);
 var chosen=squad.Players[6];var action=Enumerable.Range(0,menu.Count).Select(menu.GetItem).First(item=>item.Title==chosen.Name);action.OnSelect!();action.OnSelect!();
 Check(selections==1&&roster.Players.Count==10&&!roster.Players.Contains(chosen)&&squad.Players.Count==11,"named menu choice applies once and cannot remove a second stale player: "+language);
}
var finalFoul=Match();finalFoul.AwayTeam.Tactics.Tackling=TacklingAggression.Reckless;foreach(var p in finalFoul.AwayTeam.Players.Where(p=>p.Role!=PlayerRole.Goalkeeper).Take(4).ToArray())finalFoul.IssueCard(p,true);var finalTackler=finalFoul.AwayTeam.Players.First(p=>p.IsActiveOnPitch&&p.Role!=PlayerRole.Goalkeeper);finalFoul.HandleFoulSanction(finalTackler,finalFoul.HomeTeam.Players[9],true,true);
Check(finalFoul.State==MatchState.GameOver&&finalFoul.AbandonmentReason!=null&&finalFoul.Restart==null&&finalFoul.PendingInjuryPlayer==null&&!finalFoul.AwaitingStoppageConfirmation,"same severe foul that abandons a match cannot reopen a restart/injury menu afterward");
// Complete repeated matches, not only individual methods. Invariants are checked after every tick.
MatchSettings.Current.ResetToDefaults();MatchSettings.Current.MatchDurationMinutes=1;MatchSettings.Current.ExtraTimeEnabled=false;MatchSettings.Current.PenaltyShootoutEnabled=false;
for(int seed=0;seed<60;seed++)
{
 var rng=new Random(seed);MatchSettings.Current.Difficulty=(DifficultyLevel)(seed%4);float frameTime=seed<20?.02f:seed<40?.1f:.25f;var game=Match();game.SetStoppageMinutes(0,0);bool finite=true,ownerLegal=true,ballFlags=true,finished=false;int frames=0;
 var choices=new[]{GameAction.ShortPass,GameAction.Cross,GameAction.Shoot,GameAction.ThroughBall,GameAction.SwitchPlayer,GameAction.MoveRight,GameAction.MoveLeft,GameAction.MoveUp,GameAction.MoveDown,GameAction.Sprint};
 for(;frames<12000;frames++)
 {
  if(game.State==MatchState.GameOver){finished=true;break;}
  if(game.PendingInjuryPlayer is {} injured)game.ResolveInjurySubstitution(injured,game.HomeTeam.Bench.FirstOrDefault(game.HomeTeam.CanEnterAsSubstitute));
  if(game.AwaitingStoppageConfirmation)game.DismissStoppageReplay();if(game.AnalysisReplayPending)game.CompleteAnalysisReplay();
  if(game.State is MatchState.HalfTime or MatchState.Paused)game.ResumeMatch();if(game.State==MatchState.GoalCelebration)game.CompleteGoalCelebration();
  input.Actions.Clear();if(rng.Next(3)==0)input.Actions.Add(choices[rng.Next(choices.Length)]);game.Update(frameTime);
  var players=game.HomeTeam.Players.Concat(game.AwayTeam.Players).ToArray();
  finite &= float.IsFinite(game.MatchTimeMinutes)&&float.IsFinite(game.Ball.Position.X)&&float.IsFinite(game.Ball.Position.Y)&&players.All(p=>float.IsFinite(p.Position2D.X)&&float.IsFinite(p.Position2D.Y));
  ownerLegal &= game.Ball.Owner==null||players.Contains(game.Ball.Owner)&&game.Ball.Owner.IsActiveOnPitch;
  ballFlags &= players.Count(p=>p.HasBall)==(game.Ball.Owner==null?0:1)&&players.All(p=>p.HasBall==(p==game.Ball.Owner));
  if(!finite||!ownerLegal||!ballFlags){Console.WriteLine("BAD seed="+seed+" frame="+frames+" actions="+string.Join(",",input.Actions)+" ball="+game.Ball.Position+" badplayers="+string.Join(";",players.Where(p=>!float.IsFinite(p.Position2D.X)||!float.IsFinite(p.Position2D.Y)).Select(p=>p.Name+" "+p.Position2D+" home "+p.HomePosition+" vel "+p.Velocity)));break;}
 }
 Check(finite&&ownerLegal&&ballFlags,"whole-match tick invariants seed "+seed+" frames "+frames+" finite="+finite+" owner="+ownerLegal+" flags="+ballFlags);
 Check(finished&&game.AbandonmentReason==null&&game.FullTimeAnnounced,"whole match reaches actual full time through incidents and halftime seed "+seed+" abandoned="+(game.AbandonmentReason!=null));
}
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
// Restored categories expose every documented sample and report missing recordings honestly.
foreach(var language in new[]{UiLanguage.Arabic,UiLanguage.English})
{
 MatchSettings.Current.Language=language;var r=new Reader();var h=new Host();var academy=SoundAcademyMenuBuilder.CreateSoundAcademyMenu(r,anthem,h.Push,h.Pop);h.Push(academy);var cats=SoundAcademyDatabase.GetCategories();
 Check(cats.Select(c=>c.Id).SequenceEqual(new[]{"earcons","sfx","atmosphere","beacon","anthems"}),"academy restores all five documented categories: "+language);
 foreach(var category in cats)
 {
  academy.GetItem(cats.IndexOf(category)).OnSelect!();var sub=h.Current!;var items=SoundAcademyDatabase.GetItemsByCategory(category.Id);
  Check(sub.Count==items.Count+1,"every documented sample is reachable in "+category.Id+": "+language);
  for(int n=0;n<items.Count;n++)
  {
   commands.Clear();r.Spoken.Clear();sub.GetItem(n).OnSelect!();
   Check(r.Spoken.Any(t=>t.Contains(items[n].GetDescription(language))),"selected preview explains actual sample "+items[n].Id+": "+language);
   if(items[n].Id is "GoalkeeperSave" or "GoalNet" or "WoodworkHit" or "RefereeWhistle" or "NationalAnthem")
    Check(!commands.Any(c=>c.StartsWith("play "))&&r.Spoken.Any(t=>t.Contains(language==UiLanguage.English?"unavailable":"غير متاحة")),"missing real sample neither substitutes a kick nor silently claims playback "+items[n].Id+": "+language);
  }
  sub.GetItem(sub.Count-1).OnSelect!();
 }
}
commands.Clear();Check(!SoundAcademyMenuBuilder.TryPlayPreview("unknown-id",anthem)&&!commands.Any(c=>c.StartsWith("play ")),"unknown academy sound cannot fall back to an unrelated menu chime");
SoundEffectsPlayer.MasterVolumeSetting=0;commands.Clear();Check(!SoundAcademyMenuBuilder.TryPlayPreview("BallKick",anthem)&&!commands.Any(c=>c.StartsWith("play ")),"muted academy recording reports no successful playback");SoundEffectsPlayer.MasterVolumeSetting=50;
MciPlayback.TestCommand=(command,buffer)=>{commands.Add(command);return command.StartsWith("open ")?1:0;};Check(!SoundAcademyMenuBuilder.TryPlayPreview("BallKick",anthem),"academy reports playback transport failure instead of success");
commands.Clear();MciPlayback.TestCommand=(command,buffer)=>{commands.Add(command);return command.Contains("left volume")?1:0;};SoundEffectsPlayer.PlayKick(-1);
Check(!commands.Contains("play kick from 0"),"failed spatial-channel setup cannot play an incorrectly centered effect");
MciPlayback.TestCommand=null;
Console.WriteLine($"RESULT {passed} passed, {failed} failed. Native Windows UI/audio was not executed.");return failed==0?0:1;
sealed class Reader:IScreenReader{public List<string> Spoken=new();public bool IsAvailable=>true;public void Speak(string text,bool interrupt=true)=>Spoken.Add(text);public void CancelSpeech(){}public Task SpeakUntilFinishedAsync(string text,bool interrupt=true){Speak(text,interrupt);return Task.CompletedTask;}}
sealed class Input:IInputHandler{public HashSet<GameAction> Actions=new();public HashSet<GameAction> PollCurrentActions(bool possession){var x=Actions.ToHashSet();Actions.Clear();return x;}public bool IsActionTriggered(GameAction action,bool possession)=>Actions.Remove(action);}
sealed class Host{public AccessibleMenu? Current;public Stack<AccessibleMenu> History=new();public void Push(AccessibleMenu menu){if(Current!=null)History.Push(Current);Current=menu;menu.Open();}public void Pop(){Current?.Close();Current=History.Count>0?History.Pop():null;Current?.Open();}}
