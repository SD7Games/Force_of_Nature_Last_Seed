using System.Collections.Generic;
using UnityEngine;

public sealed class RewardRollService
{
    private const int MAX_CHOICES = 3;
    private const float NewWeaponUnlockMinWormProgress = 0.3f;

    private readonly RewardDatabase _database;
    private readonly List<RewardRaritySlot> _defaultSlots = new()
    {
        new RewardRaritySlot(RewardRarity.Common),
        new RewardRaritySlot(RewardRarity.Common),
        new RewardRaritySlot(RewardRarity.Common)
    };

    public RewardRollService(RewardDatabase database)
    {
        _database = database;
    }

    public List<RewardChoiceData> Roll3(
        RewardRuntimeContext context,
        CocoonRewardProfile cocoonProfile = null,
        RewardRarity? guaranteedRarity = null,
        int guaranteedRaritySlotCount = 1,
        RewardRollContext rollContext = default)
    {
        var result = new List<RewardChoiceData>(MAX_CHOICES);

        if (_database == null)
        {
            Debug.LogWarning("Reward database is not set.");
            return result;
        }

        if (context == null)
        {
            Debug.LogWarning("Cannot roll rewards: runtime context is not initialized.");
            return result;
        }

        var source = _database.Rewards;

        if (source == null || source.Count == 0)
        {
            Debug.LogWarning("Reward database is empty.");
            return result;
        }

        var pools = BuildPools(source, context, rollContext);
        IReadOnlyList<RewardRaritySlot> slots = GetSlots(cocoonProfile);
        int count = Mathf.Min(MAX_CHOICES, Mathf.Min(CountRewards(pools), slots.Count));
        int guaranteedSlotCount = guaranteedRarity.HasValue && count > 0
            ? Mathf.Clamp(guaranteedRaritySlotCount, 1, count)
            : 0;
        RewardRarity[] slotRarities = guaranteedRarity.HasValue
            ? RewardRarityRoller.BuildGuaranteedSlotRarities(
                slots,
                count,
                guaranteedRarity.Value,
                guaranteedSlotCount,
                pools)
            : RewardRarityRoller.BuildSlotRarities(slots, count);
        bool useLegendaryProfileRules = cocoonProfile != null
            && cocoonProfile.GuaranteesLegendaryReward;

        if (useLegendaryProfileRules)
        {
            RewardRarityRoller.ApplySecondaryLegendaryRolls(
                slotRarities,
                guaranteedSlotCount,
                cocoonProfile.SecondaryLegendaryChance,
                pools);
        }

        RewardWeaponDpsBias weaponDpsBias =
            RewardWeaponDpsBiasCalculator.Calculate(context);
        var usedCategories = new HashSet<RewardModifierCategory>();
        var usedCategoryRarities = new HashSet<int>();

        for (int i = 0; i < count; i++)
        {
            RewardRarity rarity = slotRarities[i];
            bool allowLegendaryFallback = useLegendaryProfileRules
                || rarity == RewardRarity.Legendary;

            RewardModifierEntry selected = null;
            bool isSelected = false;

            if (RewardSelectionPolicy.ShouldUseAssistDpsBias(rollContext))
            {
                isSelected = TryRollAssistPrimaryDpsReward(
                    pools,
                    rarity,
                    usedCategories,
                    usedCategoryRarities,
                    out selected,
                    rollContext,
                    weaponDpsBias);
            }

            if (!isSelected)
            {
                isSelected = TryRollRewardForRarity(
                    pools,
                    rarity,
                    usedCategories,
                    usedCategoryRarities,
                    out selected,
                    rollContext,
                    weaponDpsBias);
            }

            if (!isSelected && useLegendaryProfileRules)
            {
                isSelected = TryRollPremiumReward(
                    pools,
                    rarity,
                    usedCategories,
                    usedCategoryRarities,
                    out selected,
                    rollContext,
                    weaponDpsBias);
            }

            if (!isSelected
                && !TryRollReward(
                    pools,
                    usedCategories,
                    usedCategoryRarities,
                    out selected,
                    allowLegendaryFallback,
                    rollContext,
                    weaponDpsBias))
            {
                break;
            }

            result.Add(new RewardChoiceData(selected));
            usedCategories.Add(selected.Category);
            usedCategoryRarities.Add(RewardSelectionPolicy.GetCategoryRarityKey(selected));
        }

        return result;
    }

