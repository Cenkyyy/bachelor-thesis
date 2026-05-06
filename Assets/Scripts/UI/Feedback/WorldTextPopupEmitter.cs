using System.Collections;
using System.Collections.Generic;
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
    [SerializeField, Min(0f)] private float _stackSpacing = 0.2f;
    [SerializeField, Min(1)] private int _maxActivePopups = 2;

    private float _nextAllowedShowTime;
    private readonly List<WordTextPopupRuntimeData> _activePopups = new();

    public bool HasActivePopup => _activePopups.Count > 0;

    private void Awake()
    {
        if (_fontAsset == null)
            _fontAsset = TMP_Settings.defaultFontAsset;
    }

    public void ShowMessage(string message)
    {
        ShowMessage(message, null, null);
    }

    public void ShowMessageAtWorldPosition(string message, Vector3 worldPosition)
    {
        ShowMessageAtWorldPosition(message, worldPosition, null, null);
    }

    public void ShowMessageAtWorldPosition(string message, Vector3 worldPosition, Color? colorOverride, float? cooldownOverrideSeconds)
    {
        ShowMessageInternal(message, colorOverride, cooldownOverrideSeconds, worldPosition, null);
    }

    public void ShowMessage(string message, Color? colorOverride, float? cooldownOverrideSeconds)
    {
        ShowMessageInternal(message, colorOverride, cooldownOverrideSeconds, null, transform);
    }

    private void ShowMessageInternal(string message, Color? colorOverride, float? cooldownOverrideSeconds, Vector3? worldPosition, Transform parent) 
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        var cooldown = cooldownOverrideSeconds ?? _cooldownSeconds;
        if (Time.time < _nextAllowedShowTime)
            return;

        _nextAllowedShowTime = Time.time + cooldown;

        if (_activePopups.Count >= _maxActivePopups)
            RemoveOldestPopupImmediately();

        // create an object to display the message at
        var textRoot = new GameObject("WorldPopupText");
        textRoot.layer = gameObject.layer;

        if (parent != null)
            textRoot.transform.SetParent(parent, false);

        if (worldPosition.HasValue)
            textRoot.transform.position = worldPosition.Value + _localOffset;

        // add the text component with basic settings
        var text = textRoot.AddComponent<TextMeshPro>();
        text.text = message;
        text.color = colorOverride ?? _textColor;
        text.fontSize = _fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.horizontalAlignment = HorizontalAlignmentOptions.Center;
        text.verticalAlignment = VerticalAlignmentOptions.Middle;
        text.textWrappingMode = TextWrappingModes.NoWrap;

        if (_fontAsset != null)
            text.font = _fontAsset;

        ApplySorting(text);

        var popup = new WordTextPopupRuntimeData(textRoot, text)
        {
            IsWorldSpace = worldPosition.HasValue
        };

        _activePopups.Add(popup);
        RefreshStackLayout();
        popup.Routine = StartCoroutine(ShowMessageRoutine(popup));
    }

    private IEnumerator ShowMessageRoutine(WordTextPopupRuntimeData popup)
    {
        var elapsed = 0f;
        var start = popup.StartLocalPosition;
        var end = start + new Vector3(0f, _riseDistance, 0f);
        var startColor = popup.Text.color;

        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / _duration);

            if (popup.Root == null || popup.Text == null)
                yield break;

            var position = Vector3.Lerp(start, end, t);
            if (popup.IsWorldSpace)
                popup.Root.transform.position = position;
            else
                popup.Root.transform.localPosition = position;

            var color = startColor;
            color.a = 1f - t;
            popup.Text.color = color;

            yield return null;
        }

        RemovePopup(popup);
    }

    private void RemoveOldestPopupImmediately()
    {
        if (_activePopups.Count == 0)
            return;

        // stop the first's popup coroutine and then remove it from the active popups
        var popup = _activePopups[0];
        if (popup.Routine != null)
            StopCoroutine(popup.Routine);

        RemovePopup(popup);
    }

    private void RemovePopup(WordTextPopupRuntimeData popup)
    {
        if (popup.Root != null)
            Destroy(popup.Root);

        _activePopups.Remove(popup);
        RefreshStackLayout();
    }

    private void RefreshStackLayout()
    {
        // based on the active popups, refresh each's popup local position
        for (var i = 0; i < _activePopups.Count; i++)
        {
            var popup = _activePopups[i];
            if (popup.Root == null)
                continue;

                        if (popup.IsWorldSpace)
            {
                popup.StartLocalPosition = popup.Root.transform.position + Vector3.up * (i * _stackSpacing);
                popup.Root.transform.position = popup.StartLocalPosition;
                continue;
            }

            var localPosition = _localOffset + Vector3.up * (i * _stackSpacing);
            popup.Root.transform.localPosition = localPosition;
            popup.StartLocalPosition = localPosition;
        }
    }

    private void ApplySorting(TextMeshPro text)
    {
        if (_inheritHostRendererSorting && TryGetComponent<Renderer>(out var hostRenderer))
        {
            text.sortingLayerID = hostRenderer.sortingLayerID;
            text.sortingOrder = hostRenderer.sortingOrder + _sortingOrderOffset;
            return;
        }

        var sortingLayerName = string.IsNullOrWhiteSpace(_sortingLayerName) ? "Default" : _sortingLayerName;
        text.sortingLayerID = SortingLayer.NameToID(sortingLayerName);
        text.sortingOrder = _sortingOrder;
    }

    private sealed class WordTextPopupRuntimeData
    {
        public GameObject Root { get; }
        public TextMeshPro Text { get; }
        public Coroutine Routine { get; set; }
        public Vector3 StartLocalPosition { get; set; }
        public bool IsWorldSpace { get; set; }

        public WordTextPopupRuntimeData(GameObject root, TextMeshPro text)
        {
            Root = root;
            Text = text;
            StartLocalPosition = Vector3.zero;
        }
    }
}
