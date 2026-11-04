using System;
using System.Collections.Generic;
using DefendAndCollect.Grid;
using UnityEngine;

namespace DefendAndCollect.Pathfinding
{
    [DefaultExecutionOrder(100)]
    public sealed class PathfindingDebugController : MonoBehaviour
    {
        [SerializeField] private PathfindingAlgorithm algorithm = PathfindingAlgorithm.Bfs;
        [SerializeField] private NavigationMode navigationMode = NavigationMode.UniformCost;
        [SerializeField] private Color pathColor = new Color(1f, 0.82f, 0.2f, 1f);
        [SerializeField] private Color startColor = new Color(0.2f, 1f, 0.35f, 1f);
        [SerializeField] private Color endColor = new Color(0.2f, 0.6f, 1f, 1f);
        [SerializeField] private bool allowObstacleToggle = true;
        [SerializeField] private bool allowTerrainToggle = true;

        private static Sprite sharedMarkerSprite;

        private readonly Rect panelRect = new Rect(12f, 40f, 430f, 330f);
        private readonly List<GameObject> markers = new List<GameObject>();

        private GridView gridView;
        private GameGrid grid;
        private Vector2Int? startCell;
        private Vector2Int? endCell;
        private IReadOnlyList<Vector2Int> currentPath = PathfindingSupport.EmptyPath;
        private string statusMessage = "Elegí inicio (clic izquierdo) y fin (clic derecho).";

        public PathfindingAlgorithm ActiveAlgorithm => algorithm;

        public NavigationMode ActiveNavigationMode => navigationMode;

        public event Action<PathfindingAlgorithm, NavigationMode> NavigationChanged;

        private void Start()
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
            startCell = FindPassableCell(true);
            endCell = FindPassableCell(false);
            RecalculatePath();
        }

        private void Update()
        {
            if (grid == null || IsPointerOverPanel())
            {
                return;
            }

            if (Input.GetMouseButtonDown(0) && TryGetHoveredCell(out Vector2Int startHover))
            {
                TrySetStart(startHover);
            }

            if (Input.GetMouseButtonDown(1) && TryGetHoveredCell(out Vector2Int endHover))
            {
                TrySetEnd(endHover);
            }

            if (allowObstacleToggle && Input.GetKeyDown(KeyCode.O) && TryGetHoveredCell(out Vector2Int obstacleHover))
            {
                ToggleObstacle(obstacleHover);
            }

            if (allowTerrainToggle && Input.GetKeyDown(KeyCode.T) && TryGetHoveredCell(out Vector2Int terrainHover))
            {
                ToggleTerrain(terrainHover);
            }
        }

        private void OnGUI()
        {
            GUI.Box(panelRect, GUIContent.none);
            GUILayout.BeginArea(new Rect(panelRect.x + 8f, panelRect.y + 6f, panelRect.width - 16f, panelRect.height - 12f));

            GUILayout.Label("Pathfinding (spec 002) + navegación (spec 003)");

            GUILayout.BeginHorizontal();
            Color previousColor = GUI.backgroundColor;
            GUI.backgroundColor = algorithm == PathfindingAlgorithm.Bfs ? new Color(0.45f, 0.9f, 0.45f) : Color.white;
            if (GUILayout.Button("BFS"))
            {
                SelectAlgorithm(PathfindingAlgorithm.Bfs);
            }

            GUI.backgroundColor = algorithm == PathfindingAlgorithm.AStar ? new Color(0.45f, 0.9f, 0.45f) : Color.white;
            if (GUILayout.Button("A*"))
            {
                SelectAlgorithm(PathfindingAlgorithm.AStar);
            }

            GUI.backgroundColor = previousColor;
            GUILayout.EndHorizontal();

            GUILayout.Label($"Algoritmo activo: {AlgorithmName}");

            GUILayout.BeginHorizontal();
            previousColor = GUI.backgroundColor;
            GUI.backgroundColor = navigationMode == NavigationMode.UniformCost ? new Color(0.45f, 0.9f, 0.45f) : Color.white;
            if (GUILayout.Button("Costes iguales"))
            {
                SelectNavigationMode(NavigationMode.UniformCost);
            }

            GUI.backgroundColor = navigationMode == NavigationMode.TerrainCost ? new Color(0.45f, 0.9f, 0.45f) : Color.white;
            if (GUILayout.Button("Costes de terreno"))
            {
                SelectNavigationMode(NavigationMode.TerrainCost);
            }

            GUI.backgroundColor = previousColor;
            GUILayout.EndHorizontal();

            GUILayout.Label($"Navegación activa: {NavigationModeName}");
            GUILayout.Label("Verde: normal | Marrón: lento | Rojo: obstáculo");

            if (algorithm == PathfindingAlgorithm.Bfs)
            {
                GUILayout.Label("BFS ignora los costes: siempre minimiza la cantidad de pasos.");
            }

            GUILayout.Label(statusMessage);
            GUILayout.Label("Clic izquierdo: inicio | Clic derecho: fin");
            GUILayout.Label("O: alternar obstáculo | T: alternar terreno lento");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Recalcular"))
            {
                RecalculatePath();
            }

            if (GUILayout.Button("Quitar inicio/fin"))
            {
                ClearEndpoints();
            }

            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            DestroyMarkers();
        }

        private string AlgorithmName => algorithm == PathfindingAlgorithm.Bfs ? "BFS" : "A*";

        private string NavigationModeName => navigationMode == NavigationMode.UniformCost
            ? "Costes iguales"
            : "Costes de terreno";

        public void SelectAlgorithm(PathfindingAlgorithm selectedAlgorithm)
        {
            algorithm = selectedAlgorithm;
            RecalculatePath();
            NotifyNavigationChanged();
        }

