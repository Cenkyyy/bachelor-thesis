using System.Collections.Generic;
using UnityEngine;

public class EntitySpawnController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _player;
    [SerializeField] private Transform _spawnParent;
    [SerializeField] private WorldGenerationController _worldGenerator;
    [SerializeField] private LayerMask _obstacleMask;

    [Header("Spawn Data")]
    [SerializeField] private List<EnemyData> _spawnableEntities = new();

    [Header("Behaviour")]
    [SerializeField] private SpawnStrategyType _spawnStrategyType = SpawnStrategyType.AroundPlayer;
    [SerializeField] private EntitySpawnSettings _settings = new();

    private EntitySpawner<EnemyData, EnemyCore> _entitySpawner;
    private float _nextSpawnTime;

    private void Awake()
    {
        if (_player == null)
        {
            var taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (taggedPlayer != null)
                _player = taggedPlayer.transform;
        }

        if (_spawnParent == null)
            _spawnParent = transform;

        if (_worldGenerator == null)
            _worldGenerator = FindFirstObjectByType<WorldGenerationController>();
    }

    private void Start()
    {
        TryBuildSpawner();
    }

    private void Update()
    {
        if (_entitySpawner == null)
        {
            if (!TryBuildSpawner())
                return;
        }

        if (_player == null)
            return;

        var playerPosition = (Vector2)_player.position;

        // Check for despawning far enemies before attempting to spawn new ones
        _entitySpawner.DespawnFarEntities(playerPosition);

        if (Time.time < _nextSpawnTime)
            return;

        if (!CanSpawnForCurrentDay())
            return;

        // Spawn cycle
        _entitySpawner.RunSpawnCycle(playerPosition);
        _nextSpawnTime = DayNightSystem.Instance != null && DayNightSystem.Instance.IsNight
            ? Time.time + _settings.SpawnIntervalSeconds * _settings.NightSpawnAccelerationMultiplier
            : Time.time + _settings.SpawnIntervalSeconds;
    }

    private bool TryBuildSpawner()
    {
        if (_worldGenerator == null || _worldGenerator.GroundTilemap == null || _worldGenerator.CurrentWorldData == null)
        {
            return false;
        }

        var factory = new EntityFactory<EnemyData, EnemyCore>();
        var strategy = CreateSpawnStrategy();
        var selection = new WeightedBiomeEntitySelectionStrategy<EnemyData>(_spawnableEntities);
        var worldQuery = new TilemapSpawnWorldQuery(_worldGenerator.GroundTilemap, _worldGenerator.CurrentWorldData, _obstacleMask);
        var registry = new SpawnedEntityRegistry<EnemyCore>();

        _entitySpawner = new EntitySpawner<EnemyData, EnemyCore>(factory, strategy, selection, worldQuery, registry, _settings, _spawnParent);

        return true;
    }

    private ISpawnStrategy CreateSpawnStrategy()
    {
        return _spawnStrategyType switch
        {
            SpawnStrategyType.AroundPlayer => new AroundPlayerSpawnStrategy(),
            _ => new AroundPlayerSpawnStrategy()
        };
    }

    private bool CanSpawnForCurrentDay()
    {
        if (DayNightSystem.Instance == null)
            return true;
        if (_settings == null || _settings.IgnoreFirstSpawnDay)
            return true;

        return DayNightSystem.Instance.CurrentDay >= _settings.FirstSpawnDay;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_player == null || _settings == null)
            return;

        // Draw radius for minimum spawn distance
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_player.position, _settings.MinSpawnDistance);

        // Draw radius for maximum spawn distance
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_player.position, _settings.SpawnRadius);

        // Draw radius for despawn distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_player.position, _settings.DespawnRadius);
    }
#endif
}
