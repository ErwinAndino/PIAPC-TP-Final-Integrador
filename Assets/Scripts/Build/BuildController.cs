using System.Collections.Generic;
using DefendAndCollect.Agents;
using DefendAndCollect.Core;
using DefendAndCollect.Grid;
using DefendAndCollect.World;
using UnityEngine;

namespace DefendAndCollect.Build
{
    public enum StructureType
    {
        Wall = 0,
        Tower = 1
    }

    [DefaultExecutionOrder(50)]
    public sealed class BuildController : MonoBehaviour
    {
        private const float WallScale = 0.82f;
        private const float TowerScale = 0.62f;
        private const int StructureSortingOrder = 6;
        private const int PreviewSortingOrder = 3;

        public const float TowerRange = 4f;
        public const float TowerDamage = 25f;
        public const float TowerFireInterval = 1f;

        private static readonly Color WallColor = new Color(0.72f, 0.72f, 0.78f, 1f);
        private static readonly Color TowerColor = new Color(0.4f, 0.72f, 1f, 1f);
        private static readonly Color ValidPreviewColor = new Color(0.35f, 1f, 0.45f, 0.5f);
        private static readonly Color InvalidPreviewColor = new Color(1f, 0.35f, 0.35f, 0.5f);

        private static readonly Vector2Int[] Neighbours =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        private readonly Dictionary<Vector2Int, StructureType> structures = new Dictionary<Vector2Int, StructureType>();
        private readonly Dictionary<Vector2Int, GameObject> structureMarkers = new Dictionary<Vector2Int, GameObject>();
        private readonly Rect panelRect = new Rect(12f, 380f, 430f, 200f);

        private GameGrid grid;
        private GridView gridView;
        private FsmDemoBootstrap demo;
        private Transform structuresRoot;
        private GameObject previewMarker;
        private SpriteRenderer previewRenderer;
        private StructureType selectedType = StructureType.Wall;
        private string statusMessage = "B: modo construcción | 1: muro | 2: torre";
        private bool buildMode;
        private float towerFireElapsed;
        private int towerAttackCount;

        public bool IsBuildMode => buildMode;

        public StructureType SelectedType => selectedType;

        public int StructureCount => structures.Count;

        public string StatusMessage => statusMessage;

        public int TowerAttackCount => towerAttackCount;

        public IReadOnlyDictionary<Vector2Int, StructureType> Structures => structures;

        private void Start()
        {
            ResolveDependencies();
            CreatePreviewMarker();
        }

        private void Update()
        {
            if (grid == null)
            {
                return;
            }

            AdvanceCombat(Time.deltaTime);

            if (Input.GetKeyDown(KeyCode.B))
            {
                SetBuildMode(!buildMode);
            }

            if (!buildMode)
            {
                HidePreview();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetBuildMode(false);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SelectStructure(StructureType.Wall);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SelectStructure(StructureType.Tower);
            }

            Vector2Int cell = default;
            bool hasCell = !IsPointerOverPanel() && TryGetHoveredCell(out cell);
            RefreshPreview(hasCell, cell);

            if (!hasCell)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                TryPlace(cell);
            }
            else if (Input.GetMouseButtonDown(1))
            {
                TryRemove(cell);
            }
        }

        public void SetBuildMode(bool active)
        {
            buildMode = active;
            statusMessage = active
                ? "Clic izquierdo: colocar | Clic derecho: quitar | Esc: salir."
                : "Modo construcción desactivado.";

            if (!active)
            {
                HidePreview();
            }
        }

        public void SelectStructure(StructureType type)
        {
            selectedType = type;
            statusMessage = $"Estructura seleccionada: {TypeName(type)}.";
        }

        public bool CanPlace(Vector2Int cell)
        {
            return CanPlace(cell, out _);
        }

        public bool HasStructure(Vector2Int cell)
        {
            return structures.ContainsKey(cell);
        }

