using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Crafting/Recipe", fileName = "NewCraftingRecipe")]
public class CraftingRecipeData : ScriptableObject
{
    [field: Header("Output")]
    [field: SerializeField] public ItemData OutputItem { get; private set; }
    [field: SerializeField] public int OutputAmount { get; private set; } = 1;

    [field: Header("Presentation")]
    [field: SerializeField][TextArea(2, 6)] public string Description { get; private set; }
    [field: SerializeField] public CraftingCategory Category { get; private set; } = CraftingCategory.Other;

    [field: Header("Timing")]
    [field: SerializeField] public float CraftDurationSeconds = 1.5f;

    [Header("Requirements")]
    [SerializeField] private List<CraftingIngredient> _ingredients = new();
    [SerializeField] private string _displayName;

    public string DisplayName => string.IsNullOrWhiteSpace(_displayName) && OutputItem != null ? OutputItem.ItemName : _displayName;
    public IReadOnlyList<CraftingIngredient> Ingredients => _ingredients;

    public InventoryItem GetOutputStack()
    {
        if (OutputItem == null)
        {
            return InventoryItem.Empty;
        }

        return new InventoryItem(OutputItem, OutputAmount);
    }
}
