using System;
using UnityEngine;

public sealed class CraftingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player _player;
    [SerializeField] private WorldItemSpawner _itemDropSpawner;

    public event Action<CraftingRecipeData> OnCraftStarted;
    public event Action<CraftingRecipeData> OnCraftCompleted;
    public event Action<CraftingRecipeData, CraftingFailureReason> OnCraftFailed;

    public bool TryStartCraft(CraftingRecipeData recipe)
    {
        if (recipe == null)
        {
            OnCraftFailed?.Invoke(recipe, CraftingFailureReason.InvalidRecipe);
            return false;
        }

        if (_player == null || _player.Inventory == null)
        {
            OnCraftFailed?.Invoke(recipe, CraftingFailureReason.InvalidRecipe);
            return false;
        }

        var inventory = _player.Inventory;
        var output = recipe.GetOutputStack();
        if (output.IsEmpty)
        {
            OnCraftFailed?.Invoke(recipe, CraftingFailureReason.InvalidRecipe);
            return false;
        }

        if (!CraftingInventoryUtility.HasIngredients(inventory, recipe.Ingredients))
        {
            OnCraftFailed?.Invoke(recipe, CraftingFailureReason.MissingIngredients);
            return false;
        }

        ConsumeIngredients(inventory, recipe);

        OnCraftStarted?.Invoke(recipe);
        CompleteCraft(recipe);
        return true;
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

    private void CompleteCraft(CraftingRecipeData recipe)
    {
        var inventory = _player != null ? _player.Inventory : null;
        var output = recipe != null ? recipe.GetOutputStack() : InventoryItem.Empty;

        if (inventory == null || output.IsEmpty)
        {
            OnCraftFailed?.Invoke(recipe, CraftingFailureReason.InvalidRecipe);
            return;
        }

        var range = new SlotRange(0, inventory.Capacity);
        inventory.TryAddItemToRange(output, range, out var leftover);
        ItemPickupFeedReporter.ReportAddedToInventory(output, leftover);
            
        if (!leftover.IsEmpty)
        {
            SpawnOverflow(leftover);
        }

        OnCraftCompleted?.Invoke(recipe);
    }

    private void SpawnOverflow(InventoryItem item)
    {
        if (_itemDropSpawner == null || _player == null)
            return;

        _itemDropSpawner.Spawn(item, _player.transform.position);
    }
}
