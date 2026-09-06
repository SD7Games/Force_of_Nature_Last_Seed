using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

internal static class WormBalanceRewardLogFormatter
{
    public static void AppendRewardLog(
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
