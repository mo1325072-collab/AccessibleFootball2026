using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AccessibleFootball2026.Core.Input;

namespace AccessibleFootball2026.Input;

/// <summary>
/// معالج مدخلات لوحة المفاتيح فائق الدقة والاستجابة (Edge-Triggered & Continuous Input Handler):
/// - المعالجة اللحظية للحافة الصاعدة (Rising-Edge On-Press) للأوامر الفردية (D, W, S, A, Ctrl, Space, C, Esc).
/// - كبح التكرار التلقائي لنظام التشغيل (OS Auto-Repeat Suppression) لمنع تكرار الإجراء عند استمرار الضغط.
/// - دعم الحركة المستمرة والجري السريع (Arrow Keys, Shift) طالما المفتاح مضغوط.
/// - دعم مفاتيح Control و Shift لكلا الجانبين الأيمن والأيسر.
/// </summary>
public class KeyboardInputHandler : IInputHandler
{
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    // Virtual Key Codes (Win32)
    private const int VK_LEFT = 0x25;
    private const int VK_UP = 0x26;
    private const int VK_RIGHT = 0x27;
    private const int VK_DOWN = 0x28;
    private const int VK_ESCAPE = 0x1B;
    private const int VK_SPACE = 0x20;

    // Shift variants
    private const int VK_SHIFT = 0x10;
    private const int VK_LSHIFT = 0xA0;
    private const int VK_RSHIFT = 0xA1;

    // Control variants
    private const int VK_CONTROL = 0x11;
    private const int VK_LCONTROL = 0xA2;
    private const int VK_RCONTROL = 0xA3;

    // Action keys
    private const int VK_KEY_A = 0x41;
    private const int VK_KEY_C = 0x43;
    private const int VK_KEY_D = 0x44;
    private const int VK_KEY_S = 0x53;
    private const int VK_KEY_W = 0x57;

    // State collections
    private readonly HashSet<int> _heldContinuousKeys = new();
    private readonly HashSet<int> _heldDiscreteKeys = new();
    private readonly Queue<GameAction> _pendingDiscreteActions = new();
    private readonly HashSet<int> _win32LastHeld = new();
    private readonly object _stateLock = new();
    private volatile bool _hasReceivedEvents = false;

    // Hold-to-charge kick power: possession S/W/D charge while held and fire once on release.
    private const double ChargeTapSeconds = 0.25;   // a tap up to this stays normal power (current feel)
    private const double ChargeFullSeconds = 1.5;   // holding to here reaches full power
    private readonly Dictionary<int, (GameAction action, double startSeconds)> _activeCharges = new();
    private readonly Queue<(GameAction action, float charge)> _pendingChargedActions = new();
    private readonly Dictionary<GameAction, float> _releasedChargesThisPoll = new();
    private readonly Dictionary<int, double> _win32ChargeStart = new();
    private readonly Func<double> _nowSeconds;

    public KeyboardInputHandler(Func<double>? nowSecondsProvider = null)
    {
        _nowSeconds = nowSecondsProvider
            ?? (() => System.Diagnostics.Stopwatch.GetElapsedTime(System.Diagnostics.Stopwatch.GetTimestamp()).TotalSeconds);
    }

    internal static float ChargeFraction(double holdSeconds)
    {
        if (holdSeconds <= ChargeTapSeconds) return 0f;
        if (holdSeconds >= ChargeFullSeconds) return 1f;
        return (float)((holdSeconds - ChargeTapSeconds) / (ChargeFullSeconds - ChargeTapSeconds));
    }

    private static bool IsChargeableAction(GameAction action) =>
        action is GameAction.ShortPass or GameAction.Cross or GameAction.Shoot;

    public float ActiveCharge01(GameAction action)
    {
        lock (_stateLock)
        {
            foreach (var kv in _activeCharges)
            {
                if (kv.Value.action == action)
                    return ChargeFraction(_nowSeconds() - kv.Value.startSeconds);
            }
            return -1f;
        }
    }

    public float ConsumeReleasedCharge01(GameAction action)
    {
        lock (_stateLock)
        {
            return _releasedChargesThisPoll.Remove(action, out float charge) ? charge : 0f;
        }
    }

