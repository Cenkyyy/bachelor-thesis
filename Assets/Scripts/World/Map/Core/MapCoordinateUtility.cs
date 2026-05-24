using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Provides coordinate conversion helpers for map UI rendering.
/// </summary>
public static class MapCoordinateUtility
{
    /// <summary>
    /// Converts a world position into normalized coordinates within the world data bounds.
    /// </summary>
    public static Vector2 WorldToDataNormalized(Tilemap groundTilemap, WorldRuntimeData worldData, Vector3 worldPosition)
    {
        if (groundTilemap == null || worldData == null || worldData.Width <= 0 || worldData.Height <= 0)
            return Vector2.zero;

        var cell = groundTilemap.WorldToCell(worldPosition);
        var dataPos = worldData.CellToData(cell);

        float normalizedX = Mathf.Clamp01(dataPos.x / (float)worldData.Width);
        float normalizedY = Mathf.Clamp01(dataPos.y / (float)worldData.Height);

        return new Vector2(normalizedX, normalizedY);
    }
}
