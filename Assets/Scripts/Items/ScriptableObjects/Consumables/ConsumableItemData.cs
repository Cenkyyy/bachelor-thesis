using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Items/Consumable Item")]
public class ConsumableItemData : ItemData
{
    [field: SerializeField] public ConsumableKind Kind { get; private set; } = ConsumableKind.Food;

    [field: Header("Instant Effects")]
    [field: SerializeField] public int RestoreHealth { get; private set; }
    [field: SerializeField] public int RestoreMana { get; private set; }
    [field: SerializeField] public int RestoreHunger { get; private set; }

    [field: Header("Usage")]
    [field: SerializeField, Min(0f)] public float CooldownSeconds { get; private set; }

    [field: Header("Optional Timed Effects")]
    [field: SerializeField] public float EffectDurationSeconds { get; private set; }
    [SerializeField] private List<ItemStatModifier> _timedModifiers = new();
    public IReadOnlyList<ItemStatModifier> TimedModifiers => _timedModifiers;

    public ConsumableItemData()
    {
        Category = ItemType.Consumable;
    }

    protected override ItemType? ExpectedCategory => ItemType.Consumable;

    protected override void OnValidate()
    {
        base.OnValidate();

        if (RestoreHealth < 0)
            RestoreHealth = 0;

        if (RestoreMana < 0)
            RestoreMana = 0;

        if (RestoreHunger < 0)
            RestoreHunger = 0;

        if (EffectDurationSeconds < 0f)
            EffectDurationSeconds = 0f;

        if (CooldownSeconds < 0f)
            CooldownSeconds = 0f;
    }
}
