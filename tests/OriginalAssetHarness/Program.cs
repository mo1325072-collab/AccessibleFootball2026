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
SoundEffectsPlayer.StopAll();transport.SetValue(null,null);
Console.WriteLine($"RESULT {passed} passed, {failed} failed. Real files, recorded MCI transport. No native playback or NVDA run.");return failed==0?0:1;

sealed class Reader:IScreenReader{public bool IsAvailable=>true;public void Speak(string s,bool interrupt=true){}public void CancelSpeech(){}}
