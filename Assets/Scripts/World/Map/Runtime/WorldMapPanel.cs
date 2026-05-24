using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen world map panel that displays the shared map texture with pan, zoom, and markers.
/// </summary>
public sealed class WorldMapPanel : MonoBehaviour, IMajorPanel, IMapMarkerView
{
    [Header("Panel Root")]
    [SerializeField] private GameObject _root;

    [Header("View References")]
    [SerializeField] private RectTransform _mapContent;
    [SerializeField] private RawImage _terrainImage;
    [SerializeField] private RectTransform _playerMarker;
    [SerializeField] private RectTransform _markerContainer;
    [SerializeField] private RectTransform _markerPrefab;

    [Header("Dependencies")]
    [SerializeField] private MapTextureController _mapTexture;
    [SerializeField] private WorldMapPanelZoomController _zoom;

    public PanelId Id => PanelId.Map;
    public bool IsOpen => _root != null && _root.activeSelf;
    public bool PausesGame => false;
    public bool BlocksGameplayInput => true;

    private bool _texturesBound;
    private bool _fitAppliedThisOpen;
    private MapMarkerViewController _markerViews;

    private void Awake()
    {
        _markerViews = new MapMarkerViewController(this, _markerContainer, _mapContent, _markerPrefab);

        if (_root != null)
            _root.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!IsOpen)
            return;

        TryBindTexture();

        if (_mapTexture == null || !_mapTexture.IsInitialized)
            return;

        UpdatePlayerMarker();
        RefreshMarkers();
    }

    /// <summary>
    /// Adds a marker to the full world map view.
    /// </summary>
    public void AddMarker(MapMarkerRuntimeState marker)
    {
        _markerViews.AddMarker(marker);
        RefreshMarkers();
    }

    /// <summary>
    /// Updates a marker on the full world map view.
    /// </summary>
    public void UpdateMarker(MapMarkerRuntimeState marker)
    {
        _markerViews.UpdateMarker(marker);
        RefreshMarkers();
    }

    /// <summary>
    /// Removes a marker from the full world map view.
    /// </summary>
    public void RemoveMarker(string markerId)
    {
        _markerViews.RemoveMarker(markerId);
    }

    public void Open()
    {
        if (_root != null)
            _root.SetActive(true);

        _texturesBound = false;
        _fitAppliedThisOpen = false;
        TryBindTexture();
    }

    public void Close()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    private void TryBindTexture()
    {
        if (_texturesBound || _mapTexture == null || !_mapTexture.IsInitialized || _terrainImage == null || _mapContent == null)
            return;

        _terrainImage.texture = _mapTexture.TerrainTexture;
        _terrainImage.uvRect = new Rect(0f, 0f, 1f, 1f);

        if (_terrainImage.texture != null)
            _mapContent.sizeDelta = new Vector2(_terrainImage.texture.width, _terrainImage.texture.height);

        _texturesBound = true;
        ApplyInitialFit();
    }

    private void ApplyInitialFit()
    {
        if (_fitAppliedThisOpen || _zoom == null)
            return;

        Canvas.ForceUpdateCanvases();
        _zoom.FitToViewport();
        CenterMapOnPlayer();
        _fitAppliedThisOpen = true;
    }

    private void CenterMapOnPlayer()
    {
        if (_mapTexture == null || _zoom == null)
            return;

        _zoom.CenterOnNormalizedPosition(_mapTexture.GetPlayerPositionNormalized());
    }

    private void UpdatePlayerMarker()
    {
        if (_playerMarker == null || _mapContent == null || _mapTexture == null)
            return;

        Vector2 normalizedPos = _mapTexture.GetPlayerPositionNormalized();
        var contentSize = _mapContent.rect.size;
        var contentPivot = _mapContent.pivot;
        float localX = (normalizedPos.x - contentPivot.x) * contentSize.x;
        float localY = (normalizedPos.y - contentPivot.y) * contentSize.y;

        _playerMarker.anchorMin = new Vector2(0.5f, 0.5f);
        _playerMarker.anchorMax = new Vector2(0.5f, 0.5f);
        _playerMarker.anchoredPosition = new Vector2(localX, localY);
        _playerMarker.localRotation = Quaternion.Euler(0f, 0f, _mapTexture.PlayerRotationZ);
    }

    private void RefreshMarkers()
    {
        if (_mapTexture == null || !_mapTexture.IsInitialized)
            return;

        _markerViews.RefreshMarkers(_mapTexture.GroundTilemap, _mapTexture.WorldData, new Rect(0f, 0f, 1f, 1f), hideOutsideVisibleRect: false);
    }
}
