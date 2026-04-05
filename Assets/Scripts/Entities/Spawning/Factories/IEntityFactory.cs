using UnityEngine;

public interface IEntityFactory<in TData, out TEntity>
    where TData : EntityData
    where TEntity : EntityCore
{
    TEntity Create(TData data, Vector2 spawnPoint, Transform parent = null);
}
