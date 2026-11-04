using System;
using UnityEngine;

namespace DefendAndCollect.Grid
{
    [DefaultExecutionOrder(-100)]
    public sealed class GridBootstrap : MonoBehaviour
    {
        [SerializeField] private bool frameCamera = true;
        [SerializeField] private float cameraPadding = 1.5f;

        public GameGrid Grid { get; private set; }

        private void Awake()
        {
            Build();
        }

        public void Build()
        {
            Build(DefaultGridLayout.Create());
        }

        public void Build(GameGrid grid)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            Grid = grid;

            GridView gridView = GetComponent<GridView>();
            if (gridView == null)
            {
                gridView = gameObject.AddComponent<GridView>();
            }

            gridView.Build(Grid);

            if (frameCamera)
            {
                FrameCamera();
            }
        }

        private void FrameCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            Vector3 center = Grid.WorldCenter;
            mainCamera.transform.position = new Vector3(center.x, center.y, mainCamera.transform.position.z);

            float halfHeight = Grid.Height * 0.5f + cameraPadding;
            float halfWidth = Grid.Width * 0.5f + cameraPadding;
            float aspect = mainCamera.aspect > 0f ? mainCamera.aspect : 1f;

            mainCamera.orthographic = true;
            mainCamera.orthographicSize = Mathf.Max(halfHeight, halfWidth / aspect);
        }

        private void OnGUI()
        {
            if (Grid == null)
            {
                return;
            }

            GUI.color = Color.white;
            GUI.Label(new Rect(12f, 12f, 360f, 24f), $"Grid {Grid.Width}x{Grid.Height} | Obstáculos: {Grid.BlockedCount}");
        }
    }
}
