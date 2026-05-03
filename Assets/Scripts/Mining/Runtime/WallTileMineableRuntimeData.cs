using System;
using UnityEngine;

/// <summary>
/// Runtime mineable target implementation for wall tile-based nodes.
/// </summary>
public sealed class WallTileMineableRuntimeData : IMineableTarget
{
    public WallData WallData { get; }
    public Vector2Int Tile { get; }
    public Vector3 WorldPosition => _worldPositionProvider != null ? _worldPositionProvider(Tile) : Vector3.zero;
    public float CurrentDurability { get; private set; }
    public float MiningProgressNormalized => Mathf.Clamp01(1f - (CurrentDurability / WallData.MineableData.MaxDurability));
    public bool IsDepleted { get; private set; }
    public bool IsBeingMined => _isBeingMined;
    public bool HasDamage
    {
        get
        {
            RefreshReplenishState();
            return CurrentDurability < WallData.MineableData.MaxDurability;
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
        CurrentDurability = WallData.MineableData.MaxDurability;
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

    public void ApplyMiningDamage(float basePower, Player miner, WorldItemSpawner dropSpawner)
    {
        if (IsDepleted || WallData.MineableData == null)
            return;

        NotifyMiningStarted();

        var finalPower = basePower * WallData.MineableData.ToolPowerMultiplier;
        if (finalPower <= 0f)
            return;

        CurrentDurability = Mathf.Max(0f, CurrentDurability - finalPower);
        if (CurrentDurability > 0f)
            return;

        MarkDepleted();
        _depletedHandler?.Invoke(this, miner, dropSpawner);
    }

    public void NotifyMiningStarted()
    {
        _isBeingMined = true;
        _replenishTimer = 0f;
    }

    public void NotifyMiningStopped()
    {
        _isBeingMined = false;
        if (!HasDamage)
            return;

        if (WallData.MineableData.ReplenishDurationSeconds <= 0f)
        {
            ResetDurability();
            return;
        }

        _replenishTimer = WallData.MineableData.ReplenishDurationSeconds;
    }

    public void ShowHigherToolRequiredFeedback() => _feedbackPopupEmitter?.ShowMessageAtWorldPosition(_higherToolRequiredMessage, WorldPosition);

    public bool IsSameTarget(IMineableTarget other)
    {
        var tileTarget = other as WallTileMineableRuntimeData;
        if (tileTarget == null)
            return false;

        return tileTarget.Tile == Tile;
    }

    public void MarkDepleted() => IsDepleted = true;

    private void RefreshReplenishState()
    {
        if (_isBeingMined || _replenishTimer <= 0f || CurrentDurability >= WallData.MineableData.MaxDurability)
            return;

        _replenishTimer = _replenishTimer - Time.deltaTime;
        if (_replenishTimer > 0f)
            return;

        ResetDurability();
    }

    private void ResetDurability()
    {
        CurrentDurability = WallData.MineableData.MaxDurability;
        _replenishTimer = 0f;
    }
}
