using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class WormSectionHealthTests
    {
        [Test]
        public void ApplyDamage_ClampsAtZeroAndRaisesDestroyedOnce()
        {
            WormSectionHealth health = new WormSectionHealth();
            int changedCount = 0;
            int destroyedCount = 0;
            health.Changed += () => changedCount++;
            health.Destroyed += () => destroyedCount++;
            health.Initialize(10);

            health.ApplyDamage(15);
            health.ApplyDamage(1);

            Assert.That(health.CurrentHp, Is.Zero);
            Assert.That(health.IsDestroyed, Is.True);
            Assert.That(changedCount, Is.EqualTo(1));
            Assert.That(destroyedCount, Is.EqualTo(1));
        }

        [Test]
        public void ResetHp_RestoresFullHealthAndNotifiesObservers()
        {
            WormSectionHealth health = new WormSectionHealth();
            int changedCount = 0;
            health.Changed += () => changedCount++;
            health.Initialize(10);
            health.ApplyDamage(4);

            health.ResetHp(20);

            Assert.That(health.MaxHp, Is.EqualTo(20));
            Assert.That(health.CurrentHp, Is.EqualTo(20));
            Assert.That(health.HasTakenDamage, Is.False);
            Assert.That(changedCount, Is.EqualTo(2));
        }

        [Test]
        public void ApplyDamage_IgnoresNonPositiveDamage()
        {
            WormSectionHealth health = new WormSectionHealth();
            health.Initialize(10);

            health.ApplyDamage(0);
            health.ApplyDamage(-5);

            Assert.That(health.CurrentHp, Is.EqualTo(10));
        }
    }
}
