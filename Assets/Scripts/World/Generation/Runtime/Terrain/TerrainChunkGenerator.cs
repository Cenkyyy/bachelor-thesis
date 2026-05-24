using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public sealed class TerrainChunkGenerator : WorldChunkGeneratorBase
{
    [Header("Terrain Streaming")]
    [SerializeField] private bool _enableChunkUnloading;

    private readonly HashSet<Vector2Int> _renderedChunks = new HashSet<Vector2Int>();

    protected override bool EnableChunkUnloading => _enableChunkUnloading;

    protected override bool IsChunkLoaded(Vector2Int chunkCoord)
    {
        return _renderedChunks.Contains(chunkCoord);
    }

    protected override void GenerateChunk(WorldRuntimeData data, Vector2Int chunkCoord)
    {
        RunImmediate(GenerateChunkTiles(data, chunkCoord, 0, null));
    }

    protected override void UnloadChunk(Vector2Int chunkCoord)
    {
        RunImmediate(UnloadChunkTiles(chunkCoord, 0, null));
    }

    protected override IEnumerator GenerateChunkCoroutine(WorldRuntimeData data, Vector2Int chunkCoord)
    {
        yield return GenerateChunkTiles(data, chunkCoord, loadOperationsPerFrame, null);
    }

    protected override IEnumerator UnloadChunkCoroutine(Vector2Int chunkCoord)
    {
        yield return UnloadChunkTiles(chunkCoord, unloadOperationsPerFrame, null);
    }

    protected override IEnumerable<Vector2Int> GetLoadedChunks()
    {
        foreach (var chunk in _renderedChunks)
            yield return chunk;
    }

    protected override void ClearGeneratedChunks()
    {
        _renderedChunks.Clear();
        ClearTilemaps();
    }

    private void ClearTilemaps()
    {
        if (worldGenerator == null)
            return;

        if (worldGenerator.GroundTilemap != null)
            worldGenerator.GroundTilemap.ClearAllTiles();

        if (worldGenerator.BorderVisualTilemap != null)
            worldGenerator.BorderVisualTilemap.ClearAllTiles();

        if (worldGenerator.BorderCollisionTilemap != null)
            worldGenerator.BorderCollisionTilemap.ClearAllTiles();
    }

    private bool IsInnerBorderTile(WorldRuntimeData data, int x, int y)
    {
        if (!data.IsInside(x, y))
            return false;

        if (data.GetTile(x, y).TileType != WorldTileType.BorderBase)
            return false;

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0)
                    continue;

                if (HasWalkableNeighbour(data, x + i, y + j))
                    return true;
            }
        }

        return false;
    }

    private bool HasWalkableNeighbour(WorldRuntimeData data, int x, int y)
    {
        if (!data.IsInside(x, y))
            return false;

        return data.GetTile(x, y).TileType != WorldTileType.BorderBase;
    }

    private IEnumerator GenerateChunkTiles(WorldRuntimeData data, Vector2Int chunkCoord, int yieldEveryOperations, YieldInstruction yieldInstruction)
    {
        if (_renderedChunks.Contains(chunkCoord) || worldGenerator == null)
            yield break;

        int startX = chunkCoord.x * chunkSize;
        int startY = chunkCoord.y * chunkSize;

        if (startX < 0 || startY < 0 || startX >= data.Width || startY >= data.Height)
            yield break;

        int width = Mathf.Min(chunkSize, data.Width - startX);
        int height = Mathf.Min(chunkSize, data.Height - startY);

        if (width <= 0 || height <= 0)
            yield break;

        // build ground and border tile arrays from the world data
        int tileCount = width * height;
        var groundTiles = new TileBase[tileCount];
        var borderTiles = new TileBase[tileCount];
        var borderCollisionTiles = new TileBase[tileCount];

        var borderTileAsset = worldGenerator.GetBorderTileAsset();
        var borderCollisionTileAsset = worldGenerator.GetBorderCollisionTileAsset();

        int operations = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int dataX = startX + x;
                int dataY = startY + y;
                int index = x + (y * width);
                var tile = data.GetTile(dataX, dataY);

                if (tile.TileType == WorldTileType.BorderBase)
                {
                    borderTiles[index] = borderTileAsset;

                    if (IsInnerBorderTile(data, dataX, dataY))
                        borderCollisionTiles[index] = borderCollisionTileAsset;
                }
                else
                {
                    groundTiles[index] = worldGenerator.GetTileAsset(tile.TileType);
                }

                operations++;
                if (yieldEveryOperations > 0 && operations >= yieldEveryOperations)
                {
                    operations = 0;
                    yield return yieldInstruction ?? null;
                }
            }
        }

        // write the tile arrays onto the tilemaps
        var chunkOriginCell = data.DataToCell(startX, startY);
        var chunkBounds = new BoundsInt(chunkOriginCell.x, chunkOriginCell.y, 0, width, height, 1);

        if (worldGenerator.GroundTilemap != null)
            worldGenerator.GroundTilemap.SetTilesBlock(chunkBounds, groundTiles);


        if (worldGenerator.BorderVisualTilemap != null)
            worldGenerator.BorderVisualTilemap.SetTilesBlock(chunkBounds, borderTiles);


        if (worldGenerator.BorderCollisionTilemap != null)
            worldGenerator.BorderCollisionTilemap.SetTilesBlock(chunkBounds, borderCollisionTiles);

        _renderedChunks.Add(chunkCoord);
    }


    private IEnumerator UnloadChunkTiles(Vector2Int chunkCoord, int yieldEveryOperations, YieldInstruction yieldInstruction)
    {
        if (!_renderedChunks.Contains(chunkCoord) || !_enableChunkUnloading || worldGenerator == null || worldGenerator.CurrentWorldData == null)
            yield break;

        // get the final chunk's width and height
        var data = worldGenerator.CurrentWorldData;
        int startX = chunkCoord.x * chunkSize;
        int startY = chunkCoord.y * chunkSize;

        if (startX < 0 || startY < 0 || startX >= data.Width || startY >= data.Height)
            yield break;

        int width = Mathf.Min(chunkSize, data.Width - startX);
        int height = Mathf.Min(chunkSize, data.Height - startY);

        if (width <= 0 || height <= 0)
            yield break;

        int tileCount = width * height;
        var emptyTiles = new TileBase[tileCount];

        // write empty tile arrays to clear the chunk and remove it from rendered chunks
        var chunkOriginCell = data.DataToCell(startX, startY);
        var chunkBounds = new BoundsInt(chunkOriginCell.x, chunkOriginCell.y, 0, width, height, 1);

        if (worldGenerator.GroundTilemap != null)
            worldGenerator.GroundTilemap.SetTilesBlock(chunkBounds, emptyTiles);

        if (worldGenerator.BorderVisualTilemap != null)
            worldGenerator.BorderVisualTilemap.SetTilesBlock(chunkBounds, emptyTiles);

        if (worldGenerator.BorderCollisionTilemap != null)
            worldGenerator.BorderCollisionTilemap.SetTilesBlock(chunkBounds, emptyTiles);

        _renderedChunks.Remove(chunkCoord);
    }
}
