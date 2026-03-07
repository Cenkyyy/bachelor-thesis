using UnityEngine;

public enum BiomeType
{
    None = 0,
    Grassland = 1,
    IceTundra = 2,
    Desert = 3,
    AmethystRift = 4
}

public enum TileType
{
    None = 0,
    Void = 1,
    GrasslandBase = 2,
    IceTundraBase = 3,
    DesertBase = 4,
    AmethystRift = 5
}

public struct WorldTile
{
    public BiomeType Biome;
    public TileType TileType;

    public WorldTile(BiomeType biome, TileType tileType)
    {
        Biome = biome;
        TileType = tileType;
    }
}

public class WorldData
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public WorldTile[,] Tiles { get; private set; }
    public Vector2Int SpawnTile { get; set; }

    public int OffsetX => -Width / 2;
    public int OffsetY => -Height / 2;
    
    public Vector3Int DataToCell(int x, int y)
    {
        return new Vector3Int(x + OffsetX, y + OffsetY, 0);
    }

    public Vector2Int CellToData(Vector3Int cell)
    {
        return new Vector2Int(cell.x - OffsetX, cell.y - OffsetY);
    }

    public WorldData(int width, int height)
    {
        Width = width;
        Height = height;
        Tiles = new WorldTile[width, height];

        // Default spawn tile is the center of the world
        SpawnTile = new Vector2Int(width / 2, height / 2);
    }
}
