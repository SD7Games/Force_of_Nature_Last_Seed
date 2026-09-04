using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class WeaponShotPatternStateTests
    {
        [Test]
        public void SetLimits_RestrictsParallelAndSalvoProgression()
        {
            WeaponShotPatternState state = new();
            state.SetLimits(maxParallelProjectiles: 2, maxSalvoExtraShots: 1);

            Assert.That(state.AddParallelProjectiles(3, 0.7f), Is.EqualTo(1));
            Assert.That(state.AddSalvoShots(3, 0.15f), Is.EqualTo(1));
            Assert.That(state.CanAddParallelProjectiles, Is.False);
            Assert.That(state.CanAddSalvoShots, Is.False);
        }

        [Test]
        public void Clone_CopiesValuesWithoutSharingFutureProgression()
        {
            WeaponShotPatternState source = new();
            source.AddParallelProjectiles(1, 0.8f);
            WeaponShotPatternState clone = source.Clone();

            source.AddParallelProjectiles(1, 1f);

            Assert.That(clone.ParallelProjectileCount, Is.EqualTo(2));
            Assert.That(clone.ParallelSpacing, Is.EqualTo(0.8f));
            Assert.That(source.ParallelProjectileCount, Is.EqualTo(3));
        }

        [Test]
        public void Reset_ClearsPatternProgression()
        {
            WeaponShotPatternState state = new();
            state.AddParallelProjectiles(2, 1f);
            state.AddSalvoShots(2, 0.1f);

            state.Reset();

            Assert.That(state.ParallelProjectileCount, Is.EqualTo(1));
            Assert.That(state.ParallelSpacing, Is.EqualTo(0.5f));
            Assert.That(state.SalvoExtraShots, Is.Zero);
            Assert.That(state.SalvoInterval, Is.EqualTo(0.2f));
        }
    }
}
