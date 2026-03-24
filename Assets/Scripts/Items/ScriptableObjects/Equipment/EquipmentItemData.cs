using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEquipmentItem", menuName = "Items/Equipment Item")]
public sealed class EquipmentItemData : ItemData
{
    [field: SerializeField] public EquipmentSlotType Slot { get; private set; }
    [field: SerializeField] public ToolTier ProgressionTier { get; private set; } = ToolTier.Copper;

    [SerializeField] private List<ItemStatModifier> _statBonuses = new();

    public IReadOnlyList<ItemStatModifier> StatBonuses => _statBonuses;
    public bool HasProgressionTier => !IsAccessorySlot(Slot) && ProgressionTier != ToolTier.None;

    protected override ItemType? ExpectedCategory => IsAccessorySlot(Slot) ? ItemType.Accessory : ItemType.Armor;

    protected override void OnValidate()
    {
        base.OnValidate();

        if (IsAccessorySlot(Slot) && ProgressionTier != ToolTier.None)
            ProgressionTier = ToolTier.None;
    }

    private static bool IsAccessorySlot(EquipmentSlotType slot)
    {
        return slot == EquipmentSlotType.Necklace
               || slot == EquipmentSlotType.RingLeft
               || slot == EquipmentSlotType.RingRight
               || slot == EquipmentSlotType.Amulet;
    }
}
