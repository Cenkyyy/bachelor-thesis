using System.Collections.Generic;
using UnityEngine;

public sealed class SpawnedEnemyRegistry : ISpawnRegistry<EnemyCore>
{
    private readonly List<EnemyCore> _alive = new();

    public int AliveCount
    {
        get
        {
            PruneDeadReferences();
            return _alive.Count;
        }
    }

    public void Register(EnemyCore enemy)
    {
        if (enemy != null)
        {
            _alive.Add(enemy);
        }
    }

    public bool HasAnyWithin(Vector2 point, float minDistance)
    {
        if (minDistance <= 0f)
        {
            return false;
        }

        var sqrDistance = minDistance * minDistance;

        for (var i = _alive.Count - 1; i >= 0; i--)
        {
            var enemy = _alive[i];
            if (enemy == null)
            {
                _alive.RemoveAt(i);
                continue;
            }

            if (((Vector2)enemy.transform.position - point).sqrMagnitude < sqrDistance)
            {
                return true;
            }
        }

        return false;
    }

    public void DespawnOutsideRadius(Vector2 center, float radius)
    {
        var sqrRadius = radius * radius;

        for (var i = _alive.Count - 1; i >= 0; i--)
        {
            var enemy = _alive[i];
            if (enemy == null)
            {
                _alive.RemoveAt(i);
                continue;
            }

            if (((Vector2)enemy.transform.position - center).sqrMagnitude <= sqrRadius)
            {
                continue;
            }

            Object.Destroy(enemy.gameObject);
            _alive.RemoveAt(i);
        }
    }

    private void PruneDeadReferences()
    {
        for (var i = _alive.Count - 1; i >= 0; i--)
        {
            if (_alive[i] == null)
            {
                _alive.RemoveAt(i);
            }
        }
    }
}
