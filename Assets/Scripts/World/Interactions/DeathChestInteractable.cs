using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D), typeof(DeathChestInventory))]
public sealed class DeathChestInteractable : InteractableBase
{
    [SerializeField, Min(0.1f)] private float _autoCloseDistance = 1.5f;

    private DeathChestInventory _inventory;
    private Transform _playerTransform;

    private void Awake()
    {
        if (_inventory == null)
        {
            _inventory = GetComponent<DeathChestInventory>();
        }
    }

    private void Update()
    {
        if (_playerTransform == null || _inventory == null)
            return;

        var distanceSqr = ((Vector2)_playerTransform.position - (Vector2)transform.position).sqrMagnitude;
        if (distanceSqr <= _autoCloseDistance * _autoCloseDistance)
            return;

        PanelManager.Instance?.CloseDeathChestIfBoundTo(_inventory.Inventory);
    }

    protected override void OnPlayerEnterTrigger(Collider2D playerCollider)
    {
        _playerTransform = playerCollider.transform;
    }

    public override bool CanInteract() =>
        IsPlayerInside && !GameStateManager.IsGamePaused && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject());

    public override void Interact()
    {
        if (!CanInteract())
            return;

        PanelManager.Instance.InteractWithDeathChest(_inventory.Inventory);
    }
}
