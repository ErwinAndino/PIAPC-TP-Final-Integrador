using System;
using System.Collections.Generic;
using DefendAndCollect.Grid;
using UnityEngine;

namespace DefendAndCollect.Pathfinding
{
    internal static class PathfindingSupport
    {
        public static readonly Vector2Int[] CardinalDirections =
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0)
        };

        public static readonly IReadOnlyList<Vector2Int> EmptyPath = Array.Empty<Vector2Int>();

        public static bool IsEndpointUsable(GameGrid grid, Vector2Int cell)
        {
            return grid.IsInBounds(cell) && grid.IsPassable(cell);
        }

        public static int MovementCost(GameGrid grid, Vector2Int cell, NavigationMode mode)
        {
            if (mode == NavigationMode.UniformCost)
            {
                return 1;
            }

            return grid.GetTerrain(cell) == TerrainType.Slow ? 2 : 1;
        }

        public static int PathCost(GameGrid grid, IReadOnlyList<Vector2Int> path, NavigationMode mode)
        {
            int total = 0;

            for (int i = 1; i < path.Count; i++)
            {
                total += MovementCost(grid, path[i], mode);
            }

            return total;
        }

        public static IReadOnlyList<Vector2Int> Reconstruct(Vector2Int[] previous, Vector2Int start, Vector2Int end, int width)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            Vector2Int current = end;

            while (current != start)
            {
                path.Add(current);
                current = previous[current.y * width + current.x];
            }

            path.Add(start);
            path.Reverse();

            return path;
        }
    }
}
