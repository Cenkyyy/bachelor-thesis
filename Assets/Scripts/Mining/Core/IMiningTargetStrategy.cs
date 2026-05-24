using UnityEngine;

/// <summary>
/// Contract for systems that convert a world position into a mineable target.
/// </summary>
public interface IMiningTargetStrategy
{
    bool TryResolveTarget(Vector3 worldPosition, out IMineableTarget target);
}
