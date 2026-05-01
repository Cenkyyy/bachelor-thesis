using UnityEngine;

public static class PlaceablePlacementUtility
{
    private static readonly Collider2D[] _overlapResults = new Collider2D[8];

    public static Vector3 GetSnappedTileCenter(Vector3 worldPosition, Grid worldGrid)
    {
        worldPosition.z = 0f;

        if (worldGrid == null)
            return new Vector3(Mathf.Floor(worldPosition.x) + 0.5f, Mathf.Floor(worldPosition.y) + 0.5f, 0f);

        var cell = worldGrid.WorldToCell(worldPosition);
        var center = worldGrid.GetCellCenterWorld(cell);
        center.z = 0f;
        return center;
    }

    public static bool IsAreaFree(Vector2 center, Vector2 footprint, LayerMask blockingMask)
    {
        var normalizedCheckSize = new Vector2(Mathf.Max(0.01f, footprint.x), Mathf.Max(0.01f, footprint.y));
        var filter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = blockingMask,
            useTriggers = false
        };

        var hitCount = Physics2D.OverlapBox(center, normalizedCheckSize, 0f, filter, _overlapResults);
        return hitCount == 0;
    }
}
