using System;
using System.Collections.Generic;
using System.Linq;
using AccessibleFootball2026.Audio;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.App.Menus;

/// <summary>
/// مدير اختيار الفرق بنظام المستويين (2-Tier Accessible Team Selection):
/// المستوى الأول: اختيار الفئة (الدوريات المحلية الكبرى، بطولات الأندية القارية، المنتخبات الوطنية حسب القارة، البطولات الدولية، أو كل الفرق).
/// المستوى الثاني: اختيار الفريق أو المنتخب داخل الفئة المختارة، مع تطبيق قاعدة منع تكرار نفس الفريق (No Mirror Match)
/// لضمان عدم وجود أي ارتباك صوتي للاعب الكفيف.
/// </summary>
public class TeamSelectionManager
{
    private readonly List<Team> _allTeams;
    private readonly IScreenReader _screenReader;
    private readonly Action<AccessibleMenu>? _pushMenu;
    private readonly Action? _popMenu;

    public Team? SelectedHomeTeam { get; private set; }
    public Team? SelectedAwayTeam { get; private set; }

    public TeamSelectionManager(IScreenReader screenReader, Action<AccessibleMenu>? pushMenu = null, Action? popMenu = null)
    {
        _screenReader = screenReader;
        _pushMenu = pushMenu;
        _popMenu = popMenu;
        _allTeams = TeamDatabase.GetAllAvailableTeams();
    }

    /// <summary>
    /// قائمة المستوى الأول لاختيار فئة فريق اللاعب الأساسي (Home Team)
    /// </summary>
    public AccessibleMenu CreateHomeTeamSelectMenu(Action<Team> onTeamSelected, Action<AccessibleMenu>? pushMenu = null, Action? popMenu = null)
    {
        var push = pushMenu ?? _pushMenu;
        var pop = popMenu ?? _popMenu;
        var categoryMenu = new AccessibleMenu("اختر فئة الفريق الأساسي", _screenReader);

        AddCategoriesToMenu(categoryMenu, (teams, categoryName) =>
        {
            var teamSubMenu = CreateSubTeamSelectMenu($"اختر فريقك من {categoryName}", teams, null, t =>
            {
                SelectedHomeTeam = t;
                SelectedHomeTeam.Side = TeamSide.Home;
                _screenReader.Speak($"تم اختيار {t.Name} كفريقك الأساسي");
                onTeamSelected(t);
            }, pop);

            if (push != null)
            {
                push(teamSubMenu);
            }
            else
            {
                teamSubMenu.Open();
            }
        });

        if (pop != null)
        {
            categoryMenu.AddItem("رجوع للقائمة الرئيسية", () => pop());
        }

        return categoryMenu;
    }

    /// <summary>
    /// قائمة المستوى الأول لاختيار فئة فريق الخصم المنافس (Away Team)
    /// مع استبعاد الفريق الأساسي من أي قائمة (No Mirror Match)
    /// </summary>
    public AccessibleMenu CreateAwayTeamSelectMenu(Action<Team> onOpponentSelected, Action<AccessibleMenu>? pushMenu = null, Action? popMenu = null)
    {
        var push = pushMenu ?? _pushMenu;
        var pop = popMenu ?? _popMenu;
        var categoryMenu = new AccessibleMenu("اختر فئة فريق الخصم المنافس", _screenReader);

        AddCategoriesToMenu(categoryMenu, (teams, categoryName) =>
        {
            var teamSubMenu = CreateSubTeamSelectMenu($"اختر الخصم من {categoryName}", teams, SelectedHomeTeam, t =>
            {
                SelectedAwayTeam = t;
                SelectedAwayTeam.Side = TeamSide.Away;
                _screenReader.Speak($"تم اختيار {t.Name} كمنافس، لتكون المباراة: {SelectedHomeTeam?.Name} ضد {SelectedAwayTeam.Name}");
                onOpponentSelected(t);
            }, pop);

            if (push != null)
            {
                push(teamSubMenu);
            }
            else
            {
                teamSubMenu.Open();
            }
        });

        if (pop != null)
        {
            categoryMenu.AddItem("رجوع", () => pop());
        }

        return categoryMenu;
    }

