using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Root crafting UI panel that manages recipe category selection, recipe details,
/// craft button state, and delegates crafting execution to the player crafting controller.
/// </summary>
public sealed class CraftingPanel : MonoBehaviour, IMajorPanel
{
    [Header("Panel Root")]
    [SerializeField] private GameObject _root;

    [Header("Dependencies")]
    [SerializeField] private Player _player;
    [SerializeField] private PlayerCraftingController _playerCraftingController;

    [Header("Recipe Data")]
    [SerializeField] private CraftingRecipeBookData _craftingBook;

    [Header("Category Tabs")]
    [SerializeField] private Button _armsAndArmorTab;
    [SerializeField] private Button _foodTab;
    [SerializeField] private Button _potionsTab;
    [SerializeField] private Button _otherTab;

    [Header("Recipe Grid")]
    [SerializeField] private Transform _recipeGridRoot;
    [SerializeField] private CraftingRecipeSlotView _recipeSlotPrefab;
    [SerializeField] private ScrollRect _recipeScrollRect;

    [Header("Recipe Details")]
    [SerializeField] private CraftingRecipeDetailsView _detailsView;

    public PanelId Id => PanelId.Crafting;
    public bool IsOpen => _root != null && _root.activeSelf;
    public bool PausesGame => false;
    public bool BlocksGameplayInput => true;

    private CraftingRecipeCategory _currentCategory = CraftingRecipeCategory.ArmsAndArmor;
    private readonly List<CraftingRecipeSlotView> _slots = new();
    private CraftingRecipeData _selectedRecipe;

    private void Awake()
    {
        if (_root != null)
            _root.SetActive(false);

        if (_armsAndArmorTab != null)
            _armsAndArmorTab.onClick.AddListener(() => SelectCategory(CraftingRecipeCategory.ArmsAndArmor));
        if (_foodTab != null)
            _foodTab.onClick.AddListener(() => SelectCategory(CraftingRecipeCategory.Food));
        if (_potionsTab != null)
            _potionsTab.onClick.AddListener(() => SelectCategory(CraftingRecipeCategory.Potions));
        if (_otherTab != null)
            _otherTab.onClick.AddListener(() => SelectCategory(CraftingRecipeCategory.Other));

        if (_detailsView != null && _detailsView.CraftButton != null)
            _detailsView.CraftButton.onClick.AddListener(HandleCraftPressed);

        if (_playerCraftingController != null)
        {
            _playerCraftingController.OnCraftStarted += HandleCraftStarted;
            _playerCraftingController.OnCraftCompleted += HandleCraftFinished;
        }
    }

    private void Start()
    {
        if (_player != null && _player.Inventory != null)
            _player.Inventory.OnItemChanged += HandleInventoryChanged;
    }

    private void OnDestroy()
    {
        if (_player != null && _player.Inventory != null)
            _player.Inventory.OnItemChanged -= HandleInventoryChanged;

        if (_playerCraftingController != null)
        {
            _playerCraftingController.OnCraftStarted -= HandleCraftStarted;
            _playerCraftingController.OnCraftCompleted -= HandleCraftFinished;
        }

        if (_detailsView != null && _detailsView.CraftButton != null)
            _detailsView.CraftButton.onClick.RemoveListener(HandleCraftPressed);
    }

    public void Open()
    {
        if (_root != null)
            _root.SetActive(true);

        SelectCategory(_currentCategory);
    }

    public void Close()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    private void SelectCategory(CraftingRecipeCategory category)
    {
        _currentCategory = category;
        BuildRecipeGrid();
    }

    private void BuildRecipeGrid()
    {
        ClearRecipeGrid();

        if (_craftingBook == null || _recipeGridRoot == null || _recipeSlotPrefab == null)
            return;

        var recipes = _craftingBook.GetRecipesByCategory(_currentCategory);
        foreach (var recipe in recipes)
        {
            if (recipe == null)
                continue;

            var slot = Instantiate(_recipeSlotPrefab, _recipeGridRoot);
            var craftable = IsRecipeCraftable(recipe);
            slot.Bind(recipe, craftable);
            slot.OnSelected += HandleRecipeSelected;
            _slots.Add(slot);
        }

        if (_recipeScrollRect != null)
        {
            _recipeScrollRect.StopMovement();
            _recipeScrollRect.verticalNormalizedPosition = 1f;
        }

        if (_recipeGridRoot is RectTransform recipeGridRect)
            recipeGridRect.anchoredPosition = Vector2.zero;

        if (_slots.Count == 0)
        {
            _detailsView?.Clear();
            _selectedRecipe = null;
            return;
        }

        HandleRecipeSelected(_slots[0].Recipe);
    }

    private void ClearRecipeGrid()
    {
        foreach (var slot in _slots)
        {
            if (slot == null)
                continue;
            slot.OnSelected -= HandleRecipeSelected;
            Destroy(slot.gameObject);
        }

        _slots.Clear();
    }

    private void HandleRecipeSelected(CraftingRecipeData recipe)
    {
        _selectedRecipe = recipe;
        if (_detailsView == null)
            return;

        _detailsView.SetRecipe(recipe, _player != null ? _player.Inventory : null);
        _detailsView.SetCraftButtonEnabled(IsRecipeCraftable(recipe));
    }

    private void HandleCraftPressed()
    {
        if (_playerCraftingController == null || _selectedRecipe == null)
            return;

        if (!_playerCraftingController.TryCraft(_selectedRecipe))
            RefreshRecipeAvailability();
    }

    private void HandleInventoryChanged(int _)
    {
        RefreshRecipeAvailability();
    }

    private void RefreshRecipeAvailability()
    {
        if (_slots.Count == 0)
            return;

        foreach (var slot in _slots)
        {
            if (slot == null)
                continue;
            slot.SetCraftable(IsRecipeCraftable(slot.Recipe));
        }

        if (_selectedRecipe != null)
        {
            _detailsView?.SetRecipe(_selectedRecipe, _player != null ? _player.Inventory : null);
            _detailsView?.SetCraftButtonEnabled(IsRecipeCraftable(_selectedRecipe));
        }
    }

    private bool IsRecipeCraftable(CraftingRecipeData recipe)
    {
        return _playerCraftingController != null && _playerCraftingController.CanCraft(recipe);
    }

    private void HandleCraftStarted(CraftingRecipeData recipe)
    {
        if (_detailsView != null && recipe == _selectedRecipe)
            _detailsView.SetCraftButtonEnabled(false);
    }

    private void HandleCraftFinished(CraftingRecipeData recipe)
    {
        RefreshRecipeAvailability();
    }
}
