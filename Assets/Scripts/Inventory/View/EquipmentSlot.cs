using UnityEngine;

/// <summary>
/// UI slot for an equipment position. Inherits all visuals from Slot, adds a typed slot indicator.
/// </summary>
public sealed class EquipmentSlot : Slot
{
    [field: Header("Equipment")]
    [field: SerializeField] public EquipmentType SlotType { get; private set; }
}
