using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Persistent display service for UI scaling and camera size setting.
/// </summary>
[DisallowMultipleComponent]
public sealed class DisplaySettingsManager : MonoBehaviour
{
    private const int DefaultMinVirtualWidth = 480;
    private const int DefaultMinVirtualHeight = 270;
    private const int DefaultAssetsPPU = 32;
    private const int DefaultUiReferenceWidth = 640;
    private const int DefaultUiReferenceHeight = 360;

    public static DisplaySettingsManager Instance { get; private set; }

    [Header("Pixel View")]
    [SerializeField, Min(1)] private int _minVirtualWidth = DefaultMinVirtualWidth;
    [SerializeField, Min(1)] private int _minVirtualHeight = DefaultMinVirtualHeight;
    [SerializeField, Min(1)] private int _assetsPPU = DefaultAssetsPPU;
    [SerializeField, Min(1)] private int _pixelScale = 4;

    [Header("UI Scaling")]
    [SerializeField, Min(1)] private int _uiReferenceWidth = DefaultUiReferenceWidth;
    [SerializeField, Min(1)] private int _uiReferenceHeight = DefaultUiReferenceHeight;
    [SerializeField, Min(0.1f)] private float _minUiScale = 0.75f;
    [SerializeField, Min(0.1f)] private float _maxUiScale = 8f;
    [SerializeField, Min(0.01f)] private float _uiScaleStep = 0.25f;

    private int _lastScreenWidth;
    private int _lastScreenHeight;

    public int CurrentPixelScale => Mathf.Max(1, _pixelScale);
    public Vector2Int MinimumVirtualResolution => new(_minVirtualWidth, _minVirtualHeight);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;

        ApplyDisplayPolicy();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void LateUpdate()
    {
        bool sizeChanged = Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight;

        if (!sizeChanged)
            return;

        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;

        ApplyDisplayPolicy();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyDisplayPolicy();
    }

    private void ApplyDisplayPolicy()
    {
        ApplyCameraPolicy();
        ApplyCanvasPolicy();
    }

    private void ApplyCameraPolicy()
    {
        int pixelScale = CurrentPixelScale;
        float orthographicSize = CalculateOrthographicSize(Screen.width, Screen.height);

        ApplyCinemachinePolicy(orthographicSize);

        var cameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
            ApplyCameraPolicy(cameras[i], pixelScale, orthographicSize);
    }

    private void ApplyCanvasPolicy()
    {
        CalculateUiScalePolicy(Screen.width, Screen.height, out Vector2 referenceResolution, out float matchWidthOrHeight);
        var canvasScalers = FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < canvasScalers.Length; i++)
        {
            var canvasScaler = canvasScalers[i];
            if (canvasScaler == null)
                continue;

            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = referenceResolution;
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = matchWidthOrHeight;
        }
    }

    private void ApplyCinemachinePolicy(float orthographicSize)
    {
        var virtualCameras = FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < virtualCameras.Length; i++)
        {
            var virtualCamera = virtualCameras[i];
            var lens = virtualCamera.Lens;
            lens.OrthographicSize = orthographicSize;
            virtualCamera.Lens = lens;
        }
    }

    private void ApplyCameraPolicy(Camera camera, int pixelScale, float orthographicSize)
    {
        if (camera == null || !camera.orthographic || camera.targetTexture != null)
            return;

        camera.orthographicSize = orthographicSize;
        SnapCameraTransform(camera, pixelScale);
    }

    private void SnapCameraTransform(Camera camera, int pixelScale)
    {
        float unitsPerScreenPixel = 1f / (pixelScale * _assetsPPU);
        var position = camera.transform.position;
        position.x = Mathf.Round(position.x / unitsPerScreenPixel) * unitsPerScreenPixel;
        position.y = Mathf.Round(position.y / unitsPerScreenPixel) * unitsPerScreenPixel;
        camera.transform.position = position;
    }

    private float CalculateOrthographicSize(int screenWidth, int screenHeight)
    {
        float aspect = screenHeight > 0 ? screenWidth / (float)screenHeight : _minVirtualWidth / (float)_minVirtualHeight;
        float referenceAspect = _minVirtualWidth / (float)_minVirtualHeight;
        float visibleVirtualHeight = aspect >= referenceAspect ? _minVirtualHeight : _minVirtualWidth / aspect;
        return visibleVirtualHeight / _assetsPPU * 0.5f;
    }

    private void CalculateUiScalePolicy(int screenWidth, int screenHeight, out Vector2 referenceResolution, out float matchWidthOrHeight)
    {
        Vector2 visibleReferenceResolution = CalculateVisibleReferenceResolution(
            screenWidth,
            screenHeight,
            _uiReferenceWidth,
            _uiReferenceHeight);

        float widthScale = screenWidth / visibleReferenceResolution.x;
        float heightScale = screenHeight / visibleReferenceResolution.y;
        float unclampedScale = Mathf.Min(widthScale, heightScale);
        float minScale = Mathf.Min(_minUiScale, _maxUiScale);
        float maxScale = Mathf.Max(_minUiScale, _maxUiScale);
        float clampedScale = Mathf.Clamp(SnapScale(unclampedScale), minScale, maxScale);

        matchWidthOrHeight = 0f;
        referenceResolution = new Vector2(screenWidth / clampedScale, screenHeight / clampedScale);
    }

    private static Vector2 CalculateVisibleReferenceResolution(
        int screenWidth,
        int screenHeight,
        int referenceWidth,
        int referenceHeight)
    {
        float fallbackAspect = referenceWidth / (float)referenceHeight;
        float aspect = screenHeight > 0 ? screenWidth / (float)screenHeight : fallbackAspect;
        float referenceAspect = referenceWidth / (float)referenceHeight;

        if (aspect >= referenceAspect)
            return new Vector2(referenceHeight * aspect, referenceHeight);

        return new Vector2(referenceWidth, referenceWidth / aspect);
    }

    private float SnapScale(float scale)
    {
        float step = Mathf.Max(0.01f, _uiScaleStep);
        return Mathf.Round(scale / step) * step;
    }
}
