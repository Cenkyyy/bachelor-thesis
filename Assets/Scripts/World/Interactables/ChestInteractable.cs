using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D), typeof(ChestInventory))]
public sealed class ChestInteractable : MonoBehaviour
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

    private bool CanInteractNow() =>
        _playerInside && !GameStateManager.IsGamePaused && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject());

    private void OnMouseOver()
    {
        if (!CanInteractNow())
            return;

        if (Input.GetMouseButtonDown(1))
        {
            PanelManager.Instance.InteractWithChest(_inventory.Inventory);
        }
    }
}
