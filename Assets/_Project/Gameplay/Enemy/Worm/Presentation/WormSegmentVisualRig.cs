using System;
using UnityEngine;

public sealed class WormSegmentVisualRig
{
    private readonly Transform _visualRoot;
    private readonly Transform _cocoonTransform;
    private readonly SpriteRenderer _anchorRenderer;

    private SpriteRenderer[] _visualRenderers = Array.Empty<SpriteRenderer>();
    private int[] _visualSortingOrderOffsets = Array.Empty<int>();
    private SpriteRenderer[] _tailRenderers = Array.Empty<SpriteRenderer>();
    private Transform[] _tailVisualParts = Array.Empty<Transform>();
    private int[] _tailSortingOrderOffsets = Array.Empty<int>();
    private Vector3[] _tailRotationOffsets = Array.Empty<Vector3>();
    private Transform[] _headFollowParts = Array.Empty<Transform>();
    private Vector3[] _headFollowRotationOffsets = Array.Empty<Vector3>();

    public WormSegmentVisualRig(
        WormSegmentType type,
        Transform segmentTransform,
        Transform visualRoot,
        Transform cocoonTransform,
        SpriteRenderer anchorRenderer)
    {
        _visualRoot = visualRoot;
        _cocoonTransform = cocoonTransform;
        _anchorRenderer = anchorRenderer;

        if (type == WormSegmentType.Tail)
        {
            CacheTailVisualChain();
            return;
        }

        CacheVisualRenderers();

        if (type == WormSegmentType.Head)
            CacheHeadFollowChain(segmentTransform);
    }

    public bool HasTailVisualChain => _tailVisualParts.Length > 1;
    public int TailVisualPartCount => _tailVisualParts.Length;
    public bool HasHeadFollowChain => _headFollowParts.Length > 0;
    public int HeadFollowPartCount => _headFollowParts.Length;

    public void SetSortingOrder(int order)
    {
        if (HasTailVisualChain)
        {
            for (int i = 0; i < _tailRenderers.Length; i++)
            {
                if (_tailRenderers[i] != null)
                    _tailRenderers[i].sortingOrder = order + _tailSortingOrderOffsets[i];
            }

            return;
        }

        if (_visualRenderers.Length > 0)
        {
            for (int i = 0; i < _visualRenderers.Length; i++)
            {
                if (_visualRenderers[i] != null)
                    _visualRenderers[i].sortingOrder = order + _visualSortingOrderOffsets[i];
            }

            return;
        }

        if (_anchorRenderer != null)
            _anchorRenderer.sortingOrder = order;
    }

    public void ResetTailVisualRootRotation()
    {
        if (_visualRoot != null && _visualRoot.localRotation != Quaternion.identity)
            _visualRoot.localRotation = Quaternion.identity;
    }

    public void SetTailVisualPartPose(int index, Vector3 position, float angle)
    {
        SetPartPose(_tailVisualParts, _tailRotationOffsets, index, position, angle);
    }

    public void SetHeadFollowPartPose(int index, Vector3 position, float angle)
    {
        SetPartPose(_headFollowParts, _headFollowRotationOffsets, index, position, angle);
    }

    public void SetHeadFollowChainVisible(bool visible)
    {
        for (int i = 0; i < _headFollowParts.Length; i++)
        {
            Transform part = _headFollowParts[i];

            if (part != null && part.gameObject.activeSelf != visible)
                part.gameObject.SetActive(visible);
        }
    }

    public bool TryGetLastHeadFollowPartPosition(out Vector3 position)
    {
        for (int i = _headFollowParts.Length - 1; i >= 0; i--)
        {
            Transform part = _headFollowParts[i];

            if (part == null)
                continue;

            position = part.position;
            return true;
        }

        position = default;
        return false;
    }

    private void CacheVisualRenderers()
    {
        if (_visualRoot == null)
            return;

        SpriteRenderer[] allRenderers =
            _visualRoot.GetComponentsInChildren<SpriteRenderer>(true);
        SpriteRenderer sortingAnchor = ResolveSortingAnchor(allRenderers);

        if (sortingAnchor == null)
            return;

        int anchorSortingOrder = sortingAnchor.sortingOrder;
        int rendererCount = CountSortingTrackedRenderers(allRenderers, anchorSortingOrder);

        if (rendererCount == 0)
            return;

        _visualRenderers = new SpriteRenderer[rendererCount];
        _visualSortingOrderOffsets = new int[rendererCount];
        int writeIndex = 0;

        for (int i = 0; i < allRenderers.Length; i++)
        {
            SpriteRenderer renderer = allRenderers[i];

            if (!ShouldTrackSortingRenderer(renderer, anchorSortingOrder))
                continue;

            _visualRenderers[writeIndex] = renderer;
            _visualSortingOrderOffsets[writeIndex] =
                renderer.sortingOrder - anchorSortingOrder;
            writeIndex++;
        }
    }

