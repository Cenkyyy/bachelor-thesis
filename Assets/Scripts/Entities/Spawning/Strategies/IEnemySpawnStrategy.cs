using UnityEngine;

public interface IEnemySpawnStrategy
{
    bool TryGetSpawnPoint(Vector2 playerPosition, EnemySpawnSettings settings, out Vector2 spawnPoint);
}
