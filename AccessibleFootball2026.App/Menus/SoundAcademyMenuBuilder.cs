using System;
using System.Collections.Generic;
using AccessibleFootball2026.Audio;
using AccessibleFootball2026.Audio.Anthems;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.App.Menus;

/// <summary>
/// منشئ شاشة تعلم أصوات اللعبة التفاعلية (Sound Academy Menu Builder):
/// يتيح استعراض وسماع نغمات التنبيه الـ 9، ومؤثرات المباراة الحقيقية، وأجواء المدرجات، ومنارة المرمى 3D، والأناشيد
/// مع تشغيل فوري للعينة الصوتية وشرح تكتيكي موجز بلغة الواجهة المعتمدة
/// </summary>
public static class SoundAcademyMenuBuilder
{
    public static AccessibleMenu CreateSoundAcademyMenu(
        IScreenReader screenReader,
        AnthemEngine anthemEngine,
        Action<AccessibleMenu> pushMenu,
        Action popMenu)
    {
        var lang = MatchSettings.Current.Language;
        string title = LocalizationService.GetString("SoundAcademyTitle");
        var menu = new AccessibleMenu(title, screenReader) { OnClosed = () => StopPreview(anthemEngine) };

        var categories = SoundAcademyDatabase.GetCategories();
        foreach (var category in categories)
        {
            var cat = category;
            string catTitle = lang == UiLanguage.English ? cat.EnglishTitle : cat.ArabicTitle;

            menu.AddItem(catTitle, () =>
            {
                var subMenu = CreateCategorySubMenu(cat, screenReader, anthemEngine, popMenu);
                pushMenu(subMenu);
            });
        }

        string backTitle = LocalizationService.GetString("BackToMainMenu");
        menu.AddItem(backTitle, () => popMenu());

        return menu;
    }

    private static AccessibleMenu CreateCategorySubMenu(
        SoundAcademyCategory category,
        IScreenReader screenReader,
        AnthemEngine anthemEngine,
        Action popMenu)
    {
        var lang = MatchSettings.Current.Language;
        string catTitle = lang == UiLanguage.English ? category.EnglishTitle : category.ArabicTitle;
        var menu = new AccessibleMenu(catTitle, screenReader) { OnClosed = () => StopPreview(anthemEngine) };

        var items = SoundAcademyDatabase.GetItemsByCategory(category.Id, id => GetAudioAction(id, anthemEngine));
        foreach (var soundItem in items)
        {
            var item = soundItem;
            string itemName = item.GetName(lang);
            string itemDesc = item.GetDescription(lang);

            menu.AddItem(itemName, () =>
            {
                // A new preview replaces the old one; loops never leak across categories.
                StopPreview(anthemEngine);
                bool played = TryPlayPreview(item.Id, anthemEngine);
                string status = played ? "" : LocalizationService.Text(" العينة غير متاحة أو تعذر تشغيلها بالمستوى الصوتي الحالي.", " Sample unavailable or could not play at the current volume.");
                screenReader.Speak($"{itemName}. {itemDesc}{status}", interrupt: true);
            }, description: itemDesc, suppressSelectionSound: true);
        }

        string backTitle = LocalizationService.GetString("Back");
        menu.AddItem(backTitle, () => popMenu());

        return menu;
    }

    private static void StopPreview(AnthemEngine anthemEngine)
    {
        anthemEngine.StopAllSounds();
        GoalBeaconPlayer.Stop();
        SoundEffectsPlayer.StopAll();
    }

    public static bool TryPlayPreview(string soundId, AnthemEngine anthemEngine) => soundId switch
    {
        "BeaconCenter" => GoalBeaconPlayer.PlayBeaconPulse(.85f, 0),
        "BeaconLeft" => GoalBeaconPlayer.PlayBeaconPulse(.85f, -.85f),
        "BeaconRight" => GoalBeaconPlayer.PlayBeaconPulse(.85f, .85f),
        "ChampionsLeagueAnthem" => anthemEngine.PlayChampionsLeagueSample(),
        "NationalAnthem" => anthemEngine.TryPlayNationalAnthemSample("EGY"),
        _ => SoundEffectsPlayer.TryPlayAcademySample(soundId)
    };

    public static Action GetAudioAction(string soundId, AnthemEngine anthemEngine) => () => TryPlayPreview(soundId, anthemEngine);
}
