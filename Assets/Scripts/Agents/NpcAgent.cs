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
        private float detectionRadius = 6f;
        private PathfindingAlgorithm pathfindingAlgorithm = PathfindingAlgorithm.AStar;
        private NavigationMode navigationMode = NavigationMode.UniformCost;
        private SpriteRenderer spriteRenderer;

        public GameGrid Grid { get; private set; }

        public Vector2Int Cell { get; private set; }

        public Vector2Int DestinationCell => destinationCell;

        public bool IsMoving => pathIndex < path.Count;

        public bool HasArrived => hasDestination && !IsMoving && Cell == destinationCell;

        public bool IsConfigured => Grid != null;

        public bool IsTargetable => gameObject.activeInHierarchy && IsConfigured && IsAlive;

        public PathfindingAlgorithm ActiveAlgorithm => pathfindingAlgorithm;

        public NavigationMode ActiveNavigationMode => navigationMode;

        public int RepathCount { get; private set; }

        public const float MaxHealthValue = 100f;

        public float Health { get; private set; } = MaxHealthValue;

        public bool IsAlive => Health > 0f;

        public float DetectionRadius
        {
            get => detectionRadius;
            set => detectionRadius = Mathf.Max(0f, value);
        }

        public abstract string CurrentStateName { get; }

        public event Action<NpcAgent> StateChanged;

        public void Initialize(
            GameGrid grid,
            Vector2Int startCell,
            float speed,
            PathfindingAlgorithm algorithm = PathfindingAlgorithm.AStar,
            NavigationMode mode = NavigationMode.UniformCost,
            Color? color = null,
            float detectionRadius = 6f)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (!grid.IsPassable(startCell))
            {
                throw new ArgumentOutOfRangeException(nameof(startCell), startCell, "Start cell must be passable.");
            }

            if (Grid != null)
            {
                Grid.Changed -= HandleGridChanged;
            }

            Grid = grid;
            Cell = startCell;
            moveSpeed = Mathf.Max(0.1f, speed);
            DetectionRadius = detectionRadius;
            pathfindingAlgorithm = algorithm;
            navigationMode = mode;
            Health = MaxHealthValue;
            Grid.Changed += HandleGridChanged;
            transform.position = grid.CellToWorld(startCell);

            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = PrimitiveSprite.Get();
            spriteRenderer.color = color ?? Color.white;
            spriteRenderer.sortingOrder = 10;

            OnConfigured();
        }

        public bool MoveTo(Vector2Int destination)
        {
            if (Grid == null || !IsAlive)
            {
                return false;
            }

            destinationCell = destination;
            hasDestination = true;
            return BuildPath();
        }

        public void SetNavigation(PathfindingAlgorithm algorithm, NavigationMode mode)
        {
            if (pathfindingAlgorithm == algorithm && navigationMode == mode)
            {
                return;
            }

            pathfindingAlgorithm = algorithm;
            navigationMode = mode;
            RepathToDestination();
        }

        public void SetVisualColor(Color color)
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = color;
            }
        }

        public void TakeDamage(float amount)
        {
            if (!IsConfigured || !IsAlive || amount <= 0f)
            {
                return;
            }

            Health = Mathf.Max(0f, Health - amount);

            if (Health <= 0f)
            {
                HandleDeath();
            }
        }

        public void RestoreHealth()
        {
            bool wasDead = !IsAlive;
            Health = MaxHealthValue;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }

            if (wasDead)
            {
                OnRevived();
            }
        }

        public void Stop()
        {
            path.Clear();
            pathIndex = 0;
            hasDestination = false;
        }

        public void Advance(float deltaTime)
        {
            if (Grid == null || deltaTime <= 0f || !IsAlive)
            {
                return;
            }

            TickBehavior(deltaTime);
            MoveAlongPath(deltaTime);
        }

        protected abstract void OnConfigured();

        protected abstract void TickBehavior(float deltaTime);

        protected virtual void OnDied()
        {
        }

        protected virtual void OnRevived()
        {
        }

        protected void NotifyStateChanged()
        {
            StateChanged?.Invoke(this);
        }

        private void HandleDeath()
        {
            Stop();
            OnDied();

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (Grid != null)
            {
                Grid.Changed -= HandleGridChanged;
            }
        }

        private void HandleGridChanged()
        {
            RepathToDestination();
        }

        private void RepathToDestination()
        {
            if (!hasDestination)
            {
                return;
            }

            RepathCount++;
            BuildPath();
        }

        private bool BuildPath()
        {
            path.Clear();
            pathIndex = 0;

            if (Grid == null || !Grid.IsPassable(destinationCell))
            {
                hasDestination = false;
                return false;
            }

            IPathfinder pathfinder = PathfinderFactory.Create(pathfindingAlgorithm);
            IReadOnlyList<Vector2Int> nextPath = pathfinder.GetPath(Grid, Cell, destinationCell, navigationMode);

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
