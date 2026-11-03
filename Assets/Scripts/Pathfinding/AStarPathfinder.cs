using System;
using System.Collections.Generic;
using DefendAndCollect.Grid;
using UnityEngine;

namespace DefendAndCollect.Pathfinding
{
    public sealed class AStarPathfinder : IPathfinder
    {
        public IReadOnlyList<Vector2Int> GetPath(
            GameGrid grid,
            Vector2Int start,
            Vector2Int end,
            NavigationMode mode = NavigationMode.UniformCost)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (!PathfindingSupport.IsEndpointUsable(grid, start) || !PathfindingSupport.IsEndpointUsable(grid, end))
            {
                return PathfindingSupport.EmptyPath;
            }

            if (start == end)
            {
                return new[] { start };
            }

            int width = grid.Width;
            int height = grid.Height;
            int cellCount = width * height;
            int[] costSoFar = new int[cellCount];
            Vector2Int[] previous = new Vector2Int[cellCount];
            bool[] closed = new bool[cellCount];

            for (int i = 0; i < cellCount; i++)
            {
                costSoFar[i] = int.MaxValue;
            }

            int startIndex = start.y * width + start.x;
            costSoFar[startIndex] = 0;

            MinHeap open = new MinHeap();
            int startHeuristic = Manhattan(start, end);
            open.Push(new Node(start, startHeuristic, startHeuristic));

            while (open.Count > 0)
            {
                Node node = open.Pop();
                Vector2Int current = node.Cell;
                int currentIndex = current.y * width + current.x;

                if (closed[currentIndex])
                {
                    continue;
                }

                if (current == end)
                {
                    return PathfindingSupport.Reconstruct(previous, start, end, width);
                }

                closed[currentIndex] = true;
                int currentCost = costSoFar[currentIndex];

                foreach (Vector2Int direction in PathfindingSupport.CardinalDirections)
                {
                    Vector2Int neighbor = current + direction;

                    if (!grid.IsPassable(neighbor))
                    {
                        continue;
                    }

                    int neighborIndex = neighbor.y * width + neighbor.x;
                    if (closed[neighborIndex])
                    {
                        continue;
                    }

                    int tentativeCost = currentCost + PathfindingSupport.MovementCost(grid, neighbor, mode);
                    if (tentativeCost >= costSoFar[neighborIndex])
                    {
                        continue;
                    }

                    costSoFar[neighborIndex] = tentativeCost;
                    previous[neighborIndex] = current;

                    int heuristic = Manhattan(neighbor, end);
                    open.Push(new Node(neighbor, tentativeCost + heuristic, heuristic));
                }
            }

            return PathfindingSupport.EmptyPath;
        }

        private static int Manhattan(Vector2Int a, Vector2Int b)
        {
            return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
        }

        private readonly struct Node
        {
            public Node(Vector2Int cell, int priority, int heuristic)
            {
                Cell = cell;
                Priority = priority;
                Heuristic = heuristic;
            }

            public Vector2Int Cell { get; }

            public int Priority { get; }

            public int Heuristic { get; }
        }

        private sealed class MinHeap
        {
            private readonly List<Node> items = new List<Node>();

            public int Count => items.Count;

            public void Push(Node node)
            {
                items.Add(node);
                BubbleUp(items.Count - 1);
            }

            public Node Pop()
            {
                Node result = items[0];
                int lastIndex = items.Count - 1;
                items[0] = items[lastIndex];
                items.RemoveAt(lastIndex);

                if (items.Count > 0)
                {
                    BubbleDown(0);
                }

                return result;
            }

            private void BubbleUp(int index)
            {
                while (index > 0)
                {
                    int parentIndex = (index - 1) / 2;
                    if (!HasHigherPriority(items[index], items[parentIndex]))
                    {
                        break;
                    }

                    Swap(index, parentIndex);
                    index = parentIndex;
                }
            }

            private void BubbleDown(int index)
            {
                while (true)
                {
                    int leftIndex = index * 2 + 1;
                    int rightIndex = leftIndex + 1;
                    int bestIndex = index;

                    if (leftIndex < items.Count && HasHigherPriority(items[leftIndex], items[bestIndex]))
                    {
                        bestIndex = leftIndex;
                    }

                    if (rightIndex < items.Count && HasHigherPriority(items[rightIndex], items[bestIndex]))
                    {
                        bestIndex = rightIndex;
                    }

                    if (bestIndex == index)
                    {
                        return;
                    }

                    Swap(index, bestIndex);
                    index = bestIndex;
                }
            }

            private static bool HasHigherPriority(Node a, Node b)
            {
                if (a.Priority != b.Priority)
                {
                    return a.Priority < b.Priority;
                }

                if (a.Heuristic != b.Heuristic)
                {
                    return a.Heuristic < b.Heuristic;
                }

                if (a.Cell.y != b.Cell.y)
                {
                    return a.Cell.y < b.Cell.y;
                }

                return a.Cell.x < b.Cell.x;
            }

            private void Swap(int firstIndex, int secondIndex)
            {
                Node temporary = items[firstIndex];
                items[firstIndex] = items[secondIndex];
                items[secondIndex] = temporary;
            }
        }
    }
}
