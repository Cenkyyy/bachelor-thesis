using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class PlayerBedrollPlacementController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Player _player;
    [SerializeField] private Camera _camera;
    [SerializeField] private Grid _worldGrid;

    [Header("Input")]
    [SerializeField] private GameplayInputBindingsData _inputBindings;

    [Header("Placement")]
    [SerializeField] private LayerMask _blockingLayerMask = ~0;
    [SerializeField, Min(0f)] private float _safePlacementRadius = 0.4f;
    [SerializeField, Min(0f)] private float _placementRadius = 1f;
    [SerializeField] private Transform _placementParent;

    [Header("Preview")]
    [SerializeField] private float _previewAlpha = 0.55f;
    [SerializeField] private Color _validPlacementPreviewColor = Color.blue;
    [SerializeField] private Color _invalidPlacementPreviewColor = Color.red;

    private GameObject _previewInstance;
    private GameObject _previewSourcePrefab;
    private SpriteRenderer _previewRenderer;

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
        var hasSelectedPlaceable = TryGetSelectedPlaceable(out var placeableItem, out var slotIndex, out var slotItem);
        UpdatePlacementPreview(hasSelectedPlaceable ? placeableItem : null);

        if (!CanProcessPlacementInput())
            return;

        if (!hasSelectedPlaceable)
            return;

        if (!TryResolvePlacementPosition(placeableItem, out var targetPosition))
            return;

        PlaceAndConsume(placeableItem, targetPosition, slotIndex, slotItem);
    }

    private void OnDisable()
    {
        DestroyPreview();
    }

    private void OnDestroy()
    {
        DestroyPreview();
    }

    private bool CanProcessPlacementInput()
    {
        if (PanelManager.Instance != null && PanelManager.Instance.BlocksGameplayInput)
            return false;

        if (!Input.GetKeyDown(_inputBindings.PlacementKey))
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

    private bool TryResolvePlacementPosition(IPlaceableItem placeableItem, out Vector3 targetPosition)
    {
        targetPosition = default;

        if (placeableItem == null || placeableItem.PlacementPrefab == null)
            return false;

        if (!TryGetPlacementTarget(placeableItem.PlacementCheckSize, out targetPosition, out var canPlaceAtTarget))
            return false;

        return canPlaceAtTarget;
    }

    private bool TryGetPlacementTarget(Vector2 placementCheckSize, out Vector3 targetPosition, out bool canPlaceAtTarget)
    {
        var mouseWorld = _camera != null ? _camera.ScreenToWorldPoint(Input.mousePosition) : transform.position;

        var constrainedTarget = GetPlacementTargetWithinAllowedRange(mouseWorld);
        targetPosition = PlaceablePlacementUtility.GetSnappedTileCenter(constrainedTarget, _worldGrid);
        canPlaceAtTarget = CanPlaceAt(targetPosition, placementCheckSize);
        return true;
    }

    private Vector3 GetPlacementTargetWithinAllowedRange(Vector3 desiredTarget)
    {
        if (_player == null)
            return desiredTarget;

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
        if (_player == null)
            return false;

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

        var tileHalfExtents = new Vector2(Mathf.Abs(_worldGrid.cellSize.x) * 0.5f, Mathf.Abs(_worldGrid.cellSize.y) * 0.5f);
        var closestPointOnTile = new Vector2(
            Mathf.Clamp(playerPosition.x, targetPosition.x - tileHalfExtents.x, targetPosition.x + tileHalfExtents.x),
            Mathf.Clamp(playerPosition.y, targetPosition.y - tileHalfExtents.y, targetPosition.y + tileHalfExtents.y));

        var distanceSqrToClosestTilePoint = (closestPointOnTile - playerPosition).sqrMagnitude;
        var maxDistanceSqr = _placementRadius * _placementRadius;
        return distanceSqrToClosestTilePoint <= maxDistanceSqr;
    }

    private void UpdatePlacementPreview(IPlaceableItem placeableItem)
    {
        if (!TryEnsurePreviewInstance(placeableItem))
        {
            DestroyPreview();
            return;
        }

        if (!TryGetPlacementTarget(placeableItem.PlacementCheckSize, out var targetPosition, out var canPlaceAtTarget))
        {
            _previewInstance.SetActive(false);
            return;
        }

        _previewInstance.SetActive(true);
        _previewInstance.transform.position = targetPosition;

        var previewColor = canPlaceAtTarget ? _validPlacementPreviewColor : _invalidPlacementPreviewColor;
        previewColor.a = _previewAlpha;
        _previewRenderer.color = previewColor;
    }

    private bool TryEnsurePreviewInstance(IPlaceableItem placeableItem)
    {
        if (placeableItem == null || placeableItem.PlacementPrefab == null)
            return false;

        if (_previewInstance != null && _previewSourcePrefab == placeableItem.PlacementPrefab)
            return true;

        DestroyPreview();

        _previewInstance = Instantiate(placeableItem.PlacementPrefab, Vector3.zero, Quaternion.identity, _placementParent);
        _previewInstance.name = $"{placeableItem.PlacementPrefab.name}_Preview";
        _previewSourcePrefab = placeableItem.PlacementPrefab;
        _previewRenderer = _previewInstance.GetComponentInChildren<SpriteRenderer>(true);

        var colliders = _previewInstance.GetComponentsInChildren<Collider2D>(true);
        for (var i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        var behaviours = _previewInstance.GetComponentsInChildren<MonoBehaviour>(true);
        for (var i = 0; i < behaviours.Length; i++)
            behaviours[i].enabled = false;

        return true;
    }

    private void PlaceAndConsume(IPlaceableItem placeableItem, Vector3 targetPosition, int slotIndex, InventoryItem slotItem)
    {
        Instantiate(placeableItem.PlacementPrefab, targetPosition, Quaternion.identity, _placementParent);
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
        if (_previewInstance != null)
            Destroy(_previewInstance);

        _previewInstance = null;
        _previewSourcePrefab = null;
        _previewRenderer = null;
    }

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
}
