using System;
using System.Collections.Generic;
using AccessibleFootball2026.Audio;
using AccessibleFootball2026.Audio.Anthems;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.App.Menus;

/// <summary>
/// منشئ شاشة تعلم أصوات اللعبة التفاعلية (Sound Academy Menu Builder):
/// يتيح استعراض وسماع نغمات التنبيه الـ 9 ومنارة المرمى بجميع حالاتها في قائمة واحدة
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

        var items = SoundAcademyDatabase.GetAllItems();
        foreach (var soundItem in items)
        {
            var item = soundItem;
            string itemName = item.GetName(lang);
            string itemDesc = item.GetDescription(lang);

            menu.AddItem(itemName, () =>
            {
                // A new preview replaces the old one; previews never leak after selection or exit.
                StopPreview(anthemEngine);
                bool played = TryPlayPreview(item.Id, anthemEngine);
                string status = played ? "" : LocalizationService.Text(" العينة غير متاحة أو تعذر تشغيلها بالمستوى الصوتي الحالي.", " Sample unavailable or could not play at the current volume.");
                screenReader.Speak($"{itemName}. {itemDesc}{status}", interrupt: true);
            }, description: itemDesc, suppressSelectionSound: true);
        }

        string backTitle = LocalizationService.GetString("BackToMainMenu");
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
        "BeaconNear" => PreviewPosition(103,34,false),
        "BeaconFar" => PreviewPosition(65,34,false),
        "BeaconSecondHalfLeft" => PreviewPosition(20,10,true),
        "BeaconSecondHalfRight" => PreviewPosition(20,58,true),
        "BeaconOutOfRange" or "BeaconNoPossession" => PreviewSilence(),
        _ => SoundAcademyDatabase.GetItemsByCategory("earcons").Any(i=>i.Id==soundId) && SoundEffectsPlayer.TryPlayAcademySample(soundId)
    };

    private static bool PreviewPosition(float x,float y,bool secondHalf)
    {
        GoalBeaconPlayer.CalculateBeacon(x,y,secondHalf,1,out _,out float volume,out float pan);
        return GoalBeaconPlayer.PlayBeaconPulse(volume,pan);
    }
    private static bool PreviewSilence() { GoalBeaconPlayer.Stop(); return true; }

    public static Action GetAudioAction(string soundId, AnthemEngine anthemEngine) => () => TryPlayPreview(soundId, anthemEngine);
}
