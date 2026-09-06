using System.Collections.Generic;
using UnityEngine;

public enum RailPathInterpolationMode
{
    Linear = 0,
    Smooth = 1
}

[DisallowMultipleComponent]
public sealed partial class RailPath : MonoBehaviour, IWormRailPath
{
    private const float DefaultSampleStep = 0.1f;
    private const float MinSampleStep = 0.01f;
    private const float MinSegmentLength = 0.0001f;

    [SerializeField] private List<Vector3> _localPoints = new();
    [SerializeField][Min(MinSampleStep)] private float _sampleStep = DefaultSampleStep;
    [SerializeField] private RailPathInterpolationMode _interpolationMode = RailPathInterpolationMode.Linear;

    [Header("Smoothing")]
    [SerializeField][Min(0f)] private float _cornerRadius = 0.35f;
    [SerializeField][Range(2, 16)] private int _cornerSamples = 6;

    [SerializeField][HideInInspector] private Transform[] _waypoints;

    private Vector3[] _worldPoints;
    private Vector3[] _samples;
    private float[] _distances;
    private float[] _controlPointDistances;
    private float _totalLength;

    public int PointCount => _localPoints != null ? _localPoints.Count : 0;
    public float TotalLength => _totalLength;

    private void Reset()
    {
        _localPoints = new List<Vector3>
        {
            new(-1f, 0f, 0f),
            new(1f, 0f, 0f)
        };

        Invalidate();
    }

    private void Awake()
    {
        if (!EnsureBuilt())
        {
            Debug.LogError("RailPath requires at least 2 points.", this);
            return;
        }

        if (_totalLength <= MinSegmentLength)
            Debug.LogError("RailPath total length must be greater than zero.", this);
    }

    private void OnValidate()
    {
        if (_localPoints == null)
            _localPoints = new List<Vector3>();

        if (_sampleStep < MinSampleStep)
            _sampleStep = DefaultSampleStep;

        if (_cornerRadius < 0f)
            _cornerRadius = 0f;

        _cornerSamples = Mathf.Clamp(_cornerSamples, 2, 16);

        Invalidate();
    }

    public Vector3 GetPoint(float distance)
    {
        if (!EnsureBuilt())
            return transform.position;

        distance = Mathf.Clamp(distance, 0f, _totalLength);

        float fIndex = distance / _sampleStep;
        int index = Mathf.FloorToInt(fIndex);
        float t = fIndex - index;

        if (index >= _samples.Length - 1)
            return _samples[^1];

        return Vector3.Lerp(
            _samples[index],
            _samples[index + 1],
            t);
    }

    public float GetClosestDistance(Vector3 worldPosition)
    {
        if (!EnsureBuilt() || _samples == null || _samples.Length == 0)
            return 0f;

        return FindClosestSampleDistance(worldPosition);
    }

    private float FindClosestSampleDistance(Vector3 worldPosition)
    {
        int closestIndex = 0;
        float closestSqrDistance = float.MaxValue;

        for (int i = 0; i < _samples.Length; i++)
        {
            float sqrDistance = Vector3.SqrMagnitude(_samples[i] - worldPosition);
            if (sqrDistance >= closestSqrDistance)
                continue;

            closestSqrDistance = sqrDistance;
            closestIndex = i;
        }

        return Mathf.Clamp(closestIndex * _sampleStep, 0f, _totalLength);
    }

    public bool TryGetControlPointDistance(int pointIndex, out float distance)
    {
        distance = 0f;

        if (pointIndex < 0 || !EnsureBuilt() ||
            _controlPointDistances == null ||
            pointIndex >= _controlPointDistances.Length)
        {
            return false;
        }

        distance = _controlPointDistances[pointIndex];
        return true;
    }

    public float GetControlPointProgressNormalized(float distance)
    {
        if (!EnsureBuilt() || PointCount <= 1)
            return 0f;

        float clampedDistance = Mathf.Clamp(distance, 0f, _totalLength);
        int passedPointIndex = 0;

        for (int i = 1; i < PointCount; i++)
        {
            if (!TryGetControlPointDistance(i, out float pointDistance))
                break;

            if (clampedDistance + MinSegmentLength < pointDistance)
                break;

            passedPointIndex = i;
        }

        return Mathf.Clamp01(passedPointIndex / (float)(PointCount - 1));
    }

