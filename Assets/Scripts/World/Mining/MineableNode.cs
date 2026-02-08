using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MineableNode : MonoBehaviour
{
    [Header("Definition")]
    [SerializeField] private MineableNodeDefinition _definition;
    [SerializeField] private Transform _dropAnchor;

    private float _currentDurability;
    private bool _isDepleted;

    public MineableNodeDefinition Definition => _definition;
    public float CurrentDurability => _currentDurability;
    public float MaxDurability => _definition ? _definition.MaxDurability : 0f;

    public event Action<float> OnMiningProgressChanged;
    public event Action OnMiningStopped;

    private void Awake()
    {
        _currentDurability = Mathf.Max(0f, _definition.MaxDurability);
    }

    public bool CanBeMinedWith(MiningToolContext tool)
    {
        if (tool.IsHand)
            return _definition.AllowHandMining;

        if (tool.ToolType != _definition.RequiredToolType)
            return false;

        return tool.Tier >= _definition.MinimumTier;
    }

    public void ApplyMiningDamage(float basePower, Player miner, ItemDropSpawner dropSpawner)
    {
        if (_isDepleted)
            return;

        var powerMultiplier = Mathf.Max(0f, _definition.ToolPowerMultiplier);
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
        if (player != null && _definition.GrantsMemoryXP)
        {
            player.Data.GainMemoryXP(_definition.MemoryXpAmount);
        }
        else if (player != null)
        {
            TryDropLoot(player, dropSpawner);
        }

        OnMiningStopped?.Invoke();
        Destroy(gameObject);
    }

    private void TryDropLoot(Player player, ItemDropSpawner dropSpawner)
    {
        if (_definition.Drops == null || _definition.Drops.Count == 0)
            return;

        var dropPosition = _dropAnchor ? _dropAnchor.position : transform.position;
        dropPosition.z = 0f;

        for (int i = 0; i < _definition.Drops.Count; i++)
        {
            var entry = _definition.Drops[i];
            var amount = entry.RollAmount();
            if (amount <= 0 || entry.Item == null)
                continue;

            var dropItem = new InventoryItem(entry.Item, amount);
            player.Inventory.TryAddItemToRange(dropItem, new SlotRange(0, player.Inventory.Capacity), out var leftoverItem);

            if (!leftoverItem.IsEmpty && dropSpawner != null)
                dropSpawner.Spawn(leftoverItem, dropPosition);
        }
    }
}