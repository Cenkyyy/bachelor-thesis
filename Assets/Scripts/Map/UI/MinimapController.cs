using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public sealed class MinimapController : MonoBehaviour, IMapMarkerPresenter
{
    [Serializable]
    public struct TileColor
    {
        public TileType TileType;
        public Color32 Color;
    }

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

    [Header("Config")]
    [SerializeField] private int _minimapHalfSizeTiles = 18;
    [SerializeField] private int _revealRadiusTiles = 8;
    [SerializeField, Min(1)] private int _rowsProcessedPerFrameDuringInitialization = 48;
    [SerializeField] private TileColor[] _tileColors;

    public WorldRuntimeData WorldData { get; private set; }
    private ExplorationData _exploration;

    public Texture2D TerrainTexture { get; private set; }

    private Dictionary<TileType, Color32> _colorByTile;
    private readonly Dictionary<string, MapMarkerRuntimeData> _markerDataById = new Dictionary<string, MapMarkerRuntimeData>();
    private readonly Dictionary<string, RectTransform> _markerViewById = new Dictionary<string, RectTransform>();
    private Color32[] _terrainPixelsByIndex;
    private Vector2Int _lastPlayerTile = new Vector2Int(int.MinValue, int.MinValue);
    public bool IsInitialized { get; private set; } = false;
    private Coroutine _enableMaskCoroutine;

    private void OnEnable()
    {
        if (_minimapMask == null)
            return;

        _minimapMask.enabled = false;
        _enableMaskCoroutine = StartCoroutine(EnableMaskNextFrameCoroutine());
    }

    private void OnDisable()
    {
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
        yield return BuildTerrainTextureAsync();

        _exploration = new ExplorationData(WorldData.Width, WorldData.Height);
        _terrainImage.texture = TerrainTexture;

        UpdateView(force: true);
        IsInitialized = true;
    }

    private void LateUpdate()
    {
        if (!IsInitialized)
            return;

        UpdateView(force: false);
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
        _colorByTile = new Dictionary<TileType, Color32>();
        for (int i = 0; i < _tileColors.Length; i++)
            _colorByTile[_tileColors[i].TileType] = _tileColors[i].Color;
    }

    private IEnumerator BuildTerrainTextureAsync()
    {
        int w = WorldData.Width;
        int h = WorldData.Height;
        int rowsPerFrame = Mathf.Max(1, _rowsProcessedPerFrameDuringInitialization);

        TerrainTexture = new Texture2D(w, h, TextureFormat.RGBA32, mipChain: false);
        TerrainTexture.filterMode = FilterMode.Point;
        TerrainTexture.wrapMode = TextureWrapMode.Clamp;

        _terrainPixelsByIndex = new Color32[w * h];
        var hiddenPixels = new Color32[w * h];

        int processedRows = 0;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int index = (y * w) + x;
                var tileType = WorldData.GetTile(x, y).TileType;

                if (!_colorByTile.TryGetValue(tileType, out var col))
                    col = new Color32(0, 0, 0, 255);

                _terrainPixelsByIndex[index] = col;
                hiddenPixels[index] = new Color32(col.r, col.g, col.b, 0);
            }

            processedRows++;
            if (processedRows >= rowsPerFrame)
            {
                processedRows = 0;
                yield return null;
            }
        }

        TerrainTexture.SetPixels32(hiddenPixels);
        TerrainTexture.Apply(updateMipmaps: false);
    }

    private void UpdateView(bool force)
    {
        var playerTile = GetPlayerTile();

        if (!force && playerTile == _lastPlayerTile)
        {
            UpdateUvRect(playerTile);
            return;
        }

        _lastPlayerTile = playerTile;

        RevealAround(playerTile);
        UpdateUvRect(playerTile);
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
        int r = _revealRadiusTiles;

        int w = WorldData.Width;

        bool terrainChanged = false;

        for (int dy = -r; dy <= r; dy++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                if (!IsTileInsideRevealCircle(dx, dy, r))
                    continue;

                int x = center.x + dx;
                int y = center.y + dy;

                if (_exploration.TrySetExplored(x, y))
                {
                    int index = (y * w) + x;
                    TerrainTexture.SetPixel(x, y, _terrainPixelsByIndex[index]);
                    terrainChanged = true;
                }
            }
        }

        if (terrainChanged)
            TerrainTexture.Apply(updateMipmaps: false);
    }

    private bool IsTileInsideRevealCircle(int dx, int dy, int radius)
    {
        float sampleX = Mathf.Abs(dx) + 0.5f;
        float sampleY = Mathf.Abs(dy) + 0.5f;

        float tileCenterDistanceSquared = (sampleX * sampleX) + (sampleY * sampleY);
        float radiusSquared = radius * radius;

        return tileCenterDistanceSquared <= radiusSquared;
    }

    private void UpdateUvRect(Vector2Int center)
    {
        int w = WorldData.Width;
        int h = WorldData.Height;

        float viewW = Mathf.Min(w, _minimapHalfSizeTiles * 2) / (float)w;
        float viewH = Mathf.Min(h, _minimapHalfSizeTiles * 2) / (float)h;

        float u = (center.x - _minimapHalfSizeTiles) / (float)w;
        float v = (center.y - _minimapHalfSizeTiles) / (float)h;

        u = Mathf.Clamp01(u);
        v = Mathf.Clamp01(v);

        if (u + viewW > 1f) u = 1f - viewW;
        if (v + viewH > 1f) v = 1f - viewH;

        var rect = new Rect(u, v, viewW, viewH);
        _terrainImage.uvRect = rect;
    }

    private void UpdatePlayerMarkerRotation()
    {
        if (_playerMarker == null || _playerTransform == null)
            return;

        float z = _playerTransform.eulerAngles.z;
        _playerMarker.localRotation = Quaternion.Euler(0f, 0f, z);
    }

    private RectTransform CreateMarkerView()
    {
        if (_markerPrefab == null)
            return null;

        var container = _markerContainer != null ? _markerContainer : _minimapViewport;
        if (container == null)
            return null;

        var marker = Instantiate(_markerPrefab, container);
        marker.anchorMin = marker.anchorMax = new Vector2(0.5f, 0.5f);
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

        if (!IsInitialized)
            return;

        if (!_markerViewById.TryGetValue(markerId, out var markerView) || markerView == null)
            return;

        var normalized = MapCoordinateUtility.WorldToDataNormalized(_groundTilemap, WorldData, markerData.WorldPosition);
        var uvRect = _terrainImage.uvRect;
        bool isVisible =
            normalized.x >= uvRect.xMin && normalized.x <= uvRect.xMax &&
            normalized.y >= uvRect.yMin && normalized.y <= uvRect.yMax;

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
