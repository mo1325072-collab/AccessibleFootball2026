using System.Numerics;
using System.Reflection;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Commentary;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Input;
using AccessibleFootball2026.Core.Simulation;
using AccessibleFootball2026.Audio;

int passed=0,failed=0;
void Check(bool ok,string label){ Console.WriteLine((ok?"PASS ":"FAIL ")+label);if(ok)passed++;else failed++;}
Team Team(string id,TeamSide side){var t=new Team(id,id,side);for(int i=0;i<11;i++)t.Players.Add(new Player(i+(side==TeamSide.Home?100:200),"P"+i,i+1,i==0?SpecificPosition.GK:SpecificPosition.ST,75,75,75,75,75));return t;}
(MatchEngine m,List<MatchEvent> ev,Input input) Fresh(){var h=Team("h",TeamSide.Home);var a=Team("a",TeamSide.Away);var b=new MatchEventBus();var ev=new List<MatchEvent>();b.OnMatchEvent+=ev.Add;var input=new Input();var m=new MatchEngine(h,a,b,input);m.ResumeMatch();return(m,ev,input);}
foreach(var technique in new[]{ShotTechnique.Header,ShotTechnique.Bicycle,ShotTechnique.Volley})
{
 var(m,ev,input)=Fresh();Check(!m.TryExecuteTechnique(technique),technique+" rejects owned ground ball");
 var p=m.ActiveUserPlayer;m.Ball.Owner=null;m.Ball.Position=new Vector3(p.Position2D,technique==ShotTechnique.Volley?.6f:1.6f);
 Check(m.TryExecuteTechnique(technique),technique+" executes nearby aerial contact");
 var shot=ev.Last(e=>e.Type==MatchEventType.ShotTaken);
 Check(shot.Context.Facts["shot.method"]==technique.ToString().ToLowerInvariant(),technique+" event matches physical technique");
 Check(m.Ball.Owner==null && m.Ball.Velocity.Length()>10,technique+" actually kicks the ball");
 var another=Fresh();another.m.Ball.Owner=null;another.m.Ball.Position=new Vector3(0,0,1.6f);Check(!another.m.TryExecuteTechnique(technique),technique+" rejects unreachable ball");
}
foreach(var technique in new[]{ShotTechnique.Driven,ShotTechnique.Lob,ShotTechnique.Curled})
{
 var(m,ev,input)=Fresh();Check(m.TryExecuteTechnique(technique),technique+" available with possession");
 Check(ev.Last().Context.Facts["shot.method"]==(technique==ShotTechnique.Driven?"openPlay":technique.ToString().ToLowerInvariant()),technique+" records correct method");
 if(technique==ShotTechnique.Lob)Check(m.Ball.Velocity.Z>=4,"Lob has real elevated flight");
 if(technique==ShotTechnique.Curled){float y=m.Ball.Velocity.Y;m.Ball.Update(.1f);Check(m.Ball.Velocity.Y!=y,"Curved shot changes actual flight direction");}
}
{
 var(m,ev,input)=Fresh();var p=m.ActiveUserPlayer;p.Position2D=new Vector2(100,34);m.AwardFoulRestart(p);
 Check(m.Restart?.Kind==RestartKind.Penalty,"Attacking-box foul creates real penalty restart");
 Check(Math.Abs(m.Ball.Position.X-94)<.01,"Penalty placed eleven metres from goal");
 input.Actions.Clear();var pos=m.Ball.Position;m.Update(.2f);Check(m.Ball.Position==pos,"Restart waits for user kick instead of live AI challenge");
 Check(m.TakeRestartShot(),"User takes actual penalty");Check(m.Restart==null,"Taken penalty clears restart state");
 Check(ev.Last(e=>e.Type==MatchEventType.ShotTaken).Context.Facts["shot.method"]=="penalty","Penalty method flows into shot context");
}
{
 var(m,ev,input)=Fresh();var p=m.ActiveUserPlayer;p.Position2D=new Vector2(25,34);m.AwardFoulRestart(p);
 Check(m.Restart?.Kind==RestartKind.FreeKick,"Foul outside attacking box is free kick, not penalty");
 Check(m.TakeRestartShot(),"Free kick executes actual shot");Check(ev.Last(e=>e.Type==MatchEventType.ShotTaken).Context.Facts["shot.method"]=="freeKick","Free-kick goal context starts from real restart");
}
{
 var(m,ev,input)=Fresh();var p=m.AwayTeam.Players[9];p.Position2D=new Vector2(5,34);m.AwardFoulRestart(p);
 Check(m.Restart?.Kind==RestartKind.Penalty,"Away-team attacking box mirrors home direction");var taker=m.Restart!.Taker;m.Update(2.6f);
 Check(ev.Any(e=>e.Type==MatchEventType.ShotTaken && e.SubjectPlayer==taker),"CPU takes its actual set piece");
}
{
 var(m,ev,input)=Fresh();m.TriggerHalfTime();Check(ev.Last().Context.Facts["match.half"]=="1","Half-time line snapshots ended first half");m.ResumeMatch();
 Check(ev.Last().Type==MatchEventType.KickOff&&ev.Last().Context.Facts["match.half"]=="2","Resume produces second-half kickoff");
 m.TriggerExtraTime();Check(m.GetCommentarySnapshot().Facts["match.half"]=="3","Extra time starts first extra half");
 typeof(MatchEngine).GetProperty("MatchTimeMinutes")!.SetValue(m,104.99f);m.Update(.1f);
 Check(m.State==MatchState.HalfTime,"105 minutes pauses at extra-time interval");
 Check(ev.Last().Type==MatchEventType.ExtraTimeHalfTime&&ev.Last().Context.Facts["match.half"]=="3","Extra-time interval has ended-half snapshot");
 m.ResumeMatch();Check(ev.Last().Context.Facts["match.half"]=="4","Extra-time resume produces fourth-half kickoff");
}
{
 var(m,ev,input)=Fresh();m.AutomaticGoalRestart=false;m.Ball.Position=new Vector3(105.1f,34,1);
 typeof(MatchEngine).GetMethod("CheckRefereeRules",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(m,null);
 m.Update(10);Check(m.State==MatchState.GoalCelebration,"Audio-owned goal does not restart on fixed timer");
 m.CompleteGoalCelebration();Check(m.State==MatchState.InPlay,"Completed audio sequence resumes play once");int count=ev.Count;m.CompleteGoalCelebration();Check(ev.Count==count,"Stale goal completion cannot award or restart again");
}
{
 var goal=new GoalDetails(new Player(),null,false,1,1,0,Team("h",TeamSide.Home));
 var replay=new GoalReplay(new[]{new ReplayFrame(1,new Vector3(0,34,0),Vector3.Zero,null,1),new ReplayFrame(1,new Vector3(105,34,2),Vector3.Zero,null,2)},CommentaryContext.Empty,goal);
 var path=GoalReplayAudio.Render(replay);Check(path!=null&&new FileInfo(path).Length==88244,"Replay renders real elapsed second as stereo PCM");
 Check(new FileInfo(path!).Length>44,"Replay is not an empty/fake playback flag");
 var noTime=new GoalReplay(new[]{replay.Frames[0],replay.Frames[0]},CommentaryContext.Empty,goal);Check(GoalReplayAudio.Render(noTime)==null,"Invalid replay timestamps fail closed");
}
{
 Check(CommentaryIdentity.PlayerAlias("Thibaut Courtois")=="ThibautCourtois","Exact keeper full name binds correctly");
 Check(CommentaryIdentity.PlayerAlias("Titouan Courtois")==null,"Different Courtois cannot inherit keeper commentary");
 Check(CommentaryIdentity.PlayerAlias("Luka Modrić")=="LukaModric","Exact accented full name binds correctly");
 Check(CommentaryIdentity.PlayerAlias("Ronaldo Vieira")==null,"Ronaldo surname collision fails closed");
 var(m,ev,input)=Fresh();m.PauseMatch();int n=ev.Count;m.ResumeMatch();Check(ev.Count==n,"Pause resume is not a new kickoff");
 m.Mode=MatchMode.CupFinal;m.State=MatchState.PenaltyShootout;m.CompleteShootout(5,4);
 Check(m.TrophyWinner==m.HomeTeam&&ev.Any(e=>e.Type==MatchEventType.TrophyWon),"Actual decided custom cup awards one trophy");
 n=ev.Count;m.CompleteShootout(5,4);Check(ev.Count==n,"Repeated shootout callback cannot award second trophy");
 var friendly=Fresh();friendly.m.State=MatchState.PenaltyShootout;friendly.m.CompleteShootout(5,4);
 Check(friendly.m.TrophyWinner==null,"Friendly shootout never invents a trophy");
}
{
 Check(OffsidePosition.Margin(true,91,70,new float[]{80,90,95})==1,"Offside uses second-last opponent including goalkeeper");
 Check(OffsidePosition.Margin(true,91,92,new float[]{80,90,95})==null,"Player behind ball is onside");
 Check(OffsidePosition.Margin(true,50,40,new float[]{30,40,45})==null,"Own-half position cannot be offside");
 Check(OffsidePosition.Margin(true,90,70,new float[]{80,90,95})==null,"Level with second-last opponent is onside");
 Check(OffsidePosition.Margin(false,15,25,new float[]{10,20,40})==5,"Offside mirrors left-attacking direction");
 var(m,ev,input)=Fresh();var receiver=m.HomeTeam.Players[8];receiver.Position2D=new Vector2(90.1f,34);
 for(int i=0;i<m.AwayTeam.Players.Count;i++)m.AwayTeam.Players[i].Position2D=new Vector2(i==0?100:90,20+i);
 Check(!m.CheckOffsideTrap(m.HomeTeam,m.AwayTeam,receiver,new Vector2(100,34)),"Position alone does not whistle before involvement");
 Check(!ev.Any(e=>e.Type==MatchEventType.Offside),"No speculative offside commentary at planned pass target");
 m.Ball.Owner=null;m.Ball.Position=new Vector3(receiver.Position2D,0);m.TryControlIncomingBall(receiver,testRoll:1);
 m.Ball.Owner=null;m.Ball.Position=new Vector3(105.1f,34,1);
 typeof(MatchEngine).GetMethod("CheckRefereeRules",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(m,null);
 Check(m.HomeTeam.Score==0,"Actual offside involvement prevents score increment");
 Check(ev.Any(e=>e.Type==MatchEventType.GoalDisallowed),"Actual offside goal produces disallowed-goal event");
 Check(m.Restart?.Kind==RestartKind.IndirectFreeKick,"Offside awards indirect, never penalty restart");
 Check(!m.TakeRestartShot(),"Indirect restart cannot be shot directly into goal");
 Check(ev.Single(e=>e.Type==MatchEventType.GoalDisallowed).Context.Facts["offside.goalDisallowed"]=="true","Disallowed commentary has actual decision evidence");
}
{
 var(m,ev,input)=Fresh();var p=m.HomeTeam.Players[8];p.Position2D=new Vector2(70,34);
 for(int i=0;i<m.AwayTeam.Players.Count;i++)m.AwayTeam.Players[i].Position2D=new Vector2(80+i,0);
 m.CheckOffsideTrap(m.HomeTeam,m.AwayTeam,p,new Vector2(99,34));
 m.Ball.Owner=null;m.Ball.Position=new Vector3(p.Position2D,0);m.TryControlIncomingBall(p,testRoll:1);
 m.Ball.Owner=null;m.Ball.Position=new Vector3(105.1f,34,1);
 typeof(MatchEngine).GetMethod("CheckRefereeRules",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(m,null);
 Check(m.HomeTeam.Score==1&&!ev.Any(e=>e.Type==MatchEventType.GoalDisallowed),"Onside at actual kick stays valid despite future target behind line");
}
{
 var(m,ev,input)=Fresh();var p=m.HomeTeam.Players[8];p.Position2D=new Vector2(70,30);m.Ball.Owner=null;m.Ball.Position=new Vector3(p.Position2D,0);m.Ball.Velocity=new Vector3(35,0,0);
 Check(!m.TryControlIncomingBall(p,testRoll:0),"Forced high-speed first-touch error makes loose ball");
 Check(m.Ball.Owner==null&&ev.Any(e=>e.Type==MatchEventType.ControlLost),"Control-lost event follows real failed control");
 Check(!m.TryControlIncomingBall(p,testRoll:1),"Miscontroller cannot immediately auto-catch his error");
}
{
 var(m,ev,input)=Fresh();m.TriggerHalfTime();m.ResumeMatch();m.TriggerExtraTime();m.HomeTeam.Score=1;
 Check(m.GetCommentarySnapshot().Facts["match.finished"]=="false","Leading in extra time is not already full time");
}

{
 var(m,ev,input)=Fresh();var p=m.HomeTeam.Players[8];p.Position2D=new Vector2(91,34);
 for(int i=0;i<11;i++)m.AwayTeam.Players[i].Position2D=new Vector2(i==0?100:90,i);
 m.CheckOffsideTrap(m.HomeTeam,m.AwayTeam,p,p.Position2D);
 var defender=m.AwayTeam.Players[5];
 typeof(MatchEngine).GetMethod("RecordBallTouch",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(m,new object[]{defender,false});
 m.Ball.Owner=null;m.Ball.Position=new Vector3(p.Position2D,0);m.TryControlIncomingBall(p,testRoll:1);
 m.Ball.Owner=null;m.Ball.Position=new Vector3(105.1f,34,1);typeof(MatchEngine).GetMethod("CheckRefereeRules",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(m,null);
 Check(m.HomeTeam.Score==0,"Opponent deflection does not erase offside phase");
}
{
 var(m,ev,input)=Fresh();var p=m.HomeTeam.Players[8];p.Position2D=new Vector2(91,34);
 for(int i=0;i<11;i++)m.AwayTeam.Players[i].Position2D=new Vector2(i==0?100:90,i);
 m.CheckOffsideTrap(m.HomeTeam,m.AwayTeam,p,p.Position2D);
 typeof(MatchEngine).GetMethod("RecordBallTouch",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(m,new object[]{m.AwayTeam.Players[5],true});
 m.Ball.Owner=null;m.Ball.Position=new Vector3(p.Position2D,0);m.TryControlIncomingBall(p,testRoll:1);
 m.Ball.Owner=null;m.Ball.Position=new Vector3(105.1f,34,1);typeof(MatchEngine).GetMethod("CheckRefereeRules",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(m,null);
 Check(m.HomeTeam.Score==1,"Opponent deliberate control clears prior offside phase");
}
{
 var(m,ev,input)=Fresh();var p=m.ActiveUserPlayer;p.Position2D=new Vector2(75,34);m.Ball.CatchOrTackle(p);
 bool played=(bool)typeof(MatchEngine).GetMethod("TryPlayAdvantage",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(m,new object[]{p,false,false})!;
 Check(played&&ev.Last().Type==MatchEventType.Advantage,"Advantage keeps actually possessed attack live");
 m.Ball.CatchOrTackle(m.AwayTeam.Players[5]);
 typeof(MatchEngine).GetMethod("UpdateAdvantage",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(m,null);
 Check(m.Restart!=null&&ev.Any(e=>e.Context.Facts.GetValueOrDefault("referee.advantageRecalled")=="true"),"Lost advantage recalls the actual foul spot");
}
{
 var(m,ev,input)=Fresh();var p=m.ActiveUserPlayer;p.Position2D=new Vector2(75,34);m.Ball.CatchOrTackle(p);
 bool played=(bool)typeof(MatchEngine).GetMethod("TryPlayAdvantage",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(m,new object[]{p,true,false})!;
 Check(!played,"Severe foul is never incorrectly waved on");
}

{
 var(m,ev,input)=Fresh();var p=m.ActiveUserPlayer;var start=p.Position2D;
 Check(m.BeginSlowRunUp(),"Slow run-up begins real approach");
 Check(m.Ball.Owner==null&&Vector2.Distance(p.Position2D,start)>.7f,"Run-up puts player behind stationary loose ball");
 var step=typeof(MatchEngine).GetMethod("UpdateRunUp",BindingFlags.Instance|BindingFlags.NonPublic)!;
 step.Invoke(m,new object[]{.6f});Check(!ev.Any(e=>e.Type==MatchEventType.ShotTaken),"Slow run-up does not immediately shoot");
 step.Invoke(m,new object[]{.61f});Check(ev.Last(e=>e.Type==MatchEventType.ShotTaken).Context.Facts["shot.runUp"]=="slow","Only completed slow approach labels actual shot slow");
}
{
 var(m,ev,input)=Fresh();m.BeginSlowRunUp();m.Ball.CatchOrTackle(m.AwayTeam.Players[5]);
 typeof(MatchEngine).GetMethod("UpdateRunUp",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(m,new object[]{2f});
 Check(!ev.Any(e=>e.Type==MatchEventType.ShotTaken),"Intercepted slow approach cannot fabricate a shot");
}

{
 var(m,ev,input)=Fresh();PlayReplay? replay=null;m.OnAnalysisReplayReady+=r=>replay=r;
 m.Update(.1f);m.Update(.1f);
 var request=typeof(MatchEngine).GetMethod("RequestAnalysisReplay",BindingFlags.Instance|BindingFlags.NonPublic)!;
 request.Invoke(m,new object[]{MatchEventType.OffsideAnalysis,m.GetCommentarySnapshot()});
 Check(replay?.Analysis.Type==MatchEventType.OffsideAnalysis&&replay.Analysis.Context.Facts["replay.available"]=="true","Actual sampled offside replay carries real replay provenance");
 Check(m.AnalysisReplayPending,"Actual replay waits for audio completion");float minute=m.MatchTimeMinutes;m.Update(2);Check(m.MatchTimeMinutes==minute,"Analysis replay never advances live match clock");
 Check(GoalReplayAudio.RenderFrames(replay!.Frames)!=null,"Non-goal replay renders actual sampled ball path");
 m.CompleteAnalysisReplay();Check(!m.AnalysisReplayPending,"Completed replay releases simulation hold");
}

{
 var reader=new CompletionReader();var audio=new SmartAudioCoordinator(reader,()=>true);bool finished=false;audio.OnGoalSequenceCompleted+=()=>finished=true;
 audio.HandleMatchEvent(new MatchEvent(MatchEventType.GoalScored,"goal",goalData:new GoalDetails(new Player(),null,false,1,1,0,Team("h",TeamSide.Home))));
 await System.Threading.Tasks.Task.Delay(650);
 Check(reader.Started&&!finished,"Goal restart waits for actual reader completion, not a two-second guess");
 reader.Done.SetResult();await System.Threading.Tasks.Task.Delay(50);
 Check(finished,"Actual reader completion releases official goal sequence");
}

{
 var queue=new AccessibleFootball2026.Audio.Commentary.CommentaryEngine(baseDir:System.IO.Path.GetTempPath());
 var done=new System.Threading.Tasks.TaskCompletionSource(System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);bool first=false,second=false;
 queue.RunAfterRecordedCommentaryAsync(async()=>{first=true;await done.Task;});
 queue.RunAfterRecordedCommentary(()=>second=true);
 await System.Threading.Tasks.Task.Delay(550);
 Check(first&&!second&&!queue.IsIdle,"Speech-completion callback holds recorded queue until finished");
 done.SetResult();await System.Threading.Tasks.Task.Delay(550);
 Check(second&&queue.IsIdle,"Queue resumes following callback after actual speech completion");
}

{
 var held=ShootoutKick.Resolve(1,1,80,80,true,.5);
 Check(held.Saved&&held.KeeperHeld&&!held.Scored,"Slow central shootout kick really enters goalkeeper holding reach");
 var parry=ShootoutKick.Resolve(1,1,80,80,false,.5);
 Check(parry.Saved&&!parry.KeeperHeld,"Fast shootout save is a parry, not fabricated catch");
 var miss=ShootoutKick.Resolve(0,2,30,80,true,1);
 Check(!miss.Scored&&!miss.Saved&&miss.GoalLinePosition.Y>Pitch.GoalYMax,"Slow inaccurate kick actually misses goal mouth");
 var scored=ShootoutKick.Resolve(0,2,80,80,false,.5);
 Check(scored.Scored&&!scored.Saved,"Correctly placed kick beats opposite dive");
 var facts=ShootoutCommentary.AfterKick(CommentaryContext.Empty,true,held.Scored,held.Saved,0,0,1,0,held.RunUp,held.KeeperHeld);
 Check(facts.Facts["shootout.keeperHeld"]=="true"&&facts.Facts["shootout.runUp"]=="slow","Shootout commentary receives resolved held/run-up facts");
 var unknown=ShootoutCommentary.AfterKick(CommentaryContext.Empty,true,false,true,0,0,1,0);
 Check(!unknown.Facts.ContainsKey("shootout.keeperHeld"),"Legacy save outcome does not invent catch evidence");
}
{
 var(m,ev,input)=Fresh();var outP=m.HomeTeam.Players[2];var inP=new Player();m.HomeTeam.Bench.Add(inP);outP.IsSentOff=true;
 Check(!m.MakeSubstitution(m.HomeTeam,outP,inP)&&!ev.Any(e=>e.Type==MatchEventType.SubstitutionPlanned),"Rejected sent-off substitution never announces planned change");
}
{
 var(m,ev,input)=Fresh();typeof(MatchEngine).GetMethod("AwardCornerKick",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(m,new object[]{false});
 var start=m.Ball.Position;m.Update(1);Check(m.Restart?.Kind==RestartKind.Corner&&m.Ball.Position==start,"CPU corner waits in an actual stopped ready phase");
 m.Update(2);Check(m.Restart==null&&m.Ball.Velocity.Length()>1,"CPU corner is actually kicked only after readiness interval");
 Check(ev.Last(e=>e.Type==MatchEventType.BallKicked).Context.Facts["attack.origin"]=="corner","Taken CPU corner preserves actual corner origin");
}
{
 var(m,ev,input)=Fresh();typeof(MatchEngine).GetMethod("AwardCornerKick",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(m,new object[]{true});
 Check(!m.TakeRestartShot(),"Corner cannot be relabeled as free-kick shot through public restart API");
 input.Actions.Add(GameAction.Cross);m.Update(.1f);Check(m.Restart==null&&ev.Last(e=>e.Type==MatchEventType.BallKicked).Context.Facts["attack.origin"]=="corner","User cross consumes corner restart without losing origin");
}
{
 var(m,ev,input)=Fresh();var p=m.ActiveUserPlayer;p.Position2D=new Vector2(12,34);p.FacingDirection=Vector2.UnitX;m.Ball.CatchOrTackle(p);m.ExecuteClearance();
 Check(!ev.Any(e=>e.Type==MatchEventType.Clearance),"Clearance never claims danger ended at kick time");
 m.Ball.Position=new Vector3(40,34,2);typeof(MatchEngine).GetMethod("UpdateClearanceOutcome",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(m,null);
 Check(ev.Last().Context.Facts["clearance.dangerContinues"]=="false","Clearance safe outcome follows actual ball leaving defensive danger zone");
}
{
 var(m,ev,input)=Fresh();var p=m.ActiveUserPlayer;p.Position2D=new Vector2(12,34);p.FacingDirection=-Vector2.UnitX;m.Ball.CatchOrTackle(p);m.ExecuteClearance();
 m.Ball.CatchOrTackle(m.AwayTeam.Players[3]);m.Ball.Position=new Vector3(10,34,0);typeof(MatchEngine).GetMethod("UpdateClearanceOutcome",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(m,null);
 Check(ev.Last().Context.Facts["clearance.dangerContinues"]=="true","Recaptured failed clearance cannot claim danger ended");
}
foreach(bool onTarget in new[]{true,false})
{
 var(m,ev,input)=Fresh();var shooter=m.ActiveUserPlayer;shooter.Position2D=new Vector2(80,34);m.Ball.CatchOrTackle(shooter);m.TryExecuteTechnique(ShotTechnique.Driven);
 m.Ball.Position=new Vector3(100,onTarget?34:45,1);m.Ball.Velocity=new Vector3(25,0,0);
 typeof(MatchEngine).GetMethod("RememberShotDeflection",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(m,null);
 var defender=m.AwayTeam.Players[4];typeof(MatchEngine).GetMethod("RecordBallTouch",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(m,new object[]{defender,false});
 m.Ball.Position=new Vector3(105.1f,34,1);typeof(MatchEngine).GetMethod("CheckRefereeRules",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(m,null);
 var g=ev.Last(e=>e.Type==MatchEventType.GoalScored).GoalData!;
 Check(onTarget?g.Scorer==shooter&&!g.IsOwnGoal:g.Scorer==defender&&g.IsOwnGoal,onTarget?"On-target shot deflection retains original scorer":"Off-target shot redirected in is an own goal, not attacker goal");
}
{
 var(m,ev,input)=Fresh();m.HomeTeam.Score=1;m.TriggerHalfTime();m.ResumeMatch();typeof(MatchEngine).GetProperty("MatchTimeMinutes")!.SetValue(m,100f);m.Update(.02f);
 Check(m.State==MatchState.GameOver,"True regulation finish is GameOver before final commentary dispatch");
 Check(ev.Last(e=>e.Type==MatchEventType.FullTime).Context.Facts["match.finished"]=="true","Final whistle snapshot is actual final state");
 Check(ev.Where(e=>e.Type==MatchEventType.MatchAnalysis).All(e=>e.Context.Facts["match.finished"]=="true"),"Final match review cannot capture still-live finished state");
}
{
 var(m,ev,input)=Fresh();m.Mode=MatchMode.CupFinal;m.TriggerHalfTime();m.ResumeMatch();typeof(MatchEngine).GetProperty("MatchTimeMinutes")!.SetValue(m,100f);m.Update(.02f);
 Check(m.IsExtraTime&&!ev.Any(e=>e.Type==MatchEventType.FullTime),"Tied cup regulation continues to extra time without a false full-time event");
 Check(m.TrophyWinner==null,"Continued match cannot award trophy before winner exists");
}
{
 var(m,ev,input)=Fresh();m.Update(.1f);Check(m.HomeTeam.Players.Concat(m.AwayTeam.Players).Any(p=>p.Velocity.Length()>1),"Actual AI motion records nonzero measured player velocity for arriving support");
}
{
 using var queue=new AccessibleFootball2026.Audio.Commentary.CommentaryEngine(baseDir:Path.GetTempPath());
 var gate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var done=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var order=new List<int>();
 queue.RunAfterRecordedCommentaryAsync(()=>gate.Task);
 for(int n=0;n<10;n++){int index=n;queue.RunAfterRecordedCommentary(()=>order.Add(index));}
 queue.RunAfterRecordedCommentary(()=>done.SetResult());gate.SetResult();
 await done.Task.WaitAsync(TimeSpan.FromSeconds(8));
 Check(order.SequenceEqual(Enumerable.Range(0,10)),"Queue longer than six never evicts completion callbacks or changes their order");
}
Console.WriteLine($"RESULT {passed} passed, {failed} failed");return failed==0?0:1;
class Input:IInputHandler{ public HashSet<GameAction> Actions=new();public HashSet<GameAction> PollCurrentActions(bool p)=>Actions;public bool IsActionTriggered(GameAction a,bool p)=>Actions.Contains(a);}

class CompletionReader:IScreenReader
{
 public bool IsAvailable=>true;
 public bool Started;
 public System.Threading.Tasks.TaskCompletionSource Done=new(System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);
 public void Speak(string text,bool interrupt=true){}
 public void CancelSpeech(){}
 public System.Threading.Tasks.Task SpeakUntilFinishedAsync(string text,bool interrupt=true){Started=true;return Done.Task;}
}
