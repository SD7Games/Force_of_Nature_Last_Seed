using System.Collections.Generic;

public sealed class WormFactory
{
    private readonly WormSegmentPool _pool;

    public WormFactory(WormSegmentPool pool)
    {
        _pool = pool;
    }

    public List<WormSegment> CreateSegments(
        List<WormSegmentType> pattern,
        out WormSegment head,
        out WormSegment tail)
    {
        List<WormSegment> segments = new(pattern.Count);

        head = null;
        tail = null;

        for (int i = 0; i < pattern.Count; i++)
        {
            WormSegment seg = _pool.Get(pattern[i]);

            if (seg == null)
                continue;

            seg.Activate();

            if (seg.Type == WormSegmentType.Head)
                head = seg;

            if (seg.Type == WormSegmentType.Tail)
                tail = seg;

            segments.Add(seg);
        }

        return segments;
    }

    public void AttachDamageReceivers(
    List<WormSegment> segments,
    WormCombatController combat)
    {
        for (int i = 0; i < segments.Count; i++)
        {
            WormSegment seg = segments[i];

            var receiver = seg.GetComponent<WormSegmentDamageReceiver>();

            if (receiver == null)
                receiver = seg.gameObject.AddComponent<WormSegmentDamageReceiver>();

            receiver.Initialize(combat, seg);
        }
    }
}