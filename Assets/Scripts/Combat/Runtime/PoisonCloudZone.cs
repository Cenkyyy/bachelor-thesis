using System.Collections.Generic;
using UnityEngine;

public class PoisonCloudZone : MonoBehaviour
{
    private readonly List<Collider2D> _buffer = new(32);

    private LayerMask _targetMask;
    private ContactFilter2D _targetFilter;
    private float _radius;
    private float _duration;
    private float _dps;
    private float _elapsed;
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

        _buffer.Clear();
        Physics2D.OverlapCircle(transform.position, _radius, _targetFilter, _buffer);

        for (var i = 0; i < _buffer.Count; i++)
        {
            var target = _buffer[i].GetComponentInParent<ISpellTarget>();
            if (target == null || !target.IsAlive)
                continue;

            target.ReceiveSpellDamage(_dps * delta, _damageSource);
            target.ApplyStatus(CombatStatusEffect.Poison, 0.2f);
        }

        if (_elapsed >= _duration)
            Destroy(gameObject);
    }
}
