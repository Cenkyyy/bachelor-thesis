using UnityEngine;

public sealed class PlayerMiningController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Player _player;
    [SerializeField] private PlayerToolDurability _toolDurability;
    [SerializeField] private ItemDropSpawner _itemDropSpawner;
    [SerializeField] private Camera _camera;

    [Header("Mining Input")]
    [SerializeField] private int _mouseButton = 0;
    [SerializeField] private LayerMask _mineableMask;
    [SerializeField] private float _miningRange = 1.5f;

    [Header("Hand Mining")]
    [SerializeField] private bool _allowHandMining = true;
    [SerializeField] private float _handMiningPower = 1f;

    private MineableNode _currentNode;
    private int _currentToolSlot = -1;

    private void Update()
    {
        if (GameStateManager.IsGamePaused)
            return;

        if (!Input.GetMouseButton(_mouseButton))
        {
            ResetMining();
            return;
        }

        if (!TryResolveTarget(out var node))
        {
            ResetMining();
            return;
        }

        if (!TryResolveTool(out var toolContext))
        {
            ResetMining();
            return;
        }

        if (!node.CanBeMinedWith(toolContext))
        {
            node.ShowHigherToolRequiredFeedback();
            ResetMining();
            return;
        }

        if (_currentNode != null && _currentNode != node)
            _currentNode.NotifyMiningStopped();

        _currentNode = node;
        _currentToolSlot = toolContext.SlotIndex;

        node.ApplyMiningDamage(toolContext.Power * Time.deltaTime, _player, _itemDropSpawner);

        if (toolContext.ConsumesDurability && _toolDurability != null)
        {
            _toolDurability.TryConsumeDurability(toolContext.SlotIndex, toolContext.DurabilityLossPerSecond * Time.deltaTime, out _, out var broke);
            if (broke)
                ResetMining();
        }
    }

    private void ResetMining()
    {
        _currentNode = null;
        _currentToolSlot = -1;
    }

    private bool TryResolveTarget(out MineableNode node)
    {
        node = null;

        var mouseWorld = _camera ? _camera.ScreenToWorldPoint(Input.mousePosition) : transform.position;
        mouseWorld.z = 0f;

        var hit = Physics2D.OverlapPoint(mouseWorld, _mineableMask);
        if (hit == null)
            return false;

        node = hit.GetComponent<MineableNode>() ?? hit.GetComponentInParent<MineableNode>();
        if (node == null)
            return false;

        var distance = Vector2.Distance(transform.position, node.transform.position);
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
