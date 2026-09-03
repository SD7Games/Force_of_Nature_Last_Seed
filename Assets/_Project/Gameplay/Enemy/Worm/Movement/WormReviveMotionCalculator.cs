using System;

public sealed class WormReviveMotionCalculator
{
    private const float MinimumDistance = 0.01f;
    private const float MinimumDuration = 0.01f;
    private const float MinimumGameplaySpeed = 0.01f;
    private const float MinimumDecelerationDistance = 0.001f;
    private const float TravelStretchX = 0.06f;
    private const float TravelStretchY = 0.04f;
    private const float BackEaseOvershoot = 1.70158f;

    public float CalculateCruiseSpeed(
        float rollbackDistance,
        float throwDuration,
        float decelerationPathFraction,
        float gameplaySpeed)
    {
        float distance = Math.Max(MinimumDistance, rollbackDistance);
        float duration = Math.Max(MinimumDuration, throwDuration);
        float decelerationFraction = Clamp01(decelerationPathFraction);
        float safeGameplaySpeed = Math.Max(MinimumGameplaySpeed, gameplaySpeed);

        if (decelerationFraction <= 0f)
            return Math.Max(safeGameplaySpeed, distance / duration);

        float fastDistance = distance * (1f - decelerationFraction);
        float weightedDistance = distance * (1f + decelerationFraction);
        float durationSpeed = duration * safeGameplaySpeed;
        float difference = durationSpeed - weightedDistance;
        float root = (float)Math.Sqrt(
            difference * difference
            + 4f * duration * fastDistance * safeGameplaySpeed);
        float cruiseSpeed = (weightedDistance - durationSpeed + root) / (2f * duration);

        return Math.Max(safeGameplaySpeed, cruiseSpeed);
    }

    public float CalculateThrowSpeed(
        float remainingDistance,
        float rollbackDistance,
        float cruiseSpeed,
        float decelerationPathFraction,
        float gameplaySpeed)
    {
        float decelerationDistance = rollbackDistance * Clamp01(decelerationPathFraction);

        if (decelerationDistance <= MinimumDecelerationDistance
            || remainingDistance > decelerationDistance)
        {
            return cruiseSpeed;
        }

        float slowdownProgress = 1f - Clamp01(remainingDistance / decelerationDistance);
        float eased = SmootherStep(slowdownProgress);
        float safeGameplaySpeed = Math.Max(MinimumGameplaySpeed, gameplaySpeed);
        return Lerp(cruiseSpeed, safeGameplaySpeed, eased);
    }

    public WormScale2 CalculateTravelScale(
        float normalizedTime,
        float squashXScale,
        float squashYScale)
    {
        float settle = EaseOutCubic(normalizedTime);
        float stretch = (float)Math.Sin(Clamp01(normalizedTime) * Math.PI);

        return new WormScale2(
            LerpUnclamped(squashXScale, 1f, settle) + stretch * TravelStretchX,
            LerpUnclamped(squashYScale, 1f, settle) - stretch * TravelStretchY);
    }

    public float EaseOutCubic(float value)
    {
        float inverse = 1f - Clamp01(value);
        return 1f - inverse * inverse * inverse;
    }

    public float EaseOutBack(float value)
    {
        float time = Clamp01(value) - 1f;
        return 1f
            + (BackEaseOvershoot + 1f) * time * time * time
            + BackEaseOvershoot * time * time;
    }

    private static float SmootherStep(float value)
    {
        float time = Clamp01(value);
        return time * time * time * (time * (time * 6f - 15f) + 10f);
    }

    private static float Clamp01(float value)
    {
        return Math.Max(0f, Math.Min(1f, value));
    }

    private static float Lerp(float from, float to, float time)
    {
        return from + (to - from) * Clamp01(time);
    }

    private static float LerpUnclamped(float from, float to, float time)
    {
        return from + (to - from) * time;
    }
}
