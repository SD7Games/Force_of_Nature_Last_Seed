using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

internal static class WormBalanceSimulator
{
    private const int ThousandHp = 1000;
    private const int TenThousandHp = 10000;
    private const int MillionHp = 1000000;
    private const int TenMillionHp = 10000000;

    public static WormBalanceSimulationReport Run(WormBalanceSimulationSettings settings)
    {
        Random.State previousRandomState = Random.state;
        var runs = new List<WormBalanceRunResult>(settings.RunCount * settings.ScenarioCount);

        try
        {
            for (int i = 0; i < settings.RunCount; i++)
            {
                if (settings.IncludesScenario(WormBalanceScenario.NoAds))
                {
                    Random.InitState(settings.Seed + i * 7919);
                    runs.Add(SimulateRun(settings, i, WormBalanceScenario.NoAds));
                }

                if (settings.IncludesScenario(WormBalanceScenario.ReviveOnly))
                {
                    Random.InitState(settings.Seed + i * 7919);
                    runs.Add(SimulateRun(settings, i, WormBalanceScenario.ReviveOnly));
                }

                if (settings.IncludesScenario(WormBalanceScenario.AdsAssistNoRevive))
                {
                    Random.InitState(settings.Seed + i * 7919);
                    runs.Add(SimulateRun(settings, i, WormBalanceScenario.AdsAssistNoRevive));
                }

                if (settings.IncludesScenario(WormBalanceScenario.AdsAssist))
                {
                    Random.InitState(settings.Seed + i * 7919);
                    runs.Add(SimulateRun(settings, i, WormBalanceScenario.AdsAssist));
                }
            }
        }
        finally
        {
            Random.state = previousRandomState;
        }

        return new WormBalanceSimulationReport(settings, runs);
    }

