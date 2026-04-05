using UnityEngine;

public sealed class EnemyFactory : IEntityFactory<EnemyData, EnemyCore>
{
    public EnemyCore Create(EnemyData data, Vector2 spawnPoint, Transform parent = null)
    {
        if (data == null || data.Prefab == null)
            return null;

        var spawnedEntity = Object.Instantiate(data.Prefab, spawnPoint, Quaternion.identity, parent);
        
        if (spawnedEntity is not EnemyCore enemy)
        {
            Object.Destroy(spawnedEntity.gameObject);
            return null;
        }

        return enemy;
    }
}
