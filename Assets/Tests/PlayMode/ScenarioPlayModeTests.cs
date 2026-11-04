using System.Collections;
using DefendAndCollect.Agents;
using DefendAndCollect.Build;
using DefendAndCollect.Grid;
using DefendAndCollect.Pathfinding;
using DefendAndCollect.Scenarios;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DefendAndCollect.Tests.PlayMode
{
    public sealed class ScenarioPlayModeTests
    {
        private static readonly Vector2Int FreeCell = new Vector2Int(28, 28);

        [UnityTest]
        public IEnumerator Controller_AppliesScenarios_RebuildingGridDemoAndStructures()
        {
            GameObject root = CreateRoot();
            yield return null;

            GridBootstrap bootstrap = root.GetComponent<GridBootstrap>();
            FsmDemoBootstrap demo = root.GetComponent<FsmDemoBootstrap>();
            BuildController build = root.GetComponent<BuildController>();
            ScenarioController controller = root.GetComponent<ScenarioController>();

            Assert.That(controller.CurrentIndex, Is.Zero);
            Assert.That(controller.Current, Is.SameAs(ScenarioLibrary.Default));

            for (int index = 0; index < ScenarioLibrary.All.Count; index++)
            {
                ScenarioDefinition scenario = ScenarioLibrary.All[index];

                Assert.That(controller.ApplyScenario(index), Is.True);
                GameGrid grid = bootstrap.Grid;

                Assert.That(controller.CurrentIndex, Is.EqualTo(index));
                Assert.That(controller.Current, Is.SameAs(scenario));
                Assert.That(grid.BlockedCount, Is.EqualTo(scenario.CreateGrid().BlockedCount));
                Assert.That(demo.Deposit, Is.EqualTo(scenario.Deposit));
                Assert.That(demo.Allies[0].Grid, Is.SameAs(grid), "Los agentes deben quedar en el grid nuevo.");
                Assert.That(demo.Allies[0].DepositCell, Is.EqualTo(scenario.Deposit));
                Assert.That(bootstrap.GetComponent<GridView>().Grid, Is.SameAs(grid));
                Assert.That(demo.AllyCount, Is.EqualTo(scenario.AllySpawns.Count));
                Assert.That(demo.EnemyCount, Is.EqualTo(scenario.EnemySpawns.Count));
                Assert.That(demo.Allies[0].Cell, Is.EqualTo(scenario.AllySpawns[0]));
                Assert.That(demo.Enemies[demo.EnemyCount - 1].Cell, Is.EqualTo(scenario.EnemySpawns[scenario.EnemySpawns.Count - 1]));

                for (int i = 0; i < scenario.Resources.Count; i++)
                {
                    Assert.That(demo.Resources[i].Cell, Is.EqualTo(scenario.Resources[i]));
                }

                Assert.That(build.StructureCount, Is.Zero, "Cambiar de escenario descarta las construcciones del jugador.");
                Assert.That(build.TryPlace(FreeCell), Is.True);
                Assert.That(grid.IsBlocked(FreeCell), Is.True, "El controlador de construcción debe usar el grid nuevo.");
                Assert.That(build.StructureCount, Is.EqualTo(1));

                yield return null;
            }

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EveryScenario_AllowsAnAllyToCollectAndDeliver()
        {
            GameObject root = CreateRoot();
            yield return null;

            ScenarioController controller = root.GetComponent<ScenarioController>();
            FsmDemoBootstrap demo = root.GetComponent<FsmDemoBootstrap>();

            for (int index = 0; index < ScenarioLibrary.All.Count; index++)
            {
                Assert.That(controller.ApplyScenario(index), Is.True);
                yield return null;

                for (int enemyIndex = 0; enemyIndex < demo.Enemies.Count; enemyIndex++)
                {
                    demo.Enemies[enemyIndex].enabled = false;
                }

                AllyAgent ally = demo.Allies[0];
                ally.enabled = false;

                for (int step = 0; step < 3000 && ally.DeliveredCount == 0; step++)
                {
                    ally.Advance(0.05f);
                }

                Assert.That(
                    ally.DeliveredCount,
                    Is.GreaterThan(0),
                    $"Escenario {index + 1}: el aliado debe recolectar y entregar.");
                Assert.That(ally.IsAlive, Is.True, $"Escenario {index + 1}: el aliado no debería morir.");
            }

            Object.Destroy(root);
            yield return null;
        }

        private static GameObject CreateRoot()
        {
            GameObject root = new GameObject("ScenarioTest");
            root.AddComponent<GridBootstrap>();
            root.AddComponent<FsmDemoBootstrap>();
            root.AddComponent<BuildController>();
            root.AddComponent<PathfindingDebugController>();
            root.AddComponent<ScenarioController>();
            return root;
        }
    }
}
