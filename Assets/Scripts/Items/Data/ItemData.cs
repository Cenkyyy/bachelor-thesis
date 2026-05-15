using UnityEngine;

/// <summary>
/// Defines the shared base data for every item type in the game.
/// It centralizes identity, classification, rarity, biome affinity,
/// and stack behavior so all concrete item assets follow one consistent contract.
/// </summary>
public abstract class ItemData : ScriptableObject
{
    [field: Header("Identification")]
    [field: SerializeField] public string ItemName { get; private set; }
    [field: SerializeField] public Sprite Icon { get; private set; }

    [field: Header("Classification")]
    [field: SerializeField] public ItemType Category { get; protected set; }
    [field: SerializeField] public ItemRarity Rarity { get; private set; } = ItemRarity.Common;
    [field: SerializeField] public ItemBiomeAffinity BiomeAffinity { get; private set; } = ItemBiomeAffinity.None;

    [field: Header("Stacking")]
    [field: SerializeField] public int MaxStackSize { get; private set; } = 999;
    public bool IsStackable => MaxStackSize > 1;

    [field: Header("Held Visual")]
    [field: SerializeField] public HeldItemPresentationMode HeldPresentationMode { get; private set; } = HeldItemPresentationMode.Simple;
    [field: SerializeField] public Vector2 HeldScale { get; private set; } = new(0.3f, 0.3f);

    protected virtual ItemType? ExpectedCategory => null;

    protected virtual void OnValidate()
    {
        if (MaxStackSize < 1)
            MaxStackSize = 1;

        if (HeldScale.x < 0f)
            HeldScale = new Vector2(0f, HeldScale.y);

        if (HeldScale.y < 0f)
            HeldScale = new Vector2(HeldScale.x, 0f);

        if (string.IsNullOrWhiteSpace(ItemName))
            ItemName = name;

        if (ExpectedCategory.HasValue)
            Category = ExpectedCategory.Value;
    }
}
