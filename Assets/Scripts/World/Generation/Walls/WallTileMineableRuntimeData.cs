using UnityEngine;

public sealed class WallTileMineableRuntimeData : IMineableTarget
{
    public WallData WallData { get; }
    public Vector2Int Tile { get; }
    public Vector3 WorldPosition => _owner != null ? _owner.GetTileCenterWorld(Tile) : Vector3.zero;

    public float CurrentDurability { get; private set; }
    public float MaxDurability => WallData != null && WallData.MineableData != null ? Mathf.Max(0f, WallData.MineableData.MaxDurability) : 0f;
    public float MiningProgressNormalized => MaxDurability <= 0f ? 0f : Mathf.Clamp01(1f - (CurrentDurability / MaxDurability));
    public bool HasDamage => CurrentDurability < MaxDurability;
    public bool IsAwaitingReplenishTick => !_isBeingMined && HasDamage && _replenishTimer > 0f;

    private bool _isBeingMined;
    private float _replenishTimer;
    private readonly WallChunkGenerator _owner;

    public WallTileMineableRuntimeData(WallChunkGenerator owner, Vector2Int tile, WallData wallData)
    {
        _owner = owner;
        Tile = tile;
        WallData = wallData;
        CurrentDurability = MaxDurability;
    }

    public bool CanBeMinedWith(MiningToolState tool)
    {
        if (WallData.MineableData == null)
            return false;

        if (tool.IsHand)
            return WallData.MineableData.AllowHandMining;

        if (tool.ToolType != WallData.MineableData.RequiredToolType)
            return false;

        return tool.Tier >= WallData.MineableData.MinimumTier;
    }

    public bool ApplyDamage(float basePower)
    {
        if (WallData.MineableData == null)
            return false;

        NotifyMiningStarted();

        var powerMultiplier = Mathf.Max(0f, WallData.MineableData.ToolPowerMultiplier);
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

    public void NotifyMiningStopped()
    {
        StopMiningAndScheduleReplenish();
    }

    public bool StopMiningAndScheduleReplenish() 
    {
        _isBeingMined = false;
        if (!HasDamage)
            return false;

        var replenishDuration = Mathf.Max(0f, WallData.MineableData.ReplenishDurationSeconds);
        if (replenishDuration <= 0f)
        {
            ResetDurability();
            return true;
        }

        _replenishTimer = replenishDuration;
        return false;
    }

    public void ShowHigherToolRequiredFeedback()
    {
        _owner?.ShowHigherToolRequiredFeedback(Tile);
    }

    public void ApplyMiningDamage(float basePower, Player miner, WorldItemSpawner dropSpawner)
    {
        _owner?.ApplyMiningDamage(Tile, basePower, miner, dropSpawner);
    }

    public bool IsSameTarget(IMineableTarget other)
    {
        var tileTarget = other as WallTileMineableRuntimeData;
        if (tileTarget == null)
            return false;

        return tileTarget._owner == _owner && tileTarget.Tile == Tile;
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
