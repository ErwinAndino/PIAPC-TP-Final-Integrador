using System.Collections;
using DefendAndCollect.Grid;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DefendAndCollect.Tests.PlayMode
{
    public sealed class GridViewPlayModeTests
    {
        [UnityTest]
        public IEnumerator Build_CreatesOneTilePerCell_AndColorsBlockedTiles()
        {
            GameObject root = new GameObject("GridViewTest");
            GridView view = root.AddComponent<GridView>();
            GameGrid grid = new GameGrid(4, 4);
            Vector2Int blockedCell = new Vector2Int(1, 2);
            grid.SetBlocked(blockedCell, true);

            view.Build(grid);

            yield return null;

            Assert.That(view.TileCount, Is.EqualTo(16));
            Assert.That(root.GetComponentsInChildren<SpriteRenderer>().Length, Is.EqualTo(16));

            SpriteRenderer blockedRenderer = root.transform
                .Find($"Tiles/Tile_{blockedCell.x}_{blockedCell.y}")
                .GetComponent<SpriteRenderer>();
            SpriteRenderer passableRenderer = root.transform
                .Find("Tiles/Tile_0_0")
                .GetComponent<SpriteRenderer>();

            Assert.That(blockedRenderer.color, Is.EqualTo(view.BlockedColor));
            Assert.That(passableRenderer.color, Is.Not.EqualTo(view.BlockedColor));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Bootstrap_BuildsThirtyByThirtyGrid()
        {
            GameObject root = new GameObject("BootstrapTest");
            GridBootstrap bootstrap = root.AddComponent<GridBootstrap>();

            yield return null;

            Assert.That(bootstrap.Grid, Is.Not.Null);
            Assert.That(bootstrap.Grid.Width, Is.EqualTo(30));
            Assert.That(bootstrap.Grid.Height, Is.EqualTo(30));
            Assert.That(root.GetComponent<GridView>().TileCount, Is.EqualTo(900));

            Object.Destroy(root);
            yield return null;
        }
    }
}
