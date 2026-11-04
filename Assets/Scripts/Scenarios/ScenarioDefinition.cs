using System;
using System.Collections.Generic;
using DefendAndCollect.Grid;
using UnityEngine;

namespace DefendAndCollect.Scenarios
{
    public sealed class ScenarioDefinition
    {
        private const float MinimumDetectionRadius = 0.1f;

        private readonly string[] rows;

        public ScenarioDefinition(
            string id,
            string displayName,
            string description,
            string[] rows,
            Vector2Int deposit,
            IReadOnlyList<Vector2Int> resources,
            IReadOnlyList<Vector2Int> allySpawns,
            IReadOnlyList<Vector2Int> enemySpawns,
            float detectionRadius = 6f)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Scenario id must not be empty.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Scenario display name must not be empty.", nameof(displayName));
            }

            if (rows == null || rows.Length == 0)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            if (resources == null || resources.Count == 0)
            {
                throw new ArgumentException("Scenario needs at least one resource.", nameof(resources));
            }

            if (allySpawns == null || allySpawns.Count == 0)
            {
                throw new ArgumentException("Scenario needs at least one ally spawn.", nameof(allySpawns));
            }

            if (enemySpawns == null || enemySpawns.Count == 0)
            {
                throw new ArgumentException("Scenario needs at least one enemy spawn.", nameof(enemySpawns));
            }

            if (detectionRadius < MinimumDetectionRadius)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(detectionRadius),
                    detectionRadius,
                    "Detection radius must be positive.");
            }

            this.rows = (string[])rows.Clone();
            Id = id;
            DisplayName = displayName;
            Description = description ?? string.Empty;
            Deposit = deposit;
            Resources = Copy(resources);
            AllySpawns = Copy(allySpawns);
            EnemySpawns = Copy(enemySpawns);
            DetectionRadius = detectionRadius;

            ValidateCells();
        }

        public string Id { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public IReadOnlyList<string> Rows => rows;

        public Vector2Int Deposit { get; }

        public IReadOnlyList<Vector2Int> Resources { get; }

        public IReadOnlyList<Vector2Int> AllySpawns { get; }

        public IReadOnlyList<Vector2Int> EnemySpawns { get; }

        public float DetectionRadius { get; }

        public GameGrid CreateGrid()
        {
            return GameGrid.FromRows(rows);
        }

        private void ValidateCells()
        {
            GameGrid grid = CreateGrid();
            HashSet<Vector2Int> usedCells = new HashSet<Vector2Int>();

            RequirePassableCell(grid, Deposit, "deposit", usedCells);

            for (int i = 0; i < Resources.Count; i++)
            {
                RequirePassableCell(grid, Resources[i], $"resource {i + 1}", usedCells);
            }

            for (int i = 0; i < AllySpawns.Count; i++)
            {
                RequirePassableCell(grid, AllySpawns[i], $"ally spawn {i + 1}", usedCells);
            }

            for (int i = 0; i < EnemySpawns.Count; i++)
            {
                RequirePassableCell(grid, EnemySpawns[i], $"enemy spawn {i + 1}", usedCells);
            }
        }

        private static void RequirePassableCell(GameGrid grid, Vector2Int cell, string label, HashSet<Vector2Int> usedCells)
        {
            if (!grid.IsInBounds(cell))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cell),
                    cell,
                    $"Scenario {label} is outside the grid.");
            }

            if (!grid.IsPassable(cell))
            {
                throw new ArgumentException(
                    $"Scenario {label} at ({cell.x}, {cell.y}) must be passable.",
                    nameof(cell));
            }

            if (!usedCells.Add(cell))
            {
                throw new ArgumentException(
                    $"Scenario cell ({cell.x}, {cell.y}) is used more than once.",
                    nameof(cell));
            }
        }

        private static IReadOnlyList<Vector2Int> Copy(IReadOnlyList<Vector2Int> source)
        {
            Vector2Int[] copy = new Vector2Int[source.Count];

            for (int i = 0; i < source.Count; i++)
            {
                copy[i] = source[i];
            }

            return Array.AsReadOnly(copy);
        }
    }
}
