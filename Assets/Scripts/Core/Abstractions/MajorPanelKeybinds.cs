using UnityEngine;

[CreateAssetMenu(menuName = "Game/Input/Panel Keybinds", fileName = "MajorPanelKeybinds")]
public sealed class MajorPanelKeybinds : ScriptableObject
{
    public KeyCode Inventory = KeyCode.E;
    public KeyCode Map = KeyCode.M;
    public KeyCode Crafting = KeyCode.C;
    public KeyCode CloseOrPause = KeyCode.Escape;
}
