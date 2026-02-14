using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Items/Consumable")]
public class ConsumableItem : Item
{
    [field: SerializeField] public ConsumableKind Kind { get; private set; } = ConsumableKind.Food;

    [Header("Instant Effects")]
    [field: SerializeField] public int RestoreHealth { get; private set; }
    [field: SerializeField] public int RestoreMana { get; private set; }
    [field: SerializeField] public int RestoreHunger { get; private set; }

    [Header("Optional Timed Effects")]
    [field: SerializeField] public float EffectDurationSeconds { get; private set; }
    [SerializeField] private List<ItemStatModifier> _timedModifiers = new();
    public IReadOnlyList<ItemStatModifier> TimedModifiers => _timedModifiers;

    public ConsumableItem()
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
    }
}