    private static WormBalanceRunResult SimulateRun(
        WormBalanceSimulationSettings settings,
        int runIndex,
        WormBalanceScenario scenario)
    {
        WeaponRuntimeState mainState = WormBalanceWeaponSimulation.CreateMainWeaponState(settings.MainWeaponConfig);
        AcaciaThornRuntimeState acaciaState = WormBalanceWeaponSimulation.CreateAcaciaThornState(settings.AcaciaThornConfig);
        RewardRuntimeContext rewardContext = new(
            mainState,
            acaciaState,
            () => WormBalanceWeaponSimulation.BuildMainWeaponDamage(settings.MainWeaponConfig, mainState),
            settings.MainWeaponConfig,
            settings.AcaciaThornConfig);
        IRandomSource randomSource = new UnityRandomSource();
        RewardRollService rewardRollService = new(settings.RewardDatabase, randomSource);
        WormSectionHpResolver hpResolver = new(settings.HpConfig);
        WormBalanceSectionState[] sections = BuildSections(settings);
        WormBalanceAdSessionState adSession = WormBalanceAdSessionState.Create(settings, scenario);

        float time = 0f;
        float headProgress = 0f;
        float playerX = settings.PathMetrics.GetHeadX(headProgress);
        float maxPlayerXError = 0f;
        float pressureElapsedTime = 0f;
        float pressureSampleTimer = 0f;
        float runtimePressureMultiplier = 1f;
        bool pressureChanged = false;
        int destroyedSegments = 0;
        int rewardsTaken = 0;
        int lastSectionIndex = -1;
        float firstRewardTime = -1f;
        bool hasRevivedThisRun = false;
        StringBuilder rewardLog = new();

        RebuildSectionHp(
            settings,
            hpResolver,
            sections,
            0,
            mainState,
            acaciaState,
            runtimePressureMultiplier,
            headProgress,
            hasRevivedThisRun,
            allowHpDecrease: true);

        int totalProgressSegments = CountProgressSegments(sections);

        for (int i = 0; i < sections.Length; i++)
        {
            WormBalanceSectionState section = sections[i];
            WeaponPowerSnapshot power = WormBalanceWeaponSimulation.EstimatePower(settings, mainState, acaciaState);

            if (!power.IsValid || power.EstimatedDps <= 0f)
            {
                return WormBalanceRunResult.Loss(
                    scenario,
                    runIndex,
                    "No DPS",
                    time,
                    GetDestructionProgress(destroyedSegments, totalProgressSegments),
                    headProgress,
                    i,
                    lastSectionIndex,
                    rewardsTaken,
                    firstRewardTime,
                    0f,
                    playerX,
                    settings.PathMetrics.GetHeadX(headProgress),
                    maxPlayerXError,
                    settings.PathMetrics.GetLocation(headProgress),
                    adSession.ToStats(),
                    rewardLog.ToString());
            }

            float dps = Mathf.Max(0.01f, power.EstimatedDps * settings.HitEfficiency);
            float killTime = section.Hp / dps;
            float timeBeforeSectionDamage = time;

            if (!AdvanceTime(
                    settings,
                    killTime,
                    ref time,
                    ref pressureElapsedTime,
                    ref headProgress,
                    ref pressureSampleTimer,
                    ref runtimePressureMultiplier,
                    ref pressureChanged,
                    ref playerX,
                    ref maxPlayerXError))
            {
                int remainingSectionHp = CalculateRemainingSectionHp(
                    section.Hp,
                    dps,
                    time - timeBeforeSectionDamage);
                float endpointSectionDamageProgress = CalculateSectionDamageProgress(
                    section.Hp,
                    remainingSectionHp);
                float endpointDestructionProgress = GetDestructionProgress(
                    destroyedSegments + (section.SegmentCount * endpointSectionDamageProgress),
                    totalProgressSegments);

                if (TryUseRevive(
                        settings,
                        adSession,
                        ref hasRevivedThisRun,
                        ref headProgress,
                        ref pressureElapsedTime,
                        ref pressureSampleTimer,
                        ref runtimePressureMultiplier,
                        ref pressureChanged,
                        ref playerX,
                        ref maxPlayerXError))
                {
                    section.Hp = remainingSectionHp;
                    RebuildSectionHp(
                        settings,
                        hpResolver,
                        sections,
                        i + 1,
                        mainState,
                        acaciaState,
                        runtimePressureMultiplier,
                        headProgress,
                        hasRevivedThisRun,
                        allowHpDecrease: true);
                    pressureChanged = false;
                    i--;
                    continue;
                }

                return WormBalanceRunResult.Loss(
                    scenario,
                    runIndex,
                    "Path completed",
                    time,
                    GetDestructionProgress(destroyedSegments, totalProgressSegments),
                    headProgress,
                    i,
                    lastSectionIndex,
                    rewardsTaken,
                    firstRewardTime,
                    dps,
                    playerX,
                    settings.PathMetrics.GetHeadX(headProgress),
                    maxPlayerXError,
                    settings.PathMetrics.GetLocation(headProgress),
                    adSession.ToStats(),
                    rewardLog.ToString(),
                    endpointDestructionProgress,
                    endpointSectionDamageProgress);
            }

            destroyedSegments += section.SegmentCount;
            lastSectionIndex = i;

            if (settings.ApplySectionRollback)
            {
                headProgress = ApplyRollback(settings, section.SegmentCount, headProgress);
                AlignPlayerXWithHead(settings, ref playerX, headProgress, ref maxPlayerXError);
            }

            if (pressureChanged)
            {
                RebuildSectionHp(
                    settings,
                    hpResolver,
                    sections,
                    i + 1,
                    mainState,
                    acaciaState,
                    runtimePressureMultiplier,
                    headProgress,
                    hasRevivedThisRun);
                pressureChanged = false;
            }

            if (!section.HasCocoon)
                continue;

            RewardRollContext rollContext = new(
                headProgress,
                GetDestructionProgress(destroyedSegments, totalProgressSegments),
                hasRevivedThisRun);
            WormBalanceRewardSelection rewardSelection = WormBalanceRewardSimulator.ResolveRewardPopup(
                rewardRollService,
                rewardContext,
                section.CocoonProfile,
                rollContext,
                settings,
                mainState,
                acaciaState,
                adSession,
                randomSource);

            if (rewardSelection.Rewards == null || rewardSelection.Rewards.Count == 0)
            {
                AppendRewardLog(
                    rewardLog,
                    time,
                    section.CocoonProfile,
                    null,
                    0f);
                continue;
            }

            for (int rewardIndex = 0; rewardIndex < rewardSelection.Rewards.Count; rewardIndex++)
            {
                RewardChoiceData selectedReward = rewardSelection.Rewards[rewardIndex];

                if (selectedReward == null || selectedReward.Effect == null)
                    continue;

                if (!selectedReward.Effect.CanApply(rewardContext))
                    continue;

                selectedReward.Effect.Apply(rewardContext);
                rewardsTaken++;

                if (firstRewardTime < 0f)
                    firstRewardTime = time;

                AppendRewardLog(
                    rewardLog,
                    time,
                    section.CocoonProfile,
                    selectedReward,
                    rewardSelection.GetDpsGain(selectedReward));
            }

            RebuildSectionHp(
                settings,
                hpResolver,
                sections,
                i + 1,
                mainState,
                acaciaState,
                runtimePressureMultiplier,
                headProgress,
                hasRevivedThisRun);
        }

        WeaponPowerSnapshot finalPower = WormBalanceWeaponSimulation.EstimatePower(settings, mainState, acaciaState);

        return WormBalanceRunResult.Win(
            scenario,
            runIndex,
            time,
            GetDestructionProgress(destroyedSegments, totalProgressSegments),
            headProgress,
            sections.Length,
            lastSectionIndex,
            rewardsTaken,
            firstRewardTime,
            finalPower.IsValid ? finalPower.EstimatedDps * settings.HitEfficiency : 0f,
            playerX,
            settings.PathMetrics.GetHeadX(headProgress),
            maxPlayerXError,
            settings.PathMetrics.GetLocation(headProgress),
            adSession.ToStats(),
            rewardLog.ToString());
    }

