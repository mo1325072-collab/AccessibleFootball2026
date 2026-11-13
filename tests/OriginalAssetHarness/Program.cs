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
// National ceremony canceled; original bytes are retained as user-classified national-team chants.
var anthemCommands=new List<string>();
transport.SetValue(null,(Func<string,System.Text.StringBuilder?,int>)((command,buffer)=>{anthemCommands.Add(command);buffer?.Append("stopped");return 0;}));
var national=new AccessibleFootball2026.Audio.Anthems.AnthemEngine(new Reader(),Path.Combine(root,"Anthems"));
var egypt=AccessibleFootball2026.Core.Models.TeamDatabase.CreateEgypt();
foreach(var code in new[]{"EGY","MAR","ALG","TUN","KSA","FRA","ENG","ESP","ARG","BRA","UNKNOWN"})
 Check(!national.TryPlayNationalAnthemSample(code),"cancelednationalpreviewrefuses "+code);
national.PlayNationalAnthemSample();
var ceremony=national.PlayMatchNationalAnthemsAsync(egypt,AccessibleFootball2026.Core.Models.TeamDatabase.CreateArgentina());
Check(ceremony.IsCompletedSuccessfully,"cancelednationalceremonydoesnotwait");
national.PlayMatchNationalAnthems(egypt,AccessibleFootball2026.Core.Models.TeamDatabase.CreateMorocco());
Check(!anthemCommands.Any(c=>c.StartsWith("open ")),"cancelednationalentrypointscannotopenanyrecording");
Check(AccessibleFootball2026.Audio.Anthems.AnthemEngine.ResolveNationalAnthemCode(egypt)==null,"cancelednationalfeaturehasnoidentityroute");
Check(!SoundAcademyDatabase.GetAllItems().Any(i=>i.Id=="NationalAnthem"),"nationalpreviewremovedfromacademy");
Check(!SoundAcademyDatabase.GetAllItems().Any(i=>i.Id=="ChampionsLeagueAnthem"),"academycontainsnocanceledoutofscopeUCLpreviewwhilegameintroispreserved");
Check(national.PlayChampionsLeagueSample()&&national.IsAnthemPlaying,"approvedUCLsampleplaybackpreserved");
national.StopAllSounds();Check(!national.IsAnthemPlaying,"UCLstopclearsstate");
Check(File.Exists(Path.Combine(root,"Anthems","National","EGY.wav")),"originalnationalnamedchantbytesremainpresent");

// Stadium subject: event identity tests do not authenticate the chant's recording label.
anthemCommands.Clear();
transport.SetValue(null,(Func<string,System.Text.StringBuilder?,int>)((command,buffer)=>{anthemCommands.Add(command);buffer?.Append("stopped");return 0;}));
var stadiumAudit=new AccessibleFootball2026.Audio.SFX.StadiumSoundEngine(root);
Check(stadiumAudit.ActiveStadiumKey==""&&stadiumAudit.ActiveStadiumName==LocalizationService.Text("لم يتم اختيار ملعب","No stadium selected"),"newstadiumsessionisunselectednotinventedneutralground");
var madrid=AccessibleFootball2026.Core.Models.TeamDatabase.CreateRealMadrid();
var barcelona=AccessibleFootball2026.Core.Models.TeamDatabase.CreateBarcelona();
stadiumAudit.ActiveStadiumKey="SantiagoBernabeu";stadiumAudit.SetMatchTeams(madrid,barcelona);
Check(stadiumAudit.ShouldPlayChantForTeam(madrid)&&!stadiumAudit.ShouldPlayChantForTeam(barcelona),"home-team chant gate recognizes real database Madrid only at its selected ground");
anthemCommands.Clear();
stadiumAudit.HandleMatchContext(new AccessibleFootball2026.Core.MatchEvent(AccessibleFootball2026.Core.MatchEventType.FoulCommitted,"foul",subjectPlayer:madrid.Players[0]),0,0,1);
Check(!anthemCommands.Any(c=>c.StartsWith("play chant_ch")),"home foul cannot trigger home crowd protesting an opponent foul");
anthemCommands.Clear();
stadiumAudit.HandleMatchContext(new AccessibleFootball2026.Core.MatchEvent(AccessibleFootball2026.Core.MatchEventType.FoulCommitted,"foul",subjectPlayer:barcelona.Players[0]),0,0,1);
Check(!anthemCommands.Any(c=>c.StartsWith("play chant_ch")),"opponent foul does not play an unverified protest recording or a cheerful team chant");
stadiumAudit.ActiveStadiumKey="CampNou";stadiumAudit.SetMatchTeams(madrid,barcelona);
Check(!stadiumAudit.ShouldPlayChantForTeam(madrid)&&stadiumAudit.ShouldPlayChantForTeam(barcelona),"participating away-slot Barcelona owns CampNou crowd just like home-slot Barcelona");

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
anthemCommands.Clear();sparseStadium.ActiveStadiumKey="";sparseStadium.SetMatchTeams(madrid,barcelona);
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

