using UnityEngine;

public sealed class WorldMapPanelZoomController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private RectTransform _viewport;
    [SerializeField] private RectTransform _content;
    [SerializeField] private WorldMapPanelController _panel;

    [Header("Zoom")]
    [SerializeField] private float _zoomSpeed = 0.15f;
    [SerializeField] private float _maxZoom = 4f;

    [Header("Pan")]
    [SerializeField] private float _panSpeed = 1f;
    [SerializeField] private bool _panScalesWithZoom = true;

    private bool _dragging;
    private Vector2 _dragStartMouse;
    private Vector2 _dragStartContentPos;

    private float _minZoomRuntime = 1f;
    private float _maxZoomRuntime = 4f;

    private void Update()
    {
        if (!_panel.IsOpen)
        {
            _dragging = false;
            return;
        }

        var cam = GetCanvasCamera();
        bool pointerInViewport = RectTransformUtility.RectangleContainsScreenPoint(_viewport, Input.mousePosition, cam);

        if (!pointerInViewport && !_dragging)
        {
            return;
        }

        if (pointerInViewport)
            HandleZoom(cam);

        HandlePan(cam);
    }

    public void FitToViewport()
    {
        Canvas.ForceUpdateCanvases();

        _content.localScale = Vector3.one;
        _content.anchoredPosition = Vector2.zero;

        var viewportSize = _viewport.rect.size;
        var contentSize = _content.rect.size;

        float fit = Mathf.Min(viewportSize.x / contentSize.x, viewportSize.y / contentSize.y);
        fit = Mathf.Clamp(fit, 0.01f, 999f);

        _minZoomRuntime = fit;
        _maxZoomRuntime = Mathf.Max(_maxZoom, _minZoomRuntime);

        _content.localScale = new Vector3(fit, fit, 1f);
        _content.anchoredPosition = Vector2.zero;

        ClampContentToViewport();
    }

    private void HandleZoom(Camera cam)
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) < 0.001f)
            return;

        float oldZoom = _content.localScale.x;
        float targetZoom = oldZoom * (1f + scroll * _zoomSpeed);
        float newZoom = Mathf.Clamp(targetZoom, _minZoomRuntime, _maxZoomRuntime);

        if (Mathf.Abs(newZoom - oldZoom) < 0.0001f)
            return;

        float ratio = newZoom / oldZoom;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _viewport, Input.mousePosition, cam, out var mouseLocal);

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

        float zoom = Mathf.Max(_content.localScale.x, 0.0001f);
        float panMultiplier = _panScalesWithZoom ? 1f / zoom : 1f;
        _content.anchoredPosition = _dragStartContentPos + mouseDelta * (_panSpeed * panMultiplier);

        ClampContentToViewport();
    }

    private void ClampContentToViewport()
    {
        float zoom = _content.localScale.x;

        var viewportSize = _viewport.rect.size;
        var contentSize = _content.rect.size * zoom;

        float maxX = Mathf.Max(0f, (contentSize.x - viewportSize.x) * 0.5f);
        float maxY = Mathf.Max(0f, (contentSize.y - viewportSize.y) * 0.5f);

        var p = _content.anchoredPosition;
        p.x = Mathf.Clamp(p.x, -maxX, maxX);
        p.y = Mathf.Clamp(p.y, -maxY, maxY);
        _content.anchoredPosition = p;
    }

    private Camera GetCanvasCamera()
    {
        if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return _canvas.worldCamera;
    }
}
