using System.Collections.Generic;

public static class CraftingInventoryUtility
{
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

    public static Dictionary<Item, int> GetMissingIngredients(IReadOnlyInventory inventory, IReadOnlyList<CraftingIngredient> ingredients)
    {
        var missing = new Dictionary<Item, int>();
        if (inventory == null || ingredients == null)
            return missing;

        foreach (var ingredient in ingredients)
        {
            if (ingredient.Item == null)
                continue;

            var available = CountItem(inventory, ingredient.Item);
            if (available < ingredient.Amount)
            {
                missing[ingredient.Item] = ingredient.Amount - available;
            }
        }

        return missing;
    }

    public static int CountItem(IReadOnlyInventory inventory, Item item)
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

    public static bool CanFitOutput(IReadOnlyInventory inventory, InventoryItem output)
    {
        if (inventory == null || output.IsEmpty)
            return false;

        var remaining = output.Amount;
        for (int i = 0; i < inventory.Capacity && remaining > 0; i++)
        {
            var slotItem = inventory.GetItemAt(i);
            if (slotItem.IsEmpty)
            {
                if (output.Item.IsStackable)
                {
                    remaining -= System.Math.Min(remaining, output.Item.MaxStackSize);
                }
                else
                {
                    remaining -= 1;
                }
                continue;
            }

            if (!output.Item.IsStackable || slotItem.Item != output.Item)
                continue;

            var freeSpace = output.Item.MaxStackSize - slotItem.Amount;
            if (freeSpace <= 0)
                continue;

            remaining -= System.Math.Min(remaining, freeSpace);
        }

        return remaining <= 0;
    }
}