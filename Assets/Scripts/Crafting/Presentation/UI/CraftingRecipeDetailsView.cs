using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CraftingRecipeDetailsView : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _title;
    [SerializeField] private TMP_Text _description;

    [Header("Ingredients")]
    [SerializeField] private Transform _ingredientRoot;
    [SerializeField] private CraftingIngredientRow _ingredientRowPrefab;

    [Header("Crafting")]
    [SerializeField] private Button _craftButton;

    private readonly List<CraftingIngredientRow> _rows = new();

    public Button CraftButton => _craftButton;

    public void SetRecipe(CraftingRecipeData recipe, IReadOnlyInventory inventory)
    {
        if (_icon != null)
        {
            _icon.sprite = recipe != null && recipe.OutputItem != null ? recipe.OutputItem.Icon : null;
            _icon.enabled = _icon.sprite != null;
        }

        if (_title != null)
        {
            _title.text = recipe != null ? recipe.DisplayName : string.Empty;
        }

        if (_description != null)
        {
            _description.text = recipe != null ? recipe.Description : string.Empty;
        }

        RebuildIngredients(recipe, inventory);
    }

    public void SetCraftButtonEnabled(bool enabled)
    {
        if (_craftButton != null)
        {
            _craftButton.interactable = enabled;
        }
    }

    public void Clear()
    {
        SetRecipe(null, null);
        SetCraftButtonEnabled(false);
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