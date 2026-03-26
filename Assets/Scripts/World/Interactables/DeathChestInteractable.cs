using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D), typeof(DeathChestInventory))]
public sealed class DeathChestInteractable : InteractableBase
{
    private DeathChestInventory _inventory;

    private void Awake()
    {
        if (_inventory == null)
        {
            _inventory = GetComponent<DeathChestInventory>();
        }
    }

    protected override void OnPlayerExitTrigger(Collider2D playerCollider)
    {
        PanelManager.Instance.CloseDeathChestIfBoundTo(_inventory.Inventory);
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
