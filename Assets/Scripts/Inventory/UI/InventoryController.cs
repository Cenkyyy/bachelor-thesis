using UnityEngine;

public class InventoryController : MonoBehaviour
{
    [SerializeField] GameObject inventoryPanel;
    [SerializeField] GameObject slotPrefab;
    [SerializeField] int slotCount = 24;

    private Slot[] _slots;
    public bool IsInventoryOpen => inventoryPanel != null && inventoryPanel.activeSelf;

    void Start()
    {
        _slots = new Slot[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            GameObject slotObject = Instantiate(slotPrefab, inventoryPanel.transform);
            Slot slot = slotObject.GetComponent<Slot>();
            _slots[i] = slot;
        }

        inventoryPanel.gameObject.SetActive(false);
    }

    void Update()
    {
        // toggle inventory with 'E' key while the game is not paused
        if (!GameStateManager.IsGamePaused && Input.GetKeyDown(KeyCode.E))
        {
            inventoryPanel.SetActive(!inventoryPanel.gameObject.activeSelf);
        }
    }

    public void CloseInventory()
    {
        if (inventoryPanel != null && IsInventoryOpen)
        {
            inventoryPanel.SetActive(false);
        }
    }
}
