namespace AccessibleFootball2026.Core.Input;

/// <summary>Direct held-key aiming. Up/down use the same facing-Y rate as ordinary possession play.</summary>
public sealed class DirectShootoutInput
{
    private readonly HashSet<int> _held = new();
    public float FacingY { get; private set; }
    public bool IsKeeper { get; }
    public bool Submitted { get; private set; }
    public float AimY => _held.Contains(0x27) && !_held.Contains(0x25) ? 3.25f :
        _held.Contains(0x25) && !_held.Contains(0x27) ? -3.25f :
        Math.Clamp(11 * FacingY / MathF.Sqrt(Math.Max(.001f, 1 - FacingY * FacingY)), -5, 5);
    public int KeeperDive => AimY > 1 ? 0 : AimY < -1 ? 2 : 1;
    public DirectShootoutInput(bool keeper = false) => IsKeeper = keeper;
    public void Tick(float seconds)
    {
        if (Submitted || !float.IsFinite(seconds) || seconds <= 0) return;
        float delta = _held.Contains(0x26) ? -.9f * seconds : _held.Contains(0x28) ? .9f * seconds : 0;
        FacingY = Math.Clamp(FacingY + delta, -.9f, .9f);
    }
    public bool KeyDown(int key)
    {
        if (Submitted || !_held.Add(key)) return false;
        if (key is 0x57 or 0x53) { Submitted = true; return true; }
        return false;
    }
    public void KeyUp(int key) => _held.Remove(key);
    public void ReleaseAll() => _held.Clear();
}