        public void SelectNavigationMode(NavigationMode selectedMode)
        {
            navigationMode = selectedMode;
            RecalculatePath();
            NotifyNavigationChanged();
        }

        private void NotifyNavigationChanged()
        {
            NavigationChanged?.Invoke(algorithm, navigationMode);
        }

        private void ClearEndpoints()
        {
            startCell = null;
            endCell = null;
            currentPath = PathfindingSupport.EmptyPath;
            statusMessage = "Elegí inicio (clic izquierdo) y fin (clic derecho).";
            RebuildMarkers();
        }

        private void TrySetStart(Vector2Int cell)
        {
            if (!grid.IsPassable(cell))
            {
                statusMessage = "El inicio debe ser un tile transitable.";
                return;
            }

            startCell = cell;
            RecalculatePath();
        }

        private void TrySetEnd(Vector2Int cell)
        {
            if (!grid.IsPassable(cell))
            {
                statusMessage = "El fin debe ser un tile transitable.";
                return;
            }

            endCell = cell;
            RecalculatePath();
        }

        private void ToggleObstacle(Vector2Int cell)
        {
            if ((startCell.HasValue && startCell.Value == cell) || (endCell.HasValue && endCell.Value == cell))
            {
                statusMessage = "No se puede bloquear el inicio ni el fin.";
                return;
            }

            bool nextBlocked = !grid.IsBlocked(cell);
            grid.SetBlocked(cell, nextBlocked);

            if (gridView != null)
            {
                gridView.RefreshTile(cell);
            }

            string action = nextBlocked
                ? $"Obstáculo colocado en ({cell.x}, {cell.y})."
                : $"Obstáculo quitado en ({cell.x}, {cell.y}).";

            RecalculatePath();
            statusMessage = $"{action} {statusMessage}";
        }

        private void ToggleTerrain(Vector2Int cell)
        {
            if (!grid.IsPassable(cell))
            {
                statusMessage = "No se puede pintar terreno lento en un obstáculo.";
                return;
            }

            bool nextSlow = grid.GetTerrain(cell) != TerrainType.Slow;
            grid.SetTerrain(cell, nextSlow ? TerrainType.Slow : TerrainType.Normal);

            if (gridView != null)
            {
                gridView.RefreshTile(cell);
            }

            string action = nextSlow
                ? $"Terreno lento colocado en ({cell.x}, {cell.y})."
                : $"Terreno lento quitado en ({cell.x}, {cell.y}).";

            RecalculatePath();
            statusMessage = $"{action} {statusMessage}";
        }

        private void RecalculatePath()
        {
            if (grid == null)
            {
                currentPath = PathfindingSupport.EmptyPath;
                RebuildMarkers();
                return;
            }

            if (!startCell.HasValue || !endCell.HasValue)
            {
                currentPath = PathfindingSupport.EmptyPath;
                statusMessage = "Elegí inicio (clic izquierdo) y fin (clic derecho).";
                RebuildMarkers();
                return;
            }

            IPathfinder pathfinder = PathfinderFactory.Create(algorithm);
            currentPath = pathfinder.GetPath(grid, startCell.Value, endCell.Value, navigationMode);
            int pathCost = PathfindingSupport.PathCost(grid, currentPath, navigationMode);

            statusMessage = currentPath.Count == 0
                ? $"{AlgorithmName} + {NavigationModeName}: sin ruta."
                : $"{AlgorithmName} + {NavigationModeName}: {currentPath.Count} tiles | costo {pathCost}.";

            RebuildMarkers();
        }

        private void RebuildMarkers()
        {
            DestroyMarkers();

            if (grid == null)
            {
                return;
            }

            if (startCell.HasValue)
            {
                CreateMarker(startCell.Value, startColor, 0.85f, 2);
            }

            if (endCell.HasValue)
            {
                CreateMarker(endCell.Value, endColor, 0.85f, 2);
            }

            for (int i = 0; i < currentPath.Count; i++)
            {
                Vector2Int cell = currentPath[i];

                if ((startCell.HasValue && startCell.Value == cell) || (endCell.HasValue && endCell.Value == cell))
                {
                    continue;
                }

                CreateMarker(cell, pathColor, 0.55f, 1);
            }
        }

        private void CreateMarker(Vector2Int cell, Color color, float scale, int sortingOrder)
        {
            GameObject marker = new GameObject($"PathMarker_{cell.x}_{cell.y}");
            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = grid.CellToWorld(cell);
            marker.transform.localScale = Vector3.one * scale;

            SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = GetSharedMarkerSprite();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            markers.Add(marker);
        }

        private void DestroyMarkers()
        {
            for (int i = 0; i < markers.Count; i++)
            {
                if (markers[i] != null)
                {
                    Destroy(markers[i]);
                }
            }

            markers.Clear();
        }

        private Vector2Int? FindPassableCell(bool fromStart)
        {
            int cellCount = grid.Width * grid.Height;

            for (int offset = 0; offset < cellCount; offset++)
            {
                int index = fromStart ? offset : cellCount - 1 - offset;
                Vector2Int cell = new Vector2Int(index % grid.Width, index / grid.Width);

                if (grid.IsPassable(cell))
                {
                    return cell;
                }
            }

            return null;
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

        private static Sprite GetSharedMarkerSprite()
        {
            if (sharedMarkerSprite != null)
            {
                return sharedMarkerSprite;
            }

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            sharedMarkerSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            sharedMarkerSprite.name = "PathfindingMarkerSprite";

            return sharedMarkerSprite;
        }
    }
}
