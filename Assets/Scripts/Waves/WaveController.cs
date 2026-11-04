using System;
using System.Collections.Generic;
using DefendAndCollect.Agents;
using DefendAndCollect.Grid;
using UnityEngine;

namespace DefendAndCollect.Waves
{
    public enum MatchState
    {
        Idle = 0,
        InWave = 1,
        BetweenWaves = 2,
        Victory = 3,
        Defeat = 4
    }

    [DefaultExecutionOrder(10)]
    public sealed class WaveController : MonoBehaviour
    {
        private const int MaxSpawnSearchRadius = 4;

        private readonly List<Vector2Int> waveSpawnCells = new List<Vector2Int>();

        private FsmDemoBootstrap demo;
        private IReadOnlyList<Vector2Int> spawnPoints;
        private int currentWaveIndex = -1;
        private float pauseRemaining;
        private MatchState state = MatchState.Idle;

        public WavePlan Plan { get; } = WavePlan.Default;

        public MatchState State => state;

        public int CurrentWave => currentWaveIndex + 1;

        public int WaveCount => Plan.WaveCount;

        public int EnemiesRemaining => CountAliveEnemies();

        public int AlliesAlive => CountAliveAllies();

        public int AlliesTotal => demo != null ? demo.Allies.Count : 0;

        public float PauseRemaining => pauseRemaining;

        public event Action<int> WaveStarted;

        public event Action<MatchState> MatchEnded;

        private void Start()
        {
            ResolveDependencies();

            if (state == MatchState.Idle &&
                demo != null &&
                demo.Grid != null &&
                demo.EnemyCount == 0 &&
                demo.EnemySpawnCells.Count > 0)
            {
                StartMatch(demo.EnemySpawnCells);
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void StartMatch(IReadOnlyList<Vector2Int> enemySpawnCells)
        {
            ResolveDependencies();

            if (demo == null || demo.Grid == null)
            {
                return;
            }

            IReadOnlyList<Vector2Int> cells = enemySpawnCells ?? demo.EnemySpawnCells;

            if (cells == null || cells.Count == 0)
            {
                return;
            }

            spawnPoints = cells;
            ClearEnemies();
            currentWaveIndex = -1;
            pauseRemaining = 0f;
            SpawnWave(0);
        }

        public void Restart()
        {
            StartMatch(spawnPoints);
        }

        public void StopMatch()
        {
            ClearEnemies();
            currentWaveIndex = -1;
            pauseRemaining = 0f;
            state = MatchState.Idle;
        }

        public void Tick(float deltaTime)
        {
            if (demo == null || deltaTime <= 0f)
            {
                return;
            }

            if (state != MatchState.InWave && state != MatchState.BetweenWaves)
            {
                return;
            }

            if (CountAliveAllies() == 0)
            {
                EndMatch(MatchState.Defeat);
                return;
            }

            if (state == MatchState.InWave)
            {
                if (CountAliveEnemies() > 0)
                {
                    return;
                }

                if (currentWaveIndex >= Plan.WaveCount - 1)
                {
                    EndMatch(MatchState.Victory);
                    return;
                }

                state = MatchState.BetweenWaves;
                pauseRemaining = Plan.PauseBetweenWaves;
                return;
            }

            pauseRemaining -= deltaTime;

            if (pauseRemaining <= 0f)
            {
                SpawnWave(currentWaveIndex + 1);
            }
        }

        private void SpawnWave(int waveIndex)
        {
            currentWaveIndex = waveIndex;
            pauseRemaining = 0f;
            state = MatchState.InWave;
            waveSpawnCells.Clear();

            int enemyCount = Plan.EnemiesInWave(waveIndex);

            for (int i = 0; i < enemyCount; i++)
            {
                Vector2Int preferred = spawnPoints[(waveIndex + i) % spawnPoints.Count];

                if (!TryFindSpawnCell(preferred, out Vector2Int cell))
                {
                    continue;
                }

                EnemyAgent enemy = demo.SpawnEnemy(cell);

                if (enemy != null)
                {
                    waveSpawnCells.Add(cell);
                }
            }

            WaveStarted?.Invoke(CurrentWave);
        }

        private bool TryFindSpawnCell(Vector2Int preferred, out Vector2Int cell)
        {
            for (int radius = 0; radius <= MaxSpawnSearchRadius; radius++)
            {
                for (int y = preferred.y - radius; y <= preferred.y + radius; y++)
                {
                    for (int x = preferred.x - radius; x <= preferred.x + radius; x++)
                    {
                        bool onRing = Mathf.Abs(x - preferred.x) == radius || Mathf.Abs(y - preferred.y) == radius;

                        if (!onRing)
                        {
                            continue;
                        }

                        Vector2Int candidate = new Vector2Int(x, y);

                        if (IsSpawnCellFree(candidate))
                        {
                            cell = candidate;
                            return true;
                        }
                    }
                }
            }

            cell = preferred;
            return false;
        }

        private bool IsSpawnCellFree(Vector2Int cell)
        {
            GameGrid grid = demo.Grid;

            if (grid == null || !grid.IsPassable(cell))
            {
                return false;
            }

            if (cell == demo.Deposit)
            {
                return false;
            }

            if (waveSpawnCells.Contains(cell))
            {
                return false;
            }

            for (int i = 0; i < demo.Allies.Count; i++)
            {
                AllyAgent ally = demo.Allies[i];

                if (ally != null && ally.Cell == cell)
                {
                    return false;
                }
            }

            for (int i = 0; i < demo.Enemies.Count; i++)
            {
                EnemyAgent enemy = demo.Enemies[i];

                if (enemy != null && enemy.IsAlive && enemy.Cell == cell)
                {
                    return false;
                }
            }

            return true;
        }

        private int CountAliveEnemies()
        {
            if (demo == null)
            {
                return 0;
            }

            int alive = 0;

            for (int i = 0; i < demo.Enemies.Count; i++)
            {
                EnemyAgent enemy = demo.Enemies[i];

                if (enemy != null && enemy.IsAlive)
                {
                    alive++;
                }
            }

            return alive;
        }

        private int CountAliveAllies()
        {
            if (demo == null)
            {
                return 0;
            }

            int alive = 0;

            for (int i = 0; i < demo.Allies.Count; i++)
            {
                AllyAgent ally = demo.Allies[i];

                if (ally != null && ally.IsAlive)
                {
                    alive++;
                }
            }

            return alive;
        }

        private void ClearEnemies()
        {
            if (demo != null)
            {
                demo.ClearEnemies();
            }

            waveSpawnCells.Clear();
        }

        private void EndMatch(MatchState result)
        {
            state = result;
            pauseRemaining = 0f;
            MatchEnded?.Invoke(result);
        }

        private void ResolveDependencies()
        {
            if (demo != null)
            {
                return;
            }

            demo = GetComponent<FsmDemoBootstrap>();

            if (demo == null)
            {
                demo = FindAnyObjectByType<FsmDemoBootstrap>();
            }
        }
    }
}
