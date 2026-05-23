using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies poison damage and poison status to combat targets inside a temporary area.
/// </summary>
public sealed class PoisonCloudZone : MonoBehaviour
{
    private const float TickIntervalSeconds = 0.5f;

    private readonly List<Collider2D> _buffer = new(32);

    private LayerMask _targetMask;
    private ContactFilter2D _targetFilter;
    private float _radius;
    private float _duration;
    private float _dps;
    private float _elapsed;
    private float _tickAccumulator;
    private object _damageSource;

    private void Update()
    {
        float delta = Time.deltaTime;
        _elapsed += delta;

        _tickAccumulator += delta;

        while (_tickAccumulator >= TickIntervalSeconds)
        {
            _tickAccumulator -= TickIntervalSeconds;

            float remainingDuration = Mathf.Max(0f, _duration - (_elapsed - _tickAccumulator));
            float tickDuration = Mathf.Min(TickIntervalSeconds, remainingDuration);
            ApplyPoisonTick(tickDuration);
        }

        if (_elapsed >= _duration)
            Destroy(gameObject);
    }

    public void Initialize(float radius, float durationSeconds, float damagePerSecond, LayerMask targetMask, object damageSource)
    {
        _radius = radius;
        _duration = durationSeconds;
        _dps = damagePerSecond;
        _targetMask = targetMask;
        _damageSource = damageSource;
        _targetFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = _targetMask,
            useTriggers = true
        };
    }

    private void ApplyPoisonTick(float tickDuration)
    {
        if (tickDuration <= 0f)
            return;

        _buffer.Clear();
        Physics2D.OverlapCircle(transform.position, _radius, _targetFilter, _buffer);

        for (var i = 0; i < _buffer.Count; i++)
        {
            if (!SpellCombatTargetUtility.TryGetCombatTarget(_buffer[i], out ICombatTarget target))
                continue;

            if (!SpellCombatTargetUtility.TryApplyDamage(target, _dps * tickDuration, _damageSource, out _))
                continue;

            IStatusEffectTarget statusTarget = SpellCombatTargetUtility.GetStatusEffectTarget(target);
            statusTarget?.ApplyStatus(CombatStatusEffectType.Poison, TickIntervalSeconds);
        }
    }
}
