using UnityEngine;

[CreateAssetMenu(menuName = "Game/Input/Spellcasting Input Bindings", fileName = "SpellCastingInputBindings")]
public sealed class SpellCastingInputBindingsData : ScriptableObject
{
    [field: SerializeField] public KeyCode Slot1 { get; private set; } = KeyCode.Alpha1;
    [field: SerializeField] public KeyCode Slot2 { get; private set; } = KeyCode.Alpha2;
    [field: SerializeField] public KeyCode Slot3 { get; private set; } = KeyCode.Alpha3;
    [field: SerializeField] public KeyCode Slot4 { get; private set; } = KeyCode.Alpha4;
    [field: SerializeField] public KeyCode Slot5 { get; private set; } = KeyCode.Alpha5;

    public int? TryGetPressedIndex()
    {
        if (Input.GetKeyDown(Slot1)) return 0;
        if (Input.GetKeyDown(Slot2)) return 1;
        if (Input.GetKeyDown(Slot3)) return 2;
        if (Input.GetKeyDown(Slot4)) return 3;
        if (Input.GetKeyDown(Slot5)) return 4;
        return null;
    }
}
