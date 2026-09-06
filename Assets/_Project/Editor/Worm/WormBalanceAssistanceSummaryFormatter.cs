using System.Text;

internal static class WormBalanceAssistanceSummaryFormatter
{
    public static void Append(
        StringBuilder builder,
        WormBalanceScenarioStatistics noAds,
        WormBalanceScenarioStatistics reviveOnly,
        WormBalanceScenarioStatistics adsNoRevive,
        WormBalanceScenarioStatistics fullAds)
    {
        if (!HasSamples(noAds))
            return;

        if (HasSamples(reviveOnly))
        {
            builder.AppendLine(
                $"Revive rescue uplift: +{(reviveOnly.WinRate - noAds.WinRate) * 100f:0.0} pp | target: revive should convert most endpoint losses into wins");
        }

        if (HasSamples(adsNoRevive))
        {
            builder.AppendLine(
                $"Paid assist uplift without revive: +{(adsNoRevive.WinRate - noAds.WinRate) * 100f:0.0} pp | target: paid reroll/take-all should help, not replace revive");
        }

        if (HasSamples(fullAds))
        {
            builder.AppendLine(
                $"Full ads uplift: +{(fullAds.WinRate - noAds.WinRate) * 100f:0.0} pp | target: full assist should feel like a near-guaranteed save");
        }
    }

    private static bool HasSamples(WormBalanceScenarioStatistics statistics)
    {
        return statistics != null && statistics.SampleCount > 0;
    }
}
