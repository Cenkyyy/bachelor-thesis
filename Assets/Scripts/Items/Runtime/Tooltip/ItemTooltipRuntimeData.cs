using System.Collections.Generic;

/// <summary>
/// Runtime text data prepared for rendering an item tooltip.
/// </summary>
public sealed class ItemTooltipRuntimeData
{
    public string Title { get; set; }
    public string Rarity { get; set; }
    public List<ItemTooltipLineRuntimeData> Lines { get; } = new List<ItemTooltipLineRuntimeData>();
}

/// <summary>
/// Represents one row in an item tooltip.
/// </summary>
public readonly struct ItemTooltipLineRuntimeData
{
    public string Label { get; }
    public string Value { get; }

    public ItemTooltipLineRuntimeData(string label, string value)
    {
        Label = label;
        Value = value;
    }
}
