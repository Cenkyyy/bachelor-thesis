using UnityEngine;

/// <summary>
/// Authored key bindings used to open and close major HUD panels.
/// </summary>
[CreateAssetMenu(menuName = "Game/Input/Panel Keybinds", fileName = "MajorPanelKeybinds")]
public sealed class MajorPanelKeybindsData : ScriptableObject
{
    [field: Header("Panels")]
    [field: SerializeField] public KeyCode Inventory { get; private set; } = KeyCode.E;
    [field: SerializeField] public KeyCode Map { get; private set; } = KeyCode.M;
    [field: SerializeField] public KeyCode Crafting { get; private set; } = KeyCode.C;
    [field: SerializeField] public KeyCode CloseOrPause { get; private set; } = KeyCode.Escape;
}