    private static WormBalanceSectionState[] BuildSections(
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

    private static void RebuildSectionHp(
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

    private static bool AdvanceTime(
        WormBalanceSimulationSettings settings,
        float duration,
        ref float time,
        ref float pressureElapsedTime,
        ref float headProgress,
        ref float pressureSampleTimer,
        ref float runtimePressureMultiplier,
        ref bool pressureChanged,
        ref float playerX,
        ref float maxPlayerXError)
    {
        float remaining = Mathf.Max(0f, duration);
        int maximumStepCount = 1;

        if (settings.UseRuntimePressure && settings.PressureConfig != null)
        {
            float minimumStep = Mathf.Max(
                0.0001f,
                settings.PressureConfig.SampleInterval);
            maximumStepCount = Mathf.CeilToInt(remaining / minimumStep) + 1;
        }

        for (int stepIndex = 0;
             stepIndex < maximumStepCount && remaining > 0f;
             stepIndex++)
        {
            float step = remaining;

            if (settings.UseRuntimePressure && settings.PressureConfig != null)
            {
                float timeToPressureSample = Mathf.Max(
                    0.0001f,
                    settings.PressureConfig.SampleInterval - pressureSampleTimer);
                step = Mathf.Min(step, timeToPressureSample);
            }

            if (settings.PathTimeLimitSeconds > 0f)
            {
                float timeToPathEnd = (1f - headProgress) * settings.PathTimeLimitSeconds;

                if (step >= timeToPathEnd)
                {
                    time += Mathf.Max(0f, timeToPathEnd);
                    headProgress = 1f;
                    AlignPlayerXWithHead(settings, ref playerX, headProgress, ref maxPlayerXError);
                    return false;
                }
            }

            time += step;
            pressureElapsedTime += step;
            headProgress = Mathf.Clamp01(headProgress + step / settings.PathTimeLimitSeconds);
            AlignPlayerXWithHead(settings, ref playerX, headProgress, ref maxPlayerXError);
            remaining -= step;

            if (!settings.UseRuntimePressure || settings.PressureConfig == null)
                continue;

            pressureSampleTimer += step;

            if (pressureSampleTimer + Mathf.Epsilon < settings.PressureConfig.SampleInterval)
                continue;

            pressureSampleTimer = 0f;
            float nextPressure = CalculateRuntimePressure(
                settings.PressureConfig,
                pressureElapsedTime,
                headProgress,
                runtimePressureMultiplier);

            if (Mathf.Approximately(nextPressure, runtimePressureMultiplier))
                continue;

            runtimePressureMultiplier = nextPressure;
            pressureChanged = true;
        }

        return true;
    }

    private static void AlignPlayerXWithHead(
        WormBalanceSimulationSettings settings,
        ref float playerX,
        float headProgress,
        ref float maxPlayerXError)
    {
        if (!settings.SimulatePlayerXFollow)
            return;

        float headX = settings.PathMetrics.GetHeadX(headProgress);
        playerX = headX;
        maxPlayerXError = Mathf.Max(maxPlayerXError, Mathf.Abs(playerX - headX));
    }

    private static float CalculateRuntimePressure(
        WormPressureConfig config,
        float elapsedTime,
        float headProgress,
        float currentPressure)
    {
        float expectedProgress = config.GetExpectedProgress(elapsedTime);
        float deadZone = config.ProgressDeadZone;

        if (headProgress + deadZone < expectedProgress)
            return Mathf.Min(config.MaxMultiplier, currentPressure + config.IncreasePerSample);

        if (headProgress > expectedProgress + deadZone)
            return Mathf.Max(1f, currentPressure - config.RecoveryPerSample);

        return currentPressure;
    }

    private static bool TryUseRevive(
        WormBalanceSimulationSettings settings,
        WormBalanceAdSessionState adSession,
        ref bool hasRevivedThisRun,
        ref float headProgress,
        ref float pressureElapsedTime,
        ref float pressureSampleTimer,
        ref float runtimePressureMultiplier,
        ref bool pressureChanged,
        ref float playerX,
        ref float maxPlayerXError)
    {
        if (adSession == null || !adSession.TryUseRevive())
            return false;

        hasRevivedThisRun = true;
        headProgress = settings.ReviveRollbackProgress;
        pressureElapsedTime = settings.PressureConfig != null
            ? settings.PressureConfig.GetElapsedTimeForExpectedProgress(headProgress)
            : 0f;
        pressureSampleTimer = 0f;
        runtimePressureMultiplier = 1f;
        pressureChanged = true;
        AlignPlayerXWithHead(settings, ref playerX, headProgress, ref maxPlayerXError);
        return true;
    }

    private static float GetHeadPressureMultiplier(
        WormBalanceSimulationSettings settings,
        float headProgress)
    {
        return settings.HpConfig != null
            ? settings.HpConfig.GetHeadPathPressureMultiplier(headProgress)
            : 1f;
    }

    private static float ApplyRollback(
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

    private static int CalculateRemainingSectionHp(
        int currentHp,
        float dps,
        float elapsedDamageTime)
    {
        if (currentHp <= 1 || dps <= 0f || elapsedDamageTime <= 0f)
            return Mathf.Max(1, currentHp);

        float remainingHp = currentHp - (dps * elapsedDamageTime);
        return Mathf.Max(1, Mathf.CeilToInt(remainingHp));
    }

    private static float CalculateSectionDamageProgress(
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

    private static int CountProgressSegments(WormBalanceSectionState[] sections)
    {
        if (sections == null)
            return 0;

        int count = 0;

        for (int i = 0; i < sections.Length; i++)
            count += sections[i].SegmentCount;

        return count;
    }

    private static float GetDestructionProgress(
        float destroyedSegments,
        int totalSegments)
    {
        return totalSegments > 0
            ? Mathf.Clamp01(destroyedSegments / (float)totalSegments)
            : 0f;
    }

    private static void AppendRewardLog(
        StringBuilder builder,
        float time,
        CocoonRewardProfile cocoonProfile,
        RewardChoiceData reward,
        float dpsGain)
    {
        if (builder == null)
            return;

        if (builder.Length > 0)
            builder.Append(" | ");

        string profileName = cocoonProfile != null
            ? cocoonProfile.DisplayName
            : "NoProfile";

        if (reward == null)
        {
            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0:0.0}s {1}: no reward",
                time,
                profileName);
            return;
        }

        builder.AppendFormat(
            CultureInfo.InvariantCulture,
            "{0:0.0}s {1}: {2} {3} {4}",
            time,
            profileName,
            reward.Rarity,
            reward.Title,
            reward.ValueText);

        if (dpsGain > 0f)
        {
            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                " (+{0:0.00} DPS)",
                dpsGain);
        }
    }
}
