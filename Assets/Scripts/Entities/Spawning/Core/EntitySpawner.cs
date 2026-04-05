using UnityEngine;

public sealed class EntitySpawner<TData, TEntity>
    where TData : EntityData
    where TEntity : EnemyCore
{
    private readonly IEntityFactory<TData, TEntity> _factory;
    private readonly ISpawnStrategy _spawnStrategy;
    private readonly IEntitySelectionStrategy<TData> _selectionStrategy;
    private readonly ISpawnWorldQuery _worldQuery;
    private readonly ISpawnRegistry<TEntity> _registry;
    private readonly EnemySpawnSettings _settings;
    private readonly Transform _spawnParent;

    public EntitySpawner(
        IEntityFactory<TData, TEntity> factory,
        ISpawnStrategy spawnStrategy,
        IEntitySelectionStrategy<TData> selectionStrategy,
        ISpawnWorldQuery worldQuery,
        ISpawnRegistry<TEntity> registry,
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
        var effectiveMaxAliveMultiplier = DayNightSystem.Instance != null && DayNightSystem.Instance.IsNight ? _settings.NightMaxAliveEnemiesMultiplier : 1f;
        var effectiveMaxAliveEntities = Mathf.Max(1, Mathf.FloorToInt(_settings.MaxAliveEnemies * effectiveMaxAliveMultiplier));

        if (_registry.AliveCount >= effectiveMaxAliveEntities)
        {
            return;
        }

        var remainingCapacity = effectiveMaxAliveEntities - _registry.AliveCount;
        var attempts = Mathf.Min(_settings.AttemptsPerCycle, remainingCapacity);

        for (var i = 0; i < attempts; i++)
        {
            TrySpawnOne(playerPosition);
        }
    }

    public void DespawnFarEntities(Vector2 playerPosition)
    {
        _registry.DespawnOutsideRadius(playerPosition, _settings.DespawnRadius);
    }

    private bool TrySpawnOne(Vector2 playerPosition)
    {
        for (var sample = 0; sample < _settings.MaxSamplesPerAttempt; sample++)
        {
            // Gets a candidate spawn point
            if (!_spawnStrategy.TryGetSpawnPoint(playerPosition, _settings, out var spawnPoint))
                return false;

            // Checks if the spawn point is on walkable terrain
            if (!_worldQuery.IsWalkable(spawnPoint, _settings.WalkableProbeRadius))
                continue;

            // Checks if the current spawn point is too close to an existing enemy
            if (_registry.HasAnyWithin(spawnPoint, _settings.MinSpacingFromEnemies))
                continue;

            // Gets the biome at the spawn point, which is used to determine what type of enemy to spawn
            if (!_worldQuery.TryGetBiome(spawnPoint, out var biome))
                continue;

            // Selects an enemy to spawn based on the biome
            if (!_selectionStrategy.TrySelect(biome, out var entityData))
                return false;

            var entity = _factory.Create(entityData, spawnPoint, _spawnParent);
            if (entity == null)
                return false;

            _registry.Register(entity);
            return true;
        }

        return false;
    }
}
