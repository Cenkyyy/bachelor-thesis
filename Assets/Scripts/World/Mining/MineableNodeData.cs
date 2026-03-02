using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "World/Mineable Node")]
public sealed class MineableNodeData : ScriptableObject
{
    [field: Header("Requirements")]
    [field: SerializeField] public ToolType RequiredToolType { get; private set; } = ToolType.Pickaxe;
    [field: SerializeField] public ToolTier MinimumTier { get; private set; } = ToolTier.Wooden;
    [field: SerializeField] public bool AllowHandMining { get; private set; } = true;

    [field: Header("Durability")]
    [field: SerializeField] public float MaxDurability { get; private set; } = 10f;
    [field: SerializeField] public float ToolPowerMultiplier { get; private set; } = 1f;

    [field: Header("Drops")]
    [SerializeField] private List<MiningDropEntry> _drops = new List<MiningDropEntry>();
    public IReadOnlyList<MiningDropEntry> Drops => _drops;

    [field: Header("Memory Ore")]
    [field: SerializeField] public bool GrantsMemoryXP { get; private set; } = false;
    [field: SerializeField] public int MemoryXpAmount { get; private set; } = 0;
}

[Serializable]
public struct MiningDropEntry
{
    [SerializeField] private ItemData _item;
    [SerializeField] private int _minAmount;
    [SerializeField] private int _maxAmount;

    public ItemData Item => _item;

    public int RollAmount()
    {
        if (_item == null)
            return 0;

        var min = Mathf.Max(0, _minAmount);
        var max = Mathf.Max(min, _maxAmount);
        return UnityEngine.Random.Range(min, max + 1);
    }
}
