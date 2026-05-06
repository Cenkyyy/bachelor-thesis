using System;
using UnityEngine;

/// <summary>
/// Player-owned inventory that is split into hotbar and backpack.
/// </summary>
[System.Serializable]
public sealed class PlayerInventory : IInventory
{
    private const int MinHotbarSize = 2;
    private const int MaxHotbarSize = 12;
    private const int DefaultHotbarSize = 8;

    private const int MinBackpackSize = 8;
    private const int MaxBackpackSize = 40;
    private const int DefaultBackpackSize = 24;

    public int HotbarSize { get; private set; } =  DefaultHotbarSize;
    public int BackpackSize { get; private set; } =  DefaultBackpackSize;
    public int Capacity => _inventory.Capacity;

    public int SelectedHotbarIndex { get; private set; }
    public event Action<int> OnHotbarSelectionChanged;

    public event Action<int> OnItemChanged 
    {
        add => _inventory.OnItemChanged += value;
        remove => _inventory.OnItemChanged -= value;
    }

    private readonly Inventory _inventory;

    public PlayerInventory(int hotbarSize, int backpackSize)
    {
        HotbarSize = Mathf.Clamp(hotbarSize, MinHotbarSize, MaxHotbarSize);
        BackpackSize = Mathf.Clamp(backpackSize, MinBackpackSize, MaxBackpackSize);
        _inventory = new Inventory(HotbarSize + BackpackSize);
    }

    public void InitializeFrom(PlayerData data)
    {
        if (data == null)
            return;

        // fill hotbar with starting hotbar items (indices 0-7), truncate if more than hotbarSize
        for (int i = 0; i < data.StartingHotbarItems.Count && i < HotbarSize; i++)
        {
            _inventory.SetItemAt(i, data.StartingHotbarItems[i]);
        }

        // fill inventory with starting inventory items (indices 8-31), truncate if more than inventorySize
        for (int i = 0; i < data.StartingBackpackItems.Count && i < BackpackSize; i++)
        {
            _inventory.SetItemAt(i + HotbarSize, data.StartingBackpackItems[i]);
        }
    }

    public void SelectHotbar(int index)
    {
        if (index < 0 || index >= HotbarSize || index == SelectedHotbarIndex)
            return;
        
        SelectedHotbarIndex = index;
        OnHotbarSelectionChanged?.Invoke(index);
    }

    public SlotRange GetBackpackSlotRange() => new SlotRange(HotbarSize, HotbarSize + BackpackSize);
    public InventoryItem GetItemAt(int index) => _inventory.GetItemAt(index);
    public void SetItemAt(int index, InventoryItem item) => _inventory.SetItemAt(index, item);
    public void ClearItemAt(int index) => _inventory.ClearItemAt(index);
    public bool TryAddItemToRange(InventoryItem item, SlotRange range, out InventoryItem leftover) => _inventory.TryAddItemToRange(item, range, out leftover);
    public bool TryRemoveFromRange(InventoryItem item, SlotRange range, out InventoryItem removed) => _inventory.TryRemoveFromRange(item, range, out removed);
    public bool TryMergeInto(InventoryItem item, int toIndex, out InventoryItem leftover) => _inventory.TryMergeInto(item, toIndex, out leftover);
}