        private void AddCategoriesToMenu(AccessibleMenu menu, Action<List<Team>, string> onCategoryChosen)
    {
        // 1. الدوري المصري الممتاز (20 نادياً من قاعدة بيانات المستخدم)
        var egTeams = JsonDatabaseProvider.GetTeamsByGroup("Egyptian Premier League");
        if (egTeams.Count == 0) egTeams = EgyptianLeagueDatabase.GetAllClubs();
        menu.AddItem($"الدوري المصري الممتاز ({egTeams.Count} نادياً)", () => onCategoryChosen(egTeams, "الدوري المصري"));

        // 2. دوري روشن السعودي للمحترفين (18 نادياً)
        var saudiTeams = JsonDatabaseProvider.GetTeamsByGroup("Saudi Pro League");
        if (saudiTeams.Count == 0) saudiTeams = SaudiLeagueDatabase.GetAllClubs();
        menu.AddItem($"دوري روشن السعودي للمحترفين ({saudiTeams.Count} نادياً)", () => onCategoryChosen(saudiTeams, "دوري روشن السعودي"));

        // 3. الدوريات الأوروبية الكبرى
        var engTeams = JsonDatabaseProvider.GetTeamsByGroup("Premier League (England)");
        if (engTeams.Count == 0) engTeams = EnglishPremierLeagueDatabase.GetAllClubs();
        menu.AddItem($"الدوري الإنجليزي الممتاز ({engTeams.Count} نادياً)", () => onCategoryChosen(engTeams, "الدوري الإنجليزي"));

        var espTeams = JsonDatabaseProvider.GetTeamsByGroup("LaLiga (Spain)");
        if (espTeams.Count == 0) espTeams = SpanishLaLigaDatabase.GetAllClubs();
        menu.AddItem($"الدوري الإسباني - لا ليغا ({espTeams.Count} نادياً)", () => onCategoryChosen(espTeams, "الدوري الإسباني"));

        var itaTeams = JsonDatabaseProvider.GetTeamsByGroup("Serie A (Italy)");
        if (itaTeams.Count == 0) itaTeams = ItalianSerieADatabase.GetAllClubs();
        menu.AddItem($"الدوري الإيطالي - سيري آ ({itaTeams.Count} نادياً)", () => onCategoryChosen(itaTeams, "الدوري الإيطالي"));

        var gerTeams = JsonDatabaseProvider.GetTeamsByGroup("Bundesliga (Germany)");
        if (gerTeams.Count == 0) gerTeams = GermanBundesligaDatabase.GetAllClubs();
        menu.AddItem($"الدوري الألماني - بوندسليغا ({gerTeams.Count} نادياً)", () => onCategoryChosen(gerTeams, "الدوري الألماني"));

        var fraTeams = JsonDatabaseProvider.GetTeamsByGroup("Ligue 1 (France)");
        if (fraTeams.Count == 0) fraTeams = FrenchLigue1Database.GetAllClubs();
        menu.AddItem($"الدوري الفرنسي - ليغ 1 ({fraTeams.Count} نادياً)", () => onCategoryChosen(fraTeams, "الدوري الفرنسي"));

        // 4. بطولات الأندية الإفريقية (أندية الكاف من قاعدة بيانات المستخدم)
        var cafClClubs = JsonDatabaseProvider.GetTeamsByGroup("CAF Champions League (clubs outside Egyptian league)");
        if (cafClClubs.Count == 0) cafClClubs = CAFCompetitions.GetChampionsLeagueClubs();
        menu.AddItem($"دوري أبطال إفريقيا للأندية ({cafClClubs.Count} نادياً)", () => onCategoryChosen(cafClClubs, "دوري أبطال إفريقيا"));

        var cafConfClubs = JsonDatabaseProvider.GetTeamsByGroup("CAF Confederation Cup clubs");
        if (cafConfClubs.Count == 0) cafConfClubs = CAFCompetitions.GetConfederationCupClubs();
        menu.AddItem($"كأس الكونفدرالية الإفريقية للأندية ({cafConfClubs.Count} نادياً)", () => onCategoryChosen(cafConfClubs, "كأس الكونفدرالية الإفريقية"));

        // 5. بطولات الأندية الأوروبية
        var uclClubs = JsonDatabaseProvider.GetTeamsByGroup("UEFA Champions League (clubs outside the 7 leagues)");
        if (uclClubs.Count == 0) uclClubs = UEFACompetitions.GetChampionsLeagueClubs();
        menu.AddItem($"دوري أبطال أوروبا للأندية ({uclClubs.Count} نادياً)", () => onCategoryChosen(uclClubs, "دوري أبطال أوروبا"));

        var uelClubs = JsonDatabaseProvider.GetTeamsByGroup("UEFA Europa League (clubs outside the 7 leagues)");
        if (uelClubs.Count == 0) uelClubs = UEFACompetitions.GetEuropaLeagueClubs();
        menu.AddItem($"الدوري الأوروبي للأندية ({uelClubs.Count} نادياً)", () => onCategoryChosen(uelClubs, "الدوري الأوروبي"));

        var ueclClubs = JsonDatabaseProvider.GetTeamsByGroup("UEFA Conference League (clubs outside the 7 leagues)");
        menu.AddItem($"دوري المؤتمر الأوروبي للأندية ({ueclClubs.Count} نادياً)", () => onCategoryChosen(ueclClubs, "دوري المؤتمر الأوروبي"));

        // 6. المنتخبات الوطنية الرسمية (211 منتخباً)
        var nationalTeams = JsonDatabaseProvider.GetTeamsByGroup("National teams - current squads");
        if (nationalTeams.Count == 0) nationalTeams = JsonDatabaseProvider.GetAllNationalTeams();
        if (nationalTeams.Count == 0) nationalTeams = NationalTeamDatabase.GetAllNationalTeams();
        menu.AddItem($"المنتخبات الوطنية الرسمية ({nationalTeams.Count} منتخباً)", () => onCategoryChosen(nationalTeams, "المنتخبات الوطنية"));

        // 7. جميع الفرق (529 فريقاً موثقاً من ملف المستخدم)
        var all = JsonDatabaseProvider.AllTeams.Count > 0 ? JsonDatabaseProvider.AllTeams.ToList() : _allTeams;
        menu.AddItem($"جميع الأندية والمنتخبات ({all.Count} فريقاً)", () => onCategoryChosen(all, "جميع الفرق"));
    }

private AccessibleMenu CreateSubTeamSelectMenu(string title, List<Team> teams, Team? excludedTeam, Action<Team> onSelected, Action? popMenu = null)
    {
        var subMenu = new AccessibleMenu(title, _screenReader);

        foreach (var team in teams)
        {
            if (excludedTeam != null && team.Id.Equals(excludedTeam.Id, StringComparison.OrdinalIgnoreCase))
            {
                continue; // منع التكرار No Mirror Match
            }

            var t = team;
            subMenu.AddItem(t.Name, () => onSelected(t));
        }

        if (popMenu != null)
        {
            subMenu.AddItem("رجوع لقائمة الفئات", () => popMenu());
        }

        return subMenu;
    }
}

