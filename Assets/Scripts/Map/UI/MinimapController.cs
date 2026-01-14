using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public sealed class MinimapController : MonoBehaviour
{
    [Serializable]
    public struct TileColor
    {
        public TileType TileType;
        public Color32 Color;
    }

    [Header("UI Refs")]
    [SerializeField] private RawImage _terrainImage;
    [SerializeField] private RawImage _fogImage;
    [SerializeField] private RectTransform _minimapViewport;
    [SerializeField] private RectTransform _playerMarker;

    [Header("World Refs")]
    [SerializeField] private Tilemap _groundTilemap;
    [SerializeField] private Transform _playerTransform;

    [Header("Config")]
    [SerializeField] private int _minimapHalfSizeTiles = 18;
    [SerializeField] private int _revealRadiusTiles = 8;
    [SerializeField] private TileColor[] _tileColors;

    public WorldData WorldData { get; private set; }
    private ExplorationData _exploration;

    public Texture2D TerrainTexture { get; private set; }
    public Texture2D FogTexture { get; private set; }

    private Dictionary<TileType, Color32> _colorByTile;
    private Vector2Int _lastPlayerTile = new Vector2Int(int.MinValue, int.MinValue);
    public bool IsInitialized { get; private set; } = false;

    public void Initialize(WorldData worldData)
    {
        WorldData = worldData;

        BuildColorLookup();
        BuildTerrainTexture();
        BuildFogTexture();

        _exploration = new ExplorationData(WorldData.Width, WorldData.Height);

        _terrainImage.texture = TerrainTexture;
        _fogImage.texture = FogTexture;

        UpdateView(force: true);
        IsInitialized = true;
    }

    private void LateUpdate()
    {
        if (!IsInitialized)
            return;

        UpdateView(force: false);
        UpdatePlayerMarkerRotation();
    }

    private void BuildColorLookup()
    {
        _colorByTile = new Dictionary<TileType, Color32>();
        for (int i = 0; i < _tileColors.Length; i++)
            _colorByTile[_tileColors[i].TileType] = _tileColors[i].Color;
    }

    private void BuildTerrainTexture()
    {
        int w = WorldData.Width;
        int h = WorldData.Height;

        TerrainTexture = new Texture2D(w, h, TextureFormat.RGBA32, mipChain: false);
        TerrainTexture.filterMode = FilterMode.Point;
        TerrainTexture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                var tileType = WorldData.Tiles[x, y].TileType;

                if (!_colorByTile.TryGetValue(tileType, out var col))
                    col = new Color32(0, 0, 0, 255);

                TerrainTexture.SetPixel(x, y, col);
            }
        }

        TerrainTexture.Apply(updateMipmaps: false);
    }

    private void BuildFogTexture()
    {
        int w = WorldData.Width;
        int h = WorldData.Height;

        FogTexture = new Texture2D(w, h, TextureFormat.RGBA32, mipChain: false);
        FogTexture.filterMode = FilterMode.Point;
        FogTexture.wrapMode = TextureWrapMode.Clamp;

        var pixels = new Color32[w * h];
        var fog = new Color32(0, 0, 0, 255);

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = fog;

        FogTexture.SetPixels32(pixels);
        FogTexture.Apply(updateMipmaps: false);
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
        int rSq = r * r;

        bool anyChanged = false;

        for (int dy = -r; dy <= r; dy++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                if (dx * dx + dy * dy > rSq)
                    continue;

                int x = center.x + dx;
                int y = center.y + dy;

                if (_exploration.TrySetExplored(x, y))
                {
                    FogTexture.SetPixel(x, y, new Color32(0, 0, 0, 0));
                    anyChanged = true;
                }
            }
        }

        if (anyChanged)
            FogTexture.Apply(updateMipmaps: false);
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
        _fogImage.uvRect = rect;
    }

    private void UpdatePlayerMarkerRotation()
    {
        if (_playerMarker == null || _playerTransform == null)
            return;

        float z = _playerTransform.eulerAngles.z;
        _playerMarker.localRotation = Quaternion.Euler(0f, 0f, z);
    }
}
