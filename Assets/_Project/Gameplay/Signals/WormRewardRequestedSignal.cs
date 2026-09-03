namespace LastSeed.Gameplay.Signals
{
    public sealed class WormRewardRequestedSignal
    {
        public WormRewardRequestedSignal(
            CocoonRewardProfile rewardProfile,
            float headPathProgressNormalized,
            float wormDestructionProgressNormalized)
        {
            RewardProfile = rewardProfile;
            HeadPathProgressNormalized = headPathProgressNormalized;
            WormDestructionProgressNormalized = wormDestructionProgressNormalized;
        }

        public CocoonRewardProfile RewardProfile { get; }
        public float HeadPathProgressNormalized { get; }
        public float WormDestructionProgressNormalized { get; }
    }
}
