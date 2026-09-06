using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class RewardPopupStateFactoryTests
    {
        [Test]
        public void Create_WithFreeAttempt_EnablesOnlyFreeReroll()
        {
            RewardAttemptState attempts = new(new RewardFlowSettings(1, 1, 1));
            RewardPopupStateFactory factory = new(attempts);
            RewardRollContext context = new(
                headPathProgressNormalized: 1f,
                wormDestructionProgressNormalized: 0f,
                hasRevivedThisRun: false);

            RewardPopupState state = factory.Create(
                RewardRarity.Common,
                cocoonProfile: null,
                context,
                isRewardOperationPending: false);

            Assert.That(state.CanFreeReroll, Is.True);
            Assert.That(state.CanAdReroll, Is.False);
            Assert.That(state.CanTakeAll, Is.True);
        }

        [Test]
        public void Create_AfterFreeAttemptConsumed_EnablesAdReroll()
        {
            RewardAttemptState attempts = new(new RewardFlowSettings(1, 1, 0));
            RewardPopupStateFactory factory = new(attempts);
            attempts.ConsumeFreeReroll();

            RewardPopupState state = factory.Create(
                RewardRarity.Rare,
                cocoonProfile: null,
                rollContext: default,
                isRewardOperationPending: false);

            Assert.That(state.CanFreeReroll, Is.False);
            Assert.That(state.CanAdReroll, Is.True);
            Assert.That(state.CanTakeAll, Is.False);
        }

        [Test]
        public void Create_WhileOperationPending_DisablesAllActions()
        {
            RewardAttemptState attempts = new(new RewardFlowSettings(1, 1, 1));
            RewardPopupStateFactory factory = new(attempts);
            RewardRollContext context = new(
                headPathProgressNormalized: 1f,
                wormDestructionProgressNormalized: 0f,
                hasRevivedThisRun: false);

            RewardPopupState state = factory.Create(
                RewardRarity.Legendary,
                cocoonProfile: null,
                context,
                isRewardOperationPending: true);

            Assert.That(state.CanFreeReroll, Is.False);
            Assert.That(state.CanAdReroll, Is.False);
            Assert.That(state.CanTakeAll, Is.False);
        }
    }
}
