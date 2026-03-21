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

    public MineableNodeData Data => _data;
    public float CurrentDurability => _currentDurability;
    public float MaxDurability => _data ? _data.MaxDurability : 0f;

    public event Action<float> OnMiningProgressChanged;
    public event Action OnMiningStopped;
    public event Action<MineableNode> OnDepleted;

    private void Awake()
    {
        _currentDurability = Mathf.Max(0f, _data.MaxDurability);

        if (_feedbackPopup == null)
            _feedbackPopup = GetComponent<WorldTextPopupEmitter>();

        if (_feedbackPopup == null)
            _feedbackPopup = gameObject.AddComponent<WorldTextPopupEmitter>();
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

    public void ApplyMiningDamage(float basePower, Player miner, ItemDropSpawner dropSpawner)
    {
        if (_isDepleted)
            return;

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

        OnMiningStopped?.Invoke();
    }

    private void RaiseProgressChanged()
    {
        var max = MaxDurability;
        if (max <= 0f)
            return;

        var progress = Mathf.Clamp01(1f - (_currentDurability / max));
        OnMiningProgressChanged?.Invoke(progress);
    }

    private void HandleBreak(Player player, ItemDropSpawner dropSpawner)
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

    private void TryDropLoot(Player player, ItemDropSpawner dropSpawner)
    {
        if (_data.Drops == null || _data.Drops.Count == 0)
            return;

        var dropPosition = _dropAnchor ? _dropAnchor.position : transform.position;
        dropPosition.z = 0f;

        for (int i = 0; i < _data.Drops.Count; i++)
        {
            var entry = _data.Drops[i];
            var amount = entry.RollAmount();
            if (amount <= 0 || entry.Item == null)
                continue;

            var dropItem = new InventoryItem(entry.Item, amount);
            player.Inventory.TryAddItemToRange(dropItem, new SlotRange(0, player.Inventory.Capacity), out var leftoverItem);
            ItemPickupFeedReporter.ReportAddedToInventory(dropItem, leftoverItem);

            if (!leftoverItem.IsEmpty && dropSpawner != null)
                dropSpawner.Spawn(leftoverItem, dropPosition);
        }
    }
}
