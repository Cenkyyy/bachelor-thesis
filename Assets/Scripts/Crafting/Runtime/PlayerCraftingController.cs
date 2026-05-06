using System;
using UnityEngine;

/// <summary>
/// Executes player crafting requests by validating recipes, consuming ingredients,
/// and adding crafted output to the player's inventory or dropping overflow into the world.
/// </summary>
public sealed class PlayerCraftingController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Player _player;
    [SerializeField] private WorldItemSpawner _itemDropSpawner;

    public event Action<CraftingRecipeData> OnCraftStarted;
    public event Action<CraftingRecipeData> OnCraftCompleted;

    public bool CanCraft(CraftingRecipeData recipe)
    {
        return TryResolveCraftingState(recipe, out _, out _);
    }

    public bool TryCraft(CraftingRecipeData recipe)
    {
        if (!TryResolveCraftingState(recipe, out var inventory, out var output))
            return false;

        OnCraftStarted?.Invoke(recipe);

        ConsumeIngredients(inventory, recipe);
        AddOutputToInventory(inventory, output);
        OnCraftCompleted?.Invoke(recipe);
        return true;
    }

    private bool TryResolveCraftingState(CraftingRecipeData recipe, out IInventory inventory, out InventoryItem output)
    {
        inventory = null;
        output = InventoryItem.Empty;

        if (recipe == null || _player == null || _player.Inventory == null)
            return false;

        output = recipe.GetOutputStack();
        if (output.IsEmpty)
            return false;

        inventory = _player.Inventory;
        return CraftingInventoryUtility.HasIngredients(inventory, recipe.Ingredients);
    }

    private void ConsumeIngredients(IInventory inventory, CraftingRecipeData recipe)
    {
        var range = new SlotRange(0, inventory.Capacity);

        foreach (var ingredient in recipe.Ingredients)
        {
            if (ingredient.Item == null || ingredient.Amount <= 0)
                continue;

            var toRemove = ingredient.ToInventoryItem();
            inventory.TryRemoveFromRange(toRemove, range, out _);
        }
    }

    private void AddOutputToInventory(IInventory inventory, InventoryItem output)
    {
        var range = new SlotRange(0, inventory.Capacity);
        inventory.TryAddItemToRange(output, range, out var leftover);
        ItemPickupFeedReporter.ReportAddedToInventory(output, leftover);
            
        if (!leftover.IsEmpty)
            SpawnOverflow(leftover);
    }

    private void SpawnOverflow(InventoryItem item)
    {
        if (_itemDropSpawner == null || _player == null)
            return;

        _itemDropSpawner.Spawn(item, _player.transform.position);
    }
}
