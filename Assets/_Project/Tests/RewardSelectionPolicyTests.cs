using System.Collections.Generic;
using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class RewardSelectionPolicyTests
    {
        [Test]
        public void ShouldUseAssistDpsBias_OnlyForReviveOrPaidRoll()
        {
            var normal = new RewardRollContext(0f, 0f, false);
            var revived = new RewardRollContext(0f, 0f, true);
            RewardRollContext paid = normal.WithPaidAssistRoll();

            Assert.That(RewardSelectionPolicy.ShouldUseAssistDpsBias(normal), Is.False);
            Assert.That(RewardSelectionPolicy.ShouldUseAssistDpsBias(revived), Is.True);
            Assert.That(RewardSelectionPolicy.ShouldUseAssistDpsBias(paid), Is.True);
        }

        [Test]
        public void IsEligible_AnyModeAcceptsPositiveWeightEntry()
        {
            var entry = new RewardModifierEntry();

            bool isEligible = RewardSelectionPolicy.IsEligible(
                entry,
                new HashSet<RewardModifierCategory>(),
                new HashSet<int>(),
                RewardPickMode.Any,
                RewardWeaponGroup.None);

            Assert.That(isEligible, Is.True);
        }

        [Test]
        public void IsEligible_RejectsNullEntry()
        {
            bool isEligible = RewardSelectionPolicy.IsEligible(
                null,
                new HashSet<RewardModifierCategory>(),
                new HashSet<int>(),
                RewardPickMode.Any,
                RewardWeaponGroup.None);

            Assert.That(isEligible, Is.False);
        }
    }
}
