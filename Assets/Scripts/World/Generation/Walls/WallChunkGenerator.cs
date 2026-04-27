using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public sealed class WallChunkGenerator : ChunkWorldContentGeneratorBase
{
    private static readonly Vector2Int[] CardinalDirections =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

    [System.Serializable]
    private sealed class BiomeWallTileSettings
    {
        [field: SerializeField] public BiomeType Biome { get; private set; }
        [field: SerializeField] public TileBase RuleTile { get; private set; }
        [field: SerializeField] public MineableNodeData MineableData { get; private set; }
    }

    private struct PlannedWallTile
    {
        public Vector2Int DataTile;
        public TileBase TileAsset;
        public MineableNodeData MineableData;
    }

    [Header("Dependencies")]
    [SerializeField] private TerrainChunkGenerator _terrainChunkGenerator;
    [SerializeField] private Tilemap _wallTilemap;
    [SerializeField] private Transform _wallMiningBarsContainer;

    [Header("Mining Bar")]
    [SerializeField] private MiningProgressBar _miningBarPrefab;
    [SerializeField, Min(0)] private int _maxInactiveMiningBars = 32;

    [Header("Feedback")]
    [SerializeField] private WorldTextPopupEmitter _feedbackPopupEmitter;
    [SerializeField] private string _higherToolRequiredMessage = "Higher tool is required";

    [Header("Biome Wall Settings")]
    [SerializeField] private List<BiomeWallTileSettings> _biomeWallTiles = new();

    [Header("Wall Cluster Tuning")]
    [SerializeField] private bool _enableChunkUnloading = true;
    [SerializeField, Min(0)] private int _minClustersPerChunk = 1;
    [SerializeField, Min(0)] private int _maxClustersPerChunk = 3;
    [SerializeField, Min(1)] private int _minClusterSizeTiles = 20;
    [SerializeField, Min(1)] private int _maxClusterSizeTiles = 50;
    [SerializeField, Range(0f, 1f)] private float _branchingChance = 0.35f;
    [SerializeField, Range(0f, 1f)] private float _expansionChance = 0.8f;
    [SerializeField, Min(1)] private int _maxSeedSearchAttempts = 24;
    [SerializeField, Min(0)] private int _defaultSpawnExclusionRadiusTiles = 5;

    private readonly Dictionary<BiomeType, BiomeWallTileSettings> _wallSettingsByBiome = new();
    private readonly Dictionary<Vector2Int, List<Vector2Int>> _spawnedChunkTiles = new();
    private readonly Dictionary<Vector2Int, WallTileRuntimeData> _runtimeByTile = new();
    private readonly WallTileModificationState _modificationState = new();
    private readonly Dictionary<Vector2Int, MiningProgressBar> _miningBarsByTile = new();
    private readonly Dictionary<Vector2Int, float> _inactiveMiningBarHiddenAtByTile = new();
    private readonly List<Vector2Int> _inactiveMiningBarTilesBuffer = new();
    private readonly List<Vector2Int> _replenishedTilesBuffer = new();
    private readonly List<Vector2Int> _replenishTickTilesBuffer = new();
    private readonly HashSet<Vector2Int> _tilesAwaitingReplenishTick = new();
    private readonly List<Vector3Int> _tileWriteCellsBuffer = new();
    private readonly List<TileBase> _tileWriteAssetsBuffer = new();

    protected override bool EnableChunkUnloading => _enableChunkUnloading;

    private void Update()
    {
        if (_tilesAwaitingReplenishTick.Count == 0)
            return;

        _replenishedTilesBuffer.Clear();
        _replenishTickTilesBuffer.Clear();
        _replenishTickTilesBuffer.AddRange(_tilesAwaitingReplenishTick);

        for (int i = 0; i < _replenishTickTilesBuffer.Count; i++)
        {
            var tile = _replenishTickTilesBuffer[i];
            if (!_runtimeByTile.TryGetValue(tile, out var runtimeData))
            {
                _tilesAwaitingReplenishTick.Remove(tile);
                continue;
            }

            if (runtimeData.TickReplenish(Time.deltaTime))
            {
                _tilesAwaitingReplenishTick.Remove(tile);
                _replenishedTilesBuffer.Add(tile);
                continue;
            }

            if (!runtimeData.IsAwaitingReplenishTick)
                _tilesAwaitingReplenishTick.Remove(tile);
        }

        for (int i = 0; i < _replenishedTilesBuffer.Count; i++)
            HandleTileReplenished(_replenishedTilesBuffer[i]);
    }

    protected override void OnEnable()
    {
        BuildBiomeWallIndex();
        base.OnEnable();
    }

    protected override bool CanStartStreaming()
    {
        return _terrainChunkGenerator == null || _terrainChunkGenerator.IsReadyForSceneReveal;
    }

    protected override bool IsChunkLoaded(Vector2Int chunkCoord)
    {
        return _spawnedChunkTiles.ContainsKey(chunkCoord);
    }

    protected override void GenerateChunk(WorldRuntimeData data, Vector2Int chunkCoord)
    {
        RunImmediate(GenerateChunkTiles(data, chunkCoord, 0, null));
    }

    protected override void UnloadChunk(Vector2Int chunkCoord)
    {
        RunImmediate(UnloadChunkTiles(chunkCoord, 0, null));
    }

    protected override IEnumerable<Vector2Int> GetLoadedChunks()
    {
        foreach (var pair in _spawnedChunkTiles)
            yield return pair.Key;
    }

    protected override void ClearGeneratedChunks()
    {
        if (_wallTilemap != null)
            _wallTilemap.ClearAllTiles();

        _spawnedChunkTiles.Clear();
        _runtimeByTile.Clear();
        _tilesAwaitingReplenishTick.Clear();
        _replenishTickTilesBuffer.Clear();
        _replenishedTilesBuffer.Clear();
        DestroyMiningBars();
    }

    protected override IEnumerator GenerateChunkCoroutine(WorldRuntimeData data, Vector2Int chunkCoord)
    {
        yield return GenerateChunkTiles(data, chunkCoord, loadOperationsPerFrame, null);
    }

    protected override IEnumerator UnloadChunkCoroutine(Vector2Int chunkCoord)
    {
        yield return UnloadChunkTiles(chunkCoord, loadOperationsPerFrame, null);
    }

    public bool TryCreateMiningTarget(Vector3 worldPosition, out IMineableTarget target)
    {
        target = null;

        if (_wallTilemap == null)
            return false;

        var cell = _wallTilemap.WorldToCell(worldPosition);
        var dataTile = worldGenerator.CurrentWorldData.CellToData(cell);

        if (!_runtimeByTile.ContainsKey(dataTile))
            return false;

        target = new WallTileMiningTarget(this, dataTile);
        return true;
    }

    public bool HasWallAtDataTile(Vector2Int dataTile)
    {
        return _runtimeByTile.ContainsKey(dataTile);
    }

    public bool IsChunkLoadedAt(Vector2Int chunkCoord)
    {
        return _spawnedChunkTiles.ContainsKey(chunkCoord);
    }

    public Vector3 GetTileCenterWorld(Vector2Int dataTile)
    {
        if (_wallTilemap == null)
            return Vector3.zero;

        var cell = worldGenerator.CurrentWorldData.DataToCell(dataTile.x, dataTile.y);
        return _wallTilemap.GetCellCenterWorld(cell);
    }

    public bool CanMineTile(Vector2Int dataTile, MiningToolContext tool)
    {
        return _runtimeByTile.TryGetValue(dataTile, out var runtimeData) && runtimeData.CanBeMinedWith(tool);
    }

    public void ShowHigherToolRequiredFeedback(Vector2Int dataTile)
    {
        if (!_runtimeByTile.ContainsKey(dataTile) || _feedbackPopupEmitter == null || string.IsNullOrWhiteSpace(_higherToolRequiredMessage))
            return;

        if (_feedbackPopupEmitter.HasActivePopup)
            return;

        _feedbackPopupEmitter.transform.position = GetTileCenterWorld(dataTile);
        _feedbackPopupEmitter.ShowMessage(_higherToolRequiredMessage);
    }

    public void ApplyMiningDamage(Vector2Int dataTile, float basePower, Player miner, ItemDropSpawner dropSpawner)
    {
        if (!_runtimeByTile.TryGetValue(dataTile, out var runtimeData))
            return;

        runtimeData.NotifyMiningStarted();
        var miningBar = EnsureMiningBar(dataTile);

        bool depleted = runtimeData.ApplyDamage(basePower);
        miningBar?.SetProgressValue(runtimeData.MiningProgressNormalized);

        if (!depleted)
            return;

        if (_wallTilemap != null)
            _wallTilemap.SetTile(worldGenerator.CurrentWorldData.DataToCell(dataTile.x, dataTile.y), null);

        _runtimeByTile.Remove(dataTile);
        _tilesAwaitingReplenishTick.Remove(dataTile);
        _modificationState.MarkRemoved(dataTile);
        DestroyMiningBarForTile(dataTile);

        if (miner != null && runtimeData.MineableData != null)
            MiningDropResolver.ResolveDrops(runtimeData.MineableData.Drops, miner, dropSpawner, GetTileCenterWorld(dataTile));
    }

    public void NotifyMiningStarted(Vector2Int dataTile)
    {
        if (!_runtimeByTile.TryGetValue(dataTile, out var runtimeData))
            return;

        runtimeData.NotifyMiningStarted();
        var miningBar = EnsureMiningBar(dataTile);
        miningBar?.SetProgressValue(runtimeData.MiningProgressNormalized);
    }

    public void NotifyMiningStopped(Vector2Int dataTile)
    {
        if (!_runtimeByTile.TryGetValue(dataTile, out var runtimeData))
            return;

        if (!runtimeData.HasDamage)
        {
            _tilesAwaitingReplenishTick.Remove(dataTile);
            HideMiningBarForTile(dataTile);
            runtimeData.NotifyMiningStopped();
            return;
        }

        if (runtimeData.NotifyMiningStopped())
        {
            _tilesAwaitingReplenishTick.Remove(dataTile);
            HandleTileReplenished(dataTile);
            return;
        }

        if (runtimeData.IsAwaitingReplenishTick)
            _tilesAwaitingReplenishTick.Add(dataTile);
    }

    private MiningProgressBar EnsureMiningBar(Vector2Int dataTile)
    {
        if (_miningBarsByTile.TryGetValue(dataTile, out var existingBar) && existingBar != null)
        {
            if (!existingBar.gameObject.activeSelf)
                existingBar.gameObject.SetActive(true);

            _inactiveMiningBarHiddenAtByTile.Remove(dataTile);
            return existingBar;
        }
        
        if (_miningBarPrefab == null)
            return null;

        var miningBarParent = _wallMiningBarsContainer != null ? _wallMiningBarsContainer : transform;
        var miningBar = Instantiate(_miningBarPrefab, miningBarParent);
        miningBar.transform.position = GetTileCenterWorld(dataTile);
        miningBar.SetIdle();
        _miningBarsByTile[dataTile] = miningBar;
        return miningBar;
    }

    private void HideMiningBarForTile(Vector2Int dataTile)
    {
        if (!_miningBarsByTile.TryGetValue(dataTile, out var miningBar) || miningBar == null)
            return;

        miningBar.SetIdle();
        if (miningBar.gameObject.activeSelf)
            miningBar.gameObject.SetActive(false);

        _inactiveMiningBarHiddenAtByTile[dataTile] = Time.time;
        EnforceInactiveMiningBarsCap();
    }

    private void DestroyMiningBars()
    {
        if (_miningBarsByTile.Count == 0)
            return;

        foreach (var pair in _miningBarsByTile)
        {
            var miningBar = pair.Value;
            if (miningBar == null)
                continue;

            if (Application.isPlaying)
                Destroy(miningBar.gameObject);
            else
                DestroyImmediate(miningBar.gameObject);
        }

        _miningBarsByTile.Clear();
        _inactiveMiningBarHiddenAtByTile.Clear();
    }

    private void DestroyMiningBarForTile(Vector2Int dataTile)
    {
        if (!_miningBarsByTile.TryGetValue(dataTile, out var miningBar))
            return;

        if (miningBar != null)
        {
            if (Application.isPlaying)
                Destroy(miningBar.gameObject);
            else
                DestroyImmediate(miningBar.gameObject);
        }

        _miningBarsByTile.Remove(dataTile);
        _inactiveMiningBarHiddenAtByTile.Remove(dataTile);
    }

    private void HandleTileReplenished(Vector2Int dataTile)
    {
        HideMiningBarForTile(dataTile);
    }

    private void EnforceInactiveMiningBarsCap()
    {
        _inactiveMiningBarTilesBuffer.Clear();

        foreach (var pair in _miningBarsByTile)
        {
            var tile = pair.Key;
            var miningBar = pair.Value;
            if (miningBar == null || miningBar.gameObject.activeSelf)
                continue;

            if (!_inactiveMiningBarHiddenAtByTile.ContainsKey(tile))
                _inactiveMiningBarHiddenAtByTile[tile] = 0f;

            _inactiveMiningBarTilesBuffer.Add(tile);
        }

        int inactiveCount = _inactiveMiningBarTilesBuffer.Count;
        if (inactiveCount <= _maxInactiveMiningBars)
            return;

        _inactiveMiningBarTilesBuffer.Sort((left, right) =>
        {
            float leftHiddenAt = _inactiveMiningBarHiddenAtByTile[left];
            float rightHiddenAt = _inactiveMiningBarHiddenAtByTile[right];
            return leftHiddenAt.CompareTo(rightHiddenAt);
        });

        int barsToDestroy = inactiveCount - _maxInactiveMiningBars;
        for (int i = 0; i < barsToDestroy; i++)
            DestroyMiningBarForTile(_inactiveMiningBarTilesBuffer[i]);
    }

    private IEnumerator GenerateChunkTiles(WorldRuntimeData data, Vector2Int chunkCoord, int yieldEveryOperations, YieldInstruction yieldInstruction)
    {
        if (_wallTilemap == null || _spawnedChunkTiles.ContainsKey(chunkCoord))
            yield break;

        var plannedTiles = BuildWallTilesForChunk(data, chunkCoord);
        var chunkTiles = new List<Vector2Int>(plannedTiles.Count);
        int operationCount = 0;
        _tileWriteCellsBuffer.Clear();
        _tileWriteAssetsBuffer.Clear();

        for (int i = 0; i < plannedTiles.Count; i++)
        {
            var planned = plannedTiles[i];
            if (_modificationState.IsRemoved(planned.DataTile))
                continue;

            _tileWriteCellsBuffer.Add(data.DataToCell(planned.DataTile.x, planned.DataTile.y));
            _tileWriteAssetsBuffer.Add(planned.TileAsset);
            _runtimeByTile[planned.DataTile] = new WallTileRuntimeData(planned.DataTile, planned.MineableData);
            chunkTiles.Add(planned.DataTile);

            operationCount++;
            if (yieldEveryOperations > 0 && operationCount >= yieldEveryOperations)
            {
                ApplyBufferedTileWrites();
                operationCount = 0;
                yield return yieldInstruction;
            }
        }

        ApplyBufferedTileWrites();
        _spawnedChunkTiles[chunkCoord] = chunkTiles;
    }

    private IEnumerator UnloadChunkTiles(Vector2Int chunkCoord, int yieldEveryOperations, YieldInstruction yieldInstruction)
    {
        if (!_spawnedChunkTiles.TryGetValue(chunkCoord, out var tiles))
            yield break;

        int operationCount = 0;
        _tileWriteCellsBuffer.Clear();
        _tileWriteAssetsBuffer.Clear();

        if (_wallTilemap != null)
        {
            var worldData = worldGenerator.CurrentWorldData;
            for (int i = 0; i < tiles.Count; i++)
            {
                var tile = tiles[i];
                _tileWriteCellsBuffer.Add(worldData.DataToCell(tile.x, tile.y));
                _tileWriteAssetsBuffer.Add(null);
                _runtimeByTile.Remove(tile);
                _tilesAwaitingReplenishTick.Remove(tile);
                DestroyMiningBarForTile(tile);

                operationCount++;
                if (yieldEveryOperations > 0 && operationCount >= yieldEveryOperations)
                {
                    ApplyBufferedTileWrites();
                    operationCount = 0;
                    yield return yieldInstruction;
                }
            }
        }
        else
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                var tile = tiles[i];
                _runtimeByTile.Remove(tile);
                _tilesAwaitingReplenishTick.Remove(tile);
                DestroyMiningBarForTile(tile);
            }
        }

        ApplyBufferedTileWrites();
        _spawnedChunkTiles.Remove(chunkCoord);
    }

    private void ApplyBufferedTileWrites()
    {
        if (_wallTilemap == null || _tileWriteCellsBuffer.Count == 0)
            return;

        _wallTilemap.SetTiles(_tileWriteCellsBuffer.ToArray(), _tileWriteAssetsBuffer.ToArray());

        _tileWriteCellsBuffer.Clear();
        _tileWriteAssetsBuffer.Clear();
    }

    private List<PlannedWallTile> BuildWallTilesForChunk(WorldRuntimeData data, Vector2Int chunkCoord)
    {
        var plannedTiles = new Dictionary<Vector2Int, PlannedWallTile>();

        int startX = chunkCoord.x * chunkSize;
        int startY = chunkCoord.y * chunkSize;

        if (startX < 0 || startY < 0 || startX >= data.Width || startY >= data.Height)
            return new List<PlannedWallTile>();

        int width = Mathf.Min(chunkSize, data.Width - startX);
        int height = Mathf.Min(chunkSize, data.Height - startY);

        if (width <= 0 || height <= 0)
            return new List<PlannedWallTile>();

        int chunkSeed = WorldSeedUtils.CombineSeed(worldGenerator.CurrentSeed, chunkCoord.x, chunkCoord.y);
        int seed = WorldSeedUtils.CombineSeed(chunkSeed, 99173, -99173);
        var rng = new System.Random(seed);

        int minClusterCount = Mathf.Max(0, _minClustersPerChunk);
        int maxClusterCount = Mathf.Max(minClusterCount, _maxClustersPerChunk);
        int clusterCount = rng.Next(minClusterCount, maxClusterCount + 1);

        for (int i = 0; i < clusterCount; i++)
            GrowSingleCluster(data, rng, startX, startY, width, height, plannedTiles);

        return new List<PlannedWallTile>(plannedTiles.Values);
    }

    private void GrowSingleCluster(
        WorldRuntimeData data,
        System.Random rng,
        int startX,
        int startY,
        int width,
        int height,
        Dictionary<Vector2Int, PlannedWallTile> plannedTiles)
    {
        if (!TryFindValidSeedTile(data, rng, startX, startY, width, height, out var seedTile))
            return;

        int minClusterSize = Mathf.Max(1, _minClusterSizeTiles);
        int maxClusterSize = Mathf.Max(minClusterSize, _maxClusterSizeTiles);
        int targetTileCount = rng.Next(minClusterSize, maxClusterSize + 1);

        if (!TryPlanTile(data, seedTile, plannedTiles))
            return;

        var clusterTiles = new List<Vector2Int>(targetTileCount) { seedTile };

        int safetyBudget = targetTileCount * 20;
        int safety = 0;

        while (clusterTiles.Count < targetTileCount && safety < safetyBudget)
        {
            safety++;
            var anchor = clusterTiles[rng.Next(0, clusterTiles.Count)];
            var direction = CardinalDirections[rng.Next(0, CardinalDirections.Length)];
            var candidate = anchor + direction;

            bool withinChunk =
                candidate.x >= startX && candidate.x < startX + width &&
                candidate.y >= startY && candidate.y < startY + height;

            if (!withinChunk || plannedTiles.ContainsKey(candidate))
                continue;

            if (data.GetTile(candidate.x, candidate.y).TileType == TileType.Void)
                continue;

            if (rng.NextDouble() > _expansionChance)
                continue;

            if (!TryPlanTile(data, candidate, plannedTiles))
                continue;

            clusterTiles.Add(candidate);

            if (rng.NextDouble() < _branchingChance)
            {
                var branchDirection = CardinalDirections[rng.Next(0, CardinalDirections.Length)];
                var branchTile = candidate + branchDirection;
                bool branchWithinChunk =
                    branchTile.x >= startX && branchTile.x < startX + width &&
                    branchTile.y >= startY && branchTile.y < startY + height;

                if (branchWithinChunk && !plannedTiles.ContainsKey(branchTile) && data.GetTile(branchTile.x, branchTile.y).TileType != TileType.Void)
                {
                    if (TryPlanTile(data, branchTile, plannedTiles))
                        clusterTiles.Add(branchTile);
                }
            }
        }
    }

    private bool TryPlanTile(WorldRuntimeData data, Vector2Int dataTile, Dictionary<Vector2Int, PlannedWallTile> plannedTiles)
    {
        if (IsInsideDefaultSpawnExclusionRadius(data, dataTile.x, dataTile.y))
            return false;

        var worldTile = data.GetTile(dataTile.x, dataTile.y);
        if (!_wallSettingsByBiome.TryGetValue(worldTile.Biome, out var wallSettings) || wallSettings == null)
            return false;

        if (wallSettings.RuleTile == null || wallSettings.MineableData == null)
            return false;

        plannedTiles[dataTile] = new PlannedWallTile
        {
            DataTile = dataTile,
            TileAsset = wallSettings.RuleTile,
            MineableData = wallSettings.MineableData
        };

        return true;
    }

    private bool TryFindValidSeedTile(WorldRuntimeData data, System.Random rng, int startX, int startY, int width, int height, out Vector2Int seed)
    {
        int maxAttempts = Mathf.Max(1, _maxSeedSearchAttempts);

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int x = startX + rng.Next(0, width);
            int y = startY + rng.Next(0, height);

            if (data.GetTile(x, y).TileType == TileType.Void)
                continue;

            if (IsInsideDefaultSpawnExclusionRadius(data, x, y))
                continue;

            var biome = data.GetTile(x, y).Biome;
            if (!_wallSettingsByBiome.TryGetValue(biome, out var wallSettings) || wallSettings == null || wallSettings.RuleTile == null || wallSettings.MineableData == null)
                continue;

            seed = new Vector2Int(x, y);
            return true;
        }

        seed = default;
        return false;
    }

    private bool IsInsideDefaultSpawnExclusionRadius(WorldRuntimeData data, int tileX, int tileY)
    {
        if (_defaultSpawnExclusionRadiusTiles <= 0)
            return false;

        int dx = tileX - data.DefaultSpawnTile.x;
        int dy = tileY - data.DefaultSpawnTile.y;
        int exclusionRadiusSquared = _defaultSpawnExclusionRadiusTiles * _defaultSpawnExclusionRadiusTiles;
        return dx * dx + dy * dy <= exclusionRadiusSquared;
    }

    private void BuildBiomeWallIndex()
    {
        _wallSettingsByBiome.Clear();

        for (int i = 0; i < _biomeWallTiles.Count; i++)
        {
            var entry = _biomeWallTiles[i];
            if (entry == null)
                continue;

            _wallSettingsByBiome[entry.Biome] = entry;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (worldGenerator == null || worldGenerator.GroundTilemap == null || worldGenerator.CurrentWorldData == null)
            return;

        var spawnCell = worldGenerator.CurrentWorldData.DataToCell(worldGenerator.CurrentWorldData.DefaultSpawnTile.x, worldGenerator.CurrentWorldData.DefaultSpawnTile.y);
        var spawnCellCenter = worldGenerator.GroundTilemap.GetCellCenterWorld(spawnCell);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(spawnCellCenter, _defaultSpawnExclusionRadiusTiles);
        Handles.Label(spawnCellCenter + Vector3.up * _defaultSpawnExclusionRadiusTiles, "Default Spawn Exclusion Radius");
    }
}
