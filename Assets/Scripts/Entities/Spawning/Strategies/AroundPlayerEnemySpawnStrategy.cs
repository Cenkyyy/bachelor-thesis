using UnityEngine;

public sealed class AroundPlayerEnemySpawnStrategy : IEnemySpawnStrategy
{
    public bool TryGetSpawnPoint(Vector2 playerPosition, EnemySpawnSettings settings, out Vector2 spawnPoint)
    {
        var innerRadius = Mathf.Min(settings.MinSpawnDistance, settings.SpawnRadius);
        var outerRadius = settings.SpawnRadius;

        var direction = Random.insideUnitCircle.normalized;
        if (direction.sqrMagnitude < Mathf.Epsilon)
        {
            direction = Vector2.right;
        }

        var sampledRadius = Random.Range(innerRadius, outerRadius);
        spawnPoint = playerPosition + direction * sampledRadius;
        return true;
    }
}
