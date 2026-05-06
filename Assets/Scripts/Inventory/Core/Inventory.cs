using System;
using UnityEngine;

/// <summary>
/// Stores inventory items in a fixed-size array and provides basic stack operations from the IInventory interface.
/// </summary>
[Serializable]
public class Inventory : IInventory
{
    private readonly InventoryItem[] _items;

    public int Capacity => _items.Length;
    public event Action<int> OnItemChanged;

    public Inventory(int capacity)
    {
        var clampedCapacity = Mathf.Max(1, capacity);
        _items = new InventoryItem[clampedCapacity];

        for (int i = 0; i < _items.Length; i++)
        {
            _items[i] = InventoryItem.Empty;
        }
    }

    public InventoryItem GetItemAt(int index)
    {
        if (index < 0 || index >= _items.Length)
            return InventoryItem.Empty;

        return _items[index];
    }

    public void SetItemAt(int index, InventoryItem item)
    {
        if (index < 0 || index >= _items.Length)
            return;

        if (_items[index].Item != item.Item || _items[index].Amount != item.Amount)
        {
            _items[index] = item;
            OnItemChanged?.Invoke(index);
        }
    }

    public void ClearItemAt(int index)
    {
        if (index < 0 || index >= _items.Length)
            return;

        if (!_items[index].IsEmpty)
        {
            _items[index] = InventoryItem.Empty;
            OnItemChanged?.Invoke(index);
        }
    }

    public bool TryAddItemToRange(InventoryItem item, SlotRange range, out InventoryItem leftoverItem)
    {
        if (!IsValidRange(range))
        {
            leftoverItem = item;
            return false;
        }

        if (item.IsEmpty)
        {
            leftoverItem = InventoryItem.Empty;
            return true;
        }

        // try stacking into existing stacks
        if (item.Item.IsStackable)
        {
            for (int i = range.StartInclusive; i < range.EndExclusive; i++)
            {
                if (_items[i].IsEmpty || _items[i].Item != item.Item) 
                    continue;

                // get free space for this stack
                var freeSpace = item.Item.MaxStackSize - _items[i].Amount;
                if (freeSpace <= 0)
                    continue;

                // calculate how much we can move
                var toMove = Math.Min(item.Amount, freeSpace);

                // move it
                _items[i] = _items[i].WithAmount(_items[i].Amount + toMove);
                OnItemChanged?.Invoke(i);

                // reduce from incoming item
                item = item.WithAmount(item.Amount - toMove);
                if (item.IsEmpty)
                {
                    leftoverItem = InventoryItem.Empty;
                    return true;
                }
            }
        }

        // place into first empty slot
        for (int i = range.StartInclusive; i < range.EndExclusive; i++)
        {
            if (_items[i].IsEmpty)
            {
                _items[i] = item;
                OnItemChanged?.Invoke(i);
                leftoverItem = InventoryItem.Empty;
                return true;
            }
        }

        // range full
        leftoverItem = item; 
        return false;
    }

    public bool TryRemoveFromRange(InventoryItem item, SlotRange range, out InventoryItem removedItem)
    {
        removedItem = InventoryItem.Empty;
        
        if (!IsValidRange(range))
            return false;

        if (item.IsEmpty || item.Item == null || item.Amount <= 0)
            return false;

        int remaining = item.Amount;
        int removed = 0;

        for (int i = range.StartInclusive; i < range.EndExclusive && remaining > 0; i++)
        {
            // different item type
            if (_items[i].Item != item.Item)
                continue;

            // calculate how much we can remove from this slot
            var toRemove = Math.Min(_items[i].Amount, remaining);

            // remove it
            _items[i] = _items[i].WithAmount(_items[i].Amount - toRemove);
            OnItemChanged?.Invoke(i);

            // update values 
            removed += toRemove;
            remaining -= toRemove;
        }

        if (removed > 0)
        {
            removedItem = new InventoryItem(item.Item, removed);
            return removed == item.Amount;
        }
        return false;
    }

    public bool TryMergeInto(InventoryItem item, int toIndex, out InventoryItem leftoverItem)
    {
        leftoverItem = item;

        // validation
        if (item.IsEmpty)
            return false;
        if (toIndex < 0 || toIndex >= _items.Length)
            return false;
        if (_items[toIndex].IsEmpty || !_items[toIndex].Item.IsStackable) 
            return false;
        if (_items[toIndex].Item != item.Item) 
            return false;

        // calculate free space in the destination stack
        var free = _items[toIndex].Item.MaxStackSize - _items[toIndex].Amount;
        if (free <= 0) 
            return false;

        // calculate how much we can move
        var toMove = Math.Min(item.Amount, free);

        // move it
        _items[toIndex] = _items[toIndex].WithAmount(_items[toIndex].Amount + toMove);
        OnItemChanged?.Invoke(toIndex);

        // return leftover
        leftoverItem = item.WithAmount(item.Amount - toMove);
        return toMove > 0;
    }

    private bool IsValidRange(SlotRange range) => range.StartInclusive >= 0 && range.EndExclusive <= _items.Length;
}
