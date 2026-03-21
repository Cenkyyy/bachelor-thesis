using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DecorationChunkGenerator : MonoBehaviour, ISceneTransitionReadinessBlocker
{
    [Header("Dependencies")]
    [SerializeField] private WorldGeneratorBehaviour _worldGenerator;
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private DecorationsListData _decorationsListData;
    [SerializeField] private Transform _spawnedDecorationsRoot;

    [Header("Chunk Settings")]
    [SerializeField, Min(8)] private int _chunkSize = 32;
    [SerializeField, Min(0)] private int _initialGenerationRadiusInChunks = 4;
    [SerializeField, Min(0)] private int _generationRadiusInChunks = 4;
    [SerializeField, Min(0)] private int _unloadRadiusInChunks = 6;
    [SerializeField, Min(0.02f)] private float _refreshIntervalSeconds = 0.1f;
    [SerializeField, Min(1)] private int _chunksGeneratedPerFrame = 1;

    [Header("Spawn Tuning")]
    [SerializeField, Min(1)] private int _baseSpawnAttemptsPerChunk = 72;
    [SerializeField, Range(0f, 1f)] private float _tileCenterJitter = 0.35f;

    private readonly Dictionary<Vector2Int, List<GameObject>> _spawnedChunkInstances = new Dictionary<Vector2Int, List<GameObject>>();
    private readonly Dictionary<BiomeType, List<DecorationListEntry>> _entriesByBiome = new Dictionary<BiomeType, List<DecorationListEntry>>();
    private readonly DecorationModificationState _modificationState = new DecorationModificationState();

    private Coroutine _streamingCoroutine;

    public bool IsReadyForSceneReveal { get; private set; }

    private void OnEnable()
    {
        IsReadyForSceneReveal = false;

        if (_streamingCoroutine != null)
            StopCoroutine(_streamingCoroutine);

        _streamingCoroutine = StartCoroutine(StreamDecorationsCoroutine());
    }

    private void OnDisable()
    {
        if (_streamingCoroutine != null)
        {
            StopCoroutine(_streamingCoroutine);
            _streamingCoroutine = null;
        }

        ClearAllSpawnedDecorations();
        IsReadyForSceneReveal = false;
    }

    internal void MarkDecorationRemoved(string decorationInstanceId)
    {
        _modificationState.MarkRemoved(decorationInstanceId);
    }

    private IEnumerator StreamDecorationsCoroutine()
    {
        while (_worldGenerator == null || !_worldGenerator.IsReadyForSceneReveal || _worldGenerator.CurrentWorldData == null)
            yield return null;

        RebuildBiomeIndex();

        var initialData = _worldGenerator.CurrentWorldData;
        if (initialData != null)
            yield return StreamInitialChunksCoroutine(initialData);

        IsReadyForSceneReveal = true;

        while (enabled && gameObject.activeInHierarchy)
        {
            var data = _worldGenerator.CurrentWorldData;
            if (data == null)
            {
                yield return null;
                continue;
            }

            var focusTile = ResolveFocusTile(data);
            var focusChunk = GetChunkCoordFromTile(focusTile);
            var desiredChunks = BuildChunkSetInRadius(focusChunk, Mathf.Max(0, _generationRadiusInChunks));

            int chunkBudget = 0;
            foreach (var chunk in desiredChunks)
            {
                if (_spawnedChunkInstances.ContainsKey(chunk))
                    continue;

                SpawnChunk(data, chunk);
                chunkBudget++;

                if (chunkBudget >= Mathf.Max(1, _chunksGeneratedPerFrame))
                {
                    chunkBudget = 0;
                    yield return null;
                }
            }

            UnloadFarChunks(focusChunk);
            yield return new WaitForSeconds(Mathf.Max(0.02f, _refreshIntervalSeconds));
        }
    }

    private IEnumerator StreamInitialChunksCoroutine(WorldData data)
    {
        var focusTile = ResolveFocusTile(data);
        var focusChunk = GetChunkCoordFromTile(focusTile);
        var initialChunks = BuildChunkSetInRadius(focusChunk, Mathf.Max(0, _initialGenerationRadiusInChunks));

        int chunkBudget = 0;
        int perFrameBudget = Mathf.Max(1, _chunksGeneratedPerFrame);

        for (int i = 0; i < initialChunks.Count; i++)
        {
            var chunk = initialChunks[i];
            if (_spawnedChunkInstances.ContainsKey(chunk))
                continue;

            SpawnChunk(data, chunk);
            chunkBudget++;

            if (chunkBudget >= perFrameBudget)
            {
                chunkBudget = 0;
                yield return null;
            }
        }
    }
    private void RebuildBiomeIndex()
    {
        _entriesByBiome.Clear();
        if (_decorationsListData == null || _decorationsListData.Entries == null)
            return;

        var allEntries = _decorationsListData.Entries;
        for (int i = 0; i < allEntries.Count; i++)
        {
            var entry = allEntries[i];
            if (entry == null || entry.Prefab == null || string.IsNullOrWhiteSpace(entry.DecorationId))
                continue;

            var allowedBiomes = entry.AllowedBiomes;
            if (allowedBiomes == null || allowedBiomes.Count == 0)
            {
                AddEntryToBiome(BiomeType.Grassland, entry);
                AddEntryToBiome(BiomeType.IceTundra, entry);
                AddEntryToBiome(BiomeType.Desert, entry);
                AddEntryToBiome(BiomeType.AmethystRift, entry);
                continue;
            }

            for (int biomeIndex = 0; biomeIndex < allowedBiomes.Count; biomeIndex++)
                AddEntryToBiome(allowedBiomes[biomeIndex], entry);
        }
    }

    private void AddEntryToBiome(BiomeType biome, DecorationListEntry entry)
    {
        if (biome == BiomeType.None)
            return;

        if (!_entriesByBiome.TryGetValue(biome, out var list))
        {
            list = new List<DecorationListEntry>();
            _entriesByBiome.Add(biome, list);
        }

        if (!list.Contains(entry))
            list.Add(entry);
    }

    private Vector2Int ResolveFocusTile(WorldData data)
    {
        if (_playerTransform == null || _worldGenerator.GroundTilemap == null)
            return data.SpawnTile;

        var playerCell = _worldGenerator.GroundTilemap.WorldToCell(_playerTransform.position);
        var playerTile = data.CellToData(playerCell);
        playerTile.x = Mathf.Clamp(playerTile.x, 0, data.Width - 1);
        playerTile.y = Mathf.Clamp(playerTile.y, 0, data.Height - 1);
        return playerTile;
    }

    private Vector2Int GetChunkCoordFromTile(Vector2Int tile)
    {
        int safeChunkSize = Mathf.Max(1, _chunkSize);
        return new Vector2Int(tile.x / safeChunkSize, tile.y / safeChunkSize);
    }

    private List<Vector2Int> BuildChunkSetInRadius(Vector2Int centerChunk, int radius)
    {
        var chunks = new List<Vector2Int>();
        int sqrRadius = radius * radius;
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                int sqrDistance = (x * x) + (y * y);
                if (sqrDistance > sqrRadius)
                    continue;

                chunks.Add(new Vector2Int(centerChunk.x + x, centerChunk.y + y));
            }
        }

        chunks.Sort((a, b) =>
        {
            int ax = a.x - centerChunk.x;
            int ay = a.y - centerChunk.y;
            int bx = b.x - centerChunk.x;
            int by = b.y - centerChunk.y;

            int aDistance = (ax * ax) + (ay * ay);
            int bDistance = (bx * bx) + (by * by);
            return aDistance.CompareTo(bDistance);
        });

        return chunks;
    }

    private void SpawnChunk(WorldData data, Vector2Int chunkCoord)
    {
        var placements = GeneratePlacementsForChunk(data, chunkCoord);
        var instances = new List<GameObject>(placements.Count);

        for (int i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            if (placement.Entry == null || placement.Entry.Prefab == null)
                continue;

            if (_modificationState.IsRemoved(placement.InstanceId))
                continue;

            var instance = Instantiate(placement.Entry.Prefab, placement.WorldPosition, Quaternion.identity, ResolveRootTransform());
            instance.name = $"{placement.Entry.DecorationId}_{placement.Tile.x}_{placement.Tile.y}";

            var node = instance.GetComponent<MineableNode>();
            if (node != null)
            {
                var tracker = instance.AddComponent<DecorationInstanceTracker>();
                tracker.Initialize(placement.InstanceId, this, node);
            }

            instances.Add(instance);
        }

        _spawnedChunkInstances[chunkCoord] = instances;
    }

    private Transform ResolveRootTransform()
    {
        if (_spawnedDecorationsRoot != null)
            return _spawnedDecorationsRoot;

        _spawnedDecorationsRoot = transform;
        return _spawnedDecorationsRoot;
    }

    private List<DecorationPlacement> GeneratePlacementsForChunk(WorldData data, Vector2Int chunkCoord)
    {
        int startX = chunkCoord.x * _chunkSize;
        int startY = chunkCoord.y * _chunkSize;

        if (startX < 0 || startY < 0 || startX >= data.Width || startY >= data.Height)
            return new List<DecorationPlacement>();

        int width = Mathf.Min(_chunkSize, data.Width - startX);
        int height = Mathf.Min(_chunkSize, data.Height - startY);
        int chunkSeed = CombineSeed(_worldGenerator.CurrentSeed, chunkCoord.x, chunkCoord.y);
        var rng = new System.Random(chunkSeed);

        var placements = new List<DecorationPlacement>();
        int attempts = Mathf.Max(1, _baseSpawnAttemptsPerChunk);

        for (int attemptIndex = 0; attemptIndex < attempts; attemptIndex++)
        {
            int localX = rng.Next(0, width);
            int localY = rng.Next(0, height);
            int worldX = startX + localX;
            int worldY = startY + localY;

            var worldTile = data.Tiles[worldX, worldY];
            if (worldTile.TileType == TileType.Void)
                continue;

            if (!_entriesByBiome.TryGetValue(worldTile.Biome, out var biomeEntries) || biomeEntries == null || biomeEntries.Count == 0)
                continue;

            var selectedEntry = SelectWeightedEntry(rng, biomeEntries);
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
                var clusterPlacement = TryBuildClusterPlacement(data, selectedEntry, centerPlacement.Value, chunkCoord, attemptIndex, clusterIndex, placements, rng);
                if (clusterPlacement.HasValue)
                    placements.Add(clusterPlacement.Value);
            }
        }

        return placements;
    }

    private DecorationPlacement? TryBuildPlacement(WorldData data, DecorationListEntry entry, int tileX, int tileY, Vector2Int chunkCoord, int attemptIndex, List<DecorationPlacement> existing, System.Random rng)
    {
        string instanceId = BuildInstanceId(entry.DecorationId, chunkCoord, attemptIndex, 0);

        float jitterX = ((float)rng.NextDouble() * 2f - 1f) * _tileCenterJitter;
        float jitterY = ((float)rng.NextDouble() * 2f - 1f) * _tileCenterJitter;

        var worldPosition = TileToWorldPosition(data, tileX, tileY, jitterX, jitterY);
        float minSpacing = Mathf.Max(0f, entry.MinimumSpacingTiles);

        if (!HasValidSpacing(existing, worldPosition, minSpacing))
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

    private DecorationPlacement? TryBuildClusterPlacement(WorldData data, DecorationListEntry entry, DecorationPlacement center, Vector2Int chunkCoord, int attemptIndex, int clusterIndex, List<DecorationPlacement> existing, System.Random rng)
    {
        float radius = Mathf.Max(0.25f, entry.ClusterRadiusTiles);
        float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
        float distance = Mathf.Sqrt((float)rng.NextDouble()) * radius;

        var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
        int tileX = Mathf.RoundToInt(center.Tile.x + offset.x);
        int tileY = Mathf.RoundToInt(center.Tile.y + offset.y);

        if (tileX < 0 || tileX >= data.Width || tileY < 0 || tileY >= data.Height)
            return null;

        var tile = data.Tiles[tileX, tileY];
        if (tile.TileType == TileType.Void)
            return null;

        if (!IsBiomeAllowed(entry, tile.Biome))
            return null;

        string instanceId = BuildInstanceId(entry.DecorationId, chunkCoord, attemptIndex, clusterIndex);

        var worldPosition = TileToWorldPosition(data, tileX, tileY, 0f, 0f);
        float minSpacing = Mathf.Max(0f, entry.MinimumSpacingTiles);
        if (!HasValidSpacing(existing, worldPosition, minSpacing))
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

    private bool IsBiomeAllowed(DecorationListEntry entry, BiomeType biome)
    {
        var allowed = entry.AllowedBiomes;
        if (allowed == null || allowed.Count == 0)
            return biome != BiomeType.None;

        for (int i = 0; i < allowed.Count; i++)
        {
            if (allowed[i] == biome)
                return true;
        }

        return false;
    }

    private bool HasValidSpacing(List<DecorationPlacement> existing, Vector3 candidatePosition, float candidateSpacing)
    {
        if (candidateSpacing <= 0f)
            return true;

        for (int i = 0; i < existing.Count; i++)
        {
            float requiredSpacing = Mathf.Max(candidateSpacing, existing[i].SpacingRadius);
            if (requiredSpacing <= 0f)
                continue;

            float sqrDistance = (existing[i].WorldPosition - candidatePosition).sqrMagnitude;
            if (sqrDistance < requiredSpacing * requiredSpacing)
                return false;
        }

        return true;
    }

    private DecorationListEntry SelectWeightedEntry(System.Random rng, List<DecorationListEntry> entries)
    {
        float total = 0f;
        for (int i = 0; i < entries.Count; i++)
            total += Mathf.Max(0f, entries[i].SpawnWeight);

        if (total <= Mathf.Epsilon)
            return entries[rng.Next(0, entries.Count)];

        float pick = (float)rng.NextDouble() * total;
        float cumulative = 0f;

        for (int i = 0; i < entries.Count; i++)
        {
            cumulative += Mathf.Max(0f, entries[i].SpawnWeight);
            if (pick <= cumulative)
                return entries[i];
        }

        return entries[entries.Count - 1];
    }

    private Vector3 TileToWorldPosition(WorldData data, int tileX, int tileY, float jitterX, float jitterY)
    {
        if (_worldGenerator.GroundTilemap == null)
            return new Vector3(tileX + jitterX, tileY + jitterY, 0f);

        var cell = data.DataToCell(tileX, tileY);
        var basePosition = _worldGenerator.GroundTilemap.CellToWorld(cell) + new Vector3(0.5f, 0.5f, 0f);
        return new Vector3(basePosition.x + jitterX, basePosition.y + jitterY, 0f);
    }

    private void UnloadFarChunks(Vector2Int focusChunk)
    {
        int unloadRadius = Mathf.Max(_generationRadiusInChunks, _unloadRadiusInChunks);
        int sqrRadius = unloadRadius * unloadRadius;

        var chunksToUnload = new List<Vector2Int>();
        foreach (var pair in _spawnedChunkInstances)
        {
            int dx = pair.Key.x - focusChunk.x;
            int dy = pair.Key.y - focusChunk.y;
            if ((dx * dx) + (dy * dy) > sqrRadius)
                chunksToUnload.Add(pair.Key);
        }

        for (int i = 0; i < chunksToUnload.Count; i++)
            UnloadChunk(chunksToUnload[i]);
    }

    private void UnloadChunk(Vector2Int chunk)
    {
        if (!_spawnedChunkInstances.TryGetValue(chunk, out var instances))
            return;

        for (int i = 0; i < instances.Count; i++)
        {
            if (instances[i] != null)
                Destroy(instances[i]);
        }

        _spawnedChunkInstances.Remove(chunk);
    }

    private void ClearAllSpawnedDecorations()
    {
        foreach (var pair in _spawnedChunkInstances)
        {
            var instances = pair.Value;
            for (int i = 0; i < instances.Count; i++)
            {
                if (instances[i] != null)
                    Destroy(instances[i]);
            }
        }

        _spawnedChunkInstances.Clear();
    }

    private static int CombineSeed(int seed, int x, int y)
    {
        unchecked
        {
            int hash = seed;
            hash = (hash * 397) ^ x;
            hash = (hash * 397) ^ y;
            return hash;
        }
    }

    private static string BuildInstanceId(string decorationId, Vector2Int chunkCoord, int attemptIndex, int clusterIndex)
    {
        return $"{decorationId}:{chunkCoord.x}:{chunkCoord.y}:{attemptIndex}:{clusterIndex}";
    }

    private struct DecorationPlacement
    {
        public DecorationListEntry Entry;
        public string InstanceId;
        public Vector2Int Tile;
        public Vector3 WorldPosition;
        public float SpacingRadius;
    }
}
