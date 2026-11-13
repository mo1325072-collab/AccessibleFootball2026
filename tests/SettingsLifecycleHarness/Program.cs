using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using AccessibleFootball2026.Audio;
using AccessibleFootball2026.Audio.Anthems;
using AccessibleFootball2026.Audio.Commentary;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Input;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;
using AccessibleFootball2026.Core.Tactics;

if(args.Length>0 && args[0]=="--reopen")
{
    MatchSettings.CustomSettingsFilePath=args[1]; MatchSettings.Load();
    var actual=JsonSerializer.Serialize(MatchSettings.Current);
    var childExpected=JsonSerializer.Serialize(JsonSerializer.Deserialize<MatchSettings>(File.ReadAllText(args[1])));
    Console.WriteLine(actual==childExpected ? "Fresh process retained every saved property" : "Fresh process mismatch");
    return actual==childExpected ? 0 : 1;
}
int passed=0,failed=0;
void Check(bool condition,string name){Console.WriteLine((condition?"PASS ":"FAIL ")+name);if(condition)passed++;else failed++;}
var root=Path.Combine(Path.GetTempPath(),"settings-lifecycle-"+Guid.NewGuid());Directory.CreateDirectory(root);
var file=Path.Combine(root,"settings.json");MatchSettings.CustomSettingsFilePath=file;
var commands=new List<string>();MciPlayback.TestCommand=(command,buffer)=>{commands.Add(command);buffer?.Append("stopped");return 0;};
var s=MatchSettings.Current;
void Apply(){s.ApplyLive();commands.Clear();}
void Defaults(){s.ResetToDefaults();s.SettingsVersion=2;Apply();}
void Fixture(string relative){var p=Path.Combine(root,relative);Directory.CreateDirectory(Path.GetDirectoryName(p)!);File.WriteAllBytes(p,new byte[100]);}
bool Played(string alias)=>commands.Any(c=>c.StartsWith("play "+alias,StringComparison.Ordinal));
bool VolumeBeforePlay(string alias,int volume)=>commands.FindIndex(c=>c==$"setaudio {alias} volume to {volume}") is var index && index>=0 && index<commands.FindIndex(c=>c.StartsWith("play "+alias,StringComparison.Ordinal));
object? Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(target,args);
// Persistence covers every stored instance property, not a small hand-picked subset.
s.Difficulty=DifficultyLevel.Legendary;s.MatchDurationMinutes=15;s.ExtraTimeEnabled=false;s.PenaltyShootoutEnabled=false;
s.Commentator=CommentatorId.RaoufKhlif;s.CommentaryVoice=CommentaryVoiceOption.Off;s.CommentaryEnabled=false;
s.MasterVolumePercent=81;s.SfxVolumePercent=72;s.CommentaryVolumePercent=63;s.CrowdVolumePercent=0;s.GoalBeaconVolumePercent=0;s.MusicVolumePercent=0;
s.ScreenReaderSpokenEventsEnabled=false;s.Language=UiLanguage.English;s.AutoSubstitutionsEnabled=true;s.AutoTacticalAdjustmentsEnabled=true;s.AiVarEnabled=false;s.CheckForUpdatesOnStartup=true;s.SettingsVersion=0;
var save=typeof(MatchSettings).GetMethod("Save")!.Invoke(s,new object?[]{null});
Check(save is true || File.Exists(file),"save writes a real persistent file");
var expected=JsonSerializer.Deserialize<MatchSettings>(File.ReadAllText(file))!;
s.ResetToDefaults();MatchSettings.Load();
foreach(var property in typeof(MatchSettings).GetProperties(BindingFlags.Instance|BindingFlags.Public).Where(p=>p.CanWrite))
    Check(Equals(property.GetValue(s),property.GetValue(expected)),"reload preserves "+property.Name);