    public RewardRarity RollGuaranteeRarity(
        RewardRuntimeContext context,
        CocoonRewardProfile cocoonProfile = null,
        RewardRollContext rollContext = default)
    {
        if (_database == null || context == null)
            return RewardRarity.Common;

        var source = _database.Rewards;

        if (source == null || source.Count == 0)
            return RewardRarity.Common;

        var pools = BuildPools(source, context, rollContext);

        if (cocoonProfile != null && cocoonProfile.GuaranteesLegendaryReward)
            return HasRewardsForRarity(pools, RewardRarity.Legendary)
                ? RewardRarity.Legendary
                : GetHighestAvailableRarity(pools);

        IReadOnlyList<RewardRaritySlot> slots = GetSlots(cocoonProfile);

        float commonWeight = 0f;
        float rareWeight = 0f;
        float legendaryWeight = 0f;

        for (int i = 0; i < slots.Count; i++)
        {
            RewardRaritySlot slot = slots[i];

            if (slot == null)
                continue;

            RewardRarityRoller.AddAvailableWeight(
                slot.Rarity,
                1f - slot.AlternateChance,
                pools,
                ref commonWeight,
                ref rareWeight,
                ref legendaryWeight);

            RewardRarityRoller.AddAvailableWeight(
                slot.AlternateRarity,
                slot.AlternateChance,
                pools,
                ref commonWeight,
                ref rareWeight,
                ref legendaryWeight);
        }

        float totalWeight = commonWeight + rareWeight + legendaryWeight;

        return totalWeight > 0f
            ? RewardRarityRoller.RollFromWeights(commonWeight, rareWeight, legendaryWeight)
            : GetHighestAvailableRarity(pools);
    }

    private bool TryRollRewardForRarity(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        RewardRarity rarity,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        out RewardModifierEntry selected,
        RewardRollContext rollContext,
        RewardWeaponDpsBias weaponDpsBias = default)
    {
        selected = null;

        if (TryRollRewardForRarity(
                pools,
                rarity,
                usedCategories,
                usedCategoryRarities,
                RewardPickMode.UniqueCategory,
                RewardWeaponGroup.None,
                out selected,
                rollContext,
                weaponDpsBias))
        {
            return true;
        }

        if (TryRollRewardForRarity(
                pools,
                rarity,
                usedCategories,
                usedCategoryRarities,
                RewardPickMode.UniqueCategoryRarity,
                RewardWeaponGroup.None,
                out selected,
                rollContext,
                weaponDpsBias))
        {
            return true;
        }

        return TryRollRewardForRarity(
            pools,
            rarity,
            usedCategories,
            usedCategoryRarities,
            RewardPickMode.Any,
            RewardWeaponGroup.None,
            out selected,
            rollContext,
            weaponDpsBias);
    }

    private bool TryRollAssistPrimaryDpsReward(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        RewardRarity preferredRarity,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        out RewardModifierEntry selected,
        RewardRollContext rollContext,
        RewardWeaponDpsBias weaponDpsBias = default,
        bool requireAssistDpsReward = false)
    {
        selected = null;

        if (!TryRollPrimaryDpsRewardForRarity(
                pools,
                preferredRarity,
                usedCategories,
                usedCategoryRarities,
                out selected,
                rollContext,
                weaponDpsBias))
        {
            return preferredRarity switch
            {
                RewardRarity.Legendary => TryRollPrimaryDpsRewardForRarity(
                        pools,
                        RewardRarity.Rare,
                        usedCategories,
                        usedCategoryRarities,
                        out selected,
                        rollContext,
                        weaponDpsBias)
                    || TryRollPrimaryDpsRewardForRarity(
                        pools,
                        RewardRarity.Common,
                        usedCategories,
                        usedCategoryRarities,
                        out selected,
                        rollContext,
                        weaponDpsBias),

                RewardRarity.Rare => TryRollPrimaryDpsRewardForRarity(
                    pools,
                    RewardRarity.Common,
                    usedCategories,
                    usedCategoryRarities,
                    out selected,
                    rollContext,
                    weaponDpsBias),

                _ => false
            };
        }

        return true;
    }

