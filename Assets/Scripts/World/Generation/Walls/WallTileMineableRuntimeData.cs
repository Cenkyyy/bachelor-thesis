using System;
using UnityEngine;

public sealed class WallTileMineableRuntimeData : IMineableTarget
{
    public WallData WallData { get; }
    public Vector2Int Tile { get; }
    public Vector3 WorldPosition => _worldPositionProvider != null ? _worldPositionProvider(Tile) : Vector3.zero;

    public float CurrentDurability { get; private set; }
    public float MaxDurability => WallData != null && WallData.MineableData != null ? Mathf.Max(0f, WallData.MineableData.MaxDurability) : 0f;
    public float MiningProgressNormalized => MaxDurability <= 0f ? 0f : Mathf.Clamp01(1f - (CurrentDurability / MaxDurability));
    public bool IsDepleted { get; private set; }
    public bool IsAwaitingReplenishTick => !_isBeingMined && HasDamage && _replenishTimer > 0f;
    public bool HasDamage
    {
        get
        {
            RefreshReplenishState();
            return CurrentDurability < MaxDurability;
        }
    }


    private bool _isBeingMined;
    private float _replenishTimer;
    private readonly Func<Vector2Int, Vector3> _worldPositionProvider;
    private readonly Action<WallTileMineableRuntimeData, Player, WorldItemSpawner> _depletedHandler;
    private readonly WorldTextPopupEmitter _feedbackPopupEmitter;
    private readonly string _higherToolRequiredMessage;

    public WallTileMineableRuntimeData(
        Vector2Int tile,
        WallData wallData,
        Func<Vector2Int, Vector3> worldPositionProvider,
        Action<WallTileMineableRuntimeData, Player, WorldItemSpawner> depletedHandler,
        WorldTextPopupEmitter feedbackPopupEmitter,
        string higherToolRequiredMessage)
    {
        Tile = tile;
        WallData = wallData;
        _worldPositionProvider = worldPositionProvider;
        _depletedHandler = depletedHandler;
        _feedbackPopupEmitter = feedbackPopupEmitter;
        _higherToolRequiredMessage = higherToolRequiredMessage;
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
        RefreshReplenishState();

        if (!HasDamage)
        {
            StopMiningAndScheduleReplenish();
            return;
        }

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
        RefreshReplenishState();

        if (!HasDamage)
        {
            StopMiningAndScheduleReplenish();
            return;
        }

        StopMiningAndScheduleReplenish();
    }

    public void ApplyMiningDamage(float basePower, Player miner, WorldItemSpawner dropSpawner)
    {
        if (IsDepleted)
            return;

        RefreshReplenishState();
        bool depleted = ApplyDamage(basePower);
        if (!depleted)
            return;

        MarkDepleted();
        _depletedHandler?.Invoke(this, miner, dropSpawner);
    }

    public void MarkDepleted()
    {
        IsDepleted = true;
    }

    public bool IsSameTarget(IMineableTarget other)
    {
        var tileTarget = other as WallTileMineableRuntimeData;
        if (tileTarget == null)
            return false;

        return tileTarget.Tile == Tile;
    }

    private void RefreshReplenishState()
    {
        if (_isBeingMined || _replenishTimer <= 0f || CurrentDurability >= MaxDurability)
            return;

        _replenishTimer = Mathf.Max(0f, _replenishTimer - Time.deltaTime);
        if (_replenishTimer > 0f)
            return;

        ResetDurability();
    }

    private void ResetDurability()
    {
        CurrentDurability = MaxDurability;
        _replenishTimer = 0f;
    }
}
