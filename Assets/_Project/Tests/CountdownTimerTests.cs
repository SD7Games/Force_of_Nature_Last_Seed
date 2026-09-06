using LastSeed.Core.Timing;
using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class CountdownTimerTests
    {
        [Test]
        public void StartAndAdvance_ClampAtElapsedState()
        {
            CountdownTimer timer = new();
            timer.Start(1f);

            timer.Advance(2f);

            Assert.That(timer.Remaining, Is.Zero);
            Assert.That(timer.IsElapsed, Is.True);
        }

        [Test]
        public void Advance_WithNegativeDelta_DoesNotIncreaseRemainingTime()
        {
            CountdownTimer timer = new();
            timer.Start(1f);

            timer.Advance(-1f);

            Assert.That(timer.Remaining, Is.EqualTo(1f));
        }

        [Test]
        public void LimitTo_ReducesRemainingTimeWithoutExtendingIt()
        {
            CountdownTimer timer = new();
            timer.Start(2f);

            timer.LimitTo(1f);
            Assert.That(timer.Remaining, Is.EqualTo(1f));

            timer.LimitTo(3f);
            Assert.That(timer.Remaining, Is.EqualTo(1f));
        }

        [Test]
        public void Reset_MakesTimerElapsed()
        {
            CountdownTimer timer = new();
            timer.Start(1f);

            timer.Reset();

            Assert.That(timer.IsElapsed, Is.True);
        }
    }
}
