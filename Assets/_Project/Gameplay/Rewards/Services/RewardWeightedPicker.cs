using System.Collections.Generic;
using UnityEngine;

public static class RewardWeightedPicker
{
    public static bool TryTakeFromRarity(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        RewardRarity rarity,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        RewardPickMode mode,
        out RewardModifierEntry selected,
        RewardRollContext rollContext,
        RewardWeaponDpsBias weaponDpsBias = default,
        bool requireAssistDpsReward = false)
    {
        selected = null;

        if (pools == null || !pools.TryGetValue(rarity, out List<RewardModifierEntry> pool))
            return false;

        return TryTakeFromPool(
            pool,
            usedCategories,
            usedCategoryRarities,
            mode,
            out selected,
            rollContext,
            weaponDpsBias,
            requireAssistDpsReward);
    }

    public static bool TryTakeFromAllRarities(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        RewardPickMode mode,
        out RewardModifierEntry selected,
        bool allowLegendary,
        RewardRollContext rollContext,
        RewardWeaponDpsBias weaponDpsBias = default)
    {
        selected = null;

        if (pools == null || pools.Count == 0)
            return false;

        float totalWeight = GetTotalWeight(
            pools,
            usedCategories,
            usedCategoryRarities,
            mode,
            allowLegendary,
            rollContext,
            weaponDpsBias);

        if (totalWeight <= 0f)
            return false;

        float roll = Random.value * totalWeight;
        float currentWeight = 0f;

        foreach (KeyValuePair<RewardRarity, List<RewardModifierEntry>> rarityPool in pools)
        {
            if (!allowLegendary && rarityPool.Key == RewardRarity.Legendary)
                continue;

            List<RewardModifierEntry> pool = rarityPool.Value;

            if (pool == null)
                continue;

            for (int i = 0; i < pool.Count; i++)
            {
                RewardModifierEntry entry = pool[i];

                if (!RewardSelectionPolicy.IsEligible(
                        entry,
                        usedCategories,
                        usedCategoryRarities,
                        mode,
                        RewardWeaponGroup.None))
                {
                    continue;
                }

                currentWeight += RewardSelectionPolicy.GetEffectiveWeight(
                    entry,
                    rollContext,
                    weaponDpsBias);

                if (roll > currentWeight)
                    continue;

                selected = entry;
                pool.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    private static bool TryTakeFromPool(
        List<RewardModifierEntry> pool,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        RewardPickMode mode,
        out RewardModifierEntry selected,
        RewardRollContext rollContext,
        RewardWeaponDpsBias weaponDpsBias,
        bool requireAssistDpsReward)
    {
        selected = null;

        if (pool == null || pool.Count == 0)
            return false;

        bool preferAssistDpsRewards = !requireAssistDpsReward
            && RewardSelectionPolicy.ShouldPreferAssistDpsRewards(
                pool,
                usedCategories,
                usedCategoryRarities,
                mode,
                RewardWeaponGroup.None,
                rollContext);
        bool requireDpsReward = requireAssistDpsReward || preferAssistDpsRewards;
        float totalWeight = 0f;

        for (int i = 0; i < pool.Count; i++)
        {
            RewardModifierEntry entry = pool[i];

            if (RewardSelectionPolicy.IsEligible(
                    entry,
                    usedCategories,
                    usedCategoryRarities,
                    mode,
                    RewardWeaponGroup.None,
                    requireDpsReward))
            {
                totalWeight += RewardSelectionPolicy.GetEffectiveWeight(
                    entry,
                    rollContext,
                    weaponDpsBias);
            }
        }

        if (totalWeight <= 0f)
            return false;

        float roll = Random.value * totalWeight;
        float currentWeight = 0f;

        for (int i = 0; i < pool.Count; i++)
        {
            RewardModifierEntry entry = pool[i];

            if (!RewardSelectionPolicy.IsEligible(
                    entry,
                    usedCategories,
                    usedCategoryRarities,
                    mode,
                    RewardWeaponGroup.None,
                    requireDpsReward))
            {
                continue;
            }

            currentWeight += RewardSelectionPolicy.GetEffectiveWeight(
                entry,
                rollContext,
                weaponDpsBias);

            if (roll > currentWeight)
                continue;

            selected = entry;
            pool.RemoveAt(i);
            return true;
        }

        return false;
    }

    private static float GetTotalWeight(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        RewardPickMode mode,
        bool allowLegendary,
        RewardRollContext rollContext,
        RewardWeaponDpsBias weaponDpsBias)
    {
        float totalWeight = 0f;

        foreach (KeyValuePair<RewardRarity, List<RewardModifierEntry>> rarityPool in pools)
        {
            if (!allowLegendary && rarityPool.Key == RewardRarity.Legendary)
                continue;

            List<RewardModifierEntry> pool = rarityPool.Value;

            if (pool == null)
                continue;

            for (int i = 0; i < pool.Count; i++)
            {
                RewardModifierEntry entry = pool[i];

                if (RewardSelectionPolicy.IsEligible(
                        entry,
                        usedCategories,
                        usedCategoryRarities,
                        mode,
                        RewardWeaponGroup.None))
                {
                    totalWeight += RewardSelectionPolicy.GetEffectiveWeight(
                        entry,
                        rollContext,
                        weaponDpsBias);
                }
            }
        }

        return totalWeight;
    }
}
