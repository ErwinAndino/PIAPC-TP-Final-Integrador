using System.Collections.Generic;
using DefendAndCollect.Core;
using DefendAndCollect.Grid;
using DefendAndCollect.Pathfinding;
using DefendAndCollect.World;
using UnityEngine;

namespace DefendAndCollect.Agents
{
    [DefaultExecutionOrder(0)]
    public sealed class FsmDemoBootstrap : MonoBehaviour
    {
        private const float AllySpeed = 3.5f;
        private const float EnemySpeed = 3f;
        private const int MaxTransitions = 8;
        private const float DefaultDetectionRadius = 6f;
        private const float MaxDetectionRadius = 30f;

        private static readonly Color AllyColor = new Color(0.35f, 1f, 0.45f, 1f);
        private static readonly Color CarryingColor = new Color(1f, 1f, 0.55f, 1f);
        private static readonly Color EnemyColor = new Color(1f, 0.3f, 0.3f, 1f);
        private static readonly Color DepositColor = new Color(0.3f, 0.7f, 1f, 1f);
        private static readonly Color ResourceColor = new Color(1f, 0.85f, 0.25f, 1f);

        private static readonly Vector2Int DepositCell = new Vector2Int(4, 4);

        private static readonly Vector2Int[] ResourceCells =
        {
            new Vector2Int(1, 1),
            new Vector2Int(8, 2),
            new Vector2Int(9, 4),
            new Vector2Int(8, 7),
            new Vector2Int(4, 9),
            new Vector2Int(1, 8)
        };

        private readonly List<AllyAgent> allies = new List<AllyAgent>();
        private readonly List<EnemyAgent> enemies = new List<EnemyAgent>();
        private readonly List<ResourceNode> resources = new List<ResourceNode>();
        private readonly List<GameObject> resourceMarkers = new List<GameObject>();
        private readonly List<string> recentTransitions = new List<string>();
        private readonly Dictionary<NpcAgent, string> lastStates = new Dictionary<NpcAgent, string>();

        private GameGrid grid;
        private GameObject demoRoot;
        private float detectionRadius = DefaultDetectionRadius;
        private PathfindingDebugController navigationController;
        private PathfindingAlgorithm activeAlgorithm = PathfindingAlgorithm.AStar;
        private NavigationMode activeNavigationMode = NavigationMode.TerrainCost;

        public int AllyCount => allies.Count;

        public int EnemyCount => enemies.Count;

        public IReadOnlyList<AllyAgent> Allies => allies;

        public IReadOnlyList<EnemyAgent> Enemies => enemies;

        public IReadOnlyList<ResourceNode> Resources => resources;

        public float DetectionRadius => detectionRadius;

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

            RefreshResourceMarkers();
            RefreshCarryTint();
        }

        private void OnDestroy()
        {
            UnbindNavigationController();
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
            detectionRadius = DefaultDetectionRadius;
            BindNavigationController();
            demoRoot = new GameObject("FsmDemo");
            demoRoot.transform.SetParent(transform, false);

            CreateMarker("Deposito", DepositCell, DepositColor, 1.2f);

            for (int i = 0; i < ResourceCells.Length; i++)
            {
                ResourceNode node = new ResourceNode(ResourceCells[i]);
                resources.Add(node);

                GameObject marker = CreateMarker($"Recurso {i + 1}", node.Cell, ResourceColor, 0.7f);
                resourceMarkers.Add(marker);
            }

            Vector2Int[] allyStarts =
            {
                new Vector2Int(2, 3),
                new Vector2Int(5, 3)
            };

            for (int i = 0; i < allyStarts.Length; i++)
            {
                CreateAlly($"Aliado {i + 1}", allyStarts[i]);
            }

            Vector2Int[] enemyStarts =
            {
                new Vector2Int(16, 16),
                new Vector2Int(20, 20),
                new Vector2Int(24, 16)
            };

            for (int i = 0; i < enemyStarts.Length; i++)
            {
                CreateEnemy($"Enemigo {i + 1}", enemyStarts[i]);
            }
        }

