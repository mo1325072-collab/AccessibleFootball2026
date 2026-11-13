using AccessibleFootball2026.Core.Models;
using System.Reflection;
using System.Security.Cryptography;
using AccessibleFootball2026.Audio;
using AccessibleFootball2026.Audio.Commentary;
var root=Path.GetFullPath(args[0]);int passed=0,failed=0;
void Check(bool ok,string name){Console.WriteLine((ok?"PASS ":"FAIL ")+name);if(ok)passed++;else failed++;}
// Real files, recording transport only. This cannot certify native playback.
var transport=typeof(SoundEffectsPlayer).Assembly.GetType("AccessibleFootball2026.Audio.MciPlayback")!.GetField("TestCommand",BindingFlags.Static|BindingFlags.NonPublic)!;
Func<string,System.Text.StringBuilder?,int> recorder=(command,buffer)=>{buffer?.Append("stopped");return 0;};transport.SetValue(null,recorder);
var music=File.ReadAllBytes(Path.Combine(root,"Menu","MenuMusic.mp3"));
SoundEffectsPlayer.Initialize(root);
// The runtime-selected root wins over legacy locations for every default consumer.
string Field(object o,string n)=>(string)o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(o)!;
var selected=Path.Combine(Path.GetTempPath(),"selected-audio-"+Guid.NewGuid());Directory.CreateDirectory(selected);
SoundEffectsPlayer.Initialize(selected);
var stadium=new AccessibleFootball2026.Audio.SFX.StadiumSoundEngine();
Check(Field(stadium,"_chantsDir")==Path.Combine(selected,"SFX","Chants"),"default stadium chants use the runtime-selected audio root");
Check(Field(stadium,"_acousticsDir")==Path.Combine(selected,"SFX","StadiumAcoustics"),"default stadium acoustics use the runtime-selected audio root");
var anthem=new AccessibleFootball2026.Audio.Anthems.AnthemEngine(new Reader());
Check(Field(anthem,"_assetsBaseDir")==Path.Combine(selected,"Anthems"),"default anthem uses the runtime-selected audio root");
var callnames=new CallnameManager();
Check(Field(callnames,"_callnamesDir")==Path.Combine(selected,"Callnames"),"default callnames use the runtime-selected audio root");
using(var commentary=new CommentaryEngine())Check(Field(commentary,"_baseCommentaryDir")==Path.Combine(selected,"Commentary"),"default commentary uses the runtime-selected audio root");
int generatedBeforeFullPack=SoundEffectsPlayer.SyntheticAssetsGenerated.Count;
SoundEffectsPlayer.Initialize(root);

Check(music.SequenceEqual(File.ReadAllBytes(Path.Combine(root,"Menu","MenuMusic.mp3"))),"initialization preserves exact Gemini menu music");
Check(SoundEffectsPlayer.SyntheticAssetsGenerated.Count==generatedBeforeFullPack,"complete original pack needs no generated match-impact fallback");
foreach(var id in new[]{"BallKick","PowerfulShot","RefereeWhistle","WoodworkHit","CrowdAmbiance","GoalRoar","MissGasp"})
 Check(SoundEffectsPlayer.TryPlayAcademySample(id),"original recording reaches academy playback: "+id);
