using UnityEngine;

public sealed class EnemySpawner<TEnemy> where TEnemy : EnemyCore
{
    private readonly IEnemyFactory<TEnemy> _factory;
    private readonly ISpawnStrategy _spawnStrategy;
    private readonly IEnemySelectionStrategy _selectionStrategy;
    private readonly ISpawnWorldQuery _worldQuery;
    private readonly SpawnedEnemyRegistry _registry;
    private readonly EnemySpawnSettings _settings;
    private readonly Transform _spawnParent;

    public EnemySpawner(
        IEnemyFactory<TEnemy> factory,
        ISpawnStrategy spawnStrategy,
        IEnemySelectionStrategy selectionStrategy,
        ISpawnWorldQuery worldQuery,
        SpawnedEnemyRegistry registry,
        EnemySpawnSettings settings,
        Transform spawnParent)
    {
        _factory = factory;
        _spawnStrategy = spawnStrategy;
        _selectionStrategy = selectionStrategy;
        _worldQuery = worldQuery;
        _registry = registry;
        _settings = settings;
        _spawnParent = spawnParent;
    }

    public void RunSpawnCycle(Vector2 playerPosition)
    {
        if (_registry.AliveCount >= _settings.MaxAliveEnemies)
        {
            return;
        }

        var remainingCapacity = _settings.MaxAliveEnemies - _registry.AliveCount;
        var attempts = Mathf.Min(_settings.AttemptsPerCycle, remainingCapacity);

        for (var i = 0; i < attempts; i++)
        {
            TrySpawnOne(playerPosition, _settings);
        }
    }

    public void DespawnFarEnemies(Vector2 playerPosition)
    {
        _registry.DespawnOutsideRadius(playerPosition, _settings.DespawnRadius);
    }

    private bool TrySpawnOne(Vector2 playerPosition, EnemySpawnSettings _settings)
    {
        for (var sample = 0; sample < _settings.MaxSamplesPerAttempt; sample++)
        {
            // Gets a candidate spawn point
            if (!_spawnStrategy.TryGetSpawnPoint(playerPosition, _settings, out var spawnPoint))
            {
                return false;
            }

            // Checks if the spawn point is on walkable terrain
            if (!_worldQuery.IsWalkable(spawnPoint, _settings.WalkableProbeRadius))
            {
                continue;
            }

            // Checks if the current spawn point is too close to an existing enemy
            if (_registry.HasEnemyWithin(spawnPoint, _settings.MinSpacingFromEnemies))
            {
                continue;
            }

            // Gets the biome at the spawn point, which is used to determine what type of enemy to spawn
            if (!_worldQuery.TryGetBiome(spawnPoint, out var biome))
            {
                continue;
            }

            // Selects an enemy to spawn based on the biome
            if (!_selectionStrategy.TrySelect(biome, out var enemyData))
            {
                return false;
            }

            var enemy = _factory.Create(enemyData, spawnPoint, _spawnParent);
            if (enemy == null)
            {
                return false;
            }

            _registry.Register(enemy);
            return true;
        }

        return false;
    }
}
