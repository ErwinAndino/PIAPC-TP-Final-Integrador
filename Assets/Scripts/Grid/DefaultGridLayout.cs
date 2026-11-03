using UnityEngine;

namespace DefendAndCollect.Grid
{
    public static class DefaultGridLayout
    {
        public const int Width = 30;
        public const int Height = 30;

        public static GameGrid Create()
        {
            GameGrid grid = new GameGrid(Width, Height);

            FillVertical(grid, 10, 6, 23);
            FillHorizontal(grid, 18, 18, 27);
            FillRectangle(grid, 23, 7, 2, 3);
            FillHorizontal(grid, 14, 4, 12);

            return grid;
        }

        private static void FillVertical(GameGrid grid, int x, int startY, int endY)
        {
            for (int y = startY; y <= endY; y++)
            {
                grid.SetBlocked(new Vector2Int(x, y), true);
            }
        }

        private static void FillHorizontal(GameGrid grid, int y, int startX, int endX)
        {
            for (int x = startX; x <= endX; x++)
            {
                grid.SetBlocked(new Vector2Int(x, y), true);
            }
        }

        private static void FillRectangle(GameGrid grid, int startX, int startY, int width, int height)
        {
            for (int y = startY; y < startY + height; y++)
            {
                for (int x = startX; x < startX + width; x++)
                {
                    grid.SetBlocked(new Vector2Int(x, y), true);
                }
            }
        }
    }
}