Check(!SoundEffectsPlayer.TryPlayAcademySample("GoalNet"),"generated repository goal-net fallback is not advertised as an original academy recording");
foreach(var (action,path) in new (Action,string)[]{(()=>SoundEffectsPlayer.PlayPassKick(),"Pass_Center.wav"),(()=>SoundEffectsPlayer.PlayShotKick(),"Shot_Center.wav"),(()=>SoundEffectsPlayer.PlayCrossKick(),"Cross_Center.wav")})
{ action();Check(Path.GetFileName(SoundEffectsPlayer.LastPlayedFilePath)==path,"actual action selects its mapped original: "+path); }
foreach(var name in new[]{"Move","Select"})
{
 var expected=name=="Move"?"4c884c7e":"8f9f9ebd";
 Check(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root,"Menu",name+".wav")))).ToLowerInvariant().StartsWith(expected),"approved "+name+" bytes survive complete original-pack initialization");
}
var catalog=new ContextualCommentaryCatalog(Path.Combine(root,"Commentary","FaresAwad"));
Check(catalog.IsValid&&catalog.Entries.Count==935&&catalog.Entries.Select(c=>c.File).Distinct().Count()==902,"all approved contextual mappings resolve to the 902 active recordings");
// Anthem subject: exact identity, missing-file refusal, playback state and match route.
var anthemCommands=new List<string>();
transport.SetValue(null,(Func<string,System.Text.StringBuilder?,int>)((command,buffer)=>{anthemCommands.Add(command);buffer?.Append("stopped");return 0;}));
var national=new AccessibleFootball2026.Audio.Anthems.AnthemEngine(new Reader(),Path.Combine(root,"Anthems"));
national.PlayNationalAnthemSample("UNKNOWN");
Check(!anthemCommands.Any(c=>c.StartsWith("open ")),"missing national anthem never substitutes another country's recording");
anthemCommands.Clear();
var egypt=AccessibleFootball2026.Core.Models.TeamDatabase.CreateEgypt();
national.PlayMatchNationalAnthems(egypt,new AccessibleFootball2026.Core.Models.Team("club","Club",AccessibleFootball2026.Core.Models.TeamSide.Away),skipChecker:()=>false);
Check(anthemCommands.Any(c=>c.Contains("EGY.wav")),"actual JSON Egypt identity reaches EGY anthem instead of numeric-ID lookup");
Check(national.TryPlayNationalAnthemSample("EGY")&&national.IsAnthemPlaying,"national preview records successful playing state");
national.StopAllSounds();Check(!national.IsAnthemPlaying,"stopping anthem clears actual playback state");

foreach(var (factory,code) in new (Func<AccessibleFootball2026.Core.Models.Team>,string)[]{
 (AccessibleFootball2026.Core.Models.TeamDatabase.CreateEgypt,"EGY"),(AccessibleFootball2026.Core.Models.TeamDatabase.CreateMorocco,"MAR"),
 (AccessibleFootball2026.Core.Models.TeamDatabase.CreateAlgeria,"ALG"),(AccessibleFootball2026.Core.Models.TeamDatabase.CreateTunisia,"TUN"),
 (AccessibleFootball2026.Core.Models.TeamDatabase.CreateSaudiArabia,"KSA"),(AccessibleFootball2026.Core.Models.TeamDatabase.CreateFrance,"FRA"),
 (AccessibleFootball2026.Core.Models.TeamDatabase.CreateEngland,"ENG"),(AccessibleFootball2026.Core.Models.TeamDatabase.CreateSpain,"ESP"),
 (AccessibleFootball2026.Core.Models.TeamDatabase.CreateArgentina,"ARG"),(AccessibleFootball2026.Core.Models.TeamDatabase.CreateBrazil,"BRA")})
{
 anthemCommands.Clear();var actual=factory();
 await national.PlayMatchNationalAnthemsAsync(actual,new AccessibleFootball2026.Core.Models.Team("club","Club",AccessibleFootball2026.Core.Models.TeamSide.Away));
 Check(anthemCommands.Count(c=>c.StartsWith("open ")&&c.Contains(code+".wav"))==1,"actual database team uses exact available national file: "+code);
}
Check(AccessibleFootball2026.Audio.Anthems.AnthemEngine.ResolveNationalAnthemCode(new AccessibleFootball2026.Core.Models.Team("EGY","Egypt",AccessibleFootball2026.Core.Models.TeamSide.Home))==null,"club named Egypt cannot trigger national ceremony");
anthemCommands.Clear();
var stopwatch=System.Diagnostics.Stopwatch.StartNew();bool secondStarted=false;
transport.SetValue(null,(Func<string,System.Text.StringBuilder?,int>)((command,buffer)=>{
 anthemCommands.Add(command);
 if(command.StartsWith("open ")&&command.Contains("ARG.wav"))secondStarted=true;
 if(command=="status anthem mode")buffer?.Append(secondStarted||stopwatch.ElapsedMilliseconds>=4300?"stopped":"playing");return 0;
}));
var ceremony=national.PlayMatchNationalAnthemsAsync(egypt,AccessibleFootball2026.Core.Models.TeamDatabase.CreateArgentina());
Check(!ceremony.IsCompleted,"match ceremony yields while first anthem plays instead of blocking UI thread");
await Task.Delay(4100);
Check(!ceremony.IsCompleted&&!secondStarted,"national anthem remains active beyond old four-second truncation without overlapping away anthem");
await ceremony;
Check(secondStarted&&!national.IsAnthemPlaying,"away anthem follows natural home completion then closes cleanly");
transport.SetValue(null,(Func<string,System.Text.StringBuilder?,int>)((command,buffer)=>{buffer?.Append("playing");return 0;}));
bool skip=false;stopwatch.Restart();ceremony=national.PlayMatchNationalAnthemsAsync(egypt,AccessibleFootball2026.Core.Models.TeamDatabase.CreateArgentina(),()=>skip);
await Task.Delay(50);skip=true;await ceremony;
Check(stopwatch.ElapsedMilliseconds<1000&&national.WasSkipped&&!national.IsAnthemPlaying,"skip cancels ceremony promptly while awaited playback is active");
transport.SetValue(null,(Func<string,System.Text.StringBuilder?,int>)((command,buffer)=>command.StartsWith("open ")?1:0));
Check(!national.TryPlayNationalAnthemSample("EGY")&&national.LastPlayedAnthem==null&&!national.IsAnthemPlaying,"device failure never reports a successfully played anthem");