    private static bool IsKeyDown(int vKey)
    {
        try
        {
            return (GetAsyncKeyState(vKey) & 0x8000) != 0;
        }
        catch (DllNotFoundException)
        {
            // Non-Windows host (headless testing): treat key as not pressed.
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// معالجة حدث الضغط على المفتاح (KeyDown) من واجهة المستخدم Form
    /// </summary>
    public void HandleKeyDown(int keyCode, bool isPossession)
    {
        _hasReceivedEvents = true;

        lock (_stateLock)
        {
            // 1. الحركة المستمرة والجري
            if (IsContinuousKey(keyCode))
            {
                _heldContinuousKeys.Add(keyCode);
                return;
            }

            // 2. الأوامر الفردية (Discrete Actions)
            if (IsDiscreteKey(keyCode))
            {
                // منع التكرار التلقائي: إذا كان المفتاح مضغوطاً بالفعل، يتم تجاهل الحدث
                if (IsDiscreteKeyHeld(keyCode))
                {
                    // Track both physical Control keys even when their shared action is debounced.
                    if(keyCode is VK_CONTROL or VK_LCONTROL or VK_RCONTROL)_heldDiscreteKeys.Add(keyCode);
                    return;
                }

                MarkDiscreteKeyHeld(keyCode, true);

                GameAction action = ResolveDiscreteAction(keyCode, isPossession);
                if (IsChargeableAction(action))
                {
                    // الشحن بالضغط المطوّل: لا إطلاق عند الضغط، تُحسب القوة من مدة الضغط وتُطلق عند رفع اليد
                    _activeCharges[keyCode] = (action, _nowSeconds());
                }
                else if (action != GameAction.None)
                {
                    _pendingDiscreteActions.Enqueue(action);
                }
            }
        }
    }

    /// <summary>
    /// معالجة حدث رفع اليد عن المفتاح (KeyUp)
    /// </summary>
    public void HandleKeyUp(int keyCode)
    {
        lock (_stateLock)
        {
            _heldContinuousKeys.Remove(keyCode);

            if (_activeCharges.Remove(keyCode, out (GameAction action, double startSeconds) charge))
            {
                _pendingChargedActions.Enqueue((charge.action, ChargeFraction(_nowSeconds() - charge.startSeconds)));
            }

            MarkDiscreteKeyHeld(keyCode, false);
        }
    }

    /// <summary>
    /// تسجيل KeyDown متوافق مع عقود المشروع
    /// </summary>
    public void RegisterKeyDown(int keyCode, bool isPossession = false) => HandleKeyDown(keyCode, isPossession);

    /// <summary>
    /// تسجيل KeyUp متوافق مع عقود المشروع
    /// </summary>
    public void RegisterKeyUp(int keyCode) => HandleKeyUp(keyCode);

    /// <summary>
    /// تصفير كافة الحالات والمفاتيح المضغوطة (عند تغيير التركيز أو الإيقاف المؤقت)
    /// </summary>
    public void Reset()
    {
        lock (_stateLock)
        {
            _heldContinuousKeys.Clear();
            _heldDiscreteKeys.Clear();
            _pendingDiscreteActions.Clear();
            _win32LastHeld.Clear();
            _activeCharges.Clear();
            _pendingChargedActions.Clear();
            _releasedChargesThisPoll.Clear();
            _win32ChargeStart.Clear();
        }
    }

    /// <summary>
    /// استهلاك وقراءة الإجراءات الحالية لدورة محاكاة 50Hz الحالية
    /// يتم سحب وتفريغ الأوامر الفردية بينما تظل الأوامر المستمرة نشطة طالما استمر الضغط
    /// </summary>
    public HashSet<GameAction> PollCurrentActions(bool isPossession)
    {
        var actions = new HashSet<GameAction>();

        lock (_stateLock)
        {
            _releasedChargesThisPoll.Clear();

            if (_hasReceivedEvents)
            {
                // 1. استهلاك الأوامر الفردية المقيدة بالطابور اللحظي
                // الأمر المشحون/المؤجل يُسقط إذا انقلبت حالة الاستحواذ بعد ضغطة المفتاح:
                // لا تسديدة مشحونة تنطلق بعد فقدان الكرة، ولا التحام يطلق وقد صارت الكرة معنا.
                while (_pendingDiscreteActions.Count > 0)
                {
                    var pending = _pendingDiscreteActions.Dequeue();
                    if (isPossession ? IsDefenseOnlyAction(pending) : IsPossessionOnlyAction(pending)) continue;
                    actions.Add(pending);
                }
                while (_pendingChargedActions.Count > 0)
                {
                    (GameAction chargedAction, float charge) = _pendingChargedActions.Dequeue();
                    if (isPossession ? IsDefenseOnlyAction(chargedAction) : IsPossessionOnlyAction(chargedAction)) continue;
                    actions.Add(chargedAction);
                    _releasedChargesThisPoll[chargedAction] = charge;
                }

                // 2. تقييم مفاتيح الحركة المستمرة
                if (_heldContinuousKeys.Contains(VK_UP)) actions.Add(GameAction.MoveUp);
                if (_heldContinuousKeys.Contains(VK_DOWN)) actions.Add(GameAction.MoveDown);
                if (_heldContinuousKeys.Contains(VK_LEFT)) actions.Add(GameAction.MoveLeft);
                if (_heldContinuousKeys.Contains(VK_RIGHT)) actions.Add(GameAction.MoveRight);

                // 3. تقييم مفتاح الجري السريع (أي من أشكال Shift)
                if (_heldContinuousKeys.Contains(VK_SHIFT) ||
                    _heldContinuousKeys.Contains(VK_LSHIFT) ||
                    _heldContinuousKeys.Contains(VK_RSHIFT))
                {
                    actions.Add(GameAction.Sprint);
                }
            }
            else
            {
                // مسار الدعم البديل عبر Win32 مع كشف الحافة الصاعدة (Edge-Detection)
                PollWin32Fallback(actions, isPossession);
            }
        }

        return actions;
    }

    private static bool IsPossessionOnlyAction(GameAction action) => action is
        GameAction.ShortPass or GameAction.ThroughBall or GameAction.Cross or GameAction.Shoot
        or GameAction.LobShot or GameAction.CurledShot or GameAction.LoftedThroughBall
        or GameAction.GroundCross or GameAction.FarPostCross
        or GameAction.Header or GameAction.BicycleKick or GameAction.Volley;

    private static bool IsDefenseOnlyAction(GameAction action) => action is
        GameAction.StandTackle or GameAction.SlideTackle;

    /// <summary>
    /// اسم بديل متوافق مع عقد المشروع لـ PollCurrentActions
    /// </summary>
    public HashSet<GameAction> ConsumeCurrentActions(bool hasPossession) => PollCurrentActions(hasPossession);

    public bool IsActionTriggered(GameAction action, bool isPossession)
    {
        return PollCurrentActions(isPossession).Contains(action);
    }

    private void PollWin32Fallback(HashSet<GameAction> actions, bool isPossession)
    {
        // 1. الحركة المستمرة والجري
        if (IsKeyDown(VK_UP)) actions.Add(GameAction.MoveUp);
        if (IsKeyDown(VK_DOWN)) actions.Add(GameAction.MoveDown);
        if (IsKeyDown(VK_LEFT)) actions.Add(GameAction.MoveLeft);
        if (IsKeyDown(VK_RIGHT)) actions.Add(GameAction.MoveRight);

        if (IsKeyDown(VK_SHIFT) || IsKeyDown(VK_LSHIFT) || IsKeyDown(VK_RSHIFT))
        {
            actions.Add(GameAction.Sprint);
        }

        // 2. الأوامر الفردية مع كشف الحافة الصاعدة (Rising Edge)
        CheckWin32DiscreteKey(VK_ESCAPE, GameAction.Pause, actions);

        bool ctrlDown = IsKeyDown(VK_CONTROL) || IsKeyDown(VK_LCONTROL) || IsKeyDown(VK_RCONTROL);
        bool ctrlWasDown = _win32LastHeld.Contains(VK_CONTROL) || _win32LastHeld.Contains(VK_LCONTROL) || _win32LastHeld.Contains(VK_RCONTROL);
        if (ctrlDown && !ctrlWasDown)
        {
            actions.Add(GameAction.SwitchPlayer);
        }
        UpdateWin32Key(VK_CONTROL);
        UpdateWin32Key(VK_LCONTROL);
        UpdateWin32Key(VK_RCONTROL);

        if (isPossession)
        {
            CheckWin32ChargeableKey(VK_KEY_S, GameAction.ShortPass, actions);
            CheckWin32ChargeableKey(VK_KEY_D, GameAction.Cross, actions);
            CheckWin32ChargeableKey(VK_KEY_W, GameAction.Shoot, actions);
            CheckWin32DiscreteKey(VK_KEY_A, GameAction.ThroughBall, actions);
        }
        else
        {
            CheckWin32DiscreteKey(VK_KEY_S, GameAction.StandTackle, actions);

            bool slideDown = IsKeyDown(VK_KEY_W) || IsKeyDown(VK_KEY_D) || IsKeyDown(VK_KEY_A);
            bool slideWasDown = _win32LastHeld.Contains(VK_KEY_W) || _win32LastHeld.Contains(VK_KEY_D) || _win32LastHeld.Contains(VK_KEY_A);
            if (slideDown && !slideWasDown)
            {
                actions.Add(GameAction.SlideTackle);
            }
            UpdateWin32Key(VK_KEY_W);
            UpdateWin32Key(VK_KEY_D);
            UpdateWin32Key(VK_KEY_A);
        }
    }

    private void CheckWin32ChargeableKey(int vKey, GameAction action, HashSet<GameAction> actions)
    {
        bool isDown = IsKeyDown(vKey);
        if (isDown && !_win32ChargeStart.ContainsKey(vKey))
        {
            _win32ChargeStart[vKey] = _nowSeconds();
            _activeCharges[vKey] = (action, _win32ChargeStart[vKey]);
        }
        else if (!isDown && _win32ChargeStart.Remove(vKey, out double start))
        {
            _activeCharges.Remove(vKey);
            actions.Add(action);
            _releasedChargesThisPoll[action] = ChargeFraction(_nowSeconds() - start);
        }
    }

    private void CheckWin32DiscreteKey(int vKey, GameAction action, HashSet<GameAction> actions)
    {
        bool isDown = IsKeyDown(vKey);
        bool wasDown = _win32LastHeld.Contains(vKey);
        if (isDown && !wasDown)
        {
            actions.Add(action);
            _win32LastHeld.Add(vKey);
        }
        else if (!isDown && wasDown)
        {
            _win32LastHeld.Remove(vKey);
        }
    }

    private void UpdateWin32Key(int vKey)
    {
        if (IsKeyDown(vKey))
            _win32LastHeld.Add(vKey);
        else
            _win32LastHeld.Remove(vKey);
    }

    private static bool IsContinuousKey(int keyCode) =>
        keyCode is VK_UP or VK_DOWN or VK_LEFT or VK_RIGHT or VK_SHIFT or VK_LSHIFT or VK_RSHIFT;

    private static bool IsDiscreteKey(int keyCode) =>
        keyCode is VK_ESCAPE or
                   VK_CONTROL or VK_LCONTROL or VK_RCONTROL or
                   VK_KEY_A or VK_KEY_D or VK_KEY_S or VK_KEY_W;

    private bool IsDiscreteKeyHeld(int keyCode)
    {
        if (keyCode is VK_CONTROL or VK_LCONTROL or VK_RCONTROL)
        {
            return _heldDiscreteKeys.Contains(VK_CONTROL) ||
                   _heldDiscreteKeys.Contains(VK_LCONTROL) ||
                   _heldDiscreteKeys.Contains(VK_RCONTROL);
        }
        return _heldDiscreteKeys.Contains(keyCode);
    }

    private void MarkDiscreteKeyHeld(int keyCode, bool held)
    {
        if(held)_heldDiscreteKeys.Add(keyCode);
        else _heldDiscreteKeys.Remove(keyCode);
    }

    private static GameAction ResolveDiscreteAction(int keyCode, bool isPossession)
    {
        // الأوامر العامة المشتركة
        if (keyCode == VK_ESCAPE) return GameAction.Pause;

        // تبديل المدافعين
        if (keyCode is VK_CONTROL or VK_LCONTROL or VK_RCONTROL) return GameAction.SwitchPlayer;

        if (isPossession)
        {
            // أزرار الهجوم: S للتمرير، D للرفع، W للتسديد، A للبينية
            if (keyCode == VK_KEY_S) return GameAction.ShortPass;
            if (keyCode == VK_KEY_D) return GameAction.Cross;
            if (keyCode == VK_KEY_W) return GameAction.Shoot;
            if (keyCode == VK_KEY_A) return GameAction.ThroughBall;
        }
        else
        {
            // أزرار الدفاع: S للاستخلاص الهادئ، W/D/A للاستخلاص القوي
            if (keyCode == VK_KEY_S) return GameAction.StandTackle;
            if (keyCode is VK_KEY_W or VK_KEY_D or VK_KEY_A) return GameAction.SlideTackle;
        }

        return GameAction.None;
    }
}
