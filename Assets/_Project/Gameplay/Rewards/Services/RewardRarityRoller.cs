using System.Collections.Generic;
using UnityEngine;

public static class RewardRarityRoller
{
    public static RewardRarity[] BuildSlotRarities(
        IReadOnlyList<RewardRaritySlot> slots,
        int count,
        IRandomSource randomSource)
    {
        var result = new RewardRarity[count];

        for (int i = 0; i < count; i++)
        {
            result[i] = slots[i] != null
                ? slots[i].RollRarity(randomSource)
                : RewardRarity.Common;
        }

        return result;
    }

    public static RewardRarity[] BuildGuaranteedSlotRarities(
        IReadOnlyList<RewardRaritySlot> slots,
        int count,
        RewardRarity guaranteedRarity,
        int guaranteedSlotCount,
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        IRandomSource randomSource)
    {
        var result = new RewardRarity[count];

        if (count == 0)
            return result;

        guaranteedSlotCount = Mathf.Clamp(guaranteedSlotCount, 1, count);

        for (int i = 0; i < guaranteedSlotCount; i++)
            result[i] = guaranteedRarity;

        float commonWeight = 0f;
        float rareWeight = 0f;
        float legendaryWeight = 0f;

        CollectAvailableRarityWeights(
            slots,
            guaranteedSlotCount,
            pools,
            ref commonWeight,
            ref rareWeight,
            ref legendaryWeight);

        for (int i = guaranteedSlotCount; i < count; i++)
        {
            result[i] = RollFromWeights(
                commonWeight,
                rareWeight,
                legendaryWeight,
                randomSource);
        }

        return result;
    }

    public static void ApplySecondaryLegendaryRolls(
        RewardRarity[] slotRarities,
        int guaranteedSlotCount,
        float secondaryLegendaryChance,
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        IRandomSource randomSource)
    {
        if (slotRarities == null ||
            slotRarities.Length == 0 ||
            secondaryLegendaryChance <= 0f ||
            !RewardPoolInspector.HasRewards(pools, RewardRarity.Legendary))
        {
            return;
        }

        int startIndex = Mathf.Clamp(guaranteedSlotCount, 0, slotRarities.Length);

        for (int i = startIndex; i < slotRarities.Length; i++)
        {
            if (slotRarities[i] != RewardRarity.Legendary &&
                randomSource.NextUnitFloat() < secondaryLegendaryChance)
            {
                slotRarities[i] = RewardRarity.Legendary;
            }
        }
    }

    public static void AddAvailableWeight(
        RewardRarity rarity,
        float weight,
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        ref float commonWeight,
        ref float rareWeight,
        ref float legendaryWeight)
    {
        if (weight <= 0f || !RewardPoolInspector.HasRewards(pools, rarity))
            return;

        switch (rarity)
        {
            case RewardRarity.Rare:
                rareWeight += weight;
                break;
            case RewardRarity.Legendary:
                legendaryWeight += weight;
                break;
            default:
                commonWeight += weight;
                break;
        }
    }

    public static RewardRarity RollFromWeights(
        float commonWeight,
        float rareWeight,
        float legendaryWeight,
        IRandomSource randomSource)
    {
        float totalWeight = commonWeight + rareWeight + legendaryWeight;

        if (totalWeight <= 0f)
            return RewardRarity.Common;

        float roll = randomSource.NextUnitFloat() * totalWeight;

        if (roll < commonWeight)
            return RewardRarity.Common;

        roll -= commonWeight;
        return roll < rareWeight
            ? RewardRarity.Rare
            : RewardRarity.Legendary;
    }

    private static void CollectAvailableRarityWeights(
        IReadOnlyList<RewardRaritySlot> slots,
        int startIndex,
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        ref float commonWeight,
        ref float rareWeight,
        ref float legendaryWeight)
    {
        if (slots == null)
            return;

        startIndex = Mathf.Clamp(startIndex, 0, slots.Count);

        for (int i = startIndex; i < slots.Count; i++)
        {
            RewardRaritySlot slot = slots[i];

            if (slot == null)
                continue;

            AddAvailableWeight(
                slot.Rarity,
                1f - slot.AlternateChance,
                pools,
                ref commonWeight,
                ref rareWeight,
                ref legendaryWeight);
            AddAvailableWeight(
                slot.AlternateRarity,
                slot.AlternateChance,
                pools,
                ref commonWeight,
                ref rareWeight,
                ref legendaryWeight);
        }
    }

}
