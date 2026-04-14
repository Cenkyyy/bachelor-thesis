using System.Collections.Generic;
using UnityEngine;

public static class GridAStarPathfinder
{
    private const float BlockedGoalPenalty = 5f;
    private static readonly Collider2D[] OverlapResults = new Collider2D[24];

    private readonly struct GridNode
    {
        public readonly Vector2Int Cell;
        public readonly float FScore;
        public readonly float HScore;

        public GridNode(Vector2Int cell, float fScore, float hscore)
        {
            Cell = cell;
            FScore = fScore;
            HScore = hscore;
        }
    }

    public static bool TryBuildPath(
        Vector2 startWorld,
        Vector2 goalWorld,
        float nodeStep,
        LayerMask obstacleMask,
        float probeRadius,
        int maxIterations,
        List<Vector2> output,
        LayerMask additionalBlockedMask = default,
        bool allowPartialPath = true,
        Transform ignoredRoot = null)
    {
        {
            output.Clear();

            var startCell = ToCell(startWorld, nodeStep);
            var goalCell = ToCell(goalWorld, nodeStep);

            var startHeuristic = Heuristic(startCell, goalCell);
            var openList = new List<GridNode> { new GridNode(startCell, startHeuristic, startHeuristic) };
            var closedList = new HashSet<Vector2Int>();

            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, float> { [startCell] = 0f };

            var bestCell = startCell;
            var bestHeuristic = startHeuristic;

            var iterations = 0;
            while (openList.Count > 0 && iterations < maxIterations)
            {
                iterations++;

                // Find the node in the open list with the lowest F score
                var currentIndex = 0;
                var bestF = openList[0].FScore;
                for (var i = 1; i < openList.Count; i++)
                {
                    // Compare F scores, and if they are equal, compare H scores to break ties
                    if (openList[i].FScore < bestF || (Mathf.Approximately(openList[i].FScore, bestF) && openList[i].HScore < openList[currentIndex].HScore))
                    {
                        bestF = openList[i].FScore;
                        currentIndex = i;
                    }
                }

                var current = openList[currentIndex].Cell;
                openList.RemoveAt(currentIndex);

                // If the goal is reached, reconstruct the path
                if (current == goalCell)
                {
                    ReconstructPath(current, cameFrom, nodeStep, output);
                    return output.Count > 0;
                }

                var currentHeuristic = Heuristic(current, goalCell) + (IsBlocked(current, nodeStep, obstacleMask, additionalBlockedMask, probeRadius, ignoredRoot) ? BlockedGoalPenalty : 0f);
                if (currentHeuristic < bestHeuristic)
                {
                    bestHeuristic = currentHeuristic;
                    bestCell = current;
                }

                closedList.Add(current);

                // Iterate through the neighbors of the current node
                foreach (var neighbor in GetNeighbors(current, nodeStep, obstacleMask, additionalBlockedMask, probeRadius, ignoredRoot))
                {
                    if (closedList.Contains(neighbor))
                    {
                        continue;
                    }

                    if (IsBlocked(neighbor, nodeStep, obstacleMask, additionalBlockedMask, probeRadius, ignoredRoot))
                    {
                        continue;
                    }

                    // Calculate tentative G score for the neighbor
                    var tentativeG = gScore[current] + StepCost(current, neighbor);
                    if (gScore.TryGetValue(neighbor, out var knownG) && tentativeG >= knownG)
                    {
                        continue;
                    }

                    // Record the best path to the neighbor
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    var h = Heuristic(neighbor, goalCell);
                    var f = tentativeG + h;

                    // Check if the neighbor is already in the open list and update its F score if necessary
                    var found = false;
                    for (var i = 0; i < openList.Count; i++)
                    {
                        if (openList[i].Cell != neighbor)
                        {
                            continue;
                        }

                        openList[i] = new GridNode(neighbor, f, h);
                        found = true;
                        break;
                    }

                    // If the neighbor is not in the open list, add it
                    if (!found)
                    {
                        openList.Add(new GridNode(neighbor, f, h));
                    }
                }
            }

            if (allowPartialPath && bestCell != startCell)
            {
                ReconstructPath(bestCell, cameFrom, nodeStep, output);
                return output.Count > 0;
            }

            // No path found within iteration limit
            return false;
        }
    }


    private static IEnumerable<Vector2Int> GetNeighbors(Vector2Int cell, float nodeStep, LayerMask obstacleMask, LayerMask additionalBlockedMask, float probeRadius, Transform ignoredRoot)
    {
        for (var y = -1; y <= 1; y++)
        {
            for (var x = -1; x <= 1; x++)
            {
                if (x == 0 && y == 0)
                {
                    continue;
                }

                var neighbor = new Vector2Int(cell.x + x, cell.y + y);

                // For diagonal neighbors, ensure that both adjacent orthogonal neighbors are not blocked to prevent cutting corners
                if (x != 0 && y != 0)
                {
                    var horizontal = new Vector2Int(cell.x + x, cell.y);
                    var vertical = new Vector2Int(cell.x, cell.y + y);
                    if (IsBlocked(horizontal, nodeStep, obstacleMask, additionalBlockedMask, probeRadius, ignoredRoot) || IsBlocked(vertical, nodeStep, obstacleMask, additionalBlockedMask, probeRadius, ignoredRoot))
                    {
                        continue;
                    }
                }

                yield return neighbor;
            }
        }
    }

    private static float StepCost(Vector2Int a, Vector2Int b)
    {
        var dx = Mathf.Abs(a.x - b.x);
        var dy = Mathf.Abs(a.y - b.y);
        return (dx + dy == 1) ? 1f : 1.4142135f;
    }

    private static float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Vector2Int.Distance(a, b);
    }

    private static bool IsBlocked(
        Vector2Int cell,
        float nodeStep,
        LayerMask obstacleMask,
        LayerMask additionalBlockedMask,
        float probeRadius,
        Transform ignoredRoot)
    {
        var world = ToWorld(cell, nodeStep);
        var blockedMask = obstacleMask | additionalBlockedMask;
        var contactFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = blockedMask,
            useTriggers = true
        };

        var hitCount = Physics2D.OverlapCircle(world, probeRadius, contactFilter, OverlapResults);
        for (var i = 0; i < hitCount; i++)
        {
            var collider = OverlapResults[i];
            if (collider == null)
            {
                continue;
            }

            if (ignoredRoot != null)
            {
                var hitTransform = collider.transform;
                if (hitTransform == ignoredRoot || hitTransform.IsChildOf(ignoredRoot))
                {
                    continue;
                }
            }

            return true;
        }

        return false;
    }

    private static Vector2Int ToCell(Vector2 world, float step)
    {
        return new Vector2Int(Mathf.RoundToInt(world.x / step), Mathf.RoundToInt(world.y / step));
    }

    private static Vector2 ToWorld(Vector2Int cell, float step)
    {
        return new Vector2(cell.x * step, cell.y * step);
    }

    private static void ReconstructPath(Vector2Int current, Dictionary<Vector2Int, Vector2Int> cameFrom, float nodeStep, List<Vector2> output)
    {
        output.Add(ToWorld(current, nodeStep));

        while (cameFrom.TryGetValue(current, out var parent))
        {
            current = parent;
            output.Add(ToWorld(current, nodeStep));
        }

        output.Reverse();

        if (output.Count > 0)
        {
            output.RemoveAt(0);
        }
    }
}
