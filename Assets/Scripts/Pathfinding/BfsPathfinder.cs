using System;
using System.Collections.Generic;
using DefendAndCollect.Grid;
using UnityEngine;

namespace DefendAndCollect.Pathfinding
{
    public sealed class BfsPathfinder : IPathfinder
    {
        public IReadOnlyList<Vector2Int> GetPath(GameGrid grid, Vector2Int start, Vector2Int end)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (!PathfindingSupport.IsEndpointUsable(grid, start) || !PathfindingSupport.IsEndpointUsable(grid, end))
            {
                return PathfindingSupport.EmptyPath;
            }

            if (start == end)
            {
                return new[] { start };
            }

            int width = grid.Width;
            int height = grid.Height;
            bool[] visited = new bool[width * height];
            Vector2Int[] previous = new Vector2Int[width * height];
            Queue<Vector2Int> pending = new Queue<Vector2Int>();

            visited[start.y * width + start.x] = true;
            pending.Enqueue(start);

            while (pending.Count > 0)
            {
                Vector2Int current = pending.Dequeue();

                if (current == end)
                {
                    return PathfindingSupport.Reconstruct(previous, start, end, width);
                }

                foreach (Vector2Int direction in PathfindingSupport.CardinalDirections)
                {
                    Vector2Int neighbor = current + direction;

                    if (!grid.IsPassable(neighbor))
                    {
                        continue;
                    }

                    int neighborIndex = neighbor.y * width + neighbor.x;
                    if (visited[neighborIndex])
                    {
                        continue;
                    }

                    visited[neighborIndex] = true;
                    previous[neighborIndex] = current;
                    pending.Enqueue(neighbor);
                }
            }

            return PathfindingSupport.EmptyPath;
        }
    }
}
