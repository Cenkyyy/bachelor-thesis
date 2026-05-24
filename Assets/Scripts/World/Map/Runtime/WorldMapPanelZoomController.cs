using UnityEngine;

/// <summary>
/// Handles zooming, panning, and clamping for the full-screen world map panel.
/// </summary>
public sealed class WorldMapPanelZoomController : MonoBehaviour
{
    [Header("View References")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private RectTransform _viewport;
    [SerializeField] private RectTransform _content;
    [SerializeField] private WorldMapPanel _panel;

    [Header("Zoom Settings")]
    [SerializeField] private float _zoomSpeed = 0.15f;
    [SerializeField, Min(1f)] private float _maxZoom = 20f;
    [SerializeField, Min(1f)] private float _startZoomPercent = 1000f;

    [Header("Pan Settings")]
    [SerializeField] private float _panSpeed = 1f;
    [SerializeField] private bool _panScalesWithZoom = true;

    private bool _dragging;
    private Vector2 _dragStartMouse;
    private Vector2 _dragStartContentPos;
    private float _minZoomRuntime = 1f;
    private float _maxZoomRuntime = 4f;

    private void Update()
    {
        if (_panel == null || !_panel.IsOpen)
        {
            _dragging = false;
            return;
        }

        if (_viewport == null || _content == null)
            return;

        var cam = GetCanvasCamera();
        bool pointerInViewport = RectTransformUtility.RectangleContainsScreenPoint(_viewport, Input.mousePosition, cam);
        if (!pointerInViewport && !_dragging)
            return;

        if (pointerInViewport)
            HandleZoom(cam);

        HandlePan(cam);
    }

    /// <summary>
    /// Fits the map content into the viewport and applies the configured starting zoom.
    /// </summary>
    public void FitToViewport()
    {
        if (_viewport == null || _content == null)
            return;

        Canvas.ForceUpdateCanvases();

        _content.localScale = Vector3.one;
        _content.anchoredPosition = Vector2.zero;

        var viewportSize = _viewport.rect.size;
        var contentSize = _content.rect.size;
        float fit = Mathf.Min(viewportSize.x / contentSize.x, viewportSize.y / contentSize.y);
        fit = Mathf.Clamp(fit, 0.01f, 999f);

        _minZoomRuntime = fit;
        _maxZoomRuntime = _minZoomRuntime * _maxZoom;

        ApplyStartZoom();
        ClampContentToViewport();
    }

    /// <summary>
    /// Centers the map content on a normalized texture position.
    /// </summary>
    public void CenterOnNormalizedPosition(Vector2 normalizedPosition)
    {
        if (_content == null)
            return;

        var contentSize = _content.rect.size;
        var contentPivot = _content.pivot;
        float zoom = _content.localScale.x;
        float localX = (normalizedPosition.x - contentPivot.x) * contentSize.x;
        float localY = (normalizedPosition.y - contentPivot.y) * contentSize.y;

        _content.anchoredPosition = new Vector2(-localX * zoom, -localY * zoom);
        ClampContentToViewport();
    }

    private void ApplyStartZoom()
    {
        float zoomMultiplier = Mathf.Max(0.01f, _startZoomPercent * 0.01f);
        float startZoom = _minZoomRuntime * zoomMultiplier;
        float clampedZoom = Mathf.Clamp(startZoom, _minZoomRuntime, _maxZoomRuntime);

        _content.localScale = new Vector3(clampedZoom, clampedZoom, 1f);
        _content.anchoredPosition = Vector2.zero;
    }

    private void HandleZoom(Camera cam)
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) < Mathf.Epsilon)
            return;

        float oldZoom = _content.localScale.x;
        float targetZoom = oldZoom * (1f + scroll * _zoomSpeed);
        float newZoom = Mathf.Clamp(targetZoom, _minZoomRuntime, _maxZoomRuntime);
        if (Mathf.Abs(newZoom - oldZoom) < Mathf.Epsilon)
            return;

        float ratio = newZoom / oldZoom;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _viewport,
            Input.mousePosition,
            cam,
            out var mouseLocal);

        var pos = _content.anchoredPosition;
        _content.localScale = new Vector3(newZoom, newZoom, 1f);
        pos = mouseLocal + (pos - mouseLocal) * ratio;
        _content.anchoredPosition = pos;

        ClampContentToViewport();
    }

    private void HandlePan(Camera cam)
    {
        if (Input.GetMouseButtonDown(0))
        {
            _dragging = true;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, Input.mousePosition, cam, out _dragStartMouse);
            _dragStartContentPos = _content.anchoredPosition;
        }

        if (Input.GetMouseButtonUp(0))
            _dragging = false;

        if (!_dragging)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, Input.mousePosition, cam, out var mouseLocal);
        Vector2 mouseDelta = mouseLocal - _dragStartMouse;
        float zoom = Mathf.Max(_content.localScale.x, Mathf.Epsilon);
        float panMultiplier = _panScalesWithZoom ? 1f / zoom : 1f;
        _content.anchoredPosition = _dragStartContentPos + mouseDelta * (_panSpeed * panMultiplier);

        ClampContentToViewport();
    }

    private void ClampContentToViewport()
    {
        if (_viewport == null || _content == null)
            return;

        float zoom = _content.localScale.x;
        var viewportSize = _viewport.rect.size;
        var contentSize = _content.rect.size * zoom;
        float maxX = Mathf.Max(0f, (contentSize.x - viewportSize.x) * 0.5f);
        float maxY = Mathf.Max(0f, (contentSize.y - viewportSize.y) * 0.5f);
        var position = _content.anchoredPosition;

        position.x = Mathf.Clamp(position.x, -maxX, maxX);
        position.y = Mathf.Clamp(position.y, -maxY, maxY);
        _content.anchoredPosition = position;
    }

    private Camera GetCanvasCamera()
    {
        if (_canvas == null || _canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return _canvas.worldCamera;
    }
}
