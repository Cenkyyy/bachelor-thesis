public readonly struct MiningToolContext
{
    public bool IsHand { get; }
    public ToolType ToolType { get; }
    public ToolTier Tier { get; }
    public float Power { get; }
    public float DurabilityLossPerSecond { get; }
    public int SlotIndex { get; }

    public bool ConsumesDurability => !IsHand && DurabilityLossPerSecond > 0f;

    private MiningToolContext(bool isHand, ToolType toolType, ToolTier tier, float power, float durabilityLossPerSecond, int slotIndex)
    {
        IsHand = isHand;
        ToolType = toolType;
        Tier = tier;
        Power = power;
        DurabilityLossPerSecond = durabilityLossPerSecond;
        SlotIndex = slotIndex;
    }

    public static MiningToolContext Hand(float power)
    {
        return new MiningToolContext(true, ToolType.None, ToolTier.None, power, 0f, -1);
    }

    public static MiningToolContext Tool(int slotIndex, ToolItem tool)
    {
        return new MiningToolContext(false, tool.ToolType, tool.Tier, tool.MiningPower, tool.DurabilityLossPerSecond, slotIndex);
    }
}