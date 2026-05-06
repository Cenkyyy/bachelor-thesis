using System.Collections.Generic;

/// <summary>
/// Adds item-specific lines to the tooltip body.
/// </summary>
public interface IItemTooltipProvider
{
    int Order { get; }
    bool CanHandle(ItemData itemData);
    void AppendLines(InventorySlotView slot, InventoryItem inventoryItem, List<ItemTooltipLineRuntimeData> lines);
}
