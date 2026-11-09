using System;
using System.Collections.Generic;
using AccessibleFootball2026.Audio;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.App.Menus;

/// <summary>
/// منشئ شاشة دليل المستخدم التفاعلية (User Guide Menu Builder):
/// يوفر قوائم منظمة لكافة أزرار التحكم، قوانين التحكيم والتبديلات، الرادار والمنارة 3D، وركلات الترجيح
/// مع دعم ثنائي اللغة (عربي / إنجليزي) ونطق صوتي فوري وتوافقي 100% مع NVDA
/// </summary>
public static class UserGuideMenuBuilder
{
    public static AccessibleMenu CreateUserGuideMenu(
        IScreenReader screenReader,
        Action<AccessibleMenu> pushMenu,
        Action popMenu)
    {
        var lang = MatchSettings.Current.Language;
        string title = LocalizationService.GetString("UserGuideTitle");
        var menu = new AccessibleMenu(title, screenReader);

        var categories = UserGuideDatabase.GetCategories();
        foreach (var category in categories)
        {
            var cat = category;
            string catTitle = lang == UiLanguage.English ? cat.EnglishTitle : cat.ArabicTitle;

            menu.AddItem(catTitle, () =>
            {
                var subMenu = CreateCategorySubMenu(cat, screenReader, popMenu);
                pushMenu(subMenu);
            });
        }

        string backTitle = LocalizationService.GetString("BackToMainMenu");
        menu.AddItem(backTitle, () => popMenu());

        return menu;
    }

    private static AccessibleMenu CreateCategorySubMenu(
        UserGuideCategory category,
        IScreenReader screenReader,
        Action popMenu)
    {
        var lang = MatchSettings.Current.Language;
        string catTitle = lang == UiLanguage.English ? category.EnglishTitle : category.ArabicTitle;
        var menu = new AccessibleMenu(catTitle, screenReader);

        var items = UserGuideDatabase.GetItemsByCategory(category.Id);
        foreach (var guideItem in items)
        {
            var item = guideItem;
            string itemTitle = item.GetTitle(lang);
            string itemDesc = item.GetDescription(lang);

            menu.AddItem(itemTitle, () =>
            {
                screenReader.Speak($"{itemTitle}. {itemDesc}", interrupt: true);
            }, description: itemDesc);
        }

        string backTitle = LocalizationService.GetString("Back");
        menu.AddItem(backTitle, () => popMenu());

        return menu;
    }
}
