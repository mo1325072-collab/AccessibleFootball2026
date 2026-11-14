using System.Text.Json;
using AccessibleFootball2026.Audio.SFX;
using AccessibleFootball2026.Core.Models;
if(args.Length<2||args.Length>3)throw new ArgumentException("audio root and output JSON required");
string root=Path.GetFullPath(args[0]);
var missing=AccessibleFootball2026.Audio.PackageAudioGuard.FindMissing(root);
if(missing.Count>0)throw new InvalidDataException("Package audio paths missing before initialization: "+string.Join(",",missing));
string chants=Path.Combine(root,"SFX","Chants");
var linked=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach(var pool in TeamChantCatalog.Pools)
{
 var paths=StadiumSoundEngine.ResolveLinkedChantFiles(chants,pool);
 if(paths.Count==0)throw new InvalidDataException("Linked chant pool missing: "+pool.TeamName);
 foreach(var path in paths)linked.Add(Path.GetRelativePath(root,path).Replace('\\','/'));
}
var required=AccessibleFootball2026.Audio.PackageAudioGuard.RequiredPaths.ToList();
var whistles=Directory.GetFiles(Path.Combine(root,"SFX","Whistles"),"*.wav");
if(whistles.Length==0)throw new InvalidDataException("Referee whistle assets missing");
required.AddRange(whistles.Select(p=>Path.GetRelativePath(root,p).Replace('\\','/')));
foreach(var path in required.Concat(linked))if(!File.Exists(Path.Combine(root,path))||new FileInfo(Path.Combine(root,path)).Length==0)throw new InvalidDataException("Required package sound missing: "+path);
File.WriteAllText(args[1],JsonSerializer.Serialize(new{linked_chants=linked.Order().ToArray(),required=required.Order().ToArray(),pool_count=TeamChantCatalog.Pools.Count},new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine($"Package audio inventory: {linked.Count} linked chants, {required.Count} required sound/catalog paths, {TeamChantCatalog.Pools.Count} pools");

if(args.Length==3&&args[2]=="--probe")
{
 var commands=new List<string>();
 var hook=typeof(AccessibleFootball2026.Audio.SoundEffectsPlayer).Assembly.GetType("AccessibleFootball2026.Audio.MciPlayback")!.GetField("TestCommand",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)!;
 hook.SetValue(null,(Func<string,System.Text.StringBuilder?,int>)((command,buffer)=>{commands.Add(command);buffer?.Append("stopped");return 0;}));
 MatchSettings.Current.ResetToDefaults();
 AccessibleFootball2026.Audio.SoundEffectsPlayer.Initialize(root);
 AccessibleFootball2026.Audio.GoalBeaconPlayer.Initialize(root);
 void Check(bool ok,string message){if(!ok)throw new InvalidDataException(message);}
 foreach(string cue in new[]{"TackleWon","TackleMissed","BallLost","Foul","Penalty","Corner","GoalKick","ThrowIn","PlayerSwitch"})
 {
  commands.Clear();Check(AccessibleFootball2026.Audio.SoundEffectsPlayer.TryPlayAcademySample(cue)&&commands.Contains("play academy_earcon from 0"),"Staged academy cue cannot resolve/play: "+cue);
 }
 foreach(var entry in new (Action Action,string Alias)[]{
  (()=>AccessibleFootball2026.Audio.SoundEffectsPlayer.PlayMenuMove(),"ui_move"),
  (()=>AccessibleFootball2026.Audio.SoundEffectsPlayer.PlayMenuSelect(),"ui_select"),
  (()=>AccessibleFootball2026.Audio.SoundEffectsPlayer.PlayPassKick(),"pass_kick"),
  (()=>AccessibleFootball2026.Audio.SoundEffectsPlayer.PlayShotKick(),"shot_kick"),
  (()=>AccessibleFootball2026.Audio.SoundEffectsPlayer.PlayCrossKick(),"cross_kick"),
  (()=>AccessibleFootball2026.Audio.SoundEffectsPlayer.PlayWhistle(),"whistle")})
 {
  commands.Clear();entry.Action();Check(commands.Contains("play "+entry.Alias+" from 0"),"Staged runtime route cannot play: "+entry.Alias);
 }
 commands.Clear();Check(AccessibleFootball2026.Audio.GoalBeaconPlayer.PlayBeaconPulse(1,0)&&commands.Contains("play beacon_ch from 0"),"Staged GoalBeacon route cannot play");
 AccessibleFootball2026.Audio.SoundEffectsPlayer.SetMatchAudioContext(true);
 commands.Clear();Check(AccessibleFootball2026.Audio.SoundEffectsPlayer.PlayMatchLineupMusic(),"Intentional staged lineup music missing");
 AccessibleFootball2026.Audio.SoundEffectsPlayer.StopMatchLineupMusic();
 Check(commands.Contains("stop lineup_music")&&commands.Contains("close lineup_music"),"Lineup music must close at kickoff");
 commands.Clear();AccessibleFootball2026.Audio.SoundEffectsPlayer.StartMenuMusic();MatchSettings.Current.MusicVolumePercent=70;MatchSettings.Current.ApplyLive();
 Check(!commands.Any(c=>c.StartsWith("play bgm_menu")),"Menu music must not start during live match context");
 AccessibleFootball2026.Audio.SoundEffectsPlayer.StopAll();
 hook.SetValue(null,null);
 Console.WriteLine("PASS relocated payload: 9 cue, Move/Select, pass/shot/cross/whistle, GoalBeacon, lineup closure and match music exclusion routes. Recorded transport, NOT native audible output.");
}
