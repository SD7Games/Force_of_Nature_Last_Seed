using UnityEngine;

public static class RewardAdRerollPolicy
{
    public const float LegendaryChanceMinDestructionProgress = 0.65f;
    public const float TakeAllMinHeadPathProgress = 0.7f;

    private const float LockedAdditionalWeaponLegendaryChance = 0.1f;
    private const float UnlockedAdditionalWeaponLegendaryChance = 0.18f;

    public static RewardRarity GetDisplayedGuaranteeRarity(CocoonRewardProfile cocoonProfile)
    {
        return IsLegendaryCocoon(cocoonProfile)
            ? RewardRarity.Legendary
            : RewardRarity.Rare;
    }

    public static RewardRarity RollGuaranteedRarity(
        RewardRuntimeContext context,
        CocoonRewardProfile cocoonProfile,
        RewardRollContext rollContext)
    {
        if (IsLegendaryCocoon(cocoonProfile))
            return RewardRarity.Legendary;

        return Random.value < GetLegendaryChance(context, rollContext)
            ? RewardRarity.Legendary
            : RewardRarity.Rare;
    }

    public static float GetLegendaryChance(
        RewardRuntimeContext context,
        RewardRollContext rollContext)
    {
        if (rollContext.WormDestructionProgressNormalized < LegendaryChanceMinDestructionProgress)
            return 0f;

        return HasAdditionalWeaponUnlocked(context)
            ? UnlockedAdditionalWeaponLegendaryChance
            : LockedAdditionalWeaponLegendaryChance;
    }

    public static bool CanOfferTakeAll(RewardRollContext rollContext)
    {
        return CanOfferTakeAll(rollContext, TakeAllMinHeadPathProgress);
    }

    public static bool CanOfferTakeAll(
        RewardRollContext rollContext,
        float minHeadPathProgress)
    {
        return rollContext.HeadPathProgressNormalized >= Mathf.Clamp01(minHeadPathProgress);
    }

    private static bool IsLegendaryCocoon(CocoonRewardProfile cocoonProfile)
    {
        return cocoonProfile != null && cocoonProfile.GuaranteesLegendaryReward;
    }

    private static bool HasAdditionalWeaponUnlocked(RewardRuntimeContext context)
    {
        AcaciaThornRuntimeState acaciaState = context?.AcaciaThornState;
        return acaciaState != null && acaciaState.IsUnlocked;
    }
}
