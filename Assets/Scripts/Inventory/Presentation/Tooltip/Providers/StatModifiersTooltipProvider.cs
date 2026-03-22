using System.Collections.Generic;

public sealed class StatModifiersTooltipProvider : IItemTooltipProvider
{
    public int Order => 10;

    public bool CanHandle(ItemData itemData)
    {
        return itemData is EquipmentItemData || itemData is ConsumableItemData;
    }

    public void AppendLines(Slot slot, InventoryItem inventoryItem, List<ItemTooltipLineRuntimeData> lines)
    {
        if (inventoryItem.Item is EquipmentItemData equipment)
        {
            AppendModifiers(equipment.StatBonuses, lines);
            return;
        }

        if (inventoryItem.Item is ConsumableItemData consumable)
        {
            AppendModifiers(consumable.TimedModifiers, lines);
        }
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