// Stadium subject: event identity tests do not authenticate the chant's recording label.
anthemCommands.Clear();
transport.SetValue(null,(Func<string,System.Text.StringBuilder?,int>)((command,buffer)=>{anthemCommands.Add(command);buffer?.Append("stopped");return 0;}));
var stadiumAudit=new AccessibleFootball2026.Audio.SFX.StadiumSoundEngine(root);
Check(stadiumAudit.ActiveStadiumKey=="NeutralStadium","new stadium session starts neutral rather than assuming Real Madrid's ground");
var madrid=AccessibleFootball2026.Core.Models.TeamDatabase.CreateRealMadrid();
var barcelona=AccessibleFootball2026.Core.Models.TeamDatabase.CreateBarcelona();
stadiumAudit.ActiveStadiumKey="SantiagoBernabeu";stadiumAudit.SetMatchTeams(madrid,barcelona);
Check(stadiumAudit.ShouldPlayChantForTeam(madrid)&&!stadiumAudit.ShouldPlayChantForTeam(barcelona),"home-team chant gate recognizes real database Madrid only at its selected ground");
anthemCommands.Clear();
stadiumAudit.HandleMatchContext(new AccessibleFootball2026.Core.MatchEvent(AccessibleFootball2026.Core.MatchEventType.FoulCommitted,"foul",subjectPlayer:madrid.Players[0]),0,0,1);
Check(!anthemCommands.Any(c=>c.StartsWith("play chant_ch")),"home foul cannot trigger home crowd protesting an opponent foul");
anthemCommands.Clear();
stadiumAudit.HandleMatchContext(new AccessibleFootball2026.Core.MatchEvent(AccessibleFootball2026.Core.MatchEventType.FoulCommitted,"foul",subjectPlayer:barcelona.Players[0]),0,0,1);
Check(anthemCommands.Any(c=>c.StartsWith("play chant_ch")),"opponent foul still reaches selected home chant route (recording identity separate)");
stadiumAudit.ActiveStadiumKey="CampNou";stadiumAudit.SetMatchTeams(madrid,barcelona);
Check(!stadiumAudit.ShouldPlayChantForTeam(madrid)&&!stadiumAudit.ShouldPlayChantForTeam(barcelona),"away side at its own stadium cannot impersonate the home crowd");

