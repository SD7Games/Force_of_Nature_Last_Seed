using System.Collections.Generic;
using UnityEngine;

public sealed class WormReviveVisualScaler
{
    private readonly List<Vector3> _baseScales = new();

    public void Capture(IReadOnlyList<WormSegment> segments)
    {
        _baseScales.Clear();

        if (_baseScales.Capacity < segments.Count)
            _baseScales.Capacity = segments.Count;

        for (int index = 0; index < segments.Count; index++)
        {
            WormSegment segment = segments[index];
            Transform visual = segment != null ? segment.VisualRoot : null;
            _baseScales.Add(visual != null ? visual.localScale : Vector3.one);
        }
    }

    public void Apply(
        IReadOnlyList<WormSegment> segments,
        float xMultiplier,
        float yMultiplier)
    {
        int count = Mathf.Min(segments.Count, _baseScales.Count);

        for (int index = 0; index < count; index++)
        {
            WormSegment segment = segments[index];
            Transform visual = segment != null ? segment.VisualRoot : null;
            if (visual == null)
                continue;

            Vector3 baseScale = _baseScales[index];
            visual.localScale = new Vector3(
                baseScale.x * xMultiplier,
                baseScale.y * yMultiplier,
                baseScale.z);
        }
    }

    public void RestoreAndClear(IReadOnlyList<WormSegment> segments)
    {
        int count = Mathf.Min(segments.Count, _baseScales.Count);

        for (int index = 0; index < count; index++)
        {
            WormSegment segment = segments[index];
            Transform visual = segment != null ? segment.VisualRoot : null;

            if (visual != null)
                visual.localScale = _baseScales[index];
        }

        _baseScales.Clear();
    }
}
