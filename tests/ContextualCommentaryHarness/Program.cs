using System.Reflection;
using System.Numerics;
using System.Text.Json;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Commentary;
using AccessibleFootball2026.Core.Input;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;
using AccessibleFootball2026.Audio.Commentary;

int passed = 0, failed = 0;
void Check(bool ok, string name) { Console.WriteLine((ok ? "PASS " : "FAIL ") + name); if (ok) passed++; else failed++; }
var empty = CommentaryContext.Empty;
var c = empty.With(("score", 3), ("flag", false));
Check(!empty.TryGet("score", out _), "With does not mutate the original snapshot");
foreach (var op in new[] { "eq", "ne", "gt", "ge", "lt", "le" })
    Check(!new ContextualCommentaryCatalog.Requirement { Fact = "missing", Op = op, Value = "0" }.Matches(c), "Missing fact fails closed: " + op);
Check(new ContextualCommentaryCatalog.Requirement { Fact = "flag", Value = "false" }.Matches(c), "Known false is not unknown");
Check(new ContextualCommentaryCatalog.Requirement { Fact = "score", Op = "ge", Value = "3" }.Matches(c), "Invariant numeric comparison");
Check(!new ContextualCommentaryCatalog.Requirement { Fact = "score", Op = "ge", Value = "NaN" }.Matches(c), "Nonfinite conditions rejected");
string root = Path.Combine(Path.GetTempPath(), "commentary-test-" + Guid.NewGuid());
Directory.CreateDirectory(root); File.WriteAllBytes(Path.Combine(root, "sample.mp3"), new byte[] {1});
void Catalog(params ContextualCommentaryCatalog.Entry[] entries) => File.WriteAllText(Path.Combine(root, "commentary_context.json"), JsonSerializer.Serialize(new ContextualCommentaryCatalog.Document { Version = 1, Clips = entries }));
ContextualCommentaryCatalog.Entry Clip(string id, string fact = "", string value = "") => new() { Id = id, File = "sample.mp3", Event = "Goal", Transcript = "test fixture", Review = "verified", All = fact == "" ? Array.Empty<ContextualCommentaryCatalog.Requirement>() : new[] { new ContextualCommentaryCatalog.Requirement { Fact = fact, Value = value } } };
Catalog(Clip("generic"), Clip("third", "goal.teamOrdinal", "3"));
var cat = new ContextualCommentaryCatalog(root);
Check(cat.IsValid, "Valid catalog loads");
Check(cat.Eligible("Goal", empty.With(("goal.teamOrdinal", 1))).Single().Id == "generic", "Third-goal line never selected on first goal");
Check(cat.Eligible("Goal", empty.With(("goal.teamOrdinal", 3))).Count==2, "Specific true condition does not discard a safe generic take");
Check(cat.Choose("Goal",empty.With(("goal.teamOrdinal",3)))!=null,"True specific context selects a recording");
Check(cat.Eligible("Save", c).Count == 0, "Different event cannot select goal clip");
Catalog(Clip("equalizer", "goal.equalizer", "true")); cat = new(root);
Check(cat.Eligible("Goal", empty.With(("goal.equalizer", false))).Count == 0, "Equalizer rejected for lead goal");
Check(cat.Eligible("Goal", empty.With(("goal.equalizer", true))).Count == 1, "Equalizer accepted for actual equalizer");
Catalog(Clip("red", "card.kind", "secondYellow")); cat = new(root);
Check(cat.Eligible("Goal", empty.With(("card.kind", "directRed"))).Count == 0, "Second-yellow explanation rejected for direct red");
var bad = Clip("unsafe");bad.File = "../sample.mp3";Catalog(bad);Check(!new ContextualCommentaryCatalog(root).IsValid, "Traversal invalidates catalog");
bad.File = "/tmp/sample.mp3";Catalog(bad);Check(!new ContextualCommentaryCatalog(root).IsValid, "Absolute path invalidates catalog");
bad.File = "sample.mp3";bad.Review = "unverified";Catalog(bad);Check(!new ContextualCommentaryCatalog(root).IsValid, "Unreviewed transcript cannot become active");
Catalog(Clip("dup"), Clip("dup"));Check(!new ContextualCommentaryCatalog(root).IsValid, "Duplicate clip IDs rejected");
File.WriteAllText(Path.Combine(root,"commentary_context.json"),"broken");Check(!new ContextualCommentaryCatalog(root).IsValid,"Malformed catalog fails closed");

