using UnityEngine;

[DisallowMultipleComponent]
public sealed class MiningProgressBar : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PrefabMineableRuntimeData _data;
    [SerializeField] private SpriteRenderer _backgroundRenderer;
    [SerializeField] private SpriteRenderer _fillRenderer;
    [SerializeField] private Transform _fillTransform;

    [Header("Visuals")]
    [SerializeField] private float _minFillScale = 0.02f;

    private float _lastProgress;

    private void Awake()
    {
        SetVisible(false);
    }

    private void OnEnable()
    {
        if (_data == null)
            return;

        _data.OnMiningProgressChanged += HandleProgressChanged;
        _data.OnMiningStopped += HandleMiningStopped;
    }

    private void OnDisable()
    {
        if (_data == null)
            return;

        _data.OnMiningProgressChanged -= HandleProgressChanged;
        _data.OnMiningStopped -= HandleMiningStopped;
    }

    private void HandleProgressChanged(float progress)
    {
        _lastProgress = Mathf.Clamp01(progress);
        SetProgressValue(progress);
    }

    private void HandleMiningStopped()
    {
        if (_lastProgress > Mathf.Epsilon)
        {
            SetProgressValue(_lastProgress);
            return;
        }

        SetIdle();
    }

    public void SetProgressValue(float progress)
    {
        SetVisible(true);
        SetFill(progress);
    }

    public void SetIdle()
    {
        SetVisible(false);
    }

    private void SetFill(float progress)
    {
        var clamped = Mathf.Clamp01(progress);
        var scale = Mathf.Max(_minFillScale, clamped);
        var local = _fillTransform.localScale;
        _fillTransform.localScale = new Vector3(scale, local.y, local.z);
    }

    private void SetVisible(bool visible)
    {
        _backgroundRenderer.enabled = visible;
        _fillRenderer.enabled = visible;
    }
}
