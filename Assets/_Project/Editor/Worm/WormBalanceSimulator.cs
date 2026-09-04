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
        WeaponRuntimeState mainState = CreateMainWeaponState(settings.MainWeaponConfig);
        AcaciaThornRuntimeState acaciaState = CreateAcaciaThornState(settings.AcaciaThornConfig);
        RewardRuntimeContext rewardContext = new(
            mainState,
            acaciaState,
            () => BuildMainWeaponDamage(settings.MainWeaponConfig, mainState),
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
            WeaponPowerSnapshot power = EstimatePower(settings, mainState, acaciaState);

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
            WormBalanceRewardSelection rewardSelection = ResolveRewardPopup(
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

        WeaponPowerSnapshot finalPower = EstimatePower(settings, mainState, acaciaState);

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

        WeaponPowerSnapshot power = EstimatePower(settings, mainState, acaciaState);
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

    private static WormBalanceRewardSelection ResolveRewardPopup(
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
        WeaponPowerSnapshot power = EstimatePower(settings, mainState, acaciaState);
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
            () => BuildMainWeaponDamage(settings.MainWeaponConfig, mainClone),
            settings.MainWeaponConfig,
            settings.AcaciaThornConfig);

        WeaponPowerSnapshot before = EstimatePower(settings, mainState, acaciaState);

        if (!choice.Effect.CanApply(clonedContext))
            return float.NegativeInfinity;

        choice.Effect.Apply(clonedContext);

        WeaponPowerSnapshot after = EstimatePower(settings, mainClone, acaciaClone);

        float beforeDps = before.IsValid ? before.EstimatedDps : 0f;
        float afterDps = after.IsValid ? after.EstimatedDps : 0f;

        return afterDps - beforeDps;
    }

    private static WeaponRuntimeState CreateMainWeaponState(WeaponConfig config)
    {
        WeaponRuntimeState state = new();

        if (config == null)
            return state;

        state.SetFireRateBonusLimit(config.MaxFireRateBonus);
        state.SetProjectileSpeedBonusLimit(config.MaxProjectileSpeedBonus);
        state.SetProgressionLimits(
            config.MaxDamageMultiplier,
            config.MaxCriticalChance,
            config.MaxCriticalDamageMultiplier,
            config.MaxPenetrationBonus,
            config.MaxParallelProjectiles,
            config.MaxSalvoExtraShots);

        return state;
    }

    private static AcaciaThornRuntimeState CreateAcaciaThornState(
        AcaciaThornWeaponConfig config)
    {
        AcaciaThornRuntimeState state = new();

        if (config == null)
            return state;

        state.SetProgressionLimits(
            config.MaxDamageMultiplier,
            config.MaxFireRateBonus,
            config.MaxSalvoExtraShots,
            config.MaxProjectileSpeedBonus,
            config.MaxCriticalChance,
            config.CriticalDamageMultiplier,
            config.MaxCriticalDamageMultiplier);
        state.SetBaseDamage(config.Damage);

        return state;
    }

    private static WeaponPowerSnapshot EstimatePower(
        WormBalanceSimulationSettings settings,
        WeaponRuntimeState mainState,
        AcaciaThornRuntimeState acaciaState)
    {
        return WeaponPowerEstimator.Estimate(
            settings.MainWeaponConfig,
            mainState,
            settings.AcaciaThornConfig,
            acaciaState);
    }

    private static int BuildMainWeaponDamage(
        WeaponConfig config,
        WeaponRuntimeState state)
    {
        if (config == null || config.Projectile == null || state == null)
            return 0;

        return WeaponRuntimeState.ClampDamage(
            config.Projectile.Damage * (double)state.DamageMultiplier);
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
