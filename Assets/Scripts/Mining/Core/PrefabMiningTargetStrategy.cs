using UnityEngine;

public sealed class PrefabMiningTargetStrategy : IMiningTargetStrategy
{
    private readonly LayerMask _mineableMask;

    public PrefabMiningTargetStrategy(LayerMask mineableMask)
    {
        _mineableMask = mineableMask;
    }

    public bool TryResolveTarget(Vector3 worldPosition, out IMineableTarget target)
    {
        target = null;

        var hit = Physics2D.OverlapPoint(worldPosition, _mineableMask);
        if (hit == null)
            return false;

        var node = hit.GetComponent<PrefabMineableRuntimeData>() ?? hit.GetComponentInParent<PrefabMineableRuntimeData>();
        if (node == null)
            return false;

        target = node;
        return true;
    }
}
