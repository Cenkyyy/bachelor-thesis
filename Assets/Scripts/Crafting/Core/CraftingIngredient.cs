using System;
using UnityEngine;

/// <summary>
/// Represents an ingredient and its required quantity for a crafting recipe.
/// </summary>
[Serializable]
public struct CraftingIngredient
{
    [field: Header("Ingredient")]
    [field: SerializeField] public ItemData Item { get; private set; }
    [field: SerializeField] public int Amount { get; private set; }

    public CraftingIngredient(ItemData item, int amount)
    {
        Item = item;
        Amount = Mathf.Max(1, amount);
    }

    public InventoryItem ToInventoryItem() => new InventoryItem(Item, Amount);
}
