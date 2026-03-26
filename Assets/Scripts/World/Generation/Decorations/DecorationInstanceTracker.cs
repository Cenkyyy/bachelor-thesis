using UnityEngine;

public sealed class DecorationInstanceTracker : MonoBehaviour
{
    private string _instanceId;
    private DecorationChunkGenerator _owner;
    private MineableNode _node;

    public void Initialize(string instanceId, DecorationChunkGenerator owner, MineableNode node)
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

    private void HandleNodeDepleted(MineableNode node)
    {
        if (_owner == null)
            return;

        _owner.MarkDecorationRemoved(_instanceId);
    }
}
