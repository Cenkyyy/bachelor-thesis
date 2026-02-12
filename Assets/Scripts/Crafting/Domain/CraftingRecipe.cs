using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Crafting/Recipe", fileName = "NewCraftingRecipe")]
public class CraftingRecipe : ScriptableObject
{
    [Header("Output")]
    [SerializeField] private Item _outputItem;
    [SerializeField] private int _outputAmount = 1;

    [Header("Presentation")]
    [SerializeField] private string _displayName;
    [SerializeField][TextArea(2, 6)] private string _description;
    [SerializeField] private CraftingCategory _category = CraftingCategory.Other;

    [Header("Requirements")]
    [SerializeField] private List<CraftingIngredient> _ingredients = new();

    [Header("Timing")]
    [SerializeField] private float _craftDurationSeconds = 1.5f;

    public Item OutputItem => _outputItem;
    public int OutputAmount => Mathf.Max(1, _outputAmount);
    public string DisplayName => string.IsNullOrWhiteSpace(_displayName) && _outputItem != null
        ? _outputItem.ItemName
        : _displayName;
    public string Description => _description;
    public CraftingCategory Category => _category;
    public IReadOnlyList<CraftingIngredient> Ingredients => _ingredients;
    public float CraftDurationSeconds => Mathf.Max(0.1f, _craftDurationSeconds);

    public InventoryItem GetOutputStack()
    {
        if (_outputItem == null)
        {
            return InventoryItem.Empty;
        }

        return new InventoryItem(_outputItem, OutputAmount);
    }
}