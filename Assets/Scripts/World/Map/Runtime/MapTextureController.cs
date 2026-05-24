using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Builds and updates the shared terrain texture used by minimap and full world map views.
/// </summary>
[DisallowMultipleComponent]
public sealed class MapTextureController : MonoBehaviour
{
    private struct ChunkBounds
    {
        public int MinX;
        public int MinY;
        public int MaxX;
        public int MaxY;
    }

    [Header("World References")]
    [SerializeField] private Tilemap _groundTilemap;
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private WallChunkGenerator _wallChunkGenerator;

    [Header("Tile Data")]
    [SerializeField] private WorldTileData[] _worldTiles;

    [Header("Reveal Settings")]
    [SerializeField, Min(1)] private int _revealRadiusTiles = 8;
    [SerializeField, Min(0)] private int _wallRevealDepthBehindOuterTile = 3;

    [Header("Texture Settings")]
    [SerializeField, Min(1)] private int _chunkSizeTiles = 32;
    [SerializeField, Min(1)] private int _maxChunkUpdatesPerFrame = 2;
    [SerializeField, Min(0)] private int _initializationChunkRadiusAroundPlayer = 4;
    [SerializeField, Min(1)] private int _initializationRowsPerFrame = 128;
    [SerializeField, Min(0.1f)] private float _maxInitializationMillisecondsPerFrame = 6f;
    [SerializeField] private Color32 _unexploredColor = new(0, 0, 0, 0);

    public WorldRuntimeData WorldData { get; private set; }
    public Texture2D TerrainTexture { get; private set; }
    public Tilemap GroundTilemap => _groundTilemap;
    public bool IsInitialized { get; private set; }
    public Vector2Int CurrentPlayerTile { get; private set; } = new(int.MinValue, int.MinValue);
    public float PlayerRotationZ => _playerTransform != null ? _playerTransform.eulerAngles.z : 0f;

    private MapExplorationData _exploration;
    private MapTextureRuntimeData _textureRuntimeData;
    private int _chunksX;
    private int _chunksY;
    private Coroutine _runtimeDataBuildCoroutine;

    private void OnEnable()
    {
        SubscribeToWallChanges();
    }

    private void OnDisable()
    {
        UnsubscribeFromWallChanges();
        StopRuntimeDataBuild();
    }

    private void LateUpdate()
    {
        if (!IsInitialized)
            return;

        UpdateExplorationAroundPlayer(force: false);
        ProcessDirtyChunks(forceAll: false);
    }

    /// <summary>
    /// Initializes the shared map texture from generated world data.
    /// </summary>
    public IEnumerator InitializeCoroutine(WorldRuntimeData worldData)
    {
        StopRuntimeDataBuild();

        WorldData = worldData;
        IsInitialized = false;

        if (WorldData == null)
            yield break;

        _chunksX = Mathf.CeilToInt(WorldData.Width / (float)_chunkSizeTiles);
        _chunksY = Mathf.CeilToInt(WorldData.Height / (float)_chunkSizeTiles);
        _textureRuntimeData = new MapTextureRuntimeData(WorldData.Width, WorldData.Height, _chunksX * _chunksY, _unexploredColor);
        _exploration = new MapExplorationData(WorldData.Width, WorldData.Height);

        BuildTexture();
        ResolveChunksAround(GetPlayerTile(), _initializationChunkRadiusAroundPlayer);
        UpdateExplorationAroundPlayer(force: true);
        ProcessDirtyChunks(forceAll: true);

        IsInitialized = true;
        _runtimeDataBuildCoroutine = StartCoroutine(BuildRuntimeDataInBackgroundCoroutine());
    }

    /// <summary>
    /// Converts a world position into normalized map texture coordinates.
    /// </summary>
    private Vector2 GetWorldPositionNormalized(Vector3 worldPosition)
    {
        return MapCoordinateUtility.WorldToDataNormalized(_groundTilemap, WorldData, worldPosition);
    }

    /// <summary>
    /// Gets the player's current normalized position on the map texture.
    /// </summary>
    public Vector2 GetPlayerPositionNormalized()
    {
        if (_playerTransform == null)
            return Vector2.zero;

        return GetWorldPositionNormalized(_playerTransform.position);
    }

