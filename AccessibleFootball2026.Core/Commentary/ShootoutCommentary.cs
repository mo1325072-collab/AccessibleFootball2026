using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.Core.Commentary;

/// <summary>Facts from the actual penalty counters, never inferred from announcements.</summary>
public static class ShootoutCommentary
{
    public static CommentaryContext BeforeKick(CommentaryContext snapshot, bool homeKicking, int homeScore, int awayScore, int homeKicks, int awayKicks)
    {
        int kick = (homeKicking ? homeKicks : awayKicks) + 1;
        bool winsIfScored = homeKicking
            ? PenaltyShootoutRules.IsDecided(homeScore + 1, awayScore, homeKicks + 1, awayKicks) && homeScore + 1 > awayScore
            : PenaltyShootoutRules.IsDecided(homeScore, awayScore + 1, homeKicks, awayKicks + 1) && awayScore + 1 > homeScore;
        bool losesIfMissed = homeKicking
            ? PenaltyShootoutRules.IsDecided(homeScore, awayScore, homeKicks + 1, awayKicks) && homeScore < awayScore
            : PenaltyShootoutRules.IsDecided(homeScore, awayScore, homeKicks, awayKicks + 1) && awayScore < homeScore;
        return snapshot.With(("shootout.homeKicking", homeKicking), ("shootout.kickOrdinal", kick),
            ("shootout.firstKick", homeKicks + awayKicks == 0), ("shootout.winsIfScored", winsIfScored),
            ("shootout.losesIfMissed", losesIfMissed), ("shootout.homeScore", homeScore),
            ("shootout.awayScore", awayScore), ("shootout.homeKicks", homeKicks), ("shootout.awayKicks", awayKicks),
            ("shootout.suddenDeath", homeKicks >= 5 && awayKicks >= 5));
    }

    public static CommentaryContext AfterKick(CommentaryContext before, bool homeKicking, bool scored, bool saved, int homeScore, int awayScore, int homeKicks, int awayKicks, string? runUp=null, bool? keeperHeld=null)
    {
        bool decided = PenaltyShootoutRules.IsDecided(homeScore, awayScore, homeKicks, awayKicks);
        return before.With(("shootout.runUp",runUp),("shootout.keeperHeld",saved?keeperHeld:null),("shootout.scored", scored), ("shootout.saved", saved),
            ("shootout.decided", decided), ("shootout.homeScore", homeScore), ("shootout.awayScore", awayScore),
            ("shootout.homeKicks", homeKicks), ("shootout.awayKicks", awayKicks),
            ("shootout.winningGoal", decided && scored && (homeKicking ? homeScore > awayScore : awayScore > homeScore)),
            ("shootout.decisiveSave", decided && saved && (homeKicking ? awayScore > homeScore : homeScore > awayScore)),
            ("shootout.winner", decided ? (homeScore > awayScore ? "home" : "away") : "undecided"));
    }
}
