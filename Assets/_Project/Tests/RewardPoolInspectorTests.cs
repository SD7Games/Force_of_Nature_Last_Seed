using System.Collections.Generic;
using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class RewardPoolInspectorTests
    {
        [Test]
        public void CountRewards_IgnoresNullPools()
        {
            var pools = new Dictionary<RewardRarity, List<RewardModifierEntry>>
            {
                { RewardRarity.Common, new List<RewardModifierEntry> { new(), new() } },
                { RewardRarity.Rare, null }
            };

            Assert.That(RewardPoolInspector.CountRewards(pools), Is.EqualTo(2));
        }

        [Test]
        public void GetHighestAvailableRarity_ReturnsHighestNonEmptyPool()
        {
            var pools = new Dictionary<RewardRarity, List<RewardModifierEntry>>
            {
                { RewardRarity.Common, new List<RewardModifierEntry> { new() } },
                { RewardRarity.Rare, new List<RewardModifierEntry> { new() } },
                { RewardRarity.Legendary, new List<RewardModifierEntry>() }
            };

            Assert.That(
                RewardPoolInspector.GetHighestAvailableRarity(pools),
                Is.EqualTo(RewardRarity.Rare));
        }
    }
}
