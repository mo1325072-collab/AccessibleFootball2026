using System.Numerics;
using AccessibleFootball2026.Audio;
using AccessibleFootball2026.App.Menus;
using System.Reflection;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Input;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;
int passed=0,failed=0;void Check(bool ok,string text){Console.WriteLine((ok?"PASS ":"FAIL ")+text);if(ok)passed++;else failed++;}
Team Squad(string id,TeamSide side){var t=new Team(id,id,side);for(int i=0;i<11;i++)t.Players.Add(new Player((side==TeamSide.Home?100:200)+i,id+i,i+1,i==0?SpecificPosition.GK:i<5?SpecificPosition.CB:i<8?SpecificPosition.CM:SpecificPosition.ST,75,75,75,75,75));t.SetupFormation433();foreach(var p in t.Players)p.PrimaryPosition=p.CurrentAssignedPosition;return t;}
MatchEngine Game()=>new(Squad("home",TeamSide.Home),Squad("away",TeamSide.Away),new MatchEventBus(),new Input());
void AI(MatchEngine m,float dt)=>typeof(MatchEngine).GetMethod("UpdatePlayersAI",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(m,new object[]{dt});
void ClearGrace(MatchEngine m){foreach(var name in new[]{"_kickoffProtectionTimer","_possessionGraceTimer","_aiTackleCooldownTimer"})typeof(MatchEngine).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(m,0f);}
MatchSettings.Current.ResetToDefaults();MatchSettings.Current.AutoTacticalAdjustmentsEnabled=false;
foreach(var row in new (int age,int minute,float minimum,float maximum)[]{(18,90,60f,101f),(20,79,40.1f,101f),(20,90,0f,40f),(27,60,0f,25.1f),(32,45,0f,20f)})
{
 var m=Game();m.HomeTeam.Tactics.PressingIntensity=100;var p=m.HomeTeam.Players[5];p.Age=row.age;for(int i=0;i<row.minute;i++){m.UpdateStaminaDecay(1);if(i==44){m.TriggerHalfTime();m.ResumeMatch();}}Console.WriteLine($"MEASURE age={row.age} minute={row.minute} stamina={p.CurrentStamina:F2}");Check(p.CurrentStamina>=row.minimum&&p.CurrentStamina<=row.maximum,$"sustained high press age {row.age}, minute {row.minute} follows requested fatigue window");
}
foreach(var focus in Enum.GetValues<AttackingFocus>())
{
 var m=Game();ClearGrace(m);var p=m.AwayTeam.Players[6];m.AwayTeam.Tactics.Focus=focus;m.AwayTeam.Tactics.PassingDirectness=focus==AttackingFocus.LongBall?95:20;m.AwayTeam.Tactics.Tempo=95;foreach(var x in m.HomeTeam.Players)x.Position2D=new Vector2(100,60);foreach(var x in m.AwayTeam.Players)x.Position2D=new Vector2(80,60);p.Position2D=new Vector2(focus==AttackingFocus.ShootOnSight?29:65,34);m.AwayTeam.Players[9].Position2D=new Vector2(40,34);m.AwayTeam.Players[10].Position2D=new Vector2(60,5);m.Ball.CatchOrTackle(p);for(int tick=0;tick<10&&m.Ball.Owner==p;tick++)AI(m,.1f);Console.WriteLine($"MEASURE focus={focus} owner={m.Ball.Owner?.Name??"none"} passes={m.StatisticsFor(m.AwayTeam).Passes} shots={m.StatisticsFor(m.AwayTeam).Shots}");Check(focus==AttackingFocus.ShootOnSight?m.StatisticsFor(m.AwayTeam).Shots==1:m.StatisticsFor(m.AwayTeam).Passes==1,"actual CPU carrier consumes attacking focus rather than random pressured-only passing: "+focus);
}

// Paired actual decision outcomes. Same players/positions, change one tactical choice.
(MatchEngine game,Player passer,Player near,Player far,Player wide) PassingFixture(AttackingFocus focus,int directness,int tempo)
{
 var m=Game();ClearGrace(m);var p=m.AwayTeam.Players[6];m.AwayTeam.Tactics.Focus=focus;m.AwayTeam.Tactics.PassingDirectness=directness;m.AwayTeam.Tactics.Tempo=tempo;
 foreach(var x in m.HomeTeam.Players)x.Position2D=new Vector2(5,65);foreach(var x in m.AwayTeam.Players)x.Position2D=x.HomePosition=new Vector2(95,60);
 p.Position2D=new Vector2(70,34);var near=m.AwayTeam.Players[5];near.Position2D=near.HomePosition=new Vector2(54,34);var far=m.AwayTeam.Players[9];far.Position2D=far.HomePosition=new Vector2(25,34);var wide=m.AwayTeam.Players[10];wide.Position2D=wide.HomePosition=new Vector2(58,4);m.Ball.CatchOrTackle(p);return(m,p,near,far,wide);
}
Player? PassTarget(MatchEngine m){var pass=typeof(MatchEngine).GetField("_commentaryPass",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(m);return pass?.GetType().GetProperty("Target")?.GetValue(pass) as Player;}
void WaitForDecision(MatchEngine m,Player p,int ticks=40){for(int tick=0;tick<ticks&&m.Ball.Owner==p;tick++)AI(m,.1f);}
var shortPass=PassingFixture(AttackingFocus.Balanced,5,95);var longPass=PassingFixture(AttackingFocus.Balanced,100,95);WaitForDecision(shortPass.game,shortPass.passer);WaitForDecision(longPass.game,longPass.passer);
Console.WriteLine($"MEASURE receivers short={PassTarget(shortPass.game)?.Name} long={PassTarget(longPass.game)?.Name}");Check(PassTarget(shortPass.game)==shortPass.near&&PassTarget(longPass.game)==longPass.far,"actual short/long sliders choose nearby versus forward outlet, not the same random pass");Check(shortPass.game.Ball.Velocity.Z<1&&longPass.game.Ball.Velocity.Z>2,"actual long-pass flight lifts over pressing lines at the cost of longer aerial travel");
var wings=PassingFixture(AttackingFocus.Wings,20,95);var central=PassingFixture(AttackingFocus.Central,20,95);WaitForDecision(wings.game,wings.passer);WaitForDecision(central.game,central.passer);Check(PassTarget(wings.game)==wings.wide&&PassTarget(central.game)==central.near,"actual Wings versus Central focus chooses wide versus central receiver");
var slow=PassingFixture(AttackingFocus.Balanced,20,0);var quick=PassingFixture(AttackingFocus.Balanced,20,100);for(int tick=0;tick<8;tick++){AI(slow.game,.1f);AI(quick.game,.1f);}Check(slow.game.StatisticsFor(slow.game.AwayTeam).Passes==0&&quick.game.StatisticsFor(quick.game.AwayTeam).Passes==1,"fast tempo releases real pass before slow-tempo hold; not a label-only bonus");
float Consume(int age,int press,int tempo,int minutes){var m=Game();m.HomeTeam.Tactics.PressingIntensity=press;m.HomeTeam.Tactics.Tempo=tempo;var p=m.HomeTeam.Players[6];p.Age=age;for(int j=0;j<minutes;j++){m.UpdateStaminaDecay(1);if(j==44){m.TriggerHalfTime();m.ResumeMatch();}}return p.CurrentStamina;}
Check(Consume(27,20,50,45)>Consume(27,45,50,45)&&Consume(27,45,50,45)>Consume(27,95,50,45),"low/mid/high press pay increasing actual stamina costs");Check(Consume(27,45,0,45)>Consume(27,45,100,45),"tempo's earlier passing benefit has a real energy cost");
Check(Consume(32,70,50,59)>40&&Consume(32,70,50,65)<40,"active but not extreme pressing exhausts older players around the requested second-half window");
foreach(var age in new[]{18,20,27,32})
 foreach(var tempo in new[]{0,100})
 {
  var remain=Consume(age,100,tempo,age==18?90:age==20?90:age==27?60:45);
  Check(age==18?remain>40:age==20?remain<=40.01f:remain<=25.01f,$"age-window integration includes real halftime recovery: age {age}, tempo {tempo}, stamina {remain:F2}");
 }
var exhausted=Game().HomeTeam.Players[5];exhausted.CurrentStamina=100;var freshSpeed=MatchEngine.FatigueMovementMultiplier(exhausted);exhausted.CurrentStamina=10;Check(MatchEngine.FatigueMovementMultiplier(exhausted)<freshSpeed,"late fatigue reduces actual chase/carry/support speed, not only duel arithmetic");
(MatchEngine game,int wins,int shots) PressEpisode(int press)
{
 var m=Game();ClearGrace(m);m.AwayTeam.Tactics.PressingIntensity=press;m.AwayTeam.Tactics.Focus=AttackingFocus.ShootOnSight;m.AwayTeam.Tactics.Tempo=95;
 foreach(var x in m.HomeTeam.Players)x.Position2D=new Vector2(5,65);foreach(var x in m.AwayTeam.Players)x.Position2D=new Vector2(95,65);
 var carrier=m.ActiveUserPlayer;carrier.Position2D=new Vector2(25,34);carrier.FacingDirection=Vector2.UnitX;carrier.BasePassing=carrier.BasePhysical=carrier.BaseSpeed=45;
 var p=m.AwayTeam.Players[1];p.Position2D=new Vector2(43,34);p.FacingDirection=-Vector2.UnitX;p.BaseDefending=p.BasePhysical=p.BaseSpeed=99;m.Ball.CatchOrTackle(carrier);
 for(int j=0;j<60;j++){AI(m,.1f);if(m.StatisticsFor(m.AwayTeam).Shots>0)break;}
 return(m,m.StatisticsFor(m.AwayTeam).Tackles,m.StatisticsFor(m.AwayTeam).Shots);
}
var lowEpisode=PressEpisode(20);var highEpisode=PressEpisode(100);Console.WriteLine($"MEASURE paired pressing recoveries low={lowEpisode.wins} high={highEpisode.wins}, shots low={lowEpisode.shots} high={highEpisode.shots}");Check(highEpisode.wins>lowEpisode.wins&&highEpisode.shots>lowEpisode.shots,"same controlled build-up: high press produces an actual recovery and shot that low block does not");
foreach(bool home in new[]{true,false})
{
 var m=Game();var team=home?m.HomeTeam:m.AwayTeam;var other=home?m.AwayTeam:m.HomeTeam;var cb=team.Players.First(p=>p.CurrentAssignedPosition==SpecificPosition.CB);m.Ball.CatchOrTackle(other.Players[9]);team.Tactics.PressingIntensity=20;var low=m.GetFormationSupportTarget(team,cb);team.Tactics.PressingIntensity=100;var high=m.GetFormationSupportTarget(team,cb);float Local(Vector2 p)=>home?p.X:Pitch.Length-p.X;
 Check(Local(high)>Local(low)+20,"high press auto-linked line leaves measurably more space behind in both directions: "+home);
 team.Tactics.OffsideTrap=false;var plain=m.GetFormationSupportTarget(team,cb);team.Tactics.OffsideTrap=true;cb.CurrentStamina=100;var trap=m.GetFormationSupportTarget(team,cb);cb.CurrentStamina=20;var late=m.GetFormationSupportTarget(team,cb);Check(Local(trap)>Local(plain)+4.9f&&Local(late)<Local(trap)-4.9f,"actual trap step is lost for a tired defender, exposing a broken line: "+home);
}
// Counter benefit is a bounded transition, not permanent possession immunity.
var counter=Game();ClearGrace(counter);counter.HomeTeam.Tactics.Focus=AttackingFocus.CounterAttack;counter.HomeTeam.Tactics.Tempo=100;var runner=counter.HomeTeam.Players[9];counter.Ball.CatchOrTackle(counter.AwayTeam.Players[9]);AI(counter,.01f);counter.Ball.CatchOrTackle(counter.HomeTeam.Players[6]);AI(counter,.01f);var burst=counter.GetFormationSupportTarget(counter.HomeTeam,runner);typeof(MatchEngine).GetField("_elapsedSeconds",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(counter,10f);var settled=counter.GetFormationSupportTarget(counter.HomeTeam,runner);Check(burst.X>settled.X+10&&!counter.IsCounterTransition(counter.HomeTeam),"counter-attacking support bursts only after a real regain and expires, not for the entire match");

// The trap must change real positions before the kick; tired players can keep a runner onside.
(MatchEngine game,bool candidate,float line) TrapEpisode(bool tired)
{
 var m=Game();m.AwayTeam.ChangeFormation(FormationType.Formation433_Attack);m.HomeTeam.ChangeFormation(FormationType.Formation433_Attack);ClearGrace(m);m.AwayTeam.Tactics.PressingIntensity=100;m.AwayTeam.Tactics.OffsideTrap=true;m.AwayTeam.Tactics.Marking=DefensiveMarking.Zonal;
 typeof(MatchEngine).GetField("_possessionGraceTimer",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(m,999f);
 var kicker=m.HomeTeam.Players[6];var runner=m.ActiveUserPlayer;runner.Position2D=new Vector2(58,34);kicker.Position2D=new Vector2(40,34);m.Ball.CatchOrTackle(kicker);
 var lagger=m.AwayTeam.Players.First(p=>p.CurrentAssignedPosition==SpecificPosition.CB);if(tired)lagger.CurrentStamina=20;
 for(int tick=0;tick<70;tick++){AI(m,.1f);kicker.Position2D=new Vector2(40,34);m.Ball.CatchOrTackle(kicker);runner.Position2D=new Vector2(58,34);}
 m.CheckOffsideTrap(m.HomeTeam,m.AwayTeam,runner,new Vector2(80,34));
 var dictionary=(System.Collections.IDictionary)typeof(MatchEngine).GetField("_offsidePositions",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(m)!;
 float line=m.AwayTeam.Players.Where(p=>p.IsActiveOnPitch).Select(p=>p.Position2D.X).OrderByDescending(x=>x).Skip(1).First();
 bool candidate=dictionary.Contains(runner);
 if(candidate){m.Ball.Owner=null;m.Ball.Position=new Vector3(runner.Position2D,0);m.TryControlIncomingBall(runner,testRoll:1);typeof(MatchEngine).GetMethod("ReviewPendingOffside",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(m,null);}
 return(m,candidate,line);
}
MatchSettings.Current.AiVarEnabled=false;var readyTrap=TrapEpisode(false);var brokenTrap=TrapEpisode(true);Console.WriteLine($"MEASURE actual trap line ready={readyTrap.line:F2} broken={brokenTrap.line:F2}");Check(readyTrap.candidate&&!brokenTrap.candidate,"actual coordinated movement catches runner offside; one tired defender breaks the real kick-time line");Check(readyTrap.game.Restart?.Kind==RestartKind.IndirectFreeKick&&brokenTrap.game.Restart==null,"actual trap success needs touch and awards indirect restart; failure does not invent an offside whistle");
// Same body-first geometry, all commitment choices. The CPU centre-ball representation must not erase fouls.
var foulGame=Game();var defender=foulGame.AwayTeam.Players[1];var protectedCarrier=foulGame.ActiveUserPlayer;foreach(var x in foulGame.HomeTeam.Players.Concat(foulGame.AwayTeam.Players))x.Position2D=new Vector2(95,65);defender.Position2D=new Vector2(49,34);protectedCarrier.Position2D=new Vector2(50,34);defender.FacingDirection=protectedCarrier.FacingDirection=Vector2.UnitX;defender.BaseDefending=defender.BasePhysical=defender.BaseSpeed=55;protectedCarrier.BasePassing=protectedCarrier.BasePhysical=protectedCarrier.BaseSpeed=90;
float previousAgg=-100;
foreach(var aggression in Enum.GetValues<TacklingAggression>())
{
 foulGame.AwayTeam.Tactics.Tackling=aggression;var d=MatchEngine.EvaluateTackleDuel(defender,protectedCarrier,protectedCarrier.Position2D,foulGame.AwayTeam,foulGame.HomeTeam,DifficultyLevel.Normal);
 Check(d.TacticalModifier>previousAgg,"higher commitment changes the real same-geometry challenge advantage: "+aggression);previousAgg=d.TacticalModifier;
 Check(!d.TacklerWon&&(aggression!=TacklingAggression.Cautious||!d.IsFoul),"body-first CPU cannot win through the protected carrier; cautious containment avoids foul: "+aggression);
 if(aggression==TacklingAggression.Reckless)Check(d.IsFoul&&d.IsSevereInjuryRisk&&d.FromBehind&&d.SlidingCommitment,"reckless body-first tackle has actual foul, rear-angle and severe-injury cost even with ball stored at carrier centre");
}
var frontGame=Game();var fp=frontGame.AwayTeam.Players[1];var fc=frontGame.ActiveUserPlayer;foreach(var x in frontGame.HomeTeam.Players.Concat(frontGame.AwayTeam.Players))x.Position2D=new Vector2(95,65);fp.Position2D=new Vector2(51,34);fc.Position2D=new Vector2(50,34);fp.FacingDirection=-Vector2.UnitX;fc.FacingDirection=Vector2.UnitX;fp.PrimaryPosition=fp.CurrentAssignedPosition;fc.PrimaryPosition=fc.CurrentAssignedPosition;fp.BaseDefending=fp.BasePhysical=fp.BaseSpeed=55;fc.BasePassing=fc.BasePhysical=fc.BaseSpeed=75;frontGame.AwayTeam.Tactics.Tackling=TacklingAggression.Cautious;var cautious=MatchEngine.EvaluateTackleDuel(fp,fc,fc.Position2D,frontGame.AwayTeam,frontGame.HomeTeam,DifficultyLevel.Normal);frontGame.AwayTeam.Tactics.Tackling=TacklingAggression.Reckless;var committed=MatchEngine.EvaluateTackleDuel(fp,fc,fc.Position2D,frontGame.AwayTeam,frontGame.HomeTeam,DifficultyLevel.Normal);Check(committed.TacticalModifier>cautious.TacticalModifier+30&&!committed.IsFoul,"commitment has a clean front-on benefit, not an automatic foul or guaranteed goal");
// Transition advantage may not remain active forever, and the short-build trap belongs near the opponent box.
frontGame.HomeTeam.Tactics.Focus=AttackingFocus.CounterAttack;frontGame.HomeTeam.Tactics.Tempo=100;var settledDuel=MatchEngine.EvaluateTackleDuel(fp,fc,fc.Position2D,frontGame.AwayTeam,frontGame.HomeTeam,DifficultyLevel.Normal);var transitionDuel=MatchEngine.EvaluateTackleDuel(fp,fc,fc.Position2D,frontGame.AwayTeam,frontGame.HomeTeam,DifficultyLevel.Normal,counterTransitionActive:true);Check(Math.Abs(settledDuel.TacticalModifier-transitionDuel.TacticalModifier-14)<.01f,"counter duel benefit is exactly bounded to an active transition, not a permanent setting immunity");
frontGame.AwayTeam.Tactics.PressingIntensity=100;frontGame.HomeTeam.Tactics.PassingDirectness=20;fp.Position2D=new Vector2(11.1f,34);fc.Position2D=new Vector2(10,34);var boxDuel=MatchEngine.EvaluateTackleDuel(fp,fc,fc.Position2D,frontGame.AwayTeam,frontGame.HomeTeam,DifficultyLevel.Normal);fp.Position2D=new Vector2(51.1f,34);fc.Position2D=new Vector2(50,34);var midfieldDuel=MatchEngine.EvaluateTackleDuel(fp,fc,fc.Position2D,frontGame.AwayTeam,frontGame.HomeTeam,DifficultyLevel.Normal);Check(Math.Abs(boxDuel.TacticalModifier-midfieldDuel.TacticalModifier-14)<.01f,"high-press v short-build trap applies around the actual penalty-area build-up, not everywhere on the pitch");

// Commitments reach the actual CPU foul/card/injury path, not merely a printed risk number.
MatchEngine RearChallenge(TacklingAggression commitment)
{
 var m=Game();ClearGrace(m);m.AwayTeam.Tactics.PressingIntensity=100;m.AwayTeam.Tactics.Tackling=commitment;
 foreach(var x in m.HomeTeam.Players.Concat(m.AwayTeam.Players))x.Position2D=new Vector2(95,65);
 var p=m.AwayTeam.Players[1];var c=m.ActiveUserPlayer;p.Position2D=new Vector2(49,34);c.Position2D=new Vector2(50,34);p.FacingDirection=c.FacingDirection=Vector2.UnitX;p.BaseDefending=p.BasePhysical=p.BaseSpeed=55;c.BasePassing=c.BasePhysical=c.BaseSpeed=90;m.Ball.CatchOrTackle(c);AI(m,.01f);return m;
}
var carefulChallenge=RearChallenge(TacklingAggression.Cautious);var recklessChallenge=RearChallenge(TacklingAggression.Reckless);
Check(carefulChallenge.StatisticsFor(carefulChallenge.AwayTeam).Fouls==0&&carefulChallenge.StatisticsFor(carefulChallenge.AwayTeam).RedCards==0&&carefulChallenge.Ball.Owner==carefulChallenge.ActiveUserPlayer,"actual cautious CPU contains shielded carrier without a free win, foul or card");
Check(recklessChallenge.StatisticsFor(recklessChallenge.AwayTeam).Fouls==1&&recklessChallenge.StatisticsFor(recklessChallenge.AwayTeam).RedCards==1&&recklessChallenge.PendingInjuryPlayer!=null,"actual reckless rear slide costs foul, red card and severe injury choice, not just a risk description");
// Tracking a player is not the same as protecting a zone.
(float distance,float displacement) MarkingEpisode(DefensiveMarking marking)
{
 var m=Game();m.AwayTeam.ChangeFormation(FormationType.Formation433_Attack);ClearGrace(m);m.AwayTeam.Tactics.PressingIntensity=20;m.AwayTeam.Tactics.Marking=marking;foreach(var p in m.HomeTeam.Players)p.Position2D=new Vector2(20,34);
 var bait=m.ActiveUserPlayer;bait.Position2D=new Vector2(70,2);var holder=m.HomeTeam.Players[6];holder.Position2D=new Vector2(20,34);m.Ball.CatchOrTackle(holder);var marker=m.AwayTeam.Players[1];var before=marker.Position2D;
 for(int j=0;j<30;j++){AI(m,.1f);bait.Position2D=new Vector2(70,2);holder.Position2D=new Vector2(20,34);m.Ball.CatchOrTackle(holder);}
 return(Vector2.Distance(marker.Position2D,bait.Position2D),Math.Abs(marker.Position2D.Y-before.Y));
}
var zonal=MarkingEpisode(DefensiveMarking.Zonal);var individual=MarkingEpisode(DefensiveMarking.ManToMan);Check(individual.distance<zonal.distance&&individual.displacement>zonal.displacement+20,"actual man-marking follows the wide bait more tightly but vacates its original lane; zonal preserves it");
// Age24 is a user decision (13 Nov voice), not an inferred boundary.
Check(Math.Abs(Consume(24,100,100,79)-Consume(20,100,100,79))<.001f&&Consume(24,100,100,79)>40&&Consume(24,100,100,90)<=40,"confirmed age24 has youth last-ten-minute curve, not the age25-29 minute60 curve");
Check(Consume(25,100,100,60)<=25.01f,"age25 immediately starts the mature minute60 group without overlapping age24");
// No stale transition from a prior half may grant the next kickoff a free counter.
var reset=Game();ClearGrace(reset);reset.Ball.CatchOrTackle(reset.HomeTeam.Players[6]);AI(reset,.01f);reset.Ball.CatchOrTackle(reset.AwayTeam.Players[6]);AI(reset,.01f);Check(reset.IsCounterTransition(reset.AwayTeam),"transition fixture begins from a real possession change");reset.TriggerHalfTime();Check(!reset.IsCounterTransition(reset.AwayTeam)&&!reset.IsCounterTransition(reset.HomeTeam),"halftime clears prior transition and CPU decision timers");
// Exhaustion must persist after lowering pressure, not disappear when a slider is changed.
var recover=Game();var veteran=recover.HomeTeam.Players[6];veteran.Age=32;recover.HomeTeam.Tactics.PressingIntensity=100;recover.UpdateStaminaDecay(40);float spent=veteran.CurrentStamina;recover.HomeTeam.Tactics.PressingIntensity=20;recover.UpdateStaminaDecay(1);Check(veteran.CurrentStamina<=spent&&veteran.CurrentStamina>spent-1,"lowering pressure slows further fatigue but never restores spent energy");
var reserve=Game();var reserveStarter=reserve.HomeTeam.Players[6];reserveStarter.Age=32;reserve.HomeTeam.Tactics.PressingIntensity=100;reserve.UpdateStaminaDecay(40);var bench=new Player(999,"Fresh reserve",19,SpecificPosition.CM,75,75,75,75,75){Age=32};reserve.HomeTeam.Bench.Add(bench);reserve.SetMatchTimeMinutes(70);reserve.State=MatchState.HalfTime;bool cameOn=reserve.MakeSubstitution(reserve.HomeTeam,reserveStarter,bench);Check(cameOn&&bench.CurrentStamina>90,"fresh substitute starts from own energy, not veteran's cumulative team fatigue");

var nearAim=MatchEngine.ComputeCpuShotTarget(new Vector2(15,34),false,.1,.95,75,100);var distantAim=MatchEngine.ComputeCpuShotTarget(new Vector2(30,34),false,.1,.95,75,100);Check(Math.Abs(distantAim.Y-34)>Math.Abs(nearAim.Y-34),"same actual shot sample has greater angular error from distance: long-shot benefit has an accuracy cost");var expertAim=MatchEngine.ComputeCpuShotTarget(new Vector2(30,34),false,.1,.95,95,100);var weakAim=MatchEngine.ComputeCpuShotTarget(new Vector2(30,34),false,.1,.95,40,100);Check(Math.Abs(expertAim.Y-34)<Math.Abs(weakAim.Y-34),"CPU shooter accuracy uses actual shooting ability, not only power or a fixed spread");
foreach(var focus in Enum.GetValues<AttackingFocus>())
 foreach(bool invert in new[]{false,true})
 {
  MatchSettings.Current.ResetToDefaults();MatchSettings.Current.AutoTacticalAdjustmentsEnabled=false;MatchSettings.Current.MatchDurationMinutes=1;MatchSettings.Current.ExtraTimeEnabled=false;MatchSettings.Current.PenaltyShootoutEnabled=false;
  var m=Game();m.HomeTeam.ChangeFormation(FormationType.Formation3421);m.AwayTeam.ChangeFormation(FormationType.Formation4222);m.SetStoppageMinutes(0,0);m.HomeTeam.Tactics.Focus=focus;m.AwayTeam.Tactics.Focus=focus;
  m.HomeTeam.Tactics.PressingIntensity=invert?20:100;m.AwayTeam.Tactics.PressingIntensity=invert?100:20;m.HomeTeam.Tactics.PassingDirectness=invert?20:90;m.AwayTeam.Tactics.PassingDirectness=invert?90:20;m.HomeTeam.Tactics.OffsideTrap=m.AwayTeam.Tactics.OffsideTrap=true;m.AwayTeam.Tactics.Marking=invert?DefensiveMarking.ManToMan:DefensiveMarking.Zonal;
  bool valid=true,finished=false,first=false,second=false;
  for(int j=0;j<16000;j++)
  {
   if(m.State==MatchState.GameOver){finished=true;break;}
   if(m.PendingInjuryPlayer is {} hurt)m.ResolveInjurySubstitution(hurt,m.HomeTeam.Bench.FirstOrDefault(m.HomeTeam.CanEnterAsSubstitute));
   if(m.AwaitingStoppageConfirmation)m.DismissStoppageReplay();if(m.AnalysisReplayPending)m.CompleteAnalysisReplay();if(m.State is MatchState.HalfTime or MatchState.Paused)m.ResumeMatch();if(m.State==MatchState.GoalCelebration)m.CompleteGoalCelebration();first|=m.UserAttackingRight;second|=!m.UserAttackingRight;m.Update(.1f);
   var all=m.HomeTeam.Players.Concat(m.AwayTeam.Players).ToArray();valid&=float.IsFinite(m.MatchTimeMinutes)&&float.IsFinite(m.Ball.Position.X)&&float.IsFinite(m.Ball.Position.Y)&&all.All(p=>float.IsFinite(p.Position2D.X)&&float.IsFinite(p.Position2D.Y)&&p.CurrentStamina>=0)&&(m.Ball.Owner==null||m.Ball.Owner.IsActiveOnPitch)&&all.Count(p=>p.HasBall)==(m.Ball.Owner==null?0:1);
  }
  Check(valid&&finished&&m.FullTimeAnnounced&&first&&second,"whole-match combined tactical instructions maintain legal finite state through both halves: "+focus+" high-home="+!invert);
 }

foreach(var language in new[]{UiLanguage.Arabic,UiLanguage.English})
{
 MatchSettings.Current.Language=language;var team=Squad("menu",TeamSide.Home);var reader=new Reader();var menu=GamePlanMenuBuilder.CreateTacticsMenu(team,reader,()=>{});
 var pressing=(AccessibleSliderMenuItem)menu.GetItem(0);int original=team.Tactics.PressingIntensity;menu.NavigateRight();Check(team.Tactics.PressingIntensity==original+1&&team.Tactics.DefensiveLineDepth==original+1,"actual menu Right increments pressure by one and updates linked line: "+language);
 Check(((AccessibleSliderMenuItem)menu.GetItem(2)).Value==team.Tactics.DefensiveLineDepth,"linked line display cannot silently remain stale: "+language);for(int j=0;j<200;j++)menu.NavigateLeft();Check(team.Tactics.PressingIntensity==0,"actual menu slider clamps at zero without wrapping: "+language);for(int j=0;j<200;j++)menu.NavigateRight();Check(team.Tactics.PressingIntensity==100,"actual menu slider reaches every value and clamps at100: "+language);
 var depth=(AccessibleSliderMenuItem)menu.GetItem(2);depth.OnLeft();pressing.OnLeft();Check(team.Tactics.DefensiveLineDepth==99&&team.Tactics.PressingIntensity==99,"explicit line control can diverge from pressing instead of being overwritten: "+language);depth.OnLeft();pressing.OnLeft();pressing.OnLeft();Check(team.Tactics.DefensiveLineDepth==98&&team.Tactics.PressingIntensity==97,"manual line remains independent across later pressure changes: "+language);
 menu.GetItem(4).OnSelect!();Check(team.Tactics.DefensiveLineDepth==team.Tactics.PressingIntensity&&team.Tactics.Tempo==team.Tactics.PassingDirectness,"actual relink callback restores linked controls and displayed state: "+language);
 reader.Spoken.Clear();menu.GetItem(menu.Count-2).OnSelect!();Check(reader.Spoken.Last().Contains(language==UiLanguage.English?"No setting guarantees":"لا توجد نسبة مضمونة"),"actual report explains benefit/cost and avoids guaranteed-chance claim: "+language);
 Check(UserGuideDatabase.GetItemsByCategory("tactics").Count==4,"tactical formulas, age windows and all choice tradeoffs are reachable guide items: "+language);
}
var linked=new TeamTactics();var linkedCopy=linked.Clone();linkedCopy.PressingIntensity=95;linkedCopy.PassingDirectness=90;Check(linkedCopy.DefensiveLineDepth==95&&linkedCopy.Tempo==90,"cloning preserves automatic control links instead of silently breaking them");var independent=new TeamTactics{DefensiveLineDepth=20,Tempo=15};var independentCopy=independent.Clone();independentCopy.PressingIntensity=95;independentCopy.PassingDirectness=90;Check(independentCopy.DefensiveLineDepth==20&&independentCopy.Tempo==15,"cloning also preserves deliberate independent line/tempo overrides");

var initialized=Squad("initial",TeamSide.Home);Check(initialized.Players.Any(p=>p.CurrentAssignedPosition==SpecificPosition.CDM)&&initialized.Players.Count(p=>p.CurrentAssignedPosition is SpecificPosition.LB or SpecificPosition.RB)==2,"initial default formation assigns functional pivot/full-back slots without waiting for the user to reselect it");initialized.Players[1].PrimaryPosition=SpecificPosition.CB;initialized.SetupFormation433();Check(initialized.Players[1].PrimaryPosition==SpecificPosition.CB&&initialized.Players[1].CurrentAssignedPosition==SpecificPosition.LB,"initial role assignment preserves source primary-position evidence rather than rewriting player data");

foreach(var shape in new[]{FormationType.Formation343_AllOut,FormationType.Formation3421,FormationType.Formation352_Wings,FormationType.Formation532_Counter,FormationType.Formation541_Bus})
{
 var m=Game();m.AwayTeam.ChangeFormation(shape);m.AwayTeam.Tactics.PressingIntensity=90;m.AwayTeam.Tactics.OffsideTrap=true;m.Ball.CatchOrTackle(m.HomeTeam.Players[6]);var back=m.AwayTeam.Players.Where(p=>p.CurrentAssignedPosition is SpecificPosition.LWB or SpecificPosition.RWB or SpecificPosition.CB).Select(p=>m.GetFormationSupportTarget(m.AwayTeam,p).X).ToArray();Check(back.Max()-back.Min()<=2.1f,"wing-back recovery joins the actual coordinated centre-back line, not a separate deeper line: "+shape);
}
foreach(var language in new[]{UiLanguage.Arabic,UiLanguage.English})
{
 MatchSettings.Current.Language=language;var t=Squad("labels",TeamSide.Home);var menu=GamePlanMenuBuilder.CreateTacticsMenu(t,new Reader(),()=>{});bool changed=true;for(int index=5;index<=8;index++){var before=menu.GetItem(index).Title;menu.GetItem(index).OnSelect!();changed&=menu.GetItem(index).Title!=before;}Check(changed,"all changed tactical choice labels reflect actual state without reopening menu: "+language);
}
Console.WriteLine($"RESULT {passed} passed, {failed} failed; no Windows runtime.");return failed==0?0:1;
sealed class Input:IInputHandler{public HashSet<GameAction> PollCurrentActions(bool possession)=>new();public bool IsActionTriggered(GameAction action,bool possession)=>false;}

sealed class Reader:IScreenReader{public List<string> Spoken=new();public bool IsAvailable=>true;public void Speak(string text,bool interrupt=true)=>Spoken.Add(text);public void CancelSpeech(){}public Task SpeakUntilFinishedAsync(string text,bool interrupt=true){Speak(text,interrupt);return Task.CompletedTask;}}
