using System;
using System.Collections.Generic;
using System.Threading;
using AccessibleFootball2026.Audio;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.App.Menus;

public interface IInteractiveMenuItem
{
    void OnLeft();
    void OnRight();
    void OnToggle();
}

public class AccessibleMenuItem
{
    public virtual string Title { get; set; }
    public virtual string SpokenText => Title;
    public string? Description { get; set; }
    public Action? OnSelect { get; set; }
    public bool SuppressSelectionSound { get; set; } = false;

    public AccessibleMenuItem(string title, Action? onSelect = null, string? description = null, bool suppressSelectionSound = false)
    {
        Title = title;
        OnSelect = onSelect;
        Description = description;
        SuppressSelectionSound = suppressSelectionSound;
    }
}

/// <summary>
/// عنصر مربع تحديد تفاعلي للخيارات الثنائية (Check Box)
/// </summary>
public class AccessibleCheckboxMenuItem : AccessibleMenuItem, IInteractiveMenuItem
{
    public string Label { get; set; }
    public bool IsChecked { get; set; }
    public Action<bool>? OnToggleChange { get; set; }
    private readonly IScreenReader _screenReader;

    public AccessibleCheckboxMenuItem(string label, bool initialValue, Action<bool> onToggleChange, IScreenReader screenReader)
        : base(string.Empty)
    {
        Label = label;
        IsChecked = initialValue;
        OnToggleChange = onToggleChange;
        _screenReader = screenReader;
    }

    public override string Title => MatchSettings.Current.Language == UiLanguage.English
        ? $"{Label}: [{(IsChecked ? "X" : " ")}] {(IsChecked ? "Checked" : "Unchecked")}"
        : $"{Label}: [{(IsChecked ? "X" : " ")}] {(IsChecked ? "محدد" : "غير محدد")}";

    public override string SpokenText => MatchSettings.Current.Language == UiLanguage.English
        ? $"{Label}: {(IsChecked ? "Checked" : "Unchecked")}"
        : $"{Label}، {(IsChecked ? "محدد" : "غير محدد")}";

    public void OnToggle()
    {
        IsChecked = !IsChecked;
        SoundEffectsPlayer.PlayMenuSelect();
        OnToggleChange?.Invoke(IsChecked);
        string state = MatchSettings.Current.Language == UiLanguage.English
            ? (IsChecked ? "Checked" : "Unchecked")
            : (IsChecked ? "محدد" : "غير محدد");
        _screenReader.Speak(state, interrupt: true);
    }

    public void OnLeft() => OnToggle();
    public void OnRight() => OnToggle();
}

/// <summary>
/// عنصر قائمة اختيار تفاعلية / كومبوبوكس (Combo Box)
/// </summary>
public class AccessibleComboBoxMenuItem : AccessibleMenuItem, IInteractiveMenuItem
{
    public string Label { get; set; }
    public List<string> Options { get; }
    public int SelectedIndex { get; set; }
    public Action<int>? OnOptionChanged { get; set; }
    public Action? OnOpenDropdown { get; set; }
    private readonly IScreenReader _screenReader;

    public AccessibleComboBoxMenuItem(string label, List<string> options, int initialIndex, Action<int> onOptionChanged, Action? onOpenDropdown, IScreenReader screenReader)
        : base(string.Empty)
    {
        Label = label;
        Options = options;
        SelectedIndex = Math.Clamp(initialIndex, 0, Math.Max(0, options.Count - 1));
        OnOptionChanged = onOptionChanged;
        OnOpenDropdown = onOpenDropdown;
        _screenReader = screenReader;
    }

    public override string Title
    {
        get
        {
            string opt = (SelectedIndex >= 0 && SelectedIndex < Options.Count) ? Options[SelectedIndex] : "";
            return $"{Label}: < {opt} >";
        }
    }

    public override string SpokenText
    {
        get
        {
            string opt = (SelectedIndex >= 0 && SelectedIndex < Options.Count) ? Options[SelectedIndex] : "";
            return MatchSettings.Current.Language == UiLanguage.English
                ? $"{Label}: {opt}"
                : $"{Label}، {opt}";
        }
    }

