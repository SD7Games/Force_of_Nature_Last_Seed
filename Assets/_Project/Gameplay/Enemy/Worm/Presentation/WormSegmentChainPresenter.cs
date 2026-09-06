using System.Collections.Generic;
using UnityEngine;

public sealed class WormSegmentChainPresenter
{
    private const float MinimumSpacing = 0.01f;
    private const float DirectionSqrMagnitudeThreshold = 0.0001f;
    private const float PositionSqrMagnitudeThreshold = 0.000001f;
    private const float RotationThresholdDegrees = 0.1f;

    private int _activeStartIndex = -1;
    private int _activeEndIndex = -1;
    private Vector3 _temporaryEuler;

    public void Reset()
    {
        _activeStartIndex = -1;
        _activeEndIndex = -1;
    }

    public void Render(
        IReadOnlyList<WormSegment> segments,
        RailPath rail,
        IReadOnlyDictionary<WormSegment, float> rollbackAnchoredDistances,
        in WormSegmentChainLayout layout)
    {
        if (segments == null || segments.Count == 0 || rail == null)
            return;

        if (layout.IsSectionRollback || layout.IsReviveRollback)
        {
            RenderDuringRollback(segments, rail, rollbackAnchoredDistances, layout);
            return;
        }

        if (!TryGetActiveRange(segments.Count, rail.TotalLength, layout, out int start, out int end))
        {
            HidePreviousActiveRange(segments, -1, -1);
            return;
        }

        HidePreviousActiveRange(segments, start, end);

        for (int index = start; index <= end; index++)
        {
            WormSegment segment = segments[index];
            if (segment == null)
                continue;

            float distance = GetSegmentDistance(
                segments,
                rollbackAnchoredDistances,
                index,
                segment,
                layout);
            Vector3 position = CalculatePositionAtDistance(rail, distance, layout);

            UpdateSegmentPosition(segment, position);
            UpdateHeadFollowChain(segments, rail, index, segment, distance, layout);

            if (index > start && !segment.HasTailVisualChain)
                UpdateSegmentRotation(segments, index, segment, position);

            UpdateTailVisualChain(segments, rail, index, segment, distance, layout);
            segment.SetRuntimeVisible(true);
            segment.UpdateCocoonPresentation();
        }

        _activeStartIndex = start;
        _activeEndIndex = end;
    }

    private void RenderDuringRollback(
        IReadOnlyList<WormSegment> segments,
        RailPath rail,
        IReadOnlyDictionary<WormSegment, float> rollbackAnchoredDistances,
        in WormSegmentChainLayout layout)
    {
        float maxDistance = rail.TotalLength + layout.ActiveDistancePadding;

        for (int index = 0; index < segments.Count; index++)
        {
            WormSegment segment = segments[index];
            if (segment == null)
                continue;

            float distance = GetSegmentDistance(
                segments,
                rollbackAnchoredDistances,
                index,
                segment,
                layout);

            if (distance < 0f || distance > maxDistance)
            {
                segment.SetRuntimeVisible(false);
                continue;
            }

            Vector3 position = CalculatePositionAtDistance(rail, distance, layout);
            UpdateSegmentPosition(segment, position);
            UpdateHeadFollowChain(segments, rail, index, segment, distance, layout);

            if (index > 0 && !segment.HasTailVisualChain)
                UpdateSegmentRotation(segments, index, segment, position);

            UpdateTailVisualChain(segments, rail, index, segment, distance, layout);
            segment.SetRuntimeVisible(true);
            segment.UpdateCocoonPresentation();
        }

        Reset();
    }

    private static bool TryGetActiveRange(
        int segmentCount,
        float railLength,
        in WormSegmentChainLayout layout,
        out int startIndex,
        out int endIndex)
    {
        float spacing = Mathf.Max(MinimumSpacing, layout.SegmentSpacing);
        float maxDistance = railLength + layout.ActiveDistancePadding;

        startIndex = Mathf.Max(
            0,
            Mathf.CeilToInt((layout.HeadDistance - maxDistance) / spacing));
        endIndex = Mathf.Min(
            segmentCount - 1,
            Mathf.FloorToInt(layout.HeadDistance / spacing));

        return startIndex <= endIndex;
    }

    private void HidePreviousActiveRange(
        IReadOnlyList<WormSegment> segments,
        int nextStartIndex,
        int nextEndIndex)
    {
        if (_activeStartIndex < 0 || _activeEndIndex < _activeStartIndex)
            return;

        for (int index = _activeStartIndex; index <= _activeEndIndex; index++)
        {
            if (index >= nextStartIndex && index <= nextEndIndex)
                continue;

            if (index < 0 || index >= segments.Count)
                continue;

            WormSegment segment = segments[index];
            if (segment != null)
                segment.SetRuntimeVisible(false);
        }
    }

    private static Vector3 CalculatePositionAtDistance(
        RailPath rail,
        float distance,
        in WormSegmentChainLayout layout)
    {
        Vector3 position = rail.GetPoint(distance);
        float wave = Mathf.Sin(distance * layout.WaveFrequency + layout.WaveTime);
        position.y += wave * layout.WaveAmplitude + layout.VerticalOffset;
        return position;
    }

