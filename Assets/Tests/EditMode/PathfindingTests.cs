using System;
using System.Collections.Generic;
using DefendAndCollect.Grid;
using DefendAndCollect.Pathfinding;
using NUnit.Framework;
using UnityEngine;

namespace DefendAndCollect.Tests.EditMode
{
    public sealed class PathfindingTests
    {
        private static readonly PathfindingAlgorithm[] AllAlgorithms =
        {
            PathfindingAlgorithm.Bfs,
            PathfindingAlgorithm.AStar
        };

        [Test]
        public void Bfs_FindsShortestPath_OnOpenGrid()
        {
            GameGrid grid = new GameGrid(10, 10);
            Vector2Int start = new Vector2Int(0, 0);
            Vector2Int end = new Vector2Int(3, 0);

            IReadOnlyList<Vector2Int> path = new BfsPathfinder().GetPath(grid, start, end);

            Assert.That(path.Count, Is.EqualTo(4));
            AssertPathIsValid(grid, path, start, end);
        }

        [Test]
        public void AStar_FindsShortestPath_OnOpenGrid()
        {
            GameGrid grid = new GameGrid(10, 10);
            Vector2Int start = new Vector2Int(0, 0);
            Vector2Int end = new Vector2Int(3, 0);

            IReadOnlyList<Vector2Int> path = new AStarPathfinder().GetPath(grid, start, end);

            Assert.That(path.Count, Is.EqualTo(4));
            AssertPathIsValid(grid, path, start, end);
        }

        [Test]
        public void BothAlgorithms_DetourAroundObstacle()
        {
            GameGrid grid = new GameGrid(7, 7);
            Vector2Int start = new Vector2Int(0, 0);
            Vector2Int end = new Vector2Int(6, 0);

            for (int y = 0; y <= 5; y++)
            {
                grid.SetBlocked(new Vector2Int(3, y), true);
            }

            foreach (PathfindingAlgorithm algorithm in AllAlgorithms)
            {
                IReadOnlyList<Vector2Int> path = PathfinderFactory.Create(algorithm).GetPath(grid, start, end);

                Assert.That(path.Count, Is.EqualTo(19), $"{algorithm} should take the only opening.");
                AssertPathIsValid(grid, path, start, end);
            }
        }

        [Test]
        public void BothAlgorithms_ReturnEmpty_WhenNoPathExists()
        {
            GameGrid grid = new GameGrid(5, 5);
            Vector2Int start = new Vector2Int(0, 2);
            Vector2Int end = new Vector2Int(4, 2);

            for (int y = 0; y < grid.Height; y++)
            {
                grid.SetBlocked(new Vector2Int(2, y), true);
            }

            foreach (PathfindingAlgorithm algorithm in AllAlgorithms)
            {
                IReadOnlyList<Vector2Int> path = PathfinderFactory.Create(algorithm).GetPath(grid, start, end);

                Assert.That(path, Is.Empty, algorithm.ToString());
            }
        }

        [Test]
        public void BothAlgorithms_StartEqualsEnd_ReturnSingleCell()
        {
            GameGrid grid = new GameGrid(5, 5);
            Vector2Int cell = new Vector2Int(2, 3);

            foreach (PathfindingAlgorithm algorithm in AllAlgorithms)
            {
                IReadOnlyList<Vector2Int> path = PathfinderFactory.Create(algorithm).GetPath(grid, cell, cell);

                Assert.That(path.Count, Is.EqualTo(1), algorithm.ToString());
                Assert.That(path[0], Is.EqualTo(cell), algorithm.ToString());
            }
        }

        [Test]
        public void BothAlgorithms_UnusableEndpoints_ReturnEmpty()
        {
            GameGrid grid = new GameGrid(5, 5);
            Vector2Int blocked = new Vector2Int(1, 1);
            Vector2Int passable = new Vector2Int(2, 2);
            grid.SetBlocked(blocked, true);

            Vector2Int[] unusableCells =
            {
                new Vector2Int(-1, 0),
                new Vector2Int(0, -1),
                new Vector2Int(5, 0),
                new Vector2Int(0, 5)
            };

            foreach (PathfindingAlgorithm algorithm in AllAlgorithms)
            {
                IPathfinder pathfinder = PathfinderFactory.Create(algorithm);

                Assert.That(pathfinder.GetPath(grid, blocked, passable), Is.Empty, algorithm.ToString());
                Assert.That(pathfinder.GetPath(grid, passable, blocked), Is.Empty, algorithm.ToString());

                foreach (Vector2Int unusable in unusableCells)
                {
                    Assert.That(pathfinder.GetPath(grid, unusable, passable), Is.Empty, algorithm.ToString());
                    Assert.That(pathfinder.GetPath(grid, passable, unusable), Is.Empty, algorithm.ToString());
                }
            }
        }

        [Test]
        public void BothAlgorithms_NullGrid_Throw()
        {
            foreach (PathfindingAlgorithm algorithm in AllAlgorithms)
            {
                IPathfinder pathfinder = PathfinderFactory.Create(algorithm);

                Assert.Throws<ArgumentNullException>(
                    () => pathfinder.GetPath(null, new Vector2Int(0, 0), new Vector2Int(1, 1)),
                    algorithm.ToString());
            }
        }

        [Test]
        public void Factory_ReturnsSelectedAlgorithm()
        {
            Assert.That(PathfinderFactory.Create(PathfindingAlgorithm.Bfs), Is.TypeOf<BfsPathfinder>());
            Assert.That(PathfinderFactory.Create(PathfindingAlgorithm.AStar), Is.TypeOf<AStarPathfinder>());
            Assert.Throws<ArgumentOutOfRangeException>(() => PathfinderFactory.Create((PathfindingAlgorithm)99));
        }