Team Team(string id, TeamSide side) { var t = new Team(id, id, side); for(int i=0;i<11;i++) t.Players.Add(new Player(i + (side == TeamSide.Home ? 100 : 200), "P"+i, i+1, i==0 ? SpecificPosition.GK : SpecificPosition.ST,75,75,75,75,75));return t; }
var home=Team("h",TeamSide.Home);var away=Team("a",TeamSide.Away);var bus=new MatchEventBus();var events=new List<MatchEvent>();bus.OnMatchEvent += events.Add;
var match=new MatchEngine(home,away,bus,new Input());
void Goal() { match.Ball.Position = new Vector3(Pitch.Length+.1f,Pitch.Width/2,1); typeof(MatchEngine).GetMethod("CheckRefereeRules",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(match,null); }
Goal(); var goal=events.Last(e=>e.Type==MatchEventType.GoalScored);
Check(match.LastGoalReplay!=null && match.LastGoalReplay.Frames.Count>0,"Actual goal freezes sampled ball trajectory");
var replay=match.LastGoalReplay!;
Check(goal.Context.Facts["goal.teamOrdinal"]=="1","Actual goal captures team ordinal after score increment");
Check(goal.Context.Facts["goal.playerOrdinal"]=="1","Scorer count captured before goal dispatch");
Check(goal.Context.Facts["goal.takesLead"]=="true","First goal takes lead");
Check(goal.Context.Facts["goal.equalizer"]=="false","First goal is not equalizer");
home.Score=2;away.Score=3;Goal();var eq=events.Last(e=>e.Type==MatchEventType.GoalScored);
Check(replay.Context.Facts["score.home"]=="1","Old goal replay retains original score snapshot");
Check(replay.Context.Facts["replay.available"]=="true","Replay provenance explicitly available");
Check(eq.Context.Facts["goal.equalizer"]=="true","Engine-generated equalizer has true fact");
Check(goal.Context.Facts["score.home"]=="1","Earlier queued event retains old score snapshot");
var player=home.Players[1];match.IssueCard(player,false);Check(events.Last().Context.Facts["card.kind"]=="yellow","Actual yellow-card kind");
match.IssueCard(player,false);Check(events.Last().Context.Facts["card.kind"]=="secondYellow","Second yellow has distinct context");
match.IssueCard(home.Players[2],true);Check(events.Last().Context.Facts["card.kind"]=="directRed","Direct red remains distinct");

var before=ShootoutCommentary.BeforeKick(empty,true,0,0,0,0);
Check(before.Facts["shootout.firstKick"]=="true", "Shootout actual first kick");
Check(before.Facts["shootout.winsIfScored"]=="false", "First kick cannot decide shootout");
before=ShootoutCommentary.BeforeKick(empty,true,4,3,4,4);
Check(before.Facts["shootout.winsIfScored"]=="true", "Fifth kick can clinch shootout");
var after=ShootoutCommentary.AfterKick(before,true,true,false,5,3,5,4);
Check(after.Facts["shootout.winningGoal"]=="true", "Winning penalty goal tagged only after actual score");
before=ShootoutCommentary.BeforeKick(empty,false,4,3,5,4);
Check(before.Facts["shootout.losesIfMissed"]=="true", "Last equalizing penalty is must-score");
after=ShootoutCommentary.AfterKick(before,false,false,true,4,3,5,5);
Check(after.Facts["shootout.decisiveSave"]=="true", "Actual decisive save distinguishes ordinary save");
after=ShootoutCommentary.AfterKick(before,false,false,true,1,0,1,1);
Check(after.Facts["shootout.decisiveSave"]=="false", "Early save cannot claim match decided");
before=ShootoutCommentary.BeforeKick(empty,true,5,5,5,5);
Check(before.Facts["shootout.winsIfScored"]=="false", "First kick in sudden-death pair does not clinch");
before=ShootoutCommentary.BeforeKick(empty,false,5,5,6,5);
Check(before.Facts["shootout.winsIfScored"]=="true", "Reply after sudden-death miss can clinch");
File.WriteAllBytes(Path.Combine(root,"second.mp3"),new byte[]{2});
var first=Clip("takeA");var second=Clip("takeB");second.File="second.mp3";
Catalog(first,second);cat=new(root);string? previous=null;var seen=new HashSet<string>();
for(int i=0;i<20;i++){var path=cat.Choose("Goal",empty);Check(path!=previous,"Two eligible performances alternate: "+i);if(path!=null)seen.Add(path);previous=path;}
Check(seen.Count==2,"Both distinct same-event recordings remain available");
// Optional complete-corpus tests use the staged artifact and real decoded clip paths.
if (args.Length>0)
{
 var corpusRoot=args[0];var document=JsonSerializer.Deserialize<ContextualCommentaryCatalog.Document>(File.ReadAllText(Path.Combine(corpusRoot,"commentary_context.json")),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
 var corpus=new ContextualCommentaryCatalog(corpusRoot);Check(corpus.IsValid,"Complete candidate corpus validates structure and source paths: "+corpus.Error);
 var library=new CommentaryLibrary(corpusRoot);
 Check(library.Error==null&&library.Recordings.Count==936,"Full library preserves all936 distinct original byte streams");
 var slots=library.Recordings.SelectMany(r=>r.OriginalSlots).ToArray();
 Check(slots.Length==4918&&slots.Distinct().Count()==4918,"All4918 original slots accounted exactly once");
 Check(library.Recordings.Count(r=>r.ReviewStatus=="digital_zero")==1&&library.Recordings.Count(r=>r.ReviewStatus=="nonzero_fragment")==2,"Stereo silent encoding and two nonzero fragments remain separate from mono commentary");
 Check(!document.Clips.Any(c=>library.Recordings.Single(r=>r.File==c.File).ReviewStatus!="reviewed"),"Review holds and promos cannot leak into automatic selection");
 var coachPraise=document.Clips.Single(c=>c.Id=="04487");
 Check(coachPraise.Event=="CoachPraise"&&coachPraise.All.All(r=>r.Matches(empty.With(("coach.response","teamGoalPraise"),("goal.assisted",true),("goal.own",false))))&&!coachPraise.All.All(r=>r.Matches(empty.With(("coach.response","teamGoalPraise"),("goal.assisted",false),("goal.own",false)))),"Direct coach praise requires actual assisted team-goal response, not a generic goal or inferred mood");
 var wideCoach=document.Clips.Single(c=>c.Id=="01032");
 Check(wideCoach.Event=="CoachWideInstruction"&&wideCoach.All.All(r=>r.Matches(empty.With(("coach.instruction","wideHoldThenRecover"),("tactics.focus","Wings"),("wide.discipline.active",true))))&&!wideCoach.All.All(r=>r.Matches(empty.With(("coach.instruction","wideHoldThenRecover"),("tactics.focus","Central"),("wide.discipline.active",true))))&&!wideCoach.All.All(r=>r.Matches(empty.With(("analysis.coachInstructionSpoken",true)))),"Wide touchline recording requires the real wide instruction, Wings focus and active mechanic");
 var reviewHolds=new[]{"00520","01201","01616","02009","04397","04431","04446","04665","04721"};
 Check(!document.Clips.Any(c=>reviewHolds.Contains(c.Id.Split('-')[0])),"Independent-review unknown words, names and historical cause cannot leak into automatic matches");
 var ballContinues=document.Clips.Single(c=>c.Id=="04472");
 Check(ballContinues.Review=="verified"&&ballContinues.Transcript.Contains("مستمرة"),"User-confirmed exact words preserved for continuing-ball line");
 Check(ballContinues.Event=="PossessionPlay"&&ballContinues.All.All(r=>r.Matches(empty.With(("attack.continues","true"),("analysis.hasPossession","true"),("possession.seconds","7")))),"User-verified continuing-ball line plays during actual sustained live possession");
 Check(!ballContinues.All.All(r=>r.Matches(empty.With(("attack.continues","true"),("analysis.hasPossession","false"),("possession.seconds","7")))),"Continuing-ball line rejected when possession lost (stoppage/turnover)");
 Check(!ballContinues.All.All(r=>r.Matches(empty.With(("attack.continues","true"),("analysis.hasPossession","true"),("possession.seconds","2")))),"Continuing-ball line rejected on a fresh touch, not yet still-continuing play");
 Check(library.Recordings.Single(r=>r.Slot=="01616").ReviewStatus=="context_history_pending","Genuine historical speech remains historical, not a current Ronaldo goal");
 var ovation=document.Clips.Where(c=>c.Id is "01643" or "01644" or "01647").ToArray();
 Check(ovation.Length==3,"Three genuine ovation variants preserved for high-rated substitution exits");
 foreach(var ov in ovation)
 {
  Check(ov.Event=="SubstitutionMade"&&ov.All.All(r=>r.Matches(empty.With(("substitution.playerRating","8.4"),("applause.played","true")))),ov.Id+" ovation line plays only after real applause started for a high-rated exit");
  Check(!ov.All.All(r=>r.Matches(empty.With(("substitution.playerRating","8.4"),("applause.played","false")))),ov.Id+" ovation line rejected when the genuine applause recording did not actually start");
  Check(!ov.All.All(r=>r.Matches(empty.With(("substitution.playerRating","7.9"),("applause.played","true")))),ov.Id+" ovation line rejected for an ordinary-rated exit");
  Check(!ov.All.All(r=>r.Matches(empty.With(("substitution.playerRating","8.4")))),ov.Id+" ovation line rejected with applause state unknown (fail closed)");
 }
 var applauseAsset=Path.GetFullPath(Path.Combine(corpusRoot,"..","..","SFX","Crowd","CrowdApplause.wav"));
 Check(File.Exists(applauseAsset)&&new FileInfo(applauseAsset).Length>100000,"Genuine CC0 stadium applause recording is bundled, not a procedural substitute");
 var applauseLicense=Path.GetFullPath(Path.Combine(corpusRoot,"..","..","SFX","Crowd","CrowdApplause-LICENSE.txt"));
 Check(File.Exists(applauseLicense)&&File.ReadAllText(applauseLicense).Contains("CC0"),"Applause recording carries its license and provenance");
 var coachJoy=document.Clips.Single(c=>c.Id=="00010");
 Check(coachJoy.Event=="GoalAnalysis"&&coachJoy.All.All(r=>r.Matches(empty.With(("replay.available","true"),("goal.takesLead","true"),("coach.reaction","joyfulLead")))),"Coach-joy replay line plays for modeled joy after an actual lead-taking goal");
 Check(!coachJoy.All.All(r=>r.Matches(empty.With(("replay.available","true"),("goal.takesLead","true"),("coach.reaction","dejected")))),"Coach-joy line rejected under a different modeled reaction");
 Check(!coachJoy.All.All(r=>r.Matches(empty.With(("replay.available","true"),("goal.takesLead","true")))),"Coach-joy line rejected when no reaction state is represented");
 var anxiousMiss=document.Clips.Single(c=>c.Id=="00033");
 Check(anxiousMiss.Event=="Miss"&&anxiousMiss.All.All(r=>r.Matches(empty.With(("shot.underPressure","true"),("player.reaction","anxious")))),"Anxious-miss line plays after an actual pressured miss with modeled anxiety");
 Check(!anxiousMiss.All.All(r=>r.Matches(empty.With(("shot.underPressure","false"),("player.reaction","anxious"))))&&!anxiousMiss.All.All(r=>r.Matches(empty.With(("shot.underPressure","true")))),"Anxious-miss line rejected without real pressure or without the modeled state");
 var dejected=document.Clips.Single(c=>c.Id=="01107");
 Check(dejected.Event=="MatchAnalysis"&&dejected.All.All(r=>r.Matches(empty.With(("analysis.result","loss"),("score.margin","3"),("coach.reaction","dejected")))),"Coach-dejection analysis plays for modeled dejection at a real 2+ deficit");
 Check(!dejected.All.All(r=>r.Matches(empty.With(("analysis.result","loss"),("score.margin","3"),("coach.reaction","joyfulLead"))))&&!dejected.All.All(r=>r.Matches(empty.With(("analysis.result","loss"),("score.margin","1"),("coach.reaction","dejected")))),"Coach-dejection analysis rejected under wrong reaction or one-goal margin");
 var displeased=document.Clips.Single(c=>c.Id=="01445");
 Check(displeased.Event=="Goal"&&displeased.All.All(r=>r.Matches(empty.With(("goal.equalizer","true"),("coach.opponent.reaction","displeasedConceded")))),"Equalizer line with conceding-coach displeasure plays only for the modeled conceding reaction");
 Check(!displeased.All.All(r=>r.Matches(empty.With(("goal.equalizer","true"),("coach.opponent.reaction","joyfulLead"))))&&!displeased.All.All(r=>r.Matches(empty.With(("goal.equalizer","false"),("coach.opponent.reaction","displeasedConceded")))),"Equalizer displeasure line rejected for non-equalizer or wrong modeled reaction");
 var coachSad=document.Clips.Single(c=>c.Id=="01492");
 Check(coachSad.Event=="MatchAnalysis"&&coachSad.All.All(r=>r.Matches(empty.With(("analysis.result","loss"),("score.margin","3"),("coach.reaction","sad")))),"Coach-sadness analysis plays for modeled sadness at a real 3+ deficit");
 Check(!coachSad.All.All(r=>r.Matches(empty.With(("analysis.result","loss"),("score.margin","2"),("coach.reaction","sad")))),"Coach-sadness analysis rejected below the real 3+ margin");
 foreach(var ownId in new[]{"01629","02959"})
 {
  var own=document.Clips.Single(c=>c.Id==ownId);
  Check(own.Event=="OwnGoal"&&own.All.All(r=>r.Matches(empty.With(("player.reaction","devastated")))),ownId+" own-goal sadness line plays only for the modeled devastated scorer");
  Check(!own.All.All(r=>r.Matches(empty.With(("player.reaction","anxious")))),ownId+" own-goal sadness line rejected under a different modeled emotion");
 }
 var easySave=document.Clips.Single(c=>c.Id=="03245");
 Check(!easySave.All.All(r=>r.Matches(empty.With(("save.difficulty","hard"))))&&easySave.All.All(r=>r.Matches(empty.With(("save.difficulty","easy")))),"Independent easy-save meaning excludes hard saves");
 var farPost=document.Clips.Single(c=>c.Id=="00372");
 Check(farPost.All.All(r=>r.Matches(empty.With(("action.kind","cross"),("cross.target","farPost"),("cross.height","ground"))))&&!farPost.All.All(r=>r.Matches(empty.With(("action.kind","cross"),("cross.target","farPost"),("cross.height","high")))),"Independent ground far-post delivery excludes aerial cross");
 var setPiecePass=document.Clips.Single(c=>c.Id=="03971");
 Check(setPiecePass.Event=="FreeKickTaken"&&setPiecePass.All.All(r=>r.Matches(empty.With(("action.kind","pass"))))&&!setPiecePass.All.All(r=>r.Matches(empty.With(("action.kind","shot")))),"Independent set-piece pass cannot trigger for a shot");
 int positive=0,missing=0,negative=0;
 foreach(var entry in document.Clips)
 {
  var facts=CommentaryContext.Empty;
  foreach(var req in entry.All)
  {
   string value=req.Value;
   if(req.Op=="ne") value="__other__";
   if(req.Op is "gt" or "lt") value=(double.Parse(req.Value,System.Globalization.CultureInfo.InvariantCulture)+(req.Op=="gt"?1:-1)).ToString(System.Globalization.CultureInfo.InvariantCulture);
   facts=facts.With((req.Fact,value));
  }
  if(entry.All.All(req=>req.Matches(facts)))positive++;
  foreach(var req in entry.All)
  {
   var without=new CommentaryContext(facts.Facts.Where(p=>p.Key!=req.Fact));
   if(!req.Matches(without))missing++;
   string wrong=req.Op switch {"eq"=>"__wrong__","ne"=>req.Value,"ge" or "gt"=>(double.Parse(req.Value,System.Globalization.CultureInfo.InvariantCulture)-1).ToString(System.Globalization.CultureInfo.InvariantCulture),_=>(double.Parse(req.Value,System.Globalization.CultureInfo.InvariantCulture)+1).ToString(System.Globalization.CultureInfo.InvariantCulture)};
   if(!req.Matches(facts.With((req.Fact,wrong))))negative++;
  }
 }
 int totalRequirements=document.Clips.Sum(c=>c.All.Length);
 Check(positive==document.Clips.Length,$"All {document.Clips.Length} candidate entries have satisfiable positive predicates");
 Check(missing==totalRequirements,$"All {totalRequirements} absent-fact mutations fail closed");
 Check(negative==totalRequirements,$"All {totalRequirements} conflicting-fact mutations reject the clip");
 Console.WriteLine("NOTE predicate tests are not proof every producer/mechanic or semantic mapping is complete.");
}

if(args.Length>1)
{
 using var db=JsonDocument.Parse(File.ReadAllText(args[1]));
 var names=db.RootElement.GetProperty("teams").EnumerateArray().SelectMany(t=>t.GetProperty("players").EnumerateArray()).Select(p=>p.GetProperty("name").GetString()!).ToHashSet(StringComparer.Ordinal);
 Check(names.Contains("Erling Haaland")&&names.Contains("Markus Haaland"),"Actual repository database supplies Haaland name collision fixture");
 var doc=JsonSerializer.Deserialize<ContextualCommentaryCatalog.Document>(File.ReadAllText(Path.Combine(args[0],"commentary_context.json")),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
 var call=doc.Clips.Where(c=>c.Id.StartsWith("02096")).ToArray();
 Check(call.Any(c=>c.All.All(r=>r.Matches(empty.With(("player.name","Erling Haaland"))))),"Actual Erling full-name recording matches source database player");
 Check(!call.Any(c=>c.All.All(r=>r.Matches(empty.With(("player.name","Markus Haaland"))))),"Actual Markus collision cannot inherit Erling recording");
 int exact=0;foreach(var entry in doc.Clips.Where(c=>c.All.Any(r=>r.Fact=="player.name")))
 {var req=entry.All.Single(r=>r.Fact=="player.name");if(names.Contains(req.Value))exact++;}
 Check(exact>0,$"Actual database matches {exact} exact named-recording predicates; absent names are not invented");
}
Directory.Delete(root,true);
Console.WriteLine($"RESULT {passed} passed, {failed} failed");return failed == 0 ? 0 : 1;
class Input : IInputHandler { public HashSet<GameAction> PollCurrentActions(bool p)=>new(); public bool IsActionTriggered(GameAction a,bool p)=>false; }
