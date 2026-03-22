using System.Collections.Generic;

public sealed class EquipmentTooltipProvider : IItemTooltipProvider
{
    public int Order => 15;

    public bool CanHandle(ItemData itemData)
    {
        return itemData is EquipmentItemData;
    }

    public void AppendLines(Slot slot, InventoryItem inventoryItem, List<ItemTooltipLineRuntimeData> lines)
    {
        if (inventoryItem.Item is not EquipmentItemData equipment)
            return;

        lines.Add(new ItemTooltipLineRuntimeData("Tier", ItemTooltipFormatter.FormatEnumValue(equipment.ProgressionTier)));

        AppendModifiers(equipment.StatBonuses, lines);
    }

    private static void AppendModifiers(IReadOnlyList<ItemStatModifier> modifiers, List<ItemTooltipLineRuntimeData> lines)
    {
        if (modifiers == null)
            return;

        for (int i = 0; i < modifiers.Count; i++)
        {
            var modifier = modifiers[i];
            lines.Add(new ItemTooltipLineRuntimeData(ItemTooltipFormatter.FormatStatName(modifier.Stat), ItemTooltipFormatter.FormatModifierValue(modifier.Value)));
        }
    }
}