    private static bool TryRollPrimaryDpsRewardForRarity(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        RewardRarity rarity,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        out RewardModifierEntry selected,
        RewardRollContext rollContext,
        RewardWeaponDpsBias weaponDpsBias = default)
    {
        selected = null;

        if (TryRollRewardForRarity(
                pools,
                rarity,
                usedCategories,
                usedCategoryRarities,
                RewardPickMode.UniqueCategory,
                RewardWeaponGroup.None,
                out selected,
                rollContext,
                weaponDpsBias,
                requireAssistDpsReward: true))
        {
            return true;
        }

        if (TryRollRewardForRarity(
                pools,
                rarity,
                usedCategories,
                usedCategoryRarities,
                RewardPickMode.UniqueCategoryRarity,
                RewardWeaponGroup.None,
                out selected,
                rollContext,
                weaponDpsBias,
                requireAssistDpsReward: true))
        {
            return true;
        }

        return TryRollRewardForRarity(
            pools,
            rarity,
            usedCategories,
            usedCategoryRarities,
            RewardPickMode.Any,
            RewardWeaponGroup.None,
            out selected,
            rollContext,
            weaponDpsBias,
            requireAssistDpsReward: true);
    }

    private bool TryRollPremiumReward(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        RewardRarity preferredRarity,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        out RewardModifierEntry selected,
        RewardRollContext rollContext,
        RewardWeaponDpsBias weaponDpsBias = default)
    {
        selected = null;

        if (preferredRarity == RewardRarity.Legendary)
        {
            return TryRollRewardForRarity(
                    pools,
                    RewardRarity.Legendary,
                    usedCategories,
                    usedCategoryRarities,
                    out selected,
                    rollContext,
                    weaponDpsBias)
                || TryRollRewardForRarity(
                    pools,
                    RewardRarity.Rare,
                    usedCategories,
                    usedCategoryRarities,
                    out selected,
                    rollContext,
                    weaponDpsBias);
        }

        return TryRollRewardForRarity(
                pools,
                RewardRarity.Rare,
                usedCategories,
                usedCategoryRarities,
                out selected,
                rollContext,
                weaponDpsBias)
            || TryRollRewardForRarity(
                pools,
                RewardRarity.Legendary,
                usedCategories,
                usedCategoryRarities,
                out selected,
                rollContext,
                weaponDpsBias);
    }

    private static bool TryRollRewardForRarity(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        RewardRarity rarity,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        RewardPickMode mode,
        RewardWeaponGroup requiredWeaponGroup,
        out RewardModifierEntry selected,
        RewardRollContext rollContext,
        RewardWeaponDpsBias weaponDpsBias = default,
        bool requireAssistDpsReward = false)
    {
        selected = null;

        if (!pools.TryGetValue(rarity, out var pool))
            return false;

        return TryTakeReward(
            pool,
            usedCategories,
            usedCategoryRarities,
            mode,
            requiredWeaponGroup,
            out selected,
            rollContext,
            weaponDpsBias,
            requireAssistDpsReward);
    }

    private Dictionary<RewardRarity, List<RewardModifierEntry>> BuildPools(
        IReadOnlyList<RewardModifierEntry> source,
        RewardRuntimeContext context,
        RewardRollContext rollContext)
    {
        var pools = new Dictionary<RewardRarity, List<RewardModifierEntry>>();

        foreach (RewardModifierEntry entry in source)
        {
            if (entry == null || entry.Effect == null)
                continue;

            if (!entry.Effect.CanApply(context))
                continue;

            if (RewardSelectionPolicy.IsNewWeaponUnlockReward(entry)
                && rollContext.WormDestructionProgressNormalized < NewWeaponUnlockMinWormProgress)
            {
                continue;
            }

            if (!pools.TryGetValue(entry.Rarity, out var pool))
            {
                pool = new List<RewardModifierEntry>();
                pools.Add(entry.Rarity, pool);
            }

            pool.Add(entry);
        }

        return pools;
    }

    private static bool TryRollReward(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        out RewardModifierEntry selected,
        bool allowLegendary = true,
        RewardRollContext rollContext = default,
        RewardWeaponDpsBias weaponDpsBias = default)
    {
        selected = null;

        if (TryRollReward(
                pools,
                usedCategories,
                usedCategoryRarities,
                RewardPickMode.UniqueCategory,
                RewardWeaponGroup.None,
                out selected,
                allowLegendary,
                rollContext,
                weaponDpsBias))
        {
            return true;
        }

        if (TryRollReward(
                pools,
                usedCategories,
                usedCategoryRarities,
                RewardPickMode.UniqueCategoryRarity,
                RewardWeaponGroup.None,
                out selected,
                allowLegendary,
                rollContext,
                weaponDpsBias))
        {
            return true;
        }

        return TryRollReward(
            pools,
            usedCategories,
            usedCategoryRarities,
            RewardPickMode.Any,
            RewardWeaponGroup.None,
            out selected,
            allowLegendary,
            rollContext,
            weaponDpsBias);
    }

