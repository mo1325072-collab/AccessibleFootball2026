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
        var menu = new AccessibleMenu(title, screenReader);

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
        var menu = new AccessibleMenu(catTitle, screenReader);

        var items = SoundAcademyDatabase.GetItemsByCategory(category.Id, id => GetAudioAction(id, anthemEngine));
        foreach (var soundItem in items)
        {
            var item = soundItem;
            string itemName = item.GetName(lang);
            string itemDesc = item.GetDescription(lang);

            menu.AddItem(itemName, () =>
            {
                // تشغيل العينة الصوتية فورياً
                item.PlayAudioAction?.Invoke();

                // نطق التفسير التكتيكي فورياً لقارئ الشاشة
                screenReader.Speak($"{itemName}. {itemDesc}", interrupt: true);
            }, description: itemDesc, suppressSelectionSound: true);
        }

        string backTitle = LocalizationService.GetString("Back");
        menu.AddItem(backTitle, () => popMenu());

        return menu;
    }

    public static Action GetAudioAction(string soundId, AnthemEngine anthemEngine) => soundId switch
    {
        "TackleWon" => () => SoundEffectsPlayer.PlayTackleWon(),
        "TackleMissed" => () => SoundEffectsPlayer.PlayTackleMissed(),
        "BallLost" => () => SoundEffectsPlayer.PlayBallLost(),
        "Foul" => () => SoundEffectsPlayer.PlayFoul(),
        "Penalty" => () => SoundEffectsPlayer.PlayPenalty(),
        "Corner" => () => SoundEffectsPlayer.PlayCorner(),
        "GoalKick" => () => SoundEffectsPlayer.PlayGoalKick(),
        "ThrowIn" => () => SoundEffectsPlayer.PlayThrowIn(),
        "PlayerSwitch" => () => SoundEffectsPlayer.PlayPlayerSwitch(),
        "BallKick" => () => SoundEffectsPlayer.PlayPassKick(0.0f),
        "PowerfulShot" => () => SoundEffectsPlayer.PlayShotKick(0.0f),
        "GoalNet" => () => SoundEffectsPlayer.PlayGoalNet(),
        "WoodworkHit" => () => SoundEffectsPlayer.PlayPostHit(),
        "RefereeWhistle" => () => SoundEffectsPlayer.PlayWhistle(),
        "GoalkeeperSave" => () => SoundEffectsPlayer.PlayShotKick(0.4f),
        "CrowdAmbiance" => () => SoundEffectsPlayer.StartCrowdAmbiance(),
        "GoalRoar" => () => SoundEffectsPlayer.PlayCrowdGoal(),
        "MissGasp" => () => SoundEffectsPlayer.PlayCrowdMiss(),
        "BeaconCenter" => () => GoalBeaconPlayer.PlayBeaconPulse(0.85f, 0.0f),
        "BeaconLeft" => () => GoalBeaconPlayer.PlayBeaconPulse(0.85f, -0.85f),
        "BeaconRight" => () => GoalBeaconPlayer.PlayBeaconPulse(0.85f, 0.85f),
        "ChampionsLeagueAnthem" => () => anthemEngine.PlayChampionsLeagueSample(),
        "NationalAnthem" => () => anthemEngine.PlayNationalAnthemSample("EGY"),
        _ => () => SoundEffectsPlayer.PlayMenuSelect()
    };
}
