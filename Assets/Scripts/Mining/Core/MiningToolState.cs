public readonly struct MiningToolState
{
    public bool IsHand { get; }
    public ToolType ToolType { get; }
    public ToolTier Tier { get; }
    public float Power { get; }
    public float DurabilityLossPerSecond { get; }
    public int SlotIndex { get; }

    public bool ConsumesDurability => !IsHand && DurabilityLossPerSecond > 0f;

    private MiningToolState(bool isHand, ToolType toolType, ToolTier tier, float power, float durabilityLossPerSecond, int slotIndex)
    {
        IsHand = isHand;
        ToolType = toolType;
        Tier = tier;
        Power = power;
        DurabilityLossPerSecond = durabilityLossPerSecond;
        SlotIndex = slotIndex;
    }

    public static MiningToolState Hand(float power)
    {
        return new MiningToolState(true, ToolType.None, ToolTier.None, power, 0f, -1);
    }

    public static MiningToolState Tool(int slotIndex, IMiningTool tool)
    {
        return new MiningToolState(false, tool.ToolType, tool.Tier, tool.MiningPower, tool.DurabilityLossPerSecond, slotIndex);
    }
}
