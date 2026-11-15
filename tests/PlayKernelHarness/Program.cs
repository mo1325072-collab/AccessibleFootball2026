using System.Numerics;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Input;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;
using AccessibleFootball2026.Core.Simulation.Play;
using AccessibleFootball2026.Input;
int pass = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception(name); Console.WriteLine("PASS " + name); pass++; }
Player P(int id) => new(id, "P" + id, id, SpecificPosition.ST, 75, 75, 75, 75, 75);
// Same physical sequence, all modifier/action press orders.
foreach (var order in new[] { new[] { 16, 39, 17, 83 }, new[] { 83, 17, 39, 16 }, new[] { 39, 83, 16, 17 } })
{
 double clock = 0; var input = new KeyboardInputHandler(() => clock);
 foreach (int key in order) input.HandleKeyDown(key, false);
 var result = input.PollCurrentActions(false);
 Check(result.SetEquals(new[] { GameAction.Sprint, GameAction.MoveRight, GameAction.SwitchPlayer, GameAction.StandTackle }), "independent concurrent key downs: " + string.Join(",", order));
 foreach (int key in order) input.HandleKeyDown(key, false);
 Check(input.PollCurrentActions(false).SetEquals(new[] { GameAction.Sprint, GameAction.MoveRight }), "OS repeats never repeat switch/tackle");
 input.HandleKeyUp(17); input.HandleKeyUp(83);
 Check(input.PollCurrentActions(false).SetEquals(new[] { GameAction.Sprint, GameAction.MoveRight }), "releasing action/control preserves held locomotion");
 input.HandleKeyDown(68, false);
 Check(input.PollCurrentActions(false).SetEquals(new[] { GameAction.Sprint, GameAction.MoveRight, GameAction.SlideTackle }), "slide and held sprint are independent");
 input.HandleKeyUp(68); input.HandleKeyDown(17, false);
 Check(input.PollCurrentActions(false).Contains(GameAction.SwitchPlayer), "fresh Ctrl press switches again");
 input.HandleKeyUp(39);
 Check(input.PollCurrentActions(false).SetEquals(new[] { GameAction.Sprint }), "arrow release stops only that direction");
}
{
 double clock = 0; var input = new KeyboardInputHandler(() => clock);
 input.HandleKeyDown(16, true); input.HandleKeyDown(39, true); input.HandleKeyDown(17, true); input.HandleKeyDown(87, true);
 Check(!input.PollCurrentActions(true).Contains(GameAction.Shoot), "charged shot does not fire on key down");
 clock = 1.5; Check(input.ActiveCharge01(GameAction.Shoot) == 1, "independent clock reaches full shot power");
 input.HandleKeyUp(17); input.HandleKeyUp(87);
 Check(input.PollCurrentActions(true).SetEquals(new[] { GameAction.MoveRight, GameAction.Sprint, GameAction.Shoot }) && input.ConsumeReleasedCharge01(GameAction.Shoot) == 1, "release executes one charged shot while sprint continues");
 Check(input.PollCurrentActions(true).SetEquals(new[] { GameAction.MoveRight, GameAction.Sprint }), "released shot is not repeated");
 input.HandleKeyDown(87, true); input.PollCurrentActions(true); input.PollCurrentActions(false); input.HandleKeyUp(87);
 Check(!input.PollCurrentActions(false).Contains(GameAction.Shoot), "possession loss cancels an in-progress charge");
 input.HandleKeyDown(83, false); input.PollCurrentActions(true);
 Check(!input.PollCurrentActions(true).Contains(GameAction.ShortPass), "held defensive key cannot become an attack after winning possession");
 input.Reset(); input.HandleKeyDown(16, true); input.HandleKeyDown(39, true);
 Check(input.PollCurrentActions(true).Count == 0, "held keys crossing pause/focus reset cannot leak into resumed play");
 input.HandleKeyUp(16); input.HandleKeyUp(39); input.HandleKeyDown(16, true); input.HandleKeyDown(39, true);
 Check(input.PollCurrentActions(true).SetEquals(new[] { GameAction.Sprint, GameAction.MoveRight }), "fresh release and press restores locomotion after reset");
 input.HandleKeyDown(32, true); Check(input.PollCurrentActions(true).Contains(GameAction.TacticalRadar), "Space is an explicit reader request");
 input.HandleKeyDown(32, true); Check(!input.PollCurrentActions(true).Contains(GameAction.TacticalRadar), "held Space does not flood reader requests");
}
{
 var a=P(1);var b=P(2);a.Position2D=new(10,20);b.Position2D=new(11,20);a.FacingDirection=Vector2.UnitX;
 var motion=PlayerMotion.Advance(a,new Vector2(30,0),.2f,new[]{b});
 Check(motion.Contact && motion.Position.X <= 10.451f && Vector2.Distance(motion.Position,b.Position2D)>=.549f,"swept motion cannot tunnel through a body at a large tick");
 var free=PlayerMotion.Advance(a,new Vector2(0,10),.1f,Array.Empty<Player>());
 Check(Vector2.Distance(free.Position,new Vector2(10,21))<.0001f,"physical arrows remain absolute pitch coordinates");
 var ball=new Ball();ball.CatchOrTackle(a);Check(a.HasBall&&ball.Owner==a,"first control sets player ownership");
 ball.CatchOrTackle(b);Check(!a.HasBall&&b.HasBall&&ball.Owner==b,"ownership transition clears previous player flag");
 ball.Owner=null;Check(!b.HasBall&&ball.State==BallState.Free,"direct release clears flags and controlled state");
 ball.CatchOrTackle(a);ball.Kick(Vector2.UnitX,20,4,a);Check(!a.HasBall&&ball.Owner==null&&ball.State==BallState.InFlight,"kick is one atomic release into flight");
 ball.Position=new Vector3(a.Position2D,0);
 Check(PossessionArbiter.Evaluate(ball,a,true,false,false,1)==BallControlDecision.LiveShot,"a live shot cannot become a silent foot pickup");
 Check(PossessionArbiter.NearestEligible(ball,new[]{b,a},true)==null,"batch pickup cannot steal a live shot either");
 var f1=BallFlight.Advance(new Vector3(0,0,1),new Vector3(20,3,8),.3f,1);
 var f2=new BallFlight.Frame(new Vector3(0,0,1),new Vector3(20,3,8),true);
 for(int i=0;i<50;i++)f2=BallFlight.Advance(f2.Position,f2.Velocity,.3f,.02f);
 Check(Vector3.Distance(f1.Position,f2.Position)<.03f&&Vector3.Distance(f1.Velocity,f2.Velocity)<.03f,"flight and curve agree across render tick sizes");
 var ground=BallFlight.Advance(Vector3.Zero,new Vector3(10,0,0),0,1);
 Check(Math.Abs(ground.Position.X-8.75f)<.001f&&Math.Abs(ground.Velocity.X-7.5f)<.001f,"rolling uses integrated time-based grass deceleration");
 var team=new Team("h","h",TeamSide.Home);team.Players.Add(a);var ledger=new AttemptLedger();
 var shot=ledger.Begin(a,team);Check(ledger.Resolve(AttemptOutcome.Interrupted,shot.Id)!=null&&ledger.Resolve(AttemptOutcome.Goal,shot.Id)==null,"attempt has one terminal identity; late goal cannot rescore an interrupted attempt");
 Check(ledger.OfficialShots(team)==0&&ledger.OnTarget(team)==0,"interrupted attempt is neither an official shot nor on target");
 shot=ledger.Begin(a,team);ledger.Resolve(AttemptOutcome.Annulled,shot.Id);Check(ledger.OfficialShots(team)==0,"annulled play stays outside official shot totals");
 shot=ledger.Begin(a,team);ledger.Resolve(AttemptOutcome.Saved,shot.Id);Check(ledger.OfficialShots(team)==1&&ledger.OnTarget(team)==1,"a measured save is a shot on target");
}
// Real-engine input/report integration, with deterministic layouts and no AI step.
Team Squad(string id, TeamSide side)
{
 var team=new Team(id,id,side);
 for(int i=0;i<11;i++) team.Players.Add(new Player((side==TeamSide.Home?0:100)+i,id+i,i+1,i==0?SpecificPosition.GK:SpecificPosition.ST,75,75,75,75,75));
 team.AssignDefaultRoles();return team;
}
void Call(MatchEngine game,string name,params object[] args)=>typeof(MatchEngine).GetMethod(name,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.Invoke(game,args);
foreach(bool secondHalf in new[]{false,true})
{
 var keys=new KeyboardInputHandler();var bus=new MatchEventBus();var events=new List<MatchEvent>();bus.OnMatchEvent+=events.Add;
 var game=new MatchEngine(Squad("H",TeamSide.Home),Squad("A",TeamSide.Away),bus,keys);
 if(secondHalf){game.TriggerHalfTime();game.ResumeMatch();}
 var carrier=game.ActiveUserPlayer;carrier.Position2D=new Vector2(40,34);carrier.FacingDirection=Vector2.UnitX;game.Ball.CatchOrTackle(carrier);
 keys.HandleKeyDown(32,true);Call(game,"ProcessUserInput",.02f);
 var report=events.Last(e=>e.Type==MatchEventType.TacticalRadarInfo);
 int expected=(int)Vector2.Distance(game.Ball.Position2D,game.UserAttackingRight?game.Pitch.RightGoalCenter:game.Pitch.LeftGoalCenter);
 Check(report.Description.Contains(carrier.Name)&&report.Description.Contains(expected.ToString())&&!report.Description.Contains("H,"),"Space report: own holder and attacking goal distance, half "+secondHalf);
 int count=events.Count(e=>e.Type==MatchEventType.TacticalRadarInfo);Call(game,"ProcessUserInput",.02f);
 Check(events.Count(e=>e.Type==MatchEventType.TacticalRadarInfo)==count,"Space reports once per press through engine, half "+secondHalf);
 keys.HandleKeyUp(32);var other=game.AwayTeam.Players[8];other.Position2D=new Vector2(70,34);other.FacingDirection=-Vector2.UnitX;game.Ball.CatchOrTackle(other);
 keys.HandleKeyDown(32,false);Call(game,"ProcessUserInput",.02f);report=events.Last(e=>e.Type==MatchEventType.TacticalRadarInfo);
 expected=(int)Vector2.Distance(game.Ball.Position2D,game.UserAttackingRight?game.Pitch.LeftGoalCenter:game.Pitch.RightGoalCenter);
 Check(report.Description.Contains(other.Name)&&report.Description.Contains(expected.ToString())&&!report.Description.Contains("(A)"),"opponent report uses user's defended goal without team, half "+secondHalf);
 keys.HandleKeyUp(32);
 // The shared player clamp must not protect the goal: carried feet cross the line.
 foreach(bool right in new[]{false,true})
 {
  carrier.Position2D=new Vector2(right?104.8f:.2f,34);carrier.FacingDirection=new Vector2(right?1:-1,0);
  game.Ball.CatchOrTackle(carrier);keys.HandleKeyDown(right?39:37,true);Call(game,"ProcessUserInput",.1f);game.Ball.Update(.1f);
  Check(game.Pitch.IsGoal(game.Ball.Position,out var atRight)&&atRight==right,"carried ball can cross either goal line, half "+secondHalf+" right "+right);
  keys.HandleKeyUp(right?39:37);
 }
}
// Triggering sequences through real challenge and attempt pipelines.
void Field(MatchEngine game)
{
 foreach(var player in game.HomeTeam.Players.Concat(game.AwayTeam.Players)) {player.Position2D=new Vector2(5+player.Id%11*8,5+(player.Id<100?0:5));player.Velocity=Vector2.Zero;}
}
MatchEngine Game(out KeyboardInputHandler keys,out List<MatchEvent> events)
{
 keys=new KeyboardInputHandler();var bus=new MatchEventBus();var list=new List<MatchEvent>();events=list;bus.OnMatchEvent+=list.Add;
 var game=new MatchEngine(Squad("H",TeamSide.Home),Squad("A",TeamSide.Away),bus,keys);Field(game);return game;
}
{
 var game=Game(out var keys,out var events);var carrier=game.AwayTeam.Players[8];var defender=game.ActiveUserPlayer;
 carrier.Position2D=new(50,34);carrier.FacingDirection=Vector2.UnitX;game.Ball.CatchOrTackle(carrier);
 defender.Position2D=new(50.7f,34);defender.FacingDirection=-Vector2.UnitX;
 var original=carrier.Position2D;game.ExecuteStandTackle();
 Check(game.Ball.Owner==defender&&!carrier.HasBall&&defender.HasBall,"manual S challenge resolves ownership through shared contest");
 Check(carrier.Position2D==original,"winning a challenge never teleports former carrier 1.5 metres");
 // Reuse the same positions and skills with the AI entry point: same result, not separate rules.
 var ai=Game(out _,out _);var ac=ai.HomeTeam.Players[8];var ad=ai.AwayTeam.Players[8];ac.Position2D=new(50,34);ac.FacingDirection=Vector2.UnitX;ad.Position2D=new(50.7f,34);ad.FacingDirection=-Vector2.UnitX;ai.Ball.CatchOrTackle(ac);
 Call(ai,"ResolveChallenge",ad,ac,false,false);Check(ai.Ball.Owner==ad,"CPU uses same skill, geometry and committed transition as manual challenge");
 var ball=Ball.FeetOf(ac);ad.Position2D=new(49.45f,34);ad.FacingDirection=Vector2.UnitX;ad.Velocity=Vector2.Zero;
 var behind=ChallengeResolver.Evaluate(ad,ac,ball,ai.AwayTeam,ai.HomeTeam,DifficultyLevel.Normal,Vector2.Zero,Vector2.Zero,true);
 Check(!behind.TacklerWon&&behind.IsFoul&&!behind.IsSevereInjuryRisk,"stationary sliding through shielding body is a foul without invented severe impact");
 ad.Position2D=new(20,34);var far=ChallengeResolver.Evaluate(ad,ac,ball,ai.AwayTeam,ai.HomeTeam,DifficultyLevel.Legendary,new Vector2(99,0),Vector2.Zero,true,contestRoll:0,foulRoll:0);
 Check(!far.TacklerWon&&!far.IsFoul,"unreachable challenge cannot win or foul, even with old forced-roll test seams");
}
foreach(string outcome in new[]{"dead","touchline","blocked","goal","interrupt45","interrupt90","interrupt105","interrupt120"})
{
 var game=Game(out _,out var events);var shooter=game.ActiveUserPlayer;
 if(outcome is "interrupt90" or "interrupt105" or "interrupt120") { game.TriggerHalfTime();game.ResumeMatch(); }
 if(outcome is "interrupt105" or "interrupt120") { game.TriggerExtraTime();game.ResumeMatch(); }
 if(outcome=="interrupt120") {game.SetMatchTimeMinutes(104.99f);game.Update(.02f);if(game.State==MatchState.HalfTime)game.ResumeMatch();}
 Field(game);shooter=game.ActiveUserPlayer;shooter.Position2D=new(60,34);shooter.FacingDirection=Vector2.UnitX;game.Ball.CatchOrTackle(shooter);
 Check(game.TryExecuteTechnique(ShotTechnique.Driven),"shot starts before "+outcome+" sequence");
 int beforeMiss=game.PerformanceOf(shooter).Misses;var previous=game.Ball.Position;
 if(outcome=="dead") {game.Ball.Position=new(70,34,0);game.Ball.Velocity=Vector3.Zero;Call(game,"CheckRefereeRules",previous);}
 if(outcome=="touchline") {game.Ball.Position=new(70,69,0);game.Ball.Velocity=new(10,3,0);Call(game,"CheckRefereeRules",previous);}
 if(outcome=="blocked") {var block=game.AwayTeam.Players[8];block.Position2D=new(61,34);game.Ball.Position=new(62,34,0);Call(game,"CheckShotBlock",previous);}
 if(outcome=="goal") {game.Ball.Position=new(106,34,0);game.Ball.Velocity=new(20,0,0);Call(game,"CheckRefereeRules",new Vector3(104,34,0));}
 if(outcome.StartsWith("interrupt"))
 {
  int minute=int.Parse(outcome[9..]);game.SetStoppageMinutes(0,0);game.SetMatchTimeMinutes(minute-.01f);game.Update(.2f);
  Check(game.PerformanceOf(shooter).Misses==beforeMiss&&!events.Any(e=>e.Type==MatchEventType.MissedChance),"period whistle never fabricates miss/rating penalty at "+minute);
  Check(game.StatisticsFor(game.HomeTeam).Shots==0,"interrupted shot excluded from official total at "+minute);
 }
 Check(game.Attempts.Count==1&&game.Attempts[0].Outcome!=null,"one terminal attempt outcome after actual "+outcome+" sequence");
 var terminal=game.Attempts[0].Outcome;Call(game,"CheckRefereeRules",game.Ball.Position);
 Check(game.Attempts[0].Outcome==terminal,"later consumers cannot terminally resolve same "+outcome+" twice");
}
foreach(bool secondHalf in new[]{false,true})
{
 var game=Game(out var keys,out var events);if(secondHalf){game.TriggerHalfTime();game.ResumeMatch();}Field(game);
 var actor=game.ActiveUserPlayer;float sign=game.UserAttackingRight?-1:1;
 actor.Position2D=new(game.UserAttackingRight?.15f:104.85f,34);actor.FacingDirection=new(sign,0);
 game.Ball.Owner=null;game.Ball.Position=new Vector3(actor.Position2D,0);game.Ball.Velocity=Vector3.Zero;game.TryControlIncomingBall(actor,testRoll:1);
 keys.HandleKeyDown(sign<0?37:39,true);game.Update(.04f);
 var goal=events.SingleOrDefault(e=>e.Type==MatchEventType.GoalScored);
 Check(goal?.GoalData is {IsOwnGoal:true}&&goal.GoalData.Scorer==actor&&goal.GoalData.ScoringTeam==game.AwayTeam,"actual held-arrow own goal keeps scorer and beneficiary, half "+secondHalf);
 Check(game.HomeTeam.Score==0&&game.AwayTeam.Score==1&&game.StatisticsFor(game.HomeTeam).Shots==0&&game.StatisticsFor(game.AwayTeam).ShotsOnTarget==0,"actual carried own goal never fabricates a shot or on-target attempt, half "+secondHalf);
}
{
 var game=Game(out var keys,out var events);var actor=game.ActiveUserPlayer;actor.Position2D=new(60,34);game.Ball.CatchOrTackle(actor);
 keys.HandleKeyDown(87,true);keys.PollCurrentActions(true);
 game.Ball.CatchOrTackle(game.AwayTeam.Players[8]);game.Ball.CatchOrTackle(actor);keys.HandleKeyUp(87);Call(game,"ProcessUserInput",.02f);
 Check(!events.Any(e=>e.Type==MatchEventType.ShotTaken),"loss and regain between input polls invalidates original charge");
 keys.HandleKeyDown(16,true);keys.HandleKeyDown(39,true);keys.HandleKeyDown(87,true);game.PauseMatch();keys.HandleKeyUp(87);game.ResumeMatch();game.Update(.02f);
 Check(!events.Any(e=>e.Type==MatchEventType.ShotTaken),"pause cancels charge without a delayed shot after resume");
}
{
 var game=Game(out var keys,out var events);var carrier=game.AwayTeam.Players[8];carrier.Position2D=new(50,34);carrier.FacingDirection=Vector2.UnitX;game.Ball.CatchOrTackle(carrier);
 var old=game.ActiveUserPlayer;old.Position2D=new(20,34);old.FacingDirection=Vector2.UnitY;game.ExecuteSlideTackle();
 var next=game.HomeTeam.Players.First(p=>p!=old&&p.Role!=PlayerRole.Goalkeeper);next.Position2D=new(49,34);
 typeof(MatchEngine).GetField("_defensiveApproachAssistStrength",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.SetValue(game,0f);
 keys.HandleKeyDown(16,false);keys.HandleKeyDown(39,false);keys.HandleKeyDown(17,false);Call(game,"ProcessUserInput",.1f);
 Check(game.ActiveUserPlayer==next&&next.Position2D.X>49&&Math.Abs(next.Position2D.Y-34)<.001f,"Ctrl while sliding moves new defender with held horizontal sprint, never old slide impulse");
 Check(!game.IsSliding,"slide state remains attached to old actor after switch");
}
{
 var game=Game(out _,out var events);var actor=game.ActiveUserPlayer;actor.Position2D=new(90,34);actor.FacingDirection=Vector2.UnitX;game.Ball.CatchOrTackle(actor);game.TryExecuteTechnique(ShotTechnique.Driven);
 var keeper=game.AwayTeam.Players[0];keeper.Position2D=new(103,34);game.Ball.Position=new(103,34,.3f);game.Ball.Velocity=new(20,0,0);
 var type=typeof(MatchEngine).Assembly.GetType("AccessibleFootball2026.Core.Simulation.SimulationRandom")!;var rng=type.GetField("Override",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!;rng.SetValue(null,(Func<double>)(()=>0));
 Call(game,"CheckRefereeRules",new Vector3(102.6f,34,.3f));rng.SetValue(null,null);
 Check(game.Attempts.Single().Outcome==AttemptOutcome.Saved&&game.Ball.Owner==keeper,"measured held save terminates one live attempt before keeper control");
 Check(game.StatisticsFor(game.HomeTeam).ShotsOnTarget==1&&game.StatisticsFor(game.AwayTeam).Saves==1,"measured save uses original shooter team, one target and one save");
 Call(game,"CheckRefereeRules",game.Ball.Position);Check(game.StatisticsFor(game.AwayTeam).Saves==1,"held keeper ball cannot resolve the same save twice");
}
{
 var game=Game(out _,out var events);game.HomeTeam.Players[8].Position2D=new(100,34);var kicker=game.ActiveUserPlayer;kicker.Position2D=new(70,34);game.Ball.CatchOrTackle(kicker);
 foreach(var p in game.AwayTeam.Players)p.Position2D=new(90,5+p.Id%11);
 Call(game,"SnapshotOffsideAtKick",kicker);var runner=game.HomeTeam.Players[8];Call(game,"ObserveOffsideTouch",runner,true);
 game.TryExecuteTechnique(ShotTechnique.Driven);game.Ball.Position=new(106,34,0);game.Ball.Velocity=new(20,0,0);
 bool prior=MatchSettings.Current.AiVarEnabled;MatchSettings.Current.AiVarEnabled=false;Call(game,"CheckRefereeRules",new Vector3(104,34,0));MatchSettings.Current.AiVarEnabled=prior;
 Check(game.HomeTeam.Score==0&&game.Attempts.Single().Outcome==AttemptOutcome.Annulled,"offside goal path annuls actual attempt before score and restart");
 Check(game.StatisticsFor(game.HomeTeam).Shots==0&&game.StatisticsFor(game.HomeTeam).ShotsOnTarget==0&&!events.Any(e=>e.Type==MatchEventType.MissedChance),"annulled attempt has no shot/target/miss statistical or rating proxy");
}
{
 var game=Game(out _,out var events);var actor=game.ActiveUserPlayer;actor.Position2D=new(100,34);actor.FacingDirection=Vector2.UnitX;game.Ball.CatchOrTackle(actor);game.TryExecuteTechnique(ShotTechnique.Driven);
 game.Ball.Position=new(101,34,.5f);game.Ball.Velocity=new(20,0,0);var defender=game.AwayTeam.Players[8];defender.Position2D=new(102,34);game.Ball.Position=new(103,34,.5f);
 Call(game,"CheckShotBlock",new Vector3(101,34,.5f));Check(game.Attempts.Single().Outcome==AttemptOutcome.Blocked,"on-target projectile first records actual defender contact");
 game.Ball.Position=new(106,34,.2f);game.Ball.Velocity=new(12,0,0);Call(game,"CheckRefereeRules",new Vector3(104.5f,34,.2f));
 Check(game.HomeTeam.Score==1&&game.Attempts.Count==1&&game.Attempts.Single().Outcome==AttemptOutcome.Goal,"untouched deflection crossing confirms one original attempt goal, no fabricated second launch");
 Check(game.StatisticsFor(game.HomeTeam).Shots==1&&game.StatisticsFor(game.HomeTeam).ShotsOnTarget==1&&game.StatisticsFor(game.HomeTeam).BlockedShots==0,"deflected goal final shot/target totals replace provisional block bucket once");
}
// Shared pickup protection is symmetric and finite. Rechecking a controlled ball must not
// refresh it; manual input remains an intervention and is never silenced by AI grace.
foreach(bool home in new[]{false,true})
{
 var game=Game(out _,out _);var receiver=(home?game.HomeTeam:game.AwayTeam).Players[8];receiver.Position2D=new(50,34);
 typeof(MatchEngine).GetField("_possessionGraceTimer",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.SetValue(game,0f);
 game.Ball.Owner=null;game.Ball.State=BallState.Free;game.Ball.Position=new(50,34,0);game.Ball.Velocity=Vector3.Zero;
 Call(game,"CollectLooseBall");Check(game.Ball.Owner==receiver&&Math.Abs(game.PossessionGraceRemaining-1.4f)<.001f,"same finite pickup grace on team "+home);
 var opponent=(home?game.AwayTeam:game.HomeTeam).Players[8];opponent.Position2D=new(50.7f,34);opponent.FacingDirection=-Vector2.UnitX;receiver.FacingDirection=Vector2.UnitX;game.Ball.CatchOrTackle(receiver);
 Call(game,"CollectLooseBall");Check(Math.Abs(game.PossessionGraceRemaining-1.4f)<.001f,"controlled pickup recheck never extends grace team "+home);
 Call(game,"ResolveChallenge",opponent,receiver,false,true);Check(game.Ball.Owner==opponent,"explicit manual challenge not blocked by AI grace team "+home);
 Field(game);game.ActiveUserPlayer.Position2D=new(80,60);game.Ball.CatchOrTackle(game.ActiveUserPlayer);
 for(int i=0;i<110;i++)game.Update(.02f);
 Check(game.PossessionGraceRemaining==0,"pickup protection expires during live play without endless owner refresh team "+home);
}
Console.WriteLine($"RESULT {pass} passed. Kernel and input contracts only, not integrated or native play verification.");