    private void CacheHeadFollowChain(Transform segmentTransform)
    {
        WormSegmentDamageReceiver[] receivers =
            segmentTransform.GetComponentsInChildren<WormSegmentDamageReceiver>(true);
        int receiverCount = CountChildDamageReceivers(receivers, segmentTransform);

        if (receiverCount == 0)
            return;

        _headFollowParts = new Transform[receiverCount];
        _headFollowRotationOffsets = new Vector3[receiverCount];
        int writeIndex = 0;

        for (int i = 0; i < receivers.Length; i++)
        {
            WormSegmentDamageReceiver receiver = receivers[i];

            if (!IsHeadFollowReceiver(receiver, segmentTransform))
                continue;

            _headFollowParts[writeIndex++] = receiver.transform;
        }

        Array.Sort(_headFollowParts, CompareTransformSiblingIndex);

        for (int i = 0; i < _headFollowParts.Length; i++)
            _headFollowRotationOffsets[i] = _headFollowParts[i].localEulerAngles;
    }

    private void CacheTailVisualChain()
    {
        if (_visualRoot == null)
            return;

        SpriteRenderer[] allRenderers =
            _visualRoot.GetComponentsInChildren<SpriteRenderer>(true);
        int rendererCount = CountVisualRenderers(allRenderers);

        if (rendererCount == 0)
            return;

        _tailRenderers = new SpriteRenderer[rendererCount];
        _tailVisualParts = new Transform[rendererCount];
        _tailSortingOrderOffsets = new int[rendererCount];
        _tailRotationOffsets = new Vector3[rendererCount];
        int writeIndex = 0;

        for (int i = 0; i < allRenderers.Length; i++)
        {
            SpriteRenderer renderer = allRenderers[i];

            if (IsVisualRenderer(renderer))
                _tailRenderers[writeIndex++] = renderer;
        }

        Array.Sort(_tailRenderers, CompareTailRenderers);
        int anchorSortingOrder = _tailRenderers[0].sortingOrder;

        for (int i = 0; i < _tailRenderers.Length; i++)
        {
            SpriteRenderer renderer = _tailRenderers[i];
            Transform part = renderer.transform;

            _tailVisualParts[i] = part;
            _tailSortingOrderOffsets[i] = renderer.sortingOrder - anchorSortingOrder;
            _tailRotationOffsets[i] = part.localEulerAngles;
        }
    }

    private static void SetPartPose(
        Transform[] parts,
        Vector3[] rotationOffsets,
        int index,
        Vector3 position,
        float angle)
    {
        if (index < 0 || index >= parts.Length)
            return;

        Transform part = parts[index];

        if (part == null)
            return;

        part.position = position;
        Vector3 rotationOffset = rotationOffsets[index];
        part.rotation = Quaternion.Euler(
            rotationOffset.x,
            rotationOffset.y,
            angle + rotationOffset.z);
    }

    private int CountVisualRenderers(SpriteRenderer[] renderers)
    {
        int count = 0;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (IsVisualRenderer(renderers[i]))
                count++;
        }

        return count;
    }

    private int CountSortingTrackedRenderers(
        SpriteRenderer[] renderers,
        int anchorSortingOrder)
    {
        int count = 0;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (ShouldTrackSortingRenderer(renderers[i], anchorSortingOrder))
                count++;
        }

        return count;
    }

    private static int CountChildDamageReceivers(
        WormSegmentDamageReceiver[] receivers,
        Transform segmentTransform)
    {
        int count = 0;

        for (int i = 0; i < receivers.Length; i++)
        {
            if (IsHeadFollowReceiver(receivers[i], segmentTransform))
                count++;
        }

        return count;
    }

    private bool ShouldTrackSortingRenderer(
        SpriteRenderer renderer,
        int anchorSortingOrder)
    {
        return IsVisualRenderer(renderer) &&
            renderer.sortingOrder <= anchorSortingOrder;
    }

    private bool IsVisualRenderer(SpriteRenderer renderer)
    {
        return renderer != null &&
            (_cocoonTransform == null ||
             !renderer.transform.IsChildOf(_cocoonTransform));
    }

    private SpriteRenderer ResolveSortingAnchor(SpriteRenderer[] renderers)
    {
        if (IsVisualRenderer(_anchorRenderer))
            return _anchorRenderer;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (IsVisualRenderer(renderers[i]))
                return renderers[i];
        }

        return null;
    }

    private static bool IsHeadFollowReceiver(
        WormSegmentDamageReceiver receiver,
        Transform segmentTransform)
    {
        return receiver != null && receiver.transform != segmentTransform;
    }

    private static int CompareTailRenderers(SpriteRenderer left, SpriteRenderer right)
    {
        int sortingComparison = right.sortingOrder.CompareTo(left.sortingOrder);

        return sortingComparison != 0
            ? sortingComparison
            : left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex());
    }

    private static int CompareTransformSiblingIndex(Transform left, Transform right)
    {
        return left.GetSiblingIndex().CompareTo(right.GetSiblingIndex());
    }
}
