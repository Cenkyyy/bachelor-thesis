using UnityEngine;

/// <summary>
/// Contract for placeable items that create a prefab instance in the world.
/// </summary>
public interface IPrefabPlaceableItem : IPlaceableItem
{
    GameObject Prefab { get; }
}
