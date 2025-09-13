using System;

/// <summary>
/// Holds all items belonging to the player (hotbar + backpack inventory).
/// Acts as the single source for player's item storage.
/// </summary>
[System.Serializable]
public sealed class PlayerInventory
{
    private const int MinHotbarSize = 2;
    private const int MaxHotbarSize = 12;
    private const int DefaultHotbarSize = 8;

    private const int MinBackpackSize = 8;
    private const int MaxBackpackSize = 40;
    private const int DefaultBackpackSize = 24;

    /// <summary> Current hotbar size (clamped between <see cref="MinHotbarSize"/> and <see cref="MaxHotbarSize"/>). </summary>
    public int HotbarSize { get; private set; } =  DefaultHotbarSize;

    /// <summary> Current backpack size (clamped between <see cref="MinBackpackSize"/> and <see cref="MaxBackpackSize"/>). </summary>
    public int BackpackSize { get; private set; } =  DefaultBackpackSize;

    /// <summary> Total inventory size (hotbar + backpack). </summary>
    public int TotalSize => HotbarSize + BackpackSize;

    /// <summary> Currently selected hotbar index (0..HotbarSize-1). </summary>
    public int SelectedHotbarIndex { get; private set; }

    /// <summary> Raised when the selected hotbar slot changes; args: changedSlotIndex.</summary>
    public event Action<int> OnHotbarSelectionChanged;
    /// <summary> Raised when any slot's content changes; args: changedSlotIndex. </summary>
    public event Action<int> OnItemChanged;

    // Storage for all items (hotbar -> [0..HotbarSize - 1] + backpack -> [HotbarSize..TotalSize])
    private readonly InventoryItem[] _items;

    /// <summary>
    /// Initializes a new instance of PlayerInventory with specified sizes.
    /// </summary>
    /// <param name="hotbarSize">Size of player's hotbar.</param>
    /// <param name="inventorySize">Size of player's backpack.</param>
    public PlayerInventory(int hotbarSize, int inventorySize)
    {
        // clamp sizes to valid ranges
        if (hotbarSize < MinHotbarSize)
            hotbarSize = MinHotbarSize;
        else if (hotbarSize > MaxHotbarSize)
            hotbarSize = MaxHotbarSize;

        if (inventorySize < MinBackpackSize)
            inventorySize = MinBackpackSize;
        else if (inventorySize > MaxBackpackSize)
            inventorySize = MaxBackpackSize;

        HotbarSize = hotbarSize;
        BackpackSize = inventorySize;

        // initialize empty inventory
        _items = new InventoryItem[TotalSize];
        for (int i = 0; i < TotalSize; i++)
        {
            _items[i] = InventoryItem.Empty;
        }
    }

    /// <summary>
    /// Initializes the inventory with starting items from a PlayerDataSO.
    /// Extra items are ignored.
    /// </summary>
    /// <param name="dataSO">Player's default stats, including their starting items.</param>
    public void InitializeFromSO(PlayerDataSO dataSO)
    {
        if (dataSO == null)
            return;

        // fill hotbar with starting hotbar items (indices 0-7), truncate if more than hotbarSize
        for (int i = 0; i < dataSO.startingHotbarItems.Count && i < HotbarSize; i++)
        {
            SetItemAt(i, dataSO.startingHotbarItems[i]);
        }

        // fill inventory with starting inventory items (indices 8-31), truncate if more than inventorySize
        for (int i = 0; i < dataSO.startingBackpackItems.Count && i < BackpackSize; i++)
        {
            SetItemAt(i + HotbarSize, dataSO.startingBackpackItems[i]);
        }
    }

