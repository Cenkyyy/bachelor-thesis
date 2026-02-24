using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public sealed class WorldMapPanelController : MonoBehaviour, IMajorPanel
{
    [Header("UI")]
    [SerializeField] private GameObject _root;
    [SerializeField] private RectTransform _mapContent;
    [SerializeField] private RawImage _terrainImage;
    [SerializeField] private RawImage _fogImage;
    [SerializeField] private RectTransform _playerMarker;

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

    private void LateUpdate()
    {
        if (!IsOpen)
            return;

        TryBindTextures();

        if (!_minimap.IsInitialized)
            return;

        UpdatePlayerMarker();
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
        _fogImage.texture = _minimap.FogTexture;

        _terrainImage.uvRect = new Rect(0f, 0f, 1f, 1f);
        _fogImage.uvRect = new Rect(0f, 0f, 1f, 1f);

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
        _fitAppliedThisOpen = true;
    }

    private void UpdatePlayerMarker()
    {
        Vector2 dataPos = GetPlayerDataPositionContinuous();

        var contentSize = _mapContent.rect.size;
        var contentOffset = new Vector2(-contentSize.x * _mapContent.pivot.x, -contentSize.y * _mapContent.pivot.y);

        _playerMarker.anchorMin = _playerMarker.anchorMax = new Vector2(0.5f, 0.5f);
        _playerMarker.anchoredPosition = contentOffset + dataPos;

        float z = _playerTransform.eulerAngles.z;
        _playerMarker.localRotation = Quaternion.Euler(0f, 0f, z);
    }

    private Vector2 GetPlayerDataPositionContinuous()
    {
        var grid = _groundTilemap.layoutGrid;
        Vector3 playerLocalInGrid = grid.transform.InverseTransformPoint(_playerTransform.position);
        Vector3 cellPos = grid.LocalToCellInterpolated(playerLocalInGrid);

        float dataX = cellPos.x - _minimap.WorldData.OffsetX;
        float dataY = cellPos.y - _minimap.WorldData.OffsetY;

        float clampedX = Mathf.Clamp(dataX, 0f, _minimap.WorldData.Width);
        float clampedY = Mathf.Clamp(dataY, 0f, _minimap.WorldData.Height);

        return new Vector2(clampedX, clampedY);
    }
}
