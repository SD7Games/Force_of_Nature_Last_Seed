using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WormController : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private float _segmentSpacing = 0.5f;

    [SerializeField] private int _historyCapacity = 2000;

    private readonly List<WormSegment> _segments = new();
    private WormPathHistory _pathHistory;

    private void Awake()
    {
        _pathHistory = new WormPathHistory(_historyCapacity);
    }

    public void Initialize(List<WormSegment> segments)
    {
        _segments.Clear();
        _segments.AddRange(segments);

        if (_segments.Count > 0)
            _pathHistory.Clear(_segments[0].transform.position);
    }

    private void LateUpdate()
    {
        if (_segments.Count == 0)
            return;

        RecordHead();
        UpdateSegments();
        RotateAllSegments();
    }

    private void RecordHead()
    {
        _pathHistory.Record(_segments[0].transform.position);
    }

    private void UpdateSegments()
    {
        for (int i = 1; i < _segments.Count; i++)
        {
            float requiredDistance = i * _segmentSpacing;
            Vector3 target = _pathHistory.GetPointAtDistance(requiredDistance);

            _segments[i].transform.position = target;
        }
    }

    private void RotateAllSegments()
    {
        for (int i = 0; i < _segments.Count; i++)
        {
            WormSegment segment = _segments[i];

            if (segment.Type == WormSegmentType.Cocoon)
                continue;

            Transform visual = segment.VisualRoot;
            if (visual == null)
                continue;

            Vector3 direction;

            if (i == 0)
            {
                Vector3 forwardPoint = _pathHistory.GetPointAtDistance(0.1f);
                direction = forwardPoint - segment.transform.position;
            }
            else
            {
                direction = _segments[i - 1].transform.position - segment.transform.position;
            }

            if (direction.sqrMagnitude < 0.0001f)
                continue;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            angle += segment.RotationOffset;

            visual.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    public void RemoveSegment(WormSegment segment)
    {
        _segments.Remove(segment);
    }

    public void RollbackBySegments(int destroyedCount)
    {
        float distance = destroyedCount * _segmentSpacing;
        _pathHistory.RollbackByDistance(distance);

        if (_segments.Count > 0)
            _segments[0].transform.position = _pathHistory.CurrentHeadPosition;
    }

    public void RollbackByPercent(float percent)
    {
        _pathHistory.RollbackByPercent(percent);

        if (_segments.Count > 0)
            _segments[0].transform.position = _pathHistory.CurrentHeadPosition;
    }
}