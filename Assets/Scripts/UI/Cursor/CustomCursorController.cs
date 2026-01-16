using UnityEngine;
using UnityEngine.UI;

public sealed class CustomCursorController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CursorVisualSettings _settings;
    [SerializeField] private RectTransform _cursorRoot;
    [SerializeField] private Image _fillImage;
    [SerializeField] private Image _outlineImage;

    [Header("Offset in screen pixels")]
    [SerializeField] private Vector2 _screenOffset;

    private Color _currentFillColor;
    private float _currentScale;

    private void Awake()
    {
        _currentFillColor = _settings.FillColor;
        _currentScale = _settings.Scale;
    }

    private void OnEnable()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.None;

        _fillImage.raycastTarget = false;
        _outlineImage.raycastTarget = false;

        ApplyCurrent();
        UpdatePosition();
    }

    private void OnDisable()
    {
        Cursor.visible = true;
    }

    private void Update()
    {
        UpdatePosition();
    }

    public Color GetCurrentFillColor() => _currentFillColor;

    public void ApplyFillColor(Color color)
    {
        _currentFillColor = color;
        ApplyCurrent();
    }

    private void ApplyCurrent()
    {
        _fillImage.color = _currentFillColor;
        _outlineImage.color = Color.black;
        _cursorRoot.localScale = Vector3.one * _currentScale;
    }

    private void UpdatePosition()
    {
        _cursorRoot.position = (Vector2)Input.mousePosition + _screenOffset;
    }
}
