using UnityEngine;
using UnityEngine.Tilemaps;

public static class MapCoordinateUtility
{
    public static Vector2 WorldToDataNormalized(Tilemap groundTilemap, WorldRuntimeData worldData, Vector3 worldPosition)
    {
        var cell = groundTilemap.WorldToCell(worldPosition);
        var dataPos = worldData.CellToData(cell);

        float normalizedX = Mathf.Clamp01(dataPos.x / (float)worldData.Width);
        float normalizedY = Mathf.Clamp01(dataPos.y / (float)worldData.Height);

        return new Vector2(normalizedX, normalizedY);
    }
}
