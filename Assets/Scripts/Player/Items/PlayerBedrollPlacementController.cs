using UnityEngine;
using UnityEngine.EventSystems;

public sealed class PlayerBedrollPlacementController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Player _player;
    [SerializeField] private Camera _camera;
    [SerializeField] private Grid _worldGrid;

    [Header("Input")]
    [SerializeField] private int _placeMouseButton = 1;

    [Header("Placement")]
    [SerializeField] private LayerMask _blockingLayerMask = ~0;
    [SerializeField] private float _placementRadius = 2f;

    private void Awake()
    {
        if (_player == null)
            _player = GetComponent<Player>() ?? GetComponentInParent<Player>();

        if (_camera == null)
            _camera = Camera.main;

        if (_worldGrid == null)
        {
            var grids = FindObjectsByType<Grid>(FindObjectsSortMode.None);
            if (grids != null && grids.Length > 0)
                _worldGrid = grids[0];
        }
    }

    private void Update()
    {
        if (!CanProcessPlacementInput())
            return;

        if (!TryGetSelectedPlaceable(out var placeableItem, out var slotIndex, out var slotItem))
            return;

        if (!TryResolvePlacementPosition(out var targetPosition))
            return;

        if (!CanPlaceAt(targetPosition, placeableItem))
            return;

        PlaceAndConsume(placeableItem, targetPosition, slotIndex, slotItem);
    }

    private bool CanProcessPlacementInput()
    {
        if (!Input.GetMouseButtonDown(_placeMouseButton))
            return false;

        if (_player == null || _player.Inventory == null || GameStateManager.IsGamePaused)
            return false;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;

        return true;
    }

    private bool TryGetSelectedPlaceable(out IPlaceableItem placeableItem, out int slotIndex, out InventoryItem slotItem)
    {
        placeableItem = null;
        slotIndex = _player.Inventory.SelectedHotbarIndex;
        slotItem = _player.Inventory.GetItemAt(slotIndex);

        if (slotItem.IsEmpty)
            return false;

        placeableItem = slotItem.Item as IPlaceableItem;
        return placeableItem != null;
    }

    private bool TryResolvePlacementPosition(out Vector3 targetPosition)
    {
        var mouseWorld = _camera != null
            ? _camera.ScreenToWorldPoint(Input.mousePosition)
            : transform.position;

        targetPosition = PlaceablePlacementUtility.GetSnappedTileCenter(mouseWorld, _worldGrid);
        return true;
    }

    private bool CanPlaceAt(Vector2 targetPosition, IPlaceableItem placeableItem)
    {
        if (placeableItem.PlacementPrefab == null)
            return false;

        if (!IsWithinPlacementRange(targetPosition))
            return false;

        return PlaceablePlacementUtility.IsAreaFree(targetPosition, placeableItem.PlacementCheckSize, _blockingLayerMask);
    }

    private bool IsWithinPlacementRange(Vector2 targetPosition)
    {
        if (_placementRadius <= 0f)
            return true;

        var playerPosition = (Vector2)_player.transform.position;
        var maxDistanceSqr = _placementRadius * _placementRadius;
        var distanceSqr = (targetPosition - playerPosition).sqrMagnitude;
        return distanceSqr <= maxDistanceSqr;
    }

    private void PlaceAndConsume(IPlaceableItem placeableItem, Vector3 targetPosition, int slotIndex, InventoryItem slotItem)
    {
        Instantiate(placeableItem.PlacementPrefab, targetPosition, Quaternion.identity);
        ConsumeOneItem(slotIndex, slotItem);
    }

    private void ConsumeOneItem(int slotIndex, InventoryItem slotItem)
    {
        var remainingAmount = slotItem.Amount - 1;
        if (remainingAmount <= 0)
            _player.Inventory.ClearItemAt(slotIndex);
        else
            _player.Inventory.SetItemAt(slotIndex, slotItem.WithAmount(remainingAmount));
    }
}
