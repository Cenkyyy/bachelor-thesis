using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Controller responsible for managing the lifecycle and viewport projection of map marker UI elements on the world map.
/// </summary>
public sealed class MapMarkerViewController
{
    private readonly MonoBehaviour _owner;
    private readonly RectTransform _markerContainer;
    private readonly RectTransform _fallbackContainer;
    private readonly RectTransform _markerPrefab;
    private readonly Dictionary<string, MapMarkerRuntimeState> _markerDataById = new();
    private readonly Dictionary<string, RectTransform> _markerViewById = new();

    public MapMarkerViewController(MonoBehaviour owner, RectTransform markerContainer, RectTransform fallbackContainer, RectTransform markerPrefab)
    {
        _owner = owner;
        _markerContainer = markerContainer;
        _fallbackContainer = fallbackContainer;
        _markerPrefab = markerPrefab;
    }

    /// <summary>
    /// Adds or replaces a marker and ensures a view exists for it when possible.
    /// </summary>
    public void AddMarker(MapMarkerRuntimeState marker)
    {
        if (string.IsNullOrEmpty(marker.Id))
            return;

        _markerDataById[marker.Id] = marker;
        EnsureMarkerView(marker.Id);
    }

    /// <summary>
    /// Updates an existing marker or adds it if this view has not seen it before.
    /// </summary>
    public void UpdateMarker(MapMarkerRuntimeState marker)
    {
        AddMarker(marker);
    }

    /// <summary>
    /// Removes a marker and destroys its instantiated UI view.
    /// </summary>
    public void RemoveMarker(string markerId)
    {
        if (string.IsNullOrEmpty(markerId))
            return;

        _markerDataById.Remove(markerId);

        if (!_markerViewById.TryGetValue(markerId, out var markerView))
            return;

        if (markerView != null)
            Object.Destroy(markerView.gameObject);

        _markerViewById.Remove(markerId);
    }

    /// <summary>
    /// Projects every known marker into the provided map viewport.
    /// </summary>
    public void RefreshMarkers(Tilemap groundTilemap, WorldRuntimeData worldData, Rect visibleUvRect, bool hideOutsideVisibleRect)
    {
        foreach (var markerId in _markerDataById.Keys)
        {
            UpdateMarkerView(markerId, groundTilemap, worldData, visibleUvRect, hideOutsideVisibleRect);
        }
    }

    private RectTransform Container => _markerContainer != null ? _markerContainer : _fallbackContainer;

    private RectTransform EnsureMarkerView(string markerId)
    {
        if (_markerViewById.TryGetValue(markerId, out var existingView) && existingView != null)
            return existingView;

        if (_markerPrefab == null || Container == null || _owner == null)
            return null;

        var marker = Object.Instantiate(_markerPrefab, Container);
        marker.anchorMin = new Vector2(0.5f, 0.5f);
        marker.anchorMax = new Vector2(0.5f, 0.5f);
        _markerViewById[markerId] = marker;
        return marker;
    }

    private void UpdateMarkerView(string markerId, Tilemap groundTilemap, WorldRuntimeData worldData, Rect visibleUvRect, bool hideOutsideVisibleRect)
    {
        if (!_markerDataById.TryGetValue(markerId, out var markerData))
            return;

        var markerView = EnsureMarkerView(markerId);
        if (markerView == null || Container == null || worldData == null || groundTilemap == null)
            return;

        var normalized = MapCoordinateUtility.WorldToDataNormalized(groundTilemap, worldData, markerData.WorldPosition);
        bool isVisible = normalized.x >= visibleUvRect.xMin
            && normalized.x <= visibleUvRect.xMax
            && normalized.y >= visibleUvRect.yMin
            && normalized.y <= visibleUvRect.yMax;

        markerView.gameObject.SetActive(!hideOutsideVisibleRect || isVisible);
        if (hideOutsideVisibleRect && !isVisible)
            return;

        float localU = Mathf.Approximately(visibleUvRect.width, 0f) ? 0f : (normalized.x - visibleUvRect.xMin) / visibleUvRect.width;
        float localV = Mathf.Approximately(visibleUvRect.height, 0f) ? 0f : (normalized.y - visibleUvRect.yMin) / visibleUvRect.height;

        var containerSize = Container.rect.size;
        var containerPivot = Container.pivot;
        float localX = (localU - containerPivot.x) * containerSize.x;
        float localY = (localV - containerPivot.y) * containerSize.y;

        markerView.anchoredPosition = new Vector2(localX, localY);
    }
}
