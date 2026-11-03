using System;

namespace DefendAndCollect.Pathfinding
{
    public static class PathfinderFactory
    {
        public static IPathfinder Create(PathfindingAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case PathfindingAlgorithm.Bfs:
                    return new BfsPathfinder();
                case PathfindingAlgorithm.AStar:
                    return new AStarPathfinder();
                default:
                    throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "Unknown pathfinding algorithm.");
            }
        }
    }
}
