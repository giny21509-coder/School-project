using UnityEngine;

/// <summary>
/// 第六章的按鍵集中在這裡（沿用專案目前的舊版 Input 寫法；Active Input Handling = Both）。
/// TODO（設計文件 §9.6）：改成 InputSystem_Actions 的 Listen（Hold）與 Interact，提示文字從綁定產生。
/// 鍵位待組內確認（設計文件 §12-1）。
/// </summary>
public static class Ch06Input
{
    public static KeyCode ListenKey = KeyCode.Q;
    public static KeyCode ListenPadButton = KeyCode.JoystickButton4; // LB
    public static KeyCode InteractKey = KeyCode.R;                   // 與探索互動鍵一致
    public static KeyCode InteractPadButton = KeyCode.JoystickButton2; // X
    public static KeyCode EatFromPouchKey = KeyCode.H;               // 與貓咪章補血鍵一致

    public static bool ListenHeld => Input.GetKey(ListenKey) || Input.GetKey(ListenPadButton);
    public static bool InteractPressed => Input.GetKeyDown(InteractKey) || Input.GetKeyDown(InteractPadButton);
    public static bool EatFromPouchPressed => Input.GetKeyDown(EatFromPouchKey);
}
