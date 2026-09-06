using System.Collections.Generic;
using UnityEngine;

internal static class RailPathGeometry
{
    private const float CornerDistanceFraction = 0.45f;

    public static Vector3[] BuildPathPoints(
        Vector3[] worldPoints,
        RailPathInterpolationMode interpolationMode,
        float cornerRadius,
        int cornerSamples,
        float minimumSegmentLength)
    {
        if (interpolationMode != RailPathInterpolationMode.Smooth ||
            worldPoints.Length < 3 ||
            cornerRadius <= minimumSegmentLength)
        {
            return worldPoints;
        }

        List<Vector3> points = new(worldPoints.Length * (cornerSamples + 1));
        AddPointIfSeparated(points, worldPoints[0], minimumSegmentLength);

        for (int i = 1; i < worldPoints.Length - 1; i++)
        {
            Vector3 previous = worldPoints[i - 1];
            Vector3 corner = worldPoints[i];
            Vector3 next = worldPoints[i + 1];
            float previousLength = Vector3.Distance(previous, corner);
            float nextLength = Vector3.Distance(corner, next);
            float cornerDistance = Mathf.Min(
                cornerRadius,
                previousLength * CornerDistanceFraction,
                nextLength * CornerDistanceFraction);

            if (cornerDistance <= minimumSegmentLength)
            {
                AddPointIfSeparated(points, corner, minimumSegmentLength);
                continue;
            }

            Vector3 entry = corner + (previous - corner).normalized * cornerDistance;
            Vector3 exit = corner + (next - corner).normalized * cornerDistance;
            AddPointIfSeparated(points, entry, minimumSegmentLength);

            for (int sample = 1; sample <= cornerSamples; sample++)
            {
                float t = sample / (float)cornerSamples;
                AddPointIfSeparated(
                    points,
                    EvaluateQuadraticBezier(entry, corner, exit, t),
                    minimumSegmentLength);
            }
        }

        AddPointIfSeparated(points, worldPoints[^1], minimumSegmentLength);
        return points.ToArray();
    }

    public static float[] CalculateDistances(Vector3[] pathPoints, out float totalLength)
    {
        float[] distances = new float[pathPoints.Length];
        distances[0] = 0f;

        for (int i = 1; i < pathPoints.Length; i++)
        {
            distances[i] = distances[i - 1] + Vector3.Distance(
                pathPoints[i - 1],
                pathPoints[i]);
        }

        totalLength = distances[^1];
        return distances;
    }

    public static Vector3[] BuildSamples(
        Vector3[] pathPoints,
        float[] distances,
        float totalLength,
        float sampleStep,
        float minimumSegmentLength)
    {
        if (totalLength <= minimumSegmentLength)
        {
            return new[]
            {
                pathPoints[0],
                pathPoints[^1]
            };
        }

        int count = Mathf.Max(2, Mathf.CeilToInt(totalLength / sampleStep) + 1);
        Vector3[] samples = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            float distance = Mathf.Min(i * sampleStep, totalLength);
            samples[i] = GetPoint(
                pathPoints,
                distances,
                totalLength,
                distance,
                minimumSegmentLength);
        }

        return samples;
    }

    private static Vector3 GetPoint(
        Vector3[] pathPoints,
        float[] distances,
        float totalLength,
        float distance,
        float minimumSegmentLength)
    {
        distance = Mathf.Clamp(distance, 0f, totalLength);

        for (int i = 1; i < distances.Length; i++)
        {
            if (distance > distances[i])
                continue;

            float segmentLength = distances[i] - distances[i - 1];
            if (segmentLength <= minimumSegmentLength)
                return pathPoints[i];

            float t = (distance - distances[i - 1]) / segmentLength;
            return Vector3.Lerp(pathPoints[i - 1], pathPoints[i], t);
        }

        return pathPoints[^1];
    }

    private static Vector3 EvaluateQuadraticBezier(
        Vector3 start,
        Vector3 control,
        Vector3 end,
        float t)
    {
        float inverseT = 1f - t;
        return inverseT * inverseT * start +
               2f * inverseT * t * control +
               t * t * end;
    }

    private static void AddPointIfSeparated(
        List<Vector3> points,
        Vector3 point,
        float minimumSegmentLength)
    {
        if (points.Count > 0 &&
            Vector3.SqrMagnitude(points[^1] - point) <=
            minimumSegmentLength * minimumSegmentLength)
        {
            return;
        }

        points.Add(point);
    }
}
