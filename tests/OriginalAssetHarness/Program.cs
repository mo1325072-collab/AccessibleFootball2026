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
Check(music.SequenceEqual(File.ReadAllBytes(Path.Combine(root,"Menu","MenuMusic.mp3"))),"initialization preserves exact Gemini menu music");
Check(SoundEffectsPlayer.SyntheticAssetsGenerated.Count==0,"complete original pack needs no generated match-impact fallback");
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
SoundEffectsPlayer.StopAll();transport.SetValue(null,null);
Console.WriteLine($"RESULT {passed} passed, {failed} failed. Real files, recorded MCI transport. No native playback or NVDA run.");return failed==0?0:1;