    /// <summary>
    /// Creates a UV rectangle centered around a tile coordinate and clamped to the terrain texture.
    /// </summary>
    public Rect GetCenteredUvRect(Vector2Int center, int halfSizeTiles)
    {
        if (WorldData == null)
            return new Rect(0f, 0f, 1f, 1f);

        int worldWidth = WorldData.Width;
        int worldHeight = WorldData.Height;

        float viewW = Mathf.Min(worldWidth, halfSizeTiles * 2) / (float)worldWidth;
        float viewH = Mathf.Min(worldHeight, halfSizeTiles * 2) / (float)worldHeight;

        float u = (center.x - halfSizeTiles) / (float)worldWidth;
        float v = (center.y - halfSizeTiles) / (float)worldHeight;

        u = Mathf.Clamp01(u);
        v = Mathf.Clamp01(v);

        if (u + viewW > 1f)
            u = 1f - viewW;

        if (v + viewH > 1f)
            v = 1f - viewH;

        return new Rect(u, v, viewW, viewH);
    }

    private void StopRuntimeDataBuild()
    {
        if (_runtimeDataBuildCoroutine == null)
            return;

        StopCoroutine(_runtimeDataBuildCoroutine);
        _runtimeDataBuildCoroutine = null;
    }

    private IEnumerator BuildRuntimeDataInBackgroundCoroutine()
    {
        yield return null;

        int rowBudget = _initializationRowsPerFrame;
        float timeBudget = Mathf.Max(Mathf.Epsilon, _maxInitializationMillisecondsPerFrame * 0.001f);
        int processedRows = 0;
        float frameStartTime = Time.realtimeSinceStartup;

        bool hasPendingTiles;
        do
        {
            hasPendingTiles = false;

            for (int y = 0; y < WorldData.Height; y++)
            {
                for (int x = 0; x < WorldData.Width; x++)
                {
                    int index = ToIndex(x, y);
                    if (_textureRuntimeData.HasTerrainPixelColorByIndex[index])
                        continue;

                    if (!WorldData.IsTileGenerated(x, y))
                    {
                        hasPendingTiles = true;
                        continue;
                    }

                    ResolveBaseTileColor(x, y);
                }

                processedRows++;
                if (processedRows >= rowBudget || Time.realtimeSinceStartup - frameStartTime >= timeBudget)
                {
                    processedRows = 0;
                    frameStartTime = Time.realtimeSinceStartup;
                    yield return null;
                }
            }
        } while (hasPendingTiles);

        _runtimeDataBuildCoroutine = null;
    }

