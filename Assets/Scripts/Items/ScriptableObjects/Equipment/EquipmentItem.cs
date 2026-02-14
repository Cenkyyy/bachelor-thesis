using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEquipmentItem", menuName = "Items/Equipment Item")]
public sealed class EquipmentItem : Item
{
    [field: SerializeField] public EquipmentSlotType Slot { get; private set; }
    [field: SerializeField] public ToolTier ProgressionTier { get; private set; } = ToolTier.Copper;
    [field: SerializeField] public int ArmorValue { get; private set; }
    [SerializeField] private List<ItemStatModifier> _statBonuses = new();

    public IReadOnlyList<ItemStatModifier> StatBonuses => _statBonuses;

    protected override ItemType? ExpectedCategory => IsAccessorySlot(Slot) ? ItemType.Accessory : ItemType.Armor;

    protected override void OnValidate()
    {
        base.OnValidate();

        if (ArmorValue < 0)
            ArmorValue = 0;
    }

    private static bool IsAccessorySlot(EquipmentSlotType slot)
    {
        return slot == EquipmentSlotType.Necklace
               || slot == EquipmentSlotType.RingLeft
               || slot == EquipmentSlotType.RingRight
               || slot == EquipmentSlotType.Amulet;
    }
}
