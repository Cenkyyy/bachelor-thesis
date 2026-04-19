using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

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

    [Header("Mining Bar")]
    [SerializeField] private MiningProgressBar _miningBarPrefab;
    [SerializeField] private Transform _miningBarRoot;
    [SerializeField] private Vector3 _miningBarOffset = new Vector3(0f, 0.8f, 0f);

    [Header("Biome Wall Settings")]
    [SerializeField] private List<BiomeWallTileSettings> _biomeWallTiles = new();
    [SerializeField] private string _higherToolRequiredMessage = "Higher tool is required";

    [Header("Wall Cluster Tuning")]
    [SerializeField] private bool _enableChunkUnloading = true;
    [SerializeField, Min(0)] private int _minClustersPerChunk = 1;
    [SerializeField, Min(0)] private int _maxClustersPerChunk = 3;
    [SerializeField, Min(1)] private int _minClusterSizeTiles = 20;
    [SerializeField, Min(1)] private int _maxClusterSizeTiles = 50;
    [SerializeField, Range(0f, 1f)] private float _branchingChance = 0.35f;
    [SerializeField, Range(0f, 1f)] private float _expansionChance = 0.8f;
    [SerializeField, Min(1)] private int _maxSeedSearchAttempts = 24;

    private readonly Dictionary<BiomeType, BiomeWallTileSettings> _wallSettingsByBiome = new();
    private readonly Dictionary<Vector2Int, List<Vector2Int>> _spawnedChunkTiles = new();
    private readonly Dictionary<Vector2Int, WallTileRuntimeData> _runtimeByTile = new();
    private readonly WallTileModificationState _modificationState = new();

    private MiningProgressBar _activeMiningBar;
    private Vector2Int? _activeMiningBarTile;

    protected override bool EnableChunkUnloading => _enableChunkUnloading;

    protected override void OnEnable()
    {
        BuildBiomeWallIndex();
        base.OnEnable();
    }

    protected override bool CanStartStreaming(WorldRuntimeData data)
    {
        return _terrainChunkGenerator == null || _terrainChunkGenerator.IsReadyForSceneReveal;
    }

    protected override bool IsChunkLoaded(Vector2Int chunkCoord)
    {
        return _spawnedChunkTiles.ContainsKey(chunkCoord);
    }

    protected override void GenerateChunk(WorldRuntimeData data, Vector2Int chunkCoord)
    {
        if (_wallTilemap == null || _spawnedChunkTiles.ContainsKey(chunkCoord))
            return;

        var plannedTiles = BuildWallTilesForChunk(data, chunkCoord);
        var chunkTiles = new List<Vector2Int>(plannedTiles.Count);

        for (int i = 0; i < plannedTiles.Count; i++)
        {
            var planned = plannedTiles[i];
            if (_modificationState.IsRemoved(planned.DataTile))
                continue;

            _wallTilemap.SetTile(data.DataToCell(planned.DataTile.x, planned.DataTile.y), planned.TileAsset);
            _runtimeByTile[planned.DataTile] = new WallTileRuntimeData(planned.DataTile, planned.MineableData);
            chunkTiles.Add(planned.DataTile);
        }

        _spawnedChunkTiles[chunkCoord] = chunkTiles;
    }

    protected override void UnloadChunk(Vector2Int chunkCoord)
    {
        if (!_spawnedChunkTiles.TryGetValue(chunkCoord, out var tiles))
            return;

        if (_wallTilemap != null)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                var tile = tiles[i];
                _wallTilemap.SetTile(_worldGenerator.CurrentWorldData.DataToCell(tile.x, tile.y), null);
                _runtimeByTile.Remove(tile);
            }
        }

        _spawnedChunkTiles.Remove(chunkCoord);
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
        ClearMiningBar();
    }

    public bool TryCreateMiningTarget(Vector3 worldPosition, out IMineableTarget target)
    {
        target = null;

        if (_wallTilemap == null)
            return false;

        var cell = _wallTilemap.WorldToCell(worldPosition);
        var dataTile = _worldGenerator.CurrentWorldData.CellToData(cell);

        if (!_runtimeByTile.ContainsKey(dataTile))
            return false;

        target = new WallTileMiningTarget(this, dataTile);
        return true;
    }

    public bool HasWallAtDataTile(Vector2Int dataTile)
    {
        return _runtimeByTile.ContainsKey(dataTile);
    }

    public Vector3 GetTileCenterWorld(Vector2Int dataTile)
    {
        if (_wallTilemap == null)
            return Vector3.zero;

        var cell = _worldGenerator.CurrentWorldData.DataToCell(dataTile.x, dataTile.y);
        return _wallTilemap.GetCellCenterWorld(cell);
    }

    public bool CanMineTile(Vector2Int dataTile, MiningToolContext tool)
    {
        return _runtimeByTile.TryGetValue(dataTile, out var runtimeData) && runtimeData.CanBeMinedWith(tool);
    }

    public void ShowHigherToolRequiredFeedback()
    {
        _ = _higherToolRequiredMessage;
    }

    public void ApplyMiningDamage(Vector2Int dataTile, float basePower, Player miner, ItemDropSpawner dropSpawner)
    {
        if (!_runtimeByTile.TryGetValue(dataTile, out var runtimeData))
            return;

        EnsureMiningBar(dataTile);

        bool depleted = runtimeData.ApplyDamage(basePower);
        if (_activeMiningBar != null && _activeMiningBarTile.HasValue && _activeMiningBarTile.Value == dataTile)
            _activeMiningBar.SetProgressValue(runtimeData.MiningProgressNormalized);

        if (!depleted)
            return;

        if (_wallTilemap != null)
            _wallTilemap.SetTile(_worldGenerator.CurrentWorldData.DataToCell(dataTile.x, dataTile.y), null);

        _runtimeByTile.Remove(dataTile);
        _modificationState.MarkRemoved(dataTile);
        HideMiningBarForTile(dataTile);

        if (miner != null && runtimeData.MineableData != null)
            MiningDropResolver.ResolveDrops(runtimeData.MineableData.Drops, miner, dropSpawner, GetTileCenterWorld(dataTile));
    }

    public void NotifyMiningStopped(Vector2Int dataTile)
    {
        HideMiningBarForTile(dataTile);
    }

    private void EnsureMiningBar(Vector2Int dataTile)
    {
        if (_activeMiningBarTile.HasValue && _activeMiningBarTile.Value == dataTile && _activeMiningBar != null)
            return;

        ClearMiningBar();

        if (_miningBarPrefab == null)
            return;

        var spawnRoot = _miningBarRoot != null ? _miningBarRoot : transform;
        _activeMiningBar = Instantiate(_miningBarPrefab, spawnRoot);
        _activeMiningBar.transform.position = GetTileCenterWorld(dataTile) + _miningBarOffset;
        _activeMiningBarTile = dataTile;
        _activeMiningBar.SetIdle();
    }

    private void HideMiningBarForTile(Vector2Int dataTile)
    {
        if (!_activeMiningBarTile.HasValue || _activeMiningBarTile.Value != dataTile)
            return;

        ClearMiningBar();
    }

    private void ClearMiningBar()
    {
        _activeMiningBarTile = null;

        if (_activeMiningBar == null)
            return;

        if (Application.isPlaying)
            Destroy(_activeMiningBar.gameObject);
        else
            DestroyImmediate(_activeMiningBar.gameObject);

        _activeMiningBar = null;
    }

    private List<PlannedWallTile> BuildWallTilesForChunk(WorldRuntimeData data, Vector2Int chunkCoord)
    {
        var plannedTiles = new Dictionary<Vector2Int, PlannedWallTile>();

        int startX = chunkCoord.x * _chunkSize;
        int startY = chunkCoord.y * _chunkSize;

        if (startX < 0 || startY < 0 || startX >= data.Width || startY >= data.Height)
            return new List<PlannedWallTile>();

        int width = Mathf.Min(_chunkSize, data.Width - startX);
        int height = Mathf.Min(_chunkSize, data.Height - startY);

        if (width <= 0 || height <= 0)
            return new List<PlannedWallTile>();

        int chunkSeed = WorldSeedUtils.CombineSeed(_worldGenerator.CurrentSeed, chunkCoord.x, chunkCoord.y);
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

        var clusterTiles = new List<Vector2Int>(targetTileCount) { seedTile };
        TryPlanTile(data, seedTile, plannedTiles);

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

            var biome = data.GetTile(x, y).Biome;
            if (!_wallSettingsByBiome.TryGetValue(biome, out var wallSettings) || wallSettings == null || wallSettings.RuleTile == null || wallSettings.MineableData == null)
                continue;

            seed = new Vector2Int(x, y);
            return true;
        }

        seed = default;
        return false;
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
}
