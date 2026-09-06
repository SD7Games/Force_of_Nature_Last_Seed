using System;
using LastSeed.Core.Timing;
using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class TimedBurstTests
    {
        [Test]
        public void Begin_MakesFirstShotReadyImmediately()
        {
            TimedBurst burst = new();

            burst.Begin(3);

            Assert.That(burst.IsActive, Is.True);
            Assert.That(burst.IsShotReady, Is.True);
            Assert.That(burst.ShotsRemaining, Is.EqualTo(3));
        }

        [Test]
        public void CommitShot_SchedulesNextShotAndCompletesLastShot()
        {
            TimedBurst burst = new();
            burst.Begin(2);

            burst.CommitShot(0.5f);
            burst.Advance(0.49f);

            Assert.That(burst.IsShotReady, Is.False);
            Assert.That(burst.ShotsRemaining, Is.EqualTo(1));

            burst.Advance(0.01f);
            burst.CommitShot(0.5f);

            Assert.That(burst.IsActive, Is.False);
            Assert.That(burst.IsShotReady, Is.False);
            Assert.That(burst.ShotsRemaining, Is.Zero);
        }

        [Test]
        public void Advance_WithLargeDelta_ProducesOnlyOneReadyShot()
        {
            TimedBurst burst = new();
            burst.Begin(3);
            burst.CommitShot(0.1f);

            burst.Advance(1f);
            burst.Advance(1f);

            Assert.That(burst.IsShotReady, Is.True);
            Assert.That(burst.ShotsRemaining, Is.EqualTo(2));
        }

        [Test]
        public void CommitShot_WithoutReadyShot_Throws()
        {
            TimedBurst burst = new();

            Assert.Throws<InvalidOperationException>(() => burst.CommitShot(0.1f));
        }

        [Test]
        public void Reset_ClearsActiveSequence()
        {
            TimedBurst burst = new();
            burst.Begin(3);

            burst.Reset();

            Assert.That(burst.IsActive, Is.False);
            Assert.That(burst.IsShotReady, Is.False);
            Assert.That(burst.ShotsRemaining, Is.Zero);
        }
    }
}
