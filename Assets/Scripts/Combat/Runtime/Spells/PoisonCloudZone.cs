using System.Collections.Generic;
using UnityEngine;

public class PoisonCloudZone : MonoBehaviour
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

    private void Update()
    {
        var delta = Time.deltaTime;
        _elapsed += delta;

        _tickAccumulator += delta;

        while (_tickAccumulator >= TickIntervalSeconds)
        {
            _tickAccumulator -= TickIntervalSeconds;

            var remainingDuration = Mathf.Max(0f, _duration - (_elapsed - _tickAccumulator));
            var tickDuration = Mathf.Min(TickIntervalSeconds, remainingDuration);
            ApplyPoisonTick(tickDuration);
        }

        if (_elapsed >= _duration)
            Destroy(gameObject);
    }

    private void ApplyPoisonTick(float tickDuration)
    {
        if (tickDuration <= 0f)
            return;

        _buffer.Clear();
        Physics2D.OverlapCircle(transform.position, _radius, _targetFilter, _buffer);

        for (var i = 0; i < _buffer.Count; i++)
        {
            var target = _buffer[i].GetComponentInParent<ISpellTarget>();
            if (target == null || !target.IsAlive)
                continue;

            target.ReceiveSpellDamage(_dps * tickDuration, _damageSource);
            target.ApplyStatus(CombatStatusEffectType.Poison, TickIntervalSeconds);
        }
    }
}
