using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MineableNode : MonoBehaviour
{
    [Header("Definition")]
    [SerializeField] private MineableNodeData _data;
    [SerializeField] private Transform _dropAnchor;

    [Header("Feedback")]
    [SerializeField] private WorldTextPopupEmitter _feedbackPopup;
    [SerializeField] private string _higherToolRequiredMessage = "Higher tool is required";

    private float _currentDurability;
    private bool _isDepleted;
    private bool _isBeingMined;
    private float _replenishTimer;

    public MineableNodeData Data => _data;
    public float CurrentDurability => _currentDurability;
    public float MaxDurability => _data ? _data.MaxDurability : 0f;

    public event Action<float> OnMiningProgressChanged;
    public event Action OnMiningStopped;
    public event Action<MineableNode> OnDepleted;

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
        if (_isDepleted || _isBeingMined || !HasDamage())
            return;

        if (_replenishTimer <= 0f)
            return;

        _replenishTimer -= Time.deltaTime;
        if (_replenishTimer > 0f)
            return;

        ResetDurability();
    }

    public void ResetRuntimeState()
    {
        _isDepleted = false;
        _isBeingMined = false;
        _replenishTimer = 0f;
        _currentDurability = Mathf.Max(0f, _data != null ? _data.MaxDurability : 0f);
    }

    public bool CanBeMinedWith(MiningToolContext tool)
    {
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
        if (_isDepleted)
            return;

        _isBeingMined = true;
        _replenishTimer = 0f;
        RaiseProgressChanged();
    }

    public void ApplyMiningDamage(float basePower, Player miner, WorldItemSpawner dropSpawner)
    {
        if (_isDepleted)
            return;

        NotifyMiningStarted();

        var powerMultiplier = Mathf.Max(0f, _data.ToolPowerMultiplier);
        var power = Mathf.Max(0f, basePower * powerMultiplier);
        if (power <= 0f)
            return;

        _currentDurability = Mathf.Max(0f, _currentDurability - power);
        RaiseProgressChanged();

        if (_currentDurability <= 0f)
        {
            _isDepleted = true;
            HandleBreak(miner, dropSpawner);
        }
    }

    public void NotifyMiningStopped()
    {
        if (_isDepleted)
            return;

        bool wasBeingMined = _isBeingMined;
        _isBeingMined = false;
        if (!HasDamage())
        {
            if (wasBeingMined)
                OnMiningStopped?.Invoke();
            return;
        }


        var replenishDuration = Mathf.Max(0f, _data.ReplenishDurationSeconds);
        if (replenishDuration <= 0f)
        {
            ResetDurability();
            return;
        }

        _replenishTimer = replenishDuration;
    }

    private void RaiseProgressChanged()
    {
        var max = MaxDurability;
        if (max <= 0f)
            return;

        var progress = Mathf.Clamp01(1f - (_currentDurability / max));
        OnMiningProgressChanged?.Invoke(progress);
    }

    private void HandleBreak(Player player, WorldItemSpawner dropSpawner)
    {
        if (player != null && _data.GrantsMemoryXP)
        {
            player.Data.GainMemoryXP(_data.MemoryXpAmount);
        }
        else if (player != null)
        {
            TryDropLoot(player, dropSpawner);
        }

        OnDepleted?.Invoke(this);
        OnMiningStopped?.Invoke();
        Destroy(gameObject);
    }

    private bool HasDamage()
    {
        return _currentDurability < MaxDurability;
    }

    private void ResetDurability()
    {
        _currentDurability = MaxDurability;
        _replenishTimer = 0f;
        RaiseProgressChanged();
        OnMiningStopped?.Invoke();
    }

    private void TryDropLoot(Player player, WorldItemSpawner dropSpawner)
    {
        var dropPosition = _dropAnchor ? _dropAnchor.position : transform.position;
        MiningDropResolver.ResolveDrops(_data.Drops, player, dropSpawner, dropPosition);
    }
}
