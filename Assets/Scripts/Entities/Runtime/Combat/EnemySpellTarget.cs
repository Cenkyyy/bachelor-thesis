using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemySpellTarget : MonoBehaviour, ICombatTarget, IStatusEffectTarget, ISpellWordEffectivenessTarget
{
    [Header("References")]
    [SerializeField] private EnemyCore _enemyCore;
    [SerializeField] private EntityDamageable _damageable;

    private readonly Dictionary<CombatStatusEffectType, float> _statusUntil = new();
    private readonly List<CombatStatusEffectType> _cleanupBuffer = new();

    public Vector2 Position => transform.position;

    public bool IsAlive => _damageable != null && _damageable.CanReceiveDamage;

    public bool HasAnyNegativeStatus
    {
        get
        {
            CleanupExpiredStatuses();
            return _statusUntil.Count > 0;
        }
    }

    public bool HasStatus(CombatStatusEffectType effect)
    {
        CleanupExpiredStatuses();
        return _statusUntil.ContainsKey(effect);
    }

    private void Awake()
    {
        if (_enemyCore == null)
            _enemyCore = GetComponent<EnemyCore>() ?? GetComponentInParent<EnemyCore>();

        if (_damageable == null)
            _damageable = GetComponent<EntityDamageable>() ?? GetComponentInParent<EntityDamageable>();
    }

    public bool TryGetSpellWordEffectivenessData(out ItemBiomeAffinity biome, out EnemyRoleTag roleTags)
    {
        if (_enemyCore == null || _enemyCore.Data == null)
        {
            biome = ItemBiomeAffinity.None;
            roleTags = EnemyRoleTag.None;
            return false;
        }

        biome = _enemyCore.Data.HomeBiome;
        roleTags = _enemyCore.Data.RoleTags;
        return true;
    }

    public void ApplyStatus(CombatStatusEffectType effect, float durationSeconds)
    {
        if (effect == CombatStatusEffectType.None || durationSeconds <= 0f)
            return;

        _statusUntil[effect] = Time.time + durationSeconds;

        if (effect == CombatStatusEffectType.Stunned)
            _enemyCore?.StopMovement();
    }

    private void CleanupExpiredStatuses()
    {
        if (_statusUntil.Count == 0)
            return;

        _cleanupBuffer.Clear();

        foreach (var pair in _statusUntil)
        {
            if (pair.Value <= Time.time)
                _cleanupBuffer.Add(pair.Key);
        }

        for (var i = 0; i < _cleanupBuffer.Count; i++)
            _statusUntil.Remove(_cleanupBuffer[i]);
    }
}
