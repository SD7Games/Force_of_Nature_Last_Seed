using System.Collections.Generic;
using UnityEngine;

internal static class WormBalanceStatistics
{
    public static float Average(IReadOnlyList<float> values)
    {
        if (values == null || values.Count == 0)
            return 0f;

        float total = 0f;

        for (int i = 0; i < values.Count; i++)
            total += values[i];

        return total / values.Count;
    }

    public static float Percentile(IReadOnlyList<float> sortedValues, float percentile)
    {
        if (sortedValues == null || sortedValues.Count == 0)
            return 0f;

        if (sortedValues.Count == 1)
            return sortedValues[0];

        float position = Mathf.Clamp01(percentile) * (sortedValues.Count - 1);
        int lower = Mathf.FloorToInt(position);
        int upper = Mathf.CeilToInt(position);

        if (lower == upper)
            return sortedValues[lower];

        return Mathf.Lerp(
            sortedValues[lower],
            sortedValues[upper],
            position - lower);
    }
}
