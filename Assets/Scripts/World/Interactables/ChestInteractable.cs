using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D), typeof(ChestInventory))]
public sealed class ChestInteractable : InteractableBase
{
    private ChestInventory _inventory;

    private void Awake()
    {
        if (_inventory == null)
        {
            _inventory = GetComponent<ChestInventory>();
        }
    }

    protected override void OnPlayerExitTrigger(Collider2D playerCollider)
    {
        PanelManager.Instance.CloseChestIfBoundTo(_inventory.Inventory);
    }

    public override bool CanInteract() =>
        IsPlayerInside && !GameStateManager.IsGamePaused && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject());

    public override void Interact()
    {
        if (!CanInteract())
            return;

        PanelManager.Instance.InteractWithChest(_inventory.Inventory);
    }
}
