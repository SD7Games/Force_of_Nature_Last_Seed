using System.Collections.Generic;
using UnityEngine;

public enum RewardPickMode
{
    UniqueCategory,
    UniqueCategoryRarity,
    Any
}

public static class RewardSelectionPolicy
{
    private const float PostRevivePrimaryDpsWeightMultiplier = 4f;
    private const float PostReviveSecondaryDpsWeightMultiplier = 1.25f;
    private const float PaidAssistPrimaryDpsWeightMultiplier = 2.75f;
    private const float PaidAssistSecondaryDpsWeightMultiplier = 1.15f;

    public static float GetEffectiveWeight(
        RewardModifierEntry entry,
        RewardRollContext rollContext,
        RewardWeaponDpsBias weaponDpsBias = default)
    {
        if (entry == null || entry.Weight <= 0f)
            return 0f;

        float multiplier = weaponDpsBias.GetMultiplier(GetWeaponGroup(entry));
        multiplier *= GetAssistDpsWeightMultiplier(entry, rollContext);
        return Mathf.Max(0.01f, entry.Weight * multiplier);
    }

    public static bool IsEligible(
        RewardModifierEntry entry,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        RewardPickMode mode,
        RewardWeaponGroup requiredWeaponGroup,
        bool requireAssistDpsReward = false)
    {
        if (entry == null || entry.Weight <= 0f)
            return false;

        if (requiredWeaponGroup != RewardWeaponGroup.None &&
            GetWeaponGroup(entry) != requiredWeaponGroup)
        {
            return false;
        }

        if (requireAssistDpsReward && !IsAssistPrimaryDpsReward(entry))
            return false;

        return mode switch
        {
            RewardPickMode.UniqueCategory => !usedCategories.Contains(entry.Category),
            RewardPickMode.UniqueCategoryRarity =>
                !usedCategoryRarities.Contains(GetCategoryRarityKey(entry)),
            _ => true
        };
    }

    public static bool ShouldPreferAssistDpsRewards(
        List<RewardModifierEntry> pool,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        RewardPickMode mode,
        RewardWeaponGroup requiredWeaponGroup,
        RewardRollContext rollContext)
    {
        if (!ShouldUseAssistDpsBias(rollContext))
            return false;

        for (int i = 0; i < pool.Count; i++)
        {
            RewardModifierEntry entry = pool[i];

            if (IsAssistPrimaryDpsReward(entry) &&
                IsEligible(
                    entry,
                    usedCategories,
                    usedCategoryRarities,
                    mode,
                    requiredWeaponGroup))
            {
                return true;
            }
        }

        return false;
    }

    public static int GetCategoryRarityKey(RewardModifierEntry entry)
    {
        return ((int)entry.Category * 10) + (int)entry.Rarity;
    }

    public static bool IsNewWeaponUnlockReward(RewardModifierEntry entry)
    {
        return entry != null &&
            entry.Category == RewardModifierCategory.AcaciaThornUnlock;
    }

    public static bool ShouldUseAssistDpsBias(RewardRollContext rollContext)
    {
        return rollContext.HasRevivedThisRun || rollContext.IsPaidAssistRoll;
    }

    public static RewardWeaponGroup GetWeaponGroup(RewardModifierEntry entry)
    {
        if (entry == null)
            return RewardWeaponGroup.None;

        return entry.Category switch
        {
            RewardModifierCategory.Damage
                or RewardModifierCategory.FireRate
                or RewardModifierCategory.CriticalChance
                or RewardModifierCategory.CriticalPower
                or RewardModifierCategory.Penetration
                or RewardModifierCategory.ParallelProjectiles
                or RewardModifierCategory.Salvo
                or RewardModifierCategory.ProjectileSpeed => RewardWeaponGroup.MainWeapon,

            RewardModifierCategory.AcaciaThornDamage
                or RewardModifierCategory.AcaciaThornFireRate
                or RewardModifierCategory.AcaciaThornSalvo
                or RewardModifierCategory.AcaciaThornProjectileSpeed
                or RewardModifierCategory.AcaciaThornCriticalChance
                or RewardModifierCategory.AcaciaThornCriticalPower => RewardWeaponGroup.AcaciaThorn,

            _ => RewardWeaponGroup.None
        };
    }

    private static float GetAssistDpsWeightMultiplier(
        RewardModifierEntry entry,
        RewardRollContext rollContext)
    {
        if (rollContext.HasRevivedThisRun)
        {
            return GetDpsWeightMultiplier(
                entry,
                PostRevivePrimaryDpsWeightMultiplier,
                PostReviveSecondaryDpsWeightMultiplier);
        }

        if (rollContext.IsPaidAssistRoll)
        {
            return GetDpsWeightMultiplier(
                entry,
                PaidAssistPrimaryDpsWeightMultiplier,
                PaidAssistSecondaryDpsWeightMultiplier);
        }

        return 1f;
    }

    private static float GetDpsWeightMultiplier(
        RewardModifierEntry entry,
        float primaryMultiplier,
        float secondaryMultiplier)
    {
        if (IsAssistPrimaryDpsReward(entry))
            return primaryMultiplier;

        return IsAssistSecondaryDpsReward(entry)
            ? secondaryMultiplier
            : 1f;
    }

    private static bool IsAssistPrimaryDpsReward(RewardModifierEntry entry)
    {
        if (entry == null)
            return false;

        return entry.Category switch
        {
            RewardModifierCategory.Damage
                or RewardModifierCategory.FireRate
                or RewardModifierCategory.CriticalChance
                or RewardModifierCategory.CriticalPower
                or RewardModifierCategory.Penetration
                or RewardModifierCategory.ParallelProjectiles
                or RewardModifierCategory.Salvo
                or RewardModifierCategory.AcaciaThornUnlock
                or RewardModifierCategory.AcaciaThornDamage
                or RewardModifierCategory.AcaciaThornFireRate
                or RewardModifierCategory.AcaciaThornSalvo
                or RewardModifierCategory.AcaciaThornCriticalChance
                or RewardModifierCategory.AcaciaThornCriticalPower => true,
            _ => false
        };
    }

    private static bool IsAssistSecondaryDpsReward(RewardModifierEntry entry)
    {
        return entry != null &&
            entry.Category is RewardModifierCategory.ProjectileSpeed
                or RewardModifierCategory.AcaciaThornProjectileSpeed;
    }
}
