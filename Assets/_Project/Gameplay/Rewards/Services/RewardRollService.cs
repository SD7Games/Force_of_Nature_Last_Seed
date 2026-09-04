using System.Collections.Generic;
using UnityEngine;

public sealed class RewardRollService
{
    private const int MaxChoices = 3;

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
        var result = new List<RewardChoiceData>(MaxChoices);

        if (!TryGetRewardSource(context, true, out IReadOnlyList<RewardModifierEntry> source))
            return result;

        Dictionary<RewardRarity, List<RewardModifierEntry>> pools =
            RewardPoolBuilder.Build(source, context, rollContext);
        IReadOnlyList<RewardRaritySlot> slots = GetSlots(cocoonProfile);
        int count = Mathf.Min(
            MaxChoices,
            Mathf.Min(RewardPoolInspector.CountRewards(pools), slots.Count));
        int guaranteedSlotCount = GetGuaranteedSlotCount(
            guaranteedRarity,
            guaranteedRaritySlotCount,
            count);
        RewardRarity[] slotRarities = BuildSlotRarities(
            slots,
            count,
            guaranteedRarity,
            guaranteedSlotCount,
            pools);
        bool usePremiumRules = cocoonProfile != null
            && cocoonProfile.GuaranteesLegendaryReward;

        ApplyPremiumRarityRules(
            cocoonProfile,
            slotRarities,
            guaranteedSlotCount,
            pools,
            usePremiumRules);
        FillChoices(
            result,
            pools,
            slotRarities,
            usePremiumRules,
            context,
            rollContext);

        return result;
    }

    public RewardRarity RollGuaranteeRarity(
        RewardRuntimeContext context,
        CocoonRewardProfile cocoonProfile = null,
        RewardRollContext rollContext = default)
    {
        if (!TryGetRewardSource(context, false, out IReadOnlyList<RewardModifierEntry> source))
            return RewardRarity.Common;

        Dictionary<RewardRarity, List<RewardModifierEntry>> pools =
            RewardPoolBuilder.Build(source, context, rollContext);

        if (cocoonProfile != null && cocoonProfile.GuaranteesLegendaryReward)
        {
            return RewardPoolInspector.HasRewards(pools, RewardRarity.Legendary)
                ? RewardRarity.Legendary
                : RewardPoolInspector.GetHighestAvailableRarity(pools);
        }

        return RollAvailableRarity(GetSlots(cocoonProfile), pools);
    }

    private static void FillChoices(
        List<RewardChoiceData> result,
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        RewardRarity[] slotRarities,
        bool usePremiumRules,
        RewardRuntimeContext context,
        RewardRollContext rollContext)
    {
        RewardWeaponDpsBias weaponDpsBias =
            RewardWeaponDpsBiasCalculator.Calculate(context);
        var usedCategories = new HashSet<RewardModifierCategory>();
        var usedCategoryRarities = new HashSet<int>();
        bool useAssistDpsBias = RewardSelectionPolicy.ShouldUseAssistDpsBias(rollContext);

        for (int i = 0; i < slotRarities.Length; i++)
        {
            RewardRarity rarity = slotRarities[i];
            bool allowLegendaryFallback = usePremiumRules
                || rarity == RewardRarity.Legendary;

            if (!RewardChoiceSelector.TrySelectForSlot(
                    pools,
                    rarity,
                    usedCategories,
                    usedCategoryRarities,
                    useAssistDpsBias,
                    usePremiumRules,
                    allowLegendaryFallback,
                    rollContext,
                    weaponDpsBias,
                    out RewardModifierEntry selected))
            {
                break;
            }

            result.Add(new RewardChoiceData(selected));
            usedCategories.Add(selected.Category);
            usedCategoryRarities.Add(RewardSelectionPolicy.GetCategoryRarityKey(selected));
        }
    }

    private bool TryGetRewardSource(
        RewardRuntimeContext context,
        bool logWarnings,
        out IReadOnlyList<RewardModifierEntry> source)
    {
        source = null;

        if (_database == null)
        {
            if (logWarnings)
                Debug.LogWarning("Reward database is not set.");

            return false;
        }

        if (context == null)
        {
            if (logWarnings)
                Debug.LogWarning("Cannot roll rewards: runtime context is not initialized.");

            return false;
        }

        source = _database.Rewards;

        if (source != null && source.Count > 0)
            return true;

        if (logWarnings)
            Debug.LogWarning("Reward database is empty.");

        return false;
    }

    private IReadOnlyList<RewardRaritySlot> GetSlots(CocoonRewardProfile cocoonProfile)
    {
        IReadOnlyList<RewardRaritySlot> configuredSlots = cocoonProfile?.RaritySlots;
        return configuredSlots != null && configuredSlots.Count > 0
            ? configuredSlots
            : _defaultSlots;
    }

    private static int GetGuaranteedSlotCount(
        RewardRarity? guaranteedRarity,
        int requestedCount,
        int availableCount)
    {
        return guaranteedRarity.HasValue && availableCount > 0
            ? Mathf.Clamp(requestedCount, 1, availableCount)
            : 0;
    }

    private static RewardRarity[] BuildSlotRarities(
        IReadOnlyList<RewardRaritySlot> slots,
        int count,
        RewardRarity? guaranteedRarity,
        int guaranteedSlotCount,
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools)
    {
        return guaranteedRarity.HasValue
            ? RewardRarityRoller.BuildGuaranteedSlotRarities(
                slots,
                count,
                guaranteedRarity.Value,
                guaranteedSlotCount,
                pools)
            : RewardRarityRoller.BuildSlotRarities(slots, count);
    }

    private static void ApplyPremiumRarityRules(
        CocoonRewardProfile cocoonProfile,
        RewardRarity[] slotRarities,
        int guaranteedSlotCount,
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        bool usePremiumRules)
    {
        if (!usePremiumRules)
            return;

        RewardRarityRoller.ApplySecondaryLegendaryRolls(
            slotRarities,
            guaranteedSlotCount,
            cocoonProfile.SecondaryLegendaryChance,
            pools);
    }

    private static RewardRarity RollAvailableRarity(
        IReadOnlyList<RewardRaritySlot> slots,
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools)
    {
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
            : RewardPoolInspector.GetHighestAvailableRarity(pools);
    }
}
