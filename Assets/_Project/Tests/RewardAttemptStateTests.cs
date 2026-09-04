using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class RewardAttemptStateTests
    {
        [Test]
        public void ConsumeAndReset_PreserveIndependentAttemptBudgets()
        {
            RewardAttemptState state = new(new RewardFlowSettings(1, 2, 3));

            Assert.That(state.ConsumeFreeReroll(), Is.True);
            Assert.That(state.ConsumeFreeReroll(), Is.False);
            Assert.That(state.ConsumeAdReroll(), Is.True);
            Assert.That(state.ConsumeTakeAll(), Is.True);

            Assert.That(state.FreeRerollsLeft, Is.Zero);
            Assert.That(state.AdRerollsLeft, Is.EqualTo(1));
            Assert.That(state.TakeAllLeft, Is.EqualTo(2));

            state.Reset();

            Assert.That(state.FreeRerollsLeft, Is.EqualTo(1));
            Assert.That(state.AdRerollsLeft, Is.EqualTo(2));
            Assert.That(state.TakeAllLeft, Is.EqualTo(3));
        }
    }
}
