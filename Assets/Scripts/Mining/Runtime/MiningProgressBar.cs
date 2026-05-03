using UnityEngine;

[DisallowMultipleComponent]
public sealed class MiningProgressBar : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SpriteRenderer _backgroundRenderer;
    [SerializeField] private SpriteRenderer _fillRenderer;
    [SerializeField] private Transform _fillTransform;

    [Header("Visuals")]
    [SerializeField] private float _minFillScale = 0.02f;

    private void Awake()
    {
        SetVisible(false);
    }

    public void SetProgressValue(float progress)
    {
        SetVisible(true);
        SetFill(progress);
    }

    public void SetIdle() => SetVisible(false);
    public void SetWorldPosition(Vector3 worldPosition) => transform.position = worldPosition;

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
