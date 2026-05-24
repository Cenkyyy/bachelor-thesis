using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class TilemapSpawnWorldQuery : ISpawnWorldQuery
{
    private readonly Tilemap _groundTilemap;
    private readonly WorldRuntimeData _worldData;
    private readonly LayerMask _obstacleMask;

    public TilemapSpawnWorldQuery(Tilemap groundTilemap, WorldRuntimeData worldData, LayerMask obstacleMask)
    {
        _groundTilemap = groundTilemap;
        _worldData = worldData;
        _obstacleMask = obstacleMask;
    }

    public bool IsWalkable(Vector2 worldPoint, float probeRadius)
    {
        return Physics2D.OverlapCircle(worldPoint, probeRadius, _obstacleMask) == null;
    }

    public bool TryGetBiome(Vector2 worldPoint, out ItemBiomeAffinity biome)
    {
        biome = ItemBiomeAffinity.None;
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

        var worldTile = _worldData.GetTile(data.x, data.y);
        biome = MapBiome(worldTile.Biome);
        return biome != ItemBiomeAffinity.None;
    }

    private static ItemBiomeAffinity MapBiome(WorldBiomeType biomeType)
    {
        switch (biomeType)
        {
            case WorldBiomeType.Grassland:
                return ItemBiomeAffinity.Grassland;
            case WorldBiomeType.IceTundra:
                return ItemBiomeAffinity.IceTundra;
            case WorldBiomeType.Desert:
                return ItemBiomeAffinity.Desert;
            case WorldBiomeType.AmethystRift:
                return ItemBiomeAffinity.AmethystRift;
            case WorldBiomeType.None:
            default:
                return ItemBiomeAffinity.None;
        }
    }
}
