using System;
using UnityEngine;

/// <summary>
/// Runtime mineable target implementation for prefab-based nodes.
/// </summary>
[DisallowMultipleComponent]
public sealed class PrefabMineableRuntimeData : MonoBehaviour, IMineableTarget
{
    [Header("Mineable Definition")]
    [SerializeField] private MineableNodeData _data;
    [SerializeField] private Transform _dropAnchor;

    [Header("Feedback Settings")]
    [SerializeField] private WorldTextPopupEmitter _feedbackPopup;
    [SerializeField] private string _higherToolRequiredMessage = "Higher tool is required";

    public Vector3 WorldPosition => transform.position;
    public float MiningProgressNormalized => Mathf.Clamp01(1f - (_currentDurability / _data.MaxDurability));
    public bool HasDamage => _currentDurability < _data.MaxDurability;
    public bool IsDepleted { get; private set; }
    public bool IsBeingMined => _isBeingMined;

    public event Action<float> OnMiningProgressChanged;
    public event Action OnMiningStopped;
    public event Action<PrefabMineableRuntimeData> OnDepleted;

    private float _currentDurability;
    private bool _isBeingMined;
    private float _replenishTimer;

    private void Awake()
    {
        ResetRuntimeState();
    }

    private void Update()
    {
        if (_isBeingMined || !HasDamage || _replenishTimer <= 0f)
            return;

        _replenishTimer -= Time.deltaTime;
        if (_replenishTimer > 0f)
            return;

        ResetDurability();
        RaiseProgressChanged();
        OnMiningStopped?.Invoke();
    }

    public void ResetRuntimeState()
    {
        _currentDurability = _data.MaxDurability;
        _isBeingMined = false;
        _replenishTimer = 0f;
        IsDepleted = false;
    }

    public bool CanBeMinedWith(MiningToolState tool)
    {
        if (tool.IsHand)
            return _data.AllowHandMining;

        if (tool.ToolType != _data.RequiredToolType)
            return false;

        return tool.Tier >= _data.MinimumTier;
    }

    public void ShowHigherToolRequiredFeedback() => _feedbackPopup.ShowMessage(_higherToolRequiredMessage);

    public void NotifyMiningStarted()
    {
        _isBeingMined = true;
        _replenishTimer = 0f;
        RaiseProgressChanged();
    }

    public void ApplyMiningDamage(float basePower, Player miner, WorldItemSpawner dropSpawner)
    {
        NotifyMiningStarted();

        var finalPower = basePower * _data.ToolPowerMultiplier;
        if (finalPower <= 0f)
            return;

        _currentDurability = Mathf.Max(0f, _currentDurability - finalPower);
        RaiseProgressChanged();

        if (_currentDurability > 0f)
            return;

        HandleBreak(miner, dropSpawner);
    }

    public void NotifyMiningStopped()
    {
        _isBeingMined = false;
        if (!HasDamage)
        {
            OnMiningStopped?.Invoke();
            return;
        }

        if (_data.ReplenishDurationSeconds <= 0f)
        {
            ResetDurability();
            RaiseProgressChanged();
            OnMiningStopped?.Invoke();
            return;
        }

        _replenishTimer = _data.ReplenishDurationSeconds;
        OnMiningStopped?.Invoke();
    }

    public bool IsSameTarget(IMineableTarget other)
    {
        var prefabTarget = other as PrefabMineableRuntimeData;
        if (prefabTarget == null)
            return false;
        
        return prefabTarget == this;
    }

    private void RaiseProgressChanged() => OnMiningProgressChanged?.Invoke(MiningProgressNormalized);

    private void HandleBreak(Player player, WorldItemSpawner dropSpawner)
    {
        if (player != null && _data.GrantsMemoryXP)
            player.Data.GainMemoryXP(_data.MemoryXpAmount);

        if (player != null)
            TryDropLoot(player, dropSpawner);

        OnDepleted?.Invoke(this);
        OnMiningStopped?.Invoke();
        IsDepleted = true;
        Destroy(gameObject);
    }

    private void TryDropLoot(Player player, WorldItemSpawner dropSpawner)
    {
        var dropPosition = _dropAnchor ? _dropAnchor.position : transform.position;
        MiningDropUtility.ResolveDrops(_data.Drops, player, dropSpawner, dropPosition);
    }

    private void ResetDurability()
    {
        _currentDurability = _data.MaxDurability;
        _replenishTimer = 0f;
    }
}