var atletico=new AccessibleFootball2026.Core.Models.Team("ESP_ATM","أتلتيكو مدريد",AccessibleFootball2026.Core.Models.TeamSide.Home);
stadiumAudit.ActiveStadiumKey="SantiagoBernabeu";stadiumAudit.SetMatchTeams(atletico,barcelona);
Check(!stadiumAudit.ShouldPlayChantForTeam(atletico),"another Madrid club cannot impersonate Real Madrid through substring matching");
anthemCommands.Clear();stadiumAudit.SetMatchTeams(madrid,barcelona);
stadiumAudit.HandleMatchContext(new AccessibleFootball2026.Core.MatchEvent(AccessibleFootball2026.Core.MatchEventType.FoulCommitted,"unknown foul"),0,0,1);
Check(!anthemCommands.Any(c=>c.StartsWith("play chant_ch")),"unknown foul actor does not invent an away-team foul");

// Missing selected acoustic must not retain a previous stadium or pick an arbitrary file.
var sparseRoot=Path.Combine(Path.GetTempPath(),"sparse-stadium-"+Guid.NewGuid());
Directory.CreateDirectory(Path.Combine(sparseRoot,"SFX","StadiumAcoustics"));
File.Copy(Path.Combine(root,"SFX","StadiumAcoustics","Acoustic_3.wav"),Path.Combine(sparseRoot,"SFX","StadiumAcoustics","Acoustic_3.wav"));
var sparseStadium=new AccessibleFootball2026.Audio.SFX.StadiumSoundEngine(sparseRoot){ActiveStadiumKey="SantiagoBernabeu"};
sparseStadium.SetMatchTeams(madrid,barcelona);
anthemCommands.Clear();sparseStadium.ActiveStadiumKey="NeutralStadium";sparseStadium.SetMatchTeams(madrid,barcelona);
Check(!anthemCommands.Any(c=>c.StartsWith("open ")&&c.Contains("Acoustic_3.wav")),"missing neutral acoustic never substitutes the available Bernabeu file");
var acousticField=typeof(SoundEffectsPlayer).GetField("_stadiumAcousticPath",BindingFlags.Static|BindingFlags.NonPublic)!;
Check(acousticField.GetValue(null)==null,"missing selected acoustic clears stale previous stadium configuration");

// Event routing follows typed facts, not the language used by event prose.
SoundEffectsPlayer.Initialize(root);
var effectsCoordinator=new SmartAudioCoordinator(new Reader());
void RouteCheck(AccessibleFootball2026.Core.MatchEvent e,string alias,string name)
{
 anthemCommands.Clear();effectsCoordinator.HandleMatchEvent(e);
 Check(anthemCommands.Any(c=>c=="play "+alias+" from 0"),name);
}
RouteCheck(new AccessibleFootball2026.Core.MatchEvent(AccessibleFootball2026.Core.MatchEventType.BallKicked,"Cross into box")
 {Context=AccessibleFootball2026.Core.Commentary.CommentaryContext.Empty.With(("action.kind","cross"))},"cross_kick","cross event uses typed action even with English prose");
RouteCheck(new AccessibleFootball2026.Core.MatchEvent(AccessibleFootball2026.Core.MatchEventType.BallKicked,"عرضية")
 {Context=AccessibleFootball2026.Core.Commentary.CommentaryContext.Empty.With(("action.kind","pass"),("pass.type","short"))},"pass_kick","explicit short pass facts override misleading cross prose");
