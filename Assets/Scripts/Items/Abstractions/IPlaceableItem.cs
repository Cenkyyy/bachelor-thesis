using UnityEngine;

public interface IPlaceableItem
{
    GameObject PlacementPrefab { get; }
    Vector2 PlacementCheckSize { get; }
}
