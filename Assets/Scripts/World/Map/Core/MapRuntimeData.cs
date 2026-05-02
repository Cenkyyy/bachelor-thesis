using System.Collections.Generic;
using UnityEngine;

public sealed class MapRuntimeData
{
    public bool[] IsChunkDirty { get; private set; }
    public Color32[] ResolvedPixelByIndex { get; private set;  }
    public Color32[] DisplayPixelByIndex {  get; private set; }
    public Queue<int> DirtyChunkQueue { get; private set; } = new();
    public HashSet<int> QueuedChunkIndices { get; private set; } = new();

    public MapRuntimeData(int worldWidth, int worldHeight, int chunkCount, Color32 unexploredColor)
    {
        IsChunkDirty = new bool[Mathf.Max(1, chunkCount)];
        ResolvedPixelByIndex = new Color32[worldWidth * worldHeight];
        DisplayPixelByIndex = new Color32[worldWidth * worldHeight];

        for (int i = 0; i < DisplayPixelByIndex.Length; i++)
            DisplayPixelByIndex[i] = unexploredColor;
    }

    public void ClearDirtyState()
    {
        for (int i = 0; i < IsChunkDirty.Length; i++)
            IsChunkDirty[i] = false;

        DirtyChunkQueue.Clear();
        QueuedChunkIndices.Clear();
    }
}