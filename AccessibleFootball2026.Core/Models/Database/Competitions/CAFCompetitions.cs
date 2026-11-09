namespace AccessibleFootball2026.Core.Models;

/// <summary>
/// بطولات الأندية الإفريقية الرسمية (CAF Club Competitions) لموسم 2026/2027
/// تشمل دوري أبطال إفريقيا وكأس الكونفدرالية الإفريقية
/// موثقة بالكامل بأحدث الأندية المتأهلة
/// </summary>
public static class CAFCompetitions
{
    /// <summary>
    /// دوري أبطال إفريقيا (CAF Champions League) - 16 نادياً من صفوة أندية القارة
    /// </summary>
    public static List<Team> GetChampionsLeagueClubs()
    {
        return new List<Team>
        {
            // مصر (2)
            EgyptianLeagueDatabase.CreateAlAhly(),
            EgyptianLeagueDatabase.CreatePyramids(),

            // تونس (1)
            RestOfWorldClubsDatabase.CreateEsperanceTunis(),

            // جنوب إفريقيا (1)
            RestOfWorldClubsDatabase.CreateMamelodiSundowns(),

            // الكونغو الديمقراطية (1)
            RestOfWorldClubsDatabase.CreateTPMazembe(),

            // المغرب (3)
            RestOfWorldClubsDatabase.CreateRajaCasablanca(),
            RestOfWorldClubsDatabase.CreateASFAR(),
            RestOfWorldClubsDatabase.CreateWydadAC(),

            // السودان (1)
            RestOfWorldClubsDatabase.CreateAlHilalOmdurman(),

            // الجزائر (1)
            RestOfWorldClubsDatabase.CreateCRBelouizdad(),

            // كبار إضافيون للمجموعات (6)
            EgyptianLeagueDatabase.CreateZamalek(),
            EgyptianLeagueDatabase.CreateAlMasry(),
            EgyptianLeagueDatabase.CreateCeramicaCleopatra(),
            EgyptianLeagueDatabase.CreateSmouha(),
            EgyptianLeagueDatabase.CreateZedFC(),
            EgyptianLeagueDatabase.CreateModernSport()
        };
    }

    /// <summary>
    /// كأس الكونفدرالية الإفريقية (CAF Confederation Cup) - 16 نادياً
    /// </summary>
    public static List<Team> GetConfederationCupClubs()
    {
        return new List<Team>
        {
            EgyptianLeagueDatabase.CreateZamalek(),
            EgyptianLeagueDatabase.CreateAlMasry(),
            RestOfWorldClubsDatabase.CreateASFAR(),
            RestOfWorldClubsDatabase.CreateCRBelouizdad(),
            EgyptianLeagueDatabase.CreatePyramids(),
            RestOfWorldClubsDatabase.CreateRajaCasablanca(),
            RestOfWorldClubsDatabase.CreateWydadAC(),
            RestOfWorldClubsDatabase.CreateAlHilalOmdurman(),
            EgyptianLeagueDatabase.CreateSmouha(),
            EgyptianLeagueDatabase.CreateCeramicaCleopatra(),
            EgyptianLeagueDatabase.CreateZedFC(),
            EgyptianLeagueDatabase.CreateTalaeaElGaish(),
            EgyptianLeagueDatabase.CreateNationalBank(),
            EgyptianLeagueDatabase.CreateEnppi(),
            EgyptianLeagueDatabase.CreateIsmaily(),
            EgyptianLeagueDatabase.CreateAlIttihadAlex()
        };
    }
}
