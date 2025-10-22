using UnityEngine;

[CreateAssetMenu(fileName = "NewEquipmentItem", menuName = "Items/Equipment Item")]
public sealed class EquipmentItem : Item
{
    [field: SerializeField] public EquipmentSlotType Slot { get; private set; }
}
