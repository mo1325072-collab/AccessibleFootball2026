using System.Numerics;
using System.Reflection;
using AccessibleFootball2026.Audio;
using AccessibleFootball2026.App.Menus;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Input;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;
int passed=0, failed=0;
void Check(bool result,string label){Console.WriteLine((result?"PASS ":"FAIL ")+label);if(result)passed++;else failed++;}
Team Squad(string id,TeamSide side){var t=new Team(id,id,side);for(int i=0;i<11;i++)t.Players.Add(new Player((side==TeamSide.Home?100:200)+i,id+i,i+1,i==0?SpecificPosition.GK:i<5?SpecificPosition.CB:i<8?SpecificPosition.CM:SpecificPosition.ST,75,75,75,75,75));t.SetupFormation433();return t;}
MatchEngine Game()=>new(Squad("home",TeamSide.Home),Squad("away",TeamSide.Away),new MatchEventBus(),new Input());
MatchSettings.Current.ResetToDefaults();MatchSettings.Current.AutoTacticalAdjustmentsEnabled=false;
(bool won,MatchEngine game) RealTackle(int ability,bool slide=false,float stamina=100,int fit=0){var m=Game();var p=m.ActiveUserPlayer;var c=m.AwayTeam.Players[9];foreach(var other in m.HomeTeam.Players.Concat(m.AwayTeam.Players))other.Position2D=new Vector2(10,10);p.Position2D=new Vector2(51,34);p.FacingDirection=-Vector2.UnitX;p.PrimaryPosition=p.CurrentAssignedPosition;p.BaseDefending=p.BasePhysical=p.BaseSpeed=ability;p.CurrentStamina=stamina;if(fit==1){p.SecondaryPositions.Add(SpecificPosition.CM);p.CurrentAssignedPosition=SpecificPosition.CM;}else if(fit==2)p.CurrentAssignedPosition=SpecificPosition.CB;c.Position2D=new Vector2(50,34);c.FacingDirection=Vector2.UnitX;c.PrimaryPosition=c.CurrentAssignedPosition;m.Ball.CatchOrTackle(c);if(slide)m.ExecuteSlideTackle();else m.ExecuteStandTackle();return(m.Ball.Owner==p,m);}
foreach(bool slide in new[]{false,true}){var weak=RealTackle(30,slide);var strong=RealTackle(95,slide);Check(!weak.won&&strong.won,"real "+(slide?"slide":"standing")+" command distinguishes weak from strong at identical ball-first geometry");}
foreach(var language in new[]{UiLanguage.Arabic,UiLanguage.English})
{
 MatchSettings.Current.Language=language;var team=Squad("report",TeamSide.Home);team.ChangeFormation(FormationType.Formation532_Counter);team.Tactics.PressingIntensity=90;team.Tactics.DefensiveLineDepth=90;var r=new Reader();var menu=GamePlanMenuBuilder.CreateTacticsMenu(team,r,()=>{});menu.GetItem(menu.Count-2).OnSelect!();var actual=team.Tactics.CalculateSynergy(team.CurrentFormation);
 Check(r.Spoken.Last().Contains(actual.SynergyScore.ToString()),"menu reports actual formation-specific synergy: "+language);
}
var req=FormationManager.GetRequirement(FormationType.Formation4231_Wide);Check(req.Type==FormationType.Formation4231_Wide,"4-2-3-1 requirements cannot silently fall back to 4-3-3");
// The full catalogue has real ten-player slots, one keeper and distinct new shapes.
foreach(var f in FormationManager.GetAllFormations())
{
 var team=Squad("shape",TeamSide.Home);team.ChangeFormation(f);var slots=team.Players.Where(p=>p.Role!=PlayerRole.Goalkeeper).ToArray();
 Check(team.Players.Count(p=>p.Role==PlayerRole.Goalkeeper)==1&&slots.Length==10&&slots.Select(p=>p.HomePosition).Distinct().Count()==10,"eleven-player legal unique slots: "+f);
 Check(FormationManager.GetRequirement(f).Type==f,"formation requirements identify actual shape: "+f);
 foreach(var language in new[]{UiLanguage.Arabic,UiLanguage.English})
 {
  MatchSettings.Current.Language=language;var r=new Reader();var menu=GamePlanMenuBuilder.CreateFormationMenu(team,r,()=>{});menu.GetItem(menu.Count-2).OnSelect!();
  Check(r.Spoken.Last().Contains(language==UiLanguage.English?"Primary positions:":"مراكز أساسية:"),"requested requirements and real player fit reachable: "+f+" "+language);
 }
}
var newShapes=new[]{FormationType.Formation4141,FormationType.Formation3421,FormationType.Formation4222}.Select(f=>{var t=Squad("distinct",TeamSide.Home);t.ChangeFormation(f);return string.Join(";",t.Players.Select(p=>p.HomePosition));}).ToArray();Check(newShapes.Distinct().Count()==3,"new 4-1-4-1,3-4-2-1,4-2-2-2 are not shared-coordinate aliases");
// Same player, same situation. Change one cause at a time.
var paired=Game();var tackler=paired.HomeTeam.Players[5];var carrier=paired.AwayTeam.Players[9];foreach(var p in paired.HomeTeam.Players.Concat(paired.AwayTeam.Players))p.Position2D=new Vector2(10,10);tackler.Position2D=new Vector2(51,34);carrier.Position2D=new Vector2(50,34);tackler.FacingDirection=-Vector2.UnitX;carrier.FacingDirection=Vector2.UnitX;tackler.PrimaryPosition=SpecificPosition.CM;tackler.SecondaryPositions.Clear();tackler.SecondaryPositions.Add(SpecificPosition.CDM);carrier.PrimaryPosition=carrier.CurrentAssignedPosition;
MatchEngine.DuelResult Duel()=>MatchEngine.EvaluateTackleDuel(tackler,carrier,new Vector2(50.35f,34),paired.HomeTeam,paired.AwayTeam,DifficultyLevel.Normal);
tackler.CurrentAssignedPosition=SpecificPosition.CM;var primary=Duel();tackler.CurrentAssignedPosition=SpecificPosition.CDM;var secondary=Duel();tackler.CurrentAssignedPosition=SpecificPosition.ST;var wrong=Duel();
Check(primary.TacklerSkill>secondary.TacklerSkill&&secondary.TacklerSkill>wrong.TacklerSkill&&primary.TacklerWinProbability>wrong.TacklerWinProbability,"primary / supported secondary / wrong position really change duel attributes and advantage");
tackler.CurrentAssignedPosition=SpecificPosition.CM;tackler.CurrentStamina=100;var fresh=Duel();tackler.CurrentStamina=20;var tired=Duel();Check(fresh.TacklerSkill>tired.TacklerSkill&&fresh.TacklerWinProbability>tired.TacklerWinProbability,"fatigue changes same-player duel, not only menu text");tackler.CurrentStamina=100;
paired.HomeTeam.Tactics.PressingIntensity=90;paired.HomeTeam.Tactics.DefensiveLineDepth=90;var coherent=Duel();paired.HomeTeam.Tactics.DefensiveLineDepth=10;var conflict=Duel();Check(coherent.TacklerSkill>conflict.TacklerSkill&&coherent.TacklerWinProbability>conflict.TacklerWinProbability,"conflicting high press / deep line affects actual duel");
paired.HomeTeam.Tactics.PressingIntensity=45;paired.HomeTeam.Tactics.DefensiveLineDepth=45;tackler.Position2D=new Vector2(49,34);var shielded=Duel();tackler.Position2D=new Vector2(51,34);var facing=Duel();Check(facing.TacticalModifier>shielded.TacticalModifier,"same skills respect face-on versus shielding geometry");
var deterministic=Enumerable.Range(0,100).Select(_=>Duel()).ToArray();Check(deterministic.All(d=>d==deterministic[0]),"production duel never rolls dice across 100 identical situations");
float previous=-1;bool monotonic=true;for(int ability=20;ability<=99;ability++){tackler.BaseDefending=tackler.BasePhysical=tackler.BaseSpeed=ability;var d=Duel();monotonic&=d.TacklerWinProbability>=previous;previous=d.TacklerWinProbability;}Check(monotonic,"raising defending/physical/speed cannot worsen identical-geometry ball-winning score");
// Supporting roles change actual targets with possession, without changing user's positions.
foreach(bool home in new[]{true,false})
{
 var m=Game();var team=home?m.HomeTeam:m.AwayTeam;var other=home?m.AwayTeam:m.HomeTeam;team.ChangeFormation(FormationType.Formation3421);var wing=team.Players.First(p=>p.CurrentAssignedPosition==SpecificPosition.LWB);var assigned=wing.CurrentAssignedPosition;
 m.Ball.CatchOrTackle(team.Players.First(p=>p.Role!=PlayerRole.Goalkeeper&&p!=wing));var attack=m.GetFormationSupportTarget(team,wing);m.Ball.CatchOrTackle(other.Players[9]);var defend=m.GetFormationSupportTarget(team,wing);
 Check((home?attack.X>defend.X:attack.X<defend.X)&&wing.CurrentAssignedPosition==assigned,"wing-back advances/recoveries are mirrored actual targets, not role rewrites: "+home);
 var cb=team.Players.First(p=>p.CurrentAssignedPosition==SpecificPosition.CB);team.Tactics.DefensiveLineDepth=10;var low=m.GetFormationSupportTarget(team,cb);team.Tactics.DefensiveLineDepth=90;var high=m.GetFormationSupportTarget(team,cb);Check(home?high.X>low.X:high.X<low.X,"defensive-line slider moves actual defender target: "+home);
 team.Tactics.PressingIntensity=10;var lowPress=MatchEngine.TacticalPressRange(13.5f,team);team.Tactics.PressingIntensity=90;Check(MatchEngine.TacticalPressRange(13.5f,team)>lowPress,"pressing slider changes real CPU engagement range: "+home);
}
foreach(var shape in new[]{FormationType.Formation41212_Diamond,FormationType.Formation4222})
{
 var m=Game();m.HomeTeam.ChangeFormation(shape);var fullback=m.HomeTeam.Players.First(p=>p.CurrentAssignedPosition==SpecificPosition.LB);m.Ball.CatchOrTackle(m.HomeTeam.Players[9]);Check(m.GetFormationSupportTarget(m.HomeTeam,fullback).X>fullback.HomePosition.X,"narrow midfield obtains width from actual full-back support: "+shape);
}
var ownPass=Game();typeof(MatchEngine).GetField("_kickoffProtectionTimer",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(ownPass,0f);var away=ownPass.AwayTeam;var receiver=away.Players[4];var holder=away.Players[9];holder.Position2D=new Vector2(60,34);receiver.Position2D=receiver.HomePosition;var receiverBefore=receiver.Position2D;ownPass.Ball.CatchOrTackle(holder);typeof(MatchEngine).GetMethod("UpdatePlayersAI",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(ownPass,new object[]{.05f});Check(Vector2.Distance(receiver.Position2D,receiverBefore)<.01f,"CPU support player no longer presses its own ball carrier");
foreach(int ability in new[]{30,95})
{
 var m=Game();typeof(MatchEngine).GetField("_kickoffProtectionTimer",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(m,0f);typeof(MatchEngine).GetField("_possessionGraceTimer",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(m,0f);typeof(MatchEngine).GetField("_aiTackleCooldownTimer",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(m,0f);var p=m.AwayTeam.Players[1];var c=m.ActiveUserPlayer;foreach(var other in m.HomeTeam.Players.Concat(m.AwayTeam.Players))other.Position2D=new Vector2(10,10);p.Position2D=new Vector2(51,34);p.FacingDirection=-Vector2.UnitX;p.PrimaryPosition=p.CurrentAssignedPosition;p.BaseDefending=p.BasePhysical=p.BaseSpeed=ability;c.Position2D=new Vector2(50,34);c.FacingDirection=Vector2.UnitX;c.PrimaryPosition=c.CurrentAssignedPosition;m.Ball.CatchOrTackle(c);typeof(MatchEngine).GetMethod("UpdatePlayersAI",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(m,new object[]{.01f});
 Check((m.Ball.Owner==p)==(ability==95),"real CPU engagement differentiates weak/strong at identical geometry: "+ability);
}
foreach(bool home in new[]{true,false})
{
 var m=Game();var team=home?m.HomeTeam:m.AwayTeam;team.ChangeFormation(FormationType.Formation4141);var pivot=team.Players.First(p=>p.CurrentAssignedPosition==SpecificPosition.CDM);var wide=team.Players.First(p=>p.CurrentAssignedPosition==SpecificPosition.LM);var opposition=home?m.AwayTeam:m.HomeTeam;m.Ball.CatchOrTackle(opposition.Players[9]);m.Ball.Position=new Vector3(home?104:1,34,0);var screen=m.GetFormationSupportTarget(team,pivot);Check((home?screen.X:Pitch.Length-screen.X)<=42,"single pivot remains a screen instead of blindly following advanced ball: "+home);
 m.Ball.CatchOrTackle(team.Players[9]);var advanced=m.GetFormationSupportTarget(team,wide);m.Ball.CatchOrTackle(opposition.Players[9]);var recovered=m.GetFormationSupportTarget(team,wide);Check(home?advanced.X>recovered.X:advanced.X<recovered.X,"4-1-4-1 wide midfielder advances with ball and recovers without it: "+home);
}
// Existing presets remain stable in saved integer form; added shapes use new identities.
Check((int)FormationType.Formation433_Attack==0&&(int)FormationType.Formation541_Bus==8&&(int)FormationType.Formation4141==9&&(int)FormationType.Formation3421==10&&(int)FormationType.Formation4222==11,"catalogue revision preserves old numeric identities rather than corrupting saved formations");
foreach(bool slide in new[]{false,true})
{
 Check(RealTackle(95,slide).won&&!RealTackle(95,slide,20).won,"real human command applies fatigue to a strong defender: "+slide);
 Check(RealTackle(95,slide).won&&!RealTackle(95,slide,100,2).won,"real human command applies out-of-position penalty: "+slide);
}
tackler.BaseDefending=tackler.BasePhysical=tackler.BaseSpeed=75;previous=101;monotonic=true;
for(int ability=20;ability<=99;ability++){carrier.BasePassing=carrier.BasePhysical=carrier.BaseSpeed=ability;var d=Duel();monotonic&=d.TacklerWinProbability<=previous;previous=d.TacklerWinProbability;}Check(monotonic,"raising carrier skill cannot make it easier for the same defender to win the ball");
var young=Game();var older=Game();young.HomeTeam.Tactics.PressingIntensity=95;older.HomeTeam.Tactics.PressingIntensity=95;young.HomeTeam.Players[3].Age=20;older.HomeTeam.Players[3].Age=32;young.UpdateStaminaDecay(45);older.UpdateStaminaDecay(45);Check(young.HomeTeam.Players[3].CurrentStamina>older.HomeTeam.Players[3].CurrentStamina,"actual stamina consumer distinguishes young and older players under equal high pressing");
foreach(var language in new[]{UiLanguage.Arabic,UiLanguage.English})
{
 MatchSettings.Current.Language=language;var reader=new Reader();AccessibleMenu? opened=null;var guide=UserGuideMenuBuilder.CreateUserGuideMenu(reader,m=>opened=m,()=>{});var index=UserGuideDatabase.GetCategories().FindIndex(c=>c.Id=="formations");guide.GetItem(index).OnSelect!();Check(opened!.Count==FormationManager.GetAllFormations().Count+1,"guide has every revised formation, not stale descriptions: "+language);reader.Spoken.Clear();opened.GetItem(FormationManager.GetAllFormations().IndexOf(FormationType.Formation3421)).OnSelect!();Check(reader.Spoken.Last().Contains(language==UiLanguage.English?"two number tens":"صانعا ألعاب"),"actual guide callback explains two-tens requirements: "+language);
}
MatchSettings.Current.ResetToDefaults();MatchSettings.Current.MatchDurationMinutes=1;MatchSettings.Current.ExtraTimeEnabled=false;MatchSettings.Current.PenaltyShootoutEnabled=false;
foreach(var shape in new[]{FormationType.Formation4141,FormationType.Formation3421,FormationType.Formation4222})
{
 var m=Game();m.HomeTeam.ChangeFormation(shape);m.AwayTeam.ChangeFormation(shape);m.SetStoppageMinutes(0,0);bool legal=true,finished=false,first=false,second=false;
 for(int tick=0;tick<12000;tick++)
 {
  if(m.State==MatchState.GameOver){finished=true;break;}
  if(m.PendingInjuryPlayer is {} injury)m.ResolveInjurySubstitution(injury,m.HomeTeam.Bench.FirstOrDefault(m.HomeTeam.CanEnterAsSubstitute));
  if(m.AwaitingStoppageConfirmation)m.DismissStoppageReplay();if(m.AnalysisReplayPending)m.CompleteAnalysisReplay();if(m.State is MatchState.HalfTime or MatchState.Paused)m.ResumeMatch();if(m.State==MatchState.GoalCelebration)m.CompleteGoalCelebration();
  first|=m.UserAttackingRight;second|=!m.UserAttackingRight;m.Update(.1f);var all=m.HomeTeam.Players.Concat(m.AwayTeam.Players).ToArray();legal&=float.IsFinite(m.Ball.Position.X)&&float.IsFinite(m.Ball.Position.Y)&&all.All(p=>float.IsFinite(p.Position2D.X)&&float.IsFinite(p.Position2D.Y))&&(m.Ball.Owner==null||m.Ball.Owner.IsActiveOnPitch)&&all.Count(p=>p.HasBall)==(m.Ball.Owner==null?0:1);
 }
 Check(legal&&finished&&m.FullTimeAnnounced&&first&&second,"new formation completes a full match across both attacking directions with finite/legal player and ball state: "+shape);
}
Console.WriteLine($"PAIRED position tackler-skill primary={primary.TacklerSkill:F2} secondary={secondary.TacklerSkill:F2} outside={wrong.TacklerSkill:F2}; fresh={fresh.TacklerSkill:F2} tired={tired.TacklerSkill:F2}; coherent={coherent.TacklerSkill:F2} conflict={conflict.TacklerSkill:F2}");
Console.WriteLine($"RESULT {passed} passed, {failed} failed. Native Windows UI/audio was not executed.");return failed==0?0:1;
sealed class Reader:IScreenReader{public List<string> Spoken=new();public bool IsAvailable=>true;public void Speak(string text,bool interrupt=true)=>Spoken.Add(text);public void CancelSpeech(){}public Task SpeakUntilFinishedAsync(string text,bool interrupt=true){Speak(text,interrupt);return Task.CompletedTask;}}
sealed class Input:IInputHandler{public HashSet<GameAction> PollCurrentActions(bool possession)=>new();public bool IsActionTriggered(GameAction action,bool possession)=>false;}
