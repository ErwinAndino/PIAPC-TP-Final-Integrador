using System.Collections;
using DefendAndCollect.Agents;
using DefendAndCollect.Build;
using DefendAndCollect.Grid;
using DefendAndCollect.Pathfinding;
using DefendAndCollect.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DefendAndCollect.Tests.PlayMode
{
    public sealed class BuildControllerPlayModeTests
    {
        [UnityTest]
        public IEnumerator Placement_BlocksTile_AndUpdatesGridAndView()
        {
            GameObject root = CreateDemoRoot();
            yield return null;

            GameGrid grid = root.GetComponent<GridBootstrap>().Grid;
            GridView gridView = root.GetComponent<GridView>();
            BuildController controller = root.GetComponent<BuildController>();

            Vector2Int cell = new Vector2Int(2, 2);
            int blockedBefore = grid.BlockedCount;

            Assert.That(controller.CanPlace(cell), Is.True);
            Assert.That(controller.TryPlace(cell), Is.True);
            Assert.That(grid.IsBlocked(cell), Is.True);
            Assert.That(grid.BlockedCount, Is.EqualTo(blockedBefore + 1));
            Assert.That(controller.StructureCount, Is.EqualTo(1));
            Assert.That(controller.HasStructure(cell), Is.True);
            Assert.That(controller.TryGetStructure(cell, out StructureType type), Is.True);
            Assert.That(type, Is.EqualTo(StructureType.Wall));

            Transform tile = gridView.transform.Find("Tiles/Tile_2_2");
            Assert.That(tile, Is.Not.Null);
            Assert.That(tile.GetComponent<SpriteRenderer>().color, Is.EqualTo(gridView.BlockedColor));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Placement_IsRefused_OnOccupiedOrBlockedCells()
        {
            GameObject root = CreateDemoRoot();
            yield return null;

            BuildController controller = root.GetComponent<BuildController>();

            Assert.That(controller.TryPlace(new Vector2Int(4, 4)), Is.False, "deposit");
            Assert.That(controller.TryPlace(new Vector2Int(2, 3)), Is.False, "ally");
            Assert.That(controller.TryPlace(new Vector2Int(16, 16)), Is.False, "enemy");
            Assert.That(controller.TryPlace(new Vector2Int(1, 1)), Is.False, "resource");
            Assert.That(controller.TryPlace(new Vector2Int(10, 10)), Is.False, "layout wall");
            Assert.That(controller.TryPlace(new Vector2Int(-1, 0)), Is.False, "out of bounds");
            Assert.That(controller.TryPlace(new Vector2Int(30, 0)), Is.False, "out of bounds");
            Assert.That(controller.StructureCount, Is.Zero);

            Assert.That(controller.TryPlace(new Vector2Int(2, 2)), Is.True);
            Assert.That(controller.TryPlace(new Vector2Int(2, 2)), Is.False, "already built");
            Assert.That(controller.StructureCount, Is.EqualTo(1));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator WallPlacedAheadOfMovingAlly_Repaths_AndAllyStillCollects()
        {
            GameObject root = CreateDemoRoot();
            yield return null;

            GameGrid grid = root.GetComponent<GridBootstrap>().Grid;
            BuildController controller = root.GetComponent<BuildController>();

            ResourceNode resource = new ResourceNode(new Vector2Int(3, 0));
            GameObject allyObject = new GameObject("AllyDetourTest");
            AllyAgent ally = allyObject.AddComponent<AllyAgent>();
            ally.Configure(
                grid,
                new Vector2Int(0, 0),
                new[] { resource },
                new Vector2Int(0, 2),
                4f,
                PathfindingAlgorithm.AStar,
                NavigationMode.UniformCost,
                null,
                6f);
            ally.enabled = false;

            for (int i = 0; i < 100 && !ally.IsMoving; i++)
            {
                ally.Advance(0.05f);
            }

            Assert.That(ally.IsMoving, Is.True);

            Vector2Int cell = new Vector2Int(2, 0);
            Assert.That(grid.IsPassable(cell), Is.True);
            Assert.That(controller.TryPlace(cell), Is.True);
            Assert.That(ally.RepathCount, Is.GreaterThanOrEqualTo(1));

            for (int i = 0; i < 600 && ally.CollectCount == 0; i++)
            {
                ally.Advance(0.05f);
                Assert.That(ally.Cell, Is.Not.EqualTo(cell));
            }

            Assert.That(ally.CollectCount, Is.EqualTo(1));

            Object.Destroy(allyObject);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RemovingStructure_UnblocksTile_AndRestoresView()
        {
            GameObject root = CreateDemoRoot();
            yield return null;

            GameGrid grid = root.GetComponent<GridBootstrap>().Grid;
            GridView gridView = root.GetComponent<GridView>();
            BuildController controller = root.GetComponent<BuildController>();

            Vector2Int cell = new Vector2Int(2, 2);
            int blockedBefore = grid.BlockedCount;

            Assert.That(controller.TryPlace(cell), Is.True);
            Assert.That(controller.TryRemove(cell), Is.True);
            Assert.That(grid.IsBlocked(cell), Is.False);
            Assert.That(grid.BlockedCount, Is.EqualTo(blockedBefore));
            Assert.That(controller.StructureCount, Is.Zero);
            Assert.That(controller.TryRemove(cell), Is.False);

            Transform tile = gridView.transform.Find("Tiles/Tile_2_2");
            Assert.That(tile, Is.Not.Null);
            Assert.That(tile.GetComponent<SpriteRenderer>().color, Is.EqualTo(gridView.PassableColor));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SealingTheDeposit_IsRefused()
        {
            GameObject root = CreateDemoRoot();
            yield return null;

            BuildController controller = root.GetComponent<BuildController>();

            Vector2Int[] neighbours =
            {
                new Vector2Int(3, 4),
                new Vector2Int(5, 4),
                new Vector2Int(4, 3),
                new Vector2Int(4, 5)
            };

            int placed = 0;
            bool refused = false;

            for (int i = 0; i < neighbours.Length; i++)
            {
                if (controller.TryPlace(neighbours[i]))
                {
                    placed++;
                }
                else
                {
                    refused = true;
                }
            }

            Assert.That(placed, Is.EqualTo(3), "Only the last wall should be refused.");
            Assert.That(refused, Is.True);
            Assert.That(controller.StructureCount, Is.EqualTo(3));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RebuildingTheDemo_ClearsPlayerStructures()
        {
            GameObject root = CreateDemoRoot();
            yield return null;

            GameGrid grid = root.GetComponent<GridBootstrap>().Grid;
            BuildController controller = root.GetComponent<BuildController>();
            FsmDemoBootstrap demo = root.GetComponent<FsmDemoBootstrap>();

            Vector2Int cell = new Vector2Int(2, 2);
            Assert.That(controller.TryPlace(cell), Is.True);

            demo.BuildDemo();

            Assert.That(controller.StructureCount, Is.Zero);
            Assert.That(grid.IsBlocked(cell), Is.False);
            Assert.That(demo.AllyCount, Is.EqualTo(2));

            Object.Destroy(root);
            yield return null;
        }

        private static GameObject CreateDemoRoot()
        {
            GameObject root = new GameObject("BuildDemoTest");
            root.AddComponent<GridBootstrap>();
            root.AddComponent<FsmDemoBootstrap>();
            root.AddComponent<BuildController>();
            return root;
        }
    }
}
