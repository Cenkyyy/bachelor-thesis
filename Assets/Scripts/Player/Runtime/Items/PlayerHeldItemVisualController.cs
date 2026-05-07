using UnityEngine;

/// <summary>
/// Renders the selected hotbar item in the player's hand and anchors it to the current facing direction.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerHeldItemVisualController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player _player;
    [SerializeField] private PlayerAnimationController _playerAnimation;
    [SerializeField] private SpriteRenderer _heldItemRenderer;

    [Header("Directional Hand Anchors")]
    [SerializeField] private Transform _downAnchor;
    [SerializeField] private Transform _upAnchor;
    [SerializeField] private Transform _leftAnchor;
    [SerializeField] private Transform _rightAnchor;

    [Header("Render Order")]
    [SerializeField] private int _abovePlayerOrder = 12;
    [SerializeField] private int _belowPlayerOrder = 8;

    [Header("Visual")]
    [SerializeField] private Vector3 _itemLocalScale = Vector3.one;

    private bool _isSubscribed;

    public Transform CurrentHandAnchor { get; private set; }

    private void Awake()
    {
        _heldItemRenderer.gameObject.SetActive(true);
        CurrentHandAnchor = _downAnchor;
    }

    private void Start()
    {
        TryInitialize();
    }

    private void OnEnable()
    {
        TryInitialize();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        if (!_isSubscribed)
            TryInitialize();
    }

    private void TryInitialize()
    {
        if (_player.Inventory == null)
            return;

        Subscribe();
        RefreshVisual();
    }

    private void Subscribe()
    {
        if (_isSubscribed)
            return;

        _player.Inventory.OnHotbarSelectionChanged += HandleHotbarSelectionChanged;
        _player.Inventory.OnItemChanged += HandleInventoryItemChanged;
        _playerAnimation.OnFacingDirectionChanged += HandleFacingDirectionChanged;
        _isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_isSubscribed || _player.Inventory == null)
            return;

        _player.Inventory.OnHotbarSelectionChanged -= HandleHotbarSelectionChanged;
        _player.Inventory.OnItemChanged -= HandleInventoryItemChanged;
        _playerAnimation.OnFacingDirectionChanged -= HandleFacingDirectionChanged;
        _isSubscribed = false;
    }

    private void HandleHotbarSelectionChanged(int _)
    {
        RefreshVisual();
    }

    private void HandleInventoryItemChanged(int changedIndex)
    {
        if (changedIndex != _player.Inventory.SelectedHotbarIndex)
            return;

        RefreshVisual();
    }

    private void HandleFacingDirectionChanged(PlayerFacingDirection direction)
    {
        ApplyFacingDirection(direction);
    }

    private void RefreshVisual()
    {
        var selectedItem = _player.Inventory.GetItemAt(_player.Inventory.SelectedHotbarIndex);
        if (selectedItem.IsEmpty || selectedItem.Item.Icon == null)
        {
            _heldItemRenderer.sprite = null;
            _heldItemRenderer.gameObject.SetActive(false);
            return;
        }

        _heldItemRenderer.sprite = selectedItem.Item.Icon;
        _heldItemRenderer.gameObject.SetActive(true);
        _heldItemRenderer.transform.localScale = _itemLocalScale;
        ApplyFacingDirection(_playerAnimation.FacingDirection);
    }

    private void ApplyFacingDirection(PlayerFacingDirection direction)
    {
        var anchor = GetAnchorForFacingDirection(direction);
        CurrentHandAnchor = anchor;

        if (_heldItemRenderer.transform.parent != anchor)
        {
            _heldItemRenderer.transform.SetParent(anchor, false);
            _heldItemRenderer.transform.localPosition = Vector3.zero;
            _heldItemRenderer.transform.localRotation = Quaternion.identity;
            _heldItemRenderer.transform.localScale = _itemLocalScale;
        }

        _heldItemRenderer.sortingOrder = GetSortingOrder(direction);
    }

    private Transform GetAnchorForFacingDirection(PlayerFacingDirection direction)
    {
        return direction switch
        {
            PlayerFacingDirection.Up => _upAnchor,
            PlayerFacingDirection.Left => _leftAnchor,
            PlayerFacingDirection.Right => _rightAnchor,
            _ => _downAnchor
        };
    }

    private int GetSortingOrder(PlayerFacingDirection direction)
    {
        return direction switch
        {
            PlayerFacingDirection.Up => _belowPlayerOrder,
            _ => _abovePlayerOrder
        };
    }
}
