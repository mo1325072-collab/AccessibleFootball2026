namespace AccessibleFootball2026.Core.Models;

/// <summary>
/// بطولات الأندية الأوروبية الرسمية (UEFA Club Competitions) لموسم 2026/2027
/// تشمل دوري أبطال أوروبا (36 نادياً بنظام الدوري الموحد) والدوري الأوروبي
/// استبعاد قطعي وصارم لأي أندية من الكيان الصهيوني
/// </summary>
public static class UEFACompetitions
{
    /// <summary>
    /// دوري أبطال أوروبا (UEFA Champions League) - 36 نادياً بنظام المرحلة الموحدة
    /// نخبة الأندية الأوروبية من واقع كبرى الدوريات وأبطال القارة
    /// خالية تماماً 100% من أي أندية صهيونية
    /// </summary>
    public static List<Team> GetChampionsLeagueClubs()
    {
        return new List<Team>
        {
            // إنجلترا (4)
            EnglishPremierLeagueDatabase.CreateManchesterCity(),
            EnglishPremierLeagueDatabase.CreateArsenal(),
            EnglishPremierLeagueDatabase.CreateLiverpool(),
            EnglishPremierLeagueDatabase.CreateAstonVilla(),

            // إسبانيا (4)
            SpanishLaLigaDatabase.CreateRealMadrid(),
            SpanishLaLigaDatabase.CreateBarcelona(),
            SpanishLaLigaDatabase.CreateAtleticoMadrid(),
            SpanishLaLigaDatabase.CreateGirona(),

            // إيطاليا (5)
            ItalianSerieADatabase.CreateInterMilan(),
            ItalianSerieADatabase.CreateACMilan(),
            ItalianSerieADatabase.CreateJuventus(),
            ItalianSerieADatabase.CreateAtalanta(),
            ItalianSerieADatabase.CreateBologna(),

            // ألمانيا (5)
            GermanBundesligaDatabase.CreateBayerLeverkusen(),
            GermanBundesligaDatabase.CreateVfBStuttgart(),
            GermanBundesligaDatabase.CreateBayernMunich(),
            GermanBundesligaDatabase.CreateRBLeipzig(),
            GermanBundesligaDatabase.CreateBorussiaDortmund(),

            // فرنسا (4)
            FrenchLigue1Database.CreateParisSaintGermain(),
            FrenchLigue1Database.CreateMonaco(),
            FrenchLigue1Database.CreateBrest(),
            FrenchLigue1Database.CreateLille(),

            // البرتغال (2)
            RestOfWorldClubsDatabase.CreateSportingCP(),
            RestOfWorldClubsDatabase.CreateBenfica(),

            // هولندا (2)
            RestOfWorldClubsDatabase.CreatePSVEindhoven(),
            RestOfWorldClubsDatabase.CreateFeyenoord(),

            // اسكتلندا (1)
            RestOfWorldClubsDatabase.CreateCeltic(),

            // تركيا (1)
            RestOfWorldClubsDatabase.CreateGalatasaray(),

            // أندية أوروبية متأهلة إضافية (8)
            RestOfWorldClubsDatabase.CreatePorto(),
            RestOfWorldClubsDatabase.CreateAjax(),
            RestOfWorldClubsDatabase.CreateFenerbahce(),
            EnglishPremierLeagueDatabase.CreateChelsea(),
            SpanishLaLigaDatabase.CreateAthleticBilbao(),
            ItalianSerieADatabase.CreateASRoma(),
            GermanBundesligaDatabase.CreateEintrachtFrankfurt(),
            FrenchLigue1Database.CreateMarseille()
        };
    }

    /// <summary>
    /// الدوري الأوروبي (UEFA Europa League) - 32 نادياً أوروبياً
    /// خالية تماماً 100% من أي أندية صهيونية
    /// </summary>
    public static List<Team> GetEuropaLeagueClubs()
    {
        return new List<Team>
        {
            EnglishPremierLeagueDatabase.CreateManchesterUnited(),
            EnglishPremierLeagueDatabase.CreateTottenham(),
            SpanishLaLigaDatabase.CreateRealSociedad(),
            SpanishLaLigaDatabase.CreateRealBetis(),
            ItalianSerieADatabase.CreateLazio(),
            ItalianSerieADatabase.CreateFiorentina(),
            GermanBundesligaDatabase.CreateTSGHoffenheim(),
            GermanBundesligaDatabase.CreateSCFreiburg(),
            FrenchLigue1Database.CreateLyon(),
            FrenchLigue1Database.CreateNice(),
            FrenchLigue1Database.CreateLens(),
            RestOfWorldClubsDatabase.CreateRangers(),
            RestOfWorldClubsDatabase.CreatePorto(),
            RestOfWorldClubsDatabase.CreateAjax(),
            RestOfWorldClubsDatabase.CreateFenerbahce(),
            RestOfWorldClubsDatabase.CreateGalatasaray(),
            EnglishPremierLeagueDatabase.CreateNewcastleUnited(),
            EnglishPremierLeagueDatabase.CreateWestHam(),
            SpanishLaLigaDatabase.CreateVillarreal(),
            SpanishLaLigaDatabase.CreateSevilla(),
            ItalianSerieADatabase.CreateNapoli(),
            ItalianSerieADatabase.CreateTorino(),
            GermanBundesligaDatabase.CreateBorussiaMonchengladbach(),
            GermanBundesligaDatabase.CreateUnionBerlin(),
            FrenchLigue1Database.CreateRennes(),
            FrenchLigue1Database.CreateReims(),
            RestOfWorldClubsDatabase.CreateCeltic(),
            RestOfWorldClubsDatabase.CreateSportingCP(),
            RestOfWorldClubsDatabase.CreateBenfica(),
            RestOfWorldClubsDatabase.CreateFeyenoord(),
            RestOfWorldClubsDatabase.CreatePSVEindhoven(),
            EnglishPremierLeagueDatabase.CreateBrighton()
        };
    }
}
