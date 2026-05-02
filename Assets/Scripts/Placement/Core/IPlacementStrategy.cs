using UnityEngine;

public interface IPlacementStrategy
{
    bool CanPlace(IPlaceableItem placeableItem);
    bool CanPreview(IPlaceableItem placeableItem);
    void Place(IPlaceableItem placeableItem, Vector3 targetPosition, Transform parent);
    void UpdatePreview(
        IPlaceableItem placeableItem,
        Vector3 targetPosition,
        bool canPlaceAtTarget,
        Transform previewParent,
        PlacementPreviewState previewState,
        float previewAlpha,
        Color validColor,
        Color invalidColor);
}
