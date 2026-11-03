using System.Collections.Generic;
using UnityEngine;

namespace DefendAndCollect.Grid
{
    public sealed class GridView : MonoBehaviour
    {
        [SerializeField] private Color passableColor = new Color(0.36f, 0.55f, 0.38f, 1f);
        [SerializeField] private Color passableAlternateColor = new Color(0.31f, 0.49f, 0.33f, 1f);
        [SerializeField] private Color blockedColor = new Color(0.55f, 0.16f, 0.16f, 1f);
        [SerializeField] private float tileScale = 0.94f;

        private static Sprite sharedTileSprite;

        private readonly List<SpriteRenderer> tileRenderers = new List<SpriteRenderer>();
        private Transform tilesRoot;

        public GameGrid Grid { get; private set; }

        public int TileCount => tileRenderers.Count;

        public Color BlockedColor => blockedColor;

        public Color PassableColor => passableColor;

        public void Build(GameGrid grid)
        {
            Clear();
            Grid = grid;

            GameObject rootObject = new GameObject("Tiles");
            rootObject.transform.SetParent(transform, false);
            tilesRoot = rootObject.transform;

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    GameObject tileObject = new GameObject($"Tile_{x}_{y}");
                    tileObject.transform.SetParent(tilesRoot, false);
                    tileObject.transform.localPosition = grid.CellToWorld(cell);
                    tileObject.transform.localScale = Vector3.one * tileScale;

                    SpriteRenderer tileRenderer = tileObject.AddComponent<SpriteRenderer>();
                    tileRenderer.sprite = GetSharedTileSprite();
                    tileRenderer.color = GetTileColor(grid, cell);
                    tileRenderers.Add(tileRenderer);
                }
            }
        }

        public void Clear()
        {
            if (tilesRoot != null)
            {
                Destroy(tilesRoot.gameObject);
                tilesRoot = null;
            }

            tileRenderers.Clear();
            Grid = null;
        }

        private Color GetTileColor(GameGrid grid, Vector2Int cell)
        {
            if (!grid.IsPassable(cell))
            {
                return blockedColor;
            }

            return (cell.x + cell.y) % 2 == 0 ? passableColor : passableAlternateColor;
        }

        private static Sprite GetSharedTileSprite()
        {
            if (sharedTileSprite != null)
            {
                return sharedTileSprite;
            }

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            sharedTileSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            sharedTileSprite.name = "GridTileSprite";

            return sharedTileSprite;
        }
    }
}
