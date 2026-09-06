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
            WormSectionHealthChange finalChange = default;
            health.Changed += _ => changedCount++;
            health.Destroyed += change =>
            {
                destroyedCount++;
                finalChange = change;
            };
            health.Initialize(10);

            health.ApplyDamage(15);
            health.ApplyDamage(1);

            Assert.That(health.CurrentHp, Is.Zero);
            Assert.That(health.IsDestroyed, Is.True);
            Assert.That(changedCount, Is.EqualTo(1));
            Assert.That(destroyedCount, Is.EqualTo(1));
            Assert.That(finalChange.PreviousHp, Is.EqualTo(10));
            Assert.That(finalChange.CurrentHp, Is.Zero);
            Assert.That(finalChange.MaxHp, Is.EqualTo(10));
            Assert.That(finalChange.AppliedDamage, Is.EqualTo(10));
            Assert.That(finalChange.IsReset, Is.False);
            Assert.That(finalChange.IsDestroyed, Is.True);
        }

        [Test]
        public void ResetHp_RestoresFullHealthAndNotifiesObservers()
        {
            WormSectionHealth health = new WormSectionHealth();
            int changedCount = 0;
            WormSectionHealthChange lastChange = default;
            health.Changed += change =>
            {
                changedCount++;
                lastChange = change;
            };
            health.Initialize(10);
            health.ApplyDamage(4);

            health.ResetHp(20);

            Assert.That(health.MaxHp, Is.EqualTo(20));
            Assert.That(health.CurrentHp, Is.EqualTo(20));
            Assert.That(health.HasTakenDamage, Is.False);
            Assert.That(changedCount, Is.EqualTo(2));
            Assert.That(lastChange.PreviousHp, Is.EqualTo(6));
            Assert.That(lastChange.CurrentHp, Is.EqualTo(20));
            Assert.That(lastChange.MaxHp, Is.EqualTo(20));
            Assert.That(lastChange.AppliedDamage, Is.Zero);
            Assert.That(lastChange.IsReset, Is.True);
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
