using UnityEngine;
using UnityEngine.UI;

public sealed class CustomCursorController : MonoBehaviour
{
    public static CustomCursorController Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private CursorVisualSettingsData _settings;
    [SerializeField] private RectTransform _cursorRoot;
    [SerializeField] private Image _fillImage;
    [SerializeField] private Image _outlineImage;

    [Header("Offset in screen pixels")]
    [SerializeField] private Vector2 _screenOffset;

    private Color _currentFillColor;
    private float _currentScale;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _currentFillColor = _settings != null ? _settings.FillColor : Color.white;
        _currentScale = _settings != null ? _settings.Scale : 1f;

        ApplyCurrent();
    }

    private void OnEnable()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.None;

        if (_fillImage != null)
        {
            _fillImage.raycastTarget = false;
        }

        if (_outlineImage != null)
        {
            _outlineImage.raycastTarget = false;
        }

        ApplyCurrent();
        UpdatePosition();
    }

    private void OnDisable()
    {
        Cursor.visible = true;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
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
        if (_fillImage != null)
        {
            _fillImage.color = _currentFillColor;
        }

        if (_cursorRoot != null)
        {
            _cursorRoot.localScale = Vector3.one * _currentScale;
        }
    }

    private void UpdatePosition()
    {
        if (_cursorRoot == null)
            return;

        _cursorRoot.position = (Vector2)Input.mousePosition + _screenOffset;
    }
}
