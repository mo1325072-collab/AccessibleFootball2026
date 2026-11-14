namespace AccessibleFootball2026.Core.Input;

/// <summary>Exclusive pre-match state. A skip key cannot leak into gameplay through auto-repeat.</summary>
public sealed class MatchLineupInput
{
    public bool IsActive { get; private set; }
    private int? _dismissKey;
    public bool IsDismissKeyHeld => _dismissKey.HasValue;
    public void Begin() { IsActive = true; _dismissKey = null; }
    public bool KeyDown(int key, out bool skip)
    {
        skip = false;
        if (_dismissKey == key) return true;
        if (!IsActive) return false;
        if (key is 13 or 32) { _dismissKey = key; skip = true; }
        return true;
    }
    public bool KeyUp(int key)
    {
        if (_dismissKey != key) return IsActive;
        _dismissKey = null;
        return true;
    }
    public void ReleaseAll() => _dismissKey = null;
    public void Complete() => IsActive = false;
}
