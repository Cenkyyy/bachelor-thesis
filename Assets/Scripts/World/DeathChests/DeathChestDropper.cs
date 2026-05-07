public sealed class DeathChestDropper
{
    public void MoveBackpackItemsToDeathChest(PlayerInventory playerInventory, IInventory deathChestInventory)
    {
        if (playerInventory == null || deathChestInventory == null)
            return;

        var backpackRange = playerInventory.GetBackpackSlotRange();
        var deathChestRange = new SlotRange(0, deathChestInventory.Capacity);

        for (int index = backpackRange.StartInclusive; index < backpackRange.EndExclusive; index++)
        {
            var item = playerInventory.GetItemAt(index);
            if (item.IsEmpty)
                continue;

            deathChestInventory.TryAddItemToRange(item, deathChestRange, out var leftover);

            if (leftover.IsEmpty)
                playerInventory.ClearItemAt(index);
            else
                playerInventory.SetItemAt(index, leftover);
        }
    }
}
