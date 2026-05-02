using UnityEngine;

public interface IMiningTargetStrategy
{
    bool TryResolveTarget(Vector3 worldPosition, out IMineableTarget target);
}
