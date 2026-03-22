using System.Collections.Generic;

public interface IItemTooltipProvider
{
    int Order { get; }
    bool CanHandle(ItemData itemData);
    void AppendLines(Slot slot, InventoryItem inventoryItem, List<ItemTooltipLineRuntimeData> lines);
}
