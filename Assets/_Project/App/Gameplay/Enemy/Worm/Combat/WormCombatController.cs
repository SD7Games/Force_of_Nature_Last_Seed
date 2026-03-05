using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WormCombatController : MonoBehaviour
{
    [SerializeField] private WormController _wormController;

    private readonly List<WormSection> _sections = new();
    private readonly Dictionary<WormSegment, WormSection> _segmentToSection = new();

    private WormSegment _head;
    private WormSegment _tail;

    public void Initialize(WormSegment head, WormSegment tail, List<WormSection> sections)
    {
        _head = head;
        _tail = tail;

        _sections.Clear();
        _sections.AddRange(sections);

        _segmentToSection.Clear();

        foreach (var section in _sections)
        {
            foreach (var seg in section.Segments)
                _segmentToSection[seg] = section;
        }
    }

    public void RegisterHit(WormSegment segment, int damage)
    {
        if (segment == null)
            return;

        if (segment.Type is WormSegmentType.Head or WormSegmentType.Tail)
            return;

        if (!_segmentToSection.TryGetValue(segment, out var section))
            return;

        section.Damage(damage);

        if (!section.IsDestroyed)
            return;

        DestroySection(section);
    }

    private void DestroySection(WormSection section)
    {
        if (section == null)
            return;

        bool rewardTriggered = false;

        List<WormSegment> destroyedSegments = new();

        foreach (var seg in section.Segments)
        {
            if (seg == null || !seg.IsAlive)
                continue;

            if (seg.Type == WormSegmentType.Cocoon && seg.HasReward)
                rewardTriggered = true;

            if (seg.Type is WormSegmentType.Head or WormSegmentType.Tail)
                continue;

            seg.KillVisualAndCollision();

            destroyedSegments.Add(seg);

            _segmentToSection.Remove(seg);
        }

        _sections.Remove(section);

        int removedFromChain = 0;

        if (_wormController != null)
            removedFromChain = _wormController.RemoveDestroyedSectionSegments(destroyedSegments);

        if (_wormController != null && removedFromChain > 0)
            _wormController.RollbackBySegments(removedFromChain);

        if (rewardTriggered)
            Debug.Log("Reward popup should be shown (cocoon destroyed)");

        if (_sections.Count == 0)
        {
            if (_head != null && _head.IsAlive)
                _head.KillVisualAndCollision();

            if (_tail != null && _tail.IsAlive)
                _tail.KillVisualAndCollision();
        }
    }
}