// New user-supplied index + contemporaneous PES2013 map: identity, not suffix semantics.
var indexedStadium=new AccessibleFootball2026.Audio.SFX.StadiumSoundEngine(root){ActiveStadiumKey="SantiagoBernabeu"};
indexedStadium.SetMatchTeams(madrid,barcelona);anthemCommands.Clear();
transport.SetValue(null,(Func<string,System.Text.StringBuilder?,int>)((command,buffer)=>{anthemCommands.Add(command);buffer?.Append("stopped");return 0;}));
indexedStadium.PlayTeamChant(madrid,AccessibleFootball2026.Audio.SFX.ChantSlot.GeneralRhythm,force:true);
Check(anthemCommands.Any(c=>c.StartsWith("open ")&&Enumerable.Range(550,8).Any(i=>c.Contains("Chant_"+i+".wav"))),"Madrid home chant uses documented550-557 pool, never archive entry173");
indexedStadium.ActiveStadiumKey="CampNou";indexedStadium.SetMatchTeams(barcelona,madrid);anthemCommands.Clear();
indexedStadium.PlayTeamChant(barcelona,AccessibleFootball2026.Audio.SFX.ChantSlot.GeneralRhythm,force:true);
Check(anthemCommands.Any(c=>c.StartsWith("open ")&&Enumerable.Range(541,8).Any(i=>c.Contains("Chant_"+i+".wav"))),"Barcelona home chant uses supplied documented541-548 pool, never archive entry153");

