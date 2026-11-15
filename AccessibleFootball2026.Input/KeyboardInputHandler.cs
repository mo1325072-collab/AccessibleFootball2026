using System.Diagnostics;
using System.Runtime.InteropServices;
using AccessibleFootball2026.Core.Input;

namespace AccessibleFootball2026.Input;

/// <summary>
/// Independent physical key states. Movement is held, discrete actions are press edges,
/// and attacking S/D/W release exactly one charged action. Ctrl is never a modifier.
/// </summary>
public class KeyboardInputHandler : IInputHandler
{
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    private readonly object _gate = new();
    private readonly HashSet<int> _held = new();
    private readonly HashSet<int> _blockedUntilRelease = new();
    private readonly Dictionary<int, Press> _presses = new();
    private readonly Queue<ReleasedAction> _pending = new();
    private readonly Dictionary<GameAction, float> _released = new();
    private readonly Func<double> _now;
    private bool _eventsReceived;
    private bool? _lastPossession;
    private sealed record Press(GameAction Action, bool Possession, double Started, bool Charged);
    private sealed record ReleasedAction(GameAction Action, bool? Possession, float Charge);
    private static readonly int[] Watched = { 16, 160, 161, 17, 162, 163, 27, 32, 37, 38, 39, 40, 65, 83, 68, 87 };

    public KeyboardInputHandler(Func<double>? nowSecondsProvider = null) =>
        _now = nowSecondsProvider ?? (() => (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency);

    internal static float ChargeFraction(double seconds) =>
        double.IsFinite(seconds) ? (float)Math.Clamp((seconds - .25) / 1.25, 0, 1) : 0;
    private static bool Control(int key) => key is 17 or 162 or 163;
    private static bool Shift(int key) => key is 16 or 160 or 161;
    private static bool Continuous(int key) => key is 37 or 38 or 39 or 40 || Shift(key);
    private static bool AttackOnly(GameAction action) => action is GameAction.ShortPass or GameAction.Cross or GameAction.ThroughBall or GameAction.Shoot;
    private static bool DefenseOnly(GameAction action) => action is GameAction.StandTackle or GameAction.SlideTackle;
    private static GameAction ActionFor(int key, bool possession) => key switch
    {
        27 => GameAction.Pause,
        32 => GameAction.TacticalRadar,
        17 or 162 or 163 => GameAction.SwitchPlayer,
        83 => possession ? GameAction.ShortPass : GameAction.StandTackle,
        68 => possession ? GameAction.Cross : GameAction.SlideTackle,
        65 when possession => GameAction.ThroughBall,
        87 when possession => GameAction.Shoot,
        _ => GameAction.None
    };

    public void HandleKeyDown(int keyCode, bool isPossession)
    {
        lock (_gate) { _eventsReceived = true; Down(keyCode, isPossession); }
    }
    private void Down(int key, bool possession)
    {
        if (_blockedUntilRelease.Contains(key) || _held.Contains(key)) return;
        bool anotherControl = Control(key) && _held.Any(Control);
        _held.Add(key);
        if (Continuous(key) || anotherControl) return;
        var action = ActionFor(key, possession);
        if (action == GameAction.None) return;
        bool charged = possession && action is GameAction.ShortPass or GameAction.Cross or GameAction.Shoot;
        _presses[key] = new(action, possession, _now(), charged);
        if (!charged) _pending.Enqueue(new(action, AttackOnly(action) || DefenseOnly(action) ? possession : null, 0));
    }
    public void HandleKeyUp(int keyCode)
    {
        lock (_gate) { _eventsReceived = true; Up(keyCode); }
    }
    private void Up(int key)
    {
        _held.Remove(key);
        _blockedUntilRelease.Remove(key);
        if (_presses.Remove(key, out var press) && press.Charged)
            _pending.Enqueue(new(press.Action, press.Possession, ChargeFraction(_now() - press.Started)));
    }
    public void RegisterKeyDown(int keyCode, bool isPossession = false) => HandleKeyDown(keyCode, isPossession);
    public void RegisterKeyUp(int keyCode) => HandleKeyUp(keyCode);

    public HashSet<GameAction> PollCurrentActions(bool isPossession)
    {
        lock (_gate)
        {
            if (!_eventsReceived) SamplePhysicalKeys(isPossession);
            if (_lastPossession.HasValue && _lastPossession != isPossession)
            {
                // A held attack key cannot become a defensive action, or release an old shot later.
                foreach (var item in _presses.Where(p => p.Value.Possession != isPossession && (AttackOnly(p.Value.Action) || DefenseOnly(p.Value.Action))).ToArray())
                    _presses.Remove(item.Key);
            }
            _lastPossession = isPossession;
            var result = new HashSet<GameAction>();
            _released.Clear();
            while (_pending.TryDequeue(out var action))
            {
                if (action.Possession.HasValue && action.Possession != isPossession) continue;
                result.Add(action.Action);
                if (AttackOnly(action.Action)) _released[action.Action] = action.Charge;
            }
            if (_held.Contains(37)) result.Add(GameAction.MoveLeft);
            if (_held.Contains(38)) result.Add(GameAction.MoveUp);
            if (_held.Contains(39)) result.Add(GameAction.MoveRight);
            if (_held.Contains(40)) result.Add(GameAction.MoveDown);
            if (_held.Any(Shift)) result.Add(GameAction.Sprint);
            return result;
        }
    }

    private void SamplePhysicalKeys(bool possession)
    {
        foreach (int key in Watched)
        {
            bool down;
            try { down = (GetAsyncKeyState(key) & 0x8000) != 0; }
            catch (DllNotFoundException) { return; }
            catch (EntryPointNotFoundException) { return; }
            if (down) Down(key, possession); else Up(key);
        }
    }
    public void InvalidatePossessionActions()
    {
        lock (_gate)
        {
            foreach (var entry in _presses.Where(p => AttackOnly(p.Value.Action) || DefenseOnly(p.Value.Action)).ToArray()) _presses.Remove(entry.Key);
            var keep = _pending.Where(p => !p.Possession.HasValue).ToArray();
            _pending.Clear(); foreach (var action in keep) _pending.Enqueue(action);
            _released.Clear();
        }
    }
    public void Reset()
    {
        lock (_gate)
        {
            // Keys still physically held across a focus/pause boundary require a fresh release.
            _blockedUntilRelease.UnionWith(_held);
            _held.Clear(); _presses.Clear(); _pending.Clear(); _released.Clear(); _lastPossession = null;
        }
    }
    public float ActiveCharge01(GameAction action)
    {
        lock (_gate)
        {
            var press = _presses.Values.FirstOrDefault(p => p.Charged && p.Action == action);
            return press == null ? -1 : ChargeFraction(_now() - press.Started);
        }
    }
    public float ConsumeReleasedCharge01(GameAction action)
    {
        lock (_gate) return _released.Remove(action, out var value) ? value : 0;
    }
    public HashSet<GameAction> ConsumeCurrentActions(bool hasPossession) => PollCurrentActions(hasPossession);
    public bool IsActionTriggered(GameAction action, bool isPossession) => PollCurrentActions(isPossession).Contains(action);
}
