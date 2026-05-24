using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Item data for consumables that restore stats, apply timed effects, or use cooldown rules.
/// </summary>
[CreateAssetMenu(menuName = "Items/Consumable Item")]
public class ConsumableItemData : ItemData, ICooldownItem
{
    [field: SerializeField] public ConsumableType Kind { get; private set; } = ConsumableType.Food;

    [field: Header("Instant Effects")]
    [field: SerializeField] public int RestoreHealth { get; private set; }
    [field: SerializeField] public int RestoreMana { get; private set; }
    [field: SerializeField] public int RestoreHunger { get; private set; }


    [field: Header("Cooldown Settings")]
    [field: SerializeField, Min(0f)] public float CooldownSeconds { get; private set; }
    [SerializeField] private List<ItemData> _cooldownBlockedItems = new();
    public IReadOnlyList<ItemData> CooldownBlockedItems => _cooldownBlockedItems;

    [field: Header("Status Effect Settings")]
    [field: SerializeField] public float EffectDurationSeconds { get; private set; }
    [SerializeField] private List<ItemStatusEffect> _timedStatusEffects = new();
    public IReadOnlyList<ItemStatusEffect> StatusEffects => _timedStatusEffects;

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

    public float GetCooldownSeconds() => Kind == ConsumableType.Potion ? CooldownSeconds : 0f;
}
