using System.Collections.Generic;
using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class RewardRarityRollerTests
    {
        [Test]
        public void BuildGuaranteedSlotRarities_FillsGuaranteedPrefix()
        {
            var slots = new List<RewardRaritySlot> { null, null };
            var pools = new Dictionary<RewardRarity, List<RewardModifierEntry>>();

            RewardRarity[] result = RewardRarityRoller.BuildGuaranteedSlotRarities(
                slots,
                count: 2,
                RewardRarity.Legendary,
                guaranteedSlotCount: 2,
                pools);

            Assert.That(result, Is.EqualTo(new[]
            {
                RewardRarity.Legendary,
                RewardRarity.Legendary
            }));
        }

        [Test]
        public void AddAvailableWeight_IgnoresRarityWithoutRewards()
        {
            var pools = new Dictionary<RewardRarity, List<RewardModifierEntry>>();
            float commonWeight = 0f;
            float rareWeight = 0f;
            float legendaryWeight = 0f;

            RewardRarityRoller.AddAvailableWeight(
                RewardRarity.Rare,
                10f,
                pools,
                ref commonWeight,
                ref rareWeight,
                ref legendaryWeight);

            Assert.That(commonWeight, Is.Zero);
            Assert.That(rareWeight, Is.Zero);
            Assert.That(legendaryWeight, Is.Zero);
        }

        [Test]
        public void RollFromWeights_WithoutAvailableWeight_ReturnsCommon()
        {
            RewardRarity rarity = RewardRarityRoller.RollFromWeights(0f, 0f, 0f);

            Assert.That(rarity, Is.EqualTo(RewardRarity.Common));
        }
    }
}
