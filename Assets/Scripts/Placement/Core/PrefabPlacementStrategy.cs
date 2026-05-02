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
            
        Object.Instantiate(prefabPlaceableItem.Prefab, targetPosition, Quaternion.identity, parent);
    }

    public void UpdatePreview(
        IPlaceableItem placeableItem,
        Vector3 targetPosition,
        bool canPlaceAtTarget,
        Transform previewParent,
        ref GameObject previewInstance,
        ref GameObject previewSource,
        ref SpriteRenderer previewRenderer,
        float previewAlpha,
        Color validColor,
        Color invalidColor)
    {
        if (placeableItem is not IPrefabPlaceableItem prefabPlaceableItem)
            return;

        if (previewInstance == null || previewSource != prefabPlaceableItem.Prefab)
        {
            DestroyPreview(ref previewInstance, ref previewSource, ref previewRenderer);

            previewInstance = Object.Instantiate(prefabPlaceableItem.Prefab, Vector3.zero, Quaternion.identity, previewParent);
            previewInstance.name = $"{prefabPlaceableItem.Prefab.name}_Preview";
            previewSource = prefabPlaceableItem.Prefab;
            previewRenderer = previewInstance.GetComponentInChildren<SpriteRenderer>(true);

            var colliders = previewInstance.GetComponentsInChildren<Collider2D>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            var behaviours = previewInstance.GetComponentsInChildren<MonoBehaviour>(true);
            for (var i = 0; i < behaviours.Length; i++)
            {
                behaviours[i].enabled = false;
            }
        }

        previewInstance.SetActive(true);
        previewInstance.transform.position = targetPosition;

        var previewColor = canPlaceAtTarget ? validColor : invalidColor;
        previewColor.a = previewAlpha;
        previewRenderer.color = previewColor;
    }

    private static void DestroyPreview(ref GameObject previewInstance, ref GameObject previewSource, ref SpriteRenderer previewRenderer)
    {
        if (previewInstance != null)
            Object.Destroy(previewInstance);

        previewInstance = null;
        previewSource = null;
        previewRenderer = null;
    }
}
