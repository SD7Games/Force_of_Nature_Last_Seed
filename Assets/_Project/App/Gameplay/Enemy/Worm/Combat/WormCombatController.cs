using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WormCombatController : MonoBehaviour
{
    [SerializeField] private WormController _wormController;

    private readonly List<WormSection> _sections = new();

    private WormSegment _head;
    private WormSegment _tail;

    public void Init(WormSegment head, WormSegment tail, List<WormSection> sections)
    {
        _head = head;
        _tail = tail;

        _sections.Clear();
        _sections.AddRange(sections);
    }

    public void RegisterHit(WormSegment segment, int damage)
    {
        if (segment == null)
            return;

        if (segment.Type is WormSegmentType.Head or WormSegmentType.Tail)
            return;

        var section = segment.Section;

        if (section == null)
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
        }

        _sections.Remove(section);

        int removedFromChain = 0;
        int firstRemovedIndex = -1;

        if (_wormController != null)
            removedFromChain = _wormController.RemoveDestroyedSectionSegments(destroyedSegments, out firstRemovedIndex);

        if (_wormController != null && removedFromChain > 0)
            _wormController.RollbackDestroyedGap(removedFromChain, firstRemovedIndex);

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