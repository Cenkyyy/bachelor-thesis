using UnityEngine;

/// <summary>
/// Controls player mining interactions, including target selection, mining progress, and tool usage within the game world.
/// </summary>
public sealed class PlayerMiningController : MonoBehaviour
{
    private const float HandMiningPower = 1f;

    [Header("Component References")]
    [SerializeField] private Player _player;
    [SerializeField] private PlayerToolDurabilityRuntimeState _toolDurability;
    [SerializeField] private WorldItemSpawner _itemDropSpawner;
    [SerializeField] private Camera _camera;
    [SerializeField] private WallChunkGenerator _wallChunkGenerator;
    [SerializeField] private MiningProgressBarController _miningProgressBarController;

    [Header("Mining Input")]
    [SerializeField] private GameplayInputBindingsData _inputBindings;
    [SerializeField] private LayerMask _mineableMask;
    [SerializeField] private float _miningRange = 1.5f;
    [SerializeField, Min(0.1f)] private float _miningTickIntervalSeconds = 1f;

    private readonly IMiningTargetStrategy[] _targetStrategies = new IMiningTargetStrategy[2];
    private IMineableTarget _currentTarget;
    private int _currentToolSlot = -1;
    private float _miningTickAccumulator;

    private void Awake()
    {
        _targetStrategies[0] = new PrefabMiningTargetStrategy(_mineableMask);
        _targetStrategies[1] = new WallTileMiningTargetStrategy(_wallChunkGenerator);
    }

    private void Update()
    {
        if (GameStateManager.IsGamePaused)
        {
            ResetMining();
            return;
        }

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

        if (!TryGetMiningContext(out var target, out var toolState))
            return;

        BeginOrContinueMining(target, toolState);
        ProcessMiningTicks(target, toolState);
    }

    private bool TryGetMiningContext(out IMineableTarget target, out MiningToolState toolState)
    {
        target = null;
        toolState = default;

        if (!TryResolveTarget(out target))
        {
            ResetMining();
            return false;
        }

        if (!TryResolveTool(target, out toolState))
        {
            if (ShouldShowToolRequirementFeedback(target))
                target.ShowHigherToolRequiredFeedback();

            ResetMining();
            return false;
        }

        if (target.CanBeMinedWith(toolState))
            return true;

        target.ShowHigherToolRequiredFeedback();
        ResetMining();
        return false;
    }

    private void BeginOrContinueMining(IMineableTarget target, MiningToolState toolState)
    {
        bool targetChanged = _currentTarget == null || !_currentTarget.IsSameTarget(target);
        if (_currentTarget != null && targetChanged)
        {
            _miningProgressBarController?.HandleMiningStopped(_currentTarget);
            _currentTarget.NotifyMiningStopped();
            _miningTickAccumulator = 0f;
        }

        var toolChanged = _currentToolSlot != toolState.SlotIndex;
        if (toolChanged)
            _miningTickAccumulator = 0f;

        _currentTarget = target;
        _currentToolSlot = toolState.SlotIndex;

        if (targetChanged || toolChanged)
            _currentTarget.NotifyMiningStarted();

        _miningProgressBarController?.ShowProgress(_currentTarget);

        _miningTickAccumulator += Time.deltaTime;
    }

    private void ProcessMiningTicks(IMineableTarget target, MiningToolState toolState)
    {
        while (_miningTickAccumulator >= _miningTickIntervalSeconds)
        {
            _miningTickAccumulator -= _miningTickIntervalSeconds;
            target.ApplyMiningDamage(toolState.Power * _miningTickIntervalSeconds, _player, _itemDropSpawner);
            _miningProgressBarController?.ShowProgress(target);

            if (target.IsDepleted)
            {
                _miningProgressBarController?.HandleMiningStopped(target);
                ResetMining();
                return;
            }

            if (!toolState.ConsumesDurability || _toolDurability == null)
                return;

            _toolDurability.TryConsumeDurability(toolState.SlotIndex, toolState.DurabilityLossPerSecond * _miningTickIntervalSeconds, out _, out var broke);
            if (!broke)
                continue;

            ResetMining();
            return;
        }
    }

    private void ResetMining()
    {
        _miningProgressBarController?.HandleMiningStopped(_currentTarget);
        _currentTarget?.NotifyMiningStopped();
        _currentTarget = null;
        _currentToolSlot = -1;
        _miningTickAccumulator = 0f;
    }

    private bool TryResolveTarget(out IMineableTarget target)
    {
        target = null;

        var mouseWorld = _camera.ScreenToWorldPoint(Input.mousePosition);
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

    private bool TryResolveTool(IMineableTarget target, out MiningToolState toolState)
    {
        toolState = default;

        if (target != null)
        {
            var handState = MiningToolState.Hand(HandMiningPower);
            if (target.CanBeMinedWith(handState))
            {
                toolState = handState;
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

        toolState = MiningToolState.Tool(slotIndex, tool);
        return true;
    }

    private bool ShouldShowToolRequirementFeedback(IMineableTarget target)
    {
        if (target == null)
            return false;

        return !target.CanBeMinedWith(MiningToolState.Hand(HandMiningPower));
    }

    private bool IsWithinMiningRange(Vector3 targetPosition)
    {
        var distance = Vector2.Distance(transform.position, targetPosition);
        return distance <= _miningRange;
    }
}
