using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class WorldRuntimeState
{
    public int Seed { get; private set; }
    public WorldRuntimeData Data { get; private set; }
    public Tilemap GroundTilemap { get; private set; }

    public bool IsInitialized => Data != null && GroundTilemap != null;

    public void Update(int seed, WorldRuntimeData data, Tilemap groundTilemap)
    {
        Seed = seed;
        Data = data;
        GroundTilemap = groundTilemap;
    }

    public void Clear()
    {
        Seed = default;
        Data = null;
        GroundTilemap = null;
    }

    public Vector2Int ResolveTileFromWorld(Vector3 worldPosition)
    {
        if (!IsInitialized)
            return Vector2Int.zero;

        var cell = GroundTilemap.WorldToCell(worldPosition);
        var dataTile = Data.CellToData(cell);

        dataTile.x = Mathf.Clamp(dataTile.x, 0, Data.Width - 1);
        dataTile.y = Mathf.Clamp(dataTile.y, 0, Data.Height - 1);
        return dataTile;
    }
}
