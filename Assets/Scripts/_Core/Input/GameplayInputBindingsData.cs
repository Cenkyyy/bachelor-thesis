using UnityEngine;

[CreateAssetMenu(menuName = "Game/Input/Gameplay Input Bindings", fileName = "GameplayInputBindings")]
public sealed class GameplayInputBindingsData : ScriptableObject
{
    [field: Header("Action Keys")]
    [field: SerializeField] public KeyCode InteractKey { get; private set; } = KeyCode.Mouse1;
    [field: SerializeField] public KeyCode PlacementKey { get; private set; } = KeyCode.Mouse1;
    [field: SerializeField] public KeyCode MiningKey { get; private set; } = KeyCode.Mouse0;
    [field: SerializeField] public KeyCode ConsumeKey { get; private set; } = KeyCode.Mouse1;
    [field: SerializeField] public KeyCode DropKey { get; private set; } = KeyCode.Q;
    [field: SerializeField] public KeyCode DropAllModifierKey { get; private set; } = KeyCode.LeftControl;
}
