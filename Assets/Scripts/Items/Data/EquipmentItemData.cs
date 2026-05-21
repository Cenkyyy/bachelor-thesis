using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEquipmentItem", menuName = "Items/Equipment Item")]
public sealed class EquipmentItemData : ItemData
{
    private const EquipmentType AccessorySlots = EquipmentType.Necklace | EquipmentType.RingLeft | EquipmentType.RingRight | EquipmentType.Amulet;

    [field: Header("Equipment Settings")]
    [field: SerializeField] public EquipmentType Slot { get; private set; }
    [field: SerializeField] public EquipmentTier Tier { get; private set; } = EquipmentTier.Copper;

    [field: Header("Status Effects")]
    [SerializeField] private List<ItemStatusEffect> _statusEffects = new();

    public IReadOnlyList<ItemStatusEffect> StatusEffect => _statusEffects;
    public bool HasProgressionTier => !IsAccessorySlot(Slot);

    protected override ItemType? ExpectedCategory => IsAccessorySlot(Slot) ? ItemType.Accessory : ItemType.Armor;

    private bool IsAccessorySlot(EquipmentType slot) => (slot & AccessorySlots) != 0;
}
