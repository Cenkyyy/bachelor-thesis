using UnityEngine;

public sealed class EnemyFactory<TEnemy> : IEnemyFactory<TEnemy> where TEnemy : EnemyCore
{
    public TEnemy Create(EnemyData data, Vector2 spawnPoint, Transform parent = null)
    {
        if (data == null || data.Prefab == null)
        {
            return null;
        }

        var spawnedEntity = Object.Instantiate(data.Prefab, spawnPoint, Quaternion.identity, parent);
        var enemy = spawnedEntity as EnemyCore;
        if (enemy == null)
        {
            Object.Destroy(spawnedEntity.gameObject);
            return null;
        }
        return enemy as TEnemy;
    }
}