Check(s.CommentaryVoice==CommentaryVoiceOption.Off&&!s.CommentaryEnabled,"first explicit save is not migrated back to enabled commentary");
var start=new ProcessStartInfo(Environment.ProcessPath!){RedirectStandardOutput=true,RedirectStandardError=true};
if(Path.GetFileNameWithoutExtension(Environment.ProcessPath)=="dotnet")start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
start.ArgumentList.Add("--reopen");start.ArgumentList.Add(file);using(var child=Process.Start(start)!){var text=child.StandardOutput.ReadToEnd();child.WaitForExit();Check(child.ExitCode==0,"fresh application process retains all saved choices: "+text.Trim());}
// A bad destination must never be reported as a successful save.
var impossible=Path.Combine(root,"not-a-directory");File.WriteAllText(impossible,"fixture");
var badSave=typeof(MatchSettings).GetMethod("Save")!.Invoke(s,new object?[]{Path.Combine(impossible,"settings.json")});
Check(badSave is false,"failed save returns failure instead of silent success");
Check((string?)typeof(MatchSettings).GetProperty("LastSaveError")?.GetValue(s) is {Length:>0},"failed save exposes its error for the UI");
// Legacy Off is also a deliberate choice, and explicit load paths must not rewrite the default file.
var legacy=Path.Combine(root,"legacy.json");s.SettingsVersion=0;s.CommentaryEnabled=false;s.CommentaryVoice=CommentaryVoiceOption.Off;File.WriteAllText(legacy,JsonSerializer.Serialize(s));
var previousFile=File.ReadAllText(file);MatchSettings.Load(legacy);
Check(!s.CommentaryEnabled&&s.CommentaryVoice==CommentaryVoiceOption.Off,"legacy disabled commentary is not re-enabled by migration");
Check(File.ReadAllText(file)==previousFile,"explicit legacy load does not write a different settings file");
Defaults();
Fixture("SFX/Crowd/CrowdAmbiance.wav");Fixture("stadium.wav");Fixture("chant.wav");Fixture("Anthems/ChampionsLeague.wav");Fixture("Commentary/FaresAwad/Intro/intro.wav");Fixture("Commentary/RaoufKhlif/Intro/intro.wav");
s.CrowdVolumePercent=17;s.MasterVolumePercent=52; // Non-default loaded values must win over static audio defaults.
SoundEffectsPlayer.Initialize(root);GoalBeaconPlayer.Initialize(root);commands.Clear();
Check(SoundEffectsPlayer.CrowdVolumeSetting==17&&SoundEffectsPlayer.MasterVolumeSetting==52,"audio initialization synchronizes the already-loaded settings");
// Starting a match is the actual stadium-acoustics path, not only StartCrowdAmbiance.
s.CrowdVolumePercent=0;Apply();SoundEffectsPlayer.SetStadiumAcoustics(Path.Combine(root,"stadium.wav"));SoundEffectsPlayer.StartCrowdAmbiance();SoundEffectsPlayer.SetCrowdVolume(1000,true);SoundEffectsPlayer.PlayOneShotChant(Path.Combine(root,"chant.wav"));SoundEffectsPlayer.PlayCrowdGoal();
Check(!commands.Any(c=>c.StartsWith("play ")),"crowd zero prevents stadium, ambience, reaction and chant playback");
s.CrowdVolumePercent=40;s.MasterVolumePercent=50;Apply();SoundEffectsPlayer.SetStadiumAcoustics(Path.Combine(root,"stadium.wav"));
Check(VolumeBeforePlay("crowd",160),"stadium gets master x crowd volume BEFORE first play");
commands.Clear();SoundEffectsPlayer.SetCrowdVolume(1000,true);Check(commands.Contains("setaudio crowd volume to 200"),"dynamic crowd scales saved category and master exactly once");
s.CrowdVolumePercent=20;s.ApplyLive();Check(commands.Last(c=>c.StartsWith("setaudio crowd volume"))=="setaudio crowd volume to 100","live saved crowd change is not squared");
commands.Clear();SoundEffectsPlayer.PlayOneShotChant(Path.Combine(root,"chant.wav"));Check(VolumeBeforePlay("chant_ch",100),"chant follows crowd, not SFX volume");
s.CrowdVolumePercent=0;s.ApplyLive();Check(commands.Contains("stop crowd")&&commands.Contains("stop chant_ch"),"muting crowd stops already-playing ambience and chants");
commands.Clear();SoundEffectsPlayer.SetCrowdVolume(1000,true);SoundEffectsPlayer.ResumeCrowdAmbiance();Check(!commands.Any(c=>c.StartsWith("play ")),"proximity and resume cannot revive muted crowd");
SoundEffectsPlayer.StopCrowdAmbiance();s.CrowdVolumePercent=50;s.ApplyLive();Check(!Played("crowd"),"applying settings outside a match cannot start crowd");
// Each volume channel gets a real playback command test, including the gameplay beacon API.
Defaults();s.MasterVolumePercent=50;s.SfxVolumePercent=40;Apply();SoundEffectsPlayer.PlayWhistle();Check(VolumeBeforePlay("whistle",200),"SFX uses category x master before playback");
s.SfxVolumePercent=0;s.ApplyLive();commands.Clear();SoundEffectsPlayer.PlayWhistle();Check(!Played("whistle"),"zero SFX prevents playback");
Defaults();s.MasterVolumePercent=50;s.GoalBeaconVolumePercent=20;Apply();GoalBeaconPlayer.PlayBeaconPulse(1,0);Check(VolumeBeforePlay("beacon_ch",100),"beacon pulse uses saved master and beacon volume");
s.GoalBeaconVolumePercent=0;s.ApplyLive();commands.Clear();GoalBeaconPlayer.UpdateBeacon(100,34,false,true);Thread.Sleep(50);Check(!Played("beacon_ch"),"actual gameplay beacon call honors saved zero");
Defaults();s.MasterVolumePercent=50;s.MusicVolumePercent=30;Apply();var anthem=new AnthemEngine(new Reader(),Path.Combine(root,"Anthems"));Check(anthem.PlayChampionsLeagueSample()&&VolumeBeforePlay("anthem",150),"startup anthem follows saved music x master volume before play");anthem.StopAllSounds();
s.MusicVolumePercent=0;Apply();Check(!anthem.PlayChampionsLeagueSample()&&!Played("anthem"),"muted music suppresses startup anthem without a volume leak");
Defaults();s.MasterVolumePercent=50;s.MusicVolumePercent=30;Apply();SoundEffectsPlayer.StartMenuMusic();Check(VolumeBeforePlay("bgm_menu",150),"menu song follows saved music x master volume");SoundEffectsPlayer.StopMenuMusic();
s.MusicVolumePercent=0;Apply();SoundEffectsPlayer.StartMenuMusic();Check(!Played("bgm_menu"),"zero music prevents menu song playback");s.MusicVolumePercent=40;s.ApplyLive();Check(Played("bgm_menu"),"unmuting music restores requested menu song without reopening the app");SoundEffectsPlayer.StopMenuMusic();commands.Clear();s.MusicVolumePercent=60;s.ApplyLive();Check(!Played("bgm_menu"),"changing saved music in a match cannot start stopped menu music");
Defaults();s.CommentaryEnabled=false;s.CommentaryVoice=CommentaryVoiceOption.Off;Apply();
using(var commentary=new CommentaryEngine(CommentatorId.FaresAwad,root))
{
 Check(!commentary.TryPlayMatchIntro()&&commentary.IsIdle&&!Played("comm_voice"),"commentary Off rejects real intro and does not block reader with a silent queue");
 s.CommentaryEnabled=true;s.CommentaryVoice=CommentaryVoiceOption.FaresAwad;s.Commentator=CommentatorId.FaresAwad;s.MasterVolumePercent=50;s.CommentaryVolumePercent=40;Apply();
 Check(commentary.TryPlayMatchIntro()&&VolumeBeforePlay("comm_voice",200),"enabled commentary uses saved master and commentary volume");
 s.CommentaryEnabled=false;s.CommentaryVoice=CommentaryVoiceOption.Off;s.ApplyLive();Check(commands.Contains("stop comm_voice")&&!SoundEffectsPlayer.IsCommentaryPlaying(),"switching Off stops current recording and clears busy state");
 s.CommentaryEnabled=true;s.CommentaryVoice=CommentaryVoiceOption.RaoufKhlif;s.Commentator=CommentatorId.RaoufKhlif;s.ApplyLive();Check(commentary.ActiveCommentator.Id==CommentatorId.RaoufKhlif,"saved voice change reaches the existing commentary engine");
}
Defaults();s.MasterVolumePercent=0;Apply();SoundEffectsPlayer.SetStadiumAcoustics(Path.Combine(root,"stadium.wav"));SoundEffectsPlayer.PlayOneShotChant(Path.Combine(root,"chant.wav"));SoundEffectsPlayer.PlayWhistle();SoundEffectsPlayer.PlayCommentaryAudio(Path.Combine(root,"chant.wav"));GoalBeaconPlayer.PlayBeaconPulse(1,0);anthem.PlayChampionsLeagueSample();Check(!commands.Any(c=>c.StartsWith("play ")),"master zero suppresses all six sound paths");
// A failed native volume command must not fall through to full-volume playback.
SoundEffectsPlayer.StopMenuMusic();SoundEffectsPlayer.StopCrowdAmbiance();Defaults();
MciPlayback.TestCommand=(command,buffer)=>{commands.Add(command);buffer?.Append("stopped");return command.StartsWith("setaudio ")?1:0;};commands.Clear();
SoundEffectsPlayer.StartMenuMusic();SoundEffectsPlayer.SetStadiumAcoustics(Path.Combine(root,"stadium.wav"));SoundEffectsPlayer.PlayWhistle();GoalBeaconPlayer.PlayBeaconPulse(1,0);anthem.PlayChampionsLeagueSample();
Check(!commands.Any(c=>c.StartsWith("play ")),"failed volume commands cannot start music, stadium, SFX, beacon or anthem at default loudness");
SoundEffectsPlayer.StopMenuMusic();SoundEffectsPlayer.StopCrowdAmbiance();MciPlayback.TestCommand=(command,buffer)=>{commands.Add(command);buffer?.Append("stopped");return 0;};
// Round-trip into actual engine consumers rather than only comparing serialized values.
Team Make(string id,TeamSide side){var t=new Team(id,id,side);for(int i=0;i<11;i++)t.Players.Add(new Player(i+(side==TeamSide.Home?0:100),id+i,i+1,i==0?SpecificPosition.GK:SpecificPosition.ST,75,75,75,75,75));t.AssignDefaultRoles();return t;}
MatchEngine Match()=>new(Make("h",TeamSide.Home),Make("a",TeamSide.Away),new MatchEventBus(),new Input());
void Persist(){s.Save();s.ResetToDefaults();MatchSettings.Load();}
Defaults();s.MatchDurationMinutes=3;Persist();var fast=Match();fast.Update(.1f);s.MatchDurationMinutes=15;Persist();var slow=Match();slow.Update(.1f);Check(Math.Abs(fast.MatchTimeMinutes/slow.MatchTimeMinutes-5)<.01f,"saved duration changes actual simulation clock rate");
foreach(var extra in new[]{false,true})foreach(var penalties in new[]{false,true})
{
 s.ExtraTimeEnabled=extra;s.PenaltyShootoutEnabled=penalties;Persist();var match=Match();match.SetStoppageMinutes(0,0);match.TriggerHalfTime();match.ResumeMatch();match.SetMatchTimeMinutes(90);match.Update(.1f);
 Check(extra?match.IsExtraTime:penalties?match.State==MatchState.PenaltyShootout:match.State==MatchState.GameOver,$"saved extra={extra}/penalties={penalties} changes tied-match continuation");
}
float AiMove(DifficultyLevel difficulty)
{
 s.Difficulty=difficulty;Persist();var game=Match();var opponent=game.AwayTeam.Players[9];
 foreach(var player in game.AwayTeam.Players)player.Position2D=new(90,60);
 opponent.Position2D=new(60,34);game.HomeTeam.Players[9].Position2D=new(55,34);game.Ball.CatchOrTackle(game.HomeTeam.Players[9]);game.Ball.Position=new(55,34,0);
 typeof(MatchEngine).GetField("_kickoffProtectionTimer",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(game,0f);
 var before=opponent.Position2D;Call(game,"UpdatePlayersAI",.1f);return Vector2.Distance(before,opponent.Position2D);
}
Check(AiMove(DifficultyLevel.Legendary)>AiMove(DifficultyLevel.Easy)*1.5f,"saved difficulty changes actual opponent pressing speed");
Defaults();var auto=Match();auto.HomeTeam.Players[9].CurrentStamina=1;auto.HomeTeam.Bench.Add(new Player(500,"bench",50,SpecificPosition.ST,90,90,90,90,90));auto.TriggerHalfTime();
s.AutoSubstitutionsEnabled=false;Persist();Check(!TacticalAiAdvisor.TryPerformAutoSubstitution(auto,out _),"saved auto-sub Off leaves team unchanged");s.AutoSubstitutionsEnabled=true;Persist();Check(TacticalAiAdvisor.TryPerformAutoSubstitution(auto,out _),"saved auto-sub On reaches actual optional fatigue replacement");
var tactics=Match();tactics.SetMatchTimeMinutes(80);tactics.AwayTeam.Score=2;s.AutoTacticalAdjustmentsEnabled=false;Persist();Check(!TacticalAiAdvisor.TryPerformDynamicTacticalAdjustment(tactics,tactics.HomeTeam,out _),"saved automatic-tactics Off prevents user-team adjustment");s.AutoTacticalAdjustmentsEnabled=true;Persist();Check(TacticalAiAdvisor.TryPerformDynamicTacticalAdjustment(tactics,tactics.HomeTeam,out _),"saved automatic-tactics On changes actual tactics");
foreach(var review in new[]{false,true})
{
 s.AiVarEnabled=review;Persist();var match=Match();var kicker=match.HomeTeam.Players[9];var attacker=match.HomeTeam.Players[10];kicker.Position2D=new(70,34);attacker.Position2D=new(80.1f,34);match.AwayTeam.Players[0].Position2D=new(100,34);foreach(var p in match.AwayTeam.Players.Skip(1))p.Position2D=new(80,34);match.Ball.Position=new(70,34,0);Call(match,"SnapshotOffsideAtKick",kicker);Call(match,"ObserveOffsideTouch",attacker,true);var deadline=(float)match.GetType().GetField("_offsideReviewDeadline",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(match)!;Check(review?deadline>3:deadline==0,"saved native close-offside review="+review+" reaches actual review deadline");
}
Defaults();var reader=new Reader();var coordinator=new SmartAudioCoordinator(reader);s.ScreenReaderSpokenEventsEnabled=false;Persist();coordinator.HandleMatchEvent(new MatchEvent(MatchEventType.HalfTime,"interval"));Check(reader.Spoken.Count==0,"saved spoken-events Off suppresses automatic announcements");coordinator.HandleMatchEvent(new MatchEvent(MatchEventType.TacticalRadarInfo,"requested radar"));Check(reader.Spoken.Contains("requested radar"),"explicit radar stays accessible with automatic spoken events Off");
// Model the actual Windows driver contract: waveaudio rejects setaudio (MCIERR_UNSUPPORTED_FUNCTION).
Defaults();SoundEffectsPlayer.StopAll();SoundEffectsPlayer.SetMatchAudioContext(false);
var drivers=new Dictionary<string,string>();
MciPlayback.TestCommand=(command,buffer)=>
{
 commands.Add(command);
 if(command.StartsWith("open "))
 {
  var tokens=command.Split(' ');drivers[tokens[^1]]=tokens[^3];
 }
 if(command.StartsWith("setaudio ")&&drivers.GetValueOrDefault(command.Split(' ')[1])=="waveaudio")return 274;
 buffer?.Append(command.EndsWith(" length")?"35000":"stopped");return 0;
};
foreach(var id in new[]{"TackleWon","TackleMissed","BallLost","Foul","Penalty","Corner","GoalKick","ThrowIn","PlayerSwitch"})
{
 commands.Clear();Check(SoundEffectsPlayer.TryPlayAcademySample(id)&&Played("academy_earcon"),"Windows-compatible academy cue route "+id);
 commands.Clear();SoundEffectsPlayer.PlayEarcon(id);Check(Played("earcon_ch"),"Windows-compatible match cue route "+id);
}
foreach(var effect in new Action[]{()=>SoundEffectsPlayer.PlayWhistle(),()=>SoundEffectsPlayer.PlayShotKick(),()=>SoundEffectsPlayer.PlayPassKick(),()=>SoundEffectsPlayer.PlayCrossKick()})
{
 commands.Clear();effect();Check(commands.Any(c=>c.StartsWith("play ")),"shared Windows-compatible driver plays referee/pass/shot/cross entrypoint");
}
SoundEffectsPlayer.StartMenuMusic();commands.Clear();SoundEffectsPlayer.SetMatchAudioContext(true);SoundEffectsPlayer.StartMenuMusic();s.MusicVolumePercent=55;s.ApplyLive();Check(!Played("bgm_menu")&&commands.Contains("stop bgm_menu"),"match context stops music and rejects restart and live-settings paths");
SoundEffectsPlayer.SetMatchAudioContext(false);commands.Clear();SoundEffectsPlayer.StartMenuMusic();Check(Played("bgm_menu"),"return to menu can resume the approved song");SoundEffectsPlayer.StopAll();
commands.Clear();int starts=0;Parallel.For(0,16,_=>{if(SoundEffectsPlayer.PlayCommentaryAudio(Path.Combine(root,"chant.wav")))Interlocked.Increment(ref starts);});Check(starts==1&&commands.Count(c=>c=="play comm_voice from 0")==1,"concurrent real playback callers cannot overlap or replace a phrase");
var busy=(DateTime)typeof(SoundEffectsPlayer).GetField("_commentaryBusyUntil",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;Check(busy>DateTime.UtcNow.AddSeconds(34),"native35secondclip length is not truncated to30seconds");
commands.Clear();using(var engine=new CommentaryEngine(CommentatorId.FaresAwad,root))
{
 engine.TryCommentOnEvent(new MatchEvent(MatchEventType.GoalScored,"goal"));engine.TryCommentOnEvent(new MatchEvent(MatchEventType.PenaltyAwarded,"penalty"));
 Check(!commands.Contains("stop comm_voice")&&!commands.Contains("close comm_voice"),"real goal and penalty admissions never interrupt a current phrase");
}
SoundEffectsPlayer.StopAll();commands.Clear();Check(anthem.PlayChampionsLeagueSample()&&anthem.HasPlaybackCompleted,"startup completion is observable for the UI transition");anthem.StopAllSounds();
// Pause is resumable; leaving the match invalidates the whole queue and later dispatches.
SoundEffectsPlayer.StopAll();SoundEffectsPlayer.SetMatchAudioContext(true);
using(var lifecycle=new CommentaryEngine(CommentatorId.FaresAwad,root))
{
 SoundEffectsPlayer.PlayCommentaryAudio(Path.Combine(root,"chant.wav"));commands.Clear();lifecycle.SuspendPlayback();
 bool staleCallback=false;lifecycle.RunAfterRecordedCommentary(()=>staleCallback=true);Thread.Sleep(300);
 Check(commands.Contains("pause comm_voice")&&!lifecycle.IsRecordedChannelBusy&&!staleCallback,"pause silences current commentary and holds queue without blocking menu reader");
 commands.Clear();SoundEffectsPlayer.PlayWhistle();SoundEffectsPlayer.PlayMenuMove();Check(!Played("whistle")&&Played("ui_move"),"pause blocks match effects but preserves approved menu feedback");
 lifecycle.ResumePlayback();Check(commands.Contains("resume comm_voice")&&SoundEffectsPlayer.IsCommentaryPlaying(),"resume continues the same phrase instead of replacing or restarting it");
 lifecycle.EndPlaybackScope();commands.Clear();Thread.Sleep(300);Check(!staleCallback&&!Played("comm_voice"),"leaving a match clears stale queued commentary and completion callbacks");
 Check(!lifecycle.TryCommentOnEvent(new MatchEvent(MatchEventType.GoalScored,"late goal")),"late event cannot reopen a closed match commentary scope");
 lifecycle.ResumePlayback();bool freshCallback=false;lifecycle.RunAfterRecordedCommentary(()=>freshCallback=true);Thread.Sleep(350);Check(freshCallback&&!staleCallback,"a new playback scope admits fresh work without resurrecting old callbacks");
}
SoundEffectsPlayer.StopAll();SoundEffectsPlayer.SetMatchAudioContext(false);
// Reproduce launch-event starvation with two launch clips and fresh outcome/current-play recordings.
var varietyRoot=Path.Combine(root,"variety");var varietyDir=Path.Combine(varietyRoot,"Commentary","FaresAwad");Directory.CreateDirectory(varietyDir);
foreach(var name in new[]{"launch-a.wav","launch-b.wav","outcome.wav","current-play.wav","half.wav"})File.WriteAllBytes(Path.Combine(varietyDir,name),new byte[100]);
File.WriteAllText(Path.Combine(varietyDir,"commentary_context.json"),JsonSerializer.Serialize(new {version=1,clips=new[]{
 new{id="a",file="launch-a.wav",@event="BallKicked",transcript="sends ball",review="verified",all=Array.Empty<object>(),priority=0},
 new{id="b",file="launch-b.wav",@event="BallKicked",transcript="high pass",review="verified",all=Array.Empty<object>(),priority=0},
 new{id="o",file="outcome.wav",@event="PassCompleted",transcript="completed pass",review="verified",all=Array.Empty<object>(),priority=0},
 new{id="p",file="current-play.wav",@event="PossessionPlay",transcript="actual current play",review="verified",all=Array.Empty<object>(),priority=0},
 new{id="h",file="half.wav",@event="HalfTime",transcript="half time",review="verified",all=Array.Empty<object>(),priority=0}
}}));
SoundEffectsPlayer.StopAll();using(var variety=new CommentaryEngine(CommentatorId.FaresAwad,varietyRoot))
{
 SoundEffectsPlayer.PlayCommentaryAudio(Path.Combine(root,"chant.wav"));
 for(int i=0;i<30;i++)variety.TryCommentOnEvent(new MatchEvent(MatchEventType.BallKicked,"launch"));
 variety.TryCommentOnEvent(new MatchEvent(MatchEventType.PassCompleted,"completed"));variety.TryCommentOnEvent(new MatchEvent(MatchEventType.PossessionPlay,"current"));
 SoundEffectsPlayer.StopCommentaryAudio();Call(variety,"DispatchPending",new object?[]{null}!);Thread.Sleep(100);
 Check(Path.GetFileName(SoundEffectsPlayer.LastPlayedFilePath)=="outcome.wav","repeated launch events cannot starve the actual pass outcome");
 SoundEffectsPlayer.StopCommentaryAudio();typeof(CommentaryEngine).GetField("_lastCommentaryTime",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(variety,DateTime.MinValue);Call(variety,"DispatchPending",new object?[]{null}!);Thread.Sleep(100);
 Check(Path.GetFileName(SoundEffectsPlayer.LastPlayedFilePath)=="current-play.wav","fresh possession commentary survives a busy channel instead of being dropped");
 var heard=new HashSet<string>();for(int i=0;i<2;i++){SoundEffectsPlayer.StopCommentaryAudio();variety.TryCommentOnEvent(new MatchEvent(MatchEventType.BallKicked,"launch"),force:true);heard.Add(Path.GetFileName(SoundEffectsPlayer.LastPlayedFilePath)!);}
 SoundEffectsPlayer.StopCommentaryAudio();Check(heard.SetEquals(new[]{"launch-a.wav","launch-b.wav"})&&!variety.TryCommentOnEvent(new MatchEvent(MatchEventType.BallKicked,"launch"),force:true),"both eligible launch performances remain available, then silence replaces a two-phrase loop");
 Check(variety.TryCommentOnEvent(new MatchEvent(MatchEventType.HalfTime,"half"))&&Path.GetFileName(SoundEffectsPlayer.LastPlayedFilePath)=="half.wav","critical halftime still starts after repeated routine events");
}
SoundEffectsPlayer.StopAll();
// Shootout completion must use the existing truthful FullTime recording, even while busy.
var finalDir=Path.Combine(root,"final","Commentary","FaresAwad");Directory.CreateDirectory(finalDir);
File.WriteAllBytes(Path.Combine(finalDir,"end.wav"),new byte[100]);
File.WriteAllText(Path.Combine(finalDir,"commentary_context.json"),JsonSerializer.Serialize(new {version=1,clips=new[]{
 new{id="end",file="end.wav",@event="FullTime",transcript="full time",review="verified",all=new[]{new{fact="match.finished",op="eq",value="true"}},priority=0}
}}));
using(var ending=new CommentaryEngine(CommentatorId.FaresAwad,Path.Combine(root,"final")))
{
 SoundEffectsPlayer.PlayCommentaryAudio(Path.Combine(root,"chant.wav"));
 bool accepted=ending.TryCommentOnEvent(new MatchEvent(MatchEventType.ShootoutFinished,"end"){Context=AccessibleFootball2026.Core.Commentary.CommentaryContext.Empty.With(("match.finished",true))});
 Check(accepted,"shootout completion cannot be throttled away during a previous outcome recording");
 SoundEffectsPlayer.StopCommentaryAudio();Call(ending,"DispatchPending",new object?[]{null}!);Thread.Sleep(100);
 Check(Path.GetFileName(SoundEffectsPlayer.LastPlayedFilePath)=="end.wav","shootout completion plays the existing conditioned FullTime recording");
}
SoundEffectsPlayer.StopAll();
var updateRelease = new { draft=false,prerelease=false,tag_name="game-v2026.11.15",html_url="https://github.com/mo1325072-collab/AccessibleFootball2026/releases/tag/game-v2026.11.15",assets=new[]{new{name="AccessibleFootball2026-Full-Setup.exe",size=1234,state="uploaded",browser_download_url="https://github.com/mo1325072-collab/AccessibleFootball2026/releases/download/game-v2026.11.15/AccessibleFootball2026-Full-Setup.exe"}}};
var updateJson=JsonSerializer.Serialize(new[]{updateRelease});
Check(AccessibleFootball2026.App.GameUpdateChecker.Parse(updateJson).State==AccessibleFootball2026.App.GameUpdateState.Available,"update checker accepts a stable newer game release with uploaded installer");
foreach(var invalid in new[]{updateJson.Replace("game-v","build-inputs-"),updateJson.Replace("Full-Setup.exe","ExtraAudio.bin"),updateJson.Replace("\"prerelease\":false","\"prerelease\":true"),updateJson.Replace("\"draft\":false","\"draft\":true"),"[]"})Check(AccessibleFootball2026.App.GameUpdateChecker.Parse(invalid).State==AccessibleFootball2026.App.GameUpdateState.NoPublishedGame,"build inputs, pre-releases, drafts and missing installers are not game updates");
Check(AccessibleFootball2026.App.GameUpdateChecker.Parse(updateJson.Replace("2026.11.15","2026.11.14")).State==AccessibleFootball2026.App.GameUpdateState.Current,"only an actual published matching game release confirms current");
Check(AccessibleFootball2026.App.GameUpdateChecker.Parse("{}").State==AccessibleFootball2026.App.GameUpdateState.Failed,"unexpected API response is failure, not up-to-date");
MciPlayback.TestCommand=null;Console.WriteLine($"RESULT {passed} passed, {failed} failed. Native MCI/Windows sound was not executed.");return failed==0?0:1;
sealed class Reader:IScreenReader{public List<string> Spoken=new();public bool IsAvailable=>true;public void Speak(string text,bool interrupt=true)=>Spoken.Add(text);public void CancelSpeech(){}public Task SpeakUntilFinishedAsync(string text,bool interrupt=true){Speak(text,interrupt);return Task.CompletedTask;}}
sealed class Input:IInputHandler{public HashSet<GameAction> PollCurrentActions(bool hasPossession)=>new();public bool IsActionTriggered(GameAction action,bool possession)=>false;}
