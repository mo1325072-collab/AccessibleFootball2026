using System.Text.RegularExpressions;
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
Check(s.CommentaryEnabled&&s.CommentaryVoice==CommentaryVoiceOption.FaresAwad&&s.Commentator==CommentatorId.FaresAwad&&s.SettingsVersion==3,"legacy saved voice migrates to the single Fares commentator");
Check(File.ReadAllText(file)==previousFile,"explicit legacy load does not write a different settings file");
// Ordinary fouls never pause for Enter; only genuine VAR reviews do, and every VAR review ends with Enter after the narration.
Defaults();s.AiVarEnabled=false;Persist();var foulMatch=Match();foulMatch.Update(.1f);
var foulVictim=foulMatch.HomeTeam.Players[5];foulVictim.Position2D=new(50,Pitch.Width/2);
foulMatch.AwardFoulRestart(foulVictim);
Check(foulMatch.Restart!=null&&foulMatch.Restart.Kind==RestartKind.FreeKick,"ordinary foul still places the real free kick restart");
Check(!foulMatch.AwaitingStoppageConfirmation,"ordinary foul never pauses play for Enter");
s.AiVarEnabled=true;Persist();var varMatch=Match();varMatch.Update(.1f);
bool homeRight=varMatch.UserAttackingRight;
var boxVictim=varMatch.HomeTeam.Players[5];boxVictim.Position2D=new(homeRight?Pitch.Length-2:2,Pitch.Width/2);
varMatch.AwardFoulRestart(boxVictim);
Check(varMatch.VideoReviewPending,"box foul with VAR starts a genuine review instead of an instant whistle");
Check(varMatch.Restart==null,"restart waits for the VAR decision instead of being guessed early");
for(int i=0;i<80&&!varMatch.AwaitingStoppageConfirmation;i++)varMatch.Update(.1f);
Check(varMatch.AwaitingStoppageConfirmation,"finished VAR narration holds play until Enter");
Check(varMatch.Restart==null,"VAR decision is not applied before the user confirms with Enter");
Check(varMatch.DismissStoppageReplay(),"Enter after the VAR narration dismisses the review");
Check(varMatch.Restart!=null&&varMatch.Restart.Kind==RestartKind.Penalty,"confirmed VAR decision applies the penalty restart only after Enter");
Defaults();
Fixture("SFX/Crowd/CrowdAmbiance.wav");Fixture("stadium.wav");Fixture("chant.wav");Fixture("Anthems/ChampionsLeague.wav");Fixture("Commentary/FaresAwad/Intro/intro.wav");Fixture("Commentary/RaoufKhlif/Intro/intro.wav");
s.CrowdVolumePercent=17;s.MasterVolumePercent=52; // Non-default loaded values must win over static audio defaults.
SoundEffectsPlayer.Initialize(root);GoalBeaconPlayer.Initialize(root);commands.Clear();
Check(SoundEffectsPlayer.CrowdVolumeSetting==17&&SoundEffectsPlayer.MasterVolumeSetting==52,"audio initialization synchronizes the already-loaded settings");
// Starting a match is the actual stadium-acoustics path, not only StartCrowdAmbiance.
s.CrowdVolumePercent=0;Apply();SoundEffectsPlayer.SetStadiumAcoustics(Path.Combine(root,"stadium.wav"));SoundEffectsPlayer.StartCrowdAmbiance();SoundEffectsPlayer.SetCrowdVolume(1000,true);SoundEffectsPlayer.PlayOneShotChant(Path.Combine(root,"chant.wav"));SoundEffectsPlayer.PlayCrowdGoal();
Check(!commands.Any(c=>c.StartsWith("play ")),"crowd zero prevents stadium, ambience, reaction and chant playback");
s.CrowdVolumePercent=40;s.MasterVolumePercent=50;Apply();SoundEffectsPlayer.SetStadiumAcoustics(Path.Combine(root,"stadium.wav"));
Check(VolumeBeforePlay("crowd",88),"stadium gets master x crowd x steady bed gain BEFORE first play");
commands.Clear();SoundEffectsPlayer.SetCrowdVolume(1000,true);Check(commands.Contains("setaudio crowd volume to 110"),"dynamic crowd scales saved category, master and steady bed gain exactly once");
s.CrowdVolumePercent=20;s.ApplyLive();Check(commands.Last(c=>c.StartsWith("setaudio crowd volume"))=="setaudio crowd volume to 55","live saved crowd change is not squared");
commands.Clear();SoundEffectsPlayer.PlayOneShotChant(Path.Combine(root,"chant.wav"));Check(VolumeBeforePlay("chant_ch",50),"chant follows crowd with its steady gain, not SFX volume");
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
 s.CommentaryEnabled=true;s.CommentaryVoice=CommentaryVoiceOption.RaoufKhlif;s.Commentator=CommentatorId.RaoufKhlif;s.ApplyLive();Check(commentary.ActiveCommentator.Id==CommentatorId.FaresAwad,"removed commentator request keeps the single Fares voice");
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
// A native playback rejection must never masquerade as successful sound telemetry.
SoundEffectsPlayer.StopAll();Defaults();SoundEffectsPlayer.SetMatchAudioContext(false);
foreach(var rejectedStage in new[]{"open","volume","play"})
{
 SoundEffectsPlayer.ResetTelemetry();commands.Clear();
 MciPlayback.TestCommand=(command,buffer)=>
 {
  commands.Add(command);buffer?.Append("stopped");
  return (rejectedStage=="open"&&command.StartsWith("open ")) ||
   (rejectedStage=="volume"&&command.StartsWith("setaudio ")) ||
   (rejectedStage=="play"&&command.StartsWith("play ")) ? 277 : 0;
 };
 SoundEffectsPlayer.PlayWhistle();
 Check(SoundEffectsPlayer.TotalSoundsPlayedCount==0&&SoundEffectsPlayer.LastPlayedFilePath==null,"rejected "+rejectedStage+" is not counted as played");
 Check(SoundEffectsPlayer.LastPlaybackFailure is { ErrorCode:277 } failure&&failure.Stage==rejectedStage&&failure.Alias=="whistle"&&failure.FileName.Length>0,"native "+rejectedStage+" failure preserves exact stage code alias and filename");
 Check(rejectedStage=="play"||!Played("whistle"),"failed "+rejectedStage+" never advances to play");
}
MciPlayback.TestCommand=(command,buffer)=>{commands.Add(command);buffer?.Append("stopped");return 0;};
SoundEffectsPlayer.ResetTelemetry();s.SfxVolumePercent=0;s.ApplyLive();SoundEffectsPlayer.PlayWhistle();
Check(SoundEffectsPlayer.LastPlaybackFailure?.Stage=="muted"&&SoundEffectsPlayer.TotalSoundsPlayedCount==0,"intentional mute is distinguished from native failure and successful playback");
Defaults();SoundEffectsPlayer.PlayWhistle();
Check(SoundEffectsPlayer.TotalSoundsPlayedCount==1&&SoundEffectsPlayer.LastPlaybackFailure==null,"later accepted start clears old failure and counts only accepted playback");
SoundEffectsPlayer.ResetTelemetry();SoundEffectsPlayer.SetMatchAudioContext(true);SoundEffectsPlayer.PauseMatchSounds();SoundEffectsPlayer.PlayWhistle();
Check(SoundEffectsPlayer.LastPlaybackFailure?.Stage=="match-paused"&&SoundEffectsPlayer.TotalSoundsPlayedCount==0,"pause suppression has its own reason rather than claiming sound played");
SoundEffectsPlayer.StopAll();SoundEffectsPlayer.SetMatchAudioContext(false);
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
// The real Space/radar route ducks every crowd alias only during actual reader output.
Fixture("SFX/Crowd/CrowdGoal.wav");Fixture("SFX/Crowd/CrowdCheer.wav");
SoundEffectsPlayer.StopAll();Defaults();s.MasterVolumePercent=50;s.CrowdVolumePercent=40;s.ScreenReaderSpokenEventsEnabled=false;s.ApplyLive();
SoundEffectsPlayer.SetStadiumAcoustics(Path.Combine(root,"stadium.wav"));SoundEffectsPlayer.PlayOneShotChant(Path.Combine(root,"chant.wav"));SoundEffectsPlayer.PlayCrowdGoal();
var heldReader=new ControlledReader();bool recordedBusy=true;
using(var orderedReader=new PriorityScreenReader(heldReader,()=>recordedBusy))
{
 orderedReader.SpeechActivityChanged += speaking=>SoundEffectsPlayer.SetCrowdDucking(speaking?.2f:1f);
 var radarCoordinator=new SmartAudioCoordinator(orderedReader);
 commands.Clear();
 radarCoordinator.HandleMatchEvent(new MatchEvent(MatchEventType.TacticalRadarInfo,"Space radar request"));
 Thread.Sleep(100);
 Check(!heldReader.Started.Task.IsCompleted&&!commands.Contains("setaudio chant_ch volume to 20"),"waiting for the commentator does not duck crowd early");
 recordedBusy=false;SpinWait.SpinUntil(()=>heldReader.Started.Task.IsCompleted,1500);
 Check(commands.Contains("setaudio crowd volume to 17")&&commands.Contains("setaudio chant_ch volume to 20")&&commands.Contains("setaudio crowd_goal volume to 20"),"actual Space reader speech ducks ambience, full chant and crowd reaction together");
 Check(s.CrowdVolumePercent==40&&SoundEffectsPlayer.CrowdVolumeSetting==40,"temporary speech attenuation never changes the saved crowd slider");
 commands.Clear();SoundEffectsPlayer.TryPlayAudibleChant(Path.Combine(root,"chant.wav"));SoundEffectsPlayer.PlayCrowdCheer();
 Check(commands.Contains("setaudio chant_ch volume to 20")&&commands.Contains("setaudio crowd_cheer volume to 20"),"new chants and reactions started during Space speech inherit attenuation");
 s.CrowdVolumePercent=60;s.ApplyLive();
 Check(commands.Contains("setaudio chant_ch volume to 30"),"live user volume edit during speech keeps attenuation without squaring category volume");
 commands.Clear();heldReader.Finish.TrySetResult(true);SpinWait.SpinUntil(()=>commands.Contains("setaudio chant_ch volume to 150"),1500);
 Check(commands.Contains("setaudio crowd volume to 132")&&commands.Contains("setaudio chant_ch volume to 150")&&commands.Contains("setaudio crowd_cheer volume to 150"),"reader completion restores every crowd channel to the latest saved volume");
}
SoundEffectsPlayer.StopAll();SoundEffectsPlayer.SetCrowdDucking(1);Defaults();
// Commentary context reflects actual crowd channels, not the removed musical bed or a stale alias.
SoundEffectsPlayer.StopAll();Defaults();SoundEffectsPlayer.SetStadiumAcoustics(null);
string crowdMode="playing";bool statusFails=false;
MciPlayback.TestCommand=(command,buffer)=>{commands.Add(command);if(command.StartsWith("status "))buffer?.Append(crowdMode);return command.StartsWith("status ")&&statusFails?1:0;};
Check(!SoundEffectsPlayer.IsCrowdActive,"no crowd fact merely because a ground/lineup was selected");
SoundEffectsPlayer.PlayWhistle();Check(!SoundEffectsPlayer.IsCrowdActive,"SFX cannot masquerade as active crowd");
var oneShotMethod=typeof(SoundEffectsPlayer).GetMethod("PlayOneShot",BindingFlags.Static|BindingFlags.NonPublic)!;
oneShotMethod.Invoke(null,new object?[]{Path.Combine(root,"chant.wav"),"lineup_music",null,null,null});
Check(!SoundEffectsPlayer.IsCrowdActive,"intentional lineup music never supplies crowd fact even when its status is playing");
SoundEffectsPlayer.PlayOneShotChant(Path.Combine(root,"chant.wav"));
Check(SoundEffectsPlayer.IsCrowdActive,"playing full chant supports crowd context without continuous musical bed");
crowdMode="stopped";Check(!SoundEffectsPlayer.IsCrowdActive,"retained completed chant alias does not claim audible crowd");
crowdMode="paused";Check(!SoundEffectsPlayer.IsCrowdActive,"paused chant does not claim audible crowd");
crowdMode="playing";statusFails=true;Check(!SoundEffectsPlayer.IsCrowdActive,"failed crowd status query fails closed for commentary facts");
statusFails=false;s.SfxVolumePercent=0;Apply();Check(SoundEffectsPlayer.IsCrowdActive,"crowd fact follows crowd category, not SFX volume");
s.CrowdVolumePercent=0;Apply();Check(!SoundEffectsPlayer.IsCrowdActive,"crowd mute removes crowd fact");
s.CrowdVolumePercent=100;Apply();SoundEffectsPlayer.PlayCrowdGoal();
Check(SoundEffectsPlayer.IsCrowdActive,"playing goal reaction supports crowd fact without ambience or chant");
SoundEffectsPlayer.StopAll();Check(!SoundEffectsPlayer.IsCrowdActive,"menu/end cleanup clears crowd fact");
MciPlayback.TestCommand=(command,buffer)=>{commands.Add(command);buffer?.Append("stopped");return 0;};Defaults();
// In-game updater: release discovery on the dedicated releases repository, smallest verified download.
var relBase="https://github.com/mo1325072-collab/Accessible-Football-Releases";
object RelAsset(string name,long size)=>new{name,size,state="uploaded",browser_download_url=$"{relBase}/releases/download/game-v2026.2/{name}"};
var installer=RelAsset("AccessibleFootball2026-Full-Setup.exe",500000000);var installerRec=RelAsset("AccessibleFootball2026-Full-Setup.json",120);
var patchZip=RelAsset("AccessibleFootball2026-CodePatch.zip",90000000);var patchRec=RelAsset("AccessibleFootball2026-CodePatch.json",200);
object Release(string tag,string published,object[] assets)=>new{draft=false,prerelease=false,tag_name=tag,published_at=published,html_url=$"{relBase}/releases/tag/{tag}",assets};
var fullAssets=new[]{installer,installerRec};var allAssets=new[]{installer,installerRec,patchZip,patchRec};
var updateJson=JsonSerializer.Serialize(new[]{Release("game-v2026.2","2026-11-20T12:00:00Z",allAssets)});
var parsed=AccessibleFootball2026.App.GameUpdateChecker.Parse(updateJson);
Check(parsed.State==AccessibleFootball2026.App.GameUpdateState.Available,"a published stable release with a different tag is an available update");
Check(parsed.Offer!=null&&parsed.Offer.Kind==AccessibleFootball2026.App.GameUpdateDownloadKind.CodePatch&&parsed.Offer.Asset.Name=="AccessibleFootball2026-CodePatch.zip","updater prefers the small code patch when the release ships one");
Check(parsed.Offer!=null&&parsed.Offer.HashRecordAsset.Name=="AccessibleFootball2026-CodePatch.json"&&parsed.Offer.InstallerAsset.Name=="AccessibleFootball2026-Full-Setup.exe","offer carries its hash record and the full installer fallback");
Check(AccessibleFootball2026.App.GameUpdateChecker.Parse(JsonSerializer.Serialize(new[]{Release("game-v2026.2","2026-11-20T12:00:00Z",fullAssets)})).Offer!.Kind==AccessibleFootball2026.App.GameUpdateDownloadKind.FullInstaller,"a release without patch assets offers the full installer");
Check(AccessibleFootball2026.App.GameUpdateChecker.Parse(JsonSerializer.Serialize(new[]{Release("game-v2026.2","2026-11-18T12:00:00Z",fullAssets),Release("game-v2026.1","2026-11-20T12:00:00Z",fullAssets)})).Offer!.Version=="2026.2","the higher version wins even when an older version was published later");
Check(AccessibleFootball2026.App.GameUpdateChecker.Parse(JsonSerializer.Serialize(new[]{Release("game-v2026.1","2026-11-20T12:00:00Z",allAssets)})).State==AccessibleFootball2026.App.GameUpdateState.Current,"installed tag confirms current only against the same release tag");
// Version ordering, downgrade protection and legacy migration.
Check(AccessibleFootball2026.App.GameUpdateChecker.CompareVersions(new Version(2026,11,14),new Version(2026,1))<0,"a legacy date-scheme version orders before the current scheme");
Check(AccessibleFootball2026.App.GameUpdateChecker.CompareVersions(new Version(2026,1),new Version(2026,11,14))>0,"the current scheme outranks every legacy date-scheme version");
Check(AccessibleFootball2026.App.GameUpdateChecker.CompareVersions(new Version(2026,2),new Version(2026,1))>0&&AccessibleFootball2026.App.GameUpdateChecker.CompareVersions(new Version(2026,1),new Version(2026,1))==0,"same-scheme versions compare numerically");
Check(AccessibleFootball2026.App.GameUpdateChecker.Parse(JsonSerializer.Serialize(new[]{Release("game-v2026.0","2026-11-25T12:00:00Z",allAssets)})).State==AccessibleFootball2026.App.GameUpdateState.Current,"an older release published later is never offered as an update");
Check(AccessibleFootball2026.App.GameUpdateChecker.Parse(JsonSerializer.Serialize(new[]{Release("game-v2026.11.20","2026-11-26T12:00:00Z",allAssets)})).State==AccessibleFootball2026.App.GameUpdateState.Current,"a legacy-scheme tag published later cannot pull a current install backwards");
Check(AccessibleFootball2026.App.GameUpdateChecker.Parse(JsonSerializer.Serialize(new[]{Release("game-v2026.1.0","2026-11-26T12:00:00Z",allAssets)})).State==AccessibleFootball2026.App.GameUpdateState.Current,"an aliased three-part tag of the installed version is not a reinstall prompt");
var migration=AccessibleFootball2026.App.GameUpdateChecker.Parse(JsonSerializer.Serialize(new[]{Release("game-v2026.1","2026-11-20T12:00:00Z",allAssets)}),"2026.11.14");
Check(migration.State==AccessibleFootball2026.App.GameUpdateState.Available&&migration.Version=="2026.1","a legacy date-scheme install is migrated to the first current-scheme release");
Check(AccessibleFootball2026.App.GameUpdateChecker.Parse(JsonSerializer.Serialize(new[]{Release("game-v2026.1","2026-11-20T12:00:00Z",allAssets),Release("game-v2026.2","2026-11-21T12:00:00Z",allAssets)}),"2026.11.14").Version=="2026.2","a legacy install is offered the highest current-scheme release");
foreach(var invalid in new[]{updateJson.Replace("game-v","build-inputs-"),updateJson.Replace("Full-Setup.exe","ExtraAudio.bin"),updateJson.Replace("Full-Setup.json","ExtraAudio.json"),updateJson.Replace("\"prerelease\":false","\"prerelease\":true"),updateJson.Replace("\"draft\":false","\"draft\":true"),updateJson.Replace("Accessible-Football-Releases","AccessibleFootball2026"),"[]"})Check(AccessibleFootball2026.App.GameUpdateChecker.Parse(invalid).State==AccessibleFootball2026.App.GameUpdateState.NoPublishedGame,"build inputs, other repos, pre-releases, drafts and missing installer/hash assets are not game updates");
Check(AccessibleFootball2026.App.GameUpdateChecker.Parse("{}").State==AccessibleFootball2026.App.GameUpdateState.Failed,"unexpected API response is failure, not up-to-date");
var goodRecord=AccessibleFootball2026.App.GameUpdateDownloader.ParseHashRecord("{\"artifact\":\"AccessibleFootball2026-CodePatch.zip\",\"bytes\":90000000,\"sha256\":\""+new string('a',64)+"\",\"exe_bytes\":85000000,\"exe_sha256\":\""+new string('b',64)+"\",\"assets_changed\":false,\"base_version\":\"2026.1\"}","AccessibleFootball2026-CodePatch.zip");
Check(goodRecord!=null&&goodRecord.Bytes==90000000&&goodRecord.ExeSha256==new string('b',64)&&!goodRecord.AssetsChanged&&goodRecord.BaseVersion=="2026.1","published patch hash record parses with exe verification and baseline fields");
// A code patch is safe only on its exact baseline; anything else needs the full installer.
Check(AccessibleFootball2026.App.GameUpdateDownloader.IsPatchCompatible(goodRecord!,90000000,"2026.1"),"a patch record matching the installed baseline is compatible");
Check(!AccessibleFootball2026.App.GameUpdateDownloader.IsPatchCompatible(goodRecord!,90000000,"2026.11.14"),"a patch is never applied to an older baseline than it was built from");
Check(!AccessibleFootball2026.App.GameUpdateDownloader.IsPatchCompatible(goodRecord! with{BaseVersion=null},90000000,"2026.1"),"a patch record without an explicit baseline is refused");
Check(!AccessibleFootball2026.App.GameUpdateDownloader.IsPatchCompatible(goodRecord! with{AssetsChanged=true},90000000,"2026.1"),"assets_changed=true always forces the full installer");
Check(!AccessibleFootball2026.App.GameUpdateDownloader.IsPatchCompatible(goodRecord!,90000001,"2026.1"),"a size mismatch between record and published asset forces the full installer");
Check(AccessibleFootball2026.App.GameUpdateDownloader.ParseHashRecord(goodRecord!=null?"{\"artifact\":\"Other.zip\",\"bytes\":1,\"sha256\":\""+new string('a',64)+"\"}":"{}","AccessibleFootball2026-CodePatch.zip")==null,"a hash record for a different artifact is rejected");
Check(AccessibleFootball2026.App.GameUpdateDownloader.ParseHashRecord("{\"artifact\":\"AccessibleFootball2026-CodePatch.zip\",\"bytes\":90000000,\"sha256\":\"not-a-hash\"}","AccessibleFootball2026-CodePatch.zip")==null,"a hash record without a real SHA256 is rejected");
// Staged patch application swaps only the executable and preserves every other file.
var fakeInstall=Path.Combine(root,"install-"+Guid.NewGuid());Directory.CreateDirectory(fakeInstall);
File.WriteAllText(Path.Combine(fakeInstall,"players_database.json"),"database");Directory.CreateDirectory(Path.Combine(fakeInstall,"Assets"));File.WriteAllText(Path.Combine(fakeInstall,"Assets","audio.wav"),"audio");
File.WriteAllText(Path.Combine(fakeInstall,"AccessibleFootball2026.exe"),"old-exe");
var pending=AccessibleFootball2026.App.GameUpdateDownloader.PendingRoot(fakeInstall);Directory.CreateDirectory(pending);File.WriteAllText(Path.Combine(pending,"AccessibleFootball2026.exe"),"new-exe");
Check(AccessibleFootball2026.App.GameUpdateDownloader.HasPendingUpdate(fakeInstall),"staged patch is detected as pending");
AccessibleFootball2026.App.GameUpdateDownloader.ApplyPendingUpdateIfAny(fakeInstall);
Check(File.ReadAllText(Path.Combine(fakeInstall,"AccessibleFootball2026.exe"))=="new-exe","pending patch replaces the executable");
Check(File.ReadAllText(Path.Combine(fakeInstall,"players_database.json"))=="database"&&File.ReadAllText(Path.Combine(fakeInstall,"Assets","audio.wav"))=="audio","patch application preserves database and audio");
Check(!AccessibleFootball2026.App.GameUpdateDownloader.HasPendingUpdate(fakeInstall)&&File.Exists(Path.Combine(fakeInstall,"Updates","AccessibleFootball2026.previous.exe")),"applied patch clears staging and keeps a rollback copy");
// A locked executable (the running game on Windows) can never be swapped: staging survives untouched.
var lockedExe=Path.Combine(fakeInstall,"AccessibleFootball2026.exe");File.WriteAllText(lockedExe,"old-exe");Directory.CreateDirectory(pending);File.WriteAllText(Path.Combine(pending,"AccessibleFootball2026.exe"),"new-exe");
using(var lockStream=new FileStream(lockedExe,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
{
    AccessibleFootball2026.App.GameUpdateDownloader.ApplyPendingUpdateIfAny(fakeInstall);
    Check(AccessibleFootball2026.App.GameUpdateDownloader.HasPendingUpdate(fakeInstall),"a locked executable refuses the swap and the staged update survives for the next launch");
    lockStream.Position=0;Check(new StreamReader(lockStream,leaveOpen:true).ReadToEnd()=="old-exe","the locked executable is left byte-for-byte intact");
}
AccessibleFootball2026.App.GameUpdateDownloader.ApplyPendingUpdateIfAny(fakeInstall);
Check(File.ReadAllText(Path.Combine(fakeInstall,"AccessibleFootball2026.exe"))=="new-exe","once unlocked, the surviving staged update applies cleanly");
// The applier waits for the game process to be fully gone before any swap.
Check(AccessibleFootball2026.App.GameUpdateDownloader.WaitForProcessExit(int.MaxValue,TimeSpan.FromMilliseconds(500)),"a vanished game process counts as exited so the applier proceeds");
{
    var sleeper=OperatingSystem.IsLinux()?Process.Start("/bin/sleep","30")!:Process.Start(new ProcessStartInfo("cmd.exe","/c ping 127.0.0.1 -n 30 > nul"){CreateNoWindow=true})!;
    Check(!AccessibleFootball2026.App.GameUpdateDownloader.WaitForProcessExit(sleeper.Id,TimeSpan.FromMilliseconds(600)),"the applier keeps waiting while the game process is alive instead of swapping over it");
    sleeper.Kill();sleeper.WaitForExit();
    Check(AccessibleFootball2026.App.GameUpdateDownloader.WaitForProcessExit(sleeper.Id,TimeSpan.FromSeconds(5)),"the applier proceeds once the game process has really exited");
}
// Shootout taker choice: the picked five lead in exact order, the rest of the on-pitch players
// follow in squad order, and nobody repeats before every eligible player has taken a kick.
var shootoutTeam=new Team("shootout-home","shootout-home",TeamSide.Home);
for(var i=0;i<11;i++)shootoutTeam.Players.Add(new Player(i,"player"+i,i+1,i==0?SpecificPosition.GK:SpecificPosition.ST,75,75,75,75,75));
var roster=new ShootoutRoster(shootoutTeam);
var firstDefault=roster.Taker(0);
Check(roster.OrderTakers(new List<Player>{roster.Players[7],roster.Players[3],roster.Players[3]})==false&&roster.Taker(0)==firstDefault,"a duplicate pick is refused and the taker order stands");
Check(roster.OrderTakers(new List<Player>{new Player(999,"stranger",99,SpecificPosition.ST,1,1,1,1,1)})==false&&roster.Taker(0)==firstDefault,"a player who is not on the pitch is refused and the taker order stands");
Check(roster.OrderTakers(new List<Player>()).Equals(false),"an empty taker choice is refused");
var chosen5=new List<Player>{roster.Players[7],roster.Players[3],roster.Players[9],roster.Players[1],roster.Players[5]};
Check(roster.OrderTakers(chosen5),"five distinct on-pitch picks are accepted");
Check(roster.Taker(0)==chosen5[0]&&roster.Taker(1)==chosen5[1]&&roster.Taker(4)==chosen5[4],"the picked five shoot in the exact chosen order");
Check(roster.Taker(5)==shootoutTeam.Players[0]&&roster.Taker(10)==shootoutTeam.Players[10],"the remaining on-pitch players follow in squad order");
Check(roster.Taker(11)==chosen5[0],"nobody repeats until every one of the 11 eligible players has taken a kick");
var sentOffTeam=new Team("shootout-away","shootout-away",TeamSide.Away);
for(var i=0;i<11;i++)sentOffTeam.Players.Add(new Player(100+i,"away"+i,i+1,i==0?SpecificPosition.GK:SpecificPosition.ST,75,75,75,75,75));
sentOffTeam.Players[4].IsSentOff=true;sentOffTeam.Players[6].IsOffPitch=true;
var reducedRoster=new ShootoutRoster(sentOffTeam);
Check(reducedRoster.Players.Count==9,"red cards and substitutions shrink the eligible pool instead of inventing players");
var reducedFirst=reducedRoster.Players[4];
Check(reducedRoster.OrderTakers(new List<Player>{reducedFirst,reducedRoster.Players[0],reducedRoster.Players[8],reducedRoster.Players[2],reducedRoster.Players[5]}),"five picks are accepted from the reduced eligible pool");
Check(reducedRoster.Taker(9)==reducedFirst,"with nine eligible players the first taker repeats on the tenth kick only");
// Keeper release regression: a near launch must escape the releasing keeper, for either side.
foreach(bool keeperHome in new[]{false,true})
{
 Defaults();s.AiVarEnabled=false;Persist();var km=Match();km.Update(.02f);
 typeof(MatchEngine).GetField("_kickoffProtectionTimer",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(km,0f);
 typeof(MatchEngine).GetProperty("ActiveUserPlayer")!.SetValue(km,km.HomeTeam.Players[1]);
 foreach(var p in km.HomeTeam.Players.Concat(km.AwayTeam.Players))p.Position2D=new(52,10);
 var kt=keeperHome?km.HomeTeam:km.AwayTeam;var keeper=kt.Players.First(p=>p.Role==PlayerRole.Goalkeeper);
 bool defendingRight=keeperHome?!km.UserAttackingRight:km.UserAttackingRight;
 keeper.Position2D=new(defendingRight?Pitch.Length-4:4,Pitch.Width/2);
 km.Ball.CatchOrTackle(keeper);Call(km,"RecordBallTouch",keeper,true);
 typeof(MatchEngine).GetField("_gkHoldTimer",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(km,keeperHome?2.99f:1.19f);
 int before=km.StatisticsFor(kt).Passes;
 Call(km,"UpdatePlayersAI",.02f);
 Check(km.Ball.Owner==null,$"{(keeperHome?"home":"away")} keeper releases the ball after hold time");
 Check(km.StatisticsFor(kt).Passes==before+1,$"{(keeperHome?"home":"away")} keeper distribution counts one actual pass attempt");
 km.Ball.Update(.02f);Call(km,"UpdatePlayersAI",.02f);
 Check(km.Ball.Owner!=keeper,$"{(keeperHome?"home":"away")} keeper does not re-catch own launch next frame");
}
// Keeper collection stays live for every legitimate ball: back-pass, opponent shot, and any ball touched by another player after his own release.
foreach(bool keeperHome in new[]{false,true})
{
 Defaults();s.AiVarEnabled=false;Persist();var km=Match();km.Update(.02f);
 typeof(MatchEngine).GetField("_kickoffProtectionTimer",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(km,0f);
 typeof(MatchEngine).GetProperty("ActiveUserPlayer")!.SetValue(km,km.HomeTeam.Players[1]);
 var kt=keeperHome?km.HomeTeam:km.AwayTeam;var opp=keeperHome?km.AwayTeam:km.HomeTeam;
 var keeper=kt.Players.First(p=>p.Role==PlayerRole.Goalkeeper);
 bool defendingRight=keeperHome?!km.UserAttackingRight:km.UserAttackingRight;
 float gkx=defendingRight?Pitch.Length-4:4;
 keeper.Position2D=new(gkx,Pitch.Width/2);

 // Back-pass: teammate deliberately passes to keeper; collection must work.
 var tp=kt.Players.First(p=>p.Role!=PlayerRole.Goalkeeper);tp.Position2D=new(gkx+(defendingRight?-8:8),Pitch.Width/2);
 km.Ball.Position=new System.Numerics.Vector3(tp.Position2D.X,tp.Position2D.Y,0);
 km.Ball.CatchOrTackle(tp);Call(km,"RecordBallTouch",tp,true);
 km.Ball.Kick(new Vector2(defendingRight?1:-1,0),6f,0f,tp);Call(km,"RecordBallTouch",tp,true);
 km.Ball.Position=new System.Numerics.Vector3(gkx+(defendingRight?-1f:1f),Pitch.Width/2,0);
 km.Ball.Velocity=new System.Numerics.Vector3(defendingRight?1f:-1f,0,0);
 Call(km,"UpdatePlayersAI",.02f);
 Check(km.Ball.Owner==keeper,$"{(keeperHome?"home":"away")} keeper collects a teammate back-pass");

 typeof(MatchEngine).GetProperty("ActiveUserPlayer")!.SetValue(km,km.HomeTeam.Players[1]);
 // Opponent shot: deliberate opponent touch, then slow ball near keeper; collection must work.
 var os=opp.Players.First(p=>p.Role!=PlayerRole.Goalkeeper);
 km.Ball.CatchOrTackle(os);Call(km,"RecordBallTouch",os,true);
 km.Ball.Kick(new Vector2(defendingRight?1:-1,0),2f,0f,os);Call(km,"RecordBallTouch",os,true);
 km.Ball.Position=new System.Numerics.Vector3(gkx+(defendingRight?-1.2f:1.2f),Pitch.Width/2,0);
 km.Ball.Velocity=new System.Numerics.Vector3(defendingRight?0.5f:-0.5f,0,0);
 Call(km,"UpdatePlayersAI",.02f);
 Check(km.Ball.Owner==keeper,$"{(keeperHome?"home":"away")} keeper collects after a deliberate opponent touch");

 typeof(MatchEngine).GetProperty("ActiveUserPlayer")!.SetValue(km,km.HomeTeam.Players[1]);
 // Penalty-area edge launch still escapes: keeper standing on the box line.
 keeper.Position2D=new(defendingRight?Pitch.Length-Pitch.PenaltyBoxDepth:Pitch.PenaltyBoxDepth,Pitch.Width/2);
 km.Ball.CatchOrTackle(keeper);Call(km,"RecordBallTouch",keeper,true);
 typeof(MatchEngine).GetField("_gkHoldTimer",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(km,keeperHome?2.99f:1.19f);
 Call(km,"UpdatePlayersAI",.02f);
 km.Ball.Update(.02f);Call(km,"UpdatePlayersAI",.02f);
 Check(km.Ball.Owner!=keeper,$"{(keeperHome?"home":"away")} keeper box-line launch is not re-caught");
}

// IFAB 2026/27: only a clearly incorrect second caution with clear-error evidence is reviewable.
Defaults();s.AiVarEnabled=true;Persist();
var secondCardMatch=Match();secondCardMatch.Update(.1f);
var secondCardPlayer=secondCardMatch.AwayTeam.Players[5];secondCardPlayer.YellowCards=1;
Check(!secondCardMatch.ReviewCardDecision(secondCardPlayer,secondCardPlayer,false,false,false)&&!secondCardMatch.VideoReviewPending,"second caution without clear-error evidence never triggers VAR");
Check(secondCardMatch.ReviewCardDecision(secondCardPlayer,secondCardPlayer,false,false,true)&&secondCardMatch.VideoReviewPending,"clearly incorrect second caution remains reviewable under IFAB 2026/27");
var cleanGoalMatch=Match();cleanGoalMatch.Update(.1f);
cleanGoalMatch.Ball.Position=new Vector3(Pitch.Length+.5f,Pitch.Width/2,0);
Call(cleanGoalMatch,"CheckRefereeRules");
Check(!cleanGoalMatch.VideoReviewPending&&cleanGoalMatch.HomeTeam.Score+cleanGoalMatch.AwayTeam.Score==1,"clean goal counts without a routine VAR review hold");
// Product identity: name is exactly "Accessible Football"; 2026.1 is the version, not part of the name.
s.Language=UiLanguage.English;
Check(LocalizationService.GetString("Welcome")=="Welcome to Accessible Football.","English welcome uses the plain product name");
s.Language=UiLanguage.Arabic;
Check(!LocalizationService.GetString("Welcome").Contains("2026"),"Arabic welcome carries no year in the product name");
{
 var dir=AppContext.BaseDirectory;string? buildScript=null;
 for(int up=0;up<12&&dir!=null;up++){var c=Path.Combine(dir,"Build-Complete.py");if(File.Exists(c)){buildScript=c;break;}dir=Path.GetDirectoryName(dir);}
 if(buildScript==null){Console.WriteLine("SKIP installer-script checks outside the repo checkout");}
 else{
  var py=File.ReadAllText(buildScript);
  Check(py.Contains("AppId=AccessibleFootball2026-Full"),"installer AppId stays stable for update identity");
  Check(py.Contains("AppName=Accessible Football\\n")&&!py.Contains("AppName=Accessible Football 2026"),"installer product name is exactly Accessible Football");
  Check(py.Contains("DefaultDirName={localappdata}\\\\AccessibleFootball2026"),"installer keeps the existing install directory");
  Check(py.Contains("Name: \"desktopicon\"")&&py.Contains("{cm:CreateDesktopIcon}")&&py.Contains("{autodesktop}")&&py.Contains("Tasks: desktopicon"),"installer offers an optional Create a desktop icon task");
  Check(!py.Contains("{autodesktop}\\Accessible Football 2026"),"desktop shortcut label carries no year");
  var win=File.ReadAllText(Path.Combine(Path.GetDirectoryName(buildScript)!,"AccessibleFootball2026.App","AccessibleFootballWindow.cs"));
  Check(!win.Contains("Accessible Football 2026")&&!win.Contains("2026/2027"),"window title and big menu label use the plain product name");
 }
}
Defaults();
// Keyboard event contract: held modifiers remain continuous; discrete actions debounce per physical press.
var kb=new AccessibleFootball2026.Input.KeyboardInputHandler();
kb.HandleKeyDown(0x10,false);kb.HandleKeyDown(0x27,false);kb.HandleKeyDown(0x11,false);kb.HandleKeyDown(0x53,false);
var keys=kb.PollCurrentActions(false);
Check(keys.Contains(GameAction.Sprint)&&keys.Contains(GameAction.MoveRight)&&keys.Contains(GameAction.SwitchPlayer)&&keys.Contains(GameAction.StandTackle),"Shift+Right, Ctrl and S coexist in one poll");
kb.HandleKeyDown(0x11,false);kb.HandleKeyDown(0x53,false);
keys=kb.PollCurrentActions(false);
Check(keys.Contains(GameAction.Sprint)&&keys.Contains(GameAction.MoveRight)&&!keys.Contains(GameAction.SwitchPlayer)&&!keys.Contains(GameAction.StandTackle),"physical holds retain movement but never repeat Ctrl/S actions");
for(int tap=0;tap<5;tap++){
 kb.HandleKeyUp(0x53);kb.HandleKeyDown(0x53,false);keys=kb.PollCurrentActions(false);
 Check(keys.Contains(GameAction.StandTackle)&&keys.Contains(GameAction.Sprint)&&keys.Contains(GameAction.MoveRight),$"distinct S tap {tap+1} registers while Shift+Right stay held");
}
kb.HandleKeyUp(0x53);kb.HandleKeyDown(0x44,false);keys=kb.PollCurrentActions(false);
Check(keys.Contains(GameAction.SlideTackle)&&keys.Contains(GameAction.MoveRight)&&keys.Contains(GameAction.Sprint),"D slide tackle coexists with held Shift+Right");
kb.HandleKeyUp(0x11);kb.HandleKeyDown(0x11,false);keys=kb.PollCurrentActions(false);
Check(keys.Contains(GameAction.SwitchPlayer),"fresh Ctrl after release switches once again");
kb.Reset();keys=kb.PollCurrentActions(false);
Check(keys.Count==0,"focus/pause Reset clears pending and held actions without a stale burst");
// Hold-to-charge kick power contract: possession S/W/D charge while held and fire once on release.
double fakeNow=0.0;
var chargeKb=new AccessibleFootball2026.Input.KeyboardInputHandler(()=>fakeNow);
chargeKb.HandleKeyDown(0x57,true);
keys=chargeKb.PollCurrentActions(true);
Check(!keys.Contains(GameAction.Shoot),"held shot charges without firing on press");
fakeNow=0.1;chargeKb.HandleKeyUp(0x57);keys=chargeKb.PollCurrentActions(true);
Check(keys.Contains(GameAction.Shoot)&&chargeKb.ConsumeReleasedCharge01(GameAction.Shoot)==0f,"quick tap fires a normal-power shot on release");
chargeKb.HandleKeyDown(0x57,true);fakeNow=1.0;
Check(Math.Abs(chargeKb.ActiveCharge01(GameAction.Shoot)-0.52f)<0.05f,"charge progress is queryable while the key is held");
keys=chargeKb.PollCurrentActions(true);
Check(!keys.Contains(GameAction.Shoot),"still no shot while the key remains held");
chargeKb.HandleKeyUp(0x57);keys=chargeKb.PollCurrentActions(true);
Check(keys.Contains(GameAction.Shoot)&&Math.Abs(chargeKb.ConsumeReleasedCharge01(GameAction.Shoot)-0.52f)<0.05f,"a 0.9s hold releases a mid-power charged shot");
keys=chargeKb.PollCurrentActions(true);
Check(!keys.Contains(GameAction.Shoot)&&chargeKb.ConsumeReleasedCharge01(GameAction.Shoot)==0f,"a charged release fires exactly once with no stale repeat");
chargeKb.HandleKeyDown(0x44,true);fakeNow=3.2;
Check(chargeKb.ActiveCharge01(GameAction.Cross)>=1f,"charge caps at full power after 1.5s");
chargeKb.HandleKeyUp(0x44);keys=chargeKb.PollCurrentActions(true);
Check(keys.Contains(GameAction.Cross)&&chargeKb.ConsumeReleasedCharge01(GameAction.Cross)>=1f,"a long cross hold releases a fully charged high cross");
chargeKb.HandleKeyDown(0x53,true);fakeNow=4.0;chargeKb.Reset();chargeKb.HandleKeyUp(0x53);
keys=chargeKb.PollCurrentActions(true);
Check(!keys.Contains(GameAction.ShortPass),"pause/focus Reset drops a stale charge so no pass fires after");
var offballKb=new AccessibleFootball2026.Input.KeyboardInputHandler(()=>fakeNow);
offballKb.HandleKeyDown(0x53,false);keys=offballKb.PollCurrentActions(false);
Check(keys.Contains(GameAction.StandTackle)&&!keys.Contains(GameAction.ShortPass),"off-ball S still fires a stand tackle instantly on press");
// Charge wiring contract: the engine consumes the released charge and scales the real kicks; cue is an earcon, never speech.
{
 var dir=AppContext.BaseDirectory;string? chargeAnchor=null;
 for(int up=0;up<12&&dir!=null;up++){var c=Path.Combine(dir,"Build-Complete.py");if(File.Exists(c)){chargeAnchor=c;break;}dir=Path.GetDirectoryName(dir);}
 if(chargeAnchor==null){Console.WriteLine("SKIP charge wiring checks outside the repo checkout");}
 else{
  var chargeRoot=Path.GetDirectoryName(chargeAnchor)!;
  var engineSrc=File.ReadAllText(Path.Combine(chargeRoot,"AccessibleFootball2026.Core","Simulation","MatchEngine.cs"));
  Check(engineSrc.Contains("ConsumeReleasedCharge01(GameAction.ShortPass)")&&engineSrc.Contains("ConsumeReleasedCharge01(GameAction.Cross)")&&engineSrc.Contains("ConsumeReleasedCharge01(GameAction.Shoot)"),"engine consumes the released charge for pass, cross and shot");
  Check(!engineSrc.Contains("KickChargeCue"),"no charging tones: hold power works silently, only the executed kick is heard");
  var shotSrc=File.ReadAllText(Path.Combine(chargeRoot,"AccessibleFootball2026.Core","Simulation","ShotTechniques.cs"));
  Check(shotSrc.Contains("1f + .35f * charge")&&shotSrc.Contains("1f + .7f * charge"),"charged driven shots gain speed but lose accuracy, never a guaranteed goal");
  var audioSrc=File.ReadAllText(Path.Combine(chargeRoot,"AccessibleFootball2026.Audio","SmartAudioCoordinator.cs"));
  Check(!audioSrc.Contains("KickChargeCue")&&!audioSrc.Contains("PlayChargeMax"),"audio layer carries no charge-start or full-charge cue");
  var gkSrc=File.ReadAllText(Path.Combine(chargeRoot,"AccessibleFootball2026.Core","Simulation","MatchEngineIncidents.cs"));
  var awayGuard=Regex.Escape("&& (Ball.Owner == null || AwayTeam.Players.Contains(Ball.Owner))");
  var homeGuard=Regex.Escape("&& (Ball.Owner == null || HomeTeam.Players.Contains(Ball.Owner))");
  Check(Regex.Matches(gkSrc,awayGuard).Count>=1&&Regex.Matches(gkSrc,homeGuard).Count>=1,"keeper auto-catch never takes an opponent-owned controlled ball (both teams)");
  Check(Regex.Matches(gkSrc,Regex.Escape("HandleFoulSanction(")).Count>=3,"keeper duels route fouls through the same sanction path as outfield tackles");
 }
}
// Charged kicks move real ball physics: same engine, same positions, only hold duration changes.
float ShotSpeed(float charge){var m=Match();m.Update(.1f);var shooter=m.ActiveUserPlayer;shooter.Position2D=new(75,34);m.Ball.CatchOrTackle(shooter);m.Ball.Position=new(75,34,0);m.ExecuteShot(charge);return m.Ball.Velocity.Length();}
Check(ShotSpeed(0f)>10f,"a tap shot still fires at normal power");
Check(ShotSpeed(1f)>ShotSpeed(0f)*1.25f,"a fully charged shot is physically faster than a tap");
float PassSpeed(float charge){var m=Match();m.Update(.1f);var passer=m.ActiveUserPlayer;passer.Position2D=new(50,34);m.Ball.CatchOrTackle(passer);m.Ball.Position=new(50,34,0);m.ExecutePass(new Vector2(1,0),false,false,charge);return m.Ball.Velocity.Length();}
Check(PassSpeed(1f)>PassSpeed(0f)*1.35f,"a fully charged pass is physically faster than a tap pass");
float CrossLift(float charge){var m=Match();m.Update(.1f);var crosser=m.ActiveUserPlayer;crosser.Position2D=new(85,50);m.Ball.CatchOrTackle(crosser);m.Ball.Position=new(85,50,0);m.ExecuteCross(CrossStyle.Center,charge);return m.Ball.Velocity.Z;}
Check(CrossLift(1f)-CrossLift(0f)>3.5f,"a long cross hold physically raises the cross into a high lob");
// Charge transition/regression contracts on the real engine path.
MatchEngine ChargeMatch(IInputHandler h)=>new(Make("h",TeamSide.Home),Make("a",TeamSide.Away),new MatchEventBus(),h);
{
 double engNow=0.0;
 var engKb=new AccessibleFootball2026.Input.KeyboardInputHandler(()=>engNow);
 var em=ChargeMatch(engKb);em.Update(.1f);
 var owner=em.ActiveUserPlayer;owner.Position2D=new(50,34);em.Ball.CatchOrTackle(owner);em.Ball.Position=new(50,34,0);
 engKb.HandleKeyDown(0x53,true);
 em.Ball.CatchOrTackle(em.AwayTeam.Players[4]);
 engNow=0.6;engKb.HandleKeyUp(0x53);em.Update(.02f);
 Check(!em.IsPassInTransit,"a charged pass released after losing the ball mid-hold never launches");
 em.Ball.CatchOrTackle(owner);em.Ball.Position=new(50,34,0);
 engKb.HandleKeyDown(0x53,true);engNow=1.1;engKb.HandleKeyUp(0x53);em.Update(.02f);
 Check(em.IsPassInTransit,"a fresh charged pass fires normally after regaining the ball");
}
{
 double sw=0.0;
 var swKb=new AccessibleFootball2026.Input.KeyboardInputHandler(()=>sw);
 var sm=ChargeMatch(swKb);sm.Update(.1f);
 var teammate=sm.HomeTeam.Players.First(p=>p.IsActiveOnPitch&&p!=sm.ActiveUserPlayer);
 sm.Ball.CatchOrTackle(teammate);sm.Ball.Position=new(50,34,0);
 swKb.HandleKeyDown(0x53,true);swKb.HandleKeyDown(0x11,true);
 sm.Update(.02f);
 sw=0.5;swKb.HandleKeyUp(0x53);swKb.HandleKeyUp(0x11);sm.Update(.02f);
 Check(!sm.IsPassInTransit,"charging while a teammate owns the ball never launches a pass after a player switch");
}
{
 double mq=0.0;
 var mqKb=new AccessibleFootball2026.Input.KeyboardInputHandler(()=>mq);
 mqKb.HandleKeyDown(0x53,true);mq=0.4;mqKb.HandleKeyUp(0x53);
 mqKb.HandleKeyDown(0x57,true);mq=0.8;mqKb.HandleKeyUp(0x57);
 var set=mqKb.PollCurrentActions(true);
 Check(set.Contains(GameAction.ShortPass)&&set.Contains(GameAction.Shoot),"S and W charged releases queue together in one poll");
 Check(mqKb.ConsumeReleasedCharge01(GameAction.ShortPass)>0f&&mqKb.ConsumeReleasedCharge01(GameAction.Shoot)>0f,"each queued charged release keeps its own charge fraction");
 Check(mqKb.ConsumeReleasedCharge01(GameAction.ShortPass)==0f&&mqKb.ConsumeReleasedCharge01(GameAction.Shoot)==0f,"charge consumption is single-use with nothing left stale");
 mqKb.HandleKeyDown(0x53,true);mq=1.0;mqKb.HandleKeyUp(0x53);mqKb.HandleKeyDown(0x53,true);mq=1.4;mqKb.HandleKeyUp(0x53);
 set=mqKb.PollCurrentActions(true);
 Check(set.Contains(GameAction.ShortPass)&&Math.Abs(mqKb.ConsumeReleasedCharge01(GameAction.ShortPass)-0.12f)<0.05f,"rapid release and repress between polls collapses to one action with the latest charge");
}
{
 double ch=0.0;
 var chKb=new AccessibleFootball2026.Input.KeyboardInputHandler(()=>ch);
 chKb.HandleKeyDown(0x57,true);chKb.HandleKeyDown(0x11,true);chKb.HandleKeyDown(0x11,true);
 var chSet=chKb.PollCurrentActions(true);
 Check(chSet.Contains(GameAction.SwitchPlayer)&&!chSet.Contains(GameAction.Shoot),"Ctrl fires once during a shot charge while the shot keeps charging");
 chKb.HandleKeyDown(0x11,true);
 chSet=chKb.PollCurrentActions(true);
 Check(!chSet.Contains(GameAction.SwitchPlayer),"held Ctrl never repeats while a charge is in progress");
 ch=0.5;chKb.HandleKeyUp(0x57);chKb.HandleKeyUp(0x11);
 chSet=chKb.PollCurrentActions(true);
 Check(chSet.Contains(GameAction.Shoot),"the charged shot still fires on release after the Ctrl hold ends");
}

// Contact simulation parameters, not a claim of complete Law 12 coverage.
{
    s.AiVarEnabled=false;
    (MatchEngine e,Player t,Player c,List<MatchEvent> cards) FoulCase(bool homeTackles,float distance=52)
    {
        var h=Make("lawh",TeamSide.Home);var a=Make("lawa",TeamSide.Away);var bus=new MatchEventBus();
        var e=new MatchEngine(h,a,bus,new Input());
        var t=(homeTackles?h:a).Players[1];var c=(homeTackles?a:h).Players[1];
        float x=homeTackles?distance:Pitch.Length-distance;float sign=homeTackles?-1:1;
        foreach(var p in h.Players.Concat(a.Players)){p.Position2D=new(homeTackles?70:35,10);p.Velocity=Vector2.Zero;}
        c.Position2D=new(x,34);c.FacingDirection=new(sign,0);
        t.Position2D=new(x,35);t.FacingDirection=new(0,-1);
        e.Ball.CatchOrTackle(c);
        var cards=new List<MatchEvent>();bus.OnMatchEvent+=ev=>{if(ev.Type is MatchEventType.YellowCard or MatchEventType.RedCard)cards.Add(ev);};
        return(e,t,c,cards);
    }
    foreach(bool side in new[]{true,false})
    {
        var f=FoulCase(side);f.e.HandleFoulSanction(f.t,f.c,true,true,MatchEngine.CaptureFoulIncident(f.t,f.c,f.e.Ball.Position2D));
        Check(f.cards.Count==0,"stationary foul has no automatic card despite slide/behind flags on side "+side);
        f=FoulCase(side);f.t.Velocity=new(0,-16);f.e.HandleFoulSanction(f.t,f.c,false,false,MatchEngine.CaptureFoulIncident(f.t,f.c,f.e.Ball.Position2D));
        Check(f.cards.Count==1&&f.t.YellowCards==1&&!f.t.IsSentOff,"16m/s impact cautions with stationary victim on side "+side);
        // A new incident with the same offender exercises the unchanged second-caution workflow.
        f.t.Position2D=f.c.Position2D+Vector2.UnitY;f.t.Velocity=new(0,-16);f.c.Velocity=Vector2.Zero;
        f.e.HandleFoulSanction(f.t,f.c,false,false,MatchEngine.CaptureFoulIncident(f.t,f.c,f.e.Ball.Position2D));
        Check(f.t.IsSentOff&&f.t.YellowCards==2,"second reckless contact uses second-yellow dismissal on side "+side);
        f=FoulCase(side);f.t.Velocity=new(0,-26);f.e.HandleFoulSanction(f.t,f.c,false,false,MatchEngine.CaptureFoulIncident(f.t,f.c,f.e.Ball.Position2D));
        Check(f.cards.Any(ev=>ev.Type==MatchEventType.RedCard)&&f.t.IsSentOff&&f.t.YellowCards==0,"high-speed contact keeps SFP red possible on side "+side);
        f=FoulCase(side);f.t.Velocity=new(0,-26);f.c.Velocity=new(0,-26);
        var following=MatchEngine.CaptureFoulIncident(f.t,f.c,f.e.Ball.Position2D);
        Check(following.Severity==MatchEngine.FoulContactSeverity.Careless&&following.ClosingSpeed==0,"moving-away victim avoids false high-speed impact on side "+side);
        f=FoulCase(side);f.c.Velocity=new(0,12);
        Check(MatchEngine.CaptureFoulIncident(f.t,f.c,f.e.Ball.Position2D).Severity==MatchEngine.FoulContactSeverity.Careless,"stationary tackler not assigned force from victim speed on side "+side);
        f=FoulCase(side,10);f.e.HandleFoulSanction(f.t,f.c,false,false,MatchEngine.CaptureFoulIncident(f.t,f.c,f.e.Ball.Position2D));
        Check(f.t.YellowCards==1&&!f.t.IsSentOff&&f.e.Restart?.Kind==RestartKind.Penalty,"inside-box DOGSO genuine side ball challenge cautions plus penalty on side "+side);
        f=FoulCase(side,10);float sign=side?-1:1;f.t.Position2D=f.c.Position2D-new Vector2(sign,0);f.t.FacingDirection=new(sign,0);
        f.e.HandleFoulSanction(f.t,f.c,false,true,MatchEngine.CaptureFoulIncident(f.t,f.c,f.e.Ball.Position2D));
        Check(f.t.IsSentOff&&f.t.YellowCards==0&&f.e.Restart?.Kind==RestartKind.Penalty,"inside-box DOGSO blocked ball path remains red plus penalty on side "+side);
        f=FoulCase(side,20);f.e.HandleFoulSanction(f.t,f.c,false,false,MatchEngine.CaptureFoulIncident(f.t,f.c,f.e.Ball.Position2D));
        Check(f.t.IsSentOff&&f.e.Restart?.Kind==RestartKind.FreeKick,"outside-box DOGSO remains red on side "+side);
        f=FoulCase(side);f.t.Velocity=new(0,-4);
        var lunge=MatchEngine.CaptureFoulIncident(f.t,f.c,f.e.Ball.Position2D,2.8f,new Vector2(0,-4));
        Check(Math.Abs(lunge.ClosingSpeed-4)<.001f,"slide impulse never counted twice on side "+side);
        (side?f.e.HomeTeam:f.e.AwayTeam).Tactics.Tackling=TacklingAggression.Reckless;
        var aggressive=MatchEngine.CaptureFoulIncident(f.t,f.c,f.e.Ball.Position2D);
        (side?f.e.HomeTeam:f.e.AwayTeam).Tactics.Tackling=TacklingAggression.Cautious;
        Check(aggressive==MatchEngine.CaptureFoulIncident(f.t,f.c,f.e.Ball.Position2D),"tactical aggression does not classify contact on side "+side);
        f.e.AwardFoulRestart(f.c);
        Check(f.e.HomeTeam.Players.Concat(f.e.AwayTeam.Players).All(p=>p.Velocity==Vector2.Zero),"restart placement clears contact momentum on side "+side);
    }
    foreach(float dt in new[]{.01f,.04f})
    {
        var movement=new LawMovementInput();var e=new MatchEngine(Make("mh",TeamSide.Home),Make("ma",TeamSide.Away),new MatchEventBus(),movement);
        e.ActiveUserPlayer.Position2D=new(50,20);var before=e.ActiveUserPlayer.Position2D;
        typeof(MatchEngine).GetField("_kickoffProtectionTimer",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(e,0f);
        var keeper=e.AwayTeam.Players.First(p=>p.Role==PlayerRole.Goalkeeper);keeper.Position2D=new(100,27);var keeperBefore=keeper.Position2D;
        e.Update(dt);var speed=(e.ActiveUserPlayer.Position2D-before).Length()/dt;
        float keeperMeasured=(keeper.Position2D-keeperBefore).Length()/dt;
        Check(Math.Abs(keeper.Velocity.Length()-keeperMeasured)<.01f,"CPU keeper velocity records physical displacement per second at dt "+dt);
        var contactMotion=typeof(MatchEngine).GetMethod("ContactMotion",BindingFlags.NonPublic|BindingFlags.Instance)!;
        Check((Vector2)contactMotion.Invoke(e,new object[]{keeper})! == Vector2.Zero,"keeper positional correction contributes zero contact force at dt "+dt);
        var correctionVictim=e.HomeTeam.Players[1];correctionVictim.Position2D=new(52,34);keeper.Position2D=new(52,35);keeper.FacingDirection=new(0,-1);
        keeper.Velocity=new(0,-30); // deliberately contaminate display velocity; live contact source stays zero
        e.Ball.CatchOrTackle(correctionVictim);e.HandleFoulSanction(keeper,correctionVictim,true,true);
        Check(!keeper.IsSentOff&&keeper.YellowCards==0,"keeper correction/display velocity cannot synthesize a SFP red at dt "+dt);
        Console.WriteLine($"CONTACT CALIBRATION dt={dt}: user={speed:F3}m/s keeper={keeperMeasured:F3}m/s; CPU 75-speed press base Easy/Normal/Hard/Legendary=3.375/4.650/6.375/7.875m/s before fatigue/reaction modifiers");
        Check(speed>8&&speed<18&&Math.Abs(speed-10)<.01f,"actual movement velocity is metres per second at dt "+dt);
        var t=e.HomeTeam.Players[1];var c=e.AwayTeam.Players[1];t.Position2D=new(50,35);c.Position2D=new(50,34);t.FacingDirection=new(0,-1);t.Velocity=new(0,-16);c.Velocity=Vector2.Zero;
        Check(MatchEngine.CaptureFoulIncident(t,c,c.Position2D).Severity==MatchEngine.FoulContactSeverity.Reckless,"classification independent of frame length "+dt);
    }
    foreach(float dt in new[]{.01f,.02f,.04f}) foreach(bool sprint in new[]{false,true}) foreach(bool slide in new[]{false,true}) foreach(int rating in new[]{75,100})
    {
        var actions=new LawMovementInput();actions.Actions.Add(slide?GameAction.SlideTackle:GameAction.StandTackle);
        if(sprint)actions.Actions.Add(GameAction.Sprint);
        var h=Make("rh",TeamSide.Home);var a=Make("ra",TeamSide.Away);var bus=new MatchEventBus();var e=new MatchEngine(h,a,bus,actions);
        var t=e.ActiveUserPlayer;var c=a.Players[1];t.BaseSpeed=rating;
        foreach(var pl in h.Players.Concat(a.Players))pl.Position2D=new(80,10);
        t.Position2D=new(50,34);c.Position2D=new(51.5f,34);c.FacingDirection=Vector2.UnitX;e.Ball.CatchOrTackle(c);
        int fouls=0;bus.OnMatchEvent+=ev=>{if(ev.Type==MatchEventType.FoulCommitted)fouls++;};
        e.Update(dt);
        bool highImpact=false;
        Check(fouls==1&&t.IsSentOff==highImpact&&t.YellowCards==(sprint?1:0),$"actual input dt={dt} rating={rating} sprint={sprint} slide={slide}: normal run no red, calibrated high-speed contact SFP");
        Console.WriteLine($"ACTUAL CONTACT dt={dt} rating={rating} sprint={sprint} slide={slide} fouls={fouls} yellow={t.YellowCards} red={t.IsSentOff}");
    }
    foreach(float dt in new[]{.01f,.02f,.04f})
    {
        var actions=new LawMovementInput();var h=Make("ih",TeamSide.Home);var a=Make("ia",TeamSide.Away);
        var e=new MatchEngine(h,a,new MatchEventBus(),actions);var t=e.ActiveUserPlayer;var c=a.Players[1];
        foreach(var pl in h.Players.Concat(a.Players))pl.Position2D=new(80,10);
        t.Position2D=new(40,34);c.Position2D=new(52,34);e.Ball.CatchOrTackle(c);
        typeof(MatchEngine).GetField("_kickoffProtectionTimer",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(e,0f);
        actions.Actions.Clear();e.Update(dt); // CPU carrier really runs left, about4.875m/s.
        float victimX=c.Position2D.X;
        t.Position2D=new(victimX-(.4f+16*dt),34); // after the next movement, a physical contact at.4m
        actions.Actions.UnionWith(new[]{GameAction.MoveRight,GameAction.Sprint,GameAction.StandTackle});
        e.Update(dt);
        Check(t.IsSentOff&&t.YellowCards==0,"actual sprint into oncoming carrier contact can produce SFP red even ball-first at dt "+dt);
    }
    // Change live motion/positions after review starts: the original incident must survive.
    s.AiVarEnabled=true;var v=FoulCase(true);v.t.Velocity=new(0,-26);
    v.e.HandleFoulSanction(v.t,v.c,false,false,MatchEngine.CaptureFoulIncident(v.t,v.c,v.e.Ball.Position2D));
    Check(v.e.VideoReviewPending&&!v.t.IsSentOff,"SFP waits for approved VAR narration and Enter");
    v.t.Velocity=Vector2.Zero;v.c.Position2D=new(10,34);
    for(int i=0;i<80&&!v.e.AwaitingStoppageConfirmation;i++)v.e.Update(.1f);
    v.e.DismissStoppageReplay();
    Check(v.t.IsSentOff&&v.t.YellowCards==0,"VAR preserves original force snapshot despite live changes");
    Check(v.e.Restart?.Kind==RestartKind.FreeKick&&Math.Abs(v.e.Restart.Spot.X-52)<.01f,"VAR preserves original restart location not changed victim position");
    s.AiVarEnabled=false;
}

// === Targeted regressions: tackle path geometry, converging home press, CPU receiver parity ===
{
    void ZeroTimers(MatchEngine e){var T=typeof(MatchEngine);foreach(var n in new[]{"_kickoffProtectionTimer","_possessionGraceTimer","_aiTackleCooldownTimer"})T.GetField(n,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(e,0f);}
    void ParkAll(MatchEngine e){foreach(var pl in e.HomeTeam.Players.Concat(e.AwayTeam.Players))pl.Position2D=new(80,10);}
    // Let every AI player reach his formation anchor before measuring (kills ParkAll Lerp-drift
    // artifacts): the holder keeps the ball far from all pressers with passing disabled.
    void Settle(MatchEngine e,Player holder)
    {
        foreach(var pl in e.HomeTeam.Players.Concat(e.AwayTeam.Players))pl.Position2D=new(80,10);
        holder.Position2D=new(95,34);holder.FacingDirection=-Vector2.UnitX;e.Ball.CatchOrTackle(holder);
        e.ActiveUserPlayer.Position2D=new(80,10);
        var dw=(System.Collections.IDictionary)typeof(MatchEngine).GetField("_tacticalDecisionWait",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(e)!;dw[holder]=50f;
        for(int k=0;k<75;k++)e.Update(.02f);
    }
    float GraceOf(MatchEngine e)=>(float)typeof(MatchEngine).GetField("_possessionGraceTimer",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(e)!;

    // A. Close-quarters rear poke with no closing speed is a duel, not an automatic foul.
    // The existing 24-scenario matrix above still covers body-blocked 1.5m lunges and sprint collisions.
    foreach(float dt in new[]{.01f,.04f})
    {
        var acts=new LawMovementInput();acts.Actions.Clear();acts.Actions.Add(GameAction.StandTackle);
        var h=Make("pkh",TeamSide.Home);var a=Make("pka",TeamSide.Away);var bus=new MatchEventBus();var e=new MatchEngine(h,a,bus,acts);
        ParkAll(e);ZeroTimers(e);
        var c=a.Players[1];c.Position2D=new(50,34);c.FacingDirection=Vector2.UnitX;e.Ball.CatchOrTackle(c);
        var t=e.ActiveUserPlayer;t.Position2D=new(49.3f,34);t.Velocity=Vector2.Zero; // 0.7m directly behind, stationary
        int fouls=0,duels=0;
        bus.OnMatchEvent+=ev=>{if(ev.Type==MatchEventType.FoulCommitted)fouls++;if(ev.Type is MatchEventType.TackleAttempted or MatchEventType.UserTackleSuccessful)duels++;};
        e.Update(dt);
        Check(fouls==0&&duels==1,$"close-quarters rear poke at 0.7m with no closing speed is a duel not an automatic foul at dt {dt}");
    }

    // B. Selected-player control preserved: the press never moves the user's active player.
    {
        var acts=new Input();var h=Make("sph",TeamSide.Home);var a=Make("spa",TeamSide.Away);var bus=new MatchEventBus();var e=new MatchEngine(h,a,bus,acts);
        ParkAll(e);ZeroTimers(e);h.Tactics.PressingIntensity=50;
        var c=a.Players[1];c.Position2D=new(54,34);c.FacingDirection=Vector2.UnitX;e.Ball.CatchOrTackle(c);
        var t=e.ActiveUserPlayer;t.Position2D=new(52,34);t.Velocity=Vector2.Zero;
        var dw=(System.Collections.IDictionary)typeof(MatchEngine).GetField("_tacticalDecisionWait",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(e)!;dw[c]=50f;
        for(int i=0;i<40;i++)e.Update(.02f);
        Check(t.Position2D==new Vector2(52,34)&&t.Velocity==Vector2.Zero,"converging press never moves the user's selected player");
    }

    // C. Press convergence is velocity-bounded, directional, and count-limited (measured in the
    // first second of the press, before any duel/restart can reposition players; end-to-end arrival
    // at duel range is covered by the AI-duel foul/win evidence in the hoard probe).
    {
        var acts=new Input();var h=Make("cvh",TeamSide.Home);var a=Make("cva",TeamSide.Away);var bus=new MatchEventBus();var e=new MatchEngine(h,a,bus,acts);
        ZeroTimers(e);h.Tactics.PressingIntensity=50;
        var c=a.Players[1];Settle(e,c);
        // start the observed press from a realistic goal-side spot against a midfielder carrier
        c.Position2D=new(50,34);c.FacingDirection=-Vector2.UnitX;
        e.Ball.Position=new Vector3(50,34,.1f);e.Ball.CatchOrTackle(c);
        var ph=h.Players.Where(pl=>pl!=e.ActiveUserPlayer&&pl.Role!=PlayerRole.Goalkeeper).OrderBy(pl=>Vector2.DistanceSquared(pl.Position2D,c.Position2D)).First();
        float d0=Vector2.Distance(ph.Position2D,e.Ball.Position2D),maxStep=0;int maxPressers=0;
        var prev=ph.Position2D;int frames=0;
        for(int i=0;i<50&&e.Restart==null&&e.Ball.Owner==c;i++)
        {
            e.Update(.02f);frames++;
            maxStep=Math.Max(maxStep,Vector2.Distance(ph.Position2D,prev));prev=ph.Position2D;
            maxPressers=Math.Max(maxPressers,h.Players.Count(pl=>pl!=e.ActiveUserPlayer&&pl.Role!=PlayerRole.Goalkeeper
                &&pl.Velocity.Length()>3&&Vector2.Distance(pl.Position2D,e.Ball.Position2D)<14f
                &&Vector2.Dot(Vector2.Normalize(pl.Velocity),Vector2.Normalize(e.Ball.Position2D-pl.Position2D))>.8f));
        }
        float d1=Vector2.Distance(ph.Position2D,e.Ball.Position2D);
        Check(e.Ball.Owner==c&&d1<=d0-.7f,$"pressing teammate converges on the carrier by real movement ({d0:F1}m -> {d1:F1}m over {frames} frames)");
        Check(maxStep<=.25f,$"press convergence is velocity-bounded with no teleport (max frame step {maxStep:F3}m at dt 0.02)");
        Check(maxPressers>=1&&maxPressers<=3,$"press stays count-limited, no whole-team swarm (peak in-range ball-facing pressers {maxPressers})");
    }

    // D. CPU receiver guidance: named receiver runs to meet the pass and completes it.
    {
        var acts=new Input();var h=Make("rgh",TeamSide.Home);var a=Make("rga",TeamSide.Away);var bus=new MatchEventBus();var e=new MatchEngine(h,a,bus,acts);
        ParkAll(e);ZeroTimers(e);
        var c1=a.Players[1];var c2=a.Players[2];c1.Position2D=new(40,34);c2.Position2D=new(55,20);
        e.Ball.Kick(Vector2.Normalize(new Vector2(12,-14)),16,.15f,c1);
        Call(e,"BeginCommentaryPass",c1,c2,c2.Position2D,"short","ground",false);
        float d0=Vector2.Distance(c2.Position2D,e.Ball.Position2D),minD=d0;
        int completed=0;Player? recv=null;
        bus.OnMatchEvent+=ev=>{if(ev.Type==MatchEventType.PassCompleted){completed++;recv=ev.SubjectPlayer;}};
        for(int i=0;i<800&&completed==0;i++){e.Update(.01f);minD=Math.Min(minD,Vector2.Distance(c2.Position2D,e.Ball.Position2D));}
        Check(minD<=d0-3f,$"CPU pass receiver runs to meet the ball ({d0:F1}m -> {minD:F1}m)");
        Check(completed==1&&recv==c2,"CPU receiver guidance produces a completed pass to the named receiver");
    }

    // E. Reception guidance never claims an opponent-controlled ball.
    {
        var acts=new Input();var h=Make("och",TeamSide.Home);var a=Make("oca",TeamSide.Away);var bus=new MatchEventBus();var e=new MatchEngine(h,a,bus,acts);
        ParkAll(e);ZeroTimers(e);
        var c1=a.Players[1];var c2=a.Players[2];c1.Position2D=new(40,34);c2.Position2D=new(46,34);
        var hp=h.Players.First(pl=>pl!=e.ActiveUserPlayer);hp.Position2D=new(52,34);
        e.Ball.CatchOrTackle(hp);
        Call(e,"BeginCommentaryPass",c1,c2,c2.Position2D,"short","ground",false);
        int completed=0;bus.OnMatchEvent+=ev=>{if(ev.Type==MatchEventType.PassCompleted)completed++;};
        for(int i=0;i<100;i++)e.Update(.01f);
        Check(e.Ball.Owner==hp&&completed==0,"CPU receiver guidance cannot claim an opponent-controlled ball");
    }

    // F. Receiver-guidance guards. The guidance reception is the only away pickup path that grants
    // possession grace (line-level fact), so a positive grace sample proves guidance acted; its
    // absence proves the guard suppressed guidance regardless of other movement.
    {
        (float approach,float grace,bool owned,int frames) RunGuard(float ballOffset,bool bumpAge,bool outOfBounds)
        {
            var acts=new Input();var h=Make("guh",TeamSide.Home);var a=Make("gua",TeamSide.Away);var bus=new MatchEventBus();var e=new MatchEngine(h,a,bus,acts);
            ZeroTimers(e);
            var c1=a.Players[1];var c2=a.Players[2];Settle(e,c1);
            float dx=c2.Position2D.X<60?ballOffset:-ballOffset;
            var spot=c2.Position2D+new Vector2(dx,0);
            spot.X=Math.Clamp(spot.X,3f,102f);
            e.Ball.Kick(Vector2.UnitX,.01f,.15f,c1);e.Ball.Position=new Vector3(spot.X,spot.Y,.1f);
            Call(e,"BeginCommentaryPass",c1,c2,c2.Position2D,"short","ground",false);
            if(bumpAge){var ef=typeof(MatchEngine).GetField("_elapsedSeconds",BindingFlags.Instance|BindingFlags.NonPublic)!;ef.SetValue(e,(float)ef.GetValue(e)!+9f);}
            if(outOfBounds)e.Ball.State=BallState.OutOfBounds;
            float d0=Vector2.Distance(c2.Position2D,spot),maxGrace=0;int k=0;
            bool WindowOpen()=>e.Restart==null&&(!outOfBounds||e.Ball.State==BallState.OutOfBounds);
            for(;k<160&&e.Ball.Owner!=c2&&WindowOpen();k++){e.Update(.01f);maxGrace=Math.Max(maxGrace,GraceOf(e));}
            return (d0-Vector2.Distance(c2.Position2D,spot),maxGrace,e.Ball.Owner==c2,k);
        }
        var beyondCap=RunGuard(19f,false,false);
        // grace is the guidance reception's unique signature; ordinary loose-ball cleanup may still
        // move players, so the guard is proven by its absence
        Check(beyondCap.grace==0,$"no receiver guidance beyond the 18m reachability cap (grace {beyondCap.grace:F2}, approach {beyondCap.approach:F2}m)");
        var insideCap=RunGuard(13f,false,false); // 13m: inside the cap and clear of interception range from home anchors
        Check(insideCap.grace>0&&insideCap.owned,$"receiver guidance acts inside the cap (grace {insideCap.grace:F2}, received {insideCap.owned})");
        var expired=RunGuard(6f,true,false);
        Check(expired.grace==0,$"no receiver guidance from an expired transit >8s (grace {expired.grace:F2}, approach {expired.approach:F2}m)");
        var live=RunGuard(6f,false,false);
        Check(live.grace>0&&live.owned,"live transit at the same spot guides the receiver into possession");
        var oob=RunGuard(6f,false,true);
        Check(oob.grace==0&&oob.approach<2f,$"no receiver guidance to an out-of-bounds ball (approach {oob.approach:F2}m over {oob.frames} frames, grace {oob.grace:F2})");
    }
}
MciPlayback.TestCommand=null;Console.WriteLine($"RESULT {passed} passed, {failed} failed. Native MCI/Windows sound was not executed.");return failed==0?0:1;
sealed class Reader:IScreenReader{public List<string> Spoken=new();public bool IsAvailable=>true;public void Speak(string text,bool interrupt=true)=>Spoken.Add(text);public void CancelSpeech(){}public Task SpeakUntilFinishedAsync(string text,bool interrupt=true){Speak(text,interrupt);return Task.CompletedTask;}}
sealed class Input:IInputHandler{public HashSet<GameAction> PollCurrentActions(bool hasPossession)=>new();public bool IsActionTriggered(GameAction action,bool possession)=>false;}

sealed class ControlledReader:IScreenReader
{
 public bool IsAvailable=>true;
 public TaskCompletionSource<bool> Started=new(TaskCreationOptions.RunContinuationsAsynchronously),Finish=new(TaskCreationOptions.RunContinuationsAsynchronously);
 public void Speak(string text,bool interrupt=true){}
 public Task SpeakUntilFinishedAsync(string text,bool interrupt=true){Started.TrySetResult(true);return Finish.Task;}
 public void CancelSpeech()=>Finish.TrySetResult(true);
}

sealed class LawMovementInput:IInputHandler{public HashSet<GameAction> Actions=new(){GameAction.MoveRight};public HashSet<GameAction> PollCurrentActions(bool hasPossession)=>Actions;public bool IsActionTriggered(GameAction action,bool possession)=>false;}
