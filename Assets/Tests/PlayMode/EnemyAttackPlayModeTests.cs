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
    public sealed class EnemyAttackPlayModeTests
    {
        [UnityTest]
        public IEnumerator Enemy_KillsAlly_WithBfs()
        {
            RunKillCycle(PathfindingAlgorithm.Bfs);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Enemy_KillsAlly_WithAStar()
        {
            RunKillCycle(PathfindingAlgorithm.AStar);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Enemy_RetargetsToRemainingAlly_AfterKillingFirst()
        {
            GameGrid grid = new GameGrid(12, 10);
            AllyAgent first = CreateAlly(grid, new Vector2Int(5, 0), new Vector2Int(5, 0));
            AllyAgent second = CreateAlly(grid, new Vector2Int(8, 0), new Vector2Int(8, 0));
            EnemyAgent enemy = CreateEnemy(
                grid,
                new Vector2Int(0, 0),
                new[] { second, first },
                PathfindingAlgorithm.AStar,
                12f);

            for (int i = 0; i < 40 && enemy.Target == null; i++)
            {
                enemy.Advance(0.05f);
            }

            Assert.That(enemy.Target, Is.SameAs(first));

            for (int i = 0; i < 400 && first.IsAlive; i++)
            {
                enemy.Advance(0.05f);
            }

            Assert.That(first.IsAlive, Is.False);
            Assert.That(first.IsTargetable, Is.False);
            Assert.That(second.IsAlive, Is.True);

            for (int i = 0; i < 80 && enemy.Target != second; i++)
            {
                enemy.Advance(0.05f);
            }

            Assert.That(enemy.Target, Is.SameAs(second));

            for (int i = 0; i < 100 && second.Health >= NpcAgent.MaxHealthValue; i++)
            {
                enemy.Advance(0.05f);
            }

            Assert.That(second.Health, Is.LessThan(NpcAgent.MaxHealthValue));

            Object.Destroy(enemy.gameObject);
            Object.Destroy(first.gameObject);
            Object.Destroy(second.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Enemy_DoesNotDamageAlly_WhileOutOfAttackRange()
        {
            GameGrid grid = new GameGrid(14, 6);
            AllyAgent ally = CreateAlly(grid, new Vector2Int(6, 0), new Vector2Int(6, 0));
            EnemyAgent enemy = CreateEnemy(
                grid,
                new Vector2Int(0, 0),
                new[] { ally },
                PathfindingAlgorithm.AStar,
                8f,
                0.2f);

            for (int i = 0; i < 60 && enemy.CurrentState != EnemyState.Chase; i++)
            {
                enemy.Advance(0.05f);
            }

            Assert.That(enemy.Target, Is.SameAs(ally));

            for (int i = 0; i < 60; i++)
            {
                enemy.Advance(0.05f);
            }

            Assert.That(enemy.AttackCount, Is.Zero);
            Assert.That(ally.Health, Is.EqualTo(NpcAgent.MaxHealthValue));

            Object.Destroy(enemy.gameObject);
            Object.Destroy(ally.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DeadAlly_StopsCollecting_AndResumesAfterRevive()
        {
            GameGrid grid = new GameGrid(10, 10);
            ResourceNode resource = new ResourceNode(new Vector2Int(3, 0));
            AllyAgent ally = CreateAlly(
                grid,
                new Vector2Int(0, 0),
                new Vector2Int(0, 2),
                8f,
                6f,
                new[] { resource });

            for (int i = 0; i < 100 && !ally.IsMoving; i++)
            {
                ally.Advance(0.05f);
            }

            Assert.That(ally.IsMoving, Is.True);

            ally.TakeDamage(NpcAgent.MaxHealthValue);

            Assert.That(ally.IsAlive, Is.False);
            Assert.That(ally.Health, Is.Zero);
            Assert.That(ally.IsMoving, Is.False);
            Assert.That(ally.IsTargetable, Is.False);
            Assert.That(ally.CurrentResource, Is.Null);

            Vector2Int deadCell = ally.Cell;

            for (int i = 0; i < 100; i++)
            {
                ally.Advance(0.05f);
            }

            Assert.That(ally.Cell, Is.EqualTo(deadCell));
            Assert.That(ally.CollectCount, Is.Zero);

            ally.RestoreHealth();

            Assert.That(ally.IsAlive, Is.True);
            Assert.That(ally.Health, Is.EqualTo(NpcAgent.MaxHealthValue));
            Assert.That(ally.IsTargetable, Is.True);

            for (int i = 0; i < 400 && ally.CollectCount == 0; i++)
            {
                ally.Advance(0.05f);
            }

            Assert.That(ally.CollectCount, Is.EqualTo(1));

            Object.Destroy(ally.gameObject);
            yield return null;
        }

        private static void RunKillCycle(PathfindingAlgorithm algorithm)
        {
            GameGrid grid = new GameGrid(10, 10);
            AllyAgent ally = CreateAlly(grid, new Vector2Int(5, 0), new Vector2Int(5, 0));
            EnemyAgent enemy = CreateEnemy(grid, new Vector2Int(0, 0), new[] { ally }, algorithm);

            for (int i = 0; i < 400 && ally.IsAlive; i++)
            {
                enemy.Advance(0.05f);
            }

            Assert.That(ally.IsAlive, Is.False, $"{algorithm} enemy should kill the unarmed ally.");
            Assert.That(ally.Health, Is.Zero, "Health must clamp at zero.");
            Assert.That(ally.IsTargetable, Is.False);
            Assert.That(enemy.AttackCount, Is.GreaterThanOrEqualTo(5));

            SpriteRenderer renderer = ally.GetComponent<SpriteRenderer>();
            Assert.That(renderer.enabled, Is.False, "A dead ally must be hidden.");

            Object.Destroy(enemy.gameObject);
            Object.Destroy(ally.gameObject);
        }

        private static AllyAgent CreateAlly(
            GameGrid grid,
            Vector2Int startCell,
            Vector2Int deposit,
            float speed = 1f,
            float detectionRadius = 6f,
            ResourceNode[] resources = null)
        {
            GameObject allyObject = new GameObject("AllyTest");
            AllyAgent ally = allyObject.AddComponent<AllyAgent>();
            ally.Configure(
                grid,
                startCell,
                resources ?? new ResourceNode[0],
                deposit,
                speed,
                PathfindingAlgorithm.AStar,
                NavigationMode.UniformCost,
                null,
                detectionRadius);
            ally.enabled = false;
            return ally;
        }

        private static EnemyAgent CreateEnemy(
            GameGrid grid,
            Vector2Int startCell,
            AllyAgent[] allies,
            PathfindingAlgorithm algorithm,
            float detectionRadius = 6f,
            float speed = 16f)
        {
            GameObject enemyObject = new GameObject("EnemyTest");
            EnemyAgent enemy = enemyObject.AddComponent<EnemyAgent>();
            enemy.Configure(
                grid,
                startCell,
                allies,
                speed,
                algorithm,
                NavigationMode.UniformCost,
                null,
                detectionRadius);
            enemy.enabled = false;
            return enemy;
        }
    }
}
