using UnityEngine;

[CreateAssetMenu(menuName = "Game/Input/Panel Keybinds", fileName = "MajorPanelKeybinds")]
public sealed class MajorPanelKeybinds : ScriptableObject
{
    public KeyCode Inventory = KeyCode.E;
    public KeyCode Map = KeyCode.M;
    public KeyCode ParallelWorld = KeyCode.P;
    public KeyCode Crafting = KeyCode.B;
    public KeyCode CloseOrPause = KeyCode.Escape;
}
