using UnityEngine;

public sealed class EnemySpawner
{
    private readonly IEntityFactory<EnemyData, EnemyCore> _factory;
    private readonly IEnemySpawnStrategy _spawnStrategy;
    private readonly IEntitySelectionStrategy<EnemyData> _selectionStrategy;
    private readonly ISpawnWorldQuery _worldQuery;
    private readonly ISpawnRegistry<EnemyCore> _registry;
    private readonly EnemySpawnSettings _settings;
    private readonly Transform _spawnParent;

    public EnemySpawner(
        IEntityFactory<EnemyData, EnemyCore> factory,
        IEnemySpawnStrategy spawnStrategy,
        IEntitySelectionStrategy<EnemyData> selectionStrategy,
        ISpawnWorldQuery worldQuery,
        ISpawnRegistry<EnemyCore> registry,
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

    public void RunSpawnCycle(Vector2 playerPosition, int maxAliveEnemies)
    {
        var effectiveMaxAliveEnemies = Mathf.Max(1, maxAliveEnemies);
        if (_registry.AliveCount >= effectiveMaxAliveEnemies)
        {
            return;
        }

        var remainingCapacity = effectiveMaxAliveEnemies - _registry.AliveCount;
        var attempts = Mathf.Min(_settings.AttemptsPerCycle, remainingCapacity);

        for (var i = 0; i < attempts; i++)
        {
            TrySpawnOne(playerPosition);
        }
    }

    public void DespawnFarEnemies(Vector2 playerPosition)
    {
        _registry.DespawnOutsideRadius(playerPosition, _settings.DespawnRadius);
    }

    public int DespawnEnemiesInsideMinimumSpawnDistance(Vector2 playerPosition)
    {
        return _registry.DespawnInsideRadius(playerPosition, _settings.MinSpawnDistance);
    }

    private bool TrySpawnOne(Vector2 playerPosition)
    {
        for (var sample = 0; sample < _settings.MaxSamplesPerAttempt; sample++)
        {
            if (!_spawnStrategy.TryGetSpawnPoint(playerPosition, _settings, out var spawnPoint))
                return false;

            if (!_worldQuery.IsWalkable(spawnPoint, _settings.WalkableProbeRadius))
                continue;

            if (_registry.HasAnyWithin(spawnPoint, _settings.MinSpacingFromEntities))
                continue;

            if (!_worldQuery.TryGetBiome(spawnPoint, out var biome))
                continue;

            if (!_selectionStrategy.TrySelect(biome, out var enemyData))
                return false;

            var enemy = _factory.Create(enemyData, spawnPoint, _spawnParent);
            if (enemy == null)
                return false;

            _registry.Register(enemy);
            return true;
        }

        return false;
    }
}
