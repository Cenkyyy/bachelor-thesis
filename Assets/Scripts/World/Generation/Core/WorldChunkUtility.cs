using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class WorldChunkUtility
{
    public static Vector2Int ResolveFocusTile(WorldRuntimeData data, Tilemap groundTilemap, Transform focusTransform)
    {
        if (data == null)
            return Vector2Int.zero;

        if (focusTransform == null || groundTilemap == null)
            return data.SpawnTile;

        var focusCell = groundTilemap.WorldToCell(focusTransform.position);
        var focusTile = data.CellToData(focusCell);

        focusTile.x = Mathf.Clamp(focusTile.x, 0, data.Width - 1);
        focusTile.y = Mathf.Clamp(focusTile.y, 0, data.Height - 1);

        return focusTile;
    }

    public static Vector2Int GetChunkCoordFromTile(Vector2Int tilePos, int chunkSize)
    {
        int safeChunkSize = Mathf.Max(1, chunkSize);
        return new Vector2Int(tilePos.x / safeChunkSize, tilePos.y / safeChunkSize);
    }

    public static List<Vector2Int> BuildChunkSetInRadius(Vector2Int centerChunk, int radius)
    {
        int safeRadius = Mathf.Max(0, radius);
        int sqrRadius = safeRadius * safeRadius;

        var chunks = new List<Vector2Int>();
        for (int y = -safeRadius; y <= safeRadius; y++)
        {
            for (int x = -safeRadius; x <= safeRadius; x++)
            {
                int sqrDistance = (x * x) + (y * y);
                if (sqrDistance > sqrRadius)
                    continue;

                chunks.Add(new Vector2Int(centerChunk.x + x, centerChunk.y + y));
            }
        }

        chunks.Sort((a, b) =>
        {
            int ax = a.x - centerChunk.x;
            int ay = a.y - centerChunk.y;
            int bx = b.x - centerChunk.x;
            int by = b.y - centerChunk.y;

            int aDistance = (ax * ax) + (ay * ay);
            int bDistance = (bx * bx) + (by * by);
            return aDistance.CompareTo(bDistance);
        });

        return chunks;
    }
}
