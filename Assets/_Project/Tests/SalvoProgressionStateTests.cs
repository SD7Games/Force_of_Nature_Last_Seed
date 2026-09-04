using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class SalvoProgressionStateTests
    {
        [Test]
        public void Add_ClampsToConfiguredLimit()
        {
            SalvoProgressionState state = new(defaultLimit: 2, hardLimit: 5);

            int accepted = state.Add(4);

            Assert.That(accepted, Is.EqualTo(2));
            Assert.That(state.ExtraShots, Is.EqualTo(2));
            Assert.That(state.CanAdd, Is.False);
        }

        [Test]
        public void ExpandedProspectiveLimit_DoesNotMutateLimit()
        {
            SalvoProgressionState state = new(defaultLimit: 1, hardLimit: 5);

            Assert.That(state.CanApply(2, limitAfterApply: 3), Is.True);
            Assert.That(state.Add(2), Is.EqualTo(1));
        }

        [Test]
        public void Clone_HasIndependentProgression()
        {
            SalvoProgressionState source = new(defaultLimit: 3, hardLimit: 5);
            source.Add(1);
            SalvoProgressionState clone = source.Clone();

            source.Add(1);

            Assert.That(clone.ExtraShots, Is.EqualTo(1));
            Assert.That(source.ExtraShots, Is.EqualTo(2));
        }
    }
}
