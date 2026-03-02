using System;
using UnityEngine;

/// <summary>
/// Strictly-typed equipment storage for the player. Capacity is fixed to the number of equipment slots.
/// Uses per-slot acceptance rules (e.g., only helmet goes into Helmet slot).
/// </summary>
[Serializable]
public sealed class EquipmentInventory : IInventory
{
    public int Capacity => _items.Length;

    public event Action<int> OnItemChanged;

    private readonly InventoryItem[] _items;
    private readonly EquipmentSlotType[] _slotLayout;

    /// <summary>
    /// Default layout: [Helmet, Chest, Legs, Boots, Necklace, RingLeft, RingRight, Amulet]
    /// </summary>
    public EquipmentInventory()
    {
        _slotLayout = new[]
        {
            EquipmentSlotType.Helmet,
            EquipmentSlotType.Chest,
            EquipmentSlotType.Legs,
            EquipmentSlotType.Boots,
            EquipmentSlotType.Necklace,
            EquipmentSlotType.RingLeft,
            EquipmentSlotType.RingRight,
            EquipmentSlotType.Amulet
        };

        _items = new InventoryItem[_slotLayout.Length];
        for (int i = 0; i < _items.Length; i++)
        {
            _items[i] = InventoryItem.Empty;
        }
    }

    public InventoryItem GetItemAt(int index)
    {
        if (!IsValidIndex(index)) return InventoryItem.Empty;
        return _items[index];
    }

    /// <summary>
    /// Directly sets the item at index if valid for the equipment slot type.
    /// </summary>
    public void SetItemAt(int index, InventoryItem item)
    {
        if (!IsValidIndex(index)) return;

        if (!item.IsEmpty && !IsItemAllowedAtIndex(item, index))
        {
            return;
        }

        _items[index] = item;
        OnItemChanged?.Invoke(index);
    }

    public void ClearItemAt(int index)
    {
        if (!IsValidIndex(index)) return;
        if (_items[index].IsEmpty) return;

        _items[index] = InventoryItem.Empty;
        OnItemChanged?.Invoke(index);
    }

    /// <summary>
    /// Adds a single equipment piece into the first compatible empty slot within the given range.
    /// (Equipment is non-stackable.)
    /// </summary>
    public bool TryAddItemToRange(InventoryItem item, SlotRange range, out InventoryItem leftoverItem)
    {
        leftoverItem = item;
        if (item.IsEmpty) return true;

        if (!IsEquipment(item.Item, out var equip)) return false;

        // find first compatible empty slot in [start, end)
        for (int i = range.StartInclusive; i < range.EndExclusive && i < Capacity; i++)
        {
            if (!IsItemAllowedAtIndex(item, i)) continue;
            if (!_items[i].IsEmpty) continue;

            _items[i] = new InventoryItem(item.Item, 1);
            OnItemChanged?.Invoke(i);
            leftoverItem = item.WithAmount(item.Amount - 1);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes up to 'item.Amount' of the given equipment item anywhere within range (effectively 0 or 1).
    /// </summary>
    public bool TryRemoveFromRange(InventoryItem item, SlotRange range, out InventoryItem removedItem)
    {
        removedItem = InventoryItem.Empty;
        if (item.IsEmpty) return true;

        for (int i = range.StartInclusive; i < range.EndExclusive && i < Capacity; i++)
        {
            if (_items[i].IsEmpty) continue;
            if (_items[i].Item == item.Item)
            {
                removedItem = _items[i];
                _items[i] = InventoryItem.Empty;
                OnItemChanged?.Invoke(i);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// For equipment, merge == place if the target empty slot is compatible; otherwise no-op.
    /// </summary>
    public bool TryMergeInto(InventoryItem item, int toIndex, out InventoryItem leftoverItem)
    {
        leftoverItem = item;
        if (item.IsEmpty) return true;
        if (!IsValidIndex(toIndex)) return false;
        if (!_items[toIndex].IsEmpty) return false;

        if (!IsItemAllowedAtIndex(item, toIndex)) return false;

        _items[toIndex] = new InventoryItem(item.Item, 1);
        OnItemChanged?.Invoke(toIndex);
        leftoverItem = item.WithAmount(item.Amount - 1);
        return true;
    }

    private bool IsValidIndex(int i) => i >= 0 && i < Capacity;

    private static bool IsEquipment(ItemData item, out EquipmentItem eq)
    {
        eq = item as EquipmentItem;
        return eq != null;
    }

    private bool IsItemAllowedAtIndex(InventoryItem item, int index)
    {
        if (!IsEquipment(item.Item, out var equip)) return false;
        var slotType = _slotLayout[index];

        if (equip.Slot == EquipmentSlotType.RingLeft || equip.Slot == EquipmentSlotType.RingRight)
        {
            // Rings are mutually compatible: a "ring" scriptable object can target either left or right.
            return slotType == EquipmentSlotType.RingLeft || slotType == EquipmentSlotType.RingRight;
        }

        // If the SO declares generic "RingLeft/Right" we also accept the generic ring check above.
        // Otherwise, it must match exactly.
        return equip.Slot == slotType;
    }
}
