public interface IMiningTool
{
    ToolType ToolType { get; }
    ToolTier Tier { get; }
    float MiningPower { get; }
    float DurabilityLossPerSecond { get; }
    int MaxDurability { get; }
}
