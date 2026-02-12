using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class CraftingPanel : MonoBehaviour, IMajorPanel
{
    [Header("Root")]
    [SerializeField] private GameObject _root;

    [Header("References")]
    [SerializeField] private Player _player;
    [SerializeField] private CraftingBook _craftingBook;
    [SerializeField] private CraftingController _craftingController;

    [Header("Tabs")]
    [SerializeField] private Button _toolsAndEquipmentTab;
    [SerializeField] private Button _foodTab;
    [SerializeField] private Button _potionsTab;
    [SerializeField] private Button _otherTab;

    [Header("Recipe Grid")]
    [SerializeField] private Transform _recipeGridRoot;
    [SerializeField] private CraftingRecipeSlot _recipeSlotPrefab;

    [Header("Details")]
    [SerializeField] private CraftingRecipeDetailsView _detailsView;

    public PanelId Id => PanelId.Crafting;
    public bool IsOpen => _root != null && _root.activeSelf;
    public bool PausesGame => false;
    public bool BlocksGameplayInput => true;

    private CraftingCategory _currentCategory = CraftingCategory.ToolsAndEquipment;
    private readonly List<CraftingRecipeSlot> _slots = new();
    private CraftingRecipe _selectedRecipe;

    private void Awake()
    {
        if (_root != null)
        {
            _root.SetActive(false);
        }

        if (_toolsAndEquipmentTab != null)
            _toolsAndEquipmentTab.onClick.AddListener(() => SelectCategory(CraftingCategory.ToolsAndEquipment));
        if (_foodTab != null)
            _foodTab.onClick.AddListener(() => SelectCategory(CraftingCategory.Food));
        if (_potionsTab != null)
            _potionsTab.onClick.AddListener(() => SelectCategory(CraftingCategory.Potions));
        if (_otherTab != null)
            _otherTab.onClick.AddListener(() => SelectCategory(CraftingCategory.Other));

        if (_detailsView != null && _detailsView.CraftButton != null)
        {
            _detailsView.CraftButton.onClick.AddListener(HandleCraftPressed);
        }

        if (_craftingController != null)
        {
            _craftingController.OnCraftStarted += HandleCraftStarted;
            _craftingController.OnCraftProgress += HandleCraftProgress;
            _craftingController.OnCraftCompleted += HandleCraftFinished;
            _craftingController.OnCraftFailed += HandleCraftFailed;
        }
    }

    private void Start()
    {
        if (_player != null && _player.Inventory != null)
        {
            _player.Inventory.OnItemChanged += HandleInventoryChanged;
        }
    }

    private void OnDestroy()
    {
        if (_player != null && _player.Inventory != null)
        {
            _player.Inventory.OnItemChanged -= HandleInventoryChanged;
        }

        if (_craftingController != null)
        {
            _craftingController.OnCraftStarted -= HandleCraftStarted;
            _craftingController.OnCraftProgress -= HandleCraftProgress;
            _craftingController.OnCraftCompleted -= HandleCraftFinished;
            _craftingController.OnCraftFailed -= HandleCraftFailed;
        }

        if (_detailsView != null && _detailsView.CraftButton != null)
        {
            _detailsView.CraftButton.onClick.RemoveListener(HandleCraftPressed);
        }
    }

    public void Open()
    {
        if (_root != null)
        {
            _root.SetActive(true);
        }

        SelectCategory(_currentCategory);
    }

    public void Close()
    {
        if (_root != null)
        {
            _root.SetActive(false);
        }
    }

    private void SelectCategory(CraftingCategory category)
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
            var craftable = CanCraft(recipe);
            slot.Bind(recipe, craftable);
            slot.OnSelected += HandleRecipeSelected;
            _slots.Add(slot);
        }

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

    private void HandleRecipeSelected(CraftingRecipe recipe)
    {
        _selectedRecipe = recipe;
        if (_detailsView == null)
            return;

        _detailsView.SetRecipe(recipe, _player != null ? _player.Inventory : null);
        _detailsView.SetCraftButtonEnabled(CanCraft(recipe));
    }

    private void HandleCraftPressed()
    {
        if (_craftingController == null || _selectedRecipe == null)
            return;

        _craftingController.TryStartCraft(_selectedRecipe);
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
            slot.SetCraftable(CanCraft(slot.Recipe));
        }

        if (_selectedRecipe != null)
        {
            var isCraftingSelected = _craftingController != null &&
                _craftingController.IsCrafting &&
                _craftingController.CurrentRecipe == _selectedRecipe;

            if (!isCraftingSelected)
            {
                _detailsView?.SetRecipe(_selectedRecipe, _player != null ? _player.Inventory : null);
            }
            _detailsView?.SetCraftButtonEnabled(CanCraft(_selectedRecipe));
        }
    }

    private bool CanCraft(CraftingRecipe recipe)
    {
        if (recipe == null || _player == null || _player.Inventory == null)
            return false;

        var inventory = _player.Inventory;
        return CraftingInventoryUtility.HasIngredients(inventory, recipe.Ingredients) &&
            CraftingInventoryUtility.CanFitOutput(inventory, recipe.GetOutputStack());
    }

    private void HandleCraftStarted(CraftingRecipe recipe)
    {
        if (_detailsView != null && recipe == _selectedRecipe)
        {
            _detailsView.SetCraftButtonEnabled(false);
        }
    }

    private void HandleCraftProgress(CraftingRecipe recipe, float progress)
    {
        if (_detailsView != null && recipe == _selectedRecipe)
        {
            _detailsView.SetProgress(progress);
        }
    }

    private void HandleCraftFinished(CraftingRecipe recipe)
    {
        if (_detailsView != null && recipe == _selectedRecipe)
        {
            _detailsView.SetProgress(0f);
        }

        RefreshRecipeAvailability();
    }

    private void HandleCraftFailed(CraftingRecipe recipe, CraftingFailureReason _)
    {
        if (_detailsView != null && recipe == _selectedRecipe)
        {
            _detailsView.SetProgress(0f);
            _detailsView.SetCraftButtonEnabled(CanCraft(recipe));
        }

        RefreshRecipeAvailability();
    }
}