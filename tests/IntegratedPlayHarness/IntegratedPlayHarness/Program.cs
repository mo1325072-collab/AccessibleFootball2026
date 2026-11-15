using System.Numerics;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Input;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;

int passed = 0, failed = 0;
void Check(bool c, string name) { if (c) { passed++; Console.WriteLine("PASS " + name); } else { failed++; Console.WriteLine("FAIL " + name); } }

void SeedRandom(int seed)
{
    var t = typeof(MatchEngine).Assembly.GetType("AccessibleFootball2026.Core.Simulation.SimulationRandom")!;
    var rng = new Random(seed);
    t.GetField("Override", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.SetValue(null, (Func<double>)(() => rng.NextDouble()));
}

Team Make(string id, TeamSide side, int bench = 0)
{
    var team = new Team(id, id, side);
    for (int i = 0; i < 11; i++) team.Players.Add(new Player((side == TeamSide.Home ? 0 : 100) + i, id + i, i + 1,
        i == 0 ? SpecificPosition.GK : i < 5 ? SpecificPosition.CB : i < 8 ? SpecificPosition.CM : SpecificPosition.ST, 75, 75, 75, 75, 75));
    for (int i = 0; i < bench; i++) team.Bench.Add(new Player(200 + i, id + "-bench" + i, 12 + i, SpecificPosition.CM, 75, 75, 75, 75, 75));
    team.AssignDefaultRoles();
    return team;
}

// ---------- Fixture A: unpinned production-AI full matches ----------
int s0 = args.Length > 0 ? int.Parse(args[0]) : 101;
foreach (var (dur, dt, label, seed) in new[] { (2, 0.2f, "A1 dt.2", s0), (1, 0.05f, "A2 dt.05", s0 + 1), (3, 0.2f, "A3 dt.2 long", s0 + 2) })
{
    SeedRandom(seed);
    MatchSettings.Current.Difficulty = args.Length > 1 ? Enum.Parse<DifficultyLevel>(args[1]) : DifficultyLevel.Normal;
    var home = Make("h" + label, TeamSide.Home); var away = Make("a" + label, TeamSide.Away);
    var bus = new MatchEventBus(); var events = new List<MatchEvent>(); bus.OnMatchEvent += events.Add;
    var bot = new Bot();
    var e = new MatchEngine(home, away, bus, bot); bot.E = e;
    e.SetStoppageMinutes(0, 0);
    var savedDuration = MatchSettings.Current.MatchDurationMinutes; MatchSettings.Current.MatchDurationMinutes = dur;
    int unclassifiedRun = 0, maxUnclassified = 0; string? firstUnclassified = null;
    int ticks = 0; var budget = (int)(dur * 2 * 60 / dt) + 20000;
    Exception? crash = null;
    // Shot lifecycle instrumentation: every ShotTaken must reach a classified end.
    var shotLives = new List<string>();
    (string team, Player shooter, int tick, Vector2 pos, float speed)? openShot = null;
    void CloseShot(string how) { if (openShot != null) { shotLives.Add($"{openShot.Value.team} shot@{openShot.Value.tick} from {openShot.Value.pos:F0} v={openShot.Value.speed:F0} -> {how} (tick {ticks})"); openShot = null; } }
    bus.OnMatchEvent += ev => {
        if (ticks >= 630 && ticks <= 665) Console.WriteLine($"    [trace t{ticks}] {ev.Type}: {ev.Description}");
        if (ev.Type == MatchEventType.ShotTaken && ev.SubjectPlayer != null)
        { CloseShot("replaced-by-new-shot"); var t = home.Players.Concat(home.Bench).Contains(ev.SubjectPlayer) ? "home" : "away"; openShot = (t, ev.SubjectPlayer, ticks, e.Ball.Position2D, e.Ball.Velocity.Length()); }
        else if (ev.Type is MatchEventType.GoalScored or MatchEventType.MissedChance or MatchEventType.GoalkeeperSave or MatchEventType.ShotBlocked or MatchEventType.WoodworkHit or MatchEventType.ShotAnnulled or MatchEventType.ShotInterrupted) CloseShot(ev.Type.ToString());
    };
    var shotFlagField = typeof(MatchEngine).GetField("_isShotInProgress", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
    bool prevShotFlag = false;
    try
    {
        while (e.State is not MatchState.GameOver and not MatchState.PenaltyShootout && ticks < budget)
        {
            ticks++;
            if (e.State == MatchState.HalfTime) e.ResumeMatch();
            if (e.AwaitingStoppageConfirmation || e.AnalysisReplayPending || e.State == MatchState.GoalCelebration) e.DismissStoppageReplay();
            if (e.PendingInjuryPlayer != null)
            {
                var inj = e.PendingInjuryPlayer;
                var sub = home.Bench.FirstOrDefault(home.CanEnterAsSubstitute);
                if (!e.ResolveInjurySubstitution(inj, sub)) { Console.WriteLine($"  [{label}] NOTE: unresolvable pending injury choice for {inj.Name} (offPitch={inj.IsOffPitch}) at tick {ticks}"); break; }
            }
            e.Update(dt);
            bool shotFlag = (bool)shotFlagField.GetValue(e)!;
            if (prevShotFlag && !shotFlag && openShot != null)
                Console.WriteLine($"  [{label}] SILENT-CLEAR tick {ticks} pos={e.Ball.Position2D:F0} z={e.Ball.Position.Z:F2} v={e.Ball.Velocity.Length():F1} owner={(e.Ball.Owner?.Name ?? "none")} restart={(e.Restart != null)} state={e.State} awaiting={e.AwaitingStoppageConfirmation}");
            prevShotFlag = shotFlag;
            if (openShot != null)
            {
                if (e.Ball.Owner != null && e.Ball.Owner != openShot.Value.shooter)
                    CloseShot($"collected-by {(e.Ball.Owner.Role == PlayerRole.Goalkeeper ? "GK" : "outfield")} {e.Ball.Owner.Name} at {e.Ball.Position2D:F0} speed {e.Ball.Velocity.Length():F1}");
                else if (e.Ball.Owner == null && e.Ball.Velocity.Length() < 1.0f)
                    CloseShot($"died-open-play at {e.Ball.Position2D:F0}");
                else if (ticks - openShot.Value.tick > 200) CloseShot($"timeout-200-ticks state owner={(e.Ball.Owner?.Name ?? "none")} v={e.Ball.Velocity.Length():F1}");
            }
            if (e.State == MatchState.InPlay && !e.AwaitingStoppageConfirmation && !e.AnalysisReplayPending && !e.VideoReviewPending
                && e.Ball.Owner == null && e.Ball.Velocity.Length() < 0.05f)
            {
                unclassifiedRun++;
                if (unclassifiedRun > maxUnclassified) { maxUnclassified = unclassifiedRun; firstUnclassified ??= $"tick {ticks} restart={(e.Restart != null)} pos={e.Ball.Position2D}"; }
            }
            else unclassifiedRun = 0;
        }
    }
    catch (Exception ex) { crash = ex; Console.WriteLine($"  [{label}] CRASH {ex}"); }
    MatchSettings.Current.MatchDurationMinutes = savedDuration;

    Check(crash == null, $"{label}: no exception ({crash?.GetType().Name} {crash?.Message})");
    Check(e.State is MatchState.GameOver or MatchState.PenaltyShootout, $"{label}: match completes (state={e.State}, ticks={ticks}, t={e.MatchTimeMinutes:F1})");
    var halfTimes = events.Count(x => x.Type == MatchEventType.HalfTime);
    var fullTimes = events.Count(x => x.Type == MatchEventType.FullTime);
    Check(halfTimes == 1, $"{label}: exactly one HalfTime event ({halfTimes})");
    Check(fullTimes == 1, $"{label}: exactly one FullTime event ({fullTimes})");
    var goals = events.Where(x => x.Type == MatchEventType.GoalScored && x.GoalData != null).Select(x => x.GoalData!).ToList();
    Check(home.Score == goals.Count(g => g.ScoringTeam == home) && away.Score == goals.Count(g => g.ScoringTeam == away),
        $"{label}: scoreboard == GoalScored event identities (h {home.Score}/{goals.Count(g => g.ScoringTeam == home)}, a {away.Score}/{goals.Count(g => g.ScoringTeam == away)})");
    if (goals.Count > 0)
        Check(goals[^1].HomeScore == home.Score && goals[^1].AwayScore == away.Score, $"{label}: last goal's reported scoreline == final score");
    var hs = e.StatisticsFor(home); var as_ = e.StatisticsFor(away);
    int homeNonOwnGoals = goals.Count(g => g.ScoringTeam == home && !g.IsOwnGoal);
    int awayNonOwnGoals = goals.Count(g => g.ScoringTeam == away && !g.IsOwnGoal);
    int homeDisallowed = events.Count(x => x.Type == MatchEventType.GoalDisallowed && x.SubjectPlayer != null && home.Players.Concat(home.Bench).Contains(x.SubjectPlayer));
    int awayDisallowed = events.Count(x => x.Type == MatchEventType.GoalDisallowed && x.SubjectPlayer != null && away.Players.Concat(away.Bench).Contains(x.SubjectPlayer));
    int homeWhistleCut = events.Count(x => x.Type == MatchEventType.ShotInterrupted && x.SubjectPlayer != null && home.Players.Concat(home.Bench).Contains(x.SubjectPlayer));
    int awayWhistleCut = events.Count(x => x.Type == MatchEventType.ShotInterrupted && x.SubjectPlayer != null && away.Players.Concat(away.Bench).Contains(x.SubjectPlayer));
    // On-target is only saves + non-own goals: annulled play and whistle-stopped attempts are NOT shots on target.
    Check(hs.ShotsOnTarget == as_.Saves + homeNonOwnGoals, $"{label}: home OnTarget ({hs.ShotsOnTarget}) == away saves ({as_.Saves}) + home non-own goals ({homeNonOwnGoals})");
    Check(as_.ShotsOnTarget == hs.Saves + awayNonOwnGoals, $"{label}: away OnTarget ({as_.ShotsOnTarget}) == home saves ({hs.Saves}) + away non-own goals ({awayNonOwnGoals})");
    // Annulled/interrupted buckets record their events truthfully.
    Check(hs.ShotsAnnulled <= events.Count(x => x.Type == MatchEventType.ShotAnnulled) && as_.ShotsAnnulled <= events.Count(x => x.Type == MatchEventType.ShotAnnulled), $"{label}: annulled buckets bounded by disallowed events (h {hs.ShotsAnnulled}/{homeDisallowed}, a {as_.ShotsAnnulled}/{awayDisallowed})");
    Check(hs.ShotsInterrupted == homeWhistleCut && as_.ShotsInterrupted == awayWhistleCut, $"{label}: interrupted buckets == whistle-stopped events (h {hs.ShotsInterrupted}/{homeWhistleCut}, a {as_.ShotsInterrupted}/{awayWhistleCut})");
    Console.WriteLine($"  [{label}] shot breakdown home: Shots={hs.Shots} OT={hs.ShotsOnTarget} Off={hs.ShotsOffTarget} Blocked={hs.BlockedShots} Wood={hs.Woodwork} Ann={hs.ShotsAnnulled} Cut={hs.ShotsInterrupted} | away: Shots={as_.Shots} OT={as_.ShotsOnTarget} Off={as_.ShotsOffTarget} Blocked={as_.BlockedShots} Wood={as_.Woodwork} Ann={as_.ShotsAnnulled} Cut={as_.ShotsInterrupted}");
    if (hs.Shots != hs.ShotsOnTarget + hs.ShotsOffTarget + hs.BlockedShots + hs.Woodwork || as_.Shots != as_.ShotsOnTarget + as_.ShotsOffTarget + as_.BlockedShots + as_.Woodwork)
    { Console.WriteLine($"  [{label}] LEAK-DUMP openShot={(openShot.HasValue ? openShot.Value.ToString() : "null")}"); foreach (var s in shotLives) Console.WriteLine("    " + s); }
    Check(hs.Shots == hs.ShotsOnTarget + hs.ShotsOffTarget + hs.BlockedShots + hs.Woodwork, $"{label}: home shots decompose exactly [shots={hs.Shots} on={hs.ShotsOnTarget} off={hs.ShotsOffTarget} blocked={hs.BlockedShots} wood={hs.Woodwork} annulled={hs.ShotsAnnulled} interrupted={hs.ShotsInterrupted}]");
    Check(as_.Shots == as_.ShotsOnTarget + as_.ShotsOffTarget + as_.BlockedShots + as_.Woodwork, $"{label}: away shots decompose exactly [shots={as_.Shots} on={as_.ShotsOnTarget} off={as_.ShotsOffTarget} blocked={as_.BlockedShots} wood={as_.Woodwork} annulled={as_.ShotsAnnulled} interrupted={as_.ShotsInterrupted}]");
    var foulEvents = events.Count(x => x.Type == MatchEventType.FoulCommitted);
    var advantageEvents = events.Count(x => x.Type == MatchEventType.Advantage);
    Check(foulEvents >= hs.Fouls + as_.Fouls, $"{label}: FoulCommitted events ({foulEvents}) >= stat fouls ({hs.Fouls + as_.Fouls}); delta {foulEvents - hs.Fouls - as_.Fouls} (advantage-recall republications, advantage events={advantageEvents})");
    var yellowEvents = events.Count(x => x.Type == MatchEventType.YellowCard || x.Type == MatchEventType.RedCard && x.Context.TryGet("card.kind", out var kind) && kind == "secondYellow"); var redEvents = events.Count(x => x.Type == MatchEventType.RedCard);
    Check(hs.YellowCards + as_.YellowCards == yellowEvents && hs.RedCards + as_.RedCards == redEvents, $"{label}: card stats == card events ({hs.YellowCards + as_.YellowCards}/{yellowEvents} Y, {hs.RedCards + as_.RedCards}/{redEvents} R)");
    Check(maxUnclassified <= 10, $"{label}: no dead-ball-in-open-play beyond 2s grace (max {maxUnclassified} ticks{(firstUnclassified != null ? ", first at " + firstUnclassified : "")})");
    Check(home.Players.Concat(away.Players).All(p => float.IsFinite(p.Position2D.X) && float.IsFinite(p.Position2D.Y)), $"{label}: all positions finite at end");
    Console.WriteLine($"  [{label}] t={e.MatchTimeMinutes:F1} score {home.Score}-{away.Score} events={events.Count} shots {hs.Shots}/{as_.Shots} fouls {hs.Fouls}/{as_.Fouls} ticks={ticks}");
    foreach (var life in shotLives.Where(l => l.StartsWith("away"))) Console.WriteLine($"  [{label}] LIFE {life}");
    if (openShot != null) Console.WriteLine($"  [{label}] LIFE away shot still open at whistle from {openShot.Value.pos:F0}");
    // trace events surrounding the first away ShotTaken
    int firstAwayShot = events.FindIndex(x => x.Type == MatchEventType.ShotTaken && x.SubjectPlayer != null && away.Players.Contains(x.SubjectPlayer));
    if (firstAwayShot >= 0)
    {
        Console.WriteLine($"  [{label}] TRACE around first away ShotTaken (event #{firstAwayShot}):");
        foreach (var ev in events.Skip(Math.Max(0, firstAwayShot - 4)).Take(10)) Console.WriteLine($"    {ev.Type}: {ev.Description} [subj={ev.SubjectPlayer?.Name}]");
    }
}

// ---------- Fixture B1a: forced severe injury WITH bench ----------
{
    var home = Make("b1h", TeamSide.Home, bench: 7); var away = Make("b1a", TeamSide.Away);
    var bus = new MatchEventBus(); var events = new List<MatchEvent>(); bus.OnMatchEvent += events.Add;
    var bot = new Bot(); var e = new MatchEngine(home, away, bus, bot); bot.E = e;
    e.SetStoppageMinutes(0, 0);
    var userPlayer = e.ActiveUserPlayer!;
    var subsBefore = home.SubstitutionsUsed;
    e.ApplyInjury(userPlayer, PlayerInjuryStatus.SevereInjury);
    Check(e.PendingInjuryPlayer == userPlayer, "B1a: severe injury to user player raises substitution choice");
    var sub = home.Bench.FirstOrDefault(home.CanEnterAsSubstitute);
    Check(sub != null, "B1a: bench offers an eligible substitute");
    Check(e.ResolveInjurySubstitution(userPlayer, sub!), "B1a: resolving with the substitute succeeds");
    Check(events.Any(x => x.Type == MatchEventType.SubstitutionMade), "B1a: SubstitutionMade event published");
    Check(e.ActiveUserPlayer == sub, "B1a: control reassigned to the substitute");
    Check(home.SubstitutionsUsed == subsBefore + 1, "B1a: substitution counted");
    Check(e.PendingInjuryPlayer == null, "B1a: no pending choice left after resolution");
}

// ---------- Fixture B1b: forced severe injury with NO eligible bench (ghost-choice regression) ----------
{
    var home = Make("b2h", TeamSide.Home); var away = Make("b2a", TeamSide.Away);
    var bus = new MatchEventBus(); var events = new List<MatchEvent>(); bus.OnMatchEvent += events.Add;
    var bot = new Bot(); var e = new MatchEngine(home, away, bus, bot); bot.E = e;
    e.SetStoppageMinutes(0, 0);
    var userPlayer = e.ActiveUserPlayer!;
    e.ApplyInjury(userPlayer, PlayerInjuryStatus.SevereInjury);
    Check(userPlayer.IsOffPitch, "B1b: severely injured user player leaves the pitch (no bench)");
    Check(e.PendingInjuryPlayer == userPlayer, "B1b: truthful continue-short acknowledgment stays pending");
    Check(e.ResolveInjurySubstitution(userPlayer, null) && e.PendingInjuryPlayer == null, "B1b: acknowledging clears the entry");
    var second = home.Players.First(p => p.IsActiveOnPitch && p.Role != PlayerRole.Goalkeeper);
    e.ApplyInjury(second, PlayerInjuryStatus.SevereInjury);
    Check(e.PendingInjuryPlayer == second, "B1b: later injury surfaces once no ghost blocks the queue");
}

// ---------- Fixture B2: held-stoppage action drain ----------
{
    var home = Make("dh", TeamSide.Home); var away = Make("da", TeamSide.Away);
    var bus = new MatchEventBus(); var events = new List<MatchEvent>(); bus.OnMatchEvent += events.Add;
    var bot = new Bot(); var e = new MatchEngine(home, away, bus, bot); bot.E = e;
    e.SetStoppageMinutes(0, 0);
    e.AwardFoulRestart(home.Players[9], new Vector2(30, 34));
    e.Update(0.2f);
    Check(!e.AwaitingStoppageConfirmation, "B2: plain free kick does NOT hold for confirmation (design: play runs)");
    typeof(MatchEngine).GetMethod("AwardCornerKick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(e, new object[] { true });
    Check(e.AwaitingStoppageConfirmation, "B2: corner kick IS held for confirmation");
    bot.ForceAttackActions = true;
    int before = events.Count(x => x.Type is MatchEventType.ShotTaken or MatchEventType.TackleAttempted);
    for (int i = 0; i < 10; i++) e.Update(0.2f);
    int leakedDuringHold = events.Count(x => x.Type is MatchEventType.ShotTaken or MatchEventType.TackleAttempted) - before;
    e.DismissStoppageReplay();
    bot.ForceAttackActions = false;
    for (int i = 0; i < 5; i++) e.Update(0.2f);
    Check(leakedDuringHold == 0, $"B2: actions queued during held restart never fire ({leakedDuringHold} leaked)");
    Check(e.Restart == null || !e.AwaitingStoppageConfirmation, "B2: dismissal releases the restart");
}

// ---------- Fixture B3: full-time accounting ----------
{
    var home = Make("fh", TeamSide.Home); var away = Make("fa", TeamSide.Away);
    var bus = new MatchEventBus(); var events = new List<MatchEvent>(); bus.OnMatchEvent += events.Add;
    var bot = new Bot(); var e = new MatchEngine(home, away, bus, bot); bot.E = e;
    e.SetStoppageMinutes(0, 0);
    e.TriggerHalfTime(); e.ResumeMatch();
    home.Score = 2; away.Score = 1;
    e.SetMatchTimeMinutes(89.99f); e.Update(0.2f);
    Check(e.State == MatchState.GameOver, "B3: second half reaches GameOver");
    Check(events.Any(x => x.Type == MatchEventType.FullTime), "B3: FullTime event published");
    var t = e.MatchTimeMinutes; e.Update(5f);
    Check(e.MatchTimeMinutes == t, "B3: clock frozen after GameOver");
    Check(e.GetStatisticsAnnouncements().Any(), "B3: statistics announcements available at full time");
}

Console.WriteLine($"== fullmatch-probe: {passed} passed, {failed} failed ==");
return failed == 0 ? 0 : 1;

sealed class Bot : IInputHandler
{
    public MatchEngine? E;
    public bool ForceAttackActions;
    private int _tick;
    public HashSet<GameAction> PollCurrentActions(bool possession)
    {
        var a = new HashSet<GameAction>();
        var e = E; if (e == null) return a;
        _tick++;
        if (ForceAttackActions) { a.Add(GameAction.Shoot); a.Add(GameAction.StandTackle); return a; }
        if (e.State != MatchState.InPlay || e.AwaitingStoppageConfirmation) return a;
        var me = e.ActiveUserPlayer; if (me == null) return a;
        Vector2 target;
        if (possession)
        {
            float gx = e.UserAttackingRight ? 105f : 0f;
            var goal = new Vector2(gx, 34f);
            target = goal;
            if (Vector2.Distance(me.Position2D, goal) < 16f) a.Add(GameAction.Shoot);
            else if (_tick % 90 == 0) a.Add(GameAction.ShortPass);
        }
        else
        {
            var carrier = e.Ball.Owner;
            target = carrier != null ? carrier.Position2D : e.Ball.Position2D;
            if (carrier != null && Vector2.Distance(me.Position2D, carrier.Position2D) < 0.9f) a.Add(GameAction.StandTackle);
        }
        var d = target - me.Position2D;
        if (d.X > 0.3f) a.Add(GameAction.MoveRight); else if (d.X < -0.3f) a.Add(GameAction.MoveLeft);
        if (d.Y > 0.3f) a.Add(GameAction.MoveDown); else if (d.Y < -0.3f) a.Add(GameAction.MoveUp);
        return a;
    }
    public bool IsActionTriggered(GameAction action, bool possession) => PollCurrentActions(possession).Contains(action);
}
