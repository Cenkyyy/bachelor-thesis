using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Owns the item currently held by the cursor, renders it in the UI,
/// and resolves held stacks back into the inventory or world.
/// </summary>
public sealed class HeldInventoryItemController : MonoBehaviour
{
    [Header("View")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private RectTransform _heldItemContainer;
    [SerializeField] private Image _heldItemIcon;
    [SerializeField] private TMP_Text _heldItemAmountText;

    [Header("Item Dropping")]
    [SerializeField] private Player _player;
    [SerializeField] private WorldItemSpawner _worldItemSpawner;
    [SerializeField] private float _dropSpawnDistance = 0.6f;

    public InventoryItem HeldItem { get; private set; } = InventoryItem.Empty;
    public bool HasHeldItem => !HeldItem.IsEmpty;
    public Camera UICamera => _canvas != null ? _canvas.worldCamera : null;

    private void Awake()
    {
        // make sure the held item UI does not block raycasts
        if (_heldItemIcon != null)
            _heldItemIcon.raycastTarget = false;

        if (_heldItemAmountText != null)
            _heldItemAmountText.raycastTarget = false;

        // hide held item UI initially
        if (_heldItemContainer != null)
            _heldItemContainer.gameObject.SetActive(false);
    }

    public void SetHeldItem(InventoryItem item)
    {
        HeldItem = item;
        UpdateHeldItem();
    }

    public void UpdateCursorPosition()
    {
        if (!HasHeldItem || _canvas == null || _heldItemContainer == null)
            return;

        // make the item icon follow the mouse cursor
        RectTransformUtility.ScreenPointToLocalPointInRectangle
        (
            _canvas.transform as RectTransform,
            Input.mousePosition,
            _canvas.worldCamera,
            out var position
        );
        _heldItemContainer.anchoredPosition = position;
    }

    public void DropHeldItemInWorld()
    {
        if (!HasHeldItem || _player == null)
            return;

        // spawn the held item in the world
        AimUtils.ComputeAim2D(_player.transform, _dropSpawnDistance, out var direction, out var spawnPosition);
        _worldItemSpawner?.Spawn(HeldItem, spawnPosition, direction);

        HeldItem = InventoryItem.Empty;
        HideHeldItem();
    }

    public void ResolveHeldItemToInventoryOrDrop(IInventory inventory)
    {
        if (!HasHeldItem || inventory == null)
            return;

        inventory.TryAddItemToRange(HeldItem, new SlotRange(0, inventory.Capacity), out var leftover);

        if (!leftover.IsEmpty && _player != null)
        {
            AimUtils.ComputeAim2D(_player.transform, _dropSpawnDistance, out var direction, out var spawnPosition);
            _worldItemSpawner?.Spawn(leftover, spawnPosition, direction);
        }

        HeldItem = InventoryItem.Empty;
        HideHeldItem();
    }

    /// <summary>
    /// Updates the held-item UI visibility and content based on the current held item.
    /// </summary>
    private void UpdateHeldItem()
    {
        if (HeldItem.IsEmpty)
            HideHeldItem();
        else
            ShowHeldItem();
    }

    /// <summary>
    /// Shows the floating held item (icon + amount).
    /// </summary>
    private void ShowHeldItem()
    {
        if (HeldItem.IsEmpty)
        {
            HideHeldItem();
            return;
        }

        _heldItemContainer.gameObject.SetActive(true);

        // show icon
        ImageIconUtility.SetIcon(_heldItemIcon, HeldItem.Item.Icon);
        _heldItemIcon.gameObject.SetActive(true);

        if (HeldItem.Item.IsStackable && HeldItem.Amount > 1)
        {
            // show item amount text if amount is greater than 1
            _heldItemAmountText.text = HeldItem.Amount.ToString();
            _heldItemAmountText.gameObject.SetActive(true);
        }
        else
        {
            // hide item amount text if amount is 1 or less
            _heldItemAmountText.text = string.Empty;
            _heldItemAmountText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Hides the floating held item.
    /// </summary>
    private void HideHeldItem()
    {
        HeldItem = InventoryItem.Empty;

        ImageIconUtility.SetIcon(_heldItemIcon, null);
        _heldItemIcon.gameObject.SetActive(false);
        _heldItemAmountText.gameObject.SetActive(false);
        _heldItemContainer.gameObject.SetActive(false);
    }
}