    public void OnRight()
    {
        if (Options.Count == 0) return;
        SelectedIndex = (SelectedIndex + 1) % Options.Count;
        SoundEffectsPlayer.PlayMenuMove();
        OnOptionChanged?.Invoke(SelectedIndex);
        _screenReader.Speak(Options[SelectedIndex], interrupt: true);
    }

    public void OnLeft()
    {
        if (Options.Count == 0) return;
        SelectedIndex = (SelectedIndex - 1 + Options.Count) % Options.Count;
        SoundEffectsPlayer.PlayMenuMove();
        OnOptionChanged?.Invoke(SelectedIndex);
        _screenReader.Speak(Options[SelectedIndex], interrupt: true);
    }

    public void OnToggle()
    {
        if (OnOpenDropdown != null)
        {
            SoundEffectsPlayer.PlayMenuSelect();
            OnOpenDropdown();
        }
        else
        {
            OnRight();
        }
    }
}

/// <summary>
/// عنصر شريط تمرير تفاعلي لمستويات الصوت والنسب المئوية (Slider)
/// </summary>
public class AccessibleSliderMenuItem : AccessibleMenuItem, IInteractiveMenuItem
{
    public string Label { get; set; }
    public int Value { get; set; }
    public int Min { get; set; }
    public int Max { get; set; }
    public int Step { get; set; }
    public Action<int>? OnValueChanged { get; set; }
    private readonly IScreenReader _screenReader;

    public AccessibleSliderMenuItem(string label, int initialValue, int min, int max, int step, Action<int> onValueChanged, IScreenReader screenReader)
        : base(string.Empty)
    {
        Label = label;
        Min = min;
        Max = max;
        Step = step;
        Value = Math.Clamp(initialValue, min, max);
        OnValueChanged = onValueChanged;
        _screenReader = screenReader;
    }

    public override string Title
    {
        get
        {
            float ratio = (Max > Min) ? (float)(Value - Min) / (Max - Min) : 0f;
            int filled = Math.Clamp((int)Math.Round(ratio * 10), 0, 10);
            string bar = new string('|', filled).PadRight(10, ' ');
            return $"{Label}: [{bar}] {Value}%";
        }
    }

    public override string SpokenText => MatchSettings.Current.Language == UiLanguage.English
        ? $"{Label}: {Value} percent"
        : $"{Label}، {Value} بالمئة";

    public void OnRight()
    {
        Value = Math.Min(Max, Value + Step);
        SoundEffectsPlayer.PlayMenuMove();
        OnValueChanged?.Invoke(Value);
        string announcement = MatchSettings.Current.Language == UiLanguage.English
            ? $"{Value} percent"
            : $"{Value} بالمئة";
        _screenReader.Speak(announcement, interrupt: true);
    }

    public void OnLeft()
    {
        Value = Math.Max(Min, Value - Step);
        SoundEffectsPlayer.PlayMenuMove();
        OnValueChanged?.Invoke(Value);
        string announcement = MatchSettings.Current.Language == UiLanguage.English
            ? $"{Value} percent"
            : $"{Value} بالمئة";
        _screenReader.Speak(announcement, interrupt: true);
    }

    public void OnToggle()
    {
        OnRight();
    }
}

/// <summary>
/// نظام القوائم الصوتية الاحترافي المتوافق 100% مع قارئ الشاشة NVDA
/// - يدعم مربعات التحديد CheckBoxes، القوائم المنسدلة ComboBoxes، وأشرطة التمرير Sliders
/// - نطق صوتي صافٍ بدون رموز كونسول أو أرقام مشتتة
/// - مؤثرات صوتية حقيقية لنقرات التنقل والتأكيد
/// </summary>
public class AccessibleMenu
{
    public string MenuTitle { get; }
    public bool SuppressSelectSound { get; set; } = false;
    private readonly List<AccessibleMenuItem> _items = new();
    private int _selectedIndex = 0;
    private readonly IScreenReader _screenReader;

