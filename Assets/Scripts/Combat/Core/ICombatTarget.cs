using UnityEngine;

/// <summary>
/// Contract for objects that can be found and positioned by combat targeting.
/// </summary>
public interface ICombatTarget
{
    Vector2 Position { get; }
    bool IsAlive { get; }
}
