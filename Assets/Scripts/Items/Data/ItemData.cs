using UnityEngine;

public abstract class ItemData : ScriptableObject
{
    [field: Header("Basic Info")]
    [field: SerializeField] public string ItemName { get; private set; }
    [field: SerializeField] public Sprite Icon { get; private set; }

    [field: Header("Classification")]
    [field: SerializeField] public ItemType Category { get; protected set; }
    [field: SerializeField] public ItemRarity Rarity { get; private set; } = ItemRarity.Common;
    [field: SerializeField] public ItemBiomeAffinity BiomeAffinity { get; private set; } = ItemBiomeAffinity.None;

    [field: Header("Stacking")]
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
