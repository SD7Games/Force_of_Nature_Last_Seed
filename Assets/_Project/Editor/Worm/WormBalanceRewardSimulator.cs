using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

internal static class WormBalanceRewardSimulator
{
    public static WormBalanceRewardSelection ResolveRewardPopup(
        RewardRollService rewardRollService,
        RewardRuntimeContext rewardContext,
        CocoonRewardProfile cocoonProfile,
        RewardRollContext rollContext,
        WormBalanceSimulationSettings settings,
        WeaponRuntimeState mainState,
        AcaciaThornRuntimeState acaciaState,
        WormBalanceAdSessionState adSession,
        IRandomSource randomSource)
    {
        WormBalanceRewardOffer offer = RollAndEvaluateOffer(
            rewardRollService,
            rewardContext,
            cocoonProfile,
            rollContext,
            settings,
            mainState,
            acaciaState);

        float currentDps = GetCurrentEstimatedDps(settings, mainState, acaciaState);

        for (int attemptIndex = 0;
             attemptIndex < settings.FreeRerollAttemptsPerSession;
             attemptIndex++)
        {
            if (adSession == null
                || !ShouldRerollOffer(offer, currentDps, settings.FreeRerollMinDpsGainRatio)
                || !adSession.TryUseFreeReroll())
            {
                break;
            }

            offer = RollAndEvaluateOffer(
                rewardRollService,
                rewardContext,
                cocoonProfile,
                rollContext,
                settings,
                mainState,
                acaciaState);
        }

        for (int attemptIndex = 0;
             attemptIndex < settings.AdRerollAttemptsPerSession;
             attemptIndex++)
        {
            if (adSession == null
                || !ShouldRerollOffer(offer, currentDps, settings.AdRerollMinDpsGainRatio)
                || !adSession.TryUseAdReroll())
            {
                break;
            }

            offer = RollAndEvaluateOffer(
                rewardRollService,
                rewardContext,
                cocoonProfile,
                rollContext,
                settings,
                mainState,
                acaciaState,
                RewardAdRerollPolicy.RollGuaranteedRarity(
                    rewardContext,
                    cocoonProfile,
                    rollContext,
                    randomSource),
                1,
                isPaidAssistRoll: true);
        }

        if (adSession != null
            && ShouldTakeAll(
                offer,
                rollContext,
                currentDps,
                settings.TakeAllMinTotalDpsGainRatio,
                settings.TakeAllMinHeadPathProgress)
            && adSession.TryUseTakeAll())
        {
            return offer.CreateTakeAllSelection();
        }

        return offer.CreateSingleSelection();
    }

    private static WormBalanceRewardOffer RollAndEvaluateOffer(
        RewardRollService rewardRollService,
        RewardRuntimeContext rewardContext,
        CocoonRewardProfile cocoonProfile,
        RewardRollContext rollContext,
        WormBalanceSimulationSettings settings,
        WeaponRuntimeState mainState,
        AcaciaThornRuntimeState acaciaState,
        RewardRarity? guaranteedRarity = null,
        int guaranteedRaritySlotCount = 1,
        bool isPaidAssistRoll = false)
    {
        RewardRollContext effectiveRollContext = isPaidAssistRoll
            ? rollContext.WithPaidAssistRoll()
            : rollContext;
        RewardRarity rarity = guaranteedRarity
            ?? rewardRollService.RollGuaranteeRarity(
                rewardContext,
                cocoonProfile,
                effectiveRollContext);
        List<RewardChoiceData> choices = rewardRollService.Roll3(
            rewardContext,
            cocoonProfile,
            rarity,
            guaranteedRaritySlotCount,
            effectiveRollContext);
        RewardChoiceData selectedReward = PickReward(
            choices,
            settings,
            mainState,
            acaciaState,
            out float selectedDpsGain);
        var evaluations = new List<WormBalanceRewardChoiceEvaluation>(
            choices != null ? choices.Count : 0);

        if (choices != null)
        {
            for (int i = 0; i < choices.Count; i++)
            {
                RewardChoiceData choice = choices[i];
                float dpsGain = choice != null && choice.Effect != null
                    ? CalculateEstimatedDpsGain(choice, settings, mainState, acaciaState)
                    : float.NegativeInfinity;

                evaluations.Add(new WormBalanceRewardChoiceEvaluation(choice, dpsGain));
            }
        }

        return new WormBalanceRewardOffer(
            choices,
            evaluations,
            selectedReward,
            selectedDpsGain);
    }

    private static bool ShouldRerollOffer(
        WormBalanceRewardOffer offer,
        float currentDps,
        float minDpsGainRatio)
    {
        if (offer == null || offer.SelectedReward == null)
            return true;

        float minimumDpsGain = Mathf.Max(0.01f, currentDps * minDpsGainRatio);
        return offer.SelectedDpsGain < minimumDpsGain;
    }

