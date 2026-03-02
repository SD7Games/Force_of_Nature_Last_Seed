using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SpawnEnemyPoint : MonoBehaviour
{
    [Header("Waypoints (Rails)")]
    [SerializeField] private Transform[] _waypoints;

    [Header("Speed")]
    [SerializeField] private float _moveSpeed = 2f;

    [Header("Prefabs (4 segment types)")]
    [SerializeField] private WormSegment _headPrefab;

    [SerializeField] private WormSegment _bodyPrefab;
    [SerializeField] private WormSegment _cocoonPrefab;
    [SerializeField] private WormSegment _tailPrefab;

    [Header("Worm Generation")]
    [SerializeField] private WormController _wormController;

    [Min(3)]
    [SerializeField] private int _totalLength = 30;

    [Min(1)]
    [SerializeField] private int _minBodyBeforeCocoon = 4;

    [Min(1)]
    [SerializeField] private int _maxBodyBeforeCocoon = 10;

    [Header("Pooling")]
    [Tooltip("Extra pooled elements on top of Total Length (safety margin).")]
    [Min(0)]
    [SerializeField] private int _poolPadding = 5;

    private readonly Queue<WormSegment> _headPool = new();
    private readonly Queue<WormSegment> _bodyPool = new();
    private readonly Queue<WormSegment> _cocoonPool = new();
    private readonly Queue<WormSegment> _tailPool = new();

    private void Awake()
    {
        if (_wormController == null)
            Debug.LogError("WormController is not assigned!", this);

        if (_headPrefab == null || _bodyPrefab == null || _cocoonPrefab == null || _tailPrefab == null)
            Debug.LogError("One or more worm prefabs are not assigned!", this);

        if (_minBodyBeforeCocoon > _maxBodyBeforeCocoon)
        {
            int tmp = _minBodyBeforeCocoon;
            _minBodyBeforeCocoon = _maxBodyBeforeCocoon;
            _maxBodyBeforeCocoon = tmp;
        }

        int capacity = Mathf.Max(3, _totalLength) + _poolPadding;

        Prewarm(_headPrefab, 1, _headPool);
        Prewarm(_tailPrefab, 1, _tailPool);
        Prewarm(_bodyPrefab, capacity, _bodyPool);
        Prewarm(_cocoonPrefab, capacity, _cocoonPool);
    }

    private void Start()
    {
        SpawnWorm();
    }

    public void SpawnWorm()
    {
        if (_waypoints == null || _waypoints.Length == 0)
        {
            Debug.LogWarning($"{name}: No waypoints assigned.");
            return;
        }

        List<WormSegmentType> pattern = BuildPattern();
        List<WormSegment> createdSegments = new(pattern.Count);

        for (int i = 0; i < pattern.Count; i++)
        {
            WormSegment segment = GetFromPool(pattern[i]);

            segment.StartMove(_waypoints, _moveSpeed);

            createdSegments.Add(segment);
        }

        _wormController.Initialize(createdSegments);

        for (int i = 0; i < createdSegments.Count; i++)
            createdSegments[i].AssignController(_wormController);
    }

    private List<WormSegmentType> BuildPattern()
    {
        int length = Mathf.Max(3, _totalLength);

        List<WormSegmentType> result = new(length)
        {
            WormSegmentType.Head
        };

        int currentCount = 1;

        while (currentCount < length - 1)
        {
            int bodyCount = Random.Range(_minBodyBeforeCocoon, _maxBodyBeforeCocoon + 1);

            for (int i = 0; i < bodyCount; i++)
            {
                if (currentCount >= length - 1)
                    break;

                result.Add(WormSegmentType.Body);
                currentCount++;
            }

            if (currentCount < length - 1)
            {
                result.Add(WormSegmentType.Cocoon);
                currentCount++;
            }
        }

        result.Add(WormSegmentType.Tail);
        return result;
    }

    private WormSegment GetFromPool(WormSegmentType type)
    {
        Queue<WormSegment> pool = GetPool(type);

        if (pool.Count > 0)
            return pool.Dequeue();

        WormSegment prefab = GetPrefab(type);
        WormSegment created = Instantiate(prefab, transform);
        created.gameObject.SetActive(false);
        return created;
    }

    private void Prewarm(WormSegment prefab, int count, Queue<WormSegment> pool)
    {
        if (prefab == null || count <= 0)
            return;

        for (int i = 0; i < count; i++)
        {
            WormSegment created = Instantiate(prefab, transform);
            created.gameObject.SetActive(false);
            pool.Enqueue(created);
        }
    }

    private Queue<WormSegment> GetPool(WormSegmentType type)
    {
        return type switch
        {
            WormSegmentType.Head => _headPool,
            WormSegmentType.Body => _bodyPool,
            WormSegmentType.Cocoon => _cocoonPool,
            WormSegmentType.Tail => _tailPool,
            _ => _bodyPool
        };
    }

    private WormSegment GetPrefab(WormSegmentType type)
    {
        return type switch
        {
            WormSegmentType.Head => _headPrefab,
            WormSegmentType.Body => _bodyPrefab,
            WormSegmentType.Cocoon => _cocoonPrefab,
            WormSegmentType.Tail => _tailPrefab,
            _ => _bodyPrefab
        };
    }
}