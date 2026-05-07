using UnityEngine;

public sealed class AroundPlayerSpawnStrategy : ISpawnStrategy
{
    public bool TryGetSpawnPoint(Vector2 playerPosition, EntitySpawnSettings _settings, out Vector2 spawnPoint)
    {
        var innerRadius = Mathf.Min(_settings.MinSpawnDistance, _settings.SpawnRadius);
        var outerRadius = _settings.SpawnRadius;

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
