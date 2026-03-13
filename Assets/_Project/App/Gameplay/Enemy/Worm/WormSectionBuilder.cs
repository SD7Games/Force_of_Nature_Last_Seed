using System.Collections.Generic;

/// <summary>
/// Builds logical worm sections from a sequential list of worm segments.
///
/// A section represents a gameplay unit with its own HP and destruction logic.
/// Sections are independent from cocoon placement and are defined by a fixed
/// number of body segments to ensure consistent gameplay pacing.
///
/// Each section:
/// - contains up to SECTION_SIZE segments
/// - receives its own HP value via WormSectionHPGenerator
/// - stores references to its segments
/// - assigns itself back to each segment for damage routing
///
/// Head and tail segments are excluded from section logic.
///
/// This approach guarantees predictable destruction behavior where removing
/// a section never affects neighboring sections, preventing unintended
/// chain removals when segments are destroyed.
/// </summary>
public static class WormSectionBuilder
{
    private const int SECTION_SIZE = 5;

    public static List<WormSection> BuildSectionsByCocoons(List<WormSegment> segments)
    {
        List<WormSection> sections = new();

        WormSection current = null;
        int countInSection = 0;

        for (int i = 0; i < segments.Count; i++)
        {
            WormSegment seg = segments[i];

            if (seg.Type is WormSegmentType.Head or WormSegmentType.Tail)
                continue;

            if (current == null)
            {
                current = new WormSection();
                current.Init(WormSectionHPGenerator.GetHP(sections.Count));
                sections.Add(current);
                countInSection = 0;
            }

            current.AddSegment(seg);
            seg.Section = current;

            countInSection++;

            if (countInSection >= SECTION_SIZE)
            {
                current = null;
            }
        }

        return sections;
    }
}