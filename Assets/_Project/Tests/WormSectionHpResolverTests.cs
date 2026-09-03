using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class WormSectionHpResolverTests
    {
        [Test]
        public void ResolveHp_WhenScalingIsDisabled_ReturnsGeneratedHp()
        {
            FakeScalingPolicy policy = new FakeScalingPolicy { Enabled = false };
            WormSectionHpResolver resolver = new WormSectionHpResolver(policy);

            int resolvedHp = resolver.ResolveHp(
                125,
                sectionIndex: 0,
                totalSections: 10,
                levelNumber: 1,
                WeaponPowerSnapshot.Invalid,
                runtimePressureMultiplier: 1f,
                headPathPressureMultiplier: 1f);

            Assert.That(resolvedHp, Is.EqualTo(125));
        }

        [Test]
        public void ResolveHp_WhenDynamicScalingIsEnabled_UsesWeaponDpsAndLifetime()
        {
            FakeScalingPolicy policy = new FakeScalingPolicy
            {
                Enabled = true,
                UsesDynamicHp = true,
                DynamicHpWeight = 1f,
                MaxDynamicHpMultiplier = 20f,
                MinHp = 1,
                MaxHp = 10000,
                HpMultiplier = 1f
            };
            WormSectionHpResolver resolver = new WormSectionHpResolver(policy);
            WeaponPowerSnapshot power = new WeaponPowerSnapshot(
                isValid: true,
                estimatedDps: 50f,
                damagePerProjectile: 10,
                projectilesPerShot: 1,
                salvoShots: 1,
                shotCycleTime: 0.2f);

            int resolvedHp = resolver.ResolveHp(
                baseHp: 100,
                sectionIndex: 0,
                totalSections: 10,
                levelNumber: 1,
                power,
                runtimePressureMultiplier: 1f,
                headPathPressureMultiplier: 1f);

            Assert.That(resolvedHp, Is.EqualTo(250));
        }

        [Test]
        public void Settings_ClampInvalidValuesAtCompositionBoundary()
        {
            WormAdaptiveHpSettings settings = new WormAdaptiveHpSettings(
                levelNumber: 0,
                upgradeRebalanceInterval: 0,
                minimumRebalanceInterval: -1f);

            Assert.That(settings.LevelNumber, Is.EqualTo(1));
            Assert.That(settings.UpgradeRebalanceInterval, Is.EqualTo(1));
            Assert.That(settings.MinimumRebalanceInterval, Is.Zero);
        }

        private sealed class FakeScalingPolicy : IWormHpScalingPolicy
        {
            public bool Enabled { get; set; }
            public bool UsesDynamicHp { get; set; }
            public int BaseSectionHp { get; set; }
            public float DynamicHpWeight { get; set; }
            public float MaxDynamicHpMultiplier { get; set; } = 1f;
            public bool UseBaseHpAsFloor { get; set; }
            public int MinHp { get; set; } = 1;
            public int MaxHp { get; set; } = int.MaxValue;
            public float HpMultiplier { get; set; } = 1f;

            public float GetLevelMultiplier(int levelNumber) => 1f;
            public float GetBaseHpMultiplier(int sectionIndex, int totalSections) => 1f;
            public float GetTargetSectionLifetime(int sectionIndex, int totalSections) => 5f;
            public float GetPressureMultiplier(int sectionIndex, int totalSections) => 1f;
            public float GetHeadPathPressureMultiplier(float headProgressNormalized) => 1f;
            public float GetPostReviveHpMultiplier(bool hasRevivedThisRun) => 1f;
        }
    }
}
