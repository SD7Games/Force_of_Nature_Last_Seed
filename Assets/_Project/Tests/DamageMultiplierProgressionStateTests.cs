using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class DamageMultiplierProgressionStateTests
    {
        [Test]
        public void Apply_MultipliesAndClampsToLimit()
        {
            DamageMultiplierProgressionState state = new(limit: 3f);

            Assert.That(state.Apply(2f), Is.EqualTo(1f));
            Assert.That(state.Apply(2f), Is.EqualTo(1f));
            Assert.That(state.Value, Is.EqualTo(3f));
            Assert.That(state.CanAdd, Is.False);
        }

        [Test]
        public void SetLimit_ClampsExistingValue()
        {
            DamageMultiplierProgressionState state = new(limit: 10f);
            state.Apply(5f);

            state.SetLimit(2f);

            Assert.That(state.Value, Is.EqualTo(2f));
        }

        [Test]
        public void Clone_HasIndependentProgression()
        {
            DamageMultiplierProgressionState source = new(limit: 10f);
            source.Apply(2f);
            DamageMultiplierProgressionState clone = source.Clone();

            source.Apply(2f);

            Assert.That(clone.Value, Is.EqualTo(2f));
            Assert.That(source.Value, Is.EqualTo(4f));
        }
    }
}
