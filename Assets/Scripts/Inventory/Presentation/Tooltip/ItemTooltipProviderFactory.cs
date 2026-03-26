using System.Collections.Generic;

public static class ItemTooltipProviderFactory
{
    public static List<IItemTooltipProvider> CreateDefault(PlayerToolDurability playerToolDurability)
    {
        var providers = new List<IItemTooltipProvider>
        {
            new StatModifiersTooltipProvider(),
            new EquipmentTooltipProvider(),
            new ConsumableTooltipProvider(),
            new WeaponTooltipProvider(),
            new ToolTooltipProvider(playerToolDurability)
        };

        providers.Sort((a, b) => a.Order.CompareTo(b.Order));
        return providers;
    }
}
