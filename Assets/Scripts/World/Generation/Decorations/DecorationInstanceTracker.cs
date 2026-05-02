using UnityEngine;

public sealed class DecorationInstanceTracker : MonoBehaviour
{
    private string _instanceId;
    private DecorationChunkGenerator _owner;
    private PrefabMineableRuntimeData _node;

    public void Initialize(string instanceId, DecorationChunkGenerator owner, PrefabMineableRuntimeData node)
    {
        if (_node != null)
            _node.OnDepleted -= HandleNodeDepleted;

        _instanceId = instanceId;
        _owner = owner;
        _node = node;

        if (_node != null)
            _node.OnDepleted += HandleNodeDepleted;
    }

    private void OnDestroy()
    {
        if (_node != null)
            _node.OnDepleted -= HandleNodeDepleted;
    }

    private void HandleNodeDepleted(PrefabMineableRuntimeData node)
    {
        if (_owner == null)
            return;

        _owner.MarkDecorationRemoved(_instanceId);
    }
}
