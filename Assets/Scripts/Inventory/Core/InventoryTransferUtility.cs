/// <summary>
/// Provides shared inventory transfer operations used by inventory UI interactions.
/// </summary>
public static class InventoryTransferUtility
{
    /// <summary>
    /// Moves a stack from one inventory slot into another inventory.
    /// The source slot is cleared when the whole stack moves, or updated with the leftover.
    /// </summary>
    public static void TransferStack(IInventory sourceInventory, int sourceIndex, IInventory destinationInventory)
    {
        if (destinationInventory == null)
            return;

        TransferStackToRange(sourceInventory, sourceIndex, destinationInventory, new SlotRange(0, destinationInventory.Capacity));
    }

    /// <summary>
    /// Moves a stack from one inventory slot into a specific destination range.
    /// The source slot is cleared when the whole stack moves, or updated with the leftover.
    /// </summary>
    public static void TransferStackToRange(IInventory sourceInventory, int sourceIndex, IInventory destinationInventory, SlotRange destinationRange)
    {
        if (sourceInventory == null || destinationInventory == null)
            return;

        var item = sourceInventory.GetItemAt(sourceIndex);
        if (item.IsEmpty)
            return;

        destinationInventory.TryAddItemToRange(item, destinationRange, out var leftover);
        ApplyLeftoverToSource(sourceInventory, sourceIndex, leftover);
    }

    /// <summary>
    /// Quick transfers a player inventory item into the equipment inventory if the source item is compatible
    /// and the equipment slot is empty; otherwise the stack moves between hotbar and backpack ranges.
    /// </summary>
    public static void QuickTransferPlayerInventorySlot(PlayerInventory playerInventory, EquipmentInventory equipmentInventory, int sourceIndex)
    {
        if (playerInventory == null)
            return;

        if (TryEquipFromInventorySlot(playerInventory, sourceIndex, equipmentInventory))
            return;

        var destinationRange = GetOppositePlayerInventoryRange(playerInventory, sourceIndex);
        TransferStackToRange(playerInventory, sourceIndex, playerInventory, destinationRange);
    }

    /// <summary>
    /// Moves a stack from any inventory into the player inventory, preferring hotbar space before backpack space.
    /// </summary>
    public static void TransferStackToPlayerInventoryPreferred(IInventory sourceInventory, int sourceIndex, PlayerInventory playerInventory)
    {
        if (sourceInventory == null || playerInventory == null)
            return;

        var item = sourceInventory.GetItemAt(sourceIndex);
        if (item.IsEmpty)
            return;

        playerInventory.TryAddItemToRange(item, new SlotRange(0, playerInventory.HotbarSize), out var leftoverAfterHotbar);
        if (!leftoverAfterHotbar.IsEmpty)
            playerInventory.TryAddItemToRange(leftoverAfterHotbar, new SlotRange(playerInventory.HotbarSize, playerInventory.Capacity), out leftoverAfterHotbar);

        ApplyLeftoverToSource(sourceInventory, sourceIndex, leftoverAfterHotbar);
    }

    private static bool TryEquipFromInventorySlot(IInventory sourceInventory, int sourceIndex, EquipmentInventory equipmentInventory)
    {
        if (sourceInventory == null || equipmentInventory == null)
            return false;

        var item = sourceInventory.GetItemAt(sourceIndex);
        if (item.IsEmpty || item.Item is not EquipmentItemData)
            return false;

        if (!equipmentInventory.TryAddItemToRange(item, new SlotRange(0, equipmentInventory.Capacity), out var leftover))
            return false;

        ApplyLeftoverToSource(sourceInventory, sourceIndex, leftover);
        return true;
    }

    private static SlotRange GetOppositePlayerInventoryRange(PlayerInventory playerInventory, int sourceIndex)
    {
        var fromHotbar = sourceIndex < playerInventory.HotbarSize;
        if (fromHotbar)
            return new SlotRange(playerInventory.HotbarSize, playerInventory.Capacity);

        return new SlotRange(0, playerInventory.HotbarSize);
    }

    private static void ApplyLeftoverToSource(IInventory sourceInventory, int sourceIndex, InventoryItem leftover)
    {
        if (leftover.IsEmpty)
            sourceInventory.ClearItemAt(sourceIndex);
        else
            sourceInventory.SetItemAt(sourceIndex, leftover);
    }
}
