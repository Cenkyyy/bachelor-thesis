using UnityEngine;

public sealed class WorldRuntimeData
{
    private readonly WorldTile[,] _tiles;
    private readonly bool[,] _generatedTiles;

    public int Width { get; }
    public int Height { get; }
    public Vector2Int DefaultSpawnTile { get; private set; }
    public Vector2Int SpawnTile { get; set; }

    public int OffsetX => -Width / 2;
    public int OffsetY => -Height / 2;

    public WorldRuntimeData(int width, int height)
    {
        Width = width;
        Height = height;
        _tiles = new WorldTile[width, height];
        _generatedTiles = new bool[width, height];
        var initialSpawn = new Vector2Int(width / 2, height / 2);
        DefaultSpawnTile = initialSpawn;
        SpawnTile = initialSpawn;
    }

    public bool IsInside(int x, int y)
    {
        return x >= 0 && y >= 0 && x < Width && y < Height;
    }

    public WorldTile GetTile(int x, int y)
    {
        return _tiles[x, y];
    }

    public void SetTile(int x, int y, WorldTile tile)
    {
        _tiles[x, y] = tile;
        _generatedTiles[x, y] = true;
    }

    public void MarkTileGenerated(int x, int y)
    {
        _generatedTiles[x, y] = true;
    }

    public bool IsTileGenerated(int x, int y)
    {
        return IsInside(x, y) && _generatedTiles[x, y];
    }

    public bool IsRegionGenerated(int startX, int startY, int width, int height)
    {
        if (width <= 0 || height <= 0)
            return true;

        int maxX = Mathf.Min(Width, startX + width);
        int maxY = Mathf.Min(Height, startY + height);
        int minX = Mathf.Max(0, startX);
        int minY = Mathf.Max(0, startY);

        for (int y = minY; y < maxY; y++)
        {
            for (int x = minX; x < maxX; x++)
            {
                if (!_generatedTiles[x, y])
                    return false;
            }
        }

        return true;
    }

    public Vector3Int DataToCell(int x, int y)
    {
        return new Vector3Int(x + OffsetX, y + OffsetY, 0);
    }

    public Vector2Int CellToData(Vector3Int cell)
    {
        return new Vector2Int(cell.x - OffsetX, cell.y - OffsetY);
    }
}
