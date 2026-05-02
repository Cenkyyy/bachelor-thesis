using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PrefabMineableRuntimeData : MonoBehaviour, IMineableTarget
{
    [Header("Definition")]
    [SerializeField] private MineableNodeData _data;
    [SerializeField] private Transform _dropAnchor;

    [Header("Feedback")]
    [SerializeField] private WorldTextPopupEmitter _feedbackPopup;
    [SerializeField] private string _higherToolRequiredMessage = "Higher tool is required";

    private float _currentDurability;
    private bool _isBeingMined;
    private float _replenishTimer;

    public Vector3 WorldPosition => transform.position;
    public float MaxDurability => _data != null ? Mathf.Max(0f, _data.MaxDurability) : 0f;
    public float MiningProgressNormalized => MaxDurability <= 0f ? 0f : Mathf.Clamp01(1f - (_currentDurability / MaxDurability));
    public bool HasDamage => _currentDurability < MaxDurability;

    public event Action<float> OnMiningProgressChanged;
    public event Action OnMiningStopped;
    public event Action<PrefabMineableRuntimeData> OnDepleted;

    private void Awake()
    {
        ResetRuntimeState();

        if (_feedbackPopup == null)
            _feedbackPopup = GetComponent<WorldTextPopupEmitter>();

        if (_feedbackPopup == null)
            _feedbackPopup = gameObject.AddComponent<WorldTextPopupEmitter>();
    }

    private void Update()
    {
        if (_isBeingMined || !HasDamage || _replenishTimer <= 0f)
            return;

        if (_replenishTimer <= 0f)
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
        _currentDurability = MaxDurability;
        _isBeingMined = false;
        _replenishTimer = 0f;
    }

    public bool CanBeMinedWith(MiningToolState tool)
    {
        if (_data == null)
            return false;

        if (tool.IsHand)
            return _data.AllowHandMining;

        if (tool.ToolType != _data.RequiredToolType)
            return false;

        return tool.Tier >= _data.MinimumTier;
    }

    public void ShowHigherToolRequiredFeedback()
    {
        _feedbackPopup.ShowMessage(_higherToolRequiredMessage);
    }

    public void NotifyMiningStarted()
    {
        _isBeingMined = true;
        _replenishTimer = 0f;
        RaiseProgressChanged();
    }

    public void ApplyMiningDamage(float basePower, Player miner, WorldItemSpawner dropSpawner)
    {
        NotifyMiningStarted();

        var powerMultiplier = _data != null ? Mathf.Max(0f, _data.ToolPowerMultiplier) : 0f;
        var power = Mathf.Max(0f, basePower * powerMultiplier);
        if (power <= 0f)
            return;

        _currentDurability = Mathf.Max(0f, _currentDurability - power);
        RaiseProgressChanged();

        if (_currentDurability > 0f)
            return;

        HandleBreak(miner, dropSpawner);
    }

    public void NotifyMiningStopped()
    {
        bool wasBeingMined = _isBeingMined;
        _isBeingMined = false;
        if (!HasDamage)
        {
            OnMiningStopped?.Invoke();
            return;
        }

        var replenishDuration = _data != null ? Mathf.Max(0f, _data.ReplenishDurationSeconds) : 0f;
        if (replenishDuration <= 0f)
        {
            ResetDurability();
            RaiseProgressChanged();
            OnMiningStopped?.Invoke();
            return;
        }

        _replenishTimer = replenishDuration;
        OnMiningStopped?.Invoke();
    }

    public bool IsSameTarget(IMineableTarget other)
    {
        var prefabTarget = other as PrefabMineableRuntimeData;
        if (prefabTarget == null)
            return false;
        
        return prefabTarget == this;
    }

    private void RaiseProgressChanged()
    {
        OnMiningProgressChanged?.Invoke(MiningProgressNormalized);
    }

    private void HandleBreak(Player player, WorldItemSpawner dropSpawner)
    {
        if (player != null && _data != null && _data.GrantsMemoryXP)
            player.Data.GainMemoryXP(_data.MemoryXpAmount);
        else if (player != null && _data != null)
            TryDropLoot(player, dropSpawner);

        OnDepleted?.Invoke(this);
        OnMiningStopped?.Invoke();
        Destroy(gameObject);
    }

    private void TryDropLoot(Player player, WorldItemSpawner dropSpawner)
    {
        var dropPosition = _dropAnchor ? _dropAnchor.position : transform.position;
        MiningDropUtility.ResolveDrops(_data.Drops, player, dropSpawner, dropPosition);
    }

    private void ResetDurability()
    {
        _currentDurability = MaxDurability;
        _replenishTimer = 0f;
    }
}
