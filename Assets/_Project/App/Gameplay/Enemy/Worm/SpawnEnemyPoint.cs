using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SpawnEnemyPoint : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private WormSegment _headPrefab;

    [SerializeField] private WormSegment _bodyPrefab;
    [SerializeField] private WormSegment _cocoonPrefab;
    [SerializeField] private WormSegment _tailPrefab;

    [Header("Controllers")]
    [SerializeField] private WormController _wormController;

    [SerializeField] private WormCombatController _wormCombat;

    [Header("Generation")]
    [Min(3)]
    [SerializeField] private int _totalLength = 60;

    [Header("Cocoon spacing")]
    [SerializeField] private int _minBodyBeforeCocoon = 5;

    [SerializeField] private int _maxBodyBeforeCocoon = 10;

    [Header("Pooling")]
    [SerializeField] private int _poolPadding = 10;

    private readonly Queue<WormSegment> _headPool = new();
    private readonly Queue<WormSegment> _bodyPool = new();
    private readonly Queue<WormSegment> _cocoonPool = new();
    private readonly Queue<WormSegment> _tailPool = new();

    private bool _isSpawned;

    private void Awake()
    {
        if (_wormController == null)
            Debug.LogError("WormController not assigned", this);

        if (_wormCombat == null)
            Debug.LogError("WormCombatController not assigned", this);

        if (_minBodyBeforeCocoon > _maxBodyBeforeCocoon)
            (_minBodyBeforeCocoon, _maxBodyBeforeCocoon) = (_maxBodyBeforeCocoon, _minBodyBeforeCocoon);

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
        if (_isSpawned)
            return;

        List<WormSegmentType> pattern = BuildPattern();

        List<WormSegment> segments = new(pattern.Count);

        WormSegment head = null;
        WormSegment tail = null;

        for (int i = 0; i < pattern.Count; i++)
        {
            WormSegment seg = GetFromPool(pattern[i]);

            seg.Activate();

            if (seg.Type == WormSegmentType.Head)
                head = seg;

            if (seg.Type == WormSegmentType.Tail)
                tail = seg;

            segments.Add(seg);
        }

        _wormController.Init(segments);

        List<WormSection> sections = BuildSectionsByCocoons(segments);

        _wormCombat.Init(head, tail, sections);

        foreach (var seg in segments)
        {
            var receiver = seg.GetComponent<WormSegmentDamageReceiver>();

            if (receiver == null)
                receiver = seg.gameObject.AddComponent<WormSegmentDamageReceiver>();

            receiver.Initialize(_wormCombat, seg);
        }

        _isSpawned = true;
    }

    private List<WormSection> BuildSectionsByCocoons(List<WormSegment> segments)
    {
        List<WormSection> sections = new();

        WormSection current = null;

        for (int i = 0; i < segments.Count; i++)
        {
            WormSegment seg = segments[i];

            if (seg.Type is WormSegmentType.Head or WormSegmentType.Tail)
                continue;

            if (current == null)
            {
                current = new WormSection();

                current.Init(WormSectionHPGenerator.GetHP(sections.Count));

                sections.Add(current);
            }

            current.AddSegment(seg);

            seg.Section = current;

            if (seg.Type == WormSegmentType.Cocoon)
                current = null;
        }

        return sections;
    }

    private List<WormSegmentType> BuildPattern()
    {
        int length = Mathf.Max(3, _totalLength);

        List<WormSegmentType> result = new(length)
        {
            WormSegmentType.Head
        };

        int bodyCounter = 0;

        int nextCocoon = Random.Range(_minBodyBeforeCocoon, _maxBodyBeforeCocoon + 1);

        while (result.Count < length - 1)
        {
            bodyCounter++;

            if (bodyCounter >= nextCocoon)
            {
                result.Add(WormSegmentType.Cocoon);

                bodyCounter = 0;

                nextCocoon = Random.Range(_minBodyBeforeCocoon, _maxBodyBeforeCocoon + 1);
            }
            else
            {
                result.Add(WormSegmentType.Body);
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

        WormSegment created = Instantiate(GetPrefab(type), transform);

        created.gameObject.SetActive(false);

        return created;
    }

    private void Prewarm(WormSegment prefab, int count, Queue<WormSegment> pool)
    {
        if (prefab == null)
            return;

        for (int i = 0; i < count; i++)
        {
            WormSegment seg = Instantiate(prefab, transform);

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