using System.Collections.Generic;

/// <summary>
/// Provides utility methods for querying and validating crafting inventory contents.
/// </summary>
public static class CraftingInventoryUtility
{
    /// <summary>
    /// Returns true when the inventory contains every required recipe ingredient.
    /// </summary>
    public static bool HasIngredients(IReadOnlyInventory inventory, IReadOnlyList<CraftingIngredient> ingredients)
    {
        if (inventory == null || ingredients == null)
            return false;

        foreach (var ingredient in ingredients)
        {
            if (ingredient.Item == null)
                return false;

            if (CountItem(inventory, ingredient.Item) < ingredient.Amount)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Counts all stacks of the given item across the inventory.
    /// </summary>
    public static int CountItem(IReadOnlyInventory inventory, ItemData item)
    {
        if (inventory == null || item == null)
            return 0;

        var total = 0;
        for (int i = 0; i < inventory.Capacity; i++)
        {
            var stack = inventory.GetItemAt(i);
            if (stack.IsEmpty || stack.Item != item)
                continue;
            total += stack.Amount;
        }

        return total;
    }
}
