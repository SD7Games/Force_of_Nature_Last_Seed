using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class WormPathProgressStateTests
    {
        [Test]
        public void Apply_UpdatesMotionStateAndCompletesPathOnlyOnce()
        {
            WormPathProgressState state = new();
            WormForwardMotionResult result = new(
                headDistance: 12f,
                isCatchingUp: true,
                completedPath: true);

            bool firstCompletion = state.Apply(result);
            bool repeatedCompletion = state.Apply(result);

            Assert.That(state.HeadDistance, Is.EqualTo(12f));
            Assert.That(state.IsCatchingUp, Is.True);
            Assert.That(firstCompletion, Is.True);
            Assert.That(repeatedCompletion, Is.False);
        }

        [Test]
        public void ReopenPath_AllowsCompletionAfterRollback()
        {
            WormPathProgressState state = new();
            state.TryComplete(true);

            state.ReopenPath();

            Assert.That(state.TryComplete(true), Is.True);
        }

        [Test]
        public void Reset_ClearsDistanceCompletionAndSetsCatchUpState()
        {
            WormPathProgressState state = new();
            state.SetHeadDistance(10f);
            state.TryComplete(true);

            state.Reset(isCatchingUp: true);

            Assert.That(state.HeadDistance, Is.Zero);
            Assert.That(state.IsCatchingUp, Is.True);
            Assert.That(state.TryComplete(true), Is.True);
        }
    }
}
