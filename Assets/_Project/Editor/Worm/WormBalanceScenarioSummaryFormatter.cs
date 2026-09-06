using System.Text;

internal static class WormBalanceScenarioSummaryFormatter
{
    public static void Append(
        StringBuilder builder,
        WormBalanceSimulationSettings settings,
        WormBalanceScenarioStatistics statistics,
        string title,
        float targetMinWinRate,
        float targetMaxWinRate)
    {
        builder.AppendLine($"{title}:");

        if (statistics.SampleCount == 0)
        {
            builder.AppendLine("No samples.");
            builder.AppendLine();
            return;
        }

        AppendOutcome(builder, statistics, targetMinWinRate, targetMaxWinRate);
        AppendAverages(builder, statistics);

        if (statistics.Scenario != WormBalanceScenario.NoAds)
            AppendAdStatistics(builder, statistics);

        if (statistics.LossCount > 0)
            AppendLossStatistics(builder, settings, statistics);

        builder.AppendLine(
            $"Verdict: {WormBalanceScenarioVerdictPolicy.Build(statistics, targetMinWinRate, targetMaxWinRate)}");
        builder.AppendLine();
    }

    private static void AppendOutcome(
        StringBuilder builder,
        WormBalanceScenarioStatistics statistics,
        float targetMinWinRate,
        float targetMaxWinRate)
    {
        builder.AppendLine(
            $"Wins: {statistics.WinCount}/{statistics.SampleCount} ({statistics.WinRate * 100f:0.0}%) | Losses: {statistics.LossCount}/{statistics.SampleCount} ({(1f - statistics.WinRate) * 100f:0.0}%) | Target: {targetMinWinRate * 100f:0}-{targetMaxWinRate * 100f:0}% | {WormBalanceScenarioVerdictPolicy.GetWinRateVerdict(statistics.WinRate, targetMinWinRate, targetMaxWinRate)}");
    }

    private static void AppendAverages(
        StringBuilder builder,
        WormBalanceScenarioStatistics statistics)
    {
        float averageTime = statistics.TotalTime / statistics.SampleCount;
        builder.AppendLine(statistics.FirstRewardSampleCount > 0
            ? $"Avg: time={averageTime:0.0}s, rewards={statistics.AverageRewards:0.00}, first reward={statistics.AverageFirstRewardTime:0.0}s"
            : $"Avg: time={averageTime:0.0}s, rewards={statistics.AverageRewards:0.00}, first reward=none");
    }

    private static void AppendAdStatistics(
        StringBuilder builder,
        WormBalanceScenarioStatistics statistics)
    {
        builder.AppendLine(
            $"Ads: avg={statistics.AverageAdsWatched:0.00}, p50={WormBalanceStatistics.Percentile(statistics.AdsWatched, 0.5f):0.0}, p90={WormBalanceStatistics.Percentile(statistics.AdsWatched, 0.9f):0.0}, sessions={statistics.AdSessionRate * 100f:0.0}% | uses: ad reroll={statistics.TotalAdRerolls / statistics.SampleCount:0.00}, take all={statistics.TotalTakeAllAds / statistics.SampleCount:0.00}, revive={statistics.TotalRevives / statistics.SampleCount:0.00}");
    }

    private static void AppendLossStatistics(
        StringBuilder builder,
        WormBalanceSimulationSettings settings,
        WormBalanceScenarioStatistics statistics)
    {
        builder.AppendLine(
            $"Loss tension: destroyed avg={WormBalanceStatistics.Average(statistics.LossProgress) * 100f:0.0}%, path avg={WormBalanceStatistics.Average(statistics.LossHeadProgress) * 100f:0.0}%");

        if (statistics.EndpointLossProgress.Count == 0)
            return;

        float endpointLossSamples = statistics.EndpointLossProgress.Count;
        float endpointProgressP10 = WormBalanceStatistics.Percentile(
            statistics.EndpointLossProgress,
            0.1f);
        float endpointProgressP50 = WormBalanceStatistics.Percentile(
            statistics.EndpointLossProgress,
            0.5f);
        float endpointProgressP90 = WormBalanceStatistics.Percentile(
            statistics.EndpointLossProgress,
            0.9f);
        float endpointSectionP50 = WormBalanceStatistics.Percentile(
            statistics.EndpointLossSections,
            0.5f);
        float averageEndpointSectionDamage = WormBalanceStatistics.Average(
            statistics.EndpointLossSectionDamage);
        builder.AppendLine(
            $"Endpoint losses: samples={statistics.EndpointLossProgress.Count}, " +
            $"destroyed p10/p50/p90={endpointProgressP10 * 100f:0.0}%/{endpointProgressP50 * 100f:0.0}%/{endpointProgressP90 * 100f:0.0}%, " +
            $"section p50={endpointSectionP50:0.0}/{settings.SectionCount}, " +
            $"current section damage avg={averageEndpointSectionDamage * 100f:0.0}%, " +
            $"rewards avg={statistics.TotalEndpointLossRewards / endpointLossSamples:0.00}, " +
            $"time avg={statistics.TotalEndpointLossTime / endpointLossSamples:0.0}s");
    }
}
