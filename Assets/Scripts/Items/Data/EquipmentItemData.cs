using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEquipmentItem", menuName = "Items/Equipment Item")]
public sealed class EquipmentItemData : ItemData
{
    private const EquipmentType AccessorySlots = EquipmentType.Necklace | EquipmentType.RingLeft | EquipmentType.RingRight | EquipmentType.Amulet;

    [field: Header("Equipment Settings")]
    [field: SerializeField] public EquipmentType Slot { get; private set; }
    [field: SerializeField] public ToolTier ProgressionTier { get; private set; } = ToolTier.Copper;

    [field: Header("Status Effects")]
    [SerializeField] private List<ItemStatusEffect> _statusEffects = new();

    public IReadOnlyList<ItemStatusEffect> StatusEffect => _statusEffects;
    public bool HasProgressionTier => !IsAccessorySlot(Slot) && ProgressionTier != ToolTier.None;

    protected override ItemType? ExpectedCategory => IsAccessorySlot(Slot) ? ItemType.Accessory : ItemType.Armor;

    protected override void OnValidate()
    {
        base.OnValidate();

        if (IsAccessorySlot(Slot) && ProgressionTier != ToolTier.None)
            ProgressionTier = ToolTier.None;
    }

    private bool IsAccessorySlot(EquipmentType slot) => (slot & AccessorySlots) != 0;
}
