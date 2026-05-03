using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the runtime durability state of tools equipped by the player, tracking current and maximum durability for
/// each inventory slot and handling durability consumption and breakage.
/// </summary>
public sealed class PlayerToolDurabilityRuntimeState : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Player _player;

    private readonly Dictionary<int, ToolDurabilityState> _durabilityBySlot = new();

    private struct ToolDurabilityState
    {
        public ItemData ToolData;
        public float Current;
        public float Max;
    }

    private void Start()
    {
        StartCoroutine(InitializeDurabilityStateCoroutine());
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

    private IEnumerator InitializeDurabilityStateCoroutine()
    {
        yield return null;
        RefreshAllSlots();
    }

    public bool TryGetToolState(int slotIndex, out ItemData toolDefinition, out float current, out float max)
    {
        toolDefinition = null;
        current = 0f;
        max = 0f;

        if (_player?.Inventory == null)
            return false;

        RefreshSlot(slotIndex);

        if (_durabilityBySlot.TryGetValue(slotIndex, out var state) && state.ToolData != null)
        {
            toolDefinition = state.ToolData;
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
        if (item.Item is IMiningTool miningTool && item.Item != null)
        {
            if (_durabilityBySlot.TryGetValue(index, out var state) && state.ToolData == item.Item)
            {
                if (!Mathf.Approximately(state.Max, miningTool.MaxDurability))
                {
                    state.Max = miningTool.MaxDurability;
                    state.Current = Mathf.Min(state.Current, state.Max);
                    _durabilityBySlot[index] = state;
                }
            }
            else
            {
                _durabilityBySlot[index] = new ToolDurabilityState
                {
                    ToolData = item.Item,
                    Max = miningTool.MaxDurability,
                    Current = miningTool.MaxDurability
                };
            }
        }
        else
        {
            _durabilityBySlot.Remove(index);
        }
    }
}
