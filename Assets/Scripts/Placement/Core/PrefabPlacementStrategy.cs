using UnityEngine;

/// <summary>
/// Placement strategy that instantiates a prefab from a placeable item.
/// </summary>
public sealed class PrefabPlacementStrategy : IPlacementStrategy
{
    public bool CanPlace(IPlaceableItem placeableItem)
    {
        return placeableItem is IPrefabPlaceableItem prefabPlaceableItem && prefabPlaceableItem.Prefab != null;
    }

    public bool CanPreview(IPlaceableItem placeableItem)
    {
        return CanPlace(placeableItem);
    }

    public void Place(IPlaceableItem placeableItem, Vector3 targetPosition, Transform parent)
    {
        if (placeableItem is not IPrefabPlaceableItem prefabPlaceableItem)
            return;
            
        var instance = Object.Instantiate(prefabPlaceableItem.Prefab, targetPosition, Quaternion.identity, parent);
        WorldNavigationObstacle.AttachTo(instance);
    }

    public void UpdatePreview(
        IPlaceableItem placeableItem,
        Vector3 targetPosition,
        bool canPlaceAtTarget,
        Transform previewParent,
        PlacementPreviewState previewState,
        float previewAlpha,
        Color validColor,
        Color invalidColor)
    {
        if (placeableItem is not IPrefabPlaceableItem prefabPlaceableItem)
            return;

        if (previewState.Instance == null || previewState.Source != prefabPlaceableItem.Prefab)
        {
            DestroyPreview(previewState);

            previewState.Instance = Object.Instantiate(prefabPlaceableItem.Prefab, Vector3.zero, Quaternion.identity, previewParent);
            previewState.Instance.name = $"{prefabPlaceableItem.Prefab.name}_Preview";
            previewState.Source = prefabPlaceableItem.Prefab;
            previewState.Renderer = previewState.Instance.GetComponentInChildren<SpriteRenderer>(true);

            var colliders = previewState.Instance.GetComponentsInChildren<Collider2D>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            var behaviours = previewState.Instance.GetComponentsInChildren<MonoBehaviour>(true);
            for (var i = 0; i < behaviours.Length; i++)
            {
                behaviours[i].enabled = false;
            }
        }

        previewState.Instance.SetActive(true);
        previewState.Instance.transform.position = targetPosition;

        var previewColor = canPlaceAtTarget ? validColor : invalidColor;
        previewColor.a = previewAlpha;
        if (previewState.Renderer != null)
            previewState.Renderer.color = previewColor;
    }

    private static void DestroyPreview(PlacementPreviewState previewState)
    {
        if (previewState.Instance != null)
            Object.Destroy(previewState.Instance);

        previewState.Clear();
    }
}
