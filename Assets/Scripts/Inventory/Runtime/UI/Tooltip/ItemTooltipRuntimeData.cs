using System.Collections.Generic;

public sealed class ItemTooltipRuntimeData
{
    public string Title { get; set; }
    public string Rarity { get; set; }
    public List<ItemTooltipLineRuntimeData> Lines { get; } = new List<ItemTooltipLineRuntimeData>();
}

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
