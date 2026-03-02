using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class CraftingRecipeSlot : MonoBehaviour
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

    public void Bind(CraftingRecipeData recipe, bool craftable)
    {
        _recipe = recipe;
        if (_icon != null)
        {
            _icon.sprite = recipe != null && recipe.OutputItem != null ? recipe.OutputItem.Icon : null;
            _icon.enabled = _icon.sprite != null;
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
            _canvasGroup.alpha = craftable ? 1f : 0.45f;
        }
    }

    private void HandleClick()
    {
        if (_recipe == null)
            return;

        OnSelected?.Invoke(_recipe);
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(HandleClick);
        }
    }
}