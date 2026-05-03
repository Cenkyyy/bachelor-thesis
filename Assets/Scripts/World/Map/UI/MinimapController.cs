using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public sealed class MinimapController : MonoBehaviour, IMapMarkerPresenter
{
    [Header("UI Refs")]
    [SerializeField] private RawImage _terrainImage;
    [SerializeField] private RectTransform _minimapViewport;
    [SerializeField] private RectMask2D _minimapMask;
    [SerializeField] private RectTransform _playerMarker;
    [SerializeField] private RectTransform _markerContainer;
    [SerializeField] private RectTransform _markerPrefab;

    [Header("World Refs")]
    [SerializeField] private Tilemap _groundTilemap;
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private WallChunkGenerator _wallChunkGenerator;

    [Header("Config")]
    [SerializeField, Min(1)] private int _minimapHalfSizeTiles = 18;
    [SerializeField, Min(1)] private int _revealRadiusTiles = 8;
    [SerializeField, Min(1)] private int _chunkSizeTiles = 32;
    [SerializeField, Min(1)] private int _maxChunkUpdatesPerFrame = 2;
    [SerializeField, Min(1)] private int _initializationRowsPerFrame = 128;
    [SerializeField, Min(0.1f)] private float _maxInitializationMillisecondsPerFrame = 6f;
    [SerializeField] private Color32 _unexploredColor = new(0, 0, 0, 0);
    [SerializeField] private TileColor[] _tileColors;

    [Serializable]
    public struct TileColor
    {
        public TileType TileType;
        public Color32 Color;
    }

    private struct ChunkBounds
    {
        public int MinX;
        public int MinY;
        public int MaxX;
        public int MaxY;
    }

    public WorldRuntimeData WorldData { get; private set; }
    public Texture2D TerrainTexture { get; private set; }
    public bool IsInitialized { get; private set; } = false;

    private ExplorationData _exploration;
    private Color32[] _tileColorLookup;
    private bool[] _hasColorOverrideForTileType;
    private MapRuntimeData _runtimeData;
    private int _chunksX;
    private int _chunksY;
    private readonly Dictionary<string, MapMarkerRuntimeData> _markerDataById = new();
    private readonly Dictionary<string, RectTransform> _markerViewById = new();
    private Vector2Int _lastPlayerTile = new(int.MinValue, int.MinValue);
    private Coroutine _enableMaskCoroutine;

    private void OnEnable()
    {
        SubscribeToWallChanges();

        if (_minimapMask == null)
            return;

        _minimapMask.enabled = false;
        _enableMaskCoroutine = StartCoroutine(EnableMaskNextFrameCoroutine());
    }

    private void OnDisable()
    {
        UnsubscribeFromWallChanges();

        if (_enableMaskCoroutine != null)
        {
            StopCoroutine(_enableMaskCoroutine);
            _enableMaskCoroutine = null;
        }
    }

    private IEnumerator EnableMaskNextFrameCoroutine()
    {
        yield return null;
        if (_minimapMask != null)
            _minimapMask.enabled = true;

        _enableMaskCoroutine = null;
    }

    public IEnumerator InitializeAsync(WorldRuntimeData worldData)
    {
        WorldData = worldData;
        IsInitialized = false;

        BuildColorLookup();
        _chunksX = Mathf.CeilToInt(WorldData.Width / (float)_chunkSizeTiles);
        _chunksY = Mathf.CeilToInt(WorldData.Height / (float)_chunkSizeTiles);
        yield return BuildRuntimeDataAsync();
        BuildTexture();

        _exploration = new ExplorationData(WorldData.Width, WorldData.Height);
        _terrainImage.texture = TerrainTexture;

        RevealAround(GetPlayerTile());
        ProcessDirtyChunks(forceAll: true);
        UpdateView(force: true);
        IsInitialized = true;
        yield break;
    }

    private void LateUpdate()
    {
        if (!IsInitialized)
            return;

        UpdateView(force: false);
        ProcessDirtyChunks(forceAll: false);
        UpdatePlayerMarkerRotation();
        UpdateMarkers();
    }

    public void AddMarker(MapMarkerRuntimeData marker)
    {
        if (string.IsNullOrEmpty(marker.Id))
            return;

        _markerDataById[marker.Id] = marker;

        if (!_markerViewById.ContainsKey(marker.Id))
            _markerViewById[marker.Id] = CreateMarkerView();

        if (!IsInitialized)
            return;

        UpdateMarkerView(marker.Id);
    }

    public void UpdateMarker(MapMarkerRuntimeData marker)
    {
        AddMarker(marker);
    }

    public void RemoveMarker(string markerId)
    {
        if (string.IsNullOrEmpty(markerId))
            return;

        _markerDataById.Remove(markerId);

        if (_markerViewById.TryGetValue(markerId, out var markerView))
        {
            if (markerView != null)
                Destroy(markerView.gameObject);

            _markerViewById.Remove(markerId);
        }
    }

    private void BuildColorLookup()
    {
        int maxTileTypeValue = 0;
        for (int i = 0; i < _tileColors.Length; i++)
            maxTileTypeValue = Mathf.Max(maxTileTypeValue, (int)_tileColors[i].TileType);

        int lookupSize = Mathf.Max(maxTileTypeValue + 1, Enum.GetValues(typeof(TileType)).Length);
        _tileColorLookup = new Color32[lookupSize];
        _hasColorOverrideForTileType = new bool[lookupSize];

        for (int i = 0; i < _tileColors.Length; i++)
        {
            int tileTypeIndex = (int)_tileColors[i].TileType;
            _tileColorLookup[tileTypeIndex] = _tileColors[i].Color;
            _hasColorOverrideForTileType[tileTypeIndex] = true;
        }
    }

    private IEnumerator BuildRuntimeDataAsync()
    {
        int chunkCount = _chunksX * _chunksY;
        _runtimeData = new MapRuntimeData(WorldData.Width, WorldData.Height, chunkCount, _unexploredColor);
        int rowBudget = Mathf.Max(1, _initializationRowsPerFrame);
        float timeBudget = Mathf.Max(0.0001f, _maxInitializationMillisecondsPerFrame * 0.001f);
        int processedRows = 0;
        float frameStartTime = Time.realtimeSinceStartup;

        for (int y = 0; y < WorldData.Height; y++)
        {
            for (int x = 0; x < WorldData.Width; x++)
            {
                int index = ToIndex(x, y);
                var tileType = WorldData.GetTile(x, y).TileType;
                _runtimeData.ResolvedPixelByIndex[index] = ResolveTileColor(tileType);
            }

            processedRows++;
            if (processedRows >= rowBudget || Time.realtimeSinceStartup - frameStartTime >= timeBudget)
            {
                processedRows = 0;
                frameStartTime = Time.realtimeSinceStartup;
                yield return null;
            }
        }

        _runtimeData.ClearDirtyState();
        yield break;
    }

    private void BuildTexture()
    {
        TerrainTexture = new Texture2D(WorldData.Width, WorldData.Height, TextureFormat.RGBA32, mipChain: false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        TerrainTexture.SetPixels32(_runtimeData.DisplayPixelByIndex);
        TerrainTexture.Apply(updateMipmaps: false);
    }

    private Color32 ResolveTileColor(TileType tileType)
    {
        int tileTypeIndex = (int)tileType;
        if (_tileColorLookup != null
            && _hasColorOverrideForTileType != null
            && tileTypeIndex >= 0
            && tileTypeIndex < _tileColorLookup.Length
            && _hasColorOverrideForTileType[tileTypeIndex])
        {
            return _tileColorLookup[tileTypeIndex];
        }

        return new Color32(0, 0, 0, 255);
    }

    private void UpdateView(bool force)
    {
        var playerTile = GetPlayerTile();

        if (!force && playerTile != _lastPlayerTile)
            RevealAround(playerTile);

        _lastPlayerTile = playerTile;

        UpdateUvRect(playerTile);
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
        var cell = _groundTilemap.WorldToCell(_playerTransform.position);
        var dataPos = WorldData.CellToData(cell);

        dataPos.x = Mathf.Clamp(dataPos.x, 0, WorldData.Width - 1);
        dataPos.y = Mathf.Clamp(dataPos.y, 0, WorldData.Height - 1);

        return dataPos;
    }

    private void RevealAround(Vector2Int center)
    {
        int radius = _revealRadiusTiles;

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

        while (currentX != targetX || currentY != targetY)
        {
            if (currentX != originX || currentY != originY)
            {
                if (HasWallAt(currentX, currentY))
                    return false;
            }

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
        }

        return true;
    }

    private void MarkTileDirty(int x, int y)
    {
        if (!IsInsideWorld(x, y))
            return;

        int chunkIndex = ToChunkIndex(x / _chunkSizeTiles, y / _chunkSizeTiles);
        if (_runtimeData.IsChunkDirty[chunkIndex])
            return;

        _runtimeData.IsChunkDirty[chunkIndex] = true;
        if (_runtimeData.QueuedChunkIndices.Add(chunkIndex))
            _runtimeData.DirtyChunkQueue.Enqueue(chunkIndex);
    }

    private void ProcessDirtyChunks(bool forceAll)
    {
        int budget = forceAll ? int.MaxValue : _maxChunkUpdatesPerFrame;
        int processed = 0;
        bool textureChanged = false;

        while (_runtimeData.DirtyChunkQueue.Count > 0 && processed < budget)
        {
            int chunkIndex = _runtimeData.DirtyChunkQueue.Dequeue();
            _runtimeData.QueuedChunkIndices.Remove(chunkIndex);
            if (!_runtimeData.IsChunkDirty[chunkIndex])
                continue;

            _runtimeData.IsChunkDirty[chunkIndex] = false;
            ChunkBounds bounds = GetChunkBounds(chunkIndex);

            for (int y = bounds.MinY; y < bounds.MaxY; y++)
            {
                for (int x = bounds.MinX; x < bounds.MaxX; x++)
                {
                    if (!_exploration.IsExplored(x, y))
                        continue;

                    int index = ToIndex(x, y);
                    _runtimeData.DisplayPixelByIndex[index] = ResolveVisibleTileColor(x, y);
                }
            }

            var width = bounds.MaxX - bounds.MinX;
            var height = bounds.MaxY - bounds.MinY;
            var colors = new Color32[width * height];
            int write = 0;
            for (int y = bounds.MinY; y < bounds.MaxY; y++)
            {
                for (int x = bounds.MinX; x < bounds.MaxX; x++)
                {
                    colors[write] = _runtimeData.DisplayPixelByIndex[ToIndex(x, y)];
                    write++;
                }
            }

            TerrainTexture.SetPixels32(bounds.MinX, bounds.MinY, width, height, colors);
            textureChanged = true;
            processed++;
        }

        if (textureChanged)
            TerrainTexture.Apply(updateMipmaps: false);
    }

    private Color32 ResolveVisibleTileColor(int x, int y)
    {
        if (_wallChunkGenerator == null || !_wallChunkGenerator.TryGetWallDataAtDataTile(new Vector2Int(x, y), out var wallData))
            return _runtimeData.ResolvedPixelByIndex[ToIndex(x, y)];

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

        return _wallChunkGenerator != null && _wallChunkGenerator.HasWallAtDataTile(new Vector2Int(x, y));
    }

    private bool IsInsideWorld(int x, int y)
    {
        return x >= 0 && x < WorldData.Width && y >= 0 && y < WorldData.Height;
    }

    private int ToIndex(int x, int y) => (y * WorldData.Width) + x;

    private int ToChunkIndex(int chunkX, int chunkY) => (chunkY * _chunksX) + chunkX;

    private ChunkBounds GetChunkBounds(int chunkIndex)
    {
        int chunkX = chunkIndex % _chunksX;
        int chunkY = chunkIndex / _chunksX;

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

    private void UpdateUvRect(Vector2Int center)
    {
        int worldWidth = WorldData.Width;
        int worldHeight = WorldData.Height;

        float viewW = Mathf.Min(worldWidth, _minimapHalfSizeTiles * 2) / (float)worldWidth;
        float viewH = Mathf.Min(worldHeight, _minimapHalfSizeTiles * 2) / (float)worldHeight;

        float u = (center.x - _minimapHalfSizeTiles) / (float)worldWidth;
        float v = (center.y - _minimapHalfSizeTiles) / (float)worldHeight;

        u = Mathf.Clamp01(u);
        v = Mathf.Clamp01(v);

        if (u + viewW > 1f)
            u = 1f - viewW;

        if (v + viewH > 1f)
            v = 1f - viewH;

        _terrainImage.uvRect = new Rect(u, v, viewW, viewH);
    }

    private void UpdatePlayerMarkerRotation()
    {
        if (_playerMarker == null || _playerTransform == null)
            return;

        _playerMarker.localRotation = Quaternion.Euler(0f, 0f, _playerTransform.eulerAngles.z);
    }

    private RectTransform CreateMarkerView()
    {
        if (_markerPrefab == null)
            return null;

        var container = _markerContainer != null ? _markerContainer : _minimapViewport;
        if (container == null)
            return null;

        var marker = Instantiate(_markerPrefab, container);
        marker.anchorMin = new Vector2(0.5f, 0.5f);
        marker.anchorMax = new Vector2(0.5f, 0.5f);
        return marker;
    }

    private void UpdateMarkers()
    {
        foreach (var markerId in _markerDataById.Keys)
            UpdateMarkerView(markerId);
    }

    private void UpdateMarkerView(string markerId)
    {
        if (!_markerDataById.TryGetValue(markerId, out var markerData))
            return;

        if (!_markerViewById.TryGetValue(markerId, out var markerView) || markerView == null)
            return;

        var normalized = MapCoordinateUtility.WorldToDataNormalized(_groundTilemap, WorldData, markerData.WorldPosition);
        var uvRect = _terrainImage.uvRect;
        bool isVisible = normalized.x >= uvRect.xMin
            && normalized.x <= uvRect.xMax
            && normalized.y >= uvRect.yMin
            && normalized.y <= uvRect.yMax;

        markerView.gameObject.SetActive(isVisible);
        if (!isVisible)
            return;

        float localU = (normalized.x - uvRect.xMin) / uvRect.width;
        float localV = (normalized.y - uvRect.yMin) / uvRect.height;

        var container = _markerContainer != null ? _markerContainer : _minimapViewport;
        var containerSize = container.rect.size;
        var containerPivot = container.pivot;

        float localX = (localU - containerPivot.x) * containerSize.x;
        float localY = (localV - containerPivot.y) * containerSize.y;

        markerView.anchoredPosition = new Vector2(localX, localY);
    }
}
