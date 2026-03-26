using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ItemTooltipController : MonoBehaviour
{
    public static ItemTooltipController Instance { get; private set; }

    [Header("View")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private RectTransform _panelRoot;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _rarityText;
    [SerializeField] private TMP_Text _bodyText;

    [Header("Behavior")]
    [SerializeField, Range(0.1f, 3f)] private float _hoverDelaySeconds = 0.75f;
    [SerializeField] private Vector2 _slotOffset = new Vector2(30f, 30f);

    [Header("Runtime Dependencies")]
    [SerializeField] private PlayerToolDurability _playerToolDurability;

    private readonly List<IItemTooltipProvider> _providers = new List<IItemTooltipProvider>();

    private Coroutine _hoverCoroutine;
    private Slot _currentHoveredSlot;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        RegisterProviders();
        Hide();
    }

    private void OnDisable()
    {
        CancelPendingHover();
        Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void OnSlotPointerEnter(Slot slot, PointerEventData eventData)
    {
        if (!CanShowForSlot(slot))
        {
            CancelPendingHover();
            Hide();
            return;
        }

        _currentHoveredSlot = slot;
        RestartHoverCountdown(slot);
    }

    public void OnSlotPointerExit(Slot slot, PointerEventData eventData)
    {
        if (!ReferenceEquals(_currentHoveredSlot, slot))
            return;

        CancelPendingHover();
        Hide();
        _currentHoveredSlot = null;
    }

    public void OnSlotDisabled(Slot slot)
    {
        if (!ReferenceEquals(_currentHoveredSlot, slot))
            return;

        CancelPendingHover();
        Hide();
        _currentHoveredSlot = null;
    }

    private static bool CanShowForSlot(Slot slot)
    {
        if (slot == null || slot.Owner == null || slot.SlotIndex < 0)
            return false;

        var slotItem = slot.Owner.GetItemAt(slot.SlotIndex);
        return !slotItem.IsEmpty && slotItem.Item != null;
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
        _providers.AddRange(ItemTooltipProviderFactory.CreateDefault(_playerToolDurability));
    }

    private void RestartHoverCountdown(Slot slot)
    {
        CancelPendingHover();
        _hoverCoroutine = StartCoroutine(ShowAfterDelayCoroutine(slot));
    }

    private IEnumerator ShowAfterDelayCoroutine(Slot slot)
    {
        yield return new WaitForSeconds(_hoverDelaySeconds);

        if (!ReferenceEquals(_currentHoveredSlot, slot) || !CanShowForSlot(slot))
            yield break;

        var slotItem = slot.Owner.GetItemAt(slot.SlotIndex);
        if (slotItem.IsEmpty || slotItem.Item == null)
            yield break;

        Show(slot, slotItem);
    }

    private void Show(Slot slot, InventoryItem slotItem)
    {
        if (_panelRoot == null)
            return;

        var tooltipRuntimeData = BuildRuntimeData(slot, slotItem);
        Render(tooltipRuntimeData);
        PositionNearSlot(slot);

        _panelRoot.gameObject.SetActive(true);
    }

    private ItemTooltipRuntimeData BuildRuntimeData(Slot slot, InventoryItem slotItem)
    {
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

    private void PositionNearSlot(Slot slot)
    {
        if (_panelRoot == null || slot == null)
            return;

        var slotTransform = slot.transform as RectTransform;
        if (slotTransform == null)
            return;

        var worldCorners = new Vector3[4];
        slotTransform.GetWorldCorners(worldCorners);

        var topRight = worldCorners[2];
        var screenTopRight = RectTransformUtility.WorldToScreenPoint(_canvas != null ? _canvas.worldCamera : null, topRight);

        var canvasRect = _canvas.transform as RectTransform;
        if (canvasRect == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenTopRight, _canvas != null ? _canvas.worldCamera : null, out var localPoint))
            return;

        var desired = localPoint + _slotOffset;

        // force layout before clamping so rect has proper size
        LayoutRebuilder.ForceRebuildLayoutImmediate(_panelRoot);

        var clamped = ClampToCanvas(desired, canvasRect, _panelRoot);
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
