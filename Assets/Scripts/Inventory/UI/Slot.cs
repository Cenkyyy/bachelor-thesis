using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Slot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
{
    [Header("UI References")]
    [SerializeField] protected Image backgroundImage;
    [SerializeField] protected Image itemIconImage;
    [SerializeField] protected TMP_Text itemAmountText;

    [Header("Sprites")]
    [SerializeField] protected Sprite backgroundSprite;

    public int SlotIndex { get; private set; } = -1;

    private void Awake()
    {
        // set default background sprite if assigned
        if (backgroundImage != null && backgroundSprite != null)
        {
            backgroundImage.sprite = backgroundSprite;
        }

        // initialize ui slot
        ClearSlot();
    }

    public void Bind(int index, Item item)
    {
        SlotIndex = index;
        Refresh(item);
    }

    public void Refresh(Item item)
    {
        if (item == null || item.item == null || SlotIndex < 0)
        {
            ClearSlot();
            return;
        }

        // show item icon
        itemIconImage.enabled = true;
        itemIconImage.sprite = item.item.icon;

        if (item.item.IsStackable && item.amount > 1) 
        {
            // show item amount text if amount is greater than 1
            itemAmountText.text = item.amount.ToString();
            itemAmountText.gameObject.SetActive(true);
        }
        else 
        {
            // hide item amount text if amount is 1 or less
            itemAmountText.text = string.Empty;
            itemAmountText.gameObject.SetActive(false);
        }
    }

    public void ClearSlot()
    {    
        itemIconImage.sprite = null;
        itemIconImage.enabled = false;
        itemAmountText.text = string.Empty;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // handle double click
            if (eventData.clickCount >= 2)
            {
                ItemInteractionManager.Instance.HandleDoubleLeftClick(this);
                return;
            }

            // handle regular click or quick-transfer click using shift
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                ItemInteractionManager.Instance.HandleQuickTransferLeftClick(this);
            }
            else
            {
                ItemInteractionManager.Instance.HandleRegularLeftClick(this);
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            // handle right click
            ItemInteractionManager.Instance.HandleRegularRightClick(this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData) 
    {
        if (Input.GetMouseButton(1))
        {
            // handle pointer enter for tooltip
            ItemInteractionManager.Instance.HandleRightClickPointerEnter(this);
        }
    }
}
