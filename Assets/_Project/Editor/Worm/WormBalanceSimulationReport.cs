using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

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

    public readonly WormBalanceSimulationSettings Settings;
    public readonly List<WormBalanceRunResult> Runs;

    public WormBalanceSimulationReport(
        WormBalanceSimulationSettings settings,
        List<WormBalanceRunResult> runs)
    {
        Settings = settings;
        Runs = runs ?? new List<WormBalanceRunResult>();
    }

    public string BuildSummary()
    {
        StringBuilder builder = new();
        builder.AppendLine("Worm Balance Lab");
        builder.AppendLine($"Simulated games: {Runs.Count} ({Settings.RunCount} per scenario)");
        builder.AppendLine($"Targets: No Ads/No Revive {NoAdsTargetMinWinRate * 100f:0}-{NoAdsTargetMaxWinRate * 100f:0}% wins, Revive Only {ReviveOnlyTargetMinWinRate * 100f:0}-{ReviveOnlyTargetMaxWinRate * 100f:0}% wins, Ads No Revive {AdsNoReviveTargetMinWinRate * 100f:0}-{AdsNoReviveTargetMaxWinRate * 100f:0}% wins, Full Ads {FullAdsTargetMinWinRate * 100f:0}-{FullAdsTargetMaxWinRate * 100f:0}% wins");
        builder.AppendLine(
            $"Setup: reward={Settings.RewardPickStrategy}, ads={Settings.AdSimulationMode}, worm={Settings.SectionCount} sections / {WormPatternBuilder.GetBodySegmentCount(Settings.SectionCount)} body segments / {Settings.PathTimeLimitSeconds:0.0}s path");
        builder.AppendLine(
            $"Damage: estimated DPS x {Settings.HitEfficiency:0.00}, rollback={(Settings.ApplySectionRollback ? "ON" : "OFF")} speed={Settings.RollbackSpeed:0.0} forward x{Settings.SectionRollbackForwardSpeedMultiplier:0.00}, pressure={(Settings.UseRuntimePressure ? "ON" : "OFF")}");
        builder.AppendLine(
            $"Ad power: reroll rare+, legendary after {RewardAdRerollPolicy.LegendaryChanceMinDangerProgress * 100f:0}% danger, take all after {Settings.TakeAllMinHeadPathProgress * 100f:0}% path and {Settings.TakeAllMinTotalDpsGainRatio:0.00}x total DPS gain");
        builder.AppendLine($"Ad limits: free reroll={Settings.FreeRerollAttemptsPerSession}, ad reroll={Settings.AdRerollAttemptsPerSession}, take all={Settings.TakeAllAttemptsPerSession}, revive={Settings.ReviveAttemptsPerSession}");
        builder.AppendLine(Settings.SimulatePlayerXFollow
            ? "Player X follow: ON, instant head X match"
            : "Player X follow: OFF");
        builder.AppendLine();

        if (Settings.IncludesScenario(WormBalanceScenario.NoAds))
            AppendScenarioSummary(builder, WormBalanceScenario.NoAds, "No Ads / No Revive", NoAdsTargetMinWinRate, NoAdsTargetMaxWinRate);

        if (Settings.IncludesScenario(WormBalanceScenario.ReviveOnly))
            AppendScenarioSummary(builder, WormBalanceScenario.ReviveOnly, "Revive Only", ReviveOnlyTargetMinWinRate, ReviveOnlyTargetMaxWinRate);

        if (Settings.IncludesScenario(WormBalanceScenario.AdsAssistNoRevive))
            AppendScenarioSummary(builder, WormBalanceScenario.AdsAssistNoRevive, "Ads Assist / No Revive", AdsNoReviveTargetMinWinRate, AdsNoReviveTargetMaxWinRate);

        if (Settings.IncludesScenario(WormBalanceScenario.AdsAssist))
            AppendScenarioSummary(builder, WormBalanceScenario.AdsAssist, "Full Ads", FullAdsTargetMinWinRate, FullAdsTargetMaxWinRate);

        AppendAssistanceUplift(builder);

        return builder.ToString();
    }

    private void AppendScenarioSummary(
        StringBuilder builder,
        WormBalanceScenario scenario,
        string title,
        float targetMinWinRate,
        float targetMaxWinRate)
    {
        int sampleCount = 0;
        int winCount = 0;
        int lossCount = 0;
        int firstRewardSamples = 0;
        int adSessionCount = 0;
        float totalTime = 0f;
        float totalRewards = 0f;
        float totalFirstRewardTime = 0f;
        float totalAdsWatched = 0f;
        float totalAdRerolls = 0f;
        float totalTakeAllAds = 0f;
        float totalRevives = 0f;
        float totalEndpointLossRewards = 0f;
        float totalEndpointLossTime = 0f;
        List<float> adsWatched = new();
        List<float> lossProgress = new();
        List<float> lossHeadProgress = new();
        List<float> endpointLossProgress = new();
        List<float> endpointLossSectionDamage = new();
        List<float> endpointLossSections = new();

        for (int i = 0; i < Runs.Count; i++)
        {
            WormBalanceRunResult run = Runs[i];

            if (run.Scenario != scenario)
                continue;

            sampleCount++;
            totalTime += run.TimeSeconds;
            totalRewards += run.RewardsTaken;
            totalAdsWatched += run.AdStats.AdsWatched;
            totalAdRerolls += run.AdStats.AdRerollsUsed;
            totalTakeAllAds += run.AdStats.TakeAllAdsUsed;
            totalRevives += run.AdStats.RevivesUsed;
            adsWatched.Add(run.AdStats.AdsWatched);

            if (run.AdStats.AdsWatched > 0)
                adSessionCount++;

            if (run.FirstRewardTime >= 0f)
            {
                totalFirstRewardTime += run.FirstRewardTime;
                firstRewardSamples++;
            }

            if (run.Won)
            {
                winCount++;
            }
            else
            {
                lossCount++;
                lossProgress.Add(run.DestructionProgress);
                lossHeadProgress.Add(run.HeadProgress);

                if (run.IsEndpointLoss)
                {
                    endpointLossProgress.Add(run.EndpointDestructionProgress);
                    endpointLossSectionDamage.Add(run.EndpointSectionDamageProgress);
                    endpointLossSections.Add(run.SectionsDestroyed + 1f);
                    totalEndpointLossRewards += run.RewardsTaken;
                    totalEndpointLossTime += run.TimeSeconds;
                }
            }
        }

        builder.AppendLine($"{title}:");

        if (sampleCount == 0)
        {
            builder.AppendLine("No samples.");
            builder.AppendLine();
            return;
        }

        float samples = sampleCount;
        float winRate = winCount / samples;
        float averageRewards = totalRewards / samples;
        float averageFirstRewardTime = firstRewardSamples > 0
            ? totalFirstRewardTime / firstRewardSamples
            : -1f;

        adsWatched.Sort();
        lossProgress.Sort();
        lossHeadProgress.Sort();
        endpointLossProgress.Sort();
        endpointLossSectionDamage.Sort();
        endpointLossSections.Sort();

        builder.AppendLine(
            $"Wins: {winCount}/{sampleCount} ({winRate * 100f:0.0}%) | Losses: {lossCount}/{sampleCount} ({(1f - winRate) * 100f:0.0}%) | Target: {targetMinWinRate * 100f:0}-{targetMaxWinRate * 100f:0}% | {GetWinRateVerdict(winRate, targetMinWinRate, targetMaxWinRate)}");
        builder.AppendLine(firstRewardSamples > 0
            ? $"Avg: time={totalTime / samples:0.0}s, rewards={averageRewards:0.00}, first reward={averageFirstRewardTime:0.0}s"
            : $"Avg: time={totalTime / samples:0.0}s, rewards={averageRewards:0.00}, first reward=none");

        if (scenario != WormBalanceScenario.NoAds)
        {
            builder.AppendLine(
                $"Ads: avg={totalAdsWatched / samples:0.00}, p50={Percentile(adsWatched, 0.5f):0.0}, p90={Percentile(adsWatched, 0.9f):0.0}, sessions={adSessionCount / samples * 100f:0.0}% | uses: ad reroll={totalAdRerolls / samples:0.00}, take all={totalTakeAllAds / samples:0.00}, revive={totalRevives / samples:0.00}");
        }

        if (lossCount > 0)
        {
            builder.AppendLine(
                $"Loss tension: destroyed avg={Average(lossProgress) * 100f:0.0}%, path avg={Average(lossHeadProgress) * 100f:0.0}%");

            if (endpointLossProgress.Count > 0)
            {
                float endpointLossSamples = endpointLossProgress.Count;
                float endpointProgressP10 = Percentile(endpointLossProgress, 0.1f);
                float endpointProgressP50 = Percentile(endpointLossProgress, 0.5f);
                float endpointProgressP90 = Percentile(endpointLossProgress, 0.9f);
                float endpointSectionP50 = Percentile(endpointLossSections, 0.5f);
                float averageEndpointSectionDamage = Average(endpointLossSectionDamage);
                builder.AppendLine(
                    $"Endpoint losses: samples={endpointLossProgress.Count}, " +
                    $"destroyed p10/p50/p90={endpointProgressP10 * 100f:0.0}%/{endpointProgressP50 * 100f:0.0}%/{endpointProgressP90 * 100f:0.0}%, " +
                    $"section p50={endpointSectionP50:0.0}/{Settings.SectionCount}, " +
                    $"current section damage avg={averageEndpointSectionDamage * 100f:0.0}%, " +
                    $"rewards avg={totalEndpointLossRewards / endpointLossSamples:0.00}, " +
                    $"time avg={totalEndpointLossTime / endpointLossSamples:0.0}s");
            }
        }

        string verdict = BuildCompactScenarioVerdict(
            scenario,
            winRate,
            targetMinWinRate,
            targetMaxWinRate,
            averageRewards,
            averageFirstRewardTime,
            totalAdsWatched / samples,
            adSessionCount / samples,
            lossCount,
            lossProgress,
            lossHeadProgress,
            endpointLossProgress);
        builder.AppendLine(
            $"Verdict: {verdict}");
        builder.AppendLine();
    }

    private void AppendAssistanceUplift(StringBuilder builder)
    {
        if (!TryGetWinRate(WormBalanceScenario.NoAds, out float noAdsWinRate))
            return;

        if (TryGetWinRate(WormBalanceScenario.ReviveOnly, out float reviveOnlyWinRate))
        {
            builder.AppendLine(
                $"Revive rescue uplift: +{(reviveOnlyWinRate - noAdsWinRate) * 100f:0.0} pp | target: revive should convert most endpoint losses into wins");
        }

        if (TryGetWinRate(WormBalanceScenario.AdsAssistNoRevive, out float adsNoReviveWinRate))
        {
            builder.AppendLine(
                $"Paid assist uplift without revive: +{(adsNoReviveWinRate - noAdsWinRate) * 100f:0.0} pp | target: paid reroll/take-all should help, not replace revive");
        }

        if (TryGetWinRate(WormBalanceScenario.AdsAssist, out float fullAdsWinRate))
        {
            builder.AppendLine(
                $"Full ads uplift: +{(fullAdsWinRate - noAdsWinRate) * 100f:0.0} pp | target: full assist should feel like a near-guaranteed save");
        }
    }

    private bool TryGetWinRate(
        WormBalanceScenario scenario,
        out float winRate)
    {
        int sampleCount = 0;
        int winCount = 0;

        for (int i = 0; i < Runs.Count; i++)
        {
            WormBalanceRunResult run = Runs[i];

            if (run.Scenario != scenario)
                continue;

            sampleCount++;

            if (run.Won)
                winCount++;
        }

        if (sampleCount <= 0)
        {
            winRate = 0f;
            return false;
        }

        winRate = winCount / (float)sampleCount;
        return true;
    }

    private static string GetWinRateVerdict(
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

    private static string BuildCompactScenarioVerdict(
        WormBalanceScenario scenario,
        float winRate,
        float targetMinWinRate,
        float targetMaxWinRate,
        float averageRewards,
        float averageFirstRewardTime,
        float averageAdsWatched,
        float adSessionRate,
        int lossCount,
        List<float> lossProgress,
        List<float> lossHeadProgress,
        List<float> endpointLossProgress)
    {
        List<string> notes = new();

        if (winRate < targetMinWinRate)
            notes.Add(scenario switch
            {
                WormBalanceScenario.NoAds => "baseline is too harsh before revive",
                WormBalanceScenario.ReviveOnly => "revive does not reliably save the run",
                WormBalanceScenario.AdsAssistNoRevive => "paid assist without revive is under target",
                WormBalanceScenario.AdsAssist => "full ad assist is under target",
                _ => "win rate is under target"
            });
        else if (winRate > targetMaxWinRate)
            notes.Add(scenario switch
            {
                WormBalanceScenario.NoAds => "pre-revive pressure is too weak",
                WormBalanceScenario.ReviveOnly => "revive save rate is at cap",
                WormBalanceScenario.AdsAssistNoRevive => "paid assist without revive is too strong",
                WormBalanceScenario.AdsAssist => "full ad assist is at cap",
                _ => "win rate is above target"
            });
        else
            notes.Add("win rate is in target");

        if (averageFirstRewardTime < 0f)
            notes.Add("first reward is unreachable");
        else if (averageFirstRewardTime > 12f)
            notes.Add("first reward is late");
        else if (averageFirstRewardTime <= 6f)
            notes.Add("first reward is very early");

        if (averageRewards > 22f)
            notes.Add("reward count is high");
        else if (averageRewards < 10f)
            notes.Add("reward count is low");

        if (scenario != WormBalanceScenario.NoAds)
        {
            if (averageAdsWatched > 2f)
                notes.Add("ads are frequent");
            else if (averageAdsWatched < 0.6f || adSessionRate < 0.5f)
                notes.Add("ads may feel underused");
        }

        if (lossCount > 0)
        {
            List<float> endpointProgressForVerdict = endpointLossProgress != null && endpointLossProgress.Count > 0
                ? endpointLossProgress
                : lossProgress;
            float averageLossDestroyed = Average(endpointProgressForVerdict);
            float averageLossPath = Average(lossHeadProgress);

            if (averageLossPath >= 0.95f &&
                averageLossDestroyed >= 0.7f &&
                averageLossDestroyed <= 0.82f)
            {
                notes.Add("endpoint pressure is in the revive offer zone");
            }
            else if (averageLossPath >= 0.95f && averageLossDestroyed < 0.65f)
            {
                notes.Add("endpoint catches too early; lower mid HP or rollback pressure");
            }
            else if (averageLossPath >= 0.95f && averageLossDestroyed > 0.85f)
            {
                notes.Add("losses are too close; increase path pressure or reduce rollback relief");
            }
        }

        return string.Join("; ", notes);
    }

    private static void AppendScenarioVerdict(
        StringBuilder builder,
        WormBalanceScenario scenario,
        float winRate,
        float targetMinWinRate,
        float targetMaxWinRate,
        float averageRewards,
        float averageFirstRewardTime,
        float averageAdsWatched,
        float adSessionRate,
        int lossCount,
        List<float> lossProgress,
        List<float> lossHeadProgress)
    {
        builder.AppendLine("Verdict:");

        if (winRate >= targetMinWinRate && winRate <= targetMaxWinRate)
        {
            builder.AppendLine("- Win rate is inside the target tension band.");
        }
        else if (winRate < targetMinWinRate)
        {
            builder.AppendLine("- Too hard for this scenario. Lower mid/end HP or increase reward pressure.");

            if (scenario == WormBalanceScenario.NoAds && winRate < 0.45f)
                builder.AppendLine("- No-ads win rate is dangerously low. Player may read this as a paywall.");
        }
        else
        {
            builder.AppendLine("- Too easy for this scenario. Raise mid/end HP or delay paid-help power.");
        }

        if (averageFirstRewardTime < 0f)
            builder.AppendLine("- Player does not reach the first reward. Lower early HP heavily.");
        else if (averageFirstRewardTime > 12f)
            builder.AppendLine("- First reward is late. Lower only early HP.");
        else if (averageFirstRewardTime < 6f)
            builder.AppendLine("- First reward is very fast. Early hook is strong.");
        else
            builder.AppendLine("- First reward timing is good.");

        if (averageRewards < 10f)
            builder.AppendLine("- Too few rewards for a fun survival curve. Early/mid HP is probably too high.");
        else if (averageRewards > 22f)
            builder.AppendLine("- Reward count is very high. Watch for runaway DPS spikes.");
        else
            builder.AppendLine("- Reward count is in a usable range.");

        if (scenario != WormBalanceScenario.NoAds)
        {
            if (averageAdsWatched < 0.6f || adSessionRate < 0.5f)
                builder.AppendLine("- Ads are underused. The player may not feel the revive/take-all offer often enough.");
            else if (averageAdsWatched > 2f)
                builder.AppendLine("- Ads are too frequent. Lower paid attempts or make take-all stricter.");
            else
                builder.AppendLine("- Ad pressure is in the intended 1-ish view per session zone.");
        }

        if (lossCount <= 0)
            return;

        float averageLossDestroyed = Average(lossProgress);
        float averageLossPath = Average(lossHeadProgress);

        if (averageLossPath >= 0.95f &&
            averageLossDestroyed >= 0.7f &&
            averageLossDestroyed <= 0.82f)
        {
            builder.AppendLine("- Losses happen at endpoint with 70-80% worm destroyed. This is the intended revive offer zone.");
        }
        else if (averageLossPath >= 0.95f && averageLossDestroyed < 0.65f)
        {
            builder.AppendLine("- Endpoint catches too early. Lower mid HP or reduce path pressure.");
        }
        else if (averageLossPath >= 0.95f && averageLossDestroyed > 0.85f)
        {
            builder.AppendLine("- Losses are too close. Increase path pressure or reduce rollback relief.");
        }
    }

    private static void AppendBalanceVerdict(
        StringBuilder builder,
        float winRate,
        float averageRewards,
        float averageFirstRewardTime,
        int lossCount,
        List<float> lossProgress,
        List<float> lossHeadProgress)
    {
        float averageLossDestroyed = Average(lossProgress);
        float averageLossPath = Average(lossHeadProgress);

        builder.AppendLine("Verdict:");

        if (averageFirstRewardTime < 0f)
        {
            builder.AppendLine("- Player does not reach the first reward. Lower early HP heavily.");
        }
        else if (averageFirstRewardTime > 12f)
        {
            builder.AppendLine(
                $"- First reward is late ({averageFirstRewardTime:0.0}s). Lower only early HP, keep mid/end pressure.");
        }
        else if (averageFirstRewardTime < 6f)
        {
            builder.AppendLine(
                $"- First reward is very fast ({averageFirstRewardTime:0.0}s). Early hook is strong enough.");
        }
        else
        {
            builder.AppendLine(
                $"- First reward timing is good ({averageFirstRewardTime:0.0}s).");
        }

        if (averageRewards < 3f)
        {
            builder.AppendLine(
                $"- Too few rewards before loss ({averageRewards:0.00}). Early/mid HP is too high for fun pacing.");
        }
        else if (averageRewards > 7f)
        {
            builder.AppendLine(
                $"- Many rewards before loss ({averageRewards:0.00}). Raise mid/end HP if win rate grows.");
        }
        else
        {
            builder.AppendLine(
                $"- Reward count before loss is in a useful range ({averageRewards:0.00}).");
        }

        if (winRate <= 0.1f)
        {
            builder.AppendLine(
                $"- Loss target is strong ({winRate * 100f:0.0}% wins). Keep this if revive/ad continuation is the goal.");
        }
        else
        {
            builder.AppendLine(
                $"- Too many wins ({winRate * 100f:0.0}%). Raise mid/end HP, not early HP.");
        }

        if (lossCount > 0)
        {
            builder.AppendLine(
                $"- Loss happens at path {averageLossPath * 100f:0.0}% with worm destroyed {averageLossDestroyed * 100f:0.0}%.");

            if (averageLossPath >= 0.95f && averageLossDestroyed < 0.9f)
                builder.AppendLine("- Player reaches the endpoint with a lot of worm left. Make early rewards faster, then retest.");
            else if (averageLossPath >= 0.85f && averageLossDestroyed >= 0.85f)
                builder.AppendLine("- Loss is late and close. This is a good tension zone.");
        }

        builder.AppendLine("Recommended first tuning:");
        builder.AppendLine("- Set early Base Section HP around 3-4.");
        builder.AppendLine("- Set target lifetime curve early keys around 1.1-1.6s, first reward target 8-12s.");
        builder.AppendLine("- If wins appear after that, raise only mid/end lifetime or pressure curve.");
    }

    private void AppendWorstLosses(StringBuilder builder)
    {
        int appended = 0;
        HashSet<int> listedRuns = new();

        for (int i = 0; i < Runs.Count && appended < 8; i++)
        {
            WormBalanceRunResult worst = null;

            for (int j = 0; j < Runs.Count; j++)
            {
                WormBalanceRunResult candidate = Runs[j];

                if (candidate.Won)
                    continue;

                if (listedRuns.Contains(candidate.RunIndex))
                    continue;

                if (worst == null || candidate.DestructionProgress < worst.DestructionProgress)
                    worst = candidate;
            }

            if (worst == null)
                break;

            builder.AppendLine(worst.BuildDebugLine());
            listedRuns.Add(worst.RunIndex);
            appended++;
        }

        if (appended == 0)
            builder.AppendLine("No losses.");
    }

    private void AppendLocationDistribution(
        StringBuilder builder,
        string title,
        bool won)
    {
        int total = 0;
        Dictionary<int, int> bucketCounts = new();
        Dictionary<int, int> controlPointCounts = new();

        for (int i = 0; i < Runs.Count; i++)
        {
            WormBalanceRunResult run = Runs[i];

            if (run.Won != won)
                continue;

            total++;
            Increment(bucketCounts, run.EndLocation.BucketIndex);
            Increment(controlPointCounts, run.EndLocation.ControlPointIndex);
        }

        builder.AppendLine($"{title}:");

        if (total == 0)
        {
            builder.AppendLine("No samples.");
            return;
        }

        builder.Append("Path buckets: ");
        AppendBucketCounts(
            builder,
            bucketCounts,
            total,
            Settings.ProgressBucketCount);
        builder.AppendLine();

        if (Settings.PathMetrics.ControlPointCount <= 0)
        {
            builder.AppendLine("Rail control points: no RailPath assigned.");
            return;
        }

        builder.Append("Rail control points: ");
        AppendControlPointCounts(
            builder,
            controlPointCounts,
            total,
            Settings.PathMetrics.ControlPointCount);
        builder.AppendLine();
    }

    private static void AppendBucketCounts(
        StringBuilder builder,
        Dictionary<int, int> counts,
        int total,
        int bucketCount)
    {
        bool wroteAny = false;

        for (int i = 0; i < bucketCount; i++)
        {
            if (!counts.TryGetValue(i, out int count))
                continue;

            if (wroteAny)
                builder.Append("; ");

            float start = i / (float)Mathf.Max(1, bucketCount) * 100f;
            float end = (i + 1) / (float)Mathf.Max(1, bucketCount) * 100f;
            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0:0}-{1:0}%: {2} ({3:0.0}%)",
                start,
                end,
                count,
                count / (float)total * 100f);
            wroteAny = true;
        }

        if (!wroteAny)
            builder.Append("none");
    }

    private static void AppendControlPointCounts(
        StringBuilder builder,
        Dictionary<int, int> counts,
        int total,
        int controlPointCount)
    {
        bool wroteAny = false;

        for (int i = 0; i < controlPointCount; i++)
        {
            if (!counts.TryGetValue(i, out int count))
                continue;

            if (wroteAny)
                builder.Append("; ");

            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "CP {0}: {1} ({2:0.0}%)",
                i,
                count,
                count / (float)total * 100f);
            wroteAny = true;
        }

        if (!wroteAny)
            builder.Append("none");
    }

    private static void Increment(Dictionary<int, int> counts, int key)
    {
        if (counts.TryGetValue(key, out int value))
        {
            counts[key] = value + 1;
            return;
        }

        counts.Add(key, 1);
    }

    private void AppendSampleRuns(StringBuilder builder)
    {
        int count = Mathf.Min(8, Runs.Count);

        for (int i = 0; i < count; i++)
            builder.AppendLine(Runs[i].BuildDebugLine());
    }

    private static float Average(List<float> values)
    {
        if (values == null || values.Count == 0)
            return 0f;

        float total = 0f;

        for (int i = 0; i < values.Count; i++)
            total += values[i];

        return total / values.Count;
    }

    private static float Percentile(List<float> sortedValues, float percentile)
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
