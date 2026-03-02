using System;
using UnityEngine;

public sealed class CraftingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player _player;
    [SerializeField] private ItemDropSpawner _itemDropSpawner;

    [Header("Defaults")]
    [SerializeField] private float _defaultCraftDurationSeconds = 1.5f;

    public event Action<CraftingRecipeData> OnCraftStarted;
    public event Action<CraftingRecipeData, float> OnCraftProgress;
    public event Action<CraftingRecipeData> OnCraftCompleted;
    public event Action<CraftingRecipeData, CraftingFailureReason> OnCraftFailed;

    public bool IsCrafting => _activeCraft != null;
    public CraftingRecipeData CurrentRecipe => _activeCraft?.Recipe;

    private CraftingProcess _activeCraft;

    private void Update()
    {
        if (_activeCraft == null)
            return;

        _activeCraft.ElapsedSeconds += Time.deltaTime;
        var progress = Mathf.Clamp01(_activeCraft.ElapsedSeconds / _activeCraft.DurationSeconds);
        OnCraftProgress?.Invoke(_activeCraft.Recipe, progress);

        if (_activeCraft.ElapsedSeconds < _activeCraft.DurationSeconds)
            return;

        CompleteCraft();
    }

    public bool TryStartCraft(CraftingRecipeData recipe)
    {
        if (recipe == null)
        {
            OnCraftFailed?.Invoke(recipe, CraftingFailureReason.InvalidRecipe);
            return false;
        }

        if (IsCrafting)
        {
            OnCraftFailed?.Invoke(recipe, CraftingFailureReason.AlreadyCrafting);
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

        var duration = recipe.CraftDurationSeconds > 0f ? recipe.CraftDurationSeconds : _defaultCraftDurationSeconds;
        _activeCraft = new CraftingProcess(recipe, Mathf.Max(0.1f, duration));
        OnCraftStarted?.Invoke(recipe);
        OnCraftProgress?.Invoke(recipe, 0f);
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

    private void CompleteCraft()
    {
        var recipe = _activeCraft.Recipe;
        var inventory = _player != null ? _player.Inventory : null;
        var output = recipe != null ? recipe.GetOutputStack() : InventoryItem.Empty;

        _activeCraft = null;

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

    private sealed class CraftingProcess
    {
        public CraftingRecipeData Recipe { get; }
        public float DurationSeconds { get; }
        public float ElapsedSeconds { get; set; }

        public CraftingProcess(CraftingRecipeData recipe, float durationSeconds)
        {
            Recipe = recipe;
            DurationSeconds = durationSeconds;
            ElapsedSeconds = 0f;
        }
    }
}
