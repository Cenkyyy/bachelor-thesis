using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D), typeof(ChestInventory))]
public sealed class ChestInteractable : MonoBehaviour, IInteractable
{
    private ChestInventory _inventory;
    private bool _playerInside;

    private void Awake()
    {
        if (_inventory == null)
        {
            _inventory = GetComponent<ChestInventory>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInside = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInside = false;
            PanelManager.Instance.CloseChestIfBoundTo(_inventory.Inventory);
        }
    }

    public bool CanInteract() =>
        _playerInside && !GameStateManager.IsGamePaused && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject());

    public void Interact()
    {
        if (!CanInteract())
            return;

        PanelManager.Instance.InteractWithChest(_inventory.Inventory);
    }

    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Interact();
        }
    }
}
