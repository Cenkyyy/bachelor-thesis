using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class WorldObjectPlacementUtility
{
    public static void BuildBiomeIndex<TEntry>(
        IReadOnlyList<TEntry> allEntries,
        Dictionary<BiomeType, List<TEntry>> entriesByBiome,
        Func<TEntry, GameObject> prefabSelector,
        Func<TEntry, string> idSelector,
        Func<TEntry, IReadOnlyList<BiomeType>> allowedBiomesSelector)
        where TEntry : class
    {
        entriesByBiome.Clear();
        if (allEntries == null)
            return;

        for (int i = 0; i < allEntries.Count; i++)
        {
            var entry = allEntries[i];
            if (entry == null || prefabSelector(entry) == null || string.IsNullOrWhiteSpace(idSelector(entry)))
                continue;

            var allowedBiomes = allowedBiomesSelector(entry);
            if (allowedBiomes == null || allowedBiomes.Count == 0)
            {
                AddEntryToBiome(BiomeType.Grassland, entry, entriesByBiome);
                AddEntryToBiome(BiomeType.IceTundra, entry, entriesByBiome);
                AddEntryToBiome(BiomeType.Desert, entry, entriesByBiome);
                AddEntryToBiome(BiomeType.AmethystRift, entry, entriesByBiome);
                continue;
            }

            for (int biomeIndex = 0; biomeIndex < allowedBiomes.Count; biomeIndex++)
                AddEntryToBiome(allowedBiomes[biomeIndex], entry, entriesByBiome);
        }
    }

    public static bool IsBiomeAllowed(IReadOnlyList<BiomeType> allowedBiomes, BiomeType biome)
    {
        if (allowedBiomes == null || allowedBiomes.Count == 0)
            return biome != BiomeType.None;

        for (int i = 0; i < allowedBiomes.Count; i++)
        {
            if (allowedBiomes[i] == biome)
                return true;
        }

        return false;
    }

    public static TEntry SelectWeightedEntry<TEntry>(System.Random rng, List<TEntry> entries, Func<TEntry, float> weightSelector)
    {
        float total = 0f;
        for (int i = 0; i < entries.Count; i++)
            total += Mathf.Max(0f, weightSelector(entries[i]));

        if (total <= Mathf.Epsilon)
            return entries[rng.Next(0, entries.Count)];

        float pick = (float)rng.NextDouble() * total;
        float cumulative = 0f;

        for (int i = 0; i < entries.Count; i++)
        {
            cumulative += Mathf.Max(0f, weightSelector(entries[i]));
            if (pick <= cumulative)
                return entries[i];
        }

        return entries[entries.Count - 1];
    }

    public static bool HasValidSpacing<TPlacement>(
        List<TPlacement> existing,
        Vector3 candidatePosition,
        float candidateSpacing,
        Func<TPlacement, Vector3> positionSelector,
        Func<TPlacement, float> spacingSelector)
    {
        if (candidateSpacing <= 0f)
            return true;

        for (int i = 0; i < existing.Count; i++)
        {
            float requiredSpacing = Mathf.Max(candidateSpacing, spacingSelector(existing[i]));
            if (requiredSpacing <= 0f)
                continue;

            float sqrDistance = (positionSelector(existing[i]) - candidatePosition).sqrMagnitude;
            if (sqrDistance < requiredSpacing * requiredSpacing)
                return false;
        }

        return true;
    }

    public static Vector3 TileToWorldPosition(WorldRuntimeData data, Tilemap groundTilemap, int tileX, int tileY, float jitterX, float jitterY)
    {
        if (groundTilemap == null)
            return new Vector3(tileX + jitterX, tileY + jitterY, 0f);

        var cell = data.DataToCell(tileX, tileY);
        var basePosition = groundTilemap.CellToWorld(cell) + new Vector3(0.5f, 0.5f, 0f);
        return new Vector3(basePosition.x + jitterX, basePosition.y + jitterY, 0f);
    }

    private static void AddEntryToBiome<TEntry>(BiomeType biome, TEntry entry, Dictionary<BiomeType, List<TEntry>> entriesByBiome)
    {
        if (biome == BiomeType.None)
            return;

        if (!entriesByBiome.TryGetValue(biome, out var list))
        {
            list = new List<TEntry>();
            entriesByBiome.Add(biome, list);
        }

        if (!list.Contains(entry))
            list.Add(entry);
    }
}
