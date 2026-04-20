using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DecorationChunkGenerator : ChunkWorldContentGeneratorBase
{
    [Header("Dependencies")]
    [SerializeField] private TerrainChunkGenerator _terrainChunkGenerator;
    [SerializeField] private WallChunkGenerator _wallChunkGenerator;
    [SerializeField] private DecorationsListData _decorationsListData;
    [SerializeField] private Transform _spawnedDecorationsRoot;

    [Header("Spawn Tuning")]
    [SerializeField, Min(1)] private int _baseSpawnAttemptsPerChunk = 72;
    [SerializeField, Min(1)] private int _maxPlacementsPerChunk = 56;
    [SerializeField, Range(0f, 1f)] private float _tileCenterJitter = 0.35f;
    [SerializeField, Min(1)] private int _spawnOperationsPerFrame = 16;
    [SerializeField, Min(1)] private int _unloadOperationsPerFrame = 48;

    private readonly Dictionary<Vector2Int, List<GameObject>> _spawnedChunkInstances = new();
    private readonly Dictionary<BiomeType, List<DecorationEntryData>> _entriesByBiome = new();
    private readonly DecorationModificationState _modificationState = new();
    private readonly GameObjectInstancePool _instancePool = new();

    protected override void OnEnable()
    {
        if (_spawnedDecorationsRoot == null)
            _spawnedDecorationsRoot = transform;

        WorldObjectPlacementUtility.BuildBiomeIndex(_decorationsListData.Entries, _entriesByBiome, entry => entry.Prefab, entry => entry.DecorationId, entry => entry.AllowedBiomes);
        base.OnEnable();
    }

    public void MarkDecorationRemoved(string decorationInstanceId)
    {
        _modificationState.MarkRemoved(decorationInstanceId);
    }

    protected override bool CanStartStreaming(WorldRuntimeData data)
    {
        return _terrainChunkGenerator == null || _terrainChunkGenerator.IsReadyForSceneReveal;
    }

    protected override bool IsChunkLoaded(Vector2Int chunkCoord)
    {
        return _spawnedChunkInstances.ContainsKey(chunkCoord);
    }

    protected override void GenerateChunk(WorldRuntimeData data, Vector2Int chunkCoord)
    {
        RunImmediate(SpawnChunkInstances(data, chunkCoord, yieldEveryOperations: 0, yieldInstruction: null));
    }

    protected override void UnloadChunk(Vector2Int chunk)
    {
        RunImmediate(UnloadChunkInstances(chunk, 0, null));
    }

    protected override IEnumerable<Vector2Int> GetLoadedChunks()
    {
        foreach (var pair in _spawnedChunkInstances)
            yield return pair.Key;
    }

    protected override void ClearGeneratedChunks()
    {
        foreach (var pair in _spawnedChunkInstances)
        {
            var instances = pair.Value;
            for (int i = 0; i < instances.Count; i++)
            {
                if (instances[i] == null)
                    continue;

                if (!_instancePool.Release(instances[i], _spawnedDecorationsRoot))
                    Destroy(instances[i]);
            }
        }

        _spawnedChunkInstances.Clear();
        _instancePool.Clear();
    }

    private List<DecorationPlacement> GeneratePlacementsForChunk(WorldRuntimeData data, Vector2Int chunkCoord)
    {
        int startX = chunkCoord.x * _chunkSize;
        int startY = chunkCoord.y * _chunkSize;

        if (startX < 0 || startY < 0 || startX >= data.Width || startY >= data.Height)
            return new List<DecorationPlacement>();

        int width = Mathf.Min(_chunkSize, data.Width - startX);
        int height = Mathf.Min(_chunkSize, data.Height - startY);
        int chunkSeed = WorldSeedUtils.CombineSeed(_worldGenerator.CurrentSeed, chunkCoord.x, chunkCoord.y);
        var rng = new System.Random(chunkSeed);

        var placements = new List<DecorationPlacement>();
        int attempts = Mathf.Max(1, _baseSpawnAttemptsPerChunk);

        for (int attemptIndex = 0; attemptIndex < attempts; attemptIndex++)
        {
            if (placements.Count >= _maxPlacementsPerChunk)
                break;

            int localX = rng.Next(0, width);
            int localY = rng.Next(0, height);
            int worldX = startX + localX;
            int worldY = startY + localY;

            var worldTile = data.GetTile(worldX, worldY);
            if (worldTile.TileType == TileType.Void)
                continue;

            if (IsBlockedByWall(new Vector2Int(worldX, worldY)))
                continue;

            if (!_entriesByBiome.TryGetValue(worldTile.Biome, out var biomeEntries) || biomeEntries == null || biomeEntries.Count == 0)
                continue;

            var selectedEntry = WorldObjectPlacementUtility.SelectWeightedEntry(rng, biomeEntries, entry => entry.SpawnWeight);
            if (selectedEntry == null)
                continue;

            var centerPlacement = TryBuildPlacement(data, selectedEntry, worldX, worldY, chunkCoord, attemptIndex, placements, rng);
            if (!centerPlacement.HasValue)
                continue;

            placements.Add(centerPlacement.Value);

            if (!selectedEntry.AllowCluster)
                continue;

            int minCluster = Mathf.Max(1, selectedEntry.MinClusterCount);
            int maxCluster = Mathf.Max(minCluster, selectedEntry.MaxClusterCount);
            int clusterCount = rng.Next(minCluster, maxCluster + 1);

            for (int clusterIndex = 1; clusterIndex < clusterCount; clusterIndex++)
            {
                if (placements.Count >= _maxPlacementsPerChunk)
                    break;

                var clusterPlacement = TryBuildClusterPlacement(data, selectedEntry, centerPlacement.Value, chunkCoord, attemptIndex, clusterIndex, placements, rng);
                if (clusterPlacement.HasValue)
                    placements.Add(clusterPlacement.Value);
            }
        }

        return placements;
    }

    private DecorationPlacement? TryBuildPlacement(WorldRuntimeData data, DecorationEntryData entry, int tileX, int tileY, Vector2Int chunkCoord, int attemptIndex, List<DecorationPlacement> existing, System.Random rng)
    {
        string instanceId = BuildInstanceId(entry.DecorationId, chunkCoord, attemptIndex, 0);

        float jitterX = ((float)rng.NextDouble() * 2f - 1f) * _tileCenterJitter;
        float jitterY = ((float)rng.NextDouble() * 2f - 1f) * _tileCenterJitter;

        var worldPosition = WorldObjectPlacementUtility.TileToWorldPosition(data, _worldGenerator.GroundTilemap, tileX, tileY, jitterX, jitterY);
        float minSpacing = Mathf.Max(0f, entry.MinimumSpacingTiles);

        if (!WorldObjectPlacementUtility.HasValidSpacing(existing, worldPosition, minSpacing, placement => placement.WorldPosition, placement => placement.SpacingRadius))
            return null;

        return new DecorationPlacement
        {
            Entry = entry,
            InstanceId = instanceId,
            Tile = new Vector2Int(tileX, tileY),
            WorldPosition = worldPosition,
            SpacingRadius = minSpacing
        };
    }

    private DecorationPlacement? TryBuildClusterPlacement(WorldRuntimeData data, DecorationEntryData entry, DecorationPlacement center, Vector2Int chunkCoord, int attemptIndex, int clusterIndex, List<DecorationPlacement> existing, System.Random rng)
    {
        float radius = Mathf.Max(0.25f, entry.ClusterRadiusTiles);
        float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
        float distance = Mathf.Sqrt((float)rng.NextDouble()) * radius;

        var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
        int tileX = Mathf.RoundToInt(center.Tile.x + offset.x);
        int tileY = Mathf.RoundToInt(center.Tile.y + offset.y);

        if (tileX < 0 || tileX >= data.Width || tileY < 0 || tileY >= data.Height)
            return null;

        var tile = data.GetTile(tileX, tileY);
        if (tile.TileType == TileType.Void)
            return null;

        if (IsBlockedByWall(new Vector2Int(tileX, tileY)))
            return null;

        if (!WorldObjectPlacementUtility.IsBiomeAllowed(entry.AllowedBiomes, tile.Biome))
            return null;

        string instanceId = BuildInstanceId(entry.DecorationId, chunkCoord, attemptIndex, clusterIndex);

        var worldPosition = WorldObjectPlacementUtility.TileToWorldPosition(data, _worldGenerator.GroundTilemap, tileX, tileY, 0f, 0f);
        float minSpacing = Mathf.Max(0f, entry.MinimumSpacingTiles);
        if (!WorldObjectPlacementUtility.HasValidSpacing(existing, worldPosition, minSpacing, placement => placement.WorldPosition, placement => placement.SpacingRadius))
            return null;

        return new DecorationPlacement
        {
            Entry = entry,
            InstanceId = instanceId,
            Tile = new Vector2Int(tileX, tileY),
            WorldPosition = worldPosition,
            SpacingRadius = minSpacing
        };
    }

    private bool IsBlockedByWall(Vector2Int dataTile)
    {
        return _wallChunkGenerator != null && _wallChunkGenerator.HasWallAtDataTile(dataTile);
    }

    private static string BuildInstanceId(string decorationId, Vector2Int chunkCoord, int attemptIndex, int clusterIndex)
    {
        return $"{decorationId}:{chunkCoord.x}:{chunkCoord.y}:{attemptIndex}:{clusterIndex}";
    }

    private struct DecorationPlacement
    {
        public DecorationEntryData Entry;
        public string InstanceId;
        public Vector2Int Tile;
        public Vector3 WorldPosition;
        public float SpacingRadius;
    }

    protected override IEnumerator GenerateChunkCoroutine(WorldRuntimeData data, Vector2Int chunkCoord)
    {
        yield return SpawnChunkInstances(data, chunkCoord, Mathf.Max(1, _spawnOperationsPerFrame), null);
    }

    protected override IEnumerator UnloadChunkCoroutine(Vector2Int chunkCoord)
    {
        yield return UnloadChunkInstances(chunkCoord, Mathf.Max(1, _unloadOperationsPerFrame), null);
    }

    private IEnumerator SpawnChunkInstances(WorldRuntimeData data, Vector2Int chunkCoord, int yieldEveryOperations, YieldInstruction yieldInstruction)
    {
        var placements = GeneratePlacementsForChunk(data, chunkCoord);
        var instances = new List<GameObject>(placements.Count);
        int operationCount = 0;

        for (int i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            if (placement.Entry == null || placement.Entry.Prefab == null)
                continue;

            if (_modificationState.IsRemoved(placement.InstanceId))
                continue;

            var instance = _instancePool.Acquire(placement.Entry.Prefab, placement.WorldPosition, Quaternion.identity, _spawnedDecorationsRoot, out _);
            if (instance == null)
                continue;

            instance.name = $"{placement.Entry.DecorationId}_{placement.Tile.x}_{placement.Tile.y}";

            var node = instance.GetComponent<MineableNode>();
            if (node != null)
            {
                node.ResetRuntimeState();
                var tracker = instance.GetComponent<DecorationInstanceTracker>();
                if (tracker == null)
                    tracker = instance.AddComponent<DecorationInstanceTracker>();
                tracker.Initialize(placement.InstanceId, this, node);
            }

            instances.Add(instance);
            operationCount++;

            if (yieldEveryOperations > 0 && operationCount >= yieldEveryOperations)
            {
                operationCount = 0;
                yield return yieldInstruction;
            }
        }

        _spawnedChunkInstances[chunkCoord] = instances;
    }

    private IEnumerator UnloadChunkInstances(Vector2Int chunkCoord, int yieldEveryOperations, YieldInstruction yieldInstruction)
    {
        if (!_spawnedChunkInstances.TryGetValue(chunkCoord, out var instances))
            yield break;

        int operationCount = 0;
        for (int i = 0; i < instances.Count; i++)
        {
            if (instances[i] == null)
                continue;

            if (!_instancePool.Release(instances[i], _spawnedDecorationsRoot))
                Destroy(instances[i]);

            operationCount++;
            if (yieldEveryOperations > 0 && operationCount >= yieldEveryOperations)
            {
                operationCount = 0;
                yield return yieldInstruction;
            }
        }

        _spawnedChunkInstances.Remove(chunkCoord);
    }

    private static void RunImmediate(IEnumerator routine)
    {
        while (routine.MoveNext()) { }
    }
}
