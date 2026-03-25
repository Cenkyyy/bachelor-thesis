using UnityEngine;

public interface ISpawnWorldQuery
{
    bool IsWalkable(Vector2 worldPoint, float probeRadius);
    bool TryGetBiome(Vector2 worldPoint, out BiomeAffinity biome);
}