indexedStadium.ActiveStadiumKey="SantiagoBernabeu";indexedStadium.SetMatchTeams(barcelona,madrid);anthemCommands.Clear();
indexedStadium.HandleMatchContext(new AccessibleFootball2026.Core.MatchEvent(AccessibleFootball2026.Core.MatchEventType.BallZoneChanged,"zone"),0,0,1);
Check(anthemCommands.Any(c=>c.StartsWith("open ")&&Enumerable.Range(550,8).Any(i=>c.Contains("Chant_"+i+".wav"))),"automatic context uses away-slot Madrid's crowd at Bernabeu");
indexedStadium.ActiveStadiumKey="CampNou";indexedStadium.SetMatchTeams(madrid,barcelona);anthemCommands.Clear();
indexedStadium.HandleMatchContext(new AccessibleFootball2026.Core.MatchEvent(AccessibleFootball2026.Core.MatchEventType.BallZoneChanged,"zone"),0,0,1);
Check(anthemCommands.Any(c=>c.StartsWith("open ")&&Enumerable.Range(541,8).Any(i=>c.Contains("Chant_"+i+".wav"))),"automatic context uses away-slot Barcelona's crowd at CampNou");
indexedStadium.ActiveStadiumKey="SanSiro";indexedStadium.SetMatchTeams(madrid,barcelona);anthemCommands.Clear();
indexedStadium.PlayTeamChant(madrid,AccessibleFootball2026.Audio.SFX.ChantSlot.GeneralRhythm,force:true);
indexedStadium.PlayTeamChant(barcelona,AccessibleFootball2026.Audio.SFX.ChantSlot.GeneralRhythm,force:true);
Check(!anthemCommands.Any(c=>c.StartsWith("open ")),"Madrid+Barcelona at SanSiro has neutral crowd with neither team's chants");
indexedStadium.ActiveStadiumKey="SantiagoBernabeu";indexedStadium.SetMatchTeams(atletico,barcelona);
Check(!indexedStadium.ShouldPlayChantForTeam(madrid),"a ground owner's chant stays off when that team is absent from match");
indexedStadium.ActiveStadiumKey="CampNou";indexedStadium.SetMatchTeams(madrid,barcelona);
int protestClaims=0;indexedStadium.OnDiscontentWhistlePlayed+=_=>protestClaims++;anthemCommands.Clear();
indexedStadium.PlayTeamChant(barcelona,AccessibleFootball2026.Audio.SFX.ChantSlot.DiscontentWhistle,force:true);
Check(protestClaims==0&&!anthemCommands.Any(c=>c.StartsWith("open ")),"a numbered chant suffix alone never claims or plays recorded protest whistles");
var indexMethod=typeof(AccessibleFootball2026.Audio.SFX.StadiumSoundEngine).GetMethod("EnsureChantLabelIndexLoaded",BindingFlags.Instance|BindingFlags.NonPublic)!;
indexMethod.Invoke(indexedStadium,null);
var loadedLabels=(Dictionary<string,int>?)typeof(AccessibleFootball2026.Audio.SFX.StadiumSoundEngine).GetField("_chantLabelIndex",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(indexedStadium);
Check(loadedLabels?.Count==643&&loadedLabels["SP_R0109_01"]==550&&loadedLabels["A_SP_C0001_03"]==153,"supplied643-record envelope parses original konami_ainf_label without dropping labels");

// Compatibility and fail-closed selection use real bytes copied into isolated test roots.
var indexRoot=Path.Combine(Path.GetTempPath(),"chant-index-"+Guid.NewGuid());
var indexDir=Path.Combine(indexRoot,"SFX","Chants");Directory.CreateDirectory(indexDir);
File.Copy(Directory.GetFiles(Path.Combine(root,"SFX","Chants"),"Chant_550.wav",SearchOption.AllDirectories).Single(),Path.Combine(indexDir,"Chant_550.wav"));
File.Copy(Directory.GetFiles(Path.Combine(root,"SFX","Chants"),"Chant_173.wav",SearchOption.AllDirectories).Single(),Path.Combine(indexDir,"Chant_173.wav"));
File.Copy(Directory.GetFiles(Path.Combine(root,"SFX","Chants"),"Chant_153.wav",SearchOption.AllDirectories).Single(),Path.Combine(indexDir,"Chant_153.wav"));
void TrialIndex(string? json,bool expected,string name)
{
 var indexPath=Path.Combine(indexDir,"chants_index.json");
 if(json==null)File.Delete(indexPath);else File.WriteAllText(indexPath,json);
 var trial=new AccessibleFootball2026.Audio.SFX.StadiumSoundEngine(indexRoot){ActiveStadiumKey="SantiagoBernabeu"};
 trial.SetMatchTeams(madrid,barcelona);anthemCommands.Clear();
 trial.PlayTeamChant(madrid,AccessibleFootball2026.Audio.SFX.ChantSlot.GeneralRhythm,force:true);
 Check(anthemCommands.Any(c=>c.StartsWith("open ")&&c.Contains("Chant_550.wav"))==expected &&
       !anthemCommands.Any(c=>c.StartsWith("open ")&&(c.Contains("Chant_173.wav")||c.Contains("Chant_153.wav"))),name);
}
TrialIndex(null,false,"missing chant index never invents a team from numeric archive173/153");
TrialIndex("[{\"Index\":550,\"Label\":\"SP_R0109_01\"}]",true,"extractor Index/Label array remains compatible");
TrialIndex("[{\"index\":550,\"label\":\"SP_R0108_01\"}]",false,"archive range alone cannot override a conflicting team label");
TrialIndex("[{\"index\":550,\"label\":\"SP_R0109_01\"},{\"index\":173,\"label\":\"SP_R0109_01\"}]",false,"conflicting duplicated label fails closed");
TrialIndex("{not-json",false,"invalid chant JSON fails closed without numeric fallback");
Directory.Delete(indexRoot,true);

indexedStadium.ActiveStadiumKey="SantiagoBernabeu";indexedStadium.SetMatchTeams(madrid,barcelona);
SoundEffectsPlayer.StopAll();anthemCommands.Clear();bool holdChant=false;
transport.SetValue(null,(Func<string,System.Text.StringBuilder?,int>)((command,buffer)=>{anthemCommands.Add(command);buffer?.Append(command=="status chant_ch mode"&&holdChant?"playing":"stopped");return 0;}));
indexedStadium.PlayTeamChant(madrid,AccessibleFootball2026.Audio.SFX.ChantSlot.GeneralRhythm);holdChant=true;anthemCommands.Clear();
indexedStadium.PlayTeamChant(madrid,AccessibleFootball2026.Audio.SFX.ChantSlot.GeneralRhythm,force:true);
Check(!anthemCommands.Any(c=>c.StartsWith("open ")||c=="close chant_ch"),"unfinishedteamchant cannotbe cut evenby force, nonewclipselectedforplayback");
holdChant=false;anthemCommands.Clear();indexedStadium.PlayTeamChant(madrid,AccessibleFootball2026.Audio.SFX.ChantSlot.GeneralRhythm);
Check(anthemCommands.Any(c=>c.StartsWith("open ")&&c.Contains("Chant_")),"naturalchantcompletion permits next randomclip immediately withoutold25secondgap");
anthemCommands.Clear();indexedStadium.PlayTeamChant(madrid,AccessibleFootball2026.Audio.SFX.ChantSlot.GoalCelebration,force:true);
Check(!anthemCommands.Any(c=>c.StartsWith("open ")),"goalcelebration request neverstartsateamchant");
RouteCheck(new AccessibleFootball2026.Core.MatchEvent(AccessibleFootball2026.Core.MatchEventType.MissedChance,"miss"),"crowd_miss","missedchance routescrowdgaspnotteamchant");
transport.SetValue(null,(Func<string,System.Text.StringBuilder?,int>)((command,buffer)=>{anthemCommands.Add(command);if(command=="status chant_ch mode")return 1;buffer?.Append("stopped");return 0;}));
anthemCommands.Clear();indexedStadium.PlayTeamChant(madrid,AccessibleFootball2026.Audio.SFX.ChantSlot.GeneralRhythm);
Check(!anthemCommands.Any(c=>c.StartsWith("open ")),"failedplaybackstatuscannotbe mistakenfornaturalcompletion");
SoundEffectsPlayer.StopAll();anthemCommands.Clear();
transport.SetValue(null,(Func<string,System.Text.StringBuilder?,int>)((command,buffer)=>{anthemCommands.Add(command);buffer?.Append("stopped");return 0;}));
indexedStadium.PlayTeamChant(madrid,AccessibleFootball2026.Audio.SFX.ChantSlot.GeneralRhythm);
Check(anthemCommands.Any(c=>c.StartsWith("open ")&&c.Contains("Chant_")),"StopAllclearsstalechantgatefornextmatch");
var scoringGoal=new AccessibleFootball2026.Core.GoalDetails(madrid.Players[0],null,false,10,1,0,madrid);
var chantGoalEvent=new AccessibleFootball2026.Core.MatchEvent(AccessibleFootball2026.Core.MatchEventType.GoalScored,"goal"){GoalData=scoringGoal};
anthemCommands.Clear();indexedStadium.HandleMatchContext(chantGoalEvent,1,0,10);
Check(!anthemCommands.Any(c=>c.StartsWith("open ")),"actualgoaleventdoesnotstartorreplaceateamchant");
anthemCommands.Clear();effectsCoordinator.HandleMatchEvent(chantGoalEvent);
Check(anthemCommands.Any(c=>c.StartsWith("open ")&&c.Contains("CrowdGoal.wav"))&&anthemCommands.Contains("play crowd_goal from 0"),"actualgoaleventusesexistingCrowdGoalassetandcrowdgoalchannel");
Check(Directory.GetFiles(Path.Combine(root,"SFX","Crowd"),"CrowdMiss.wav").Length==1,"suppliedCrowdMissfileexistsforgasp_routewithoutsyntheticcreation");
var grounds=StadiumCatalog.GetAvailableStadiums();
Check(grounds.Count==4&&grounds.Select(g=>g.Key).Order().SequenceEqual(new[]{"CampNou","OldTrafford","SanSiro","SantiagoBernabeu"}),"fourgroundscoverfivewiredchantteamswithoutpadding");
Check(grounds.All(g=>g.TeamNames.All(n=>TeamChantCatalog.Pools.Any(p=>p.TeamName==n))),"nogroundwithoutcorroboratedteamchantpoolisoffered");
Check(StadiumCatalog.Find("Wembley")==null&&StadiumCatalog.Find("Velodrome")==null,"priorpaddinggroundsremovednotmerelyhiddenbygroup");
Check(StadiumCatalog.GetGroundsForProductionGroup("Premier League (England)").Count==1,"unsupportedteamsdonotcreateotherstadiumchoices");
indexedStadium.ActiveStadiumKey="UnknownGround";indexedStadium.SetMatchTeams(madrid,barcelona);
Check(indexedStadium.ResolveGroundCrowdTeam()==null,"unknownkeyhasnoteamchant");
indexedStadium.ActiveStadiumKey="SantiagoBernabeu";indexedStadium.SetMatchTeams(barcelona,madrid);
Check(indexedStadium.ResolveGroundCrowdTeam()==madrid,"away_slotMadridstillownsitschantbackedground");
Check(SoundAcademyDatabase.GetCategories().Count==0,"academyhasnocategorynavigation");
Check(SoundAcademyDatabase.GetItemsByCategory("earcons").Count==9,"academyhasexactlyninesemanticcues");
Check(SoundAcademyDatabase.GetAllItems().Count==18&&SoundAcademyDatabase.GetAllItems().All(i=>i.CategoryId is "earcons" or "beacon"),"academyonlyninesemanticandninebeacondemonstrations");
Check(new[]{"BeaconCenter","BeaconLeft","BeaconRight","BeaconNear","BeaconFar","BeaconOutOfRange","BeaconNoPossession","BeaconSecondHalfLeft","BeaconSecondHalfRight"}.All(id=>SoundAcademyDatabase.GetAllItems().Any(i=>i.Id==id)),"beaconcoverageincludesdirectiondistancehalfandintentional_silence");
foreach(var (name,key,first,count) in new[]{("Manchester United","OldTrafford",535,6),("Inter Milan","SanSiro",592,6),("AC Milan","SanSiro",604,7)})
{
 var owner=JsonDatabaseProvider.GetTeamByName(name)!;
 Check(owner!=null&&TeamChantCatalog.Find(owner)!=null,"newpoolresolvesactualproductionname: "+name);
 indexedStadium.ActiveStadiumKey=key;indexedStadium.SetMatchTeams(madrid,owner);
 anthemCommands.Clear();indexedStadium.PlayTeamChant(owner,AccessibleFootball2026.Audio.SFX.ChantSlot.GeneralRhythm);
 Check(anthemCommands.Any(c=>c.StartsWith("open ")&&Enumerable.Range(first,count).Any(n=>c.Contains("Chant_"+n+".wav"))),"newownerplaysonlydocumentedrecoveredrange: "+name);
 Check(!indexedStadium.ShouldPlayChantForTeam(madrid),"visitingMadriddoesnotclaimnewownersground: "+name);
 var pool=TeamChantCatalog.Find(owner)!;
 Check(Enumerable.Range(first,count).All(n=>loadedLabels!.Any(l=>l.Value==n&&l.Key.StartsWith(pool.LabelPrefix))&&Directory.GetFiles(Path.Combine(root,"SFX","Chants"),"Chant_"+n+".wav",SearchOption.AllDirectories).Length>0),"everynewpoolentryhasmatchinglabelandactualfile: "+name);
}
var acMilan=JsonDatabaseProvider.GetTeamByName("AC Milan")!;var interMilan=JsonDatabaseProvider.GetTeamByName("Inter Milan")!;
indexedStadium.ActiveStadiumKey="SanSiro";indexedStadium.SetMatchTeams(acMilan,interMilan);
Check(indexedStadium.ResolveGroundCrowdTeam()==acMilan&&indexedStadium.ShouldPlayChantForTeam(acMilan)&&!indexedStadium.ShouldPlayChantForTeam(interMilan),"sharedSanSiroselectshomeMilanpoolonly");
indexedStadium.SetMatchTeams(interMilan,acMilan);
Check(indexedStadium.ResolveGroundCrowdTeam()==interMilan&&indexedStadium.ShouldPlayChantForTeam(interMilan)&&!indexedStadium.ShouldPlayChantForTeam(acMilan),"sharedSanSiroreversesforhomeInterpool");
Check(grounds.Count(g=>g.Key=="SanSiro")==1,"sharedgroundappearsexactlyonce");
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
