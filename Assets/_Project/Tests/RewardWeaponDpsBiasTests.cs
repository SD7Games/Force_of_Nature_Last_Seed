using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class RewardWeaponDpsBiasTests
    {
        [Test]
        public void None_DoesNotChangeWeaponWeights()
        {
            Assert.That(
                RewardWeaponDpsBias.None.GetMultiplier(RewardWeaponGroup.MainWeapon),
                Is.EqualTo(1f));
            Assert.That(
                RewardWeaponDpsBias.None.GetMultiplier(RewardWeaponGroup.AcaciaThorn),
                Is.EqualTo(1f));
        }

        [Test]
        public void Create_PrefersWeakerWeaponAndReducesStrongerWeaponWeight()
        {
            RewardWeaponDpsBias bias = RewardWeaponDpsBias.Create(
                RewardWeaponGroup.MainWeapon,
                normalizedImbalance: 1f);

            Assert.That(
                bias.GetMultiplier(RewardWeaponGroup.MainWeapon),
                Is.GreaterThan(1f));
            Assert.That(
                bias.GetMultiplier(RewardWeaponGroup.AcaciaThorn),
                Is.LessThan(1f));
            Assert.That(
                bias.GetMultiplier(RewardWeaponGroup.None),
                Is.EqualTo(1f));
        }
    }
}
