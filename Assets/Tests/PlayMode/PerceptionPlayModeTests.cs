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
    public sealed class PerceptionPlayModeTests
    {
        [UnityTest]
        public IEnumerator Ally_IgnoresResourcesOutsideDetectionRadius()
        {
            GameGrid grid = new GameGrid(20, 5);
            ResourceNode node = new ResourceNode(new Vector2Int(10, 0));
            AllyAgent ally = CreateAlly(
                grid,
                new Vector2Int(0, 0),
                new[] { node },
                new Vector2Int(0, 2),
                8f,
                3f);

            for (int i = 0; i < 100; i++)
            {
                ally.Advance(0.05f);
            }

            Assert.That(ally.CurrentResource, Is.Null);
            Assert.That(ally.IsMoving, Is.False);
            Assert.That(ally.CollectCount, Is.Zero);

            Object.Destroy(ally.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ally_SelectsNearestResourceInsideRadius()
        {
            GameGrid grid = new GameGrid(20, 5);
            ResourceNode farther = new ResourceNode(new Vector2Int(5, 1));
            ResourceNode nearest = new ResourceNode(new Vector2Int(2, 0));
            AllyAgent ally = CreateAlly(
                grid,
                new Vector2Int(0, 0),
                new[] { farther, nearest },
                new Vector2Int(0, 2),
                1f,
                6f);

            for (int i = 0; i < 40 && ally.CurrentResource == null; i++)
            {
                ally.Advance(0.05f);
            }

            Assert.That(ally.CurrentResource, Is.SameAs(nearest));

            Object.Destroy(ally.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ally_Retargets_WhenCurrentResourceIsTaken()
        {
            GameGrid grid = new GameGrid(20, 5);
            ResourceNode first = new ResourceNode(new Vector2Int(3, 0));
            ResourceNode second = new ResourceNode(new Vector2Int(5, 0));
            AllyAgent ally = CreateAlly(
                grid,
                new Vector2Int(0, 0),
                new[] { first, second },
                new Vector2Int(0, 2),
                1f,
                6f);

            for (int i = 0; i < 40 && ally.CurrentResource != first; i++)
            {
                ally.Advance(0.05f);
            }

            Assert.That(ally.CurrentResource, Is.SameAs(first));

            first.MarkTaken();

            for (int i = 0; i < 20; i++)
            {
                ally.Advance(0.05f);
            }

            Assert.That(ally.CurrentResource, Is.SameAs(second));
            Assert.That(ally.IsMoving, Is.True);

            Object.Destroy(ally.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ally_MarksResourceTaken_AfterCollectingIt()
        {
            GameGrid grid = new GameGrid(10, 5);
            ResourceNode node = new ResourceNode(new Vector2Int(1, 0));
            AllyAgent ally = CreateAlly(
                grid,
                new Vector2Int(0, 0),
                new[] { node },
                new Vector2Int(0, 2),
                8f,
                6f);

            for (int i = 0; i < 200 && ally.CollectCount == 0; i++)
            {
                ally.Advance(0.05f);
            }

            Assert.That(ally.CollectCount, Is.EqualTo(1));
            Assert.That(node.IsAvailable, Is.False);
            Assert.That(ally.CurrentState, Is.EqualTo(AllyState.TransportToDeposit));

            Object.Destroy(ally.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ally_AfterDepositing_SeeksAnotherAvailableResource()
        {
            GameGrid grid = new GameGrid(12, 6);
            ResourceNode first = new ResourceNode(new Vector2Int(1, 0));
            ResourceNode second = new ResourceNode(new Vector2Int(1, 4));
            AllyAgent ally = CreateAlly(
                grid,
                new Vector2Int(0, 0),
                new[] { second, first },
                new Vector2Int(0, 2),
                8f,
                6f);

            for (int i = 0; i < 200 && ally.CollectCount == 0; i++)
            {
                ally.Advance(0.05f);
            }

            Assert.That(ally.CollectCount, Is.EqualTo(1));
            Assert.That(first.IsAvailable, Is.False);

            for (int i = 0; i < 200 && ally.CurrentResource != second; i++)
            {
                ally.Advance(0.05f);
            }

            Assert.That(ally.CurrentResource, Is.SameAs(second));
            Assert.That(ally.CurrentState, Is.EqualTo(AllyState.SeekResource));

            Object.Destroy(ally.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Enemy_IgnoresAlliesOutsideDetectionRadius()
        {
            GameGrid grid = new GameGrid(20, 5);
            AllyAgent ally = CreateAlly(
                grid,
                new Vector2Int(10, 0),
                new ResourceNode[0],
                new Vector2Int(10, 0));

            EnemyAgent enemy = CreateEnemy(grid, new Vector2Int(0, 0), new[] { ally }, 3f);

            for (int i = 0; i < 100; i++)
            {
                enemy.Advance(0.05f);
            }

            Assert.That(enemy.Target, Is.Null);
            Assert.That(enemy.AttackCount, Is.Zero);
            Assert.That(enemy.CurrentState, Is.Not.EqualTo(EnemyState.Chase));
            Assert.That(enemy.CurrentState, Is.Not.EqualTo(EnemyState.Attack));

            Object.Destroy(enemy.gameObject);
            Object.Destroy(ally.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Enemy_AcquiresNearestAlly_AndRetargetsWhenItDisappears()
        {
            GameGrid grid = new GameGrid(20, 5);
            AllyAgent nearest = CreateAlly(
                grid,
                new Vector2Int(3, 0),
                new ResourceNode[0],
                new Vector2Int(3, 0));
            AllyAgent farther = CreateAlly(
                grid,
                new Vector2Int(4, 0),
                new ResourceNode[0],
                new Vector2Int(4, 0));

            EnemyAgent enemy = CreateEnemy(grid, new Vector2Int(0, 0), new[] { farther, nearest });

            for (int i = 0; i < 40 && enemy.Target == null; i++)
            {
                enemy.Advance(0.05f);
            }

            Assert.That(enemy.Target, Is.SameAs(nearest));

            nearest.gameObject.SetActive(false);

            for (int i = 0; i < 20; i++)
            {
                enemy.Advance(0.05f);
            }

            Assert.That(enemy.Target, Is.SameAs(farther));

            Object.Destroy(enemy.gameObject);
            Object.Destroy(nearest.gameObject);
            Object.Destroy(farther.gameObject);
            yield return null;
        }

        private static AllyAgent CreateAlly(
            GameGrid grid,
            Vector2Int startCell,
            ResourceNode[] resources,
            Vector2Int deposit,
            float speed = 8f,
            float detectionRadius = 6f)
        {
            GameObject allyObject = new GameObject("AllyTest");
            AllyAgent ally = allyObject.AddComponent<AllyAgent>();
            ally.Configure(
                grid,
                startCell,
                resources,
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
            float detectionRadius = 6f)
        {
            GameObject enemyObject = new GameObject("EnemyTest");
            EnemyAgent enemy = enemyObject.AddComponent<EnemyAgent>();
            enemy.Configure(
                grid,
                startCell,
                allies,
                3f,
                PathfindingAlgorithm.AStar,
                NavigationMode.UniformCost,
                null,
                detectionRadius);
            enemy.enabled = false;
            return enemy;
        }
    }
}
