using UnityEngine;

public sealed class EntityFactory<TData, TEntity> : IEntityFactory<TData, TEntity>
    where TData : EntityData
    where TEntity : EntityCore
{
    public TEntity Create(TData data, Vector2 spawnPoint, Transform parent = null)
    {
        if (data == null || data.Prefab == null)
            return null;

        var spawnedEntity = Object.Instantiate(data.Prefab, spawnPoint, Quaternion.identity, parent);
        
        if (spawnedEntity is not TEntity entity)
        {
            Object.Destroy(spawnedEntity.gameObject);
            return null;
        }

        entity.Initialize(data);

        return entity;
    }
}
