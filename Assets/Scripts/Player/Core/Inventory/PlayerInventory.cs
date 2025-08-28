using System.Collections.Generic;

/// <summary>
/// Holds all items belonging to the player (hotbar + inventory).
/// Acts as the single source for player's item storage.
/// </summary>
[System.Serializable]
public class PlayerInventory
{
    private const int DefaultHotbarSize = 8;
    private const int DefaultInventorySize = 24;

    public int HotbarSize { get; private set; } =  DefaultHotbarSize;
    public int InventorySize { get; private set; } =  DefaultInventorySize;
    public int TotalSize => HotbarSize + InventorySize;

    /// <summary>
    /// Total list of items (hotbar first, then inventory).
    /// Null entries mean empty slots.
    /// </summary>
    private List<Item> _items;

    /// <summary>
    /// Initializes a new instance of PlayerInventory with specified sizes.
    /// </summary>
    /// <param name="hotbarSize">Size of player's hotbar</param>
    /// <param name="inventorySize">Size of player's inventory (backpack)</param>
    public PlayerInventory(int hotbarSize, int inventorySize)
    {
        HotbarSize = hotbarSize;
        InventorySize = inventorySize;

        _items = new List<Item>(TotalSize);
        for (int i = 0; i < TotalSize; i++)
            _items.Add(null);
    }

    /// <summary>
    /// Returns item at a specific slot index.
    /// </summary>
    public Item GetItemAt(int index)
    {
        if (index < 0 || index >= _items.Count)
        {
            return null;
        }
        return _items[index];
    }

    /// <summary>
    /// Sets an item at a specific slot index.
    /// </summary>
    public void SetItemAt(int index, Item item)
    {
        if (index < 0 || index >= _items.Count)
        {
            return;
        }
        _items[index] = item;
    }

    /// <summary>
    /// Clears an item at a specific slot index.
    /// </summary>
    public void ClearItemAt(int index)
    {
        if (index < 0 || index >= _items.Count)
        {
            return;
        }
        _items[index] = null;
    }
}
