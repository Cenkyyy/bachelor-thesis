using UnityEngine;

public sealed class PlayerMiningController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Player _player;
    [SerializeField] private PlayerToolDurability _toolDurability;
    [SerializeField] private ItemDropSpawner _itemDropSpawner;
    [SerializeField] private Camera _camera;
    [SerializeField] private WallChunkGenerator _wallChunkGenerator;

    [Header("Mining Input")]
    [SerializeField] private GameplayInputBindingsData _inputBindings;
    [SerializeField] private LayerMask _mineableMask;
    [SerializeField] private float _miningRange = 1.5f;

    [Header("Hand Mining")]
    [SerializeField] private bool _allowHandMining = true;
    [SerializeField] private float _handMiningPower = 1f;

    private IMineableTarget _currentTarget;
    private int _currentToolSlot = -1;

    private void Update()
    {
        if (GameStateManager.IsGamePaused)
            return;

        if (PanelManager.Instance != null && PanelManager.Instance.BlocksGameplayInput)
        {
            ResetMining();
            return;
        }

        if (!Input.GetKey(_inputBindings.MiningKey))
        {
            ResetMining();
            return;
        }

        if (!TryResolveTarget(out var target))
        {
            ResetMining();
            return;
        }

        if (!TryResolveTool(out var toolContext))
        {
            ResetMining();
            return;
        }

        if (!target.CanBeMinedWith(toolContext))
        {
            target.ShowHigherToolRequiredFeedback();
            ResetMining();
            return;
        }

        if (_currentTarget != null && !_currentTarget.IsSameTarget(target))
            _currentTarget.NotifyMiningStopped();

        _currentTarget = target;
        _currentToolSlot = toolContext.SlotIndex;

        target.ApplyMiningDamage(toolContext.Power * Time.deltaTime, _player, _itemDropSpawner);

        if (toolContext.ConsumesDurability && _toolDurability != null)
        {
            _toolDurability.TryConsumeDurability(toolContext.SlotIndex, toolContext.DurabilityLossPerSecond * Time.deltaTime, out _, out var broke);
            if (broke)
                ResetMining();
        }
    }

    private void ResetMining()
    {
        _currentTarget?.NotifyMiningStopped();
        _currentTarget = null;
        _currentToolSlot = -1;
    }

    private bool TryResolveTarget(out IMineableTarget target)
    {
        target = null;

        var mouseWorld = _camera ? _camera.ScreenToWorldPoint(Input.mousePosition) : transform.position;
        mouseWorld.z = 0f;

        var hit = Physics2D.OverlapPoint(mouseWorld, _mineableMask);
        if (hit != null)
        {
            var node = hit.GetComponent<MineableNode>() ?? hit.GetComponentInParent<MineableNode>();
            if (node != null)
            {
                var nodeTarget = new MineableNodeMiningTarget(node);
                if (IsWithinMiningRange(nodeTarget.WorldPosition))
                {
                    target = nodeTarget;
                    return true;
                }

                return false;
            }
        }

        if (_wallChunkGenerator == null || !_wallChunkGenerator.TryCreateMiningTarget(mouseWorld, out target))
            return false;

        return IsWithinMiningRange(target.WorldPosition);
    }

    private bool IsWithinMiningRange(Vector3 targetPosition)
    {
        var distance = Vector2.Distance(transform.position, targetPosition);
        return distance <= _miningRange;
    }

    private bool TryResolveTool(out MiningToolContext toolContext)
    {
        toolContext = default;

        if (_player.Inventory == null)
            return false;

        var slotIndex = _player.Inventory.SelectedHotbarIndex;
        var item = _player.Inventory.GetItemAt(slotIndex);

        if (item.Item is IMiningTool tool)
        {
            if (_toolDurability != null && _toolDurability.TryGetToolState(slotIndex, out _, out var currentDurability, out _))
            {
                if (currentDurability <= 0f)
                    return false;
            }

            toolContext = MiningToolContext.Tool(slotIndex, tool);
            return true;
        }

        if (_allowHandMining)
        {
            toolContext = MiningToolContext.Hand(_handMiningPower);
            return true;
        }

        return false;
    }
}
