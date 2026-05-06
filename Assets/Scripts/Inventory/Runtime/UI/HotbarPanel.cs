using System.Collections;
using UnityEngine;

public class HotbarPanel : InventoryPanelBase<HotbarSlotView>
{
    protected override int SlotCount => player.Inventory.HotbarSize;
    protected override int GetInventorySlotIndex(int panelSlotIndex) => panelSlotIndex;

    private int _selectedIndex = 0;

    protected override void Start()
    {
        base.Start();
        StartCoroutine(BuildSlotsCoroutine());
    }

    private IEnumerator BuildSlotsCoroutine()
    {
        yield return null;

        slots = new HotbarSlotView[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            slots[i] = Instantiate(slotPrefab, slotParent);
            slots[i].Bind(player.Inventory, i, player.Inventory.GetItemAt(i));
            slots[i]?.SetToDefault();

            // subscribe to slot ui events
            slots[i].OnPointerClicked += HandleSlotClicked;
            slots[i].OnPointerEntered += HandleSlotEnter;
            slots[i].OnPointerExited += HandleSlotExit;
            slots[i].OnSlotDisabled += HandleSlotDisabled;

            if ((i + 1) % slotBuildBatchSize == 0)
                yield return null;
        }

        if (slots.Length > 0)
            slots[_selectedIndex].HighlightSelected();

        // subscribe to hotbar selection changes
        player.Inventory.OnHotbarSelectionChanged += ChangeSelectedSlot;
    }

    protected override void Update()
    {
        base.Update();

        if (PanelManager.Instance != null && PanelManager.Instance.ShouldBlockHotbarScrollInput())
            return;

        var scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            // scroll up - to the left of the hotbar
            player.Inventory.SelectHotbar((_selectedIndex - 1 + slots.Length) % slots.Length);
        }
        else if (scroll < 0f)
        {
            // scroll down - to the right of the hotbar
            player.Inventory.SelectHotbar((_selectedIndex + 1) % slots.Length);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        player.Inventory.OnHotbarSelectionChanged -= ChangeSelectedSlot;
    }

    private void ChangeSelectedSlot(int newIndex)
    {
        if (slots == null || slots.Length == 0)
            return;

        slots[_selectedIndex].SetToDefault();
        _selectedIndex = newIndex;
        slots[_selectedIndex].HighlightSelected();
    }

    public override void RefreshSlot(int hotbarIndex)
    {
        if (slots == null)
            return;

        if (hotbarIndex >= 0 && hotbarIndex < slots.Length)
        {
            var slotItem = player.Inventory.GetItemAt(hotbarIndex);
            slots[hotbarIndex].Refresh(slotItem);
            RefreshCooldownOverlayForPanelSlot(hotbarIndex);
        }
    }
}
