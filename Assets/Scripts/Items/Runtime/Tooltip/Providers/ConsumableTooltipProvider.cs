using System.Collections.Generic;

/// <summary>
/// Adds consumable restore and cooldown lines to item tooltips.
/// </summary>
public sealed class ConsumableTooltipProvider : IItemTooltipProvider
{
    public int Order => 20;

    public bool CanHandle(ItemData itemData)
    {
        return itemData is ConsumableItemData;
    }

    public void AppendLines(InventorySlotView slot, InventoryItem inventoryItem, List<ItemTooltipLineRuntimeData> lines)
    {
        if (inventoryItem.Item is not ConsumableItemData consumable)
            return;

        if (consumable.RestoreHealth > 0)
            lines.Add(new ItemTooltipLineRuntimeData("Restore Health", ItemTooltipFormatter.FormatInt(consumable.RestoreHealth)));

        if (consumable.RestoreMana > 0)
            lines.Add(new ItemTooltipLineRuntimeData("Restore Mana", ItemTooltipFormatter.FormatInt(consumable.RestoreMana)));

        if (consumable.RestoreHunger > 0)
            lines.Add(new ItemTooltipLineRuntimeData("Restore Hunger", ItemTooltipFormatter.FormatInt(consumable.RestoreHunger)));

        var cooldownSeconds = consumable.GetCooldownSeconds();
        if (cooldownSeconds > 0f)
            lines.Add(new ItemTooltipLineRuntimeData("Cooldown", ItemTooltipFormatter.FormatSeconds(cooldownSeconds)));
    }
}
