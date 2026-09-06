using System;
using LastSeed.Core.Combat;
using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class HealthTests
    {
        [Test]
        public void ApplyDamage_ClampsAtZeroAndRaisesDepletedOnce()
        {
            Health health = new();
            int changedCount = 0;
            int depletedCount = 0;
            HealthChange finalChange = default;
            health.Changed += _ => changedCount++;
            health.Depleted += change =>
            {
                depletedCount++;
                finalChange = change;
            };
            health.Initialize(10);

            health.ApplyDamage(15);
            health.ApplyDamage(1);

            Assert.That(health.CurrentHp, Is.Zero);
            Assert.That(health.IsDepleted, Is.True);
            Assert.That(changedCount, Is.EqualTo(1));
            Assert.That(depletedCount, Is.EqualTo(1));
            Assert.That(finalChange.PreviousHp, Is.EqualTo(10));
            Assert.That(finalChange.CurrentHp, Is.Zero);
            Assert.That(finalChange.MaxHp, Is.EqualTo(10));
            Assert.That(finalChange.AppliedDamage, Is.EqualTo(10));
            Assert.That(finalChange.IsReset, Is.False);
            Assert.That(finalChange.IsDepleted, Is.True);
        }

        [Test]
        public void Reset_RestoresFullHealthAndNotifiesObservers()
        {
            Health health = new();
            int changedCount = 0;
            HealthChange lastChange = default;
            health.Changed += change =>
            {
                changedCount++;
                lastChange = change;
            };
            health.Initialize(10);
            health.ApplyDamage(4);

            health.Reset(20);

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
            Health health = new();
            health.Initialize(10);

            health.ApplyDamage(0);
            health.ApplyDamage(-5);

            Assert.That(health.CurrentHp, Is.EqualTo(10));
        }
    }
}