        [Test]
        public void BothAlgorithms_HandleThirtyByThirtyDefaultLayout()
        {
            GameGrid grid = DefaultGridLayout.Create();
            Vector2Int start = new Vector2Int(0, 0);
            Vector2Int end = new Vector2Int(grid.Width - 1, grid.Height - 1);

            int? bfsLength = null;

            foreach (PathfindingAlgorithm algorithm in AllAlgorithms)
            {
                IReadOnlyList<Vector2Int> path = PathfinderFactory.Create(algorithm).GetPath(grid, start, end);

                Assert.That(path, Is.Not.Empty, algorithm.ToString());
                AssertPathIsValid(grid, path, start, end);

                if (bfsLength.HasValue)
                {
                    Assert.That(path.Count, Is.EqualTo(bfsLength.Value), "A* should match BFS on an unweighted grid.");
                }
                else
                {
                    bfsLength = path.Count;
                }
            }
        }

        [Test]
        public void AStar_UsesTerrainCosts_WhenTerrainModeSelected()
        {
            GameGrid grid = CreateTerrainCorridorGrid();
            Vector2Int start = new Vector2Int(0, 1);
            Vector2Int end = new Vector2Int(4, 1);
            AStarPathfinder pathfinder = new AStarPathfinder();

            IReadOnlyList<Vector2Int> uniform = pathfinder.GetPath(grid, start, end, NavigationMode.UniformCost);
            IReadOnlyList<Vector2Int> terrain = pathfinder.GetPath(grid, start, end, NavigationMode.TerrainCost);

            Assert.That(uniform.Count, Is.EqualTo(5));
            Assert.That(terrain.Count, Is.GreaterThan(uniform.Count));
            Assert.That(PathCost(grid, uniform, NavigationMode.UniformCost), Is.EqualTo(4));
            Assert.That(PathCost(grid, uniform, NavigationMode.TerrainCost), Is.EqualTo(7));
            Assert.That(PathCost(grid, terrain, NavigationMode.TerrainCost), Is.EqualTo(6));
            CollectionAssert.AreNotEqual(uniform, terrain);

            AssertPathIsValid(grid, uniform, start, end);
            AssertPathIsValid(grid, terrain, start, end);
        }

        [Test]
        public void Bfs_IgnoresTerrainCosts()
        {
            GameGrid grid = CreateTerrainCorridorGrid();
            Vector2Int start = new Vector2Int(0, 1);
            Vector2Int end = new Vector2Int(4, 1);
            BfsPathfinder pathfinder = new BfsPathfinder();

            IReadOnlyList<Vector2Int> uniform = pathfinder.GetPath(grid, start, end, NavigationMode.UniformCost);
            IReadOnlyList<Vector2Int> terrain = pathfinder.GetPath(grid, start, end, NavigationMode.TerrainCost);

            CollectionAssert.AreEqual(uniform, terrain);
            Assert.That(uniform.Count, Is.EqualTo(5));
            AssertPathIsValid(grid, uniform, start, end);
        }

        [Test]
        public void AStar_DefaultLayout_ChangesPathWithNavigationMode()
        {
            GameGrid grid = DefaultGridLayout.Create();
            Vector2Int start = new Vector2Int(0, 0);
            Vector2Int end = new Vector2Int(grid.Width - 1, grid.Height - 1);
            AStarPathfinder pathfinder = new AStarPathfinder();

            IReadOnlyList<Vector2Int> uniform = pathfinder.GetPath(grid, start, end, NavigationMode.UniformCost);
            IReadOnlyList<Vector2Int> terrain = pathfinder.GetPath(grid, start, end, NavigationMode.TerrainCost);

            int uniformCostUnderTerrain = PathCost(grid, uniform, NavigationMode.TerrainCost);
            int terrainCost = PathCost(grid, terrain, NavigationMode.TerrainCost);

            Assert.That(terrainCost, Is.LessThan(uniformCostUnderTerrain));
            CollectionAssert.AreNotEqual(uniform, terrain);
            AssertPathIsValid(grid, uniform, start, end);
            AssertPathIsValid(grid, terrain, start, end);
        }

        private static GameGrid CreateTerrainCorridorGrid()
        {
            GameGrid grid = new GameGrid(5, 3);

            for (int x = 1; x <= 3; x++)
            {
                grid.SetTerrain(new Vector2Int(x, 1), TerrainType.Slow);
            }

            return grid;
        }

        private static int PathCost(GameGrid grid, IReadOnlyList<Vector2Int> path, NavigationMode mode)
        {
            int total = 0;

            for (int i = 1; i < path.Count; i++)
            {
                bool slow = mode == NavigationMode.TerrainCost
                    && grid.GetTerrain(path[i]) == TerrainType.Slow;
                total += slow ? 2 : 1;
            }

            return total;
        }

        private static void AssertPathIsValid(
            GameGrid grid,
            IReadOnlyList<Vector2Int> path,
            Vector2Int start,
            Vector2Int end)
        {
            Assert.That(path, Is.Not.Empty);
            Assert.That(path[0], Is.EqualTo(start));
            Assert.That(path[path.Count - 1], Is.EqualTo(end));

            for (int i = 0; i < path.Count; i++)
            {
                Assert.That(grid.IsPassable(path[i]), Is.True, $"Path cell {path[i]} should be passable.");

                if (i > 0)
                {
                    int distance = Math.Abs(path[i].x - path[i - 1].x) + Math.Abs(path[i].y - path[i - 1].y);
                    Assert.That(distance, Is.EqualTo(1), "Path cells must be orthogonally adjacent.");
                }
            }
        }
    }
}
