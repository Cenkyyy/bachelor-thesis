using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class FoliageChunkGenerator : ChunkWorldContentGeneratorBase
{
    [Header("Dependencies")]
    [SerializeField] private DecorationChunkGenerator _decorationChunkGenerator;
    [SerializeField] private FoliageListData _foliageListData;
    [SerializeField] private Transform _spawnedFoliageRoot;

    [Header("Spawn Tuning")]
    [SerializeField, Min(1)] private int _baseSpawnAttemptsPerChunk = 256;
    [SerializeField, Range(0f, 1f)] private float _tileCenterJitter = 0.45f;

    private readonly Dictionary<Vector2Int, List<GameObject>> _spawnedChunkInstances = new Dictionary<Vector2Int, List<GameObject>>();
    private readonly Dictionary<BiomeType, List<FoliageListEntry>> _entriesByBiome = new Dictionary<BiomeType, List<FoliageListEntry>>();
    private readonly GameObjectInstancePool _instancePool = new GameObjectInstancePool();

    protected override void OnEnable()
    {
        RebuildBiomeIndex();
        base.OnEnable();
    }

    protected override bool CanStartStreaming(WorldRuntimeData data)
    {
        return _decorationChunkGenerator == null || _decorationChunkGenerator.IsReadyForSceneReveal;
    }

    protected override bool IsChunkLoaded(Vector2Int chunkCoord)
    {
        return _spawnedChunkInstances.ContainsKey(chunkCoord);
    }

    protected override void GenerateChunk(WorldRuntimeData data, Vector2Int chunkCoord)
    {
        var placements = GeneratePlacementsForChunk(data, chunkCoord);
        var instances = new List<GameObject>(placements.Count);

        for (int i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            if (placement.Entry == null || placement.Entry.Prefab == null)
                continue;

            var instance = _instancePool.Acquire(placement.Entry.Prefab, placement.WorldPosition, Quaternion.identity, ResolveRootTransform(), out _);
            if (instance == null)
                continue;

            instance.name = $"{placement.Entry.FoliageId}_{placement.Tile.x}_{placement.Tile.y}";
            instance.transform.localScale = Vector3.one;
            instances.Add(instance);
        }

        _spawnedChunkInstances[chunkCoord] = instances;
    }

    protected override void UnloadChunk(Vector2Int chunk)
    {
        if (!_spawnedChunkInstances.TryGetValue(chunk, out var instances))
            return;

        for (int i = 0; i < instances.Count; i++)
        {
            if (instances[i] == null)
                continue;

            if (!_instancePool.Release(instances[i], ResolveRootTransform()))
                Destroy(instances[i]);
        }

        _spawnedChunkInstances.Remove(chunk);
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

                if (!_instancePool.Release(instances[i], ResolveRootTransform()))
                    Destroy(instances[i]);
            }
        }

        _spawnedChunkInstances.Clear();
        _instancePool.Clear();
    }

    private void RebuildBiomeIndex()
    {
        _entriesByBiome.Clear();
        if (_foliageListData == null || _foliageListData.Entries == null)
            return;

        var allEntries = _foliageListData.Entries;
        for (int i = 0; i < allEntries.Count; i++)
        {
            var entry = allEntries[i];
            if (entry == null || entry.Prefab == null || string.IsNullOrWhiteSpace(entry.FoliageId))
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

    private void AddEntryToBiome(BiomeType biome, FoliageListEntry entry)
    {
        if (biome == BiomeType.None)
            return;

        if (!_entriesByBiome.TryGetValue(biome, out var list))
        {
            list = new List<FoliageListEntry>();
            _entriesByBiome.Add(biome, list);
        }

        if (!list.Contains(entry))
            list.Add(entry);
    }

    private Transform ResolveRootTransform()
    {
        if (_spawnedFoliageRoot != null)
            return _spawnedFoliageRoot;

        _spawnedFoliageRoot = transform;
        return _spawnedFoliageRoot;
    }

    private List<FoliagePlacement> GeneratePlacementsForChunk(WorldRuntimeData data, Vector2Int chunkCoord)
    {
        int startX = chunkCoord.x * _chunkSize;
        int startY = chunkCoord.y * _chunkSize;

        if (startX < 0 || startY < 0 || startX >= data.Width || startY >= data.Height)
            return new List<FoliagePlacement>();

        int width = Mathf.Min(_chunkSize, data.Width - startX);
        int height = Mathf.Min(_chunkSize, data.Height - startY);
        int baseChunkSeed = WorldSeedUtils.CombineSeed(_worldGenerator.CurrentSeed, chunkCoord.x, chunkCoord.y);
        int chunkSeed = WorldSeedUtils.CombineSeed(baseChunkSeed, 7927, 0);
        var rng = new System.Random(chunkSeed);

        var placements = new List<FoliagePlacement>();
        int attempts = Mathf.Max(1, _baseSpawnAttemptsPerChunk);

        for (int attemptIndex = 0; attemptIndex < attempts; attemptIndex++)
        {
            int localX = rng.Next(0, width);
            int localY = rng.Next(0, height);
            int worldX = startX + localX;
            int worldY = startY + localY;

            var worldTile = data.GetTile(worldX, worldY);
            if (worldTile.TileType == TileType.Void)
                continue;

            if (!_entriesByBiome.TryGetValue(worldTile.Biome, out var biomeEntries) || biomeEntries == null || biomeEntries.Count == 0)
                continue;

            var selectedEntry = SelectWeightedEntry(rng, biomeEntries);
            if (selectedEntry == null)
                continue;

            var centerPlacement = TryBuildPlacement(data, selectedEntry, worldX, worldY, placements, rng);
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
                var clusterPlacement = TryBuildClusterPlacement(data, selectedEntry, centerPlacement.Value, placements, rng);
                if (clusterPlacement.HasValue)
                    placements.Add(clusterPlacement.Value);
            }
        }

        return placements;
    }

    private FoliagePlacement? TryBuildPlacement(WorldRuntimeData data, FoliageListEntry entry, int tileX, int tileY, List<FoliagePlacement> existing, System.Random rng)
    {
        float jitterX = ((float)rng.NextDouble() * 2f - 1f) * _tileCenterJitter;
        float jitterY = ((float)rng.NextDouble() * 2f - 1f) * _tileCenterJitter;

        var worldPosition = TileToWorldPosition(data, tileX, tileY, jitterX, jitterY);
        float minSpacing = Mathf.Max(0f, entry.MinimumSpacingTiles);

        if (!HasValidSpacing(existing, worldPosition, minSpacing))
            return null;

        return new FoliagePlacement
        {
            Entry = entry,
            Tile = new Vector2Int(tileX, tileY),
            WorldPosition = worldPosition,
            SpacingRadius = minSpacing,
        };
    }

    private FoliagePlacement? TryBuildClusterPlacement(WorldRuntimeData data, FoliageListEntry entry, FoliagePlacement center, List<FoliagePlacement> existing, System.Random rng)
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

        if (!IsBiomeAllowed(entry, tile.Biome))
            return null;

        var worldPosition = TileToWorldPosition(data, tileX, tileY, 0f, 0f);
        float minSpacing = Mathf.Max(0f, entry.MinimumSpacingTiles);
        if (!HasValidSpacing(existing, worldPosition, minSpacing))
            return null;

        return new FoliagePlacement
        {
            Entry = entry,
            Tile = new Vector2Int(tileX, tileY),
            WorldPosition = worldPosition,
            SpacingRadius = minSpacing,
        };
    }

    private bool IsBiomeAllowed(FoliageListEntry entry, BiomeType biome)
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

    private bool HasValidSpacing(List<FoliagePlacement> existing, Vector3 candidatePosition, float candidateSpacing)
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

    private FoliageListEntry SelectWeightedEntry(System.Random rng, List<FoliageListEntry> entries)
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

    private Vector3 TileToWorldPosition(WorldRuntimeData data, int tileX, int tileY, float jitterX, float jitterY)
    {
        if (_worldGenerator.GroundTilemap == null)
            return new Vector3(tileX + jitterX, tileY + jitterY, 0f);

        var cell = data.DataToCell(tileX, tileY);
        var basePosition = _worldGenerator.GroundTilemap.CellToWorld(cell) + new Vector3(0.5f, 0.5f, 0f);
        return new Vector3(basePosition.x + jitterX, basePosition.y + jitterY, 0f);
    }

    private struct FoliagePlacement
    {
        public FoliageListEntry Entry;
        public Vector2Int Tile;
        public Vector3 WorldPosition;
        public float SpacingRadius;
    }
}