    /// <summary>
    /// Selects a hotbar slot by index (0..HotbarSize-1).
    /// Raises <see cref="OnHotbarSelectionChanged"/> if the selection changed.
    /// </summary>
    /// <param name="index">Chosen hotbar slot's index.</param>
    public void SelectHotbar(int index)
    {
        if (index < 0 || index >= HotbarSize)
            return;

        if (SelectedHotbarIndex != index)
        {
            SelectedHotbarIndex = index;
            OnHotbarSelectionChanged?.Invoke(SelectedHotbarIndex);
        }
    }

    /// <summary>
    /// Returns the item at an absolute slot index.
    /// </summary>
    /// <param name="index">Absolute slot index (0..TotalSize-1).</param>
    /// <returns>The item in the slot, or <see cref="InventoryItem.Empty"/> if out of range.</returns>
    public InventoryItem GetItemAt(int index)
    {
        if (index < 0 || index >= _items.Length)
        {
            return InventoryItem.Empty;
        }
        return _items[index];
    }

    /// <summary>
    /// Sets the item at an absolute slot index and raises <see cref="OnItemChanged"/>.
    /// </summary>
    /// <param name="index">Absolute slot index.</param>
    /// <param name="item">Item to assign.</param>
    public void SetItemAt(int index, InventoryItem item)
    {
        if (index < 0 || index >= _items.Length)
        {
            return;
        }
        if (_items[index].ItemSO != item.ItemSO || _items[index].Amount != item.Amount)
        {
            _items[index] = item;
            OnItemChanged?.Invoke(index);
        }
    }

    /// <summary>
    /// Clears the item at an absolute slot index (sets to <see cref="InventoryItem.Empty"/>) and raises <see cref="OnItemChanged"/>.
    /// </summary>
    /// <param name="index">Absolute slot index.</param>
    public void ClearItemAt(int index)
    {
        if (index < 0 || index >= _items.Length)
        {
            return;
        }
        if (!_items[index].IsEmpty)
        {
            _items[index] = InventoryItem.Empty;
            OnItemChanged?.Invoke(index);
        }
    }

    /// <summary>
    /// Tries to add an item into a inventory's slot range [rangeStart, rangeEndExclusive).
    /// First tries stacking into existing same type before placing into the first empty slot.
    /// Raises <see cref="OnItemChanged"/> for each changed slot.
    /// </summary>
    /// <param name="item">Item to add (may be partially consumed).</param>
    /// <param name="rangeStartInclusive">Inclusive start index.</param>
    /// <param name="rangeEndExclusive">Exclusive end index.</param>
    /// <returns>Empty item if item was successfully placed, otherwise the rest of the item.</returns>
    private InventoryItem TryAddItemCore(InventoryItem item, int rangeStartInclusive, int rangeEndExclusive)
    {
        if (item.IsEmpty)
            return InventoryItem.Empty;

        // try stacking into existing stacks
        if (item.ItemSO.IsStackable)
        {
            for (int i = rangeStartInclusive; i < rangeEndExclusive; i++)
            {
                if (!IsSameItem(_items[i], item)) 
                    continue;

                // get free space for this stack
                int freeSpace = item.ItemSO.MaxStackSize - _items[i].Amount;
                if (freeSpace <= 0)
                    continue;

                // calculate how much we can move
                int toMove = Math.Min(item.Amount, freeSpace);

                // move it
                _items[i] = _items[i].WithAmount(_items[i].Amount + toMove);
                OnItemChanged?.Invoke(i);

                // reduce from incoming item
                item = item.WithAmount(item.Amount - toMove);
                if (item.IsEmpty)
                    return InventoryItem.Empty;
            }
        }

        // place into first empty slot
        for (int i = rangeStartInclusive; i < rangeEndExclusive; i++)
        {
            if (_items[i].IsEmpty)
            {
                _items[i] = item;
                OnItemChanged?.Invoke(i);
                return InventoryItem.Empty;
            }
        }

        // range full
        return item;
    }

