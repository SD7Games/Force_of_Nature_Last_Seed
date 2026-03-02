using System.Collections.Generic;
using UnityEngine;

public sealed class WormPathHistory
{
    private readonly List<Vector3> _history = new();
    private readonly int _capacity;
    private readonly float _minRecordDistance;

    public WormPathHistory(int capacity, float minRecordDistance = 0.05f)
    {
        _capacity = capacity;
        _minRecordDistance = minRecordDistance;
    }

    public void Clear(Vector3 initialPosition)
    {
        _history.Clear();
        _history.Add(initialPosition);
    }

    public void Record(Vector3 position)
    {
        if (_history.Count == 0)
        {
            _history.Add(position);
            return;
        }

        if (Vector3.Distance(_history[0], position) < _minRecordDistance)
            return;

        _history.Insert(0, position);

        if (_history.Count > _capacity)
            _history.RemoveAt(_history.Count - 1);
    }

    public Vector3 GetPointAtDistance(float distance)
    {
        if (_history.Count == 0)
            return Vector3.zero;

        float accumulated = 0f;

        for (int i = 1; i < _history.Count; i++)
        {
            Vector3 from = _history[i - 1];
            Vector3 to = _history[i];

            float segmentLength = Vector3.Distance(from, to);

            if (accumulated + segmentLength >= distance)
            {
                float t = (distance - accumulated) / segmentLength;
                return Vector3.Lerp(from, to, t);
            }

            accumulated += segmentLength;
        }

        return _history[_history.Count - 1];
    }

    public void RollbackByDistance(float distance)
    {
        float accumulated = 0f;

        for (int i = 1; i < _history.Count; i++)
        {
            float segment = Vector3.Distance(_history[i - 1], _history[i]);
            accumulated += segment;

            if (accumulated >= distance)
            {
                _history.RemoveRange(0, i);
                return;
            }
        }

        _history.Clear();
    }

    public void RollbackByPercent(float percent)
    {
        percent = Mathf.Clamp01(percent);

        float totalDistance = GetTotalDistance();
        float rollbackDistance = totalDistance * percent;

        RollbackByDistance(rollbackDistance);
    }

    private float GetTotalDistance()
    {
        float total = 0f;

        for (int i = 1; i < _history.Count; i++)
        {
            total += Vector3.Distance(_history[i - 1], _history[i]);
        }

        return total;
    }

    public Vector3 CurrentHeadPosition =>
        _history.Count > 0 ? _history[0] : Vector3.zero;
}