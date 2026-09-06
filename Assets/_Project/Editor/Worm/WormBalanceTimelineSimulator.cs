using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

internal static class WormBalanceTimelineSimulator
{
    public static bool AdvanceTime(
        WormBalanceSimulationSettings settings,
        float duration,
        ref float time,
        ref float pressureElapsedTime,
        ref float headProgress,
        ref float pressureSampleTimer,
        ref float runtimePressureMultiplier,
        ref bool pressureChanged,
        ref float playerX,
        ref float maxPlayerXError)
    {
        float remaining = Mathf.Max(0f, duration);
        int maximumStepCount = 1;

        if (settings.UseRuntimePressure && settings.PressureConfig != null)
        {
            float minimumStep = Mathf.Max(
                0.0001f,
                settings.PressureConfig.SampleInterval);
            maximumStepCount = Mathf.CeilToInt(remaining / minimumStep) + 1;
        }

        for (int stepIndex = 0;
             stepIndex < maximumStepCount && remaining > 0f;
             stepIndex++)
        {
            float step = remaining;

            if (settings.UseRuntimePressure && settings.PressureConfig != null)
            {
                float timeToPressureSample = Mathf.Max(
                    0.0001f,
                    settings.PressureConfig.SampleInterval - pressureSampleTimer);
                step = Mathf.Min(step, timeToPressureSample);
            }

            if (settings.PathTimeLimitSeconds > 0f)
            {
                float timeToPathEnd = (1f - headProgress) * settings.PathTimeLimitSeconds;

                if (step >= timeToPathEnd)
                {
                    time += Mathf.Max(0f, timeToPathEnd);
                    headProgress = 1f;
                    AlignPlayerXWithHead(settings, ref playerX, headProgress, ref maxPlayerXError);
                    return false;
                }
            }

            time += step;
            pressureElapsedTime += step;
            headProgress = Mathf.Clamp01(headProgress + step / settings.PathTimeLimitSeconds);
            AlignPlayerXWithHead(settings, ref playerX, headProgress, ref maxPlayerXError);
            remaining -= step;

            if (!settings.UseRuntimePressure || settings.PressureConfig == null)
                continue;

            pressureSampleTimer += step;

            if (pressureSampleTimer + Mathf.Epsilon < settings.PressureConfig.SampleInterval)
                continue;

            pressureSampleTimer = 0f;
            float nextPressure = CalculateRuntimePressure(
                settings.PressureConfig,
                pressureElapsedTime,
                headProgress,
                runtimePressureMultiplier);

            if (Mathf.Approximately(nextPressure, runtimePressureMultiplier))
                continue;

            runtimePressureMultiplier = nextPressure;
            pressureChanged = true;
        }

        return true;
    }

    public static void AlignPlayerXWithHead(
        WormBalanceSimulationSettings settings,
        ref float playerX,
        float headProgress,
        ref float maxPlayerXError)
    {
        if (!settings.SimulatePlayerXFollow)
            return;

        float headX = settings.PathMetrics.GetHeadX(headProgress);
        playerX = headX;
        maxPlayerXError = Mathf.Max(maxPlayerXError, Mathf.Abs(playerX - headX));
    }

    private static float CalculateRuntimePressure(
        WormPressureConfig config,
        float elapsedTime,
        float headProgress,
        float currentPressure)
    {
        float expectedProgress = config.GetExpectedProgress(elapsedTime);
        float deadZone = config.ProgressDeadZone;

        if (headProgress + deadZone < expectedProgress)
            return Mathf.Min(config.MaxMultiplier, currentPressure + config.IncreasePerSample);

        if (headProgress > expectedProgress + deadZone)
            return Mathf.Max(1f, currentPressure - config.RecoveryPerSample);

        return currentPressure;
    }

    public static bool TryUseRevive(
        WormBalanceSimulationSettings settings,
        WormBalanceAdSessionState adSession,
        ref bool hasRevivedThisRun,
        ref float headProgress,
        ref float pressureElapsedTime,
        ref float pressureSampleTimer,
        ref float runtimePressureMultiplier,
        ref bool pressureChanged,
        ref float playerX,
        ref float maxPlayerXError)
    {
        if (adSession == null || !adSession.TryUseRevive())
            return false;

        hasRevivedThisRun = true;
        headProgress = settings.ReviveRollbackProgress;
        pressureElapsedTime = settings.PressureConfig != null
            ? settings.PressureConfig.GetElapsedTimeForExpectedProgress(headProgress)
            : 0f;
        pressureSampleTimer = 0f;
        runtimePressureMultiplier = 1f;
        pressureChanged = true;
        AlignPlayerXWithHead(settings, ref playerX, headProgress, ref maxPlayerXError);
        return true;
    }

}
