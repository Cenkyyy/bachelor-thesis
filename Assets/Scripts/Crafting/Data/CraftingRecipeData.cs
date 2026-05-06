using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Authored recipe data describing crafted output, presentation category, and required ingredients.
/// </summary>
[CreateAssetMenu(menuName = "Crafting/Recipe", fileName = "NewCraftingRecipe")]
public class CraftingRecipeData : ScriptableObject
{
    [field: Header("Output")]
    [field: SerializeField] public ItemData OutputItem { get; private set; }
    [field: SerializeField] public int OutputAmount { get; private set; } = 1;

    [field: Header("Presentation")]
    [field: SerializeField] public CraftingRecipeCategory Category { get; private set; } = CraftingRecipeCategory.Other;
    [SerializeField] private string _displayName;

    [Header("Requirements")]
    [SerializeField] private List<CraftingIngredient> _ingredients = new();

    public string DisplayName => string.IsNullOrWhiteSpace(_displayName) && OutputItem != null ? OutputItem.ItemName : _displayName;
    public IReadOnlyList<CraftingIngredient> Ingredients => _ingredients;

    public InventoryItem GetOutputStack()
    {
        if (OutputItem == null)
            return InventoryItem.Empty;

        return new InventoryItem(OutputItem, OutputAmount);
    }
}
