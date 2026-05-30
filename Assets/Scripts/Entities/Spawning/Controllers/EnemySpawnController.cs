using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class EnemySpawnController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _player;
    [SerializeField] private Transform _spawnParent;
    [SerializeField] private WorldGenerationController _worldGenerator;
    [SerializeField] private LayerMask _obstacleMask;

    [Header("Spawn Data")]
    [FormerlySerializedAs("_spawnableEntities")]
    [SerializeField] private List<EnemyData> _spawnableEnemies = new();

    [Header("Behaviour")]
    [SerializeField] private EnemySpawnStrategyType _spawnStrategyType = EnemySpawnStrategyType.AroundPlayer;
    [SerializeField] private EnemySpawnSettings _settings = new();

    private readonly List<ISceneTransitionReadinessBlocker> _worldReadinessBlockers = new();
    private EnemySpawner _enemySpawner;
    private float _nextSpawnTime;

    private void Awake()
    {
        if (_spawnParent == null)
            _spawnParent = transform;

        _worldReadinessBlockers.Clear();
        var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        for (var i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is ISceneTransitionReadinessBlocker blocker)
                _worldReadinessBlockers.Add(blocker);
        }
    }

    private void Start()
    {
        TryBuildSpawner();
    }

    private void Update()
    {
        if (!IsWorldReadyForEnemySpawning())
            return;

        if (_enemySpawner == null)
        {
            if (!TryBuildSpawner())
                return;
        }

        if (_player == null)
            return;

        var playerPosition = (Vector2)_player.position;
        _enemySpawner.DespawnFarEnemies(playerPosition);

        if (Time.time < _nextSpawnTime)
            return;

        if (!CanSpawnForCurrentDay())
            return;

        _enemySpawner.RunSpawnCycle(playerPosition, GetMaxAliveEnemies());
        _nextSpawnTime = Time.time + GetCurrentSpawnInterval();
    }

    public int DespawnEnemiesInsideMinimumSpawnDistance(Vector2 center)
    {
        if (_enemySpawner == null && !TryBuildSpawner())
            return 0;

        return _enemySpawner.DespawnEnemiesInsideMinimumSpawnDistance(center);
    }

    private bool TryBuildSpawner()
    {
        if (_worldGenerator == null || _worldGenerator.GroundTilemap == null || _worldGenerator.CurrentWorldData == null)
            return false;

        var factory = new EntityFactory<EnemyData, EnemyCore>();
        var strategy = CreateSpawnStrategy();
        var selection = new WeightedBiomeEnemySelectionStrategy(_spawnableEnemies);
        var worldQuery = new TilemapSpawnWorldQuery(_worldGenerator.GroundTilemap, _worldGenerator.CurrentWorldData, _obstacleMask);
        var registry = new SpawnedEntityRegistry<EnemyCore>();

        _enemySpawner = new EnemySpawner(factory, strategy, selection, worldQuery, registry, _settings, _spawnParent);
        return true;
    }

    private bool IsWorldReadyForEnemySpawning()
    {
        if (_worldGenerator == null || _worldGenerator.GroundTilemap == null || _worldGenerator.CurrentWorldData == null || !_worldGenerator.IsReadyForSceneReveal)
            return false;

        for (var i = 0; i < _worldReadinessBlockers.Count; i++)
        {
            if (!_worldReadinessBlockers[i].IsReadyForSceneReveal)
                return false;
        }

        return true;
    }

    private IEnemySpawnStrategy CreateSpawnStrategy()
    {
        return _spawnStrategyType switch
        {
            EnemySpawnStrategyType.AroundPlayer => new AroundPlayerEnemySpawnStrategy(),
            _ => new AroundPlayerEnemySpawnStrategy()
        };
    }

    private bool CanSpawnForCurrentDay()
    {
        if (DayNightSystem.Instance == null)
            return true;

        if (_settings == null || _settings.IgnoreFirstSpawnDay)
            return true;

        if (DayNightSystem.Instance.CurrentDay < _settings.FirstSpawnDay)
            return false;

        if (_settings.RequireNightOnFirstSpawnDay && DayNightSystem.Instance.CurrentDay == _settings.FirstSpawnDay)
            return DayNightSystem.Instance.IsNight;

        return true;
    }

    private int GetMaxAliveEnemies()
    {
        var multiplier = IsNight() ? _settings.NightMaxAliveEntitiesMultiplier : 1f;
        return Mathf.Max(1, Mathf.FloorToInt(_settings.MaxAliveEntities * multiplier));
    }

    private float GetCurrentSpawnInterval()
    {
        var multiplier = IsNight() ? _settings.NightSpawnAccelerationMultiplier : 1f;
        return _settings.SpawnIntervalSeconds * multiplier;
    }

    private static bool IsNight()
    {
        return DayNightSystem.Instance != null && DayNightSystem.Instance.IsNight;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_player == null || _settings == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_player.position, _settings.MinSpawnDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_player.position, _settings.SpawnRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_player.position, _settings.DespawnRadius);
    }
#endif
}
