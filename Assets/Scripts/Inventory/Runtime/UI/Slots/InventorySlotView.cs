using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Visual UI slot that displays one inventory item and exposes pointer events to panel controllers.
/// </summary>
[DisallowMultipleComponent]
public class InventorySlotView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IItemTooltipSource
{
    [Header("UI References")]
    [SerializeField] protected Image backgroundImage;
    [SerializeField] protected Image itemIconImage;
    [SerializeField] protected TMP_Text itemAmountText;
    [SerializeField] private Image _itemCooldownOverlay;

    [Header("Sprites")]
    [SerializeField] protected Sprite backgroundSprite;

    public IReadOnlyInventory Owner { get; private set; }
    public int SlotIndex { get; private set; } = -1;

    public event Action<InventorySlotView, PointerEventData> OnPointerClicked;
    public event Action<InventorySlotView, PointerEventData> OnPointerEntered;
    public event Action<InventorySlotView, PointerEventData> OnPointerExited;
    public event Action<InventorySlotView> OnSlotDisabled;

    private void Awake()
    {
        if (backgroundImage != null && backgroundSprite != null)
            backgroundImage.sprite = backgroundSprite;

        Clear();
    }

    private void OnDisable()
    {
        OnSlotDisabled?.Invoke(this);
    }

    public virtual void Bind(IReadOnlyInventory owner, int index, InventoryItem item)
    {
        Owner = owner;
        SlotIndex = index;
        Refresh(item);
    }

    public virtual void Refresh(InventoryItem item)
    {
        if (item.IsEmpty || SlotIndex < 0)
        {
            Clear();
            return;
        }

        // show item icon
        if (itemIconImage != null)
            ImageIconUtility.SetIcon(itemIconImage, item.Item.Icon);

        if (item.Item.IsStackable && item.Amount > 1)
        {
            // show item amount text if amount is greater than 1
            if (itemAmountText != null)
            {
                itemAmountText.text = item.Amount.ToString();
                itemAmountText.gameObject.SetActive(true);
            }
        }
        else
        {
            // hide item amount text if amount is 1 or less
            if (itemAmountText != null)
            {
                itemAmountText.text = string.Empty;
                itemAmountText.gameObject.SetActive(false);
            }
        }
    }

    public virtual void Clear()
    {
        if (itemIconImage != null)
            ImageIconUtility.SetIcon(itemIconImage, null);
        
        if (itemAmountText != null)
        {
            itemAmountText.text = string.Empty;
            itemAmountText.gameObject.SetActive(false);
        }

        SetItemCooldownOverlayFill(0f);
    }

    public void SetItemCooldownOverlayFill(float fill01)
    {
        if (_itemCooldownOverlay == null)
            return;

        var clampedFill = Mathf.Clamp01(fill01);
        var showOverlay = clampedFill > 0f && itemIconImage != null && itemIconImage.enabled;

        _itemCooldownOverlay.gameObject.SetActive(showOverlay);
        if (showOverlay)
            _itemCooldownOverlay.fillAmount = clampedFill;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnPointerClicked?.Invoke(this, eventData);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnPointerEntered?.Invoke(this, eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnPointerExited?.Invoke(this, eventData);
    }

    public RectTransform TooltipAnchor => transform as RectTransform;

    public bool TryGetTooltipData(out InventorySlotView slotContext, out InventoryItem inventoryItem)
    {
        slotContext = this;
        inventoryItem = InventoryItem.Empty;

        if (Owner == null || SlotIndex < 0)
            return false;

        inventoryItem = Owner.GetItemAt(SlotIndex);
        return !inventoryItem.IsEmpty && inventoryItem.Item != null;
    }
}
