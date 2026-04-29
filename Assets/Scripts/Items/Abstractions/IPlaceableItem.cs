using UnityEngine;

public interface IPlaceableItem
{
    GameObject Prefab { get; }
    Vector2 PlacementCheckSize { get; }
}
