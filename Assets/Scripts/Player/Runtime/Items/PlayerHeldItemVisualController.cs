using UnityEngine;

/// <summary>
/// Renders the currently selected hotbar item in the player's hand.
/// The held item is anchored per facing direction so it can match animations.
/// </summary>
public sealed class PlayerHeldItemVisualController : MonoBehaviour
{
    [Header("Refs")]
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
        if (_player == null)
            _player = GetComponent<Player>();

        if (_playerAnimation == null)
            _playerAnimation = GetComponent<PlayerAnimationController>();

        if (_heldItemRenderer != null)
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
        // Player inventory is created in Player.Awake. Depending on Unity execution order,
        // this component can run before that initialization is complete.
        if (!_isSubscribed)
            TryInitialize();

    }

    private void TryInitialize()
    {
        if (_player?.Inventory == null)
            return;

        Subscribe();
        RefreshVisual();
    }

    private void Subscribe()
    {
        if (_isSubscribed)
            return;

        if (_player?.Inventory != null)
        {
            _player.Inventory.OnHotbarSelectionChanged += HandleHotbarSelectionChanged;
            _player.Inventory.OnItemChanged += HandleInventoryItemChanged;
        }

        if (_playerAnimation != null)
            _playerAnimation.OnFacingDirectionChanged += HandleFacingDirectionChanged;

        _isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_isSubscribed)
            return;

        if (_player?.Inventory != null)
        {
            _player.Inventory.OnHotbarSelectionChanged -= HandleHotbarSelectionChanged;
            _player.Inventory.OnItemChanged -= HandleInventoryItemChanged;
        }

        if (_playerAnimation != null)
            _playerAnimation.OnFacingDirectionChanged -= HandleFacingDirectionChanged;

        _isSubscribed = false;
    }

    private void HandleHotbarSelectionChanged(int _)
    {
        RefreshVisual();
    }

    private void HandleInventoryItemChanged(int changedIndex)
    {
        if (_player?.Inventory == null)
            return;

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
        if (_player?.Inventory == null || _heldItemRenderer == null)
            return;

        var selectedItem = _player.Inventory.GetItemAt(_player.Inventory.SelectedHotbarIndex);
        if (selectedItem.IsEmpty || selectedItem.Item == null || selectedItem.Item.Icon == null)
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
        if (_heldItemRenderer == null)
            return;

        var anchor = GetAnchorForFacingDirection(direction);
        CurrentHandAnchor = anchor;
        if (anchor != null && _heldItemRenderer.transform.parent != anchor)
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
