using System.Collections;
using System.Collections.Generic;
using DefendAndCollect.Agents;
using DefendAndCollect.Grid;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DefendAndCollect.Tests.PlayMode
{
    public sealed class NpcFsmPlayModeTests
    {
        [UnityTest]
        public IEnumerator Ally_FollowsExpectedStateSequence()
        {
            GameGrid grid = new GameGrid(10, 10);
            GameObject allyObject = new GameObject("AllyTest");
            AllyAgent ally = allyObject.AddComponent<AllyAgent>();
            ally.Configure(
                grid,
                new Vector2Int(0, 0),
                new[] { new Vector2Int(4, 0) },
                new Vector2Int(0, 2),
                16f);
            ally.enabled = false;

            List<AllyState> states = new List<AllyState>();
            AddIfNew(states, ally.CurrentState);

            for (int i = 0; i < 200 && states.Count < 4; i++)
            {
                ally.Advance(0.05f);
                AddIfNew(states, ally.CurrentState);
            }

            CollectionAssert.AreEqual(
                new[] { AllyState.Idle, AllyState.SeekResource, AllyState.Collect, AllyState.TransportToDeposit },
                states);
            Assert.That(ally.CollectCount, Is.GreaterThanOrEqualTo(1));

            Object.Destroy(allyObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Enemy_FollowsExpectedStateSequence()
        {
            GameGrid grid = new GameGrid(10, 10);

            GameObject allyObject = new GameObject("AllyTargetTest");
            AllyAgent ally = allyObject.AddComponent<AllyAgent>();
            ally.Configure(
                grid,
                new Vector2Int(3, 0),
                new[] { new Vector2Int(3, 0) },
                new Vector2Int(3, 0),
                1f);
            ally.enabled = false;

            GameObject enemyObject = new GameObject("EnemyTest");
            EnemyAgent enemy = enemyObject.AddComponent<EnemyAgent>();
            enemy.Configure(
                grid,
                new Vector2Int(0, 0),
                new[] { ally },
                16f);
            enemy.enabled = false;

            List<EnemyState> states = new List<EnemyState>();
            AddIfNew(states, enemy.CurrentState);

            for (int i = 0; i < 400 && enemy.AttackCount == 0; i++)
            {
                enemy.Advance(0.05f);
                AddIfNew(states, enemy.CurrentState);
            }

            Assert.That(states.Count, Is.GreaterThanOrEqualTo(4));
            Assert.That(states[0], Is.EqualTo(EnemyState.Idle));
            Assert.That(states[1], Is.EqualTo(EnemyState.SeekAlly));
            Assert.That(states[2], Is.EqualTo(EnemyState.Chase));
            Assert.That(states[3], Is.EqualTo(EnemyState.Attack));
            Assert.That(enemy.AttackCount, Is.GreaterThanOrEqualTo(1));

            Object.Destroy(enemyObject);
            Object.Destroy(allyObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DemoBootstrap_CreatesTwoAlliesAndThreeEnemies()
        {
            GameObject root = new GameObject("FsmDemoTest");
            root.AddComponent<GridBootstrap>();
            FsmDemoBootstrap demo = root.AddComponent<FsmDemoBootstrap>();

            yield return null;

            Assert.That(demo.AllyCount, Is.EqualTo(2));
            Assert.That(demo.EnemyCount, Is.EqualTo(3));

            for (int i = 0; i < demo.Allies.Count; i++)
            {
                Assert.That(demo.Allies[i].CurrentStateName, Is.Not.Empty);
            }

            for (int i = 0; i < demo.Enemies.Count; i++)
            {
                Assert.That(demo.Enemies[i].CurrentStateName, Is.Not.Empty);
            }

            Object.Destroy(root);
            yield return null;
        }

        private static void AddIfNew<T>(List<T> states, T state)
        {
            if (states.Count == 0 || !EqualityComparer<T>.Default.Equals(states[states.Count - 1], state))
            {
                states.Add(state);
            }
        }
    }
}
