using System.Collections.Generic;
using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class RewardWeightedPickerTests
    {
        [Test]
        public void TryTakeFromRarity_WithSingleEligibleEntry_ReturnsAndRemovesIt()
        {
            var entry = new RewardModifierEntry();
            var pool = new List<RewardModifierEntry> { entry };
            var pools = new Dictionary<RewardRarity, List<RewardModifierEntry>>
            {
                { RewardRarity.Common, pool }
            };

            bool taken = RewardWeightedPicker.TryTakeFromRarity(
                pools,
                RewardRarity.Common,
                new HashSet<RewardModifierCategory>(),
                new HashSet<int>(),
                RewardPickMode.Any,
                out RewardModifierEntry selected,
                default);

            Assert.That(taken, Is.True);
            Assert.That(selected, Is.SameAs(entry));
            Assert.That(pool, Is.Empty);
        }

        [Test]
        public void TryTakeFromRarity_WhenRarityIsMissing_DoesNotSelectReward()
        {
            bool taken = RewardWeightedPicker.TryTakeFromRarity(
                new Dictionary<RewardRarity, List<RewardModifierEntry>>(),
                RewardRarity.Legendary,
                new HashSet<RewardModifierCategory>(),
                new HashSet<int>(),
                RewardPickMode.Any,
                out RewardModifierEntry selected,
                default);

            Assert.That(taken, Is.False);
            Assert.That(selected, Is.Null);
        }
    }
}
