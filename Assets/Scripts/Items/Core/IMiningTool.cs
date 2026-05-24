/// <summary>
/// Contract for item data that can provide mining tool stats to the mining system.
/// </summary>
public interface IMiningTool
{
    ToolType ToolType { get; }
    ToolTier Tier { get; }
    float MiningPower { get; }
    float DurabilityLossPerSecond { get; }
    int MaxDurability { get; }
}
