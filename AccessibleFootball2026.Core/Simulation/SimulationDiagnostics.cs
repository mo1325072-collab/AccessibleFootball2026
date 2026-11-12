using System.Numerics;
using AccessibleFootball2026.Core.Input;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Core.Simulation;

/// <summary>Small deterministic engine smoke suite, independent of Windows/audio startup.</summary>
public static class SimulationDiagnostics
{
    public static int Run(TextWriter output)
    {
        int failures = 0;
        void Check(bool condition, string name)
        {
            output.WriteLine($"{(condition ? "PASS" : "FAIL")} {name}");
            if (!condition) failures++;
        }
        Team Fixture(string id, TeamSide side)
        {
            var team = new Team(id, id, side);
            for (int i = 0; i < 11; i++)
                team.Players.Add(new Player((side == TeamSide.Home ? 0 : 100) + i, id + i, i + 1,
                    i == 0 ? SpecificPosition.GK : i < 5 ? SpecificPosition.CB : i < 8 ? SpecificPosition.CM : SpecificPosition.ST,
                    75, 75, 75, 75, 75));
            team.AssignDefaultRoles();
            return team;
        }
        var home = Fixture("Diagnostic home", TeamSide.Home);
        var away = Fixture("Diagnostic away", TeamSide.Away);
        var match = new MatchEngine(home, away, new MatchEventBus(), new IdleInput());
        match.SetStoppageMinutes(0, 0);
        match.SetMatchTimeMinutes(44.99f); match.Update(.2f);
        Check(match.State == MatchState.HalfTime && !match.UserAttackingRight, "First half ends at the interval and swaps attacking goal");
        float intervalTime = match.MatchTimeMinutes; match.Update(5);
        Check(match.MatchTimeMinutes == intervalTime, "Interval freezes engine time until explicit resume");
        match.ResumeMatch();
        Check(match.State == MatchState.InPlay && away.Players.Contains(match.Ball.Owner!), "Explicit resume starts second half with the correct kickoff team");
        match.AwardFoulRestart(home.Players[9], new Vector2(30, 34));
        intervalTime = match.MatchTimeMinutes; var position = match.Ball.Position; match.Update(5);
        Check(match.AwaitingStoppageConfirmation && match.MatchTimeMinutes == intervalTime && match.Ball.Position == position,
            "Foul restart is held without advancing clock or ball");
        Check(match.DismissStoppageReplay() && match.Restart != null && !match.AwaitingStoppageConfirmation,
            "Explicit incident confirmation retains the actual restart");
        Check(match.StatisticsFor(home).FreeKicks == 1 && match.StatisticsFor(away).FreeKicks == 0,
            "Restart statistics are measured for the awarded team");
        match = new MatchEngine(Fixture("Home 2", TeamSide.Home), Fixture("Away 2", TeamSide.Away), new MatchEventBus(), new IdleInput());
        match.SetStoppageMinutes(0, 0); match.TriggerHalfTime(); match.ResumeMatch();
        match.HomeTeam.Score = 1;
        match.SetMatchTimeMinutes(89.99f); match.Update(.2f);
        Check(match.State == MatchState.GameOver, "Second half reaches full time without freezing");
        output.WriteLine($"Simulation diagnostics: {failures} failure(s). No Windows UI, NVDA or playback was exercised.");
        return failures == 0 ? 0 : 1;
    }

    private sealed class IdleInput : IInputHandler
    {
        public HashSet<GameAction> PollCurrentActions(bool possession) => new();
        public bool IsActionTriggered(GameAction action, bool possession) => false;
    }
}
