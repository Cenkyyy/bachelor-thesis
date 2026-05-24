using UnityEngine;

/// <summary>
/// Immutable runtime position state for a marker displayed on map views.
/// </summary>
public readonly struct MapMarkerRuntimeState
{
    public string Id { get; }
    public Vector3 WorldPosition { get; }

    /// <summary>
    /// Creates a map marker state with a stable marker id and world position.
    /// </summary>
    public MapMarkerRuntimeState(string id, Vector3 worldPosition)
    {
        Id = id;
        WorldPosition = worldPosition;
    }
}
