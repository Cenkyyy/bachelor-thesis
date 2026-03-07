using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class TilemapSpawnWorldQuery : ISpawnWorldQuery
{
    private readonly Tilemap _groundTilemap;
    private readonly WorldData _worldData;
    private readonly LayerMask _obstacleMask;

    public TilemapSpawnWorldQuery(Tilemap groundTilemap, WorldData worldData, LayerMask obstacleMask)
    {
        _groundTilemap = groundTilemap;
        _worldData = worldData;
        _obstacleMask = obstacleMask;
    }

    public bool IsWalkable(Vector2 worldPoint, float probeRadius)
    {
        return Physics2D.OverlapCircle(worldPoint, probeRadius, _obstacleMask) == null;
    }

    public bool TryGetBiome(Vector2 worldPoint, out BiomeAffinity biome)
    {
        biome = BiomeAffinity.None;
        if (_groundTilemap == null || _worldData == null)
        {
            return false;
        }

        var cell = _groundTilemap.WorldToCell(worldPoint);
        var data = _worldData.CellToData(cell);

        if (data.x < 0 || data.y < 0 || data.x >= _worldData.Width || data.y >= _worldData.Height)
        {
            return false;
        }

        var worldTile = _worldData.Tiles[data.x, data.y];
        biome = MapBiome(worldTile.Biome);
        return biome != BiomeAffinity.None;
    }

    private static BiomeAffinity MapBiome(BiomeType biomeType)
    {
        switch (biomeType)
        {
            case BiomeType.Grassland:
                return BiomeAffinity.Grassland;
            case BiomeType.IceTundra:
                return BiomeAffinity.IceTundra;
            case BiomeType.Desert:
                return BiomeAffinity.Desert;
            case BiomeType.AmethystRift:
                return BiomeAffinity.AmethystRift;
            case BiomeType.None:
            default:
                return BiomeAffinity.None;
        }
    }
}
