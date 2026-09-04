using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class DisabledRewardedAdServiceTests
    {
        [Test]
        public void ShowRewardedAd_CompletesWithoutGrantingReward()
        {
            DisabledRewardedAdService service = new();
            bool? granted = null;

            service.ShowRewardedAd(value => granted = value);

            Assert.That(service.IsReady, Is.False);
            Assert.That(granted, Is.False);
        }
    }
}
