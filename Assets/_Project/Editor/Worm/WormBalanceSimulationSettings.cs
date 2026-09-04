using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

internal sealed class WormBalanceSimulationSettings
{
    public readonly RewardDatabase RewardDatabase;
    public readonly WormHpScalingConfig HpConfig;
    public readonly WormPressureConfig PressureConfig;
    public readonly WeaponConfig MainWeaponConfig;
    public readonly AcaciaThornWeaponConfig AcaciaThornConfig;
    public readonly int RunCount;
    public readonly int Seed;
    public readonly int LevelNumber;
    public readonly int SectionCount;
    public readonly float PathTimeLimitSeconds;
    public readonly bool DerivePathTimeFromRail;
    public readonly float WormSpeed;
    public readonly float SegmentSpacing;
    public readonly float RollbackSpeed;
    public readonly float SectionRollbackForwardSpeedMultiplier;
    public readonly float HitEfficiency;
    public readonly int ProgressBucketCount;
    public readonly bool SimulatePlayerXFollow;
    public readonly bool UseRuntimePressure;
    public readonly bool ApplySectionRollback;
    public readonly WormBalanceRewardPickStrategy RewardPickStrategy;
    public readonly WormBalanceAdSimulationMode AdSimulationMode;
    public readonly int FreeRerollAttemptsPerSession;
    public readonly int AdRerollAttemptsPerSession;
    public readonly int TakeAllAttemptsPerSession;
    public readonly int ReviveAttemptsPerSession;
    public readonly float ReviveRollbackProgress;
    public readonly float FreeRerollMinDpsGainRatio;
    public readonly float AdRerollMinDpsGainRatio;
    public readonly float TakeAllMinTotalDpsGainRatio;
    public readonly float TakeAllMinHeadPathProgress;
    public readonly WormBalancePathMetrics PathMetrics;

    public WormBalanceSimulationSettings(
        RewardDatabase rewardDatabase,
        WormHpScalingConfig hpConfig,
        WormPressureConfig pressureConfig,
        WeaponConfig mainWeaponConfig,
        AcaciaThornWeaponConfig acaciaThornConfig,
        int runCount,
        int seed,
        int levelNumber,
        int sectionCount,
        float pathTimeLimitSeconds,
        bool derivePathTimeFromRail,
        float wormSpeed,
        float segmentSpacing,
        float rollbackSpeed,
        float sectionRollbackForwardSpeedMultiplier,
        float hitEfficiency,
        int progressBucketCount,
        bool simulatePlayerXFollow,
        bool useRuntimePressure,
        bool applySectionRollback,
        WormBalanceRewardPickStrategy rewardPickStrategy,
        WormBalanceAdSimulationMode adSimulationMode,
        int freeRerollAttemptsPerSession,
        int adRerollAttemptsPerSession,
        int takeAllAttemptsPerSession,
        int reviveAttemptsPerSession,
        float reviveRollbackProgress,
        float freeRerollMinDpsGainRatio,
        float adRerollMinDpsGainRatio,
        float takeAllMinTotalDpsGainRatio,
        float takeAllMinHeadPathProgress,
        WormBalancePathMetrics pathMetrics)
    {
        RewardDatabase = rewardDatabase;
        HpConfig = hpConfig;
        PressureConfig = pressureConfig;
        MainWeaponConfig = mainWeaponConfig;
        AcaciaThornConfig = acaciaThornConfig;
        RunCount = Mathf.Max(1, runCount);
        Seed = seed;
        LevelNumber = Mathf.Max(1, levelNumber);
        SectionCount = Mathf.Max(1, sectionCount);
        DerivePathTimeFromRail = derivePathTimeFromRail;
        WormSpeed = Mathf.Max(0.01f, wormSpeed);
        PathMetrics = pathMetrics ?? WormBalancePathMetrics.CreateFallback(
            pathTimeLimitSeconds,
            WormSpeed,
            progressBucketCount);
        PathTimeLimitSeconds = Mathf.Max(1f, PathMetrics.PathTimeLimitSeconds);
        SegmentSpacing = Mathf.Max(0.01f, segmentSpacing);
        RollbackSpeed = Mathf.Max(0.01f, rollbackSpeed);
        SectionRollbackForwardSpeedMultiplier = Mathf.Max(0f, sectionRollbackForwardSpeedMultiplier);
        HitEfficiency = Mathf.Max(0.01f, hitEfficiency);
        ProgressBucketCount = Mathf.Clamp(progressBucketCount, 2, 20);
        SimulatePlayerXFollow = simulatePlayerXFollow;
        UseRuntimePressure = useRuntimePressure;
        ApplySectionRollback = applySectionRollback;
        RewardPickStrategy = rewardPickStrategy;
        AdSimulationMode = adSimulationMode;
        FreeRerollAttemptsPerSession = Mathf.Max(0, freeRerollAttemptsPerSession);
        AdRerollAttemptsPerSession = Mathf.Max(0, adRerollAttemptsPerSession);
        TakeAllAttemptsPerSession = Mathf.Max(0, takeAllAttemptsPerSession);
        ReviveAttemptsPerSession = Mathf.Max(0, reviveAttemptsPerSession);
        ReviveRollbackProgress = Mathf.Clamp01(reviveRollbackProgress);
        FreeRerollMinDpsGainRatio = Mathf.Max(0f, freeRerollMinDpsGainRatio);
        AdRerollMinDpsGainRatio = Mathf.Max(0f, adRerollMinDpsGainRatio);
        TakeAllMinTotalDpsGainRatio = Mathf.Max(0f, takeAllMinTotalDpsGainRatio);
        TakeAllMinHeadPathProgress = Mathf.Clamp01(takeAllMinHeadPathProgress);
    }

    public bool IncludesScenario(WormBalanceScenario scenario)
    {
        return AdSimulationMode switch
        {
            WormBalanceAdSimulationMode.NoAdsOnly => scenario == WormBalanceScenario.NoAds,
            WormBalanceAdSimulationMode.AdsAssistOnly => scenario == WormBalanceScenario.AdsAssist,
            WormBalanceAdSimulationMode.CompareNoAdsAndAdsAssist =>
                scenario == WormBalanceScenario.NoAds || scenario == WormBalanceScenario.AdsAssist,
            WormBalanceAdSimulationMode.BalanceMatrix => true,
            _ => false
        };
    }

    public int ScenarioCount
    {
        get
        {
            return AdSimulationMode switch
            {
                WormBalanceAdSimulationMode.CompareNoAdsAndAdsAssist => 2,
                WormBalanceAdSimulationMode.BalanceMatrix => 4,
                _ => 1
            };
        }
    }

    public bool IsValid(out string error)
    {
        if (RewardDatabase == null)
        {
            error = "Reward database is missing.";
            return false;
        }

        if (HpConfig == null)
        {
            error = "Worm HP config is missing.";
            return false;
        }

        if (MainWeaponConfig == null || MainWeaponConfig.Projectile == null)
        {
            error = "Main weapon config or projectile config is missing.";
            return false;
        }

        error = null;
        return true;
    }
}
