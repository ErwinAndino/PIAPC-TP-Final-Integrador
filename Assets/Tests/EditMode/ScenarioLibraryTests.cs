using System.Linq;
using DefendAndCollect.Agents;
using DefendAndCollect.Grid;
using DefendAndCollect.Pathfinding;
using DefendAndCollect.Scenarios;
using NUnit.Framework;
using UnityEngine;

namespace DefendAndCollect.Tests.EditMode
{
    public sealed class ScenarioLibraryTests
    {
        private static readonly PathfindingAlgorithm[] Algorithms =
        {
            PathfindingAlgorithm.Bfs,
            PathfindingAlgorithm.AStar
        };

        private static readonly NavigationMode[] NavigationModes =
        {
            NavigationMode.UniformCost,
            NavigationMode.TerrainCost
        };

        [Test]
        public void Library_ProvidesThreeDistinctScenarios()
        {
            Assert.That(ScenarioLibrary.All.Count, Is.EqualTo(3));
            Assert.That(ScenarioLibrary.Default, Is.SameAs(ScenarioLibrary.All[0]));
            Assert.That(ScenarioLibrary.All.Select(scenario => scenario.Id).Distinct().Count(), Is.EqualTo(3));
            Assert.That(
                ScenarioLibrary.All.Select(scenario => string.Join("\n", scenario.Rows)).Distinct().Count(),
                Is.EqualTo(3),
                "Cada plantilla debe tener un layout propio.");

            foreach (ScenarioDefinition scenario in ScenarioLibrary.All)
            {
                Assert.That(scenario.AllySpawns.Count, Is.EqualTo(2), $"{scenario.Id}: 2 aliados.");
                Assert.That(scenario.EnemySpawns.Count, Is.EqualTo(3), $"{scenario.Id}: 3 enemigos.");
                Assert.That(scenario.Resources.Count, Is.GreaterThanOrEqualTo(1), $"{scenario.Id}: recursos.");
            }
        }

        [Test]
        public void FirstScenario_MatchesDefaultGridLayout()
        {
            GameGrid defaultGrid = DefaultGridLayout.Create();
            GameGrid scenarioGrid = ScenarioLibrary.Default.CreateGrid();

            Assert.That(scenarioGrid.Width, Is.EqualTo(defaultGrid.Width));
            Assert.That(scenarioGrid.Height, Is.EqualTo(defaultGrid.Height));
            Assert.That(scenarioGrid.BlockedCount, Is.EqualTo(defaultGrid.BlockedCount));
            Assert.That(scenarioGrid.BlockedCells, Is.EquivalentTo(defaultGrid.BlockedCells));

            for (int y = 0; y < defaultGrid.Height; y++)
            {
                for (int x = 0; x < defaultGrid.Width; x++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    Assert.That(
                        scenarioGrid.GetTerrain(cell),
                        Is.EqualTo(defaultGrid.GetTerrain(cell)),
                        $"Terreno distinto en ({x}, {y}).");
                }
            }
        }

        [Test]
        public void EveryScenario_IsThirtyByThirty_WithEntitiesOnPassableCells()
        {
            foreach (ScenarioDefinition scenario in ScenarioLibrary.All)
            {
                GameGrid grid = scenario.CreateGrid();

                Assert.That(grid.Width, Is.EqualTo(30), $"{scenario.Id}: ancho.");
                Assert.That(grid.Height, Is.EqualTo(30), $"{scenario.Id}: alto.");
                Assert.That(grid.IsPassable(scenario.Deposit), Is.True, $"{scenario.Id}: depósito transitable.");

                for (int i = 0; i < scenario.Resources.Count; i++)
                {
                    Vector2Int cell = scenario.Resources[i];
                    Assert.That(grid.IsPassable(cell), Is.True, $"{scenario.Id}: recurso {i + 1} transitable.");
                }

                for (int i = 0; i < scenario.AllySpawns.Count; i++)
                {
                    Vector2Int cell = scenario.AllySpawns[i];
                    Assert.That(grid.IsPassable(cell), Is.True, $"{scenario.Id}: aliado {i + 1} transitable.");
                }

                for (int i = 0; i < scenario.EnemySpawns.Count; i++)
                {
                    Vector2Int cell = scenario.EnemySpawns[i];
                    Assert.That(grid.IsPassable(cell), Is.True, $"{scenario.Id}: enemigo {i + 1} transitable.");
                }
            }
        }

