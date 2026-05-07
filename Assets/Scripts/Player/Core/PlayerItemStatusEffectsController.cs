using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies item-based player stat changes from equipment and timed consumables.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerItemStatusEffectsController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player _player;
    [SerializeField] private PlayerMovement _playerMovement;

    private readonly List<ActiveTimedItemStatusEffects> _activeTimedStatusEffects = new();
    private bool _isEquipmentSubscribed;

    public IReadOnlyList<ActiveTimedItemStatusEffects> ActiveTimedStatusEffects => _activeTimedStatusEffects;

    public event Action TimedStatusEffectsChanged;

    public sealed class ActiveTimedItemStatusEffects
    {
        public IReadOnlyList<ItemStatusEffect> StatusEffects;
        public float DurationSeconds;
        public float ExpiresAt;
    }

    private struct StatAggregation
    {
        public float MaxHealthAdditive;
        public float MaxManaAdditive;
        public float DefenceAdditive;
        public float HealthRegenAdditive;
        public float ManaRegenAdditive;
        public float MoveSpeedPercentAdditive;
        public float SpellDamageAdditive;
    }

    private void Awake()
    {
        if (_player == null)
            _player = GetComponent<Player>();

        if (_playerMovement == null)
            _playerMovement = GetComponent<PlayerMovement>();
    }

    private void OnEnable()
    {
        if (_player?.Data != null)
            _player.Data.OnInitialized += HandlePlayerDataInitialized;

        EnsureEquipmentSubscription();
        RecalculateAndApply();
    }

    private void OnDisable()
    {
        if (_player?.Data != null)
            _player.Data.OnInitialized -= HandlePlayerDataInitialized;

        if (_isEquipmentSubscribed && _player?.Equipment != null)
        {
            _player.Equipment.OnItemChanged -= HandleEquipmentChanged;
            _isEquipmentSubscribed = false;
        }

        if (_playerMovement != null)
            _playerMovement.SetItemSpeedMultiplier(1f);
    }

    private void Update()
    {
        EnsureEquipmentSubscription();

        if (_activeTimedStatusEffects.Count == 0)
            return;

        if (!RemoveExpiredTimedStatusEffects())
            return;

        RecalculateAndApply();
        TimedStatusEffectsChanged?.Invoke();
    }

    public void ApplyTimedConsumableStatusEffect(ConsumableItemData consumable)
    {
        if (consumable == null)
            return;

        if (consumable.EffectDurationSeconds <= 0f)
            return;

        var timedModifiers = consumable.StatusEffects;
        if (timedModifiers == null || timedModifiers.Count == 0)
            return;

        _activeTimedStatusEffects.Add(new ActiveTimedItemStatusEffects
        {
            StatusEffects = timedModifiers,
            DurationSeconds = consumable.EffectDurationSeconds,
            ExpiresAt = Time.time + consumable.EffectDurationSeconds
        });

        RecalculateAndApply();
        TimedStatusEffectsChanged?.Invoke();
    }

    private bool RemoveExpiredTimedStatusEffects()
    {
        bool removedAny = false;
        float now = Time.time;

        for (int i = _activeTimedStatusEffects.Count - 1; i >= 0; i--)
        {
            if (_activeTimedStatusEffects[i].ExpiresAt > now)
                continue;

            _activeTimedStatusEffects.RemoveAt(i);
            removedAny = true;
        }

        return removedAny;
    }

    private void EnsureEquipmentSubscription()
    {
        if (_isEquipmentSubscribed)
            return;
        if (_player == null)
            return;
        if (_player.Equipment == null)
            return;

        _player.Equipment.OnItemChanged += HandleEquipmentChanged;
        _isEquipmentSubscribed = true;
    }

    private void HandlePlayerDataInitialized()
    {
        RecalculateAndApply();
    }

    private void HandleEquipmentChanged(int _)
    {
        RecalculateAndApply();
    }

    private void RecalculateAndApply()
    {
        if (_player == null || _player.Data == null)
            return;

        var aggregation = new StatAggregation();
        AggregateEquippedStatusEffects(ref aggregation);
        AggregateTimedConsumableStatusEffects(ref aggregation);

        _player.Data.ApplyCombatItemModifiers(aggregation.DefenceAdditive, aggregation.HealthRegenAdditive, aggregation.ManaRegenAdditive, aggregation.MaxHealthAdditive, aggregation.MaxManaAdditive, aggregation.SpellDamageAdditive);
        ApplyMoveSpeed(aggregation.MoveSpeedPercentAdditive);
    }

    private void AggregateEquippedStatusEffects(ref StatAggregation aggregation)
    {
        if (_player.Equipment == null)
            return;

        for (int i = 0; i < _player.Equipment.Capacity; i++)
        {
            var inventoryItem = _player.Equipment.GetItemAt(i);
            if (inventoryItem.IsEmpty || inventoryItem.Item is not EquipmentItemData equipmentItem)
                continue;

            AggregateStatusEffectsList(equipmentItem.StatusEffect, ref aggregation);
        }
    }

    private void AggregateTimedConsumableStatusEffects(ref StatAggregation aggregation)
    {
        for (int i = 0; i < _activeTimedStatusEffects.Count; i++)
        {
            AggregateStatusEffectsList(_activeTimedStatusEffects[i].StatusEffects, ref aggregation);
        }
    }

    private void AggregateStatusEffectsList(IReadOnlyList<ItemStatusEffect> modifiers, ref StatAggregation aggregation)
    {
        if (modifiers == null || modifiers.Count == 0)
            return;

        for (int i = 0; i < modifiers.Count; i++)
        {
            ApplyStatusEffect(modifiers[i], ref aggregation);
        }
    }

    private static void ApplyStatusEffect(ItemStatusEffect modifier, ref StatAggregation aggregation)
    {
        switch (modifier.StatusEffectType)
        {
            case ItemStatusEffectType.MaxHealth:
                ApplyValue(modifier, ref aggregation.MaxHealthAdditive);
                break;
            case ItemStatusEffectType.MaxMana:
                ApplyValue(modifier, ref aggregation.MaxManaAdditive);
                break;
            case ItemStatusEffectType.Defence:
                ApplyValue(modifier, ref aggregation.DefenceAdditive);
                break;
            case ItemStatusEffectType.HealthRegen:
                ApplyValue(modifier, ref aggregation.HealthRegenAdditive);
                break;
            case ItemStatusEffectType.ManaRegen:
                ApplyValue(modifier, ref aggregation.ManaRegenAdditive);
                break;
            case ItemStatusEffectType.MoveSpeed:
                ApplyValue(modifier, ref aggregation.MoveSpeedPercentAdditive);
                break;
            case ItemStatusEffectType.SpellDamage:
                ApplyValue(modifier, ref aggregation.SpellDamageAdditive);
                break;
            default:
                break;
        }
    }

    private static void ApplyValue(ItemStatusEffect modifier, ref float additive)
    {
        additive += modifier.Value;
    }

    private void ApplyMoveSpeed(float moveSpeedPercent)
    {
        if (_playerMovement == null)
            return;

        float multiplier = 1f + moveSpeedPercent * 0.01f;
        _playerMovement.SetItemSpeedMultiplier(Mathf.Max(0f, multiplier));
    }
}
