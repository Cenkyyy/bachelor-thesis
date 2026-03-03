using System.Collections.Generic;
using UnityEngine;

public static class GridAStarPathfinder
{
    private readonly struct GridNode
    {
        public readonly Vector2Int Cell;
        public readonly float FScore;

        public GridNode(Vector2Int cell, float fScore)
        {
            Cell = cell;
            FScore = fScore;
        }
    }

    public static bool TryBuildPath(Vector2 startWorld, Vector2 goalWorld, float nodeStep, LayerMask obstacleMask, float probeRadius, int maxIterations, List<Vector2> output)
    {
        output.Clear();

        var startCell = ToCell(startWorld, nodeStep);
        var goalCell = ToCell(goalWorld, nodeStep);

        if (IsBlocked(goalCell, nodeStep, obstacleMask, probeRadius))
        {
            return false;
        }

        var openList = new List<GridNode> { new GridNode(startCell, Heuristic(startCell, goalCell)) };
        var closedList = new HashSet<Vector2Int>();

        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, float> { [startCell] = 0f };

        var iterations = 0;
        while (openList.Count > 0 && iterations < maxIterations)
        {
            iterations++;

            // Find the node in the open list with the lowest F score
            var currentIndex = 0;
            var bestF = openList[0].FScore;
            for (var i = 1; i < openList.Count; i++)
            {
                if (openList[i].FScore < bestF)
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

            closedList.Add(current);

            // Iterate through the neighbors of the current node
            foreach (var neighbor in GetNeighbors(current))
            {
                if (closedList.Contains(neighbor))
                {
                    continue;
                }

                if (IsBlocked(neighbor, nodeStep, obstacleMask, probeRadius))
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
                var f = tentativeG + Heuristic(neighbor, goalCell);

                // Check if the neighbor is already in the open list and update its F score if necessary
                var found = false;
                for (var i = 0; i < openList.Count; i++)
                {
                    if (openList[i].Cell != neighbor)
                    {
                        continue;
                    }

                    openList[i] = new GridNode(neighbor, f);
                    found = true;
                    break;
                }

                // If the neighbor is not in the open list, add it
                if (!found)
                {
                    openList.Add(new GridNode(neighbor, f));
                }
            }
        }

        // No path found within iteration limit
        return false;
    }

    private static IEnumerable<Vector2Int> GetNeighbors(Vector2Int cell)
    {
        for (var y = -1; y <= 1; y++)
        {
            for (var x = -1; x <= 1; x++)
            {
                if (x == 0 && y == 0)
                {
                    continue;
                }

                yield return new Vector2Int(cell.x + x, cell.y + y);
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

    private static bool IsBlocked(Vector2Int cell, float nodeStep, LayerMask obstacleMask, float probeRadius)
    {
        var world = ToWorld(cell, nodeStep);
        return Physics2D.OverlapCircle(world, probeRadius, obstacleMask) != null;
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
