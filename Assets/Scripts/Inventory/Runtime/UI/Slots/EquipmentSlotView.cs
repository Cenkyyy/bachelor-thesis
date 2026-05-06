using UnityEngine;

/// <summary>
/// Inventory slot view for a specific equipment type.
/// </summary>
public sealed class EquipmentSlotView : InventorySlotView
{
    [field: Header("Equipment")]
    [field: SerializeField] public EquipmentType SlotType { get; private set; }
}
