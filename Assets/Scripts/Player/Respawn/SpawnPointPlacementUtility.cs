using UnityEngine;

public static class SpawnPointPlacementUtility
{
    public static Vector3 ResolveNearestFreePosition(Vector3 desiredPosition, LayerMask obstacleMask, float probeRadius, int maxSearchRadius)
    {
        // in case the spawn point does not have an obstacle on it (e.g. death chest, spawn the player to the closest free tile)
        if (probeRadius <= 0f || !HasObstacleAt(desiredPosition, probeRadius, obstacleMask))
            return desiredPosition;

        // otherwise,iterate through all radii starting from 1 to max search radius
        // and check if the position has an obstacle on it, if not, spawn the player there
        for (var radius = 1; radius <= maxSearchRadius; radius++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                for (var x = -radius; x <= radius; x++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    var candidate = desiredPosition + new Vector3(x, y, 0f);
                    if (!HasObstacleAt(candidate, probeRadius, obstacleMask))
                        return candidate;
                }
            }
        }

        return desiredPosition;
    }

    private static bool HasObstacleAt(Vector3 worldPoint, float probeRadius, LayerMask obstacleMask)
    {
        return Physics2D.OverlapCircle(worldPoint, probeRadius, obstacleMask) != null;
    }
}
