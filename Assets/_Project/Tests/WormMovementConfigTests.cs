using NUnit.Framework;
using UnityEngine;

namespace LastSeed.Tests
{
    public sealed class WormMovementConfigTests
    {
        private WormMovementConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<WormMovementConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void CreateForwardMotionSettings_UsesConfiguredDefaults()
        {
            WormForwardMotionSettings settings = _config.CreateForwardMotionSettings();

            Assert.That(settings.BaseSpeed, Is.EqualTo(1.2f));
            Assert.That(settings.CatchUpRailPointIndex, Is.EqualTo(3));
            Assert.That(settings.CatchUpSpeed, Is.EqualTo(3f));
            Assert.That(settings.CatchUpStopOffset, Is.Zero);
            Assert.That(settings.CatchUpExtraDistance, Is.EqualTo(1.5f));
            Assert.That(settings.BurstDisableRailPointIndex, Is.EqualTo(-1));
            Assert.That(settings.BurstDisablePathProgress, Is.EqualTo(0.9f));
            Assert.That(settings.BurstSettings.Enabled, Is.True);
            Assert.That(settings.BurstSettings.BurstSpeed, Is.EqualTo(2.5f));
            Assert.That(settings.BurstSettings.Interval, Is.EqualTo(10f));
            Assert.That(settings.BurstSettings.Duration, Is.EqualTo(2.5f));
            Assert.That(settings.BurstSettings.SlowdownDuration, Is.EqualTo(0.35f));
        }

        [Test]
        public void CreateSegmentLayout_CombinesConfigWithRuntimeState()
        {
            WormSegmentChainLayout layout = _config.CreateSegmentLayout(
                headDistance: 12f,
                waveTime: 3f,
                verticalOffset: 2f,
                isSectionRollback: true,
                isReviveRollback: false);

            Assert.That(layout.HeadDistance, Is.EqualTo(12f));
            Assert.That(layout.SegmentSpacing, Is.EqualTo(0.6f));
            Assert.That(layout.TailVisualSpacingMultiplier, Is.EqualTo(1f));
            Assert.That(layout.HeadBridgeSpacingMultiplier, Is.EqualTo(1.25f));
            Assert.That(layout.ActiveDistancePadding, Is.EqualTo(0.5f));
            Assert.That(layout.WaveAmplitude, Is.EqualTo(0.05f));
            Assert.That(layout.WaveFrequency, Is.EqualTo(4f));
            Assert.That(layout.WaveTime, Is.EqualTo(3f));
            Assert.That(layout.VerticalOffset, Is.EqualTo(2f));
            Assert.That(layout.IsSectionRollback, Is.True);
            Assert.That(layout.IsReviveRollback, Is.False);
        }

        [Test]
        public void CreateReviveAnimationSettings_UsesConfiguredDefaults()
        {
            WormReviveAnimationSettings settings = _config.CreateReviveAnimationSettings();

            Assert.That(settings.GameplaySpeed, Is.EqualTo(1.2f));
            Assert.That(settings.SquashDuration, Is.EqualTo(0.08f));
            Assert.That(settings.ThrowDuration, Is.EqualTo(0.38f));
            Assert.That(settings.LandingDuration, Is.EqualTo(0.09f));
            Assert.That(settings.DecelerationPathFraction, Is.EqualTo(0.2f));
            Assert.That(settings.ArcHeight, Is.EqualTo(1.1f));
            Assert.That(settings.SquashXScale, Is.EqualTo(1.22f));
            Assert.That(settings.SquashYScale, Is.EqualTo(0.72f));
            Assert.That(settings.LandingXScale, Is.EqualTo(1.1f));
            Assert.That(settings.LandingYScale, Is.EqualTo(0.86f));
        }
    }
}
