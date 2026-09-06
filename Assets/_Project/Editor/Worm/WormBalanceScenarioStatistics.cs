using System.Collections.Generic;

internal sealed class WormBalanceScenarioStatistics
{
    private readonly List<float> _adsWatched = new();
    private readonly List<float> _lossProgress = new();
    private readonly List<float> _lossHeadProgress = new();
    private readonly List<float> _endpointLossProgress = new();
    private readonly List<float> _endpointLossSectionDamage = new();
    private readonly List<float> _endpointLossSections = new();

    private WormBalanceScenarioStatistics(WormBalanceScenario scenario)
    {
        Scenario = scenario;
    }

    public WormBalanceScenario Scenario { get; }
    public int SampleCount { get; private set; }
    public int WinCount { get; private set; }
    public int LossCount { get; private set; }
    public int FirstRewardSampleCount { get; private set; }
    public int AdSessionCount { get; private set; }
    public float TotalTime { get; private set; }
    public float TotalRewards { get; private set; }
    public float TotalFirstRewardTime { get; private set; }
    public float TotalAdsWatched { get; private set; }
    public float TotalAdRerolls { get; private set; }
    public float TotalTakeAllAds { get; private set; }
    public float TotalRevives { get; private set; }
    public float TotalEndpointLossRewards { get; private set; }
    public float TotalEndpointLossTime { get; private set; }
    public IReadOnlyList<float> AdsWatched => _adsWatched;
    public IReadOnlyList<float> LossProgress => _lossProgress;
    public IReadOnlyList<float> LossHeadProgress => _lossHeadProgress;
    public IReadOnlyList<float> EndpointLossProgress => _endpointLossProgress;
    public IReadOnlyList<float> EndpointLossSectionDamage => _endpointLossSectionDamage;
    public IReadOnlyList<float> EndpointLossSections => _endpointLossSections;
    public float WinRate => SampleCount > 0 ? WinCount / (float)SampleCount : 0f;
    public float AverageRewards => SampleCount > 0 ? TotalRewards / SampleCount : 0f;
    public float AverageFirstRewardTime => FirstRewardSampleCount > 0
        ? TotalFirstRewardTime / FirstRewardSampleCount
        : -1f;
    public float AverageAdsWatched => SampleCount > 0 ? TotalAdsWatched / SampleCount : 0f;
    public float AdSessionRate => SampleCount > 0 ? AdSessionCount / (float)SampleCount : 0f;

    public static WormBalanceScenarioStatistics Collect(
        IReadOnlyList<WormBalanceRunResult> runs,
        WormBalanceScenario scenario)
    {
        WormBalanceScenarioStatistics statistics = new(scenario);

        if (runs == null)
            return statistics;

        for (int i = 0; i < runs.Count; i++)
        {
            WormBalanceRunResult run = runs[i];

            if (run.Scenario == scenario)
                statistics.Add(run);
        }

        statistics.SortSamples();
        return statistics;
    }

    private void Add(WormBalanceRunResult run)
    {
        SampleCount++;
        TotalTime += run.TimeSeconds;
        TotalRewards += run.RewardsTaken;
        TotalAdsWatched += run.AdStats.AdsWatched;
        TotalAdRerolls += run.AdStats.AdRerollsUsed;
        TotalTakeAllAds += run.AdStats.TakeAllAdsUsed;
        TotalRevives += run.AdStats.RevivesUsed;
        _adsWatched.Add(run.AdStats.AdsWatched);

        if (run.AdStats.AdsWatched > 0)
            AdSessionCount++;

        if (run.FirstRewardTime >= 0f)
        {
            TotalFirstRewardTime += run.FirstRewardTime;
            FirstRewardSampleCount++;
        }

        if (run.Won)
        {
            WinCount++;
            return;
        }

        LossCount++;
        _lossProgress.Add(run.DestructionProgress);
        _lossHeadProgress.Add(run.HeadProgress);

        if (!run.IsEndpointLoss)
            return;

        _endpointLossProgress.Add(run.EndpointDestructionProgress);
        _endpointLossSectionDamage.Add(run.EndpointSectionDamageProgress);
        _endpointLossSections.Add(run.SectionsDestroyed + 1f);
        TotalEndpointLossRewards += run.RewardsTaken;
        TotalEndpointLossTime += run.TimeSeconds;
    }

    private void SortSamples()
    {
        _adsWatched.Sort();
        _lossProgress.Sort();
        _lossHeadProgress.Sort();
        _endpointLossProgress.Sort();
        _endpointLossSectionDamage.Sort();
        _endpointLossSections.Sort();
    }
}
