using UnityEngine;

/// <summary>
/// View component responsible for rendering mining progress above a mineable target.
/// </summary>
[DisallowMultipleComponent]
public sealed class MiningProgressBarView : MonoBehaviour
{
    [Header("View References")]
    [SerializeField] private SpriteRenderer _backgroundRenderer;
    [SerializeField] private SpriteRenderer _fillRenderer;
    [SerializeField] private Transform _fillTransform;

    [Header("Visuals Settings")]
    [SerializeField] private float _minFillScale = 0.02f;

    private void Awake()
    {
        SetIdle();
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
        var clampedProgress = Mathf.Clamp01(progress);
        var fillScale = Mathf.Max(_minFillScale, clampedProgress);
        var localScale = _fillTransform.localScale;
        _fillTransform.localScale = new Vector3(fillScale, localScale.y, localScale.z);
    }

    private void SetVisible(bool isVisible)
    {
        _backgroundRenderer.enabled = isVisible;
        _fillRenderer.enabled = isVisible;
    }
}
