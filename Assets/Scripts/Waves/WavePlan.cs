using System;
using System.Collections.Generic;

namespace DefendAndCollect.Waves
{
    public sealed class WavePlan
    {
        private readonly int[] enemyCounts;

        public static WavePlan Default { get; } = new WavePlan(new[] { 3, 4, 5 }, 5f);

        public WavePlan(IReadOnlyList<int> enemyCounts, float pauseBetweenWaves)
        {
            if (enemyCounts == null || enemyCounts.Count == 0)
            {
                throw new ArgumentException("A wave plan needs at least one wave.", nameof(enemyCounts));
            }

            if (pauseBetweenWaves < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pauseBetweenWaves),
                    pauseBetweenWaves,
                    "The pause between waves cannot be negative.");
            }

            this.enemyCounts = new int[enemyCounts.Count];

            for (int i = 0; i < enemyCounts.Count; i++)
            {
                if (enemyCounts[i] <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(enemyCounts),
                        enemyCounts[i],
                        "Every wave needs at least one enemy.");
                }

                this.enemyCounts[i] = enemyCounts[i];
            }

            PauseBetweenWaves = pauseBetweenWaves;
        }

        public int WaveCount => enemyCounts.Length;

        public float PauseBetweenWaves { get; }

        public IReadOnlyList<int> EnemyCounts => enemyCounts;

        public int EnemiesInWave(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= enemyCounts.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(waveIndex), waveIndex, "Unknown wave index.");
            }

            return enemyCounts[waveIndex];
        }
    }
}
