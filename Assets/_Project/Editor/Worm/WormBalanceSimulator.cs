using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

internal static class WormBalanceSimulator
{

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
        WormBalanceSectionState[] sections = WormBalanceSectionSimulator.BuildSections(settings);
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

        WormBalanceSectionSimulator.RebuildSectionHp(
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

        int totalProgressSegments = WormBalanceSectionSimulator.CountProgressSegments(sections);

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
                    WormBalanceSectionSimulator.GetDestructionProgress(destroyedSegments, totalProgressSegments),
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

            if (!WormBalanceTimelineSimulator.AdvanceTime(
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
                int remainingSectionHp = WormBalanceSectionSimulator.CalculateRemainingSectionHp(
                    section.Hp,
                    dps,
                    time - timeBeforeSectionDamage);
                float endpointSectionDamageProgress = WormBalanceSectionSimulator.CalculateSectionDamageProgress(
                    section.Hp,
                    remainingSectionHp);
                float endpointDestructionProgress = WormBalanceSectionSimulator.GetDestructionProgress(
                    destroyedSegments + (section.SegmentCount * endpointSectionDamageProgress),
                    totalProgressSegments);

                if (WormBalanceTimelineSimulator.TryUseRevive(
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
                    WormBalanceSectionSimulator.RebuildSectionHp(
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
                    WormBalanceSectionSimulator.GetDestructionProgress(destroyedSegments, totalProgressSegments),
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
                headProgress = WormBalanceSectionSimulator.ApplyRollback(settings, section.SegmentCount, headProgress);
                WormBalanceTimelineSimulator.AlignPlayerXWithHead(
                    settings,
                    ref playerX,
                    headProgress,
                    ref maxPlayerXError);
            }

            if (pressureChanged)
            {
                WormBalanceSectionSimulator.RebuildSectionHp(
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
                WormBalanceSectionSimulator.GetDestructionProgress(destroyedSegments, totalProgressSegments),
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
                WormBalanceRewardLogFormatter.AppendRewardLog(
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

                WormBalanceRewardLogFormatter.AppendRewardLog(
                    rewardLog,
                    time,
                    section.CocoonProfile,
                    selectedReward,
                    rewardSelection.GetDpsGain(selectedReward));
            }

            WormBalanceSectionSimulator.RebuildSectionHp(
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
            WormBalanceSectionSimulator.GetDestructionProgress(destroyedSegments, totalProgressSegments),
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

}