        [Test]
        public void EveryResource_StaysInsideDetectionRadiusOfDeposit()
        {
            foreach (ScenarioDefinition scenario in ScenarioLibrary.All)
            {
                for (int i = 0; i < scenario.Resources.Count; i++)
                {
                    float distance = Perception.Distance(scenario.Deposit, scenario.Resources[i]);

                    Assert.That(
                        distance,
                        Is.LessThanOrEqualTo(scenario.DetectionRadius),
                        $"{scenario.Id}: el recurso {i + 1} debe quedar dentro del radio de detección.");
                }
            }
        }

        [Test]
        public void EveryScenario_IsNavigable_WithBfsAndAStar_InBothNavigationModes()
        {
            foreach (ScenarioDefinition scenario in ScenarioLibrary.All)
            {
                GameGrid grid = scenario.CreateGrid();

                foreach (PathfindingAlgorithm algorithm in Algorithms)
                {
                    foreach (NavigationMode mode in NavigationModes)
                    {
                        IPathfinder pathfinder = PathfinderFactory.Create(algorithm);

                        for (int i = 0; i < scenario.Resources.Count; i++)
                        {
                            Assert.That(
                                pathfinder.GetPath(grid, scenario.Deposit, scenario.Resources[i], mode),
                                Is.Not.Empty,
                                $"{scenario.Id}: recurso {i + 1} inalcanzable con {algorithm} + {mode}.");
                        }

                        for (int i = 0; i < scenario.AllySpawns.Count; i++)
                        {
                            Assert.That(
                                pathfinder.GetPath(grid, scenario.Deposit, scenario.AllySpawns[i], mode),
                                Is.Not.Empty,
                                $"{scenario.Id}: aliado {i + 1} sin ruta al depósito con {algorithm} + {mode}.");
                        }
                    }
                }
            }
        }

        [Test]
        public void EveryEnemySpawn_CanReachEveryAllySpawn()
        {
            IPathfinder pathfinder = PathfinderFactory.Create(PathfindingAlgorithm.AStar);

            foreach (ScenarioDefinition scenario in ScenarioLibrary.All)
            {
                GameGrid grid = scenario.CreateGrid();

                for (int i = 0; i < scenario.EnemySpawns.Count; i++)
                {
                    for (int j = 0; j < scenario.AllySpawns.Count; j++)
                    {
                        Assert.That(
                            pathfinder.GetPath(grid, scenario.EnemySpawns[i], scenario.AllySpawns[j], NavigationMode.UniformCost),
                            Is.Not.Empty,
                            $"{scenario.Id}: el enemigo {i + 1} no puede llegar al aliado {j + 1}.");
                    }
                }
            }
        }

        [Test]
        public void Definition_RejectsBlockedOutOfBoundsOrDuplicatedCells()
        {
            string[] rows =
            {
                "...",
                ".#.",
                "..."
            };

            Assert.Throws<System.ArgumentException>(() => new ScenarioDefinition(
                "bloqueado",
                "Bloqueado",
                string.Empty,
                rows,
                new Vector2Int(1, 1),
                new[] { new Vector2Int(0, 0) },
                new[] { new Vector2Int(2, 0) },
                new[] { new Vector2Int(0, 2) }));

            Assert.Throws<System.ArgumentException>(() => new ScenarioDefinition(
                "duplicado",
                "Duplicado",
                string.Empty,
                rows,
                new Vector2Int(0, 0),
                new[] { new Vector2Int(0, 0) },
                new[] { new Vector2Int(2, 0) },
                new[] { new Vector2Int(0, 2) }));

            Assert.Throws<System.ArgumentOutOfRangeException>(() => new ScenarioDefinition(
                "fuera",
                "Fuera",
                string.Empty,
                rows,
                new Vector2Int(9, 9),
                new[] { new Vector2Int(0, 0) },
                new[] { new Vector2Int(2, 0) },
                new[] { new Vector2Int(0, 2) }));
        }
    }
}
