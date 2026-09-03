using System;
using UnityEngine;

public static class WormRewardEvents
{
    public static event Action<CocoonRewardProfile, float, float> RewardRequested;

    public static void PublishRewardRequested(
        CocoonRewardProfile profile,
        float headPathProgressNormalized,
        float wormDestructionProgressNormalized)
    {
        RewardRequested?.Invoke(
            profile,
            headPathProgressNormalized,
            wormDestructionProgressNormalized);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        RewardRequested = null;
    }
}
