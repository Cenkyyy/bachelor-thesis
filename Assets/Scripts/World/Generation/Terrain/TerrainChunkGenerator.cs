using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public sealed class TerrainChunkGenerator : ChunkWorldContentGeneratorBase
{
    [Header("Terrain Streaming")]
    [SerializeField] private bool _clearTilemapsOnEnable = true;
    [SerializeField] private bool _enableChunkUnloading;

    private readonly HashSet<Vector2Int> _renderedChunks = new HashSet<Vector2Int>();

    protected override bool EnableChunkUnloading => _enableChunkUnloading;

    protected override void OnEnable()
    {
        if (_clearTilemapsOnEnable)
            ClearTilemaps();

        base.OnEnable();
    }

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

        int chunkSize = Mathf.Max(1, _chunkSize);
        int startX = chunkCoord.x * chunkSize;
        int startY = chunkCoord.y * chunkSize;

        if (startX < 0 || startY < 0 || startX >= data.Width || startY >= data.Height)
            return;

        int width = Mathf.Min(chunkSize, data.Width - startX);
        int height = Mathf.Min(chunkSize, data.Height - startY);

        if (width <= 0 || height <= 0)
            return;

        var groundTiles = new TileBase[width * height];
        var borderTiles = new TileBase[width * height];
        var borderTileAsset = _worldGenerator.GetBorderTileAsset();

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
                    continue;
                }

                groundTiles[index] = _worldGenerator.GetTileAsset(tile.TileType);
            }
        }

        var chunkOriginCell = data.DataToCell(startX, startY);
        var chunkBounds = new BoundsInt(chunkOriginCell.x, chunkOriginCell.y, 0, width, height, 1);

        if (_worldGenerator.GroundTilemap != null)
            _worldGenerator.GroundTilemap.SetTilesBlock(chunkBounds, groundTiles);

        if (_worldGenerator.BorderTilemap != null)
            _worldGenerator.BorderTilemap.SetTilesBlock(chunkBounds, borderTiles);

        _renderedChunks.Add(chunkCoord);
    }

    protected override void UnloadChunk(Vector2Int chunkCoord)
    {
        if (!_renderedChunks.Contains(chunkCoord))
            return;

        if (!_enableChunkUnloading || _worldGenerator == null || _worldGenerator.CurrentWorldData == null)
            return;

        var data = _worldGenerator.CurrentWorldData;
        int chunkSize = Mathf.Max(1, _chunkSize);
        int startX = chunkCoord.x * chunkSize;
        int startY = chunkCoord.y * chunkSize;

        if (startX < 0 || startY < 0 || startX >= data.Width || startY >= data.Height)
            return;

        int width = Mathf.Min(chunkSize, data.Width - startX);
        int height = Mathf.Min(chunkSize, data.Height - startY);

        if (width <= 0 || height <= 0)
            return;

        var emptyTiles = new TileBase[width * height];
        var chunkOriginCell = data.DataToCell(startX, startY);
        var chunkBounds = new BoundsInt(chunkOriginCell.x, chunkOriginCell.y, 0, width, height, 1);

        if (_worldGenerator.GroundTilemap != null)
            _worldGenerator.GroundTilemap.SetTilesBlock(chunkBounds, emptyTiles);

        if (_worldGenerator.BorderTilemap != null)
            _worldGenerator.BorderTilemap.SetTilesBlock(chunkBounds, emptyTiles);

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

        if (_worldGenerator.BorderTilemap != null)
            _worldGenerator.BorderTilemap.ClearAllTiles();
    }
}
