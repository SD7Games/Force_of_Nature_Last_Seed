using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

internal sealed class WormBalanceSectionState
{
    public readonly int Index;
    public readonly int SegmentCount;
    public readonly CocoonRewardProfile CocoonProfile;

    public int Hp;
    public bool HasCocoon => CocoonProfile != null;

    public WormBalanceSectionState(
        int index,
        int segmentCount,
        CocoonRewardProfile cocoonProfile)
    {
        Index = index;
        SegmentCount = Mathf.Max(1, segmentCount);
        CocoonProfile = cocoonProfile;
    }
}

internal readonly struct WormBalanceAdSessionStats
{
    public readonly int AdsWatched;
    public readonly int FreeRerollsUsed;
    public readonly int AdRerollsUsed;
    public readonly int TakeAllAdsUsed;
    public readonly int RevivesUsed;

    public WormBalanceAdSessionStats(
        int adsWatched,
        int freeRerollsUsed,
        int adRerollsUsed,
        int takeAllAdsUsed,
        int revivesUsed)
    {
        AdsWatched = Mathf.Max(0, adsWatched);
        FreeRerollsUsed = Mathf.Max(0, freeRerollsUsed);
        AdRerollsUsed = Mathf.Max(0, adRerollsUsed);
        TakeAllAdsUsed = Mathf.Max(0, takeAllAdsUsed);
        RevivesUsed = Mathf.Max(0, revivesUsed);
    }
}

internal sealed class WormBalanceAdSessionState
{
    private int _freeRerollsLeft;
    private int _adRerollsLeft;
    private int _takeAllAdsLeft;
    private int _revivesLeft;

    private WormBalanceAdSessionState(
        int freeRerollsLeft,
        int adRerollsLeft,
        int takeAllAdsLeft,
        int revivesLeft)
    {
        _freeRerollsLeft = Mathf.Max(0, freeRerollsLeft);
        _adRerollsLeft = Mathf.Max(0, adRerollsLeft);
        _takeAllAdsLeft = Mathf.Max(0, takeAllAdsLeft);
        _revivesLeft = Mathf.Max(0, revivesLeft);
    }

    public int AdsWatched { get; private set; }
    public int FreeRerollsUsed { get; private set; }
    public int AdRerollsUsed { get; private set; }
    public int TakeAllAdsUsed { get; private set; }
    public int RevivesUsed { get; private set; }

    public static WormBalanceAdSessionState Create(
        WormBalanceSimulationSettings settings,
        WormBalanceScenario scenario)
    {
        bool allowPaidAssist = scenario is
            WormBalanceScenario.AdsAssistNoRevive or
            WormBalanceScenario.AdsAssist;
        bool allowRevive = scenario is
            WormBalanceScenario.ReviveOnly or
            WormBalanceScenario.AdsAssist;

        return new WormBalanceAdSessionState(
            settings.FreeRerollAttemptsPerSession,
            allowPaidAssist ? settings.AdRerollAttemptsPerSession : 0,
            allowPaidAssist ? settings.TakeAllAttemptsPerSession : 0,
            allowRevive ? settings.ReviveAttemptsPerSession : 0);
    }

    public bool TryUseFreeReroll()
    {
        if (_freeRerollsLeft <= 0)
            return false;

        _freeRerollsLeft--;
        FreeRerollsUsed++;
        return true;
    }

    public bool TryUseAdReroll()
    {
        if (_adRerollsLeft <= 0)
            return false;

        _adRerollsLeft--;
        AdRerollsUsed++;
        AdsWatched++;
        return true;
    }

    public bool TryUseTakeAll()
    {
        if (_takeAllAdsLeft <= 0)
            return false;

        _takeAllAdsLeft--;
        TakeAllAdsUsed++;
        AdsWatched++;
        return true;
    }

    public bool TryUseRevive()
    {
        if (_revivesLeft <= 0)
            return false;

        _revivesLeft--;
        RevivesUsed++;
        AdsWatched++;
        return true;
    }

    public WormBalanceAdSessionStats ToStats()
    {
        return new WormBalanceAdSessionStats(
            AdsWatched,
            FreeRerollsUsed,
            AdRerollsUsed,
            TakeAllAdsUsed,
            RevivesUsed);
    }
}

internal readonly struct WormBalanceRewardChoiceEvaluation
{
    public readonly RewardChoiceData Reward;
    public readonly float DpsGain;

    public WormBalanceRewardChoiceEvaluation(
        RewardChoiceData reward,
        float dpsGain)
    {
        Reward = reward;
        DpsGain = dpsGain;
    }
}

internal sealed class WormBalanceRewardOffer
{
    private readonly List<WormBalanceRewardChoiceEvaluation> _evaluations;

    public WormBalanceRewardOffer(
        List<RewardChoiceData> choices,
        List<WormBalanceRewardChoiceEvaluation> evaluations,
        RewardChoiceData selectedReward,
        float selectedDpsGain)
    {
        Choices = choices ?? new List<RewardChoiceData>();
        _evaluations = evaluations ?? new List<WormBalanceRewardChoiceEvaluation>();
        SelectedReward = selectedReward;
        SelectedDpsGain = selectedDpsGain;

        for (int i = 0; i < _evaluations.Count; i++)
        {
            float dpsGain = _evaluations[i].DpsGain;

            if (dpsGain <= 0.0001f)
                continue;

            TotalPositiveDpsGain += dpsGain;
            BeneficialRewardCount++;
        }
    }

    public readonly List<RewardChoiceData> Choices;
    public readonly RewardChoiceData SelectedReward;
    public readonly float SelectedDpsGain;
    public readonly float TotalPositiveDpsGain;
    public readonly int BeneficialRewardCount;

    public WormBalanceRewardSelection CreateSingleSelection()
    {
        var rewards = new List<RewardChoiceData>(1);

        if (SelectedReward != null)
            rewards.Add(SelectedReward);

        return new WormBalanceRewardSelection(rewards, _evaluations);
    }

    public WormBalanceRewardSelection CreateTakeAllSelection()
    {
        var rewards = new List<RewardChoiceData>(Choices.Count);

        for (int i = 0; i < Choices.Count; i++)
        {
            RewardChoiceData reward = Choices[i];

            if (reward != null && reward.Effect != null)
                rewards.Add(reward);
        }

        return new WormBalanceRewardSelection(rewards, _evaluations);
    }
}

internal sealed class WormBalanceRewardSelection
{
    private readonly List<WormBalanceRewardChoiceEvaluation> _evaluations;

    public WormBalanceRewardSelection(
        List<RewardChoiceData> rewards,
        List<WormBalanceRewardChoiceEvaluation> evaluations)
    {
        Rewards = rewards ?? new List<RewardChoiceData>();
        _evaluations = evaluations ?? new List<WormBalanceRewardChoiceEvaluation>();
    }

    public readonly List<RewardChoiceData> Rewards;

    public float GetDpsGain(RewardChoiceData reward)
    {
        if (reward == null)
            return 0f;

        for (int i = 0; i < _evaluations.Count; i++)
        {
            if (ReferenceEquals(_evaluations[i].Reward, reward))
                return _evaluations[i].DpsGain;
        }

        return 0f;
    }
}

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
