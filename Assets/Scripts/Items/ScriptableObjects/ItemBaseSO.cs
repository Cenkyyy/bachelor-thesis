using UnityEngine;

/// <summary>
/// ScriptableObject representing a type of item in the game.
/// Stores basic info, stacking rules, and category.
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Items/ItemType")]
public abstract class ItemBaseSO : ScriptableObject
{
    #region BasicInfo
    [Header(UIStrings.ItemSO_BasicInfo__Title)]
    
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
    [Header(UIStrings.ItemSO_StackingRules__Title)]

    /// <summary>
    /// Maximum number of items that can be stacked together.
    /// Only relevant if item is stackable.
    /// </summary>
    [SerializeField] protected int maxStackSize = 999;

    public int MaxStackSize => maxStackSize;
    public bool IsStackable => maxStackSize > 1;
    #endregion

    #region ItemCategory
    [Header(UIStrings.ItemSO_ItemCategory__Title)]

    /// <summary>
    /// The category this item belongs to.
    /// </summary>
    [SerializeField] protected ItemCategory category;

    public ItemCategory Category => category;
    #endregion
}
