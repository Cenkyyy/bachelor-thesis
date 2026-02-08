using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerToolDurability : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Player _player;

    private readonly Dictionary<int, ToolDurabilityState> _durabilityBySlot = new Dictionary<int, ToolDurabilityState>();

    private struct ToolDurabilityState
    {
        public ToolItem Tool;
        public float Current;
        public float Max;
    }

    private void Awake()
    {
        if (_player == null)
            _player = GetComponent<Player>() ?? GetComponentInParent<Player>();

        RefreshAllSlots();
    }

    private void OnEnable()
    {
        if (_player?.Inventory != null)
            _player.Inventory.OnItemChanged += HandleItemChanged;
    }

    private void OnDisable()
    {
        if (_player?.Inventory != null)
            _player.Inventory.OnItemChanged -= HandleItemChanged;
    }

    public bool TryGetToolState(int slotIndex, out ToolItem tool, out float current, out float max)
    {
        tool = null;
        current = 0f;
        max = 0f;

        if (_player?.Inventory == null)
            return false;

        RefreshSlot(slotIndex);

        if (_durabilityBySlot.TryGetValue(slotIndex, out var state) && state.Tool != null)
        {
            tool = state.Tool;
            current = state.Current;
            max = state.Max;
            return true;
        }

        return false;
    }

    public bool TryConsumeDurability(int slotIndex, float amount, out float remaining, out bool broke)
    {
        remaining = 0f;
        broke = false;

        if (amount <= 0f)
            return false;

        if (!_durabilityBySlot.TryGetValue(slotIndex, out var state))
            return false;

        state.Current = Mathf.Max(0f, state.Current - amount);
        remaining = state.Current;

        if (state.Current <= 0f)
        {
            broke = true;
            _durabilityBySlot.Remove(slotIndex);
            _player?.Inventory?.ClearItemAt(slotIndex);
        }
        else
        {
            _durabilityBySlot[slotIndex] = state;
        }

        return true;
    }

    private void HandleItemChanged(int index)
    {
        RefreshSlot(index);
    }

    private void RefreshAllSlots()
    {
        if (_player?.Inventory == null)
            return;

        for (int i = 0; i < _player.Inventory.Capacity; i++)
        {
            RefreshSlot(i);
        }
    }

    private void RefreshSlot(int index)
    {
        if (_player?.Inventory == null)
            return;

        var item = _player.Inventory.GetItemAt(index);
        if (item.Item is ToolItem tool)
        {
            if (_durabilityBySlot.TryGetValue(index, out var state) && state.Tool == tool)
            {
                if (!Mathf.Approximately(state.Max, tool.MaxDurability))
                {
                    state.Max = tool.MaxDurability;
                    state.Current = Mathf.Min(state.Current, state.Max);
                    _durabilityBySlot[index] = state;
                }
            }
            else
            {
                _durabilityBySlot[index] = new ToolDurabilityState
                {
                    Tool = tool,
                    Max = tool.MaxDurability,
                    Current = tool.MaxDurability
                };
            }
        }
        else
        {
            _durabilityBySlot.Remove(index);
        }
    }
}