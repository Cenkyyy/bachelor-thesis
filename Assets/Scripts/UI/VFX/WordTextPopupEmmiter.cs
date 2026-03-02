using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WorldTextPopupEmitter : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Vector3 _localOffset = new(0f, 0.5f, 0f);
    [SerializeField] private Color _textColor = Color.white;
    [SerializeField] private float _fontSize = 2.5f;
    [SerializeField] private TMP_FontAsset _fontAsset;

    [Header("Sorting")]
    [SerializeField] private bool _inheritHostRendererSorting = true;
    [SerializeField] private int _sortingOrderOffset = 10;
    [SerializeField] private string _sortingLayerName = "Default";
    [SerializeField] private int _sortingOrder = 50;

    [Header("Animation")]
    [SerializeField] private float _duration = 1.2f;
    [SerializeField] private float _riseDistance = 0.5f;
    [SerializeField] private float _cooldownSeconds = 2.5f;

    private float _nextAllowedShowTime;

    private void Awake()
    {
        if (_fontAsset == null)
            _fontAsset = TMP_Settings.defaultFontAsset;
    }

    public void ShowMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (Time.time < _nextAllowedShowTime)
            return;

        _nextAllowedShowTime = Time.time + Mathf.Max(0f, _cooldownSeconds);
        StartCoroutine(ShowMessageRoutine(message));
    }

    private IEnumerator ShowMessageRoutine(string message)
    {
        var textRoot = new GameObject("WorldPopupText");
        textRoot.transform.SetParent(transform, false);
        textRoot.transform.localPosition = _localOffset;
        textRoot.layer = gameObject.layer;

        var text = textRoot.AddComponent<TextMeshPro>();
        text.text = message;
        text.color = _textColor;
        text.fontSize = _fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.horizontalAlignment = HorizontalAlignmentOptions.Center;
        text.verticalAlignment = VerticalAlignmentOptions.Middle;
        text.textWrappingMode = TextWrappingModes.NoWrap;

        if (_fontAsset != null)
            text.font = _fontAsset;

        if (_inheritHostRendererSorting && TryGetComponent<Renderer>(out var hostRenderer))
        {
            text.sortingLayerID = hostRenderer.sortingLayerID;
            text.sortingOrder = hostRenderer.sortingOrder + _sortingOrderOffset;
        }
        else
        {
            var sortingLayerName = string.IsNullOrWhiteSpace(_sortingLayerName) ? "Default" : _sortingLayerName;
            text.sortingLayerID = SortingLayer.NameToID(sortingLayerName);
            text.sortingOrder = _sortingOrder;
        }

        var elapsed = 0f;
        var start = textRoot.transform.localPosition;
        var end = start + new Vector3(0f, _riseDistance, 0f);

        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / _duration);

            textRoot.transform.localPosition = Vector3.Lerp(start, end, t);

            var color = _textColor;
            color.a = 1f - t;
            text.color = color;

            yield return null;
        }

        Destroy(textRoot);
    }
}