RouteCheck(new AccessibleFootball2026.Core.MatchEvent(AccessibleFootball2026.Core.MatchEventType.BallKicked,"Keeper distributes")
 {Context=AccessibleFootball2026.Core.Commentary.CommentaryContext.Empty.With(("action.kind","pass"),("pass.height","high"))},"cross_kick","high keeper distribution selects airborne kick route by event facts");
RouteCheck(new AccessibleFootball2026.Core.MatchEvent(AccessibleFootball2026.Core.MatchEventType.UserTackleSuccessful,"Sliding tackle")
 {Context=AccessibleFootball2026.Core.Commentary.CommentaryContext.Empty.With(("tackle.sliding",true))},"slide_tackle","sliding tackle uses typed fact with English prose");
RouteCheck(new AccessibleFootball2026.Core.MatchEvent(AccessibleFootball2026.Core.MatchEventType.UserTackleSuccessful,"انزلاق")
 {Context=AccessibleFootball2026.Core.Commentary.CommentaryContext.Empty.With(("tackle.sliding",false))},"stand_tackle","standing tackle fact overrides misleading sliding prose");
anthemCommands.Clear();effectsCoordinator.HandleMatchEvent(new AccessibleFootball2026.Core.MatchEvent(AccessibleFootball2026.Core.MatchEventType.GoalkeeperSave,"save"));
Check(!anthemCommands.Any(c=>c=="play pass_kick from 0"),"missing goalkeeper-save recording never masquerades as a pass kick");


foreach(var (kind,alias) in new (AccessibleFootball2026.Core.MatchEventType,string)[]{
 (AccessibleFootball2026.Core.MatchEventType.WoodworkHit,"post_hit"),
 (AccessibleFootball2026.Core.MatchEventType.ShotTaken,"shot_kick"),
 (AccessibleFootball2026.Core.MatchEventType.CornerKick,"earcon_ch"),
 (AccessibleFootball2026.Core.MatchEventType.GoalKick,"earcon_ch"),
 (AccessibleFootball2026.Core.MatchEventType.ThrowIn,"earcon_ch"),
 (AccessibleFootball2026.Core.MatchEventType.FoulCommitted,"whistle"),
 (AccessibleFootball2026.Core.MatchEventType.Offside,"whistle"),
 (AccessibleFootball2026.Core.MatchEventType.HalfTime,"whistle"),
 (AccessibleFootball2026.Core.MatchEventType.FullTime,"whistle"),
 (AccessibleFootball2026.Core.MatchEventType.PenaltyAwarded,"whistle"),
 (AccessibleFootball2026.Core.MatchEventType.OpponentTackleSuccessful,"earcon_ch")})
 RouteCheck(new AccessibleFootball2026.Core.MatchEvent(kind,"event"),alias,"match event reaches its production effect path: "+kind);
// Reader ducking follows completion, including overlapping announcements.
var completionReader=new ControlledReader();var speechCoordinator=new SmartAudioCoordinator(completionReader);var ducking=new List<float>();
speechCoordinator.OnCrowdVolumeDucking+=factor=>{lock(ducking)ducking.Add(factor);};
var radar=new AccessibleFootball2026.Core.MatchEvent(AccessibleFootball2026.Core.MatchEventType.TacticalRadarInfo,"long explicit radar");
speechCoordinator.HandleMatchEvent(radar);await Task.Delay(1550);
Check(ducking.Last()==.2f,"crowd remains ducked beyond 1.4 seconds while reader has not completed");
speechCoordinator.HandleMatchEvent(radar);completionReader.Pending[0].SetResult();await Task.Delay(50);
Check(ducking.Last()==.2f,"first completed reader request cannot unduck a newer active request");
completionReader.Pending[1].SetResult();await Task.Delay(50);
Check(ducking.Last()==1f,"last reader completion restores crowd immediately without a fixed timer");

