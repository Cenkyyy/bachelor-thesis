using UnityEngine;

public sealed class MineableNodeMiningTarget : IMineableTarget
{
    private readonly MineableNode _node;

    public MineableNodeMiningTarget(MineableNode node)
    {
        _node = node;
    }

    public Vector3 WorldPosition => _node != null ? _node.transform.position : Vector3.zero;

    public bool CanBeMinedWith(MiningToolContext tool)
    {
        return _node != null && _node.CanBeMinedWith(tool);
    }

    public void ShowHigherToolRequiredFeedback()
    {
        _node?.ShowHigherToolRequiredFeedback();
    }

    public void NotifyMiningStarted()
    {
        _node?.NotifyMiningStarted();
    }

    public void ApplyMiningDamage(float basePower, Player miner, WorldItemSpawner dropSpawner)
    {
        _node?.ApplyMiningDamage(basePower, miner, dropSpawner);
    }

    public void NotifyMiningStopped()
    {
        _node?.NotifyMiningStopped();
    }

    public bool IsSameTarget(IMineableTarget other)
    {
        var nodeTarget = other as MineableNodeMiningTarget;
        if (nodeTarget == null)
            return false;

        return nodeTarget._node == _node;
    }
}
