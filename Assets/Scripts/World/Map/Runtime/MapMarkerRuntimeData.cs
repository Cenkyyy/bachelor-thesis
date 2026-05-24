using UnityEngine;

public readonly struct MapMarkerRuntimeState
{
    public string Id { get; }
    public Vector3 WorldPosition { get; }

    public MapMarkerRuntimeState(string id, Vector3 worldPosition)
    {
        Id = id;
        WorldPosition = worldPosition;
    }
}