    private static bool TryRollReward(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        RewardWeaponGroup requiredWeaponGroup,
        out RewardModifierEntry selected,
        bool allowLegendary = true,
        RewardRollContext rollContext = default,
        RewardWeaponDpsBias weaponDpsBias = default)
    {
        selected = null;

        if (TryRollReward(
                pools,
                usedCategories,
                usedCategoryRarities,
                RewardPickMode.UniqueCategory,
                requiredWeaponGroup,
                out selected,
                allowLegendary,
                rollContext,
                weaponDpsBias))
        {
            return true;
        }

        if (TryRollReward(
                pools,
                usedCategories,
                usedCategoryRarities,
                RewardPickMode.UniqueCategoryRarity,
                requiredWeaponGroup,
                out selected,
                allowLegendary,
                rollContext,
                weaponDpsBias))
        {
            return true;
        }

        return TryRollReward(
            pools,
            usedCategories,
            usedCategoryRarities,
            RewardPickMode.Any,
            requiredWeaponGroup,
            out selected,
            allowLegendary,
            rollContext,
            weaponDpsBias);
    }

    private static bool TryRollReward(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        RewardPickMode mode,
        RewardWeaponGroup requiredWeaponGroup,
        out RewardModifierEntry selected,
        bool allowLegendary = true,
        RewardRollContext rollContext = default,
        RewardWeaponDpsBias weaponDpsBias = default)
    {
        selected = null;

        if (pools == null || pools.Count == 0)
            return false;

        float totalWeight = 0f;

        foreach (var rarityPool in pools)
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
                        requiredWeaponGroup))
                {
                    totalWeight += RewardSelectionPolicy.GetEffectiveWeight(
                        entry,
                        rollContext,
                        weaponDpsBias);
                }
            }
        }

        if (totalWeight <= 0f)
            return false;

        float roll = Random.value * totalWeight;
        float currentWeight = 0f;

        foreach (var rarityPool in pools)
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
                        requiredWeaponGroup))
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

    private IReadOnlyList<RewardRaritySlot> GetSlots(
        CocoonRewardProfile cocoonProfile)
    {
        if (HasRaritySlots(cocoonProfile?.RaritySlots))
            return cocoonProfile.RaritySlots;

        return _defaultSlots;
    }

    private static bool HasRewardsForRarity(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        RewardRarity rarity)
    {
        return pools != null
            && pools.TryGetValue(rarity, out var pool)
            && pool != null
            && pool.Count > 0;
    }

    private static RewardRarity GetHighestAvailableRarity(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools)
    {
        if (HasRewardsForRarity(pools, RewardRarity.Legendary))
            return RewardRarity.Legendary;

        if (HasRewardsForRarity(pools, RewardRarity.Rare))
            return RewardRarity.Rare;

        return RewardRarity.Common;
    }

    private static bool HasRaritySlots(IReadOnlyList<RewardRaritySlot> slots)
    {
        return slots != null && slots.Count > 0;
    }

    private static bool TryTakeReward(
        List<RewardModifierEntry> pool,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        RewardPickMode mode,
        RewardWeaponGroup requiredWeaponGroup,
        out RewardModifierEntry selected,
        RewardRollContext rollContext,
        RewardWeaponDpsBias weaponDpsBias = default,
        bool requireAssistDpsReward = false)
    {
        selected = null;

        if (pool == null || pool.Count == 0)
            return false;

        bool preferAssistDpsRewards = !requireAssistDpsReward &&
            RewardSelectionPolicy.ShouldPreferAssistDpsRewards(
            pool,
            usedCategories,
            usedCategoryRarities,
            mode,
            requiredWeaponGroup,
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
                    requiredWeaponGroup,
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
                    requiredWeaponGroup,
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

    private static int CountRewards(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools)
    {
        int count = 0;

        foreach (var pool in pools.Values)
        {
            count += pool.Count;
        }

        return count;
    }

}
