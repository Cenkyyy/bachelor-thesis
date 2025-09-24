using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Items/ItemType")]
public abstract class Item : ScriptableObject
{
    #region BasicInfo

    [field: SerializeField] public string ItemName { get; private set; }
    [field: SerializeField] public Sprite Icon { get; private set; }

    #endregion

    #region StackingRules

    [field: SerializeField] public int MaxStackSize { get; private set; } = 999;
    public bool IsStackable => MaxStackSize > 1;

    #endregion

    #region ItemCategory
    
    [field: SerializeField] public ItemCategory Category { get; protected set; }

    #endregion
}
