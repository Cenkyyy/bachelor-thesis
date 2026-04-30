using UnityEngine;

[CreateAssetMenu(menuName = "Items/Tool Item")]
public class ToolItemData : ItemData, IMiningTool
{
    [field: SerializeField] public int MaxDurability { get; private set; } = 25;
    [field: SerializeField] public ToolType ToolType { get; private set; } = ToolType.None;
    [field: SerializeField] public ToolTier Tier { get; private set; } = ToolTier.Wooden;
    [field: SerializeField] public float MiningPower { get; private set; } = 3f;
    [field: SerializeField] public float DurabilityLossPerSecond { get; private set; } = 1f;

    protected override ItemType? ExpectedCategory => ItemType.Tool;

    protected override void OnValidate()
    {
        base.OnValidate();

        if (MaxDurability < 1)
            MaxDurability = 1;

        if (MiningPower < 0f)
            MiningPower = 0f;

        if (DurabilityLossPerSecond < 0f)
            DurabilityLossPerSecond = 0f;
    }
}
