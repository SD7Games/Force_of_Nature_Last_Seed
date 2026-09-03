using System.Collections;
using UnityEngine;

/// <summary>
/// Lightweight object pool for worm segments.
/// Maintains separate queues for head, body and tail segments
/// to avoid runtime allocations during worm generation.
/// </summary>
public sealed class WormSegmentPool
{
    private readonly Transform _parent;

    private readonly WormSegment _headPrefab;
    private readonly WormSegment _bodyPrefab;
    private readonly WormSegment _tailPrefab;

    private readonly ObjectPool<WormSegment> _headPool;
    private readonly ObjectPool<WormSegment> _bodyPool;
    private readonly ObjectPool<WormSegment> _tailPool;
    private int _prewarmCreatedThisFrame;

    public WormSegmentPool(WormSegmentPoolSettings settings)
    {
        _parent = settings.Parent;

        _headPrefab = settings.HeadPrefab;
        _bodyPrefab = settings.BodyPrefab;
        _tailPrefab = settings.TailPrefab;

        _headPool = CreatePool(_headPrefab);
        _bodyPool = CreatePool(_bodyPrefab);
        _tailPool = CreatePool(_tailPrefab);
    }

    /// <summary>
    /// Instantiates a predefined number of pooled objects ahead of time
    /// to avoid runtime allocations during gameplay.
    /// </summary>
    public void Prewarm(int bodyCapacity)
    {
        _headPool?.Prewarm(1);
        _tailPool?.Prewarm(1);
        _bodyPool?.Prewarm(bodyCapacity);
    }

    public IEnumerator PrewarmRoutine(int bodyCapacity, int batchSize)
    {
        int safeBatchSize = Mathf.Max(1, batchSize);
        _prewarmCreatedThisFrame = 0;

        yield return PrewarmRoutine(_headPool, 1, safeBatchSize);
        yield return PrewarmRoutine(_tailPool, 1, safeBatchSize);
        yield return PrewarmRoutine(_bodyPool, bodyCapacity, safeBatchSize);
    }

    /// <summary>
    /// Retrieves a segment instance from the pool or instantiates a new one
    /// if the pool is exhausted.
    /// </summary>
    public WormSegment Get(WormSegmentType type)
    {
        ObjectPool<WormSegment> pool = GetPool(type);

        if (pool == null)
        {
            Debug.LogError($"Prefab for {type} is not assigned");
            return null;
        }

        return pool.Rent();
    }

    public void Release(WormSegment segment)
    {
        if (segment == null)
            return;

        GetPool(segment.Type)?.Return(segment);
    }

    private IEnumerator PrewarmRoutine(
        ObjectPool<WormSegment> pool,
        int count,
        int batchSize)
    {
        if (pool == null)
        {
            Debug.LogWarning("WormSegment prefab missing in pool");
            yield break;
        }

        for (int i = 0; i < count; i++)
        {
            pool.PrewarmOne();

            _prewarmCreatedThisFrame++;

            if (_prewarmCreatedThisFrame >= batchSize)
            {
                _prewarmCreatedThisFrame = 0;
                yield return null;
            }
        }
    }

    private ObjectPool<WormSegment> CreatePool(WormSegment prefab)
    {
        if (prefab == null)
            return null;

        return new ObjectPool<WormSegment>(
            () => Object.Instantiate(prefab, _parent),
            PrepareForPool);
    }

    private static void PrepareForPool(WormSegment segment)
    {
        segment.PrepareForWorm();
        segment.gameObject.SetActive(false);
    }

    private ObjectPool<WormSegment> GetPool(WormSegmentType type) => type switch
    {
        WormSegmentType.Head => _headPool,
        WormSegmentType.Body => _bodyPool,
        WormSegmentType.Tail => _tailPool,
        _ => _bodyPool
    };

}
