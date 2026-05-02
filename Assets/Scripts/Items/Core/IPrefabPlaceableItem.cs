using UnityEngine;

interface IPrefabPlaceableItem : IPlaceableItem
{
    GameObject Prefab { get; }
}
