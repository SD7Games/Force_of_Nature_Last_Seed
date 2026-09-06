using LastSeed.Core.Timing;
using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class PreparedActionTimerTests
    {
        [Test]
        public void Advance_ReachesConfiguredDuration()
        {
            PreparedActionTimer timer = new();
            timer.Begin();

            timer.Advance(0.5f);

            Assert.That(timer.HasReached(0.5f), Is.True);
            Assert.That(timer.Elapsed, Is.EqualTo(0.5f));
        }

        [Test]
        public void TryComplete_ClampsDelayAndDeactivatesTimer()
        {
            PreparedActionTimer timer = new();
            timer.Begin();

            bool completed = timer.TryComplete(2f, 1f);

            Assert.That(completed, Is.True);
            Assert.That(timer.IsActive, Is.False);
            Assert.That(timer.LastCompletionDelay, Is.EqualTo(1f));
        }

        [Test]
        public void TryComplete_WhenInactive_DoesNotOverwriteLastDelay()
        {
            PreparedActionTimer timer = new();
            timer.Begin();
            timer.TryComplete(0.5f, 1f);

            bool completed = timer.TryComplete(0.8f, 1f);

            Assert.That(completed, Is.False);
            Assert.That(timer.LastCompletionDelay, Is.EqualTo(0.5f));
        }

        [Test]
        public void Reset_ClearsAllState()
        {
            PreparedActionTimer timer = new();
            timer.Begin();
            timer.Advance(0.5f);

            timer.Reset();

            Assert.That(timer.IsActive, Is.False);
            Assert.That(timer.Elapsed, Is.Zero);
            Assert.That(timer.LastCompletionDelay, Is.Zero);
        }
    }
}
