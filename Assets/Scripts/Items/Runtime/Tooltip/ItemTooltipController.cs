using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared UI controller that shows item tooltips for inventory slots, crafting entries, and other item sources.
/// </summary>
public sealed class ItemTooltipController : MonoBehaviour
{
    [Header("View")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private RectTransform _panelRoot;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _rarityText;
    [SerializeField] private TMP_Text _bodyText;

    [Header("Behavior")]
    [SerializeField, Range(0.1f, 3f)] private float _hoverDelaySeconds = 0.75f;

    [Header("Runtime Dependencies")]
    [SerializeField] private PlayerToolDurabilityRuntimeState _playerToolDurability;

    private readonly List<IItemTooltipProvider> _providers = new List<IItemTooltipProvider>();

    private Coroutine _hoverCoroutine;
    private IItemTooltipSource _currentHoveredSource;
    private RectTransform _currentHoveredAnchor;
    private InventorySlotView _currentHoveredSlotContext;
    private InventoryItem _currentHoveredItem;

    private void Awake()
    {
        Hide();
    }

    private void OnDisable()
    {
        ClearHoverState();
    }

    /// <summary>
    /// Starts the tooltip hover flow for an inventory slot.
    /// </summary>
    public void OnSlotPointerEnter(InventorySlotView slot)
    {
        OnTooltipSourcePointerEnter(slot);
    }

    /// <summary>
    /// Stops the tooltip hover flow for an inventory slot.
    /// </summary>
    public void OnSlotPointerExit(InventorySlotView slot)
    {
        OnTooltipSourcePointerExit(slot);
    }

    /// <summary>
    /// Starts the tooltip hover flow for an item that is not backed by an inventory slot.
    /// </summary>
    public void OnStandaloneItemPointerEnter(RectTransform hoverTarget, InventoryItem inventoryItem)
    {
        if (hoverTarget == null || inventoryItem.IsEmpty || inventoryItem.Item == null)
        {
            ClearHoverState();
            return;
        }

        _currentHoveredSource = null;
        _currentHoveredAnchor = hoverTarget;
        _currentHoveredSlotContext = null;
        _currentHoveredItem = inventoryItem;
        RestartHoverCountdown(null, hoverTarget);
    }

    public void OnStandaloneItemPointerExit(RectTransform hoverTarget)
    {
        if (_currentHoveredSource != null || hoverTarget == null || !ReferenceEquals(_currentHoveredAnchor, hoverTarget))
            return;

        ClearHoverState();
    }

    public void OnSlotDisabled(InventorySlotView slot)
    {
        if (!ReferenceEquals(_currentHoveredSource, slot))
            return;

        ClearHoverState();
    }

    /// <summary>
    /// Starts the tooltip hover flow for any object that can provide item tooltip data.
    /// </summary>
    public void OnTooltipSourcePointerEnter(IItemTooltipSource source)
    {
        if (!TryResolveTooltipData(source, out var slotContext, out var inventoryItem))
        {
            ClearHoverState();
            return;
        }

        _currentHoveredSource = source;
        _currentHoveredAnchor = source.TooltipAnchor;
        _currentHoveredSlotContext = slotContext;
        _currentHoveredItem = inventoryItem;
        RestartHoverCountdown(source, _currentHoveredAnchor);
    }

    /// <summary>
    /// Stops the tooltip hover flow when the pointer leaves the current tooltip source.
    /// </summary>
    public void OnTooltipSourcePointerExit(IItemTooltipSource source)
    {
        if (!ReferenceEquals(_currentHoveredSource, source))
            return;

        ClearHoverState();
    }

    private static bool TryResolveTooltipData(IItemTooltipSource source, out InventorySlotView slotContext, out InventoryItem inventoryItem)
    {
        slotContext = null;
        inventoryItem = InventoryItem.Empty;

        if (source == null || source.TooltipAnchor == null)
            return false;

        return source.TryGetTooltipData(out slotContext, out inventoryItem) && !inventoryItem.IsEmpty && inventoryItem.Item != null;
    }

    private void ClearHoverState()
    {
        CancelPendingHover();
        Hide();
        _currentHoveredSource = null;
        _currentHoveredAnchor = null;
        _currentHoveredSlotContext = null;
        _currentHoveredItem = InventoryItem.Empty;
    }

    private void CancelPendingHover()
    {
        if (_hoverCoroutine == null)
            return;

        StopCoroutine(_hoverCoroutine);
        _hoverCoroutine = null;
    }

    private void Hide()
    {
        if (_panelRoot != null)
            _panelRoot.gameObject.SetActive(false);
    }

    private void RegisterProviders()
    {
        if (_providers.Count > 0)
            return;

        _providers.AddRange(ItemTooltipProviderFactory.CreateDefault(_playerToolDurability));
    }

    private void RestartHoverCountdown(IItemTooltipSource source, RectTransform anchor)
    {
        CancelPendingHover();
        _hoverCoroutine = StartCoroutine(ShowAfterDelayCoroutine(source, anchor));
    }

    private IEnumerator ShowAfterDelayCoroutine(IItemTooltipSource source, RectTransform anchor)
    {
        yield return new WaitForSeconds(_hoverDelaySeconds);

        if (source != null)
        {
            if (!ReferenceEquals(_currentHoveredSource, source))
                yield break;
        }
        else
        {
            if (_currentHoveredSource != null || !ReferenceEquals(_currentHoveredAnchor, anchor))
                yield break;
        }

        if (_currentHoveredItem.IsEmpty || _currentHoveredItem.Item == null || _currentHoveredAnchor == null)
            yield break;

        Show(_currentHoveredAnchor, _currentHoveredSlotContext, _currentHoveredItem);
    }

    private void Show(RectTransform anchor, InventorySlotView slotContext, InventoryItem inventoryItem)
    {
        if (_panelRoot == null)
            return;

        var tooltipRuntimeData = BuildRuntimeData(slotContext, inventoryItem);
        Render(tooltipRuntimeData);
        PositionNearRectTransform(anchor);

        _panelRoot.gameObject.SetActive(true);
    }

    private ItemTooltipRuntimeData BuildRuntimeData(InventorySlotView slot, InventoryItem slotItem)
    {
        RegisterProviders();

        var itemData = slotItem.Item;
        var runtimeData = new ItemTooltipRuntimeData
        {
            Title = itemData.ItemName,
            Rarity = ItemTooltipFormatter.FormatRarity(itemData.Rarity)
        };

        for (int i = 0; i < _providers.Count; i++)
        {
            var provider = _providers[i];
            if (!provider.CanHandle(itemData))
                continue;

            provider.AppendLines(slot, slotItem, runtimeData.Lines);
        }

        return runtimeData;
    }

    private void Render(ItemTooltipRuntimeData runtimeData)
    {
        if (_titleText != null)
            _titleText.text = runtimeData?.Title ?? string.Empty;

        if (_rarityText != null)
            _rarityText.text = runtimeData?.Rarity ?? string.Empty;

        if (_bodyText != null)
            _bodyText.text = ItemTooltipBodyBuilder.BuildBody(runtimeData?.Lines);
    }

    private void PositionNearRectTransform(RectTransform targetTransform)
    {
        if (_panelRoot == null || targetTransform == null || _canvas == null)
            return;

        var worldCorners = new Vector3[4];
        targetTransform.GetWorldCorners(worldCorners);

        var topRight = worldCorners[2];
        var screenTopRight = RectTransformUtility.WorldToScreenPoint(_canvas.worldCamera, topRight);

        var canvasRect = _canvas.transform as RectTransform;
        if (canvasRect == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenTopRight, _canvas.worldCamera, out var localPoint))
            return;

        // force layout before clamping so rect has proper size
        LayoutRebuilder.ForceRebuildLayoutImmediate(_panelRoot);

        var clamped = ClampToCanvas(localPoint, canvasRect, _panelRoot);
        _panelRoot.anchoredPosition = clamped;
    }

    private static Vector2 ClampToCanvas(Vector2 desired, RectTransform canvasRect, RectTransform panel)
    {
        var panelRect = panel.rect;
        var canvasRectSize = canvasRect.rect;

        float minX = canvasRectSize.xMin - panelRect.xMin;
        float maxX = canvasRectSize.xMax - panelRect.xMax;
        float minY = canvasRectSize.yMin - panelRect.yMin;
        float maxY = canvasRectSize.yMax - panelRect.yMax;

        return new Vector2(Mathf.Clamp(desired.x, minX, maxX), Mathf.Clamp(desired.y, minY, maxY));
    }
}
