using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Authored mining node data that defines tool requirements, durability, drops, and memory XP rewards.
/// </summary>
[CreateAssetMenu(menuName = "World/Mineable Node")]
public sealed class MineableNodeData : ScriptableObject
{
    [field: Header("Requirements")]
    [field: SerializeField] public ToolType RequiredToolType { get; private set; } = ToolType.Pickaxe;
    [field: SerializeField] public ToolTier MinimumTier { get; private set; } = ToolTier.Wooden;
    [field: SerializeField] public bool AllowHandMining { get; private set; } = true;

    [field: Header("Durability")]
    [field: SerializeField] public float MaxDurability { get; private set; } = 2f;
    [field: SerializeField] public float ToolPowerMultiplier { get; private set; } = 1f;
    [field: SerializeField, Min(0f)] public float ReplenishDurationSeconds { get; private set; } = 4f;

    [field: Header("Drops")]
    [SerializeField] private List<DropEntry> _drops = new();
    public IReadOnlyList<DropEntry> Drops => _drops;

    [field: Header("Memory Ore")]
    [field: SerializeField] public bool GrantsMemoryXP { get; private set; } = false;
    [field: SerializeField] public int MemoryXpAmount { get; private set; } = 0;
}
