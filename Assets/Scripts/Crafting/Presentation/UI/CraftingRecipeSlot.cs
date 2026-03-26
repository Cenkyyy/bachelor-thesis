using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class CraftingRecipeSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IItemTooltipSource
{
    [SerializeField] private Image _icon;
    [SerializeField] private Button _button;
    [SerializeField] private CanvasGroup _canvasGroup;

    public event Action<CraftingRecipeData> OnSelected;

    private CraftingRecipeData _recipe;

    public CraftingRecipeData Recipe => _recipe;

    private void Awake()
    {
        if (_button != null)
        {
            _button.onClick.AddListener(HandleClick);
        }
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(HandleClick);
        }
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
        {
            _button.interactable = craftable;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = craftable ? 1f : 0.70f;
        }
    }

    private void HandleClick()
    {
        if (_recipe == null)
            return;

        OnSelected?.Invoke(_recipe);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ItemTooltipController.Instance?.OnTooltipSourcePointerEnter(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ItemTooltipController.Instance?.OnTooltipSourcePointerExit(this);
    }

    private void OnDisable()
    {
        ItemTooltipController.Instance?.OnTooltipSourcePointerExit(this);
    }

    public RectTransform TooltipAnchor => transform as RectTransform;

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
}
