using System;
using UnityEngine;

public sealed class WormSegmentPoolSettings
{
    public WormSegmentPoolSettings(
        Transform parent,
        WormSegment headPrefab,
        WormSegment bodyPrefab,
        WormSegment tailPrefab)
    {
        Parent = parent != null ? parent : throw new ArgumentNullException(nameof(parent));
        HeadPrefab = headPrefab != null
            ? headPrefab
            : throw new ArgumentNullException(nameof(headPrefab));
        BodyPrefab = bodyPrefab != null
            ? bodyPrefab
            : throw new ArgumentNullException(nameof(bodyPrefab));
        TailPrefab = tailPrefab != null
            ? tailPrefab
            : throw new ArgumentNullException(nameof(tailPrefab));
    }

    public Transform Parent { get; }
    public WormSegment HeadPrefab { get; }
    public WormSegment BodyPrefab { get; }
    public WormSegment TailPrefab { get; }
}
