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

    private static bool IsKeyDown(int vKey)
    {
        return (GetAsyncKeyState(vKey) & 0x8000) != 0;
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
                    return;
                }

                MarkDiscreteKeyHeld(keyCode, true);

                GameAction action = ResolveDiscreteAction(keyCode, isPossession);
                if (action != GameAction.None)
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
            if (keyCode is VK_SHIFT or VK_LSHIFT or VK_RSHIFT)
            {
                _heldContinuousKeys.Remove(VK_SHIFT);
                _heldContinuousKeys.Remove(VK_LSHIFT);
                _heldContinuousKeys.Remove(VK_RSHIFT);
            }
            else
            {
                _heldContinuousKeys.Remove(keyCode);
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
            if (_hasReceivedEvents)
            {
                // 1. استهلاك الأوامر الفردية المقيدة بالطابور اللحظي
                while (_pendingDiscreteActions.Count > 0)
                {
                    actions.Add(_pendingDiscreteActions.Dequeue());
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

        bool radarDown = IsKeyDown(VK_SPACE) || IsKeyDown(VK_KEY_C);
        bool radarWasDown = _win32LastHeld.Contains(VK_SPACE) || _win32LastHeld.Contains(VK_KEY_C);
        if (radarDown && !radarWasDown)
        {
            actions.Add(GameAction.TacticalRadar);
        }
        UpdateWin32Key(VK_SPACE);
        UpdateWin32Key(VK_KEY_C);

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
            CheckWin32DiscreteKey(VK_KEY_S, GameAction.ShortPass, actions);
            CheckWin32DiscreteKey(VK_KEY_D, GameAction.Cross, actions);
            CheckWin32DiscreteKey(VK_KEY_W, GameAction.Shoot, actions);
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
        keyCode is VK_ESCAPE or VK_SPACE or VK_KEY_C or
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
        if (held)
        {
            _heldDiscreteKeys.Add(keyCode);
            if (keyCode is VK_CONTROL or VK_LCONTROL or VK_RCONTROL)
            {
                _heldDiscreteKeys.Add(VK_CONTROL);
                _heldDiscreteKeys.Add(VK_LCONTROL);
                _heldDiscreteKeys.Add(VK_RCONTROL);
            }
        }
        else
        {
            _heldDiscreteKeys.Remove(keyCode);
            if (keyCode is VK_CONTROL or VK_LCONTROL or VK_RCONTROL)
            {
                _heldDiscreteKeys.Remove(VK_CONTROL);
                _heldDiscreteKeys.Remove(VK_LCONTROL);
                _heldDiscreteKeys.Remove(VK_RCONTROL);
            }
        }
    }

    private static GameAction ResolveDiscreteAction(int keyCode, bool isPossession)
    {
        // الأوامر العامة المشتركة
        if (keyCode == VK_ESCAPE) return GameAction.Pause;
        if (keyCode is VK_SPACE or VK_KEY_C) return GameAction.TacticalRadar;

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
