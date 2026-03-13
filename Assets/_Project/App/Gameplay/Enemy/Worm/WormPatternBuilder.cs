using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedurally generates a worm segment pattern used during enemy spawning.
///
/// The pattern defines the order of segment types (Head, Body, Cocoon, Tail)
/// before physical instances are created by WormFactory.
///
/// Generation rules are designed to mimic the reference gameplay layout:
///
/// - The worm is divided into short visual blocks of 4–5 body segments.
/// - A cocoon may appear inside a block but never as the first or last segment.
/// - The first sections always contain cocoons to introduce the mechanic
///   early in gameplay.
/// - Subsequent cocoons appear after 2–3 sections without one, ensuring
///   balanced distribution and preventing long empty stretches.
/// - Cocoons are inserted inside a section rather than defining its boundary.
///
/// This produces a visually readable worm structure while maintaining
/// consistent section lengths for combat and destruction logic.
/// </summary>
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

        int sectionsWithoutCocoon = 0;
        int sectionIndex = 0;

        while (result.Count < length - 1)
        {
            int bodyCount = Random.Range(4, 6);

            bool spawnCocoon;

            if (sectionIndex < 2)
            {
                spawnCocoon = true;
            }
            else
            {
                spawnCocoon =
                    sectionsWithoutCocoon >= 2 ||
                    (sectionsWithoutCocoon >= 1 && Random.value < 0.35f);
            }

            int cocoonIndex = -1;

            if (spawnCocoon)
            {
                cocoonIndex = Random.Range(1, bodyCount - 1);
                sectionsWithoutCocoon = 0;
            }
            else
            {
                sectionsWithoutCocoon++;
            }

            for (int i = 0; i < bodyCount; i++)
            {
                if (result.Count >= length - 1)
                    break;

                result.Add(WormSegmentType.Body);

                if (i == cocoonIndex && result.Count < length - 1)
                {
                    result.Add(WormSegmentType.Cocoon);
                }
            }

            sectionIndex++;
        }

        result.Add(WormSegmentType.Tail);

        return result;
    }
}