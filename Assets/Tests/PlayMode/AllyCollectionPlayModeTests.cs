using System.Collections;
using DefendAndCollect.Agents;
using DefendAndCollect.Grid;
using DefendAndCollect.Pathfinding;
using DefendAndCollect.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DefendAndCollect.Tests.PlayMode
{
    public sealed class AllyCollectionPlayModeTests
    {
        [UnityTest]
        public IEnumerator Ally_CompletesCollectionAndDelivery_WithBfs()
        {
            RunCollectionCycle(PathfindingAlgorithm.Bfs);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ally_CompletesCollectionAndDelivery_WithAStar()
        {
            RunCollectionCycle(PathfindingAlgorithm.AStar);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ally_DeliveredCountIncrementsOnlyAtDeposit()
        {
            GameGrid grid = new GameGrid(10, 10);
            ResourceNode resource = new ResourceNode(new Vector2Int(3, 0));
            AllyAgent ally = CreateAlly(
                grid,
                new Vector2Int(0, 0),
                resource,
                new Vector2Int(0, 2),
                12f,
                PathfindingAlgorithm.AStar,
                NavigationMode.UniformCost);

            for (int i = 0; i < 600 && ally.CollectCount == 0; i++)
            {
                ally.Advance(0.05f);
            }

            Assert.That(ally.CollectCount, Is.EqualTo(1));
            Assert.That(ally.DeliveredCount, Is.Zero);
            Assert.That(ally.CurrentState, Is.EqualTo(AllyState.TransportToDeposit));

            for (int i = 0; i < 600 && ally.DeliveredCount == 0; i++)
            {
                ally.Advance(0.05f);
            }

            Assert.That(ally.DeliveredCount, Is.EqualTo(1));

            Object.Destroy(ally.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ally_RepathsWhenGridChangesMidRoute()
        {
            GameGrid grid = new GameGrid(10, 10);
            ResourceNode resource = new ResourceNode(new Vector2Int(4, 0));
            AllyAgent ally = CreateAlly(
                grid,
                new Vector2Int(0, 0),
                resource,
                new Vector2Int(0, 2),
                4f,
                PathfindingAlgorithm.Bfs,
                NavigationMode.UniformCost);

            for (int i = 0; i < 100 && !ally.IsMoving; i++)
            {
                ally.Advance(0.05f);
            }

            Assert.That(ally.IsMoving, Is.True);

            Vector2Int blockedCell = new Vector2Int(2, 0);
            Assert.That(ally.Cell, Is.Not.EqualTo(blockedCell));

            grid.SetBlocked(blockedCell, true);

            Assert.That(ally.RepathCount, Is.GreaterThanOrEqualTo(1));

            for (int i = 0; i < 600 && ally.CollectCount == 0; i++)
            {
                ally.Advance(0.05f);
                Assert.That(ally.Cell, Is.Not.EqualTo(blockedCell));
            }

            Assert.That(ally.CollectCount, Is.EqualTo(1));
            Assert.That(resource.IsAvailable, Is.False);

            Object.Destroy(ally.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ally_AppliesNavigationChangeWhileMoving()
        {
            GameGrid grid = new GameGrid(12, 6);
            ResourceNode resource = new ResourceNode(new Vector2Int(6, 0));
            AllyAgent ally = CreateAlly(
                grid,
                new Vector2Int(0, 0),
                resource,
                new Vector2Int(0, 2),
                4f,
                PathfindingAlgorithm.AStar,
                NavigationMode.UniformCost);

            for (int i = 0; i < 100 && !ally.IsMoving; i++)
            {
                ally.Advance(0.05f);
            }

            Assert.That(ally.IsMoving, Is.True);
            Assert.That(ally.ActiveAlgorithm, Is.EqualTo(PathfindingAlgorithm.AStar));

            ally.SetNavigation(PathfindingAlgorithm.Bfs, NavigationMode.TerrainCost);

            Assert.That(ally.ActiveAlgorithm, Is.EqualTo(PathfindingAlgorithm.Bfs));
            Assert.That(ally.ActiveNavigationMode, Is.EqualTo(NavigationMode.TerrainCost));
            Assert.That(ally.RepathCount, Is.GreaterThanOrEqualTo(1));

            for (int i = 0; i < 600 && ally.CollectCount == 0; i++)
            {
                ally.Advance(0.05f);
            }

            Assert.That(ally.CollectCount, Is.EqualTo(1));

            Object.Destroy(ally.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ally_SkipsUnreachableNearestResource_AndPicksNextInRadius()
        {
            GameGrid grid = new GameGrid(8, 4);
            ResourceNode isolated = new ResourceNode(new Vector2Int(3, 1));
            ResourceNode reachable = new ResourceNode(new Vector2Int(5, 3));

            grid.SetBlocked(new Vector2Int(2, 1), true);
            grid.SetBlocked(new Vector2Int(4, 1), true);
            grid.SetBlocked(new Vector2Int(3, 0), true);
            grid.SetBlocked(new Vector2Int(3, 2), true);

            AllyAgent ally = CreateAlly(
                grid,
                new Vector2Int(0, 0),
                new[] { isolated, reachable },
                new Vector2Int(0, 2),
                8f,
                PathfindingAlgorithm.AStar,
                NavigationMode.UniformCost,
                6f);

            for (int i = 0; i < 600 && ally.CollectCount == 0; i++)
            {
                ally.Advance(0.05f);
            }

            Assert.That(ally.CollectCount, Is.EqualTo(1));
            Assert.That(reachable.IsAvailable, Is.False);
            Assert.That(isolated.IsAvailable, Is.True, "The isolated resource must be left untouched.");

            Object.Destroy(ally.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ally_RetargetsToReachableResource_WhenRouteIsClosedMidRoute()
        {
            GameGrid grid = new GameGrid(10, 6);
            ResourceNode cutOff = new ResourceNode(new Vector2Int(4, 0));
            ResourceNode reachable = new ResourceNode(new Vector2Int(0, 5));

            AllyAgent ally = CreateAlly(
                grid,
                new Vector2Int(0, 0),
                new[] { cutOff, reachable },
                new Vector2Int(0, 2),
                2f,
                PathfindingAlgorithm.AStar,
                NavigationMode.UniformCost,
                6f);

            for (int i = 0; i < 100 && ally.CurrentResource != cutOff; i++)
            {
                ally.Advance(0.05f);
            }

            Assert.That(ally.CurrentResource, Is.SameAs(cutOff));
            Assert.That(ally.Cell.x, Is.LessThan(2));

            for (int y = 0; y < grid.Height; y++)
            {
                grid.SetBlocked(new Vector2Int(2, y), true);
            }

            for (int i = 0; i < 900 && ally.CollectCount == 0; i++)
            {
                ally.Advance(0.05f);
            }

            Assert.That(ally.CollectCount, Is.EqualTo(1));
            Assert.That(reachable.IsAvailable, Is.False);
            Assert.That(cutOff.IsAvailable, Is.True, "The cut off resource must be left untouched.");

            Object.Destroy(ally.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Demo_ResourcesAreReachableFromDeposit()
        {
            GameObject root = new GameObject("DemoResourceTest");
            root.AddComponent<GridBootstrap>();
            root.AddComponent<PathfindingDebugController>();
            FsmDemoBootstrap demo = root.AddComponent<FsmDemoBootstrap>();

            yield return null;

            Assert.That(demo.Resources, Is.Not.Empty);
            Assert.That(demo.Allies, Is.Not.Empty);

            Vector2Int deposit = demo.Allies[0].DepositCell;
            GameGrid grid = demo.Allies[0].Grid;
            IPathfinder pathfinder = PathfinderFactory.Create(PathfindingAlgorithm.AStar);

            for (int i = 0; i < demo.Resources.Count; i++)
            {
                Vector2Int cell = demo.Resources[i].Cell;

                Assert.That(
                    Perception.Distance(deposit, cell),
                    Is.LessThanOrEqualTo(demo.DetectionRadius),
                    "Every demo resource must stay inside the detection radius of the deposit, or allies stall.");

                Assert.That(
                    pathfinder.GetPath(grid, deposit, cell, NavigationMode.TerrainCost),
                    Is.Not.Empty,
                    $"Demo resource at ({cell.x}, {cell.y}) must be reachable from the deposit.");
            }

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Demo_AppliesNavigationSelectionToAgents()
        {
            GameObject root = new GameObject("DemoNavigationTest");
            root.AddComponent<GridBootstrap>();
            PathfindingDebugController controller = root.AddComponent<PathfindingDebugController>();
            FsmDemoBootstrap demo = root.AddComponent<FsmDemoBootstrap>();

            yield return null;

            Assert.That(demo.Allies, Is.Not.Empty);
            Assert.That(demo.Enemies, Is.Not.Empty);

            for (int i = 0; i < demo.Allies.Count; i++)
            {
                Assert.That(demo.Allies[i].ActiveAlgorithm, Is.EqualTo(controller.ActiveAlgorithm));
                Assert.That(demo.Allies[i].ActiveNavigationMode, Is.EqualTo(controller.ActiveNavigationMode));
            }

            controller.SelectAlgorithm(PathfindingAlgorithm.AStar);
            controller.SelectNavigationMode(NavigationMode.TerrainCost);

            for (int i = 0; i < demo.Allies.Count; i++)
            {
                Assert.That(demo.Allies[i].ActiveAlgorithm, Is.EqualTo(PathfindingAlgorithm.AStar));
                Assert.That(demo.Allies[i].ActiveNavigationMode, Is.EqualTo(NavigationMode.TerrainCost));
            }

            for (int i = 0; i < demo.Enemies.Count; i++)
            {
                Assert.That(demo.Enemies[i].ActiveAlgorithm, Is.EqualTo(PathfindingAlgorithm.AStar));
                Assert.That(demo.Enemies[i].ActiveNavigationMode, Is.EqualTo(NavigationMode.TerrainCost));
            }

            Object.Destroy(root);
            yield return null;
        }

        private static void RunCollectionCycle(PathfindingAlgorithm algorithm)
        {
            GameGrid grid = new GameGrid(10, 10);
            ResourceNode resource = new ResourceNode(new Vector2Int(3, 0));
            AllyAgent ally = CreateAlly(
                grid,
                new Vector2Int(0, 0),
                resource,
                new Vector2Int(0, 2),
                12f,
                algorithm,
                NavigationMode.UniformCost);

            for (int i = 0; i < 600 && ally.DeliveredCount == 0; i++)
            {
                ally.Advance(0.05f);
            }

            Assert.That(ally.CollectCount, Is.EqualTo(1), $"{algorithm} should collect the resource.");
            Assert.That(ally.DeliveredCount, Is.EqualTo(1), $"{algorithm} should deliver the resource.");
            Assert.That(resource.IsAvailable, Is.False, $"{algorithm} should mark the resource as taken.");

            Object.Destroy(ally.gameObject);
        }

        private static AllyAgent CreateAlly(
            GameGrid grid,
            Vector2Int startCell,
            ResourceNode[] resources,
            Vector2Int deposit,
            float speed,
            PathfindingAlgorithm algorithm,
            NavigationMode mode,
            float detectionRadius = 10f)
        {
            GameObject allyObject = new GameObject("AllyCollectionTest");
            AllyAgent ally = allyObject.AddComponent<AllyAgent>();
            ally.Configure(
                grid,
                startCell,
                resources,
                deposit,
                speed,
                algorithm,
                mode,
                null,
                detectionRadius);
            ally.enabled = false;
            return ally;
        }

        private static AllyAgent CreateAlly(
            GameGrid grid,
            Vector2Int startCell,
            ResourceNode resource,
            Vector2Int deposit,
            float speed,
            PathfindingAlgorithm algorithm,
            NavigationMode mode,
            float detectionRadius = 10f)
        {
            GameObject allyObject = new GameObject("AllyCollectionTest");
            AllyAgent ally = allyObject.AddComponent<AllyAgent>();
            ally.Configure(
                grid,
                startCell,
                new[] { resource },
                deposit,
                speed,
                algorithm,
                mode,
                null,
                detectionRadius);
            ally.enabled = false;
            return ally;
        }
    }
}
