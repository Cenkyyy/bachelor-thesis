using UnityEngine;

public sealed class EnemyFactory<TEnemy> : IEnemyFactory<TEnemy> where TEnemy : EnemyCore
{
    public TEnemy Create(EnemyData data, Vector2 spawnPoint, Transform parent = null)
    {
        if (data == null || data.Prefab == null)
        {
            return null;
        }

        var enemy = Object.Instantiate(data.Prefab, spawnPoint, Quaternion.identity, parent);
        enemy.SetData(data);
        return enemy as TEnemy;
    }
}
