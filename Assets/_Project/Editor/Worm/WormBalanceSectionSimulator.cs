using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

internal static class WormBalanceSectionSimulator
{
    private const int ThousandHp = 1000;
    private const int TenThousandHp = 10000;
    private const int MillionHp = 1000000;
    private const int TenMillionHp = 10000000;

    public static WormBalanceSectionState[] BuildSections(
        WormBalanceSimulationSettings settings)
    {
        int bodySegmentCount = WormPatternBuilder.GetBodySegmentCount(settings.SectionCount);
        int totalSections = WormCocoonRules.CountGameplaySections(bodySegmentCount);
        var sections = new WormBalanceSectionState[totalSections];
        int remainingSegments = bodySegmentCount;
        int sectionsWithoutCocoon = 0;

        for (int i = 0; i < totalSections; i++)
        {
            int segmentCount = Mathf.Min(
                WormCocoonRules.SectionSize,
                remainingSegments);
            float progress = WormCocoonRules.GetSectionProgress(i, totalSections);
            CocoonRewardProfile cocoonProfile = null;

            if (WormCocoonRules.ShouldPlaceCocoon(
                    i,
                    totalSections,
                    progress,
                    sectionsWithoutCocoon))
            {
                cocoonProfile = WormCocoonRules.RollCocoonProfile(
                    settings.RewardDatabase.CocoonProfiles,
                    progress);
                sectionsWithoutCocoon = 0;
            }
            else
            {
                sectionsWithoutCocoon++;
            }

            sections[i] = new WormBalanceSectionState(
                i,
                Mathf.Max(1, segmentCount),
                cocoonProfile);
            remainingSegments -= segmentCount;
        }

        return sections;
    }

    public static void RebuildSectionHp(
        WormBalanceSimulationSettings settings,
        WormSectionHpResolver hpResolver,
        WormBalanceSectionState[] sections,
        int startIndex,
        WeaponRuntimeState mainState,
        AcaciaThornRuntimeState acaciaState,
        float runtimePressureMultiplier,
        float headProgress,
        bool hasRevivedThisRun,
        bool allowHpDecrease = false)
    {
        if (sections == null || sections.Length == 0)
            return;

        WeaponPowerSnapshot power = WormBalanceWeaponSimulation.EstimatePower(settings, mainState, acaciaState);
        int previousHp = 0;

        for (int i = 0; i < sections.Length; i++)
        {
            int baseHp = WormSectionHPGenerator.GetHP(i, settings.LevelNumber);
            int resolvedHp = hpResolver.ResolveHp(
                baseHp,
                i,
                sections.Length,
                settings.LevelNumber,
                power,
                runtimePressureMultiplier,
                GetHeadPressureMultiplier(settings, headProgress),
                hasRevivedThisRun);
            int hp = EnsureHpAbovePrevious(resolvedHp, previousHp);

            if (i >= startIndex)
            {
                if (!allowHpDecrease)
                    hp = Mathf.Max(hp, sections[i].Hp);

                sections[i].Hp = hp;
            }

            previousHp = GetPreviousHpForSection(
                previousHp,
                hp,
                sections[i],
                i,
                startIndex,
                i >= startIndex,
                allowHpDecrease);
        }
    }

    private static int GetPreviousHpForSection(
        int previousHp,
        int resolvedHp,
        WormBalanceSectionState section,
        int sectionIndex,
        int startIndex,
        bool canRebalance,
        bool allowHpDecrease)
    {
        if (canRebalance)
            return resolvedHp;

        if (!allowHpDecrease || sectionIndex == startIndex - 1)
            return Mathf.Max(previousHp, resolvedHp, section != null ? section.Hp : 0);

        return Mathf.Max(previousHp, resolvedHp);
    }

    private static float GetHeadPressureMultiplier(
        WormBalanceSimulationSettings settings,
        float headProgress)
    {
        return settings.HpConfig != null
            ? settings.HpConfig.GetHeadPathPressureMultiplier(headProgress)
            : 1f;
    }

    public static float ApplyRollback(
        WormBalanceSimulationSettings settings,
        int destroyedSegmentCount,
        float headProgress)
    {
        float pathLength = settings.PathMetrics.PathLength;

        if (pathLength <= 0f)
            return headProgress;

        float rollbackDistance = destroyedSegmentCount * settings.SegmentSpacing;
        float rollbackSpeed = Mathf.Max(0.01f, settings.RollbackSpeed);
        float forwardSpeed = Mathf.Max(0f, settings.WormSpeed * settings.SectionRollbackForwardSpeedMultiplier);
        float effectiveRollbackDistance = rollbackDistance *
            (rollbackSpeed / Mathf.Max(0.01f, rollbackSpeed + forwardSpeed));
        float rollbackProgress = effectiveRollbackDistance / pathLength;
        return Mathf.Clamp01(headProgress - rollbackProgress);
    }

    private static int EnsureHpAbovePrevious(int hp, int previousHp)
    {
        if (previousHp <= 0)
            return Mathf.Max(1, hp);

        if (previousHp >= WeaponRuntimeState.MaxProjectileDamage)
            return WeaponRuntimeState.MaxProjectileDamage;

        int minimumIncrease = GetMinimumVisibleHpIncrease(previousHp);

        return Mathf.Min(
            WeaponRuntimeState.MaxProjectileDamage,
            Mathf.Max(hp, previousHp + minimumIncrease));
    }

    public static int CalculateRemainingSectionHp(
        int currentHp,
        float dps,
        float elapsedDamageTime)
    {
        if (currentHp <= 1 || dps <= 0f || elapsedDamageTime <= 0f)
            return Mathf.Max(1, currentHp);

        float remainingHp = currentHp - (dps * elapsedDamageTime);
        return Mathf.Max(1, Mathf.CeilToInt(remainingHp));
    }

    public static float CalculateSectionDamageProgress(
        int currentHp,
        int remainingHp)
    {
        if (currentHp <= 0)
            return 0f;

        return Mathf.Clamp01((currentHp - remainingHp) / (float)currentHp);
    }

    private static int GetMinimumVisibleHpIncrease(int previousHp)
    {
        if (previousHp < ThousandHp)
            return 1;

        if (previousHp < TenThousandHp)
            return 100;

        if (previousHp < MillionHp)
            return 1000;

        if (previousHp < TenMillionHp)
            return 100000;

        return 1000000;
    }

    public static int CountProgressSegments(WormBalanceSectionState[] sections)
    {
        if (sections == null)
            return 0;

        int count = 0;

        for (int i = 0; i < sections.Length; i++)
            count += sections[i].SegmentCount;

        return count;
    }

    public static float GetDestructionProgress(
        float destroyedSegments,
        int totalSegments)
    {
        return totalSegments > 0
            ? Mathf.Clamp01(destroyedSegments / (float)totalSegments)
            : 0f;
    }

}
