using UnityEngine;

public sealed class WallTileRuntimeData
{
    public Vector2Int Tile { get; }
    public MineableNodeData MineableData { get; }

    public float CurrentDurability { get; private set; }
    public float MaxDurability => MineableData != null ? Mathf.Max(0f, MineableData.MaxDurability) : 0f;
    public float MiningProgressNormalized => MaxDurability <= 0f ? 0f : Mathf.Clamp01(1f - (CurrentDurability / MaxDurability));
    public bool HasDamage => CurrentDurability < MaxDurability;
    public bool IsAwaitingReplenishTick => !_isBeingMined && HasDamage && _replenishTimer > 0f;

    private bool _isBeingMined;
    private float _replenishTimer;

    public WallTileRuntimeData(Vector2Int tile, MineableNodeData mineableData)
    {
        Tile = tile;
        MineableData = mineableData;
        CurrentDurability = MaxDurability;
    }

    public bool CanBeMinedWith(MiningToolContext tool)
    {
        if (MineableData == null)
            return false;

        if (tool.IsHand)
            return MineableData.AllowHandMining;

        if (tool.ToolType != MineableData.RequiredToolType)
            return false;

        return tool.Tier >= MineableData.MinimumTier;
    }

    public bool ApplyDamage(float basePower)
    {
        if (MineableData == null)
            return false;

        NotifyMiningStarted();

        var powerMultiplier = Mathf.Max(0f, MineableData.ToolPowerMultiplier);
        var power = Mathf.Max(0f, basePower * powerMultiplier);
        if (power <= 0f)
            return false;

        CurrentDurability = Mathf.Max(0f, CurrentDurability - power);
        return CurrentDurability <= 0f;
    }

    public void NotifyMiningStarted()
    {
        _isBeingMined = true;
        _replenishTimer = 0f;
    }

    public bool NotifyMiningStopped()
    {
        _isBeingMined = false;
        if (!HasDamage)
            return false;

        var replenishDuration = Mathf.Max(0f, MineableData.ReplenishDurationSeconds);
        if (replenishDuration <= 0f)
        {
            ResetDurability();
            return true;
        }

        _replenishTimer = replenishDuration;
        return false;
    }

    public bool TickReplenish(float deltaTime)
    {
        if (_isBeingMined || !HasDamage || _replenishTimer <= 0f)
            return false;

        _replenishTimer -= deltaTime;
        if (_replenishTimer > 0f)
            return false;

        ResetDurability();
        return true;
    }

    private void ResetDurability()
    {
        CurrentDurability = MaxDurability;
        _replenishTimer = 0f;
    }
}
