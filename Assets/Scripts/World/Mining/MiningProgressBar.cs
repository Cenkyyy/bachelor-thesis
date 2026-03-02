using UnityEngine;

[DisallowMultipleComponent]
public sealed class MiningProgressBar : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private MineableNode _node;
    [SerializeField] private SpriteRenderer _backgroundRenderer;
    [SerializeField] private SpriteRenderer _fillRenderer;
    [SerializeField] private Transform _fillTransform;

    [Header("Visuals")]
    [SerializeField] private float _minFillScale = 0.02f;
    [SerializeField] private bool _hideWhenIdle = true;

    private void Awake()
    {
        SetVisible(!_hideWhenIdle);
    }

    private void OnEnable()
    {
        if (_node == null)
            return;

        _node.OnMiningProgressChanged += HandleProgressChanged;
        _node.OnMiningStopped += HandleMiningStopped;
    }

    private void OnDisable()
    {
        if (_node == null)
            return;

        _node.OnMiningProgressChanged -= HandleProgressChanged;
        _node.OnMiningStopped -= HandleMiningStopped;
    }

    private void HandleProgressChanged(float progress)
    {
        if (_hideWhenIdle)
            SetVisible(true);

        SetFill(progress);
    }

    private void HandleMiningStopped()
    {
        if (_hideWhenIdle)
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