    private static void UpdateTailVisualChain(
        IReadOnlyList<WormSegment> segments,
        RailPath rail,
        int index,
        WormSegment segment,
        float tailDistance,
        in WormSegmentChainLayout layout)
    {
        if (segment == null || !segment.HasTailVisualChain)
            return;

        segment.ResetTailVisualRootRotation();
        float spacing = Mathf.Max(
            MinimumSpacing,
            layout.SegmentSpacing * layout.TailVisualSpacingMultiplier);
        Vector3 previousPosition = ResolveTailLeaderPosition(segments, index, segment);

        for (int partIndex = 0; partIndex < segment.TailVisualPartCount; partIndex++)
        {
            float visualDistance = Mathf.Max(0f, tailDistance - partIndex * spacing);
            Vector3 visualPosition = CalculatePositionAtDistance(rail, visualDistance, layout);
            float angle = CalculateLookAngle(visualPosition, previousPosition);
            segment.SetTailVisualPartPose(partIndex, visualPosition, angle);
            previousPosition = visualPosition;
        }
    }

    private static Vector3 ResolveTailLeaderPosition(
        IReadOnlyList<WormSegment> segments,
        int index,
        WormSegment tail)
    {
        int previousIndex = index - 1;

        if (previousIndex >= 0 && previousIndex < segments.Count)
        {
            WormSegment previous = segments[previousIndex];

            if (ShouldAttachTailToHeadFollowChain(segments, index, tail)
                && previous != null
                && previous.TryGetLastHeadFollowPartPosition(out Vector3 headFollowPosition))
            {
                return headFollowPosition;
            }

            if (previous != null)
                return previous.CachedTransform.position;
        }

        return tail.CachedTransform.position;
    }

    private static void UpdateHeadFollowChain(
        IReadOnlyList<WormSegment> segments,
        RailPath rail,
        int index,
        WormSegment segment,
        float headDistance,
        in WormSegmentChainLayout layout)
    {
        if (segment == null || !segment.HasHeadFollowChain)
            return;

        bool visible = ShouldShowHeadFollowChain(segments, index, segment);
        segment.SetHeadFollowChainVisible(visible);

        if (!visible)
            return;

        float spacing = GetHeadBridgeSpacing(layout);
        Vector3 previousPosition = segment.CachedTransform.position;

        for (int partIndex = 0; partIndex < segment.HeadFollowPartCount; partIndex++)
        {
            float visualDistance = Mathf.Max(
                0f,
                headDistance - (partIndex + 1) * spacing);
            Vector3 visualPosition = CalculatePositionAtDistance(rail, visualDistance, layout);
            float angle = CalculateLookAngle(visualPosition, previousPosition);
            segment.SetHeadFollowPartPose(partIndex, visualPosition, angle);
            previousPosition = visualPosition;
        }
    }

    private static float CalculateLookAngle(Vector3 from, Vector3 to)
    {
        Vector3 direction = to - from;
        return direction.sqrMagnitude <= DirectionSqrMagnitudeThreshold
            ? 0f
            : Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    private static void UpdateSegmentPosition(WormSegment segment, Vector3 position)
    {
        Transform segmentTransform = segment.CachedTransform;

        if ((segmentTransform.position - position).sqrMagnitude > PositionSqrMagnitudeThreshold)
            segmentTransform.position = position;
    }

    private void UpdateSegmentRotation(
        IReadOnlyList<WormSegment> segments,
        int index,
        WormSegment segment,
        Vector3 position)
    {
        WormSegment previous = segments[index - 1];
        if (previous == null)
            return;

        Vector3 direction = previous.CachedTransform.position - position;
        if (direction.sqrMagnitude <= DirectionSqrMagnitudeThreshold)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Transform visual = segment.VisualRoot;
        if (visual == null)
            return;

        Vector3 currentEuler = visual.localEulerAngles;
        if (Mathf.Abs(Mathf.DeltaAngle(currentEuler.z, angle)) <= RotationThresholdDegrees)
            return;

        _temporaryEuler.z = angle;
        visual.localEulerAngles = _temporaryEuler;
    }

    private static float GetSegmentDistance(
        IReadOnlyList<WormSegment> segments,
        IReadOnlyDictionary<WormSegment, float> rollbackAnchoredDistances,
        int index,
        WormSegment segment,
        in WormSegmentChainLayout layout)
    {
        float distance = layout.HeadDistance - index * layout.SegmentSpacing;

        if (ShouldAttachTailToHeadFollowChain(segments, index, segment))
            distance -= GetHeadFollowChainDistanceOffset(segments, layout);

        if (!layout.IsSectionRollback || segment == null)
            return distance;

        return rollbackAnchoredDistances.TryGetValue(segment, out float anchoredDistance)
            ? Mathf.Min(distance, anchoredDistance)
            : distance;
    }

    private static bool ShouldShowHeadFollowChain(
        IReadOnlyList<WormSegment> segments,
        int index,
        WormSegment segment)
    {
        return index == 0
            && segment != null
            && segment.Type == WormSegmentType.Head
            && segment.HasHeadFollowChain
            && segments.Count == 2
            && segments[1] != null
            && segments[1].Type == WormSegmentType.Tail;
    }

    private static bool ShouldAttachTailToHeadFollowChain(
        IReadOnlyList<WormSegment> segments,
        int index,
        WormSegment segment)
    {
        return index == 1
            && segment != null
            && segment.Type == WormSegmentType.Tail
            && segments.Count == 2
            && segments[0] != null
            && segments[0].HasHeadFollowChain;
    }

    private static float GetHeadFollowChainDistanceOffset(
        IReadOnlyList<WormSegment> segments,
        in WormSegmentChainLayout layout)
    {
        WormSegment head = segments.Count > 0 ? segments[0] : null;
        return head != null
            ? (head.HeadFollowPartCount + 1) * GetHeadBridgeSpacing(layout)
                - Mathf.Max(MinimumSpacing, layout.SegmentSpacing)
            : 0f;
    }

    private static float GetHeadBridgeSpacing(in WormSegmentChainLayout layout)
    {
        return Mathf.Max(
            MinimumSpacing,
            layout.SegmentSpacing * layout.HeadBridgeSpacingMultiplier);
    }
}
