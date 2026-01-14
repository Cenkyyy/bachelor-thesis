using UnityEngine;

public enum BiomeType
{
    None = 0,
    Winter = 1,
    Grassland = 2
    // TODO: Add more biomes later
}

public enum TileType
{
    Void = 0,
    Snow = 1,
    Grass = 2
    // TODO: Add more tile types later
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