        private void CreateAlly(string name, Vector2Int startCell)
        {
            GameObject agentObject = new GameObject(name);
            agentObject.transform.SetParent(demoRoot.transform, false);

            AllyAgent ally = agentObject.AddComponent<AllyAgent>();
            ally.StateChanged += HandleStateChanged;
            ally.Configure(
                grid,
                startCell,
                resources,
                DepositCell,
                AllySpeed,
                activeAlgorithm,
                activeNavigationMode,
                AllyColor,
                detectionRadius);

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
                activeAlgorithm,
                activeNavigationMode,
                EnemyColor,
                detectionRadius);

            enemies.Add(enemy);
        }

        private void BindNavigationController()
        {
            UnbindNavigationController();

            navigationController = FindAnyObjectByType<PathfindingDebugController>();
            if (navigationController == null)
            {
                activeAlgorithm = PathfindingAlgorithm.AStar;
                activeNavigationMode = NavigationMode.TerrainCost;
                return;
            }

            activeAlgorithm = navigationController.ActiveAlgorithm;
            activeNavigationMode = navigationController.ActiveNavigationMode;
            navigationController.NavigationChanged += HandleNavigationChanged;
        }

        private void UnbindNavigationController()
        {
            if (navigationController == null)
            {
                return;
            }

            navigationController.NavigationChanged -= HandleNavigationChanged;
            navigationController = null;
        }

        private void HandleNavigationChanged(PathfindingAlgorithm algorithm, NavigationMode mode)
        {
            activeAlgorithm = algorithm;
            activeNavigationMode = mode;

            for (int i = 0; i < allies.Count; i++)
            {
                if (allies[i] != null)
                {
                    allies[i].SetNavigation(algorithm, mode);
                }
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null)
                {
                    enemies[i].SetNavigation(algorithm, mode);
                }
            }
        }

        private GameObject CreateMarker(string name, Vector2Int cell, Color color, float scale)
        {
            GameObject marker = new GameObject(name);
            marker.transform.SetParent(demoRoot.transform, false);
            marker.transform.localPosition = grid.CellToWorld(cell);
            marker.transform.localScale = Vector3.one * scale;

            SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = PrimitiveSprite.Get();
            renderer.color = color;
            renderer.sortingOrder = 5;

            return marker;
        }

        private void ApplyDetectionRadius()
        {
            for (int i = 0; i < allies.Count; i++)
            {
                if (allies[i] != null)
                {
                    allies[i].DetectionRadius = detectionRadius;
                }
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null)
                {
                    enemies[i].DetectionRadius = detectionRadius;
                }
            }
        }

        private void TakeResourceFromAlly(int allyIndex)
        {
            if (allyIndex >= allies.Count || allies[allyIndex] == null)
            {
                return;
            }

            AllyAgent ally = allies[allyIndex];
            ResourceNode node = ally.CurrentResource;

            if (node == null || !node.IsAvailable)
            {
                node = Perception.FindNearest(
                    ally.Cell,
                    resources,
                    candidate => candidate.Cell,
                    MaxDetectionRadius * 2f,
                    candidate => candidate.IsAvailable);
            }

            node?.MarkTaken();
            RefreshResourceMarkers();
        }

        private void ToggleAlly(int allyIndex)
        {
            if (allyIndex >= allies.Count || allies[allyIndex] == null)
            {
                return;
            }

            AllyAgent ally = allies[allyIndex];

            if (ally.IsAlive)
            {
                ally.TakeDamage(NpcAgent.MaxHealthValue);
                return;
            }

            ally.RestoreHealth();
        }

        private void HealAllies()
        {
            for (int i = 0; i < allies.Count; i++)
            {
                if (allies[i] != null)
                {
                    allies[i].RestoreHealth();
                }
            }
        }

        private void RefreshResourceMarkers()
        {
            for (int i = 0; i < resources.Count && i < resourceMarkers.Count; i++)
            {
                if (resourceMarkers[i] != null)
                {
                    resourceMarkers[i].SetActive(resources[i].IsAvailable);
                }
            }
        }

        private void RefreshCarryTint()
        {
            for (int i = 0; i < allies.Count; i++)
            {
                AllyAgent ally = allies[i];

                if (ally == null)
                {
                    continue;
                }

                ally.SetVisualColor(ally.IsCarrying ? CarryingColor : AllyColor);
            }
        }

        private int CountAvailableResources()
        {
            int count = 0;

            for (int i = 0; i < resources.Count; i++)
            {
                if (resources[i].IsAvailable)
                {
                    count++;
                }
            }

            return count;
        }

        private int TotalDelivered()
        {
            int total = 0;

            for (int i = 0; i < allies.Count; i++)
            {
                if (allies[i] != null)
                {
                    total += allies[i].DeliveredCount;
                }
            }

            return total;
        }

        private string AlgorithmName => activeAlgorithm == PathfindingAlgorithm.Bfs ? "BFS" : "A*";

        private string NavigationModeName => activeNavigationMode == NavigationMode.UniformCost
            ? "Costes iguales"
            : "Costes de terreno";

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
            UnbindNavigationController();

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
            resources.Clear();
            resourceMarkers.Clear();
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

            float panelHeight = 230f + (((allies.Count * 2) + enemies.Count + MaxTransitions) * 20f);
            Rect panelRect = new Rect(455f, 40f, 470f, Mathf.Min(panelHeight, Screen.height - 52f));

            GUI.Box(panelRect, GUIContent.none);
            GUILayout.BeginArea(new Rect(panelRect.x + 8f, panelRect.y + 6f, panelRect.width - 16f, panelRect.height - 12f));

            GUILayout.Label("FSM (004) + percepción (005) + recolección (006) + ataque (007)");
            GUILayout.Label($"Algoritmo: {AlgorithmName} | Navegación: {NavigationModeName}");
            GUILayout.Label($"Radio: {detectionRadius:0.0} | Recursos: {CountAvailableResources()} | Entregas: {TotalDelivered()}");

            float nextRadius = GUILayout.HorizontalSlider(detectionRadius, 0f, MaxDetectionRadius);

            if (!Mathf.Approximately(nextRadius, detectionRadius))
            {
                detectionRadius = nextRadius;
                ApplyDetectionRadius();
            }

            GUILayout.Label("Radio bajo: sin detección | alto: detectan y persiguen");

            for (int i = 0; i < allies.Count; i++)
            {
                AllyAgent ally = allies[i];

                if (ally == null)
                {
                    continue;
                }

                string state = ally.IsAlive
                    ? (ally.IsTargetable ? ally.CurrentStateName : "Fuera de combate")
                    : "Muerto";
                string carry = ally.IsCarrying ? " (cargando)" : string.Empty;
                string target = "sin recurso";

                if (ally.CurrentResource != null && ally.CurrentResource.IsAvailable)
                {
                    float distance = Perception.Distance(ally.Cell, ally.CurrentResource.Cell);
                    target = $"recurso ({ally.CurrentResource.Cell.x},{ally.CurrentResource.Cell.y}) d={distance:0.0}";
                }

                GUILayout.Label($"{ally.name}: {state}{carry} | vida {ally.Health:0}/{NpcAgent.MaxHealthValue:0}");
                GUILayout.Label($"  Entregas {ally.DeliveredCount} | recálculos {ally.RepathCount} | objetivo: {target}");
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyAgent enemy = enemies[i];

                if (enemy == null)
                {
                    continue;
                }

                string target = "sin objetivo";

                if (enemy.Target != null && enemy.Target.IsTargetable)
                {
                    float distance = Perception.Distance(enemy.Cell, enemy.Target.Cell);
                    target = $"{enemy.Target.name} d={distance:0.0} vida {enemy.Target.Health:0}";
                }

                GUILayout.Label($"{enemy.name}: {enemy.CurrentStateName} | {target}");
            }

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Tomar recurso de Aliado 1"))
            {
                TakeResourceFromAlly(0);
            }

            if (GUILayout.Button("Tomar recurso de Aliado 2"))
            {
                TakeResourceFromAlly(1);
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Matar/revivir Aliado 1"))
            {
                ToggleAlly(0);
            }

            if (GUILayout.Button("Matar/revivir Aliado 2"))
            {
                ToggleAlly(1);
            }

            GUILayout.EndHorizontal();

            if (GUILayout.Button("Curar aliados"))
            {
                HealAllies();
            }

            GUILayout.Label("Transiciones recientes:");

            for (int i = 0; i < recentTransitions.Count; i++)
            {
                GUILayout.Label(recentTransitions[i]);
            }

            GUILayout.Label("Cambiá BFS/A* y costes en el panel izquierdo: afecta a los NPCs.");
            GUILayout.Label("R: reiniciar la demo");

            GUILayout.EndArea();
        }
    }
}