    private static bool ShouldTakeAll(
        WormBalanceRewardOffer offer,
        RewardRollContext rollContext,
        float currentDps,
        float minTotalDpsGainRatio,
        float minHeadPathProgress)
    {
        if (offer == null || offer.BeneficialRewardCount < 2)
            return false;

        if (!RewardAdRerollPolicy.CanOfferTakeAll(rollContext, minHeadPathProgress))
            return false;

        float minimumTotalDpsGain = Mathf.Max(0.01f, currentDps * minTotalDpsGainRatio);
        return offer.TotalPositiveDpsGain >= minimumTotalDpsGain
            && offer.TotalPositiveDpsGain > offer.SelectedDpsGain + 0.0001f;
    }

    private static float GetCurrentEstimatedDps(
        WormBalanceSimulationSettings settings,
        WeaponRuntimeState mainState,
        AcaciaThornRuntimeState acaciaState)
    {
        WeaponPowerSnapshot power = WormBalanceWeaponSimulation.EstimatePower(settings, mainState, acaciaState);
        return power.IsValid ? Mathf.Max(0.01f, power.EstimatedDps) : 0.01f;
    }

    private static RewardChoiceData PickReward(
        List<RewardChoiceData> choices,
        WormBalanceSimulationSettings settings,
        WeaponRuntimeState mainState,
        AcaciaThornRuntimeState acaciaState,
        out float selectedDpsGain)
    {
        selectedDpsGain = 0f;

        if (choices == null || choices.Count == 0)
            return null;

        if (settings.RewardPickStrategy == WormBalanceRewardPickStrategy.RandomChoice)
            return choices[Random.Range(0, choices.Count)];

        if (settings.RewardPickStrategy == WormBalanceRewardPickStrategy.HighestEstimatedDpsGain)
            return PickHighestEstimatedDpsGainReward(
                choices,
                settings,
                mainState,
                acaciaState,
                out selectedDpsGain);

        RewardRarity bestRarity = RewardRarity.Common;

        for (int i = 0; i < choices.Count; i++)
        {
            if (choices[i] != null && choices[i].Rarity > bestRarity)
                bestRarity = choices[i].Rarity;
        }

        int matchingCount = 0;

        for (int i = 0; i < choices.Count; i++)
        {
            if (choices[i] != null && choices[i].Rarity == bestRarity)
                matchingCount++;
        }

        int selectedIndex = Random.Range(0, matchingCount);
        int currentIndex = 0;

        for (int i = 0; i < choices.Count; i++)
        {
            if (choices[i] == null || choices[i].Rarity != bestRarity)
                continue;

            if (currentIndex == selectedIndex)
                return choices[i];

            currentIndex++;
        }

        return choices[0];
    }

    private static RewardChoiceData PickHighestEstimatedDpsGainReward(
        List<RewardChoiceData> choices,
        WormBalanceSimulationSettings settings,
        WeaponRuntimeState mainState,
        AcaciaThornRuntimeState acaciaState,
        out float selectedDpsGain)
    {
        selectedDpsGain = float.MinValue;
        float bestRarityScore = -1f;
        var candidates = new List<RewardChoiceData>(choices.Count);

        for (int i = 0; i < choices.Count; i++)
        {
            RewardChoiceData choice = choices[i];

            if (choice == null || choice.Effect == null)
                continue;

            float dpsGain = CalculateEstimatedDpsGain(
                choice,
                settings,
                mainState,
                acaciaState);
            float rarityScore = (int)choice.Rarity;

            if (dpsGain > selectedDpsGain + 0.0001f)
            {
                candidates.Clear();
                candidates.Add(choice);
                selectedDpsGain = dpsGain;
                bestRarityScore = rarityScore;
                continue;
            }

            if (Mathf.Abs(dpsGain - selectedDpsGain) > 0.0001f)
                continue;

            if (rarityScore > bestRarityScore)
            {
                candidates.Clear();
                candidates.Add(choice);
                bestRarityScore = rarityScore;
                continue;
            }

            if (Mathf.Approximately(rarityScore, bestRarityScore))
                candidates.Add(choice);
        }

        if (candidates.Count == 0)
            return choices[Random.Range(0, choices.Count)];

        return candidates[Random.Range(0, candidates.Count)];
    }

    private static float CalculateEstimatedDpsGain(
        RewardChoiceData choice,
        WormBalanceSimulationSettings settings,
        WeaponRuntimeState mainState,
        AcaciaThornRuntimeState acaciaState)
    {
        WeaponRuntimeState mainClone = mainState.Clone();
        AcaciaThornRuntimeState acaciaClone = acaciaState.Clone();
        RewardRuntimeContext clonedContext = new(
            mainClone,
            acaciaClone,
            () => WormBalanceWeaponSimulation.BuildMainWeaponDamage(settings.MainWeaponConfig, mainClone),
            settings.MainWeaponConfig,
            settings.AcaciaThornConfig);

        WeaponPowerSnapshot before = WormBalanceWeaponSimulation.EstimatePower(settings, mainState, acaciaState);

        if (!choice.Effect.CanApply(clonedContext))
            return float.NegativeInfinity;

        choice.Effect.Apply(clonedContext);

        WeaponPowerSnapshot after = WormBalanceWeaponSimulation.EstimatePower(settings, mainClone, acaciaClone);

        float beforeDps = before.IsValid ? before.EstimatedDps : 0f;
        float afterDps = after.IsValid ? after.EstimatedDps : 0f;

        return afterDps - beforeDps;
    }

}
