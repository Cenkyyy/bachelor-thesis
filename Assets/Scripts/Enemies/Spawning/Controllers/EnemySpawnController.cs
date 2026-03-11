using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemySpawnController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _player;
    [SerializeField] private Transform _spawnParent;
    [SerializeField] private WorldGeneratorBehaviour _worldGenerator;
    [SerializeField] private LayerMask _obstacleMask;

    [Header("Spawn Data")]
    [SerializeField] private List<EnemyData> _spawnableEnemies = new();

    [Header("Behaviour")]
    [SerializeField] private SpawnStrategyType _spawnStrategyType = SpawnStrategyType.AroundPlayer;
    [SerializeField] private EnemySpawnSettings _settings = new();

    private EnemySpawner<EnemyCore> _enemySpawner;
    private float _nextSpawnTime;

    private void Awake()
    {
        if (_player == null)
        {
            var taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (taggedPlayer != null)
            {
                _player = taggedPlayer.transform;
            }
        }

        if (_spawnParent == null)
        {
            _spawnParent = transform;
        }

        if (_worldGenerator == null)
        {
            _worldGenerator = FindFirstObjectByType<WorldGeneratorBehaviour>();
        }
    }

    private void Start()
    {
        TryBuildSpawner();
    }

    private void Update()
    {
        if (_enemySpawner == null)
        {
            if (!TryBuildSpawner())
            {
                return;
            }
        }

        if (_player == null)
        {
            return;
        }

        var playerPosition = (Vector2)_player.position;

        // Check for despawning far enemies before attempting to spawn new ones
        _enemySpawner.DespawnFarEnemies(playerPosition);

        if (Time.time < _nextSpawnTime)
        {
            return;
        }

        // Spawn cycle
        _enemySpawner.RunSpawnCycle(playerPosition);
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

        var factory = new EnemyFactory<EnemyCore>();
        var strategy = CreateSpawnStrategy();
        var selection = new WeightedBiomeEnemySelectionStrategy(_spawnableEnemies);
        var worldQuery = new TilemapSpawnWorldQuery(_worldGenerator.GroundTilemap, _worldGenerator.CurrentWorldData, _obstacleMask);
        var registry = new SpawnedEnemyRegistry();

        _enemySpawner = new EnemySpawner<EnemyCore>(factory, strategy, selection, worldQuery, registry, _settings, _spawnParent);

        return true;
    }

    private ISpawnStrategy CreateSpawnStrategy()
    {
        switch (_spawnStrategyType)
        {
            case SpawnStrategyType.AroundPlayer:
            default:
                return new AroundPlayerSpawnStrategy();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_player == null || _settings == null)
        {
            return;
        }

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
