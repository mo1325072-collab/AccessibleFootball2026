namespace AccessibleFootball2026.Core.Models;

/// <summary>
/// قواعد حسم ركلات الترجيح (IFAB Law 10 - Kicks from the Penalty Mark):
/// 5 ركلات لكل فريق، مع إنهاء مبكر فور استحالة لحاق الفريق المتأخر،
/// ثم الموت المفاجئ: ركلة مقابل ركلة حتى يتقدم أحد الفريقين بعد تساوي عدد الركلات.
/// </summary>
public static class PenaltyShootoutRules
{
    public const int RegulationRounds = 5;

    /// <summary>
    /// يحدد ما إذا كانت ركلات الترجيح قد حُسمت نهائياً.
    /// </summary>
    /// <param name="team1Score">أهداف الفريق الأول المسجلة في الترجيح</param>
    /// <param name="team2Score">أهداف الفريق الثاني المسجلة في الترجيح</param>
    /// <param name="team1Kicks">عدد الركلات التي نفذها الفريق الأول</param>
    /// <param name="team2Kicks">عدد الركلات التي نفذها الفريق الثاني</param>
    public static bool IsDecided(int team1Score, int team2Score, int team1Kicks, int team2Kicks)
    {
        if (team1Kicks < RegulationRounds || team2Kicks < RegulationRounds)
        {
            // داخل الركلات الخمس الأولى: إنهاء مبكر فقط عند استحالة اللحاق حسابياً
            int team1Remaining = RegulationRounds - team1Kicks;
            int team2Remaining = RegulationRounds - team2Kicks;
            if (team1Score > team2Score + team2Remaining) return true;
            if (team2Score > team1Score + team1Remaining) return true;
            return false;
        }

        // بعد اكتمال الركلات الخمس: الموت المفاجئ، ولا حسم إلا بعد تساوي عدد الركلات
        return team1Kicks == team2Kicks && team1Score != team2Score;
    }

    /// <summary>
    /// هل دخلت الركلات مرحلة الموت المفاجئ (تعادل بعد اكتمال الخمس ركلات للفريقين)؟
    /// </summary>
    public static bool IsSuddenDeath(int team1Score, int team2Score, int team1Kicks, int team2Kicks)
    {
        return team1Kicks >= RegulationRounds &&
               team2Kicks >= RegulationRounds &&
               team1Score == team2Score;
    }
}
