using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class WormSpawnSettingsTests
    {
        [Test]
        public void Constructor_ClampsInvalidValuesAtCompositionBoundary()
        {
            WormSpawnSettings settings = new WormSpawnSettings(
                sectionCount: 0,
                poolPadding: -1,
                prewarmBatchSize: 0);

            Assert.That(settings.SectionCount, Is.EqualTo(1));
            Assert.That(settings.PoolPadding, Is.Zero);
            Assert.That(settings.PrewarmBatchSize, Is.EqualTo(1));
        }

        [Test]
        public void BodyPoolCapacity_IncludesGeneratedBodyAndPadding()
        {
            WormSpawnSettings settings = new WormSpawnSettings(
                sectionCount: 3,
                poolPadding: 7,
                prewarmBatchSize: 10);

            int expectedBodyCount = WormPatternBuilder.GetBodySegmentCount(3);
            Assert.That(settings.BodyPoolCapacity, Is.EqualTo(expectedBodyCount + 7));
        }
    }
}
