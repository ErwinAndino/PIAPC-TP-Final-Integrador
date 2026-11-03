using System;
using System.Linq;
using DefendAndCollect.Grid;
using NUnit.Framework;
using UnityEngine;

namespace DefendAndCollect.Tests.EditMode
{
    public sealed class GameGridTests
    {
        [Test]
        public void NewGrid_HasExpectedDimensions_AndAllCellsPassable()
        {
            GameGrid grid = new GameGrid(30, 30);

            Assert.That(grid.Width, Is.EqualTo(30));
            Assert.That(grid.Height, Is.EqualTo(30));
            Assert.That(grid.BlockedCount, Is.EqualTo(0));

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    Assert.That(grid.IsPassable(new Vector2Int(x, y)), Is.True);
                }
            }
        }

        [Test]
        public void SetBlocked_ChangesPassability()
        {
            GameGrid grid = new GameGrid(5, 5);
            Vector2Int cell = new Vector2Int(2, 3);

            grid.SetBlocked(cell, true);

            Assert.That(grid.IsPassable(cell), Is.False);
            Assert.That(grid.IsBlocked(cell), Is.True);
            Assert.That(grid.BlockedCount, Is.EqualTo(1));

            grid.SetBlocked(cell, false);

            Assert.That(grid.IsPassable(cell), Is.True);
            Assert.That(grid.IsBlocked(cell), Is.False);
            Assert.That(grid.BlockedCount, Is.EqualTo(0));
        }

        [Test]
        public void SetBlocked_OutOfBounds_Throws()
        {
            GameGrid grid = new GameGrid(5, 5);

            Assert.Throws<ArgumentOutOfRangeException>(() => grid.SetBlocked(new Vector2Int(-1, 0), true));
            Assert.Throws<ArgumentOutOfRangeException>(() => grid.SetBlocked(new Vector2Int(0, -1), true));
            Assert.Throws<ArgumentOutOfRangeException>(() => grid.SetBlocked(new Vector2Int(5, 0), true));
            Assert.Throws<ArgumentOutOfRangeException>(() => grid.SetBlocked(new Vector2Int(0, 5), true));
        }

        [Test]
        public void IsPassable_OutOfBounds_ReturnsFalse()
        {
            GameGrid grid = new GameGrid(5, 5);

            Assert.That(grid.IsPassable(new Vector2Int(-1, 0)), Is.False);
            Assert.That(grid.IsPassable(new Vector2Int(0, -1)), Is.False);
            Assert.That(grid.IsPassable(new Vector2Int(5, 0)), Is.False);
            Assert.That(grid.IsPassable(new Vector2Int(0, 5)), Is.False);
            Assert.That(grid.IsBlocked(new Vector2Int(-1, 0)), Is.False);
        }

        [Test]
        public void BlockedCells_ReturnsBlockedCoordinates()
        {
            GameGrid grid = new GameGrid(4, 3);
            Vector2Int first = new Vector2Int(1, 0);
            Vector2Int second = new Vector2Int(3, 2);

            grid.SetBlocked(first, true);
            grid.SetBlocked(second, true);

            Assert.That(grid.BlockedCells.ToArray(), Is.EquivalentTo(new[] { first, second }));
        }

        [Test]
        public void FromRows_ParsesAsciiLayout_TopRowFirst()
        {
            string[] rows =
            {
                "#..",
                ".#.",
                "..#"
            };

            GameGrid grid = GameGrid.FromRows(rows);

            Assert.That(grid.Width, Is.EqualTo(3));
            Assert.That(grid.Height, Is.EqualTo(3));
            Assert.That(grid.IsBlocked(new Vector2Int(0, 2)), Is.True);
            Assert.That(grid.IsBlocked(new Vector2Int(1, 1)), Is.True);
            Assert.That(grid.IsBlocked(new Vector2Int(2, 0)), Is.True);
            Assert.That(grid.BlockedCount, Is.EqualTo(3));
        }

        [Test]
        public void FromRows_RejectsInvalidLayouts()
        {
            Assert.Throws<ArgumentException>(() => GameGrid.FromRows(null));
            Assert.Throws<ArgumentException>(() => GameGrid.FromRows(new string[0]));
            Assert.Throws<ArgumentException>(() => GameGrid.FromRows(new[] { "..", "." }));
            Assert.Throws<ArgumentException>(() => GameGrid.FromRows(new[] { ".x" }));
        }

        [Test]
        public void CellToWorld_AndWorldToCell_RoundTrip()
        {
            GameGrid grid = new GameGrid(30, 30);
            Vector2Int cell = new Vector2Int(17, 23);

            Vector3 world = grid.CellToWorld(cell);

            Assert.That(world.x, Is.EqualTo(17.5f));
            Assert.That(world.y, Is.EqualTo(23.5f));
            Assert.That(grid.WorldToCell(world), Is.EqualTo(cell));
        }

        [Test]
        public void WorldToCell_MapsTilesAndHandlesEdges()
        {
            GameGrid grid = new GameGrid(30, 30);

            Assert.That(grid.WorldToCell(new Vector3(0f, 0f, 0f)), Is.EqualTo(new Vector2Int(0, 0)));
            Assert.That(grid.WorldToCell(new Vector3(0.99f, 0.99f, 0f)), Is.EqualTo(new Vector2Int(0, 0)));
            Assert.That(grid.WorldToCell(new Vector3(1f, 1f, 0f)), Is.EqualTo(new Vector2Int(1, 1)));
            Assert.That(grid.WorldToCell(new Vector3(29.99f, 29.99f, 0f)), Is.EqualTo(new Vector2Int(29, 29)));
            Assert.That(grid.WorldToCell(new Vector3(-0.01f, -0.01f, 0f)), Is.EqualTo(new Vector2Int(-1, -1)));
        }
    }
}
