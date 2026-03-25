using UnityEngine;

public interface ISpawnStrategy
{
    bool TryGetSpawnPoint(Vector2 playerPosition, EnemySpawnSettings _settings, out Vector2 spawnPoint);
}
