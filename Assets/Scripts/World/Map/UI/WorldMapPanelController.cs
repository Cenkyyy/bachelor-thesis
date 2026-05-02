using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public sealed class WorldMapPanelController : MonoBehaviour, IMajorPanel, IMapMarkerPresenter
{
    [Header("UI")]
    [SerializeField] private GameObject _root;
    [SerializeField] private RectTransform _mapContent;
    [SerializeField] private RawImage _terrainImage;
    [SerializeField] private RectTransform _playerMarker;
    [SerializeField] private RectTransform _markerContainer;
    [SerializeField] private RectTransform _markerPrefab;

    [Header("Refs")]
    [SerializeField] private MinimapController _minimap;
    [SerializeField] private Tilemap _groundTilemap;
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private WorldMapPanelZoomController _zoom;

    public PanelId Id => PanelId.Map;
    public bool IsOpen => _root.gameObject.activeSelf;
    public bool PausesGame => false;
    public bool BlocksGameplayInput => true;

    private bool _texturesBound;
    private bool _fitAppliedThisOpen;
    private readonly Dictionary<string, MapMarkerRuntimeData> _markerDataById = new Dictionary<string, MapMarkerRuntimeData>();
    private readonly Dictionary<string, RectTransform> _markerViewById = new Dictionary<string, RectTransform>();

    private void LateUpdate()
    {
        if (!IsOpen)
            return;

        TryBindTextures();

        if (!_minimap.IsInitialized)
            return;

        UpdatePlayerMarker();
        UpdateMarkers();
    }

    public void AddMarker(MapMarkerRuntimeData marker)
    {
        if (string.IsNullOrEmpty(marker.Id))
            return;

        _markerDataById[marker.Id] = marker;

        if (!_markerViewById.ContainsKey(marker.Id))
            _markerViewById[marker.Id] = CreateMarkerView();

        if (_minimap == null || !_minimap.IsInitialized)
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

    public void Open()
    {
        _root.gameObject.SetActive(true);
        _texturesBound = false;
        _fitAppliedThisOpen = false;
        TryBindTextures();
    }

    public void Close()
    {
        _root.gameObject.SetActive(false);
    }

    private void TryBindTextures()
    {
        if (_texturesBound)
            return;

        if (!_minimap.IsInitialized)
            return;

        _terrainImage.texture = _minimap.TerrainTexture;
        _terrainImage.uvRect = new Rect(0f, 0f, 1f, 1f);

        _mapContent.sizeDelta = new Vector2(_terrainImage.texture.width, _terrainImage.texture.height);

        _texturesBound = true;

        ApplyInitialFit();
    }

    private void ApplyInitialFit()
    {
        if (_fitAppliedThisOpen)
            return;

        Canvas.ForceUpdateCanvases();
        _zoom.FitToViewport();
        CenterMapOnPlayer();
        _fitAppliedThisOpen = true;
    }

    private void CenterMapOnPlayer()
    {
        Vector2 normalizedPos = GetPlayerDataPositionNormalized();
        _zoom.CenterOnNormalizedPosition(normalizedPos);
    }

    private void UpdatePlayerMarker()
    {
        Vector2 normalizedPos = GetPlayerDataPositionNormalized();

        var contentSize = _mapContent.rect.size;
        var contentPivot = _mapContent.pivot;

        float localX = (normalizedPos.x - contentPivot.x) * contentSize.x;
        float localY = (normalizedPos.y - contentPivot.y) * contentSize.y;

        _playerMarker.anchorMin = _playerMarker.anchorMax = new Vector2(0.5f, 0.5f);
        _playerMarker.anchoredPosition = new Vector2(localX, localY);

        float z = _playerTransform.eulerAngles.z;
        _playerMarker.localRotation = Quaternion.Euler(0f, 0f, z);
    }

    private Vector2 GetPlayerDataPositionNormalized()
    {
        return MapCoordinateUtility.WorldToDataNormalized(_groundTilemap, _minimap.WorldData, _playerTransform.position);
    }

    private RectTransform CreateMarkerView()
    {
        if (_markerPrefab == null)
            return null;

        var container = _markerContainer != null ? _markerContainer : _mapContent;
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

        if (_minimap == null || !_minimap.IsInitialized)
            return;

        if (!_markerViewById.TryGetValue(markerId, out var markerView) || markerView == null)
            return;

        var normalizedPos = MapCoordinateUtility.WorldToDataNormalized(_groundTilemap, _minimap.WorldData, markerData.WorldPosition);

        var contentSize = _mapContent.rect.size;
        var contentPivot = _mapContent.pivot;

        float localX = (normalizedPos.x - contentPivot.x) * contentSize.x;
        float localY = (normalizedPos.y - contentPivot.y) * contentSize.y;

        markerView.anchoredPosition = new Vector2(localX, localY);
    }
}
