using UnityEngine;

public sealed class PrefabPlacementStrategy : IPlacementStrategy
{
    public bool CanHandle(IPlaceableItem placeableItem)
    {
        return placeableItem != null && placeableItem.Prefab != null;
    }

    public void Place(IPlaceableItem placeableItem, Vector3 targetPosition, Transform parent)
    {
        Object.Instantiate(placeableItem.Prefab, targetPosition, Quaternion.identity, parent);
    }
}
