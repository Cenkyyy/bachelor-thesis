using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DisplaySettingsManager : MonoBehaviour
{
    private const string FullscreenPrefsKey = "Display.Fullscreen";
    private const string WindowWidthPrefsKey = "Display.WindowWidth";
    private const string WindowHeightPrefsKey = "Display.WindowHeight";

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

    [Header("Windowed Mode")]
    [SerializeField, Min(1)] private int _minWindowWidth = DefaultMinVirtualWidth;
    [SerializeField, Min(1)] private int _minWindowHeight = DefaultMinVirtualHeight;

    [Header("UI Scaling")]
    [SerializeField, Min(1)] private int _uiReferenceWidth = DefaultUiReferenceWidth;
    [SerializeField, Min(1)] private int _uiReferenceHeight = DefaultUiReferenceHeight;
    [SerializeField, Min(0.1f)] private float _minUiScale = 0.75f;
    [SerializeField, Min(0.1f)] private float _maxUiScale = 4f;
    [SerializeField, Min(0.01f)] private float _uiScaleStep = 0.25f;

    private int _lastScreenWidth;
    private int _lastScreenHeight;
    private bool _isFullscreen;

    public bool IsFullscreen => _isFullscreen;
    public int CurrentPixelScale => Mathf.Max(1, _pixelScale);
    public Vector2Int MinimumVirtualResolution => new(_minVirtualWidth, _minVirtualHeight);

    public static bool GetFullscreen()
    {
        return Instance != null ? Instance.IsFullscreen : Screen.fullScreen;
    }

    public static void SetFullscreen(bool isFullscreen)
    {
        if (Instance != null)
        {
            Instance.ApplyFullscreen(isFullscreen);
            return;
        }

        Screen.SetResolution(Screen.width, Screen.height, isFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _isFullscreen = PlayerPrefs.GetInt(FullscreenPrefsKey, Screen.fullScreen ? 1 : 0) != 0;
        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;

        ApplyFullscreen(_isFullscreen, shouldPersist: false);
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
        EnforceWindowedMinimumSize();

        if (Screen.width == _lastScreenWidth && Screen.height == _lastScreenHeight)
            return;

        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;

        if (!_isFullscreen)
            SaveWindowedSize(Screen.width, Screen.height);

        ApplyDisplayPolicy();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyDisplayPolicy();
    }

    private void ApplyFullscreen(bool isFullscreen, bool shouldPersist = true)
    {
        if (!_isFullscreen && isFullscreen)
            SaveWindowedSize(Screen.width, Screen.height);

        _isFullscreen = isFullscreen;

        if (shouldPersist)
        {
            PlayerPrefs.SetInt(FullscreenPrefsKey, _isFullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        if (_isFullscreen)
        {
            var resolution = Screen.currentResolution;
            int width = Mathf.Max(resolution.width, _minWindowWidth);
            int height = Mathf.Max(resolution.height, _minWindowHeight);
            Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            int width = Mathf.Max(PlayerPrefs.GetInt(WindowWidthPrefsKey, Screen.width), _minWindowWidth);
            int height = Mathf.Max(PlayerPrefs.GetInt(WindowHeightPrefsKey, Screen.height), _minWindowHeight);
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
        }

        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;
        ApplyDisplayPolicy();
    }

    private void ApplyDisplayPolicy()
    {
        ApplyCameraPolicy();
        ApplyCanvasPolicy();
    }

    private void EnforceWindowedMinimumSize()
    {
        if (_isFullscreen || Screen.fullScreen)
            return;

        int width = Mathf.Max(Screen.width, _minWindowWidth);
        int height = Mathf.Max(Screen.height, _minWindowHeight);

        if (width == Screen.width && height == Screen.height)
            return;

        Screen.SetResolution(width, height, FullScreenMode.Windowed);
        SaveWindowedSize(width, height);
    }

    private void SaveWindowedSize(int width, int height)
    {
        PlayerPrefs.SetInt(WindowWidthPrefsKey, Mathf.Max(width, _minWindowWidth));
        PlayerPrefs.SetInt(WindowHeightPrefsKey, Mathf.Max(height, _minWindowHeight));
    }

    private void ApplyCameraPolicy()
    {
        int pixelScale = CurrentPixelScale;
        float orthographicSize = CalculateOrthographicSize(Screen.height, pixelScale);

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

        if (camera.TryGetComponent(out PixelPerfectCamera pixelPerfectCamera))
        {
            pixelPerfectCamera.assetsPPU = _assetsPPU;
            pixelPerfectCamera.refResolutionX = _minVirtualWidth;
            pixelPerfectCamera.refResolutionY = _minVirtualHeight;
            pixelPerfectCamera.cropFrame = PixelPerfectCamera.CropFrame.None;
            pixelPerfectCamera.gridSnapping = PixelPerfectCamera.GridSnapping.None;
            return;
        }

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

    private float CalculateOrthographicSize(int screenHeight, int pixelScale)
    {
        float visibleVirtualHeight = screenHeight / (float)pixelScale;
        return visibleVirtualHeight / _assetsPPU * 0.5f;
    }

    private void CalculateUiScalePolicy(int screenWidth, int screenHeight, out Vector2 referenceResolution, out float matchWidthOrHeight)
    {
        float widthScale = screenWidth / (float)_uiReferenceWidth;
        float heightScale = screenHeight / (float)_uiReferenceHeight;
        float unclampedScale = Mathf.Min(widthScale, heightScale);
        float minScale = Mathf.Min(_minUiScale, _maxUiScale);
        float maxScale = Mathf.Max(_minUiScale, _maxUiScale);
        float clampedScale = Mathf.Clamp(SnapScale(unclampedScale), minScale, maxScale);

        if (widthScale <= heightScale)
        {
            matchWidthOrHeight = 0f;
            referenceResolution = new Vector2(screenWidth / clampedScale, _uiReferenceHeight);
            return;
        }

        matchWidthOrHeight = 1f;
        referenceResolution = new Vector2(_uiReferenceWidth, screenHeight / clampedScale);
    }

    private float SnapScale(float scale)
    {
        float step = Mathf.Max(0.01f, _uiScaleStep);
        return Mathf.Round(scale / step) * step;
    }
}
