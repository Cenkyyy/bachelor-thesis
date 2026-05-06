using System.Collections.Generic;

public sealed class StatModifiersTooltipProvider : IItemTooltipProvider
{
    public int Order => 10;

    public bool CanHandle(ItemData itemData)
    {
        return itemData is EquipmentItemData || itemData is ConsumableItemData;
    }

    public void AppendLines(InventorySlotView slot, InventoryItem inventoryItem, List<ItemTooltipLineRuntimeData> lines)
    {
        if (inventoryItem.Item is EquipmentItemData equipment)
        {
            AppendStatusEffects(equipment.StatusEffect, lines);
            return;
        }

        if (inventoryItem.Item is ConsumableItemData consumable)
        {
            AppendStatusEffects(consumable.StatusEffects, lines);
        }
    }

    private static void AppendStatusEffects(IReadOnlyList<ItemStatusEffect> statusEffects, List<ItemTooltipLineRuntimeData> lines)
    {
        if (statusEffects == null)
            return;

        for (int i = 0; i < statusEffects.Count; i++)
        {
            var modifier = statusEffects[i];
            lines.Add(new ItemTooltipLineRuntimeData(ItemTooltipFormatter.FormatStatName(modifier.StatusEffectType), ItemTooltipFormatter.FormatModifierValue(modifier.Value)));
        }
    }
}
