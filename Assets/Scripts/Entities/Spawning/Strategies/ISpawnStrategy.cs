using UnityEngine;

public interface ISpawnStrategy
{
    bool TryGetSpawnPoint(Vector2 playerPosition, EntitySpawnSettings _settings, out Vector2 spawnPoint);
}
