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
    private static string T(string ar, string en) => LocalizationService.Text(ar, en);
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
        var categoryMenu = new AccessibleMenu(T("اختر فئة الفريق الأساسي", "Your team category"), _screenReader);

        AddCategoriesToMenu(categoryMenu, (teams, categoryName) =>
        {
            var teamSubMenu = CreateSubTeamSelectMenu(T($"اختر فريقك من {categoryName}", $"Your team: {categoryName}"), teams, null, t =>
            {
                SelectedHomeTeam = t;
                SelectedHomeTeam.Side = TeamSide.Home;
                _screenReader.Speak(T($"تم اختيار {t.Name} كفريقك الأساسي", $"Your team: {t.Name}"));
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
        }, push, pop);

        if (pop != null)
        {
            categoryMenu.AddItem(LocalizationService.GetString("BackToMainMenu"), () => pop());
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
        var categoryMenu = new AccessibleMenu(T("اختر فئة فريق الخصم المنافس", "Opponent category"), _screenReader);

        AddCategoriesToMenu(categoryMenu, (teams, categoryName) =>
        {
            var teamSubMenu = CreateSubTeamSelectMenu(T($"اختر الخصم من {categoryName}", $"Opponent: {categoryName}"), teams, SelectedHomeTeam, t =>
            {
                SelectedAwayTeam = t;
                SelectedAwayTeam.Side = TeamSide.Away;
                _screenReader.Speak(T($"{SelectedHomeTeam?.Name} ضد {SelectedAwayTeam.Name}", $"{SelectedHomeTeam?.Name} versus {SelectedAwayTeam.Name}"));
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
        }, push, pop);

        if (pop != null)
        {
            categoryMenu.AddItem(LocalizationService.GetString("Back"), () => pop());
        }

        return categoryMenu;
    }

        private void AddCategoriesToMenu(AccessibleMenu menu, Action<List<Team>, string> onCategoryChosen, Action<AccessibleMenu>? push, Action? pop)
    {
        // 1. الدوري المصري الممتاز (20 نادياً من قاعدة بيانات المستخدم)
        var egTeams = JsonDatabaseProvider.GetTeamsByGroup("Egyptian Premier League");
        if (egTeams.Count == 0) egTeams = EgyptianLeagueDatabase.GetAllClubs();
        menu.AddItem(T($"الدوري المصري الممتاز ({egTeams.Count})", $"Egyptian Premier League ({egTeams.Count})"), () => onCategoryChosen(egTeams, T("الدوري المصري", "Egyptian Premier League")));

        // 2. دوري روشن السعودي للمحترفين (18 نادياً)
        var saudiTeams = JsonDatabaseProvider.GetTeamsByGroup("Saudi Pro League");
        if (saudiTeams.Count == 0) saudiTeams = SaudiLeagueDatabase.GetAllClubs();
        menu.AddItem(T($"دوري روشن السعودي للمحترفين ({saudiTeams.Count})", $"Saudi Pro League ({saudiTeams.Count})"), () => onCategoryChosen(saudiTeams, T("دوري روشن السعودي", "Saudi Pro League")));

        // 3. الدوريات الأوروبية الكبرى
        var engTeams = JsonDatabaseProvider.GetTeamsByGroup("Premier League (England)");
        if (engTeams.Count == 0) engTeams = EnglishPremierLeagueDatabase.GetAllClubs();
        menu.AddItem(T($"الدوري الإنجليزي الممتاز ({engTeams.Count})", $"Premier League ({engTeams.Count})"), () => onCategoryChosen(engTeams, T("الدوري الإنجليزي", "Premier League")));

        var espTeams = JsonDatabaseProvider.GetTeamsByGroup("LaLiga (Spain)");
        if (espTeams.Count == 0) espTeams = SpanishLaLigaDatabase.GetAllClubs();
        menu.AddItem(T($"الدوري الإسباني - لا ليغا ({espTeams.Count})", $"La Liga ({espTeams.Count})"), () => onCategoryChosen(espTeams, T("الدوري الإسباني", "La Liga")));

        var itaTeams = JsonDatabaseProvider.GetTeamsByGroup("Serie A (Italy)");
        if (itaTeams.Count == 0) itaTeams = ItalianSerieADatabase.GetAllClubs();
        menu.AddItem(T($"الدوري الإيطالي - سيري آ ({itaTeams.Count})", $"Serie A ({itaTeams.Count})"), () => onCategoryChosen(itaTeams, T("الدوري الإيطالي", "Serie A")));

        var gerTeams = JsonDatabaseProvider.GetTeamsByGroup("Bundesliga (Germany)");
        if (gerTeams.Count == 0) gerTeams = GermanBundesligaDatabase.GetAllClubs();
        menu.AddItem(T($"الدوري الألماني - بوندسليغا ({gerTeams.Count})", $"Bundesliga ({gerTeams.Count})"), () => onCategoryChosen(gerTeams, T("الدوري الألماني", "Bundesliga")));

        var fraTeams = JsonDatabaseProvider.GetTeamsByGroup("Ligue 1 (France)");
        if (fraTeams.Count == 0) fraTeams = FrenchLigue1Database.GetAllClubs();
        menu.AddItem(T($"الدوري الفرنسي - ليغ 1 ({fraTeams.Count})", $"Ligue 1 ({fraTeams.Count})"), () => onCategoryChosen(fraTeams, T("الدوري الفرنسي", "Ligue 1")));

        // 4. بطولات الأندية الإفريقية (أندية الكاف من قاعدة بيانات المستخدم)
        var cafClClubs = JsonDatabaseProvider.GetTeamsByGroup("CAF Champions League (clubs outside Egyptian league)");
        if (cafClClubs.Count == 0) cafClClubs = CAFCompetitions.GetChampionsLeagueClubs();
        menu.AddItem(T($"دوري أبطال إفريقيا للأندية ({cafClClubs.Count})", $"CAF Champions League ({cafClClubs.Count})"), () => onCategoryChosen(cafClClubs, T("دوري أبطال إفريقيا", "CAF Champions League")));

        var cafConfClubs = JsonDatabaseProvider.GetTeamsByGroup("CAF Confederation Cup clubs");
        if (cafConfClubs.Count == 0) cafConfClubs = CAFCompetitions.GetConfederationCupClubs();
        menu.AddItem(T($"كأس الكونفدرالية الإفريقية للأندية ({cafConfClubs.Count})", $"CAF Confederation Cup ({cafConfClubs.Count})"), () => onCategoryChosen(cafConfClubs, T("كأس الكونفدرالية الإفريقية", "CAF Confederation Cup")));

        // 5. بطولات الأندية الأوروبية
        var uclClubs = JsonDatabaseProvider.GetTeamsByGroup("UEFA Champions League (clubs outside the 7 leagues)");
        if (uclClubs.Count == 0) uclClubs = UEFACompetitions.GetChampionsLeagueClubs();
        menu.AddItem(T($"دوري أبطال أوروبا للأندية ({uclClubs.Count})", $"UEFA Champions League ({uclClubs.Count})"), () => onCategoryChosen(uclClubs, T("دوري أبطال أوروبا", "UEFA Champions League")));

        var uelClubs = JsonDatabaseProvider.GetTeamsByGroup("UEFA Europa League (clubs outside the 7 leagues)");
        if (uelClubs.Count == 0) uelClubs = UEFACompetitions.GetEuropaLeagueClubs();
        menu.AddItem(T($"الدوري الأوروبي للأندية ({uelClubs.Count})", $"UEFA Europa League ({uelClubs.Count})"), () => onCategoryChosen(uelClubs, T("الدوري الأوروبي", "UEFA Europa League")));

        var ueclClubs = JsonDatabaseProvider.GetTeamsByGroup("UEFA Conference League (clubs outside the 7 leagues)");
        menu.AddItem(T($"دوري المؤتمر الأوروبي للأندية ({ueclClubs.Count})", $"UEFA Conference League ({ueclClubs.Count})"), () => onCategoryChosen(ueclClubs, T("دوري المؤتمر الأوروبي", "UEFA Conference League")));

        // 6. المنتخبات الوطنية الرسمية (211 منتخباً)
        var nationalTeams = JsonDatabaseProvider.GetTeamsByGroup("National teams - current squads");
        if (nationalTeams.Count == 0) nationalTeams = JsonDatabaseProvider.GetAllNationalTeams();
        if (nationalTeams.Count == 0) nationalTeams = NationalTeamDatabase.GetAllNationalTeams();
        menu.AddItem(T($"المنتخبات الوطنية الرسمية ({nationalTeams.Count})", $"National teams ({nationalTeams.Count})"), () =>
        {
            if (push == null) { onCategoryChosen(nationalTeams, T("المنتخبات الوطنية", "National teams")); return; }
            var continentMenu = new AccessibleMenu(T("المنتخبات حسب القارة", "National teams by confederation"), _screenReader);
            foreach (var continent in Enum.GetValues<ContinentType>())
            {
                var selectedContinent = continent;
                var members = NationalTeamDatabase.GetNationalTeamsByContinent(selectedContinent);
                string title = NationalTeamConfederations.Title(selectedContinent, MatchSettings.Current.Language);
                continentMenu.AddItem($"{title} ({members.Count})", () => onCategoryChosen(members, title));
            }
            // Keep all source records available, including a future unclassified addition.
            continentMenu.AddItem(T("جميع المنتخبات", "All national teams"), () => onCategoryChosen(nationalTeams, T("المنتخبات الوطنية", "National teams")));
            if (pop != null) continentMenu.AddItem(LocalizationService.GetString("Back"), pop);
            push(continentMenu);
        });

        // 7. جميع الفرق (529 فريقاً موثقاً من ملف المستخدم)
        var all = JsonDatabaseProvider.AllTeams.Count > 0 ? JsonDatabaseProvider.AllTeams.ToList() : _allTeams;
        menu.AddItem(T($"جميع الأندية والمنتخبات ({all.Count})", $"All clubs and national teams ({all.Count})"), () => onCategoryChosen(all, T("جميع الفرق", "All teams")));
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
            subMenu.AddItem(T("رجوع لقائمة الفئات", "Back to categories"), () => popMenu());
        }

        return subMenu;
    }
}
