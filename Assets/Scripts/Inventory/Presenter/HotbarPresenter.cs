using UnityEngine;

public class HotbarPresenter : InventoryPresenterBase<HotbarSlot>
{
    protected override int SlotCount => player.Inventory.HotbarSize;

    private int _selectedIndex = 0;

    protected override void Start()
    {
        base.Start();

        slots = new HotbarSlot[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            slots[i] = Instantiate(slotPrefab, slotParent);
            slots[i].Bind(i, player.Inventory.GetItemAt(i));
            slots[i]?.SetToDefault();

            // subscribe to slot ui events
            slots[i].OnPointerClicked += HandleSlotClicked;
            slots[i].OnPointerEntered += HandleSlotEnter;
        }

        if (slots.Length > 0)
            slots[_selectedIndex].HighlightSelected();

        // subscribe to hotbar selection changes
        player.Inventory.OnHotbarSelectionChanged += ChangeSelectedSlot;
    }

    private void Update()
    {
        if (slots == null || slots.Length == 0) 
            return;

        var scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            // scroll up
            player.Inventory.SelectHotbar((_selectedIndex + 1) % slots.Length);
        }
        else if (scroll < 0f)
        {
            // scroll down
            player.Inventory.SelectHotbar((_selectedIndex - 1 + slots.Length) % slots.Length);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (player?.Inventory != null)
            player.Inventory.OnHotbarSelectionChanged -= ChangeSelectedSlot;
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
            slots[hotbarIndex].Refresh(player.Inventory.GetItemAt(hotbarIndex));
        }
    }
}
