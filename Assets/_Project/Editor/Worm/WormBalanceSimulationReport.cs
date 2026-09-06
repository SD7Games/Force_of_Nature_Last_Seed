using System.Collections.Generic;
using System.Text;

internal sealed class WormBalanceSimulationReport
{
    private const float NoAdsTargetMinWinRate = 0.6f;
    private const float NoAdsTargetMaxWinRate = 0.7f;
    private const float ReviveOnlyTargetMinWinRate = 0.98f;
    private const float ReviveOnlyTargetMaxWinRate = 1f;
    private const float AdsNoReviveTargetMinWinRate = 0.85f;
    private const float AdsNoReviveTargetMaxWinRate = 0.9f;
    private const float FullAdsTargetMinWinRate = 0.98f;
    private const float FullAdsTargetMaxWinRate = 1f;

    private readonly WormBalanceSimulationSettings _settings;
    private readonly List<WormBalanceRunResult> _runs;

    public WormBalanceSimulationReport(
        WormBalanceSimulationSettings settings,
        List<WormBalanceRunResult> runs)
    {
        _settings = settings;
        _runs = runs ?? new List<WormBalanceRunResult>();
    }

    public IReadOnlyList<WormBalanceRunResult> Runs => _runs;

    public string BuildSummary()
    {
        StringBuilder builder = new();
        AppendHeader(builder);

        WormBalanceScenarioStatistics noAds = AppendScenarioIfIncluded(
            builder,
            WormBalanceScenario.NoAds,
            "No Ads / No Revive",
            NoAdsTargetMinWinRate,
            NoAdsTargetMaxWinRate);
        WormBalanceScenarioStatistics reviveOnly = AppendScenarioIfIncluded(
            builder,
            WormBalanceScenario.ReviveOnly,
            "Revive Only",
            ReviveOnlyTargetMinWinRate,
            ReviveOnlyTargetMaxWinRate);
        WormBalanceScenarioStatistics adsNoRevive = AppendScenarioIfIncluded(
            builder,
            WormBalanceScenario.AdsAssistNoRevive,
            "Ads Assist / No Revive",
            AdsNoReviveTargetMinWinRate,
            AdsNoReviveTargetMaxWinRate);
        WormBalanceScenarioStatistics fullAds = AppendScenarioIfIncluded(
            builder,
            WormBalanceScenario.AdsAssist,
            "Full Ads",
            FullAdsTargetMinWinRate,
            FullAdsTargetMaxWinRate);

        WormBalanceAssistanceSummaryFormatter.Append(
            builder,
            noAds,
            reviveOnly,
            adsNoRevive,
            fullAds);
        return builder.ToString();
    }

    private void AppendHeader(StringBuilder builder)
    {
        builder.AppendLine("Worm Balance Lab");
        builder.AppendLine($"Simulated games: {_runs.Count} ({_settings.RunCount} per scenario)");
        builder.AppendLine($"Targets: No Ads/No Revive {NoAdsTargetMinWinRate * 100f:0}-{NoAdsTargetMaxWinRate * 100f:0}% wins, Revive Only {ReviveOnlyTargetMinWinRate * 100f:0}-{ReviveOnlyTargetMaxWinRate * 100f:0}% wins, Ads No Revive {AdsNoReviveTargetMinWinRate * 100f:0}-{AdsNoReviveTargetMaxWinRate * 100f:0}% wins, Full Ads {FullAdsTargetMinWinRate * 100f:0}-{FullAdsTargetMaxWinRate * 100f:0}% wins");
        builder.AppendLine(
            $"Setup: reward={_settings.RewardPickStrategy}, ads={_settings.AdSimulationMode}, worm={_settings.SectionCount} sections / {WormPatternBuilder.GetBodySegmentCount(_settings.SectionCount)} body segments / {_settings.PathTimeLimitSeconds:0.0}s path");
        builder.AppendLine(
            $"Damage: estimated DPS x {_settings.HitEfficiency:0.00}, rollback={(_settings.ApplySectionRollback ? "ON" : "OFF")} speed={_settings.RollbackSpeed:0.0} forward x{_settings.SectionRollbackForwardSpeedMultiplier:0.00}, pressure={(_settings.UseRuntimePressure ? "ON" : "OFF")}");
        builder.AppendLine(
            $"Ad power: reroll rare+, legendary after {RewardAdRerollPolicy.LegendaryChanceMinDangerProgress * 100f:0}% danger, take all after {_settings.TakeAllMinHeadPathProgress * 100f:0}% path and {_settings.TakeAllMinTotalDpsGainRatio:0.00}x total DPS gain");
        builder.AppendLine($"Ad limits: free reroll={_settings.FreeRerollAttemptsPerSession}, ad reroll={_settings.AdRerollAttemptsPerSession}, take all={_settings.TakeAllAttemptsPerSession}, revive={_settings.ReviveAttemptsPerSession}");
        builder.AppendLine(_settings.SimulatePlayerXFollow
            ? "Player X follow: ON, instant head X match"
            : "Player X follow: OFF");
        builder.AppendLine();
    }

    private WormBalanceScenarioStatistics AppendScenarioIfIncluded(
        StringBuilder builder,
        WormBalanceScenario scenario,
        string title,
        float targetMinWinRate,
        float targetMaxWinRate)
    {
        if (!_settings.IncludesScenario(scenario))
            return null;

        WormBalanceScenarioStatistics statistics =
            WormBalanceScenarioStatistics.Collect(_runs, scenario);
        WormBalanceScenarioSummaryFormatter.Append(
            builder,
            _settings,
            statistics,
            title,
            targetMinWinRate,
            targetMaxWinRate);
        return statistics;
    }
}
