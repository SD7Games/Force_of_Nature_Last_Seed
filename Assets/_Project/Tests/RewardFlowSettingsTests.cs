using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class RewardFlowSettingsTests
    {
        [Test]
        public void Constructor_ClampsNegativeAttemptCounts()
        {
            RewardFlowSettings settings = new(-1, -2, -3);

            Assert.That(settings.FreeRerollAttemptsPerSession, Is.Zero);
            Assert.That(settings.AdRerollAttemptsPerSession, Is.Zero);
            Assert.That(settings.TakeAllAttemptsPerSession, Is.Zero);
        }
    }
}
