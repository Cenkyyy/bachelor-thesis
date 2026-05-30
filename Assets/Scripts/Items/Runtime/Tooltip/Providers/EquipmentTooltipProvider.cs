using System.Collections.Generic;

/// <summary>
/// Adds equipment-specific lines to item tooltips.
/// </summary>
public sealed class EquipmentTooltipProvider : IItemTooltipProvider
{
    public int Order => 15;

    public bool CanHandle(ItemData itemData)
    {
        return itemData is EquipmentItemData;
    }

    public void AppendLines(InventorySlotView slot, InventoryItem inventoryItem, List<ItemTooltipLineRuntimeData> lines)
    {
        if (inventoryItem.Item is not EquipmentItemData equipment)
            return;

        if (equipment.HasProgressionTier)
            lines.Add(new ItemTooltipLineRuntimeData("Tier", ItemTooltipFormatter.FormatEnumValue(equipment.Tier)));

    }
}
