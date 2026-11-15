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
    TacticalRadar,   // Space / C (الرادار التكتيكي اللحظي على الطلب)
    Header, BicycleKick, Volley, LobShot, CurledShot, Clearance, GroundCross, FarPostCross, RequestLineup, SlowRunUp, LoftedThroughBall
}

public interface IInputHandler
{
    HashSet<GameAction> PollCurrentActions(bool isPossession);
    bool IsActionTriggered(GameAction action, bool isPossession);
    void HandleKeyDown(int keyCode, bool isPossession) { }
    void HandleKeyUp(int keyCode) { }
    void Reset() { }
    /// <summary>Current in-progress hold-charge fraction for a chargeable action (0..1), or -1 when that action is not being charged.</summary>
    float ActiveCharge01(GameAction action) => -1f;
    /// <summary>Charge fraction (0..1) of a charged action released in the latest poll; consumes it so each release applies once.</summary>
    float ConsumeReleasedCharge01(GameAction action) => 0f;
}
