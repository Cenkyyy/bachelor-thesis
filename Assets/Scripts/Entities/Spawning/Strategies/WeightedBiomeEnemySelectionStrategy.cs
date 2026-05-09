using System.Collections.Generic;
using UnityEngine;

public sealed class WeightedBiomeEnemySelectionStrategy : IEntitySelectionStrategy<EnemyData>
{
    private readonly IReadOnlyList<EnemyData> _enemyDataEntries;

    public WeightedBiomeEnemySelectionStrategy(IReadOnlyList<EnemyData> enemyDataEntries)
    {
        _enemyDataEntries = enemyDataEntries;
    }

    public bool TrySelect(ItemBiomeAffinity biome, out EnemyData enemyData)
    {
        enemyData = null;

        if (_enemyDataEntries == null || _enemyDataEntries.Count == 0)
        {
            return false;
        }

        var totalWeight = 0f;
        for (var i = 0; i < _enemyDataEntries.Count; i++)
        {
            var entry = _enemyDataEntries[i];
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

        var pick = Random.Range(0f, totalWeight);
        var running = 0f;

        for (var i = 0; i < _enemyDataEntries.Count; i++)
        {
            var entry = _enemyDataEntries[i];
            if (!CanSpawnInBiome(entry, biome))
            {
                continue;
            }

            running += entry.SpawnWeight;
            if (pick <= running)
            {
                enemyData = entry;
                return true;
            }
        }

        return false;
    }

    private static bool CanSpawnInBiome(EnemyData entry, ItemBiomeAffinity biome)
    {
        return entry != null && entry.Prefab != null && entry.SpawnWeight > 0f && entry.HomeBiome == biome;
    }
}
