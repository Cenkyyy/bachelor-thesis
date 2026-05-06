using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Selectable recipe grid entry that displays the crafted item icon and exposes tooltip data.
/// </summary>
public sealed class CraftingRecipeSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IItemTooltipSource
{
    [Header("View References")]
    [SerializeField] private Image _icon;
    [SerializeField] private Button _button;
    [SerializeField] private CanvasGroup _canvasGroup;

    private CraftingRecipeData _recipe;

    public CraftingRecipeData Recipe => _recipe;
    public RectTransform TooltipAnchor => transform as RectTransform;
    
    public event Action<CraftingRecipeData> OnSelected;

    private void Awake()
    {
        if (_button != null)
            _button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(HandleClick);
    }

    private void OnDisable()
    {
        ItemTooltipController.Instance?.OnTooltipSourcePointerExit(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ItemTooltipController.Instance?.OnTooltipSourcePointerEnter(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ItemTooltipController.Instance?.OnTooltipSourcePointerExit(this);
    }

    public void Bind(CraftingRecipeData recipe, bool craftable)
    {
        _recipe = recipe;
        if (_icon != null)
        {
            var iconSprite = recipe != null && recipe.OutputItem != null ? recipe.OutputItem.Icon : null;
            ImageIconUtility.SetIcon(_icon, iconSprite);
        }
        SetCraftable(craftable);
    }

    public void SetCraftable(bool craftable)
    {
        if (_button != null)
            _button.interactable = craftable;

        if (_canvasGroup != null)
            _canvasGroup.alpha = craftable ? 1f : 0.70f;
    }

    public bool TryGetTooltipData(out Slot slotContext, out InventoryItem inventoryItem)
    {
        slotContext = null;
        inventoryItem = InventoryItem.Empty;

        if (_recipe == null || _recipe.OutputItem == null)
            return false;

        var outputAmount = Mathf.Max(1, _recipe.OutputAmount);
        inventoryItem = new InventoryItem(_recipe.OutputItem, outputAmount);
        return true;
    }

    private void HandleClick()
    {
        if (_recipe == null)
            return;

        OnSelected?.Invoke(_recipe);
    }
}
