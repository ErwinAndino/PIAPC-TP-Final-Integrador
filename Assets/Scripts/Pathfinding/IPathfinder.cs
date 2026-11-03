using System.Collections.Generic;
using DefendAndCollect.Grid;
using UnityEngine;

namespace DefendAndCollect.Pathfinding
{
    public interface IPathfinder
    {
        IReadOnlyList<Vector2Int> GetPath(
            GameGrid grid,
            Vector2Int start,
            Vector2Int end,
            NavigationMode mode = NavigationMode.UniformCost);
    }
}
