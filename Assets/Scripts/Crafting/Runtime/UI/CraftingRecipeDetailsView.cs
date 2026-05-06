using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a part of the crafting book, specifically the selected crafting recipe output,
/// generated item stats, ingredient requirements, and craft button.
/// </summary>
public sealed class CraftingRecipeDetailsView : MonoBehaviour
{
    [Header("Recipe Output")]
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _title;
    [SerializeField] private TMP_Text _description;

    [Header("Ingredient List")]
    [SerializeField] private Transform _ingredientRoot;
    [SerializeField] private CraftingIngredientRowView _ingredientRowPrefab;

    [field: Header("Craft Action")]
    [field: SerializeField] public Button CraftButton { get; private set; }

    [Header("Tooltip Dependencies")]
    [SerializeField] private PlayerToolDurabilityRuntimeState _playerToolDurability;

    private readonly List<CraftingIngredientRowView> _rows = new();
    private readonly List<IItemTooltipProvider> _tooltipProviders = new();

    private void Awake()
    {
        if (_playerToolDurability == null)
            _playerToolDurability = FindFirstObjectByType<PlayerToolDurabilityRuntimeState>();

        _tooltipProviders.Clear();
        _tooltipProviders.AddRange(ItemTooltipProviderFactory.CreateDefault(_playerToolDurability));
    }

    public void SetRecipe(CraftingRecipeData recipe, IReadOnlyInventory inventory)
    {
        if (_icon != null)
        {
            var iconSprite = recipe != null && recipe.OutputItem != null ? recipe.OutputItem.Icon : null;
            ImageIconUtility.SetIcon(_icon, iconSprite);
        }

        if (_title != null)
            _title.text = recipe != null ? recipe.DisplayName : string.Empty;

        if (_description != null)
            _description.text = BuildOutputStats(recipe);

        RebuildIngredients(recipe, inventory);
    }

    public void SetCraftButtonEnabled(bool enabled)
    {
        if (CraftButton != null)
            CraftButton.interactable = enabled;
    }

    public void Clear()
    {
        SetRecipe(null, null);
        SetCraftButtonEnabled(false);
    }

    private string BuildOutputStats(CraftingRecipeData recipe)
    {
        if (recipe == null || recipe.OutputItem == null)
            return string.Empty;

        var outputItem = new InventoryItem(recipe.OutputItem, Mathf.Max(1, recipe.OutputAmount));
        var lines = new List<ItemTooltipLineRuntimeData>();

        for (int i = 0; i < _tooltipProviders.Count; i++)
        {
            var provider = _tooltipProviders[i];
            if (!provider.CanHandle(recipe.OutputItem))
                continue;

            provider.AppendLines(null, outputItem, lines);
        }

        if (lines.Count == 0)
            return "No stats available.";

        return ItemTooltipBodyBuilder.BuildBody(lines);
    }

    private void RebuildIngredients(CraftingRecipeData recipe, IReadOnlyInventory inventory)
    {
        foreach (var row in _rows)
        {
            if (row != null)
                Destroy(row.gameObject);
        }
        _rows.Clear();

        if (recipe == null || _ingredientRoot == null || _ingredientRowPrefab == null)
            return;

        foreach (var ingredient in recipe.Ingredients)
        {
            var row = Instantiate(_ingredientRowPrefab, _ingredientRoot);
            var available = CraftingInventoryUtility.CountItem(inventory, ingredient.Item);
            row.Bind(ingredient.Item, ingredient.Amount, available);
            _rows.Add(row);
        }
    }
}
