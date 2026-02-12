using System;
using UnityEngine;

[Serializable]
public struct CraftingIngredient
{
    [field: SerializeField] public Item Item { get; private set; }
    [field: SerializeField] public int Amount { get; private set; }

    public CraftingIngredient(Item item, int amount)
    {
        Item = item;
        Amount = Mathf.Max(1, amount);
    }

    public InventoryItem ToInventoryItem() => new InventoryItem(Item, Amount);
}