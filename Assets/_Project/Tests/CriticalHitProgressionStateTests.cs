using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class CriticalHitProgressionStateTests
    {
        [Test]
        public void AddChance_ClampsChanceAndRaisesMinimumDamageMultiplier()
        {
            CriticalHitProgressionState state = new(0.5f, 5f);

            float accepted = state.AddChance(1f, minimumDamageMultiplier: 3f);

            Assert.That(accepted, Is.EqualTo(0.5f));
            Assert.That(state.Chance, Is.EqualTo(0.5f));
            Assert.That(state.DamageMultiplier, Is.EqualTo(3f));
        }

        [Test]
        public void SetLimits_ClampsExistingProgression()
        {
            CriticalHitProgressionState state = new(1f, 10f);
            state.AddChance(0.8f);
            state.AddDamage(5f);

            state.SetLimits(0.25f, 4f);

            Assert.That(state.Chance, Is.EqualTo(0.25f));
            Assert.That(state.DamageMultiplier, Is.EqualTo(4f));
        }

        [Test]
        public void Clone_HasIndependentProgression()
        {
            CriticalHitProgressionState source = new(1f, 10f);
            source.AddChance(0.1f);
            CriticalHitProgressionState clone = source.Clone();

            source.AddChance(0.1f);

            Assert.That(clone.Chance, Is.EqualTo(0.1f));
            Assert.That(source.Chance, Is.EqualTo(0.2f));
        }
    }
}