    public bool TryGetControlPointWorldPosition(int pointIndex, out Vector3 worldPosition)
    {
        worldPosition = default;

        if (pointIndex < 0)
            return false;

        if (_localPoints != null && _localPoints.Count >= 2)
        {
            if (pointIndex >= _localPoints.Count)
                return false;

            worldPosition = transform.TransformPoint(_localPoints[pointIndex]);
            return true;
        }

        if (_waypoints == null)
            return false;

        int validPointIndex = 0;
        for (int i = 0; i < _waypoints.Length; i++)
        {
            Transform waypoint = _waypoints[i];
            if (waypoint == null)
                continue;

            if (validPointIndex == pointIndex)
            {
                worldPosition = waypoint.position;
                return true;
            }

            validPointIndex++;
        }

        return false;
    }

    private bool EnsureBuilt()
    {
        if (_samples != null && _samples.Length > 0)
            return true;

        if (!TryBuildWorldPoints())
            return false;

        Vector3[] pathPoints = BuildPathPoints();
        if (pathPoints == null || pathPoints.Length < 2)
            return false;

        _sampleStep = Mathf.Max(MinSampleStep, _sampleStep);
        CalculateDistances(pathPoints);
        BuildSamples(pathPoints);
        BuildControlPointDistances();

        return _samples != null && _samples.Length > 0;
    }

    private bool TryBuildWorldPoints()
    {
        if (_localPoints != null && _localPoints.Count >= 2)
        {
            _worldPoints = new Vector3[_localPoints.Count];

            for (int i = 0; i < _localPoints.Count; i++)
                _worldPoints[i] = transform.TransformPoint(_localPoints[i]);

            return true;
        }

        int legacyWaypointCount = CountValidLegacyWaypoints();
        if (legacyWaypointCount < 2)
            return false;

        _worldPoints = new Vector3[legacyWaypointCount];

        int pointIndex = 0;
        for (int i = 0; i < _waypoints.Length; i++)
        {
            Transform waypoint = _waypoints[i];
            if (waypoint == null)
                continue;

            _worldPoints[pointIndex] = waypoint.position;
            pointIndex++;
        }

        return true;
    }

    private Vector3[] BuildPathPoints()
    {
        return RailPathGeometry.BuildPathPoints(
            _worldPoints,
            _interpolationMode,
            _cornerRadius,
            _cornerSamples,
            MinSegmentLength);
    }

    private void CalculateDistances(Vector3[] pathPoints)
    {
        _distances = RailPathGeometry.CalculateDistances(pathPoints, out _totalLength);
    }

    private void BuildSamples(Vector3[] pathPoints)
    {
        _samples = RailPathGeometry.BuildSamples(
            pathPoints,
            _distances,
            _totalLength,
            _sampleStep,
            MinSegmentLength);
    }

    private void BuildControlPointDistances()
    {
        int controlPointCount = GetAvailableControlPointCount();
        _controlPointDistances = new float[controlPointCount];

        for (int i = 0; i < controlPointCount; i++)
        {
            if (TryGetControlPointWorldPosition(i, out Vector3 worldPosition))
                _controlPointDistances[i] = FindClosestSampleDistance(worldPosition);
        }
    }

    private int GetAvailableControlPointCount()
    {
        return _localPoints != null && _localPoints.Count >= 2
            ? _localPoints.Count
            : CountValidLegacyWaypoints();
    }

    private int CountValidLegacyWaypoints()
    {
        if (_waypoints == null)
            return 0;

        int count = 0;
        for (int i = 0; i < _waypoints.Length; i++)
        {
            if (_waypoints[i] != null)
                count++;
        }

        return count;
    }

    private void Invalidate()
    {
        _worldPoints = null;
        _samples = null;
        _distances = null;
        _controlPointDistances = null;
        _totalLength = 0f;
    }

}
