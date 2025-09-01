using UnityEngine;

public class HotbarController : SlotControllerBase<HotbarSlot>
{
    protected override int SlotCount => playerInventory.Inventory.HotbarSize;

    private int _selectedIndex = 0;

    protected override void Start()
    {
        base.Start();

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i]?.SetToDefault();
        }

        if (slots.Length > 0)
            slots[_selectedIndex].HighlightSelected();
    }

    private void Update()
    {
        if (slots == null || slots.Length == 0) 
            return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            // scroll up
            ChangeSelectedSlot((_selectedIndex + 1) % slots.Length);
        }
        else if (scroll < 0f)
        {
            // scroll down
            ChangeSelectedSlot((_selectedIndex - 1 + slots.Length) % slots.Length);
        }
    }

    private void ChangeSelectedSlot(int newIndex)
    {
        slots[_selectedIndex].SetToDefault();
        _selectedIndex = newIndex;
        slots[_selectedIndex].HighlightSelected();
    }

    public override void RefreshSlot(int hotbarIndex)
    {
        if (hotbarIndex >= 0 && hotbarIndex < slots.Length)
        {
            slots[hotbarIndex].Refresh(playerInventory.Inventory.GetItemAt(hotbarIndex));
        }
    }
}
