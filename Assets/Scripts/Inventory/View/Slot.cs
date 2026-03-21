using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class Slot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
{
    [Header("UI References")]
    [SerializeField] protected Image backgroundImage;
    [SerializeField] protected Image itemIconImage;
    [SerializeField] protected TMP_Text itemAmountText;
    [SerializeField] private Image _consumableCooldownOverlay;

    [Header("Sprites")]
    [SerializeField] protected Sprite backgroundSprite;

    public IReadOnlyInventory Owner { get; private set; }
    public int SlotIndex { get; private set; } = -1;

    // Events
    public event Action<Slot, PointerEventData> OnPointerClicked;
    public event Action<Slot, PointerEventData> OnPointerEntered;

    private void Awake()
    {
        if (backgroundImage != null && backgroundSprite != null)
        {
            backgroundImage.sprite = backgroundSprite;
        }

        Clear();
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
        {
            itemIconImage.enabled = true;
            itemIconImage.sprite = item.Item.Icon;
        }

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
        {
            itemIconImage.sprite = null;
            itemIconImage.enabled = false;
        }
        if (itemAmountText != null)
        {
            itemAmountText.text = string.Empty;
            itemAmountText.gameObject.SetActive(false);
        }

        SetConsumableCooldownOverlayFill(fill01: 0f);
    }

    public void SetConsumableCooldownOverlayFill(float fill01)
    {
        if (_consumableCooldownOverlay == null)
            return;

        var clampedFill = Mathf.Clamp01(fill01);
        var showOverlay = clampedFill > 0f && itemIconImage != null && itemIconImage.enabled;

        _consumableCooldownOverlay.gameObject.SetActive(showOverlay);
        if (showOverlay)
            _consumableCooldownOverlay.fillAmount = clampedFill;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnPointerClicked?.Invoke(this, eventData);
    }

    public void OnPointerEnter(PointerEventData eventData) 
    {
        OnPointerEntered?.Invoke(this, eventData);
    }
}