var staleReader=new ControlledReader();var staleCoordinator=new SmartAudioCoordinator(staleReader,()=>true);bool staleCompleted=false;
var staleDucking=new List<float>();staleCoordinator.OnCrowdVolumeDucking+=f=>staleDucking.Add(f);staleCoordinator.OnGoalSequenceCompleted+=()=>staleCompleted=true;
staleCoordinator.HandleMatchEvent(new AccessibleFootball2026.Core.MatchEvent(AccessibleFootball2026.Core.MatchEventType.GoalScored,"goal",goalData:new AccessibleFootball2026.Core.GoalDetails(egypt.Players[0],null,false,1,1,0,egypt)));
await Task.Delay(650);Check(staleReader.Pending.Count==1&&staleCoordinator.HasPendingGoalAnnouncement,"goal reader enters an actual held announcement before cancellation test");
staleCoordinator.CancelPendingGoalSequence();await Task.Delay(80);
Check(staleReader.Pending[0].Task.IsCompleted&&!staleCoordinator.HasPendingGoalAnnouncement&&!staleCompleted,"leaving match cancels already-started old goal speech without firing completion");
Check(staleDucking.Last()==1f,"cancelled old goal releases its ducking claim instead of leaking into next menu");

var deferredReader=new ControlledReader();bool waitingOnCommentary=true;var deferredGoal=new SmartAudioCoordinator(deferredReader,()=>!waitingOnCommentary);int deferredDone=0;deferredGoal.OnGoalSequenceCompleted+=()=>deferredDone++;
var goalEvent=new AccessibleFootball2026.Core.MatchEvent(AccessibleFootball2026.Core.MatchEventType.GoalScored,"goal",goalData:new AccessibleFootball2026.Core.GoalDetails(egypt.Players[0],null,false,1,1,0,egypt));
deferredGoal.HandleMatchEvent(goalEvent);await Task.Delay(650);deferredGoal.CancelPendingGoalSequence();waitingOnCommentary=false;await Task.Delay(100);
Check(deferredReader.Pending.Count==0&&deferredDone==0&&!deferredGoal.HasPendingGoalAnnouncement,"cancelled goal waiting on commentary never speaks later in another menu");
deferredGoal.HandleMatchEvent(goalEvent);await Task.Delay(650);deferredReader.Pending.Last().SetResult();await Task.Delay(60);
Check(deferredDone==1&&!deferredGoal.HasPendingGoalAnnouncement,"fresh goal after cancellation completes once without stale generation interference");

// Positional beacon: geometry, real stereo command path and source-byte preservation.
var beaconBytes=File.ReadAllBytes(Path.Combine(root,"SFX","Beacon","GoalBeacon.wav"));
GoalBeaconPlayer.Initialize(root);
GoalBeaconPlayer.CalculateBeacon(0,34,false,1,out var distance,out var strength,out var beaconPan);
Check(distance==105&&strength==0,"first-half beacon is silent at own goal");
GoalBeaconPlayer.CalculateBeacon(105,34,true,1,out distance,out strength,out beaconPan);
Check(distance==105&&strength==0,"second-half beacon is silent at own goal after direction swap");
GoalBeaconPlayer.CalculateBeacon(100,34,false,1,out distance,out var nearStrength,out beaconPan);
GoalBeaconPlayer.CalculateBeacon(70,34,false,1,out distance,out var farStrength,out beaconPan);
Check(nearStrength>farStrength&&farStrength>0,"beacon grows smoothly nearer the current attacking goal");
GoalBeaconPlayer.CalculateBeacon(100,10,false,1,out distance,out strength,out var firstHalfPan);
GoalBeaconPlayer.CalculateBeacon(5,10,true,1,out distance,out strength,out var secondHalfPan);
Check(firstHalfPan>0&&secondHalfPan<0&&Math.Abs(firstHalfPan+secondHalfPan)<.0001,"beacon left/right perspective flips at halftime");
MatchSettings.Current.MasterVolumePercent=50;MatchSettings.Current.GoalBeaconVolumePercent=40;MatchSettings.Current.ApplyLive();
foreach(var pan in new[]{-1f,0f,1f})
{
 anthemCommands.Clear();Check(GoalBeaconPlayer.PlayBeaconPulse(1,pan),"beacon pulse reaches production playback: pan "+pan);
 int playIndex=anthemCommands.IndexOf("play beacon_ch from 0");
 Check(playIndex>0&&anthemCommands.Take(playIndex).Contains("setaudio beacon_ch left volume to "+(pan>0?0:200))&&anthemCommands.Take(playIndex).Contains("setaudio beacon_ch right volume to "+(pan<0?0:200)),"beacon sets category-scaled stereo before playback: pan "+pan);
}
anthemCommands.Clear();GoalBeaconPlayer.Stop();GoalBeaconPlayer.UpdateBeacon(100,34,false,false);
Check(!anthemCommands.Any(c=>c=="play beacon_ch from 0"),"gameplay beacon cannot pulse when player lacks possession");
GoalBeaconPlayer.Stop();Check(beaconBytes.SequenceEqual(File.ReadAllBytes(Path.Combine(root,"SFX","Beacon","GoalBeacon.wav"))),"beacon initialization preserves supplied source recording bytes");
MatchSettings.Current.MasterVolumePercent=100;MatchSettings.Current.GoalBeaconVolumePercent=100;MatchSettings.Current.ApplyLive();

