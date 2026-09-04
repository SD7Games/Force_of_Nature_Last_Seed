using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

internal sealed class WormBalanceRunResult
{
    public readonly WormBalanceScenario Scenario;
    public readonly int RunIndex;
    public readonly bool Won;
    public readonly string Reason;
    public readonly float TimeSeconds;
    public readonly float DestructionProgress;
    public readonly float EndpointDestructionProgress;
    public readonly float EndpointSectionDamageProgress;
    public readonly float HeadProgress;
    public readonly int SectionsDestroyed;
    public readonly int LastSectionIndex;
    public readonly int RewardsTaken;
    public readonly float FirstRewardTime;
    public readonly float FinalDps;
    public readonly float EndPlayerX;
    public readonly float EndHeadX;
    public readonly float MaxPlayerXError;
    public readonly WormBalancePathLocation EndLocation;
    public readonly WormBalanceAdSessionStats AdStats;
    public readonly string RewardLog;

    private WormBalanceRunResult(
        WormBalanceScenario scenario,
        int runIndex,
        bool won,
        string reason,
        float timeSeconds,
        float destructionProgress,
        float endpointDestructionProgress,
        float endpointSectionDamageProgress,
        float headProgress,
        int sectionsDestroyed,
        int lastSectionIndex,
        int rewardsTaken,
        float firstRewardTime,
        float finalDps,
        float endPlayerX,
        float endHeadX,
        float maxPlayerXError,
        WormBalancePathLocation endLocation,
        WormBalanceAdSessionStats adStats,
        string rewardLog)
    {
        Scenario = scenario;
        RunIndex = runIndex;
        Won = won;
        Reason = reason;
        TimeSeconds = timeSeconds;
        DestructionProgress = destructionProgress;
        EndpointDestructionProgress = Mathf.Clamp01(endpointDestructionProgress);
        EndpointSectionDamageProgress = Mathf.Clamp01(endpointSectionDamageProgress);
        HeadProgress = headProgress;
        SectionsDestroyed = sectionsDestroyed;
        LastSectionIndex = lastSectionIndex;
        RewardsTaken = rewardsTaken;
        FirstRewardTime = firstRewardTime;
        FinalDps = finalDps;
        EndPlayerX = endPlayerX;
        EndHeadX = endHeadX;
        MaxPlayerXError = maxPlayerXError;
        EndLocation = endLocation;
        AdStats = adStats;
        RewardLog = rewardLog ?? string.Empty;
    }

    public static WormBalanceRunResult Win(
        WormBalanceScenario scenario,
        int runIndex,
        float timeSeconds,
        float destructionProgress,
        float headProgress,
        int sectionsDestroyed,
        int lastSectionIndex,
        int rewardsTaken,
        float firstRewardTime,
        float finalDps,
        float endPlayerX,
        float endHeadX,
        float maxPlayerXError,
        WormBalancePathLocation endLocation,
        WormBalanceAdSessionStats adStats,
        string rewardLog,
        float endpointDestructionProgress = -1f,
        float endpointSectionDamageProgress = 1f)
    {
        float resolvedEndpointProgress = endpointDestructionProgress >= 0f
            ? endpointDestructionProgress
            : destructionProgress;

        return new WormBalanceRunResult(
            scenario,
            runIndex,
            true,
            "Worm destroyed",
            timeSeconds,
            destructionProgress,
            resolvedEndpointProgress,
            endpointSectionDamageProgress,
            headProgress,
            sectionsDestroyed,
            lastSectionIndex,
            rewardsTaken,
            firstRewardTime,
            finalDps,
            endPlayerX,
            endHeadX,
            maxPlayerXError,
            endLocation,
            adStats,
            rewardLog);
    }

    public static WormBalanceRunResult Loss(
        WormBalanceScenario scenario,
        int runIndex,
        string reason,
        float timeSeconds,
        float destructionProgress,
        float headProgress,
        int sectionsDestroyed,
        int lastSectionIndex,
        int rewardsTaken,
        float firstRewardTime,
        float finalDps,
        float endPlayerX,
        float endHeadX,
        float maxPlayerXError,
        WormBalancePathLocation endLocation,
        WormBalanceAdSessionStats adStats,
        string rewardLog,
        float endpointDestructionProgress = -1f,
        float endpointSectionDamageProgress = 0f)
    {
        float resolvedEndpointProgress = endpointDestructionProgress >= 0f
            ? endpointDestructionProgress
            : destructionProgress;

        return new WormBalanceRunResult(
            scenario,
            runIndex,
            false,
            reason,
            timeSeconds,
            destructionProgress,
            resolvedEndpointProgress,
            endpointSectionDamageProgress,
            headProgress,
            sectionsDestroyed,
            lastSectionIndex,
            rewardsTaken,
            firstRewardTime,
            finalDps,
            endPlayerX,
            endHeadX,
            maxPlayerXError,
            endLocation,
            adStats,
            rewardLog);
    }

    public bool IsEndpointLoss => !Won && HeadProgress >= 0.999f;

    public string BuildDebugLine()
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "WormBalance scenario={0} run={1} result={2} reason='{3}' time={4:0.0}s destroyed={5:0.0}% endpointDestroyed={6:0.0}% endpointSectionDamage={7:0.0}% head={8:0.0}% bucket={9} rail={10} sections={11} rewards={12} firstReward={13} dps={14:0.00} ads={15} freeRerolls={16} adRerolls={17} takeAllAds={18} revives={19} playerX={20:0.00} headX={21:0.00} xError={22:0.00}",
            Scenario,
            RunIndex,
            Won ? "WIN" : "LOSS",
            Reason,
            TimeSeconds,
            DestructionProgress * 100f,
            EndpointDestructionProgress * 100f,
            EndpointSectionDamageProgress * 100f,
            HeadProgress * 100f,
            EndLocation.BucketLabel,
            EndLocation.ControlPointLabel,
            SectionsDestroyed,
            RewardsTaken,
            FirstRewardTime >= 0f ? FirstRewardTime.ToString("0.0s", CultureInfo.InvariantCulture) : "none",
            FinalDps,
            AdStats.AdsWatched,
            AdStats.FreeRerollsUsed,
            AdStats.AdRerollsUsed,
            AdStats.TakeAllAdsUsed,
            AdStats.RevivesUsed,
            EndPlayerX,
            EndHeadX,
            MaxPlayerXError);
    }
}
