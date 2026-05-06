using System.Collections.Generic;

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
            lines.Add(new ItemTooltipLineRuntimeData("Tier", ItemTooltipFormatter.FormatEnumValue(equipment.ProgressionTier)));

        AppendStatusEffects(equipment.StatusEffect, lines);
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
