using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class WormController : MonoBehaviour
{
    [Header("Rail")]
    [SerializeField] private RailPath _rail;

    [Header("Movement")]
    [SerializeField] private float _speed = 3f;

    [Header("Segments")]
    [SerializeField] private float _segmentSpacing = 0.5f;

    [Header("Realistic Move Segments")]
    [SerializeField] private float _waveAmplitude = 0.15f;

    [SerializeField] private float _waveFrequency = 6f;
    [SerializeField] private float _waveSpeed = 2f;

    [Header("Rollback")]
    [SerializeField] private float _rollbackSpeed = 8f;

    private readonly List<WormSegment> _segments = new();

    private float _headDistance;
    private Coroutine _rollbackRoutine;

    private bool _isSectionRollback;
    private int _rollbackSplitIndex = -1;
    private int _rollbackRemovedCount;
    private float _rollbackStartHeadDistance;

    public void Init(List<WormSegment> segments)
    {
        _segments.Clear();
        _segments.AddRange(segments);

        _headDistance = 0f;

        _isSectionRollback = false;
        _rollbackSplitIndex = -1;
        _rollbackRemovedCount = 0;
        _rollbackStartHeadDistance = 0f;
    }

    private void Update()
    {
        if (_segments.Count == 0 || _rail == null)
            return;

        if (!_isSectionRollback)
            _headDistance += _speed * Time.deltaTime;

        UpdateSegments();
    }

    private void UpdateSegments()
    {
        for (int i = 0; i < _segments.Count; i++)
        {
            WormSegment segment = _segments[i];
            if (segment == null)
                continue;

            float distance = GetSegmentDistance(i);

            Vector3 pos = _rail.GetPoint(distance);

            float wave = Mathf.Sin(distance * _waveFrequency + Time.time * _waveSpeed);
            Vector3 offset = Vector3.up * wave * _waveAmplitude;

            Vector3 finalPos = pos + offset;
            segment.transform.position = finalPos;

            if (i > 0)
            {
                WormSegment previous = _segments[i - 1];
                if (previous == null)
                    continue;

                Vector3 dir = previous.transform.position - finalPos;

                if (dir.sqrMagnitude > 0.0001f)
                {
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                    Transform visual = segment.VisualRoot;
                    if (visual != null)
                        visual.rotation = Quaternion.Euler(0f, 0f, angle);
                }
            }
        }
    }

    private float GetSegmentDistance(int index)
    {
        if (!_isSectionRollback)
            return _headDistance - (index * _segmentSpacing);

        if (index < _rollbackSplitIndex)
            return _headDistance - (index * _segmentSpacing);

        return _rollbackStartHeadDistance - ((index + _rollbackRemovedCount) * _segmentSpacing);
    }

    public int RemoveDestroyedSectionSegments(List<WormSegment> destroyed, out int firstRemovedIndex)
    {
        firstRemovedIndex = -1;
        int removed = 0;

        foreach (WormSegment seg in destroyed)
        {
            if (seg == null)
                continue;

            int index = _segments.IndexOf(seg);
            if (index < 0)
                continue;

            if (firstRemovedIndex == -1 || index < firstRemovedIndex)
                firstRemovedIndex = index;

            _segments.RemoveAt(index);
            removed++;
        }

        return removed;
    }

    public void RollbackDestroyedGap(int destroyedCount, int splitIndex)
    {
        if (destroyedCount <= 0)
            return;

        if (splitIndex < 0)
            return;

        _rollbackSplitIndex = splitIndex;
        _rollbackRemovedCount = destroyedCount;
        _rollbackStartHeadDistance = _headDistance;

        if (_rollbackRoutine != null)
            StopCoroutine(_rollbackRoutine);

        float rollbackDistance = destroyedCount * _segmentSpacing;
        _rollbackRoutine = StartCoroutine(SectionRollbackRoutine(rollbackDistance));
    }

    private IEnumerator SectionRollbackRoutine(float rollbackDistance)
    {
        _isSectionRollback = true;

        float target = Mathf.Max(0f, _rollbackStartHeadDistance - rollbackDistance);

        while (_headDistance > target)
        {
            _headDistance = Mathf.MoveTowards(
                _headDistance,
                target,
                _rollbackSpeed * Time.deltaTime
            );

            yield return null;
        }

        _headDistance = target;

        _isSectionRollback = false;
        _rollbackSplitIndex = -1;
        _rollbackRemovedCount = 0;
    }
}