using System;
using System.Collections.Generic;

public sealed class WormSegmentChain<TSegment>
    where TSegment : class
{
    private readonly List<TSegment> _segments = new();
    private readonly HashSet<TSegment> _removalLookup = new();

    public int Count => _segments.Count;
    public IReadOnlyList<TSegment> Segments => _segments;

    public void ReplaceWith(IReadOnlyList<TSegment> segments)
    {
        if (segments == null)
            throw new ArgumentNullException(nameof(segments));

        _segments.Clear();

        if (_segments.Capacity < segments.Count)
            _segments.Capacity = segments.Count;

        for (int index = 0; index < segments.Count; index++)
            _segments.Add(segments[index]);

        _removalLookup.Clear();
    }

    public void Clear()
    {
        _segments.Clear();
        _removalLookup.Clear();
    }

    public int RemoveAll(
        IReadOnlyList<TSegment> removedSegments,
        out int firstRemovedIndex)
    {
        firstRemovedIndex = -1;

        if (removedSegments == null || removedSegments.Count == 0)
            return 0;

        BuildRemovalLookup(removedSegments);
        firstRemovedIndex = FindFirstRemovedIndex();
        int removedCount = RemoveMarkedSegments();
        _removalLookup.Clear();
        return removedCount;
    }

    private void BuildRemovalLookup(IReadOnlyList<TSegment> removedSegments)
    {
        _removalLookup.Clear();

        for (int index = 0; index < removedSegments.Count; index++)
        {
            TSegment segment = removedSegments[index];

            if (segment != null)
                _removalLookup.Add(segment);
        }
    }

    private int FindFirstRemovedIndex()
    {
        for (int index = 0; index < _segments.Count; index++)
        {
            if (_removalLookup.Contains(_segments[index]))
                return index;
        }

        return -1;
    }

    private int RemoveMarkedSegments()
    {
        int removedCount = 0;

        for (int index = _segments.Count - 1; index >= 0; index--)
        {
            if (!_removalLookup.Contains(_segments[index]))
                continue;

            _segments.RemoveAt(index);
            removedCount++;
        }

        return removedCount;
    }
}
