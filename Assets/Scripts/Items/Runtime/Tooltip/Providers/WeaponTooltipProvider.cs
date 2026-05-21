using System.Collections.Generic;

/// <summary>
/// Adds weapon combat stat lines to item tooltips.
/// </summary>
public sealed class WeaponTooltipProvider : IItemTooltipProvider
{
    public int Order => 30;

    public bool CanHandle(ItemData itemData)
    {
        return itemData is WeaponItemData;
    }

    public void AppendLines(InventorySlotView slot, InventoryItem inventoryItem, List<ItemTooltipLineRuntimeData> lines)
    {
        if (inventoryItem.Item is not WeaponItemData weapon)
            return;

        lines.Add(new ItemTooltipLineRuntimeData("Tier", ItemTooltipFormatter.FormatEnumValue(weapon.Tier)));
        lines.Add(new ItemTooltipLineRuntimeData("Damage", ItemTooltipFormatter.FormatInt(weapon.Damage)));
        lines.Add(new ItemTooltipLineRuntimeData("Mana Cost", ItemTooltipFormatter.FormatInt(weapon.ManaCost)));
    }
}
