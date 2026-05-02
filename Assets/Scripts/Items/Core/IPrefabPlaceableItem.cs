using UnityEngine;

public interface IPrefabPlaceableItem : IPlaceableItem
{
    GameObject Prefab { get; }
}
