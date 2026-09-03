using System;
using System.Collections.Generic;

public sealed class WormSectionRollbackState<TSegment>
    where TSegment : class
{
    private const float MinimumSpacing = 0.01f;

    private readonly Dictionary<TSegment, float> _anchoredDistances = new();

    public bool IsActive { get; private set; }
    public float TargetDistance { get; private set; }
    public IReadOnlyDictionary<TSegment, float> AnchoredDistances => _anchoredDistances;

    public bool BeginOrExtend(
        IReadOnlyList<TSegment> segments,
        int splitIndex,
        int destroyedCount,
        float headDistance,
        float segmentSpacing)
    {
        if (segments == null)
            throw new ArgumentNullException(nameof(segments));

        if (destroyedCount <= 0 || splitIndex < 0)
            return false;

        bool shouldStartRoutine = !IsActive;
        float spacing = Math.Max(MinimumSpacing, segmentSpacing);
        AnchorTail(segments, splitIndex, destroyedCount, headDistance, spacing);

        float rollbackDistance = destroyedCount * spacing;
        TargetDistance = Math.Max(
            0f,
            (IsActive ? TargetDistance : headDistance) - rollbackDistance);
        IsActive = true;
        return shouldStartRoutine;
    }

    public void AdvanceAnchoredTail(
        IReadOnlyList<TSegment> segments,
        float maxDistance,
        float baseSpeed,
        float forwardSpeedMultiplier,
        float deltaTime)
    {
        float forwardDistance = Math.Max(0f, baseSpeed)
            * Math.Max(0f, forwardSpeedMultiplier)
            * Math.Max(0f, deltaTime);

        if (forwardDistance <= 0f)
            return;

        TargetDistance = Math.Min(maxDistance, TargetDistance + forwardDistance);

        for (int index = 0; index < segments.Count; index++)
        {
            TSegment segment = segments[index];
            if (segment == null)
                continue;

            if (_anchoredDistances.TryGetValue(segment, out float distance))
                _anchoredDistances[segment] = Math.Min(maxDistance, distance + forwardDistance);
        }
    }

    public void Forget(IReadOnlyList<TSegment> segments)
    {
        if (segments == null)
            return;

        for (int index = 0; index < segments.Count; index++)
        {
            TSegment segment = segments[index];
            if (segment != null)
                _anchoredDistances.Remove(segment);
        }
    }

    public void Complete()
    {
        IsActive = false;
        TargetDistance = 0f;
        _anchoredDistances.Clear();
    }

    private void AnchorTail(
        IReadOnlyList<TSegment> segments,
        int splitIndex,
        int destroyedCount,
        float headDistance,
        float spacing)
    {
        int startIndex = Math.Max(0, Math.Min(splitIndex, segments.Count));

        for (int index = startIndex; index < segments.Count; index++)
        {
            TSegment segment = segments[index];

            if (segment == null || _anchoredDistances.ContainsKey(segment))
                continue;

            float anchoredDistance = headDistance - (index + destroyedCount) * spacing;
            _anchoredDistances.Add(segment, anchoredDistance);
        }
    }
}
