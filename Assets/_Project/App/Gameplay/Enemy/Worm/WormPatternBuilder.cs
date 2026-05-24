using System.Collections.Generic;
using UnityEngine;

public readonly struct WormPatternEntry
{
    public readonly WormSegmentType Type;

    public WormPatternEntry(WormSegmentType type)
    {
        Type = type;
    }
}

/// <summary>
/// Generates only structural layout (Head → Body → Tail).
/// No gameplay logic (cocoons/rewards) here.
/// </summary>
public static class WormPatternBuilder
{
    public static int GetBodySegmentCount(int sectionCount)
    {
        return Mathf.Max(1, sectionCount) * WormCocoonRules.SectionSize;
    }

    public static List<WormPatternEntry> BuildPattern(int sectionCount)
    {
        int bodySegmentCount = GetBodySegmentCount(sectionCount);

        List<WormPatternEntry> result = new(bodySegmentCount + 2)
        {
            new(WormSegmentType.Head)
        };

        int remainingBodySegments = bodySegmentCount;

        while (remainingBodySegments > 0)
        {
            int bodyCount = Random.Range(4, 6);

            for (int i = 0; i < bodyCount && remainingBodySegments > 0; i++)
            {
                result.Add(new WormPatternEntry(WormSegmentType.Body));
                remainingBodySegments--;
            }
        }

        result.Add(new WormPatternEntry(WormSegmentType.Tail));

        return result;
    }
}
