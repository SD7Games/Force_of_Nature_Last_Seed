using System.Collections.Generic;

public static class WormSectionBuilder
{
    public static List<WormSection> BuildSectionsByCocoons(List<WormSegment> segments)
    {
        List<WormSection> sections = new();

        WormSection current = null;

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
            }

            current.AddSegment(seg);

            seg.Section = current;

            if (seg.Type == WormSegmentType.Cocoon)
                current = null;
        }

        return sections;
    }
}