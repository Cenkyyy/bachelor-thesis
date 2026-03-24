using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CraftingIngredientRow : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _label;
    [SerializeField] private TMP_Text _amount;
    [SerializeField] private CanvasGroup _canvasGroup;

    public void Bind(ItemData item, int requiredAmount, int availableAmount)
    {
        var iconSprite = item != null ? item.Icon : null;
        ImageIconUtility.SetIcon(_icon, iconSprite);

        _label.text = item != null ? item.ItemName : "Unknown";

        _amount.text = $"{availableAmount}/{requiredAmount}";

        _canvasGroup.alpha = availableAmount >= requiredAmount ? 1f : 0.5f;
    }
}