    public AccessibleMenu(string title, IScreenReader screenReader)
    {
        MenuTitle = title;
        _screenReader = screenReader;
    }

    public AccessibleMenuItem AddItem(string title, Action onSelect, string? description = null, bool suppressSelectionSound = false)
    {
        var item = new AccessibleMenuItem(title, onSelect, description, suppressSelectionSound);
        _items.Add(item);
        return item;
    }

    public void AddCheckbox(string label, bool initialValue, Action<bool> onToggle)
    {
        _items.Add(new AccessibleCheckboxMenuItem(label, initialValue, onToggle, _screenReader));
    }

    public void AddComboBox(string label, List<string> options, int initialIndex, Action<int> onOptionChanged, Action? onOpenDropdown = null)
    {
        _items.Add(new AccessibleComboBoxMenuItem(label, options, initialIndex, onOptionChanged, onOpenDropdown, _screenReader));
    }

    public void AddSlider(string label, int initialValue, int min, int max, int step, Action<int> onValueChanged)
    {
        _items.Add(new AccessibleSliderMenuItem(label, initialValue, min, max, step, onValueChanged, _screenReader));
    }

    public void Open()
    {
        _selectedIndex = 0;
        if (_items.Count > 0)
        {
            _screenReader.Speak($"{MenuTitle}. {_items[0].SpokenText}");
        }
    }

    public void NavigateNext()
    {
        if (_items.Count == 0) return;
        _selectedIndex = (_selectedIndex + 1) % _items.Count;
        SoundEffectsPlayer.PlayMenuMove();
        AnnounceCurrentItem();
    }

    public void NavigatePrevious()
    {
        if (_items.Count == 0) return;
        _selectedIndex = (_selectedIndex - 1 + _items.Count) % _items.Count;
        SoundEffectsPlayer.PlayMenuMove();
        AnnounceCurrentItem();
    }

    public void NavigateLeft()
    {
        if (_selectedIndex >= 0 && _selectedIndex < _items.Count)
        {
            var item = _items[_selectedIndex];
            if (item is IInteractiveMenuItem interactive)
            {
                interactive.OnLeft();
            }
        }
    }

    public void NavigateRight()
    {
        if (_selectedIndex >= 0 && _selectedIndex < _items.Count)
        {
            var item = _items[_selectedIndex];
            if (item is IInteractiveMenuItem interactive)
            {
                interactive.OnRight();
            }
        }
    }

    public void ToggleCurrent()
    {
        if (_selectedIndex >= 0 && _selectedIndex < _items.Count)
        {
            var item = _items[_selectedIndex];
            if (item is IInteractiveMenuItem interactive)
            {
                interactive.OnToggle();
                return;
            }
            SelectCurrent();
        }
    }

    public void SelectCurrent()
    {
        if (_selectedIndex >= 0 && _selectedIndex < _items.Count)
        {
            var item = _items[_selectedIndex];
            if (item is IInteractiveMenuItem interactive)
            {
                interactive.OnToggle();
                return;
            }

            bool suppress = SuppressSelectSound || item.SuppressSelectionSound;
            if (!suppress)
            {
                SoundEffectsPlayer.PlayMenuSelect();
                string prefix = MatchSettings.Current.Language == UiLanguage.English ? "Selected:" : "تم اختيار";
                _screenReader.Speak($"{prefix} {item.Title}");
            }

            item.OnSelect?.Invoke();
        }
    }

    public AccessibleMenuItem GetCurrentItem() => _items[_selectedIndex];
    public AccessibleMenuItem GetItem(int index) => _items[index];
    public int GetCurrentIndex() => _selectedIndex;
    public int Count => _items.Count;

    public void AnnounceCurrentItem()
    {
        if (_selectedIndex >= 0 && _selectedIndex < _items.Count)
        {
            var item = _items[_selectedIndex];
            _screenReader.Speak(item.SpokenText, interrupt: true);
        }
    }
}
