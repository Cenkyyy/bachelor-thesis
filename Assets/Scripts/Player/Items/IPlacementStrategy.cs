using UnityEngine;

public interface IPlacementStrategy
{
    bool CanHandle(IPlaceableItem placeableItem);
    void Place(IPlaceableItem placeableItem, Vector3 targetPosition, Transform parent);
}