// User preference: recorded commentator never ducks crowd; reader speech still does.
anthemCommands.Clear();
transport.SetValue(null,(Func<string,System.Text.StringBuilder?,int>)((command,buffer)=>{anthemCommands.Add(command);buffer?.Append("stopped");return 0;}));
SoundEffectsPlayer.SetCrowdDucking(1);SoundEffectsPlayer.StartCrowdAmbiance();SoundEffectsPlayer.SetCrowdVolume(1000,true);
int baseCrowd=int.Parse(anthemCommands.Last(c=>c.StartsWith("setaudio crowd volume to ")).Split(' ').Last());
anthemCommands.Clear();
var voicePath=Path.Combine(root,"Commentary","FaresAwad",catalog.Entries.First().File);
Check(SoundEffectsPlayer.PlayCommentaryAudio(voicePath),"supplied commentary still plays under reader-only mixing preference");
SoundEffectsPlayer.SetCrowdVolume(1000,true);
Check(anthemCommands.Last(c=>c.StartsWith("setaudio crowd volume to "))=="setaudio crowd volume to "+baseCrowd,"recorded commentary leaves crowd at its unchanged normal level");
SoundEffectsPlayer.SetCrowdDucking(.2f);anthemCommands.Clear();SoundEffectsPlayer.StopCommentaryAudio();SoundEffectsPlayer.SetCrowdVolume(1000,true);
Check(anthemCommands.Last(c=>c.StartsWith("setaudio crowd volume to "))=="setaudio crowd volume to "+(int)(baseCrowd*.2f),"commentary stop preserves reader-only crowd reduction");
SoundEffectsPlayer.SetCrowdDucking(1);
SoundEffectsPlayer.StopAll();transport.SetValue(null,null);
Console.WriteLine($"RESULT {passed} passed, {failed} failed. Real files, recorded MCI transport. No native playback or NVDA run.");return failed==0?0:1;

sealed class Reader:IScreenReader{public bool IsAvailable=>true;public void Speak(string s,bool interrupt=true){}public void CancelSpeech(){}}

sealed class ControlledReader:IScreenReader
{
 public bool IsAvailable=>true;
 public List<TaskCompletionSource> Pending=new();
 public void Speak(string text,bool interrupt=true) { Pending.Add(new(TaskCreationOptions.RunContinuationsAsynchronously)); }
 public Task SpeakUntilFinishedAsync(string text,bool interrupt=true) { Speak(text,interrupt);return Pending.Last().Task; }
 public void CancelSpeech() { foreach(var p in Pending)p.TrySetResult(); }
}
