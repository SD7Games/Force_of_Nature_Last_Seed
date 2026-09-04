using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class WeaponDamageClampTests
    {
        [TestCase(double.NaN, 1)]
        [TestCase(-10d, 1)]
        [TestCase(1d, 1)]
        [TestCase(1.5d, 2)]
        [TestCase(10000000d, WeaponDamageClamp.MaximumDamage)]
        public void Clamp_ReturnsSafeProjectileDamage(double rawDamage, int expected)
        {
            Assert.That(WeaponDamageClamp.Clamp(rawDamage), Is.EqualTo(expected));
        }

        [Test]
        public void RuntimeStateCompatibilityMethods_UseSharedClamp()
        {
            const double damage = 42.6d;

            Assert.That(WeaponRuntimeState.ClampDamage(damage), Is.EqualTo(43));
            Assert.That(AcaciaThornRuntimeState.ClampDamage(damage), Is.EqualTo(43));
        }
    }
}
