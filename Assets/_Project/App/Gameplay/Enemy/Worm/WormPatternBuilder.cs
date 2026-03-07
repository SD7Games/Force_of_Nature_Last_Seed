using System.Collections.Generic;
using UnityEngine;

public static class WormPatternBuilder
{
    public static List<WormSegmentType> BuildPattern(
        int totalLength,
        int minBodyBeforeCocoon,
        int maxBodyBeforeCocoon)
    {
        int length = Mathf.Max(3, totalLength);

        List<WormSegmentType> result = new(length)
        {
            WormSegmentType.Head
        };

        int bodyCounter = 0;

        int nextCocoon = Random.Range(minBodyBeforeCocoon, maxBodyBeforeCocoon + 1);

        while (result.Count < length - 1)
        {
            bodyCounter++;

            if (bodyCounter >= nextCocoon)
            {
                result.Add(WormSegmentType.Cocoon);

                bodyCounter = 0;

                nextCocoon = Random.Range(minBodyBeforeCocoon, maxBodyBeforeCocoon + 1);
            }
            else
            {
                result.Add(WormSegmentType.Body);
            }
        }

        result.Add(WormSegmentType.Tail);

        return result;
    }
}