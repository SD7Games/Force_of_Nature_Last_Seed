using System.Collections.Generic;

public static class RewardChoiceSelector
{
    public static bool TrySelectForSlot(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        RewardRarity preferredRarity,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        bool useAssistDpsBias,
        bool usePremiumFallback,
        bool allowLegendaryFallback,
        RewardRollContext rollContext,
        RewardWeaponDpsBias weaponDpsBias,
        out RewardModifierEntry selected)
    {
        selected = null;

        if (useAssistDpsBias && TrySelectAssistPrimaryDps(
                pools,
                preferredRarity,
                usedCategories,
                usedCategoryRarities,
                rollContext,
                weaponDpsBias,
                out selected))
        {
            return true;
        }

        if (TrySelectAtRarity(
                pools,
                preferredRarity,
                usedCategories,
                usedCategoryRarities,
                rollContext,
                weaponDpsBias,
                out selected))
        {
            return true;
        }

        if (usePremiumFallback && TrySelectPremiumFallback(
                pools,
                preferredRarity,
                usedCategories,
                usedCategoryRarities,
                rollContext,
                weaponDpsBias,
                out selected))
        {
            return true;
        }

        return TrySelectFromAllRarities(
            pools,
            usedCategories,
            usedCategoryRarities,
            allowLegendaryFallback,
            rollContext,
            weaponDpsBias,
            out selected);
    }

    private static bool TrySelectAtRarity(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        RewardRarity rarity,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        RewardRollContext rollContext,
        RewardWeaponDpsBias weaponDpsBias,
        out RewardModifierEntry selected,
        bool requireAssistDpsReward = false)
    {
        return RewardWeightedPicker.TryTakeFromRarity(
                   pools, rarity, usedCategories, usedCategoryRarities,
                   RewardPickMode.UniqueCategory, out selected, rollContext,
                   weaponDpsBias, requireAssistDpsReward)
            || RewardWeightedPicker.TryTakeFromRarity(
                pools, rarity, usedCategories, usedCategoryRarities,
                RewardPickMode.UniqueCategoryRarity, out selected, rollContext,
                weaponDpsBias, requireAssistDpsReward)
            || RewardWeightedPicker.TryTakeFromRarity(
                pools, rarity, usedCategories, usedCategoryRarities,
                RewardPickMode.Any, out selected, rollContext,
                weaponDpsBias, requireAssistDpsReward);
    }

    private static bool TrySelectAssistPrimaryDps(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        RewardRarity preferredRarity,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        RewardRollContext rollContext,
        RewardWeaponDpsBias weaponDpsBias,
        out RewardModifierEntry selected)
    {
        if (TrySelectAtRarity(
                pools, preferredRarity, usedCategories, usedCategoryRarities,
                rollContext, weaponDpsBias, out selected, true))
        {
            return true;
        }

        if (preferredRarity == RewardRarity.Legendary && TrySelectAtRarity(
                pools, RewardRarity.Rare, usedCategories, usedCategoryRarities,
                rollContext, weaponDpsBias, out selected, true))
        {
            return true;
        }

        return preferredRarity != RewardRarity.Common
            && TrySelectAtRarity(
                pools, RewardRarity.Common, usedCategories, usedCategoryRarities,
                rollContext, weaponDpsBias, out selected, true);
    }

    private static bool TrySelectPremiumFallback(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        RewardRarity preferredRarity,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        RewardRollContext rollContext,
        RewardWeaponDpsBias weaponDpsBias,
        out RewardModifierEntry selected)
    {
        RewardRarity first = preferredRarity == RewardRarity.Legendary
            ? RewardRarity.Legendary
            : RewardRarity.Rare;
        RewardRarity second = preferredRarity == RewardRarity.Legendary
            ? RewardRarity.Rare
            : RewardRarity.Legendary;

        return TrySelectAtRarity(
                   pools, first, usedCategories, usedCategoryRarities,
                   rollContext, weaponDpsBias, out selected)
            || TrySelectAtRarity(
                pools, second, usedCategories, usedCategoryRarities,
                rollContext, weaponDpsBias, out selected);
    }

    private static bool TrySelectFromAllRarities(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        bool allowLegendary,
        RewardRollContext rollContext,
        RewardWeaponDpsBias weaponDpsBias,
        out RewardModifierEntry selected)
    {
        return RewardWeightedPicker.TryTakeFromAllRarities(
                   pools, usedCategories, usedCategoryRarities,
                   RewardPickMode.UniqueCategory, out selected, allowLegendary,
                   rollContext, weaponDpsBias)
            || RewardWeightedPicker.TryTakeFromAllRarities(
                pools, usedCategories, usedCategoryRarities,
                RewardPickMode.UniqueCategoryRarity, out selected, allowLegendary,
                rollContext, weaponDpsBias)
            || RewardWeightedPicker.TryTakeFromAllRarities(
                pools, usedCategories, usedCategoryRarities,
                RewardPickMode.Any, out selected, allowLegendary,
                rollContext, weaponDpsBias);
    }
}
