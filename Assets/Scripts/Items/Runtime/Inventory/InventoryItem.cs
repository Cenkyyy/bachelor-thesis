using System;
using UnityEngine;

[Serializable]
public struct InventoryItem
{
    [field: SerializeField] public ItemData Item { get; private set; }
    [field: SerializeField] public int Amount { get; private set; }

    public bool IsEmpty => Item == null || Amount <= 0;
    public static InventoryItem Empty => default;

    public InventoryItem(ItemData item, int amount = 1)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        Item = item;
        Amount = Mathf.Max(0, amount);
    }

    public InventoryItem WithAmount(int newAmount)
    {
        return newAmount <= 0 ? Empty : new InventoryItem(Item, newAmount);
    }
}
