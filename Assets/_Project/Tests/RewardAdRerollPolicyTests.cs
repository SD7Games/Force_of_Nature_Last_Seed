using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class RewardAdRerollPolicyTests
    {
        [TestCase(0.05f, RewardRarity.Legendary)]
        [TestCase(0.5f, RewardRarity.Rare)]
        public void RollGuaranteedRarity_UsesInjectedRandomSource(
            float randomValue,
            RewardRarity expected)
        {
            RewardRollContext context = new(
                headPathProgressNormalized: 0.7f,
                wormDestructionProgressNormalized: 0f,
                hasRevivedThisRun: false);
            TestRandomSource randomSource = new(values: new[] { randomValue });

            RewardRarity result = RewardAdRerollPolicy.RollGuaranteedRarity(
                null,
                null,
                context,
                randomSource);

            Assert.That(result, Is.EqualTo(expected));
            Assert.That(randomSource.Calls, Is.EqualTo(1));
        }
    }
}
