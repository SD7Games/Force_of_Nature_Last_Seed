using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

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
