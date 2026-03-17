public sealed class DeathChestDropper
{
    public void MoveBackpackItemsToChest(PlayerInventory playerInventory, IInventory chestInventory)
    {
        if (playerInventory == null || chestInventory == null)
            return;

        var backpackRange = playerInventory.GetBackpackSlotRange();
        var chestRange = new SlotRange(0, chestInventory.Capacity);

        for (int index = backpackRange.StartInclusive; index < backpackRange.EndExclusive; index++)
        {
            var item = playerInventory.GetItemAt(index);
            if (item.IsEmpty)
                continue;

            chestInventory.TryAddItemToRange(item, chestRange, out var leftover);

            if (leftover.IsEmpty)
                playerInventory.ClearItemAt(index);
            else
                playerInventory.SetItemAt(index, leftover);
        }
    }
}