    /// <summary>
    /// Tries to add an item into a inventory's slot range [rangeStart, rangeEndExclusive).
    /// First tries stacking into existing same type before placing into the first empty slot.
    /// Raises <see cref="OnItemChanged"/> for each changed slot.
    /// </summary>
    /// <param name="item">Item to add (may be partially consumed).</param>
    /// <param name="rangeStartinclusive">Inclusive start index.</param>
    /// <param name="rangeEndExclusive">Exclusive end index.</param>
    /// <returns>true if the item was fully placed; otherwise false (range ran out of space).</returns>
    public bool TryAddItemToRange(InventoryItem item, int rangeStartinclusive, int rangeEndExclusive)
    {
        var leftover = TryAddItemCore(item, rangeStartinclusive, rangeEndExclusive);
        return leftover.IsEmpty;
    }

    public InventoryItem TryAddItem(InventoryItem item, int rangeStartinclusive, int rangeEndExclusive)
    {
        return TryAddItemCore(item, rangeStartinclusive, rangeEndExclusive);
    }

    /// <summary>
    /// Attempts to remove up to <paramref name="amount"/> of a given item type from a range.
    /// Raisses <see cref="OnItemChanged"/> for each changed slot.
    /// </summary>
    /// <param name="itemSO">Item type to remove.</param>
    /// <param name="amount">Desired amount to remove.</param>
    /// <param name="rangeStart">Inclusive start index.</param>
    /// <param name="rangeEndExclusive">Exclusive end index.</param>
    /// <returns>The actual amount removed (0..amount).</returns>
    public int TryRemoveItemFromRange(ItemBaseSO itemSO, int amount, int rangeStart, int rangeEndExclusive)
    {
        if (itemSO == null || amount <= 0)
            return 0;

        int removed = 0;
        for (int i = rangeStart; i < rangeEndExclusive && removed < amount; i++)
        {
            // different item type
            if (_items[i].ItemSO != itemSO) 
                continue;

            // calculate how much we can remove from this slot
            int toRemove = Math.Min(_items[i].Amount, amount - removed);

            // remove it
            _items[i] = _items[i].WithAmount(_items[i].Amount - toRemove);
            OnItemChanged?.Invoke(i);

            // increase removed count 
            removed += toRemove;
        }

        return removed;
    }

    /// <summary>
    /// Merges an incoming (held) item into the existing stack in <paramref name="toIndex"/> (if compatible).
    /// Does not place into empty slots—caller should handle that scenario.
    /// </summary>
    /// <param name="toIndex">Destination index (must contain a compatible stack).</param>
    /// <param name="item">The held stack to merge.</param>
    /// <returns>The leftover portion of <paramref name="item"/> that did not fit.</returns>
    public InventoryItem TryMergeIntoSlot(int toIndex, InventoryItem item)
    {
        if (item.IsEmpty) 
            return item;
        if (toIndex < 0 || toIndex >= _items.Length)
            return item;
        if (_items[toIndex].IsEmpty)
            return item;
        if (_items[toIndex].ItemSO != item.ItemSO || !_items[toIndex].ItemSO.IsStackable)
            return item;

        // calculate free space in the destination stack
        int freeSpace = _items[toIndex].ItemSO.MaxStackSize - _items[toIndex].Amount;
        if (freeSpace <= 0) return item;

        // calculate how much we can move
        int toMove = Math.Min(item.Amount, freeSpace);

        // move it
        _items[toIndex] = _items[toIndex].WithAmount(_items[toIndex].Amount + toMove);
        OnItemChanged?.Invoke(toIndex);

        // return leftover
        return item.WithAmount(item.Amount - toMove);
    }

    /// <summary>
    /// Returns if items <paramref name="a"/> and <paramref name="b"/> are the same type and neither is empty.
    /// </summary>
    /// <param name="a">First item.</param>
    /// <param name="b">Second item.</param>
    /// <returns></returns>
    private static bool IsSameItem(InventoryItem a, InventoryItem b) => !a.IsEmpty && !b.IsEmpty && a.ItemSO == b.ItemSO;
}
