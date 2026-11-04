using System;
using DefendAndCollect.Waves;
using NUnit.Framework;

namespace DefendAndCollect.Tests.EditMode
{
    public sealed class WavePlanTests
    {
        [Test]
        public void DefaultPlan_HasThreeEscalatingWaves()
        {
            WavePlan plan = WavePlan.Default;

            Assert.That(plan.WaveCount, Is.EqualTo(3));
            Assert.That(plan.EnemiesInWave(0), Is.EqualTo(3));
            Assert.That(plan.EnemiesInWave(1), Is.EqualTo(4));
            Assert.That(plan.EnemiesInWave(2), Is.EqualTo(5));
            Assert.That(plan.PauseBetweenWaves, Is.GreaterThan(0f));
            Assert.That(plan.EnemiesInWave(0), Is.LessThan(plan.EnemiesInWave(1)));
            Assert.That(plan.EnemiesInWave(1), Is.LessThan(plan.EnemiesInWave(2)));
        }

        [Test]
        public void Plan_RejectsInvalidData()
        {
            Assert.Throws<ArgumentException>(() => new WavePlan(null, 1f));
            Assert.Throws<ArgumentException>(() => new WavePlan(new int[0], 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new WavePlan(new[] { 0 }, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new WavePlan(new[] { -2 }, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new WavePlan(new[] { 2 }, -1f));
        }

        [Test]
        public void EnemiesInWave_RejectsUnknownIndex()
        {
            WavePlan plan = new WavePlan(new[] { 2, 3 }, 2f);

            Assert.Throws<ArgumentOutOfRangeException>(() => plan.EnemiesInWave(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => plan.EnemiesInWave(2));
        }
    }
}
