using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerItemStatController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player _player;
    [SerializeField] private PlayerMovement _playerMovement;

    private readonly List<ActiveTimedModifiers> _activeTimedModifiers = new();
    private bool _isEquipmentSubscribed;

    private sealed class ActiveTimedModifiers
    {
        public IReadOnlyList<ItemStatModifier> Modifiers;
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

        if (_activeTimedModifiers.Count == 0)
            return;

        if (!RemoveExpiredTimedModifiers())
            return;

        RecalculateAndApply();
    }

    public void ApplyTimedConsumableModifiers(ConsumableItemData consumable)
    {
        if (consumable == null)
            return;

        if (consumable.EffectDurationSeconds <= 0f)
            return;

        var timedModifiers = consumable.TimedModifiers;
        if (timedModifiers == null || timedModifiers.Count == 0)
            return;

        _activeTimedModifiers.Add(new ActiveTimedModifiers
        {
            Modifiers = timedModifiers,
            ExpiresAt = Time.time + consumable.EffectDurationSeconds
        });

        RecalculateAndApply();
    }

    private bool RemoveExpiredTimedModifiers()
    {
        bool removedAny = false;
        float now = Time.time;

        for (int i = _activeTimedModifiers.Count - 1; i >= 0; i--)
        {
            if (_activeTimedModifiers[i].ExpiresAt > now)
                continue;

            _activeTimedModifiers.RemoveAt(i);
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
        AggregateEquippedModifiers(ref aggregation);
        AggregateTimedConsumableModifiers(ref aggregation);

        _player.Data.ApplyCombatItemModifiers(aggregation.DefenceAdditive, aggregation.HealthRegenAdditive, aggregation.ManaRegenAdditive, aggregation.MaxHealthAdditive, aggregation.MaxManaAdditive);
        ApplyMoveSpeed(aggregation.MoveSpeedPercentAdditive);
    }

    private void AggregateEquippedModifiers(ref StatAggregation aggregation)
    {
        if (_player.Equipment == null)
            return;

        for (int i = 0; i < _player.Equipment.Capacity; i++)
        {
            var inventoryItem = _player.Equipment.GetItemAt(i);
            if (inventoryItem.IsEmpty || inventoryItem.Item is not EquipmentItemData equipmentItem)
                continue;

            AggregateModifierList(equipmentItem.StatBonuses, ref aggregation);
        }
    }

    private void AggregateTimedConsumableModifiers(ref StatAggregation aggregation)
    {
        for (int i = 0; i < _activeTimedModifiers.Count; i++)
        {
            AggregateModifierList(_activeTimedModifiers[i].Modifiers, ref aggregation);
        }
    }

    private void AggregateModifierList(IReadOnlyList<ItemStatModifier> modifiers, ref StatAggregation aggregation)
    {
        if (modifiers == null || modifiers.Count == 0)
            return;

        for (int i = 0; i < modifiers.Count; i++)
        {
            ApplyModifier(modifiers[i], ref aggregation);
        }
    }

    private static void ApplyModifier(ItemStatModifier modifier, ref StatAggregation aggregation)
    {
        switch (modifier.Stat)
        {
            case ItemStatType.MaxHealth:
                ApplyValue(modifier, ref aggregation.MaxHealthAdditive);
                break;
            case ItemStatType.MaxMana:
                ApplyValue(modifier, ref aggregation.MaxManaAdditive);
                break;
            case ItemStatType.Defence:
                ApplyValue(modifier, ref aggregation.DefenceAdditive);
                break;
            case ItemStatType.HealthRegen:
                ApplyValue(modifier, ref aggregation.HealthRegenAdditive);
                break;
            case ItemStatType.ManaRegen:
                ApplyValue(modifier, ref aggregation.ManaRegenAdditive);
                break;
            case ItemStatType.MoveSpeed:
                ApplyValue(modifier, ref aggregation.MoveSpeedPercentAdditive);
                break;
            case ItemStatType.SpellDamage:
                // TODO: Hook SpellDamage into spell damage calculation pipeline.
                break;
            case ItemStatType.CastSpeed:
                // TODO: Hook CastSpeed into spell casting/cooldown timing.
                break;
            default:
                break;
        }
    }

    private static void ApplyValue(ItemStatModifier modifier, ref float additive)
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
