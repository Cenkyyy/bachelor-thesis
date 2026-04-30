using System;
using UnityEngine;

/// <summary>
/// Represents a runtime inventory stack item.
/// Contains a reference to <see cref="ItemData"/> and keeps track
/// of the current amount of the item in the stack.
/// </summary>
[Serializable]
public struct InventoryItem
{
    [field: Header("Item stack data")]
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

    public InventoryItem WithAmount(int newAmount) => newAmount <= 0 ? Empty : new InventoryItem(Item, newAmount);
}
