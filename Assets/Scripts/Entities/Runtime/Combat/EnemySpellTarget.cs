using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemySpellTarget : MonoBehaviour, ISpellTarget, ISpellWordEffectivenessTarget
{
    [Header("References")]
    [SerializeField] private EnemyCore _enemyCore;
    [SerializeField] private EntityDamageable _damageable;

    [Header("Status Runtime")]
    [SerializeField] private float _statusTickInterval = 0.5f;

    [Header("Damage Word Text Popup Settings")]
    [SerializeField] private DamageWordTextPopupSettings _damageWordTextPopupSettings = new();

    private readonly Dictionary<CombatStatusEffect, float> _statusUntil = new();
    private readonly List<CombatStatusEffect> _cleanupBuffer = new();
    private float _stunBuildup;

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

    public void ReceiveSpellDamage(float amount, object source = null)
    {
        if (_damageable == null)
            return;

        var rounded = Mathf.RoundToInt(amount);
        if (rounded <= 0)
            rounded = 1;

        _damageable.ReceiveDamage(rounded, source);
    }

    public void ApplyStatus(CombatStatusEffect effect, float durationSeconds)
    {
        if (effect == CombatStatusEffect.None || durationSeconds <= 0f)
            return;

        _statusUntil[effect] = Time.time + durationSeconds;
    }

    public void ApplyDamageOverTime(float damagePerSecond, float durationSeconds, float effectivenessMultiplier = 1f, object source = null)
    {
        if (damagePerSecond <= 0f || durationSeconds <= 0f)
            return;

        StartCoroutine(ApplyDamageOverTimeRoutine(damagePerSecond, durationSeconds, effectivenessMultiplier, source));
    }

    public void AddStunBuildup(float amount, float threshold, float stunDurationSeconds)
    {
        if (amount <= 0f || threshold <= 0f)
            return;

        _stunBuildup += amount;
        if (_stunBuildup < threshold)
            return;

        _stunBuildup = 0f;
        ApplyStatus(CombatStatusEffect.Stunned, stunDurationSeconds);
        _enemyCore?.StopMovement();
    }

    private IEnumerator ApplyDamageOverTimeRoutine(float damagePerSecond, float durationSeconds, float effectivenessMultiplier, object source)
    {
        var elapsed = 0f;
        var initialDelay = Mathf.Min(_statusTickInterval, durationSeconds);
        if (initialDelay > 0f)
        {
            yield return new WaitForSeconds(initialDelay);
            elapsed += initialDelay;
        }

        while (elapsed < durationSeconds)
        {
            if (!IsAlive)
                yield break;

            var delta = Mathf.Min(_statusTickInterval, durationSeconds - elapsed);
            var damage = Mathf.RoundToInt(damagePerSecond * delta);
            if (damage > 0)
            {
                _damageable.ReceiveDamage(damage, source);
                DamageWordTextPopupUtility.ShowForGameObject(gameObject, damage, effectivenessMultiplier, _damageWordTextPopupSettings);
            }

            elapsed += delta;
            if (elapsed < durationSeconds)
                yield return new WaitForSeconds(Mathf.Min(_statusTickInterval, durationSeconds - elapsed));
        }
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
