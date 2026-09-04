using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

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
