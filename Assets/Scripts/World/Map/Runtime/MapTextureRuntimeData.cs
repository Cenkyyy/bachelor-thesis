using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores runtime data for the map texture.
/// </summary>
public sealed class MapTextureRuntimeData
{
    /// <summary>
    /// Stores the resolved base terrain color for each world-data tile, independent of exploration visibility.
    /// </summary>
    public Color32[] TerrainPixelColorByIndex { get; private set; }

    /// <summary>
    /// Tracks whether a base terrain color has already been resolved for each world-data tile.
    /// </summary>
    public bool[] HasTerrainPixelColorByIndex { get; private set; }

    /// <summary>
    /// Stores the current visible map texture pixels, including unexplored color and wall-aware visible colors.
    /// </summary>
    public Color32[] VisiblePixelColorByIndex { get; private set; }

    /// <summary>
    /// Queues texture chunk indices waiting to be uploaded into the map Texture2D.
    /// </summary>
    public Queue<int> PendingTextureChunkIndices { get; private set; } = new();

    /// <summary>
    /// Tracks which texture chunks need to be copied into the map Texture2D.
    /// </summary>
    public bool[] NeedsTextureRefreshByChunkIndex { get; private set; }

    /// <summary>
    /// Prevents the same texture chunk from being queued multiple times before it is processed.
    /// </summary>
    public HashSet<int> QueuedTextureChunkIndices { get; private set; } = new();

    /// <summary>
    /// Creates texture runtime data sized to the generated world.
    /// </summary>
    public MapTextureRuntimeData(int worldWidth, int worldHeight, int chunkCount, Color32 unexploredColor)
    {
        NeedsTextureRefreshByChunkIndex = new bool[Mathf.Max(1, chunkCount)];
        TerrainPixelColorByIndex = new Color32[worldWidth * worldHeight];
        HasTerrainPixelColorByIndex = new bool[worldWidth * worldHeight];
        VisiblePixelColorByIndex = new Color32[worldWidth * worldHeight];

        for (int i = 0; i < VisiblePixelColorByIndex.Length; i++)
        {
            VisiblePixelColorByIndex[i] = unexploredColor;
        }
    }
}