        public bool TryGetStructure(Vector2Int cell, out StructureType type)
        {
            return structures.TryGetValue(cell, out type);
        }

        public bool TryPlace(Vector2Int cell)
        {
            if (!CanPlace(cell, out string reason))
            {
                statusMessage = reason;
                return false;
            }

            bool[] reachableBefore = ComputeAllyReachability();
            grid.SetBlocked(cell, true);
            bool[] reachableAfter = ComputeAllyReachability();

            if (BreaksAllyRoute(reachableBefore, reachableAfter))
            {
                grid.SetBlocked(cell, false);
                statusMessage = "No se puede: dejaría a un aliado sin ruta al depósito.";
                return false;
            }

            structures[cell] = selectedType;
            CreateStructureMarker(cell, selectedType);
            RefreshTile(cell);

            statusMessage = $"{TypeName(selectedType)} en ({cell.x}, {cell.y}). Estructuras: {structures.Count}.";
            return true;
        }

        public bool TryRemove(Vector2Int cell)
        {
            if (!structures.TryGetValue(cell, out StructureType type))
            {
                statusMessage = "No hay estructura en ese tile.";
                return false;
            }

            structures.Remove(cell);
            grid.SetBlocked(cell, false);
            RefreshTile(cell);

            if (structureMarkers.TryGetValue(cell, out GameObject marker))
            {
                structureMarkers.Remove(cell);

                if (marker != null)
                {
                    Destroy(marker);
                }
            }

            statusMessage = $"{TypeName(type)} quitada en ({cell.x}, {cell.y}). Estructuras: {structures.Count}.";
            return true;
        }

        public void ClearStructures()
        {
            List<Vector2Int> cells = new List<Vector2Int>(structures.Keys);

            for (int i = 0; i < cells.Count; i++)
            {
                Vector2Int cell = cells[i];

                if (grid != null && grid.IsBlocked(cell))
                {
                    grid.SetBlocked(cell, false);
                }

                RefreshTile(cell);
            }

            for (int i = 0; i < cells.Count; i++)
            {
                if (structureMarkers.TryGetValue(cells[i], out GameObject marker) && marker != null)
                {
                    Destroy(marker);
                }
            }

            structures.Clear();
            structureMarkers.Clear();
        }

        public void Rebind(GameGrid newGrid, GridView newGridView)
        {
            grid = newGrid;
            gridView = newGridView;
        }

        public void AdvanceCombat(float deltaTime)
        {
            if (grid == null || demo == null || deltaTime <= 0f)
            {
                return;
            }

            towerFireElapsed += deltaTime;

            if (towerFireElapsed < TowerFireInterval)
            {
                return;
            }

            towerFireElapsed -= TowerFireInterval;

            if (towerFireElapsed > TowerFireInterval)
            {
                towerFireElapsed = 0f;
            }

            foreach (KeyValuePair<Vector2Int, StructureType> structure in structures)
            {
                if (structure.Value != StructureType.Tower)
                {
                    continue;
                }

                EnemyAgent target = FindNearestEnemyInRange(structure.Key);

                if (target == null)
                {
                    continue;
                }

                target.TakeDamage(TowerDamage);
                towerAttackCount++;
            }
        }

        private EnemyAgent FindNearestEnemyInRange(Vector2Int towerCell)
        {
            EnemyAgent nearest = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < demo.Enemies.Count; i++)
            {
                EnemyAgent enemy = demo.Enemies[i];

                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                float distance = Perception.Distance(towerCell, enemy.Cell);

                if (distance > TowerRange || distance >= nearestDistance)
                {
                    continue;
                }

                nearest = enemy;
                nearestDistance = distance;
            }

