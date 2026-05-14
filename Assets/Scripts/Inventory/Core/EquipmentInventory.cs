using System;
using UnityEngine;

/// <summary>
/// Equipment storage for the player. Capacity is fixed to the number of equipment slots.
/// Uses per-slot acceptance rules (e.g. only helmet goes into Helmet slot).
/// </summary>
[Serializable]
public sealed class EquipmentInventory : IInventory
{
    public int Capacity => _items.Length;

    public event Action<int> OnItemChanged;

    private readonly InventoryItem[] _items;
    private readonly EquipmentType[] _slotLayout;

    /// <summary>
    /// Default layout: [Helmet, Chest, Legs, Boots, Necklace, RingLeft, RingRight, Amulet]
    /// </summary>
    public EquipmentInventory()
    {
        _slotLayout = new[]
        {
            EquipmentType.Helmet,
            EquipmentType.Chest,
            EquipmentType.Legs,
            EquipmentType.Boots,
            EquipmentType.Necklace,
            EquipmentType.RingLeft,
            EquipmentType.RingRight,
            EquipmentType.Amulet
        };

        _items = new InventoryItem[_slotLayout.Length];
        for (int i = 0; i < _items.Length; i++)
        {
            _items[i] = InventoryItem.Empty;
        }
    }

    public InventoryItem GetItemAt(int index)
    {
        if (!IsValidIndex(index))
            return InventoryItem.Empty;
        
        return _items[index];
    }

    public bool TryGetIndexForSlotType(EquipmentType slotType, out int index)
    {
        for (int i = 0; i < _slotLayout.Length; i++)
        {
            if (_slotLayout[i] != slotType)
                continue;

            index = i;
            return true;
        }

        index = -1;
        return false;
    }

    /// <summary>
    /// Directly sets the item at index if valid for the equipment slot type.
    /// </summary>
    public void SetItemAt(int index, InventoryItem item)
    {
        if (!IsValidIndex(index))
            return;

        if (!item.IsEmpty && !IsItemAllowedAtIndex(item, index))
            return;

        _items[index] = item;
        OnItemChanged?.Invoke(index);
    }

    public void ClearItemAt(int index)
    {
        if (!IsValidIndex(index))
            return;
        
        if (_items[index].IsEmpty)
            return;

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
        if (!IsValidRange(range))
            return false;

        if (item.IsEmpty)
            return true;

        if (!IsEquipment(item.Item, out _))
            return false;

        // find first compatible empty slot in [start, end)
        for (int i = range.StartInclusive; i < range.EndExclusive && i < Capacity; i++)
        {
            if (!IsItemAllowedAtIndex(item, i))
                continue;
            
            if (!_items[i].IsEmpty)
                continue;

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
        if (!IsValidRange(range))
            return false;

        if (item.IsEmpty)
            return true;

        for (int i = range.StartInclusive; i < range.EndExclusive && i < Capacity; i++)
        {
            if (_items[i].IsEmpty)
                continue;
            
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
        if (item.IsEmpty)
            return true;
        
        if (!IsValidIndex(toIndex))
            return false;
        
        if (!_items[toIndex].IsEmpty)
            return false;

        if (!IsItemAllowedAtIndex(item, toIndex))
            return false;

        _items[toIndex] = new InventoryItem(item.Item, 1);
        OnItemChanged?.Invoke(toIndex);
        leftoverItem = item.WithAmount(item.Amount - 1);
        return true;
    }

    private bool IsValidIndex(int i) => i >= 0 && i < Capacity;

    private bool IsValidRange(SlotRange range) => range.StartInclusive >= 0 && range.EndExclusive <= Capacity;

    private static bool IsEquipment(ItemData item, out EquipmentItemData equipmentItemData)
    {
        equipmentItemData = item as EquipmentItemData;
        return equipmentItemData != null;
    }

    private bool IsItemAllowedAtIndex(InventoryItem item, int index)
    {
        if (!IsEquipment(item.Item, out var equip))
            return false;

        var slotType = _slotLayout[index];
        return (equip.Slot & slotType) != 0;
    }
}
