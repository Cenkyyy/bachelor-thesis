using UnityEngine;

/// <summary>
/// Represents the current state of an active placement preview, including the preview instance.
/// </summary>
public sealed class PlacementPreviewState
{
    public GameObject Instance { get; set; }
    public GameObject Source { get; set; }
    public SpriteRenderer Renderer { get; set; }

    public void Clear()
    {
        Instance = null;
        Source = null;
        Renderer = null;
    }
}
