using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Handles player-driven item placement flow for placeable hotbar items,
/// including placeable preview mode, placement validation, and inventory consumption.
/// </summary>
public sealed class PlayerPlacementController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Player _player;
    [SerializeField] private Camera _camera;
    [SerializeField] private Grid _worldGrid;

    [Header("Input")]
    [SerializeField] private GameplayInputBindingsData _inputBindings;

    [Header("Placement Rules")]
    [SerializeField] private LayerMask _blockingLayerMask = ~0;
    [SerializeField, Min(0f)] private float _safePlacementRadius = 0.4f;
    [SerializeField, Min(0f)] private float _placementRadius = 1f;
    [SerializeField] private Transform _placementParent;

    [Header("Preview")]
    [SerializeField] private float _previewAlpha = 0.55f;
    [SerializeField] private Color _validPlacementPreviewColor = Color.blue;
    [SerializeField] private Color _invalidPlacementPreviewColor = Color.red;

    private readonly IPlacementStrategy[] _placementStrategies = { new PrefabPlacementStrategy() };
    private readonly PlacementPreviewState _previewState = new();

    private void Update()
    {
        var hasSelectedPlaceable = TryGetSelectedPlaceable(out var placeableItem, out var slotIndex, out var slotItem);
        if (!hasSelectedPlaceable)
        {
            DestroyPreview();
            return;
        }

        if (!TryResolvePlacementStrategy(placeableItem, out var placementStrategy))
        {
            DestroyPreview();
            return;
        }

        UpdatePlacementPreview(placeableItem, placementStrategy);

        if (!CanProcessPlacementInput())
            return;

        if (!TryResolvePlacementPosition(placeableItem, out var targetPosition))
            return;

        PlaceAndConsume(placeableItem, placementStrategy, targetPosition, slotIndex, slotItem);
    }

    private void OnDisable()
    {
        DestroyPreview();
    }

    private void OnDestroy()
    {
        DestroyPreview();
    }

    private bool TryGetSelectedPlaceable(out IPlaceableItem placeableItem, out int slotIndex, out InventoryItem slotItem)
    {
        placeableItem = null;

        if (_player.Inventory == null)
        {
            slotIndex = -1;
            slotItem = InventoryItem.Empty;
            return false;
        }

        slotIndex = _player.Inventory.SelectedHotbarIndex;
        slotItem = _player.Inventory.GetItemAt(slotIndex);

        if (slotItem.IsEmpty)
            return false;

        placeableItem = slotItem.Item as IPlaceableItem;
        return placeableItem != null;
    }

    private bool TryResolvePlacementStrategy(IPlaceableItem placeableItem, out IPlacementStrategy placementStrategy)
    {
        for (var i = 0; i < _placementStrategies.Length; i++)
        {
            var strategy = _placementStrategies[i];
            if (!strategy.CanPlace(placeableItem))
                continue;

            placementStrategy = strategy;
            return true;
        }

        placementStrategy = null;
        return false;
    }

    private void UpdatePlacementPreview(IPlaceableItem placeableItem, IPlacementStrategy placementStrategy)
    {
        if (!placementStrategy.CanPreview(placeableItem))
        {
            DestroyPreview();
            return;
        }

        if (!TryGetPlacementTarget(placeableItem.PlacementCheckSize, out var targetPosition, out var canPlaceAtTarget))
        {
            if (_previewState.Instance != null)
                _previewState.Instance.SetActive(false);

            return;
        }

        placementStrategy.UpdatePreview(
            placeableItem,
            targetPosition,
            canPlaceAtTarget,
            _placementParent,
            _previewState,
            _previewAlpha,
            _validPlacementPreviewColor,
            _invalidPlacementPreviewColor);
    }

    private bool CanProcessPlacementInput()
    {
        if (!Input.GetKeyDown(_inputBindings.PlacementKey))
            return false;

        if (PanelManager.Instance != null && PanelManager.Instance.BlocksGameplayInput)
            return false;

        if (_player.Inventory == null || GameStateManager.IsGamePaused)
            return false;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;

        return true;
    }

    private bool TryResolvePlacementPosition(IPlaceableItem placeableItem, out Vector3 targetPosition)
    {
        targetPosition = default;

        if (!TryGetPlacementTarget(placeableItem.PlacementCheckSize, out targetPosition, out var canPlaceAtTarget))
            return false;

        return canPlaceAtTarget;
    }

    private bool TryGetPlacementTarget(Vector2 placementCheckSize, out Vector3 targetPosition, out bool canPlaceAtTarget)
    {
        var mouseWorld = _camera.ScreenToWorldPoint(Input.mousePosition);

        var constrainedTarget = GetPlacementTargetWithinAllowedRange(mouseWorld);
        targetPosition = PlaceablePlacementUtility.GetSnappedTileCenter(constrainedTarget, _worldGrid);
        canPlaceAtTarget = CanPlaceAt(targetPosition, placementCheckSize);
        return true;
    }

    private Vector3 GetPlacementTargetWithinAllowedRange(Vector3 desiredTarget)
    {
        var playerPosition = (Vector2)_player.transform.position;
        var desiredOffset = (Vector2)desiredTarget - playerPosition;
        var desiredDistance = desiredOffset.magnitude;

        if (desiredDistance <= Mathf.Epsilon)
            return desiredTarget;

        var direction = desiredOffset / desiredDistance;

        if (_placementRadius > 0f && desiredDistance > _placementRadius)
            return playerPosition + direction * _placementRadius;

        return desiredTarget;
    }

    private bool CanPlaceAt(Vector2 targetPosition, Vector2 placementCheckSize)
    {
        if (!IsWithinPlacementRange(targetPosition))
            return false;

        return PlaceablePlacementUtility.IsAreaFree(targetPosition, placementCheckSize, _blockingLayerMask);
    }

    private bool IsWithinPlacementRange(Vector2 targetPosition)
    {
        var playerPosition = (Vector2)_player.transform.position;
        var distanceSqr = (targetPosition - playerPosition).sqrMagnitude;

        if (_safePlacementRadius > 0f)
        {
            var minDistanceSqr = _safePlacementRadius * _safePlacementRadius;
            if (distanceSqr < minDistanceSqr)
                return false;
        }

        if (_placementRadius <= 0f)
            return true;

        var cellSize = _worldGrid != null ? _worldGrid.cellSize : Vector3.one;
        var tileHalfExtents = new Vector2(Mathf.Abs(cellSize.x) * 0.5f, Mathf.Abs(cellSize.y) * 0.5f);
        var closestPointOnTile = new Vector2(
            Mathf.Clamp(playerPosition.x, targetPosition.x - tileHalfExtents.x, targetPosition.x + tileHalfExtents.x),
            Mathf.Clamp(playerPosition.y, targetPosition.y - tileHalfExtents.y, targetPosition.y + tileHalfExtents.y));

        var distanceSqrToClosestTilePoint = (closestPointOnTile - playerPosition).sqrMagnitude;
        var maxDistanceSqr = _placementRadius * _placementRadius;
        return distanceSqrToClosestTilePoint <= maxDistanceSqr;
    }

    private void PlaceAndConsume(IPlaceableItem placeableItem, IPlacementStrategy placementStrategy, Vector3 targetPosition, int slotIndex, InventoryItem slotItem)
    {
        placementStrategy.Place(placeableItem, targetPosition, _placementParent);
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

    private void DestroyPreview()
    {
        if (_previewState.Instance != null)
            Destroy(_previewState.Instance);

        _previewState.Clear();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_player == null)
            return;

        if (_safePlacementRadius > 0f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_player.transform.position, _safePlacementRadius);
            Handles.Label(_player.transform.position + Vector3.right * _safePlacementRadius, "Cannot place here radius");
        }
        
        if (_placementRadius > 0f)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_player.transform.position, _placementRadius);
            Handles.Label(_player.transform.position + Vector3.right * _placementRadius, "Max placement radius");
        }
    }
#endif
}
