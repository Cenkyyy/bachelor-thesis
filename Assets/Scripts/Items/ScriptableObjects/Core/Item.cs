using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Items/ItemType")]
public abstract class Item : ScriptableObject
{
    [Header("Basic Info")]
    [field: SerializeField] public string ItemName { get; private set; }
    [field: SerializeField] public Sprite Icon { get; private set; }
    [field: SerializeField][TextArea(2, 4)] public string Description { get; private set; }

    [Header("Classfication")]
    [field: SerializeField] public ItemType Category { get; protected set; }
    [field: SerializeField] public ItemRarity Rarity { get; private set; } = ItemRarity.Common;
    [field: SerializeField] public BiomeAffinity BiomeAffinity { get; private set; } = BiomeAffinity.None;

    [Header("Stacking")]
    [field: SerializeField] public int MaxStackSize { get; private set; } = 999;
    public bool IsStackable => MaxStackSize > 1;

    protected virtual ItemType? ExpectedCategory => null;

    protected virtual void OnValidate()
    {
        if (MaxStackSize < 1)
            MaxStackSize = 1;

        if (string.IsNullOrWhiteSpace(ItemName))
            ItemName = name;

        if (ExpectedCategory.HasValue)
            Category = ExpectedCategory.Value;
    }
}
