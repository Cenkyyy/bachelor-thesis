using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Items/ItemType")]
public abstract class ItemBaseSO : ScriptableObject
{
    #region BasicInfo

    [Header(UIStrings.ItemSO_BasicInfo__Title)]
    [field: SerializeField] public string ItemName { get; private set; }
    [field: SerializeField] public Sprite Icon { get; private set; }

    #endregion

    #region StackingRules

    [Header(UIStrings.ItemSO_StackingRules__Title)]
    [field: SerializeField] public int MaxStackSize { get; private set; } = 999;
    public bool IsStackable => MaxStackSize > 1;

    #endregion

    #region ItemCategory
    
    [Header(UIStrings.ItemSO_ItemCategory__Title)]
    [field: SerializeField] public ItemCategory Category { get; protected set; }

    #endregion
}
