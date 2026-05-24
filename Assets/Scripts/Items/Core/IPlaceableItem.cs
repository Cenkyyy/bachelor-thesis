using UnityEngine;

/// <summary>
/// Base contract for item data that can be validated by the placement system.
/// </summary>
public interface IPlaceableItem
{
    Vector2 PlacementCheckSize { get; }
}
