using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Crafting/Book", fileName = "CraftingBook")]
public class CraftingBook : ScriptableObject
{
    [SerializeField] private List<CraftingRecipe> _recipes = new();

    public IReadOnlyList<CraftingRecipe> Recipes => _recipes;

    public List<CraftingRecipe> GetRecipesByCategory(CraftingCategory category)
    {
        var result = new List<CraftingRecipe>();
        foreach (var recipe in _recipes)
        {
            if (recipe == null || recipe.Category != category)
                continue;
            result.Add(recipe);
        }
        return result;
    }
}
