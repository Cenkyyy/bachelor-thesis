using UnityEngine;

public class HotbarController : MonoBehaviour
{
    [SerializeField] Transform hotbarPanel;
    [SerializeField] Slot slotPrefab;
    [SerializeField] PlayerInventoryWrapper playerInventory;

    private HotbarSlot[] _slots;
    private int _selectedIndex = 0;

    private void Start()
    {
        _slots = new HotbarSlot[playerInventory.Inventory.HotbarSize];

        // create hotbar slots
        for (int i = 0; i < playerInventory.Inventory.HotbarSize; i++)
        {
            _slots[i] = Instantiate(slotPrefab, hotbarPanel).GetComponent<HotbarSlot>();
            _slots[i].Bind(i, playerInventory.Inventory.GetItemAt(i));
            _slots[i].SetToDefault();
        }

        // highlight the first slot by default
        _slots[_selectedIndex].HighlightSlot();
    }

    private void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            // scroll up
            ChangeSelectedSlot((_selectedIndex + 1) % _slots.Length);
        }
        else if (scroll < 0f)
        {
            // scroll down
            ChangeSelectedSlot((_selectedIndex - 1 + _slots.Length) % _slots.Length);
        }
    }

    private void ChangeSelectedSlot(int newIndex)
    {
        _slots[_selectedIndex].SetToDefault();
        _selectedIndex = newIndex;
        _slots[_selectedIndex].HighlightSlot();
    }

    public void RefreshSlot(int hotbarIndex)
    {
        if (hotbarIndex >= 0 && hotbarIndex < _slots.Length)
        {
            _slots[hotbarIndex].Refresh(playerInventory.Inventory.GetItemAt(hotbarIndex));
        }
    }
}
