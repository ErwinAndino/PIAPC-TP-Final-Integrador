using System.Collections;
using DefendAndCollect.Agents;
using DefendAndCollect.Build;
using DefendAndCollect.Grid;
using DefendAndCollect.Pathfinding;
using DefendAndCollect.Scenarios;
using DefendAndCollect.Waves;
using DefendAndCollect.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DefendAndCollect.Tests.PlayMode
{
    public sealed class WavePlayModeTests
    {
        [UnityTest]
        public IEnumerator Match_StartsFirstWave_AtScenarioSpawns()
        {
            GameObject root = CreateRoot();
            yield return null;

            WaveController wave = root.GetComponent<WaveController>();
            FsmDemoBootstrap demo = root.GetComponent<FsmDemoBootstrap>();
            ScenarioDefinition scenario = ScenarioLibrary.Default;

            Assert.That(wave.State, Is.EqualTo(MatchState.InWave));
            Assert.That(wave.CurrentWave, Is.EqualTo(1));
            Assert.That(wave.WaveCount, Is.EqualTo(3));
            Assert.That(wave.EnemiesRemaining, Is.EqualTo(3));
            Assert.That(demo.EnemyCount, Is.EqualTo(3));
            Assert.That(wave.AlliesAlive, Is.EqualTo(2));

            for (int i = 0; i < demo.Enemies.Count; i++)
            {
                Assert.That(
                    Perception.Distance(demo.Enemies[i].Cell, scenario.EnemySpawns[i]),
                    Is.LessThanOrEqualTo(1.5f),
                    $"El enemigo {i + 1} debe aparecer en su spawn del escenario.");
            }

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClearingAWave_StartsTheNextWave_AfterThePause()
        {
            GameObject root = CreateRoot();
            yield return null;

            WaveController wave = root.GetComponent<WaveController>();
            FsmDemoBootstrap demo = root.GetComponent<FsmDemoBootstrap>();
            wave.enabled = false;

            KillAliveEnemies(demo);
            wave.Tick(0.1f);

            Assert.That(wave.State, Is.EqualTo(MatchState.BetweenWaves));
            Assert.That(wave.CurrentWave, Is.EqualTo(1));
            Assert.That(wave.EnemiesRemaining, Is.Zero);

            wave.Tick(wave.Plan.PauseBetweenWaves + 0.1f);

            Assert.That(wave.State, Is.EqualTo(MatchState.InWave));
            Assert.That(wave.CurrentWave, Is.EqualTo(2));
            Assert.That(wave.EnemiesRemaining, Is.EqualTo(4));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClearingAllWaves_WinsTheMatch()
        {
            GameObject root = CreateRoot();
            yield return null;

            WaveController wave = root.GetComponent<WaveController>();
            FsmDemoBootstrap demo = root.GetComponent<FsmDemoBootstrap>();
            wave.enabled = false;

            for (int step = 0; step < 20 && wave.State != MatchState.Victory; step++)
            {
                KillAliveEnemies(demo);
                wave.Tick(0.1f);
                wave.Tick(wave.Plan.PauseBetweenWaves + 0.1f);
            }

            Assert.That(wave.State, Is.EqualTo(MatchState.Victory));
            Assert.That(wave.CurrentWave, Is.EqualTo(wave.WaveCount));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator KillingAllAllies_LosesTheMatch_AndStopsSpawning()
        {
            GameObject root = CreateRoot();
            yield return null;

            WaveController wave = root.GetComponent<WaveController>();
            FsmDemoBootstrap demo = root.GetComponent<FsmDemoBootstrap>();
            wave.enabled = false;

            for (int i = 0; i < demo.Allies.Count; i++)
            {
                demo.Allies[i].TakeDamage(NpcAgent.MaxHealthValue);
            }

            wave.Tick(0.1f);

            Assert.That(wave.State, Is.EqualTo(MatchState.Defeat));
            Assert.That(wave.AlliesAlive, Is.Zero);

            int enemiesAfterDefeat = demo.EnemyCount;
            wave.Tick(60f);

            Assert.That(wave.State, Is.EqualTo(MatchState.Defeat));
            Assert.That(demo.EnemyCount, Is.EqualTo(enemiesAfterDefeat), "No deben spawnear más oleadas tras la derrota.");

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Enemy_AdvancesTowardTheObjective_WhenNoAllyIsVisible()
        {
            GameGrid grid = new GameGrid(14, 14);
            Vector2Int target = new Vector2Int(0, 2);

            GameObject allyObject = new GameObject("AllyTargetTest");
            AllyAgent ally = allyObject.AddComponent<AllyAgent>();
            ally.Configure(
                grid,
                new Vector2Int(0, 0),
                new ResourceNode[0],
                target,
                1f,
                PathfindingAlgorithm.AStar,
                NavigationMode.UniformCost,
                null,
                1f);
            ally.enabled = false;

            GameObject enemyObject = new GameObject("EnemyAdvanceTest");
            EnemyAgent enemy = enemyObject.AddComponent<EnemyAgent>();
            enemy.Configure(
                grid,
                new Vector2Int(12, 12),
                new[] { ally },
                4f,
                PathfindingAlgorithm.AStar,
                NavigationMode.UniformCost,
                null,
                1f,
                target);
            enemy.enabled = false;

            float startDistance = Perception.Distance(enemy.Cell, target);

            for (int i = 0; i < 60; i++)
            {
                enemy.Advance(0.05f);
            }

            Assert.That(enemy.Cell, Is.Not.EqualTo(new Vector2Int(12, 12)), "El enemigo debe avanzar hacia el objetivo.");
            Assert.That(Perception.Distance(enemy.Cell, target), Is.LessThan(startDistance));

            Object.Destroy(enemyObject);
            Object.Destroy(allyObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Tower_DamagesAndKillsEnemyInRange()
        {
            GameObject root = CreateRoot();
            yield return null;

            WaveController wave = root.GetComponent<WaveController>();
            BuildController build = root.GetComponent<BuildController>();
            FsmDemoBootstrap demo = root.GetComponent<FsmDemoBootstrap>();
            wave.enabled = false;

            EnemyAgent enemy = demo.Enemies[0];
            enemy.enabled = false;

            Assert.That(enemy.Health, Is.EqualTo(NpcAgent.MaxHealthValue));
            Assert.That(build.TowerAttackCount, Is.Zero, "Sin torres no debe haber disparos.");

            Vector2Int towerCell = enemy.Cell + Vector2Int.left;
            build.SelectStructure(StructureType.Tower);

            Assert.That(build.TryPlace(towerCell), Is.True);

            float healthBefore = enemy.Health;
            build.AdvanceCombat(0.5f);
            Assert.That(enemy.Health, Is.EqualTo(healthBefore), "La torre no dispara antes del intervalo.");

            build.AdvanceCombat(0.6f);
            Assert.That(enemy.Health, Is.EqualTo(healthBefore - BuildController.TowerDamage));
            Assert.That(build.TowerAttackCount, Is.EqualTo(1));

            for (int i = 0; i < 6 && enemy.IsAlive; i++)
            {
                build.AdvanceCombat(BuildController.TowerFireInterval);
            }

            Assert.That(enemy.IsAlive, Is.False, "La torre debe matar al enemigo en rango.");
            Assert.That(build.TowerAttackCount, Is.GreaterThanOrEqualTo(4));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Tower_DoesNotShootOutsideItsRange()
        {
            GameObject root = CreateRoot();
            yield return null;

            WaveController wave = root.GetComponent<WaveController>();
            BuildController build = root.GetComponent<BuildController>();
            FsmDemoBootstrap demo = root.GetComponent<FsmDemoBootstrap>();
            wave.enabled = false;

            EnemyAgent enemy = demo.Enemies[0];
            enemy.enabled = false;

            build.SelectStructure(StructureType.Tower);

            Assert.That(build.TryPlace(new Vector2Int(28, 28)), Is.True);
            build.AdvanceCombat(3f);

            Assert.That(enemy.Health, Is.EqualTo(NpcAgent.MaxHealthValue));
            Assert.That(build.TowerAttackCount, Is.Zero);

            Object.Destroy(root);
            yield return null;
        }

        private static void KillAliveEnemies(FsmDemoBootstrap demo)
        {
            for (int i = 0; i < demo.Enemies.Count; i++)
            {
                EnemyAgent enemy = demo.Enemies[i];

                if (enemy != null && enemy.IsAlive)
                {
                    enemy.TakeDamage(NpcAgent.MaxHealthValue);
                }
            }
        }

        private static GameObject CreateRoot()
        {
            GameObject root = new GameObject("WaveTest");
            root.AddComponent<GridBootstrap>();
            root.AddComponent<FsmDemoBootstrap>();
            root.AddComponent<BuildController>();
            root.AddComponent<PathfindingDebugController>();
            root.AddComponent<ScenarioController>();
            root.AddComponent<WaveController>();
            return root;
        }
    }
}
