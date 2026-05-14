using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a collection of crafting recipes that can be used within the crafting system.
/// </summary>
[CreateAssetMenu(menuName = "Crafting/Book", fileName = "CraftingBook")]
public class CraftingRecipeBookData : ScriptableObject
{
    [Header("Recipes")]
    [SerializeField] private List<CraftingRecipeData> _recipes = new();

    /// <summary>
    /// Returns every recipe assigned to the requested crafting category.
    /// </summary>
    public IEnumerable<CraftingRecipeData> GetRecipesByCategory(CraftingRecipeCategory category)
    {
        var result = new List<CraftingRecipeData>();
        foreach (var recipe in _recipes)
        {
            if (recipe == null || recipe.Category != category)
                continue;
            result.Add(recipe);
        }
        return result;
    }
}
