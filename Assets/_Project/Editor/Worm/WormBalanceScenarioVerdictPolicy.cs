using System.Collections.Generic;

internal static class WormBalanceScenarioVerdictPolicy
{
    private const float LateFirstRewardSeconds = 12f;
    private const float VeryEarlyFirstRewardSeconds = 6f;
    private const float HighRewardCount = 22f;
    private const float LowRewardCount = 10f;
    private const float FrequentAdsPerSession = 2f;
    private const float UnderusedAdsPerSession = 0.6f;
    private const float UnderusedAdSessionRate = 0.5f;
    private const float EndpointPathThreshold = 0.95f;
    private const float EarlyEndpointDestructionThreshold = 0.65f;
    private const float ReviveZoneMinDestruction = 0.7f;
    private const float ReviveZoneMaxDestruction = 0.82f;
    private const float CloseLossDestructionThreshold = 0.85f;

    public static string GetWinRateVerdict(
        float winRate,
        float targetMinWinRate,
        float targetMaxWinRate)
    {
        if (winRate < targetMinWinRate)
            return "too hard";

        if (winRate > targetMaxWinRate)
            return "too easy";

        return "OK";
    }

    public static string Build(
        WormBalanceScenarioStatistics statistics,
        float targetMinWinRate,
        float targetMaxWinRate)
    {
        List<string> notes = new();
        AppendWinRateVerdict(notes, statistics, targetMinWinRate, targetMaxWinRate);
        AppendRewardVerdict(notes, statistics);
        AppendAdVerdict(notes, statistics);
        AppendLossVerdict(notes, statistics);
        return string.Join("; ", notes);
    }

    private static void AppendWinRateVerdict(
        List<string> notes,
        WormBalanceScenarioStatistics statistics,
        float targetMinWinRate,
        float targetMaxWinRate)
    {
        if (statistics.WinRate < targetMinWinRate)
        {
            notes.Add(statistics.Scenario switch
            {
                WormBalanceScenario.NoAds => "baseline is too harsh before revive",
                WormBalanceScenario.ReviveOnly => "revive does not reliably save the run",
                WormBalanceScenario.AdsAssistNoRevive => "paid assist without revive is under target",
                WormBalanceScenario.AdsAssist => "full ad assist is under target",
                _ => "win rate is under target"
            });
            return;
        }

        if (statistics.WinRate > targetMaxWinRate)
        {
            notes.Add(statistics.Scenario switch
            {
                WormBalanceScenario.NoAds => "pre-revive pressure is too weak",
                WormBalanceScenario.ReviveOnly => "revive save rate is at cap",
                WormBalanceScenario.AdsAssistNoRevive => "paid assist without revive is too strong",
                WormBalanceScenario.AdsAssist => "full ad assist is at cap",
                _ => "win rate is above target"
            });
            return;
        }

        notes.Add("win rate is in target");
    }

    private static void AppendRewardVerdict(
        List<string> notes,
        WormBalanceScenarioStatistics statistics)
    {
        if (statistics.AverageFirstRewardTime < 0f)
            notes.Add("first reward is unreachable");
        else if (statistics.AverageFirstRewardTime > LateFirstRewardSeconds)
            notes.Add("first reward is late");
        else if (statistics.AverageFirstRewardTime <= VeryEarlyFirstRewardSeconds)
            notes.Add("first reward is very early");

        if (statistics.AverageRewards > HighRewardCount)
            notes.Add("reward count is high");
        else if (statistics.AverageRewards < LowRewardCount)
            notes.Add("reward count is low");
    }

    private static void AppendAdVerdict(
        List<string> notes,
        WormBalanceScenarioStatistics statistics)
    {
        if (statistics.Scenario == WormBalanceScenario.NoAds)
            return;

        if (statistics.AverageAdsWatched > FrequentAdsPerSession)
            notes.Add("ads are frequent");
        else if (statistics.AverageAdsWatched < UnderusedAdsPerSession ||
                 statistics.AdSessionRate < UnderusedAdSessionRate)
            notes.Add("ads may feel underused");
    }

    private static void AppendLossVerdict(
        List<string> notes,
        WormBalanceScenarioStatistics statistics)
    {
        if (statistics.LossCount <= 0)
            return;

        IReadOnlyList<float> destructionProgress = statistics.EndpointLossProgress.Count > 0
            ? statistics.EndpointLossProgress
            : statistics.LossProgress;
        float averageLossDestroyed = WormBalanceStatistics.Average(destructionProgress);
        float averageLossPath = WormBalanceStatistics.Average(statistics.LossHeadProgress);

        if (averageLossPath >= EndpointPathThreshold &&
            averageLossDestroyed >= ReviveZoneMinDestruction &&
            averageLossDestroyed <= ReviveZoneMaxDestruction)
        {
            notes.Add("endpoint pressure is in the revive offer zone");
        }
        else if (averageLossPath >= EndpointPathThreshold &&
                 averageLossDestroyed < EarlyEndpointDestructionThreshold)
        {
            notes.Add("endpoint catches too early; lower mid HP or rollback pressure");
        }
        else if (averageLossPath >= EndpointPathThreshold &&
                 averageLossDestroyed > CloseLossDestructionThreshold)
        {
            notes.Add("losses are too close; increase path pressure or reduce rollback relief");
        }
    }
}
