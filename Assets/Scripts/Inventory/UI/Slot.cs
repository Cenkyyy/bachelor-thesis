using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Slot : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] protected Image backgroundImage;
    [SerializeField] protected Image itemIconImage;
    [SerializeField] protected TMP_Text itemAmountText;

    [Header("Sprites")]
    [SerializeField] protected Sprite backgroundSprite;

    public ItemSO StoredItem { get; private set; }
    public int Amount { get; private set; }

    protected virtual void Awake()
    {
        if (backgroundImage != null && backgroundSprite != null)
        {
            backgroundImage.sprite = backgroundSprite;
        }
    }
}
