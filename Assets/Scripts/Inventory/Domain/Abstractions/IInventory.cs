public interface IInventory : IReadOnlyInventory
{
    void SetItemAt(int index, InventoryItem item);
    void ClearItemAt(int index);
    bool TryAddItemToRange(InventoryItem item, SlotRange range, out InventoryItem leftoverItem);
    bool TryRemoveFromRange(InventoryItem item, SlotRange range, out InventoryItem removedItem);
    bool TryMergeInto(InventoryItem item, int toIndex, out InventoryItem leftoverItem);
}