            return nearest;
        }

        private void ResolveDependencies()
        {
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
            gridView = bootstrap.GetComponent<GridView>();
            demo = GetComponent<FsmDemoBootstrap>();

            if (demo == null)
            {
                demo = FindAnyObjectByType<FsmDemoBootstrap>();
            }

            GameObject rootObject = new GameObject("PlayerStructures");
            rootObject.transform.SetParent(transform, false);
            structuresRoot = rootObject.transform;
        }

        private void CreatePreviewMarker()
        {
            previewMarker = new GameObject("BuildPreview");
            previewMarker.transform.SetParent(transform, false);
            previewMarker.transform.localScale = Vector3.one * WallScale;

            previewRenderer = previewMarker.AddComponent<SpriteRenderer>();
            previewRenderer.sprite = PrimitiveSprite.Get();
            previewRenderer.color = ValidPreviewColor;
            previewRenderer.sortingOrder = PreviewSortingOrder;
            previewMarker.SetActive(false);
        }

        private void RefreshPreview(bool hasCell, Vector2Int cell)
        {
            if (previewMarker == null)
            {
                return;
            }

            if (!hasCell || grid == null)
            {
                HidePreview();
                return;
            }

            previewMarker.SetActive(true);
            previewMarker.transform.position = grid.CellToWorld(cell);
            previewRenderer.color = CanPlace(cell) ? ValidPreviewColor : InvalidPreviewColor;
        }

        private void HidePreview()
        {
            if (previewMarker != null && previewMarker.activeSelf)
            {
                previewMarker.SetActive(false);
            }
        }

        private bool CanPlace(Vector2Int cell, out string reason)
        {
            if (grid == null)
            {
                reason = "El grid todavía no está listo.";
                return false;
            }

            if (!grid.IsInBounds(cell))
            {
                reason = "Fuera del tablero.";
                return false;
            }

            if (structures.ContainsKey(cell))
            {
                reason = "Ya hay una estructura en ese tile.";
                return false;
            }

            if (grid.IsBlocked(cell))
            {
                reason = "Ese tile ya está bloqueado.";
                return false;
            }

            if (IsOccupiedByDemo(cell))
            {
                reason = "No se puede construir sobre aliados, enemigos, recursos ni el depósito.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private bool IsOccupiedByDemo(Vector2Int cell)
        {
            if (demo == null)
            {
                return false;
            }

            if (demo.Deposit == cell)
            {
                return true;
            }

            for (int i = 0; i < demo.Allies.Count; i++)
            {
                NpcAgent ally = demo.Allies[i];

                if (ally != null && ally.Cell == cell)
                {
                    return true;
                }
            }

            for (int i = 0; i < demo.Enemies.Count; i++)
            {
                NpcAgent enemy = demo.Enemies[i];

                if (enemy != null && enemy.Cell == cell)
                {
                    return true;
                }
            }

            for (int i = 0; i < demo.Resources.Count; i++)
            {
                ResourceNode node = demo.Resources[i];

                if (node != null && node.IsAvailable && node.Cell == cell)
                {
                    return true;
                }
            }

            return false;
        }

        private bool[] ComputeAllyReachability()
        {
            int allyCount = demo != null ? demo.Allies.Count : 0;
            bool[] result = new bool[allyCount];

            if (allyCount == 0)
            {
                return result;
            }

            bool[] visited = FloodFill(demo.Deposit);

            for (int i = 0; i < allyCount; i++)
            {
                AllyAgent ally = demo.Allies[i];
                result[i] = ally != null && ally.IsAlive && grid.IsInBounds(ally.Cell) && visited[Index(ally.Cell)];
            }

            return result;
        }

        private bool[] FloodFill(Vector2Int origin)
        {
            bool[] visited = new bool[grid.Width * grid.Height];

            if (!grid.IsPassable(origin))
            {
                return visited;
            }

            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(origin);
            visited[Index(origin)] = true;

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();

                for (int i = 0; i < Neighbours.Length; i++)
                {
                    Vector2Int next = current + Neighbours[i];

                    if (!grid.IsPassable(next) || visited[Index(next)])
                    {
                        continue;
                    }

                    visited[Index(next)] = true;
                    queue.Enqueue(next);
                }
            }

            return visited;
        }

        private static bool BreaksAllyRoute(bool[] before, bool[] after)
        {
            for (int i = 0; i < before.Length && i < after.Length; i++)
            {
                if (before[i] && !after[i])
                {
                    return true;
                }
            }

            return false;
        }

        private void CreateStructureMarker(Vector2Int cell, StructureType type)
        {
            GameObject marker = new GameObject($"Structure_{type}_{cell.x}_{cell.y}");
            marker.transform.SetParent(structuresRoot != null ? structuresRoot : transform, false);
            marker.transform.position = grid.CellToWorld(cell);
            marker.transform.localScale = Vector3.one * (type == StructureType.Wall ? WallScale : TowerScale);

            SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = PrimitiveSprite.Get();
            renderer.color = type == StructureType.Wall ? WallColor : TowerColor;
            renderer.sortingOrder = StructureSortingOrder;

            structureMarkers[cell] = marker;
        }

        private void RefreshTile(Vector2Int cell)
        {
            if (gridView != null)
            {
                gridView.RefreshTile(cell);
            }
        }

        private int Index(Vector2Int cell)
        {
            return cell.y * grid.Width + cell.x;
        }

        private static string TypeName(StructureType type)
        {
            return type == StructureType.Wall ? "Muro" : "Torre";
        }

        private bool TryGetHoveredCell(out Vector2Int cell)
        {
            cell = default;

            Camera mainCamera = Camera.main;
            if (grid == null || mainCamera == null)
            {
                return false;
            }

            Vector3 screenPosition = Input.mousePosition;
            screenPosition.z = Mathf.Abs(mainCamera.transform.position.z);
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
            Vector2Int candidate = grid.WorldToCell(worldPosition);

            if (!grid.IsInBounds(candidate))
            {
                return false;
            }

            cell = candidate;
            return true;
        }

        private bool IsPointerOverPanel()
        {
            Vector3 mousePosition = Input.mousePosition;
            Vector2 guiPosition = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
            return panelRect.Contains(guiPosition);
        }

        private void OnGUI()
        {
            if (grid == null)
            {
                return;
            }

            GUI.Box(panelRect, GUIContent.none);
            GUILayout.BeginArea(new Rect(panelRect.x + 8f, panelRect.y + 6f, panelRect.width - 16f, panelRect.height - 12f));

            GUILayout.Label("Construcciones (spec 008)");

            Color previousColor = GUI.backgroundColor;
            GUI.backgroundColor = buildMode ? new Color(0.45f, 0.9f, 0.45f) : Color.white;

            if (GUILayout.Button(buildMode ? "Modo construcción: ON (B)" : "Modo construcción: OFF (B)"))
            {
                SetBuildMode(!buildMode);
            }

            GUI.backgroundColor = previousColor;

            GUILayout.BeginHorizontal();

            GUI.backgroundColor = selectedType == StructureType.Wall ? new Color(0.45f, 0.9f, 0.45f) : Color.white;
            if (GUILayout.Button("Muro (1)"))
            {
                SelectStructure(StructureType.Wall);
            }

            GUI.backgroundColor = selectedType == StructureType.Tower ? new Color(0.45f, 0.9f, 0.45f) : Color.white;
            if (GUILayout.Button("Torre (2)"))
            {
                SelectStructure(StructureType.Tower);
            }

            GUI.backgroundColor = previousColor;
            GUILayout.EndHorizontal();

            GUILayout.Label($"Estructuras: {structures.Count} | Muros y torres bloquean el paso");
            GUILayout.Label("Clic izquierdo: colocar | Clic derecho: quitar | Esc: salir");
            GUILayout.Label("No se puede construir sobre aliados, enemigos, recursos ni el depósito");
            GUILayout.Label(statusMessage);

            if (GUILayout.Button("Quitar todas"))
            {
                ClearStructures();
            }

            GUILayout.EndArea();
        }
    }
}
