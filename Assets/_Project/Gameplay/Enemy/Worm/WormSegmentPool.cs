using System.Threading;
using UnityEngine;

public sealed class WormSegmentPool
{
    private readonly Transform _parent;

    private readonly WormSegment _headPrefab;
    private readonly WormSegment _bodyPrefab;
    private readonly WormSegment _tailPrefab;

    private readonly ObjectPool<WormSegment> _headPool;
    private readonly ObjectPool<WormSegment> _bodyPool;
    private readonly ObjectPool<WormSegment> _tailPool;
    private readonly IWormCocoonShakeClock _cocoonShakeClock;
    private int _prewarmCreatedThisFrame;

    public WormSegmentPool(
        WormSegmentPoolSettings settings,
        IWormCocoonShakeClock cocoonShakeClock)
    {
        _parent = settings.Parent;
        _cocoonShakeClock = cocoonShakeClock;

        _headPrefab = settings.HeadPrefab;
        _bodyPrefab = settings.BodyPrefab;
        _tailPrefab = settings.TailPrefab;

        _headPool = CreatePool(_headPrefab);
        _bodyPool = CreatePool(_bodyPrefab);
        _tailPool = CreatePool(_tailPrefab);
    }

    public void Prewarm(int bodyCapacity)
    {
        _headPool?.Prewarm(1);
        _tailPool?.Prewarm(1);
        _bodyPool?.Prewarm(bodyCapacity);
    }

    public async Awaitable PrewarmAsync(
        int bodyCapacity,
        int batchSize,
        CancellationToken cancellationToken)
    {
        int safeBatchSize = Mathf.Max(1, batchSize);
        _prewarmCreatedThisFrame = 0;

        await PrewarmPoolAsync(_headPool, 1, safeBatchSize, cancellationToken);
        await PrewarmPoolAsync(_tailPool, 1, safeBatchSize, cancellationToken);
        await PrewarmPoolAsync(_bodyPool, bodyCapacity, safeBatchSize, cancellationToken);
    }

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

    private async Awaitable PrewarmPoolAsync(
        ObjectPool<WormSegment> pool,
        int count,
        int batchSize,
        CancellationToken cancellationToken)
    {
        if (pool == null)
        {
            Debug.LogWarning("WormSegment prefab missing in pool");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            pool.PrewarmOne();

            _prewarmCreatedThisFrame++;

            if (_prewarmCreatedThisFrame >= batchSize)
            {
                _prewarmCreatedThisFrame = 0;
                await Awaitable.NextFrameAsync(cancellationToken);
            }
        }
    }

    private ObjectPool<WormSegment> CreatePool(WormSegment prefab)
    {
        if (prefab == null)
            return null;

        return new ObjectPool<WormSegment>(
            () => CreateSegment(prefab),
            PrepareForPool);
    }

    private WormSegment CreateSegment(WormSegment prefab)
    {
        WormSegment segment = Object.Instantiate(prefab, _parent);
        segment.InitializePresentation(_cocoonShakeClock);
        return segment;
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
