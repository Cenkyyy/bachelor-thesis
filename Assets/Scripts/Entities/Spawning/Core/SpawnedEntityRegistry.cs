using System.Collections.Generic;
using UnityEngine;

public sealed class SpawnedEntityRegistry<TEntity> : ISpawnRegistry<TEntity>
    where TEntity : EntityCore
{
    private readonly List<TEntity> _alive = new();

    public int AliveCount
    {
        get
        {
            PruneDeadReferences();
            return _alive.Count;
        }
    }

    public void Register(TEntity entity)
    {
        if (entity != null)
            _alive.Add(entity);
    }

    public bool HasAnyWithin(Vector2 point, float minDistance)
    {
        if (minDistance <= 0f)
            return false;

        var sqrDistance = minDistance * minDistance;

        for (var i = _alive.Count - 1; i >= 0; i--)
        {
            var entity = _alive[i];
            if (entity == null)
            {
                _alive.RemoveAt(i);
                continue;
            }

            if (((Vector2)entity.transform.position - point).sqrMagnitude < sqrDistance)
                return true;
        }

        return false;
    }

    public void DespawnOutsideRadius(Vector2 center, float radius)
    {
        var sqrRadius = radius * radius;

        for (var i = _alive.Count - 1; i >= 0; i--)
        {
            var entity = _alive[i];
            if (entity == null)
            {
                _alive.RemoveAt(i);
                continue;
            }

            if (((Vector2)entity.transform.position - center).sqrMagnitude <= sqrRadius)
                continue;

            Object.Destroy(entity.gameObject);
            _alive.RemoveAt(i);
        }
    }

    public int DespawnInsideRadius(Vector2 center, float radius)
    {
        if (radius <= 0f)
            return 0;

        var sqrRadius = radius * radius;
        var despawnedCount = 0;

        for (var i = _alive.Count - 1; i >= 0; i--)
        {
            var entity = _alive[i];
            if (entity == null)
            {
                _alive.RemoveAt(i);
                continue;
            }

            if (((Vector2)entity.transform.position - center).sqrMagnitude > sqrRadius)
                continue;

            Object.Destroy(entity.gameObject);
            _alive.RemoveAt(i);
            despawnedCount++;
        }

        return despawnedCount;
    }

    private void PruneDeadReferences()
    {
        for (var i = _alive.Count - 1; i >= 0; i--)
        {
            if (_alive[i] == null)
                _alive.RemoveAt(i);
        }
    }
}
