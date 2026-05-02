using UnityEngine;

public readonly struct MapMarkerRuntimeData
{
    public string Id { get; }
    public Vector3 WorldPosition { get; }

    public MapMarkerRuntimeData(string id, Vector3 worldPosition)
    {
        Id = id;
        WorldPosition = worldPosition;
    }
}
