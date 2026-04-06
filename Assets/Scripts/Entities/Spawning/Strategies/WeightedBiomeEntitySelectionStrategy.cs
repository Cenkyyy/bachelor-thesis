using System.Collections.Generic;
using UnityEngine;

public sealed class WeightedBiomeEntitySelectionStrategy<TData> : IEntitySelectionStrategy<TData>
    where TData : EntityData
{
    private readonly IReadOnlyList<TData> _entityDataEntries;

    public WeightedBiomeEntitySelectionStrategy(IReadOnlyList<TData> entityDataEntries)
    {
        _entityDataEntries = entityDataEntries;
    }

    public bool TrySelect(BiomeAffinity biome, out TData entityData)
    {
        entityData = null;

        if (_entityDataEntries == null || _entityDataEntries.Count == 0)
        {
            return false;
        }

        // Calculate the total weight of all the valid entries (spawnable enemies) in the given biome
        var totalWeight = 0f;
        for (var i = 0; i < _entityDataEntries.Count; i++)
        {
            var entry = _entityDataEntries[i];
            if (!CanSpawnInBiome(entry, biome))
            {
                continue;
            }

            totalWeight += entry.SpawnWeight;
        }

        if (totalWeight <= 0f)
        {
            return false;
        }

        // Then pick a random value between 0 and the total weight and choose the entry (spawnable entity) that corresponds to that value
        var pick = Random.Range(0f, totalWeight);
        var running = 0f;

        for (var i = 0; i < _entityDataEntries.Count; i++)
        {
            var entry = _entityDataEntries[i];
            if (!CanSpawnInBiome(entry, biome))
            {
                continue;
            }

            running += entry.SpawnWeight;
            if (pick <= running)
            {
                entityData = entry;
                return true;
            }
        }

        return false;
    }

    private static bool CanSpawnInBiome(TData entry, BiomeAffinity biome)
    {
        return entry != null && entry.Prefab != null && entry.SpawnWeight > 0f && entry.HomeBiome == biome;
    }
}
