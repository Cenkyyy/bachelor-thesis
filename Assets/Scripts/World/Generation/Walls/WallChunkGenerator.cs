using System;
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

    private struct PlannedWallTile
    {
        public Vector2Int DataTile;
        public WallData WallData;
    }

    private struct PlannedWallOre
    {
        public DecorationEntryData Entry;
        public Vector2Int AnchorTile;
    }

    private sealed class PlannedChunkContent
    {
        public List<PlannedWallTile> WallTiles = new();
        public List<PlannedWallOre> Ores = new();
    }

    [Header("Dependencies")]
    [SerializeField] private TerrainChunkGenerator _terrainChunkGenerator;
    [SerializeField] private Tilemap _wallTilemap;
    [SerializeField] private WallsListData _wallsListData;
    [SerializeField] private Transform _spawnedWallOresRoot;

    [Header("Feedback")]
    [SerializeField] private WorldTextPopupEmitter _feedbackPopupEmitter;
    [SerializeField] private string _higherToolRequiredMessage = "Higher tool is required";

    [Header("Wall Cluster Tuning")]
    [SerializeField] private bool _enableChunkUnloading = true;
    [SerializeField, Min(0)] private int _minClustersPerChunk = 1;
    [SerializeField, Min(0)] private int _maxClustersPerChunk = 3;
    [SerializeField, Min(1)] private int _maxSeedSearchAttempts = 24;
    [SerializeField, Min(0)] private int _defaultSpawnExclusionRadiusTiles = 5;

    private readonly Dictionary<BiomeType, BiomeWallsListData> _wallsByBiome = new();
    private readonly Dictionary<Vector2Int, List<Vector2Int>> _spawnedChunkTiles = new();
    private readonly Dictionary<Vector2Int, List<GameObject>> _spawnedChunkOres = new();
    private readonly Dictionary<Vector2Int, WallTileMineableRuntimeData> _runtimeByTile = new();
    private readonly WallTileModificationState _modificationState = new();
    private readonly List<Vector2Int> _replenishTickTilesBuffer = new();
    private readonly HashSet<Vector2Int> _tilesAwaitingReplenishTick = new();
    private readonly List<Vector3Int> _tileWriteCellsBuffer = new();
    private readonly List<TileBase> _tileWriteAssetsBuffer = new();
    private readonly GameObjectInstancePool _orePool = new();

    public event Action<Vector2Int> OnWallTileChanged;

    protected override bool EnableChunkUnloading => _enableChunkUnloading;

    private void Update()
    {
        if (_tilesAwaitingReplenishTick.Count == 0)
            return;

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
                continue;
            }

            if (!runtimeData.IsAwaitingReplenishTick)
                _tilesAwaitingReplenishTick.Remove(tile);
        }
    }

    protected override void OnEnable()
    {
        if (_spawnedWallOresRoot == null)
            _spawnedWallOresRoot = transform;
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
        _spawnedChunkOres.Clear();
        _runtimeByTile.Clear();
        _tilesAwaitingReplenishTick.Clear();
        _replenishTickTilesBuffer.Clear();
        _orePool.Clear();
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

        if (!_runtimeByTile.TryGetValue(dataTile, out var runtimeData))
            return false;

        target = runtimeData;
        return true;
    }

    public bool HasWallAtDataTile(Vector2Int dataTile)
    {
        return _runtimeByTile.ContainsKey(dataTile);
    }

    public bool TryGetWallDataAtDataTile(Vector2Int dataTile, out WallData wallData)
    {
        wallData = null;
        if (!_runtimeByTile.TryGetValue(dataTile, out var runtimeData))
            return false;

        wallData = runtimeData.WallData;
        return wallData != null;
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

    public bool CanMineTile(Vector2Int dataTile, MiningToolState tool)
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

    public void ApplyMiningDamage(Vector2Int dataTile, float basePower, Player miner, WorldItemSpawner dropSpawner)
    {
        if (!_runtimeByTile.TryGetValue(dataTile, out var runtimeData))
            return;

        bool depleted = runtimeData.ApplyDamage(basePower);

        if (!depleted)
            return;

        if (_wallTilemap != null)
            _wallTilemap.SetTile(worldGenerator.CurrentWorldData.DataToCell(dataTile.x, dataTile.y), null);

        _runtimeByTile.Remove(dataTile);
        _tilesAwaitingReplenishTick.Remove(dataTile);
        _modificationState.MarkRemoved(dataTile);
        runtimeData.MarkDepleted();
        OnWallTileChanged?.Invoke(dataTile);

        if (miner != null && runtimeData.WallData.MineableData != null)
            MiningDropUtility.ResolveDrops(runtimeData.WallData.MineableData.Drops, miner, dropSpawner, GetTileCenterWorld(dataTile));
    }

    public void NotifyMiningStopped(Vector2Int dataTile)
    {
        if (!_runtimeByTile.TryGetValue(dataTile, out var runtimeData))
            return;

        if (!runtimeData.HasDamage)
        {
            _tilesAwaitingReplenishTick.Remove(dataTile);
            runtimeData.StopMiningAndScheduleReplenish();
            return;
        }

        if (runtimeData.StopMiningAndScheduleReplenish())
        {
            _tilesAwaitingReplenishTick.Remove(dataTile);
            return;
        }

        if (runtimeData.IsAwaitingReplenishTick)
            _tilesAwaitingReplenishTick.Add(dataTile);
    }

    private IEnumerator GenerateChunkTiles(WorldRuntimeData data, Vector2Int chunkCoord, int yieldEveryOperations, YieldInstruction yieldInstruction)
    {
        if (_wallTilemap == null || _spawnedChunkTiles.ContainsKey(chunkCoord))
            yield break;

        var plan = BuildChunkPlan(data, chunkCoord);
        var chunkTiles = new List<Vector2Int>(plan.WallTiles.Count);
        int operationCount = 0;
        _tileWriteCellsBuffer.Clear();
        _tileWriteAssetsBuffer.Clear();

        for (int i = 0; i < plan.WallTiles.Count; i++)
        {
            var planned = plan.WallTiles[i];
            if (_modificationState.IsRemoved(planned.DataTile))
                continue;

            _tileWriteCellsBuffer.Add(data.DataToCell(planned.DataTile.x, planned.DataTile.y));
            _tileWriteAssetsBuffer.Add(planned.WallData.RuleTile);
            _runtimeByTile[planned.DataTile] = new WallTileMineableRuntimeData(this, planned.DataTile, planned.WallData);
            chunkTiles.Add(planned.DataTile);
            OnWallTileChanged?.Invoke(planned.DataTile);

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
        SpawnChunkOres(data, chunkCoord, plan.Ores);
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
            }
        }

        ApplyBufferedTileWrites();
        _spawnedChunkTiles.Remove(chunkCoord);
        UnloadChunkOres(chunkCoord);
    }

    private void ApplyBufferedTileWrites()
    {
        if (_wallTilemap == null || _tileWriteCellsBuffer.Count == 0)
            return;

        _wallTilemap.SetTiles(_tileWriteCellsBuffer.ToArray(), _tileWriteAssetsBuffer.ToArray());

        _tileWriteCellsBuffer.Clear();
        _tileWriteAssetsBuffer.Clear();
    }

    private PlannedChunkContent BuildChunkPlan(WorldRuntimeData data, Vector2Int chunkCoord)
    {
        var content = new PlannedChunkContent();
        var plannedTiles = new Dictionary<Vector2Int, PlannedWallTile>();

        int startX = chunkCoord.x * chunkSize;
        int startY = chunkCoord.y * chunkSize;

        if (startX < 0 || startY < 0 || startX >= data.Width || startY >= data.Height)
            return content;

        int width = Mathf.Min(chunkSize, data.Width - startX);
        int height = Mathf.Min(chunkSize, data.Height - startY);

        if (width <= 0 || height <= 0)
            return content;

        int chunkSeed = WorldSeedUtils.CombineSeed(worldGenerator.CurrentSeed, chunkCoord.x, chunkCoord.y);
        int seed = WorldSeedUtils.CombineSeed(chunkSeed, 99173, -99173);
        var rng = new System.Random(seed);

        int minClusterCount = Mathf.Max(0, _minClustersPerChunk);
        int maxClusterCount = Mathf.Max(minClusterCount, _maxClustersPerChunk);
        int clusterCount = rng.Next(minClusterCount, maxClusterCount + 1);

        for (int i = 0; i < clusterCount; i++)
            GrowSingleCluster(data, rng, startX, startY, width, height, plannedTiles, content.Ores);

        content.WallTiles = new List<PlannedWallTile>(plannedTiles.Values);
        return content;
    }

    private void GrowSingleCluster(
        WorldRuntimeData data,
        System.Random rng,
        int startX,
        int startY,
        int width,
        int height,
        Dictionary<Vector2Int, PlannedWallTile> plannedTiles,
        List<PlannedWallOre> plannedOres)
    {
        if (!TryFindValidSeedTile(data, rng, startX, startY, width, height, out var seedTile))
            return;
        var biomeSettings = _wallsByBiome[data.GetTile(seedTile.x, seedTile.y).Biome];
        var wallData = biomeSettings.Walls[rng.Next(0, biomeSettings.Walls.Count)];
        if (wallData == null || wallData.RuleTile == null || wallData.MineableData == null)
            return;

        int targetTileCount = rng.Next(biomeSettings.MinClusterSizeTiles, biomeSettings.MaxClusterSizeTiles + 1);

        if (!TryPlanTile(data, seedTile, wallData, plannedTiles))
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

            if (rng.NextDouble() > biomeSettings.ExpansionChance)
                continue;

            if (!TryPlanTile(data, candidate, wallData, plannedTiles))
                continue;

            clusterTiles.Add(candidate);

            if (rng.NextDouble() < biomeSettings.BranchingChance)
            {
                var branchDirection = CardinalDirections[rng.Next(0, CardinalDirections.Length)];
                var branchTile = candidate + branchDirection;
                bool branchWithinChunk =
                    branchTile.x >= startX && branchTile.x < startX + width &&
                    branchTile.y >= startY && branchTile.y < startY + height;

                if (branchWithinChunk && !plannedTiles.ContainsKey(branchTile) && data.GetTile(branchTile.x, branchTile.y).TileType != TileType.Void)
                {
                    if (TryPlanTile(data, branchTile, wallData, plannedTiles))
                        clusterTiles.Add(branchTile);
                }
            }
        }

        TryPlanClusterOres(data, rng, biomeSettings, wallData, clusterTiles, plannedTiles, plannedOres);
    }

    private bool TryPlanTile(WorldRuntimeData data, Vector2Int dataTile, WallData wallData, Dictionary<Vector2Int, PlannedWallTile> plannedTiles)
    {
        if (IsInsideDefaultSpawnExclusionRadius(data, dataTile.x, dataTile.y))
            return false;

        var worldTile = data.GetTile(dataTile.x, dataTile.y);
        if (!_wallsByBiome.ContainsKey(worldTile.Biome))
            return false;

        plannedTiles[dataTile] = new PlannedWallTile
        {
            DataTile = dataTile,
            WallData = wallData
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
            if (!_wallsByBiome.ContainsKey(biome))
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
        _wallsByBiome.Clear();
        if (_wallsListData == null)
            return;

        for (int i = 0; i < _wallsListData.BiomeWalls.Count; i++)
        {
            var entry = _wallsListData.BiomeWalls[i];
            if (entry == null)
                continue;

            _wallsByBiome[entry.Biome] = entry;
        }
    }

    private void TryPlanClusterOres(WorldRuntimeData data, System.Random rng, BiomeWallsListData biomeSettings, WallData wallData, List<Vector2Int> clusterTiles, Dictionary<Vector2Int, PlannedWallTile> plannedTiles, List<PlannedWallOre> plannedOres)
    {
        if (wallData.OreDecorations == null || wallData.OreDecorations.Count == 0)
            return;

        int oreCount = rng.Next(biomeSettings.MinOresPerCluster, biomeSettings.MaxOresPerCluster + 1);
        int placementAttemptsPerOre = 12;
        var clusterSet = new HashSet<Vector2Int>(clusterTiles);
        var occupiedOreTiles = new HashSet<Vector2Int>();
        var candidateTiles = new List<Vector2Int>(clusterTiles);
        foreach (var tile in clusterTiles)
            foreach (var dir in CardinalDirections)
                if (!clusterSet.Contains(tile + dir) && !candidateTiles.Contains(tile + dir))
                    candidateTiles.Add(tile + dir);

        for (int i = 0; i < oreCount; i++)
        {
            var ore = wallData.OreDecorations[rng.Next(0, wallData.OreDecorations.Count)];
            if (ore == null || ore.Prefab == null)
                continue;

            bool placed = false;
            for (int attempt = 0; attempt < placementAttemptsPerOre && !placed; attempt++)
            {
                if (candidateTiles.Count == 0)
                    continue;

                var tile = candidateTiles[rng.Next(0, candidateTiles.Count)];
                if (occupiedOreTiles.Contains(tile))
                    continue;

                bool inBounds = tile.x >= 0 && tile.x < data.Width && tile.y >= 0 && tile.y < data.Height;
                if (!inBounds || data.GetTile(tile.x, tile.y).TileType == TileType.Void)
                    continue;

                if (plannedTiles.ContainsKey(tile))
                    plannedTiles.Remove(tile);

                plannedOres.Add(new PlannedWallOre { Entry = ore, AnchorTile = tile });
                occupiedOreTiles.Add(tile);
                placed = true;
            }
        }
    }

    private void SpawnChunkOres(WorldRuntimeData data, Vector2Int chunkCoord, List<PlannedWallOre> ores)
    {
        var spawned = new List<GameObject>();
        for (int i = 0; i < ores.Count; i++)
        {
            var ore = ores[i];
            EnsureWallClearedAtDataTile(ore.AnchorTile);

            var worldPos = WorldObjectPlacementUtility.TileToWorldPosition(data, worldGenerator.GroundTilemap, ore.AnchorTile.x, ore.AnchorTile.y, 0f, 0f);
            var instance = _orePool.Acquire(ore.Entry.Prefab, worldPos, Quaternion.identity, _spawnedWallOresRoot, out _);
            if (instance != null)
                spawned.Add(instance);
        }

        _spawnedChunkOres[chunkCoord] = spawned;
    }

    private void EnsureWallClearedAtDataTile(Vector2Int dataTile)
    {
        if (!_runtimeByTile.ContainsKey(dataTile))
            return;

        if (_wallTilemap != null)
            _wallTilemap.SetTile(worldGenerator.CurrentWorldData.DataToCell(dataTile.x, dataTile.y), null);

        _runtimeByTile.Remove(dataTile);
        _tilesAwaitingReplenishTick.Remove(dataTile);

        var ownerChunk = WorldChunkUtility.GetChunkCoordFromTile(dataTile, chunkSize);
        if (_spawnedChunkTiles.TryGetValue(ownerChunk, out var chunkTiles))
            chunkTiles.Remove(dataTile);
    }

    private void UnloadChunkOres(Vector2Int chunkCoord)
    {
        if (!_spawnedChunkOres.TryGetValue(chunkCoord, out var spawned))
            return;

        for (int i = 0; i < spawned.Count; i++)
            _orePool.Release(spawned[i], _spawnedWallOresRoot);

        _spawnedChunkOres.Remove(chunkCoord);
    }

#if UNITY_EDITOR
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
#endif
}
