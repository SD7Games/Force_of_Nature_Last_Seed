using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

internal readonly struct WormBalancePathLocation
{
    public readonly float HeadProgress;
    public readonly int BucketIndex;
    public readonly int BucketCount;
    public readonly int ControlPointIndex;
    public readonly float ControlPointProgress;

    public WormBalancePathLocation(
        float headProgress,
        int bucketIndex,
        int bucketCount,
        int controlPointIndex,
        float controlPointProgress)
    {
        HeadProgress = Mathf.Clamp01(headProgress);
        BucketIndex = Mathf.Max(0, bucketIndex);
        BucketCount = Mathf.Max(1, bucketCount);
        ControlPointIndex = controlPointIndex;
        ControlPointProgress = controlPointProgress;
    }

    public string BucketLabel
    {
        get
        {
            float start = BucketIndex / (float)BucketCount * 100f;
            float end = (BucketIndex + 1) / (float)BucketCount * 100f;
            return $"{start:0}-{end:0}%";
        }
    }

    public string ControlPointLabel =>
        ControlPointIndex >= 0
            ? $"CP {ControlPointIndex} ({ControlPointProgress * 100f:0.0}%)"
            : "No rail";
}
