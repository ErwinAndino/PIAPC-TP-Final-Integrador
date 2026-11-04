using System.Collections.Generic;
using DefendAndCollect.Agents;
using DefendAndCollect.Build;
using DefendAndCollect.Grid;
using DefendAndCollect.Pathfinding;
using DefendAndCollect.Waves;
using UnityEngine;

namespace DefendAndCollect.Scenarios
{
    [DefaultExecutionOrder(-50)]
    public sealed class ScenarioController : MonoBehaviour
    {
        private const float PanelWidth = 540f;
        private const float PanelHeight = 118f;
        private const float PanelMargin = 12f;

        private static readonly Color ActiveButtonColor = new Color(0.45f, 0.9f, 0.45f);

        [SerializeField] private int initialScenario;

        private GridBootstrap bootstrap;
        private FsmDemoBootstrap demo;
        private BuildController buildController;
        private PathfindingDebugController pathfindingController;
        private WaveController waveController;
        private int currentIndex = -1;
        private string statusMessage = "F1/F2/F3 o los botones cambian de plantilla.";

        public int CurrentIndex => currentIndex;

        public ScenarioDefinition Current => currentIndex >= 0 && currentIndex < ScenarioLibrary.All.Count
            ? ScenarioLibrary.All[currentIndex]
            : null;

        public IReadOnlyList<ScenarioDefinition> Scenarios => ScenarioLibrary.All;

        public string StatusMessage => statusMessage;

        private void Start()
        {
            ResolveDependencies();
            ApplyScenario(initialScenario);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                ApplyScenario(currentIndex >= 0 ? currentIndex : initialScenario);
                return;
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                ApplyScenario(0);
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                ApplyScenario(1);
            }
            else if (Input.GetKeyDown(KeyCode.F3))
            {
                ApplyScenario(2);
            }
        }

        public bool ApplyScenario(int index)
        {
            IReadOnlyList<ScenarioDefinition> scenarios = ScenarioLibrary.All;

            if (index < 0 || index >= scenarios.Count)
            {
                statusMessage = $"No existe el escenario {index + 1}.";
                return false;
            }

            if (bootstrap == null)
            {
                ResolveDependencies();
            }

            if (bootstrap == null || bootstrap.Grid == null)
            {
                statusMessage = "El grid todavía no está listo.";
                return false;
            }

            ScenarioDefinition definition = scenarios[index];

            if (buildController != null)
            {
                buildController.ClearStructures();
            }

            GameGrid grid = definition.CreateGrid();
            bootstrap.Build(grid);
            GridView gridView = bootstrap.GetComponent<GridView>();

            if (buildController != null)
            {
                buildController.Rebind(grid, gridView);
            }

            if (demo != null)
            {
                demo.ConfigureScenario(definition, waveController == null);
            }

            if (pathfindingController != null)
            {
                pathfindingController.Rebind(grid);
            }

            if (waveController != null)
            {
                waveController.StartMatch(definition.EnemySpawns);
            }

            currentIndex = index;
            statusMessage = $"{definition.DisplayName}: {definition.Resources.Count} recursos, " +
                $"{definition.AllySpawns.Count} aliados, {definition.EnemySpawns.Count} enemigos.";

            return true;
        }

        private void ResolveDependencies()
        {
            bootstrap = GetComponent<GridBootstrap>();
            if (bootstrap == null)
            {
                bootstrap = FindAnyObjectByType<GridBootstrap>();
            }

            demo = GetComponent<FsmDemoBootstrap>();
            if (demo == null)
            {
                demo = FindAnyObjectByType<FsmDemoBootstrap>();
            }

            buildController = GetComponent<BuildController>();
            if (buildController == null)
            {
                buildController = FindAnyObjectByType<BuildController>();
            }

            pathfindingController = GetComponent<PathfindingDebugController>();
            if (pathfindingController == null)
            {
                pathfindingController = FindAnyObjectByType<PathfindingDebugController>();
            }

            waveController = GetComponent<WaveController>();
            if (waveController == null)
            {
                waveController = FindAnyObjectByType<WaveController>();
            }
        }

        private void OnGUI()
        {
            IReadOnlyList<ScenarioDefinition> scenarios = ScenarioLibrary.All;
            Rect panelRect = new Rect(
                PanelMargin,
                Screen.height - PanelHeight - PanelMargin,
                PanelWidth,
                PanelHeight);

            GUI.Box(panelRect, GUIContent.none);
            GUILayout.BeginArea(new Rect(panelRect.x + 8f, panelRect.y + 6f, panelRect.width - 16f, panelRect.height - 12f));

            GUILayout.Label("Escenarios (spec 009)");

            GUILayout.BeginHorizontal();

            for (int i = 0; i < scenarios.Count; i++)
            {
                Color previousColor = GUI.backgroundColor;
                GUI.backgroundColor = i == currentIndex ? ActiveButtonColor : Color.white;

                if (GUILayout.Button($"F{i + 1}: {scenarios[i].DisplayName}"))
                {
                    ApplyScenario(i);
                }

                GUI.backgroundColor = previousColor;
            }

            GUILayout.EndHorizontal();

            if (Current != null)
            {
                GUILayout.Label(Current.Description);
            }

            GUILayout.Label(statusMessage);

            GUILayout.EndArea();
        }
    }
}
