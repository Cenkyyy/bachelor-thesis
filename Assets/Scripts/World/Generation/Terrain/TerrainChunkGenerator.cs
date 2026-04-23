using System.Buffers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public sealed class TerrainChunkGenerator : ChunkWorldContentGeneratorBase
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
        if (_renderedChunks.Contains(chunkCoord))
            return;

        if (_worldGenerator == null)
            return;

        // get the final chunk's width and height
        int startX = chunkCoord.x * _chunkSize;
        int startY = chunkCoord.y * _chunkSize;

        if (startX < 0 || startY < 0 || startX >= data.Width || startY >= data.Height)
            return;

        int width = Mathf.Min(_chunkSize, data.Width - startX);
        int height = Mathf.Min(_chunkSize, data.Height - startY);

        if (width <= 0 || height <= 0)
            return;

        // build ground and border tile arrays from the world data
        int tileCount = width * height;
        var groundTiles = ArrayPool<TileBase>.Shared.Rent(tileCount);
        var borderTiles = ArrayPool<TileBase>.Shared.Rent(tileCount);
        var borderCollisionTiles = ArrayPool<TileBase>.Shared.Rent(tileCount);
        System.Array.Clear(groundTiles, 0, tileCount);
        System.Array.Clear(borderTiles, 0, tileCount);
        System.Array.Clear(borderCollisionTiles, 0, tileCount);
        var borderTileAsset = _worldGenerator.GetBorderTileAsset();
        var borderCollisionTileAsset = _worldGenerator.GetBorderCollisionTileAsset();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int dataX = startX + x;
                int dataY = startY + y;
                int index = x + (y * width);
                var tile = data.GetTile(dataX, dataY);

                if (tile.TileType == TileType.Void)
                {
                    borderTiles[index] = borderTileAsset;

                    if (IsInnerBorderTile(data, dataX, dataY))
                    {
                        borderCollisionTiles[index] = borderCollisionTileAsset;
                    }

                    continue;
                }

                groundTiles[index] = _worldGenerator.GetTileAsset(tile.TileType);
            }
        }

        // write the tile arrays onto the tilemaps
        var chunkOriginCell = data.DataToCell(startX, startY);
        var chunkBounds = new BoundsInt(chunkOriginCell.x, chunkOriginCell.y, 0, width, height, 1);

        if (_worldGenerator.GroundTilemap != null)
        {
            _worldGenerator.GroundTilemap.SetTilesBlock(chunkBounds, groundTiles);
            ArrayPool<TileBase>.Shared.Return(groundTiles, clearArray: false);
        }


        if (_worldGenerator.BorderVisualTilemap != null)
        {
            _worldGenerator.BorderVisualTilemap.SetTilesBlock(chunkBounds, borderTiles);
            ArrayPool<TileBase>.Shared.Return(borderTiles, clearArray: false);
        }
                
        if (_worldGenerator.BorderCollisionTilemap != null)
        {
            _worldGenerator.BorderCollisionTilemap.SetTilesBlock(chunkBounds, borderCollisionTiles);
            ArrayPool<TileBase>.Shared.Return(borderCollisionTiles, clearArray: false);
        }

        _renderedChunks.Add(chunkCoord);
    }

    protected override void UnloadChunk(Vector2Int chunkCoord)
    {
        if (!_renderedChunks.Contains(chunkCoord))
            return;

        if (!_enableChunkUnloading || _worldGenerator == null || _worldGenerator.CurrentWorldData == null)
            return;

        // get the final chunk's width and height
        var data = _worldGenerator.CurrentWorldData;
        int startX = chunkCoord.x * _chunkSize;
        int startY = chunkCoord.y * _chunkSize;

        if (startX < 0 || startY < 0 || startX >= data.Width || startY >= data.Height)
            return;

        int width = Mathf.Min(_chunkSize, data.Width - startX);
        int height = Mathf.Min(_chunkSize, data.Height - startY);

        if (width <= 0 || height <= 0)
            return;

        // write empty tile arrays to clear the chunk and remove it from rendered chunks
        var emptyTiles = new TileBase[width * height];
        var chunkOriginCell = data.DataToCell(startX, startY);
        var chunkBounds = new BoundsInt(chunkOriginCell.x, chunkOriginCell.y, 0, width, height, 1);

        if (_worldGenerator.GroundTilemap != null)
            _worldGenerator.GroundTilemap.SetTilesBlock(chunkBounds, emptyTiles);

        if (_worldGenerator.BorderVisualTilemap != null)
            _worldGenerator.BorderVisualTilemap.SetTilesBlock(chunkBounds, emptyTiles);

        if (_worldGenerator.BorderCollisionTilemap != null)
            _worldGenerator.BorderCollisionTilemap.SetTilesBlock(chunkBounds, emptyTiles);

        _renderedChunks.Remove(chunkCoord);
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
        if (_worldGenerator == null)
            return;

        if (_worldGenerator.GroundTilemap != null)
            _worldGenerator.GroundTilemap.ClearAllTiles();

        if (_worldGenerator.BorderVisualTilemap != null)
            _worldGenerator.BorderVisualTilemap.ClearAllTiles();

        if (_worldGenerator.BorderCollisionTilemap != null)
            _worldGenerator.BorderCollisionTilemap.ClearAllTiles();
    }

    private static bool IsInnerBorderTile(WorldRuntimeData data, int x, int y)
    {
        if (!data.IsInside(x, y))
            return false;

        if (data.GetTile(x, y).TileType != TileType.Void)
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

    private static bool HasWalkableNeighbour(WorldRuntimeData data, int x, int y)
    {
        if (!data.IsInside(x, y))
            return false;

        return data.GetTile(x, y).TileType != TileType.Void;
    }
}
