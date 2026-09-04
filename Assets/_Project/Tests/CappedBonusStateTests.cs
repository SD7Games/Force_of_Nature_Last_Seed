using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class CappedBonusStateTests
    {
        [Test]
        public void Add_ClampsAcceptedBonusToLimit()
        {
            CappedBonusState state = new(limit: 2f);

            float accepted = state.Add(3f);

            Assert.That(accepted, Is.EqualTo(2f));
            Assert.That(state.Value, Is.EqualTo(2f));
            Assert.That(state.CanAdd, Is.False);
        }

        [Test]
        public void LowerLimit_ClampsExistingValue()
        {
            CappedBonusState state = new(limit: 3f);
            state.Add(2f);

            state.SetLimit(1f);

            Assert.That(state.Value, Is.EqualTo(1f));
            Assert.That(state.Limit, Is.EqualTo(1f));
        }

        [Test]
        public void Clone_DoesNotShareFutureProgression()
        {
            CappedBonusState source = new(limit: 3f);
            source.Add(1f);
            CappedBonusState clone = source.Clone();

            source.Add(1f);

            Assert.That(clone.Value, Is.EqualTo(1f));
            Assert.That(source.Value, Is.EqualTo(2f));
        }
    }
}
