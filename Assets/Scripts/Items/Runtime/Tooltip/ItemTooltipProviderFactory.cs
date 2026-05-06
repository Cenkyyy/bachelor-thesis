using System.Collections.Generic;

/// <summary>
/// Creates the default tooltip providers used by item tooltip UI.
/// </summary>
public static class ItemTooltipProviderFactory
{
    public static List<IItemTooltipProvider> CreateDefault(PlayerToolDurabilityRuntimeState playerToolDurability)
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
