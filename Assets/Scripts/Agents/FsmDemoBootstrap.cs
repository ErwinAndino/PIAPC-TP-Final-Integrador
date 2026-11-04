using System.Collections.Generic;
using DefendAndCollect.Core;
using DefendAndCollect.Grid;
using DefendAndCollect.Pathfinding;
using UnityEngine;

namespace DefendAndCollect.Agents
{
    [DefaultExecutionOrder(0)]
    public sealed class FsmDemoBootstrap : MonoBehaviour
    {
        private const float AllySpeed = 3.5f;
        private const float EnemySpeed = 3f;
        private const int MaxTransitions = 8;

        private static readonly Color AllyColor = new Color(0.35f, 1f, 0.45f, 1f);
        private static readonly Color EnemyColor = new Color(1f, 0.3f, 0.3f, 1f);
        private static readonly Color DepositColor = new Color(0.3f, 0.7f, 1f, 1f);
        private static readonly Color ResourceColor = new Color(1f, 0.85f, 0.25f, 1f);

        private readonly List<AllyAgent> allies = new List<AllyAgent>();
        private readonly List<EnemyAgent> enemies = new List<EnemyAgent>();
        private readonly List<string> recentTransitions = new List<string>();
        private readonly Dictionary<NpcAgent, string> lastStates = new Dictionary<NpcAgent, string>();

        private GameGrid grid;
        private GameObject demoRoot;

        public int AllyCount => allies.Count;

        public int EnemyCount => enemies.Count;

        public IReadOnlyList<AllyAgent> Allies => allies;

        public IReadOnlyList<EnemyAgent> Enemies => enemies;

        private void Start()
        {
            BuildDemo();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                BuildDemo();
            }
        }

        public void BuildDemo()
        {
            ClearDemo();

            GridBootstrap bootstrap = GetComponent<GridBootstrap>();
            if (bootstrap == null)
            {
                bootstrap = FindAnyObjectByType<GridBootstrap>();
            }

            if (bootstrap == null || bootstrap.Grid == null)
            {
                return;
            }

            grid = bootstrap.Grid;
            demoRoot = new GameObject("FsmDemo");
            demoRoot.transform.SetParent(transform, false);

            Vector2Int deposit = new Vector2Int(4, 4);
            Vector2Int[] resources =
            {
                new Vector2Int(16, 10),
                new Vector2Int(20, 3),
                new Vector2Int(7, 27),
                new Vector2Int(20, 12)
            };

            CreateMarker("Deposito", deposit, DepositColor, 1.2f);

            for (int i = 0; i < resources.Length; i++)
            {
                CreateMarker($"Recurso {i + 1}", resources[i], ResourceColor, 0.7f);
            }

            Vector2Int[] allyStarts =
            {
                new Vector2Int(2, 3),
                new Vector2Int(5, 3)
            };

            for (int i = 0; i < allyStarts.Length; i++)
            {
                CreateAlly($"Aliado {i + 1}", allyStarts[i], resources, deposit);
            }

            Vector2Int[] enemyStarts =
            {
                new Vector2Int(25, 25),
                new Vector2Int(26, 28),
                new Vector2Int(27, 24)
            };

            for (int i = 0; i < enemyStarts.Length; i++)
            {
                CreateEnemy($"Enemigo {i + 1}", enemyStarts[i]);
            }
        }

        private void CreateAlly(
            string name,
            Vector2Int startCell,
            IReadOnlyList<Vector2Int> resources,
            Vector2Int deposit)
        {
            GameObject agentObject = new GameObject(name);
            agentObject.transform.SetParent(demoRoot.transform, false);

            AllyAgent ally = agentObject.AddComponent<AllyAgent>();
            ally.StateChanged += HandleStateChanged;
            ally.Configure(
                grid,
                startCell,
                resources,
                deposit,
                AllySpeed,
                PathfindingAlgorithm.AStar,
                NavigationMode.TerrainCost,
                AllyColor);

            allies.Add(ally);
        }

        private void CreateEnemy(string name, Vector2Int startCell)
        {
            GameObject agentObject = new GameObject(name);
            agentObject.transform.SetParent(demoRoot.transform, false);

            EnemyAgent enemy = agentObject.AddComponent<EnemyAgent>();
            enemy.StateChanged += HandleStateChanged;
            enemy.Configure(
                grid,
                startCell,
                allies,
                EnemySpeed,
                PathfindingAlgorithm.AStar,
                NavigationMode.TerrainCost,
                EnemyColor);

            enemies.Add(enemy);
        }

        private void CreateMarker(string name, Vector2Int cell, Color color, float scale)
        {
            GameObject marker = new GameObject(name);
            marker.transform.SetParent(demoRoot.transform, false);
            marker.transform.localPosition = grid.CellToWorld(cell);
            marker.transform.localScale = Vector3.one * scale;

            SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = PrimitiveSprite.Get();
            renderer.color = color;
            renderer.sortingOrder = 5;
        }

        private void HandleStateChanged(NpcAgent agent)
        {
            string currentState = agent.CurrentStateName;
            string previousState = lastStates.TryGetValue(agent, out string value) ? value : "Inicio";
            lastStates[agent] = currentState;

            recentTransitions.Add($"{agent.name}: {previousState} -> {currentState}");

            while (recentTransitions.Count > MaxTransitions)
            {
                recentTransitions.RemoveAt(0);
            }
        }

        private void ClearDemo()
        {
            for (int i = 0; i < allies.Count; i++)
            {
                if (allies[i] != null)
                {
                    allies[i].StateChanged -= HandleStateChanged;
                }
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null)
                {
                    enemies[i].StateChanged -= HandleStateChanged;
                }
            }

            allies.Clear();
            enemies.Clear();
            recentTransitions.Clear();
            lastStates.Clear();
            grid = null;

            if (demoRoot != null)
            {
                demoRoot.SetActive(false);
                Destroy(demoRoot);
                demoRoot = null;
            }
        }

        private void OnGUI()
        {
            if (grid == null)
            {
                return;
            }

            float panelHeight = 64f + (allies.Count + enemies.Count + MaxTransitions) * 20f;
            Rect panelRect = new Rect(455f, 40f, 470f, Mathf.Min(panelHeight, Screen.height - 52f));

            GUI.Box(panelRect, GUIContent.none);
            GUILayout.BeginArea(new Rect(panelRect.x + 8f, panelRect.y + 6f, panelRect.width - 16f, panelRect.height - 12f));

            GUILayout.Label("FSM (spec 004) - estados de NPCs");

            for (int i = 0; i < allies.Count; i++)
            {
                if (allies[i] != null)
                {
                    GUILayout.Label($"{allies[i].name}: {allies[i].CurrentStateName}");
                }
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null)
                {
                    GUILayout.Label($"{enemies[i].name}: {enemies[i].CurrentStateName}");
                }
            }

            GUILayout.Label("Transiciones recientes:");

            for (int i = 0; i < recentTransitions.Count; i++)
            {
                GUILayout.Label(recentTransitions[i]);
            }

            GUILayout.Label("R: reiniciar la demo");

            GUILayout.EndArea();
        }
    }
}
