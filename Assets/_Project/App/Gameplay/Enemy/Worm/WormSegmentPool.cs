using System.Collections.Generic;
using UnityEngine;

public sealed class WormSegmentPool
{
    private readonly Transform _parent;

    private readonly WormSegment _headPrefab;
    private readonly WormSegment _bodyPrefab;
    private readonly WormSegment _cocoonPrefab;
    private readonly WormSegment _tailPrefab;

    private readonly Queue<WormSegment> _headPool = new();
    private readonly Queue<WormSegment> _bodyPool = new();
    private readonly Queue<WormSegment> _cocoonPool = new();
    private readonly Queue<WormSegment> _tailPool = new();

    public WormSegmentPool(
        Transform parent,
        WormSegment head,
        WormSegment body,
        WormSegment cocoon,
        WormSegment tail)
    {
        _parent = parent;

        _headPrefab = head;
        _bodyPrefab = body;
        _cocoonPrefab = cocoon;
        _tailPrefab = tail;
    }

    public void Prewarm(int bodyCapacity)
    {
        Prewarm(_headPrefab, 1, _headPool);
        Prewarm(_tailPrefab, 1, _tailPool);

        Prewarm(_bodyPrefab, bodyCapacity, _bodyPool);
        Prewarm(_cocoonPrefab, bodyCapacity, _cocoonPool);
    }

    public WormSegment Get(WormSegmentType type)
    {
        Queue<WormSegment> pool = GetPool(type);

        if (pool.Count > 0)
            return pool.Dequeue();

        WormSegment prefab = GetPrefab(type);

        if (prefab == null)
        {
            Debug.LogError($"Prefab for {type} is not assigned");
            return null;
        }

        WormSegment created = Object.Instantiate(prefab, _parent);
        created.gameObject.SetActive(false);

        return created;
    }

    private void Prewarm(WormSegment prefab, int count, Queue<WormSegment> pool)
    {
        if (prefab == null)
        {
            Debug.LogWarning("WormSegment prefab missing in pool");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            WormSegment seg = Object.Instantiate(prefab, _parent);

            seg.gameObject.SetActive(false);

            pool.Enqueue(seg);
        }
    }

    private Queue<WormSegment> GetPool(WormSegmentType type) => type switch
    {
        WormSegmentType.Head => _headPool,
        WormSegmentType.Body => _bodyPool,
        WormSegmentType.Cocoon => _cocoonPool,
        WormSegmentType.Tail => _tailPool,
        _ => _bodyPool
    };

    private WormSegment GetPrefab(WormSegmentType type) => type switch
    {
        WormSegmentType.Head => _headPrefab,
        WormSegmentType.Body => _bodyPrefab,
        WormSegmentType.Cocoon => _cocoonPrefab,
        WormSegmentType.Tail => _tailPrefab,
        _ => _bodyPrefab
    };
}