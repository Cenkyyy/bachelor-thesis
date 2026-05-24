using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the shared map texture as a player-centered minimap view.
/// </summary>
[DisallowMultipleComponent]
public sealed class MinimapController : MonoBehaviour, IMapMarkerView
{
    [Header("View References")]
    [SerializeField] private RawImage _terrainImage;
    [SerializeField] private RectTransform _minimapViewport;
    [SerializeField] private RectMask2D _minimapMask;
    [SerializeField] private RectTransform _playerMarker;
    [SerializeField] private RectTransform _markerContainer;
    [SerializeField] private RectTransform _markerPrefab;

    [Header("Map Texture")]
    [SerializeField] private MapTextureController _mapTexture;

    [Header("View Settings")]
    [SerializeField, Min(1)] private int _minimapHalfSizeTiles = 18;

    private MapMarkerViewController _markerViews;
    private Coroutine _enableMaskCoroutine;

    private void Awake()
    {
        ResolveMapTexture();
        _markerViews = new MapMarkerViewController(this, _markerContainer, _minimapViewport, _markerPrefab);
    }

    private void OnEnable()
    {
        if (_minimapMask == null)
            return;

        _minimapMask.enabled = false;
        _enableMaskCoroutine = StartCoroutine(EnableMaskNextFrameCoroutine());
    }

    private void OnDisable()
    {
        if (_enableMaskCoroutine == null)
            return;

        StopCoroutine(_enableMaskCoroutine);
        _enableMaskCoroutine = null;
    }

    private void LateUpdate()
    {
        if (_mapTexture == null || !_mapTexture.IsInitialized)
            return;

        BindTexture();
        UpdateUvRect();
        UpdatePlayerMarkerRotation();
        RefreshMarkers();
    }

    /// <summary>
    /// Initializes the shared map texture for compatibility with existing world generation wiring.
    /// </summary>
    public IEnumerator InitializeCoroutine(WorldRuntimeData worldData)
    {
        if (!ResolveMapTexture())
            yield break;

        yield return _mapTexture.InitializeCoroutine(worldData);
        BindTexture();
    }

    /// <summary>
    /// Adds a marker to the minimap view.
    /// </summary>
    public void AddMarker(MapMarkerRuntimeState marker)
    {
        _markerViews.AddMarker(marker);
        RefreshMarkers();
    }

    /// <summary>
    /// Updates a marker on the minimap view.
    /// </summary>
    public void UpdateMarker(MapMarkerRuntimeState marker)
    {
        _markerViews.UpdateMarker(marker);
        RefreshMarkers();
    }

    /// <summary>
    /// Removes a marker from the minimap view.
    /// </summary>
    public void RemoveMarker(string markerId)
    {
        _markerViews.RemoveMarker(markerId);
    }

    private IEnumerator EnableMaskNextFrameCoroutine()
    {
        yield return null;

        if (_minimapMask != null)
            _minimapMask.enabled = true;

        _enableMaskCoroutine = null;
    }

    private bool ResolveMapTexture()
    {
        if (_mapTexture != null)
            return true;

        _mapTexture = GetComponent<MapTextureController>();
        return _mapTexture != null;
    }

    private void BindTexture()
    {
        if (_terrainImage == null || _mapTexture == null || _mapTexture.TerrainTexture == null)
            return;

        if (_terrainImage.texture == _mapTexture.TerrainTexture)
            return;

        _terrainImage.texture = _mapTexture.TerrainTexture;
    }

    private void UpdateUvRect()
    {
        if (_terrainImage == null || _mapTexture == null)
            return;

        _terrainImage.uvRect = _mapTexture.GetCenteredUvRect(_mapTexture.CurrentPlayerTile, _minimapHalfSizeTiles);
    }

    private void UpdatePlayerMarkerRotation()
    {
        if (_playerMarker == null || _mapTexture == null)
            return;

        _playerMarker.localRotation = Quaternion.Euler(0f, 0f, _mapTexture.PlayerRotationZ);
    }

    private void RefreshMarkers()
    {
        if (_mapTexture == null || !_mapTexture.IsInitialized || _terrainImage == null)
            return;

        _markerViews.RefreshMarkers(_mapTexture.GroundTilemap, _mapTexture.WorldData, _terrainImage.uvRect, hideOutsideVisibleRect: true);
    }
}
