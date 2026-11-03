using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefendAndCollect.Grid
{
    public sealed class GameGrid
    {
        private readonly bool[] blocked;

        public int Width { get; }

        public int Height { get; }

        public Vector3 WorldCenter => new Vector3(Width * 0.5f, Height * 0.5f, 0f);

        public GameGrid(int width, int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), width, "Grid width must be positive.");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), height, "Grid height must be positive.");
            }

            Width = width;
            Height = height;
            blocked = new bool[width * height];
        }

        public int BlockedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < blocked.Length; i++)
                {
                    if (blocked[i])
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public IEnumerable<Vector2Int> BlockedCells
        {
            get
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        Vector2Int cell = new Vector2Int(x, y);
                        if (blocked[Index(cell)])
                        {
                            yield return cell;
                        }
                    }
                }
            }
        }

        public bool IsInBounds(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height;
        }

        public bool IsPassable(Vector2Int cell)
        {
            return IsInBounds(cell) && !blocked[Index(cell)];
        }

        public bool IsBlocked(Vector2Int cell)
        {
            return IsInBounds(cell) && blocked[Index(cell)];
        }

        public void SetBlocked(Vector2Int cell, bool value)
        {
            if (!IsInBounds(cell))
            {
                throw new ArgumentOutOfRangeException(nameof(cell), cell, "Cell is outside the grid.");
            }

            blocked[Index(cell)] = value;
        }

        public Vector3 CellToWorld(Vector2Int cell)
        {
            return new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
        }

        public Vector2Int WorldToCell(Vector3 worldPosition)
        {
            return new Vector2Int(Mathf.FloorToInt(worldPosition.x), Mathf.FloorToInt(worldPosition.y));
        }

        public static GameGrid FromRows(string[] rows, char blockedChar = '#', char passableChar = '.')
        {
            if (rows == null || rows.Length == 0)
            {
                throw new ArgumentException("Rows must contain at least one row.", nameof(rows));
            }

            if (rows[0] == null || rows[0].Length == 0)
            {
                throw new ArgumentException("The first row must not be empty.", nameof(rows));
            }

            int width = rows[0].Length;
            GameGrid grid = new GameGrid(width, rows.Length);

            for (int row = 0; row < rows.Length; row++)
            {
                string line = rows[row];
                if (line == null || line.Length != width)
                {
                    throw new ArgumentException("All rows must have the same length.", nameof(rows));
                }

                for (int column = 0; column < width; column++)
                {
                    char value = line[column];
                    Vector2Int cell = new Vector2Int(column, grid.Height - 1 - row);

                    if (value == blockedChar)
                    {
                        grid.SetBlocked(cell, true);
                    }
                    else if (value != passableChar)
                    {
                        throw new ArgumentException($"Unexpected character '{value}' at row {row}, column {column}.", nameof(rows));
                    }
                }
            }

            return grid;
        }

        private int Index(Vector2Int cell)
        {
            return cell.y * Width + cell.x;
        }
    }
}
