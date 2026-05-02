using UnityEngine;

public sealed class PlayerMiningController : MonoBehaviour
{
    private const float HandMiningPower = 1f;

    [Header("Refs")]
    [SerializeField] private Player _player;
    [SerializeField] private PlayerToolDurability _toolDurability;
    [SerializeField] private WorldItemSpawner _itemDropSpawner;
    [SerializeField] private Camera _camera;
    [SerializeField] private WallChunkGenerator _wallChunkGenerator;

    [Header("Mining Input")]
    [SerializeField] private GameplayInputBindingsData _inputBindings;
    [SerializeField] private LayerMask _mineableMask;
    [SerializeField] private float _miningRange = 1.5f;
    [SerializeField, Min(0.1f)] private float _miningTickIntervalSeconds = 1f;

    [Header("Hand Mining")]
    [SerializeField] private bool _allowHandMining = true;

    private readonly IMiningTargetStrategy[] _targetStrategies = new IMiningTargetStrategy[2];
    private IMineableTarget _currentTarget;
    private int _currentToolSlot = -1;
    private float _miningTickAccumulator;

    private void Awake()
    {
        _targetStrategies[0] = new PrefabMiningTargetStrategy(_mineableMask);
        _targetStrategies[1] = new TileMiningTargetStrategy(_wallChunkGenerator);
    }

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

        if (!TryResolveTool(target, out var toolContext))
        {
            if (ShouldShowToolRequirementFeedback(target))
                target.ShowHigherToolRequiredFeedback();

            ResetMining();
            return;
        }

        if (!target.CanBeMinedWith(toolContext))
        {
            target.ShowHigherToolRequiredFeedback();
            ResetMining();
            return;
        }

        bool targetChanged = _currentTarget == null || !_currentTarget.IsSameTarget(target);
        if (_currentTarget != null && targetChanged)
        {
            _currentTarget.NotifyMiningStopped();
            _miningTickAccumulator = 0f;
        }

        bool toolChanged = _currentToolSlot != toolContext.SlotIndex;
        if (toolChanged)
            _miningTickAccumulator = 0f;

        _currentTarget = target;
        _currentToolSlot = toolContext.SlotIndex;

        if (targetChanged || toolChanged)
            _currentTarget.NotifyMiningStarted();

        _miningTickAccumulator += Time.deltaTime;

        var tickInterval = Mathf.Max(0.1f, _miningTickIntervalSeconds);
        while (_miningTickAccumulator >= tickInterval)
        {
            _miningTickAccumulator -= tickInterval;
            target.ApplyMiningDamage(toolContext.Power * tickInterval, _player, _itemDropSpawner);

            if (toolContext.ConsumesDurability && _toolDurability != null)
            {
                _toolDurability.TryConsumeDurability(toolContext.SlotIndex, toolContext.DurabilityLossPerSecond * tickInterval, out _, out var broke);
                if (broke)
                {
                    ResetMining();
                    return;
                }
            }
        }
    }

    private bool ShouldShowToolRequirementFeedback(IMineableTarget target)
    {
        if (target == null)
            return false;

        return !target.CanBeMinedWith(MiningToolState.Hand(HandMiningPower));
    }

    private void ResetMining()
    {
        _currentTarget?.NotifyMiningStopped();
        _currentTarget = null;
        _currentToolSlot = -1;
        _miningTickAccumulator = 0f;
    }

    private bool TryResolveTarget(out IMineableTarget target)
    {
        target = null;

        var mouseWorld = _camera ? _camera.ScreenToWorldPoint(Input.mousePosition) : transform.position;
        mouseWorld.z = 0f;

        for (var i = 0; i < _targetStrategies.Length; i++)
        {
            var strategy = _targetStrategies[i];
            if (strategy == null || !strategy.TryResolveTarget(mouseWorld, out var resolvedTarget))
                continue;

            if (!IsWithinMiningRange(resolvedTarget.WorldPosition))
                return false;

            target = resolvedTarget;
            return true;
        }

        return false;
    }

    private bool IsWithinMiningRange(Vector3 targetPosition)
    {
        var distance = Vector2.Distance(transform.position, targetPosition);
        return distance <= _miningRange;
    }

    private bool TryResolveTool(IMineableTarget target, out MiningToolState toolContext)
    {
        toolContext = default;

        if (target != null && _allowHandMining)
        {
            var handContext = MiningToolState.Hand(HandMiningPower);
            if (target.CanBeMinedWith(handContext))
            {
                toolContext = handContext;
                return true;
            }
        }

        if (_player.Inventory == null)
            return false;

        var slotIndex = _player.Inventory.SelectedHotbarIndex;
        var item = _player.Inventory.GetItemAt(slotIndex);

        if (item.Item is not IMiningTool tool)
            return false;

        if (_toolDurability != null && _toolDurability.TryGetToolState(slotIndex, out _, out var currentDurability, out _) && currentDurability <= 0f)
            return false;

        toolContext = MiningToolState.Tool(slotIndex, tool);
        return true;
    }
}
