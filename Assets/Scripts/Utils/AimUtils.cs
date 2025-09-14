using UnityEngine;

public static class AimUtils
{
    /// <summary> Epsilon threshold for "almost zero direction", SQ represent squared magnitude.</summary>
    private const float AIM_EPSILON_SQ = 1e-4f;

    /// <summary>
    /// From origin, computes a normalized mouse aim direction and a spawn position offset by <paramref name="distance"/>.
    /// Falls back to Vector2.down if mouse is exactly on origin.
    /// </summary>
    /// <param name="origin">Origin transform to compute from.</param>
    /// <param name="distance">Distance from origin to spawn position.</param>
    /// <param name="direction">Output normalized direction from origin to mouse position.</param>
    /// <param name="spawnPos">Output spawn position offset from origin by <paramref name="distance"/> in <paramref name="direction"/>.</param>
    /// <param name="camera">Optional camera to use for screen-to-world. If null, uses Camera.main.</param>
    public static void ComputeAim2D(Transform origin, float distance, out Vector2 direction, out Vector3 spawnPos, Camera camera = null)
    {
        var originPos = origin.position;
        var cam = camera ? camera : Camera.main;

        var mouseWorld = cam ? cam.ScreenToWorldPoint(Input.mousePosition) : originPos + Vector3.down;
        mouseWorld.z = 0f;

        direction = (mouseWorld - originPos).normalized;
        if (direction.sqrMagnitude < AIM_EPSILON_SQ)
            direction = Vector2.down;

        spawnPos = originPos + (Vector3)(direction * distance);
    }
}