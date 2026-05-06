using System.Collections.Generic;

public sealed class ToolTooltipProvider : IItemTooltipProvider
{
    private readonly PlayerToolDurabilityRuntimeState _playerToolDurability;

    public int Order => 40;

    public ToolTooltipProvider(PlayerToolDurabilityRuntimeState playerToolDurability)
    {
        _playerToolDurability = playerToolDurability;
    }

    public bool CanHandle(ItemData itemData)
    {
        return itemData is IMiningTool;
    }

    public void AppendLines(InventorySlotView slot, InventoryItem inventoryItem, List<ItemTooltipLineRuntimeData> lines)
    {
        if (inventoryItem.Item is not IMiningTool miningTool)
            return;

        lines.Add(new ItemTooltipLineRuntimeData("Mining Power", ItemTooltipFormatter.FormatFloat(miningTool.MiningPower)));
        lines.Add(new ItemTooltipLineRuntimeData("Tier", ItemTooltipFormatter.FormatEnumValue(miningTool.Tier)));

        if (_playerToolDurability != null && slot != null && slot.Owner != null)
        {
            if (_playerToolDurability.TryGetToolState(slot.SlotIndex, out var toolDefinition, out var current, out var max)
                && ReferenceEquals(toolDefinition, inventoryItem.Item))
            {
                var formatted = $"{ItemTooltipFormatter.FormatFloat(current)} / {ItemTooltipFormatter.FormatFloat(max)}";
                lines.Add(new ItemTooltipLineRuntimeData("Durability", formatted));
                return;
            }
        }

        lines.Add(new ItemTooltipLineRuntimeData("Durability", $"{miningTool.MaxDurability} / {miningTool.MaxDurability}"));
    }
}
