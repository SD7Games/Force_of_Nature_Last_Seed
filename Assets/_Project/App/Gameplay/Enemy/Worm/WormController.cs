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

    public void Initialize(List<WormSegment> segments)
    {
        _segments.Clear();
        _segments.AddRange(segments);

        _headDistance = 0;
    }

    private void Update()
    {
        if (_segments.Count == 0 || _rail == null)
            return;

        _headDistance += _speed * Time.deltaTime;

        UpdateSegments();
    }

    private int _updateOffset;

    private void UpdateSegments()
    {
        for (int i = 0; i < _segments.Count; i++)
        {
            float distance = _headDistance - (i * _segmentSpacing);

            Vector3 pos = _rail.GetPoint(distance);

            float wave = Mathf.Sin(Time.time * _waveSpeed + i * _waveFrequency);
            Vector3 offset = Vector3.up * wave * _waveAmplitude;

            Vector3 finalPos = pos + offset;

            _segments[i].transform.position = finalPos;

            if (i > 0)
            {
                Vector3 dir = _segments[i - 1].transform.position - finalPos;

                if (dir.sqrMagnitude > 0.0001f)
                {
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                    Transform visual = _segments[i].VisualRoot;

                    if (visual != null)
                        visual.rotation = Quaternion.Euler(0, 0, angle);
                }
            }
        }
    }

    public int RemoveDestroyedSectionSegments(List<WormSegment> destroyed)
    {
        int removed = 0;

        foreach (var seg in destroyed)
        {
            if (_segments.Remove(seg))
                removed++;
        }

        return removed;
    }

    public void RollbackBySegments(int destroyedCount)
    {
        if (destroyedCount <= 0)
            return;

        float rollbackDistance = destroyedCount * _segmentSpacing;

        if (_rollbackRoutine != null)
            StopCoroutine(_rollbackRoutine);

        _rollbackRoutine = StartCoroutine(RollbackRoutine(rollbackDistance));
    }

    private IEnumerator RollbackRoutine(float distance)
    {
        float start = _headDistance;

        float target = Mathf.Max(0, _headDistance - distance);

        float t = 0;

        while (t < 1f)
        {
            t += Time.deltaTime * _rollbackSpeed;

            _headDistance = Mathf.Lerp(start, target, t);

            yield return null;
        }

        _headDistance = target;
    }
}