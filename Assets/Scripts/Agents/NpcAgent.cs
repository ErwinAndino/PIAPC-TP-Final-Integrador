using System;
using System.Collections.Generic;
using DefendAndCollect.Core;
using DefendAndCollect.Grid;
using DefendAndCollect.Pathfinding;
using UnityEngine;

namespace DefendAndCollect.Agents
{
    public abstract class NpcAgent : MonoBehaviour
    {
        private readonly List<Vector2Int> path = new List<Vector2Int>();

        private int pathIndex;
        private bool hasDestination;
        private Vector2Int destinationCell;
        private float moveSpeed = 3f;
        private PathfindingAlgorithm pathfindingAlgorithm = PathfindingAlgorithm.AStar;
        private NavigationMode navigationMode = NavigationMode.UniformCost;

        public GameGrid Grid { get; private set; }

        public Vector2Int Cell { get; private set; }

        public Vector2Int DestinationCell => destinationCell;

        public bool IsMoving => pathIndex < path.Count;

        public bool HasArrived => hasDestination && !IsMoving && Cell == destinationCell;

        public bool IsConfigured => Grid != null;

        public abstract string CurrentStateName { get; }

        public event Action<NpcAgent> StateChanged;

        public void Initialize(
            GameGrid grid,
            Vector2Int startCell,
            float speed,
            PathfindingAlgorithm algorithm = PathfindingAlgorithm.AStar,
            NavigationMode mode = NavigationMode.UniformCost,
            Color? color = null)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (!grid.IsPassable(startCell))
            {
                throw new ArgumentOutOfRangeException(nameof(startCell), startCell, "Start cell must be passable.");
            }

            Grid = grid;
            Cell = startCell;
            moveSpeed = Mathf.Max(0.1f, speed);
            pathfindingAlgorithm = algorithm;
            navigationMode = mode;
            transform.position = grid.CellToWorld(startCell);

            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = gameObject.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = PrimitiveSprite.Get();
            renderer.color = color ?? Color.white;
            renderer.sortingOrder = 10;

            OnConfigured();
        }

        public bool MoveTo(Vector2Int destination)
        {
            if (Grid == null)
            {
                return false;
            }

            hasDestination = true;
            destinationCell = destination;
            path.Clear();
            pathIndex = 0;

            if (!Grid.IsPassable(destination))
            {
                hasDestination = false;
                return false;
            }

            IPathfinder pathfinder = PathfinderFactory.Create(pathfindingAlgorithm);
            IReadOnlyList<Vector2Int> nextPath = pathfinder.GetPath(Grid, Cell, destination, navigationMode);

            if (nextPath.Count == 0)
            {
                hasDestination = false;
                return false;
            }

            for (int i = 0; i < nextPath.Count; i++)
            {
                path.Add(nextPath[i]);
            }

            pathIndex = path.Count > 1 ? 1 : path.Count;
            return true;
        }

        public void Stop()
        {
            path.Clear();
            pathIndex = 0;
            hasDestination = false;
        }

        public void Advance(float deltaTime)
        {
            if (Grid == null || deltaTime <= 0f)
            {
                return;
            }

            TickBehavior(deltaTime);
            MoveAlongPath(deltaTime);
        }

        protected abstract void OnConfigured();

        protected abstract void TickBehavior(float deltaTime);

        protected void NotifyStateChanged()
        {
            StateChanged?.Invoke(this);
        }

        private void Update()
        {
            Advance(Time.deltaTime);
        }

        private void MoveAlongPath(float deltaTime)
        {
            if (!IsMoving)
            {
                return;
            }

            float remainingDistance = moveSpeed * deltaTime;

            while (remainingDistance > 0f && pathIndex < path.Count)
            {
                Vector3 target = Grid.CellToWorld(path[pathIndex]);
                float distance = Vector3.Distance(transform.position, target);

                if (distance <= remainingDistance)
                {
                    transform.position = target;
                    Cell = path[pathIndex];
                    pathIndex++;
                    remainingDistance -= distance;
                }
                else
                {
                    transform.position = Vector3.MoveTowards(transform.position, target, remainingDistance);
                    remainingDistance = 0f;
                }
            }
        }
    }
}
