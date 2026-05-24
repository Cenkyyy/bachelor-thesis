using System.Collections.Generic;
using UnityEngine;

public sealed class WallTileModificationState
{
    private readonly HashSet<Vector2Int> _removedTiles = new HashSet<Vector2Int>();

    public void MarkRemoved(Vector2Int tile)
    {
        _removedTiles.Add(tile);
    }

    public bool IsRemoved(Vector2Int tile)
    {
        return _removedTiles.Contains(tile);
    }
}
