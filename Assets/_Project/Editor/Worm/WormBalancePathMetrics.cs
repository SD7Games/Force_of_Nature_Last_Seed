using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

internal sealed class WormBalancePathMetrics
{
    private readonly float[] _controlPointProgresses;
    private readonly int _progressBucketCount;
    private readonly RailPath _railPath;

    private WormBalancePathMetrics(
        float pathLength,
        float pathTimeLimitSeconds,
        float[] controlPointProgresses,
        int progressBucketCount,
        RailPath railPath)
    {
        PathLength = Mathf.Max(0f, pathLength);
        PathTimeLimitSeconds = Mathf.Max(1f, pathTimeLimitSeconds);
        _controlPointProgresses = controlPointProgresses ?? Array.Empty<float>();
        _progressBucketCount = Mathf.Clamp(progressBucketCount, 2, 20);
        _railPath = railPath;
    }

    public float PathLength { get; }
    public float PathTimeLimitSeconds { get; }
    public int ControlPointCount => _controlPointProgresses.Length;

    public static WormBalancePathMetrics FromRailPath(
        RailPath railPath,
        float fallbackPathTimeLimitSeconds,
        bool derivePathTimeFromRail,
        float wormSpeed,
        int progressBucketCount)
    {
        if (railPath == null)
            return CreateFallback(fallbackPathTimeLimitSeconds, wormSpeed, progressBucketCount);

        int pointCount = Mathf.Max(0, railPath.PointCount);

        if (pointCount <= 0)
            pointCount = Mathf.Max(0, railPath.LegacyWaypointCount);

        if (pointCount <= 0)
            pointCount = Mathf.Max(0, railPath.transform.childCount);

        float[] controlPointProgresses = pointCount > 0
            ? new float[pointCount]
            : Array.Empty<float>();

        float totalLength = Mathf.Max(0f, railPath.TotalLength);

        for (int i = 0; i < pointCount; i++)
        {
            if (!TryGetControlPointDistance(
                    railPath,
                    i,
                    out float distance))
            {
                controlPointProgresses[i] = pointCount > 1
                    ? i / (float)(pointCount - 1)
                    : 0f;
                continue;
            }

            totalLength = Mathf.Max(totalLength, railPath.TotalLength);
            controlPointProgresses[i] = totalLength > 0f
                ? Mathf.Clamp01(distance / totalLength)
                : (pointCount > 1 ? i / (float)(pointCount - 1) : 0f);
        }

        float pathTime = derivePathTimeFromRail && totalLength > 0f
            ? totalLength / Mathf.Max(0.01f, wormSpeed)
            : fallbackPathTimeLimitSeconds;

        return new WormBalancePathMetrics(
            totalLength,
            pathTime,
            controlPointProgresses,
            progressBucketCount,
            railPath);
    }

    private static bool TryGetControlPointDistance(
        RailPath railPath,
        int pointIndex,
        out float distance)
    {
        distance = 0f;

        if (railPath == null)
            return false;

        if (railPath.TryGetControlPointDistance(pointIndex, out distance))
            return true;

        if (pointIndex < 0 || pointIndex >= railPath.transform.childCount)
            return false;

        Transform child = railPath.transform.GetChild(pointIndex);

        if (child == null)
            return false;

        distance = railPath.GetClosestDistance(child.position);
        return true;
    }

    public static WormBalancePathMetrics CreateFallback(
        float pathTimeLimitSeconds,
        float wormSpeed,
        int progressBucketCount)
    {
        float safeTime = Mathf.Max(1f, pathTimeLimitSeconds);

        return new WormBalancePathMetrics(
            safeTime * Mathf.Max(0.01f, wormSpeed),
            safeTime,
            Array.Empty<float>(),
            progressBucketCount,
            null);
    }

    public float GetHeadX(float headProgress)
    {
        if (_railPath == null || PathLength <= 0f)
            return 0f;

        Vector3 headPosition = _railPath.GetPoint(Mathf.Clamp01(headProgress) * PathLength);
        return headPosition.x;
    }

    public WormBalancePathLocation GetLocation(float headProgress)
    {
        float progress = Mathf.Clamp01(headProgress);
        int bucketIndex = Mathf.Clamp(
            Mathf.FloorToInt(progress * _progressBucketCount),
            0,
            _progressBucketCount - 1);

        int controlPointIndex = GetReachedControlPointIndex(progress);

        return new WormBalancePathLocation(
            progress,
            bucketIndex,
            _progressBucketCount,
            controlPointIndex,
            GetControlPointProgress(controlPointIndex));
    }

    private int GetReachedControlPointIndex(float progress)
    {
        if (_controlPointProgresses.Length == 0)
            return -1;

        int index = 0;

        for (int i = 0; i < _controlPointProgresses.Length; i++)
        {
            if (progress + 0.0001f < _controlPointProgresses[i])
                break;

            index = i;
        }

        return index;
    }

    private float GetControlPointProgress(int index)
    {
        if (index < 0 || index >= _controlPointProgresses.Length)
            return -1f;

        return _controlPointProgresses[index];
    }
}
