namespace AccessibleFootball2026.Core.Input;

public enum GameAction
{
    None = 0,
    MoveUp,
    MoveDown,
    MoveLeft,
    MoveRight,
    
    // الهجوم
    ShortPass,      // S
    ThroughBall,    // A
    Cross,          // D
    Shoot,          // W
    Sprint,         // Left Shift / Right Shift
    
    // الدفاع
    SwitchPlayer,   // Ctrl (Left / Right)
    StandTackle,    // S
    SlideTackle,    // D / W / A
    
    // النظام
    Pause,          // Escape
    TacticalRadar   // Space / C (الرادار التكتيكي اللحظي على الطلب)
}

public interface IInputHandler
{
    HashSet<GameAction> PollCurrentActions(bool isPossession);
    bool IsActionTriggered(GameAction action, bool isPossession);
    void HandleKeyDown(int keyCode, bool isPossession) { }
    void HandleKeyUp(int keyCode) { }
    void Reset() { }
}
