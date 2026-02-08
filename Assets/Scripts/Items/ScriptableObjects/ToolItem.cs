using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Items/Tool")]
public class ToolItem : Item, IUsable
{
    [field: SerializeField] public int MaxDurability { get; private set; } = 25;
    [field: SerializeField] public ToolType ToolType { get; private set; } = ToolType.None;
    [field: SerializeField] public ToolTier Tier { get; private set; } = ToolTier.Wooden;
    [field: SerializeField] public float MiningPower { get; private set; } = 3f;
    [field: SerializeField] public float DurabilityLossPerSecond { get; private set; } = 1f;

    public ToolItem()
    {
        Category = ItemType.Tool;
    }

    public void Use(GameObject user)
    {
        Debug.Log($"{ItemName} used by {user.name}. Type: {ToolType} Tier: {Tier}.");
    }
}