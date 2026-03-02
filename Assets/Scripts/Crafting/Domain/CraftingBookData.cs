using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Crafting/Book", fileName = "CraftingBook")]
public class CraftingBookData : ScriptableObject
{
    [SerializeField] private List<CraftingRecipeData> _recipes = new();

    public IReadOnlyList<CraftingRecipeData> Recipes => _recipes;

    public List<CraftingRecipeData> GetRecipesByCategory(CraftingCategory category)
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
