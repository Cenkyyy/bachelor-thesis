using UnityEngine;

/// <summary>
/// ScriptableObject representing a type of item in the game.
/// Stores basic info, stacking rules, and category.
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Items/ItemType")]
public class ItemSO : ScriptableObject
{
    #region BasicInfo
    [Header(UIStrings.BasicInfo)]
    
    /// <summary>
    /// The display name of the item.
    /// </summary>
    public string itemName;

    /// <summary>
    /// The icon representing the item in the UI.
    /// </summary>
    public Sprite icon;
    #endregion

    #region StackingRules
    [Header(UIStrings.StackingRules)]

    /// <summary>
    /// Determines whether multiple copies of this item can be stacked in the inventory.
    /// </summary>
    public bool isStackable = true;

    /// <summary>
    /// Maximum number of items that can be stacked together.
    /// Only relevant if item is stackable.
    /// </summary>
    public int maxStackSize = 999;
    #endregion

    #region ItemCategory
    [Header(UIStrings.ItemCategory)]
    
    /// <summary>
    /// The category this item belongs to.
    /// </summary>
    public ItemCategory category;
    #endregion
}