    private void ResolveChunksAround(Vector2Int centerTile, int chunkRadius)
    {
        if (_textureRuntimeData == null || WorldData == null)
            return;

        var centerChunk = WorldChunkUtility.GetChunkCoordFromTile(centerTile, _chunkSizeTiles);
        var chunks = WorldChunkUtility.BuildChunkSetInRadius(centerChunk, chunkRadius);
        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            if (!IsChunkInsideWorld(chunk.x, chunk.y))
                continue;

            var bounds = GetChunkBounds(chunk.x, chunk.y);
            for (int y = bounds.MinY; y < bounds.MaxY; y++)
            {
                for (int x = bounds.MinX; x < bounds.MaxX; x++)
                {
                    ResolveBaseTileColor(x, y);
                }
            }
        }
    }

    private void BuildTexture()
    {
        TerrainTexture = new Texture2D(WorldData.Width, WorldData.Height, TextureFormat.RGBA32, mipChain: false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        TerrainTexture.SetPixels32(_textureRuntimeData.VisiblePixelColorByIndex);
        TerrainTexture.Apply(updateMipmaps: false);
    }

    private Color32 ResolveTileColor(WorldTileType tileType)
    {
        var worldTile = GetWorldTileData(tileType);
        if (worldTile != null)
            return worldTile.MapColor;

        return new Color32(0, 0, 0, 255);
    }

    private WorldTileData GetWorldTileData(WorldTileType tileType)
    {
        if (_worldTiles == null)
            return null;

        for (int i = 0; i < _worldTiles.Length; i++)
        {
            var worldTile = _worldTiles[i];
            if (worldTile != null && worldTile.TileType == tileType)
                return worldTile;
        }

        return null;
    }

    private Color32 ResolveBaseTileColor(int x, int y)
    {
        int index = ToIndex(x, y);
        if (_textureRuntimeData.HasTerrainPixelColorByIndex[index])
            return _textureRuntimeData.TerrainPixelColorByIndex[index];

        var tileType = WorldData.GetTile(x, y).TileType;
        var color = ResolveTileColor(tileType);
        _textureRuntimeData.TerrainPixelColorByIndex[index] = color;
        _textureRuntimeData.HasTerrainPixelColorByIndex[index] = true;
        return color;
    }

    private void UpdateExplorationAroundPlayer(bool force)
    {
        var playerTile = GetPlayerTile();

        if (force || playerTile != CurrentPlayerTile)
            RevealAround(playerTile);

        CurrentPlayerTile = playerTile;
    }

    private void SubscribeToWallChanges()
    {
        if (_wallChunkGenerator == null)
            return;

        _wallChunkGenerator.OnWallTileChanged -= HandleWallTileChanged;
        _wallChunkGenerator.OnWallTileChanged += HandleWallTileChanged;
    }

    private void UnsubscribeFromWallChanges()
    {
        if (_wallChunkGenerator == null)
            return;

        _wallChunkGenerator.OnWallTileChanged -= HandleWallTileChanged;
    }

    private void HandleWallTileChanged(Vector2Int tile)
    {
        MarkTileDirty(tile.x, tile.y);
        MarkTileDirty(tile.x - 1, tile.y);
        MarkTileDirty(tile.x + 1, tile.y);
        MarkTileDirty(tile.x, tile.y - 1);
        MarkTileDirty(tile.x, tile.y + 1);
    }

    private Vector2Int GetPlayerTile()
    {
        if (_groundTilemap == null || _playerTransform == null || WorldData == null)
            return Vector2Int.zero;

        var cell = _groundTilemap.WorldToCell(_playerTransform.position);
        var dataPos = WorldData.CellToData(cell);

        dataPos.x = Mathf.Clamp(dataPos.x, 0, WorldData.Width - 1);
        dataPos.y = Mathf.Clamp(dataPos.y, 0, WorldData.Height - 1);

        return dataPos;
    }

    private void RevealAround(Vector2Int center)
    {
        int radius = _revealRadiusTiles;
        EnsureKnownWallPlansAround(center, radius + _wallRevealDepthBehindOuterTile + 1);

        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                if (!IsTileInsideRevealCircle(dx, dy, radius))
                    continue;

                int x = center.x + dx;
                int y = center.y + dy;
                if (!IsTileVisibleFrom(center.x, center.y, x, y))
                    continue;

                if (!_exploration.TrySetExplored(x, y))
                    continue;

                MarkTileDirty(x, y);
            }
        }
    }

    private void EnsureKnownWallPlansAround(Vector2Int center, int radius)
    {
        if (_wallChunkGenerator == null || WorldData == null)
            return;

        _wallChunkGenerator.EnsureWallPlansForTileRange(
            WorldData,
            center.x - radius,
            center.y - radius,
            center.x + radius,
            center.y + radius);
    }

    private bool IsTileInsideRevealCircle(int dx, int dy, int radius)
    {
        float sampleX = Mathf.Abs(dx) + 0.5f;
        float sampleY = Mathf.Abs(dy) + 0.5f;
        return (sampleX * sampleX) + (sampleY * sampleY) <= radius * radius;
    }

    private bool IsTileVisibleFrom(int originX, int originY, int targetX, int targetY)
    {
        if (!IsInsideWorld(targetX, targetY))
            return false;

        int currentX = originX;
        int currentY = originY;
        int deltaX = Mathf.Abs(targetX - originX);
        int deltaY = Mathf.Abs(targetY - originY);
        int stepX = originX < targetX ? 1 : -1;
        int stepY = originY < targetY ? 1 : -1;
        int error = deltaX - deltaY;
        int tilesBehindFirstWall = -1;

        while (currentX != targetX || currentY != targetY)
        {
            int twiceError = error * 2;
            if (twiceError > -deltaY)
            {
                error -= deltaY;
                currentX += stepX;
            }

            if (twiceError < deltaX)
            {
                error += deltaX;
                currentY += stepY;
            }

            if (tilesBehindFirstWall >= 0)
            {
                tilesBehindFirstWall++;
                if (tilesBehindFirstWall > _wallRevealDepthBehindOuterTile)
                    return false;
            }

            if (tilesBehindFirstWall < 0 && HasWallAt(currentX, currentY))
                tilesBehindFirstWall = 0;
        }

        return true;
    }

    private void MarkTileDirty(int x, int y)
    {
        if (!IsInsideWorld(x, y) || _textureRuntimeData == null)
            return;

        int chunkIndex = ToChunkIndex(x / _chunkSizeTiles, y / _chunkSizeTiles);
        if (_textureRuntimeData.NeedsTextureRefreshByChunkIndex[chunkIndex])
            return;

        _textureRuntimeData.NeedsTextureRefreshByChunkIndex[chunkIndex] = true;
        if (_textureRuntimeData.QueuedTextureChunkIndices.Add(chunkIndex))
            _textureRuntimeData.PendingTextureChunkIndices.Enqueue(chunkIndex);
    }

    private void ProcessDirtyChunks(bool forceAll)
    {
        int budget = forceAll ? int.MaxValue : _maxChunkUpdatesPerFrame;
        int processed = 0;
        bool textureChanged = false;

        while (_textureRuntimeData.PendingTextureChunkIndices.Count > 0 && processed < budget)
        {
            int chunkIndex = _textureRuntimeData.PendingTextureChunkIndices.Dequeue();
            _textureRuntimeData.QueuedTextureChunkIndices.Remove(chunkIndex);
            if (!_textureRuntimeData.NeedsTextureRefreshByChunkIndex[chunkIndex])
                continue;

            _textureRuntimeData.NeedsTextureRefreshByChunkIndex[chunkIndex] = false;
            ChunkBounds bounds = GetChunkBounds(chunkIndex);

            for (int y = bounds.MinY; y < bounds.MaxY; y++)
            {
                for (int x = bounds.MinX; x < bounds.MaxX; x++)
                {
                    if (!_exploration.IsExplored(x, y))
                        continue;

                    int index = ToIndex(x, y);
                    _textureRuntimeData.VisiblePixelColorByIndex[index] = ResolveVisibleTileColor(x, y);
                }
            }

            ApplyChunkPixels(bounds);
            textureChanged = true;
            processed++;
        }

        if (textureChanged)
            TerrainTexture.Apply(updateMipmaps: false);
    }

    private void ApplyChunkPixels(ChunkBounds bounds)
    {
        var width = bounds.MaxX - bounds.MinX;
        var height = bounds.MaxY - bounds.MinY;
        var colors = new Color32[width * height];
        int write = 0;

        for (int y = bounds.MinY; y < bounds.MaxY; y++)
        {
            for (int x = bounds.MinX; x < bounds.MaxX; x++)
            {
                colors[write] = _textureRuntimeData.VisiblePixelColorByIndex[ToIndex(x, y)];
                write++;
            }
        }

        TerrainTexture.SetPixels32(bounds.MinX, bounds.MinY, width, height, colors);
    }

    private Color32 ResolveVisibleTileColor(int x, int y)
    {
        if (_wallChunkGenerator == null || !_wallChunkGenerator.TryGetKnownWallDataAtDataTile(new Vector2Int(x, y), out var wallData))
            return ResolveBaseTileColor(x, y);

        return HasAdjacentOpenTile(x, y) ? wallData.MinimapBorderColor : wallData.MinimapInnerColor;
    }

    private bool HasAdjacentOpenTile(int x, int y)
    {
        return !HasWallAt(x + 1, y)
            || !HasWallAt(x - 1, y)
            || !HasWallAt(x, y + 1)
            || !HasWallAt(x, y - 1);
    }

    private bool HasWallAt(int x, int y)
    {
        if (!IsInsideWorld(x, y))
            return false;

        return _wallChunkGenerator != null && _wallChunkGenerator.HasKnownWallAtDataTile(new Vector2Int(x, y));
    }

    private bool IsInsideWorld(int x, int y)
    {
        return WorldData != null && x >= 0 && x < WorldData.Width && y >= 0 && y < WorldData.Height;
    }

    private int ToIndex(int x, int y) => (y * WorldData.Width) + x;

    private int ToChunkIndex(int chunkX, int chunkY) => (chunkY * _chunksX) + chunkX;

    private bool IsChunkInsideWorld(int chunkX, int chunkY)
    {
        return chunkX >= 0 && chunkY >= 0 && chunkX < _chunksX && chunkY < _chunksY;
    }

    private ChunkBounds GetChunkBounds(int chunkIndex)
    {
        int chunkX = chunkIndex % _chunksX;
        int chunkY = chunkIndex / _chunksX;

        return GetChunkBounds(chunkX, chunkY);
    }

    private ChunkBounds GetChunkBounds(int chunkX, int chunkY)
    {
        int minX = chunkX * _chunkSizeTiles;
        int minY = chunkY * _chunkSizeTiles;

        return new ChunkBounds
        {
            MinX = minX,
            MinY = minY,
            MaxX = Mathf.Min(minX + _chunkSizeTiles, WorldData.Width),
            MaxY = Mathf.Min(minY + _chunkSizeTiles, WorldData.Height)
        };
    }
}
